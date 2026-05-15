using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Systems.DivingBell
{
    public class Core : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterMountable("divingbell", EntityDivingBellSeat.GetMountable);
            api.RegisterItemClass("ItemDivingBell", typeof(ItemDivingBell));
            api.RegisterEntity("EntityDivingBell", typeof(EntityDivingBell));
        }
    }
}
