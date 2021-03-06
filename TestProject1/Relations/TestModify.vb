﻿Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Cache
Imports Worm.Database

<TestClass()> Public Class TestModify

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

    Private _cache As New Cache.OrmCache

    Protected Function CreateCmd() As QueryCmd
        Return New QueryCmd(Function() TestManager.CreateWriteManager(New ObjectMappingEngine("1")))
    End Function

    Protected Function CreateCmdRS() As QueryCmd
        Return New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), _cache))
    End Function

    <TestMethod()> Public Sub TestAddCount()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(2)

        Dim cnt As Integer = e.GetCmd(GetType(Entity4)).Count

        e.GetCmd(GetType(Entity4)).Add(e4)
        Assert.AreEqual(1, e.GetCmd(GetType(Entity4)).Relation.Added.Count)

        Assert.AreEqual(cnt + 1, e.GetCmd(GetType(Entity4)).Count)
    End Sub

    <TestMethod()> Public Sub TestAddToList()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(2)

        Dim cnt As Integer = e.GetCmd(GetType(Entity4)).ToList(Of Entity4).Count

        e.GetCmd(GetType(Entity4)).Add(e4)

        Assert.AreEqual(cnt + 1, e.GetCmd(GetType(Entity4)).ToList(Of Entity4).Count)

        cnt = e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .ToList(Of Entity4).Count

        e4.Title = "xxxxxx"

        Assert.AreEqual(cnt - 1, e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .ToList(Of Entity4).Count)
    End Sub

    <TestMethod()> Public Sub TestRemoveFromList()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(4)

        Dim cnt As Integer = e.GetCmd(GetType(Entity4)).ToList(Of Entity4).Count

        Dim cnt2 As Integer = e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .ToList(Of Entity4).Count

        e.GetCmd(GetType(Entity4)).Remove(e4)

        Assert.AreEqual(cnt - 1, e.GetCmd(GetType(Entity4)).ToList(Of Entity4).Count)

        Assert.AreEqual(cnt2 - 1, e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .ToList(Of Entity4).Count)
    End Sub

    <TestMethod()> Public Sub TestAddToListSort()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(2)

        Assert.AreEqual(10, e.GetCmd(GetType(Entity4)) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .First(Of Entity4)().ID)

        Assert.AreEqual(1, e.GetCmd(GetType(Entity4)) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .Last(Of Entity4)().ID)

        e.GetCmd(GetType(Entity4)).Add(e4)

        Assert.AreEqual(10, e.GetCmd(GetType(Entity4)) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .First(Of Entity4)().ID)

        Assert.AreEqual(2, e.GetCmd(GetType(Entity4)) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .Last(Of Entity4)().ID)

        Assert.AreEqual(10, e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .First(Of Entity4)().ID)

        Assert.AreEqual(2, e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .Last(Of Entity4)().ID)

        e4.Title = "ab"

        Assert.AreEqual(4, e.GetCmd(GetType(Entity4)) _
            .Where(Ctor.prop(GetType(Entity4), "Title").like("%b%")) _
            .OrderBy(SCtor.prop(GetType(Entity4), "Title")) _
            .Last(Of Entity4)().ID)
    End Sub

    <TestMethod()> Public Sub TestAddContains()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(2)

        Assert.IsFalse(e.GetCmd(GetType(Entity4)).Contains(e4))

        e.GetCmd(GetType(Entity4)).Add(e4)

        Assert.IsTrue(e.GetCmd(GetType(Entity4)).Contains(e4))
    End Sub

    <TestMethod()> Public Sub TestUnderlying()
        Dim t As Table1 = CreateCmdRS.GetByID(Of Table1)(2)
        Dim c As Integer = t.GetCmd(GetType(Table3)).Count

        Using mgr As Worm.Database.OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"), _cache)
            Dim cmd As QueryCmd = t.GetCmd(GetType(Table3))
            Assert.AreEqual(c, cmd.Count(mgr))
            Assert.IsTrue(cmd.LastExecutionResult.CacheHit)

            mgr.BeginTransaction()
            Try
                Dim t1to3 As Tables1to3 = Nothing
                Using mt As New Worm.Database.ModificationsTracker(mgr)
                    t1to3 = New Tables1to3 With { _
                        .Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr), _
                        .Table3 = New QueryCmd().GetByID(Of Table33)(1, mgr), _
                        .Title = "dfasdf" _
                    }

                    mt.Add(t1to3)
                    mt.AcceptModifications()
                End Using

                Assert.AreNotEqual(0, t1to3.ID)
                Assert.AreEqual(c + 1, cmd.Count(mgr))
                Assert.IsFalse(cmd.LastExecutionResult.CacheHit)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestAddValidate()
        Dim cache As New OrmCache
        Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(mpe, cache, New SQL2000Generator))

        Dim t1 As Table1 = q1.GetByID(Of Table1)(1)

        Dim tables2 As RelationCmd = t1.Table2s
        Assert.AreEqual(2, tables2.ToList(Of Table2).Count)
        Assert.IsFalse(tables2.LastExecutionResult.CacheHit)

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(mpe, cache)
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    Dim t2 As Table2 = mt.CreateNewKeyEntity(Of Table2)()
                    tables2.Add(t2)
                    Assert.AreSame(t1, t2.Tbl)

                    Assert.AreEqual(3, tables2.ToList(Of Table2).Count)
                    Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

                    mt.AcceptModifications()
                End Using

                Assert.IsFalse(tables2.HasChanges)

                Dim r As ReadOnlyEntityList(Of Table2) = tables2.ToList(Of Table2)(mgr)
                Assert.AreEqual(3, r.Count)
                Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestLoadBatchValidate()
        Dim cache As New OrmCache
        Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(mpe, cache, New SQL2000Generator))

        Dim t1 As Table1 = q1.GetByID(Of Table1)(1, GetByIDOptions.GetAsIs)

        Dim t() As Table1 = New Table1() {t1, q1.GetByID(Of Table1)(10, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(11, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(20, GetByIDOptions.GetAsIs)}

        Table1.Table2Relation.Load(Of Table1, Table2)(t, False)

        Assert.IsTrue(t1.Table2s.IsInCache)

        Dim tables2 As RelationCmd = t1.Table2s
        Assert.AreEqual(2, tables2.ToList(Of Table2).Count)
        Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(mpe, cache)
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    Dim t2 As Table2 = mt.CreateNewKeyEntity(Of Table2)()
                    tables2.Add(t2)
                    Assert.AreSame(t1, t2.Tbl)

                    Assert.AreEqual(3, tables2.ToList(Of Table2).Count)
                    Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

                    mt.AcceptModifications()
                End Using

                Assert.IsFalse(tables2.HasChanges)
                Dim r As ReadOnlyEntityList(Of Table2) = tables2.ToList(Of Table2)(mgr)
                Assert.AreEqual(3, r.Count)
                Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
