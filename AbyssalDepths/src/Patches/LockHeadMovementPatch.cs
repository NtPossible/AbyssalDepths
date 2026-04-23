using HarmonyLib;
using Vintagestory.API.Common;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityHeadController), nameof(EntityHeadController.OnFrame))]
    public static class LockHeadMovementPatch
    {
        static readonly AccessTools.FieldRef<EntityHeadController, EntityAgent> entityRef = AccessTools.FieldRefAccess<EntityHeadController, EntityAgent>("entity");

        // Patch to force the head to stay in place
        static bool Prefix(EntityHeadController __instance)
        {
            EntityAgent? entityAgent = entityRef(__instance);
            if (entityAgent is EntityPlayer entityPlayer && entityPlayer.WatchedAttributes.GetBool("abyssalDepthsLockHeadMovement"))
            {
                // If the head lock is true then skip the original method to prevent head movement
                return false;
            }

            // Keep vanilla behaviour
            return true;
        }
    }
}