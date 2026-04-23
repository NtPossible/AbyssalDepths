using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AbyssalDepths.src.Systems
{
    public class DepthPressureSystem : ModSystem
    {
        private ICoreServerAPI? sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.RegisterGameTickListener(OnServerTick1s, 1000, 200);
        }

        private void OnServerTick1s(float dt)
        {
            if (sapi?.World == null)
            {
                return;
            }

            IServerWorldAccessor world = sapi.World;

            foreach (IPlayer player in world.AllOnlinePlayers)
            {
                ProcessPlayer(world, player);
            }
        }

        private static void ProcessPlayer(IServerWorldAccessor world, IPlayer player)
        {
            if (!AbyssalDepthsModSystem.Config.EnablePressure)
            {
                return;
            }

            if (player?.Entity is not EntityPlayer entity || !entity.Alive)
            {
                return;
            }

            if (entity.World == null || entity.Properties == null)
            {
                return;
            }

            if (!player.Entity.IsEyesSubmerged())
            {
                return;
            }

            bool hasFunctionalSuit = ModSystemUnderwaterEquipment.GetFunctionalSuit(player, out List<ItemSlot> suitSlots, out int safeDepth);
            int baseSafeDepth = AbyssalDepthsModSystem.Config.BaseSafeDepth;
            int effectiveSafeDepth = hasFunctionalSuit ? safeDepth : baseSafeDepth;
            int waterDepth = GetWaterDepth(world, entity, effectiveSafeDepth);
            if (waterDepth <= 0)
            {
                return;
            }

            int depthOver = waterDepth - effectiveSafeDepth;
            if (depthOver <= 0)
            {
                return;
            }

            int suitDamagePerSecond = hasFunctionalSuit ? GetSuitDamagePerSecond(depthOver) : 0;
            float playerDamagePerSecond = GetPlayerDamagePerSecond(depthOver);

            // If the worn suit is beyond its safe depth, damage it first
            if (hasFunctionalSuit && waterDepth > safeDepth)
            {
                ModSystemUnderwaterEquipment.TryPlaySuitCreak(world, entity, waterDepth, safeDepth, suitSlots);

                ModSystemUnderwaterEquipment.DamageSuit(world, entity, suitSlots, suitDamagePerSecond);

                // If the full suit still has durability after the damage tick, the suit is still protecting
                if (!ModSystemUnderwaterEquipment.SuitDamaged(suitSlots))
                {
                    return;
                }
            }

            // if no suit or suit is broken, take damage
            ApplyPressureDamage(entity, playerDamagePerSecond);
        }

        private static float GetDepthSeverity(int depthOver)
        {
            if (depthOver <= 0)
            {
                return 0f;
            }

            // Gentle up to 10 over then ramps hard
            if (depthOver <= 10)
            {
                return 0.3f * depthOver;
            }

            int past = depthOver - 10;
            return 3f + past * 0.5f;
        }

        private static int GetSuitDamagePerSecond(int depthOver)
        {
            return (int)GetDepthSeverity(depthOver);
        }

        private static float GetPlayerDamagePerSecond(int depthOver)
        {
            return GetDepthSeverity(depthOver);
        }

        // checks a radius around the player's head for maximum water depth
        private static int GetWaterDepth(IServerWorldAccessor world, EntityPlayer entity, int effectiveSafeDepth)
        {
            IBlockAccessor blockAccessor = world.BlockAccessor;

            EntityPos entityPosition = entity.Pos;
            BlockPos centerPos = entityPosition.AsBlockPos;

            int maxY = blockAccessor.MapSizeY - 1;

            int headY = GameMath.Clamp(centerPos.Y + 1, 0, maxY);
            if (headY > maxY)
            {
                return 0;
            }

            int maxDepth = 0;

            BlockPos scanPos = new(centerPos.X, headY, centerPos.Z);

            const int sampleRadius = 3;

            // If we're way beyond safe depth precise suit damage scaling doesn't matter as much
            const int depthOverScanCap = 80;
            int scanStopDepth = effectiveSafeDepth + depthOverScanCap;
            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
            {
                for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                {
                    int x = centerPos.X + dx;
                    int z = centerPos.Z + dz;

                    int depth = GetColumnWaterDepth(blockAccessor, x, z, headY, maxY, scanPos);
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;

                        if (maxDepth >= scanStopDepth)
                        {
                            return maxDepth;
                        }
                    }
                }
            }

            return maxDepth;
        }

        // Measures how much water is above a position, stopping at open air or flowing water that is not enclosed
        private static int GetColumnWaterDepth(IBlockAccessor blockAccessor, int x, int z, int startY, int maxY, BlockPos reusablePos)
        {
            int depth = 0;

            for (int y = startY; y <= maxY; y++)
            {
                reusablePos.Set(x, y, z);
                Block block = blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid);

                if (block == null || !block.IsLiquid())
                {
                    break;
                }

                depth++;

                // Stop at a real surface
                reusablePos.Set(x, y + 1, z);
                Block above = blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid);
                if (above == null || !above.IsLiquid())
                {
                    break;
                }

                // Stop if the water is open to air on multiple sides (e.g. waterfalls)
                if (IsExposedToAir(blockAccessor, x, y, z, reusablePos))
                {
                    break;
                }
            }

            return depth;
        }

        // Checks whether a water block is open to air on enough sides to prevent pressure buildup
        private static bool IsExposedToAir(IBlockAccessor blockAccessor, int x, int y, int z, BlockPos reusablePos)
        {
            reusablePos.Set(x + 1, y, z);
            if (!blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid).IsLiquid())
            {
                return true;
            }
            reusablePos.Set(x - 1, y, z);
            if (!blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid).IsLiquid())
            {
                return true;
            }
            reusablePos.Set(x, y, z + 1);
            if (!blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid).IsLiquid())
            {
                return true;
            }
            reusablePos.Set(x, y, z - 1);
            if (!blockAccessor.GetBlock(reusablePos, BlockLayersAccess.Fluid).IsLiquid())
            {
                return true;
            }
            return false;
        }
        private static void ApplyPressureDamage(EntityPlayer entity, float amount)
        {
            if (amount <= 0 || !entity.Alive)
            {
                return;
            }

            DamageSource damageSource = new()
            {
                Source = EnumDamageSource.Drown,
                Type = EnumDamageType.Crushing,
                DamageTier = 10
            };

            entity.ReceiveDamage(damageSource, amount);
        }
    }
}