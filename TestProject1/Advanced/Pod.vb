Imports System
Imports System.Text
Imports System.Collections
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Entities.Meta
Imports Worm

<TestClass()> Public Class Pod

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

    Public Class cls

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _title As String
        Public Property Title() As String
            Get
                Return _title
            End Get
            Set(ByVal value As String)
                _title = value
            End Set
        End Property

        Private _id As Integer
        Public Property ID() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    Public Class cls2

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _name As String
        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(ByVal value As String)
                _name = value
            End Set
        End Property

        Private _id As Integer
        Public Overridable Property ID() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    Public Class cls3

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Private _title As String
        <EntityProperty("name")> Public Property Title() As String
            Get
                Return _title
            End Get
            Set(ByVal value As String)
                _title = value
            End Set
        End Property

        Private _id As Integer
        Public Property Id() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

    End Class

    <Entity("dbo", "table1", "1")> _
    Public Class cls4
        Inherits cls2

        <EntityProperty(Field2DbRelations.PK)> _
        Public Overrides Property ID() As Integer
            Get
                Return MyBase.ID
            End Get
            Set(ByVal value As Integer)
                MyBase.ID = value
            End Set
        End Property
    End Class

    <Entity(GetType(cls5.schema), "1")> _
    Public Class cls5
        Inherits cls

        Public Class schema
            Implements IEntitySchema

            Private _tbl As New SourceFragment("dbo", "table1")

            Public ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
                Get
                    Return _tbl
                End Get
            End Property

            Public Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column) Implements Worm.Entities.Meta.IPropertyMap.GetFieldColumnMap
                Dim m As New OrmObjectIndex
                m("ID") = New MapField2Column("ID", "id", _tbl, Field2DbRelations.PrimaryKey)
                m("Title") = New MapField2Column("Title", "name", _tbl)
                m("Code") = New MapField2Column("Code", "code", _tbl)
                Return m
            End Function
        End Class
    End Class

    <TestMethod()> Public Sub TestCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID")). _
            OrderBy(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l As IList(Of cls) = q.ToPOCOList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCustomObject2()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.OrderBy(SCtor.prop(GetType(cls2), "Code")).From(t)

        Dim l As IList(Of cls2) = q.ToPOCOList(Of cls2)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCustomObject3()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.OrderBy(SCtor.prop(GetType(cls3), "Code")).From(t)

        Dim l As IList(Of cls3) = q.ToPOCOList(Of cls3)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsFalse(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObject()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)). _
            OrderBy(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l As IList(Of cls) = q.ToPOCOList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsTrue(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObject2()

        Dim t As New SourceFragment("dbo", "table1")
        Dim c As New Cache.ReadonlyCache

        Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(mpe, c)))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)). _
            OrderBy(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l As IList(Of cls) = q.ToPOCOList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        Dim q2 As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(mpe, c)))

        q2.Select(FCtor.column(t, "code", "Code").column(t, "name").column(t, "id", "ID", Field2DbRelations.PK)). _
            OrderBy(SCtor.prop(GetType(cls2), "Code")).From(t)

        Dim l2 As IList(Of cls2) = q2.ToPOCOList(Of cls2)()

        Assert.AreEqual(3, l2.Count)

        Assert.AreEqual(2, l2(0).Code)

        Assert.AreNotSame(c.GetPOCO(mpe, q.GetEntitySchema(mpe, GetType(cls)), l(0)), _
                          c.GetPOCO(mpe, q2.GetEntitySchema(mpe, GetType(cls2)), l2(0)))

        Dim q3 As New QueryCmd(New CreateManager(Function() _
           TestManagerRS.CreateManagerShared(mpe, c)))

        q3.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)) _
            .Where(Ctor.column(t, "id").greater_than(0)) _
            .OrderBy(SCtor.prop(GetType(cls), "Code")).From(t)

        Dim l3 As IList(Of cls) = q3.ToPOCOList(Of cls)()

        Assert.AreEqual(3, l3.Count)

        Assert.AreEqual(2, l3(0).Code)

        For i As Integer = 0 To l.Count - 1
            Assert.AreSame(c.GetPOCO(mpe, q.GetEntitySchema(mpe, GetType(cls)), l(i)), _
                  c.GetPOCO(mpe, q3.GetEntitySchema(mpe, GetType(cls)), l3(i)))

        Next
    End Sub

    <TestMethod()> Public Sub TestCachedCustomObjectWithoutFromClause()

        Dim t As New SourceFragment("dbo", "table1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor.column(t, "code", "Code").column(t, "name", "Title").column(t, "id", "ID", Field2DbRelations.PK)). _
            OrderBy(SCtor.prop(GetType(cls), "Code"))

        Dim l As IList(Of cls) = q.ToPOCOList(Of cls)()

        Assert.AreEqual(3, l.Count)

        Assert.AreEqual(2, l(0).Code)

        'Using mgr As OrmManager = q.GetMgr.CreateManager
        '    Assert.IsTrue(mgr.CustomObject(l(0)).IsLoaded)
        'End Using
    End Sub

    <TestMethod()> _
    Public Sub TestWithTable()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.OrderBy(SCtor.prop(GetType(cls4), "Code")) _
            .Where(Ctor.prop(GetType(cls4), "Code").greater_than(2))

        Dim l As IList(Of cls4) = q.ToPOCOList(Of cls4)()

        Assert.AreEqual(2, l.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestWithTableJoin()

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        Dim c1 As New QueryAlias(GetType(cls4))

        Dim c2 As New QueryAlias(GetType(cls4))

        q _
            .From(c1) _
            .SelectEntity(c1) _
            .Join(JCtor.join(c2).on(c1, "ID").eq(c2, "ID")) _
            .Where(Ctor.prop(c1, "Code").greater_than(2)) _
            .OrderBy(SCtor.prop(c1, "Code"))

        Dim l As IList(Of cls4) = q.ToPOCOList(Of cls4)()

        Assert.AreEqual(2, l.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestStaticSchema()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        Dim l As IList = q.SelectEntity(GetType(cls5)).ToList

        Assert.AreEqual(3, l.Count)
    End Sub
End Class
