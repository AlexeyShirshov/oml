Option Infer On

Imports System
Imports System.Text
Imports System.Collections
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Entities.Meta
Imports Worm
Imports Worm.Database
Imports System.Data

<TestClass()> Public Class BulkLoad

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
    <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
        Worm.Database.OrmReadOnlyDBManager.StmtSource.Listeners.Add( _
               New System.Diagnostics.TextWriterTraceListener(Console.Out) _
           )
    End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    <TestInitialize()> Public Sub MyTestInitialize()
        Worm.Database.OrmReadOnlyDBManager.StmtSource.Listeners.Add( _
               New System.Diagnostics.TextWriterTraceListener(Console.Out) _
           )
    End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region

    <TestMethod()> Public Sub TestBulkLoad()

        Using mgr = TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))

            Dim opt As New BulkLoadOptions
            opt.AutoColumns = True
            opt.Filename = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Advanced\BulkLoad.csv"))
            opt.TableName = "BulkLoadTable"
            opt.Delimiters = {";"}
            'opt.ColumnList.Add(New DataColumn("dt", GetType(Date)))
            opt.AutoMapColumns = True

            Dim loader As New SqlServerBuildLoader()

            loader.Load(mgr, opt)
        End Using
    End Sub
End Class
