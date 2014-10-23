Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria

Namespace Query

    <Serializable()> _
    Public Class QueryAlias
        Private _t As Type
        Private _en As String
        Private _q As Worm.Query.QueryCmd
        Private _uqName As String
        Private _tbl As Entities.Meta.SourceFragment

        Public Sub New(ByVal t As Type)
            _t = t
            _uqName = Guid.NewGuid.GetHashCode.ToString
        End Sub

        Public Sub New(ByVal entityName As String)
            _en = entityName
            _uqName = Guid.NewGuid.GetHashCode.ToString
        End Sub

        Public Sub New(ByVal query As Worm.Query.QueryCmd)
            _q = query
            _uqName = Guid.NewGuid.GetHashCode.ToString
        End Sub

        Public Sub New(ByVal t As Type, name As String)
            _t = t
            _uqName = name
        End Sub

        Public Sub New(ByVal entityName As String, name As String)
            _en = entityName
            _uqName = name
        End Sub

        Public Sub New(ByVal query As Worm.Query.QueryCmd, name As String)
            _q = query
            _uqName = name
        End Sub

        Public ReadOnly Property EntityType() As Type
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
                'Return _ToString() & "^" & _uqName
                Return _uqName
            End Get
        End Property

        Public ReadOnly Property Query() As Worm.Query.QueryCmd
            Get
                Return _q
            End Get
        End Property

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As Object) As String
            'Dim t As Type = GetRealType(mpe)
            'If t IsNot Nothing Then
            'Return mpe.GetEntityKey(t)
            Return _uqName
            'Else
            'Return _q.ToStaticString(mpe, contextInfo)
            'End If
        End Function

        Public Function _ToString() As String
            'If _t IsNot Nothing Then
            'Return _t.ToString
            Return _uqName
            'ElseIf Not String.IsNullOrEmpty(_en) Then
            'Return _en
            'Return _uqName
            'Else
            'Return _q._ToString
            'End If
        End Function

        Protected Function GetRealType(ByVal schema As ObjectMappingEngine) As Type
            Dim rt As Type = EntityType
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
        Implements IQueryElement

        Private _t As Type
        Private _en As String
        Private _a As QueryAlias

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

        Public Sub New(ByVal [alias] As QueryAlias)
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

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String Implements IQueryElement.GetStaticString
            If mpe Is Nothing Then
                Throw New ArgumentNullException("mpe")
            End If

            Dim t As Type = GetRealType(mpe)
            If t IsNot Nothing Then
                Return mpe.GetEntityKey(t)
            Else
                Return _a.ToStaticString(mpe, contextInfo)
            End If
        End Function

        Public Function _ToString() As String Implements IQueryElement._ToString
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

        Public ReadOnly Property IsDefault As Boolean
            Get
                Return EntityType IsNot Nothing OrElse Not String.IsNullOrEmpty(EntityName)
            End Get
        End Property

        Public ReadOnly Property AnyType() As Type
            Get
                If _t Is Nothing AndAlso _a IsNot Nothing Then
                    Return _a.EntityType
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

        Public ReadOnly Property EntityType() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return _en
            End Get
        End Property

        Public ReadOnly Property ObjectAlias() As QueryAlias
            Get
                Return _a
            End Get
        End Property

        Private _calc As Type
        Private _ver As String

        Public Function GetRealType(ByVal mpe As ObjectMappingEngine) As Type
            If _calc Is Nothing OrElse _ver <> mpe.Version Then
                _calc = AnyType
                _ver = mpe.Version
                If _calc Is Nothing AndAlso Not String.IsNullOrEmpty(AnyEntityName) Then
                    _calc = mpe.GetTypeByEntityName(AnyEntityName)
                ElseIf _calc Is Nothing AndAlso _a IsNot Nothing Then
                    _calc = _a.Query.GetSelectedOS.GetRealType(mpe)
                End If
            End If
            Return _calc
        End Function

        Public ReadOnly Property IsQuery() As Boolean
            Get
                Return _a IsNot Nothing AndAlso _a.Query IsNot Nothing
            End Get
        End Property

        Public Shared Function EntityNameEquals(ByVal mpe As ObjectMappingEngine, ByVal e1 As EntityUnion, ByVal e2 As EntityUnion) As Boolean
            If Not String.IsNullOrEmpty(e1._en) Then
                If Not String.IsNullOrEmpty(e2._en) Then
                    Return e1._en = e2._en
                Else
                    Return e1._en = mpe.GetEntityNameByType(e2.GetRealType(mpe))
                End If
            ElseIf String.IsNullOrEmpty(e2._en) Then
                Return mpe.GetEntityNameByType(e1.GetRealType(mpe)) = mpe.GetEntityNameByType(e2.GetRealType(mpe))
            Else
                Return EntityNameEquals(mpe, e2, e1)
            End If
        End Function

        Public Shared Function TypeEquals(ByVal mpe As ObjectMappingEngine, ByVal e1 As EntityUnion, ByVal e2 As EntityUnion) As Boolean
            If Not String.IsNullOrEmpty(e1._en) Then
                If Not String.IsNullOrEmpty(e2._en) Then
                    Return e1._en = e2._en
                Else
                    Return mpe.GetTypeByEntityName(e1._en) Is e2.GetRealType(mpe)
                End If
            ElseIf String.IsNullOrEmpty(e2._en) Then
                Return e1.GetRealType(mpe) Is e2.GetRealType(mpe)
            Else
                Return EntityNameEquals(mpe, e2, e1)
            End If
        End Function

        Public Sub Prepare(ByVal executor As IExecutor, ByVal schema As ObjectMappingEngine,
                           ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Criteria.Values.IQueryElement.Prepare
            If _a IsNot Nothing AndAlso _a.Query IsNot Nothing Then
                _a.Query.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public Shared Operator =(ByVal a As EntityUnion, ByVal b As EntityUnion) As Boolean
            Return Equals(a, b)
        End Operator

        Public Shared Operator <>(ByVal a As EntityUnion, ByVal b As EntityUnion) As Boolean
            Return Not Equals(a, b)
        End Operator

        Public Shared Widening Operator CType(entityName As String) As EntityUnion
            Return New EntityUnion(entityName)
        End Operator

        Public Shared Widening Operator CType(entityType As Type) As EntityUnion
            Return New EntityUnion(entityType)
        End Operator

        Public Shared Widening Operator CType(entityAlias As QueryAlias) As EntityUnion
            Return New EntityUnion(entityAlias)
        End Operator
    End Class

    <Serializable()> _
    Public Structure ObjectProperty
        Public ReadOnly Entity As EntityUnion
        Public ReadOnly PropertyAlias As String

        Public Const PrimaryKeyReference As String = "19rfas$%*&^ldfj"

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            Me.Entity = New EntityUnion(entityName)
            Me.PropertyAlias = propertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            Me.Entity = New EntityUnion(t)
            Me.PropertyAlias = propertyAlias
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String)
            Me.Entity = New EntityUnion([alias])
            Me.PropertyAlias = propertyAlias
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
            Me.Entity = os
            Me.PropertyAlias = propertyAlias
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If obj Is Nothing OrElse Not TypeOf obj Is ObjectProperty Then
                Return False
            End If
            Return Equals(CType(obj, ObjectProperty))
        End Function

        Public Overloads Function Equals(ByVal obj As ObjectProperty) As Boolean
            Return Entity.Equals(obj.Entity) AndAlso PropertyAlias = obj.PropertyAlias
        End Function

        Public Function GetPropertyAlias(ByVal mpe As ObjectMappingEngine) As String
            If mpe IsNot Nothing AndAlso PropertyAlias = PrimaryKeyReference Then
                Return mpe.GetSinglePK(Entity.GetRealType(mpe))
            End If

            Return PropertyAlias
        End Function

        Public Function GetPropertyAlias(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema) As String
            If mpe IsNot Nothing AndAlso PropertyAlias = PrimaryKeyReference Then
                Dim eu As EntityUnion = Entity
                Return mpe.GetSinglePK(oschema, Function() eu.GetRealType(mpe))
            End If

            Return PropertyAlias
        End Function

