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
            bool hasMk3Helmet = false;
            bool hasFullSuit = TryGetEquippedDivingSuitTier(player, out _);

            JsonObject? anySuitAttributes = null;

            foreach (ItemSlot slot in inventory)
            {
                JsonObject? attributes = GetDivingSuitAttributes(slot);
                if (attributes == null || !attributes.Exists)
                {
                    continue;
                }
                anySuitAttributes ??= attributes;

                string? bodypart = slot.Itemstack?.Item?.Variant?["bodypart"];
                string? tier = slot.Itemstack?.Item?.Variant?["tier"];

                // mk1 and mk2 only has body/legs disable swim, all parts of mk3 disable swim
                if (tier == "mk3")
                {
                    if (bodypart == "head" || bodypart == "body" || bodypart == "legs")
                    {
                        disableSwim = true;
                    }
                }
                else
                {
                    if (bodypart == "body" || bodypart == "legs")
                    {
                        disableSwim = true;
                    }
                }

                // Head lock only for mk3 helmet
                if (bodypart == "head" && tier == "mk3")
                {
                    hasMk3Helmet = true;
                }
            }

            SetWatchedBool(entity, DisableSwimKey, disableSwim);
            SetWatchedBool(entity, LockHeadKey, hasMk3Helmet);
            SetWatchedBool(entity, FullSuitKey, hasFullSuit);

            // Oxygen only for full set of same tier
            if (!hasFullSuit || anySuitAttributes == null)
            {
                ResetPlayerOxygen(player);
                return;
            }

            ApplyFullSuitOxygen(player, anySuitAttributes);
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

        private static JsonObject? GetDivingSuitAttributes(ItemSlot? slot)
        {
            if (slot?.Itemstack?.Item == null)
            {
                return null;
            }
            return slot.Itemstack.Item.Attributes?["abyssalDepths"];
        }

        private static void ApplyFullSuitOxygen(IPlayer player, JsonObject abyssalDepths)
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
            float targetMaxOxygen = GetSuitMaxOxygen(abyssalDepths, entity);
            if (!NearlyEqual(breathe.MaxOxygen, targetMaxOxygen))
            {
                breathe.MaxOxygen = targetMaxOxygen;
            }
        }

        private static float GetSuitMaxOxygen(JsonObject abyssalDepths, EntityPlayer entity)
        {
            float maxOxygen = abyssalDepths["maxOxygen"].AsFloat(-1f);
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

        public static bool TryGetEquippedDivingSuitTier(IPlayer player, out string tier)
        {
            tier = string.Empty;

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return false;
            }

            // Track which body parts we have and enforce a single tier.
            HashSet<string> bodypartsFound = [];
            string? foundTier = null;

            foreach (ItemSlot slot in inventory)
            {
                IsValidDivingSuitSlot(slot, ref foundTier, bodypartsFound);
            }

            if (foundTier != null && bodypartsFound.Count == 3)
            {
                tier = foundTier;
                return true;
            }

            return false;
        }

        private static void IsValidDivingSuitSlot(ItemSlot slot, ref string? foundTier, HashSet<string> bodypartsFound)
        {
            JsonObject? abyssalDepths = GetDivingSuitAttributes(slot);
            if (abyssalDepths == null || !abyssalDepths.Exists)
            {
                return;
            }

            if (slot.Itemstack?.Item?.GetRemainingDurability(slot.Itemstack) <= 0)
            {
                return;
            }

            string? bodypart = slot.Itemstack?.Item?.Variant?["bodypart"];
            string? suitTier = slot.Itemstack?.Item?.Variant?["tier"];

            if (bodypart == null || suitTier == null)
            {
                return;
            }
            if (bodypart != "head" && bodypart != "body" && bodypart != "legs")
            {
                return;
            }

            if (foundTier == null)
            {
                foundTier = suitTier;
            }
            else if (foundTier != suitTier)
            {
                foundTier = null;
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
