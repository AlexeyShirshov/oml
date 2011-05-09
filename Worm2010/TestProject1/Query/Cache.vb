Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database
Imports Worm
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Cache
Imports Worm.Entities
Imports System.Collections.ObjectModel

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
            q.SelectEntity(GetType(Table1))
            q.Filter = Ctor.prop(GetType(Table1), "ID").greater_than(2)
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table1 = s.CreateNewKeyEntity(Of Table1)()
                    t1.CreatedAt = Now
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)
                Assert.IsFalse(q.LastExecutionResult.CacheHit)
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
            q.SelectEntity(GetType(Table1))
            q.Filter = Ctor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetKeyEntityFromCacheOrDB(Of Table1)(1)
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
            q.SelectEntity(GetType(Table1))
            q.Filter = Ctor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetKeyEntityFromCacheOrDB(Of Table1)(1)
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

            Dim t1 As Table1 = mgr.GetKeyEntityFromCacheOrDB(Of Table1)(3)
            Assert.IsNotNull(t1)

            Dim q As New QueryCmd()
            q.SelectEntity(GetType(Table2))
            q.Filter = Ctor.prop(GetType(Table2), "Money").eq(t1)

            Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    t1.Delete()

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(0, q.ToList(Of Table2)(mgr).Count)
                Assert.IsFalse(q.LastExecutionResult.CacheHit)
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
            q.SelectEntity(GetType(Table1))
            q.Filter = Ctor.column(mgr.MappingEngine.GetTables(GetType(Table1))(0), "enum_str").eq(Enum1.sec.ToString)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetKeyEntityFromCacheOrDB(Of Table1)(1)
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
            q.SelectEntity(GetType(Table1))
            q.Filter = Ctor.prop(GetType(Table2), "Money").eq(1)
            q.AutoJoins = True
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table2 = s.CreateNewKeyEntity(Of Table2)()
                    t1.Money = 1
                    t1.Tbl = New QueryCmd().GetByID(Of Table1)(2, mgr)
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
            q.SelectEntity(GetType(Table2))
            q.Filter = Ctor.prop(GetType(Table2), "Money").greater_than(1)
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
                Where(Ctor.prop(tt2, "Table1").eq(tt1, "Enum").[and]( _
                      Ctor.prop(tt1, "Code").eq(45)))
            cq.SelectEntity(tt1)

            Dim q As QueryCmd = New QueryCmd(). _
                Where(New NonTemplateUnaryFilter(New SubQueryCmd(cq), Worm.Criteria.FilterOperation.NotExists))
            q.SelectEntity(tt2)

            Dim r As ReadOnlyList(Of Table2) = q.ToOrmList(Of Table2)(mgr)
            Assert.AreEqual(2, r.Count)
            Assert.IsFalse(q.LastExecutionResult.CacheHit)

            mgr.BeginTransaction()
            Try
                Dim t1 As Table1 = mgr.GetKeyEntityFromCacheOrDB(Of Table1)(3)
                t1.Enum = Enum1.first
                t1.SaveChanges(True)

                r = q.ToOrmList(Of Table2)(mgr)
                Assert.AreEqual(0, r.Count)
                Assert.IsFalse(q.LastExecutionResult.CacheHit)
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
        q.SelectEntity(t).Top(2).OrderBy(SCtor.prop(t, "DT"))

        Dim r As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(2, r(1).ID)

        q.OrderBy(SCtor.prop(t, "Title"))

        r = q.ToList(Of Table1)()

        Assert.IsFalse(q.LastExecutionResult.CacheHit)
        Assert.AreEqual(1, r(0).ID)
        Assert.AreEqual(3, r(1).ID)

    End Sub

    <TestMethod()> Public Sub TestSortCache2()
        Dim t As Type = GetType(Entity4)
        Dim c As New Cache.OrmCache
        c.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred

        Dim q As QueryCmd = New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(c, New ObjectMappingEngine("1")))).OrderBy(SCtor.prop(t, "ID"))
        q.SelectEntity(t)

        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        q.OrderBy(SCtor.prop(t, "Title"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        q.OrderBy(SCtor.prop(t, "ID"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        q.CacheSort = True

        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        q.OrderBy(SCtor.prop(t, "Title"))
        q.ToList(Of Entity4)()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        q.OrderBy(SCtor.prop(t, "ID"))
        q.ToList(Of Entity4)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestM2M()
        Dim tm As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = tm

            Dim e As Entity = mgr.GetKeyEntityFromCacheOrDB(Of Entity)(1)
            Assert.IsNotNull(e)

            Dim q As New RelationCmd(e, GetType(Entity4))

            Dim q2 As QueryCmd = New RelationCmd(e, GetType(Entity4)). _
                Where(Ctor.prop(GetType(Entity4), "Title").eq("first"))

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
                    Dim en4 As Entity4 = s.CreateNewKeyEntity(Of Entity4)()
                    en4.Title = "xxx"
                    e.Relations.Add(en4)

                    Assert.AreEqual(5, q.ToList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q.LastExecutionResult.CacheHit)

                    Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                    Assert.IsTrue(q2.LastExecutionResult.CacheHit)

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(5, q.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q.LastExecutionResult.CacheHit)

                Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q2.LastExecutionResult.CacheHit)
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

            Dim q As QueryCmd = New QueryCmd().SelectEntity(GetType(Entity4)).Where( _
                Ctor.prop(GetType(Entity4), "ID").greater_than(5)).OrderBy(SCtor.prop(GetType(Entity4), "Title"))

            Dim q2 As QueryCmd = New QueryCmd().SelectEntity(GetType(Entity4)).Where( _
                Ctor.prop(GetType(Entity4), "Title").eq("djkg"))

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
                Assert.IsFalse(q.LastExecutionResult.CacheHit)

                Assert.AreEqual(1, q2.ToList(Of Entity4)(mgr).Count)
                Assert.IsFalse(q2.LastExecutionResult.CacheHit)

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

            Dim q As QueryCmd = New QueryCmd().SelectEntity(GetType(Table1)).Where( _
                Ctor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)).OrderBy(SCtor.custom("name"))

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
                Assert.IsFalse(q.LastExecutionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSelectListCache()
        Dim c As New OrmCache

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        q.Select(FCtor.prop(GetType(Table1), "Title").prop(GetType(Table1), "Code"))

        Dim l As ReadOnlyList(Of Table1) = q.ToOrmList(Of Table1)()

        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        Assert.AreEqual(3, l.Count)

        For Each t As Table1 In l
            Assert.IsFalse(t.InternalProperties.IsLoaded)
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Code"))
        Next

        Dim l2 As ReadOnlyEntityList(Of Table1) = q.ToList(Of Table1)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        q.Select(FCtor.prop(GetType(Table1), "Title").prop(GetType(Table1), "Code"))
        l2 = q.ToList(Of Table1)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        q.Select(FCtor.prop(GetType(Table1), "Code").prop(GetType(Table1), "Title"))
        l2 = q.ToList(Of Table1)()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

    End Sub

    <TestMethod()> _
    Public Sub TestSelectAnonymListCache()
        Dim c As New OrmCache

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        q.Select(FCtor.prop(GetType(Table1), "Title").prop(GetType(Table2), "Money"))
        q.Join(JCtor.join(GetType(Table2)).on(GetType(Table1), "ID").eq(GetType(Table2), "Table1"))

        Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList

        Assert.IsFalse(q.LastExecutionResult.CacheHit)
        Assert.AreEqual(2, l.Count)

        l = q.ToAnonymList()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        q.Select(FCtor.prop(GetType(Table2), "Money").prop(GetType(Table1), "Title")).From(GetType(Table1))
        l = q.ToAnonymList()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

    End Sub

    <TestMethod()> _
    Public Sub TestSelectMatrixListCache()
        Dim c As New OrmCache

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        q.Select(FCtor.prop(GetType(Table1), "Title").prop(GetType(Table2), "Money"))
        q.From(GetType(Table1)).Join(JCtor.join(GetType(Table2)).on(GetType(Table1), "ID").eq(GetType(Table2), "Table1"))

        Dim l As ReadonlyMatrix = q.ToMatrix
        Assert.IsFalse(q.LastExecutionResult.CacheHit)
        Assert.AreEqual(2, l.Count)

        l = q.ToMatrix()
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        For Each r As ReadOnlyCollection(Of _IEntity) In l
            Assert.IsInstanceOfType(r(0), GetType(Table1))
            Assert.IsFalse(r(0).IsLoaded)
            Dim ll As IPropertyLazyLoad = TryCast(r(0), IPropertyLazyLoad)
            Assert.IsNull(ll)
            Using mc As IGetManager = r(0).GetMgr
                Assert.IsNotNull(mc)
                Dim oschema As IEntitySchema = r(0).GetEntitySchema(mc.Manager.MappingEngine)
                Assert.IsNotNull(oschema)
                Assert.IsNotNull(oschema.FieldColumnMap)
                Assert.IsTrue(OrmManager.IsPropertyLoaded(r(0), "Title", oschema.FieldColumnMap, mc.Manager.MappingEngine))
            End Using

            Assert.IsInstanceOfType(r(1), GetType(Table2))
            Assert.IsFalse(r(1).IsLoaded)
            ll = TryCast(r(1), IPropertyLazyLoad)
            Assert.IsNull(ll)
            Using mc As IGetManager = r(1).GetMgr
                Assert.IsNotNull(mc)
                Dim oschema As IEntitySchema = r(1).GetEntitySchema(mc.Manager.MappingEngine)
                Assert.IsNotNull(oschema)
                Assert.IsNotNull(oschema.FieldColumnMap)
                Assert.IsTrue(OrmManager.IsPropertyLoaded(r(1), "Money", oschema.FieldColumnMap, mc.Manager.MappingEngine))
            End Using

        Next

        q.Select(FCtor.prop(GetType(Table2), "Money").prop(GetType(Table1), "Title"))
        l = q.ToMatrix()
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        For Each r As ReadOnlyCollection(Of _IEntity) In l
            Assert.IsInstanceOfType(r(1), GetType(Table1))
            Assert.IsFalse(r(1).IsLoaded)
            Dim ll As IPropertyLazyLoad = TryCast(r(0), IPropertyLazyLoad)
            Assert.IsNull(ll)
            Using mc As IGetManager = r(1).GetMgr
                Assert.IsNotNull(mc)
                Dim oschema As IEntitySchema = r(1).GetEntitySchema(mc.Manager.MappingEngine)
                Assert.IsNotNull(oschema)
                Assert.IsNotNull(oschema.FieldColumnMap)
                Assert.IsTrue(OrmManager.IsPropertyLoaded(r(1), "Title", oschema.FieldColumnMap, mc.Manager.MappingEngine))
            End Using

            Assert.IsInstanceOfType(r(0), GetType(Table2))
            Assert.IsFalse(r(0).IsLoaded)
            ll = TryCast(r(0), IPropertyLazyLoad)
            Assert.IsNull(ll)
            Using mc As IGetManager = r(0).GetMgr
                Assert.IsNotNull(mc)
                Dim oschema As IEntitySchema = r(0).GetEntitySchema(mc.Manager.MappingEngine)
                Assert.IsNotNull(oschema)
                Assert.IsNotNull(oschema.FieldColumnMap)
                Assert.IsTrue(OrmManager.IsPropertyLoaded(r(0), "Money", oschema.FieldColumnMap, mc.Manager.MappingEngine))
            End Using
        Next

    End Sub

    <TestMethod()> _
    Public Sub TestModifCache()
        Dim c As New OrmCache
        Dim s As New ObjectMappingEngine("1")

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(s, c))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Assert.IsTrue(c.IsInCachePrecise(t, s))

        Assert.IsFalse(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.NotLoaded, t.InternalProperties.ObjectState)

        t.Name = "353"

        Assert.IsTrue(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.Modified, t.InternalProperties.ObjectState)

        Dim dic As System.Collections.IDictionary = c.GetOrmDictionary(GetType(Table1), s)

        Dim id As CacheKey = New CacheKey(t)

        dic.Remove(id)

        Assert.IsFalse(c.IsInCachePrecise(t, s))

        Assert.IsNotNull(t.InternalProperties.OriginalCopy)
        'Assert.IsNotNull(c.ShadowCopy(t.GetType, t, TryCast(s.GetEntitySchema(GetType(Table1)), ICacheBehavior)))

        t = q.GetByID(Of Table1)(1)

        Assert.IsNotNull(t.InternalProperties.OriginalCopy)
        'Assert.IsNotNull(c.ShadowCopy(t.GetType, t, TryCast(s.GetEntitySchema(GetType(Table1)), ICacheBehavior)))

        Assert.IsTrue(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.Modified, t.InternalProperties.ObjectState)
    End Sub

End Class