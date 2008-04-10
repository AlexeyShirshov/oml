using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using DslModeling = global::Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

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

    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(Property), FireTime = TimeToFire.TopLevelCommit)]
    public class PropertyAddRule : AddRule
    {     

        public override void ElementAdded(ElementAddedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            

            Property property = e.ModelElement as Property;
            if (property != null)
            {
                if (string.IsNullOrEmpty(property.Table) && property.Entity.Tables.Count == 1)
                {
                    property.Table = property.Entity.Tables[0].Name;
                }
                if (string.IsNullOrEmpty(property.FieldName) && !string.IsNullOrEmpty(property.Name))
                {
                    property.FieldName = property.Name;
                }
            }
        }


    }

    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(Entity), FireTime = TimeToFire.TopLevelCommit)]
    public class EntityAddRule : AddRule
    {

        public override void ElementAdded(ElementAddedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }



            Entity entity = e.ModelElement as Entity;
            if (entity != null)
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                DTE dte = Helper.GetDTE(currentProcess.Id.ToString());


                EnvDTE.CodeModel cm;

                EnvDTE.ProjectItem item;
                EnvDTE.Project project = dte.Solution.Projects.Item(1);

                for (int i = 1; i < project.ProjectItems.Count; i++)
                {
                    item = project.ProjectItems.Item(i);
                    if (item.FileCodeModel != null)
                    {
                        // do stuff
                    }
                }


                //if (string.IsNullOrEmpty(property.Table) && property.Entity.Tables.Count == 1)
                //{
                //    property.Table = property.Entity.Tables[0].Name;
                //}
            }
        }


    }


    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(SelfRelation), FireTime = TimeToFire.TopLevelCommit)]
    public class SelfRelationAddRule : AddRule
    {

        public override void ElementAdded(ElementAddedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            //if (GeneralHelper.IgnoreChange(e.ModelElement)) return;

            SelfRelation selfRelation = e.ModelElement as SelfRelation;
            if (selfRelation != null)
            {
                if (string.IsNullOrEmpty(selfRelation.Table) && selfRelation.Entity.Tables.Count == 1)
                {
                    selfRelation.Table = selfRelation.Entity.Tables[0].Name;
                }
            }

  
        }


    }

  

    /// <summary>
    /// Custom BusinessProcessesDesignerDomainModel methods.
    /// </summary>
    public partial class DesignerDomainModel
    {
        /// <summary>
        /// Returns the non-generated domain model types.
        /// </summary>
        /// <returns>An array of types.</returns>
        protected override Type[] GetCustomDomainModelTypes()
        {


            return new System.Type[] { typeof(PropertyAddRule), typeof(SelfRelationAddRule)
            , typeof(EntityAddRule)};
        }
    }

  
}
