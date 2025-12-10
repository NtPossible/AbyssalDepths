using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityPlayer), "GetWalkSpeedMultiplier")]
    public static class Patch_EntityPlayer_WalkSpeed
    {
        static void Postfix(EntityPlayer __instance, ref double __result)
        {
            SyncedTreeAttribute attribute = __instance.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                return;
            }

            if (__instance.FeetInLiquid)
            {
                __result = __instance.walkSpeed * 1.1;
            }
        }
    }
}
