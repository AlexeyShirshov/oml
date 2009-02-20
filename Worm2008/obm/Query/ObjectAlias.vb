Namespace Query

    <Serializable()> _
    Public Class EntityAlias
        Private _t As Type
        Private _en As String
        Private _q As Worm.Query.QueryCmd
        Private _uqName As String = Guid.NewGuid.GetHashCode.ToString
        Private _tbl As Entities.Meta.SourceFragment

        Public Sub New(ByVal t As Type)
            _t = t
        End Sub

        Public Sub New(ByVal entityName As String)
            _en = entityName
        End Sub

        Public Sub New(ByVal query As Worm.Query.QueryCmd)
            _q = query
        End Sub

        Public ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return _en
            End Get
        End Property

        Public ReadOnly Property UniqueName() As String
            Get
                Return _ToString() & "^" & _uqName
            End Get
        End Property

        Public ReadOnly Property Query() As Worm.Query.QueryCmd
            Get
                Return _q
            End Get
        End Property

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Dim t As Type = GetRealType(mpe)
            If t IsNot Nothing Then
                Return mpe.GetEntityKey(contextFilter, t)
            Else
                Return _q.ToStaticString(mpe, contextFilter)
            End If
        End Function

        Public Function _ToString() As String
            If _t IsNot Nothing Then
                Return _t.ToString
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return _en
            Else
                Return _q._ToString
            End If
        End Function

        Protected Function GetRealType(ByVal schema As ObjectMappingEngine) As Type
            Dim rt As Type = Type
            If rt Is Nothing Then
                rt = schema.GetTypeByEntityName(EntityName)
            End If
            Return rt
        End Function

        Friend Property Tbl() As Entities.Meta.SourceFragment
            Get
                Return _tbl
            End Get
            Set(ByVal value As Entities.Meta.SourceFragment)
                _tbl = value
            End Set
        End Property
        'Public Function GetRealType(ByVal schema As ObjectMappingEngine, ByVal defaultType As Type) As Type
        '    Dim rt As Type = Type
        '    If rt Is Nothing Then
        '        rt = schema.GetTypeByEntityName(EntityName)
        '    End If
        '    If rt Is Nothing Then
        '        rt = defaultType
        '    End If
        '    Return rt
        'End Function
    End Class

    <Serializable()> _
    Public Class EntityUnion
        Private _t As Type
        Private _en As String
        Private _a As EntityAlias

        Public Sub New(ByVal t As Type)
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If
            _t = t
        End Sub

        Public Sub New(ByVal entityName As String)
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If
            _en = entityName
        End Sub

        Public Sub New(ByVal [alias] As EntityAlias)
            If [alias] Is Nothing Then
                Throw New ArgumentNullException("alias")
            End If
            _a = [alias]
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, EntityUnion))
        End Function

        Public Overloads Function Equals(ByVal obj As EntityUnion) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return Object.Equals(_t, obj._t) AndAlso String.Equals(_en, obj._en) AndAlso Object.Equals(_a, obj._a)
        End Function

        Public Overrides Function GetHashCode() As Integer
            If _t IsNot Nothing Then
                Return _t.GetHashCode
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return _en.GetHashCode
            ElseIf _a IsNot Nothing Then
                Return _a.GetHashCode
            Else
                Throw New NotImplementedException
            End If
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            If mpe Is Nothing Then
                Throw New ArgumentNullException("mpe")
            End If

            Dim t As Type = GetRealType(mpe)
            If t IsNot Nothing Then
                Return mpe.GetEntityKey(contextFilter, t)
            Else
                Return _a.ToStaticString(mpe, contextFilter)
            End If
        End Function

        Public Function _ToString() As String
            If _t IsNot Nothing Then
                Return _t.ToString
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return _en
            Else
                Return _a._ToString()
            End If
        End Function

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property AnyType() As Type
            Get
                If _t Is Nothing AndAlso _a IsNot Nothing Then
                    Return _a.Type
                End If
                Return _t
            End Get
        End Property

        Public ReadOnly Property AnyEntityName() As String
            Get
                If _t Is Nothing AndAlso _a IsNot Nothing Then
                    Return _a.EntityName
                End If
                Return _en
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return _en
            End Get
        End Property

        Public ReadOnly Property ObjectAlias() As EntityAlias
            Get
                Return _a
            End Get
        End Property

        Private _calc As Type
        Public Function GetRealType(ByVal schema As ObjectMappingEngine) As Type
            If _calc Is Nothing Then
                _calc = AnyType
                If _calc Is Nothing AndAlso Not String.IsNullOrEmpty(AnyEntityName) Then
                    _calc = schema.GetTypeByEntityName(AnyEntityName)
                ElseIf _calc Is Nothing AndAlso _a IsNot Nothing Then
                    _calc = _a.Query.GetSelectedOS.GetRealType(schema)
                End If
            End If
            Return _calc
        End Function

        Public ReadOnly Property IsQuery() As Boolean
            Get
                Return _a IsNot Nothing AndAlso _a.Query IsNot Nothing
            End Get
        End Property

        'Public Function GetRealType(ByVal schema As ObjectMappingEngine, ByVal defaultType As Type) As Type
        '    Dim rt As Type = AnyType
        '    If rt Is Nothing Then
        '        Dim en As String = AnyEntityName
        '        If Not String.IsNullOrEmpty(en) Then
        '            rt = schema.GetTypeByEntityName(en)
        '        End If
        '    End If
        '    If rt Is Nothing Then
        '        rt = defaultType
        '    End If
        '    Return rt
        'End Function
    End Class

    <Serializable()> _
    Public Structure ObjectProperty
        Public ReadOnly ObjectSource As EntityUnion
        Public ReadOnly Field As String

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            Me.ObjectSource = New EntityUnion(entityName)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            Me.ObjectSource = New EntityUnion(t)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal [alias] As EntityAlias, ByVal propertyAlias As String)
            Me.ObjectSource = New EntityUnion([alias])
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
            Me.ObjectSource = os
            Me.Field = propertyAlias
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If obj Is Nothing OrElse Not TypeOf obj Is ObjectProperty Then
                Return False
            End If
            Return Equals(CType(obj, ObjectProperty))
        End Function

        Public Overloads Function Equals(ByVal obj As ObjectProperty) As Boolean
            Return ObjectSource.Equals(obj.ObjectSource) AndAlso Field = obj.Field
        End Function

    End Structure

End Namespace