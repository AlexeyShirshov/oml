Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics
Imports Worm.Database

<TestClass()> _
Public Class TestSerialization

    <TestMethod()> _
    Public Sub TestXml()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New DbSchema("1"))
            Dim t As Table3 = mgr.Find(Of Table3)(2)

            Assert.IsTrue(t.IsLoaded)
            Dim exp As String = t.Xml.OuterXml

            Dim s As New Xml.Serialization.XmlSerializer(GetType(Table3))
            Dim sb As New StringBuilder
            Dim sw As New IO.StringWriter(sb)
            s.Serialize(sw, t)

            t = CType(s.Deserialize(New IO.StringReader(sb.ToString)), Table3)

            Assert.IsTrue(t.IsLoaded)
            Assert.AreEqual(exp, t.Xml.OuterXml)
        End Using
    End Sub
End Class
