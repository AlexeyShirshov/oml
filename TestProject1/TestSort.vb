Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm
Imports System.Collections

<TestClass()> Public Class TestSort

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
    Public Sub TestExternalSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim r As ReadOnlyList(Of Entity) = mgr.FindTop(Of Entity)(10, Nothing, Nothing, False)
            Assert.AreEqual(10, r.Count)

            r = mgr.FindTop(Of Entity)(10, Nothing, Orm.Sorting.External("xxx", AddressOf ExternalSort), False)
            Assert.AreEqual(1, r.Count)

            r = mgr.FindTop(Of Entity)(10, Nothing, Orm.Sorting.External("yyy", AddressOf ExternalSort).Desc, False)
            Assert.AreEqual(2, r.Count)
        End Using
    End Sub

    Protected Function ExternalSort(ByVal mgr As OrmManager, ByVal generator As ObjectMappingEngine, ByVal sort As Worm.Sorting.Sort, ByVal objs As ICollection) As ICollection
        Dim col As IList(Of Entity) = CType(objs, IList(Of Entity))
        Dim r As New List(Of Entity)
        Select Case sort.FieldName
            Case "xxx"
                r.Add(col(0))
            Case "yyy"
                r.Add(col(0))
                r.Add(col(1))
            Case Else
                Throw New NotSupportedException(sort.FieldName)
        End Select
        Return New ReadOnlyList(Of Entity)(r)
    End Function
End Class
