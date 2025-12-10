using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Items.Wearable
{
    public class ItemDivingSuit : ItemWearable
    {
        public float MaxOxygenFromJson { get; private set; } = -1f;
        public int SafeDepthFromJson { get; private set; } = -1;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            JsonObject? abyssalDepths = Attributes?["abyssalDepths"];
            if (abyssalDepths != null && abyssalDepths.Exists)
            {
                MaxOxygenFromJson = abyssalDepths["maxOxygen"].AsFloat(-1f);
                SafeDepthFromJson = abyssalDepths["safeDepth"].AsInt(-1);
            }
        }
    }
}
