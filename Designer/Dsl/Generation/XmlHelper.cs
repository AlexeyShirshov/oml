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
using Microsoft.VisualStudio.Modeling;
using EnvDTE;
using System.ComponentModel.Design;
using VSLangProj;
using CodeNamespace = System.CodeDom.CodeNamespace;
using Process = EnvDTE.Process;
using Worm.CodeGen.Core;
using Worm.CodeGen.Core.Descriptors;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
namespace Worm.Designer
{
   

    public class XmlHelper
    {
      
        
        #region Private Variables
                 
        
        private static Hashtable _propertyBag = null;

        public static Hashtable PropertyBag
        {
            get { return _propertyBag; }
            set { _propertyBag = value; }
        }
        
        #endregion

        public static void Generate()
        {
            WormModel model = (WormModel)_propertyBag["Generic.Model"];

            // Читаем настройки генерации
            SetGeneratorSettings(model);


            OrmObjectsDef ormObjectsDef = new OrmObjectsDef();            
            ormObjectsDef.SchemaVersion = model.SchemaVersion;

            // Определяем DefaultNamespace
            if (string.IsNullOrEmpty(model.DefaultNamespace))
            {
                string defaultNamespace = ((string)_propertyBag["Generic.ModelFile"]).Replace(".wxml", "").Replace(".vb", "");
                ormObjectsDef.Namespace = defaultNamespace;
            }
            else
            {
                ormObjectsDef.Namespace = model.DefaultNamespace;
            }

            // Заполняем сущности
            foreach (Entity entity in model.Entities)
            {
                // Создаем сущность
                EntityDescription entityDescription = new EntityDescription(
                    entity.IdProperty, entity.Name, entity.Namespace, entity.Description, ormObjectsDef,
                   null, entity.Behaviour);
                entityDescription.MakeInterface = bool.Parse(entity.MakeInterface);
                entityDescription.UseGenerics = bool.Parse(entity.UseGenerics);
                entityDescription.InheritsBaseTables = bool.Parse(entity.InheritsBase);
                
                SetEntityType(model, entityDescription, entity.Name, ormObjectsDef);
                
                // Добавляем таблицы в сущность и в модель
                foreach (Table table in entity.Tables)
                {
                    TableDescription tableDescription = new TableDescription(table.IdProperty,
                        GetTableName(table.Schema, table.Name));
                    if (!ormObjectsDef.Tables.Exists(c => c.Identifier == table.IdProperty))
                    {
                        ormObjectsDef.Tables.Add(tableDescription);
                    }
                    if (!entityDescription.Tables.Exists(c => c.Identifier == table.IdProperty))
                    {
                        entityDescription.Tables.Add(tableDescription);
                    }
                }

                // Если наследование - Добавляем родительские таблицы в сущность и в модель
                if (entityDescription.InheritsBaseTables)
                {
                    Entity baseEntity = model.Entities.Find(e => e.Name == entity.BaseEntity);
                    if (baseEntity != null)
                    {
                        foreach (Table table in baseEntity.Tables)
                        {
                            TableDescription tableDescription = new TableDescription(table.IdProperty,
                                 GetTableName(table.Schema, table.Name));
                            if (!ormObjectsDef.Tables.Exists(c => c.Identifier == table.IdProperty))
                            {
                                ormObjectsDef.Tables.Add(tableDescription);
                            }
                            if (!entityDescription.Tables.Exists(c => c.Identifier == table.IdProperty))
                            {
                                entityDescription.Tables.Add(tableDescription);
                            }
                        }
                    }
                }
                
                // Заполняем свойства
                foreach (Property property in entity.Properties)
                {
                    TypeDescription type = GetType(model, property.Type, ormObjectsDef, bool.Parse(property.Nullable));
                    List<string> attributes = new List<string>();
                    if (bool.Parse(property.Factory)) attributes.Add("Factory");
                    if (bool.Parse(property.InsertDefault)) attributes.Add("InsertDefault");
                    if (bool.Parse(property.PK)) attributes.Add("PK");
                    if (bool.Parse(property.PrimaryKey)) attributes.Add("PrimaryKey");
                    if (bool.Parse(property.Private)) attributes.Add("Private");
                    if (bool.Parse(property.ReadOnly)) attributes.Add("ReadOnly");
                    if (bool.Parse(property.RowVersion)) attributes.Add("RowVersion");
                    if (bool.Parse(property.RV)) attributes.Add("RV");
                    if (bool.Parse(property.SyncInsert)) attributes.Add("SyncInsert");
                    if (bool.Parse(property.SyncUpdate)) attributes.Add("SyncUpdate");
                    //if (attributes.Count == 0)
                    //{
                    //    attributes.Add("None");
                    //}

                    PropertyDescription propertyDescription = new PropertyDescription
                        (property.Name, property.Alias, attributes.ToArray(),
                        property.Description, type, property.FieldName,
                        entityDescription.Tables.Find(t => t.Name == GetTableName(entity.Tables[0].Schema, property.Table)),
                        property.FieldAccessLevel, property.AccessLevel);
                    
                    propertyDescription.Disabled = bool.Parse(property.Disabled);
                    propertyDescription.Obsolete = property.Obsolete;
                    propertyDescription.ObsoleteDescripton = property.ObsoleteDescription;
                    propertyDescription.EnablePropertyChanged = bool.Parse(property.EnablePropertyChanged);
                    entityDescription.Properties.Add(propertyDescription);
                }

                // Заполняем Supressed свойства
                foreach (SupressedProperty sproperty in entity.SupressedProperties)
                {
                    PropertyDescription prop = new PropertyDescription(sproperty.Name);
                    prop.IsSuppressed = true;
                    entityDescription.SuppressedProperties.Add(prop);
                }

               
               // Сущность добавлена
                ormObjectsDef.Entities.Add(entityDescription);

              
            
            }

            //Second loop to fill base entities and underlying entities
            foreach (Entity entity in model.Entities)
            {
                // Проставляем родительскую сущность
                EntityDescription entityDescription = ormObjectsDef.Entities.Find(e => e.Name == entity.Name);
                if (!string.IsNullOrEmpty(entity.BaseEntity))
                {
                    EntityDescription baseEntity = ormObjectsDef.Entities.Find(e => e.Name == entity.BaseEntity);
                    entityDescription.BaseEntity = baseEntity;
                }
                // Заполняем selfRelation
                foreach (SelfRelation selfRelation in entity.SelfRelations)
                {
                    SelfRelationDescription self = new SelfRelationDescription(entityDescription,
                         new SelfRelationTarget(selfRelation.DirectFieldName, bool.Parse(selfRelation.DirectCascadeDelete), selfRelation.DirectAccessor),
                         new SelfRelationTarget(selfRelation.ReverseFieldName, bool.Parse(selfRelation.ReverseCascadeDelete), selfRelation.ReverseAccessor),
                         entityDescription.Tables.Find(t => t.Name == GetTableName(entity.Tables[0].Schema, selfRelation.Table)),
                         ormObjectsDef.Entities.Find(e => e.Name == selfRelation.UnderlyingEntity),
                         bool.Parse(selfRelation.Disabled));
                    if (!string.IsNullOrEmpty(selfRelation.ReverseAccessedEntityType))
                    {
                        TypeDescription typeReverse = GetType(model, selfRelation.ReverseAccessedEntityType, ormObjectsDef);
                        self.Reverse.AccessedEntityType = typeReverse;
                    }
                    if (!string.IsNullOrEmpty(selfRelation.ReverseAccessedEntityType))
                    {
                        TypeDescription typeDirect = GetType(model, selfRelation.DirectAccessedEntityType, ormObjectsDef);
                        self.Direct.AccessedEntityType = typeDirect;
                    }
                    ormObjectsDef.Relations.Add(self);
                }

            }

            // Заполняем  relations
            ReadOnlyCollection<Microsoft.VisualStudio.Modeling.ModelElement> relations = (
            (model.Store.ElementDirectory.FindElements(Worm.Designer.EntityReferencesTargetEntities.DomainClassId)));
            foreach (Worm.Designer.EntityReferencesTargetEntities relation in relations)
            {
                EntityDescription leftEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.SourceEntity.IdProperty);
                EntityDescription rightEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.TargetEntity.IdProperty);
                if (relation.TargetEntity != relation.SourceEntity)
                {                    
                    RelationDescription self = new RelationDescription(
                        new LinkTarget(leftEntity, relation.LeftFieldName, bool.Parse(relation.LeftCascadeDelete)),
                        new LinkTarget(rightEntity, relation.RightFieldName, bool.Parse(relation.RightCascadeDelete)),
                        ormObjectsDef.Tables.Find(t => t.Name == GetTableName(relation.TargetEntity.Tables[0].Schema, relation.Table)),
                        ormObjectsDef.Entities.Find(e => e.Name == relation.UnderlyingEntity),
                        bool.Parse(relation.Disabled));
                    ormObjectsDef.Relations.Add(self);
                }
                // если relation замкнут на себя - создаем SelfRelation
                else
                {
                    SelfRelationDescription self = new SelfRelationDescription(leftEntity,
                         new SelfRelationTarget(relation.LeftFieldName, bool.Parse(relation.LeftCascadeDelete), relation.LeftAccessorName),
                         new SelfRelationTarget(relation.RightFieldName, bool.Parse(relation.RightCascadeDelete), relation.RightAccessorName),
                         leftEntity.Tables.Find(t => t.Name == GetTableName(relation.TargetEntity.Tables[0].Schema, relation.Table)),
                         ormObjectsDef.Entities.Find(e => e.Name == relation.UnderlyingEntity),
                         bool.Parse(relation.Disabled));
                    if (!string.IsNullOrEmpty(relation.LeftAccessedEntityType))
                    {
                        TypeDescription typeLeft = GetType(model, relation.LeftAccessedEntityType, ormObjectsDef);
                        self.Reverse.AccessedEntityType = typeLeft;
                    }
                    if (!string.IsNullOrEmpty(relation.RightAccessedEntityType))
                    {
                        TypeDescription typeRight = GetType(model, relation.RightAccessedEntityType, ormObjectsDef);
                        self.Direct.AccessedEntityType = typeRight;
                    }
                    ormObjectsDef.Relations.Add(self);
                }

            }      
            
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;
            ormObjectsDef.GetXmlDocument().WriteTo(xw);

