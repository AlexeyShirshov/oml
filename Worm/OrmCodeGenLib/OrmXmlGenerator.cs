using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib
{
    internal class OrmXmlGenerator
    {
        private OrmXmlDocumentSet _ormXmlDocumentSet;
        private XmlDocument _ormXmlDocumentMain;
        private OrmObjectsDef _ormObjectsDef;

        private XmlNamespaceManager _nsMgr;
        private XmlNameTable _nametable;

        private OrmXmlGeneratorSettings _settings;

        internal OrmXmlGenerator(OrmObjectsDef ormObjectsDef, OrmXmlGeneratorSettings settings)
        {
            _ormObjectsDef = ormObjectsDef;
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(OrmObjectsDef.NS_PREFIX, OrmObjectsDef.NS_URI);
            _ormXmlDocumentSet = new OrmXmlDocumentSet();
            _settings = settings;
        }

        internal static OrmXmlDocumentSet Generate(OrmObjectsDef schema, OrmXmlGeneratorSettings settings)
        {
            OrmXmlGenerator generator;
            generator = new OrmXmlGenerator(schema, settings);

            generator.GenerateXmlDocumentInternal();

            return generator._ormXmlDocumentSet;
        }

        private void GenerateXmlDocumentInternal()
        {
            CreateXmlDocument();

            FillFileDescriptions();

            FillImports();           

            FillTables();

            FillTypes();

            FillEntities();

            FillRelations();
        }

        private void FillImports()
        {
            if(_ormObjectsDef.Includes.Count == 0)
                return;
            XmlNode importsNode = CreateElement("Includes");
            _ormXmlDocumentMain.DocumentElement.AppendChild(importsNode);
            foreach (OrmObjectsDef objectsDef in _ormObjectsDef.Includes)
            {
                OrmXmlGeneratorSettings settings = (OrmXmlGeneratorSettings)_settings.Clone();
                    //settings.DefaultMainFileName = _settings.DefaultIncludeFileName + _ormObjectsDef.Includes.IndexOf(objectsDef);
                    OrmXmlDocumentSet set;
                    set = Generate(objectsDef, _settings);
                    _ormXmlDocumentSet.AddRange(set);
                    foreach (OrmXmlDocument ormXmlDocument in set)
                    {
                        if ((_settings.IncludeBehaviour & IncludeGenerationBehaviour.Inline) ==
                            IncludeGenerationBehaviour.Inline)
                        {
                            XmlNode importedSchemaNode =
                                _ormXmlDocumentMain.ImportNode(ormXmlDocument.Document.DocumentElement, true);
                            importsNode.AppendChild(importedSchemaNode);
                        }
                        else
                        {
                            XmlElement includeElement =
                                _ormXmlDocumentMain.CreateElement("xi", "include", "http://www.w3.org/2001/XInclude");
                            includeElement.SetAttribute("parse", "xml");

                            string fileName = GetIncludeFileName(_ormObjectsDef, objectsDef, settings);

                            includeElement.SetAttribute("href", fileName);
                            importsNode.AppendChild(includeElement);
                        }

                    }
            }
        }

        private void CreateXmlDocument()
        {
            _ormXmlDocumentMain = new XmlDocument(_nametable);
            XmlDeclaration declaration = _ormXmlDocumentMain.CreateXmlDeclaration("1.0", Encoding.UTF8.WebName, null);
            _ormXmlDocumentMain.AppendChild(declaration);
            XmlElement root = CreateElement("OrmObjects");
            _ormXmlDocumentMain.AppendChild(root);
            string filename = GetFilename(_ormObjectsDef, _settings);
            OrmXmlDocument doc = new OrmXmlDocument(filename, _ormXmlDocumentMain);
            _ormXmlDocumentSet.Add(doc);
          
        }

        private string GetFilename(OrmObjectsDef objectsDef, OrmXmlGeneratorSettings settings)
        {
            return string.IsNullOrEmpty(objectsDef.FileName)
                       ? settings.DefaultMainFileName
                       : objectsDef.FileName;
        }

        private string GetIncludeFileName(OrmObjectsDef objectsDef, OrmObjectsDef incldeObjectsDef, OrmXmlGeneratorSettings settings)
        {
            if (string.IsNullOrEmpty(incldeObjectsDef.FileName))
            {
                string filename =
                    settings.IncludeFileNamePattern.Replace("%MAIN_FILENAME%", GetFilename(objectsDef, settings)).
                        Replace(
                        "%INCLUDE_NAME%", GetFilename(incldeObjectsDef, settings)) +
                    objectsDef.Includes.IndexOf(incldeObjectsDef);
                return
                    (((settings.IncludeBehaviour & IncludeGenerationBehaviour.PlaceInFolder) ==
                      IncludeGenerationBehaviour.PlaceInFolder)
                         ? settings.IncludeFolderName + System.IO.Path.DirectorySeparatorChar
                         : string.Empty) + filename;
            }
            else
                return incldeObjectsDef.FileName;
        }

        private void FillRelations()
        {
            if (_ormObjectsDef.Relations.Count == 0)
                return;
            XmlNode relationsNode = CreateElement("EntityRelations");
            _ormXmlDocumentMain.DocumentElement.AppendChild(relationsNode);
            foreach (RelationDescription relation in _ormObjectsDef.Relations)
            {
                XmlElement relationElement = CreateElement("Relation");

                relationElement.SetAttribute("table", relation.Table.Identifier);
                if(relation.Disabled)
                {
                    relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
                }

                XmlElement leftElement = CreateElement("Left");
                leftElement.SetAttribute("entity", relation.Left.Entity.Identifier);
                leftElement.SetAttribute("fieldName", relation.Left.FieldName);
                leftElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Left.CascadeDelete));              

                XmlElement rightElement = CreateElement("Right");
                rightElement.SetAttribute("entity", relation.Right.Entity.Identifier);
                rightElement.SetAttribute("fieldName", relation.Right.FieldName);
                rightElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Right.CascadeDelete));

                if(relation.UnderlyingEntity != null)
                {
                    relationElement.SetAttribute("underlyingEntity", relation.UnderlyingEntity.Identifier);
                }
                relationElement.AppendChild(leftElement);
                relationElement.AppendChild(rightElement);
                relationsNode.AppendChild(relationElement);
            }
			foreach (SelfRelationDescription relation in _ormObjectsDef.SelfRelations)
			{
				XmlElement relationElement = CreateElement("SelfRelation");

				relationElement.SetAttribute("table", relation.Table.Identifier);
				relationElement.SetAttribute("entity", relation.Entity.Identifier);
				if (relation.Disabled)
				{
					relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
				}

				XmlElement directElement = CreateElement("Direct");
				
				directElement.SetAttribute("fieldName", relation.Direct.FieldName);
				directElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Direct.CascadeDelete));

				XmlElement reverseElement = CreateElement("Reverse");
				reverseElement.SetAttribute("fieldName", relation.Reverse.FieldName);
				reverseElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Reverse.CascadeDelete));

				if (relation.UnderlyingEntity != null)
				{
					relationElement.SetAttribute("underlyingEntity", relation.UnderlyingEntity.Identifier);
				}
				relationElement.AppendChild(directElement);
				relationElement.AppendChild(reverseElement);
				relationsNode.AppendChild(relationElement);
			}
        }

        private void FillEntities()
        {
            XmlNode entitiesNode = CreateElement("Entities");
            _ormXmlDocumentMain.DocumentElement.AppendChild(entitiesNode);

            foreach (EntityDescription entity in _ormObjectsDef.Entities)
            {
                XmlElement entityElement = CreateElement("Entity");

                entityElement.SetAttribute("id", entity.Identifier);
                entityElement.SetAttribute("name", entity.Name);
                if (!string.IsNullOrEmpty(entity.Description))
                    entityElement.SetAttribute("description", entity.Description);
                if (entity.Namespace != entity.OrmObjectsDef.Namespace)
                    entityElement.SetAttribute("namespace", entity.Namespace);
				if(entity.Behaviour != EntityBehaviuor.Default)
					entityElement.SetAttribute("behaviour", entity.Behaviour.ToString());

                XmlNode tablesNode = CreateElement("Tables");
                foreach (TableDescription table in entity.Tables)
                {
                    XmlElement tableElement = CreateElement("Table");
                    tableElement.SetAttribute("ref", table.Identifier);
                    tablesNode.AppendChild(tableElement);
                }
                entityElement.AppendChild(tablesNode);

                XmlNode propertiesNode = CreateElement("Properties");
                foreach (PropertyDescription property in entity.Properties)
                {
                    XmlElement propertyElement =
                        CreateElement("Property");
                    propertyElement.SetAttribute("propertyName", property.Name);
                    if(property.Attributes != null && property.Attributes.Length > 0)
                    {
                        propertyElement.SetAttribute("attributes", string.Join(" ", property.Attributes));
                    }
                    propertyElement.SetAttribute("table", property.Table.Identifier);
                    propertyElement.SetAttribute("fieldName", property.FieldName);
                    propertyElement.SetAttribute("typeRef", property.PropertyType.Identifier);
                    if (!string.IsNullOrEmpty(property.Description))
                        propertyElement.SetAttribute("description", property.Description);
                    if (property.FieldAccessLevel != AccessLevel.Private)
                        propertyElement.SetAttribute("classfieldAccessLevel", property.FieldAccessLevel.ToString());
                    if (property.PropertyAccessLevel != AccessLevel.Public)
                        propertyElement.SetAttribute("propertyAccessLevel", property.PropertyAccessLevel.ToString());
                    if (property.PropertyAlias != property.Name)
                        propertyElement.SetAttribute("propertyAlias", property.PropertyAlias);
                    if (property.Disabled)
                        propertyElement.SetAttribute("disabled", XmlConvert.ToString(true));
                    propertiesNode.AppendChild(propertyElement);
                }
                entityElement.AppendChild(propertiesNode);

                entitiesNode.AppendChild(entityElement);
            }
        }

        private void FillTypes()
        {
            XmlNode typesNode = CreateElement("Types");
            _ormXmlDocumentMain.DocumentElement.AppendChild(typesNode);
            foreach (TypeDescription type in _ormObjectsDef.Types)
            {
                XmlElement typeElement = CreateElement("Type");

                typeElement.SetAttribute("id", type.Identifier);

                XmlElement typeSubElement;
                if(type.IsClrType)
                {
                    typeSubElement = CreateElement("ClrType");
                    typeSubElement.SetAttribute("name", type.ClrTypeName);
                }
                else if(type.IsUserType)
                {
                    typeSubElement = CreateElement("UserType");
                    typeSubElement.SetAttribute("name", type.TypeName);
                    if(type.UserTypeHint.HasValue && type.UserTypeHint != UserTypeHintFlags.None)
                    {
                        typeSubElement.SetAttribute("hint", type.UserTypeHint.ToString().Replace(",", string.Empty));
                    }
                }
                else
                {
                    typeSubElement = CreateElement("Entity");
                    typeSubElement.SetAttribute("ref", type.Entity.Identifier);
                }
                typeElement.AppendChild(typeSubElement);
                typesNode.AppendChild(typeElement);
            }
        }

        private void FillTables()
        {
            XmlNode tablesNode = CreateElement("Tables");
            _ormXmlDocumentMain.DocumentElement.AppendChild(tablesNode);
            foreach (TableDescription table in _ormObjectsDef.Tables)
            {
                XmlElement tableElement = CreateElement("Table");
                tableElement.InnerText = table.Name;
                tableElement.SetAttribute("id", table.Identifier);
                tablesNode.AppendChild(tableElement);
            }
        }

        private XmlElement CreateElement(string name)
        {
            return _ormXmlDocumentMain.CreateElement(OrmObjectsDef.NS_PREFIX, name, OrmObjectsDef.NS_URI);
        }

        private void FillFileDescriptions()
        {
            _ormXmlDocumentMain.DocumentElement.SetAttribute("defaultNamespace", _ormObjectsDef.Namespace);
            _ormXmlDocumentMain.DocumentElement.SetAttribute("schemaVersion", _ormObjectsDef.SchemaVersion);

            StringBuilder commentBuilder = new StringBuilder();
            foreach (string comment in _ormObjectsDef.SystemComments)
            {
                commentBuilder.AppendLine(comment);
            }

            if(_ormObjectsDef.UserComments.Count > 0)
            {
                commentBuilder.AppendLine();
                foreach (string comment in _ormObjectsDef.UserComments)
                {
                    commentBuilder.AppendLine(comment);
                }
            }

            XmlComment commentsElement =
                _ormXmlDocumentMain.CreateComment(commentBuilder.ToString());
            _ormXmlDocumentMain.InsertBefore(commentsElement, _ormXmlDocumentMain.DocumentElement);
        }
    }

    public class OrmXmlDocument
    {
        private XmlDocument m_document;
        private string m_fileName;

        public OrmXmlDocument(string filename, XmlDocument document)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");
            if (document == null)
                throw new ArgumentNullException("document");
            m_document = document;
            m_fileName = filename;
        }

        public XmlDocument Document
        {
            get { return m_document; }
            set { m_document = value; }
        }

        public string FileName
        {
            get { return m_fileName; }
            set { m_fileName = value; }
        }
    }

    public class OrmXmlDocumentSet : List<OrmXmlDocument>
    {
    }
}
