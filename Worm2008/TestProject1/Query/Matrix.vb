Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Entities

<TestClass()> Public Class Matrix

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

    <TestMethod()> Public Sub TestSelect()
        Dim t1 As New ObjectAlias(GetType(Table1))
        Dim t2 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, t2)

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelect2()
        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(GetType(Table1)).Select(GetType(Table1), GetType(Table2)). _
            Join(JCtor.join(GetType(Table2)).on(GetType(Table2), "Table1").eq(GetType(Table1), "ID"))

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub

    <TestMethod()> Public Sub TestSelectWithload()
        Dim t1 As New ObjectAlias(GetType(Table1))
        Dim t2 As New ObjectAlias(GetType(Table1))

        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.From(t1). _
            Join(JCtor.join(t2).on(t1, "ID").eq(t2, "ID")). _
            Select(t1, True).SelectAdd(t2, True)

        Dim m As ReadonlyMatrix = q.ToMatrix
    End Sub
End Class
