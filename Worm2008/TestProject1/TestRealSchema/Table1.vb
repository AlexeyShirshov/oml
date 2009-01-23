Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Sorting
Imports Worm.Query

Public Enum Enum1 As Byte
    first = 1
    sec = 2
End Enum

Public Class [Table1_x]
    Inherits Table1

    Public Overrides Property Name() As String
        Get
            Return MyBase.Name
        End Get
        Set(ByVal value As String)
            MyBase.Name = value
        End Set
    End Property
End Class

<Entity(GetType(Table1Implementation), "1"), Entity(GetType(Table12Implementation), "2"), Entity(GetType(Table13Implementation), "3"), Entity(GetType(Table1Search), "Search")> _
Public Class Table1
    Inherits OrmBaseT(Of Table1)
    Implements IOrmEditable(Of Table1), IOptimizedValues

    Private _name As String
    Private _code As Nullable(Of Integer)
    Private _e As Nullable(Of Enum1)
    Private _e2 As Nullable(Of Enum1)
    Private _dt As DateTime
    Private _cust As Integer

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Return New Comparer(CType(System.Enum.Parse(GetType(Table1Sort), sort), Table1Sort), sort_type)
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Return CType(New Comparer(CType(System.Enum.Parse(GetType(Table1Sort), sort), Table1Sort), sort_type), Global.System.Collections.Generic.IComparer(Of T))
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Sub CopyTable1(ByVal [from] As Table1, ByVal [to] As Table1) Implements IOrmEditable(Of Table1).CopyBody
        With [from]
            [to]._code = ._code
            [to]._dt = ._dt
            [to]._e = ._e
            [to]._e2 = ._e2
            [to]._name = ._name
        End With
    End Sub

    Public ReadOnly Property Table2s() As RelationCmd
        Get
            Return GetCmd(GetType(Table2))
        End Get
    End Property

    Public Overridable Sub SetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Title"
                Name = CStr(value)
            Case "Enum"
                [Enum] = CType(value, Global.System.Nullable(Of Global.TestProject1.Enum1))
            Case "EnumStr"
                EnumStr = CType(value, Global.System.Nullable(Of Global.TestProject1.Enum1))
            Case "Code"
                Code = CType(value, Global.System.Nullable(Of Integer))
            Case "DT"
                CreatedAt = CDate(value)
            Case "Custom"
                _cust = CInt(value)
            Case "ddd"
                Name = CStr(value)
            Case "ID"
                Identifier = value
            Case Else
                Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Sub

    Public Overridable Overloads Function GetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema) As Object Implements IOptimizedValues.GetValueOptimized
        If fieldName = "ddd" Then
            Return Name
        Else
            Return MappingEngine.GetProperty(Me.GetType, oschema, fieldName).GetValue(Me, Nothing)
            'Throw New NotSupportedException(fieldName)
            'Return MyBase.GetValue(pi, fieldName, oschema)
        End If
    End Function

    <EntityProperty(PropertyAlias:="Title")> _
    Public Overridable Property Name() As String
        Get
            Using SyncHelper(True, "Title")
                Return _name
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Title")
                _name = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Enum")> _
    Public Property [Enum]() As Nullable(Of Enum1)
        Get
            Using SyncHelper(True, "Enum")
                Return _e
            End Using
        End Get
        Set(ByVal value As Nullable(Of Enum1))
            Using SyncHelper(False, "Enum")
                _e = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="EnumStr")> _
    Public Property EnumStr() As Nullable(Of Enum1)
        Get
            Using SyncHelper(True, "EnumStr")
                Return _e2
            End Using
        End Get
        Set(ByVal value As Nullable(Of Enum1))
            Using SyncHelper(False, "EnumStr")
                _e2 = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Code")> _
    Public Property Code() As Nullable(Of Integer)
        Get
            Using SyncHelper(True, "Code")
                Return _code
            End Using
        End Get
        Set(ByVal value As Nullable(Of Integer))
            Using SyncHelper(False, "Code")
                _code = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="DT")> _
    Public Property CreatedAt() As Date
        Get
            Using SyncHelper(True, "DT")
                Return _dt
            End Using
        End Get
        Set(ByVal value As Date)
            Using SyncHelper(False, "DT")
                _dt = value
            End Using
        End Set
    End Property

    Public ReadOnly Property Custom() As Integer
        Get
            Return _cust
        End Get
    End Property
