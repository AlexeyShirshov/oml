Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm

#Region " Interfaces "
    Public Interface IRelation
        Function GetFirstType() As Pair(Of String, Type)
        Function GetSecondType() As Pair(Of String, Type)
    End Interface

    Public Interface IOrmObjectSchemaBase
        Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
        'Function MapSort2FieldName(ByVal sort As String) As String
        Function GetM2MRelations() As M2MRelation()
        Function GetFilter(ByVal filter_info As Object) As IFilter
        Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
        Function GetSuppressedColumns() As ColumnAttribute()
        'ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean
        'Function ExternalSort(ByVal sort As String, ByVal sortType As SortType, ByVal objs As IList) As IList
    End Interface

    Public Interface IOrmSorting
        'ReadOnly Property IsExternalSort(ByVal s As Sort) As Boolean
        Function ExternalSort(Of T As {OrmBase, New})(ByVal s As Sort, ByVal objs As ICollection(Of T)) As ICollection(Of T)
        Function CreateSortComparer(ByVal s As Sort) As IComparer
        Function CreateSortComparer(Of T As {OrmBase, New})(ByVal s As Sort) As Generic.IComparer(Of T)
    End Interface

    Public Interface IOrmSortingEx
        Inherits IOrmSorting

        ReadOnly Property SortExpiration(ByVal s As Sort) As TimeSpan
    End Interface

    Public Interface IOrmObjectSchema
        Inherits IOrmObjectSchemaBase
        Function GetTables() As OrmTable()
        Function GetJoins(ByVal left As OrmTable, ByVal right As OrmTable) As OrmJoin
    End Interface

    'Public Interface IOrmTableFunction
    '    Function GetFunction(ByVal table As OrmTable, ByVal pmgr As ParamMgr) As OrmTable
    'End Interface

    Public Interface IOrmFullTextSupport
        Function GetQueryFields(ByVal contextKey As Object) As String()
        Function GetIndexedFields() As String()
        ReadOnly Property ApplayAsterisk() As Boolean
    End Interface

    Public Interface IOrmDictionary
        Function GetFirstDicField() As String
        Function GetSecondDicField() As String
    End Interface

    Public Interface IOrmSchemaInit
        Sub GetSchema(ByVal schema As OrmSchemaBase, ByVal t As Type)
    End Interface

    Public Interface ICacheBehavior
        Function GetEntityKey() As String
        Function GetEntityTypeKey() As Object
    End Interface

    Public Interface ICreateParam
        Function CreateParam(ByVal value As Object) As String
        Function AddParam(ByVal pname As String, ByVal value As Object) As String
        ReadOnly Property NamedParams() As Boolean
        ReadOnly Property Params() As IList(Of System.Data.Common.DbParameter)
        Function GetParameter(ByVal name As String) As System.Data.Common.DbParameter
    End Interface

    Public Interface IOrmEditable(Of T As {OrmBase})
        Sub CopyBody(ByVal from As T, ByVal [to] As T)
    End Interface

    Public Interface IJoinBehavior
        ReadOnly Property AlwaysJoinMainTable() As Boolean
        Function GetJoinField(ByVal t As Type) As String
    End Interface
#End Region

