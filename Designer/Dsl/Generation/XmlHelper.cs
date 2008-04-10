
namespace Worm.Designer
{
    using System;

    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using Microsoft.CSharp;
    using Microsoft.VisualBasic;
    using EnvDTE;
    using System.ComponentModel.Design;
    using VSLangProj;
    using CodeNamespace = System.CodeDom.CodeNamespace;
    using Process = EnvDTE.Process;
    using Worm.CodeGen.Core;
    using Worm.CodeGen.Core.Descriptors;


    public class XmlHelper
    {
        private const string GenericListInterface = "IList";
        private const string GenericListClass = "List";

        #region Private Variables
                 
        
        private Hashtable _propertyBag = null;
       
        
        #endregion

        public XmlHelper(Hashtable propertyBag)
        {
            _propertyBag = propertyBag;
            WormModel model = (WormModel)propertyBag["Generic.Model"];

            OrmObjectsDef ormObjectsDef = new OrmObjectsDef();
            ormObjectsDef.Namespace = model.DefaultNamespace;
            ormObjectsDef.SchemaVersion = model.SchemaVersion;
          
            foreach (Entity entity in model.Entities)
            {
                EntityDescription entityDescription = new EntityDescription(
                    entity.IdProperty, entity.Name, entity.Namespace, entity.Description, ormObjectsDef,
                   null, entity.Behaviour);
                entityDescription.MakeInterface = entity.MakeInterface;
                entityDescription.UseGenerics = entityDescription.UseGenerics;
                //todo base entity

                foreach (Table table in entity.Tables)
                {
                    TableDescription tableDescription = new TableDescription(table.IdProperty,
                        GetTableName(table.Schema, table.Name));
                    if (!ormObjectsDef.Tables.Exists(c => c.Identifier == table.IdProperty))
                    {
                        ormObjectsDef.Tables.Add(tableDescription);
                    }
                    entityDescription.Tables.Add(tableDescription);
                }
                
                foreach (Property property in entity.Properties)
                {
                    TypeDescription type = GetClrType(property.Type, property.Nullable, ormObjectsDef);
                    entityDescription.Properties.Add(new PropertyDescription
                        (property.Name, property.Alias, property.Attributes.Split(new char[] { '|' }),
                        property.Description, type, property.FieldName,
                        entityDescription.Tables.Find(t => t.Name == GetTableName(entity.Tables[0].Schema, property.Table)), 
                        property.FieldAccessLevel, property.AccessLevel));                    
                    
                }

                foreach (SupressedProperty sproperty in entity.SupressedProperties)
                {
                    PropertyDescription prop = new PropertyDescription(sproperty.Name);
                    prop.IsSuppressed = true;
                    entityDescription.SuppressedProperties.Add(prop);
                }

               
               
                ormObjectsDef.Entities.Add(entityDescription);

              
            
            }

            //Second loop to fill base entities and underlying entities
            foreach (Entity entity in model.Entities)
            {
                EntityDescription entityDescription = ormObjectsDef.Entities.Find(e => e.Name == entity.Name);
                if (!string.IsNullOrEmpty(entity.BaseEntity))
                {
                    EntityDescription baseEntity = ormObjectsDef.Entities.Find(e => e.Name == entity.BaseEntity);
                    entityDescription.BaseEntity = baseEntity;
                }
                
                foreach (SelfRelation selfRelation in entity.SelfRelations)
                {
                    SelfRelationDescription self = new SelfRelationDescription(entityDescription,
                         new SelfRelationTarget(selfRelation.DirectFieldName, selfRelation.DirectCascadeDelete, selfRelation.DirectAccessor),
                         new SelfRelationTarget(selfRelation.ReverseFieldName, selfRelation.ReverseCascadeDelete, selfRelation.ReverseAccessor),
                         entityDescription.Tables.Find(t => t.Name == GetTableName(entity.Tables[0].Schema, selfRelation.Table)),
                         ormObjectsDef.Entities.Find(e => e.Name == selfRelation.UnderlyingEntity),
                         selfRelation.Disabled);
                    if (!string.IsNullOrEmpty(selfRelation.ReverseAccessedEntityType))
                    {
                        TypeDescription typeReverse = GetClrType(selfRelation.ReverseAccessedEntityType, false, ormObjectsDef);                        
                        self.Reverse.AccessedEntityType = typeReverse;
                    }
                    if (!string.IsNullOrEmpty(selfRelation.ReverseAccessedEntityType))
                    {
                        TypeDescription typeDirect = GetClrType(selfRelation.DirectAccessedEntityType, false, ormObjectsDef);
                        self.Direct.AccessedEntityType = typeDirect;
                    }
                    ormObjectsDef.Relations.Add(self);
                }

            }
           
            ReadOnlyCollection<Microsoft.VisualStudio.Modeling.ModelElement> relations = (
            (model.Store.ElementDirectory.FindElements(Worm.Designer.EntityReferencesTargetEntities.DomainClassId)));
            foreach (Worm.Designer.EntityReferencesTargetEntities relation in relations)
            {
                if (relation.TargetEntity != relation.SourceEntity)
                {
                    EntityDescription leftEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.SourceEntity.IdProperty);
                    EntityDescription rightEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.TargetEntity.IdProperty);
                    RelationDescription self = new RelationDescription(
                        new LinkTarget(leftEntity, relation.LeftFieldName, relation.LeftCascadeDelete),
                        new LinkTarget(rightEntity, relation.RightFieldName, relation.RightCascadeDelete),
                        ormObjectsDef.Tables.Find(t => t.Name == GetTableName(relation.TargetEntity.Tables[0].Schema, relation.Table)),
                        ormObjectsDef.Entities.Find(e => e.Identifier == relation.UndelyingEntity),
                        relation.Disabled);
                    ormObjectsDef.Relations.Add(self);
                }

            }      
            
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;
            ormObjectsDef.GetXmlDocument().WriteTo(xw);

