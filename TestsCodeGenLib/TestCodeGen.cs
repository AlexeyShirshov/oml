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

        public void TestCSCodeInternal(Stream stream)
        {
            CodeDomProvider prov = new Microsoft.CSharp.CSharpCodeProvider();
            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;

            settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

            stream.Position = 0;

            settings.Split = true;
            CompileCode(prov, settings, XmlReader.Create(stream));
           
        }

        private static void CompileCode(CodeDomProvider prov, OrmCodeDomGeneratorSettings settings, XmlReader reader)
        {
            OrmObjectsDef odef = null;
            using (reader)
            {
                odef = OrmObjectsDef.LoadFromXml(reader);
            }
            OrmCodeDomGenerator gen = new OrmCodeDomGenerator(odef);
            Dictionary<string,CodeCompileUnit> dic = gen.GetFullDom(settings);

            
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
            foreach (CompilerError error in result.Errors)
            {
                Assert.IsTrue(error.IsWarning, error.ToString());
            }
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

        public void TestVBCodeInternal(Stream stream)
        {
            CodeDomProvider prov = new Microsoft.VisualBasic.VBCodeProvider();

            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;

            settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

            stream.Position = 0;

            settings.Split = true;
            CompileCode(prov, settings, XmlReader.Create(stream));
        }
    }
}
