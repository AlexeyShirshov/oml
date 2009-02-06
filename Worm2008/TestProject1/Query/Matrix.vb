Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Entities
Imports Worm.Cache
Imports System.Collections.ObjectModel

<TestClass()> Public Class Matrix

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

    Class localcache
        Inherits OrmCache

        Protected Overrides Function CreateListConverter() As Worm.Cache.IListObjectConverter
            Return New ListConverter
        End Function
    End Class

    <TestMethod()> Public Sub TestSelect()
        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, t2)

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelect2()
        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(GetType(Table1)).Select(GetType(Table1), GetType(Table2)). _
            Join(JCtor.join(GetType(Table2)).on(GetType(Table2), "Table1").eq(GetType(Table1), "ID"))

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelectWithload()
        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, True).SelectAdd(t2, True)

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelectListConverter()
        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared( _
                                  New ObjectMappingEngine("1"), New localcache))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, t2)

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelectCache()
        Dim c As New OrmCache

        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, t2)

        Dim m As ReadonlyMatrix = q.ToMatrix
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        Assert.AreEqual(3, m.Count)

        For Each row As ReadOnlyCollection(Of _IEntity) In m
            Assert.AreEqual(2, row.Count)
            Assert.IsFalse(row(0).IsLoaded)
            Assert.IsFalse(row(1).IsLoaded)
        Next

        m = q.ToMatrix
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        m = q.Select(t1, True).SelectAdd(t2, True).ToMatrix
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        Assert.AreEqual(3, m.Count)

        For Each row As ReadOnlyCollection(Of _IEntity) In m
            Assert.AreEqual(2, row.Count)
            Assert.IsTrue(row(0).IsLoaded)
            Assert.IsTrue(row(1).IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestSelectWebCache()
        Dim c As New localcache

        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, t2)

        Dim m As ReadonlyMatrix = q.ToMatrix
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        Assert.AreEqual(3, m.Count)

        For Each row As ReadOnlyCollection(Of _IEntity) In m
            Assert.AreEqual(2, row.Count)
            Assert.IsFalse(row(0).IsLoaded)
            Assert.IsFalse(row(1).IsLoaded)
        Next

        m = q.ToMatrix
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        m = q.Select(t1, True).SelectAdd(t2, True).ToMatrix
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        Assert.AreEqual(3, m.Count)

        For Each row As ReadOnlyCollection(Of _IEntity) In m
            Assert.AreEqual(2, row.Count)
            Assert.IsTrue(row(0).IsLoaded)
            Assert.IsTrue(row(1).IsLoaded)
        Next
    End Sub
End Class
