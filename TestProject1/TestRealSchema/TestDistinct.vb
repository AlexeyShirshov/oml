Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Orm
Imports System.Diagnostics

<TestClass()> _
Public Class TestDistinct

    <TestMethod()> _
    Public Sub TestSelect()
        Dim s As New DbSchema("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tbl As OrmTable = s.GetTables(GetType(Tables1to3))(0)
            Dim f As New OrmFilter(tbl, "table1", GetType(Table1), "ID", FilterOperation.Equal)
            Dim join As New OrmJoin(tbl, JoinType.Join, f)

            Dim joins() As OrmJoin = New OrmJoin() {join}

            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(joins, Nothing, Nothing, True)

            Assert.AreEqual(1, c.Count)
            Assert.IsTrue(CType(c, IList(Of Table1))(0).IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect2()
        Dim s As New DbSchema("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), _
                t, True), Nothing, Nothing, True)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect3()
        Dim s As New DbSchema("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            'Dim tbl As String = "dbo.tables1to3relation"
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim f As CriteriaLink = New Criteria(t).Field("Code").LessThanEq(10)
            'Dim join As New OrmJoin(tbl, JoinType.Join, f)

            'Dim f2 As New OrmFilter(tbl, "table1", GetType(Table1), "ID", FilterOperation.Equal)
            'Dim join2 As New OrmJoin(tbl, JoinType.Join, f)


            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), t, True), f, Nothing, True)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect4()
        Dim s As New DbSchema("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim t As Type = s.GetTypeByEntityName("Table3")
            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(s.GetM2MRelation(GetType(Table1), _
                t, True), Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelect5()
        Dim s As New DbSchema("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)
            Dim tbl As OrmTable = s.GetTables(GetType(Tables1to3))(0)
            Dim f As New OrmFilter(tbl, "table1", GetType(Table1), "ID", FilterOperation.Equal)
            Dim join As New OrmJoin(tbl, JoinType.Join, f)

            Dim joins() As OrmJoin = New OrmJoin() {join}

            Dim c As ICollection(Of Table1) = mgr.FindDistinct(Of Table1)(joins, Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)
            Assert.IsFalse(CType(c, IList(Of Table1))(0).IsLoaded)
        End Using
    End Sub
End Class
