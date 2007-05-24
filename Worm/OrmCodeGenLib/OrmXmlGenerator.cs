using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using OrmCodeGenLib.Descriptors;
using System.Reflection;

namespace OrmCodeGenLib
{
    internal class OrmXmlGenerator
    {
        private XmlDocument _ormXmlDocument;
        private OrmObjectsDef _ormObjectsDef;

        private XmlNamespaceManager _nsMgr;
        private XmlNameTable _nametable;

        internal OrmXmlGenerator(OrmObjectsDef ormObjectsDef)
        {
            _ormObjectsDef = ormObjectsDef;
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(OrmObjectsDef.NS_PREFIX, OrmObjectsDef.NS_URI);

            
        }

        internal static System.Xml.XmlDocument Generate(OrmObjectsDef schema)
        {
            OrmXmlGenerator generator;
            generator = new OrmXmlGenerator(schema);

            generator.CreateXmlDocument();

            generator.FillImports();

            generator.FillFileDescriptions();

            generator.FillTables();

            generator.FillTypes();

            generator.FillEntities();

            generator.FillRelations();

            return generator._ormXmlDocument;
        }

        private void FillImports()
        {
            if(_ormObjectsDef.Imports.Count == 0)
                return;
            XmlNode importsNode = CreateElement("Imports");
            _ormXmlDocument.DocumentElement.AppendChild(importsNode);
            foreach (KeyValuePair<string, ImportDescription> pair in _ormObjectsDef.Imports)
            {
                XmlElement importElement = CreateElement("Import");
                importElement.SetAttribute("name", pair.Value.Name);
                importElement.SetAttribute("prefix", pair.Key);
                importElement.SetAttribute("uri", pair.Value.FileUri == null ? string.Empty : pair.Value.FileUri );
                importsNode.AppendChild(importElement);
            }
        }

        private void CreateXmlDocument()
        {
            _ormXmlDocument = new XmlDocument(_nametable);
            XmlDeclaration declaration = _ormXmlDocument.CreateXmlDeclaration("1.0", Encoding.UTF8.WebName, null);
            _ormXmlDocument.AppendChild(declaration);
            XmlElement root = CreateElement("OrmObjects");
            _ormXmlDocument.AppendChild(root);
            
          
        }

        private void FillRelations()
        {
            if (_ormObjectsDef.Relations.Count == 0)
                return;
            XmlNode relationsNode = CreateElement("EntityRelations");
            _ormXmlDocument.DocumentElement.AppendChild(relationsNode);
            foreach (RelationDescription relation in _ormObjectsDef.Relations)
            {
                XmlElement relationElement = CreateElement("Relation");

                relationElement.SetAttribute("table", relation.Table.Identifier);

                XmlElement leftElement = CreateElement("Left");
                leftElement.SetAttribute("entity", relation.Left.Entity.Identifier);
                leftElement.SetAttribute("fieldName", relation.Left.FieldName);
                leftElement.SetAttribute("cascadeDelete", relation.Left.CascadeDelete.ToString().ToLower());              

                XmlElement rightElement = CreateElement("Right");
                rightElement.SetAttribute("entity", relation.Right.Entity.Identifier);
                rightElement.SetAttribute("fieldName", relation.Right.FieldName);
                rightElement.SetAttribute("cascadeDelete", relation.Right.CascadeDelete.ToString().ToLower());

                if(relation.UnderlyingEntity != null)
                {
                    relationElement.SetAttribute("underlyingEntity", relation.UnderlyingEntity.Identifier);
                }
                relationElement.AppendChild(leftElement);
                relationElement.AppendChild(rightElement);
                relationsNode.AppendChild(relationElement);
            }
        }

        private void FillEntities()
        {
            XmlNode entitiesNode = CreateElement("Entities");
            _ormXmlDocument.DocumentElement.AppendChild(entitiesNode);

            foreach (EntityDescription entity in _ormObjectsDef.Entities)
            {
                XmlElement entityElement = CreateElement("Entity");

                entityElement.SetAttribute("id", entity.Identifier);
                entityElement.SetAttribute("name", entity.Name);
                if (!string.IsNullOrEmpty(entity.Description))
                    entityElement.SetAttribute("description", entity.Description);
                if (!string.IsNullOrEmpty(entity.Namespace))
                    entityElement.SetAttribute("namespace", entity.Namespace);

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
                    propertyElement.SetAttribute("id", property.Identifier);
                    propertyElement.SetAttribute("propertyName", property.Name);
                    if(property.Attributes != null && property.Attributes.Length > 0)
                    {
                        propertyElement.SetAttribute("attributes", string.Join(", ", property.Attributes));
                    }
                    propertyElement.SetAttribute("table", property.Table.Identifier);
                    propertyElement.SetAttribute("fieldName", property.FieldName);
                    propertyElement.SetAttribute("typeRef", property.PropertyType.Identifier);
                    if (!string.IsNullOrEmpty(property.Description))
                        propertyElement.SetAttribute("description", property.Description);

                    propertiesNode.AppendChild(propertyElement);
                }
                entityElement.AppendChild(propertiesNode);

                entitiesNode.AppendChild(entityElement);
            }
        }

        private void FillTypes()
        {
            XmlNode typesNode = CreateElement("Types");
            _ormXmlDocument.DocumentElement.AppendChild(typesNode);
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
            _ormXmlDocument.DocumentElement.AppendChild(tablesNode);
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
            return _ormXmlDocument.CreateElement(OrmObjectsDef.NS_PREFIX, name, OrmObjectsDef.NS_URI);
        }

        private void FillFileDescriptions()
        {
			if (!string.IsNullOrEmpty(_ormObjectsDef.Namespace))
				_ormXmlDocument.DocumentElement.SetAttribute("namespace", _ormObjectsDef.Namespace);

            _ormXmlDocument.DocumentElement.SetAttribute("schemaVersion", _ormObjectsDef.SchemaVersion);

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
                _ormXmlDocument.CreateComment(commentBuilder.ToString());
            _ormXmlDocument.InsertBefore(commentsElement, _ormXmlDocument.DocumentElement);
        }
    }
}
