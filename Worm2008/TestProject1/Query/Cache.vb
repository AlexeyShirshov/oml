Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Database
Imports Worm
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Criteria
Imports Worm.Criteria.Joins

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
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table1))
            q.Filter = PCtor.prop(GetType(Table1), "ID").greater_than(2)
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table1 = s.CreateNewObject(Of Table1)()
                    t1.CreatedAt = Now
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table1))
            q.Filter = PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdateBatch()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table1))
            q.Filter = PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.Saver.AcceptInBatch = True
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestOrmFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred

            Dim t1 As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(3)
            Assert.IsNotNull(t1)

            Dim q As New QueryCmd()
            q.Select(GetType(Table2))
            q.Filter = PCtor.prop(GetType(Table2), "Money").eq(t1)

            Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    t1.Delete()

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestTableFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table1))
            q.Filter = PCtor.column(mgr.MappingEngine.GetTables(GetType(Table1))(0), "enum_str").eq(Enum1.sec.ToString)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table1))
            q.Filter = PCtor.prop(GetType(Table2), "Money").eq(1)
            q.AutoJoins = True
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table2 = s.CreateNewObject(Of Table2)()
                    t1.Money = 1
                    t1.Tbl = mgr.Find(Of Table1)(2)
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred
            Dim q As New QueryCmd()
            q.Select(GetType(Table2))
            q.Filter = PCtor.prop(GetType(Table2), "Money").greater_than(1)
            Dim l As ReadOnlyEntityList(Of Table2) = q.ToList(Of Table2)(mgr)
            Assert.AreEqual(1, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table2 = l(0)
                    t1.Money = 1
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestCorrelatedSubqueryCache()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred

            Dim tt1 As Type = GetType(Table1)
            Dim tt2 As Type = GetType(Table2)

            Dim cq As QueryCmd = New QueryCmd(). _
                Where(JoinCondition.Create(tt2, "Table1").eq(tt1, "Enum").[and]( _
                      PCtor.prop(tt1, "Code").eq(45)))
            cq.Select(tt1)

            Dim q As QueryCmd = New QueryCmd(). _
                Where(New NonTemplateUnaryFilter(New SubQueryCmd(cq), Worm.Criteria.FilterOperation.NotExists))
            q.Select(tt2)

            Dim r As ReadOnlyList(Of Table2) = q.ToOrmList(Of Table2)(mgr)
            Assert.AreEqual(2, r.Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            mgr.BeginTransaction()
            Try
                Dim t1 As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(3)
                t1.Enum = Enum1.first
                t1.SaveChanges(True)

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

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache)))
        q.Select(t).Top(2).Sort(SCtor.prop(t, "DT"))

        Dim r As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(2, r(1).ID)

        q.Sort(SCtor.prop(t, "Title"))

        r = q.ToList(Of Table1)()

        Assert.IsFalse(q.LastExecitionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(3, r(1).ID)

    End Sub

    <TestMethod()> Public Sub TestSortCache2()
        Dim t As Type = GetType(Entity4)
        Dim c As New Cache.OrmCache
        c.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred

        Dim q As QueryCmd = New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(c, New ObjectMappingEngine("1")))).Sort(SCtor.prop(t, "ID"))
        q.Select(t)

        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)

        q.Sort(SCtor.prop(t, "Title"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.Sort(SCtor.prop(t, "ID"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.CacheSort = True

        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)

        q.Sort(SCtor.prop(t, "Title"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.Sort(SCtor.prop(t, "ID"))
        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestM2M()
        Dim tm As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = tm

            Dim e As Entity = mgr.GetOrmBaseFromCacheOrDB(Of Entity)(1)
            Assert.IsNotNull(e)

            Dim q As New QueryCmd(e)

            Dim q2 As QueryCmd = New QueryCmd(e). _
                Where(PCtor.prop(GetType(Entity4), "Title").eq("first"))

            Dim r As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Dim r2 As ReadOnlyEntityList(Of Entity4) = q2.ToList(Of Entity4)(mgr)

            Assert.AreEqual(4, r.Count)
            Assert.AreEqual(1, r2.Count)

            For Each o As Entity4 In r
                Assert.IsFalse(o.InternalProperties.IsLoaded)
            Next

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim en4 As Entity4 = s.CreateNewObject(Of Entity4)()
                    en4.Title = "xxx"
                    e.M2MNew.Add(en4)

                    Assert.AreEqual(4, q.ToList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q.LastExecitionResult.CacheHit)

                    Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q2.LastExecitionResult.CacheHit)

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(5, q.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)

                Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q2.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestSortValidate()
        Dim tm As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = tm
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred

            Dim q As QueryCmd = New QueryCmd().Select(GetType(Entity4)).Where( _
                PCtor.prop(GetType(Entity4), "ID").greater_than(5)).Sort(SCtor.prop(GetType(Entity4), "Title"))

            Dim q2 As QueryCmd = New QueryCmd().Select(GetType(Entity4)).Where( _
                PCtor.prop(GetType(Entity4), "Title").eq("djkg"))

            Dim l As IList(Of Entity4) = q.ToList(Of Entity4)(mgr)
            Assert.AreEqual(7, l.Count)
            Dim f As Entity4 = l(0)

            Assert.AreEqual(0, q2.ToList(Of Entity4)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    f.Title = "djkg"
                    s.AcceptModifications()
                End Using

                Assert.AreNotEqual(f, q.ToList(Of Entity4)(mgr)(0))
                Assert.IsFalse(q.LastExecitionResult.CacheHit)

                Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q2.LastExecitionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestSortValidate2()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = tm
            CType(mgr.Cache, Cache.OrmCache).ValidateBehavior = Cache.ValidateBehavior.Deferred

            Dim q As QueryCmd = New QueryCmd().Select(GetType(Table1)).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)).Sort(SCtor.Custom("name"))

            Dim l As IList(Of Table1) = q.ToList(Of Table1)(mgr)
            Assert.AreEqual(2, l.Count)
            Dim f As Table1 = l(0)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    f.Code = 20
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(f, q.ToList(Of Table1)(mgr)(0))
                Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

End Class