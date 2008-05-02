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


            OrmObjectsDef ormObjectsDef = new OrmObjectsDef();            
            ormObjectsDef.SchemaVersion = model.SchemaVersion;

            if (string.IsNullOrEmpty(model.DefaultNamespace))
            {
                string defaultNamespace = ((string)_propertyBag["Generic.ModelFile"]).Replace(".wxml", "").Replace(".vb", "");
                ormObjectsDef.Namespace = defaultNamespace;
            }
            else
            {
                ormObjectsDef.Namespace = model.DefaultNamespace;
            }

            foreach (Entity entity in model.Entities)
            {
                EntityDescription entityDescription = new EntityDescription(
                    entity.IdProperty, entity.Name, entity.Namespace, entity.Description, ormObjectsDef,
                   null, entity.Behaviour);
                entityDescription.MakeInterface = bool.Parse(entity.MakeInterface);
                entityDescription.UseGenerics = bool.Parse(entity.UseGenerics);
                
                SetEntityType(model, entityDescription, entity.Name, ormObjectsDef);
                

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
                    TypeDescription type = GetType(model, property.Type, ormObjectsDef);
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
           
            ReadOnlyCollection<Microsoft.VisualStudio.Modeling.ModelElement> relations = (
            (model.Store.ElementDirectory.FindElements(Worm.Designer.EntityReferencesTargetEntities.DomainClassId)));
            foreach (Worm.Designer.EntityReferencesTargetEntities relation in relations)
            {
                if (relation.TargetEntity != relation.SourceEntity)
                {
                    EntityDescription leftEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.SourceEntity.IdProperty);
                    EntityDescription rightEntity = ormObjectsDef.Entities.Find(e => e.Identifier == relation.TargetEntity.IdProperty);
                    RelationDescription self = new RelationDescription(
                        new LinkTarget(leftEntity, relation.LeftFieldName, bool.Parse(relation.LeftCascadeDelete)),
                        new LinkTarget(rightEntity, relation.RightFieldName, bool.Parse(relation.RightCascadeDelete)),
                        ormObjectsDef.Tables.Find(t => t.Name == GetTableName(relation.TargetEntity.Tables[0].Schema, relation.Table)),
                        ormObjectsDef.Entities.Find(e => e.Identifier == relation.UnderlyingEntity),
                        bool.Parse(relation.Disabled));
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

        private static TypeDescription GetType(WormModel model, string name, OrmObjectsDef ormObjectsDef)
        {
             WormType wormType = model.Types.Find(t => t.Name == name);
            string propertyTypeName = wormType.IdProperty;
            TypeDescription type = null;
            if (ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName) == null)
            {
                type = new TypeDescription(wormType.IdProperty, wormType.Name);
                ormObjectsDef.Types.Add(type);
            }
            else
            {
                type = ormObjectsDef.Types.Find(t => t.Identifier == propertyTypeName);
            }
            return type;
        }

        private static string GetTableName(string schema, string table)
        {
            return "[" + schema + "].[" + table + "]";
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

       
       
    }
}
