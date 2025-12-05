using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityPlayer), "LightHsv", MethodType.Getter)]
    public class Patch_Entity_LightHsv
    {
        static bool Prefix(Entity __instance, ref byte[] __result)
        {
            if (__instance is not EntityPlayer entityPlayer)
            {
                return true;
            }
            
            // Only override if helmet light flag is enabled
            bool helmetLightEnabled = entityPlayer.WatchedAttributes.GetBool("abyssalDepthsHelmetLight");
            if (!helmetLightEnabled)
            {
                return true;
            }
            
            // Emit light
            __result = new byte[] { 25, 5, 5 };
            return false;
        }
    }
}
