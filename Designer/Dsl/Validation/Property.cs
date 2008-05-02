using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Worm.Designer
{

    [ValidationState(ValidationState.Enabled)]
    public partial class Property
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
        private void ValidatePropertyTable(ValidationContext context)
        {
            if (string.IsNullOrEmpty(this.Table))
            {
                context.LogError( "Property " + this.Name + " doesn't containt Table value", "WR04",  this);
                                
            }

        }
    }
}
