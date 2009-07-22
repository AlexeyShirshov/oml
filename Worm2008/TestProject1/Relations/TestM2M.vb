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
            q.SelectEntity(GetType(Entity))
            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            Dim q2 As New QueryCmd()
            q2.SelectEntity(GetType(Entity4))
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
            q.SelectEntity(GetType(Entity))
            Assert.IsNotNull(q)

            q.Filter = Ctor.prop(GetType(Entity), "ID").eq(1)

            Dim e As Entity = q.Single(Of Entity)(mgr) 'q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Entities.IRelations).GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            Dim q2 As New QueryCmd()
            q2.SelectEntity(GetType(Entity4))
            q2.Filter = Ctor.prop(GetType(Entity4), "ID").eq(2)

            Dim e2 As Entity4 = q2.Single(Of Entity4)(mgr) 'q2.ToEntityList(Of Entity4)(mgr)(0)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)

                    Assert.IsFalse(e.InternalProperties.HasRelaionalChanges)
                    Assert.IsFalse(e2.InternalProperties.HasRelaionalChanges)

                    e.Relations.Add(e2)

                    Assert.IsTrue(e.InternalProperties.HasRelaionalChanges)
                    Assert.IsTrue(e2.InternalProperties.HasRelaionalChanges)

                    s.AcceptModifications()
                End Using

                Assert.IsFalse(e.InternalProperties.HasRelaionalChanges)
                Assert.IsFalse(e2.InternalProperties.HasRelaionalChanges)

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
            q.SelectEntity(GetType(Entity))
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

                    Assert.IsTrue(CType(e2, Worm.Entities.IRelations).GetRelation(GetType(Entity)).Added.Contains(e))

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

    <TestMethod()> Public Sub TestM2MAddCmd()
        Dim t As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = t
            Dim q As New QueryCmd()
            q.SelectEntity(GetType(Entity))
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
                    e2 = s.CreateNewObject(Of Entity4)()

                    e.GetCmd(GetType(Entity4)).Add(e2)

                    Assert.IsTrue(CType(e2, Worm.Entities.IRelations).GetRelation(GetType(Entity)).Added.Contains(e))
                    Assert.IsTrue(e2.InternalProperties.HasChanges)
                    Assert.IsTrue(e.InternalProperties.HasRelaionalChanges)
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
            Dim q As QueryCmd = New QueryCmd().SelectEntity(GetType(Entity)).Where(Ctor.prop(GetType(Entity), "ID").eq(1))

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

        Dim a As New QueryAlias(GetType(Table1))
        Dim eu As New EntityUnion(a)

        Dim mq As QueryCmd = t.GetCmd(New M2MRelationDesc(eu, M2MRelationDesc.DirKey)).SelectEntity(eu)

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        Dim a2 As New QueryAlias(GetType(Table1))

        mq.Join(JCtor.join(a2).onM2M(M2MRelationDesc.RevKey, a))

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        mq.SelectEntity(a2)

        Assert.AreEqual(1, mq.First(Of Table1).ID)
    End Sub

    <TestMethod()> _
    Public Sub TestM2MMany2()

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Dim mq As QueryCmd = t.GetCmd(New M2MRelationDesc(GetType(Table1), M2MRelationDesc.DirKey))

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        Dim a2 As New QueryAlias(GetType(Table1))

        mq.Join(JCtor.join(a2).onM2M(M2MRelationDesc.RevKey, GetType(Table1)))

        Assert.AreEqual(1, mq.Count)
        Assert.AreEqual(2, mq.First(Of Table1).ID)

        mq.SelectEntity(a2)

        Assert.AreEqual(1, mq.First(Of Table1).ID)
    End Sub

    <TestMethod()> _
    Public Sub TestM2MMany3()

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Dim a1 As New QueryAlias(GetType(Table1))
        Dim a2 As New QueryAlias(GetType(Table1))

        Dim t2 As Table1 = q _
            .From(GetType(Table1)) _
            .SelectEntity(a2) _
            .Join(JCtor _
                  .join(a1).onM2M(M2MRelationDesc.RevKey, GetType(Table1)) _
                  .join(a2).onM2M(M2MRelationDesc.RevKey, a1)) _
            .Where(Ctor.prop(GetType(Table1), "ID").eq(t)) _
            .First(Of Table1)()

        Assert.AreEqual(1, t2.ID)
    End Sub

    <TestMethod()> _
    Public Sub TestM2MManyRel()

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Dim a1 As New QueryAlias(GetType(Table1))
        Dim a2 As New QueryAlias(GetType(Table1))

        'Dim r As New M2MRelationDesc(New EntityUnion(GetType(Table1)), M2MRelationDesc.RevKey)
        Dim r2 As New RelationDescEx(New EntityUnion(GetType(Table1)), New M2MRelationDesc(New EntityUnion(a1), M2MRelationDesc.RevKey))
        Dim r3 As New RelationDescEx(New EntityUnion(a1), New M2MRelationDesc(New EntityUnion(a2), M2MRelationDesc.RevKey))

        Dim t2 As Table1 = q _
            .From(GetType(Table1)) _
            .SelectEntity(a2) _
            .Join(JCtor _
                  .join_relation(r2) _
                  .join_relation(r3)) _
            .Where(Ctor.prop(GetType(Table1), "ID").eq(t)) _
            .First(Of Table1)()

        Assert.AreEqual(1, t2.ID)
    End Sub

    <TestMethod()> _
    Public Sub TestM2MManyExists()

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.GetByID(Of Table1)(1)

        Dim a1 As New QueryAlias(GetType(Table1))
        Dim a2 As New QueryAlias(GetType(Table1))

        Dim t2 As Table1 = q _
            .From(GetType(Table1)) _
            .SelectEntity(a1) _
            .Join(JCtor _
                  .join(a1).onM2M(M2MRelationDesc.RevKey, GetType(Table1))) _
            .Where(Ctor.prop(GetType(Table1), "ID").eq(t) _
                   .and_exists(New QueryCmd() _
                               .From(a2) _
                               .SelectEntity(a2) _
                               .Join(JCtor.join(a1).onM2M(a2)))) _
            .First(Of Table1)()

        Assert.AreEqual(2, t2.ID)
    End Sub

    <TestMethod()> _
    Public Sub TestM2MManyEx()
        Dim a1 As New QueryAlias(GetType(Table1))
        Dim a2 As New QueryAlias(GetType(Table1))

        Dim a3 As New QueryAlias(GetType(Table1))
        Dim a4 As New QueryAlias(GetType(Table1))

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q1.From(a1).Join(JCtor.join(a2).onM2M(M2MRelationDesc.RevKey, a1)).SelectEntity(a1)

        Dim q2 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q2.From(a3).Join(JCtor.join(a4).onM2M(a3)).SelectEntity(a3)

        Dim q3 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q3.From(q1).Union(q2)

        'Dim q4 As QueryCmd = CType(q3.Clone, QueryCmd)
        'Dim q5 As QueryCmd = CType(q3.Clone, QueryCmd)

        Dim l As ReadOnlyEntityList(Of Table1) = q3.ToList(Of Table1)()

    End Sub
End Class
