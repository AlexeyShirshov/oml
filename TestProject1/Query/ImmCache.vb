Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Orm

<TestClass()> Public Class ImmCache

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

    <TestMethod()> Public Sub TestSortValidate()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))
            mgr.NewObjectManager = tm

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec)).Sort(Sorting.Custom("name"))

            Dim l As IList(Of Table1) = q.ToOrmList(Of Table1)(mgr)
            Assert.AreEqual(2, l.Count)

            mgr.BeginTransaction()
            Try
                Using s As New ModificationsTracker(mgr)
                    Dim f As Table1 = s.CreateNewObject(Of Table1)()
                    f.Code = 20
                    f.CreatedAt = Now

                    s.AcceptModifications()
                End Using

                Assert.AreEqual(2, q.ToOrmList(Of Table1)(mgr).Count)
                Assert.IsTrue(q.LastExecitionResult.CacheHit)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

End Class
