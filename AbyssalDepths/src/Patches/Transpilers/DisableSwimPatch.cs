using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Patches.Transpilers
{
    [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), nameof(EntityBehaviorControlledPhysics.ApplyTests))]
    public static class DisableSwimPatch
    {
        static readonly FieldInfo SwimmingField = AccessTools.Field(typeof(Entity), nameof(Entity.Swimming));
        static readonly MethodInfo ShouldAllowSwimmingMethod = AccessTools.Method(typeof(DisableSwimPatch), nameof(ShouldAllowSwimming));

        public static bool ShouldAllowSwimming(Entity entity, bool shouldSwim)
        {
            if (!shouldSwim)
            {
                return false;
            }

            if (entity is EntityPlayer entityPlayer)
            {
                SyncedTreeAttribute attribute = entityPlayer.WatchedAttributes;
                if (attribute?.GetBool("abyssalDepthsDisableSwim") == true)
                {
                    return false;
                }
            }

            return true;
        }

        // Blocks Entity.Swimming from being set to true during player-controlled physics when a diving suit is worn, enabling normal walking underwater
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var tmpBool = ilGenerator.DeclareLocal(typeof(bool));

            foreach (CodeInstruction? codeInstructions in instructions)
            {
                if (codeInstructions.opcode == OpCodes.Stfld && codeInstructions.operand is FieldInfo fieldInfo && fieldInfo == SwimmingField)
                {
                    yield return new CodeInstruction(OpCodes.Stloc, tmpBool.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldloc, tmpBool.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, ShouldAllowSwimmingMethod);
                    yield return codeInstructions;
                    continue;
                }

                yield return codeInstructions;
            }
        }
    }
}