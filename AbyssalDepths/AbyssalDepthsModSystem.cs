using AbyssalDepths.src.Config;
using AbyssalDepths.src.Items.Wearable;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AbyssalDepths
{
    public class AbyssalDepthsModSystem : ModSystem
    {
        public static AbyssalDepthsConfig Config { get; private set; } = new();

        public override void Start(ICoreAPI api)
        {
            TryLoadConfig(api);

            api.RegisterItemClass($"{Mod.Info.ModID}:ItemDivingSuit", typeof(ItemDivingSuit));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemFlippers", typeof(ItemFlippers));

            new Harmony("abyssaldepths.divingsuit").PatchAll();
        }

        public static void TryLoadConfig(ICoreAPI api)
        {
            try
            {
                Config = api.LoadModConfig<AbyssalDepthsConfig>("abyssaldepths.json") ?? new AbyssalDepthsConfig();
                api.StoreModConfig(Config, "abyssaldepths.json");
            }
            catch (Exception exception)
            {
                api.Logger.Error("AbyssalDepths: Failed to load config, using defaults.", exception);
                Config = new AbyssalDepthsConfig();
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server || Config.EnableSchematicCrafting)
            {
                return;
            }

            List<GridRecipe> recipes = api.World.GridRecipes;
            recipes.RemoveAll(recipe => recipe?.Output?.Code?.Path?.StartsWith("ad-schematic-divinggear") == true);
        }
    }
}