#Region " Operators "
        Public Shared Operator +(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) + b
        End Operator

        Public Shared Operator -(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) - b
        End Operator

        Public Shared Operator *(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) * b
        End Operator

        Public Shared Operator /(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) / b
        End Operator

        Public Shared Operator Mod(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) Mod b
        End Operator

        Public Shared Operator Xor(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) Xor b
        End Operator

        Public Shared Operator And(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) And b
        End Operator

        Public Shared Operator Or(ByVal a As ObjectProperty, ByVal b As Object) As ECtor.Int
            Return ECtor.prop(a) Or b
        End Operator

        Public Shared Operator =(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) = b
        End Operator

        Public Shared Operator <>(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) <> b
        End Operator

        Public Shared Operator >(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) > b
        End Operator

        Public Shared Operator <(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) < b
        End Operator

        Public Shared Operator >=(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) >= b
        End Operator

        Public Shared Operator <=(ByVal a As ObjectProperty, ByVal b As Object) As PredicateLink
            Return Ctor.prop(a) <= b
        End Operator

        Public Shared Widening Operator CType(ByVal a As ObjectProperty) As ECtor.Int
            Return ECtor.prop(a)
        End Operator

        Public Shared Widening Operator CType(ByVal a As ObjectProperty) As Expressions2.EntityExpression
            Return New Expressions2.EntityExpression(a)
        End Operator

#End Region
    End Structure

End Namespace