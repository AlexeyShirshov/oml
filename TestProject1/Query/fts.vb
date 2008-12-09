Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Criteria
Imports Worm.Criteria.Joins

<TestClass()> Public Class fts

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

    <TestMethod()> Public Sub TestSearch()

        Dim t As Type = GetType(Table1)

        Dim q As QueryCmd = QueryCmd.Search(t, "second", New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)

        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)

        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearch2()

        Dim tbl As New Entities.Meta.SearchFragment(GetType(Table1), "second")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))
        q.From(tbl)

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q = New QueryCmd(Function() TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1")))
        q.From(New Entities.Meta.SearchFragment(GetType(Table1), "xxx"))

        Assert.AreEqual(0, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearch3()

        Dim q As QueryCmd = QueryCmd.Search("first", New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)

        q.SetJoins(JCtor.join(GetType(Table3)).[on](GetType(Table3), "Ref").eq(GetType(Table1), "ID"))

        Assert.AreEqual(2, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
        Assert.AreEqual(2, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        q.Where(PCtor.prop(GetType(Table3), "Code").eq(2))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearch4()

        Dim tbl As New Entities.Meta.SearchFragment(GetType(Table1), "first")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))
        q.SetJoins(JCtor.join("Table3").onM2M(GetType(Table1)))
        q.Where(PCtor.prop("Table3", "Code").eq(2)).From(tbl)

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearch5()

        Dim tbl As New Entities.Meta.SearchFragment(GetType(Table1), "second")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))

        q.Where(PCtor.prop(GetType(Table1), "Code").eq(2)).From(tbl)

        Assert.AreEqual(0, q.ToList(Of Table1)().Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearchM2M()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))

        q.Where(PCtor.prop(GetType(Table3), "ID").eq(1))
        Dim t As Table3 = q.Single(Of Table3)()

        Dim r As ReadOnlyEntityList(Of Table1) = t.M2MNew.Search("second", GetType(Table1)).ToList(Of Table1)()
        Assert.AreEqual(0, r.Count)

        r = t.M2MNew.Search("second").ToList(Of Table1)()
        Assert.AreEqual(0, r.Count)
        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    End Sub

    <TestMethod()> Public Sub TestSearchM2MDyn()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New ObjectMappingEngine("1"))))

        q.Where(PCtor.prop(GetType(Table3), "ID").eq(1))
        Dim t As Table3 = q.Single(Of Table3)()

        Dim r As ReadOnlyEntityList(Of Table1) = t.M2MNew.Search("first").ToEntityList(Of Table1)()
        Assert.IsFalse(q.LastExecitionResult.CacheHit)

        Assert.AreEqual(1, r.Count)
    End Sub

End Class
