using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace AbyssalDepths.src.CollectibleBehaviour
{
    public class CollectibleBehaviorDivingSuit : CollectibleBehavior
    {
        public string SuitSet { get; private set; } = string.Empty;
        public float MaxOxygen { get; private set; } = -1f;
        public int SafeDepth { get; private set; } = -1;
        public bool Weighted { get; private set; } = false;
        public bool LockHead { get; private set; } = false;
        public AssetLocation? CreakSound { get; private set; }
        public AssetLocation? BreakSound { get; private set; }

        public CollectibleBehaviorDivingSuit(CollectibleObject collectibleObject) : base(collectibleObject) { }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            JsonObject? abyssalDepths = collObj.Attributes?["abyssalDepths"];
            if (abyssalDepths != null && abyssalDepths.Exists)
            {
                SuitSet = abyssalDepths["suitId"].AsString("");
                MaxOxygen = abyssalDepths["maxOxygen"].AsFloat(-1f);
                SafeDepth = abyssalDepths["safeDepth"].AsInt(-1);
                Weighted = abyssalDepths["weighted"].AsBool(false);
                LockHead = abyssalDepths["lockHead"].AsBool(false);

                string creakCode = abyssalDepths["creakSound"].AsString(null);
                if (!string.IsNullOrEmpty(creakCode))
                {
                    CreakSound = new AssetLocation(creakCode);
                }

                string breakCode = abyssalDepths["breakSound"].AsString(null);
                if (!string.IsNullOrEmpty(breakCode))
                {
                    BreakSound = new AssetLocation(breakCode);
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            double totalSeconds = MaxOxygen / 1000.0;
            int minutes = (int)(totalSeconds / 60);
            int seconds = (int)(totalSeconds % 60);

            string timeText;
            if (minutes > 0)
            {
                timeText = Lang.Get("{0}m {1}s", minutes, seconds);
            }
            else
            {
                timeText = Lang.Get("{0}s", seconds);
            }

            dsc.AppendLine(Lang.Get("abyssaldepths:item-divingsuit-maxoxygen", timeText));
            dsc.AppendLine(Lang.Get("abyssaldepths:item-divingsuit-safedepth", SafeDepth));
        }
    }
}