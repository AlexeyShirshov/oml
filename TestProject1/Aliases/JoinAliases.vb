Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Criteria.Joins

<TestClass()> Public Class JoinAliases

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
        Dim t1 As New EntityAlias(GetType(Table1))
        Dim t2 As New EntityAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q.From(t1).Join(JCtor.join(t2).[on](t1, "ID").eq(t2, "Enum")).Select(FCtor.count)

        Dim q2 As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        q2.From(t1).Select(FCtor.count)

        Assert.AreEqual(2, q.SingleSimple(Of Integer))
        Assert.AreEqual(3, q2.SingleSimple(Of Integer))
    End Sub

    <TestMethod()> Public Sub TestOuterJoin()
        Dim t1 As New EntityAlias(GetType(Entity))
        Dim t2 As New EntityAlias(GetType(Entity4))

        Dim q As New QueryCmd(Function() TestManager.CreateManager(New ObjectMappingEngine("joins")))
        q.From(t2).Join(JCtor.join(t1).[on](t1, "ID").eq(t2, "ID")).Select(FCtor.count)

        Assert.AreEqual(3, q.SingleSimple(Of Integer))

        q.Join(JCtor.left_join(t1).[on](t1, "ID").eq(t2, "ID")).Select(FCtor.count)

        Assert.AreEqual(12, q.SingleSimple(Of Integer))
    End Sub

    <TestMethod()> Public Sub TestOuterJoinWhere()
        Dim t1 As New EntityAlias(GetType(Entity))
        Dim t2 As New EntityAlias(GetType(Entity4))

        Dim q As New QueryCmd(Function() TestManager.CreateManager(New ObjectMappingEngine("joins")))
        q.From(t2).Join(JCtor.join(t1).[on](t1, "ID").eq(t2, "ID")).Select(FCtor.count)

        Assert.AreEqual(3, q.SingleSimple(Of Integer))

        q.Join(JCtor.left_join(t1).[on](t1, "ID").eq(t2, "ID")) _
            .Select(FCtor.count) _
            .Where(Ctor.prop(t1, "Char").eq("h"))

        Assert.AreEqual(1, q.SingleSimple(Of Integer))
    End Sub
End Class
