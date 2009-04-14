using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Worm.CodeGen.Core;
using System.Xml;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace TestsCodeGenLib
{
    /// <summary>
    /// Summary description for TestCodeGen
    /// </summary>
    [TestClass]
    public class TestCodeGen
    {
        [TestMethod]
        public void TestNullValue()
        {
            {
                int val0 = default(int);
                int val1 = 1;

                object obj0 = (object) val0;
                object obj1 = (object) val1;

                object defaultObj0 = Activator.CreateInstance(obj0.GetType());
                object defaultObj1 = Activator.CreateInstance(obj1.GetType());
                Assert.AreEqual(defaultObj0, obj0);
                Assert.AreNotEqual(defaultObj1, obj1);
            }
            {
                DateTime val0 = default(DateTime);
                DateTime val1 = DateTime.Now;

                object obj0 = (object) val0;
                object obj1 = (object) val1;

                object defaultObj0 = Activator.CreateInstance(obj0.GetType());
                object defaultObj1 = Activator.CreateInstance(obj1.GetType());
                Assert.AreEqual(defaultObj0, obj0);
                Assert.AreNotEqual(defaultObj1, obj1);
            }



        }

        [TestMethod]
        public void TestCSCodeSimple()
        {
            using (Stream stream = TestOrmXmlParse.GetSampleFileStream())
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeSuppressed()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeGroups()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeGroups()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeGroupsHideParent()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups2"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeGroupsHideParent()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups2"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeM2MCheck1()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck1"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeM2MCheck1()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck1"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        //[ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeM2MCheck2()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck2"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        //[ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeM2MCheck2()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck2"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeM2MCheck3()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck3"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeM2MCheck3()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck3"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeM2MCheck4()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck4"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeM2MCheck4()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck4"))
            {
                TestVBCodeInternal(stream);
            }
        }

        

        public void TestCSCodeInternal(Stream stream)
        {
            CodeDomProvider prov = new Microsoft.CSharp.CSharpCodeProvider();
            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;

			//settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

			//stream.Position = 0;

			//settings.Split = true;
			//CompileCode(prov, settings, XmlReader.Create(stream));
           
        }

        private static void CompileCode(CodeDomProvider prov, OrmCodeDomGeneratorSettings settings, XmlReader reader)
        {
            OrmObjectsDef odef = null;
            using (reader)
            {
                odef = OrmObjectsDef.LoadFromXml(reader);
            }
            OrmCodeDomGenerator gen = new OrmCodeDomGenerator(odef, settings);
            Dictionary<string,CodeCompileUnit> dic = gen.GetFullDom();

            
            CompilerParameters prms = new CompilerParameters();
            prms.GenerateExecutable = false;
            prms.GenerateInMemory = true;
            prms.IncludeDebugInformation = false;
            prms.TreatWarningsAsErrors = false;
            prms.OutputAssembly = "testAssembly.dll";
            prms.ReferencedAssemblies.Add("System.dll");
            prms.ReferencedAssemblies.Add("System.Data.dll");
            prms.ReferencedAssemblies.Add("System.XML.dll");
            prms.ReferencedAssemblies.Add("CoreFramework.dll");
            prms.ReferencedAssemblies.Add("Worm.Orm.dll");
            prms.TempFiles.KeepFiles = true;

            CodeCompileUnit[] units = new CodeCompileUnit[dic.Values.Count];
            int idx = 0;
            foreach (CodeCompileUnit unit in dic.Values)
            {
                units[idx++] = unit;
            }

            CompilerResults result = prov.CompileAssemblyFromDom(prms, units);
            if(result.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CompilerError str in result.Errors)
                {
                    sb.AppendLine(str.ToString());
                }
                Assert.Fail(sb.ToString());
            }
            //foreach (CompilerError error in result.Errors)
            //{
            //    Assert.IsTrue(error.IsWarning, error.ToString());
            //}
        }

        [TestMethod]
        public void TestVBCodeSimple()
        {
            using (Stream stream = TestOrmXmlParse.GetSampleFileStream())
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeSuppressed()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeCopositePK()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("CompositePK"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeCopositePK()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("CompositePK"))
            {
                TestCSCodeInternal(stream);
            }
        }

		[TestMethod]
		public void TestVBCodeDefferedLoading()
		{
			using (Stream stream = Resources.GetXmlDocumentStream("deffered"))
			{
				TestVBCodeInternal(stream);
			}
		}

		[TestMethod]
		public void TestCSCodeDefferedLoading()
		{
			using (Stream stream = Resources.GetXmlDocumentStream("deffered"))
			{
				TestCSCodeInternal(stream);
			}
		}

        public void TestVBCodeInternal(Stream stream)
        {
            CodeDomProvider prov = new Microsoft.VisualBasic.VBCodeProvider();

            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;

			//settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

			//stream.Position = 0;

			//settings.Split = true;
			//CompileCode(prov, settings, XmlReader.Create(stream));
        }
    }
}
