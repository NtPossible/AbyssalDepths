using System.Collections.Generic;
using AbyssalDepths.src.Items.Wearable;
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

        // Depth thresholds
        private const int depth20 = 20;
        private const int depth40 = 40;
        private const int depth60 = 60;

        // Damage per second at each depth level
        private const float damageDepth20 = 1f;
        private const float damageDepth40 = 3f;
        private const float damageDepth60 = 9000f;

        // Suit durability loss per second in each depth level
        private const int suitDamageDepth20 = 2;
        private const int suitDamageDepth40 = 4;
        private const int suitDamageDepth60 = 16;

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
                if (player?.Entity is not EntityPlayer entity)
                {
                    continue;
                }

                if (!entity.Alive)
                {
                    continue;
                }

                ApplyDepthPressure(world, player, entity);
            }
        }

        private static void ApplyDepthPressure(IServerWorldAccessor world, IPlayer player, EntityPlayer entity)
        {
            int waterDepth = GetWaterDepth(world, entity);

            // Not deep enough for pressure
            if (waterDepth < depth20)
            {
                return;
            }

            bool hasFullSuit = ModSystemDivingSuit.TryGetEquippedDivingSuitTier(player, out string tier);
            List<ItemSlot> suitSlots = hasFullSuit ? GetEquippedSuitSlots(player, tier) : [];
            bool hasFunctionalSuit = hasFullSuit && !SuitDamaged(suitSlots);

            if (hasFunctionalSuit && tier == "mk3")
            {
                return;
            }

            int safeDepth = 0;
            if (hasFunctionalSuit)
            {
                safeDepth = GetSuitSafeDepthFromJson(suitSlots);
            }

            // Within suit safe depth
            if (hasFunctionalSuit && waterDepth <= safeDepth)
            {
                return;
            }

            // Determine depth level and corresponding damage
            float playerDamagePerSecond;
            int suitDamagePerSecond;

            if (waterDepth < depth40)
            {
                playerDamagePerSecond = damageDepth20;
                suitDamagePerSecond = suitDamageDepth20;
            }
            else if (waterDepth < depth60)
            {
                playerDamagePerSecond = damageDepth40;
                suitDamagePerSecond = suitDamageDepth40;
            }
            else
            {
                playerDamagePerSecond = damageDepth60;
                suitDamagePerSecond = suitDamageDepth60;
            }

            // If the worn suit is beyond its safe depth, damage it first
            if (hasFunctionalSuit && waterDepth > safeDepth)
            {
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

        private static int GetSuitSafeDepthFromJson(List<ItemSlot> suitSlots)
        {
            foreach (ItemSlot slot in suitSlots)
            {
                if (slot.Itemstack?.Collectible is ItemDivingSuit suit && suit.SafeDepthFromJson >= 0)
                {
                    return suit.SafeDepthFromJson;
                }
            }

            return 0;
        }

        private static int GetWaterDepth(IServerWorldAccessor world, EntityPlayer entity)
        {
            IBlockAccessor blockAccessor = world.BlockAccessor;

            EntityPos entityPosition = entity.ServerPos ?? entity.Pos;
            if (entityPosition == null)
            {
                return 0;
            }

            BlockPos pos = entityPosition.AsBlockPos;
            int headY = pos.Y + 1;
            int maxY = blockAccessor.MapSizeY - 1;
            BlockPos scanPos = new(pos.X, headY, pos.Z);
            int waterSurfaceY = -1;

            // Scan from top of world down to head height
            for (int y = maxY; y >= headY; y--)
            {
                scanPos.Y = y;

                Block block = blockAccessor.GetBlock(scanPos);

                if (block != null && block.IsLiquid())
                {
                    waterSurfaceY = y;
                    break;
                }
            }

            if (waterSurfaceY == -1)
            {
                return 0;
            }

            int depth = waterSurfaceY - headY + 1;
            return depth < 0 ? 0 : depth;
        }

        private static List<ItemSlot> GetEquippedSuitSlots(IPlayer player, string tier)
        {
            List<ItemSlot> slots = [];

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return slots;
            }

            foreach (ItemSlot slot in inventory)
            {
                if (slot?.Itemstack?.Item is not ItemDivingSuit)
                {
                    continue;
                }

                ItemStack stack = slot.Itemstack;
                string bodypart = stack.Item.Variant["bodypart"];
                string suitTier = stack.Item.Variant["tier"];

                if (suitTier != tier)
                {
                    continue;
                }

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
            if (amountPerPiece <= 0)
            {
                return;
            }

            foreach (ItemSlot slot in slots)
            {
                if (slot?.Itemstack == null)
                {
                    continue;
                }

                if (slot.Itemstack.Item is not ItemDivingSuit)
                {
                    continue;
                }

                slot.Itemstack.Collectible.DamageItem(world, entity, slot, amountPerPiece);
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
