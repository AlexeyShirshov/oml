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
                return "e_" + this.Name;
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

                if (property.Entity.WormModel.Types.FindAll(t => t.Name == property.Type).Count == 0)
                {
                    using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        WormType type = new WormType(property.Entity.WormModel.Store);
                        type.WormModel = property.Entity.WormModel;
                        type.Name = property.Type;
                        string idProperty = Utils.GetIdProperty(property.Type);
                        if (idProperty != null && property.Nullable)
                        {
                            idProperty += "nullable";
                        }
                        type.IdProperty = idProperty ?? "t" + property.Type;
                       
                       
                        txAdd.Commit();
                    }
                }
            }
        }
       

    }

    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(Property), FireTime = TimeToFire.TopLevelCommit)]
    public class PropertyChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            Property property = e.ModelElement as Property;
            if (property != null)
            {
                if (property.Entity.WormModel.Types.FindAll(t => t.Name == property.Type).Count == 0)
                {
                    using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        WormType type = new WormType(property.Entity.WormModel.Store);
                        type.WormModel = property.Entity.WormModel;
                        type.Name = property.Type;
                        string idProperty =  Utils.GetIdProperty(property.Type);
                        if (idProperty != null && property.Nullable)
                        {
                            idProperty += "nullable";
                        }
                        type.IdProperty = idProperty ?? "t" + property.Type;


                        txAdd.Commit();
                    }
                }
            }
            base.ElementPropertyChanged(e);
        }
   
    }

    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(Property), FireTime = TimeToFire.TopLevelCommit)]
    public class PropertyDeleteRule : DeletingRule
    {

        public override void ElementDeleting(ElementDeletingEventArgs e)
        {
         
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            Property property = e.ModelElement as Property;
            if (property != null)
            {
                using (Transaction txAdd = e.ModelElement.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        int count = 0;
                        foreach (Entity entity in property.Entity.WormModel.Entities)
                        {
                            count += entity.Properties.FindAll(p => p.Type == property.Type).Count;
                            count += entity.SupressedProperties.FindAll(p => p.Type == property.Type).Count;
                        }
                        if (count == 1)
                        {
                            property.Entity.WormModel.Types.Find(t => t.Name == property.Type).Delete();
                        }
                      txAdd.Commit();
                    }
            }

            base.ElementDeleting(e);
        }
    }

    [RuleOn(typeof(SupressedProperty), FireTime = TimeToFire.TopLevelCommit)]
    public class SupressedPropertyAddRule : AddRule
    {

        public override void ElementAdded(ElementAddedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }



            SupressedProperty property = e.ModelElement as SupressedProperty;
            if (property != null)
            {

                if (property.Entity.WormModel.Types.FindAll(t => t.Name == property.Type).Count == 0)
                {
                    using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        WormType type = new WormType(property.Entity.WormModel.Store);
                        type.WormModel = property.Entity.WormModel;
                        type.Name = property.Type;
                        string idProperty = Utils.GetIdProperty(property.Type);
                      
                        type.IdProperty = idProperty ?? "t" + property.Type;


                        txAdd.Commit();
                    }
                }
            }
        }
       
  
    }
    [RuleOn(typeof(SupressedProperty), FireTime = TimeToFire.TopLevelCommit)]
    public class SupressedPropertyChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            SupressedProperty property = e.ModelElement as SupressedProperty;
            if (property != null)
            {
                if (property.Entity.WormModel.Types.FindAll(t => t.Name == property.Type).Count == 0)
                {
                    using (Transaction txAdd = property.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {
                        WormType type = new WormType(property.Entity.WormModel.Store);
                        type.WormModel = property.Entity.WormModel;
                        type.Name = property.Type;
                        string idProperty = Utils.GetIdProperty(property.Type);

                        type.IdProperty = idProperty ?? "t" + property.Type;


                        txAdd.Commit();
                    }
                }
            }
            base.ElementPropertyChanged(e);
        }
    }
        

 /// <summary>
 /// Implements one add rule for the Property domain class.
 /// </summary>
 [RuleOn(typeof(SupressedProperty), FireTime = TimeToFire.TopLevelCommit)]
    public class SupressedPropertyDeleteRule : DeletingRule
 {

     public override void ElementDeleting(ElementDeletingEventArgs e)
     {
         
         if (e == null)
         {
             throw new ArgumentNullException("e");
         }
         SupressedProperty property = e.ModelElement as SupressedProperty;
         if (property != null)
         {
             using (Transaction txAdd = e.ModelElement.Store.TransactionManager.BeginTransaction("Add property"))
             {
                 int count = 0;
                 foreach (Entity entity in property.Entity.WormModel.Entities)
                 {
                     count += entity.Properties.FindAll(p => p.Type == property.Type).Count;
                     count += entity.SupressedProperties.FindAll(p => p.Type == property.Type).Count;
                 }
                 if (count == 1)
                 {
                     property.Entity.WormModel.Types.Find(t => t.Name == property.Type).Delete();
                 }
                 txAdd.Commit();
             }
         }

         base.ElementDeleting(e);
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
                //entity.WormModel.Store.
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                DTE dte = Helper.GetDTE(currentProcess.Id.ToString());
                if (dte.ActiveDocument != null)
                {
                    string defaultNamespace = (string)(dte.ActiveDocument.ProjectItem.ContainingProject.Properties.Item("DefaultNamespace").Value);
                    if (string.IsNullOrEmpty(entity.WormModel.DefaultNamespace))
                    {
                        entity.WormModel.DefaultNamespace = defaultNamespace;
                    }
                    if (string.IsNullOrEmpty(entity.Namespace))
                    {
                        entity.Namespace = defaultNamespace;
                    }

                    
                }

                if (entity.WormModel.Types.FindAll(t => t.Name == entity.Name).Count == 0)
                {
                    using (Transaction txAdd = entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(entity.WormModel.Store);
                        type.WormModel = entity.WormModel;
                        type.Name = entity.Name;
                        type.IdProperty = "t" + entity.Name;
                        txAdd.Commit();

                    }
                }
            }
        }


    }

    [RuleOn(typeof(Entity), FireTime = TimeToFire.TopLevelCommit)]
    public class EntityChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }


            Entity entity = e.ModelElement as Entity;
            if (entity != null)
            {
                if (entity.WormModel.Types.FindAll(t => t.Name == entity.Name).Count == 0)
                {
                    using (Transaction txAdd = entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(entity.WormModel.Store);
                        type.WormModel = entity.WormModel;
                        type.Name = entity.Name;
                        type.IdProperty = "t" + entity.Name;
                        txAdd.Commit();

                    }
                }
            }
            base.ElementPropertyChanged(e);
        }
      
    }

    /// <summary>
    /// Implements one add rule for the Property domain class.
    /// </summary>
    [RuleOn(typeof(Entity), FireTime = TimeToFire.TopLevelCommit)]
    public class EntityDeleteRule : DeletingRule
    {
     
        public override void ElementDeleting(ElementDeletingEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            Entity entity = e.ModelElement as Entity;
            if (entity != null)
            {
                using (Transaction txAdd = e.ModelElement.Store.TransactionManager.BeginTransaction("Add property"))
                {
                    entity.WormModel.Types.Find(t => t.Name ==  entity.Name).Delete();
                    List<Table> tables = entity.WormModel.Tables.FindAll(t => t.Entity == entity);
                    foreach (Table table in tables)
                    {
                        table.Delete();
                    }
                    txAdd.Commit();
                }
            }

            base.ElementDeleting(e);
        }
    }

    [RuleOn(typeof(Table), FireTime = TimeToFire.TopLevelCommit)]
    public class TableDeleteRule : DeletingRule
    {
       
          public override void ElementDeleting(ElementDeletingEventArgs e)
        {
         
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            Table table = e.ModelElement as Table;
            if (table != null)
            {
                using (Transaction txAdd = e.ModelElement.Store.TransactionManager.BeginTransaction("Add property"))
                {
                    table.Entity.WormModel.Tables.Find(t => t.Name == table.Name).Delete();                   
                    txAdd.Commit();
                }
            }

            base.ElementDeleting(e);
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

            SelfRelation relation = e.ModelElement as SelfRelation;
            if (relation != null)
            {
                if (string.IsNullOrEmpty(relation.Table) && relation.Entity.Tables.Count == 1)
                {
                    relation.Table = relation.Entity.Tables[0].Name;
                }
                 relation.DirectAccessedEntityType = relation.Entity.Name;
                 relation.ReverseAccessedEntityType = relation.Entity.Name;


            }

        }
        private static string GetIdProperty(Property property)
        {
            string idProperty = null;
            switch (property.Type)
            {
                case "System.Byte[]":
                    idProperty = "tBytes";
                    break;
                case "System.String":
                    idProperty = "tString";
                    break;
                case "System.Int32":
                    idProperty = "tInt32";
                    break;
                case "System.Int16":
                    idProperty = "tInt16";
                    break;
                case "System.Int64":
                    idProperty = "tInt64";
                    break;
                case "System.Byte":
                    idProperty = "tByte";
                    break;
                case "System.DateTime":
                    idProperty = "tDateTime";
                    break;
                case "System.Decimal":
                    idProperty = "tDecimal";
                    break;
                case "System.Double":
                    idProperty = "tDouble";
                    break;
                case "System.Single":
                    idProperty = "tSingle";
                    break;

                case "System.Boolean":
                    idProperty = "tBoolean";
                    break;
                case "System.Xml.XmlDocument":
                    idProperty = "tXML";
                    break;
                case "System.Guid":
                    idProperty = "tGUID";
                    break;

            }
            return idProperty;
        }

    }

    [RuleOn(typeof(SelfRelation), FireTime = TimeToFire.TopLevelCommit)]
    public class SelfRelationChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }


            SelfRelation relation = e.ModelElement as SelfRelation;
            if (relation != null)
            {
                if (relation.Entity.WormModel.Types.FindAll(t => t.Name == relation.DirectAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.Entity.WormModel.Store);
                        type.WormModel = relation.Entity.WormModel;
                        type.Name = relation.DirectAccessedEntityType;
                        type.IdProperty = "t" + type.Name;
                        txAdd.Commit();

                    }
                }
                if (relation.Entity.WormModel.Types.FindAll(t => t.Name == relation.ReverseAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.Entity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.Entity.WormModel.Store);
                        type.WormModel = relation.Entity.WormModel;
                        type.Name = relation.ReverseAccessedEntityType;
                        type.IdProperty = "t" + type.Name;
                        txAdd.Commit();

                    }
                }
            }
            base.ElementPropertyChanged(e);
        }
        private static string GetIdProperty(Property property)
        {
            string idProperty = null;
            switch (property.Type)
            {
                case "System.Byte[]":
                    idProperty = "tBytes";
                    break;
                case "System.String":
                    idProperty = "tString";
                    break;
                case "System.Int32":
                    idProperty = "tInt32";
                    break;
                case "System.Int16":
                    idProperty = "tInt16";
                    break;
                case "System.Int64":
                    idProperty = "tInt64";
                    break;
                case "System.Byte":
                    idProperty = "tByte";
                    break;
                case "System.DateTime":
                    idProperty = "tDateTime";
                    break;
                case "System.Decimal":
                    idProperty = "tDecimal";
                    break;
                case "System.Double":
                    idProperty = "tDouble";
                    break;
                case "System.Single":
                    idProperty = "tSingle";
                    break;

                case "System.Boolean":
                    idProperty = "tBoolean";
                    break;
                case "System.Xml.XmlDocument":
                    idProperty = "tXML";
                    break;
                case "System.Guid":
                    idProperty = "tGUID";
                    break;

            }
            return idProperty;
        }
    }




    [RuleOn(typeof(EntityReferencesTargetEntities), FireTime = TimeToFire.TopLevelCommit)]
    public class EntityReferencesTargetEntitiesAddRule : AddRule
    {

        public override void ElementAdded(ElementAddedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }


            EntityReferencesTargetEntities relation = e.ModelElement as EntityReferencesTargetEntities;
            if (relation != null)
            {
                if (string.IsNullOrEmpty(relation.LeftEntity))
                {
                    relation.LeftEntity = relation.SourceEntity.Name;
                }
                if (string.IsNullOrEmpty(relation.RightEntity))
                {
                    relation.RightEntity = relation.TargetEntity.Name;
                }
                if (string.IsNullOrEmpty(relation.LeftAccessedEntityType))
                {
                    relation.LeftAccessedEntityType = relation.SourceEntity.Name;
                }
                if (string.IsNullOrEmpty(relation.RightAccessedEntityType))
                {
                    relation.RightAccessedEntityType = relation.TargetEntity.Name;
                }

                if (relation.TargetEntity.WormModel.Types.FindAll(t => t.Name == relation.LeftAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.TargetEntity.WormModel.Store);
                        type.WormModel = relation.TargetEntity.WormModel;
                        type.Name = relation.LeftAccessedEntityType;
                        type.IdProperty = "t" + relation.SourceEntity.Name;
                        txAdd.Commit();

                    }
                }
                if (relation.TargetEntity.WormModel.Types.FindAll(t => t.Name == relation.RightAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.TargetEntity.WormModel.Store);
                        type.WormModel = relation.TargetEntity.WormModel;
                        type.Name = relation.RightAccessedEntityType;
                        type.IdProperty = "t" + relation.TargetEntity.Name;
                        txAdd.Commit();

                    }
                }
            }
        }


    }

    [RuleOn(typeof(EntityReferencesTargetEntities), FireTime = TimeToFire.TopLevelCommit)]
    public class EntityReferencesTargetEntitiesChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            EntityReferencesTargetEntities relation = e.ModelElement as EntityReferencesTargetEntities;
            if (relation != null)
            {
                if (relation.TargetEntity.WormModel.Types.FindAll(t => t.Name == relation.LeftAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.TargetEntity.WormModel.Store);
                        type.WormModel = relation.TargetEntity.WormModel;
                        type.Name = relation.LeftAccessedEntityType;
                        type.IdProperty = "t" + relation.SourceEntity.Name;
                        txAdd.Commit();

                    }
                }
                if (relation.TargetEntity.WormModel.Types.FindAll(t => t.Name == relation.RightAccessedEntityType).Count == 0)
                {
                    using (Transaction txAdd = relation.TargetEntity.WormModel.Store.TransactionManager.BeginTransaction("Add property"))
                    {

                        WormType type = new WormType(relation.TargetEntity.WormModel.Store);
                        type.WormModel = relation.TargetEntity.WormModel;
                        type.Name = relation.RightAccessedEntityType;
                        type.IdProperty = "t" + relation.TargetEntity.Name;
                        txAdd.Commit();

                    }
                }
            }
            base.ElementPropertyChanged(e);
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
            , typeof(EntityAddRule), typeof(SupressedPropertyAddRule),  typeof( EntityReferencesTargetEntitiesAddRule),
            typeof(PropertyChangeRule), typeof(SelfRelationChangeRule)
            , typeof(EntityChangeRule), typeof(SupressedPropertyChangeRule),  
            typeof( EntityReferencesTargetEntitiesChangeRule),
            typeof(EntityDeleteRule), typeof(PropertyDeleteRule), typeof(SupressedPropertyDeleteRule),
            typeof(TableDeleteRule)};
        }

       
    }

    
    public delegate void ModelPropertyAddedHandler(ElementAddedEventArgs e);
    public delegate void ModelPropertyDeletedHandler(ElementDeletedEventArgs e);
    public delegate void ModelPropertyChangedHandler(ElementPropertyChangedEventArgs e);

    public delegate void ModelSelfRelationAddedHandler(ElementAddedEventArgs e);
    public delegate void ModelSelfRelationDeletedHandler(ElementDeletedEventArgs e);
    public delegate void ModelSelfRelationChangedHandler(ElementPropertyChangedEventArgs e);

    public delegate void ModelRelationAddedHandler(ElementAddedEventArgs e);
    public delegate void ModelRelationDeletedHandler(ElementDeletedEventArgs e);
    public delegate void ModelRelationChangedHandler(ElementPropertyChangedEventArgs e);

    public partial class WormModel : ModelElement
    {
        public event ModelPropertyAddedHandler ModelPropertyAdded;
        public event ModelPropertyDeletedHandler ModelPropertyDeleted;
        public event ModelPropertyChangedHandler ModelPropertyChanged;

        public event ModelSelfRelationAddedHandler ModelSelfRelationAdded;
        public event ModelSelfRelationDeletedHandler ModelSelfRelationDeleted;
        public event ModelSelfRelationChangedHandler ModelSelfRelationChanged;

        public event ModelRelationAddedHandler ModelRelationAdded;
        public event ModelRelationDeletedHandler ModelRelationDeleted;
        public event ModelRelationChangedHandler ModelRelationChanged;
        
        public void OnModelPropertyAdded(ElementAddedEventArgs e)
        {
            if (ModelPropertyAdded != null)
                ModelPropertyAdded(e);
        }

        public void OnModelPropertyDeleted(ElementDeletedEventArgs e)
        {
            if (ModelPropertyDeleted != null)
                ModelPropertyDeleted(e);
        }
        
        public void OnModelPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            if (ModelPropertyChanged != null)
                ModelPropertyChanged(e);
        }

       
    }
    public class Utils
    {
        public static string GetIdProperty(string type)
        {
            string idProperty = null;
            switch (type)
            {
                case "System.Byte[]":
                    idProperty = "tBytes";
                    break;
                case "System.String":
                    idProperty = "tString";
                    break;
                case "System.Int32":
                    idProperty = "tInt32";
                    break;
                case "System.Int16":
                    idProperty = "tInt16";
                    break;
                case "System.Int64":
                    idProperty = "tInt64";
                    break;
                case "System.Byte":
                    idProperty = "tByte";
                    break;
                case "System.DateTime":
                    idProperty = "tDateTime";
                    break;
                case "System.Decimal":
                    idProperty = "tDecimal";
                    break;
                case "System.Double":
                    idProperty = "tDouble";
                    break;
                case "System.Single":
                    idProperty = "tSingle";
                    break;

                case "System.Boolean":
                    idProperty = "tBoolean";
                    break;
                case "System.Xml.XmlDocument":
                    idProperty = "tXML";
                    break;
                case "System.Guid":
                    idProperty = "tGUID";
                    break;

            }
            return idProperty;
        }
    }

}
