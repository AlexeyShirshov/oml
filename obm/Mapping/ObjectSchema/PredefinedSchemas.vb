Imports Worm.Cache
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports System.Collections.Generic

Namespace Entities.Meta
    Public NotInheritable Class SimpleObjectSchema
        Implements IEntitySchema

        'Private _tables(-1) As SourceFragment
        Private _table As SourceFragment
        Private _cols As Collections.IndexedCollection(Of String, MapField2Column) = New OrmObjectIndex

        Friend Sub New(ByVal cols As Collections.IndexedCollection(Of String, MapField2Column))
            _cols = cols
        End Sub

        Friend Sub New(ByVal t As Type, ByVal table As String, ByVal cols As ICollection(Of EntityPropertyAttribute), ByVal pk As String)
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
                _table = New SourceFragment(table)
            End If

            For Each c As EntityPropertyAttribute In cols
                If String.IsNullOrEmpty(c.PropertyAlias) Then
                    Throw New ObjectMappingException(String.Format("Cann't create schema for entity {0}", t))
                End If

                If String.IsNullOrEmpty(c.Column) Then
                    If c.PropertyAlias = OrmBaseT.PKName Then
                        c.Column = pk
                    Else
                        Throw New ObjectMappingException(String.Format("Column for property {0} entity {1} is undefined", c.PropertyAlias, t))
                    End If
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

                _cols.Add(New MapField2Column(c.PropertyAlias, c.Column, _table))
            Next

            '_cols.Add(New MapField2Column("ID", pk, _tables(0)))
        End Sub

        'Private Function FindTbl(ByVal table As String) As SourceFragment
        '    For Each t As SourceFragment In _tables
        '        If t.TableName = table Then
        '            Return t
        '        End If
        '    Next
        '    Dim l As Integer = _tables.Length
        '    ReDim Preserve _tables(l)
        '    _tables(l) = New SourceFragment(table)
        '    Return _tables(l)
        'End Function

        'Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As OrmJoin Implements IOrmObjectSchema.GetJoins
        '    Throw New NotSupportedException("Joins is not supported in simple mode")
        'End Function

        'Public Function GetTables() As SourceFragment() Implements IOrmObjectSchema.GetTables
        '    Return New SourceFragment() {_table}
        'End Function

        'Public Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements IEntitySchema.ChangeValueType
        '    newvalue = value
        '    Return False
        'End Function

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
            Return _cols
        End Function

        'Public Function GetFilter(ByVal filter_info As Object) As IFilter Implements IContextObjectSchema.GetContextFilter
        '    Return Nothing
        'End Function

        'Public Function GetM2MRelations() As M2MRelation() Implements IOrmObjectSchema.GetM2MRelations
        '    'Throw New NotSupportedException("Many2many relations is not supported in simple mode")
        '    Return New M2MRelation() {}
        'End Function

        'Public Function GetSuppressedFields() As String() Implements IEntitySchema.GetSuppressedFields
        '    'Throw New NotSupportedException("GetSuppressedColumns relations is not supported in simple mode")
        '    Return Nothing
        'End Function

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _table
            End Get
        End Property
    End Class

End Namespace