#Region " Classes "

    Public Class OrmTable

        Private _table As String

        Public Sub New()

        End Sub

        Public Sub New(ByVal tableName As String)
            _table = tableName
        End Sub

        Public Property TableName() As String
            Get
                Return _table
            End Get
            Set(ByVal value As String)
                _table = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return _table
        End Function

        Public Overridable Function OnTableAdd(ByVal pmgr As ParamMgr) As OrmTable
            Return Nothing
        End Function
    End Class

    Public Class MapField2Column
        Public ReadOnly _fieldName As String
        Public ReadOnly _columnName As String
        Public ReadOnly _tableName As OrmTable
        Private ReadOnly _newattributes As Field2DbRelations

        Public Sub New(ByVal fieldName As String, ByVal columnName As String, ByVal tableName As OrmTable)
            _fieldName = fieldName
            _columnName = columnName
            _tableName = tableName
            _newattributes = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal columnName As String, ByVal tableName As OrmTable, _
            ByVal newAttributes As Field2DbRelations)
            _fieldName = fieldName
            _columnName = columnName
            _tableName = tableName
            _newattributes = newAttributes
        End Sub

        Public Function GetAttributes(ByVal c As ColumnAttribute) As Field2DbRelations
            If _newattributes = Field2DbRelations.None Then
                Return c._behavior
            Else
                Return _newattributes
            End If
        End Function
    End Class

    Public Class M2MRelation
        Public ReadOnly Type As Type
        Public ReadOnly Table As OrmTable
        Public ReadOnly Column As String
        Public ReadOnly DeleteCascade As Boolean
        Public ReadOnly Mapping As System.Data.Common.DataTableMapping
        Public ReadOnly non_direct As Boolean
        Public ReadOnly ConnectedType As Type

        Public Sub New(ByVal type As Type, ByVal table As OrmTable, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            Me.Type = type
            Me.Table = table
            Me.Column = column
            Me.DeleteCascade = delete
            Me.Mapping = mapping
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As OrmTable, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping)
            non_direct = Not direct
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As OrmTable, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub
    End Class

    Public Class ParamMgr
        Implements ICreateParam

        Private _params As List(Of System.Data.Common.DbParameter)
        Private _schema As DbSchema
        Private _prefix As String
        Private _named_params As Boolean

        Public Sub New(ByVal schema As DbSchema, ByVal prefix As String)
            _schema = schema
            _params = New List(Of System.Data.Common.DbParameter)
            _prefix = prefix
            _named_params = schema.ParamName("p", 1) <> schema.ParamName("p", 2)
        End Sub

        Public Function AddParam(ByVal pname As String, ByVal value As Object) As String Implements ICreateParam.AddParam
            If NamedParams Then
                Dim p As System.Data.Common.DbParameter = GetParameter(pname)
                If p Is Nothing Then
                    Return CreateParam(value)
                Else
                    If p.Value Is Nothing OrElse p.Value.Equals(value) Then
                        Return pname
                    Else
                        Return CreateParam(value)
                    End If
                End If
            Else
                Return CreateParam(value)
            End If
        End Function

        Public Function CreateParam(ByVal value As Object) As String Implements ICreateParam.CreateParam
            If _schema Is Nothing Then
                Throw New InvalidOperationException("Object must be created")
            End If

            Dim pname As String = _schema.ParamName(_prefix, _params.Count + 1)
            _params.Add(_schema.CreateDBParameter(pname, value))
            Return pname
        End Function

        Public ReadOnly Property Params() As IList(Of System.Data.Common.DbParameter) Implements ICreateParam.Params
            Get
                Return _params
            End Get
        End Property

        Public Function GetParameter(ByVal name As String) As System.Data.Common.DbParameter Implements ICreateParam.GetParameter
            If Not String.IsNullOrEmpty(name) Then
                For Each p As System.Data.Common.DbParameter In _params
                    If p.ParameterName = name Then
                        Return p
                    End If
                Next
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Prefix() As String
            Get
                Return _prefix
            End Get
            'Set(ByVal value As String)
            '    _prefix = value
            'End Set
        End Property

        'Public ReadOnly Property IsEmpty() As Boolean Implements ICreateParam.IsEmpty
        '    Get
        '        Return _params Is Nothing
        '    End Get
        'End Property

        Public ReadOnly Property NamedParams() As Boolean Implements ICreateParam.NamedParams
            Get
                Return _named_params
            End Get
        End Property

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection)
            For Each p As System.Data.Common.DbParameter In _params
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection, ByVal start As Integer, ByVal count As Integer)
            For i As Integer = start To Math.Min(_params.Count, start + count) - 1
                Dim p As System.Data.Common.DbParameter = _params(i)
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub Clear(ByVal preserve As Integer)
            If preserve > 0 Then
                _params.RemoveRange(preserve - 1, _params.Count - preserve)
            End If
        End Sub
    End Class

    Public Structure AliasMgr
        Private _aliases As IDictionary(Of OrmTable, String)

        Private Sub New(ByVal aliases As IDictionary(Of OrmTable, String))
            _aliases = aliases
        End Sub

        Public Shared Function Create() As AliasMgr
            Return New AliasMgr(New Generic.Dictionary(Of OrmTable, String))
        End Function

        Public Function AddTable(ByRef table As OrmTable) As String
            Return AddTable(table, Nothing, Nothing)
        End Function

        Public Function AddTable(ByRef table As OrmTable, ByVal schema As IOrmObjectSchema, ByVal pmgr As ParamMgr) As String
            'Dim tf As IOrmTableFunction = TryCast(schema, IOrmTableFunction)
            Dim t As OrmTable = table
            Dim tt As OrmTable = table.OnTableAdd(pmgr)
            If tt IsNot Nothing Then
                '    Dim f As OrmTable = tf.GetFunction(table, pmgr)
                '    If f IsNot Nothing Then
                table = tt
                '    End If
            End If
            Dim i As Integer = _aliases.Count + 1
            Dim [alias] As String = "t" & i
            _aliases.Add(t, [alias])
            Return [alias]
        End Function

        Friend Sub AddTable(ByVal tbl As OrmTable, ByVal [alias] As String)
            _aliases.Add(tbl, [alias])
        End Sub

        'Public Function GetAlias(ByVal table As String) As String
        '    Return _aliases(table)
        'End Function

        Public ReadOnly Property Aliases() As IDictionary(Of OrmTable, String)
            Get
                Return _aliases
            End Get
        End Property

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return _aliases Is Nothing
            End Get
        End Property
    End Structure

