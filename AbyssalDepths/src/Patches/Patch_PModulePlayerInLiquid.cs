using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(PModulePlayerInLiquid), "HandleSwimming")]
    public static class Patch_PModulePlayerInLiquid
    {
        static bool Prefix(Entity entity)
        {
            if (entity is not EntityPlayer entityPlayer)
            {
                return true;
            }

            SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
            if (attribute == null || !attribute.GetBool("abyssalDepthsDisableSwim"))
            {
                // if not in a diving suit swim normally
                return true;
            }

            // no swimming if in a diving suit
            return false;
        }
    }
}
