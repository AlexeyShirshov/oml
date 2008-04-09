using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using DslModeling = global::Microsoft.VisualStudio.Modeling;
namespace Worm.Designer
{
    public partial class Table
    {
        private string GetIdPropertyValue()
        {
            return "tbl" + this.Schema + this.Name; 
        }
    }

    public partial class Entity
    {
        private string GetIdPropertyValue()
        {
            if (this.Tables.Count > 0)
            {
                return "e_" + this.Tables[0].Schema + "_" + this.Name;
            }
            else 
            {
                return string.Empty;
            }
        }
    }

   
  
}
