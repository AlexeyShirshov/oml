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

    <TestMethod()> _
    Public Sub TestSearch2()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("Search"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec")

            Assert.AreEqual(2, c.Count)

            'For Each t As Table1 In c
            '    Assert.AreEqual(2, t.Identifier)
            'Next

            c = mgr.Search(Of Table1)("sec", "sf")
            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSearch()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("1"))
            Dim c As ICollection(Of Table2) = mgr.Search(Of Table2)(GetType(Table1), "first", Nothing)

            Assert.AreEqual(2, c.Count)

            For Each t As Table2 In c
                Assert.AreEqual(Enum1.first, t.Tbl.Enum)
            Next

            c = mgr.Search(Of Table2)(GetType(Table1), "second", Nothing)

            Assert.AreEqual(0, c.Count)
        End Using
    End Sub
End Class
