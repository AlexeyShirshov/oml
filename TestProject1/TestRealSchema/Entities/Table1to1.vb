Imports Worm.Entities
Imports Worm.Cache
Imports Worm.Entities.Meta

<Entity(GetType(Tables1to1.TablesImplementation), "1")> _
Public Class Tables1to1
    Inherits SinglePKEntity
    Implements IOptimizedValues, ICopyProperties

    Private _table1 As Table1
    Private _table1back As Table1
    Private _k As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
        PKLoaded("ID")
    End Sub

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

    Protected Function CopyProperties(ByVal [to] As Object) As Boolean Implements ICopyProperties.CopyTo
        Dim dst = TryCast([to], Tables1to1)
        If dst IsNot Nothing Then
            With Me
                dst._id = ._id
                dst._table1 = ._table1
                dst._table1back = ._table1back
                dst._k = ._k
            End With

            Return True
        End If

        Return False
    End Function

    Public Overridable Function SetValueOptimized(ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) As Boolean Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "K"
                K = CStr(value)
            Case "Table1"
                Table1 = CType(value, TestProject1.Table1)
            Case "Table1Back"
                Table1Back = CType(value, TestProject1.Table1)
            Case Else
                Return False
                'SetValueReflection(fieldName, value, oschema)
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return True
    End Function

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema, ByRef found As Boolean) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        found = True
        Select Case propertyAlias
            Case "K"
                Return _k
            Case "Table1"
                Return _table1
            Case "Table1Back"
                Return _table1back
            Case "ID"
                Return Identifier
            Case Else
                found = False
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
        Return Nothing
    End Function
    <EntityPropertyAttribute(PropertyAlias:="K")> _
    Public Property K() As String
        Get
            Using Read("K")
                Return _k
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("K")
                _k = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Table1")> _
    Public Property Table1() As Table1
        Get
            Using Read("Table1")
                Return _table1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using Write("Table1")
                _table1 = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Table1Back")> _
    Public Property Table1Back() As Table1
        Get
            Using Read("Table1")
                Return _table1back
            End Using
        End Get
        Set(ByVal value As Table1)
            Using Write("Table1")
                _table1back = value
            End Using
        End Set
    End Property

    Public Class TablesImplementation
        Inherits ObjectSchemaBaseImplementation
        Implements IRelation

        Private _idx As OrmObjectIndex
        'Public Shared _tables() As SourceFragment = {New SourceFragment("dbo.Table1to1")}

        Public Sub New()
            _tbl = New SourceFragment("dbo.Table1to1")
        End Sub

        'Public Enum Tables
        '    Main
        'End Enum

        Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
            Get
                If _idx Is Nothing Then
                    Dim idx As New OrmObjectIndex
                    idx.Add(New MapField2Column("ID", Table, "id"))
                    idx.Add(New MapField2Column("K", Table, "k"))
                    idx.Add(New MapField2Column("Table1", Table, "table1"))
                    idx.Add(New MapField2Column("Table1Back", Table, "table1_back"))
                    _idx = idx
                End If
                Return _idx
            End Get
        End Property

        'Public Overrides Function GetTables() As SourceFragment()
        '    Return _tables
        'End Function

        Public Function GetFirstType() As IRelation.RelationDesc Implements IRelation.GetFirstType
            Return New IRelation.RelationDesc("Table1", GetType(Table1), False)
        End Function

        Public Function GetSecondType() As IRelation.RelationDesc Implements IRelation.GetSecondType
            Return New IRelation.RelationDesc("Table1Back", GetType(Table1), True)
        End Function

    End Class
End Class