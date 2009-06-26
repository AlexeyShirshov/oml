Imports Worm.Cache
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Query

Namespace Entities.Meta
    Friend Class SimpleObjectSchema
        Implements IEntitySchema

        Private _table As SourceFragment
        Private _cols As Collections.IndexedCollection(Of String, MapField2Column) = New OrmObjectIndex

        Friend Sub New(ByVal cols As Collections.IndexedCollection(Of String, MapField2Column))
            _cols = cols
            _table = _cols(0).Table
        End Sub

        Friend Sub New(ByVal t As Type, ByVal table As String, ByVal schema As String, ByVal cols As ICollection(Of EntityPropertyAttribute))
            'If String.IsNullOrEmpty(pk) Then
            '    Throw New QueryGeneratorException(String.Format("Primary key required for {0}", t))
            'End If

            'If tables IsNot Nothing Then
            '    _tables = New SourceFragment(tables.Length - 1) {}
            '    For i As Integer = 0 To tables.Length - 1
            '        _tables(i) = New SourceFragment(tables(i))
            '    Next
            'End If

            If String.IsNullOrEmpty(table) Then
                Throw New ArgumentNullException("table")
            Else
                _table = New SourceFragment(schema, table)
            End If

            For Each c As EntityPropertyAttribute In cols
                If String.IsNullOrEmpty(c.PropertyAlias) Then
                    Throw New ObjectMappingException(String.Format("Cann't create schema for entity {0}", t))
                End If

                If String.IsNullOrEmpty(c.Column) Then
                    'If c.PropertyAlias = OrmBaseT.PKName Then
                    '    c.Column = pk
                    'Else
                    Throw New ObjectMappingException(String.Format("Column for property {0} entity {1} is undefined", c.PropertyAlias, t))
                    'End If
                End If

                'Dim tbl As SourceFragment = Nothing
                'If Not String.IsNullOrEmpty(c.TableName) Then
                '    tbl = FindTbl(c.TableName)
                'Else
                '    If _tables.Length = 0 Then
                '        Throw New DBSchemaException(String.Format("Neigther entity {1} nor column {0} has table name", c.FieldName, t))
                '    End If

                '    tbl = _tables(0)
                'End If

                _cols.Add(New MapField2Column(c.PropertyAlias, c.Column, _table, c.Behavior, c.DBType, c.DBSize) With {.ColumnName = c.ColumnName})
            Next

            '_cols.Add(New MapField2Column("ID", pk, _tables(0)))
        End Sub

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
            Return _cols
        End Function

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _table
            End Get
        End Property
    End Class

    Public NotInheritable Class SimpleTwotableObjectSchema
        Implements IEntitySchema
        Implements IMultiTableObjectSchema

        Private _cols As Collections.IndexedCollection(Of String, MapField2Column) = New OrmObjectIndex
        Private _tables(1) As SourceFragment
        Private _pk As String
        Private _fk As String

        Friend Sub New(ByVal realTypecols As Collections.IndexedCollection(Of String, MapField2Column), _
                       ByVal baseTypeCols As Collections.IndexedCollection(Of String, MapField2Column))
            _tables(0) = realTypecols(0).Table
            _tables(1) = baseTypeCols(0).Table

            For Each m As MapField2Column In realTypecols
                _cols.Add(m)
                If (m._newattributes And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    _pk = m.ColumnExpression
                End If
            Next

            For Each m As MapField2Column In baseTypeCols
                If (m._newattributes And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    _fk = m.ColumnExpression
                Else
                    _cols(m._propertyAlias).Table = m.Table
                End If
            Next
        End Sub

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
            Return _cols
        End Function

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _tables(0)
            End Get
        End Property

        Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Criteria.Joins.QueryJoin Implements IMultiTableObjectSchema.GetJoins
            Return New QueryJoin(right, JoinType.Join, Ctor.column(left, _pk).eq(right, _fk).Filter)
        End Function

        Public Function GetTables() As SourceFragment() Implements IMultiTableObjectSchema.GetTables
            Return _tables
        End Function
    End Class

    Friend NotInheritable Class SimpleTypedEntitySchema
        Inherits SimpleObjectSchema
        Implements ICacheBehavior ', ITypedSchema

        Private _t As Type

        Friend Sub New(ByVal t As Type, ByVal cols As Collections.IndexedCollection(Of String, MapField2Column))
            MyBase.New(cols)
            _t = t
        End Sub

        Public Function GetEntityKey(ByVal filterInfo As Object) As String Implements ICacheBehavior.GetEntityKey
            Return _t.ToString
        End Function

        Public Function GetEntityTypeKey(ByVal filterInfo As Object) As Object Implements ICacheBehavior.GetEntityTypeKey
            Return _t
        End Function

        'Public ReadOnly Property EntityType() As System.Type Implements ITypedSchema.EntityType
        '    Get
        '        Return _t
        '    End Get
        'End Property
    End Class
End Namespace