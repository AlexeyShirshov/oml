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
Imports Worm.Database.Criteria.Joins

<TestClass()> Public Class DeferredCacheQueryTest

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
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("ID").GreaterThan(2)
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table1 = s.CreateNewObject(Of Table1)()
                    t1.CreatedAt = Now
                    s.Commit()
                End Using

                Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.Commit()
                End Using

                Assert.AreEqual(3, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestTableFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.Column(mgr.MappingEngine.GetTables(GetType(Table1))(0), "enum_str").Eq(Enum1.sec.ToString)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.Commit()
                End Using

                Assert.AreEqual(3, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.Field(GetType(Table2), "Money").Eq(1)
            q.AutoJoins = True
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table2 = s.CreateNewObject(Of Table2)()
                    t1.Money = 1
                    t1.Tbl = mgr.Find(Of Table1)(2)
                    s.Commit()
                End Using

                Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd(GetType(Table2))
            q.Filter = Ctor.AutoTypeField("Money").GreaterThan(1)
            Dim l As ReadOnlyEntityList(Of Table2) = q.ToEntityList(Of Table2)(mgr)
            Assert.AreEqual(1, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table2 = l(0)
                    t1.Money = 1
                    s.Commit()
                End Using

                Assert.AreEqual(0, q.ToEntityList(Of Table2)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubqueryCache()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred

            Dim tt1 As Type = GetType(Table1)
            Dim tt2 As Type = GetType(Table2)

            Dim cq As QueryCmd = New QueryCmd(tt1). _
                Where(JoinCondition.Create(tt2, "Table1").Eq(tt1, "Enum").And( _
                      Ctor.Field(tt1, "Code").Eq(45)))

            Dim q As QueryCmd = New QueryCmd(tt2). _
                Where(New NonTemplateUnaryFilter(New Values.SubQueryCmd(cq), Worm.Criteria.FilterOperation.NotExists))

            Dim r As ReadOnlyList(Of Table2) = q.ToOrmList(Of Table2)(mgr)
            Assert.AreEqual(2, r.Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            mgr.BeginTransaction()
            Try
                r(0).Tbl.Enum = CType(r(0).ID + 1, TestProject1.Enum1)
                r(0).Tbl.SaveChanges(True)

                r = q.ToOrmList(Of Table2)(mgr)
                Assert.AreEqual(0, r.Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestSortCache()
        Dim t As Type = GetType(Table1)

        Dim cache As New Cache.OrmCache
        cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred

        Dim q As New QueryCmd(t, New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New SQLGenerator("1"), cache)))

        q.Top(2).Sort(Orm.Sorting.Field("DT"))

        Dim r As ReadOnlyEntityList(Of Table1) = q.ToEntityList(Of Table1)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(2, r(1).ID)

        q.Sort(Orm.Sorting.Field("Title"))

        r = q.ToEntityList(Of Table1)()

        Assert.IsFalse(q.LastExecitionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(3, r(1).ID)

    End Sub

    <TestMethod()> Public Sub TestSortCache2()
        Dim t As Type = GetType(Entity4)
        Dim c As New Cache.OrmCache
        c.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred

        Dim q As QueryCmd = New QueryCmd(t, New CreateManager(Function() _
            TestManager.CreateManager(c, New SQLGenerator("1")))).Sort(Orm.Sorting.Field("ID"))

        q.ToEntityList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.ToEntityList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)

        q.Sort(Orm.Sorting.Field("Title"))
        q.ToEntityList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.Sort(Orm.Sorting.Field("ID"))
        q.ToEntityList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.CacheSort = True

        q.ToEntityList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.ToEntityList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)

        q.Sort(Orm.Sorting.Field("Title"))
        q.ToEntityList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.Sort(Orm.Sorting.Field("ID"))
        q.ToEntityList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestM2M()
        Dim tm As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New SQLGenerator("1"))
            mgr.NewObjectManager = tm

            Dim e As Entity = mgr.GetOrmBaseFromCacheOrDB(Of Entity)(1)
            Assert.IsNotNull(e)

            Dim q As New QueryCmd(e)

            Dim q2 As QueryCmd = New QueryCmd(e). _
                Where(Ctor.Field(GetType(Entity4), "Title").Eq("first"))

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToEntityList(Of Entity4)(mgr)
            Dim r2 As ReadOnlyEntityList(Of Entity4) = q2.ToEntityList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            Assert.AreEqual(1, r2.Count)

            For Each o As Entity4 In r
                Assert.IsFalse(o.InternalProperties.IsLoaded)
            Next

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim en4 As Entity4 = s.CreateNewObject(Of Entity4)()
                    en4.Title = "xxx"
                    e.M2MNew.Add(en4)

                    Assert.AreEqual(4, q.ToEntityList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q.LastExecitionResult.CacheHit)

                    Assert.AreEqual(1, q2.ToEntityList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q2.LastExecitionResult.CacheHit)

                    s.Commit()
                End Using

                Assert.AreEqual(5, q.ToEntityList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)

                Assert.AreEqual(1, q2.ToEntityList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q2.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
