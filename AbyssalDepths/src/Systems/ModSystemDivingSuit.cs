using AbyssalDepths.src.CollectibleBehaviour;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Systems
{
    public class ModSystemDivingSuit : ModSystem
    {
        private const string DisableSwimKey = "abyssalDepthsDisableSwim";
        private const string FullSuitKey = "abyssalDepthsFullDivingSuit";
        private const string LockHeadKey = "abyssalDepthsLockHeadMovement";

        private const float OxygenTolerance = 0.001f;

        private ICoreServerAPI? sapi;

        public override bool ShouldLoad(EnumAppSide forSide) => true;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.RegisterGameTickListener(OnTickServer1s, 1000, 200);
        }

        private void OnTickServer1s(float dt)
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

        private static void ProcessPlayer(IPlayer? player)
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
            bool hasFullSuit = TryGetEquippedDivingSuitSet(player, out _);

            float anySuitMaxOxygen = -1f;

            foreach (ItemSlot slot in inventory)
            {
                if (slot?.Itemstack?.Item == null)
                {
                    continue;
                }

                CollectibleBehaviorDivingSuit? behavior = slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingSuit>();
                if (behavior == null)
                {
                    continue;
                }

                if (anySuitMaxOxygen < 0f)
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
            }

            SetWatchedBool(entity, DisableSwimKey, disableSwim);
            SetWatchedBool(entity, LockHeadKey, lockHead);
            SetWatchedBool(entity, FullSuitKey, hasFullSuit);

            // Oxygen only for full set of same tier
            if (!hasFullSuit || anySuitMaxOxygen <= 0f)
            {
                ResetPlayerOxygen(player);
                return;
            }

            ApplyFullSuitOxygen(player, anySuitMaxOxygen);
        }

        private static void SetWatchedBool(EntityPlayer entity, string key, bool value)
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

        private static void ApplyFullSuitOxygen(IPlayer player, float maxOxygen)
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

            if (!NearlyEqual(breathe.MaxOxygen, targetMaxOxygen))
            {
                breathe.MaxOxygen = targetMaxOxygen;
            }
        }

        private static float GetSuitMaxOxygen(float maxOxygen, EntityPlayer entity)
        {
            if (maxOxygen > 0f)
            {
                return maxOxygen;
            }

            return GetDefaultPlayerOxygen(entity);
        }

        private static float GetDefaultPlayerOxygen(EntityPlayer entity)
        {
            return entity.World.Config.GetAsInt("lungCapacity", 40000);
        }

        public static bool TryGetEquippedDivingSuitSet(IPlayer player, out string suitSet)
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

        private static void IsValidDivingSuitSlot(ItemSlot slot, ref string? foundSuitSet, HashSet<string> bodypartsFound)
        {
            if (slot?.Itemstack?.Item == null)
            {
                return;
            }

            CollectibleBehaviorDivingSuit? behavior = slot.Itemstack.Item.GetBehavior<CollectibleBehaviorDivingSuit>();
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

        private static void ResetPlayerOxygen(IPlayer player)
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

            if (!NearlyEqual(breathe.MaxOxygen, baseMax))
            {
                breathe.MaxOxygen = baseMax;
            }

            if (breathe.Oxygen > baseMax)
            {
                breathe.Oxygen = baseMax;
            }
        }

        // done to get rid of some floating point inequality complaint
        private static bool NearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) <= OxygenTolerance;
        }
    }
}