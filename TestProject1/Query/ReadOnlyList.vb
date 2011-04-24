Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Cache
Imports Worm.Entities

<TestClass()> Public Class TestReadOnlyList

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

    <TestMethod()> Public Sub TestCast()
        Dim c As New OrmCache

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim r As ReadOnlyEntityList(Of Table33) = q.ToList(Of Table33)()

        Assert.AreEqual(2, r.Count)

        Assert.IsInstanceOfType(r.Cast(Of Table3), GetType(ReadOnlyList(Of Table3)))
    End Sub

    <TestMethod()> Public Sub TestCastNonEntity()
        Dim c As New OrmCache

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim r As ReadOnlyEntityList(Of Table33) = q.ToList(Of Table33)()

        Assert.AreEqual(2, r.Count)

        Assert.IsInstanceOfType(r.Cast(Of IEntityFactory), GetType(List(Of IEntityFactory)))
    End Sub

End Class
