using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(PModulePlayerInLiquid), nameof(PModulePlayerInLiquid.HandleSwimming))]
    public static class IncreaseSwimSpeedPatch
    {
        private static bool handlingSwimming = true;

        static bool Prefix(PModulePlayerInLiquid __instance, float dt, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!handlingSwimming)
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
            handlingSwimming = false;
            __instance.HandleSwimming(dt, entity, pos, controls);
            handlingSwimming = true;
            pos.Motion.Set(prevMotion + (pos.Motion - prevMotion) * swimFactor);

            return false;
        }
    }
}