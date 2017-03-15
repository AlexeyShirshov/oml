using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreFramework;
using System.Text.RegularExpressions;
using System.Linq;

namespace CoreFrameworkTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestActivatorArgs()
        {
            var t = typeof(cls);
            Assert.AreEqual("default ctor", (t.CreateInstance() as cls).str);
            Assert.AreEqual("int ctor", (t.CreateInstance(1) as cls).str);
            Assert.AreEqual("string ctor", (t.CreateInstance("") as cls).str);
            Assert.AreEqual("string int ctor", (t.CreateInstance("", 1) as cls).str);
            Assert.AreEqual("int string ctor", (t.CreateInstance(1, "") as cls).str);
            Assert.AreEqual("double float ctor", (t.CreateInstance(1d) as cls).str);
            Assert.AreEqual("double float ctor", (t.CreateInstance(1f) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstance(1d, "") as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstance("", 1d) as cls).str);
            Assert.AreEqual("double float ctor", (t.CreateInstance(1d, 1) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstance(1d, 1f, "") as cls).str);
            Assert.AreEqual("int string ctor", (t.CreateInstance(1, "", 1) as cls).str);
            Assert.AreEqual("int ctor", (t.CreateInstance(1, 1f, 1) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstance(1d, 1f, "", 1) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstance(1d, 1f, 1, "") as cls).str);
        }

        [TestMethod]
        public void TestActivator()
        {
            var t = typeof(cls);
            Assert.AreEqual("default ctor", (t.CreateInstanceDyn(new { }) as cls).str);
            Assert.AreEqual("default ctor", (t.CreateInstance() as cls).str);
            Assert.AreEqual("default ctor", (t.CreateInstanceDyn(null) as cls).str);
            Assert.AreEqual("string ctor", (t.CreateInstanceDyn(new { s = "" }) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstanceDyn(new { s = "", d = 1d }) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstanceDyn(new { s = "", f = 1f }) as cls).str);            
            Assert.AreEqual("double float string ctor", (t.CreateInstanceDyn(new { d = 1d, f = 1f, s = "" }) as cls).str);
            Assert.AreEqual("int ctor", (t.CreateInstanceDyn(new { i = 1, ss = "" }) as cls).str);
            Assert.AreEqual("double float string ctor", (t.CreateInstanceDyn(new { d = 1d, f = 1f, s = "", ss = "" }) as cls).str);

            Assert.AreEqual("double float ctor", (t.CreateInstanceDyn(new { d = 1d }) as cls).str);
            Assert.AreEqual("double float ctor", (t.CreateInstanceDyn(new { f = 1f }) as cls).str);
            Assert.AreEqual("double float ctor", (t.CreateInstanceDyn(new { d = 1d, ss = "" }) as cls).str);
        }
        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void TestActivatorError()
        {
            var t = typeof(cls);
            Assert.AreEqual("string int ctor", (t.CreateInstanceDyn(new { s = "", i = 1 }) as cls).str);
        }
        [TestMethod]
        public void TestActivatorMap()
        {
            var t = typeof(cls);
            Assert.AreEqual("double float ctor", (t.CreateInstance((Type mtype, Type atype)=> mtype == typeof(float)?(object)1:null, 1d) as cls).str);
        }
        //[TestMethod, ExpectedException(typeof(MissingMethodException))]
        //public void TestActivatorError3()
        //{
        //    var t = typeof(cls);
        //}
        //[TestMethod, ExpectedException(typeof(MissingMethodException))]
        //public void TestActivatorError4()
        //{
        //    var t = typeof(cls);
        //}
        class cls
        {
            public string str;
            public cls()
            {
                str = "default ctor";
                Console.WriteLine(str);
            }

            public cls(string s)
            {
                str = "string ctor";
                Console.WriteLine(str);
            }

            public cls(int i)
            {
                str = "int ctor";
                Console.WriteLine(str);
            }
            public cls(string s, int i)
            {
                str = "string int ctor";
                Console.WriteLine(str);
            }
            public cls(int i, string s)
            {
                str = "int string ctor";
                Console.WriteLine(str);
            }
            public cls(double d, float f)
            {
                str = "double float ctor";
                Console.WriteLine(str);
            }
            public cls(double d, float f, string s)
            {
                str = "double float string ctor";
                Console.WriteLine(str);
            }
        }

        [TestMethod]
        public void TestRe()
        {
            string viewName = @"F:\projects\kelix\core\sources\KX.Core\KX.Wpf.Core45\bin\sign\KX.Wpf.Core45.dll";
            string v = "xxx";
            var re = new Regex(@".+\\(.+)(\..+)$");
            var m = re.Match(viewName);
            if (m.Success)
            {
                var res = viewName.Substring(0, m.Groups[1].Index) + v + m.Groups[2].Value;
                Assert.AreEqual(@"F:\projects\kelix\core\sources\KX.Core\KX.Wpf.Core45\bin\sign\xxx.dll", res);
            }
        }
        [TestMethod]
        public void TestNull()
        {
            int[] i = null;
            Assert.IsFalse(i?.Count() > 0);

            Assert.IsFalse(i?.Any() == true);

            string s = null;

            Assert.AreEqual("x", s ?? "x");
            //s = string.Empty;
            //Assert.AreEqual("x", s ?? "x");
        }
    }
}
