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

    <TestMethod()> Public Sub TestQuery()
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\wormtest.mdf"))
        Dim conn As String= "Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;"
#Else
        Dim conn As String = "Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest"
#End If

        Dim ctx As New WormDBContext(conn)

        Dim e As QueryWrapperT(Of TestProject1.Table1) = ctx.CreateQueryWrapper(Of TestProject1.Table1)()

        Dim q = From k In e Where k.Code = 2
        Dim l As IList(Of TestProject1.Table1) = q.ToList
        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(1, l(0).ID)

        Dim p As New WormLinqProvider(ctx)
        Dim exp As Expressions.Expression = Nothing
        q = p.CreateQuery(Of TestProject1.Table1)(exp)
        l = q.ToList

        Assert.AreEqual(1, l.Count)
        Assert.AreEqual(1, l(0).ID)
    End Sub

End Class
