Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Core
Imports Worm.Criteria.Values

<TestClass()> _
Public Class TestSearch

    <TestMethod()> _
    Public Sub TestSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("second")

            Assert.AreEqual(1, c.Count)

            For Each t As Table1 In c
                Assert.AreEqual(2, t.Identifier)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSearch2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("Search"))
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                Sorting.Field("DT"), Nothing)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Table1) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

            Assert.AreEqual(2, l(0).Identifier)
            Assert.AreEqual(3, l(1).Identifier)

            Dim c2 As ICollection(Of Table2) = mgr.Search(Of Table2)(GetType(Table1), "first", Sorting.Field("DT"))

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestSortSearchWrong()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("Search"))
            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("first", _
                Sorting.Field(GetType(Table2), "Money"), Nothing)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPageSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))
            Using New Worm.OrmManagerBase.PagerSwitcher(mgr, 0, 1)
                Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                    Sorting.Field("DT"), Nothing)

                Assert.AreEqual(1, c.Count)
            End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFilterSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))

            Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec", _
                Sorting.Field("DT"), Nothing, Criteria.Ctor.Field(GetType(Table1), "Code").Eq(45).Filter)

            Assert.AreEqual(1, c.Count)

            c = mgr.Search(Of Table1)("sec", _
                Nothing, Nothing, Criteria.Ctor.Field(GetType(Table1), "Code").Eq(45).Filter)

            Assert.AreEqual(1, c.Count)

            Dim os As IOrmObjectSchema = CType(mgr.ObjectSchema.GetObjectSchema(GetType(Table1)), IOrmObjectSchema)
            Dim cn As New Criteria.Conditions.Condition.ConditionConstructor
            cn.AddFilter(New TableFilter(os.GetTables(0), "code", New ScalarValue(8923), Worm.Criteria.FilterOperation.Equal))

            c = mgr.Search(Of Table1)("sec", Nothing, Nothing, cn.Condition)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSearchPaging()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("Search"))
            Using New Worm.OrmManagerBase.PagerSwitcher(mgr, 0, 1)
                Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec")

                Assert.AreEqual(1, c.Count)
            End Using

            Using New Worm.OrmManagerBase.PagerSwitcher(mgr, 1, 1)
                Dim c As ICollection(Of Table1) = mgr.Search(Of Table1)("sec")

                Assert.AreEqual(1, c.Count)
            End Using

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("Search"))
            Dim t3 As Table3 = mgr.Find(Of Table3)(1)
            Dim col As Worm.ReadOnlyList(Of Table1) = t3.M2M.Find(Of Table1)()

            Assert.AreEqual(2, col.Count)

            col = t3.M2M.Search(Of Table1)("first")

            Assert.AreEqual(1, col.Count)
            Assert.AreEqual("first", col(0).Name)
        End Using
    End Sub
End Class
