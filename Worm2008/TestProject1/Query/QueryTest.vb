﻿Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Database
Imports Worm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports System.Collections

<TestClass()> Public Class BasicQueryTest

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

    <TestMethod()> Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyEntityList(Of Entity) = q.ToEntityList(Of Entity)(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.QueryGeneratorException))> Public Sub TestFilterFromRawTableWrong()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As SourceFragment = mgr.ObjectSchema.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmdBase(t)
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Orm.OrmProperty)( _
                New Orm.OrmProperty() { _
                    New Orm.OrmProperty(t, "id", "ID") _
                })

            Assert.IsNotNull(q)

            q.Filter = Ctor.AutoTypeField("ID").Eq(1)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterFromRawTable()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As SourceFragment = mgr.ObjectSchema.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmdBase(t)
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Orm.OrmProperty)( _
                New Orm.OrmProperty() { _
                    New Orm.OrmProperty(t, "id", "ID") _
                })
            Assert.IsNotNull(q)

            Dim c As New Worm.Database.Criteria.Conditions.Condition.ConditionConstructor
            c.AddFilter(New TableFilter(t, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

            q.Filter = c.Condition

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyEntityList(Of Entity) = q.ToEntityList(Of Entity)(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

    <TestMethod()> Public Sub TestSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Filter = Ctor.AutoTypeField("Title").Like("b%")
            q.propSort = Worm.Orm.Sorting.Field("ID")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)
            Assert.AreEqual(3, r(0).ID)

            q.propSort = Orm.Sorting.Field("ID").Desc
            r = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(12, r(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestSort2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Filter = Ctor.AutoTypeField("Title").Like("b%")
            q.propSort = Worm.Orm.Sorting.Field("ID")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)
            Assert.AreEqual(3, r(0).ID)

            q.propSort.Order = Orm.SortType.Desc
            r = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(12, r(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()

    End Sub

    <TestMethod()> Public Sub TestAutoJoin()

    End Sub

    <TestMethod()> Public Sub TestDistinct()

    End Sub

    <TestMethod()> Public Sub TestInner()

    End Sub

    <TestMethod()> Public Sub TestTop()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmdBase(GetType(Entity4))
            q.Filter = Ctor.AutoTypeField("Title").Like("b%")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(3, r.Count)
            Dim m As Integer = q.Mark

            q.propTop = New Top(2)
            Assert.AreNotEqual(m, q.Mark)

            r = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(2, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestHint()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmdBase(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim s As String = "<ShowPlanXML xmlns='http://schemas.microsoft.com/sqlserver/2004/07/showplan' Version='1.0' Build='9.00.3042.00'><BatchSequence><Batch><Statements><StmtSimple StatementText='declare @p1 Int;set @p1 = 1&#xd;' StatementId='1' StatementCompId='1' StatementType='ASSIGN'/><StmtSimple StatementText='&#xa;select t1.id from dbo.ent1 t1 where t1.id = @p1&#xd;&#xa;' StatementId='2' StatementCompId='2' StatementType='SELECT' StatementSubTreeCost='0.0032831' StatementEstRows='1' StatementOptmLevel='TRIVIAL'><StatementSetOptions QUOTED_IDENTIFIER='false' ARITHABORT='true' CONCAT_NULL_YIELDS_NULL='false' ANSI_NULLS='false' ANSI_PADDING='false' ANSI_WARNINGS='false' NUMERIC_ROUNDABORT='false'/><QueryPlan CachedPlanSize='8' CompileTime='0' CompileCPU='0' CompileMemory='72'><RelOp NodeId='0' PhysicalOp='Clustered Index Seek' LogicalOp='Clustered Index Seek' EstimateRows='1' EstimateIO='0.003125' EstimateCPU='0.0001581' AvgRowSize='11' EstimatedTotalSubtreeCost='0.0032831' Parallel='0' EstimateRebinds='0' EstimateRewinds='0'><OutputList><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></OutputList><IndexScan Ordered='1' ScanDirection='FORWARD' ForcedIndex='0' NoExpandHint='0'><DefinedValues><DefinedValue><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></DefinedValue></DefinedValues><Object Schema='[dbo]' Table='[ent1]' Index='[PK_ent1]' Alias='[t1]'/><SeekPredicates><SeekPredicate><Prefix ScanType='EQ'><RangeColumns><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></RangeColumns><RangeExpressions><ScalarOperator ScalarString='[@p1]'><Identifier><ColumnReference Column='@p1'/></Identifier></ScalarOperator></RangeExpressions></Prefix></SeekPredicate></SeekPredicates></IndexScan></RelOp></QueryPlan></StmtSimple></Statements></Batch></BatchSequence></ShowPlanXML>"
            q.Hint = "option(use plan N'" & s.Replace("'", """") & "')"
            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2M()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmdBase(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmdBase(e)

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            For Each o As Entity4 In r
                Assert.IsFalse(o.InternalProperties.IsLoaded)
            Next

            q2.propWithLoad = True
            r = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            For Each o As Entity4 In r
                Assert.IsTrue(o.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MFilter()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmdBase(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmdBase(e)
            q2.Filter = Ctor.AutoTypeField("Title").Like("b%")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(1, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmdBase(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmdBase(e)
            q2.propSort = Orm.Sorting.Field("Title")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestTypeless()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t As SourceFragment = New SourceFragment("dbo", "guid_table")
            Dim q As New QueryCmdBase(t)
            q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
                New Aggregate(AggregateFunction.Count) _
            })

            q.Aggregates(0).Alias = "cnt"
            q.GroupBy(New OrmProperty() {New OrmProperty(t, "code")}). _
            Select(New OrmProperty() {New OrmProperty(t, "code", "Code")}).Sort(Sorting.Custom("cnt desc"))

            Dim l As IList(Of Worm.Orm.AnonymousEntity) = q.ToAnonymList(Of Worm.Orm.AnonymousEntity)(mgr)

            Assert.AreEqual(5, l.Count)

            Assert.AreEqual(5, l(0)("Code"))
            Assert.AreEqual(2, l(0)("cnt"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestRowNumber()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New MSSQL2005Generator("1"))
            Dim q As New QueryCmdBase(GetType(Entity))
            q.RowNumberFilter = New TableFilter(QueryCmdBase.RowNumerColumn, New ScalarValue(2), Worm.Criteria.FilterOperation.LessEqualThan)
            Dim l As ReadOnlyEntityList(Of Entity) = q.ToEntityList(Of Entity)(mgr)
            Assert.AreEqual(2, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestInterface()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmdBase = New QueryCmdBase(GetType(Entity))
            Dim r As IList(Of IEnt) = q.ToList(Of IEnt)(mgr)

            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q = New QueryCmdBase()
            r = q.ToList(Of Entity, IEnt)(mgr)
            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q = New QueryCmdBase(GetType(Entity))
            Dim r2 As IList(Of Entity) = q.ToList(Of Entity)(mgr)
            Assert.IsNotNull(r2)
            Assert.AreEqual(13, r2.Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            'q.SelectedType = GetType(Entity)
            'Dim r As List(Of Entity)
            'Dim r2 As List(Of IEnt) = r
            'r2(0) = r(0)
        End Using
    End Sub

    <TestMethod()> Public Sub TestAutoMgr()
        Dim q As QueryCmdBase = New QueryCmdBase(GetType(Entity4)).Sort(Orm.Sorting.Field("Title"))

        Dim l As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(Function() TestManager.CreateManager(New SQLGenerator("1")))

        Assert.AreEqual(12, l.Count)

        Assert.IsFalse(l(0).IsLoaded)
        Assert.AreEqual("245g0nj", l(0).Title)
        Assert.IsTrue(l(0).IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestRenew()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmdBase = New QueryCmdBase(GetType(Entity4))
            q.ToEntityList(Of Entity4)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToEntityList(Of Entity4)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q.Renew(Of Entity4)(mgr)
            q.ToEntityList(Of Entity4)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestAnonym()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t As SourceFragment = New SourceFragment("dbo", "ent2")

            'GetType(Worm.Orm.AnonymousEntity)
            Dim q As QueryCmdBase = New QueryCmdBase(t)

            'q.From(t). _
            q.Select(New OrmProperty() {New OrmProperty(t, "id", "pk"), New OrmProperty(t, "name", "Title")}). _
            Where(Ctor.Column(t, "id").GreaterThan(5)). _
            Sort(Sorting.Field("Title"))

            Dim l As IList(Of Worm.Orm.AnonymousEntity) = q.ToAnonymList(Of Worm.Orm.AnonymousEntity)(mgr)

            Assert.AreEqual(7, l.Count)

            Assert.AreEqual("2gwrbwrb", l(0)("Title"))

            q.Where(Ctor.AutoTypeField("pk").GreaterThan(5))

            'l = q.ToAnonymList(Of Worm.Orm.AnonymousEntity)(mgr)

            'Assert.AreEqual(7, l.Count)
        End Using
    End Sub
End Class