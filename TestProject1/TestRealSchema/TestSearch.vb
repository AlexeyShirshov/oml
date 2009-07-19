Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria
Imports Worm.Query

<TestClass()> _
Public Class TestSearch

    <TestMethod()> _
    Public Sub TestSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("1"))
            Dim c As ICollection(Of Table1) = New QueryCmd().FromSearch("second").ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)

            For Each t As Table1 In c
                Assert.AreEqual(2, t.Identifier)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSearch2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("Search"))
            Dim c As ICollection(Of Table1) = New QueryCmd().FromSearch("sec").ToList(Of Table1)(mgr)

            Assert.AreEqual(2, c.Count)

            'For Each t As Table1 In c
            '    Assert.AreEqual(2, t.Identifier)
            'Next
            Dim sf As New SearchFragment("sec") With {.Context = "sf"}
            c = New QueryCmd().From(sf).ToList(Of Table1)(mgr)
            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("1"))
            Dim c As ICollection(Of Table2) = New QueryCmd().FromSearch(GetType(Table1), "first").ToList(Of Table2)(mgr)

            Assert.AreEqual(2, c.Count)

            For Each t As Table2 In c
                Assert.AreEqual(Enum1.first, t.Tbl.Enum)
            Next

            c = New QueryCmd().FromSearch(GetType(Table1), "second").ToList(Of Table2)(mgr)

            Assert.AreEqual(0, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSortSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("1"))
            Dim c As ICollection(Of Table1) = New QueryCmd().FromSearch("sec").OrderBy(SCtor.prop(GetType(Table1), "DT")).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Table1) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

            Assert.AreEqual(2, l(0).Identifier)
            Assert.AreEqual(3, l(1).Identifier)

            Dim q As New QueryCmd()
            q.AutoJoins = True
            Dim c2 As ICollection(Of Table2) = q.FromSearch(GetType(Table1), "first").OrderBy(SCtor.prop(GetType(Table2), "DT")).ToList(Of Table2)(mgr)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSortSearchAutoJoin()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("Search"))
            Dim q As New QueryCmd()
            q.AutoJoins = True
            Dim c As ICollection(Of Table1) = q.FromSearch("first").OrderBy(SCtor.prop(GetType(Table2), "Money")).ToList(Of Table1)(mgr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPageSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("1"))
            'Using New Worm.OrmManager.PagerSwitcher(mgr, 0, 1)
            Dim c As ICollection(Of Table1) = New QueryCmd().FromSearch("sec").OrderBy(SCtor.prop(GetType(Table1), "DT")).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
            'End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFilterSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("1"))

            Dim c As ICollection(Of Table1) = New QueryCmd() _
                .FromSearch("sec") _
                .Where(Ctor.prop(GetType(Table1), "Code").eq(45).Filter) _
                .OrderBy(SCtor.prop(GetType(Table1), "DT")) _
                .ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)

            c = New QueryCmd().FromSearch("sec").Where(Ctor.prop(GetType(Table1), "Code").eq(45).Filter).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)

            'Dim os As IEntitySchema = CType(mgr.MappingEngine.GetEntitySchema(GetType(Table1)), IEntitySchema)
            'Dim cn As New Condition.ConditionConstructor
            'cn.AddFilter(New TableFilter(os.Table, "code", New ScalarValue(8923), Worm.Criteria.FilterOperation.Equal))

            'c = New QueryCmd().FromSearch("sec").Where(cn.Condition).ToList(Of Table1)(mgr)

            'Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSearchPaging()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText(New Worm.ObjectMappingEngine("Search"))
            'Using New Worm.OrmManager.PagerSwitcher(mgr, 0, 1)
            Dim c As ICollection(Of Table1) = New QueryCmd().Paging(0, 1).FromSearch("sec").ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
            'End Using

            'Using New Worm.OrmManager.PagerSwitcher(mgr, 1, 1)
            c = New QueryCmd().Paging(1, 1).FromSearch("sec").ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
            'End Using

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(NotImplementedException))> _
    Public Sub TestM2MSearch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerSharedFullText( _
            New Worm.ObjectMappingEngine("Search", Function(currentVersion As String, entities() As EntityAttribute, objType As Type) Array.Find(entities, Function(ea As EntityAttribute) ea.Version = "1")))

            Dim t3 As Table3 = New QueryCmd().GetByID(Of Table3)(1, mgr)
            Dim col As Worm.ReadOnlyList(Of Table1) = t3.GetCmd(GetType(Table1)).ToOrmList(Of Table1)(mgr)

            Assert.AreEqual(2, col.Count)

            col = t3.GetCmd(GetType(Table1)).FromSearch("first").ToOrmList(Of Table1)(mgr)

            Assert.AreEqual(1, col.Count)
            Assert.AreEqual("first", col(0).Name)
        End Using
    End Sub
End Class
