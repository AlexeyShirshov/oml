Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm

<TestClass()> Public Class Inheritance

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

    <Entity("dbo", "EntBase", "1")> _
    Public MustInherit Class Base
        Inherits KeyEntity

        Private _id As Integer
        Private _dt As Date

        <EntityProperty("id", Field2DbRelations.PrimaryKey)> _
        Public Overridable Property ID() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

        <EntityProperty("dt", Field2DbRelations.SyncInsert Or Field2DbRelations.InsertDefault)> _
        Public Property CreateDt() As Date
            Get
                Using Read("CreateDt")
                    Return _dt
                End Using
            End Get
            Set(ByVal value As Date)
                Using Write("CreateDt")
                    _dt = value
                End Using
            End Set
        End Property

        Public Overrides Property Identifier() As Object
            Get
                Return _id
            End Get
            Set(ByVal value As Object)
                _id = CInt(value)
            End Set
        End Property
    End Class

    <Entity("dbo", "ent2", "1")> _
    Public Class Ent2
        Inherits Base

        Private _name As String

        <EntityProperty("name")> _
        Public Property Name() As String
            Get
                Using Read("Name")
                    Return _name
                End Using
            End Get
            Set(ByVal value As String)
                Using Write("Name")
                    _name = value
                End Using
            End Set
        End Property

    End Class

    <Entity("dbo", "ent4", "1")> _
    Public Class Ent3
        Inherits Base

        Private _code As Integer

        <EntityProperty("code")> _
        Public Property Code() As Integer
            Get
                Using Read("Code")
                    Return _code
                End Using
            End Get
            Set(ByVal value As Integer)
                Using Write("Code")
                    _code = value
                End Using
            End Set
        End Property

        <EntityProperty("pk", Field2DbRelations.PrimaryKey)> _
        Public Overrides Property ID() As Integer
            Get
                Return MyBase.ID
            End Get
            Set(ByVal value As Integer)
                MyBase.ID = value
            End Set
        End Property
    End Class

    <TestMethod()> _
    Public Sub TestEnt2()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Assert.AreEqual(12, q.From(GetType(Ent2)).Count)

        For Each c As Ent2 In q.ToList(Of Ent2)()
            Console.WriteLine(c.Name)
            Console.WriteLine(c.CreateDt)
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEnt3()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Assert.AreEqual(1, q.From(GetType(Ent3)).Count)

        For Each c As Ent3 In q.ToList(Of Ent3)()
            Console.WriteLine(c.Code)
            Console.WriteLine(c.CreateDt)
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEnt()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Assert.AreEqual(14, q.From(GetType(Base)).Count)

        For Each c As Base In q.From(GetType(Base)).ToList()
            Console.WriteLine(c.CreateDt)
            If c.ID = 13 Then
                Assert.IsTrue(CType(c, Ent3).Code <> 0)
                Console.WriteLine(CType(c, Ent3).Code)
            End If
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEntLoad()
        Dim cache As New Worm.Cache.ReadonlyCache

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(cache, New ObjectMappingEngine("1"))))

        Dim l As IList(Of Base) = q.ToBaseEntity(Of Base)(True)
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        l = q.ToBaseEntity(Of Base)(True)
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        For Each c As Base In l
            Console.WriteLine(c.CreateDt)
            If c.ID = 13 Then
                Assert.IsTrue(CType(c, Ent3).Code <> 0)
                Console.WriteLine(CType(c, Ent3).Code)
            End If
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEntWhere()
        Dim cache As New Worm.Cache.ReadonlyCache

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(cache, New ObjectMappingEngine("1"))))

        Dim l As IList(Of Base) = q _
            .Where(Ctor.prop(GetType(Base), "CreateDt").greater_than(Date.Parse("2009-04-13 13:04:20", Globalization.CultureInfo.InvariantCulture))) _
            .ToBaseEntity(Of Base)(True)

        Assert.AreEqual(8, q.LastExecutionResult.RowCount)
        Assert.AreEqual(7, l.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestEntManual()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        'Dim tbl As New SourceFragment("dbo", "EntBase")

        Dim m As ReadonlyMatrix = q _
            .From(GetType(Base)) _
            .Join(JCtor _
                  .left_join(GetType(Ent2)).on(GetType(Base), "ID").eq(GetType(Ent2), "ID") _
                  .left_join(GetType(Ent3)).on(GetType(Base), "ID").eq(GetType(Ent3), "ID")) _
            .SelectEntity(GetType(Ent2), GetType(Ent3)) _
            .ToMatrix()

        Assert.AreEqual(14, m.Count)

        For Each row As System.Collections.ObjectModel.ReadOnlyCollection(Of _IEntity) In m
            If row(0) IsNot Nothing Then
                Assert.IsFalse(row(0).IsLoaded)
                Console.WriteLine(CType(row(0), Ent2).Name)
            ElseIf row(1) IsNot Nothing Then
                Assert.IsFalse(row(1).IsLoaded)
                Console.WriteLine(CType(row(1), Ent3).Code)
            End If
        Next
    End Sub

    <TestMethod()> _
   Public Sub TestEntManualLoad()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Dim tbl As New SourceFragment("dbo", "EntBase")

        Dim m As ReadonlyMatrix = q _
            .From(tbl) _
            .Join(JCtor _
                  .left_join(GetType(Ent2)).on(tbl, "id").eq(GetType(Ent2), "ID") _
                  .left_join(GetType(Ent3)).on(tbl, "id").eq(GetType(Ent3), "ID")) _
            .SelectEntity(GetType(Ent2), True) _
            .AddEntityToSelectList(GetType(Ent3), True) _
            .ToMatrix()

        Assert.AreEqual(14, m.Count)

        For Each row As System.Collections.ObjectModel.ReadOnlyCollection(Of _IEntity) In m
            If row(0) IsNot Nothing Then
                Assert.IsTrue(row(0).IsLoaded)
                Console.WriteLine(CType(row(0), Ent2).Name)
            ElseIf row(1) IsNot Nothing Then
                Assert.IsTrue(row(1).IsLoaded)
                Console.WriteLine(CType(row(1), Ent3).Code)
            End If
        Next
    End Sub
End Class

<TestClass()> Public Class InheritanceWithSchema

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

    <Entity(GetType(Base.SchemaBase), "1")> _
    Public Class Base
        Inherits KeyEntity

        Private _id As Integer
        Public Property ID() As Integer
            Get
                Return _id
            End Get
            Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

        Private _dt As Date
        Public Property CreateDt() As Date
            Get
                Using Read("CreateDt")
                    Return _dt
                End Using
            End Get
            Set(ByVal value As Date)
                Using Write("CreateDt")
                    _dt = value
                End Using
            End Set
        End Property

        Public Overrides Property Identifier() As Object
            Get
                Return _id
            End Get
            Set(ByVal value As Object)
                _id = CInt(value)
            End Set
        End Property

        Public Class SchemaBase
            Implements IEntitySchema

            Private _tbl As New SourceFragment("dbo", "EntBase")
            Private _idx As OrmObjectIndex

            Public Overridable ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
                Get
                    Return _tbl
                End Get
            End Property

            Public Overridable ReadOnly Property GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column) Implements Worm.Entities.Meta.IPropertyMap.FieldColumnMap
                Get
                    If _idx Is Nothing Then
                        _idx = New OrmObjectIndex
                        _idx.Add(New MapField2Column("ID", "id", _tbl, Field2DbRelations.PK))
                        _idx.Add(New MapField2Column("CreateDt", "dt", _tbl, Field2DbRelations.SyncInsert Or Field2DbRelations.InsertDefault))
                    End If
                    Return _idx
                End Get
            End Property
        End Class
    End Class

    <Entity(GetType(Ent2.SchemaEnt2), "1")> _
    Public Class Ent2
        Inherits Base

        Private _name As String
        Public Property Name() As String
            Get
                Using Read("Name")
                    Return _name
                End Using
            End Get
            Set(ByVal value As String)
                Using Write("Name")
                    _name = value
                End Using
            End Set
        End Property

        Public Class SchemaEnt2
            Inherits Base.SchemaBase
            Implements IMultiTableObjectSchema

            Private _tbl() As SourceFragment
            Private _idx As OrmObjectIndex

            Public Sub New()
                _tbl = New SourceFragment() {MyBase.Table, New SourceFragment("dbo", "ent2")}
            End Sub

            Public Overrides ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment
                Get
                    Return _tbl(0)
                End Get
            End Property

            Public Overrides ReadOnly Property GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column)
                Get
                    If _idx Is Nothing Then
                        _idx = New OrmObjectIndex
                        _idx.AddRange(MyBase.GetFieldColumnMap)
                        _idx.Add(New MapField2Column("Name", "name", _tbl(1)))
                    End If
                    Return _idx
                End Get
            End Property

            Public Function GetJoins(ByVal left As Worm.Entities.Meta.SourceFragment, ByVal right As Worm.Entities.Meta.SourceFragment) As Worm.Criteria.Joins.QueryJoin Implements Worm.Entities.Meta.IMultiTableObjectSchema.GetJoins
                Return JCtor.join(right).on(right, "id").eq(left, "id")
            End Function

            Public Function GetTables() As Worm.Entities.Meta.SourceFragment() Implements Worm.Entities.Meta.IMultiTableObjectSchema.GetTables
                Return _tbl
            End Function
        End Class
    End Class

    <Entity(GetType(Ent3.SchemaEnt3), "1")> _
    Public Class Ent3
        Inherits Base

        Private _code As Integer
        Public Property Code() As Integer
            Get
                Using Read("Code")
                    Return _code
                End Using
            End Get
            Set(ByVal value As Integer)
                Using Write("Code")
                    _code = value
                End Using
            End Set
        End Property

        Public Class SchemaEnt3
            Inherits Base.SchemaBase
            Implements IMultiTableObjectSchema

            Private _tbl() As SourceFragment
            Private _idx As OrmObjectIndex

            Public Sub New()
                _tbl = New SourceFragment() {MyBase.Table, New SourceFragment("dbo", "ent4")}
            End Sub

            Public Overrides ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment
                Get
                    Return _tbl(0)
                End Get
            End Property

            Public Overrides ReadOnly Property GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column)
                Get
                    If _idx Is Nothing Then
                        _idx = New OrmObjectIndex
                        _idx.AddRange(MyBase.GetFieldColumnMap)
                        _idx.Remove("ID")
                        _idx.Add(New MapField2Column("ID", "pk", _tbl(1), Field2DbRelations.PK))
                        _idx.Add(New MapField2Column("Code", "code", _tbl(1)))
                    End If
                    Return _idx
                End Get
            End Property

            Public Function GetJoins(ByVal left As Worm.Entities.Meta.SourceFragment, ByVal right As Worm.Entities.Meta.SourceFragment) As Worm.Criteria.Joins.QueryJoin Implements Worm.Entities.Meta.IMultiTableObjectSchema.GetJoins
                If right Is _tbl(1) Then
                    Return JCtor.join(right).on(right, "pk").eq(left, "id")
                Else
                    Return JCtor.join(right).on(left, "pk").eq(right, "id")
                End If
            End Function

            Public Function GetTables() As Worm.Entities.Meta.SourceFragment() Implements Worm.Entities.Meta.IMultiTableObjectSchema.GetTables
                Return _tbl
            End Function
        End Class
    End Class

    <TestMethod()> _
    Public Sub TestEnt2()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Assert.AreEqual(12, q.From(GetType(Ent2)).Count)

        For Each c As Ent2 In q.ToList(Of Ent2)()
            Console.WriteLine(c.Name)
            Console.WriteLine(c.CreateDt)
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEnt3()
        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1"))))

        Assert.AreEqual(1, q.From(GetType(Ent3)).Count)

        For Each c As Ent3 In q.ToList(Of Ent3)()
            Console.WriteLine(c.Code)
            Console.WriteLine(c.CreateDt)
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEntLoad()
        Dim cache As New Worm.Cache.ReadonlyCache

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(cache, New ObjectMappingEngine("1"))))

        Dim l As IList(Of Base) = q.ToBaseEntity(Of Base)(True)
        Assert.IsFalse(q.LastExecutionResult.CacheHit)

        l = q.ToBaseEntity(Of Base)(True)
        Assert.IsTrue(q.LastExecutionResult.CacheHit)

        For Each c As Base In l
            Console.WriteLine(c.CreateDt)
            If c.ID = 13 Then
                Assert.IsTrue(CType(c, Ent3).Code <> 0)
                Console.WriteLine(CType(c, Ent3).Code)
            End If
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestEntWhere()
        Dim cache As New Worm.Cache.ReadonlyCache

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManager.CreateManager(cache, New ObjectMappingEngine("1"))))

        Dim l As IList(Of Base) = q _
            .Where(Ctor.prop(GetType(Base), "CreateDt").greater_than(Date.Parse("2009-04-13 13:04:20", Globalization.CultureInfo.InvariantCulture))) _
            .ToBaseEntity(Of Base)(True)

        Assert.AreEqual(8, q.LastExecutionResult.RowCount)
        Assert.AreEqual(7, l.Count)
    End Sub

End Class
