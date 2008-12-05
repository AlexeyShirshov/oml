Namespace Entities

    Public Class ObjectAlias
        Private _t As Type
        Private _en As String
        Private _q As Worm.Query.QueryCmd
        Private _uqName As String = Guid.NewGuid.GetHashCode.ToString

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
                Return ToStaticString() & "^" & _uqName
            End Get
        End Property

        Public ReadOnly Property Query() As Worm.Query.QueryCmd
            Get
                Return _q
            End Get
        End Property

        Public Function ToStaticString() As String
            If _t IsNot Nothing Then
                Return _t.ToString
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return _en
            Else
                Throw New NotImplementedException
            End If
        End Function

        'Public Function GetRealType(ByVal schema As ObjectMappingEngine) As Type
        '    Dim rt As Type = Type
        '    If rt Is Nothing Then
        '        rt = schema.GetTypeByEntityName(EntityName)
        '    End If
        '    If rt Is Nothing Then
        '        Throw New NotImplementedException
        '    End If
        '    Return rt
        'End Function

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

    Public Class ObjectSource
        Private _t As Type
        Private _en As String
        Private _a As ObjectAlias

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

        Public Sub New(ByVal [alias] As ObjectAlias)
            If [alias] Is Nothing Then
                Throw New ArgumentNullException("alias")
            End If
            _a = [alias]
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, ObjectSource))
        End Function

        Public Overloads Function Equals(ByVal obj As ObjectSource) As Boolean
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

        Public Function ToStaticString() As String
            If _t IsNot Nothing Then
                Return _t.ToString
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return _en
            ElseIf _a IsNot Nothing Then
                Return _a.ToStaticString
            Else
                Throw New NotImplementedException
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

        Public ReadOnly Property ObjectAlias() As ObjectAlias
            Get
                Return _a
            End Get
        End Property

        Public Function GetRealType(ByVal schema As ObjectMappingEngine) As Type
            Dim rt As Type = AnyType
            If rt Is Nothing Then
                rt = schema.GetTypeByEntityName(AnyEntityName)
            End If
            Return rt
        End Function

        Public Function GetRealType(ByVal schema As ObjectMappingEngine, ByVal defaultType As Type) As Type
            Dim rt As Type = AnyType
            If rt Is Nothing Then
                Dim en As String = AnyEntityName
                If Not String.IsNullOrEmpty(en) Then
                    rt = schema.GetTypeByEntityName(en)
                End If
            End If
            If rt Is Nothing Then
                rt = defaultType
            End If
            Return rt
        End Function
    End Class

    Public Structure ObjectProperty
        Public ReadOnly ObjectSource As ObjectSource
        Public ReadOnly Field As String

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource(entityName)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource(t)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource([alias])
            Me.Field = propertyAlias
        End Sub
    End Structure

End Namespace