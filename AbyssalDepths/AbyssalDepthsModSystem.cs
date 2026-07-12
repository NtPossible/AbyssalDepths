using AbyssalDepths.src.CollectibleBehaviour;
using AbyssalDepths.src.Config;
using AbyssalDepths.src.Entities.Behaviors;
using AbyssalDepths.src.Items;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AbyssalDepths
{
    public class AbyssalDepthsModSystem : ModSystem
    {
        public static AbyssalDepthsConfig Config { get; private set; } = new();

        private Harmony? harmony;
        private const string HarmonyId = "abyssaldepths.divingsuit";

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            if (!Harmony.HasAnyPatches(HarmonyId))
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll();
            }
        }

        public override void Start(ICoreAPI api)
        {
            TryLoadConfig(api);

            api.RegisterCollectibleBehaviorClass($"{Mod.Info.ModID}:DivingEquipment", typeof(CollectibleBehaviorDivingEquipment));
            api.RegisterEntityBehaviorClass($"{Mod.Info.ModID}:Pressure", typeof(EntityBehaviorPressure));
            api.RegisterItemClass($"{Mod.Info.ModID}:ItemDavit", typeof(ItemDavit));

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

        public override void Dispose()
        {
            harmony?.UnpatchAll(HarmonyId);
            base.Dispose();
        }
    }
}