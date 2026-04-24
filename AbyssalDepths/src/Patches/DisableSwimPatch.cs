using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), nameof(EntityBehaviorControlledPhysics.ApplyTests))]
    public static class DisableSwimPatch
    {
        public static void Postfix(Entity ___entity)
        {
            if (___entity is not EntityPlayer entityPlayer)
            {
                return;
            }

            SyncedTreeAttribute attributes = entityPlayer.WatchedAttributes;
            if (attributes?.GetBool("abyssalDepthsDisableSwim") == true)
            {
                entityPlayer.Swimming = false;
            }
        }
    }
}