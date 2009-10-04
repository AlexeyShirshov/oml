Option Infer On

Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Cache
Imports Worm.Query
Imports Worm
Imports Worm.Expressions2

<TestClass()> Public Class QueryExpressions

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

    <TestMethod()> Public Sub TestSelectExpressionAnonym()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim rows = q _
            .From(GetType(Table1)) _
            .Select(FCtor.prop(GetType(Table1), "ID").Exp(ECtor.prop(GetType(Table1), "Title") + "-xx").into("Title")) _
            .OrderBy(SCtor.prop(GetType(Table1), "ID")) _
            .ToAnonymList

        For Each r In rows
            Dim id As Integer = CInt(r("ID"))
            If id = 1 Then Assert.AreEqual("first-xx", r("Title"))
            If id = 2 Then Assert.AreEqual("second-xx", r("Title"))
            If id = 3 Then Assert.AreEqual("fsgb-xx", r("Title"))
        Next
    End Sub

    <TestMethod()> Public Sub TestSelectExpression()
        Dim c As New ReadonlyCache

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim rows = q _
            .Select(FCtor.prop(GetType(Table1), "ID").Exp(ECtor.prop(GetType(Table1), "Title") + "-xx").into("Title")) _
            .OrderBy(SCtor.prop(GetType(Table1), "ID")) _
            .ToList(Of Table1)()

        For Each r In rows
            Dim id As Integer = r.ID
            If id = 1 Then Assert.AreEqual("first-xx", r.Name)
            If id = 2 Then Assert.AreEqual("second-xx", r.Name)
            If id = 3 Then Assert.AreEqual("fsgb-xx", r.Name)
        Next

        rows = q _
            .Select(FCtor.prop(GetType(Table1), "ID").Exp(ECtor.prop(GetType(Table1), "Title") + "-xx")) _
            .OrderBy(SCtor.prop(GetType(Table1), "ID")) _
            .ToList(Of Table1)()

        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        For Each r In rows
            Dim id As Integer = r.ID
            If id = 1 Then Assert.AreEqual("first-xx", r.Name)
            If id = 2 Then Assert.AreEqual("second-xx", r.Name)
            If id = 3 Then Assert.AreEqual("fsgb-xx", r.Name)
        Next

        rows = New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))) _
            .Select(FCtor.prop(GetType(Table1), "ID").Exp(ECtor.prop(GetType(Table1), "Title") + "-xx")) _
            .OrderBy(SCtor.prop(GetType(Table1), "ID")) _
            .ToList(Of Table1)()

        For Each r In rows
            Dim id As Integer = r.ID
            If id = 1 Then Assert.AreEqual("first-xx", r.Name)
            If id = 2 Then Assert.AreEqual("second-xx", r.Name)
            If id = 3 Then Assert.AreEqual("fsgb-xx", r.Name)
        Next
    End Sub

    <TestMethod()> Public Sub TestWhereExpression()
        Dim c As New ReadonlyCache

        Dim id As New ObjectProperty(GetType(Table1), "ID")
        Dim name As New ObjectProperty(GetType(Table1), "Title")
        Dim code As New ObjectProperty(GetType(Table1), "Code")

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c))

        Dim rows = q _
            .Select(id, name + "-postfix") _
            .OrderBy(id + code) _
            .Where(code < 10 And id = 1) _
            .SingleOrDefault(Of Table1)()

        Assert.IsNotNull(rows)
        Assert.AreEqual(1, rows.ID)
        Assert.AreEqual("first-postfix", rows.Name)

        'For Each r In rows
        '    If id = 1 Then Assert.AreEqual("first-xx", r.Name)
        '    If id = 2 Then Assert.AreEqual("second-xx", r.Name)
        '    If id = 3 Then Assert.AreEqual("fsgb-xx", r.Name)
        'Next

    End Sub
End Class
