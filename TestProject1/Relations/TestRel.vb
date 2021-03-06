﻿Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

<TestClass()> Public Class TestRel

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

    <TestMethod()> Public Sub TestFind()
        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        For Each t2 As Table2 In r
            Assert.IsFalse(t2.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestFindFilter()
        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.Where(Ctor.prop(GetType(Table2), "Money").eq(2)).ToList(Of Table2)()

        Assert.AreEqual(1, r.Count)

        Assert.AreEqual(Of Decimal)(2, r(0).Money)
    End Sub

    <TestMethod()> Public Sub TestFindTop()
        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.Top(1).OrderBy(SCtor.prop(GetType(Table2), "Money").desc).ToList(Of Table2)()

        Assert.AreEqual(1, r.Count)

        Assert.AreEqual(Of Decimal)(2, r(0).Money)
    End Sub

    <TestMethod()> Public Sub TestLoad()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)

        t.Table2s.LoadObjects()

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        For Each t2 As Table2 In r
            Assert.IsTrue(t2.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestLoad2()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        For Each t2 As Table2 In r
            Assert.IsFalse(t2.InternalProperties.IsLoaded)
        Next

        t.Table2s.LoadObjects()

        Assert.IsTrue(r(0).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestLoadRange()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)
        Dim q As RelationCmd = t.Table2s
        q.LoadObjects(0, 1)

        Assert.AreEqual(1, q.LastExecutionResult.RowCount)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        Assert.IsTrue(r(0).InternalProperties.IsLoaded)
        Assert.IsFalse(r(1).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestLoadRange2()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)
        Dim q As RelationCmd = t.Table2s

        Dim r As ReadOnlyEntityList(Of Table2) = q.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        Assert.IsFalse(r(0).InternalProperties.IsLoaded)
        Assert.IsFalse(r(1).InternalProperties.IsLoaded)

        q.LoadObjects(0, 1)

        Assert.IsTrue(r(0).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestLoadRange2005()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache, New MSSQL2005Generator))

        Dim t As Table1 = q1.GetByID(Of Table1)(1)
        Dim q As RelationCmd = t.Table2s
        q.LoadObjects(0, 1)

        Assert.AreEqual(1, q.LastExecutionResult.RowCount)

        Dim r As ReadOnlyEntityList(Of Table2) = t.Table2s.ToList(Of Table2)()

        Assert.AreEqual(2, r.Count)

        Assert.IsTrue(r(0).InternalProperties.IsLoaded)
        Assert.IsFalse(r(1).InternalProperties.IsLoaded)
    End Sub

    <TestMethod()> Public Sub TestAdd()

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))
            Dim q1 As New QueryCmd()
            Dim t As Table1 = q1.GetByID(Of Table1)(1, mgr)

            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    Dim t2 As Table2 = mt.CreateNewKeyEntity(Of Table2)()
                    Assert.IsNull(t2.Tbl)

                    t.Table2s.Add(t2)

                    Assert.IsNotNull(t2.Tbl)

                    'Assert.IsTrue(t.InternalProperties.HasChanges)
                    Assert.IsTrue(t2.InternalProperties.HasChanges)
                    Assert.IsTrue(t.InternalProperties.HasRelaionalChanges)
                    Assert.IsFalse(t2.InternalProperties.HasRelaionalChanges)
                End Using
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestLoadBatch()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache, New MSSQL2005Generator))

        Dim t() As Table1 = New Table1() {q1.GetByID(Of Table1)(1, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(10, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(11, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(20, GetByIDOptions.GetAsIs)}

        Table1.Table2Relation.Load(Of Table1, Table2)(t, False)

        Assert.IsTrue(q1.GetByID(Of Table1)(1, GetByIDOptions.GetAsIs).Table2s.IsInCache)
    End Sub

    <TestMethod()> Public Sub TestLoadBatchLoad()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache, New MSSQL2005Generator))

        Dim t() As Table1 = New Table1() {q1.GetByID(Of Table1)(1, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(10, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(11, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(20, GetByIDOptions.GetAsIs)}

        Dim r As ReadOnlyList(Of Table2) = Table1.Table2Relation.Load(Of Table1, Table2)(t, True)

        Assert.AreEqual(2, r.Count)

        For Each tb As Table2 In r
            Assert.IsTrue(tb.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestLoadBatch2()
        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache, New MSSQL2005Generator))

        Dim t() As Table1 = New Table1() {q1.GetByID(Of Table1)(1, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(10, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(11, GetByIDOptions.GetAsIs), q1.GetByID(Of Table1)(20, GetByIDOptions.GetAsIs)}

        Dim r As ReadOnlyList(Of _ISinglePKEntity) = Table1.Table2Relation.Load(Of Table1, _ISinglePKEntity)(t, False)

        Assert.AreEqual(2, r.Count)
    End Sub

    <TestMethod()>
    Public Sub TestJoinRelation()

        Dim cache As New OrmCache

        Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), cache, New MSSQL2005Generator))

        Dim rel As New RelationDesc(GetType(Table2), "Table1")
        q1.From(GetType(Table1)).Join(
            JCtor.join_relation(New RelationDescEx(GetType(Table1), rel))
            ).
        Where(Ctor.prop(GetType(Table2), "Money").greater_than(10))

        q1.ToList(Of Table1)()
    End Sub

    '<TestMethod()> Public Sub TestLoadBatchValidate2()
    '    Dim cache As New OrmCache
    '    Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

    '    Dim q1 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(mpe, cache, New MSSQL2005Generator))

    '    Dim t1 As Table1 = q1.GetByID(Of Table1)(1)

    '    Dim t() As Table1 = New Table1() {t1, q1.GetByID(Of Table1)(10), q1.GetByID(Of Table1)(11), q1.GetByID(Of Table1)(20)}

    '    Table1.Table2Relation.Load(Of Table1, Table2)(t, False)

    '    Assert.IsTrue(t1.Table2s.IsInCache)

    '    Dim tables2 As QueryCmd = t1.Table2s _
    '        .Join(JCtor.join(GetType(Table3)).on(GetType(Table3), "ID").eq(GetType(Table2), "ID"))

    '    Assert.AreEqual(1, tables2.ToList(Of Table2).Count)

    '    Assert.IsFalse(tables2.LastExecutionResult.CacheHit)

    '    Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(mpe, cache)
    '        mgr.BeginTransaction()
    '        Try
    '            Using mt As New ModificationsTracker(mgr)
    '                Dim t2 As Table3 = mt.CreateNewKeyEntity(Of Table3)()
    '                t2.Code = 1
    '                t2.RefObject = t1

    '                Assert.AreEqual(1, tables2.ToList(Of Table2).Count)
    '                Assert.IsTrue(tables2.LastExecutionResult.CacheHit)

    '                mt.AcceptModifications()
    '            End Using

    '            Assert.AreEqual(1, tables2.ToList(Of Table2)(mgr).Count)
    '            Assert.IsFalse(tables2.LastExecutionResult.CacheHit)

    '        Finally
    '            mgr.Rollback()
    '        End Try
    '    End Using
    'End Sub
End Class
