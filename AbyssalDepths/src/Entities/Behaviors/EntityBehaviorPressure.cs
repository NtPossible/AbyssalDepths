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
        private float TickTimer = 0f;

        private const float PressureUpdateInterval = 1f;

        private bool SealedEnvironment = false;

        private float LegacyBarotraumaTimer = 0f;

        private static readonly Random Rand = Random.Shared; 

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

            if (AbyssalDepthsModSystem.Config.EnableLegacyPressure)
            {
                TickLegacyBarotrauma(deltaTime, player, world);
            }

            UpdateSuitPressure(deltaTime, player, world);
        }

        private void UpdateSuitPressure(float deltaTime, EntityPlayer player, IServerWorldAccessor world)
        {
            TickTimer -= deltaTime;
            if (TickTimer > 0)
            {
                return;
            }
            TickTimer = PressureUpdateInterval;

            if (!player.IsEyesSubmerged())
            {
                SealedEnvironment = false;
                return;
            }

            bool hasFunctionalSuit = ModSystemDivingEquipment.GetFunctionalSuit(player.Player, out List<ItemSlot> suitSlots, out int suitSafeDepth);

            if (!hasFunctionalSuit && !SealedEnvironment)
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

            if (SealedEnvironment && !sealedNow && waterDepth > 0)
            {
                ApplyPressureShock(player, waterDepth);
            }

            SealedEnvironment = sealedNow;
        }

        // legacy stuff - barotrauma system based on depth
        public void TickLegacyBarotrauma(float deltaTime, EntityPlayer player, IServerWorldAccessor world)
        {
            LegacyBarotraumaTimer -= deltaTime;
            if (LegacyBarotraumaTimer > 0)
            {
                return;
            }

            if (!player.IsEyesSubmerged())
            {
                LegacyBarotraumaTimer = 1f;
                return;
            }

            int depth = GetWaterDepth(world, player, 999);
            if (depth <= 0)
            {
                LegacyBarotraumaTimer = 1f;
                return;
            }

            int depthOver = depth - AbyssalDepthsModSystem.Config.LegacyBaseSafeDepth;
            if (depthOver <= 10)
            {
                LegacyBarotraumaTimer = 1f;
                return;
            }

            if(ModSystemDivingEquipment.GetFunctionalSuit(player.Player, out _, out _))
            {
                LegacyBarotraumaTimer = 1f;
                return;
            }

            ApplyLegacyBarotrauma(player, depthOver);
            LegacyBarotraumaTimer = GetNextInterval(depthOver);
        }


        // legacy stuff - depth damage system
        private static void ApplyLegacyBarotrauma(EntityPlayer player, int depthOver)
        {
            float triggerChance = Math.Clamp(depthOver / 40f, 0f, 0.75f);
            if (Rand.NextDouble() > triggerChance)
            {
                return;
            }

            float depthFactor = Math.Clamp(depthOver / 60f, 0f, 1f);

            float minDamage = 0.1f + depthFactor * 0.4f;
            float maxDamage = 0.2f + depthFactor * 5.8f;

            float damage = minDamage + (float)Rand.NextDouble() * (maxDamage - minDamage);

            ApplyPressureDamage(player, damage);
        }

        // legacy stuff - timing for barotrauma
        public static float GetNextInterval(int depthOver)
        {
            float depthFactor = Math.Clamp(depthOver / 50f, 0f, 1f);

            float minInterval = 3f;
            float maxInterval = 6f;

            return (maxInterval - (maxInterval - minInterval) * depthFactor) + (float)(Rand.NextDouble() * 0.5);
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
        public static int GetColumnWaterDepth(IBlockAccessor blockAccessor, int x, int z, int startY, int maxY, BlockPos reusablePos)
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

        public static void ApplyPressureShock(EntityPlayer player, int depth)
        {
            float ambientPressure = 1f + (depth / 10f);
            float pressureDifference = ambientPressure - 1f;

            float damage = MathF.Pow(pressureDifference, 2.2f);

            ApplyPressureDamage(player, damage);
        }

        public static int GetSuitDamagePerSecond(int depthOver)
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