Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Entities

    Public Enum PropType
        ObjectProperty
        TableColumn
        CustomValue
        Subquery
        [Aggregate]
    End Enum

    Public Class FieldReference
        Private _op As ObjectProperty
        Private _tf As Pair(Of SourceFragment, String)

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            _op = New ObjectProperty(t, propertyAlias)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            _op = New ObjectProperty(entityName, propertyAlias)
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String)
            _op = New ObjectProperty([alias], propertyAlias)
        End Sub

        Public Sub New(ByVal prop As ObjectProperty)
            _op = prop
        End Sub

        Public Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String)
            _op = New ObjectProperty(os, propertyAlias)
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String)
            _tf = New Pair(Of SourceFragment, String)(table, column)
        End Sub

        Public ReadOnly Property [Property]() As ObjectProperty
            Get
                Return _op
            End Get
        End Property

        Public ReadOnly Property Column() As Pair(Of SourceFragment, String)
            Get
                Return _tf
            End Get
        End Property

        Public Overrides Function ToString() As String
            If _tf IsNot Nothing Then
                Return _tf.First.RawName & "$" & _tf.Second
            Else
                Return _op.ObjectSource.ToStaticString & "$" & _op.Field
            End If
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, FieldReference))
        End Function

        Public Overloads Function Equals(ByVal obj As FieldReference) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            Return Object.Equals(_tf, obj._tf) OrElse Object.Equals(_op, obj._op)
        End Function
    End Class

    Public Class SelectExpression
        Implements Cache.IQueryDependentTypes, Criteria.Values.IQueryElement

        'Private _field As String
        'Private _osrc As ObjectSource
        Private _op As ObjectProperty
        Private _table As SourceFragment
        Private _column As String
        'Private _custom As String
        Private _values() As FieldReference
        Private _attr As Field2DbRelations
        Private _q As Worm.Query.QueryCmd
        Private _agr As AggregateBase
        Private _falias As String
        Private _dst As ObjectSource

        Friend _c As ColumnAttribute
        Friend _pi As Reflection.PropertyInfo
        Friend _realAtt As Field2DbRelations

        Public Event OnChange()

        Protected Sub New()
        End Sub

        'Protected Sub New(ByVal propertyAlias As String)
        '    _field = propertyAlias
        'End Sub

        Protected Friend Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String)
            _op = New ObjectProperty(os, propertyAlias)
        End Sub

#Region " Public ctors "