            _propertyBag.Add("CodeGeneration.PrimaryOutput", sw.ToString());


            //_model = propertyBag["Generic.Model"];
            ////if (string.IsNullOrEmpty(_model.Namespace))
            ////    _namespace = propertyBag["Generic.Namespace"].ToString();
            ////else
            ////    _namespace = _model.Namespace;


            //_dte = Helper.GetDTE(_propertyBag["Generic.ProcessID"].ToString());
            //_propertyBag.Add("Generic.DTE", _dte);

            //_modelFileName = (string)_propertyBag["Generic.ModelFileFullName"];
            //_modelFilePath = Path.GetDirectoryName(_modelFileName);
            //_projectItem = _dte.Solution.FindProjectItem(_modelFileName);

          }

        private static string GetTableName(string schema, string table)
        {
            return "[" + schema + "].[" + table + "]";
        }

        private static TypeDescription GetClrType(string dbType, bool nullable, OrmObjectsDef odef)
        {
            TypeDescription t = null;
            string id = null;
            string type = null;

            switch (dbType)
            {
                case "rowversion":
                case "timestamp":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                    id = "tString";
                    type = "System.String";
                    break;
                case "int":
                    id = "tInt32";
                    type = "System.Int32";
                    break;
                case "smallint":
                    id = "tInt16";
                    type = "System.Int16";
                    break;
                case "bigint":
                    id = "tInt64";
                    type = "System.Int64";
                    break;
                case "tinyint":
                    id = "tByte";
                    type = "System.Byte";
                    break;
                case "datetime":
                case "smalldatetime":
                    id = "tDateTime";
                    type = "System.DateTime";
                    break;
                case "money":
                case "numeric":
                case "decimal":
                    id = "tDecimal";
                    type = "System.Decimal";
                    break;
                case "float":
                    id = "tDouble";
                    type = "System.Double";
                    break;
                case "real":
                    id = "tSingle";
                    type = "System.Single";
                    break;
                case "varbinary":
                case "binary":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                case "bit":
                    id = "tBoolean";
                    type = "System.Boolean";
                    break;
                case "xml":
                    id = "tXML";
                    type = "System.Xml.XmlDocument";
                    break;
                case "uniqueidentifier":
                    id = "tGUID";
                    type = "System.Guid";
                    break;
                case "image":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                default:
                    throw new ArgumentException("Unknown database type " + dbType);
            }

            if (nullable)
                id += "nullable";

            t = odef.GetType(id, false);
            if (t == null)
            {
                Type tp = GetTypeByName(type);
                if (nullable && tp.IsValueType)
                    type = String.Format("System.Nullable`1[{0}]", type);

                t = new TypeDescription(id, type);
                odef.Types.Add(t);
            }
            return t;
        }
        private static Type GetTypeByName(string type)
        {
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type tp = a.GetType(type, false, true);
                if (tp != null)
                    return tp;
            }
            throw new TypeLoadException("Cannot load type " + type);
        }
       
    }
}
