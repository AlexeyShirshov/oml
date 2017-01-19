using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreFramework;

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
        //[TestMethod, ExpectedException(typeof(MissingMethodException))]
        //public void TestActivatorError2()
        //{
        //    var t = typeof(cls);
        //}
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
    }
}