End Class

Public Class Table1Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements ISupportAlphabet, ISchemaInit, IOrmSorting, IJoinBehavior

    Private _idx As OrmObjectIndex
    'Private _schema As OrmSchemaBase
    Private _tables() As SourceFragment = {New SourceFragment("dbo.Table1")}
    Private _rels() As M2MRelationDesc

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function ChangeValueType(ByVal c As EntityPropertyAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
        If c.PropertyAlias = "EnumStr" Then
            If TypeOf value Is Enum1 Then
                newvalue = value.ToString
                Return True
            End If
        Else
            Return MyBase.ChangeValueType(c, value, newvalue)
        End If
    End Function

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Title", "name", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Enum", "enum", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("EnumStr", "enum_str", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Code", "code", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("DT", "dt", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    'Public Overrides Function MapSort2FieldName(ByVal sort As String) As String
    '    Select Case CType(System.Enum.Parse(GetType(Table1Sort), sort), Table1Sort)
    '        Case Table1Sort.DateTime
    '            Return "DT"
    '        Case Table1Sort.Enum
    '            Return "Enum"
    '        Case Else
    '            Throw New NotSupportedException("Sorting " & sort & " is not supported")
    '    End Select
    'End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        Dim t As Type = _schema.GetTypeByEntityName("Table3")
        If t Is Nothing Then
            Throw New InvalidOperationException("Cannot get type Table3")
            't = GetType(Table3)
        End If

        If _rels Is Nothing Then
            Dim t1to3 As SourceFragment = TablesImplementation._tables(0)
            _rels = New M2MRelationDesc() { _
                New M2MRelationDesc(t, t1to3, "table3", True, New System.Data.Common.DataTableMapping, GetType(Tables1to3)), _
                New M2MRelationDesc(_objectType, Tables1to1.TablesImplementation._tables(0), "table1", False, New System.Data.Common.DataTableMapping, M2MRelationDesc.RevKey, GetType(Tables1to1)), _
                New M2MRelationDesc(_objectType, Tables1to1.TablesImplementation._tables(0), "table1_back", False, New System.Data.Common.DataTableMapping, M2MRelationDesc.DirKey, GetType(Tables1to1)) _
            }
        End If
        Return _rels
    End Function

    Public Overridable Function GetFirstDicField() As String Implements ISupportAlphabet.GetFirstDicField
        Return "Title"
    End Function

    Public Overridable Function GetSecondDicField() As String Implements ISupportAlphabet.GetSecondDicField
        Return Nothing
    End Function

    'Public Sub GetSchema(ByVal schema As Worm.Orm.OrmSchemaBase, ByVal type As Type) Implements Worm.Orm.ISchemaInit.GetSchema
    '    _schema = schema
    '    _objectType = type
    'End Sub

    Public Function CreateSortComparer(ByVal s As Sort) As System.Collections.IComparer Implements IOrmSorting.CreateSortComparer
        If s.SortBy = "DT" Then
            Return New Comparer(Table1Sort.DateTime, s.Order)
        ElseIf s.SortBy = "Enum" Then
            Return New Comparer(Table1Sort.Enum, s.Order)
        End If
        Return Nothing
    End Function

    Public Function CreateSortComparer1(Of T As {_IEntity})(ByVal s As Sort) As System.Collections.Generic.IComparer(Of T) Implements IOrmSorting.CreateSortComparer
        If s.SortBy = "DT" Then
            Return CType(New Comparer(Table1Sort.DateTime, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        ElseIf s.SortBy = "Enum" Then
            Return CType(New Comparer(Table1Sort.Enum, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        End If
        Return Nothing
    End Function

    'Public Function ExternalSort(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Sort, ByVal objs As Worm.ReadOnlyList(Of T)) As Worm.ReadOnlyList(Of T) Implements IOrmSorting.ExternalSort
    '    Throw New NotSupportedException
    'End Function

    Public Enum Table1Sort
        DateTime
        [Enum]
    End Enum

    Public Class Comparer
        Implements System.Collections.IComparer, System.Collections.Generic.IComparer(Of Table1)

        Private _s As Table1Sort
        Private _st As Integer = -1

        Public Sub New(ByVal s As Table1Sort, ByVal st As SortType)
            _s = s
            If st = SortType.Asc Then
                _st = 1
            End If
        End Sub

        Protected Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Return Compare(TryCast(x, Table1), TryCast(y, Table1))
        End Function

        Public Function Compare(ByVal x As Table1, ByVal y As Table1) As Integer Implements System.Collections.Generic.IComparer(Of Table1).Compare
            If x Is Nothing Then
                If y Is Nothing Then
                    Return 0 * _st
                Else
                    Return -1 * _st
                End If
            Else
                If y Is Nothing Then
                    Return 1 * _st
                Else
                    Select Case _s
                        Case Table1Sort.DateTime
                            Return x.CreatedAt.CompareTo(y.CreatedAt) * _st
                        Case Table1Sort.Enum
                            If Not x.[Enum].HasValue Then
                                If y.[Enum].HasValue Then
                                    Return -1 * _st
                                Else
                                    Return 0 * _st
                                End If
                            Else
                                If y.[Enum].HasValue Then
                                    Return x.[Enum].Value.CompareTo(y.Enum.Value) * _st
                                Else
                                    Return 1 * _st
                                End If
                            End If
                    End Select
                End If
            End If
        End Function
    End Class

    Public ReadOnly Property AlwaysJoinMainTable() As Boolean Implements IJoinBehavior.AlwaysJoinMainTable
        Get
            Return False
        End Get
    End Property

    Public Overridable Function GetJoinField(ByVal t As System.Type) As String Implements IJoinBehavior.GetJoinField
        If t Is GetType(Table2) Then
            'Return "ID"
            'Return "Table1"
            'Throw New NotImplementedException
        End If
        Return Nothing
    End Function
End Class

Public Class Table1Search
    Inherits Table1Implementation
    Implements IFullTextSupport

    Public ReadOnly Property ApplayAsterisk() As Boolean Implements IFullTextSupport.ApplayAsterisk
        Get
            Return True
        End Get
    End Property

    Public Function GetIndexedFields() As String() Implements IFullTextSupport.GetIndexedFields
        Return New String() {"EnumStr", "Title"}
    End Function

    Public Function GetQueryFields(ByVal contextKey As Object) As String() Implements IFullTextSupport.GetQueryFields
        If CStr(contextKey) = "sf" Then
            Return New String() {"Title"}
        End If
        Return Nothing
    End Function

    Public Overrides Function GetJoinField(ByVal t As System.Type) As String
        Return String.Empty
    End Function
End Class

Public Class Table12Implementation
    Inherits Table1Implementation

    Private _tables() As SourceFragment = {New table1func("table1func")}

    'Implements IOrmTableFunction

    'Public Function GetFunction(ByVal table As SourceFragment, ByVal pmgr As Worm.Orm.ParamMgr) As SourceFragment Implements Worm.Orm.IOrmTableFunction.GetFunction
    '    If table.Equals(GetTables()(Tables.Main)) Then
    '        Return New SourceFragment("dbo.table1func()")
    '    End If
    '    Return Nothing
    'End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    Public Overrides Function GetSecondDicField() As String
        Return "EnumStr"
    End Function
End Class

Public Class Table13Implementation
    Inherits Table1Implementation

    Private _tables() As SourceFragment = {New table2func("table2func")}

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    'Implements IOrmTableFunction

    'Public Function GetFunction(ByVal table As SourceFragment, ByVal pmgr As Worm.Orm.ParamMgr) As SourceFragment Implements Worm.Orm.IOrmTableFunction.GetFunction
    '    If table.Equals(GetTables()(Tables.Main)) Then
    '        Return New SourceFragment("dbo.table2func(" & pmgr.CreateParam("sec") & ")")
    '    End If
    '    Return Nothing
    'End Function
End Class

Public Class table1func
    Inherits SourceFragment

    Public Sub New(ByVal tableName As String)
        MyBase.New(tableName)
    End Sub

    Public Overrides Function OnTableAdd(ByVal pmgr As ICreateParam) As SourceFragment
        Return New SourceFragment("dbo.table1func()")
    End Function

End Class

Public Class table2func
    Inherits SourceFragment

    Public Sub New(ByVal tableName As String)
        MyBase.New(tableName)
    End Sub

    Public Overrides Function OnTableAdd(ByVal pmgr As ICreateParam) As SourceFragment
        Return New SourceFragment("dbo.table2func(" & pmgr.CreateParam("sec") & ")")
    End Function
End Class