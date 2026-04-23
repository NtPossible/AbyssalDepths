using AbyssalDepths.src.CollectibleBehaviour;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Systems
{
    public class ModSystemUnderwaterEquipment : ModSystem
    {
        private const string DisableSwimKey = "abyssalDepthsDisableSwim";
        private const string FullSuitKey = "abyssalDepthsFullDivingSuit";
        private const string LockHeadKey = "abyssalDepthsLockHeadMovement";
        private const string SwimSpeedKey = "flippersSwimSpeed";

        private ICoreServerAPI? sapi;

        public override bool ShouldLoad(EnumAppSide forSide) => true;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.RegisterGameTickListener(OnTickServer1s, 1000, 200);
        }

        public void OnTickServer1s(float dt)
        {
            if (sapi?.World == null)
            {
                return;
            }

            foreach (IPlayer player in sapi.World.AllOnlinePlayers)
            {
                ProcessPlayer(player);
            }
        }

        public static void ProcessPlayer(IPlayer? player)
        {
            if (player?.Entity is not EntityPlayer entity || !entity.Alive)
            {
                return;
            }

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return;
            }

            bool disableSwim = false;
            bool lockHead = false;
            bool hasFullSuit = GetEquippedDivingSuitSet(player, out _);

            float anySuitMaxOxygen = -1f;
            float swimSpeedMultiplier = 1f;

            foreach (ItemSlot slot in inventory)
            {
                if (slot?.Itemstack?.Item == null)
                {
                    continue;
                }

                CollectibleBehaviorDivingEquipment? behavior = slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingEquipment>();
                if (behavior == null)
                {
                    continue;
                }

                if (anySuitMaxOxygen < 0f && behavior.MaxOxygen > 0f)
                {
                    anySuitMaxOxygen = behavior.MaxOxygen;
                }

                if (behavior.Weighted)
                {
                    disableSwim = true;
                }

                if (behavior.LockHead)
                {
                    lockHead = true;
                }

                if (behavior.SwimSpeedMultiplier != 1f)
                {
                    swimSpeedMultiplier = behavior.SwimSpeedMultiplier;
                }
            }

            SetWatchedBool(entity, DisableSwimKey, disableSwim);
            SetWatchedBool(entity, LockHeadKey, lockHead);
            SetWatchedBool(entity, FullSuitKey, hasFullSuit);

            float activeSwimSpeed = entity.Swimming ? swimSpeedMultiplier : 1f;
            entity.WatchedAttributes.SetFloat(SwimSpeedKey, activeSwimSpeed);

            // Oxygen only for full set of same tier
            if (!hasFullSuit || anySuitMaxOxygen <= 0f)
            {
                ResetPlayerOxygen(player);
                return;
            }

            ApplyFullSuitOxygen(player, anySuitMaxOxygen);
        }

        public static void SetWatchedBool(EntityPlayer entity, string key, bool value)
        {
            SyncedTreeAttribute attribute = entity.WatchedAttributes;
            if (attribute == null)
            {
                return;
            }

            if (attribute.GetBool(key) != value)
            {
                attribute.SetBool(key, value);
            }
        }

        public static void ApplyFullSuitOxygen(IPlayer player, float maxOxygen)
        {
            if (player?.Entity is not EntityPlayer entity || !entity.Alive)
            {
                return;
            }

            if (entity.SidedProperties?.Behaviors == null)
            {
                return;
            }

            EntityBehaviorBreathe? breathe = entity.GetBehavior<EntityBehaviorBreathe>();
            if (breathe == null)
            {
                return;
            }

            float targetMaxOxygen = GetSuitMaxOxygen(maxOxygen, entity);

            if (breathe.MaxOxygen != targetMaxOxygen)
            {
                breathe.MaxOxygen = targetMaxOxygen;
            }
        }

        public static float GetSuitMaxOxygen(float maxOxygen, EntityPlayer entity)
        {
            if (maxOxygen > 0f)
            {
                return maxOxygen;
            }

            return GetDefaultPlayerOxygen(entity);
        }

        public static float GetDefaultPlayerOxygen(EntityPlayer entity)
        {
            return entity.World.Config.GetAsInt("lungCapacity", 40000);
        }

        public static bool GetEquippedDivingSuitSet(IPlayer player, out string suitSet)
        {
            suitSet = string.Empty;

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return false;
            }

            // Track which body parts we have and enforce a single set.
            HashSet<string> bodypartsFound = [];
            string? foundSuitSet = null;

            foreach (ItemSlot slot in inventory)
            {
                IsValidDivingSuitSlot(slot, ref foundSuitSet, bodypartsFound);
            }

            if (foundSuitSet != null && bodypartsFound.Count == 3)
            {
                suitSet = foundSuitSet;
                return true;
            }

            return false;
        }

        public static void IsValidDivingSuitSlot(ItemSlot slot, ref string? foundSuitSet, HashSet<string> bodypartsFound)
        {
            if (slot?.Itemstack?.Item == null)
            {
                return;
            }

            CollectibleBehaviorDivingEquipment? behavior = slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingEquipment>();
            if (behavior == null)
            {
                return;
            }

            if (slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack) <= 0)
            {
                return;
            }

            string? bodypart = slot.Itemstack.Item.Variant?["bodypart"];
            if (bodypart == null)
            {
                return;
            }

            if (bodypart != "head" && bodypart != "body" && bodypart != "legs")
            {
                return;
            }

            string suitSet = behavior.SuitSet;
            if (string.IsNullOrEmpty(suitSet))
            {
                return;
            }

            if (foundSuitSet == null)
            {
                foundSuitSet = suitSet;
            }
            else if (foundSuitSet != suitSet)
            {
                foundSuitSet = null;
                return;
            }

            bodypartsFound.Add(bodypart);
        }

        public static void ResetPlayerOxygen(IPlayer player)
        {
            if (player.Entity is not EntityPlayer entity)
            {
                return;
            }

            if (entity.SidedProperties?.Behaviors == null)
            {
                return;
            }

            EntityBehaviorBreathe? breathe = entity.GetBehavior<EntityBehaviorBreathe>();
            if (breathe == null)
            {
                return;
            }

            float baseMax = GetDefaultPlayerOxygen(entity);

            if (breathe.MaxOxygen != baseMax)
            {
                breathe.MaxOxygen = baseMax;
            }

            if (breathe.Oxygen > baseMax)
            {
                breathe.Oxygen = baseMax;
            }
        }

        public static bool GetFunctionalSuit(IPlayer player, out List<ItemSlot> suitSlots, out int safeDepth)
        {
            suitSlots = new List<ItemSlot>();
            safeDepth = 0;

            if (!ModSystemUnderwaterEquipment.GetEquippedDivingSuitSet(player, out string suitSet))
            {
                return false;
            }

            suitSlots = GetEquippedSuitSlots(player, suitSet);

            if (SuitDamaged(suitSlots))
            {
                return false;
            }

            safeDepth = GetSuitSafeDepth(suitSlots);
            return safeDepth > 0;
        }

        public static CollectibleBehaviorDivingEquipment? GetDivingEquipmentBehavior(ItemSlot? slot)
        {
            if (slot == null || slot.Itemstack == null)
            {
                return null;
            }

            return slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingEquipment>();
        }

        public static void TryPlaySuitCreak(IServerWorldAccessor world, EntityPlayer entity, int waterDepth, int safeDepth, List<ItemSlot> suitSlots)
        {
            int depthOver = GameMath.Max(0, waterDepth - safeDepth);

            float chance = GameMath.Clamp(0.01f + 0.005f * depthOver, 0f, 0.08f);

            if (world.Rand.NextDouble() < chance)
            {
                AssetLocation? sound = GetSuitCreakSound(suitSlots);
                if (sound != null)
                {
                    world.PlaySoundAt(sound, entity, null, true, 14f);
                }
            }
        }

        public static AssetLocation? GetSuitCreakSound(List<ItemSlot> slots)
        {
            foreach (ItemSlot slot in slots)
            {
                CollectibleBehaviorDivingEquipment? behavior = GetDivingEquipmentBehavior(slot);
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

        public static AssetLocation? GetSuitBreakSound(List<ItemSlot> slots)
        {
            foreach (ItemSlot slot in slots)
            {
                CollectibleBehaviorDivingEquipment? behavior = GetDivingEquipmentBehavior(slot);
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

        public static int GetSuitSafeDepth(List<ItemSlot> suitSlots)
        {
            int maxSafeDepth = 0;

            foreach (ItemSlot slot in suitSlots)
            {
                CollectibleBehaviorDivingEquipment? behavior = GetDivingEquipmentBehavior(slot);
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

        public static List<ItemSlot> GetEquippedSuitSlots(IPlayer player, string suitSet)
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

                CollectibleBehaviorDivingEquipment? behavior = GetDivingEquipmentBehavior(slot);
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

        public static bool SuitDamaged(List<ItemSlot> slots)
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

        public static void DamageSuit(IServerWorldAccessor world, EntityPlayer entity, List<ItemSlot> slots, int amountPerPiece)
        {
            bool anyJustBroke = false;

            foreach (ItemSlot slot in slots)
            {
                if (slot?.Itemstack == null)
                {
                    continue;
                }

                CollectibleBehaviorDivingEquipment? behavior = GetDivingEquipmentBehavior(slot);
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
                AssetLocation? sound = GetSuitBreakSound(slots);
                if (sound != null)
                {
                    world.PlaySoundAt(sound, entity, null, true, 32f);
                }
            }
        }
    }
}