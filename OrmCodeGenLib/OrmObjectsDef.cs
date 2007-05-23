using System;
using System.Collections.Generic;
using System.Text;
using OrmCodeGenLib.Descriptors;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OrmCodeGenLib
{
    public class OrmObjectsDef
    {
        public const string NS_PREFIX = "oos";
        public const string NS_URI = "http://www.xmedia.ru/OrmObjectsSchema.xsd";

        #region Private Fields

        private readonly List<EntityDescription> _entities;
        private readonly List<TableDescription> _tables;
        private readonly List<RelationDescription> _relations;
        private readonly List<TypeDescription> _types;
        private readonly ImportsCollection _imports;

        private string _namespace;
        private string _schemaVersion;
        private string _uri;
        private readonly List<string> _userComments;
        private readonly List<string> _systemComments;
        private string _appName;
        private string _appVersion;
        private string _ns;

        #endregion Private Fields

        public OrmObjectsDef()
        {
            _entities = new List<EntityDescription>();
            _relations = new List<RelationDescription>();
            _tables = new List<TableDescription>();
            _types = new List<TypeDescription>();
            _userComments = new List<string>();
            _systemComments = new List<string>();
            _imports = new ImportsCollection();

            Assembly ass = Assembly.GetEntryAssembly();
            if (ass == null)
            {
                ass = Assembly.GetCallingAssembly();
            }
            _appName = ass.GetName().Name;
            _appVersion = ass.GetName().Version.ToString(4);
        }

        #region Properties
        public List<EntityDescription> Entities
        {
            get
            {
                return _entities;
            }
        }
        public List<TableDescription> Tables
        {
            get
            {
                return _tables;
            }
        }
        public List<RelationDescription> Relations
        {
            get
            {
                return _relations;
            }
        }

        public List<TypeDescription> Types
        {
            get
            {
                return _types;
            }
        }
        
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }
                
        public string SchemaVersion
        {
            get { return _schemaVersion; }
            set { _schemaVersion = value; }
        }

        public List<string> UserComments
        {
            get { return _userComments; }
        }

        internal List<string> SystemComments
        {
            get { return _systemComments; }
        }

        public ImportsCollection Imports
        {
            get { return _imports; }
        }

        public string FileUri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        internal string NS
        {
            get { return _ns; }
            set { _ns = value; }
        }

        #endregion Properties

        #region Methods

        public EntityDescription GetEntity(string entityId)
        {
            return GetEntity(entityId, false);
        }

        public EntityDescription GetEntity(string entityId, bool throwNotFoundException)
        {
            EntityDescription entity;
            string localName;
            OrmObjectsDef searchScope = GetSearchScope(entityId, out localName, throwNotFoundException);
            entity = searchScope.Entities.Find(delegate(EntityDescription match)
            {
                return match.Identifier == localName;
            });
            if (entity == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("Entity with id '{0}' not found.", entityId));
            return entity;
        }

        public TableDescription GetTable(string tableId)
        {
            return GetTable(tableId, false);
        }

        public TableDescription GetTable(string tableId, bool throwNotFoundException)
        {
            TableDescription table;
            string localName;
            OrmObjectsDef searchScope = GetSearchScope(tableId, out localName, throwNotFoundException);
            table = searchScope.Tables.Find(delegate(TableDescription match)
            {
                return match.Identifier == localName;
            });
            if (table == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("Table with id '{0}' not found.", tableId));
            return table;
        }

        public TypeDescription GetType(string typeId, bool throwNotFoundException)
        {
            TypeDescription type = null;
            string localName;
            OrmObjectsDef searchScope = GetSearchScope(typeId, out localName, throwNotFoundException);
            type = searchScope.Types.Find(
                    delegate(TypeDescription match) { return match.Identifier == localName; });
            if (throwNotFoundException && type == null)
                throw new KeyNotFoundException(string.Format("Type with id '{0}' not found.", typeId));
            return type;
        }

        private OrmObjectsDef GetSearchScope(string name, out string localName)
        {
            return GetSearchScope(name, out localName, false);
        }

        internal OrmObjectsDef GetSearchScope(string name, out string localName, bool throwNotFondException)
        {
            Match nameMatch = GetNsNameMatch(name);
            OrmObjectsDef searchScope = this;
            if(nameMatch.Success)
            {
                
                if(nameMatch.Groups["prefix"].Success)
                {
                    string prefix = nameMatch.Groups["prefix"].Value;
                    ImportDescription import = Imports[prefix];
                    if(import == null)
                        if(throwNotFondException)
                            throw new KeyNotFoundException(string.Format("Import with prefix '{0}' not found.", prefix));
                        else
                        {
                            localName = null;
                            return null;
                        }
                    searchScope = import.Content;
                    
                }
                localName = nameMatch.Groups["name"].Value;
            }
            else
            {
                localName = name;
            }
            return searchScope;
        }

        internal static Match GetNsNameMatch(string name)
        {
            Regex regex = new Regex(@"^(?:(?'prefix'[\w]{1,}):){0,1}(?'name'[\w\d-_.]+){1}$");
            return regex.Match(name);
        }

        public RelationDescription GetSimilarRelation(RelationDescription relation)
        {
            return _relations.Find(delegate(RelationDescription match)
                                      {
                                          return
                                              RelationDescription.IsSimilar(relation, match);
                                      });
        }

        public static OrmObjectsDef LoadFromXml(XmlReader reader)
        {
            OrmObjectsDef odef = OrmXmlParser.Parse(reader);
            odef.CreateSystemComments();
            return odef;
        }

        public void WriteToXml(XmlWriter writer)
        {
            XmlDocument doc = GetXmlDocument();
            doc.WriteTo(writer);
        }

        public XmlDocument GetXmlDocument()
        {
            CreateSystemComments();

            return OrmXmlGenerator.Generate(this);
        }

        private void CreateSystemComments()
        {
            AssemblyName executingAssemblyName = Assembly.GetExecutingAssembly().GetName();
            SystemComments.Clear();
            SystemComments.Add(string.Format("This file was generated by {0} v{1} application({3} v{4}).{2}", _appName, _appVersion, Environment.NewLine, executingAssemblyName.Name, executingAssemblyName.Version));
            SystemComments.Add(string.Format("By user '{0}' at {1:G}.{2}", System.Environment.UserName, DateTime.Now, Environment.NewLine));
        }

        #endregion Methods

        public class ImportsCollection : Dictionary<string, ImportDescription>
        {
            public ImportsCollection()
            {
                
            }

            public ImportsCollection(int capacity) : base(capacity)
            {
                
            }
        }
    }
}
