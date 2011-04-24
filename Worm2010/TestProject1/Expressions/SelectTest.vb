Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Query

<TestClass()> Public Class SelectTest

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

    <TestMethod()> Public Sub TestGroupExpression()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQLGenerator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim g As New EntityExpression("Str", GetType(Entity2))
        Dim g2 As New EntityExpression("ID", GetType(Entity2))

        Dim b As New BinaryExpressionBase(g, g2)
        Dim ge As New GroupExpression(b)

        Assert.AreEqual(ge.Expression, ge)
        Assert.AreEqual("None$$Comma(TestProject1.Entity2$Str,TestProject1.Entity2$ID)", ge.GetDynamicString)
        Assert.AreEqual("None$$Comma(TestProject1.Entity2$Str,TestProject1.Entity2$ID)", ge.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Dim al2 As String = almgr.AddTable(CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(0), New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("group by " & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "," & al2 & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("ID").SourceFieldExpression, ge.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestGroupExpressionComplex()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQLGenerator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim g As New EntityExpression("Str", GetType(Entity2))
        Dim g2 As New EntityExpression("ID", GetType(Entity2))

        Dim b As New BinaryExpressionBase(g, New BinaryExpression(g2, BinaryOperationType.Add, New ParameterExpression("e")))
        Dim ge As New GroupExpression(b)

        Assert.AreEqual(ge.Expression, ge)
        Assert.AreEqual("None$$Comma(TestProject1.Entity2$Str,Add(TestProject1.Entity2$ID,e))", ge.GetDynamicString)
        Assert.AreEqual("None$$Comma(TestProject1.Entity2$Str,Add(TestProject1.Entity2$ID,scalarval))", ge.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Dim al2 As String = almgr.AddTable(CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(0), New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("group by " & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & ",(" & al2 & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("ID").SourceFieldExpression & "+@p1)", ge.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestGroupipingSet()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New MSSQL2008Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim g As New EntityExpression("Str", GetType(Entity2))
        Dim g2 As New EntityExpression("ID", GetType(Entity2))

        Dim b As New BinaryExpressionBase(g, g2)
        Dim b2 As New BinaryExpressionBase(g)
        Dim b3 As New BinaryExpressionBase(b, b2)
        b.Parentheses = True
        b2.Parentheses = True

        Dim ge As New GroupExpression(GroupExpression.SummaryValues.GroupingSets, b3)

        Assert.AreEqual(ge.Expression, ge)
        Assert.AreEqual("GroupingSets$$Comma(Comma(TestProject1.Entity2$Str,TestProject1.Entity2$ID),Comma(TestProject1.Entity2$Str))", ge.GetDynamicString)
        Assert.AreEqual("GroupingSets$$Comma(Comma(TestProject1.Entity2$Str,TestProject1.Entity2$ID),Comma(TestProject1.Entity2$Str))", ge.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Dim al2 As String = almgr.AddTable(CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(0), New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("group by grouping sets((" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "," & al2 & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("ID").SourceFieldExpression & "),(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "))", ge.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestSortExpression()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQLGenerator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim g As New SortExpression(New EntityExpression("Str", GetType(Entity2)))
        Dim g2 As New SortExpression(SortExpression.SortType.Desc, New EntityExpression("ID", GetType(Entity2)))

        Dim b As New BinaryExpressionBase(g, g2)

        Assert.AreEqual(b.Expression, b)
        Assert.AreEqual("Comma(Asc$$TestProject1.Entity2$Str,Desc$$TestProject1.Entity2$ID)", b.GetDynamicString)
        Assert.AreEqual("Comma(Asc$$TestProject1.Entity2$Str,Desc$$TestProject1.Entity2$ID)", b.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Dim al2 As String = almgr.AddTable(CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(0), New EntityUnion(GetType(Entity2)))

        Assert.AreEqual(al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "," & al2 & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("ID").SourceFieldExpression & " desc", b.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub

    <TestMethod()> Public Sub TestAggregateExpression()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQLGenerator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim g As New AggregateExpression(AggregateExpression.AggregateFunction.Average, New EntityExpression("Str", GetType(Entity2)))
        Dim g2 As New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression("ID", GetType(Entity2)))
        Dim g3 As New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression("Str", GetType(Entity2)))

        Dim b As New BinaryExpressionBase(New BinaryExpressionBase(g, g2), g3)

        Assert.AreEqual(b.Expression, b)
        Assert.AreEqual("Comma(Comma(Average$False$TestProject1.Entity2$Str,Max$False$TestProject1.Entity2$ID),Min$False$TestProject1.Entity2$Str)", b.GetDynamicString)
        Assert.AreEqual("Comma(Comma(Average$False$TestProject1.Entity2$Str,Max$False$TestProject1.Entity2$ID),Min$False$TestProject1.Entity2$Str)", b.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))
        Dim al2 As String = almgr.AddTable(CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(0), New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("avg(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "),max(" & al2 & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("ID").SourceFieldExpression & "),min(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & ")", b.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))
    End Sub
End Class
