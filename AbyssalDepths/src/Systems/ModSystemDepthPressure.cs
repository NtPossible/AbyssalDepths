using AbyssalDepths.src.CollectibleBehaviour;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
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

            bool hasFunctionalSuit = TryGetFunctionalSuit(player, out List<ItemSlot> suitSlots, out int safeDepth);
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
                TryPlaySuitCreak(world, entity, waterDepth, safeDepth, suitSlots);

                DamageSuit(world, entity, suitSlots, suitDamagePerSecond);

                // If the full suit still has durability after the damage tick, the suit is still protecting
                if (!SuitDamaged(suitSlots))
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

        private static bool TryGetFunctionalSuit(IPlayer player, out List<ItemSlot> suitSlots, out int safeDepth)
        {
            suitSlots = new List<ItemSlot>();
            safeDepth = 0;

            if (!ModSystemDivingSuit.TryGetEquippedDivingSuitSet(player, out string suitSet))
            {
                return false;
            }

            suitSlots = GetEquippedSuitSlots(player, suitSet);

            if (SuitDamaged(suitSlots))
            {
                return false;
            }

            safeDepth = GetSuitSafeDepthFromBehavior(suitSlots);
            return safeDepth > 0;
        }

        private static CollectibleBehaviorDivingSuit? GetDivingSuitBehavior(ItemSlot? slot)
        {
            if (slot == null || slot.Itemstack == null)
            {
                return null;
            }

            return slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingSuit>();
        }

        private static void TryPlaySuitCreak(IServerWorldAccessor world, EntityPlayer entity, int waterDepth, int safeDepth, List<ItemSlot> suitSlots)
        {
            int depthOver = GameMath.Max(0, waterDepth - safeDepth);

            float chance = GameMath.Clamp(0.01f + 0.005f * depthOver, 0f, 0.08f);

            if (world.Rand.NextDouble() < chance)
            {
                AssetLocation? sound = GetSuitCreakSoundFromBehavior(suitSlots);
                if (sound != null)
                {
                    world.PlaySoundAt(sound, entity, null, true, 14f);
                }
            }
        }

        private static AssetLocation? GetSuitCreakSoundFromBehavior(List<ItemSlot> slots)
        {
            foreach (ItemSlot slot in slots)
            {
                CollectibleBehaviorDivingSuit? behavior = GetDivingSuitBehavior(slot);
                if (behavior == null)
                {
                    continue;
                }

                AssetLocation? creak = behavior.CreakSound;
                if (creak != null)
                {
                    return creak;
                }
            }

            return null;
        }

        private static AssetLocation? GetSuitBreakSoundFromBehavior(List<ItemSlot> slots)
        {
            foreach (ItemSlot slot in slots)
            {
                CollectibleBehaviorDivingSuit? behavior = GetDivingSuitBehavior(slot);
                if (behavior == null)
                {
                    continue;
                }

                AssetLocation? breakSound = behavior.BreakSound;
                if (breakSound != null)
                {
                    return breakSound;
                }
            }

            return null;
        }

        private static int GetSuitSafeDepthFromBehavior(List<ItemSlot> suitSlots)
        {
            int maxSafeDepth = 0;

            foreach (ItemSlot slot in suitSlots)
            {
                CollectibleBehaviorDivingSuit? behavior = GetDivingSuitBehavior(slot);
                if (behavior == null)
                {
                    continue;
                }

                int safeDepth = behavior.SafeDepth;
                if (safeDepth > maxSafeDepth)
                {
                    maxSafeDepth = safeDepth;
                }
            }

            return maxSafeDepth;
        }

        // checks a radius around the player's head for maximum water depth
        private static int GetWaterDepth(IServerWorldAccessor world, EntityPlayer entity, int effectiveSafeDepth)
        {
            IBlockAccessor blockAccessor = world.BlockAccessor;

            EntityPos entityPosition = entity.ServerPos ?? entity.Pos;
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

        // Measures how much water is above a position, stopping at open air or flowing water that is not enclosed.
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

        // Checks whether a water block is open to air on enough sides to prevent pressure buildup.
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

        private static List<ItemSlot> GetEquippedSuitSlots(IPlayer player, string suitSet)
        {
            List<ItemSlot> slots = new();

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return slots;
            }

            foreach (ItemSlot slot in inventory)
            {
                if (slot?.Itemstack == null)
                {
                    continue;
                }

                CollectibleBehaviorDivingSuit? behavior = GetDivingSuitBehavior(slot);
                if (behavior == null)
                {
                    continue;
                }

                if (behavior.SuitSet != suitSet)
                {
                    continue;
                }

                string bodypart = slot.Itemstack.Item.Variant["bodypart"];

                if (bodypart != "head" && bodypart != "body" && bodypart != "legs")
                {
                    continue;
                }

                slots.Add(slot);
            }

            return slots;
        }

        private static bool SuitDamaged(List<ItemSlot> slots)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot?.Itemstack == null)
                {
                    return true;
                }

                int durability = slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack);
                if (durability <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DamageSuit(IServerWorldAccessor world, EntityPlayer entity, List<ItemSlot> slots, int amountPerPiece)
        {
            bool anyJustBroke = false;

            foreach (ItemSlot slot in slots)
            {
                if (slot?.Itemstack == null)
                {
                    continue;
                }

                CollectibleBehaviorDivingSuit? behavior = GetDivingSuitBehavior(slot);
                if (behavior == null)
                {
                    continue;   
                }

                int before = slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack);

                if (before <= 0)
                {
                    continue;
                }

                slot.Itemstack.Collectible.DamageItem(world, entity, slot, amountPerPiece);

                int after = slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack);

                if (after <= 0 && before > 0)
                {
                    anyJustBroke = true;
                }
            }

            if (anyJustBroke)
            {
                AssetLocation? sound = GetSuitBreakSoundFromBehavior(slots);
                if (sound != null)
                {
                    world.PlaySoundAt(sound, entity, null, true, 32f);
                }
            }
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