#End Region

    Public Enum SortType
        Asc
        Desc
    End Enum

    Public Enum FilterOperation
        Equal
        NotEqual
        GreaterThan
        GreaterEqualThan
        LessEqualThan
        LessThan
        [In]
        NotIn
        [Like]
    End Enum

    Public Enum ConditionOperator
        [And]
        [Or]
    End Enum

    Public Enum JoinType
        Join
        LeftOuterJoin
        RightOuterJoin
        FullJoin
        CrossJoin
    End Enum

    Public NotInheritable Class SimpleObjectSchema
        Implements IOrmObjectSchema

        'Private _tables(-1) As OrmTable
        Private _table As OrmTable
        Private _cols As New Orm.OrmObjectIndex

        Friend Sub New(ByVal t As Type, ByVal table As String, ByVal cols As ICollection(Of ColumnAttribute), ByVal pk As String)
            If String.IsNullOrEmpty(pk) Then
                Throw New DBSchemaException(String.Format("Primary key required for {0}", t))
            End If

            'If tables IsNot Nothing Then
            '    _tables = New OrmTable(tables.Length - 1) {}
            '    For i As Integer = 0 To tables.Length - 1
            '        _tables(i) = New OrmTable(tables(i))
            '    Next
            'End If

            If String.IsNullOrEmpty(table) Then
                Throw New ArgumentNullException("table")
            Else
                _table = New OrmTable(table)
            End If

            For Each c As ColumnAttribute In cols
                If String.IsNullOrEmpty(c.FieldName) Then
                    Throw New DBSchemaException(String.Format("Cann't create schema for entity {0}", t))
                End If

                If String.IsNullOrEmpty(c.Column) Then
                    If c.FieldName = "ID" Then
                        c.Column = pk
                    Else
                        Throw New DBSchemaException(String.Format("Column for property {0} entity {1} is undefined", c.FieldName, t))
                    End If
                End If

                'Dim tbl As OrmTable = Nothing
                'If Not String.IsNullOrEmpty(c.TableName) Then
                '    tbl = FindTbl(c.TableName)
                'Else
                '    If _tables.Length = 0 Then
                '        Throw New DBSchemaException(String.Format("Neigther entity {1} nor column {0} has table name", c.FieldName, t))
                '    End If

                '    tbl = _tables(0)
                'End If

                _cols.Add(New MapField2Column(c.FieldName, c.Column, _table))
            Next

            '_cols.Add(New MapField2Column("ID", pk, _tables(0)))
        End Sub

        'Private Function FindTbl(ByVal table As String) As OrmTable
        '    For Each t As OrmTable In _tables
        '        If t.TableName = table Then
        '            Return t
        '        End If
        '    Next
        '    Dim l As Integer = _tables.Length
        '    ReDim Preserve _tables(l)
        '    _tables(l) = New OrmTable(table)
        '    Return _tables(l)
        'End Function

        Public Function GetJoins(ByVal left As OrmTable, ByVal right As OrmTable) As OrmJoin Implements IOrmObjectSchema.GetJoins
            Throw New NotSupportedException("Joins is not supported in simple mode")
        End Function

        Public Function GetTables() As OrmTable() Implements IOrmObjectSchema.GetTables
            Return New OrmTable() {_table}
        End Function

        Public Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements IOrmObjectSchemaBase.ChangeValueType
            newvalue = value
            Return False
        End Function

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IOrmObjectSchemaBase.GetFieldColumnMap
            Return _cols
        End Function

        Public Function GetFilter(ByVal filter_info As Object) As IFilter Implements IOrmObjectSchemaBase.GetFilter
            Return Nothing
        End Function

        Public Function GetM2MRelations() As M2MRelation() Implements IOrmObjectSchemaBase.GetM2MRelations
            'Throw New NotSupportedException("Many2many relations is not supported in simple mode")
            Return New M2MRelation() {}
        End Function

        Public Function GetSuppressedColumns() As ColumnAttribute() Implements IOrmObjectSchemaBase.GetSuppressedColumns
            Throw New NotSupportedException("GetSuppressedColumns relations is not supported in simple mode")
        End Function
    End Class

    Public MustInherit Class QueryAspect
        Public Enum AspectType
            Columns
        End Enum

        Private _type As AspectType

        Public ReadOnly Property AscpectType() As AspectType
            Get
                Return _type
            End Get
        End Property

        Public MustOverride Function GetStaticKey() As String
        Public MustOverride Function GetDynamicKey() As String
        Public MustOverride Function MakeStmt(ByVal s As OrmSchemaBase) As String

        Public Sub New(ByVal type As AspectType)
            _type = type
        End Sub
    End Class

    Public Class DistinctAspect
        Inherits QueryAspect

        Public Sub New()
            MyBase.New(AspectType.Columns)
        End Sub

        Public Overrides Function GetDynamicKey() As String
            Return String.Empty
        End Function

        Public Overrides Function GetStaticKey() As String
            Return "distinct"
        End Function

        Public Overrides Function MakeStmt(ByVal s As OrmSchemaBase) As String
            Return "distinct "
        End Function
    End Class

    Public Class TopAspect
        Inherits QueryAspect

        Private _top As Integer
        Private _sort As Sort

        Public Sub New(ByVal top As Integer)
            MyBase.New(AspectType.Columns)
            _top = top
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort As Sort)
            MyBase.New(AspectType.Columns)
            _top = top
            _sort = sort
        End Sub

        Public Overrides Function GetDynamicKey() As String
            Return "-top-" & _top.ToString & "-"
        End Function

        Public Overrides Function GetStaticKey() As String
            If _sort IsNot Nothing Then
                Return "-top-" & _sort.ToString
            End If
            Return "-top-"
        End Function

        Public Overrides Function MakeStmt(ByVal s As OrmSchemaBase) As String
            Return CType(s, DbSchema).TopStatement(_top)
        End Function
    End Class
End Namespace
