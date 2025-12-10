using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityHeadController), "OnFrame")]
    public class Patch_EntityHeadController_OnFrame
    {
        static void Postfix(EntityHeadController __instance)
        {
            // Use Harmony Traverse to get the protected "entity" field
            EntityAgent? entityAgent = Traverse.Create(__instance).Field("entity").GetValue<EntityAgent>();
            if (entityAgent is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                // Not in a diving suit, keep vanilla behaviour
                return;
            }

            // Suit on: freeze head/neck/torso movement
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
