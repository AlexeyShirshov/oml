Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Query

<TestClass()> _
Public Class TestSerialization

    <TestMethod()> _
    Public Sub TestXml()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t As Table3 = New QueryCmd().GetByID(Of Table3)(2, mgr)

            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Dim exp As String = t.Xml.OuterXml

            Dim s As New System.Xml.Serialization.XmlSerializer(GetType(Table3))
            Dim sb As New StringBuilder
            Dim sw As New IO.StringWriter(sb)
            s.Serialize(sw, t)

            t = CType(s.Deserialize(New IO.StringReader(sb.ToString)), Table3)

            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Assert.AreEqual(exp, t.Xml.OuterXml)
        End Using
    End Sub
End Class
