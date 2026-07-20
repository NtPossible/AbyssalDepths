using AbyssalDepths.src.Systems;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AbyssalDepths.src.Entities.Behaviors
{
    public class EntityBehaviorPressure : EntityBehavior
    {
        public override string PropertyName() => "Pressure";
        private float tickTimer = 0f;

        private const float PressureUpdateInterval = 1f;

        private bool sealedEnvironment = false;

        public EntityBehaviorPressure(Entity entity) : base(entity)
        {
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!AbyssalDepthsModSystem.Config.EnablePressure)
            {
                return;
            }

            if (entity.State == EnumEntityState.Inactive)
            {
                return;
            }

            if (entity is not EntityPlayer player || !player.Alive)
            {
                return;
            }

            if (entity.World is not IServerWorldAccessor world)
            {
                return;
            }

            tickTimer -= deltaTime;
            if (tickTimer > 0)
            {
                return;
            }
            tickTimer = PressureUpdateInterval;

            if (!player.IsEyesSubmerged())
            {
                sealedEnvironment = false;
                return;
            }

            bool hasFunctionalSuit = ModSystemDivingEquipment.GetFunctionalSuit(player.Player, out List<ItemSlot> suitSlots, out int suitSafeDepth);

            if (!hasFunctionalSuit && !sealedEnvironment)
            {
                return;
            }

            int waterDepth = GetWaterDepth(world, player, hasFunctionalSuit ? suitSafeDepth : 0);

            bool sealedNow = hasFunctionalSuit;

            if (hasFunctionalSuit && waterDepth > suitSafeDepth)
            {
                int depthOver = waterDepth - suitSafeDepth;

                ModSystemDivingEquipment.TryPlaySuitCreak(world, player, waterDepth, suitSafeDepth, suitSlots);
                ModSystemDivingEquipment.DamageSuit(world, player, suitSlots, GetSuitDamagePerSecond(depthOver));

                sealedNow = !ModSystemDivingEquipment.SuitDamaged(suitSlots);
            }

            if (sealedEnvironment && !sealedNow && waterDepth > 0)
            {
                ApplyPressureShock(player, waterDepth);
            }

            sealedEnvironment = sealedNow;
        }

        public static int GetWaterDepth(IServerWorldAccessor world, EntityPlayer entity, int effectiveSafeDepth)
        {
            IBlockAccessor blockAccessor = world.BlockAccessor;

            BlockPos centerPos = entity.Pos.AsBlockPos;

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
                    int depth = GetColumnWaterDepth(blockAccessor, centerPos.X + dx, centerPos.Z + dz, headY, maxY, scanPos);
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
        public static bool IsExposedToAir(IBlockAccessor blockAccessor, int x, int y, int z, BlockPos reusablePos)
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

        private static void ApplyPressureShock(EntityPlayer player, int depth)
        {
            float ambientPressure = 1f + (depth / 10f);
            float pressureDifference = ambientPressure - 1f;

            float damage = MathF.Pow(pressureDifference, 2.2f);

            ApplyPressureDamage(player, damage);
        }

        private static int GetSuitDamagePerSecond(int depthOver)
        {
            float severity = 0.5f + depthOver * 0.5f;
            return (int)severity;
        }

        public static void ApplyPressureDamage(EntityPlayer entity, float amount)
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