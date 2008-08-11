Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm.Database.Criteria.Joins
Imports Worm.Orm.Meta
Imports System.Collections
Imports Worm.Orm
Imports Worm
Imports Worm.Database.Criteria
Imports Worm.Database.Criteria.Core

<TestClass()> Public Class TestAgreegates

    Private testContextInstance As TestContext

    '''<summary>
    '''Gets or sets the test context which provides
    '''information about and functionality for the current test run.
    '''</summary>
    Public Property TestContext() As TestContext
        Get
            Return testContextInstance
        End Get
        Set(ByVal value As TestContext)
            testContextInstance = value
        End Set
    End Property

#Region "Additional test attributes"
    '
    ' You can use the following additional attributes as you write your tests:
    '
    ' Use ClassInitialize to run code before running the first test in the class
    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
    ' End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    ' <TestInitialize()> Public Sub MyTestInitialize()
    ' End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region

    <TestMethod()> Public Sub TestMax()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
                New Aggregate(AggregateFunction.Max, GetType(Entity4), "ID") _
            })

            Dim i As Integer = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCount()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
                New Aggregate(AggregateFunction.Count) _
            })

            Dim i As Integer = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)

        End Using
    End Sub

    <TestMethod()> Public Sub TestOrder()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t As Type = GetType(Entity4)
            Dim r As M2MRelation = mgr.DbSchema.GetM2MRelation(t, GetType(Entity), True)
            Dim r2 As M2MRelation = mgr.DbSchema.GetM2MRelation(GetType(Entity), t, True)
            Assert.IsNotNull(r)

            Dim table As SourceFragment = r.Table

            Dim inner As QueryCmdBase = New QueryCmdBase(table)
            inner.Filter = New JoinFilter(table, r2.Column, t, "ID", Worm.Criteria.FilterOperation.Equal)
            inner.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
                New Aggregate(AggregateFunction.Count) _
            })

            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Sort = New Worm.Sorting.SortAdv(inner, SortType.Desc)

            Dim l As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(12, l.Count)
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)

            Assert.AreEqual(11, l(1).Identifier)
            Assert.AreEqual("2tb2b25b", l(1).Title)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilter()

    End Sub

    <TestMethod()> Public Sub TestGroup()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
                New Aggregate(AggregateFunction.Count) _
            })

            q.Aggregates(0).Alias = "cnt"

            Dim t As Type = GetType(Entity4)
            Dim r As M2MRelation = mgr.DbSchema.GetM2MRelation(t, GetType(Entity), CStr(Nothing))
            Dim r2 As M2MRelation = mgr.DbSchema.GetM2MRelation(GetType(Entity), t, CStr(Nothing))
            Dim table As SourceFragment = r.Table
            Dim jf As New JoinFilter(table, r2.Column, t, "ID", Worm.Criteria.FilterOperation.Equal)
            q.Joins = New OrmJoin() {New OrmJoin(table, Worm.Criteria.Joins.JoinType.Join, jf)}

            Assert.AreEqual(39, q.ToSimpleList(Of Integer)(mgr)(0))

            q.Group = New ObjectModel.ReadOnlyCollection(Of OrmProperty)( _
                New OrmProperty() {New OrmProperty(table, r.Column)} _
            )

            Dim l As IList(Of Integer) = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)

            q.Sort = Sorting.Custom("cnt desc")
            l = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)
            Assert.AreEqual(11, l(0))
            Assert.AreEqual(4, l(1))

            q.Sort = New Worm.Sorting.SortAdv(q.Aggregates(0), SortType.Desc)
            l = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)
            Assert.AreEqual(11, l(0))
            Assert.AreEqual(4, l(1))
        End Using
    End Sub
End Class
