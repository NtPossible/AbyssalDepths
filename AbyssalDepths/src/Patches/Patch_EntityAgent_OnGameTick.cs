using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityAgent), "OnGameTick")]
    public static class Patch_EntityAgent_OnGameTick
    {
        // Sink the player faster when wearing a diving suit and in liquid
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

            EntityPos pos = entityPlayer.SidedPos;

            double velocityY = pos.Motion.Y;

            const double maxDownSpeed = -0.20;
            const double extraAccelPerSecond = -0.18;

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