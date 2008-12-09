Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Criteria

<TestClass()> Public Class TestComplexEntities

    <TestMethod()> _
    Public Sub TestGuid()
        Using mgr As OrmManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim o As GuidPK = mgr.Find(Of GuidPK)(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"))

            Assert.IsNotNull(o)

            Assert.IsTrue(o.InternalProperties.IsLoaded)

            Assert.AreEqual(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"), o.Guid)

            Assert.AreEqual(4, o.Code)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
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

    <TestMethod()> _
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

    <TestMethod()> _
    Public Sub TestCPK()
        Dim cache As New Worm.Cache.OrmCache
        Dim gen As New ObjectMappingEngine("1")

        Dim q As QueryCmd = New QueryCmd().Select(GetType(ComplexPK)).Where _
            (PCtor.prop(GetType(ComplexPK), "Int").eq(345))

        Dim l As ReadOnlyEntityList(Of ComplexPK) = q.ToEntityList(Of ComplexPK)( _
            Function() TestManager.CreateManager(cache, gen))

        Assert.AreEqual(2, l.Count)

        Dim f As ComplexPK = l(0)

        Assert.AreEqual(345, f.Int)
        Assert.AreEqual(345, l(1).Int)

        Assert.AreEqual("dglm", f.Code)
        Assert.AreEqual("oqnervg", l(1).Code)

        Assert.IsFalse(f.InternalProperties.IsLoaded)
        Assert.AreEqual("wf0pvmdb", f.Name)
        Assert.IsTrue(f.InternalProperties.IsLoaded)

        l = New QueryCmd().Select(GetType(ComplexPK)).Where _
            (PCtor.prop(GetType(ComplexPK), "Int").eq(345).[and]("Code").eq("dglm")).ToEntityList(Of ComplexPK)(Function() TestManager.CreateManager(cache, gen))

        Assert.AreEqual(1, l.Count)
        Assert.IsTrue(l(0).InternalProperties.IsLoaded)
        Assert.AreSame(f, l(0))
    End Sub

    <TestMethod()> _
    Public Sub TestCPKUpdate()
        Using mgr As OrmDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
            Dim l As ReadOnlyEntityList(Of ComplexPK) = New QueryCmd().Select(GetType(ComplexPK)).Where _
                            (PCtor.prop(GetType(ComplexPK), "Int").eq(345).[and]("Code").eq("dglm")).ToEntityList(Of ComplexPK)(mgr)

            Dim f As ComplexPK = l(0)

            Assert.AreEqual("wf0pvmdb", f.Name)
            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    f.Name = "xxx"
                    s.AcceptModifications()
                End Using

                Dim f2 As ComplexPK = New QueryCmd().Select(GetType(ComplexPK)).Where _
                             (PCtor.prop(GetType(ComplexPK), "Name").eq("xxx")).ToEntityList(Of ComplexPK)(mgr)(0)

                Assert.AreSame(f, f2)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class