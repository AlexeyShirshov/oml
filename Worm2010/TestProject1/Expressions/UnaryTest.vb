Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Expressions2
Imports Worm
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Query
Imports System.Collections

<TestClass()> Public Class UnaryTest

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

    <TestMethod()> Public Sub TestLiteral()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create
        Dim l As New LiteralExpression("x")

        Assert.AreEqual(l.Expression, l)
        Assert.AreEqual("x", l.GetDynamicString)
        Assert.AreEqual("litval", l.GetStaticString(mpe, contextFilter))
        Assert.AreEqual("x", l.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestParameter()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim p As New ParameterExpression("x")

        Assert.AreEqual(p.Expression, p)
        Assert.AreEqual("x", p.GetDynamicString)
        Assert.AreEqual("scalarval", p.GetStaticString(mpe, contextFilter))
        Assert.AreEqual("@p1", p.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestEntityExp()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim p As New EntityExpression("Str", GetType(Entity2))

        Assert.AreEqual(p.Expression, p)
        Assert.AreEqual("TestProject1.Entity2$Str", p.GetDynamicString)
        Assert.AreEqual("TestProject1.Entity2$Str", p.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Assert.AreEqual(al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression, p.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestUnary()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim p As New EntityExpression("Str", GetType(Entity2))
        Dim u As New UnaryExpression(UnaryOperationType.Negate, p)

        Assert.AreEqual(u.Expression, u)
        Assert.AreEqual(u.GetExpressions(0), u)
        Assert.AreEqual(u.GetExpressions(1), p)
        Assert.AreEqual("negTestProject1.Entity2$Str", u.GetDynamicString)
        Assert.AreEqual("negTestProject1.Entity2$Str", u.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Assert.AreEqual("-" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression, u.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestUnaryNot()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim p As New EntityExpression("Str", GetType(Entity2))
        Dim u As New UnaryExpression(UnaryOperationType.Not, p)

        Assert.AreEqual(u.Expression, u)
        Assert.AreEqual(u.GetExpressions(0), u)
        Assert.AreEqual(u.GetExpressions(1), p)
        Assert.AreEqual("notTestProject1.Entity2$Str", u.GetDynamicString)
        Assert.AreEqual("notTestProject1.Entity2$Str", u.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Assert.AreEqual("~" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression, u.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestCustom()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As IDictionary = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim p As New EntityExpression("Str", GetType(Entity2))
        Dim u As New CustomExpression("isnull({0})", p)

        Assert.AreEqual(u.Expression, u)
        Assert.AreEqual(u.GetExpressions(0), u)
        Assert.AreEqual(u.GetExpressions(1), p)
        Assert.AreEqual("isnull(TestProject1.Entity2$Str)", u.GetDynamicString)
        Assert.AreEqual("isnull(TestProject1.Entity2$Str)", u.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Assert.AreEqual("isnull(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & ")", u.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

End Class
