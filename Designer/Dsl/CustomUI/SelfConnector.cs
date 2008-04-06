using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DslModeling = global::Microsoft.VisualStudio.Modeling;

namespace Worm.Designer
{

    public partial class EntityReferencesSelfTargetEntitiesBuilder
    {


        private static bool CanAcceptEntityAndEntityAsSourceAndTarget(Entity sourceEntity, Entity targetEntity)
        {
            return sourceEntity == targetEntity;
        }

        private static bool CanAcceptEntityAsSource(Entity candidate)
        {
            return true;
        }
    }
}
