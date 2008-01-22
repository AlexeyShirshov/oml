Imports Worm.Cache
Imports Worm.Orm.Meta
Imports Worm.Orm


<Entity(GetType(Table3Implementation), "1", EntityName:="Table3")> _
Public Class Table3
    Inherits OrmBaseT(Of Table3)
    Implements IOrmEditable(Of Table3)

    Private _obj As OrmBase
    Private _code As Byte
    Private _trigger As Boolean
    Private _id As Integer
    Private _v As Byte()
    Private _x As Xml.XmlDocument

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As Worm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyTable3(CType([from], Table3), CType([to], Table3))
    'End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Table3(Identifier, OrmCache, OrmSchema)
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Sub CopyTable3(ByVal [from] As Table3, ByVal [to] As Table3) Implements IOrmEditable(Of Table3).CopyBody
        With [from]
            [to]._obj = ._obj
            [to]._code = ._code
            [to]._v = ._v
            [to]._x = ._x
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

    Public Overrides Sub CreateObject(ByVal fieldName As String, ByVal value As Object)
        _id = CInt(value)
        If _code = 0 Then
            _trigger = True
        Else
            _obj = Worm.OrmManagerBase.CurrentManager.CreateDBObject(_id, GetObjectType())
        End If
    End Sub

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "Ref"
                RefObject = CType(value, OrmBase)
            Case "Code"
                Code = CByte(value)
            Case "Version"
                Version = CType(value, Byte())
            Case "XML"
                Xml = CType(value, System.Xml.XmlDocument)
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("Ref", Field2DbRelations.Factory)> _
    Public Property RefObject() As OrmBase
        Get
            Using SyncHelper(True, "Ref")
                Return _obj
            End Using
        End Get
        Set(ByVal value As OrmBase)
            Using SyncHelper(False, "Ref")
                _obj = value
            End Using
        End Set
    End Property

    <Column("Code")> _
    Public Property Code() As Byte
        Get
            Using SyncHelper(True, "Code")
                Return _code
            End Using
        End Get
        Set(ByVal value As Byte)
            Using SyncHelper(False, "Code")
                _code = value
                If _trigger Then
                    _trigger = False
                    _obj = Worm.OrmManagerBase.CurrentManager.CreateDBObject(_id, GetObjectType())
                End If
            End Using
        End Set
    End Property

    <Column("Version", Field2DbRelations.RowVersion)> _
    Public Property Version() As Byte()
        Get
            Using SyncHelper(True, "Version")
                Return _v
            End Using
        End Get
        Set(ByVal value As Byte())
            Using SyncHelper(False, "Version")
                _v = value
            End Using
        End Set
    End Property

    <Column("XML")> _
    Public Property Xml() As Xml.XmlDocument
        Get
            Using SyncHelper(True, "XML")
                Return _x
            End Using
        End Get
        Set(ByVal value As Xml.XmlDocument)
            Using SyncHelper(False, "XML")
                _x = value
                If value IsNot Nothing Then
                    AddHandler _x.NodeChanging, AddressOf NodeChanged
                    AddHandler _x.NodeInserting, AddressOf NodeChanged
                    AddHandler _x.NodeRemoving, AddressOf NodeChanged
                End If
            End Using
        End Set
    End Property

    Protected Sub NodeChanged(ByVal sender As Object, ByVal e As Xml.XmlNodeChangedEventArgs)
        PrepareUpdate()
    End Sub
End Class

Public Class Table3Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.Table3")}
    Private _rels() As M2MRelation

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Ref", "ref_id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Code", "code", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Version", "v", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("XML", "x", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As M2MRelation()
        If _rels Is Nothing Then
            _rels = New M2MRelation() { _
                New M2MRelation(GetType(Table1), TablesImplementation._tables(0), "table1", True, New System.Data.Common.DataTableMapping, GetType(Tables1to3)) _
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

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As Worm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Function GetNew() As Table3
    '    Return New Table33(Identifier, OrmCache, OrmSchema)
    'End Function
End Class