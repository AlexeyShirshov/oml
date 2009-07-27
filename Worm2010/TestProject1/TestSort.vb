Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm
Imports System.Collections
Imports Worm.Query.Sorting
Imports Worm.Expressions2
Imports Worm.Query

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

    <TestMethod()> _
    Public Sub TestModifyResult()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd().Top(10)
            Dim r As ReadOnlyList(Of Entity) = q.ToOrmList(Of Entity)(mgr)
            Assert.AreEqual(10, r.Count)
            Assert.IsFalse(mgr.LastExecutionResult.CacheHit)

            AddHandler q.ModifyResult, AddressOf ExternalSort
            r = q.ToOrmList(Of Entity)(mgr)
            Assert.AreEqual(1, r.Count)
            Assert.IsTrue(mgr.LastExecutionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestModifyResultExpire()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            Dim q As QueryCmd = New QueryCmd().Top(10)
            AddHandler q.ModifyResult, AddressOf ExternalSort2

            Dim r As ReadOnlyList(Of Entity) = q.ToOrmList(Of Entity)(mgr)
            Assert.AreEqual(10, r.Count)
            Assert.IsFalse(mgr.LastExecutionResult.CacheHit)

            r = q.ToOrmList(Of Entity)(mgr)
            Assert.AreEqual(1, r.Count)
            Assert.IsTrue(mgr.LastExecutionResult.CacheHit)

            Threading.Thread.Sleep(1100)

            r = q.ToOrmList(Of Entity)(mgr)
            Assert.AreEqual(2, r.Count)
            Assert.IsTrue(mgr.LastExecutionResult.CacheHit)

        End Using
    End Sub

    Protected Sub ExternalSort(ByVal sender As QueryCmd, ByVal args As QueryCmd.ModifyResultArgs)
        args.ReadOnlyList = New ReadOnlyList(Of Entity)(New Entity() {CType(args.ReadOnlyList(0), Entity)})
    End Sub

    Protected Sub ExternalSort2(ByVal sender As QueryCmd, ByVal args As QueryCmd.ModifyResultArgs)
        If args.FromCache Then
            Dim d As Date = CDate(args.CustomInfo)
            If d < Now Then
                args.ReadOnlyList = New ReadOnlyList(Of Entity)(New Entity() {CType(args.ReadOnlyList(0), Entity), CType(args.ReadOnlyList(1), Entity)})
            Else
                args.ReadOnlyList = New ReadOnlyList(Of Entity)(New Entity() {CType(args.ReadOnlyList(0), Entity)})
            End If
        Else
            args.CustomInfo = Now.AddSeconds(1)
        End If
    End Sub
End Class
