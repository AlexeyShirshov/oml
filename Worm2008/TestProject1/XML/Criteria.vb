Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Cache
Imports Worm.Orm
Imports Worm
Imports System.Reflection
Imports wx = Worm.Xml
Imports Worm.Orm.Meta

<TestClass()> Public Class TestXmlCriteria

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

    <TestMethod()> Public Sub TestQuery()
        Using mgr As OrmManagerBase = CreateManager()
            Dim col As ICollection(Of Entity4) = mgr.Find(Of Entity4)( _
                mgr.ObjectSchema.CreateCriteria(GetType(Entity4), "Title").Eq("first"))

            Assert.AreEqual(1, col.Count)

            Dim e As Entity4 = CType(col, IList(Of Entity4))(0)
            Assert.AreEqual("first", e.Title)
            Assert.AreEqual(ObjectState.None, e.ObjectState)

        End Using
    End Sub

    Public Shared Function CreateManager() As OrmManagerBase
        Return New wx.QueryManager(New wx.XmlSchema("xml"), GetFileFromStream("data.xml"))
    End Function

    Public Shared Function GetFileFromStream(ByVal file As String) As IO.Stream
        Dim a As Assembly = Assembly.GetExecutingAssembly
        Dim fullName As String = String.Format("{0}.{1}", a.GetName().Name, file)
        Return a.GetManifestResourceStream(fullName)
    End Function

    <Entity(GetType(EntityXmlSchema), "xml")> _
    Public Class Entity4
        Inherits TestProject1.Entity4

    End Class

    Public Class EntityXmlSchema
        Inherits ObjectSchemaBaseImplementation

        Private _idx As OrmObjectIndex
        Protected _tables() As OrmTable = {New OrmTable("/root/objects/object")}

        Public Enum Tables
            Main
        End Enum

        Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Orm.Meta.MapField2Column)
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", "@id", GetTables()(Tables.Main)))
                idx.Add(New MapField2Column("Title", "@name", GetTables()(Tables.Main)))
                _idx = idx
            End If
            Return _idx
        End Function

        Public Overrides Function GetTables() As Worm.Orm.Meta.OrmTable()
            Return _tables
        End Function
    End Class
End Class
