Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm

#Region " Interfaces "
    Public Interface IOrmObjectSchemaBase
        Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
        Function MapSort2FieldName(ByVal sort As String) As String
        Function GetM2MRelations() As M2MRelation()
        Function GetFilter(ByVal filter_info As Object) As IOrmFilter
        Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
        Function GetSuppressedColumns() As ColumnAttribute()
        ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean
        Function ExternalSort(ByVal sort As String, ByVal sortType As SortType, ByVal objs As IList) As IList
    End Interface

    Public Interface IOrmObjectSchema
        Inherits IOrmObjectSchemaBase
        Function GetTables() As OrmTable()
        Function GetJoins(ByVal left As OrmTable, ByVal right As OrmTable) As OrmJoin
    End Interface

    Public Interface IOrmTableFunction
        Function GetFunction(ByVal table As OrmTable, ByVal pmgr As ParamMgr) As OrmTable
    End Interface

    Public Interface IOrmFullTextSupport
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

#End Region

#Region " Classes "

    Public Class OrmTable

        Private _table As String

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

    Public Class EditableList
        Private _mainId As Integer
        Private _mainList As IList(Of Integer)
        Private _addedList As New List(Of Integer)
        Private _deletedList As New List(Of Integer)
        Private _non_direct As Boolean
        Private _saved As Boolean
        Private _mainType As Type
        Private _subType As Type
        Private _new As Generic.List(Of Integer)

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type)
            _mainList = mainList
            _mainId = mainId
            _mainType = mainType
            _subType = subType
        End Sub

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean)
            MyClass.New(mainId, mainList, mainType, subType)
            _non_direct = Not direct
        End Sub

        Public ReadOnly Property Current() As ICollection(Of Integer)
            Get
                Dim arr As New List(Of Integer)(_mainList)
                arr.AddRange(_addedList)
                For Each o As Integer In _deletedList
                    arr.Remove(o)
                Next
                Return arr
            End Get
        End Property

        Public ReadOnly Property Original() As ICollection(Of Integer)
            Get
                Return _mainList
            End Get
        End Property

        Public Sub Accept()
            CType(_mainList, List(Of Integer)).AddRange(_addedList)
            _addedList.Clear()
            For Each o As Integer In _deletedList
                CType(_mainList, List(Of Integer)).Remove(o)
            Next
            _deletedList.Clear()
            _saved = False
            RemoveNew()
        End Sub

        Protected Sub RejectRelated(ByVal id As Integer, ByVal add As Boolean)
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetDirect)
            Dim l As IList(Of Integer) = m.Entry.Added
            If Not add Then
                l = m.Entry.Deleted
            End If
            If l.Contains(_mainId) Then
                l.Remove(_mainId)
            End If
        End Sub

        Public Sub Reject(ByVal rejectDual As Boolean)
            If rejectDual Then
                For Each id As Integer In _addedList
                    RejectRelated(id, True)
                Next
            End If
            _addedList.Clear()
            If rejectDual Then
                For Each id As Integer In _deletedList
                    RejectRelated(id, False)
                Next
            End If
            _deletedList.Clear()
            RemoveNew()
        End Sub

        Public ReadOnly Property Deleted() As IList(Of Integer)
            Get
                Return _deletedList
            End Get
        End Property

        Public ReadOnly Property Added() As IList(Of Integer)
            Get
                Return _addedList
            End Get
        End Property

        Public Sub AddRange(ByVal ids As IEnumerable(Of Integer))
            For Each id As Integer In ids
                Add(id)
            Next
        End Sub

        Public Sub Add(ByVal id As Integer)
            If _deletedList.Contains(id) Then
                _deletedList.Remove(id)
            Else
                _addedList.Add(id)
            End If
        End Sub

        Public Sub Delete(ByVal id As Integer)
            If _addedList.Contains(id) Then
                _addedList.Remove(id)
            Else
                _deletedList.Add(id)
            End If
        End Sub

        Public ReadOnly Property HasDeleted() As Boolean
            Get
                Return _deletedList.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasAdded() As Boolean
            Get
                Return _addedList.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return HasDeleted OrElse HasAdded
            End Get
        End Property

        Public ReadOnly Property Direct() As Boolean
            Get
                Return Not _non_direct
            End Get
        End Property

        Public Property MainId() As Integer
            Get
                Return _mainId
            End Get
            Protected Friend Set(ByVal value As Integer)
                _mainId = value
            End Set
        End Property

        'Public Function Clone(ByVal mgr As OrmManagerBase, ByVal main As Type, ByVal subType As Type, ByVal added As List(Of Integer)) As EditableList
        '    Dim newl As EditableList = Nothing
        '    If Not mgr.IsNewObject(main, _mainId) Then
        '        newl = New EditableList(_mainId, _mainList, _mainType, _subType, Direct)
        '        newl._deletedList = _deletedList
        '        Dim ad As New List(Of Integer)
        '        For Each id As Integer In _addedList
        '            If Not mgr.IsNewObject(subType, id) Then
        '                ad.Add(id)
        '            ElseIf added IsNot Nothing Then
        '                added.Add(id)
        '            End If
        '        Next
        '        newl._addedList = ad
        '        _saved = True
        '    End If
        '    Return newl
        'End Function

        Protected Function GetDirect() As Boolean
            If SubType Is MainType Then
                Return Not Direct
            Else
                Return Direct
            End If
        End Function

        Protected Function CheckDual(ByVal mgr As OrmManagerBase, ByVal id As Integer) As Boolean
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetDirect)
            Dim c As Boolean = True
            For Each i As Integer In m.Entry.original
                If i = _mainId Then
                    c = False
                    Exit For
                End If
            Next
            If c AndAlso m.Entry.Saved Then
                For Each i As Integer In m.Entry.Current
                    If i = _mainId Then
                        c = False
                        Exit For
                    End If
                Next
            End If
            Return c
        End Function

        Public Function PrepareSave(ByVal mgr As OrmManagerBase) As EditableList
            Dim newl As EditableList = Nothing
            If Not mgr.IsNewObject(_mainType, _mainId) Then
                Dim ad As New List(Of Integer)
                For Each id As Integer In _addedList
                    If mgr.IsNewObject(SubType, id) Then
                        If _new Is Nothing Then
                            _new = New List(Of Integer)
                        End If
                        _new.Add(id)
                    ElseIf CheckDual(mgr, id) Then
                        ad.Add(id)
                    End If
                Next
                If ad.Count > 0 OrElse _deletedList.Count > 0 Then
                    newl = New EditableList(_mainId, _mainList, _mainType, _subType, Direct)
                    newl._deletedList = _deletedList
                    newl._addedList = ad
                End If
            End If
            Return newl
        End Function

        Public Function PrepareNewSave(ByVal mgr As OrmManagerBase) As EditableList
            Dim newl As EditableList = Nothing
            If Not mgr.IsNewObject(_mainType, _mainId) AndAlso HasNew Then
                Dim ad As New List(Of Integer)
                For Each id As Integer In _new
                    If mgr.IsNewObject(SubType, id) Then
                        Throw New InvalidOperationException("List has new object " & id)
                    ElseIf CheckDual(mgr, id) Then
                        ad.Add(id)
                    End If
                Next
                If ad.Count > 0 Then
                    newl = New EditableList(_mainId, New List(Of Integer), _mainType, _subType, Direct)
                    newl.AddRange(ad)
                End If
            End If
            Return newl
        End Function

        Protected Friend Property Saved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

        Protected Friend Sub Update(ByVal id As Integer, ByVal oldId As Integer)
            Dim idx As Integer = _addedList.IndexOf(oldId)
            If idx < 0 Then
                Throw New ArgumentException("Old id is not found: " & oldId)
            End If

            _addedList.RemoveAt(idx)
            _addedList.Add(id)

            If HasNew Then
                If _new.Remove(oldId) Then
                    _new.Add(id)
                End If
            End If
        End Sub

        Public ReadOnly Property MainType() As Type
            Get
                Return _mainType
            End Get
        End Property

        Public ReadOnly Property SubType() As Type
            Get
                Return _subType
            End Get
        End Property

        Protected Sub RemoveNew()
            If _new IsNot Nothing Then
                _new.Clear()
            End If
        End Sub

        Public ReadOnly Property HasNew() As Boolean
            Get
                Return _new IsNot Nothing AndAlso _new.Count > 0
            End Get
        End Property
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

        Public Function AddParam(ByVal name As String, ByVal value As Object) As String Implements ICreateParam.AddParam
            If NamedParams Then
                Dim p As System.Data.Common.DbParameter = GetParameter(name)
                If p Is Nothing Then
                    Return CreateParam(value)
                Else
                    If p.Value Is Nothing OrElse p.Value.Equals(value) Then
                        Return name
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
            Dim tf As IOrmTableFunction = TryCast(schema, IOrmTableFunction)
            Dim tt As OrmTable = table
            If tf IsNot Nothing Then
                Dim f As OrmTable = tf.GetFunction(table, pmgr)
                If f IsNot Nothing Then
                    table = f
                End If
            End If
            Dim i As Integer = _aliases.Count + 1
            Dim [alias] As String = "t" & i
            _aliases.Add(tt, [alias])
            Return [alias]
        End Function

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

End Namespace
