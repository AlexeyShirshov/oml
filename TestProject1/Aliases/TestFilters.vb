Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Orm
Imports Worm.Query
Imports Worm.Database
Imports Worm.Database.Criteria.Joins
Imports Worm.Database.Criteria

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

    <TestMethod()> Public Sub TestJoin()
        Dim t1 As New ObjectAlias(GetType(Table1))
        Dim t2 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(t1, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))
        q.SetJoins(JCtor.Join(t2).On(t1, "ID").Eq(t2, "Enum")).SelectAgg(AggCtor.Count)

        Dim q2 As New QueryCmd(t1, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))
        q2.SelectAgg(AggCtor.Count)

        Assert.AreEqual(2, q.SingleSimpleDyn(Of Integer))
        Assert.AreEqual(3, q2.SingleSimpleDyn(Of Integer))
    End Sub

    <TestMethod(), ExpectedException(GetType(Reflection.TargetInvocationException))> _
    Public Sub TestFilterWrong()
        Dim t1 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(t1.Type, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))

        q.Where(Ctor.Field(t1, "Enum").Eq(2)).SelectAgg(AggCtor.Count)

        Assert.AreEqual(2, q.SingleSimpleDyn(Of Integer))
    End Sub

    <TestMethod()> _
    Public Sub TestFilter()
        Dim t1 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(t1, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))

        q.Where(Ctor.Field(t1, "Enum").Eq(2)).SelectAgg(AggCtor.Count)

        Assert.AreEqual(1, q.SingleSimpleDyn(Of Integer))
    End Sub

    <TestMethod(), ExpectedException(GetType(Reflection.TargetInvocationException))> _
    Public Sub TestFilterWrong2()
        Dim t1 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(t1, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))

        q.Where(Ctor.Field(GetType(Table1), "Enum").Eq(2)).SelectAgg(AggCtor.Count)

        Assert.AreEqual(1, q.SingleSimpleDyn(Of Integer))
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Dim t1 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(t1, Function() TestManagerRS.CreateManagerShared(New SQLGenerator("1")))

        q.Where(Ctor.Field(t1, "EnumStr").Eq(Enum1.sec)).Top(1).Sort(Sorting.Field(t1, "DT"))

        Assert.AreEqual(2, q.Single(Of Table1).ID)
    End Sub
End Class
