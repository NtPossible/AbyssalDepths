using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Items.Wearable
{
    public class ItemFlippers : ItemWearable
    {
        public float SwimSpeedFromJson { get; private set; } = 1f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            JsonObject? abyssalDepths = Attributes?["abyssalDepths"];
            if (abyssalDepths != null && abyssalDepths.Exists)
            {
                SwimSpeedFromJson = abyssalDepths["swimspeed"].AsFloat(1f);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.AppendLine(Lang.Get("abyssaldepths:item-flippers-swimspeed", SwimSpeedFromJson));
        }
    }
}