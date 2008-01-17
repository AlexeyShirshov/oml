using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly OrmObjectsDef _ormObjectsDef;

        private readonly XmlNamespaceManager _nsMgr;
        private readonly XmlNameTable _nametable;

        private readonly XmlResolver _xmlResolver;

        internal protected OrmXmlParser(XmlReader reader) : this(reader, null)
        {
            
        }

        internal protected OrmXmlParser(XmlReader reader, XmlResolver xmlResolver)
        {
            _validationResult = new List<string>();
            _reader = reader;
            _ormObjectsDef = new OrmObjectsDef();
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(OrmObjectsDef.NS_PREFIX, OrmObjectsDef.NS_URI);
            _xmlResolver = xmlResolver;
        }

        internal protected OrmXmlParser(XmlDocument document)
        {
            _ormObjectsDef = new OrmObjectsDef();
            _ormXmlDocument = document;
            _nametable = document.NameTable;
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(OrmObjectsDef.NS_PREFIX, OrmObjectsDef.NS_URI);            
        }

        internal protected static OrmObjectsDef Parse(XmlReader reader, XmlResolver xmlResolver)
        {
            OrmXmlParser parser;
            parser = new OrmXmlParser(reader, xmlResolver);

            parser.Read();

            parser.FillObjectsDef();

            return parser._ormObjectsDef;
        }

        internal protected static OrmObjectsDef LoadXmlDocument(XmlDocument document, bool skipValidation)
        {
            OrmXmlParser parser;
            if (skipValidation)
                parser = new OrmXmlParser(document);
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter xwr = XmlWriter.Create(ms))
                    {
                        document.WriteTo(xwr);
                    }
                    ms.Position = 0;
                    using (XmlReader xrd = XmlReader.Create(ms))
                    {
                        parser = new OrmXmlParser(xrd, null);
                        parser.Read();
                    }
                }
            }
            parser.FillObjectsDef();
            return parser._ormObjectsDef;                
        }

        private void FillObjectsDef()
        {
            FillFileDescriptions();

            FillImports();

            FillTables();

            FindEntities();

            FillTypes();

            FillEntities();

            FillRelations();
        }

        private void FillImports()
        {
            XmlNodeList importNodes;
            importNodes =
                _ormXmlDocument.DocumentElement.SelectNodes(
                    string.Format("{0}:Includes/{0}:OrmObjects", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode importNode in importNodes)
            {
                OrmObjectsDef import;

                XmlDocument tempDoc = new XmlDocument();
                XmlNode importedNode = tempDoc.ImportNode(importNode, true);
                tempDoc.AppendChild(importedNode);
                import = LoadXmlDocument(tempDoc, true);

                OrmObjectsDef.Includes.Add(import);
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
                XmlElement typeElement = (XmlElement)typeNode;
                id = typeElement.GetAttribute("id");
                
                XmlNode typeDefNode = typeNode.LastChild;
                XmlElement typeDefElement = (XmlElement)typeDefNode;
                if(typeDefNode.LocalName.Equals("Entity"))
                {
                    string entityId;
                    entityId = typeDefElement.GetAttribute("ref");
                    EntityDescription entity = _ormObjectsDef.GetEntity(entityId);
                    if (entity == null)
                        throw new KeyNotFoundException(
                            string.Format("Underlying entity '{1}' in type '{0}' not found.", id, entityId));
                    type = new TypeDescription(id, entity);
                }
                else
                {
                    string name = typeDefElement.GetAttribute("name");
                    if (typeDefNode.LocalName.Equals("UserType"))
                    {
                        UserTypeHintFlags? hint = null;
                        XmlAttribute hintAttribute = typeDefNode.Attributes["hint"];
                        if (hintAttribute != null)
                            hint = (UserTypeHintFlags) Enum.Parse(typeof (UserTypeHintFlags), hintAttribute.Value.Replace(" ", ", "));
                        type = new TypeDescription(id, name, hint);
                    }
                    else
                    {
                        type = new TypeDescription(id, name, false);
                    }
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

                XmlElement entityElement = (XmlElement)entityNode;
                string baseEntityId = entityElement.GetAttribute("baseEntity");

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
                FillSuppresedProperties(entity);
            }
        }

        private void FillSuppresedProperties(EntityDescription entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode;
            entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", OrmObjectsDef.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList propertiesList;
            propertiesList = entityNode.SelectNodes(string.Format("{0}:SuppressedProperties/{0}:Property", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode propertyNode in propertiesList)
            {
                string name;
                XmlElement propertyElement = (XmlElement) propertyNode;
                name = propertyElement.GetAttribute("name");

                PropertyDescription property = new PropertyDescription(name);

                entity.SuppressedProperties.Add(property);
            }
        }

        internal protected void FillFileDescriptions()
        {
            _ormObjectsDef.Namespace = _ormXmlDocument.DocumentElement.GetAttribute("defaultNamespace");
            _ormObjectsDef.SchemaVersion = _ormXmlDocument.DocumentElement.GetAttribute("schemaVersion");
            string baseUriString = _ormXmlDocument.DocumentElement.GetAttribute("xml:base");
            if (!string.IsNullOrEmpty(baseUriString))
            {
                Uri baseUri = new Uri(baseUriString, UriKind.RelativeOrAbsolute);
                _ormObjectsDef.FileName = Path.GetFileName(baseUri.ToString());
            }
        }

        internal protected void FindEntities()
        {
            XmlNodeList entitiesList;
            entitiesList = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Entities/{0}:Entity", OrmObjectsDef.NS_PREFIX), _nsMgr);

            EntityDescription entity;

            _ormObjectsDef.Entities.Clear();

            foreach (XmlNode entityNode in entitiesList)
            {
                string id, name, description, nameSpace, behaviourName;
                EntityBehaviuor behaviour = EntityBehaviuor.Default;

                XmlElement entityElement = (XmlElement) entityNode;
                id = entityElement.GetAttribute("id");
                name = entityElement.GetAttribute("name");
                description = entityElement.GetAttribute("description");
                nameSpace = entityElement.GetAttribute("namespace");
                behaviourName = entityElement.GetAttribute("behaviour");

                if (!string.IsNullOrEmpty(behaviourName))
                    behaviour = (EntityBehaviuor) Enum.Parse(typeof (EntityBehaviuor), behaviourName);


                entity = new EntityDescription(id, name, nameSpace, description, _ormObjectsDef);
                entity.Behaviour = behaviour;

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

            foreach (XmlNode propertyNode in propertiesList)
            {
                string name, description, typeId, fieldname, sAttributes, tableId, fieldAccessLevelName, propertyAccessLevelName, propertyAlias, propertyDisabled;
                string[] attributes;
                TableDescription table;
                AccessLevel fieldAccessLevel, propertyAccessLevel;
                bool disabled = false;

                XmlElement propertyElement = (XmlElement) propertyNode;
                description = propertyElement.GetAttribute("description");
                name = propertyElement.GetAttribute("propertyName");
                fieldname = propertyElement.GetAttribute("fieldName");
                typeId = propertyElement.GetAttribute("typeRef");
                sAttributes = propertyElement.GetAttribute("attributes");
                tableId = propertyElement.GetAttribute("table");
                fieldAccessLevelName = propertyElement.GetAttribute("classfieldAccessLevel");
                propertyAccessLevelName = propertyElement.GetAttribute("propertyAccessLevel");
                propertyAlias = propertyElement.GetAttribute("propertyAlias");
                propertyDisabled = propertyElement.GetAttribute("disabled");

                attributes = sAttributes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (!string.IsNullOrEmpty(propertyAccessLevelName))
                    propertyAccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), propertyAccessLevelName);
                else
                    propertyAccessLevel = AccessLevel.Public;

                if (!string.IsNullOrEmpty(fieldAccessLevelName))
                    fieldAccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), fieldAccessLevelName);
                else
                    fieldAccessLevel = AccessLevel.Private;

                table = entity.GetTable(tableId);

                if (!String.IsNullOrEmpty(propertyDisabled))
                    disabled = XmlConvert.ToBoolean(propertyDisabled);

                if (table == null)
                    throw new OrmXmlParserException(
                        string.Format("Table '{0}' for property '{1}' of entity '{2}' not found.", tableId, name,
                                      entity.Identifier));

                TypeDescription typeDesc = _ormObjectsDef.GetType(typeId, true);
                
                PropertyDescription property = new PropertyDescription(entity ,name, propertyAlias, attributes, description, typeDesc, fieldname, table, fieldAccessLevel, propertyAccessLevel);
                property.Disabled = disabled;

                entity.Properties.Add(property);
            }
        }

        internal protected void FillRelations()
        {
            XmlNodeList relationNodes;
			#region Relations
			relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:Relation", OrmObjectsDef.NS_PREFIX), _nsMgr);

			foreach (XmlNode relationNode in relationNodes)
			{
				XmlNode leftTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Left", OrmObjectsDef.NS_PREFIX), _nsMgr);
				XmlNode rightTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Right", OrmObjectsDef.NS_PREFIX), _nsMgr);

				XmlElement relationElement = (XmlElement)relationNode;
				string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");

				XmlElement leftTargetElement = (XmlElement)leftTargetNode;
				string leftLinkTargetEntityId = leftTargetElement.GetAttribute("entity");
				XmlElement rightTargetElement = (XmlElement)rightTargetNode;
				string rightLinkTargetEntityId = rightTargetElement.GetAttribute("entity");

				string leftFieldName = leftTargetElement.GetAttribute("fieldName");
				string rightFieldName = rightTargetElement.GetAttribute("fieldName");

				bool leftCascadeDelete = XmlConvert.ToBoolean(leftTargetElement.GetAttribute("cascadeDelete"));
				bool rightCascadeDelete = XmlConvert.ToBoolean(rightTargetElement.GetAttribute("cascadeDelete"));

				TableDescription relationTable = _ormObjectsDef.GetTable(relationTableId);

				EntityDescription underlyingEntity;
				if (string.IsNullOrEmpty(underlyingEntityId))
					underlyingEntity = null;
				else
					underlyingEntity = _ormObjectsDef.GetEntity(underlyingEntityId);

				bool disabled;
				if (string.IsNullOrEmpty(disabledValue))
					disabled = false;
				else
					disabled = XmlConvert.ToBoolean(disabledValue);



				EntityDescription leftLinkTargetEntity = _ormObjectsDef.GetEntity(leftLinkTargetEntityId);

				EntityDescription rightLinkTargetEntity = _ormObjectsDef.GetEntity(rightLinkTargetEntityId);

				LinkTarget leftLinkTarget = new LinkTarget(leftLinkTargetEntity, leftFieldName, leftCascadeDelete);
				LinkTarget rightLinkTarget = new LinkTarget(rightLinkTargetEntity, rightFieldName, rightCascadeDelete);

				RelationDescription relation = new RelationDescription(leftLinkTarget, rightLinkTarget, relationTable, underlyingEntity, disabled);
				_ormObjectsDef.Relations.Add(relation);
			} 
			#endregion
			#region SelfRelations
			relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:SelfRelation", OrmObjectsDef.NS_PREFIX), _nsMgr);

			foreach (XmlNode relationNode in relationNodes)
			{
				XmlNode directTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Direct", OrmObjectsDef.NS_PREFIX), _nsMgr);
				XmlNode reverseTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Reverse", OrmObjectsDef.NS_PREFIX), _nsMgr);

				XmlElement relationElement = (XmlElement)relationNode;
				string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");
				string entityId = relationElement.GetAttribute("entity");

				XmlElement directTargetElement = (XmlElement)directTargetNode;
				XmlElement reverseTargetElement = (XmlElement)reverseTargetNode;

				string directFieldName = directTargetElement.GetAttribute("fieldName");
				string reverseFieldName = reverseTargetElement.GetAttribute("fieldName");

				bool directCascadeDelete = XmlConvert.ToBoolean(directTargetElement.GetAttribute("cascadeDelete"));
				bool reverseCascadeDelete = XmlConvert.ToBoolean(reverseTargetElement.GetAttribute("cascadeDelete"));

				TableDescription relationTable = _ormObjectsDef.GetTable(relationTableId);

				EntityDescription underlyingEntity;
				if (string.IsNullOrEmpty(underlyingEntityId))
					underlyingEntity = null;
				else
					underlyingEntity = _ormObjectsDef.GetEntity(underlyingEntityId);

				bool disabled;
				if (string.IsNullOrEmpty(disabledValue))
					disabled = false;
				else
					disabled = XmlConvert.ToBoolean(disabledValue);



				EntityDescription entity = _ormObjectsDef.GetEntity(entityId);

				SelfRelationTarget directTarget = new SelfRelationTarget(directFieldName, directCascadeDelete);
				SelfRelationTarget reverseTarget = new SelfRelationTarget(reverseFieldName, reverseCascadeDelete);

				SelfRelationDescription relation = new SelfRelationDescription(entity, directTarget, reverseTarget, relationTable, underlyingEntity, disabled);
				_ormObjectsDef.Relations.Add(relation);
			}
			#endregion
        }

        internal protected void FillTables()
        {
            XmlNodeList tableNodes;
            tableNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Tables/{0}:Table", OrmObjectsDef.NS_PREFIX), _nsMgr);

            foreach (XmlNode tableNode in tableNodes)
            {
                string id;
                string name;

                XmlElement tableElement = (XmlElement) tableNode;
                id = tableElement.GetAttribute("id");
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

            foreach (XmlNode tableNode in tableNodes)
            {
                string tableId;
                XmlElement tableElement = (XmlElement) tableNode;
                tableId = tableElement.GetAttribute("ref");

                TableDescription table = entity.OrmObjectsDef.GetTable(tableId);

                entity.Tables.Add(table);
            }
        }

        internal protected void Read()
        {
            XmlSchemaSet schemaSet;
            XmlSchema schema;
            XmlReaderSettings xmlReaderSettings;

            schemaSet = new XmlSchemaSet(_nametable);

            schema = ResourceManager.GetXmlSchema("XInclude");
            schemaSet.Add(schema);
            schema = ResourceManager.GetXmlSchema(SCHEMA_NAME);
            schemaSet.Add(schema);
            

            xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.CloseInput = false;
            xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            xmlReaderSettings.IgnoreComments = true;
            xmlReaderSettings.IgnoreWhitespace = true;
            xmlReaderSettings.Schemas = schemaSet;
            xmlReaderSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.AllowXmlAttributes | XmlSchemaValidationFlags.ProcessIdentityConstraints;
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += xmlReaderSettings_ValidationEventHandler;

            XmlDocument xml;
            xml = new XmlDocument(_nametable);

            _validationResult.Clear();

            XmlDocument tDoc = new XmlDocument();
            using (Mvp.Xml.XInclude.XIncludingReader rdr = new Mvp.Xml.XInclude.XIncludingReader(_reader))
            {
                rdr.XmlResolver = _xmlResolver;
                tDoc.Load(rdr);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter wr = XmlWriter.Create(ms))
                {
                    tDoc.WriteTo(wr);
                }
                ms.Position = 0;
                using (XmlReader rdr = XmlReader.Create(ms, xmlReaderSettings))
                {
                    xml.Load(rdr);
                }
            }

            if (_validationResult.Count == 0)
                _ormXmlDocument = xml;
        }

        void xmlReaderSettings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if(e.Severity == XmlSeverityType.Warning)
                return;
            throw new OrmXmlParserException(string.Format("Xml document format error{1}: {0}", e.Message, (e.Exception != null) ? string.Format("({0},{1})", e.Exception.LineNumber, (e.Exception as XmlSchemaException).LinePosition) : string.Empty));
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
