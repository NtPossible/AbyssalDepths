using AbyssalDepths.src.Items.Wearable;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Config;

namespace AbyssalDepths.src.Systems
{
    public class ModSystemDivingSuit : ModSystemWearableTick<ItemDivingSuit>
    {
        private const float maxOxygenMk1 = 300000f; // 5 minutes
        private const float maxOxygenMk2 = 900000f; // 15 minutes
        private const float maxOxygenMk3 = 1800000f; // 30 minutes
        private const string disableSwimKey = "abyssalDepthsDisableSwim";

        public override bool ShouldLoad(EnumAppSide forSide) => true;

        protected override void HandleItem(IPlayer player, ItemDivingSuit item, ItemSlot slot, double hoursPassed, float dt)
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
            if (!TryGetEquippedDivingSuitTier(player, out string tier))
            {
                ResetPlayerOxygen(player);
                return;
            }

            if (!entity.WatchedAttributes.GetBool(disableSwimKey))
            {
                entity.WatchedAttributes.SetBool(disableSwimKey, true);
            }

            float targetMaxOxygen = GetMaxOxygenForTier(tier);
            if (breathe.MaxOxygen != targetMaxOxygen)
            {
                breathe.MaxOxygen = targetMaxOxygen;
            }
        }

        protected override void HandleMissing(IPlayer player)
        {
            // No diving suit pieces found at all
            ResetPlayerOxygen(player);
        }

        private static float GetMaxOxygenForTier(string tier)
        {
            return tier switch
            {
                "mk1" => maxOxygenMk1,
                "mk2" => maxOxygenMk2,
                "mk3" => maxOxygenMk3,
                _ => maxOxygenMk1
            };
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
                if (slot?.Itemstack?.Collectible is not ItemDivingSuit collectible)
                {
                    continue;
                }

                string? bodypart = collectible.Variant?["bodypart"];
                string? suitTier = collectible.Variant?["tier"];

                if (bodypart == null || suitTier == null)
                {
                    continue;
                }

                if (bodypart != "head" && bodypart != "body" && bodypart != "legs")
                {
                    continue;
                }

                if (foundTier == null)
                {
                    foundTier = suitTier;
                }
                else if (foundTier != suitTier)
                {
                    return false;
                }

                bodypartsFound.Add(bodypart);
            }

            if (foundTier != null && bodypartsFound.Count == 3)
            {
                tier = foundTier;
                return true;
            }

            return false;
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
