using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrmCodeGenLib;
using OrmCodeGenLib.Descriptors;
using System.IO;
namespace TestsCodeGenLib
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestOrmXmlParse
    {
        public TestOrmXmlParse()
        {
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        [Description("�������� �������� xml ��������� � ����������")]
        public void TestReadXml()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
	        {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr, null);
	            parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(GetSampleFileStream());

            Assert.AreEqual<string>(doc.OuterXml, parser.SourceXmlDocument.OuterXml);
            
        }

        [TestMethod]
        [Description("�������� �������� ���������� �����")]
        public void TestFillFileDescription()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillFileDescriptions();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            Assert.AreEqual<string>("XMedia.Framework.Media.Objects", ormObjectDef.Namespace);
            Assert.AreEqual<string>("1", ormObjectDef.SchemaVersion);
        }

        [TestMethod]
        [Description("�������� �������� ������ ������ �� �����")]
        public void TestFillTables()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillTables();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(6, ormObjectDef.Tables.Count);
            Assert.IsTrue(ormObjectDef.Tables.Exists(delegate(TableDescription match) { return match.Identifier == "tblAlbums" && match.Name == "dbo.albums"; }));
            Assert.IsTrue(ormObjectDef.Tables.Exists(delegate(TableDescription match) { return match.Identifier == "tblArtists" && match.Name == "dbo.artists"; }));
            Assert.IsTrue(ormObjectDef.Tables.Exists(delegate(TableDescription match) { return match.Identifier == "tblAl2Ar" && match.Name == "dbo.al2ar"; }));
            Assert.IsTrue(ormObjectDef.Tables.Exists(delegate(TableDescription match) { return match.Identifier == "tblSiteAccess" && match.Name == "dbo.sites_access"; }));
            
        }


        [TestMethod]
        [Description("�������� ������ ������ ���������")]
        public void TestFindEntities()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FindEntities();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(4, ormObjectDef.Entities.Count);
            Assert.IsTrue(ormObjectDef.Entities.Exists(delegate(EntityDescription match) {return match.Identifier == "eArtist" && match.Name == "Artist";}));
            Assert.IsTrue(ormObjectDef.Entities.Exists(delegate(EntityDescription match) { return match.Identifier == "eAlbum" && match.Name == "Album"; }));
            Assert.IsTrue(ormObjectDef.Entities.Exists(delegate(EntityDescription match) { return match.Identifier == "Album2ArtistRelation" && match.Name == "Album2ArtistRelation"; }));

        }

        [TestMethod]
        public void TestFillTypes()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;
            Assert.AreEqual<int>(10, ormObjectDef.Types.Count);


        }

        [TestMethod]
        [Description("�������� �������� ������ ������ ��������")]
        public void TestFillEntityTables()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillTables();
            parser.FindEntities();


            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity;
            entity = ormObjectDef.Entities.Find(delegate(EntityDescription match) { return match.Identifier == "eArtist" && match.Name == "Artist"; });

            Assert.AreEqual<int>(2, entity.Tables.Count);
            Assert.IsTrue(entity.Tables.Exists(delegate(TableDescription match)
            {
                return match.Identifier.Equals("tblArtists")
                && match.Name.Equals("dbo.artists");
            }));
            Assert.IsTrue(entity.Tables.Exists(delegate(TableDescription match)
            {
                return match.Identifier.Equals("tblSiteAccess")
                && match.Name.Equals("dbo.sites_access");
            }));
        }

        [TestMethod]
        [Description("�������� ��������� �������")]
        public void TestFillProperties()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillTables();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();
            

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity;
            entity = ormObjectDef.Entities.Find(delegate(EntityDescription match) {return match.Identifier == "eArtist" && match.Name == "Artist";});

            parser.FillProperties(entity);

            Assert.AreEqual<int>(8, entity.Properties.Count);
            PropertyDescription prop;

            prop = entity.GetProperty("ID");
            Assert.IsNotNull(prop);
            Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes is undefined");
            Assert.AreEqual<string>("PK", prop.Attributes[0], "Attributes is not correct defined");
            Assert.IsNotNull(prop.Table, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.Table.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("id", prop.FieldName, "FieldName is undefined");
            Assert.AreEqual<string>("System.Int32", prop.PropertyType.TypeName, "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property ID Description", prop.Description, "Description is undefined");
            Assert.AreEqual<AccessLevel>(AccessLevel.Private, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Public, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>(prop.Name, prop.PropertyAlias, "PropertyAlias");

            prop = entity.GetProperty("Title");
            Assert.IsNotNull(prop);
            Assert.AreEqual<int>(0, prop.Attributes.Length, "Attributes is undefined");
            Assert.IsNotNull(prop.Table, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.Table.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("name", prop.FieldName, "FieldName is undefined");
            Assert.AreEqual<string>("System.String", prop.PropertyType.TypeName, "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property Title Description", prop.Description, "Description");
            Assert.AreEqual<AccessLevel>(AccessLevel.Private, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Assembly, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>(prop.Name, prop.PropertyAlias, "PropertyAlias");

            prop = entity.GetProperty("DisplayTitle");
            Assert.IsNotNull(prop);
            Assert.AreEqual<string>("DisplayTitle", prop.Name, "Name is undefined");
            Assert.AreEqual<int>(0, prop.Attributes.Length, "Attributes is undefined");
            Assert.IsNotNull(prop.Table, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.Table.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("display_name", prop.FieldName, "FieldName is undefined");
            Assert.AreEqual<string>("System.String", prop.PropertyType.TypeName, "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property Title Description", prop.Description, "Property Title Description");
            Assert.AreEqual<AccessLevel>(AccessLevel.Family, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Family, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>("DisplayName", prop.PropertyAlias, "PropertyAlias");

            prop = entity.GetProperty("Fact");

            Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes.Factory absent");
            Assert.AreEqual<string>("Factory", prop.Attributes[0], "Attributes.Factory invalid");

            prop = entity.GetProperty("TestInsDef");

            Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes.Factory absent");
            Assert.AreEqual<string>("InsertDefault", prop.Attributes[0], "Attributes.InsertDefault invalid");

            prop = entity.GetProperty("TestNullabe");

            Assert.AreEqual<Type>(typeof (int?), prop.PropertyType.ClrType);
            Assert.IsFalse(prop.Disabled, "Disabled false");

            prop = entity.GetProperty("TestDisabled");
            Assert.IsTrue(prop.Disabled, "Disabled true");

        }

        [TestMethod]
        [Description("�������� ��������� �������")]
        public void TestFillSuppressedProperties()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(Resources.GetXmlDocumentStream("suppressed")))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillTables();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();


            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity;
            entity = ormObjectDef.GetEntity("e11");

            parser.FillEntities();

            Assert.AreEqual<int>(1, entity.SuppressedProperties.Count, "SuppressedProperties.Count");

            PropertyDescription prop = entity.SuppressedProperties[0];
            Assert.AreEqual<string>("Prop1", prop.Name, "SuppressedPropertyName");
            Assert.IsTrue(prop.IsSuppressed,"SuppressedPropery.IsSuppressed");

            EntityDescription completeEntity = entity.CompleteEntity;

            prop = completeEntity.GetProperty("Prop1");
            Assert.IsNotNull(prop);
            Assert.IsTrue(prop.IsSuppressed);

            prop = completeEntity.GetProperty("Prop11");
            Assert.IsNotNull(prop);
            Assert.IsFalse(prop.IsSuppressed);

        }

        [TestMethod]
        [Description("�������� ���������� ����������")]
        public void TestFillRelations()
        {
            OrmCodeGenLib_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = OrmCodeGenLib_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new OrmCodeGenLib_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillTables();
            parser.FillImports();
            parser.FindEntities();
            parser.FillTypes();
            parser.FillEntities();
            

            parser.FillRelations();

            OrmObjectsDef ormObjectsDef;
            ormObjectsDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(4, ormObjectsDef.Relations.Count);

            RelationDescription relation = null;

        	foreach (RelationDescriptionBase relationDescriptionBase in ormObjectsDef.Relations)
        	{
				relation = relationDescriptionBase as RelationDescription;
				if (relation != null)
					break;
        	}
            

            Assert.IsNotNull(relation);

            Assert.AreEqual<bool>(false, relation.Disabled);

            TableDescription relationTable;
            relationTable = ormObjectsDef.Tables.Find(delegate(TableDescription match) { return match.Identifier == "tblAl2Ar";});
            EntityDescription relationEntity;
            relationEntity = ormObjectsDef.Entities.Find(delegate(EntityDescription match){ return match.Identifier == "Album2ArtistRelation";});

            Assert.AreEqual<TableDescription>(relationTable, relation.Table);
            Assert.AreEqual<EntityDescription>(relationEntity, relation.UnderlyingEntity);

            Assert.IsNotNull(relation.Left);
            Assert.IsNotNull(relation.Right);

            EntityDescription leftEntity;
            leftEntity = ormObjectsDef.Entities.Find(delegate(EntityDescription match){ return match.Identifier == "eArtist";});

            EntityDescription rightEntity;
            rightEntity = ormObjectsDef.Entities.Find(delegate(EntityDescription match){ return match.Identifier == "eAlbum";});

            Assert.AreEqual<EntityDescription>(leftEntity, relation.Left.Entity);
            Assert.AreEqual<EntityDescription>(rightEntity, relation.Right.Entity);
            Assert.IsFalse(relation.Left.CascadeDelete);
            Assert.IsTrue(relation.Right.CascadeDelete);

            Assert.AreEqual<string>("artist_id", relation.Left.FieldName);
            Assert.AreEqual<string>("album_id", relation.Right.FieldName);

            PropertyDescription leftEntityLinkProperty;
            leftEntityLinkProperty = leftEntity.GetProperty("Artist");

            PropertyDescription rightEntityLinkProperty;
            rightEntityLinkProperty = leftEntity.GetProperty("Album");

			//Assert.AreEqual<int>(2, ormObjectsDef.SelfRelations.Count);

			SelfRelationDescription selfRelation = null;
			foreach (RelationDescriptionBase relationDescriptionBase in ormObjectsDef.Relations)
			{
				selfRelation = relationDescriptionBase as SelfRelationDescription;
				if (selfRelation != null)
					break;
			}

			Assert.AreEqual<bool>(false, selfRelation.Disabled);

			relationTable = ormObjectsDef.Tables.Find(delegate(TableDescription match) { return match.Identifier == "tbla2b"; });

        	int idx = 0;
			foreach (RelationDescriptionBase relationDescriptionBase in ormObjectsDef.Relations)
			{
				selfRelation = relationDescriptionBase as SelfRelationDescription;
				if (selfRelation != null && idx++ == 1)
					break;
			}

			Assert.AreEqual<bool>(false, selfRelation.Disabled);

			relationTable = ormObjectsDef.Tables.Find(delegate(TableDescription match) { return match.Identifier == "tbla2b"; });
			relationEntity = ormObjectsDef.Entities.Find(delegate(EntityDescription match) { return match.Identifier == "Album2ArtistRelation"; });

        }

        public static Stream GetSampleFileStream()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName;
            resourceName = string.Format("{0}.SchemaBased.xml", assembly.GetName().Name);

            return assembly.GetManifestResourceStream(resourceName);
        }

    }
}
