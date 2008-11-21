Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Database.Criteria.Joins

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

        Dim q As New SearchQueryCmd(t, "second", New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
    End Sub

    <TestMethod()> Public Sub TestSearch2()

        Dim tbl As New Orm.Meta.SearchFragment(GetType(Table1), "second")

        Dim q As New QueryCmd(tbl, New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
    End Sub

    <TestMethod()> Public Sub TestSearch3()

        Dim q As New SearchQueryCmd("first", New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)

        q.AddJoins(JCtor.Join(GetType(Table3)).On(GetType(Table3), "Ref").Eq(GetType(Table1), "ID"))

        Assert.AreEqual(2, q.ToList(Of Table1)().Count)

        q.Where(Ctor.Field(GetType(Table3), "Code").Eq(2))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
    End Sub

    <TestMethod()> Public Sub TestSearch4()

        Dim tbl As New Orm.Meta.SearchFragment(GetType(Table1), "second")

        Dim q As New QueryCmd(tbl, New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))
        q.Where(Ctor.AutoTypeField("Code").Eq(2))

        Assert.AreEqual(1, q.ToList(Of Table1)().Count)
    End Sub

    <TestMethod()> Public Sub TestSearchM2M()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))

        q.Where(Ctor.AutoTypeField("ID").Eq(1))
        Dim t As Table3 = q.Single(Of Table3)()

        t.M2MNew.Search("second", GetType(Table1)).ToList(Of Table1)()

        t.M2MNew.Search("second").ToList(Of Table1)()
    End Sub

    <TestMethod()> Public Sub TestSearchM2MDyn()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerSharedFullText(New SQLGenerator("1"))))

        q.Where(Ctor.AutoTypeField("ID").Eq(1))
        Dim t As Table3 = q.Single(Of Table3)()

        t.M2MNew.Search("second").ToEntityList(Of Table1)()
    End Sub

End Class
