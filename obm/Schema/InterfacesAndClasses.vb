Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Criteria.Joins
Imports Worm.Cache

Namespace Orm.Meta

#Region " Interfaces "

    Public Interface IRelation
        Structure RelationDesc
            Public PropertyName As String
            Public EntityType As Type
            Public Key As String

            Public ReadOnly Property Direction() As Boolean
                Get
                    Return Not (M2MRelation.RevKey = Key)
                End Get
            End Property

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
            End Sub

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type, ByVal direction As Boolean)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
                Me.Key = M2MRelation.GetKey(direction)
            End Sub
        End Structure

        Function GetFirstType() As RelationDesc
        Function GetSecondType() As RelationDesc
    End Interface

    Public Interface IOrmPropertyMap
        Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
    End Interface

    Public Interface IObjectSchemaBase
        Inherits IOrmPropertyMap
        Function GetTables() As SourceFragment()
        Function GetSuppressedColumns() As ColumnAttribute()
        Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
    End Interface

    Public Interface IOrmObjectSchemaBase
        Inherits IObjectSchemaBase
        Function GetFilter(ByVal filter_info As Object) As IFilter
    End Interface

    Public Interface IOrmSorting
        'ReadOnly Property IsExternalSort(ByVal s As Sort) As Boolean
        'Function ExternalSort(Of T As {OrmBase, New})(ByVal s As Sort, ByVal objs As ReadOnlyList(Of T)) As ReadOnlyList(Of T)
        Function CreateSortComparer(ByVal s As Sort) As IComparer
        Function CreateSortComparer(Of T As {_IEntity})(ByVal s As Sort) As Generic.IComparer(Of T)
    End Interface

    Public Interface IOrmSorting2
        ReadOnly Property SortExpiration(ByVal s As Sort) As TimeSpan
    End Interface

    Public Interface IOrmRelationalSchema
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As OrmJoin
    End Interface

    Public Interface IOrmRelationalSchemaWithM2M
        Inherits IOrmRelationalSchema
        Function GetM2MRelations() As M2MRelation()
    End Interface

    Public Interface IRelMapObjectSchema
        Inherits IOrmRelationalSchemaWithM2M, IObjectSchemaBase
    End Interface

    Public Interface IOrmObjectSchema
        Inherits IOrmObjectSchemaBase, IOrmRelationalSchemaWithM2M
    End Interface

    Public Interface IReadonlyObjectSchema
        Function GetEditableSchema() As IRelMapObjectSchema
    End Interface

    Public Interface IDBValueFilter
        Function CreateValue(ByVal c As ColumnAttribute, ByVal obj As IEntity, ByVal value As Object) As Object
    End Interface

    Public Interface IFactory
        Sub CreateObject(ByVal field As String, ByVal value As Object)
    End Interface

    'Public Interface IGetFilterValue
    '    ReadOnly Property FilterValue() As IDBValueFilter
    'End Interface

    'Public Interface IGetFactory
    '    ReadOnly Property Factory() As IFactory
    'End Interface

    'Public Interface IOrmTableFunction
    '    Function GetFunction(ByVal table As SourceFragment, ByVal pmgr As ParamMgr) As SourceFragment
    'End Interface

    Public Interface IOrmFullTextSupport
        Function GetQueryFields(ByVal contextKey As Object) As String()
        Function GetIndexedFields() As String()
        ReadOnly Property ApplayAsterisk() As Boolean
    End Interface

    Public Interface IOrmFullTextSupportEx
        Inherits IOrmFullTextSupport

        ReadOnly Property UseFreeText() As Boolean
        Sub MakeSearchString(ByVal contextKey As Object, ByVal tokens() As String, ByVal sb As StringBuilder)
    End Interface

    Public Interface IOrmDictionary
        Function GetFirstDicField() As String
        Function GetSecondDicField() As String
    End Interface

    Public Interface IOrmSchemaInit
        Sub GetSchema(ByVal schema As QueryGenerator, ByVal t As Type)
    End Interface

    Public Interface ICacheBehavior
        Function GetEntityKey(ByVal filterInfo As Object) As String
        Function GetEntityTypeKey(ByVal filterInfo As Object) As Object
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

    Public Interface IFtsStringFormater
        Function GetFtsString(ByVal section As String, ByVal contextKey As Object, ByVal f As IOrmFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String
        Function GetTokens() As String()
    End Interface

    Public Interface ITableFunction
        ReadOnly Property GetRealTable() As String
    End Interface

    Public Interface IGetJoinsWithContext
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As OrmJoin
    End Interface

    Public Interface IConnectedFilter
        Function ModifyFilterInfo(ByVal filterInfo As Object, ByVal selectedType As Type, ByVal filterType As Type) As Object
    End Interface
#End Region

#Region " Classes "

    Public Class PKDesc
        Public ReadOnly PropertyAlias As String
        Public ReadOnly Value As Object

        Public Sub New(ByVal propAlias As String, ByVal value As Object)
            Me.PropertyAlias = propAlias
            Me.Value = value
        End Sub
    End Class

    Public Class SourceFragment

        Private _table As String
        Private _schema As String

        Public Sub New()

        End Sub

        Public Sub New(ByVal tableName As String)
            _table = tableName
        End Sub

        Public Sub New(ByVal schema As String, ByVal tableName As String)
            _table = tableName
            _schema = schema
        End Sub

        'Public Property TableName() As String
        '    Get
        '        Return _table
        '    End Get
        '    Set(ByVal value As String)
        '        _table = value
        '    End Set
        'End Property

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Overridable Function OnTableAdd(ByVal pmgr As ICreateParam) As SourceFragment
            Return Nothing
        End Function

        Public ReadOnly Property RawName() As String
            Get
                Return _schema & "^" & _table
            End Get
        End Property

        Public ReadOnly Property Schema() As String
            Get
                Return _schema
            End Get
        End Property

        Public ReadOnly Property Name() As String
            Get
                Return _table
            End Get
        End Property
    End Class

    Public Class MapField2Column
        Public ReadOnly _fieldName As String
        Public ReadOnly _columnName As String
        Public ReadOnly _tableName As SourceFragment
        Private ReadOnly _newattributes As Field2DbRelations

        Public Sub New(ByVal fieldName As String, ByVal columnName As String, ByVal tableName As SourceFragment)
            _fieldName = fieldName
            _columnName = columnName
            _tableName = tableName
            _newattributes = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
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
        Public ReadOnly Table As SourceFragment
        Public ReadOnly Column As String
        Public ReadOnly DeleteCascade As Boolean
        Public ReadOnly Mapping As System.Data.Common.DataTableMapping
        Public ReadOnly ConnectedType As Type
        Public ReadOnly Key As String

        Public Const RevKey As String = "xxx%rev$"
        Public Const DirKey As String = "xxx%direct$"

        Private _entityName As String
        Private _type As Type

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            _type = type
            Me.Table = table
            Me.Column = column
            Me.DeleteCascade = delete
            Me.Mapping = mapping
            Me.Key = key
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As QueryGenerator, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As QueryGenerator, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal generator As QueryGenerator, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As QueryGenerator, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
        End Sub

        Public Sub New(ByVal generator As QueryGenerator, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            _entityName = entityName
            _type = generator.GetTypeByEntityName(entityName)
            Me.Table = table
            Me.Column = column
            Me.DeleteCascade = delete
            Me.Mapping = mapping
            Me.Key = key
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(type, table, column, delete, mapping, DirKey)
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping)
            Key = GetKey(direct)
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, _
            ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping, direct)
            Me.ConnectedType = connectedType
        End Sub

        Public ReadOnly Property EntityName() As String
            Get
                Return _entityName
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _type
            End Get
        End Property

        Public ReadOnly Property non_direct() As Boolean
            Get
                Return Key = RevKey
            End Get
        End Property

        Public Shared Function GetKey(ByVal direct As Boolean) As String
            If direct Then
                Return DirKey
            Else
                Return RevKey
            End If
        End Function

        Public Shared Function GetRevKey(ByVal key As String) As String
            If key = DirKey Then
                Return RevKey
            ElseIf key = RevKey Then
                Return DirKey
            Else
                Return key
            End If
        End Function
    End Class

    Public NotInheritable Class SimpleObjectSchema
        Implements IOrmObjectSchema

        'Private _tables(-1) As SourceFragment
        Private _table As SourceFragment
        Private _cols As New OrmObjectIndex

        Friend Sub New(ByVal t As Type, ByVal table As String, ByVal cols As ICollection(Of ColumnAttribute), ByVal pk As String)
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

            For Each c As ColumnAttribute In cols
                If String.IsNullOrEmpty(c.FieldName) Then
                    Throw New QueryGeneratorException(String.Format("Cann't create schema for entity {0}", t))
                End If

                If String.IsNullOrEmpty(c.Column) Then
                    If c.FieldName = "ID" Then
                        c.Column = pk
                    Else
                        Throw New QueryGeneratorException(String.Format("Column for property {0} entity {1} is undefined", c.FieldName, t))
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

                _cols.Add(New MapField2Column(c.FieldName, c.Column, _table))
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

        Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As OrmJoin Implements IOrmObjectSchema.GetJoins
            Throw New NotSupportedException("Joins is not supported in simple mode")
        End Function

        Public Function GetTables() As SourceFragment() Implements IOrmObjectSchema.GetTables
            Return New SourceFragment() {_table}
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

        Public Function GetM2MRelations() As M2MRelation() Implements IOrmObjectSchema.GetM2MRelations
            'Throw New NotSupportedException("Many2many relations is not supported in simple mode")
            Return New M2MRelation() {}
        End Function

        Public Function GetSuppressedColumns() As ColumnAttribute() Implements IOrmObjectSchemaBase.GetSuppressedColumns
            'Throw New NotSupportedException("GetSuppressedColumns relations is not supported in simple mode")
            Return Nothing
        End Function
    End Class

#End Region

End Namespace

Namespace Orm.Query

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
        Public MustOverride Function MakeStmt(ByVal s As QueryGenerator) As String

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

        Public Overrides Function MakeStmt(ByVal s As QueryGenerator) As String
            Return "distinct "
        End Function
    End Class

    Public MustInherit Class TopAspect
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
            Return "-top-" & Top.ToString & "-"
        End Function

        Public Overrides Function GetStaticKey() As String
            If _sort IsNot Nothing Then
                Return "-top-" & _sort.ToString
            End If
            Return "-top-"
        End Function

        Protected ReadOnly Property Top() As Integer
            Get
                Return _top
            End Get
        End Property
    End Class

End Namespace

Namespace Database
    Public Class TopAspect
        Inherits Orm.Query.TopAspect

        Public Sub New(ByVal top As Integer)
            MyBase.New(top)
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort As Sort)
            MyBase.New(top, sort)
        End Sub

        Public Overrides Function MakeStmt(ByVal s As QueryGenerator) As String
            Return CType(s, SQLGenerator).TopStatement(Top)
        End Function
    End Class

End Namespace