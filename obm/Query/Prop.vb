Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Orm
    Public Class FCtor
        Public Shared Function Field(ByVal t As Type, ByVal typeField As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(t, typeField))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim f As New FCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            f.GetAllProperties.Add(p)
            Return f
        End Function

        Private _l As List(Of SelectExpression)

        Public Function Add(ByVal t As Type, ByVal typeField As String) As FCtor
            GetAllProperties.Add(New SelectExpression(t, typeField))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            GetAllProperties.Add(p)
            Return Me
        End Function

        Public Function GetAllProperties() As List(Of SelectExpression)
            If _l Is Nothing Then
                _l = New List(Of SelectExpression)
            End If
            Return _l
        End Function

        Public Shared Widening Operator CType(ByVal f As FCtor) As SelectExpression()
            Return f.GetAllProperties.ToArray
        End Operator

        Public Shared Widening Operator CType(ByVal f As FCtor) As Grouping()
            Return f.GetAllProperties.ConvertAll(Function(p As SelectExpression) New Grouping(p)).ToArray
        End Operator
    End Class

    Public Enum PropType
        ObjectField
        TableColumn
        CustomValue
        Subquery
        [Aggregate]
    End Enum

    Public Class SelectExpression
        Implements Cache.IQueryDependentTypes, Criteria.Values.IQueryElement

        Private _field As String
        Private _type As Type
        Private _table As SourceFragment
        Private _column As String
        Private _custom As String
        Private _values() As Pair(Of Object, String)
        Private _attr As Field2DbRelations
        Private _q As Worm.Query.QueryCmd
        Private _agr As AggregateBase

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

        Public Sub New(ByVal agr As AggregateBase)
            _agr = agr
        End Sub

        Protected Sub RaiseOnChange()
            RaiseEvent OnChange()
        End Sub

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal selectList As IEnumerable(Of T)) As Collections.IndexedCollection(Of String, MapField2Column)
            Dim c As New OrmObjectIndex
            Return GetMapping(c, selectList)
        End Function

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal c As OrmObjectIndex, ByVal selectList As IEnumerable(Of T)) As Collections.IndexedCollection(Of String, MapField2Column)
            For Each s As T In selectList
                c.Add(New MapField2Column(s.Field, s.Column, s.Table, s.Attributes))
            Next
            Return c
        End Function

        Public ReadOnly Property PropType() As PropType
            Get
                If _type IsNot Nothing AndAlso Not String.IsNullOrEmpty(_field) Then
                    Return Orm.PropType.ObjectField
                ElseIf _table IsNot Nothing AndAlso Not String.IsNullOrEmpty(_column) Then
                    Return Orm.PropType.TableColumn
                Else
                    If Not String.IsNullOrEmpty(_custom) Then
                        Return Orm.PropType.CustomValue
                    ElseIf Not String.IsNullOrEmpty(_column) Then
                        Return Orm.PropType.TableColumn
                    ElseIf _q IsNot Nothing Then
                        Return Orm.PropType.Subquery
                    Else
                        Return Orm.PropType.Aggregate
                    End If
                End If
            End Get
        End Property

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

        Public Property [Aggregate]() As AggregateBase
            Get
                Return _agr
            End Get
            Set(ByVal value As AggregateBase)
                _agr = value
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

        Public Overridable Function _ToString() As String Implements Criteria.Values.IQueryElement._ToString
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
                    ElseIf _q IsNot Nothing Then
                        Return _q._ToString
                    ElseIf _agr IsNot Nothing Then
                        Return _agr.ToString
                    Else
                        Return _field
                    End If
                End If
            End If
        End Function

        Public Function GetCustomExpressionValues(ByVal schema As ObjectMappingEngine, ByVal aliases As IDictionary(Of SourceFragment, String)) As String()
            Return ObjectMappingEngine.ExtractValues(schema, aliases, _values).ToArray
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return _Equals(TryCast(obj, SelectExpression))
        End Function

        Protected Overridable Function _Equals(ByVal s As SelectExpression) As Boolean
            If s Is Nothing Then
                Return False
            Else
                Dim b As Boolean
                If Not String.IsNullOrEmpty(_custom) Then
                    b = _custom = s._custom
                ElseIf Not String.IsNullOrEmpty(_field) Then
                    b = _field = s._field AndAlso _type Is s._type
                ElseIf Not String.IsNullOrEmpty(_column) Then
                    b = _column = s._column AndAlso _table Is s._table
                ElseIf _q IsNot Nothing Then
                    b = _q.Equals(s._q)
                Else
                    b = _agr.Equals(_agr)
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
            Return New Cache.EmptyDependentTypes
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Criteria.Values.IQueryElement.GetStaticString
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
                    ElseIf _q IsNot Nothing Then
                        Return _q.ToStaticString(mpe)
                    ElseIf _agr IsNot Nothing Then
                        Return _agr.ToString
                    Else
                        Return _field
                    End If
                End If
            End If
        End Function
    End Class

End Namespace