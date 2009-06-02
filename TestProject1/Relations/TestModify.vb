Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm

<TestClass()> Public Class TestModify

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

    Protected Function CreateCmd() As QueryCmd
        Return New QueryCmd(Function() TestManager.CreateWriteManager(New ObjectMappingEngine("1")))
    End Function

    <TestMethod(), Ignore()> Public Sub TestAdd()
        Dim e As Entity = CreateCmd.GetByID(Of Entity)(1)
        Dim e4 As Entity4 = CreateCmd.GetByID(Of Entity4)(2)

        Dim cnt As Integer = e.GetCmd(GetType(Entity4)).Count

        e.GetCmd(GetType(Entity4)).Add(e4)

        Assert.AreEqual(cnt + 1, e.GetCmd(GetType(Entity4)).Count)
    End Sub

End Class
