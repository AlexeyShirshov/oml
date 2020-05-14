Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Query.Sorting
Imports Worm.Criteria.Core
Imports Worm.Query
Imports Worm.Criteria
Imports Worm.Entities.Meta

<TestClass()> Public Class TestComplexEntities

    <TestMethod()>
    Public Sub TestGuid()
        Using mgr As OrmManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim o As GuidPK = New QueryCmd().GetByID(Of GuidPK)(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"), GetByIDOptions.EnsureExistsInStore, mgr)

            Assert.IsNotNull(o)

            Assert.IsTrue(o.InternalProperties.IsLoaded)

            Assert.AreEqual(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"), o.Guid)

            Assert.AreEqual(4, o.Code)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))>
    Public Sub TestGuidCreateWrong()
        Using mgr As OrmDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            Dim o As GuidPK = Nothing
            Using mt As New ModificationsTracker(mgr)
                o = New GuidPK
                'o.Identifier = Guid.NewGuid
                o.Code = 2
                Assert.AreEqual(Guid.Empty, o.Guid)

                mt.Add(o)

                mt.AcceptModifications()
            End Using

            Assert.IsNotNull(o)
            Assert.IsTrue(o.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)
            Assert.AreNotEqual(Guid.Empty, o.Guid)
        End Using
    End Sub

    <TestMethod()>
    Public Sub TestGuidCreate()
        Using mgr As OrmDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            Dim o As GuidPK = Nothing

            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    o = New GuidPK
                    'o.Identifier = Guid.NewGuid
                    o.Code = 2
                    Assert.AreEqual(Guid.Empty, o.Guid)

                    mt.Add(o)

                    mt.AcceptModifications()
                End Using
            Finally
                mgr.Rollback()
            End Try

            Assert.IsNotNull(o)
            Assert.IsTrue(o.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)
            Assert.AreNotEqual(Guid.Empty, o.Guid)
        End Using
    End Sub

    <TestMethod()>
    Public Sub TestCPK()
        Dim cache As New Worm.Cache.OrmCache
        Dim gen As New ObjectMappingEngine("1")
        Dim schema = gen.GetEntitySchema(GetType(ComplexPK))
        Dim tbl = schema.Table
        Dim q As QueryCmd = New QueryCmd().SelectEntity(GetType(ComplexPK)).Where _
            (Ctor.column(tbl, "i").eq(345))

        Dim l = q.ToList(Of ComplexPK)(
            Function() TestManager.CreateManager(cache, gen))

        Assert.AreEqual(2, l.Count)

        Dim f As ComplexPK = l(0)
        Assert.IsFalse(f.InternalProperties.IsLoaded)
        Assert.IsTrue(CType(f, _ICachedEntity).IsPKLoaded)
        Assert.IsTrue(f.InternalProperties.IsPropertyLoaded(MapField2Column.PK))

        Assert.AreEqual(345, f.Int)
        Assert.IsFalse(f.InternalProperties.IsLoaded)

        Assert.AreEqual(345, l(1).Int)

        Assert.AreEqual("dglm", f.Code)
        Assert.AreEqual("oqnervg", l(1).Code)

        Assert.IsFalse(f.InternalProperties.IsLoaded)
        Assert.AreEqual("wf0pvmdb", f.Name)
        Assert.IsTrue(f.InternalProperties.IsLoaded)

        l = New QueryCmd().SelectEntity(GetType(ComplexPK)).Where _
            (Ctor.column(tbl, "i").eq(345).[and](tbl, "code").eq("dglm")).ToList(Of ComplexPK)(Function() TestManager.CreateManager(cache, gen))

        Assert.AreEqual(1, l.Count)
        Assert.IsTrue(l(0).InternalProperties.IsLoaded)
        Assert.AreSame(f, l(0))

        Dim f2 As ComplexPK = New QueryCmd().SelectEntity(GetType(ComplexPK)).Where _
                             (Ctor.prop(GetType(ComplexPK), "Name").eq("wf0pvmdb")).First(Of ComplexPK)(Function() TestManager.CreateManager(cache, gen))

        Assert.IsTrue(f2.InternalProperties.IsLoaded)

    End Sub

    <TestMethod()>
    Public Sub TestCPKUpdate()
        Dim gen As New ObjectMappingEngine("1")
        Using mgr As OrmDBManager = TestManager.CreateWriteManager(gen)
            Dim schema = gen.GetEntitySchema(GetType(ComplexPK))
            Dim tbl = schema.Table

            Dim l As ReadOnlyEntityList(Of ComplexPK) =
                New QueryCmd().SelectEntity(GetType(ComplexPK)).
                    Where(Ctor.column(tbl, "i").eq(345).[and](tbl, "code").eq("dglm")).ToEntityList(Of ComplexPK)(mgr)

            Dim f As ComplexPK = l(0)

            Assert.AreEqual("wf0pvmdb", f.Name)
            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    f.Name = "xxx"
                    s.AcceptModifications()
                End Using

                Dim f2 As ComplexPK = New QueryCmd().SelectEntity(GetType(ComplexPK)).Where _
                             (Ctor.prop(GetType(ComplexPK), "Name").eq("xxx")).ToEntityList(Of ComplexPK)(mgr)(0)

                Assert.AreSame(f, f2)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod>
    Public Sub TestComplexFK()
        Dim cache As New Worm.Cache.OrmCache
        Dim gen As New ObjectMappingEngine("1")
        Dim dx As New DataContext(Function() TestManager.CreateManager(cache, gen))

        Dim o = dx.GetByID(Of ComplexFK)(1)

        Assert.IsNotNull(o)

        Assert.IsNotNull(o.Parent)

        Assert.AreEqual("dsfklvmdlf", o.Parent.Name)
    End Sub
End Class