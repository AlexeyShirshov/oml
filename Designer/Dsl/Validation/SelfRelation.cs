using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Worm.Designer
{

    [ValidationState(ValidationState.Enabled)]
    public partial class SelfRelation     {
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
            if (this.DirectFieldName == string.Empty)
            {
                context.LogError( "Direct field name is empty for SelfRelation " + this.Name, "WR05",  this);
                                
            }

            if (this.ReverseFieldName == string.Empty)
            {
                context.LogError("Reverse field name is empty for SelfRelation " + this.Name, "WR06", this);

            }

            if (this.Table == string.Empty)
            {
                context.LogError("Table name is empty for SelfRelation " + this.Name, "WR07", this);

            }
          
        }
    }
}
