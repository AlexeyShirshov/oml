Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics

Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Cache
Imports Worm.Database

<Entity(GetType(MultiTableEn), "en")> _
<Entity(GetType(MultiTableRu), "ru")> _
Public Class MultiTable
    Inherits OrmBaseT(Of MultiTable)

    Private _title As String

    <Column(column:="msg")> _
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
End Class

Public MustInherit Class MultiTableSchemaBase
    Inherits ObjectSchemaBaseImplementation
    Implements ICacheBehavior

    Private _idx As OrmObjectIndex

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Msg", "msg", GetTables()(Tables.Main)))
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

    Private _tables() As OrmTable = {New OrmTable("dbo.m2")}

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function
End Class

Public Class MultiTableRu
    Inherits MultiTableSchemaBase

    Private _tables() As OrmTable = {New OrmTable("dbo.m1")}

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function
End Class

<TestClass()> _
Public Class TestMultiTable

    Protected Function ResolveVersion(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
        Return entities(0)
    End Function

    Protected Function GetSchema(ByVal version As String) As DbSchema
        Return New DbSchema(version, AddressOf ResolveVersion, Nothing)
    End Function

    <TestMethod()> _
    Public Sub TestLoad()
        Dim tm As New TestManagerRS
        tm.SharedCache = True

        Dim m2 As MultiTable = Nothing
        Dim t As Table3 = Nothing
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(GetSchema("en"))
            m2 = mgr.Find(Of MultiTable)(1)
            t = mgr.Find(Of Table3)(1)
        End Using

        Dim m1 As MultiTable = Nothing
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(GetSchema("ru"))
            m1 = mgr.Find(Of MultiTable)(1)
            Assert.AreSame(t, mgr.Find(Of Table3)(1))
        End Using

        Assert.AreEqual("привет", m1.Msg)
        Assert.AreEqual("hi", m2.Msg)

    End Sub
End Class
