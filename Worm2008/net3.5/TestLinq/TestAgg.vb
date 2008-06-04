Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Linq

<TestClass()> Public Class TestAgg

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

    <TestMethod()> _
    Public Sub TestCount()
        Dim ctx As New WormDBContext(TestLinq.GetConn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = (Aggregate k In e Into Count())
        Assert.AreEqual(3, q)

        q = (Aggregate k In e Into Count(k.ID = 1))
        Assert.AreEqual(1, q)

        q = (Aggregate k In e Where k.ID > 0 Into Count())
        Assert.AreEqual(3, q)

        q = (Aggregate k In e Where k.ID > 0 Select k.Code Into Count())
        Assert.AreEqual(3, q)

        q = (Aggregate k In e Where k.ID > 0 Select k.Code Skip 2 Into Count())
        Assert.AreEqual(1, q)
    End Sub

End Class
