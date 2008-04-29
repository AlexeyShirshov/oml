using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Worm.Designer
{

    [ValidationState(ValidationState.Enabled)]
    public partial class EntityReferencesTargetEntities
    {
        [ValidationMethod
 ( // These values select which events cause the method to be invoked.
      ValidationCategories.Open |
      ValidationCategories.Save |
      ValidationCategories.Menu
 )
]
        // This method is applied to each instance of the 
        // type in a model. 
        private void ValidateRelationFields(ValidationContext context)
        {
            if (this.RightFieldName == string.Empty)
            {
                context.LogError( "Right field name is empty", "WR01",  this);
                                
            }

            if (this.LeftFieldName == string.Empty)
            {
                context.LogError("Left field name is empty", "WR02", this);

            }

            if (this.Table == string.Empty)
            {
                context.LogError("Table name is empty", "WR03", this);

            }
          
        }
    }
}
