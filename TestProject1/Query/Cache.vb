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

<TestClass()> Public Class CacheQueryTest

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

    <TestMethod()> Public Sub TestFilter()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Table1))
            q.Filter = Ctor.AutoTypeField("ID").GreaterThan(2)
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table1 = s.CreateNewObject(Of Table1)()
                    t1.CreatedAt = Now
                    s.Commit()
                End Using

                Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoin()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Table1))
            q.Filter = Ctor.Field(GetType(Table2), "Money").Eq(1)
            q.AutoJoins = True
            Assert.IsNotNull(q)

            Assert.AreEqual(1, q.ToEntityList(Of Table1)(mgr).Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table2 = s.CreateNewObject(Of Table2)()
                    t1.Money = 1
                    t1.Tbl = mgr.Find(Of Table1)(2)
                    s.Commit()
                End Using

                Assert.AreEqual(2, q.ToEntityList(Of Table1)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestUpdate()
        Dim m As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = m.CreateManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Table2))
            q.Filter = Ctor.AutoTypeField("Money").GreaterThan(1)
            Dim l As ReadOnlyEntityList(Of Table2) = q.ToEntityList(Of Table2)(mgr)
            Assert.AreEqual(1, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim t1 As Table2 = l(0)
                    t1.Money = 1
                    s.Commit()
                End Using

                Assert.AreEqual(0, q.ToEntityList(Of Table2)(mgr).Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

End Class
