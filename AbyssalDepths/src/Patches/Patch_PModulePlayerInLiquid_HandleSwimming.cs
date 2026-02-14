using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(PModulePlayerInLiquid), "HandleSwimming")]
    public static class Patch_PModulePlayerInLiquid_HandleSwimming
    {
        private static bool _handlingSwimming = true;

        static bool Prefix(PModulePlayerInLiquid __instance, float dt, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!_handlingSwimming)
            {
                return true;
            }

            if (entity is not EntityPlayer player || !player.Alive || !player.Swimming)
            {
                return true;
            }

            float swimFactor = 1f;
            if (player.WatchedAttributes.HasAttribute("flippersSwimSpeed"))
            {
                swimFactor = player.WatchedAttributes.GetFloat("flippersSwimSpeed");
            }

            Vec3d? prevMotion = pos.Motion.Clone();
            _handlingSwimming = false;
            __instance.HandleSwimming(dt, entity, pos, controls);
            _handlingSwimming = true;
            pos.Motion.Set(prevMotion + (pos.Motion - prevMotion) * swimFactor);

            return false;
        }
    }
}