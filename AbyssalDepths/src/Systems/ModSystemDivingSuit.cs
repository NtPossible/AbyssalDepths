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
        private const string disableSwimKey = "abyssalDepthsDisableSwim";

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
            JsonObject? suitAttributes = null;

            foreach (ItemSlot slot in inventory)
            {
                JsonObject? attributes = GetDivingSuitAttributes(slot);
                if (attributes != null && attributes.Exists)
                {
                    suitAttributes = attributes;
                    break;
                }
            }

            if (suitAttributes != null)
            {
                HandleSuit(player, suitAttributes);
            }
            else
            {
                ResetPlayerOxygen(player);
            }
        }

        private static JsonObject? GetDivingSuitAttributes(ItemSlot? slot)
        {
            if (slot == null || slot.Itemstack == null)
            {
                return null;
            }

            return slot.Itemstack.Item.Attributes?["abyssalDepths"];
        }

        private static void HandleSuit(IPlayer player, JsonObject abyssalDepths)
        {
            EntityPlayer entity = player.Entity;
            if (entity == null)
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

            if (entity.WatchedAttributes == null)
            {
                return;
            }

            // Check if we have a full suit, and which tier it is
            if (!TryGetEquippedDivingSuitTier(player, out _))
            {
                ResetPlayerOxygen(player);
                return;
            }

            if (!entity.WatchedAttributes.GetBool(disableSwimKey))
            {
                entity.WatchedAttributes.SetBool(disableSwimKey, true);
            }

            float targetMaxOxygen = GetSuitMaxOxygen(abyssalDepths, entity);
            if (breathe.MaxOxygen != targetMaxOxygen)
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
                if (!IsValidDivingSuitSlot(slot, ref foundTier, bodypartsFound))
                {
                    continue;
                }
            }

            if (foundTier != null && bodypartsFound.Count == 3)
            {
                tier = foundTier;
                return true;
            }

            return false;
        }

        private static bool IsValidDivingSuitSlot(ItemSlot slot, ref string? foundTier, HashSet<string> bodypartsFound)
        {
            JsonObject? abyssalDepths = GetDivingSuitAttributes(slot);
            if (abyssalDepths == null || !abyssalDepths.Exists)
            {
                return false;
            }

            string? bodypart = slot!.Itemstack!.Item.Variant?["bodypart"];
            string? suitTier = slot.Itemstack.Item.Variant?["tier"];

            if (bodypart == null || suitTier == null)
            {
                return false;
            }

            if (bodypart != "head" && bodypart != "body" && bodypart != "legs")
            {
                return false;
            }

            if (foundTier == null)
            {
                foundTier = suitTier;
            }
            else if (foundTier != suitTier)
            {
                foundTier = null;
                return false;
            }

            bodypartsFound.Add(bodypart);
            return true;
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

            if (entity.WatchedAttributes == null)
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

            if (entity.WatchedAttributes.GetBool(disableSwimKey))
            {
                entity.WatchedAttributes.SetBool(disableSwimKey, false);
            }
        }
    }
}