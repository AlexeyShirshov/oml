Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports Worm
Imports Worm.Database

<TestClass()> Public Class SimpleModel

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

    <TestMethod()> Public Sub TestPlainObject()
        Dim t As New GuidPK

        Assert.IsFalse(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("Code"))
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("ID"))

    End Sub

    Private _mgr As OrmReadOnlyDBManager
    Private Sub ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs)
        If _mgr Is Nothing Then
            _mgr = TestManager.CreateWriteManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            _mgr.BeginTransaction()
        End If
        args.Manager = _mgr
        args.DisposeMgr = False
    End Sub

    <TestMethod()> Public Sub TestPlainObjectSave()
        Dim t As New GuidPK

        Assert.IsFalse(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("Code"))
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("ID"))
        Assert.AreEqual(Guid.Empty, t.Guid)

        AddHandler t.ManagerRequired, AddressOf ManagerRequired

        Try
            t.SaveChanges(True)

            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Code"))
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("ID"))
            Assert.AreNotEqual(Guid.Empty, t.Guid)
        Finally
            If _mgr IsNot Nothing Then
                _mgr.Rollback()
                _mgr.Dispose()
                _mgr = Nothing
            End If
        End Try
    End Sub

    <TestMethod()> Public Sub TestVeryPlainObjectSave()
        Dim t As New Modification.RawObj

        Assert.IsFalse(t.InternalProperties.IsLoaded)
        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("Code"))
        Assert.IsFalse(t.InternalProperties.IsPropertyLoaded("Identifier"))
        Assert.AreEqual(Guid.Empty, t.Identifier)

        AddHandler t.ManagerRequired, AddressOf ManagerRequired

        Try
            t.SaveChanges(True)

            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Code"))
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Identifier"))
            Assert.AreNotEqual(Guid.Empty, t.Identifier)
        Finally
            If _mgr IsNot Nothing Then
                _mgr.Rollback()
                _mgr.Dispose()
                _mgr = Nothing
            End If
        End Try
    End Sub

    <TestMethod()> Public Sub TestPlainObjectAlter()
        Dim t As New GuidPK
        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)

        Using mgr As OrmManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            t.Code = 10

            Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)
        End Using

    End Sub
End Class
