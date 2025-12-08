using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityAgent), "OnGameTick")]
    public class Patch_EntityAgent_OnGameTick
    {
        static void Prefix(EntityAgent __instance)
        {
            if (__instance is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                return;
            }

            // diving suits should never swim
            if (entityPlayer.Swimming)
            {
                entityPlayer.Swimming = false;
            }
        }

        static void Postfix(EntityAgent __instance, float dt)
        {
            if (__instance is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                return;
            }

            if (!entityPlayer.FeetInLiquid)
            {
                return;
            }

            if (entityPlayer.OnGround)
            {
                return;
            }

            // sink the player faster when wearing a diving suit
            EntityPos pos = entityPlayer.SidedPos;

            double velocityY = pos.Motion.Y;

            const double maxDownSpeed = -0.16;
            const double extraAccelPerSecond = -0.15;

            if (velocityY > maxDownSpeed)
            {
                velocityY += extraAccelPerSecond * dt;
                if (velocityY < maxDownSpeed)
                {
                    velocityY = maxDownSpeed;
                }
            }

            pos.Motion.Y = velocityY;
        }
    }
}
