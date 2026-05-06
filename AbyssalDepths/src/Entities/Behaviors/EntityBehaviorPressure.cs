using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common.Entities;

namespace AbyssalDepths.src.Entities.Behaviors
{
    public class EntityBehaviorPressure : EntityBehavior
    {
        public override string PropertyName() => "Pressure";

        public EntityBehaviorPressure(Entity entity) : base(entity)
        {
        }

        // this will fully replace ModSystemDepthPressure in the future but its a placeholder for now

    }
}
