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

            c = mgr.Search(Of Table1)("sec", Nothing, "sf")
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

    <TestMethod()> _
    Public Sub TestSortSearch()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("1"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                Orm.Sorting.Field("DT"), Nothing)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Table1) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

            Assert.AreEqual(2, l(0).Identifier)
            Assert.AreEqual(3, l(1).Identifier)

            Dim c2 As ICollection(Of Table2) = mgr.Search(Of Table2)(GetType(Table1), "first", Orm.Sorting.Field("DT"))

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Orm.OrmManagerException))> _
    Public Sub TestSortSearchWrong()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("Search"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("first", _
                Orm.Sorting.Field(GetType(Table2), "Money"), Nothing)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPageSearch()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("1"))
            Using New Orm.OrmManagerBase.PagerSwitcher(mgr, 0, 1)
                Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                    Orm.Sorting.Field("DT"), Nothing)

                Assert.AreEqual(1, c.Count)
            End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFilterSearch()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Orm.DbSchema("1"))

            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                Orm.Sorting.Field("DT"), Nothing, Orm.Criteria.Field(GetType(Table1), "Code").Eq(45).Filter)

            Assert.AreEqual(1, c.Count)

            c = mgr.Search(Of Table1)("sec", _
                Nothing, Nothing, Orm.Criteria.Field(GetType(Table1), "Code").Eq(45).Filter)

            Assert.AreEqual(1, c.Count)

            Dim os As Orm.IOrmObjectSchema = CType(mgr.ObjectSchema.GetObjectSchema(GetType(Table1)), Orm.IOrmObjectSchema)
            Dim cn As New Orm.Condition.ConditionConstructor
            cn.AddFilter(New Orm.TableFilter(os.GetTables(0), "code", New Orm.SimpleValue(8923), Orm.FilterOperation.Equal))

            c = mgr.Search(Of Table1)("sec", Nothing, Nothing, cn.Condition)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub
End Class
