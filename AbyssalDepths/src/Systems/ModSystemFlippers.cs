using AbyssalDepths.src.Items.Wearable;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AbyssalDepths.src.Systems
{
    public class ModSystemFlippers : ModSystem
    {
        private ICoreServerAPI? sapi;

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

        private static void ProcessPlayer(IPlayer player)
        {
            if (player?.Entity is not EntityPlayer entity || !entity.Alive)
            {
                return;
            }

            if (!entity.Swimming)
            {
                return;
            }

            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inventory == null)
            {
                return;
            }

            float swimSpeed = 1f;

            foreach (ItemSlot slot in inventory)
            {
                if (slot.Itemstack?.Collectible is ItemFlippers flippers)
                {
                    swimSpeed = flippers.SwimSpeedFromJson;
                    break;
                }
            }

            entity.WatchedAttributes.SetFloat("flippersSwimSpeed", swimSpeed);
        }
    }
}