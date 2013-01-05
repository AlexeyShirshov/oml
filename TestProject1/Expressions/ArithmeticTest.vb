Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm
Imports Worm.Database

<TestClass()> Public Class ArithmeticTest

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

    <TestMethod()> Public Sub TestAdd()
        Dim mpe As New ObjectMappingEngine
        Dim stmt As New SQL2000Generator
        Dim e As BinaryExpression = ECtor.Param(1).Add(2)
        Dim r As Object = Nothing
        e.Eval(mpe, Nothing, Nothing, r)

        Assert.AreEqual(3, r)

        Dim e2 As BinaryExpression = ECtor.Param(1) + 4
        e2.Eval(mpe, Nothing, Nothing, r)

        Assert.AreEqual(5, r)

        Dim e3 As BinaryExpression = ECtor.Literal(1).AddLiteral(10)

        Assert.AreEqual("(1+10)", e3.MakeStatement(mpe, Nothing, stmt, Nothing, Nothing, Nothing, MakeStatementMode.None, Nothing))

        Dim e4 As BinaryExpression = ECtor.Literal(1) + 10

        Dim p As New ParamMgr(stmt, "p")
        Assert.AreEqual("(1+@p1)", e4.MakeStatement(mpe, Nothing, stmt, p, Nothing, Nothing, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestComplex()
        Dim mpe As New ObjectMappingEngine
        Dim stmt As New SQL2000Generator
        Dim e As BinaryExpression = ECtor.Param(1).Add(20).Subtruct(5).Multiply(1).Divide(2)
        Dim r As Object = Nothing
        e.Eval(mpe, Nothing, Nothing, r)

        Assert.AreEqual(8, r)

        Dim e2 As BinaryExpression = ECtor.Param(1) + 4 - 9 * 4 / 6
        e2.Eval(mpe, Nothing, Nothing, r)

        Assert.AreEqual(-1, r)

        Dim e3 As BinaryExpression = ECtor.Literal(1).AddLiteral(10).SubtructLiteral(9).MultiplyLiteral(10).DivideLiteral(1).ModuloLiteral(45)

        Assert.AreEqual("(((((1+10)-9)*10)/1)%45)", e3.MakeStatement(mpe, Nothing, stmt, Nothing, Nothing, Nothing, MakeStatementMode.None, Nothing))
    End Sub
End Class
