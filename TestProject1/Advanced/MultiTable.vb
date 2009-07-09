Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics

Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Database
Imports Worm
Imports Worm.Query

<Entity(GetType(MultiTableEn), "en")> _
<Entity(GetType(MultiTableRu), "ru")> _
Public Class MultiTable
    Inherits KeyEntity

    Private _title As String

    <EntityPropertyAttribute(column:="msg")> _
    Public Property Msg() As String
        Get
            Using Read("Msg")
                Return _title
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Msg")
                _title = value
            End Using
        End Set
    End Property

    Private _id As Integer

    <EntityProperty(Field2DbRelations.PrimaryKey)> _
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
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

Public MustInherit Class MultiTableSchemaBase
    Inherits ObjectSchemaBaseImplementation
    Implements ICacheBehavior

    Private _idx As OrmObjectIndex

    'Public Enum Tables
    '    Main
    'End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", Table))
            idx.Add(New MapField2Column("Msg", "msg", Table))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Function GetEntityKey(ByVal filterInfo As Object) As String Implements ICacheBehavior.GetEntityKey
        Return _objectType.ToString
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object) As Object Implements ICacheBehavior.GetEntityTypeKey
        Return _objectType.ToString & _schema.Version
    End Function
End Class

Public Class MultiTableEn
    Inherits MultiTableSchemaBase

    Public Sub New()
        _tbl = New SourceFragment("dbo.m2")
    End Sub
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.m2")}

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class

Public Class MultiTableRu
    Inherits MultiTableSchemaBase

    Public Sub New()
        _tbl = New SourceFragment("dbo.m1")
    End Sub
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.m1")}

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class

<TestClass()> _
Public Class TestMultiTable

    Protected Function ResolveVersion(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
        Return entities(0)
    End Function

    Protected Function GetSchema(ByVal version As String) As ObjectMappingEngine
        Return New ObjectMappingEngine(version, AddressOf ResolveVersion, Nothing)
    End Function

    <TestMethod()> _
    Public Sub TestLoad()
        Dim tm As New TestManagerRS
        tm.SharedCache = True

        Dim m2 As MultiTable = Nothing
        Dim t As Table3 = Nothing
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(GetSchema("en"))
            m2 = New QueryCmd().GetByID(Of MultiTable)(1, mgr)
            t = New QueryCmd().GetByID(Of Table3)(1, mgr)
        End Using

        Dim m1 As MultiTable = Nothing
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(GetSchema("ru"))
            m1 = New QueryCmd().GetByID(Of MultiTable)(1, mgr)
            Assert.AreSame(t, New QueryCmd().GetByID(Of Table3)(1, mgr))
        End Using

        Assert.AreEqual("привет", m1.Msg)
        Assert.AreEqual("hi", m2.Msg)

    End Sub
End Class
