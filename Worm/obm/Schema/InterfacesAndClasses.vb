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
        Function GetFilter(ByVal filter_info As Object) As IOrmFilter
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

    Public Interface IAlwaysJoinMainTable
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
        Private _sort As Sort

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type, ByVal sort As Sort)
            _mainList = mainList
            _mainId = mainId
            _mainType = mainType
            _subType = subType
            _sort = sort
        End Sub

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean, ByVal sort As Sort)
            MyClass.New(mainId, mainList, mainType, subType, sort)
            _non_direct = Not direct
        End Sub

        Public ReadOnly Property CurrentCount() As Integer
            Get
                Return _mainList.Count + _addedList.Count - _deletedList.Count
            End Get
        End Property

        Public ReadOnly Property Current() As IList(Of Integer)
            Get
                Dim arr As New List(Of Integer)
                If _mainList.Count <> 0 OrElse _addedList.Count <> 0 Then
                    If _addedList.Count <> 0 AndAlso _mainList.Count <> 0 Then
                        Dim sort As Boolean = False
                        Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                        Dim col As New ArrayList
                        Dim c As IComparer = Nothing
                        If _sort IsNot Nothing Then
                            Dim sr As IOrmSorting = Nothing
                            col.AddRange(mgr.ConvertIds2Objects(_subType, _mainList, False))
                            If mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                                c = sr.CreateSortComparer(_sort)
                                sort = c IsNot Nothing
                            End If
                        End If
                        If Not sort Then
                            arr.AddRange(_mainList)
                            arr.AddRange(_addedList)
                        Else
                            Dim i, j As Integer
                            Do
                                If i = _mainList.Count Then
                                    For k As Integer = j To _addedList.Count - 1
                                        arr.Add(_addedList(k))
                                    Next
                                    Exit Do
                                End If
                                If j = _addedList.Count Then
                                    For k As Integer = i To _mainList.Count - 1
                                        arr.Add(_mainList(k))
                                    Next
                                    Exit Do
                                End If
                                Dim ex As OrmBase = CType(col(i), OrmBase)
                                Dim ad As OrmBase = mgr.CreateDBObject(_addedList(j), _subType)
                                If c.Compare(ex, ad) < 0 Then
                                    arr.Add(ex.Identifier)
                                    i += 1
                                Else
                                    arr.Add(ad.Identifier)
                                    j += 1
                                End If
                            Loop While True
                        End If
                    Else
                        arr.AddRange(_mainList)
                        arr.AddRange(_addedList)
                    End If

                    For Each o As Integer In _deletedList
                        arr.Remove(o)
                    Next
                End If
                Return arr
            End Get
        End Property

        Public ReadOnly Property Original() As ICollection(Of Integer)
            Get
                Return _mainList
            End Get
        End Property

        Public Function Accept(ByVal mgr As OrmDBManager) As Boolean
            Dim needaccept As Boolean
            If _sort Is Nothing Then
                CType(_mainList, List(Of Integer)).AddRange(_addedList)
                _addedList.Clear()
            Else
                If _addedList.Count > 0 Then
                    needaccept = True
                    Dim sr As IOrmSorting = Nothing
                    Dim c As IComparer = Nothing
                    Dim col As ArrayList = Nothing

                    If _mainList.Count > 0 Then
                        col = New ArrayList(mgr.ConvertIds2Objects(_subType, _mainList, False))
                        If Not mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                            AcceptDual()
                            Return False
                        End If
                        c = sr.CreateSortComparer(_sort)
                        If c Is Nothing Then
                            AcceptDual()
                            Return False
                        End If
                    End If

                    Dim ml As New List(Of Integer)
                    Dim i, j As Integer
                    Do
                        If i = _mainList.Count Then
                            For k As Integer = j To _addedList.Count - 1
                                ml.Add(_addedList(k))
                            Next
                            Exit Do
                        End If
                        If j = _addedList.Count Then
                            For k As Integer = i To _mainList.Count - 1
                                ml.Add(_mainList(k))
                            Next
                            Exit Do
                        End If
                        Dim ex As OrmBase = CType(col(i), OrmBase)
                        Dim ad As OrmBase = mgr.CreateDBObject(_addedList(j), _subType)
                        If c.Compare(ex, ad) < 0 Then
                            ml.Add(ex.Identifier)
                            i += 1
                        Else
                            ml.Add(ad.Identifier)
                            j += 1
                        End If
                    Loop While True

                    _mainList = ml
                End If

                _addedList.Clear()
            End If

            For Each o As Integer In _deletedList
                CType(_mainList, List(Of Integer)).Remove(o)
            Next
            needaccept = _deletedList.Count > 0
            _deletedList.Clear()
            _saved = False
            RemoveNew()

            If needaccept Then
                AcceptDual()
            End If

            Return True
        End Function

        Public Function Accept(ByVal mgr As OrmDBManager, ByVal id As Integer) As Boolean
            If _addedList.Contains(id) Then
                If _sort Is Nothing Then
                    CType(_mainList, List(Of Integer)).Add(id)
                    _addedList.Remove(id)
                Else
                    Dim sr As IOrmSorting = Nothing
                    Dim col As New ArrayList(mgr.ConvertIds2Objects(_subType, _mainList, False))
                    If Not mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                        Return False
                    End If
                    Dim c As IComparer = sr.CreateSortComparer(_sort)
                    If c Is Nothing Then
                        Return False
                    End If
                    Dim ad As OrmBase = mgr.CreateDBObject(id, _subType)
                    Dim pos As Integer = col.BinarySearch(ad, c)
                    If pos < 0 Then
                        _mainList.Insert(Not pos, id)
                    End If
                End If
                _addedList.Remove(id)
            ElseIf _deletedList.Contains(id) Then
                CType(_mainList, List(Of Integer)).Remove(id)
                _deletedList.Remove(id)
            End If

            Return True
        End Function

        Protected Sub AcceptDual()
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            For Each id As Integer In _mainList
                Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
                If m IsNot Nothing Then
                    If m.Entry.Added.Contains(_mainId) OrElse m.Entry.Deleted.Contains(_mainId) Then
                        If Not m.Entry.Accept(CType(mgr, OrmDBManager), _mainId) Then
                            Dim obj As OrmBase = mgr.CreateDBObject(id, SubType)
                            mgr.M2MCancel(obj, MainType)
                        End If
                    End If
                End If
            Next
        End Sub

        Protected Sub RejectRelated(ByVal id As Integer, ByVal add As Boolean)
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First
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
                If _sort IsNot Nothing Then
                    Dim sr As IOrmSorting = Nothing
                    Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                    Dim col As New ArrayList(mgr.ConvertIds2Objects(_subType, _addedList, False))
                    If mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                        Dim c As IComparer = sr.CreateSortComparer(_sort)
                        If c IsNot Nothing Then
                            Dim pos As Integer = col.BinarySearch(mgr.CreateDBObject(id, _subType), c)
                            If pos < 0 Then
                                _addedList.Insert(Not pos, id)
                                Return
                            End If
                        End If
                    End If
                End If
                _addedList.Add(id)
            End If
        End Sub

        Public Sub Add(ByVal id As Integer, ByVal idx As Integer)
            If _deletedList.Contains(id) Then
                _deletedList.Remove(id)
            Else
                _addedList.Insert(idx, id)
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

        Public ReadOnly Property Main() As OrmBase
            Get
                Return OrmManagerBase.CurrentManager.CreateDBObject(_mainId, _mainType)
            End Get
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

        Protected Function GetRealDirect() As Boolean
            If SubType Is MainType Then
                Return Not Direct
            Else
                Return Direct
            End If
        End Function

        Protected Function CheckDual(ByVal mgr As OrmManagerBase, ByVal id As Integer) As Boolean
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First
            Dim c As Boolean = True
            For Each i As Integer In m.Entry.Original
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
                    newl = New EditableList(_mainId, _mainList, _mainType, _subType, Direct, _sort)
                    newl._deletedList = _deletedList
                    newl._addedList = ad
                End If
            End If
            Return newl
        End Function

        'Public Function PrepareNewSave(ByVal mgr As OrmManagerBase) As EditableList
        '    Dim newl As EditableList = Nothing
        '    If Not mgr.IsNewObject(_mainType, _mainId) AndAlso HasNew Then
        '        Dim ad As New List(Of Integer)
        '        For Each id As Integer In _new
        '            If mgr.IsNewObject(SubType, id) Then
        '                Throw New InvalidOperationException("List has new object " & id)
        '            ElseIf CheckDual(mgr, id) Then
        '                ad.Add(id)
        '            End If
        '        Next
        '        If ad.Count > 0 Then
        '            newl = New EditableList(_mainId, New List(Of Integer), _mainType, _subType, Direct, _sort)
        '            newl.AddRange(ad)
        '        End If
        '    End If
        '    Return newl
        'End Function

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
                    '_new.Add(id)
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

        Public Function GetFilter(ByVal filter_info As Object) As IOrmFilter Implements IOrmObjectSchemaBase.GetFilter
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
