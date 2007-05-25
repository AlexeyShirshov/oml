Imports Worm
Imports Worm.Orm

Public Enum Enum1 As Byte
    first = 1
    sec = 2
End Enum

<Entity(GetType(Table1Implementation), "1"), Entity(GetType(Table12Implementation), "2"), Entity(GetType(Table13Implementation), "3")> _
Public Class Table1
    Inherits OrmBase

    Private _name As String
    Private _code As Nullable(Of Integer)
    Private _e As Nullable(Of Enum1)
    Private _e2 As Nullable(Of Enum1)
    Private _dt As DateTime

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
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

    Protected Sub CopyTable1(ByVal [from] As Table1, ByVal [to] As Table1)
        With [from]
            [to]._code = ._code
            [to]._dt = ._dt
            [to]._e = ._e
            [to]._e2 = ._e2
            [to]._name = ._name
        End With
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyTable1(CType(from, Table1), CType([to], Table1))
    End Sub

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New Table1(Identifier, OrmCache, OrmSchema)
    End Function

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
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
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("Title")> _
    Public Property Name() As String
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

    <Column("Enum")> _
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

    <Column("EnumStr")> _
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

    <Column("Code")> _
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

    <Column("DT")> _
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
End Class

Public Class Table1Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements IOrmDictionary, IOrmSchemaInit, IOrmSorting

    Private _idx As Orm.OrmObjectIndex
    'Private _schema As OrmSchemaBase
    Private _tables() As OrmTable = {New OrmTable("dbo.Table1")}
    Private _rels() As M2MRelation

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function ChangeValueType(ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
        If c.FieldName = "EnumStr" Then
            If TypeOf value Is Enum1 Then
                newvalue = value.ToString
                Return True
            End If
        Else
            Return MyBase.ChangeValueType(c, value, newvalue)
        End If
    End Function

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Title", "name", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Enum", "enum", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("EnumStr", "enum_str", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Code", "code", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("DT", "dt", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
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

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        Dim t As Type = _schema.GetTypeByEntityName("Table3")
        If t Is Nothing Then
            Throw New InvalidOperationException("Cannot get type Table3")
            't = GetType(Table3)
        End If

        If _rels Is Nothing Then
            _rels = New M2MRelation() { _
                New Orm.M2MRelation(t, TablesImplementation._tables(0), "table3", True, New System.Data.Common.DataTableMapping, GetType(Tables1to3)) _
            }
        End If
        Return _rels
    End Function

    Public Overridable Function GetFirstDicField() As String Implements Worm.Orm.IOrmDictionary.GetFirstDicField
        Return "Title"
    End Function

    Public Overridable Function GetSecondDicField() As String Implements Worm.Orm.IOrmDictionary.GetSecondDicField
        Return Nothing
    End Function

    'Public Sub GetSchema(ByVal schema As Worm.Orm.OrmSchemaBase, ByVal type As Type) Implements Worm.Orm.IOrmSchemaInit.GetSchema
    '    _schema = schema
    '    _objectType = type
    'End Sub

    Public Function CreateSortComparer(ByVal s As Worm.Orm.Sort) As System.Collections.IComparer Implements Worm.Orm.IOrmSorting.CreateSortComparer
        If s.FieldName = "DT" Then
            Return New Comparer(Table1Sort.DateTime, s.Order)
        ElseIf s.FieldName = "Enum" Then
            Return New Comparer(Table1Sort.Enum, s.Order)
        End If
        Return Nothing
    End Function

    Public Function CreateSortComparer1(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Worm.Orm.Sort) As System.Collections.Generic.IComparer(Of T) Implements Worm.Orm.IOrmSorting.CreateSortComparer
        If s.FieldName = "DT" Then
            Return CType(New Comparer(Table1Sort.DateTime, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        ElseIf s.FieldName = "Enum" Then
            Return CType(New Comparer(Table1Sort.Enum, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        End If
        Return Nothing
    End Function

    Public Function ExternalSort(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Worm.Orm.Sort, ByVal objs As System.Collections.Generic.ICollection(Of T)) As System.Collections.Generic.ICollection(Of T) Implements Worm.Orm.IOrmSorting.ExternalSort
        Throw New NotSupportedException
    End Function

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

End Class

Public Class Table12Implementation
    Inherits Table1Implementation
    Implements IOrmTableFunction

    Public Function GetFunction(ByVal table As OrmTable, ByVal pmgr As Worm.Orm.ParamMgr) As OrmTable Implements Worm.Orm.IOrmTableFunction.GetFunction
        If table.Equals(GetTables()(Tables.Main)) Then
            Return New OrmTable("dbo.table1func()")
        End If
        Return Nothing
    End Function

    Public Overrides Function GetSecondDicField() As String
        Return "EnumStr"
    End Function
End Class

Public Class Table13Implementation
    Inherits Table1Implementation
    Implements IOrmTableFunction

    Public Function GetFunction(ByVal table As OrmTable, ByVal pmgr As Worm.Orm.ParamMgr) As OrmTable Implements Worm.Orm.IOrmTableFunction.GetFunction
        If table.Equals(GetTables()(Tables.Main)) Then
            Return New OrmTable("dbo.table2func(" & pmgr.CreateParam("sec") & ")")
        End If
        Return Nothing
    End Function
End Class