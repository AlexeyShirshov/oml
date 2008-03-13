Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> Public Class TraceListener

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

    <TestMethod()> Public Sub TestMaxFileSize()
        Dim l As New Worm.Web.TraceListener("xxx")
        l.BaseFileName = "xxx"
        l.Location = Logging.LogFileLocation.TempDirectory
        l.MaxFileSize = 1000
        For i As Integer = 0 To 100
            l.Write("io2rf9vmdfvklamdfvlakd345gfv")
        Next
        Diagnostics.Debug.WriteLine(l.FullLogFileName)
    End Sub

End Class
