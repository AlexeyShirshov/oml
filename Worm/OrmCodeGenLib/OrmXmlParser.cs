using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib
{
    internal class OrmXmlParser
    {
        private const string SCHEMA_NAME = "OrmObjectsSchema";

        private readonly List<string> _validationResult;
        private readonly XmlReader _reader;
        private XmlDocument _ormXmlDocument;
        private OrmObjectsDef _ormObjectsDef;

        private XmlNamespaceManager _nsMgr;
        private XmlNameTable _nametable;

        internal protected OrmXmlParser(XmlReader reader)
        {
            _validationResult = new List<string>();
            _reader = reader;
            _ormObjectsDef = new OrmObjectsDef();
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(OrmObjectsDef.NS_PREFIX, OrmObjectsDef.NS_URI);
        }

        internal protected static OrmObjectsDef Parse(XmlReader reader)
        {
            OrmXmlParser parser;
            parser = new OrmXmlParser(reader);

            parser.Read();

            parser.FillFileDescriptions();

            parser.FillImports();

            parser.FillTables();

            parser.FindEntities();

            parser.FillTypes();

            parser.FillEntities();

            parser.FillRelations();

            return parser._ormObjectsDef;
        }

        private void FillImports()
        {
            XmlNodeList importNodes;
            importNodes =
                _ormXmlDocument.DocumentElement.SelectNodes(
                    string.Format("{0}:Imports/{0}:Import", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode importNode in importNodes)
            {
                ImportDescription import;
                string name, prefix, uri;
                name = (importNode as XmlElement).GetAttribute("name");
                prefix = (importNode as XmlElement).GetAttribute("prefix");
                uri = (importNode as XmlElement).GetAttribute("uri");
                OrmObjectsDef content = null;
                using (XmlReader rdr = XmlReader.Create(uri))
                {
                    content = OrmXmlParser.Parse(rdr);
                }
                content.NS = prefix;
                if (content == null)
                    throw new OrmXmlParserException(string.Format("Import file '{0}' not found.", uri));
                import = new ImportDescription(name, content, uri);
                OrmObjectsDef.Imports.Add(prefix, import);
            }
        }

        internal protected void FillTypes()
        {
            XmlNodeList typeNodes;
            typeNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Types/{0}:Type", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode typeNode in typeNodes)
            {
                TypeDescription type;
                string id;
                id = (typeNode as XmlElement).GetAttribute("id");
                
                XmlNode typeDefNode = typeNode.LastChild;
                if(typeDefNode.LocalName.Equals("Entity"))
                {
                    string entityId = (typeDefNode as XmlElement).GetAttribute("ref");
                    EntityDescription entity = _ormObjectsDef.GetEntity(entityId);
                    if (entity == null)
                        throw new KeyNotFoundException(
                            string.Format("Underlying entity '{1}' in type '{0}' not found.", id, entityId));
                    type = new TypeDescription(id, entity);
                }
                else
                {
                    string name = (typeDefNode as XmlElement).GetAttribute("name");
                    type = new TypeDescription(id, name, typeDefNode.LocalName.Equals("UserType"));
                }
                _ormObjectsDef.Types.Add(type);
            }
        }

        internal protected void FillEntities()
        {
            

            foreach (EntityDescription entity in _ormObjectsDef.Entities)
            {
                XmlNode entityNode =
                    _ormXmlDocument.DocumentElement.SelectSingleNode(
                        string.Format("{0}:Entities/{0}:Entity[@id='{1}']", OrmCodeGenLib.OrmObjectsDef.NS_PREFIX,
                                      entity.Identifier), _nsMgr);

                string baseEntityId = (entityNode as XmlElement).GetAttribute("baseEntity");

                if (!string.IsNullOrEmpty(baseEntityId))
                {
                    EntityDescription baseEntity = OrmObjectsDef.GetEntity(baseEntityId);
                    if (baseEntity == null)
                        throw new OrmXmlParserException(
                            string.Format("Base entity '{0}' for entity '{1}' not found.", baseEntityId,
                                          entity.Identifier));
                    entity.BaseEntity = baseEntity;
                }
                FillProperties(entity);
            }
        }

        internal protected void FillFileDescriptions()
        {
            _ormObjectsDef.Namespace = _ormXmlDocument.DocumentElement.GetAttribute("namespace");
            _ormObjectsDef.SchemaVersion = _ormXmlDocument.DocumentElement.GetAttribute("schemaVersion");
        }

        internal protected void FindEntities()
        {
            XmlNodeList entitiesList;
            entitiesList = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Entities/{0}:Entity", OrmObjectsDef.NS_PREFIX), _nsMgr);

            EntityDescription entity;

            _ormObjectsDef.Entities.Clear();

            foreach (XmlNode entityNode in entitiesList)
            {
                string id, name, description, nameSpace, baseEntityId;

                id = (entityNode as XmlElement).GetAttribute("id");
                name = (entityNode as XmlElement).GetAttribute("name");
                description = (entityNode as XmlElement).GetAttribute("description");
                nameSpace = (entityNode as XmlElement).GetAttribute("namespace");

                entity = new EntityDescription(id, name, nameSpace, description, _ormObjectsDef);

                _ormObjectsDef.Entities.Add(entity);

                FillEntityTables(entity);
            }
        }

        internal protected void FillProperties(EntityDescription entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode;
            entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", OrmObjectsDef.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList propertiesList;
            propertiesList = entityNode.SelectNodes(string.Format("{0}:Properties/{0}:Property", OrmObjectsDef.NS_PREFIX), _nsMgr);

            PropertyDescription property;

            foreach (XmlNode propertyNode in propertiesList)
            {
                string id, name, description, typeId, fieldname, sAttributes, tableId;
                string[] attributes;
                TableDescription table;

                id = (propertyNode as XmlElement).GetAttribute("id");
                description = (propertyNode as XmlElement).GetAttribute("description");
                name = (propertyNode as XmlElement).GetAttribute("propertyName");
                fieldname = (propertyNode as XmlElement).GetAttribute("fieldName");
                typeId = (propertyNode as XmlElement).GetAttribute("typeRef");
                sAttributes = (propertyNode as XmlElement).GetAttribute("attributes");
                tableId = (propertyNode as XmlElement).GetAttribute("table");

                attributes = sAttributes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                table = entity.GetTable(tableId);

                if (table == null)
                    throw new OrmXmlParserException(
                        string.Format("Table '{0}' for property '{1}' of entity '{2}' not found.", tableId, id,
                                      entity.Identifier));

                TypeDescription typeDesc = _ormObjectsDef.GetType(typeId, true);
                
                property = new PropertyDescription(id, name, attributes, description, typeDesc, fieldname, table);

                entity.Properties.Add(property);
            }
        }

        internal protected void FillRelations()
        {
            XmlNodeList relationNodes;
            relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:Relation", OrmObjectsDef.NS_PREFIX), _nsMgr);

            RelationDescription relation;

            TableDescription relationTable;
            string relationTableId;
            EntityDescription underlyingEntity;
            string underlyingEntityId;
            EntityDescription leftLinkTargetEntity, rightLinkTargetEntity;
            string leftLinkTargetEntityId, rightLinkTargetEntityId;
            bool leftCascadeDelete, rightCascadeDelete;
            string leftFieldName, rightFieldName;
            LinkTarget leftLinkTarget, rightLinkTarget;

            XmlNode leftTargetNode, rightTargetNode;

            foreach (XmlNode relationNode in relationNodes)
            {
                leftTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Left", OrmObjectsDef.NS_PREFIX), _nsMgr);
                rightTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Right", OrmObjectsDef.NS_PREFIX), _nsMgr);

                relationTableId = (relationNode as XmlElement).GetAttribute("table");
                underlyingEntityId = (relationNode as XmlElement).GetAttribute("underlyingEntity");

                leftLinkTargetEntityId = (leftTargetNode as XmlElement).GetAttribute("entity");
                rightLinkTargetEntityId = (rightTargetNode as XmlElement).GetAttribute("entity");

                leftFieldName = (leftTargetNode as XmlElement).GetAttribute("fieldName");
                rightFieldName = (rightTargetNode as XmlElement).GetAttribute("fieldName");

                leftCascadeDelete = XmlConvert.ToBoolean((leftTargetNode as XmlElement).GetAttribute("cascadeDelete"));
                rightCascadeDelete = XmlConvert.ToBoolean((rightTargetNode as XmlElement).GetAttribute("cascadeDelete"));

                relationTable = _ormObjectsDef.GetTable(relationTableId);

                if (string.IsNullOrEmpty(underlyingEntityId))
                    underlyingEntity = null;
                else
                    underlyingEntity = _ormObjectsDef.GetEntity(underlyingEntityId);



                leftLinkTargetEntity = _ormObjectsDef.GetEntity(leftLinkTargetEntityId);

                rightLinkTargetEntity = _ormObjectsDef.GetEntity(rightLinkTargetEntityId);

                leftLinkTarget = new LinkTarget(leftLinkTargetEntity, leftFieldName, leftCascadeDelete);
                rightLinkTarget = new LinkTarget(rightLinkTargetEntity, rightFieldName, rightCascadeDelete);

                relation = new RelationDescription(leftLinkTarget, rightLinkTarget, relationTable, underlyingEntity);
                _ormObjectsDef.Relations.Add(relation);
            }
        }

        internal protected void FillTables()
        {
            XmlNodeList tableNodes;
            tableNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Tables/{0}:Table", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode tableNode in tableNodes)
            {
                string id;
                string name;

                id = (tableNode as XmlElement).GetAttribute("id");
                name = tableNode.InnerText;
                _ormObjectsDef.Tables.Add(new TableDescription(id, name));
            }
        }

        internal protected void FillEntityTables(EntityDescription entity)
        {
            XmlNodeList tableNodes;
            XmlNode entityNode;

            if (entity == null)
                throw new ArgumentNullException("entity");

            entity.Tables.Clear();

            entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", OrmObjectsDef.NS_PREFIX, entity.Identifier), _nsMgr);

            tableNodes = entityNode.SelectNodes(string.Format("{0}:Tables/{0}:Table", OrmObjectsDef.NS_PREFIX), _nsMgr);

            TableDescription table;

            foreach (XmlNode tableNode in tableNodes)
            {
                string tableId;
                tableId = (tableNode as XmlElement).GetAttribute("ref");

                table = entity.OrmObjectsDef.GetTable(tableId);

                entity.Tables.Add(table);
            }
        }

        internal protected void Read()
        {
            XmlSchemaSet schemaSet;
            XmlSchema schema;
            XmlReaderSettings xmlReaderSettings;

            schemaSet = new XmlSchemaSet(_nametable);

            schema = ResourceManager.GetXmlSchema(SCHEMA_NAME);
            schemaSet.Add(schema);

            xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.CloseInput = false;
            xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            xmlReaderSettings.IgnoreComments = true;
            xmlReaderSettings.IgnoreWhitespace = true;
            xmlReaderSettings.Schemas = schemaSet;
            xmlReaderSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += new ValidationEventHandler(xmlReaderSettings_ValidationEventHandler);

            XmlDocument xml;
            xml = new XmlDocument(_nametable);

            _validationResult.Clear();

            using (XmlReader rdr = XmlReader.Create(_reader, xmlReaderSettings))
            {
                xml.Load(rdr);
            }

            if (_validationResult.Count == 0)
                _ormXmlDocument = xml;
        }

        void xmlReaderSettings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if(e.Severity == XmlSeverityType.Warning)
                return;
            throw new OrmXmlParserException(string.Format("Xml document format error{1}: {0}", e.Message, (e.Exception as XmlSchemaException) != null ? string.Format("({0},{1})", (e.Exception as XmlSchemaException).LineNumber, (e.Exception as XmlSchemaException).LinePosition) : string.Empty));
        }

        internal protected XmlDocument SourceXmlDocument
        {
            get
            {
                return _ormXmlDocument;
            }
            set
            {
                _ormXmlDocument = value;
            }
        }

        internal protected OrmObjectsDef OrmObjectsDef
        {
            get { return _ormObjectsDef; }
        }
    }
}
