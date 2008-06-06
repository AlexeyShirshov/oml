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

    Public Shared Function GetConn() As String
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

        q = From k In e Where TestProject1.Enum1.first > k.Enum Order By k.ID Descending, k.Enum Descending
        l = q.ToList
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

    <TestMethod()> _
    Public Sub TestDistinct()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Distinct Select k.EnumStr

        Dim l = q.ToList

        Assert.AreEqual(2, l.Count)
        Assert.AreEqual("first", l(0))
        Assert.AreEqual("sec", l(1))

        Dim q2 = From k In e Select k.EnumStr Distinct

        Dim l2 = q2.ToList

        Assert.AreEqual(2, l2.Count)
        Assert.AreEqual("first", l2(0))
        Assert.AreEqual("second", l2(1))

        Dim q3 = From k In e Where k.ID > 0 Distinct Distinct Select k.EnumStr Distinct

        Dim l3 = q3.ToList

        Assert.AreEqual(2, l3.Count)
        Assert.AreEqual("first", l3(0))
        Assert.AreEqual("second", l3(1))
    End Sub

    <TestMethod()> _
    Public Sub TestCount()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).Count

        Assert.AreEqual(3, q)

        Assert.AreEqual(1, (From k In e).Count(Function(d) d.ID = 1))

        Assert.AreEqual(1, (From k In e Where k.ID = 1).Count)

        Dim c = (From k In e).LongCount
        Assert.AreEqual(3L, c)
    End Sub

    <TestMethod()> _
    Public Sub TestFirst()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t As TestProject1.Table1 = e.First
        Assert.IsNotNull(t)

        t = e.First(Function(k) k.ID = 1)
        Assert.IsNotNull(t)
        Assert.AreEqual(1, t.ID)

        t = (From k In e).First
        Assert.IsNotNull(t)
        Assert.IsFalse(t.InternalProperties.IsLoaded)

        Dim t2 = (From k In e Where k.ID = 1).First
        Assert.IsNotNull(t2)

        Dim i? As Integer = (From k In e Select k.Code).First
        Assert.IsTrue(i.HasValue)
        'Assert.IsTrue(t.InternalProperties.IsLoaded)

        i = (From k In e Select k.Code).First(Function(l) l.Value = 45)
        Assert.IsTrue(i.HasValue)
        Assert.AreEqual(45, i)
        Assert.AreEqual(45, (From k In e Where k.Code = 45 Select k.Code).First)

    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentOutOfRangeException))> _
    Public Sub TestFirst2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e Where k.ID = 1).First(Function(r) r.ID = 10)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentOutOfRangeException))> _
    Public Sub TestFirst3()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e Where k.ID = 1 Select k.Code).First(Function(r) r.Value = 145689)
    End Sub

    <TestMethod()> _
    Public Sub TestFirstOrDefault()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e Where k.ID = 1).FirstOrDefault(Function(r) r.ID = 10)
        Assert.IsNull(t)

        t = e.FirstOrDefault
        Assert.IsNotNull(t)

        t = e.FirstOrDefault(Function(r) r.ID = 1493)
        Assert.IsNull(t)

        Dim i = (From k In e Where k.ID = 1 Select k.Code).FirstOrDefault(Function(r) r.Value = 145689)
        Assert.IsFalse(i.HasValue)

    End Sub

    <TestMethod()> _
    Public Sub TestSingle()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e Where k.ID = 1).Single
        Assert.IsNotNull(t)

        Dim i = (From k In e Where k.ID = 1 Select k.Code).Single
        Assert.IsTrue(i.HasValue)
        Assert.AreEqual(2, i.Value)

        Dim w = (From k In e Select k.Code, k.ID).Single(Function(g) g.ID = 1)
        Assert.IsNotNull(w)
        Assert.IsTrue(w.Code.HasValue)
        Assert.AreEqual(2, w.Code.Value)
        Assert.AreEqual(1, w.ID)
    End Sub

    <TestMethod(), ExpectedException(GetType(LinqException))> _
    Public Sub TestSingle2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).Single
    End Sub

    <TestMethod(), ExpectedException(GetType(LinqException))> _
    Public Sub TestSingle3()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Where k.ID = 934785).Single
    End Sub

    <TestMethod()> _
    Public Sub TestSingleOrDefault()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Where k.ID = 934785).SingleOrDefault
        Assert.IsNull(q)
    End Sub

    <TestMethod(), ExpectedException(GetType(LinqException))> _
    Public Sub TestSingleOrDefault2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).SingleOrDefault
    End Sub

    <TestMethod()> _
    Public Sub TestLast()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e).Last
        Assert.IsNotNull(t)

    End Sub

    <TestMethod(), ExpectedException(GetType(LinqException))> _
    Public Sub TestLast2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e).Last(Function(s) s.ID = 234593847)

    End Sub

    <TestMethod()> _
    Public Sub TestLastOrDefault()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e).LastOrDefault(Function(s) s.ID = 234593847)
        Assert.IsNull(t)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestDefaultIfEmpty()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = (From k In e Where k.ID = 34957).DefaultIfEmpty
        Dim r = t.ToList(0)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestContains()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()
        Dim tt = (From k In e).First
        Dim t = (From k In e).Contains(tt)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestConcat()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where k.ID = 1
        Dim q2 = q.Concat(From k In e Where k.ID = 2)
        Dim l = q2.ToList

        Assert.AreEqual(2, l.Count)
        Assert.AreEqual(1, l(0).ID)
        Assert.AreEqual(2, l(1).ID)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestUnion()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e
        Dim q2 = q.Union(From k In e Where k.ID = 2)
        Dim l = q2.ToList

        Assert.AreEqual(3, l.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestElementAt()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).ElementAt(2)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestElementAtOrDefault()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).ElementAtOrDefault(2)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestIntersect()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e
        Dim q2 = q.Concat(From k In e Where k.ID = 2)
        Dim l = q2.ToList

        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(2, l(0).ID)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestSecuenceEqual()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q2 = (From k In e).SequenceEqual(From k In e Where k.ID = 2)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestReverse()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q2 = (From k In e).Reverse.ToList
    End Sub

    <TestMethod()> _
    Public Sub TestTake()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = From k In e Take 1
        Assert.AreEqual(1, t.ToList.Count)

        Dim t2 = From k In e Where k.ID > 0 Select k.Code Take 2
        Assert.AreEqual(2, t2.ToList.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(Reflection.TargetInvocationException))> _
    Public Sub TestSkip2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = From k In e Skip 1
        Assert.AreEqual(2, t.ToList.Count)

        t = From k In e Skip 1 Take 1
        Assert.AreEqual(1, t.ToList.Count)

        t = From k In e Skip 1 Take 1 Order By k.Code Descending
        Dim l = t.ToList
        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(3, l(0).ID)

        Dim t2 = From k In e Where k.ID > 0 Select k.Code Take 2 Skip 1
        Assert.AreEqual(2, t2.ToList.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestSkip()
        Dim ctx As New WormMSSQL2005DBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim t = From k In e Skip 1
        Assert.AreEqual(2, t.ToList.Count)

        t = From k In e Skip 1 Take 2
        Assert.AreEqual(2, t.ToList.Count)

        t = From k In e Skip 1 Take 1 Order By k.Code Descending
        Dim l = t.ToList
        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(3, l(0).ID)

        Dim t2 = From k In e Where k.ID > 0 Select k.Code Take 2 Skip 1
        Assert.AreEqual(1, t2.ToList.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestAll()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).All(Function(d) d.ID = 1)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestAny()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e).Any(Function(d) d.ID = 1)
    End Sub

    <TestMethod(), ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestAggregate()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k.Code).Aggregate(Function(current, [next]) current + [next])
    End Sub

    <TestMethod()> _
    Public Sub TestAverage()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k.Code).Average()
        Assert.IsTrue(q.HasValue)
        Assert.AreEqual(Of Double)(2990, q.Value)

        q = (From k In e Select k.ID).Average()
        Assert.IsTrue(q.HasValue)
        Assert.AreEqual(Of Double)(2, q.Value)

        Dim q2 = (From k In e Where k.ID > 0 Select k.Code).Average()
        Assert.IsTrue(q2.HasValue)
        Assert.AreEqual(Of Double)(2990, q2.Value)

        q2 = (From k In e Where k.ID > 0).Average(Function(o) o.Code + o.ID)
        Assert.IsTrue(q2.HasValue)
        Assert.AreEqual(Of Double)(2992, q2.Value)
    End Sub

    <TestMethod()> _
    Public Sub TestMax()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k.Code).Max()
        Assert.AreEqual(8923, q)

        q = (From k In e Where k.ID > 2 Select k.Code).Max()
        Assert.AreEqual(45, q)
    End Sub

    <TestMethod()> _
    Public Sub TestMin()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k.Code).Min()
        Assert.AreEqual(2, q)

        q = (From k In e Select k Where k.ID > 0 Take 1 Select k.Code).Min()
        Assert.AreEqual(2, q)

        Dim q2 = (From k In e Select k Where k.ID > 0 Select k.Name).Min()
        Assert.AreEqual("first", q2)
    End Sub

    <TestMethod()> _
    Public Sub TestMin2()
        Dim ctx As New WormMSSQL2005DBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k.Code Skip 1).Min()
        Assert.AreEqual(45, q)

        q = (From k In e Select k Take 2 Select k.Code + 1).Min()
        Assert.AreEqual(3, q)

        q = (From k In e Select k.Code, k.ID Take 2 Select Code + ID).Min()
        Assert.AreEqual(3, q)

        q = (From k In e Where k.ID > 0 Skip 1 Take 1 Order By k.Name Descending Select k.Code).Min()
        Assert.AreEqual(8923, q)
    End Sub

    <TestMethod()> _
    Public Sub TestSum()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Select k).Sum(Function(t) t.Code)
        Assert.AreEqual(8970, q)

        q = (From k In e Select k Take 2).Sum(Function(r) r.Code)
        Assert.AreEqual(8925, q)

    End Sub

    <TestMethod(), ExpectedException(GetType(Reflection.TargetInvocationException))> _
    Public Sub TestSum2()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Where k.ID > 0 Select k.Code Skip 1 Take 1 Order By Code Descending).Sum()
        Assert.AreEqual(8923, q)
    End Sub

    <TestMethod()> _
    Public Sub TestSum3()
        Dim ctx As New WormMSSQL2005DBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Where k.ID > 0 Select k.Code Skip 1 Take 1 Order By Code Descending).Sum()
        Assert.AreEqual(45, q)

        q = (From k In e Select k.Code, k.Name).Sum(Function(r) r.Code)
        Assert.AreEqual(8970, q)

        q = (From k In e Where k.ID > 0 Select k.Code, k.Name Skip 1 Take 1 Order By Name Descending).Sum(Function(r) r.Code)
        Assert.AreEqual(45, q)
    End Sub

    <TestMethod()> _
    Public Sub TestLet()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (From k In e Let r = k.Name + "d" Select r).ToList

        Dim q2 = (From k In e Let r = k.ID + k.Code Select r).ToList

        Dim q3 = (From k In e Let r = k.ID + k.Code, r1 = k.Name Select r, r1, k.CreatedAt).ToList
    End Sub

    <TestMethod()> _
    Public Sub TestWhere()
        Dim ctx As New WormDBContext(GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where (k.Code > 0 AndAlso k.CreatedAt < Now) OrElse (k.ID > 0 Or k.ID <> 0) _
                Select k.Code, k.CreatedAt _
                Where Code = 2 AndAlso CreatedAt.Year = 3

        'Dim q2 = From k In e Where (k.Code > 0 AndAlso k.CreatedAt < Now) OrElse (k.ID > 0 Or k.ID <> 0) _
        '        Where k.Code = 2

        Dim l = q.ToList

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