            _propertyBag.Add("CodeGeneration.PrimaryXml", sw.ToString());
            string code = GenerateCode(sw.ToString(), (string)(_propertyBag["Generic.Ext"]));

            _propertyBag.Add("CodeGeneration.PrimaryOutput", code);

          }

        private static void SetGeneratorSettings(WormModel model)
        {
            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.EntitySchemaDefClassNameSuffix = model.EntitySchemaDefClassNameSuffix;
            settings.PrivateMembersPrefix = model.PrivateMembersPrefix;
            settings.Split = bool.Parse(model.Split);
            settings.ClassNamePrefix = model.ClassNamePrefix;
            settings.ClassNameSuffix = model.ClassNameSuffix;
            settings.FileNamePrefix = model.FileNamePrefix;
            settings.FileNameSuffix = model.FileNameSuffix;

            LanguageSpecificHacks hacks = LanguageSpecificHacks.None;
            if (bool.Parse(model.AddOptionsExplicit)) hacks |= LanguageSpecificHacks.AddOptionsExplicit;
            if (bool.Parse(model.AddOptionsStrict)) hacks |= LanguageSpecificHacks.AddOptionsStrict;
            if (bool.Parse(model.DerivedGenericMembersRequireConstraits)) hacks |= LanguageSpecificHacks.DerivedGenericMembersRequireConstraits;
            if (bool.Parse(model.GenerateCsAsStatement)) hacks |= LanguageSpecificHacks.GenerateCsAsStatement;
            if (bool.Parse(model.GenerateCsIsStatement)) hacks |= LanguageSpecificHacks.GenerateCsIsStatement;
            if (bool.Parse(model.GenerateCsLockStatement)) hacks |= LanguageSpecificHacks.GenerateCsLockStatement;
            if (bool.Parse(model.GenerateCSUsingStatement)) hacks |= LanguageSpecificHacks.GenerateCSUsingStatement;
            if (bool.Parse(model.GenerateVbSyncLockStatement)) hacks |= LanguageSpecificHacks.GenerateVbSyncLockStatement;
            if (bool.Parse(model.GenerateVbTryCastStatement)) hacks |= LanguageSpecificHacks.GenerateVbTryCastStatement;
            if (bool.Parse(model.GenerateVbTypeOfIsStatement)) hacks |= LanguageSpecificHacks.GenerateVbTypeOfIsStatement;
            if (bool.Parse(model.GenerateVBUsingStatement)) hacks |= LanguageSpecificHacks.GenerateVBUsingStatement;
            if (bool.Parse(model.MethodsInsteadParametrizedProperties)) hacks |= LanguageSpecificHacks.MethodsInsteadParametrizedProperties;
            if (bool.Parse(model.OptionsExplicitOn)) hacks |= LanguageSpecificHacks.OptionsExplicitOn;
            if (bool.Parse(model.OptionsStrictOn)) hacks |= LanguageSpecificHacks.OptionsStrictOn;
            if (bool.Parse(model.SafeUnboxToEnum)) hacks |= LanguageSpecificHacks.SafeUnboxToEnum;
            settings.LanguageSpecificHacks = hacks;

            _propertyBag["OrmCodeDomGeneratorSettings"] = settings;
        }

        private static TypeDescription SetEntityType(WormModel model, EntityDescription entity, string name, OrmObjectsDef ormObjectsDef)
        {
            WormType wormType = model.Types.Find(t => t.Name == name);
            string propertyTypeName = wormType.IdProperty;
            TypeDescription type = null;
            if (ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName) == null)
            {
                type = new TypeDescription(wormType.IdProperty, entity);
                ormObjectsDef.Types.Add(type);
            }
            else
            {
                type = ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName);
            }
            return type;
        }

        private static TypeDescription GetType(WormModel model, string name, OrmObjectsDef ormObjectsDef, bool nullable)
        {
            List<string> clrTypes = new List<string>();
            clrTypes.Add("System.Byte[]");
            clrTypes.Add("System.String");
            clrTypes.Add("System.Int32");
            clrTypes.Add("System.Int16");
            clrTypes.Add("System.Int64");
            clrTypes.Add("System.Byte");
            clrTypes.Add("System.DateTime");
            clrTypes.Add("System.Decimal");
            clrTypes.Add("System.Double");
            clrTypes.Add("System.Boolean");
            clrTypes.Add("System.Xml.XmlDocument");
            clrTypes.Add("System.Guid");
            clrTypes.Add("System.Single");

            WormType wormType = model.Types.Find(t => t.Name == name);
            string propertyTypeName = wormType.IdProperty;
            TypeDescription type = null;
            if (ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName) == null)
            {
                if (clrTypes.Contains(name))
                {
                    type = new TypeDescription(wormType.IdProperty, wormType.Name);
                }
                else
                {
                    type = new TypeDescription(wormType.IdProperty, wormType.Name,
                        nullable ? UserTypeHintFlags.Nullable : UserTypeHintFlags.None);
                }
                ormObjectsDef.Types.Add(type);
            }
            else
            {
                type = ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName);
            }
            return type;
        }

        private static TypeDescription GetType(WormModel model, string name, OrmObjectsDef ormObjectsDef)
        {
            WormType wormType = model.Types.Find(t => t.Name == name);
            string propertyTypeName = wormType.IdProperty;
            TypeDescription type = null;
            if (ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName) != null)
            {
                type = ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName);
            }
            return type;
        }


        private static string GetTableName(string schema, string table)
        {
            return "[" + schema + "].[" + table + "]";
        }

        
        //private static Type GetTypeByName(string type)
        //{
        //    foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        //    {
        //        Type tp = a.GetType(type, false, true);
        //        if (tp != null)
        //            return tp;
        //    }
        //    throw new TypeLoadException("Cannot load type " + type);
        //}

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected static string GenerateCode(string inputFileContent, string ext)
        {

         
            //Validate the XML file against the schema
            OrmObjectsDef ormObjectsDef;
            using (StringReader contentReader = new StringReader(inputFileContent))
            {
                //try
                //{
                using (XmlReader rdr = XmlReader.Create(contentReader))
                {
                    ormObjectsDef = OrmObjectsDef.LoadFromXml(rdr, new XmlUrlResolver());
                }
                //}
                //catch (Exception ex)
                //{
                //    return null;
                //}

            }

            CodeDomProvider codeDomProvider = null;
            IVSMDCodeDomProvider provider = ((IServiceProvider)_propertyBag["Generic.Host"])
                .GetService(typeof(SVSMDCodeDomProvider)) as IVSMDCodeDomProvider;
            //Query for IVSMDCodeDomProvider/SVSMDCodeDomProvider for this project type
           
            if (provider != null)
            {
                codeDomProvider = provider.CodeDomProvider as CodeDomProvider;
            }
            else
            {
                //In the case where no language specific CodeDom is available, fall back to C#
                string lang = (string)(_propertyBag["Generic.Ext"]) == ".cs" ? "C#" : "VB";
                codeDomProvider = CodeDomProvider.CreateProvider(lang);
            }

            try
            {

                OrmCodeDomGenerator generator = new OrmCodeDomGenerator(ormObjectsDef);
                              

                OrmCodeDomGeneratorSettings settings = null;
                if (_propertyBag["OrmCodeDomGeneratorSettings"] != null)
                {
                    settings = (OrmCodeDomGeneratorSettings)(XmlHelper.PropertyBag["OrmCodeDomGeneratorSettings"]);
                }
                else
                {
                    settings = new OrmCodeDomGeneratorSettings();
                }

                CodeCompileUnit compileUnit = generator.GetFullSingleUnit(settings);

                using (StringWriter writer = new StringWriter(new StringBuilder()))
                {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    codeDomProvider.GenerateCodeFromCompileUnit(compileUnit, writer, options);


                    writer.Flush();
                    return writer.ToString();
                    //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
                    //which may not work with all languages
                    //Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

                    ////Get the preamble (byte-order mark) for our encoding
                    //byte[] preamble = enc.GetPreamble();
                    //int preambleLength = preamble.Length;

                    //Convert the writer contents to a byte array
                    //byte[] body = enc.GetBytes(writer.ToString());

                    //Prepend the preamble to body (store result in resized preamble array)
                    //Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                    //Array.Copy(body, 0, preamble, preambleLength, body.Length);

                    ////Return the combined byte array
                    //return preamble;
                }
            }
            catch (Exception e)
            {
                // this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }

        private static string GetTableName(string fullName)
        {
            string table = string.Empty;
            string[] tableName = fullName.Split(new char[] { '.' });
            if (tableName == null || tableName.Length <= 1)
            {
                table = fullName;
            }
            else
            {
                table = tableName[1].Trim(new char[] { '[', ']' });
            }
            return table;
        }

        private static string GetTableSchema(string fullName)
        {
            string schema = string.Empty;
            string[] tableName = fullName.Split(new char[] { '.' });
            if (tableName != null && tableName.Length > 1)
            {
                schema = tableName[0].Trim(new char[] { '[', ']' });
            }
            return schema;
        }

        public static void Import(WormModel model, OrmObjectsDef ormObjectsDef)
        {
            using (Transaction txAdd = model.Store.TransactionManager.BeginTransaction("Import"))
            {
                model.Entities.Clear();
                model.Tables.Clear();
                model.Types.Clear();

                model.SchemaVersion = ormObjectsDef.SchemaVersion;
                model.DefaultNamespace = ormObjectsDef.Namespace;

                foreach (TableDescription tableDescription in ormObjectsDef.Tables)
                {
                    Table table = new Table(model.Store);
                    table.Schema = GetTableSchema(tableDescription.Name);
                    table.Name = GetTableName(tableDescription.Name);
                    model.Tables.Add(table);
                }
                foreach (TypeDescription typeDescription in ormObjectsDef.Types)
                {
                    WormType type = new WormType(model.Store);
                    if (typeDescription.IsEntityType)
                    {
                        type.Name = typeDescription.Entity.Name;
                    }
                    else if (typeDescription.IsClrType)
                    {
                        type.Name = typeDescription.ClrTypeName;
                    }
                    else if (typeDescription.IsUserType)
                    {
                        type.Name = typeDescription.TypeName;
                    }
                    model.Types.Add(type);
                }
                txAdd.Commit();
            }

            foreach (EntityDescription entityDescription in ormObjectsDef.Entities)
            {
                Entity entity = null;
                using (Transaction txAdd = model.Store.TransactionManager.BeginTransaction("Import"))
                {
                    entity = new Entity(model.Store);
                    entity.BaseEntity = entityDescription.BaseEntity == null ? string.Empty : entityDescription.BaseEntity.Name;
                    entity.Behaviour = entityDescription.Behaviour;
                    entity.Description = entityDescription.Description;
                    entity.InheritsBase = entityDescription.InheritsBaseTables.ToString();
                    entity.MakeInterface = entityDescription.MakeInterface.ToString();
                    entity.Name = entityDescription.Name;
                    entity.Namespace = entityDescription.Namespace;
                    entity.UseGenerics = entityDescription.UseGenerics.ToString();
                    model.Entities.Add(entity);

                    foreach (TableDescription entityTable in entityDescription.Tables)
                    {
                        Table table = model.Tables.Find(t => t.Name == GetTableName(entityTable.Name));
                        table.Entities.Add(entity);
                    }
                    txAdd.Commit();
                }

                foreach (PropertyDescription propertyDescription in entityDescription.Properties)
                {
                    using (Transaction txAdd = model.Store.TransactionManager.BeginTransaction("Import"))
                    {
                        Property property = new Property(model.Store);
                        property.Entity = entity;
                        property.AccessLevel = propertyDescription.FieldAccessLevel;
                        property.Alias = propertyDescription.PropertyAlias;
                        property.Description = propertyDescription.Description;
                        property.Disabled = propertyDescription.Disabled.ToString();
                        property.EnablePropertyChanged = propertyDescription.EnablePropertyChanged.ToString();                        
                        property.FieldName = propertyDescription.FieldName;
                        property.Name = propertyDescription.Name;
                        property.Nullable = propertyDescription.PropertyType.IsNullableType.ToString();
                        property.Obsolete = propertyDescription.Obsolete;
                        property.ObsoleteDescription = propertyDescription.ObsoleteDescripton;
                        property.Supressed = propertyDescription.IsSuppressed.ToString();

                        property.Table = GetTableName(propertyDescription.Table.Name);
                        property.Type = propertyDescription.PropertyType.IsUserType ?
                            propertyDescription.PropertyType.TypeName :
                            propertyDescription.PropertyType.IsEntityType ?
                            propertyDescription.PropertyType.Entity.Name
                            : propertyDescription.PropertyType.ClrTypeName;

                        ArrayList attributes = new ArrayList(propertyDescription.Attributes);
                        property.Factory = attributes.Contains("Factory").ToString();
                        property.InsertDefault = attributes.Contains("InsertDefault").ToString();
                        property.PK = attributes.Contains("PK").ToString();
                        property.PrimaryKey = attributes.Contains("PrimaryKey").ToString();
                        property.Private = attributes.Contains("Private").ToString();
                        property.ReadOnly = attributes.Contains("ReadOnly").ToString();
                        property.RowVersion = attributes.Contains("RowVersion").ToString();
                        property.RV = attributes.Contains("RV").ToString();
                        property.SyncInsert = attributes.Contains("SyncInsert").ToString();
                        property.SyncUpdate = attributes.Contains("SyncUpdate").ToString();

                        txAdd.Commit();
                    }
                }

                foreach (SelfRelationDescription selfRelationDescription in entityDescription.GetSelfRelations(true))
                {
                    using (Transaction txAdd = model.Store.TransactionManager.BeginTransaction("Import"))
                    {
                        SelfRelation selfRelation = new SelfRelation(model.Store);
                        selfRelation.Entity = entity;
                        selfRelation.Disabled = selfRelationDescription.Disabled.ToString();                        
                        selfRelation.Table = GetTableName(selfRelationDescription.Table.Name);
                        selfRelation.UnderlyingEntity = selfRelationDescription.UnderlyingEntity == null? string.Empty:
                            selfRelationDescription.UnderlyingEntity.Name;
                        selfRelation.DirectAccessedEntityType = selfRelationDescription.Direct.AccessedEntityType == null
                            ? string.Empty : selfRelationDescription.Direct.AccessedEntityType.Entity.Name;
                        selfRelation.DirectAccessor = selfRelationDescription.Direct.AccessorName;
                        selfRelation.DirectCascadeDelete = selfRelationDescription.Direct.CascadeDelete.ToString();
                        selfRelation.DirectFieldName = selfRelationDescription.Direct.FieldName;
                        selfRelation.ReverseAccessedEntityType = selfRelationDescription.Reverse.AccessedEntityType== null
                            ? string.Empty : selfRelationDescription.Reverse.AccessedEntityType.Entity.Name;
                        selfRelation.ReverseAccessor = selfRelationDescription.Reverse.AccessorName;
                        selfRelation.ReverseCascadeDelete = selfRelationDescription.Reverse.CascadeDelete.ToString();
                        selfRelation.ReverseFieldName = selfRelationDescription.Reverse.FieldName;
                        txAdd.Commit();
                    }
                }


            }

            foreach (RelationDescriptionBase relationBase in ormObjectsDef.Relations)
            {
                using (Transaction txAdd = model.Store.TransactionManager.BeginTransaction("Import"))
                {
                    if (relationBase is SelfRelationDescription)
                    {
                        continue;
                    }
                    RelationDescription relationDescription = relationBase as RelationDescription;
                    EntityReferencesTargetEntities relation = new EntityReferencesTargetEntities(
                        model.Entities.Find(e => e.Name == relationDescription.Left.Entity.Name),
                        model.Entities.Find(e => e.Name == relationDescription.Right.Entity.Name));
                    relation.Disabled = relationDescription.Disabled.ToString();
                    relation.Table = GetTableName(relationDescription.Table.Name);
                    relation.LeftEntity = relationDescription.Left.Entity.Name;
                    relation.RightEntity = relationDescription.Right.Entity.Name;
                    relation.UnderlyingEntity = relationDescription.UnderlyingEntity == null ?
                        string.Empty : relationDescription.UnderlyingEntity.Name;
                    relation.LeftAccessedEntityType = relationDescription.Left.AccessedEntityType == null
                                ? string.Empty : relationDescription.Left.AccessedEntityType.Entity.Name;
                    relation.LeftAccessorName = relationDescription.Left.AccessorName;
                    relation.LeftCascadeDelete = relationDescription.Left.CascadeDelete.ToString();
                    relation.LeftFieldName = relationDescription.Left.FieldName;
                    relation.RightAccessedEntityType = relationDescription.Right.AccessedEntityType == null
                                ? string.Empty : relationDescription.Right.AccessedEntityType.Entity.Name;
                    relation.RightAccessorName = relationDescription.Right.AccessorName;
                    relation.RightCascadeDelete = relationDescription.Right.CascadeDelete.ToString();
                    relation.RightFieldName = relationDescription.Right.FieldName;
                    txAdd.Commit();
                }

            }
           


        }
       
    }
}
