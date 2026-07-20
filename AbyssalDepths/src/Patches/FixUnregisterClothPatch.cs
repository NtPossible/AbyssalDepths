using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Patches
{
    // seems the rope ressurection issue was because of the cloth systems weren't removed from the region data
    // this will be removed once that issue is fixed in vanilla, but after testing several times this SHOULD fix it
    [HarmonyPatch(typeof(ClothManager), nameof(ClothManager.UnregisterCloth))]
    public static class FixUnregisterClothPatch
    {
        public static void Prefix(ClothManager __instance, int clothId, ICoreServerAPI ___sapi)
        {
            if (___sapi == null)
            {
                return;
            }
            
            ClothSystem clothSystem = __instance.GetClothSystem(clothId);
            if (clothSystem == null)
            {
                return;
            }

            HashSet<long> regionsToCheck = new();
            clothSystem.WalkPoints(point =>
            {
                BlockPos blockPos = point.Pos.AsBlockPos;
                regionsToCheck.Add(___sapi.WorldManager.MapRegionIndex2DByBlockPos(blockPos.X, blockPos.Z));
            });

            foreach (long regionIndex in regionsToCheck)
            {
                IMapRegion region = ___sapi.WorldManager.GetMapRegion(regionIndex);
                if (region == null)
                {
                    continue;
                }

                byte[] data = region.GetModdata("clothSystems");
                if (data == null)
                {
                    continue;
                }

                List<ClothSystem> clothSystemList = SerializerUtil.Deserialize<List<ClothSystem>>(data);
                if (clothSystemList.RemoveAll(cloth => cloth.ClothId == clothId) > 0)
                {
                    region.SetModdata("clothSystems", SerializerUtil.Serialize(clothSystemList));
                }
            }
        }
    }
}
