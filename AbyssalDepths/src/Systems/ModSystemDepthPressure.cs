using AbyssalDepths.src.Entities.Behaviors;
using System.Collections.Generic;
using Vintagestory.API.Common;
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

            foreach (IPlayer player in sapi.World.AllOnlinePlayers)
            {
                ProcessPlayer(sapi.World, player);
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

            bool hasFunctionalSuit = ModSystemDivingEquipment.GetFunctionalSuit(player, out List<ItemSlot> suitSlots, out int safeDepth);
            int baseSafeDepth = AbyssalDepthsModSystem.Config.BaseSafeDepth;
            int effectiveSafeDepth = hasFunctionalSuit ? safeDepth : baseSafeDepth;
            int waterDepth = EntityBehaviorPressure.GetWaterDepth(world, entity, effectiveSafeDepth);
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

            if (hasFunctionalSuit)
            {
                // If the worn suit is beyond its safe depth, damage it first
                if (waterDepth > safeDepth)
                {
                    ModSystemDivingEquipment.TryPlaySuitCreak(world, entity, waterDepth, safeDepth, suitSlots);
                    ModSystemDivingEquipment.DamageSuit(world, entity, suitSlots, suitDamagePerSecond);
                }

                // If the full suit still has durability after the damage tick, the suit is still protecting
                if (!ModSystemDivingEquipment.SuitDamaged(suitSlots))
                {
                    return;
                }
            }
        }

        private static float GetDepthSeverity(int depthOver)
        {
            if (depthOver <= 0)
            {
                return 0f;
            }

            return 0.5f + depthOver * 0.5f;
        }

        private static int GetSuitDamagePerSecond(int depthOver)
        {
            return (int)GetDepthSeverity(depthOver);
        }
    }
}