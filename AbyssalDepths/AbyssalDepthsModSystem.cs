using AbyssalDepths.src.Items.Wearable;
using HarmonyLib;
using Vintagestory.API.Common;

namespace AbyssalDepths
{
    public class AbyssalDepthsModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemDivingSuit", typeof(ItemDivingSuit));

            new Harmony("abyssaldepths.divingsuit").PatchAll();
        }
    }
}
