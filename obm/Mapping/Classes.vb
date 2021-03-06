﻿Imports Worm.Query
Imports System.Linq
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Namespace Entities.Meta
    Public Interface IPKDesc
        Inherits IEnumerable(Of ColumnValue)
        Property PKName As String
    End Interface
    <Serializable()>
    Public Class PKDesc
        Inherits List(Of ColumnValue)
        Implements IPKDesc
        Public Sub New()

        End Sub
        Public Sub New(cv As ColumnValue)
            Add(cv)
        End Sub
        Public Sub New(cv As IEnumerable(Of ColumnValue))
            MyBase.New(cv)
        End Sub
        Property PKName As String Implements IPKDesc.PKName
    End Class
    ''' <summary>
    ''' Поле и его значение 
    ''' </summary>
    <Serializable()>
    Public Class ColumnValue
        '''' <summary>
        '''' Имя поля
        '''' </summary>
        'Public ReadOnly PropertyAlias As String
        Public ReadOnly Column As String
        ''' <summary>
        ''' Значение поля
        ''' </summary>
        Public ReadOnly Value As Object
        '''' <summary>
        '''' Инициализирует объект
        '''' </summary>
        '''' <param name="propAlias">Имя поля</param>
        '''' <param name="value">Значение поля</param>
        'Public Sub New(ByVal propAlias As String, ByVal value As Object)
        '    Me.PropertyAlias = propAlias
        '    Me.Value = value
        'End Sub
        ''' <summary>
        ''' Инициализирует объект
        ''' </summary>
        ''' <param name="column">Имя поля</param>
        ''' <param name="value">Значение поля</param>
        Public Sub New(ByVal column As String, ByVal value As Object)
            'If String.IsNullOrEmpty(column) Then
            '    Throw New ArgumentNullException(NameOf(column))
            'End If

            If value Is Nothing Then
                Throw New ArgumentNullException(NameOf(value))
            End If

            Me.Column = column
            Me.Value = value
        End Sub
    End Class

    <Serializable()>
    Public Class SourceFragment
        Implements ICloneable

        Private ReadOnly _table As String
        Private ReadOnly _schema As String
        Private ReadOnly _uqName As String = Guid.NewGuid.GetHashCode.ToString
        '#If DEBUG Then
        '        Private _stack As String = Environment.StackTrace
        '#End If

        Public Sub New()

        End Sub

        Public Sub New(ByVal tableName As String)
            _table = tableName
        End Sub

        Public Sub New(ByVal prefix As String, ByVal tableName As String)
            _table = tableName
            _schema = prefix
        End Sub

        Public Property Hint As String

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

        Public ReadOnly Property UniqueName(ByVal os As EntityUnion) As String
            Get
                If os Is Nothing OrElse os.EntityType IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                    Return "%-$" & RawName & "^" & _uqName
                Else
                    Return os.ObjectAlias.UniqueName & "$" & RawName & "^" & _uqName
                End If
            End Get
        End Property

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

        Protected Overridable Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As SourceFragment
            Dim t As New SourceFragment(_schema, _table)
            Return t
        End Function
    End Class

    Public Enum SearchType
        Contains
        Freetext
    End Enum

    Public Class SearchFragment
        Inherits SourceFragment

        Private ReadOnly _eu As EntityUnion
        Private ReadOnly _searchString As String
        Private ReadOnly _st As SearchType
        Private ReadOnly _queryFields() As String
        Private ReadOnly _top As Integer = Integer.MinValue
        Private _f As IFtsStringFormatter
        Private _searchSection As String
        'Private _context As Object

#Region " Ctors "

        Public Sub New()

        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String)
            _eu = eu
            _searchString = searchString
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String, ByVal top As Integer)
            _top = top
            _searchString = searchString
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType, ByVal top As Integer)
            _top = top
            _searchString = searchString
            _st = searchType
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType,
                       ByVal top As Integer, ByVal queryFields() As String)
            _top = top
            _searchString = searchString
            _st = searchType
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType,
                       ByVal queryFields() As String)
            _searchString = searchString
            _st = searchType
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, ByVal top As Integer)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String, ByVal top As Integer)
            _eu = eu
            _searchString = searchString
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String,
                       ByVal search As SearchType)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _st = search
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String,
                       ByVal search As SearchType)
            _eu = eu
            _searchString = searchString
            _st = search
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String,
                       ByVal search As SearchType, ByVal top As Integer)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _st = search
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String,
                       ByVal search As SearchType, ByVal top As Integer)
            _eu = eu
            _searchString = searchString
            _st = search
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String,
                       ByVal queryFields() As String)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String,
                       ByVal queryFields() As String)
            _eu = eu
            _searchString = searchString
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String,
                       ByVal search As SearchType, ByVal queryFields() As String)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _st = search
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String,
                       ByVal search As SearchType, ByVal queryFields() As String)
            _eu = eu
            _searchString = searchString
            _st = search
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String,
                       ByVal search As SearchType, ByVal queryFields() As String,
                       ByVal top As Integer)
            _eu = New EntityUnion(searchType)
            _searchString = searchString
            _st = search
            _queryFields = queryFields
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal searchString As String,
                       ByVal search As SearchType, ByVal queryFields() As String,
                       ByVal top As Integer)
            _eu = eu
            _searchString = searchString
            _st = search
            _queryFields = queryFields
            _top = top
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String, ByVal queryFields() As String)
            _searchString = searchString
            _queryFields = queryFields
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String,
                       ByVal queryField As String)
            _searchString = searchString
            _queryFields = New String() {queryField}
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub

        Public Sub New(ByVal searchString As String)
            _searchString = searchString
            _f = New FtsDefaultFormatter(searchString, Nothing)
        End Sub
#End Region

        'Public Property Context() As Object
        '    Get
        '        Return _context
        '    End Get
        '    Set(ByVal value As Object)
        '        _context = value
        '    End Set
        'End Property

        Public Property SearchSection() As String
            Get
                Return _searchSection
            End Get
            Set(ByVal value As String)
                _searchSection = value
            End Set
        End Property

        Public Property Formatter() As IFtsStringFormatter
            Get
                Return _f
            End Get
            Set(ByVal value As IFtsStringFormatter)
                _f = value
            End Set
        End Property

        Public ReadOnly Property Entity() As EntityUnion
            Get
                Return _eu
            End Get
        End Property

        Public ReadOnly Property SearchText() As String
            Get
                Return _searchString
            End Get
        End Property

        Public ReadOnly Property SearchType() As SearchType
            Get
                Return _st
            End Get
        End Property

        Public ReadOnly Property QueryFields() As String()
            Get
                Return _queryFields
            End Get
        End Property

        Public ReadOnly Property Top() As Integer
            Get
                Return _top
            End Get
        End Property

        Public Function GetSearchTableName() As String
            If _st = Meta.SearchType.Contains Then
                Return "containstable"
            ElseIf _st = Meta.SearchType.Freetext Then
                Return "freetexttable"
            Else
                Throw New NotSupportedException(_st.ToString)
            End If
        End Function

        Public Function GetFtsString(ByVal ifts As IFullTextSupport, ByVal searcht As Type) As String
            Return Formatter.GetFtsString(SearchSection, ifts, searcht, GetSearchTableName)
        End Function
    End Class

    Public Class AliasMgr
        Implements IPrepareTable

        Private ReadOnly _defaultAliases As Generic.IDictionary(Of SourceFragment, String)
        Private ReadOnly _objectAlises As Generic.IDictionary(Of QueryAlias, Generic.IDictionary(Of SourceFragment, String))
        Private _cnt As Integer

        Private Sub New(ByVal aliases As Generic.IDictionary(Of SourceFragment, String))
            _defaultAliases = aliases
            _objectAlises = New Generic.Dictionary(Of QueryAlias, Generic.IDictionary(Of SourceFragment, String))
        End Sub

        Public Shared Function Create() As AliasMgr
            Return New AliasMgr(New Generic.Dictionary(Of SourceFragment, String))
        End Function

        Public Function AddTable(ByRef table As SourceFragment, ByVal os As EntityUnion) As String Implements IPrepareTable.AddTable
            Return AddTable(table, os, CType(Nothing, ICreateParam))
        End Function

        Public Function AddTable(ByRef table As SourceFragment, ByVal os As EntityUnion, ByVal pmgr As ICreateParam) As String Implements IPrepareTable.AddTable
            'Dim tf As IOrmTableFunction = TryCast(schema, IOrmTableFunction)
            Dim t As SourceFragment = table
            Dim tt As SourceFragment = table.OnTableAdd(pmgr)
            If tt IsNot Nothing Then
                '    Dim f As SourceFragment = tf.GetFunction(table, pmgr)
                '    If f IsNot Nothing Then
                table = tt
                '    End If
            End If
            Dim i As Integer = _cnt + 1
            Dim [alias] As String = "t" & i
            If os Is Nothing OrElse os.EntityType IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                _defaultAliases.Add(t, [alias])
            Else
                Dim dic As Generic.IDictionary(Of SourceFragment, String) = Nothing
                If Not _objectAlises.TryGetValue(os.ObjectAlias, dic) Then
                    dic = New Generic.Dictionary(Of SourceFragment, String)
                    _objectAlises.Add(os.ObjectAlias, dic)
                End If
                dic.Add(t, [alias])
            End If
            _cnt += 1
            Return [alias]
        End Function

        Friend Sub AddTable(ByVal tbl As SourceFragment, ByVal [alias] As String)
            _defaultAliases.Add(tbl, [alias])
        End Sub

        'Public Function GetAlias(ByVal table As String) As String
        '    Return _aliases(table)
        'End Function

        'Public ReadOnly Property Aliases() As IDictionary(Of SourceFragment, String) Implements IPrepareTable.Aliases
        '    Get
        '        Return _aliases
        '    End Get
        'End Property

        'Public ReadOnly Property IsEmpty() As Boolean
        '    Get
        '        Return _defaultAliases Is Nothing
        '    End Get
        'End Property

        'Public Sub Replace(ByVal schema As ObjectMappingEngine, ByVal gen As StmtGenerator, ByVal table As Entities.Meta.SourceFragment, ByVal os As EntityUnion, ByVal sb As System.Text.StringBuilder) Implements IPrepareTable.Replace
        '    If os Is Nothing OrElse os.EntityType IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
        '        sb.Replace(table.UniqueName(Nothing) & schema.Delimiter, _defaultAliases(table) & gen.Selector)
        '    Else
        '        sb.Replace(table.UniqueName(os) & schema.Delimiter, _objectAlises(os.ObjectAlias)(table) & gen.Selector)
        '    End If
        'End Sub

        Public Function GetAlias(ByVal table As Entities.Meta.SourceFragment, ByVal os As EntityUnion) As String Implements IPrepareTable.GetAlias
            If os Is Nothing OrElse os.IsDefault Then
                Return _defaultAliases(table)
            Else
                Return _objectAlises(os.ObjectAlias)(table)
            End If
        End Function

        Public Function ContainsKey(ByVal table As Entities.Meta.SourceFragment, ByVal os As EntityUnion) As Boolean Implements IPrepareTable.ContainsKey
            If os Is Nothing OrElse os.IsDefault Then
                Return _defaultAliases.ContainsKey(table)
            Else
                Dim dic As Generic.IDictionary(Of SourceFragment, String) = Nothing
                If Not _objectAlises.TryGetValue(os.ObjectAlias, dic) Then
                    Return False
                End If
                Return dic.ContainsKey(table)
            End If
        End Function
        Friend Function Dump() As String
            Dim sb As New StringBuilder

            If _defaultAliases IsNot Nothing Then
                sb.AppendLine("Default aliases")
                For Each kv In _defaultAliases
                    sb.AppendFormat("{0}: {1}", kv.Key.UniqueName(Nothing), kv.Value).AppendLine()
                Next
            End If
            If _objectAlises IsNot Nothing Then
                sb.AppendLine("Object aliases")
                For Each kv In _objectAlises
                    sb.AppendFormat("{0}: [{1}]", kv.Key._ToString, String.Join(",", kv.Value.Select(Function(it) String.Format("{0}: {1}", it.Key.UniqueName(kv.Key), it.Value)))).AppendLine()
                Next
            End If

            Return sb.ToString
        End Function
    End Class

    Public Class FtsDefaultFormatter
        Implements IFtsStringFormatter

        Public Delegate Function ValueForSearchDelegate(ByVal tokens() As String, ByVal sectionName As String, ByVal fs As IFullTextSupport) As String

        Private Class FProxy

            Private ReadOnly _t As Type

            Public Sub New(ByVal t As Type)
                _t = t
            End Sub

            Public Function GetValue(ByVal tokens() As String, ByVal sectionName As String,
                ByVal f As IFullTextSupport) As String
                Return Configuration.SearchSection.GetValueForFreeText(_t, tokens, sectionName)
            End Function
        End Class

        Private ReadOnly _toks() As String
        Private ReadOnly _del As ValueForSearchDelegate
        'Private _sectionName As String

        Public Sub New(ByVal s As String, ByVal sectionName As String)
            _toks = OrmManager.Split4FullTextSearch(s, sectionName)
            '  _sectionName = sectionName
        End Sub

        Public Sub New(ByVal s As String, ByVal sectionName As String, ByVal del As ValueForSearchDelegate)
            MyClass.New(s, sectionName)
            _del = del
        End Sub

        Public Function GetFtsString(ByVal section As String,
            ByVal f As IFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String Implements IFtsStringFormatter.GetFtsString
            If _del Is Nothing Then
                If ftsString = "freetexttable" Then
                    Return New FProxy(type2search).GetValue(_toks, section, f)
                ElseIf ftsString = "containstable" Then
                    Return Configuration.SearchSection.GetValueForContains(_toks, section, f)
                Else
                    Throw New NotSupportedException
                End If
            Else
                Return _del(_toks, section, f)
            End If
        End Function

        Public Function GetTokens() As String() Implements IFtsStringFormatter.GetTokens
            Return _toks
        End Function
    End Class

    Public Module PKDescModule
        <Extension>
        Public Function CalcHashCode(pk As IPKDesc) As Integer
            If pk.Count = 1 Then
                Return pk(0).Value.GetHashCode
            Else
                Dim k As Integer
                For Each p In pk
                    k = k Xor p.Value.GetHashCode
                Next
                Return k
            End If
        End Function
    End Module
End Namespace