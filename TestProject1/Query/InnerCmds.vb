Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm

<TestClass()> Public Class InnerCmds

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

    <TestMethod(), ExpectedException(GetType(QueryCmdException))> Public Sub TestInner()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Select(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).Select(GetType(Table1), True).ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsTrue(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod(), ExpectedException(GetType(QueryCmdException))> Public Sub TestInnerWrongLoad()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Select(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).Select(GetType(Table1), True).ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsTrue(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInner2()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Select(GetType(Table1), True)

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerWrongCols()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Where(Ctor.prop(GetType(Table1), "Code").not_eq(2))
        inner.AutoFields = False

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner.Select(FCtor.prop(GetType(Table1), "Code", "id"))). _
            ToList(Of Table1)()

        Assert.AreEqual(2, r.Count)

        For Each t As Table1 In r
            Assert.AreEqual(Entities.ObjectState.NotLoaded, t.InternalProperties.ObjectState)
            t.Load()
            Assert.AreEqual(Entities.ObjectState.NotFoundInSource, t.InternalProperties.ObjectState)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerWithoutLoadID()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Select(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner.Select(FCtor.prop(GetType(Table1), "ID"))). _
            ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerID()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Select(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner.Select(FCtor.prop(GetType(Table1), "ID"))). _
            ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

End Class
