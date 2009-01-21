Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database
Imports Worm
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports System.Collections
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports System.Runtime.Serialization.Formatters.Binary

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
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyEntityList(Of Entity) = q.ToList(Of Entity)(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterFromRawTableWrong()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim t As SourceFragment = mgr.MappingEngine.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmd()
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Entities.SelectExpression)( _
                New Entities.SelectExpression() { _
                    New Entities.SelectExpression(t, "id", "ID") _
                })
            q.From(t)
            Assert.IsNotNull(q)

            q.Filter = Ctor.prop(GetType(Entity4), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterFromRawTable()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim t As SourceFragment = mgr.MappingEngine.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmd()
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Entities.SelectExpression)( _
                New Entities.SelectExpression() { _
                    New Entities.SelectExpression(t, "id", "ID") _
                })
            q.From(t)
            Assert.IsNotNull(q)

            Dim c As New Condition.ConditionConstructor
            c.AddFilter(New TableFilter(t, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

            q.Filter = c.Condition

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyEntityList(Of Entity) = q.ToList(Of Entity)(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

    <TestMethod()> Public Sub TestSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd().Select(GetType(Entity4)). _
                Where(Ctor.prop(GetType(Entity4), "Title").[like]("b%")). _
                Sort(SCtor.prop(GetType(Entity4), "ID"))

            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Assert.AreEqual(3, r(0).ID)

            q.propSort = SCtor.prop(GetType(Entity4), "ID").desc
            r = q.ToList(Of Entity4)(mgr)

            Assert.AreEqual(12, r(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestSort2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.Filter = Ctor.prop(GetType(Entity4), "Title").like("b%")
            q.propSort = SCtor.prop(GetType(Entity4), "ID")
            q.Select(GetType(Entity4))
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Assert.AreEqual(3, r(0).ID)

            'q.propSort.Order = Orm.SortType.Desc
            'r = q.ToEntityList(Of Entity4)(mgr)

            'Assert.AreEqual(12, r(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity5 As Type = GetType(Entity)
            Dim q As New QueryCmd()
            q.Select(t_entity4)
            Dim jf As New JoinFilter(t_entity4, "ID", t_entity5, "ID", Worm.Criteria.FilterOperation.Equal)
            q.propJoins = New QueryJoin() {New QueryJoin(t_entity5, Worm.Criteria.Joins.JoinType.Join, jf)}
            q.Select(New SelectExpression() {New SelectExpression(t_entity4, "ID"), New SelectExpression(t_entity4, "Title")})

            q.Sort(SCtor.prop(t_entity4, "Title").desc)

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList(mgr)

            Assert.AreEqual(12, l.Count)

            Assert.AreEqual("wrtbg", l(0)("Title"))
            Assert.AreEqual("wrbwrb", l(1)("Title"))

            Assert.AreEqual(2, l(0)("ID"))
            Assert.AreEqual(7, l(1)("ID"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity As Type = GetType(Entity)
            Dim t_entity5 As Type = GetType(Entity5)
            Dim q As New QueryCmd()
            q.Select(t_entity4)
            q.propJoins = JCtor.join(t_entity).[on](t_entity4, "ID").eq(t_entity, "ID")

            Assert.AreEqual(12, q.ToAnonymList(mgr).Count)

            q.propJoins = JCtor.join(t_entity).[on](t_entity4, "ID").eq(t_entity, "ID").join(t_entity5).[on](t_entity, "ID").eq(t_entity5, "ID")

            Assert.AreEqual(3, q.ToAnonymList(mgr).Count)

            q.propJoins = JCtor.join(t_entity).[on](t_entity4, "ID").eq(t_entity, "ID").left_join(t_entity5).[on](t_entity, "ID").eq(t_entity5, "ID")

            Assert.AreEqual(12, q.ToAnonymList(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MJoinAuto()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity As Type = GetType(Entity)
            Dim t_entity5 As Type = GetType(Entity5)

            Dim q As New QueryCmd()
            q = q.Where(Ctor.prop(t_entity4, "Title").[like]("%b")). _
                Select(FCtor.prop(t_entity, "ID").prop(t_entity4, "Title"))
            q.AutoJoins = True

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToObjectList(Of AnonymousEntity)(mgr)

            Assert.AreEqual(17, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MJoin()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity As Type = GetType(Entity)
            Dim t_entity5 As Type = GetType(Entity5)

            Dim q As New QueryCmd()
            q.propJoins = New QueryJoin() {New QueryJoin(t_entity4, Worm.Criteria.Joins.JoinType.Join, t_entity)}
            q = q.Where(Ctor.prop(t_entity4, "Title").[like]("%b")). _
                Select(FCtor.prop(t_entity, "ID").prop(t_entity4, "Title"))

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToObjectList(Of AnonymousEntity)(mgr)

            Assert.AreEqual(17, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MExists()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))
            Dim t1 As Type = GetType(Table1)
            'Dim t3 As Type = GetType(Table3)

            Dim q As New QueryCmd()
            q = q.Where(Ctor.exists( _
                        New QueryCmd().Select("Table3"). _
                            Join(JCtor.join(t1).onM2M("Table3")). _
                            Where(Ctor.prop("Table3", "Code").eq(2)))).Select(t1)

            Dim l As ReadOnlyList(Of Table1) = q.ToOrmList(Of Table1)(mgr)

            Assert.AreEqual(1, l.Count)

            Assert.AreEqual(1, l(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestDistinct()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))
            Dim t1 As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)

            Dim q As QueryCmd = t1.Relations.GetCmd(GetType(Table33))

            Assert.AreEqual(3, q.ToList(Of Table3)(mgr).Count)

            Assert.AreEqual(2, q.Distinct(True).ToList(Of Table3)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestHaving()
        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Type = GetType(Table1)

        q.Select(FCtor.prop(t, "EnumStr")).GroupBy(FCtor.prop(t, "EnumStr")) _
            .Having(Ctor.count().eq(2))

        Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList

        Assert.AreEqual(1, l.Count)

        Assert.AreEqual(Enum1.sec.ToString, l(0)("EnumStr"))
    End Sub

    <TestMethod()> Public Sub TestTop()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim q As New QueryCmd()
            q.Select(GetType(Entity4))
            q.Filter = Ctor.prop(GetType(Entity4), "Title").[like]("b%")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)

            Assert.AreEqual(3, r.Count)
            Dim m As Guid = q.Mark

            q.propTop = New Top(2)
            Assert.AreNotEqual(m, q.Mark)

            r = q.ToList(Of Entity4)(mgr)

            Assert.AreEqual(2, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestHint()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)
            Assert.IsNotNull(q)

            Dim s As String = "<ShowPlanXML xmlns='http://schemas.microsoft.com/sqlserver/2004/07/showplan' Version='1.0' Build='9.00.3042.00'><BatchSequence><Batch><Statements><StmtSimple StatementText='declare @p1 Int;set @p1 = 1&#xd;' StatementId='1' StatementCompId='1' StatementType='ASSIGN'/><StmtSimple StatementText='&#xa;select t1.id from dbo.ent1 t1 where t1.id = @p1&#xd;&#xa;' StatementId='2' StatementCompId='2' StatementType='SELECT' StatementSubTreeCost='0.0032831' StatementEstRows='1' StatementOptmLevel='TRIVIAL'><StatementSetOptions QUOTED_IDENTIFIER='false' ARITHABORT='true' CONCAT_NULL_YIELDS_NULL='false' ANSI_NULLS='false' ANSI_PADDING='false' ANSI_WARNINGS='false' NUMERIC_ROUNDABORT='false'/><QueryPlan CachedPlanSize='8' CompileTime='0' CompileCPU='0' CompileMemory='72'><RelOp NodeId='0' PhysicalOp='Clustered Index Seek' LogicalOp='Clustered Index Seek' EstimateRows='1' EstimateIO='0.003125' EstimateCPU='0.0001581' AvgRowSize='11' EstimatedTotalSubtreeCost='0.0032831' Parallel='0' EstimateRebinds='0' EstimateRewinds='0'><OutputList><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></OutputList><IndexScan Ordered='1' ScanDirection='FORWARD' ForcedIndex='0' NoExpandHint='0'><DefinedValues><DefinedValue><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></DefinedValue></DefinedValues><Object Schema='[dbo]' Table='[ent1]' Index='[PK_ent1]' Alias='[t1]'/><SeekPredicates><SeekPredicate><Prefix ScanType='EQ'><RangeColumns><ColumnReference Schema='[dbo]' Table='[ent1]' Alias='[t1]' Column='id'/></RangeColumns><RangeExpressions><ScalarOperator ScalarString='[@p1]'><Identifier><ColumnReference Column='@p1'/></Identifier></ScalarOperator></RangeExpressions></Prefix></SeekPredicate></SeekPredicates></IndexScan></RelOp></QueryPlan></StmtSimple></Statements></Batch></BatchSequence></ShowPlanXML>"
            q.Hint = "option(use plan N'" & s.Replace("'", """") & "')"
            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2M()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New RelationCmd(e)

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            For Each o As Entity4 In r
                Assert.IsFalse(o.InternalProperties.IsLoaded)
            Next

            r = q2.Select(GetType(Entity4), True).ToList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            For Each o As Entity4 In r
                Assert.IsTrue(o.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> Public Sub TestM2M2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim e As Entity = mgr.GetOrmBaseFromCacheOrDB(Of Entity)(1)
            Assert.IsNotNull(e)

            Dim q As New RelationCmd(e)

            Dim q2 As QueryCmd = New RelationCmd(e). _
                Where(Ctor.prop(GetType(Entity4), "Title").eq("first"))

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Dim r2 As ReadOnlyEntityList(Of Entity4) = q2.ToList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            Assert.AreEqual(1, r2.Count)

            For Each o As Entity4 In r
                Assert.IsFalse(o.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MFilter()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New RelationCmd(e)
            q2.Filter = Ctor.prop(GetType(Entity4), "Title").[like]("b%")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToList(Of Entity4)(mgr)

            Assert.AreEqual(1, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))

            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New RelationCmd(e)
            q2.propSort = SCtor.prop(GetType(Entity4), "Title")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestTypeless()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As SourceFragment = New SourceFragment("dbo", "guid_table")
            Dim q As New QueryCmd()
            q.From(t)
            'q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
            '    New Aggregate(AggregateFunction.Count, "cnt") _
            '})

            'q.GroupBy(New OrmProperty() {New OrmProperty(t, "code")}). _
            'Select(New OrmProperty() {New OrmProperty(t, "code", "Code")}).Sort(Sorting.Custom("cnt desc"))

            q.Select(FCtor.column(t, "code", "Code").count("cnt")). _
                GroupBy(FCtor.column(t, "code")). _
                Sort(SCtor.custom("cnt desc"))

            'Assert.IsNull(q.SelectedType)
            Assert.IsNull(q.CreateType)

            Dim l As IList(Of Worm.Entities.AnonymousEntity) = q.ToObjectList(Of Worm.Entities.AnonymousEntity)(mgr)

            'Assert.IsNull(q.SelectedType)
            Assert.IsNull(q.CreateType)

            Assert.AreEqual(5, l.Count)

            Assert.AreEqual(5, l(0)("Code"))
            Assert.AreEqual(2, l(0)("cnt"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestRowNumber()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            Dim q As New QueryCmd()
            q.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(2), Worm.Criteria.FilterOperation.LessEqualThan)
            q.Select(GetType(Entity))
            Dim l As ReadOnlyEntityList(Of Entity) = q.ToList(Of Entity)(mgr)
            Assert.AreEqual(2, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestRowNumber2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            Dim q As New QueryCmd()
            q.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(2), Worm.Criteria.FilterOperation.LessEqualThan)
            q.Select(GetType(Entity4), True)
            Dim l As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Assert.AreEqual(2, l.Count)

            For Each e As Entity4 In l
                Assert.IsTrue(e.InternalProperties.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod()> Public Sub TestInterface()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd()
            q.Select(GetType(Entity))
            Dim r As IList(Of IEnt) = q.ToEntityList(Of IEnt)(mgr)

            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Assert.IsTrue(r(0).IsPKLoaded)

            q = New QueryCmd()
            r = q.ToList(Of Entity, IEnt)(mgr)
            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q = New QueryCmd()
            q.Select(GetType(Entity))
            Dim r2 As IList(Of Entity) = q.ToEntityList(Of Entity)(mgr)
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
        Dim q As QueryCmd = New QueryCmd().Select(GetType(Entity4)).Sort(SCtor.prop(GetType(Entity4), "Title"))

        Dim l As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)( _
            Function() TestManager.CreateManager(New ObjectMappingEngine("1")))

        Assert.AreEqual(12, l.Count)

        Assert.IsFalse(l(0).InternalProperties.IsLoaded)
        Assert.AreEqual("245g0nj", l(0).Title)
        Assert.IsTrue(l(0).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestRenew()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd()
            q.Select(GetType(Entity4))
            q.ToList(Of Entity4)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Assert.IsTrue(q.IsInCache(mgr))

            q.ToList(Of Entity4)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q.RenewCache(mgr, True)
            q.ToList(Of Entity4)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestRenew2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd()
            q.Select(GetType(Entity4))

            Assert.IsFalse(q.IsInCache(mgr))

            q.ToList(Of Entity4)(mgr)

            Assert.IsTrue(q.IsInCache(mgr))
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Entity4)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Assert.IsTrue(q.IsInCache(mgr))

        End Using
    End Sub

    <TestMethod()> Public Sub TestAnonym()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As SourceFragment = New SourceFragment("dbo", "ent2")

            'GetType(Worm.Orm.AnonymousEntity)
            Dim q As QueryCmd = New QueryCmd().From(t)

            'q.From(t). _
            q.Select(New SelectExpression() {New SelectExpression(t, "id", "pk"), New SelectExpression(t, "name", "Title")}). _
            Where(Ctor.column(t, "id").greater_than(5)). _
            Sort(SCtor.column(t, "name"))

            Dim l As IList(Of Worm.Entities.AnonymousEntity) = q.ToObjectList(Of Worm.Entities.AnonymousEntity)(mgr)

            Assert.AreEqual(7, l.Count)

            Assert.AreEqual("2gwrbwrb", l(0)("Title"))

            'q.Where(Ctor.Field(GetType(Tab),"pk").GreaterThan(5))

            'l = q.ToAnonymList(Of Worm.Orm.AnonymousEntity)(mgr)

            'Assert.AreEqual(7, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestEntity()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd().Select(GetType(NonCache)). _
            Where(Ctor.prop(GetType(NonCache), "Code").eq(5))

            Dim l As ReadOnlyObjectList(Of NonCache) = q.ToObjectList(Of NonCache)(mgr)

            Assert.AreEqual(2, l.Count)

            Assert.IsTrue(CType(l(0), IEntity).IsLoaded)

            Assert.IsTrue(CType(l(0), IEntity).IsPropertyLoaded("Code"))
            Assert.IsTrue(CType(l(0), IEntity).IsPropertyLoaded("ID"))

            Assert.AreEqual(5, l(0).Code)
            Assert.AreEqual(5, l(1).Code)

            l = q.ToObjectList(Of NonCache)(mgr)

            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q.ClearCache(mgr)

            l = q.ToObjectList(Of NonCache)(mgr)

            Assert.IsFalse(q.LastExecitionResult.CacheHit)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCache()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.Select(GetType(Entity))

            Assert.AreEqual(13, q.ToList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Dim q2 As New QueryCmd()
            q2.Select(GetType(Entity4))

            Assert.AreEqual(12, q2.ToList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)

            Assert.AreEqual(13, q.ToList(Of Entity)(mgr).Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            Assert.AreEqual(12, q2.ToList(Of Entity4)(mgr).Count)
            Assert.IsTrue(q2.LastExecitionResult.CacheHit)

            q.DontCache = True
            Assert.AreEqual(13, q.ToList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            q.DontCache = False

            q2.DontCache = True
            Assert.AreEqual(12, q2.ToList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)
            q2.DontCache = False

            q.LiveTime = TimeSpan.FromSeconds(5)
            Assert.AreEqual(13, q.ToList(Of Entity)(mgr).Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Threading.Thread.Sleep(5100)
            Assert.AreEqual(13, q.ToList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q2.LiveTime = TimeSpan.FromSeconds(5)
            Assert.AreEqual(12, q2.ToList(Of Entity4)(mgr).Count)
            Assert.IsTrue(q2.LastExecitionResult.CacheHit)
            Threading.Thread.Sleep(5100)
            Assert.AreEqual(12, q2.ToList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)

            Dim bf As New BinaryFormatter
            Using ms As New IO.MemoryStream
                For Each s As String In mgr.Cache.GetAllKeys
                    bf.Serialize(ms, mgr.Cache.GetQueryDictionary(s))
                Next
                Diagnostics.Debug.WriteLine(ms.ToArray.Length)
            End Using

            Using ms As New IO.MemoryStream
                bf.Serialize(ms, q)
                Diagnostics.Debug.WriteLine(ms.ToArray.Length)
            End Using

            'Dim bf2 As New BinaryFormatter
            'Using ms2 As New IO.MemoryStream
            '    bf2.Serialize(ms2, New Entity2())
            'End Using
        End Using
    End Sub

    Protected Function createdic(ByVal dic As IDictionary, ByVal mark As String) As IDictionary
        Dim k As IDictionary = New Hashtable
        dic(mark) = k
        Return k
    End Function

    <TestMethod()> Public Sub TestCacheMark()
        Dim dic As New Hashtable

        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Cache.OrmCache( _
            Function(mark As String) If(dic.Contains(mark), CType(dic(mark), IDictionary), createdic(dic, mark))), _
            New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            q.ExternalCacheMark = "ldgn"

            q.ToList(Of Entity)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Assert.AreEqual(1, dic.Count)

            q.ToList(Of Entity)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            'q.ToEntityList(Of Entity)(mgr)
            'Assert.IsFalse(q.LastExecitionResult.CacheHit)

            'Assert.AreEqual(2, dic.Count)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubquery()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))
            Dim tt2 As Type = GetType(Table2)

            Dim q As QueryCmd = New QueryCmd(). _
                Where(New Ctor(tt2).prop("Table1").exists(GetType(Table1))).Select(tt2)

            Assert.AreEqual(2, q.ToList(Of Table2)(mgr).Count)

            q.Where(New Ctor(tt2).prop("Table1").not_exists(GetType(Table1)))

            Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)

            q.Where(Ctor.prop(tt2, "Table1").not_exists(GetType(Table1), _
                Ctor.prop(GetType(Table1), "Code").eq(45). _
                [and]( _
                    Ctor.prop(tt2, "Table1").eq(GetType(Table1), "Enum") _
                )))

            Assert.AreEqual(2, q.ToList(Of Table2)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubqueryCmd()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))
            Dim tt1 As Type = GetType(Table1)
            Dim tt2 As Type = GetType(Table2)

            Dim q As QueryCmd = New QueryCmd(). _
                Where(New Ctor(tt2).prop("Table1").exists(GetType(Table1))).Select(tt2)

            Dim cq As QueryCmd = New QueryCmd(). _
                Where(Ctor.prop(tt2, "Table1").eq(tt1, "Enum").[and]( _
                      Ctor.prop(tt1, "Code").eq(45))).Select(tt1)

            q.Where(New NonTemplateUnaryFilter(New SubQueryCmd(cq), Worm.Criteria.FilterOperation.NotExists))

            Assert.AreEqual(2, q.ToList(Of Table2)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table2)(mgr)

            Assert.IsTrue(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestWrapper()
        Dim t As New TestManager

        'Dim q As OrmQueryCmd(Of Entity) = OrmQueryCmd(Of Entity).Create(t)

        Dim q As New OrmQueryCmd(Of Entity)(t)
        q.Select(GetType(Entity))

        Dim r As ReadOnlyList(Of Entity) = q

        Assert.AreEqual(13, r.Count)
    End Sub

    <TestMethod()> Public Sub TestWrapper2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            'Dim q As OrmQueryCmd(Of Entity) = QueryCmd.Create(GetType(Entity)).GetOrmCommand(Of Entity)(mgr)
            Dim q As OrmQueryCmd(Of Entity) = QueryCmd.CreateAndGetOrmCommand(Of Entity)(mgr)

            Dim r As ReadOnlyList(Of Entity) = q

            Assert.AreEqual(13, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestColumns()

        Dim t As Type = GetType(Table1)

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.prop(t, "Code").prop(t, "Title"))

        Dim l As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()

        Assert.IsFalse(l(0).InternalProperties.IsLoaded)
        Assert.IsTrue(l(0).InternalProperties.IsPropertyLoaded("Code"))
        Assert.IsFalse(l(0).InternalProperties.IsPropertyLoaded("Enum"))

    End Sub

    <TestMethod()> Public Sub TestCachedAnonym()

        Dim t As New SourceFragment("dbo", "table1")
        Dim cache As New Worm.Cache.ReadonlyCache

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache)))
        q.From(t)
        q.Select(FCtor.column(t, "code", "Code", Field2DbRelations.PK).column(t, "name", "Title"))

        Dim r As ReadOnlyEntityList(Of AnonymousCachedEntity) = q.ToEntityList(Of AnonymousCachedEntity)()

        Assert.IsTrue(r.Count > 0)


        Assert.Inconclusive()
    End Sub

    Public Class cls

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _title As String
        Public Property Title() As String
            Get
                Return _title
            End Get
            Set(ByVal value As String)
                _title = value
            End Set
        End Property

        Private _id As Integer
        Public Property ID() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    Public Class cls2

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _name As String
        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(ByVal value As String)
                _name = value
            End Set
        End Property

        Private _id As Integer
        Public Property Id() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    Public Class cls3

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _title As String
        <EntityProperty("name")> Public Property Title() As String
            Get
                Return _title
            End Get
            Set(ByVal value As String)
                _title = value
            End Set
        End Property

        Private _id As Integer
        Public Property Id() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    <TestMethod()> Public Sub TestCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID")). _
            Sort(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l As IList(Of cls) = q.ToPODList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCustomObject2()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Sort(SCtor.prop(GetType(cls2), "Code")).From(t)

        Dim l As IList(Of cls2) = q.ToPODList(Of cls2)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCustomObject3()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Sort(SCtor.prop(GetType(cls3), "Code")).From(t)

        Dim l As IList(Of cls3) = q.ToPODList(Of cls3)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)). _
            Sort(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l As IList(Of cls) = q.ToPODList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsTrue(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObject2()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)). _
            Sort(SCtor.prop(GetType(cls), "Code"))

        Dim l As IList(Of cls) = q.ToPODList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsTrue(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestExternalCache()
        Dim dic As New Hashtable

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        AddHandler q.ExternalDictionary, AddressOf New c(dic).Ext

        Assert.AreEqual(0, dic.Count)

        Dim l As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()

        Assert.AreEqual(1, dic.Count)
    End Sub

    <TestMethod()> Public Sub TestClientPaging()
        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.Select(GetType(Table1), True)
        q.ClientPaging = New Worm.Query.Paging(0, 1)

        Dim l As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()

        Assert.AreEqual(1, l.Count)
        Assert.IsTrue(l(0).InternalProperties.IsLoaded)

    End Sub

    <TestMethod()> Public Sub TestSelectEntity()

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q.Select("Table3")

        Dim t As Type = GetType(Table3)
        q.Select(FCtor.prop(t, "Code"))

        Assert.IsTrue(q.ToList(Of Table33).Count > 0)
    End Sub

    Private Class c
        Private _dic As IDictionary
        Public Sub New(ByVal dic As IDictionary)
            _dic = dic
        End Sub

        Public Sub Ext(ByVal sender As QueryCmd, ByVal args As QueryCmd.ExternalDictionaryEventArgs)
            args.Dictionary = _dic
        End Sub
    End Class

    <TestMethod()> Public Sub TestSingle()
        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table10 = q.Where(Ctor.prop(GetType(Table10), "ID").eq(3)).Single(Of Table10)()

        Assert.AreEqual(2, t.Tbl.ID)
        Assert.AreEqual("second", t.Tbl.Name)
    End Sub

    <TestMethod()> _
    Public Sub TestGetMgr()
        Dim cache As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Using mgr As OrmManager = q.GetMgr.CreateManager
            Dim t As Table1 = mgr.GetOrmBaseFromCacheOrCreate(Of Table1)(1)

            Assert.IsFalse(t.InternalProperties.IsLoaded)
            Assert.IsTrue(mgr.IsInCachePrecise(t))
        End Using

        Dim l As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()

        For Each t As Table1 In l
            Assert.IsFalse(t.InternalProperties.IsLoaded)
            Assert.IsNotNull(t.Name)
            Assert.IsTrue(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestFromInto()

        Dim cache As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As New SourceFragment("dbo", "table1")

        q.From(t).Into(GetType(Table1)).Select(FCtor.column(t, "id", "ID"))

        Dim r As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestFromInto2()

        Dim cache As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As New SourceFragment("dbo", "table1")

        q.From(t).Select(FCtor.column(t, "id", "ID", GetType(Table1)).column(t, "enum", "ID", GetType(Table2)))

        Dim r As ReadonlyMatrix = q.ToMatrix

        Assert.AreEqual(3, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestFromInto3()

        Dim cache As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As New SourceFragment("dbo", "table1")

        q.From(t).Select(FCtor.column(t, "id", "ID", GetType(Table1)). _
            custom("ID", GetType(Table2), "case when {0} = 2 then 1 else {0} end", New FieldReference(t, "enum")))

        Dim r As ReadonlyMatrix = q.ToMatrix

        Assert.AreEqual(3, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestUnion()
        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q1.Select(FCtor.prop(GetType(Table1), "ID")).UnionAll( _
            New QueryCmd().Select(FCtor.prop(GetType(Table2), "ID")))

        Dim l As ReadOnlyEntityList(Of Table1) = q1.ToList(Of Table1)()

        Assert.AreEqual(5, l.Count)

        For Each t As Table1 In l
            t.EnsureLoaded()
            If t.ID = 4 Then
                Assert.IsFalse(t.InternalProperties.IsLoaded)
            Else
                Assert.IsTrue(t.InternalProperties.IsLoaded)
            End If
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestHasJoins()
        Dim q As New QueryCmd(Function() TestManager.CreateManager(New ObjectMappingEngine("1")))

        Dim e As Entity = q.Where(Ctor.prop(GetType(Entity2), "ID").eq(2)).Single(Of Entity)()

        Assert.IsNotNull(e)

        Assert.IsInstanceOfType(e, GetType(Entity))

        Assert.AreEqual(2, e.ID)
    End Sub

    Class myom
        Inherits SQLGenerator

        Public Overrides ReadOnly Property IncludeCallStack() As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

    <TestMethod()> _
    Public Sub TestStack()

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), New ReadonlyCache, New myom))

        Dim t As Table10 = q.Where(Ctor.prop(GetType(Table10), "ID").eq(3)).Single(Of Table10)()
    End Sub

    <TestMethod()> _
    Public Sub TestGetById()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Assert.IsNotNull(t)
        Assert.AreEqual(0, q.ExecCount)
        Assert.IsFalse(t.InternalProperties.IsLoaded)

        t.Load()

        Assert.IsTrue(t.InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> _
    Public Sub TestGetById2()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim t As Table1 = q.GetByID(Of Table1)(1, True)

        Assert.IsNotNull(t)
        Assert.AreEqual(0, q.ExecCount)
        Assert.IsTrue(t.InternalProperties.IsLoaded)

        t = q.GetByID(Of Table1)(1)
        Assert.AreEqual(0, q.ExecCount)
    End Sub

    <TestMethod()> _
    Public Sub TestGetById3()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim t As Table1 = q.GetByID(Of Table1)(-59871)

        Assert.IsNotNull(t)
        Assert.AreEqual(0, q.ExecCount)
        Assert.IsFalse(t.InternalProperties.IsLoaded)

        't = q.GetByID(Of Table1)(1)
        'Assert.AreEqual(0, q.ExecCount)
    End Sub

    <TestMethod()> _
    Public Sub TestGetById4()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim t As Table1 = q.GetByID(Of Table1)(-59871, True)

        Assert.IsNull(t)

        't = q.GetByID(Of Table1)(1)
        'Assert.AreEqual(0, q.ExecCount)
    End Sub
End Class
