using AbyssalDepths.src.Systems;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AbyssalDepths.src.Entities.Behaviors
{
    public class EntityBehaviorPressure : EntityBehavior
    {
        public override string PropertyName() => "Pressure";
        private float barotraumaTimer = 0f;
        private static readonly Random rand = Random.Shared;

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

            barotraumaTimer -= deltaTime;
            if (barotraumaTimer > 0)
            {
                return;
            }

            if (!player.IsEyesSubmerged())
            {
                barotraumaTimer = 1f;
                return;
            }

            int depth = GetWaterDepth(world, player, 999);
            if (depth <= 0)
            {
                barotraumaTimer = 1f;
                return;
            }

            int depthOver = depth - AbyssalDepthsModSystem.Config.BaseSafeDepth;
            if (depthOver <= 10)
            {
                barotraumaTimer = 1f;
                return;
            }

            if (ModSystemDivingEquipment.GetFunctionalSuit(player.Player, out _, out _))
            {
                barotraumaTimer = 1f;
                return;
            }

            ApplyBarotrauma(player, depthOver);
            barotraumaTimer = GetNextInterval(depthOver);
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

        private static void ApplyBarotrauma(EntityPlayer player, int depthOver)
        {
            float triggerChance = Math.Clamp(depthOver / 40f, 0f, 0.75f);

            if (rand.NextDouble() > triggerChance)
            {
                return;
            }

            float depthFactor = Math.Clamp(depthOver / 60f, 0f, 1f);

            float minDamage = 0.1f + depthFactor * 0.4f;
            float maxDamage = 0.2f + depthFactor * 5.8f;

            float damage = minDamage + (float)rand.NextDouble() * (maxDamage - minDamage);

            ApplyPressureDamage(player, damage);
        }

        private static float GetNextInterval(int depthOver)
        {
            float depthFactor = Math.Clamp(depthOver / 50f, 0f, 1f);

            float minInterval = 3f;
            float maxInterval = 6f;

            return (maxInterval - (maxInterval - minInterval) * depthFactor) + (float)(rand.NextDouble() * 0.5);
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