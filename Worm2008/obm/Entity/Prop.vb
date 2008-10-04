Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Orm
    Public Class FCtor
        Public Shared Function Field(ByVal t As Type, ByVal typeField As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(t, typeField))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(table, tableColumn))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(table, tableColumn, [alias]))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim f As New FCtor
            Dim p As New OrmProperty(table, tableColumn, [alias])
            p.Attributes = attr
            f.GetAllProperties.Add(p)
            Return f
        End Function

        Private _l As List(Of OrmProperty)

        Public Function Add(ByVal t As Type, ByVal typeField As String) As FCtor
            GetAllProperties.Add(New OrmProperty(t, typeField))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            GetAllProperties.Add(New OrmProperty(table, tableColumn))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            GetAllProperties.Add(New OrmProperty(table, tableColumn, [alias]))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim p As New OrmProperty(table, tableColumn, [alias])
            p.Attributes = attr
            GetAllProperties.Add(p)
            Return Me
        End Function

        Public Function GetAllProperties() As List(Of OrmProperty)
            If _l Is Nothing Then
                _l = New List(Of OrmProperty)
            End If
            Return _l
        End Function

        Public Shared Widening Operator CType(ByVal f As FCtor) As OrmProperty()
            Return f.GetAllProperties.ToArray
        End Operator

        Public Shared Widening Operator CType(ByVal f As FCtor) As Grouping()
            Return f.GetAllProperties.ConvertAll(Function(p As OrmProperty) New Grouping(p)).ToArray
        End Operator
    End Class

    Public Class OrmProperty
        Implements Cache.IQueryDependentTypes

        Private _field As String
        Private _type As Type
        Private _table As SourceFragment
        Private _column As String
        Private _custom As String
        Private _values() As Pair(Of Object, String)
        Private _attr As Field2DbRelations
        Private _q As Worm.Query.QueryCmd

        Public Event OnChange()

        Protected Sub New()
        End Sub

        Protected Sub New(ByVal field As String)
            _field = field
        End Sub

        Public Sub New(ByVal t As Type, ByVal field As String)
            _field = field
            _type = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            _column = column
            _table = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal field As String)
            _column = column
            _table = t
            _field = field
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As Pair(Of Object, String), ByVal [alias] As String)
            _custom = computed
            _values = values
            _column = [alias]
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As Pair(Of Object, String))
            MyClass.New(computed, values, Nothing)
        End Sub

        Public Sub New(ByVal q As QueryCmd)
            _q = q
        End Sub

        Protected Sub RaiseOnChange()
            RaiseEvent OnChange()
        End Sub

        Public Property Attributes() As Field2DbRelations
            Get
                Return _attr
            End Get
            Set(ByVal value As Field2DbRelations)
                _attr = value
            End Set
        End Property

        Public ReadOnly Property Query() As QueryCmd
            Get
                Return _q
            End Get
        End Property

        Public Property Column() As String
            Get
                Return _column
            End Get
            Protected Friend Set(ByVal value As String)
                _column = value
                RaiseOnChange()
            End Set
        End Property

        Public Property Field() As String
            Get
                Return _field
            End Get
            Protected Friend Set(ByVal value As String)
                _field = value
                RaiseOnChange()
            End Set
        End Property

        Public Property Type() As Type
            Get
                Return _type
            End Get
            Protected Friend Set(ByVal value As Type)
                _type = value
                RaiseOnChange()
            End Set
        End Property

        Public Property Table() As SourceFragment
            Get
                Return _table
            End Get
            Protected Set(ByVal value As SourceFragment)
                _table = value
            End Set
        End Property

        Public Property Computed() As String
            Get
                Return _custom
            End Get
            Protected Set(ByVal value As String)
                _custom = value
                RaiseOnChange()
            End Set
        End Property

        Public ReadOnly Property IsCustom() As Boolean
            Get
                Return Not String.IsNullOrEmpty(_custom)
            End Get
        End Property

        Public Property Values() As Pair(Of Object, String)()
            Get
                Return _values
            End Get
            Protected Set(ByVal value As Pair(Of Object, String)())
                _values = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            If _type IsNot Nothing Then
                Return _type.ToString & "$" & _field
            Else
                If _table IsNot Nothing Then
                    Return _table.RawName & "$" & _column
                Else
                    If Not String.IsNullOrEmpty(_custom) Then
                        Return _custom
                    ElseIf Not String.IsNullOrEmpty(_column) Then
                        Return _column
                    Else
                        Debug.Assert(_q IsNot Nothing)
                        Return _q.ToStaticString
                    End If
                End If
            End If
        End Function

        Public Function GetCustomExpressionValues(ByVal schema As ObjectMappingEngine, ByVal aliases As IDictionary(Of SourceFragment, String)) As String()
            Return ObjectMappingEngine.ExtractValues(schema, aliases, _values).ToArray
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return _Equals(TryCast(obj, OrmProperty))
        End Function

        Protected Overridable Function _Equals(ByVal s As OrmProperty) As Boolean
            If s Is Nothing Then
                Return False
            Else
                Dim b As Boolean
                If Not String.IsNullOrEmpty(_custom) Then
                    b = _custom = s._custom AndAlso _type Is s._type
                ElseIf Not String.IsNullOrEmpty(_field) Then
                    b = _field = s._field AndAlso _type Is s._type
                Else
                    b = _column = s._column AndAlso _type Is s._type
                End If
                Return b
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overridable Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            If _q IsNot Nothing Then
                Return _q.Get(mpe)
            End If
            Return Nothing
        End Function
    End Class

End Namespace