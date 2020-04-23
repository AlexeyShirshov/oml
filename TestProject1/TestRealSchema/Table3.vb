Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic
Imports System.Linq
Imports Worm

<Entity(GetType(Table3Implementation), "1", EntityName:="Table3")>
Public Class Table3
    Inherits SinglePKEntity
    Implements IOptimizedValues, IEntityFactory, ICopyProperties

    Private _obj As ISinglePKEntity
    Private _code As Byte
    Private _trigger As Boolean
    Private _id As Integer
    Private _v As Byte()
    Private _x As System.Xml.XmlDocument

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
    End Sub

    <EntityProperty(Field2DbRelations.PrimaryKey)>
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

    Protected Sub CopyProperties(ByVal [to] As Object) Implements ICopyProperties.CopyTo
        With Me
            CType([to], Table3)._id = ._id
            CType([to], Table3)._obj = ._obj
            CType([to], Table3)._code = ._code
            CType([to], Table3)._v = ._v
            CType([to], Table3)._x = ._x
        End With
    End Sub

    Protected Function GetObjectType() As Type
        If _code = 1 Then
            Return GetType(Table1)
        ElseIf _code = 2 Then
            Return GetType(Table2)
        Else
            Throw New OrmObjectException("Invalid code " & _code)
        End If
    End Function

    'Public Overrides Function CreateObject(ByVal mgr As Worm.OrmManager, ByVal propertyAlias As String, ByVal value As Object) As _IEntity
    '    _id = CInt(value)
    '    If _code = 0 Then
    '        _trigger = True
    '        Return Nothing
    '    Else
    '        _obj = mgr.GetKeyEntityFromCacheOrCreate(_id, GetObjectType())
    '        Return _obj
    '    End If
    'End Function

    Public Overridable Function SetValueOptimized(
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) As Boolean Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Ref"
                RefObject = CType(value, SinglePKEntity)
            Case "Code"
                Code = CByte(value)
            Case "Version"
                Version = CType(value, Byte())
            Case "XML"
                Xml = CType(value, System.Xml.XmlDocument)
            Case "ID"
                Identifier = value
            Case Else
                Return False
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return True
    End Function

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema, ByRef found As Boolean) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        found = True
        Select Case propertyAlias
            Case "Ref"
                Return _obj
            Case "Code"
                Return _code
            Case "Version"
                Return _v
            Case "XML"
                Return _x
            Case Else
                found = False
                'Return GetValueReflection(propertyAlias, schema)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return Nothing
    End Function

    <EntityPropertyAttribute(PropertyAlias:="Ref", Behavior:=Field2DbRelations.Factory)>
    Public Property RefObject() As ISinglePKEntity
        Get
            Using Read("Ref")
                Return _obj
            End Using
        End Get
        Set(ByVal value As ISinglePKEntity)
            Using Write("Ref")
                _obj = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Code")>
    Public Property Code() As Byte
        Get
            Using Read("Code")
                Return _code
            End Using
        End Get
        Set(ByVal value As Byte)
            Using Write("Code")
                _code = value
                If _trigger Then
                    _trigger = False
                    _obj = Worm.OrmManager.CurrentManager.GetKeyEntityFromCacheOrCreate(_id, GetObjectType())
                End If
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Version", Behavior:=Field2DbRelations.RowVersion)>
    Public Property Version() As Byte()
        Get
            Using Read("Version")
                Return _v
            End Using
        End Get
        Set(ByVal value As Byte())
            Using Write("Version")
                _v = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="XML")>
    Public Property Xml() As System.Xml.XmlDocument
        Get
            Using Read("XML")
                Return _x
            End Using
        End Get
        Set(ByVal value As System.Xml.XmlDocument)
            Using Write("XML")
                _x = value
                If value IsNot Nothing Then
                    AddHandler _x.NodeChanging, AddressOf NodeChanged
                    AddHandler _x.NodeInserting, AddressOf NodeChanged
                    AddHandler _x.NodeRemoving, AddressOf NodeChanged
                End If
            End Using
        End Set
    End Property

    Protected Sub NodeChanged(ByVal sender As Object, ByVal e As System.Xml.XmlNodeChangedEventArgs)
        Using Write("XML")

        End Using
    End Sub

    Public Function CreateContainingEntity(ByVal mgr As Worm.OrmManager, ByVal propertyAlias As String, ByVal value As IEnumerable(Of PKDesc)) As Object Implements Worm.Entities.IEntityFactory.CreateContainingEntity
        _obj = mgr.GetKeyEntityFromCacheOrCreate(value(0).Value, GetObjectType())
        Return _obj
    End Function

    Public Function ExtractValues(mpe As ObjectMappingEngine, oschema As IEntitySchema, propertyAlias As String) As IEnumerable(Of PKDesc) Implements IEntityFactory.ExtractValues
        Return _obj.GetPKs(mpe)
    End Function
End Class

Public Class Table3Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements ISchemaWithM2M

    Private _idx As OrmObjectIndex
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.Table3")}
    Private _rels() As M2MRelationDesc

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.Table3")
    End Sub

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", Table, "id"))
                idx.Add(New MapField2Column("Ref", Table, "ref_id"))
                idx.Add(New MapField2Column("Code", Table, "code"))
                idx.Add(New MapField2Column("Version", Table, "v"))
                idx.Add(New MapField2Column("XML", Table, "x"))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function

    Public Function GetM2MRelations() As M2MRelationDesc() Implements ISchemaWithM2M.GetM2MRelations
        If _rels Is Nothing Then
            _rels = New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(Table1), _schema.GetTables(GetType(Tables1to3))(0), "table1", True, New System.Data.Common.DataTableMapping, GetType(Tables1to3)) _
            }
        End If
        Return _rels
    End Function

End Class

Public Class Table33
    Inherits Table3

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        MyBase.New(id)
    End Sub

    'Protected Overrides Function GetNew() As Table3
    '    Return New Table33(Identifier, OrmCache, OrmSchema)
    'End Function
End Class