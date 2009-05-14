Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm

<TestClass()> Public Class JoinTest

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
    <TestInitialize()> Public Sub MyTestInitialize()
        Worm.Database.OrmReadOnlyDBManager.StmtSource.Listeners.Add(New System.Diagnostics.TextWriterTraceListener(Console.Out))
    End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region

    <TestMethod()> Public Sub TestApply()
        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.SelectEntity(GetType(Table2)) _
            .Where(Ctor.prop(GetType(Table1), "ID").eq(GetType(Table2), "Table1")) _
            .Top(1)

        Dim al As New EntityAlias(q)

        Dim q2 As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q2.From(GetType(Table1)).Join(JCtor.apply(al)).SelectEntity(al)

        Assert.AreEqual(1, q2.Count)

        Assert.IsInstanceOfType(q2.Single, GetType(Table2))
    End Sub

End Class
