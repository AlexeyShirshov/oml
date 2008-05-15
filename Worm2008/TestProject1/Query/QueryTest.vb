Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Database
Imports Worm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Core
Imports Worm.Criteria.Values

<TestClass()> Public Class QueryTest

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

    <TestMethod()> Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim q As QueryCmd(Of Entity) = QueryCmd(Of Entity).Create(Ctor.AutoTypeField("ID").Eq(1))
            Assert.IsNotNull(q)

            Dim e As Entity = q.Single(mgr)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyList(Of Entity) = q.ToList(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.QueryGeneratorException))> Public Sub TestFilterFromRawTableWrong()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As OrmTable = mgr.ObjectSchema.GetTables(GetType(Entity4))(0)
            Dim q As QueryCmd(Of Entity) = QueryCmd(Of Entity).Create(t, "ID")
            Assert.IsNotNull(q)

            q.Filter = Ctor.AutoTypeField("ID").Eq(1)

            Dim e As Entity = q.Single(mgr)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilterFromRawTable()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))

            Dim t As OrmTable = mgr.ObjectSchema.GetTables(GetType(Entity4))(0)
            Dim q As QueryCmd(Of Entity) = QueryCmd(Of Entity).Create(t, "ID")
            Assert.IsNotNull(q)

            Dim c As New Worm.Database.Criteria.Conditions.Condition.ConditionConstructor
            c.AddFilter(New TableFilter(t, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

            q.Filter = c.Condition

            Dim e As Entity = q.Single(mgr)
            Assert.IsNotNull(e)
            Assert.AreEqual(1, e.Identifier)

            Dim r As ReadOnlyList(Of Entity) = q.ToList(mgr)

            Assert.AreEqual(1, r.Count)
            Assert.AreEqual(e, r(0))
        End Using
    End Sub

End Class
