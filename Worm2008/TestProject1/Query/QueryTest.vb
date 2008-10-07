Imports System
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
Imports Worm.Database.Criteria.Joins

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
            Dim q As New QueryCmd(GetType(Entity))
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

    <TestMethod(), ExpectedException(GetType(Worm.ObjectMappingException))> Public Sub TestFilterFromRawTableWrong()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As SourceFragment = mgr.MappingEngine.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmd(t)
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Orm.SelectExpression)( _
                New Orm.SelectExpression() { _
                    New Orm.SelectExpression(t, "id", "ID") _
                })

            Assert.IsNotNull(q)

            q.Filter = Ctor.AutoTypeField("ID").Eq(1)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterFromRawTable()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As SourceFragment = mgr.MappingEngine.GetTables(GetType(Entity4))(0)
            Dim q As New QueryCmd(t)
            q.SelectList = New System.Collections.ObjectModel.ReadOnlyCollection(Of Orm.SelectExpression)( _
                New Orm.SelectExpression() { _
                    New Orm.SelectExpression(t, "id", "ID") _
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
            Dim q As QueryCmd = New QueryCmd(GetType(Entity4)). _
                Where(Ctor.AutoTypeField("Title").Like("b%")). _
                Sort(Worm.Orm.Sorting.Field("ID"))

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
            Dim q As New QueryCmd(GetType(Entity4))
            q.Filter = Ctor.AutoTypeField("Title").Like("b%")
            q.propSort = Worm.Orm.Sorting.Field("ID")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)
            Assert.AreEqual(3, r(0).ID)

            'q.propSort.Order = Orm.SortType.Desc
            'r = q.ToEntityList(Of Entity4)(mgr)

            'Assert.AreEqual(12, r(0).ID)
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity5 As Type = GetType(Entity)
            Dim q As New QueryCmd(t_entity4)
            Dim jf As New JoinFilter(t_entity4, "ID", t_entity5, "ID", Worm.Criteria.FilterOperation.Equal)
            q.Joins = New OrmJoin() {New OrmJoin(t_entity5, Worm.Criteria.Joins.JoinType.Join, jf)}
            q.Select(New SelectExpression() {New SelectExpression(t_entity4, "ID"), New SelectExpression(t_entity4, "Title")})

            q.Sort(Sorting.Field(t_entity4, "Title").Desc)

            q.CreateType = GetType(AnonymousEntity)
            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToObjectList(Of AnonymousEntity)(mgr)

            Assert.AreEqual(12, l.Count)

            Assert.AreEqual("wrtbg", l(0)("Title"))
            Assert.AreEqual("wrbwrb", l(1)("Title"))

            Assert.AreEqual(2, l(0)("ID"))
            Assert.AreEqual(7, l(1)("ID"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t_entity4 As Type = GetType(Entity4)
            Dim t_entity As Type = GetType(Entity)
            Dim t_entity5 As Type = GetType(Entity5)
            Dim q As New QueryCmd(t_entity4)
            q.Joins = JCtor.Join(t_entity).On(t_entity4, "ID").Eq(t_entity, "ID")
            q.CreateType = GetType(AnonymousEntity)

            Assert.AreEqual(12, q.ToObjectList(Of AnonymousEntity)(mgr).Count)

            q.Joins = JCtor.Join(t_entity).On(t_entity4, "ID").Eq(t_entity, "ID").Join(t_entity5).On(t_entity, "ID").Eq(t_entity5, "ID")

            Assert.AreEqual(3, q.ToObjectList(Of AnonymousEntity)(mgr).Count)

            q.Joins = JCtor.Join(t_entity).On(t_entity4, "ID").Eq(t_entity, "ID").LeftJoin(t_entity5).On(t_entity, "ID").Eq(t_entity5, "ID")

            Assert.AreEqual(12, q.ToObjectList(Of AnonymousEntity)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestDistinct()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)

            Dim q As QueryCmd = t1.M2MNew.Find(GetType(Table33))

            Assert.AreEqual(3, q.ToOrmList(Of Table3)(mgr).Count)

            Assert.AreEqual(2, q.Distinct(True).ToOrmList(Of Table3)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestInner()
        Assert.Inconclusive()
    End Sub

    <TestMethod()> Public Sub TestHaving()
        Assert.Inconclusive()
    End Sub

    <TestMethod()> Public Sub TestTop()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Entity4))
            q.Filter = Ctor.AutoTypeField("Title").Like("b%")
            Assert.IsNotNull(q)

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(3, r.Count)
            Dim m As Guid = q.Mark

            q.propTop = New Top(2)
            Assert.AreNotEqual(m, q.Mark)

            r = q.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(2, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestHint()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Entity))
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

            Dim q As New QueryCmd(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmd(e)

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

            Dim q As New QueryCmd(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmd(e)
            q2.Filter = Ctor.AutoTypeField("Title").Like("b%")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(1, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Entity))
            q.Filter = Ctor.AutoTypeField("ID").Eq(1)
            Assert.IsNotNull(q)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Assert.IsNotNull(e)

            Dim q2 As New QueryCmd(e)
            q2.propSort = Orm.Sorting.Field("Title")

            Dim r As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestTypeless()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim t As SourceFragment = New SourceFragment("dbo", "guid_table")
            Dim q As New QueryCmd(t)
            'q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() { _
            '    New Aggregate(AggregateFunction.Count, "cnt") _
            '})

            'q.GroupBy(New OrmProperty() {New OrmProperty(t, "code")}). _
            'Select(New OrmProperty() {New OrmProperty(t, "code", "Code")}).Sort(Sorting.Custom("cnt desc"))

            q.GroupBy(FCtor.Column(t, "code")). _
                Select(FCtor.Column(t, "code", "Code")). _
                Sort(Sorting.Custom("cnt desc")). _
                SelectAgg(AggCtor.Count("cnt"))

            Dim l As IList(Of Worm.Orm.AnonymousEntity) = q.ToObjectList(Of Worm.Orm.AnonymousEntity)(mgr)

            Assert.AreEqual(5, l.Count)

            Assert.AreEqual(5, l(0)("Code"))
            Assert.AreEqual(2, l(0)("cnt"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestRowNumber()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New MSSQL2005Generator("1"))
            Dim q As New QueryCmd(GetType(Entity))
            q.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(2), Worm.Criteria.FilterOperation.LessEqualThan)
            Dim l As ReadOnlyEntityList(Of Entity) = q.ToEntityList(Of Entity)(mgr)
            Assert.AreEqual(2, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestInterface()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmd = New QueryCmd(GetType(Entity))
            Dim r As IList(Of IEnt) = q.ToList(Of IEnt)(mgr)

            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Assert.IsTrue(r(0).IsPKLoaded)

            q = New QueryCmd()
            r = q.ToList(Of Entity, IEnt)(mgr)
            Assert.IsNotNull(r)
            Assert.AreEqual(13, r.Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q = New QueryCmd(GetType(Entity))
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
        Dim q As QueryCmd = New QueryCmd(GetType(Entity4)).Sort(Orm.Sorting.Field("Title"))

        Dim l As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(Function() TestManager.CreateManager(New SQLGenerator("1")))

        Assert.AreEqual(12, l.Count)

        Assert.IsFalse(l(0).InternalProperties.IsLoaded)
        Assert.AreEqual("245g0nj", l(0).Title)
        Assert.IsTrue(l(0).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestRenew()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmd = New QueryCmd(GetType(Entity4))
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
            Dim q As QueryCmd = New QueryCmd(t)

            'q.From(t). _
            q.Select(New SelectExpression() {New SelectExpression(t, "id", "pk"), New SelectExpression(t, "name", "Title")}). _
            Where(Ctor.Column(t, "id").GreaterThan(5)). _
            Sort(Sorting.Field("Title"))

            Dim l As IList(Of Worm.Orm.AnonymousEntity) = q.ToObjectList(Of Worm.Orm.AnonymousEntity)(mgr)

            Assert.AreEqual(7, l.Count)

            Assert.AreEqual("2gwrbwrb", l(0)("Title"))

            q.Where(Ctor.AutoTypeField("pk").GreaterThan(5))

            'l = q.ToAnonymList(Of Worm.Orm.AnonymousEntity)(mgr)

            'Assert.AreEqual(7, l.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestEntity()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmd = New QueryCmd(GetType(NonCache)). _
            Where(Ctor.AutoTypeField("Code").Eq(5))

            Dim l As ReadOnlyObjectList(Of NonCache) = q.ToObjectList(Of NonCache)(mgr)

            Assert.AreEqual(2, l.Count)

            Assert.IsTrue(CType(l(0), IEntity).IsLoaded)

            Assert.IsTrue(CType(l(0), IEntity).IsFieldLoaded("Code"))
            Assert.IsTrue(CType(l(0), IEntity).IsFieldLoaded("ID"))

            Assert.AreEqual(5, l(0).Code)
            Assert.AreEqual(5, l(1).Code)

            l = q.ToObjectList(Of NonCache)(mgr)

            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            q.Reset(Of NonCache)(mgr)

            l = q.ToObjectList(Of NonCache)(mgr)

            Assert.IsFalse(q.LastExecitionResult.CacheHit)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCache()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmd(GetType(Entity))

            Assert.AreEqual(13, q.ToEntityList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Dim q2 As New QueryCmd(GetType(Entity4))

            Assert.AreEqual(12, q2.ToEntityList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)

            Assert.AreEqual(13, q.ToEntityList(Of Entity)(mgr).Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            Assert.AreEqual(12, q2.ToEntityList(Of Entity4)(mgr).Count)
            Assert.IsTrue(q2.LastExecitionResult.CacheHit)

            q.DontCache = True
            Assert.AreEqual(13, q.ToEntityList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            q.DontCache = False

            q2.DontCache = True
            Assert.AreEqual(12, q2.ToEntityList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)
            q2.DontCache = False

            q.LiveTime = TimeSpan.FromSeconds(5)
            Assert.AreEqual(13, q.ToEntityList(Of Entity)(mgr).Count)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Threading.Thread.Sleep(5100)
            Assert.AreEqual(13, q.ToEntityList(Of Entity)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q2.LiveTime = TimeSpan.FromSeconds(5)
            Assert.AreEqual(12, q2.ToEntityList(Of Entity4)(mgr).Count)
            Assert.IsTrue(q2.LastExecitionResult.CacheHit)
            Threading.Thread.Sleep(5100)
            Assert.AreEqual(12, q2.ToEntityList(Of Entity4)(mgr).Count)
            Assert.IsFalse(q2.LastExecitionResult.CacheHit)

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
            New SQLGenerator("1"))
            Dim q As New QueryCmd(GetType(Entity))
            q.ExternalCacheMark = "ldgn"

            q.ToEntityList(Of Entity)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Assert.AreEqual(1, dic.Count)

            q.ToEntityList(Of Entity)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            'q.ToEntityList(Of Entity)(mgr)
            'Assert.IsFalse(q.LastExecitionResult.CacheHit)

            'Assert.AreEqual(2, dic.Count)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubquery()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim tt2 As Type = GetType(Table2)

            Dim q As QueryCmd = New QueryCmd(tt2). _
                Where(New Ctor(tt2).Field("Table1").Exists(GetType(Table1)))

            Assert.AreEqual(2, q.ToEntityList(Of Table2)(mgr).Count)

            q.Where(New Ctor(tt2).Field("Table1").NotExists(GetType(Table1)))

            Assert.AreEqual(0, q.ToEntityList(Of Table2)(mgr).Count)

            q.Where(Ctor.Field(tt2, "Table1").NotExists(GetType(Table1), _
                Ctor.Field(GetType(Table1), "Code").Eq(45). _
                And( _
                    JoinCondition.Create(tt2, "Table1").Eq(GetType(Table1), "Enum") _
                )))

            Assert.AreEqual(2, q.ToEntityList(Of Table2)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubqueryCmd()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim tt1 As Type = GetType(Table1)
            Dim tt2 As Type = GetType(Table2)

            Dim q As QueryCmd = New QueryCmd(tt2). _
                Where(New Ctor(tt2).Field("Table1").Exists(GetType(Table1)))

            Dim cq As QueryCmd = New QueryCmd(tt1). _
                Where(JoinCondition.Create(tt2, "Table1").Eq(tt1, "Enum").And( _
                      Ctor.Field(tt1, "Code").Eq(45)))

            q.Where(New NonTemplateUnaryFilter(New Values.SubQueryCmd(cq), Worm.Criteria.FilterOperation.NotExists))

            Assert.AreEqual(2, q.ToEntityList(Of Table2)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToEntityList(Of Table2)(mgr)

            Assert.IsTrue(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestWrapper()
        Dim t As New TestManager

        'Dim q As OrmQueryCmd(Of Entity) = OrmQueryCmd(Of Entity).Create(t)

        Dim q As New OrmQueryCmd(Of Entity)(GetType(Entity), t)

        Dim r As ReadOnlyList(Of Entity) = q

        Assert.AreEqual(13, r.Count)
    End Sub

    <TestMethod()> Public Sub TestWrapper2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As OrmQueryCmd(Of Entity) = OrmQueryCmd(Of Entity).Create(mgr)

            Dim r As ReadOnlyList(Of Entity) = q

            Assert.AreEqual(13, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestColumns()

        Dim t As Type = GetType(Table1)

        Dim q As New QueryCmd(t, New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New SQLGenerator("1"))))

        q.AutoFields = True
        q.Select(FCtor.Field(t, "Code").Add(t, "Title"))

        Dim l As ReadOnlyEntityList(Of Table1) = q.ToEntityList(Of Table1)()

        Assert.IsFalse(l(0).InternalProperties.IsLoaded)
        Assert.IsTrue(l(0).InternalProperties.IsFieldLoaded("Code"))
        Assert.IsFalse(l(0).InternalProperties.IsFieldLoaded("Enum"))

    End Sub

    <TestMethod()> Public Sub TestCachedAnonym()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(t, New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New SQLGenerator("1"))))

        q.Select(FCtor.Column(t, "code", "Code").Add(t, "name", "Title"))

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

    <TestMethod()> Public Sub TestCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(t, New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New SQLGenerator("1"))))

        q.Select(FCtor.Column(t, "code", "Code").Add(t, "name", "Title").Add(t, "id", "ID")). _
            Sort(Sorting.Field("Code"))

        Dim l As IList(Of cls) = q.ToCustomList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        Using mgr As OrmManager = q.GetMgr.CreateManager
            Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        End Using
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(t, New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New SQLGenerator("1"))))

        q.Select(FCtor.Column(t, "code", "Code").Add(t, "name", "Title").Add(t, "id", "ID", Field2DbRelations.PK)). _
            Sort(Sorting.Field("Code"))

        Dim l As IList(Of cls) = q.ToCustomList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        Using mgr As OrmManager = q.GetMgr.CreateManager
            Assert.IsTrue(mgr.CustomObject(l(0)).IsLoaded)
        End Using
    End Sub
End Class
