Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Database
Imports Worm
Imports Worm.Criteria
Imports Worm.Criteria.Joins

<TestClass()> Public Class TestAliasFilters

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

    <TestMethod(), ExpectedException(GetType(Worm.ObjectMappingException))> _
    Public Sub TestFilterWrong()
        Dim t1 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1.Type).Where(Ctor.prop(t1, "Enum").eq(2)).Select(FCtor.count)

        Assert.AreEqual(2, q.SingleSimple(Of Integer))
    End Sub

    <TestMethod()> _
    Public Sub TestFilter()
        Dim t1 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1).Where(Ctor.prop(t1, "Enum").eq(2)).Select(FCtor.count)

        Assert.AreEqual(1, q.SingleSimple(Of Integer))
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.ObjectMappingException))> _
    Public Sub TestFilterWrong2()
        Dim t1 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1).Where(Ctor.prop(GetType(Table1), "Enum").eq(2)).Select(FCtor.count)

        Assert.AreEqual(1, q.SingleSimple(Of Integer))
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Dim t1 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.Select(t1).Where(Ctor.prop(t1, "EnumStr").eq(Enum1.sec)).Top(1).OrderBy(SCtor.prop(t1, "DT"))

        Assert.AreEqual(2, q.Single(Of Table1).ID)
    End Sub


End Class
