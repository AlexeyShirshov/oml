Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Orm
Imports Worm.Cache
Imports Worm

<TestClass()> Public Class ImmCache

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

    <TestMethod()> Public Sub TestSortValidate()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))
            mgr.NewObjectManager = tm

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)).Sort(Sorting.Custom("name"))

            Dim l As IList(Of Table1) = q.ToOrmList(Of Table1)(mgr)
            Assert.AreEqual(2, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim f As Table1 = s.CreateNewObject(Of Table1)()
                    f.Code = 20
                    f.CreatedAt = Now

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToOrmList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilter()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("ID").GreaterThan(2)
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table1 = s.CreateNewObject(Of Table1)()
                    t1.CreatedAt = Now
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdate2()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = q.ToEntityList(Of Table1)(mgr)(0)
                    t.EnumStr = Enum1.first

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdate3()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)
            q.Sort(Sorting.Custom("id"))
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = q.ToEntityList(Of Table1)(mgr)(0)
                    t.EnumStr = Enum1.first

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterUpdateBatch()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.Saver.AcceptInBatch = True
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestTableFilterUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.Column(mgr.MappingEngine.GetTables(GetType(Table1))(0), "enum_str").Eq(Enum1.sec.ToString)
            Assert.IsNotNull(q)

            Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    t.EnumStr = Enum1.sec

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToEntityList(Of Table1)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))

            Dim q As New QueryCmd(GetType(Table1))
            q.Filter = Ctor.Field(GetType(Table2), "Money").Eq(1)
            q.AutoJoins = True
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table2 = s.CreateNewObject(Of Table2)()
                    t1.Money = 1
                    t1.Tbl = mgr.Find(Of Table1)(2)
                    s.AcceptModifications()
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

            Dim q As New QueryCmd(GetType(Table2))
            q.Filter = Ctor.AutoTypeField("Money").GreaterThan(1)
            Dim l As ReadOnlyEntityList(Of Table2) = q.ToEntityList(Of Table2)(mgr)
            Assert.AreEqual(1, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim t1 As Table2 = l(0)
                    t1.Money = 1
                    s.AcceptModifications()
                End Using

                Assert.AreEqual(0, q.ToEntityList(Of Table2)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestGroupInsert()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            Dim q As QueryCmd = New QueryCmd(GetType(Table1)). _
                Select(FCtor.Field(GetType(Table1), "EnumStr")). _
                SelectAgg(AggCtor.Count("cnt")). _
                GroupBy(FCtor.Field(GetType(Table1), "EnumStr"))

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToObjectList(Of AnonymousEntity)(mgr)

            Assert.AreEqual(2, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim f As Table1 = s.CreateNewObject(Of Table1)()
                    f.EnumStr = 4
                    f.CreatedAt = Now

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(3, q.ToObjectList(Of AnonymousEntity)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestGroupUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateWriteManager(New SQLGenerator("1"))
            Dim q As QueryCmd = New QueryCmd(GetType(Table1)). _
                Select(FCtor.Field(GetType(Table1), "EnumStr")). _
                SelectAgg(AggCtor.Count("cnt")). _
                GroupBy(FCtor.Field(GetType(Table1), "EnumStr"))

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToObjectList(Of AnonymousEntity)(mgr)

            Assert.AreEqual(2, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim f As Table1 = mgr.GetOrmBaseFromCacheOrDB(Of Table1)(1)
                    f.EnumStr = Enum1.sec

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(1, q.ToObjectList(Of AnonymousEntity)(mgr).Count)
                Assert.IsFalse(q.LastExecitionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestSortValidate2()
        Dim tm As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New SQLGenerator("1"))
            mgr.NewObjectManager = tm

            Dim q As QueryCmd = New QueryCmd(GetType(Entity4)).Where( _
                Ctor.AutoTypeField("ID").GreaterThan(5)).Sort(Orm.Sorting.Field("Title"))

            Dim q2 As QueryCmd = New QueryCmd(GetType(Entity4)).Where( _
                Ctor.AutoTypeField("Title").Eq("djkg"))

            Dim l As IList(Of Entity4) = q.ToOrmList(Of Entity4)(mgr)
            Assert.AreEqual(7, l.Count)
            Dim f As Entity4 = l(0)

            Assert.AreEqual(0, q2.ToOrmList(Of Entity4)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    f.Title = "djkg"

                    s.AcceptModifications()
                End Using

                Assert.AreNotEqual(f, q.ToOrmList(Of Entity4)(mgr)(0))
                Assert.IsFalse(q.LastExecitionResult.CacheHit)

                Assert.AreEqual(1, q2.ToOrmList(Of Entity4)(mgr).Count)
                Assert.IsTrue(q2.LastExecitionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
