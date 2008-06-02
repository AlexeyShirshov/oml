Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Linq

<TestClass()> Public Class TestLinq

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
            testContextInstance = Value
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

    Protected Function GetConn() As String
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\wormtest.mdf"))
        Dim conn As String = "Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;"
#Else
        Dim conn As String = "Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest"
#End If
        Return conn
    End Function

    <TestMethod()> Public Sub TestQuery()

        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        'MethodCallExpression
        '	ConstantExpression
        '	UnaryExpression
        '		LambdaExpression
        '			BinaryExpression (coalese)
        '				Left: BinaryExpression (And)
        'k.Code = 2 AndAlso k.ID <> d		Left: BinaryExpression (AndAlso)
        'k.Code = 2				            	Left: BinaryExpression (Equal)
        '						                	Left: MemberExpression
        '						                	Right: UnaryExpression
        '						                		ConstantExpression
        'k.ID <> d			            		Right: UnaryExpression
        '					                		BinnaryExpression (Not Equal)
        '							                	Left: MemberExpression
        '							                	Right: MemberExpression
        'k.Name > "df"			        	Right: UnaryExpression
        '					                	BinaryExpression (GreaterThan)
        '						                	Left: MethodCallExpression
        '						                	Right: ConstantExpression
        '				Right: ConstantExpression
        Dim d As Integer = 10
        Dim q = From k In e Where k.Code = 2  'AndAlso k.ID <> d And k.Name > "df"
        Dim l As IList(Of TestProject1.Table1) = q.ToList
        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(1, l(0).ID)
        Assert.IsFalse(l(0).InternalProperties.IsLoaded)

        Dim o = (From k In e.WithLoad Where k.Code = 2).ToList(0)
        Assert.IsTrue(o.InternalProperties.IsLoaded)

        Assert.IsTrue(q.ToList(0).InternalProperties.IsLoaded)

        'Dim p As New WormLinqProvider(ctx)
        'Dim exp As Expressions.Expression(Of Func(Of Integer, Boolean)) = Function(num) num = 2
        'q = p.CreateQuery(Of TestProject1.Table1)(exp)
        'l = q.ToList

        'Assert.AreEqual(1, l.Count)
        'Assert.AreEqual(1, l(0).ID)
    End Sub

    <TestMethod()> _
    Public Sub TestOrder()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID

        Dim l = q.ToList
        Assert.AreEqual(2, l.Count)
        Assert.AreEqual(2, l(0).ID)

        q = q.OrderByDescending(Function(k) (k.ID))

        l = q.ToList
        Assert.AreEqual(2, l.Count)
        Assert.AreEqual(3, l(0).ID)

        q = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID Descending, k.Enum
        l = q.ToList
        Assert.AreEqual(2, l.Count)
        Assert.AreEqual(3, l(0).ID)

    End Sub

    <TestMethod()> _
    Public Sub TestSelect()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID Select k.Code

        Dim l = q.ToList
        Dim f = l(0)
    End Sub

    <TestMethod()> _
    Public Sub TestSelect2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID Select k.Code, k.CreatedAt

        Dim l = q.ToList
        Dim f = l(0)

        Dim q2 = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID Select New With {k.Code, k.CreatedAt, .G = 3}

        Dim l2 = q2.ToList
        Dim f2 = l(0)
    End Sub

    <TestMethod()> _
    Public Sub TestSelect3()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        'Dim q = From k In e Where TestProject1.Enum1.first < k.Enum Order By k.ID Select k.Code, k.CreatedAt()

        Dim q2 = e.Where(Function(l) l.Enum.Value > TestProject1.Enum1.first).Select(Function(k) k).OrderBy(Function(o) o.ID) '.Where(Function(l As TestProject1.Table1) l.ID = 10)

        Dim r = q2.ToList

        Dim q3 = From k In e Select k.Code, k.ID, k.Enum Where [Enum] = TestProject1.Enum1.first

        Dim r2 = q3.ToList
    End Sub

    'Class cls
    '    Public Sub New(ByVal i As Integer?)

    '    End Sub
    'End Class

    '<TestMethod()> _
    'Public Sub TestSelect3()
    '    Dim ctor = GetType(cls).GetConstructor(New Type() {GetType(Integer?)})
    '    Dim args = New Expressions.Expression() {Expressions.Expression.Constant(10)}
    '    Dim n = Expressions.Expression.[New](ctor, args)

    'End Sub
End Class
