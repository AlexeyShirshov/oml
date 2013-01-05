Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Query

<TestClass()> Public Class BinaryTest

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

    <TestMethod()> Public Sub TestMinus()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Subtract, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("Subtract(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("Subtract(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "-@p1)", a.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestPlus()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Add, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("Add(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("Add(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "+@p1)", a.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestMul()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Multiply, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("Mul(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("Mul(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "*@p1)", a.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestDiv()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Divide, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("Divide(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("Divide(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("(" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "/@p1)", a.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestEqual()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Modulo, p)
        Dim p2 As New ParameterExpression("y")
        Dim a2 As New BinaryExpression(a, BinaryOperationType.Equal, p2)

        Assert.AreEqual(a2.Expression, a2)
        Assert.AreEqual("Equal(Mod(TestProject1.Entity2$Str,x),y)", a2.GetDynamicString)
        Assert.AreEqual("Equal(Mod(TestProject1.Entity2$Str,scalarval),scalarval)", a2.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Assert.AreEqual("((" & al & "." & mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression & "%@p1) = @p2)", a2.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestAnd()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Modulo, p)
        Dim p2 As New ParameterExpression("y")
        Dim a2 As New BinaryExpression(a, BinaryOperationType.Equal, p2)

        Dim l As New LiteralExpression("s")
        Dim p3 As New ParameterExpression("5")
        Dim a3 As New BinaryExpression(l, BinaryOperationType.GreaterEqualThan, p3)

        Dim a4 As New BinaryExpression(a2, BinaryOperationType.And, a3)

        Assert.AreEqual(a4.Expression, a4)
        Assert.AreEqual("And(Equal(Mod(TestProject1.Entity2$Str,x),y),GreaterEqualThan(s,5))", a4.GetDynamicString)
        Assert.AreEqual("And(Equal(Mod(TestProject1.Entity2$Str,scalarval),scalarval),GreaterEqualThan(litval,scalarval))", a4.GetStaticString(mpe, contextFilter))

        Dim tbl As SourceFragment = CType(mpe.GetEntitySchema(GetType(Entity2)), IMultiTableObjectSchema).GetTables(1)
        Dim al As String = almgr.AddTable(tbl, New EntityUnion(GetType(Entity2)))

        Dim str As String = mpe.GetEntitySchema(GetType(Entity2)).FieldColumnMap("Str").SourceFieldExpression

        Assert.AreEqual("(((" & al & "." & str & "%@p1) = @p2) and (s >= @p3))", _
            a4.MakeStatement(mpe, Nothing, stmt, pmgr, almgr, contextFilter, MakeStatementMode.None, Nothing))

    End Sub

    <TestMethod()> Public Sub TestReplaceExpression()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.BitAnd, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim p2 As New ParameterExpression("y")

        Dim a2 As IComplexExpression = a.ReplaceExpression(p, p2)

        Assert.AreNotEqual(a2, a)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,y)", a2.GetDynamicString)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,scalarval)", a2.GetStaticString(mpe, contextFilter))

    End Sub

    <TestMethod()> Public Sub TestRemoveExpression()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.BitAnd, p)

        Assert.AreEqual(a.Expression, a)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,x)", a.GetDynamicString)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str,scalarval)", a.GetStaticString(mpe, contextFilter))

        Dim a2 As IComplexExpression = a.ReplaceExpression(p, Nothing)

        Assert.AreNotEqual(a2, a)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str)", a2.GetDynamicString)
        Assert.AreEqual("BAnd(TestProject1.Entity2$Str)", a2.GetStaticString(mpe, contextFilter))

    End Sub

    <TestMethod()> Public Sub TestEval()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.NotEqual, p)

        Assert.IsFalse(a.CanEval(mpe))

        a = New BinaryExpression(e, BinaryOperationType.Equal, p)

        'Assert.IsTrue(a.CanEval(mpe))

        'Dim e2 As New EntityExpression("ID", GetType(Entity2))
        'Dim p3 As New ParameterExpression("5")
        'Dim a3 As New BinaryExpression(e2, BinaryOperationType.Equal, p3)

        'Assert.IsTrue(New BinaryExpression(a, BinaryOperationType.And, a3).CanEval(mpe))

        'Assert.IsFalse(New BinaryExpression(a, BinaryOperationType.Or, a3).CanEval(mpe))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeDynString()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Equal, p)

        Dim obj As New Entity2
        obj.Str = "x"

        Assert.AreEqual(a.GetDynamicString, a.MakeDynamicString(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeDynStringComplex()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Equal, p)

        Dim e2 As New EntityExpression("ID", GetType(Entity2))
        Dim p2 As New ParameterExpression(1)
        Dim a2 As New BinaryExpression(e2, BinaryOperationType.Equal, p2)

        Dim a3 As New BinaryExpression(a, BinaryOperationType.And, a2)

        Dim obj As New Entity2(1, Nothing, mpe)
        obj.Str = "x"

        Assert.AreEqual(a3.GetDynamicString, a3.MakeDynamicString(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub

    <TestMethod()> _
    Public Sub TestTestMethod()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Equal, p)

        Dim obj As New Entity2
        obj.Str = "x"

        Assert.AreEqual(IParameterExpression.EvalResult.Found, a.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))

        obj.Str = "y"
        Assert.AreEqual(IParameterExpression.EvalResult.NotFound, a.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub

    <TestMethod()> _
    Public Sub TestTestMethodComplex()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Equal, p)

        Dim e2 As New EntityExpression("ID", GetType(Entity2))
        Dim p2 As New ParameterExpression(1)
        Dim a2 As New BinaryExpression(e2, BinaryOperationType.Equal, p2)

        Dim a3 As New BinaryExpression(a, BinaryOperationType.And, a2)

        Dim obj As New Entity2(1, Nothing, mpe)
        obj.Str = "x"

        Assert.AreEqual(IParameterExpression.EvalResult.Found, a.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))

        obj.Str = "y"
        Assert.AreEqual(IParameterExpression.EvalResult.NotFound, a.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub

    <TestMethod()> _
    Public Sub TestTestMethod2()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.NotEqual, p)

        Dim obj As New Entity2
        obj.Str = "x"

        Assert.AreEqual(IParameterExpression.EvalResult.NotFound, a.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub

    <TestMethod()> _
    Public Sub TestTestMethodComplex2()
        Dim mpe As New ObjectMappingEngine
        Dim contextFilter As Object = Nothing
        Dim stmt As New SQL2000Generator
        Dim pmgr As New ParamMgr(stmt, "p")
        Dim almgr As IPrepareTable = AliasMgr.Create

        Dim e As New EntityExpression("Str", GetType(Entity2))
        Dim p As New ParameterExpression("x")
        Dim a As New BinaryExpression(e, BinaryOperationType.Equal, p)

        Dim e2 As New EntityExpression("ID", GetType(Entity2))
        Dim p2 As New ParameterExpression(1)
        Dim a2 As New BinaryExpression(e2, BinaryOperationType.Equal, p2)

        Dim a3 As New BinaryExpression(a, BinaryOperationType.Or, a2)

        Dim obj As New Entity2(2, Nothing, mpe)
        obj.Str = "x"

        Assert.AreEqual(IParameterExpression.EvalResult.Found, a3.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))

        a3 = New BinaryExpression(a, BinaryOperationType.And, a2)

        Assert.AreEqual(IParameterExpression.EvalResult.NotFound, a3.Test(mpe, mpe.GetEntitySchema(GetType(Entity2)), obj))
    End Sub
End Class
