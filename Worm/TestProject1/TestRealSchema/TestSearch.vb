Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics

<TestClass()> _
Public Class TestSearch

    <TestMethod()> _
    Public Sub TestSearch()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("1"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("second")

            Assert.AreEqual(1, c.Count)

            For Each t As Table1 In c
                Assert.AreEqual(2, t.Identifier)
            Next
        End Using
    End Sub
End Class
