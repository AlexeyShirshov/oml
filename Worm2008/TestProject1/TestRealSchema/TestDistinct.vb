Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Query

<TestClass()> _
Public Class TestDistinct

    <TestMethod()> _
    Public Sub TestSelect()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tbl As SourceFragment = s.GetTables(GetType(Tables1to3))(0)
            Dim f As New JoinFilter(tbl, "table1", GetType(Table1), "ID", Worm.Criteria.FilterOperation.Equal)
            Dim join As New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.Join, f)

            Dim joins() As QueryJoin = New QueryJoin() {join}

            Dim c As ICollection(Of Table1) = mgr.FindWithJoins(Of Table1)(Nothing, joins, Nothing, Nothing, True)

            Assert.AreEqual(3, c.Count)
            Assert.IsTrue(CType(c, IList(Of Table1))(0).InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoin()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tt As Type = GetType(Table1)
            Dim t As Type = GetType(Table2)
            Dim tbl As SourceFragment = s.GetTables(tt)(0)
            Dim col As ICollection(Of String) = s.GetPropertyAliasByType(t, tt)
            Assert.AreEqual(1, col.Count)
            Dim field As String = String.Empty
            For Each fld As String In col
                field = fld
            Next

            Dim f As New JoinFilter(tbl, "ID", t, field, Worm.Criteria.FilterOperation.Equal)

            Dim join As New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.Join, f)

            Dim joins() As QueryJoin = New QueryJoin() {join}

            Dim c As ICollection(Of Table2) = mgr.FindWithJoins(Of Table2)(Nothing, joins, Nothing, Nothing, True)

            Assert.AreEqual(2, c.Count)
            Assert.IsTrue(CType(c, IList(Of Table2))(0).InternalProperties.IsLoaded)

            c = mgr.FindWithJoins(Of Table2)(New TopAspect(1), joins, Nothing, Nothing, True)
            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoin2()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tt As Type = GetType(Table1)

            Dim c As ICollection(Of Table2) = mgr.FindJoin(Of Table2)(tt, "Table1", Nothing, Nothing, True)

            Assert.AreEqual(2, c.Count)
            Assert.IsTrue(CType(c, IList(Of Table2))(0).InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoin3()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tt As Type = GetType(Table1)

            Dim c As ICollection(Of Table2) = mgr.Find(Of Table2)(PCtor.prop(tt, "Code").not_eq(2), Nothing, True)

            Assert.AreEqual(0, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect2()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), _
                t, True), Nothing, Nothing, True)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect3()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            'Dim tbl As String = "dbo.tables1to3relation"
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim f As Worm.Criteria.PredicateLink = New PCtor(t).prop("Code").less_than_eq(10)
            'Dim join As New OrmJoin(tbl, JoinType.Join, f)

            'Dim f2 As New OrmFilter(tbl, "table1", GetType(Table1), "ID", Worm.Criteria.FilterOperation.Equal)
            'Dim join2 As New OrmJoin(tbl, JoinType.Join, f)


            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), t, True), f, Nothing, True)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect4()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), _
                t, True), Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect5()
        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tbl As SourceFragment = s.GetTables(GetType(Tables1to3))(0)
            Dim f As New JoinFilter(tbl, "table1", GetType(Table1), "ID", Worm.Criteria.FilterOperation.Equal)
            Dim join As New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.Join, f)

            Dim joins() As QueryJoin = New QueryJoin() {join}

            Dim c As ICollection(Of Table1) = mgr.FindWithJoins(Of Table1)(New Query.DistinctAspect(), joins, Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)
            Assert.IsFalse(CType(c, IList(Of Table1))(0).InternalProperties.IsLoaded)
        End Using
    End Sub
End Class