#Region " Type ctors "
        'Public Sub New(ByVal t As Type)
        '    _osrc = New ObjectSource(t)
        'End Sub

        'Public Sub New(ByVal entityName As String)
        '    _osrc = New ObjectSource(entityName)
        'End Sub

        'Public Sub New(ByVal [alias] As ObjectAlias)
        '    _osrc = New ObjectSource([alias])
        'End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(t)
            _op = New ObjectProperty(t, propertyAlias)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(entityName)
            _op = New ObjectProperty(entityName, propertyAlias)
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource([alias])
            _op = New ObjectProperty([alias], propertyAlias)
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal fieldAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(t)
            _op = New ObjectProperty(t, propertyAlias)
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal fieldAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(entityName)
            _op = New ObjectProperty(entityName, propertyAlias)
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String, ByVal fieldAlias As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource([alias])
            _op = New ObjectProperty([alias], propertyAlias)
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            '_field = propertyAlias
            '_osrc = New ObjectSource(t)
            _op = New ObjectProperty(t, propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(into)
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(t)
            _op = New ObjectProperty(t, propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(intoEntityName)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            '_field = propertyAlias
            '_osrc = New ObjectSource(entityName)
            _op = New ObjectProperty(entityName, propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(into)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource(entityName)
            _op = New ObjectProperty(entityName, propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(intoEntityName)
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            '_field = propertyAlias
            '_osrc = New ObjectSource([alias])
            _op = New ObjectProperty([alias], propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(into)
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            '_field = propertyAlias
            '_osrc = New ObjectSource([alias])
            _op = New ObjectProperty([alias], propertyAlias)
            _falias = intoPropertyAlias
            _dst = New ObjectSource(intoEntityName)
        End Sub
#End Region

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            _column = column
            _table = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal fieldAlias As String)
            _column = column
            _table = t
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As FieldReference, ByVal fieldAlias As String)
            _column = computed
            _values = values
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As FieldReference)
            MyClass.New(computed, values, Nothing)
        End Sub

        Public Sub New(ByVal q As QueryCmd)
            '_osrc = New ObjectSource(New ObjectAlias(q))
            _q = q
        End Sub

        Public Sub New(ByVal q As QueryCmd, ByVal fieldAlias As String)
            '_osrc = New ObjectSource(New ObjectAlias(q))
            _q = q
            _falias = fieldAlias
        End Sub

        Public Sub New(ByVal agr As AggregateBase)
            _agr = agr
        End Sub

        Public Sub New(ByVal agr As AggregateBase, ByVal fieldAlias As String)
            _agr = agr
            _falias = fieldAlias
        End Sub
#End Region


        Protected Sub RaiseOnChange()
            RaiseEvent OnChange()
        End Sub

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal selectList As IEnumerable(Of T)) As Collections.IndexedCollection(Of String, MapField2Column)
            Dim c As New OrmObjectIndex
            Return GetMapping(c, selectList)
        End Function

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal c As OrmObjectIndex, ByVal selectList As IEnumerable(Of T)) As Collections.IndexedCollection(Of String, MapField2Column)
            For Each s As T In selectList
                c.Add(New MapField2Column(s.PropertyAlias, s.Column, s.Table, s.Attributes))
            Next
            Return c
        End Function

        Public ReadOnly Property PropType() As PropType
            Get
                If _op.ObjectSource IsNot Nothing Then
                    Return Entities.PropType.ObjectProperty
                ElseIf _table IsNot Nothing AndAlso Not String.IsNullOrEmpty(_column) Then
                    Return Entities.PropType.TableColumn
                Else
                    If _values IsNot Nothing Then
                        Return Entities.PropType.CustomValue
                    ElseIf Not String.IsNullOrEmpty(_column) Then
                        Return Entities.PropType.TableColumn
                    ElseIf _q IsNot Nothing Then
                        Return Entities.PropType.Subquery
                    Else
                        Return Entities.PropType.Aggregate
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

        Public Property PropertyAlias() As String
            Get
                Return _op.Field
            End Get
            Protected Friend Set(ByVal value As String)
                _op = New ObjectProperty(_op.ObjectSource, value)
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

        Public ReadOnly Property ObjectSource() As ObjectSource
            Get
                Return _op.ObjectSource
            End Get
        End Property

        Public Property ObjectProperty() As ObjectProperty
            Get
                Return _op
            End Get
            Friend Set(ByVal value As ObjectProperty)
                _op = value
            End Set
        End Property

        Public Property FieldAlias() As String
            Get
                Return _falias
            End Get
            Protected Friend Set(ByVal value As String)
                _falias = value
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
                Return _column
            End Get
            Protected Set(ByVal value As String)
                _column = value
                RaiseOnChange()
            End Set
        End Property

        Public ReadOnly Property IsCustom() As Boolean
            Get
                Return _values IsNot Nothing
            End Get
        End Property

        Public Property Values() As FieldReference()
            Get
                Return _values
            End Get
            Protected Set(ByVal value As FieldReference())
                _values = value
            End Set
        End Property

        Public Overridable Function _ToString() As String Implements Criteria.Values.IQueryElement._ToString
            If _op.ObjectSource IsNot Nothing Then
                Return _op.ObjectSource.ToStaticString & "$" & _op.Field
            Else
                If _table IsNot Nothing Then
                    Return _table.RawName & "$" & _column
                Else
                    If _values IsNot Nothing Then
                        Return _column
                    ElseIf Not String.IsNullOrEmpty(_column) Then
                        Return _column
                    ElseIf _q IsNot Nothing Then
                        Return _q._ToString
                    ElseIf _agr IsNot Nothing Then
                        Return _agr.ToString
                    Else
                        Throw New NotSupportedException
                        'Return _field
                    End If
                End If
            End If
        End Function

        Public Function GetCustomExpressionValues(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable) As String()
            Return ObjectMappingEngine.ExtractValues(schema, stmt, almgr, _values).ToArray
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return _Equals(TryCast(obj, SelectExpression))
        End Function

        Protected Overridable Function _Equals(ByVal s As SelectExpression) As Boolean
            If s Is Nothing Then
                Return False
            Else
                Dim b As Boolean
                If _values IsNot Nothing Then
                    b = _column = s._column
                ElseIf _op.ObjectSource IsNot Nothing Then
                    b = _op.Field = s._op.Field AndAlso _op.ObjectSource.Equals(_op.ObjectSource)
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
                Return CType(_q, Cache.IQueryDependentTypes).Get(mpe)
            End If
            Return New Cache.EmptyDependentTypes
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Criteria.Values.IQueryElement.GetStaticString
            If _op.ObjectSource IsNot Nothing Then
                Return _op.ObjectSource.ToStaticString & "$" & _op.Field
            Else
                If _table IsNot Nothing Then
                    Return _table.RawName & "$" & _column
                Else
                    If _values IsNot Nothing Then
                        Return _column
                    ElseIf Not String.IsNullOrEmpty(_column) Then
                        Return _column
                    ElseIf _q IsNot Nothing Then
                        Return _q.ToStaticString(mpe)
                    ElseIf _agr IsNot Nothing Then
                        Return _agr.ToString
                    Else
                        Throw New NotSupportedException
                        'Return _field
                    End If
                End If
            End If
        End Function
    End Class

End Namespace