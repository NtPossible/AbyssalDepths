using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityHeadController), "OnFrame")]
    public static class Patch_EntityHeadController_OnFrame
    {
        static readonly AccessTools.FieldRef<EntityHeadController, EntityAgent> entityRef = AccessTools.FieldRefAccess<EntityHeadController, EntityAgent>("entity");

        // Head clips through mk3 helmet without this patch
        static void Postfix(EntityHeadController __instance)
        {
            EntityAgent? entityAgent = entityRef(__instance);
            if (entityAgent is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsLockHeadMovement"))
            {
                // Not in a diving suit, keep vanilla behaviour
                return;
            }

            // If the suit is on freeze head/neck/torso movement
            __instance.HeadPose.degOffY = 0f;
            __instance.HeadPose.degOffZ = 0f;

            __instance.NeckPose.degOffY = 0f;
            __instance.NeckPose.degOffZ = 0f;

            __instance.UpperTorsoPose.degOffY = 0f;
            __instance.UpperTorsoPose.degOffZ = 0f;

            __instance.LowerTorsoPose.degOffZ = 0f;

            __instance.UpperFootLPose.degOffZ = 0f;
            __instance.UpperFootRPose.degOffZ = 0f;
        }
    }
}