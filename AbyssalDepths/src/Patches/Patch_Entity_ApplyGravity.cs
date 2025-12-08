using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(Entity), "get_ApplyGravity")]
    public class Patch_Entity_ApplyGravity
    {
        static void Postfix(Entity __instance, ref bool __result)
        {
            if (__instance is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute != null && attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                // diving suits are always affected by gravity
                __result = true;
            }
        }
    }
}
