Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Linq

<TestClass()> Public Class TestJoins

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

    <TestMethod()> Public Sub TestJoin()

        Dim ctx As New WormLinqContext(TestLinq.GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim e2 As QueryWrapperT(Of TestProject1.Table2) = ctx.CreateQueryWrapper(Of TestProject1.Table2)()

        Dim q = From t In e _
                 Join t2 In e2 On t Equals t2.Tbl
        Dim l = q.ToList

    End Sub

    <TestMethod()> Public Sub TestSelect()

        Dim ctx As New WormLinqContext(TestLinq.GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim e2 As QueryWrapperT(Of TestProject1.Table2) = ctx.CreateQueryWrapper(Of TestProject1.Table2)()

        Dim q = From t In e _
                 Join t2 In e2 On t Equals t2.Tbl _
                 Select t.Code, t2.Money
        Dim l = q.ToList

    End Sub

    <TestMethod()> Public Sub TestSelfJoin()

        Dim ctx As New WormLinqContext(TestLinq.GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        'Dim e2 As QueryWrapperT(Of TestProject1.Table2) = ctx.CreateQueryWrapper(Of TestProject1.Table2)()

        Dim q = From t In e _
                 Join t2 In e On t.Enum Equals t2.EnumStr _
                 Where t.Enum = TestProject1.Enum1.sec
        Dim l = q.ToList

    End Sub
End Class
