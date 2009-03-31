Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm
Imports Worm.Criteria
Imports Worm.Entities.Meta

<TestClass()> Public Class TestNewM2M

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

    <TestMethod()> Public Sub TestM2MAdd()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            Assert.IsNotNull(q)
            q.Select(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            Dim q2 As New QueryCmd()
            q2.Select(GetType(Entity4))
            q2.Filter = Ctor.prop(GetType(Entity4), "ID").eq(2)

            Dim e2 As Entity4 = q2.Single(Of Entity4)(mgr) 'q2.ToList(Of Entity4)(mgr)(0)

            Assert.IsFalse(l.Contains(e2))

            Dim l2 As Worm.ReadOnlyEntityList(Of Entity) = CType(e2, Worm.Entities.IRelations).GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.IsFalse(l2.Contains(e))

            mgr.BeginTransaction()
            Try
                Assert.IsFalse(e.InternalProperties.HasChanges())
                Assert.IsFalse(e2.InternalProperties.HasChanges())

                CType(e, Worm.Entities.IRelations).Add(e2)

                Assert.IsTrue(e.InternalProperties.HasChanges)
                Assert.IsTrue(e2.InternalProperties.HasChanges)

                e.SaveChanges(True)
                Assert.IsFalse(e.InternalProperties.HasChanges)
                Assert.IsFalse(e2.InternalProperties.HasChanges)

                Dim cmd As RelationCmd = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4))
                l = cmd.ToList(Of Entity4)(mgr)
                Assert.IsNotNull(l)
                Assert.IsFalse(cmd.LastExecutionResult.CacheHit)
                Assert.AreEqual(5, l.Count)

                Assert.IsTrue(l.Contains(e2))

                l2 = CType(e2, Worm.Entities.IRelations).GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
                Assert.IsTrue(l2.Contains(e))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MAddScope()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            Assert.IsNotNull(q)

            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            Dim q2 As New QueryCmd()
            q2.Select(GetType(Entity4))
            q2.Filter = Ctor.prop(GetType(Entity4), "ID").eq(2)

            Dim e2 As Entity4 = q2.Single(Of Entity4)(mgr) 'q2.ToEntityList(Of Entity4)(mgr)(0)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)

                    Assert.IsFalse(e.InternalProperties.HasM2MChanges)
                    Assert.IsFalse(e2.InternalProperties.HasM2MChanges)

                    e.Relations.Add(e2)

                    Assert.IsTrue(e.InternalProperties.HasM2MChanges)
                    Assert.IsTrue(e2.InternalProperties.HasM2MChanges)

                    s.AcceptModifications()
                End Using

                Assert.IsFalse(e.InternalProperties.HasM2MChanges)
                Assert.IsFalse(e2.InternalProperties.HasM2MChanges)

                l = e.Relations.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
                Assert.AreEqual(5, l.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MAddNew()
        Dim t As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = t
            Dim q As New QueryCmd()
            q.Select(GetType(Entity))
            Assert.IsNotNull(q)

            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            mgr.BeginTransaction()
            Try
                Dim e2 As Entity4 = Nothing

                Using s As New ModificationsTracker(mgr)
                    s.Add(e)

                    e2 = s.CreateNewObject(Of Entity4)()

                    CType(e, Worm.Entities.IRelations).Add(e2)

                    Assert.IsFalse(CType(e2, Worm.Entities.IRelations).GetCmd(GetType(Entity)).ToList(Of Entity)(mgr).Contains(e))

                    s.AcceptModifications()
                End Using

                l = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
                Assert.IsNotNull(l)
                Assert.AreEqual(5, l.Count)

                Assert.IsTrue(l.Contains(e2))

                Dim l2 As Worm.ReadOnlyEntityList(Of Entity) = CType(e2, Worm.Entities.IRelations).GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
                Assert.IsTrue(l2.Contains(e))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd().Select(GetType(Entity)).Where(Ctor.prop(GetType(Entity), "ID").eq(1))

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = e.Relations.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Dim c As Integer = l.Count

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    e.Relations.Remove(l(0))
                    s.AcceptModifications()
                End Using

                Dim q2 As QueryCmd = e.Relations.GetCmd(GetType(Entity4))
                l = q2.ToList(Of Entity4)(mgr)
                Assert.AreEqual(c - 1, l.Count)
                Assert.IsFalse(q2.LastExecutionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MMany()

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Dim a As New EntityAlias(GetType(Table1))

        Dim mq As QueryCmd = t.GetCmd(New M2MRelationDesc(New EntityUnion(a), M2MRelationDesc.DirKey))

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        Dim a2 As New EntityAlias(GetType(Table1))

        mq.Join(JCtor.join(a2).onM2M(M2MRelationDesc.RevKey, a))

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        mq.Select(a2)

        Assert.AreEqual(1, mq.First(Of Table1).ID)
    End Sub
End Class
