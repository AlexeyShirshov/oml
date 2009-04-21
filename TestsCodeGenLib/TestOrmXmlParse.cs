using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Worm.CodeGen.Core;
using Worm.CodeGen.Core.Descriptors;
using System.IO;
using System.Linq;

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
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
	        {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr, null);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
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
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
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
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();

        	OrmObjectsDef ormObjectDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(6, ormObjectDef.SourceFragments.Count);
            Assert.IsTrue(ormObjectDef.SourceFragments.Exists(
                          	match => match.Identifier == "tblAlbums" && match.Name == "albums"));
            Assert.IsTrue(ormObjectDef.SourceFragments.Exists(
                          	match => match.Identifier == "tblArtists" && match.Name == "artists"));
			Assert.IsTrue(ormObjectDef.SourceFragments.Exists(
			              	match => match.Identifier == "tblAl2Ar" && match.Name == "al2ar"));
			Assert.IsTrue(ormObjectDef.SourceFragments.Exists(
			              	match => match.Identifier == "tblSiteAccess" && match.Name == "sites_access"));
            
        }


        [TestMethod]
        [Description("�������� ������ ������ ���������")]
        public void TestFindEntities()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }
            parser.FillSourceFragments();
            parser.FindEntities();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(5, ormObjectDef.Entities.Count());
			Assert.AreEqual<int>(4, ormObjectDef.ActiveEntities.Count());
            Assert.IsTrue(ormObjectDef.Entities.Any(delegate(EntityDescription match) {return match.Identifier == "eArtist" && match.Name == "Artist";}));
            Assert.IsTrue(ormObjectDef.Entities.Any(delegate(EntityDescription match) { return match.Identifier == "eAlbum" && match.Name == "Album"; }));
            Assert.IsTrue(ormObjectDef.Entities.Any(delegate(EntityDescription match) { return match.Identifier == "Album2ArtistRelation" && match.Name == "Album2ArtistRelation"; }));

        }

        [TestMethod]
        public void TestFillTypes()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;
            Assert.AreEqual<int>(11, ormObjectDef.Types.Count);


        }

        [TestMethod]
        [Description("�������� �������� ������ ������ ��������")]
        public void TestFillEntityTables()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

			parser.FillSourceFragments();
            parser.FindEntities();


            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity = ormObjectDef.Entities
                .Single(match => match.Identifier == "eArtist" && match.Name == "Artist");

            Assert.AreEqual<int>(2, entity.SourceFragments.Count);
            Assert.IsTrue(entity.SourceFragments.Exists(match => match.Identifier.Equals("tblArtists")
                                                                 && match.Name.Equals("artists")));
            Assert.IsTrue(entity.SourceFragments.Exists(match => match.Identifier.Equals("tblSiteAccess")
                                                                 && match.Name.Equals("sites_access")));
        }

        [TestMethod]
        [Description("�������� ��������� �������")]
        public void TestFillProperties()
        {
        	Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

			parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();
            

            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity = ormObjectDef.Entities
                .Single(match => match.Identifier == "eArtist" && match.Name == "Artist");

            parser.FillProperties(entity);

            Assert.AreEqual<int>(8, entity.Properties.Count);
            PropertyDescription prop;

            prop = entity.GetProperty("ID");
            Assert.IsNotNull(prop);
            Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes is undefined");
            Assert.AreEqual<string>("PK", prop.Attributes[0], "Attributes is not correct defined");
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("id", prop.FieldName, "FieldName is undefined");
            Assert.AreEqual<string>("System.Int32", prop.PropertyType.TypeName, "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property ID Description", prop.Description, "Description is undefined");
            Assert.AreEqual<AccessLevel>(AccessLevel.Private, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Public, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>(prop.Name, prop.PropertyAlias, "PropertyAlias");

            prop = entity.GetProperty("Title");
            Assert.IsNotNull(prop);
            Assert.AreEqual<int>(0, prop.Attributes.Length, "Attributes is undefined");
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
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
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
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
        public void TestFillPropertiesWithGroups()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            using (XmlReader rdr = XmlReader.Create(GetFile("groups")))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

			parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();


            OrmObjectsDef ormObjectDef;
            ormObjectDef = parser.OrmObjectsDef;

            EntityDescription entity = ormObjectDef.Entities
                .Single(match => match.Identifier == "e1");

            parser.FillProperties(entity);

            Assert.AreEqual<int>(6, entity.Properties.Count);
            PropertyDescription prop;

            prop = entity.GetProperty("Identifier1");
            Assert.IsNotNull(prop);
            Assert.IsNull(prop.Group);

            prop = entity.GetProperty("prop1");
            Assert.IsNotNull(prop);
            Assert.IsNotNull(prop.Group);
            Assert.AreEqual("grp", prop.Group.Name);
            Assert.IsTrue(prop.Group.Hide);

            prop = entity.GetProperty("prop4");
            Assert.IsNotNull(prop);
            Assert.IsNotNull(prop.Group);
            Assert.AreEqual("grp1", prop.Group.Name);
            Assert.IsFalse(prop.Group.Hide);

        }

        [TestMethod]
        [Description("�������� ��������� �������")]
        public void TestFillSuppressedProperties()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(Resources.GetXmlDocumentStream("suppressed")))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

			parser.FillSourceFragments();
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
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

			parser.FillSourceFragments();
            parser.FillImports();
            parser.FindEntities();
            parser.FillTypes();
            parser.FillEntities();
            

            parser.FillRelations();

            OrmObjectsDef ormObjectsDef;
            ormObjectsDef = parser.OrmObjectsDef;

            Assert.AreEqual<int>(5, ormObjectsDef.Relations.Count);

            RelationDescription relation = null;

        	foreach (RelationDescriptionBase relationDescriptionBase in ormObjectsDef.Relations)
        	{
				relation = relationDescriptionBase as RelationDescription;
				if (relation != null)
					break;
        	}
            

            Assert.IsNotNull(relation);

            Assert.AreEqual(false, relation.Disabled);

        	var relationTable = ormObjectsDef.SourceFragments.Find(match => match.Identifier == "tblAl2Ar");
            var relationEntity = ormObjectsDef.Entities
                .Single(match => match.Identifier == "Album2ArtistRelation");

            Assert.AreEqual(relationTable, relation.SourceFragment);
            Assert.AreEqual(relationEntity, relation.UnderlyingEntity);

            Assert.IsNotNull(relation.Left);
            Assert.IsNotNull(relation.Right);

            EntityDescription leftEntity;
            leftEntity = ormObjectsDef.Entities
                .Single(delegate(EntityDescription match){ return match.Identifier == "eArtist";});

            EntityDescription rightEntity;
            rightEntity = ormObjectsDef.Entities
                .Single(delegate(EntityDescription match){ return match.Identifier == "eAlbum";});

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

			relationTable = ormObjectsDef.SourceFragments.Find(match => match.Identifier == "tbla2b");

        	int idx = 0;
			foreach (RelationDescriptionBase relationDescriptionBase in ormObjectsDef.Relations)
			{
				selfRelation = relationDescriptionBase as SelfRelationDescription;
				if (selfRelation != null && idx++ == 1)
					break;
			}

			Assert.AreEqual<bool>(false, selfRelation.Disabled);

			relationTable = ormObjectsDef.SourceFragments.Find(match => match.Identifier == "tbla2b");
			relationEntity = ormObjectsDef.Entities
                .Single(delegate(EntityDescription match) { return match.Identifier == "Album2ArtistRelation"; });

        }

        public static Stream GetSampleFileStream()
        {
            string name = "SchemaBased";
            return GetFile(name);
        }

        private static Stream GetFile(string name)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName;
            
            resourceName = string.Format("{0}.{1}.xml", assembly.GetName().Name, name);
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}
