using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Items.Wearable
{
    public class ItemDivingSuit : ItemWearable
    {
        public float MaxOxygenFromJson { get; private set; } = -1f;
        public int SafeDepthFromJson { get; private set; } = -1;
        public AssetLocation? CreakSoundFromJson { get; private set; }
        public AssetLocation? BreakSoundFromJson { get; private set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            JsonObject? abyssalDepths = Attributes?["abyssalDepths"];
            if (abyssalDepths != null && abyssalDepths.Exists)
            {
                MaxOxygenFromJson = abyssalDepths["maxOxygen"].AsFloat(-1f);
                SafeDepthFromJson = abyssalDepths["safeDepth"].AsInt(-1);

                string creakCode = abyssalDepths["creakSound"].AsString(null);
                if (!string.IsNullOrEmpty(creakCode))
                {
                    CreakSoundFromJson = new AssetLocation(creakCode);
                }

                string breakCode = abyssalDepths["breakSound"].AsString(null);
                if (!string.IsNullOrEmpty(breakCode))
                {
                    BreakSoundFromJson = new AssetLocation(breakCode);
                }
            }
        }
    }
}
