﻿Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria
Imports CoreFramework

Namespace Query

    <Serializable()>
    Public Class QueryAlias
        Implements ICopyable

        Private _t As Type
        Private _en As String
        Private _q As Worm.Query.QueryCmd
        Private ReadOnly _uqName As String
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

        Protected Sub New(uniqueName As String, designate As QueryAlias)
            _uqName = uniqueName
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

        Public Function ToStaticString(mpe As ObjectMappingEngine) As String
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

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As QueryAlias
            Dim n As New QueryAlias(UniqueName, Me)
            CopyTo(n)
            Return n
        End Function
        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, QueryAlias))
        End Function

        Public Function CopyTo(target As QueryAlias) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._t = _t
            target._en = _en

            If _q IsNot Nothing Then
                target._q = _q.Clone
            End If

            If _tbl IsNot Nothing Then
                target._tbl = _tbl.Clone
            End If

            Return True
        End Function
    End Class

    <Serializable()>
    Public Class EntityUnion
        Implements IQueryElement

        Private _t As Type
        Private _en As String
        Private _a As QueryAlias
        Private ReadOnly _spin As New CoreFramework.CFThreading.SpinLockRef

        Public Sub New(ByVal t As Type)
            If t Is Nothing Then
                Throw New ArgumentNullException(NameOf(t))
            End If
            _t = t
        End Sub

        Public Sub New(ByVal entityName As String)
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException(NameOf(entityName))
            End If
            _en = entityName
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias)
            If [alias] Is Nothing Then
                Throw New ArgumentNullException(NameOf([alias]))
            End If
            _a = [alias]
        End Sub
        Protected Sub New()

        End Sub
        'Protected Sub New(t As Type, en As String, a As QueryAlias)
        '    _t = t
        '    _en = en
        '    _a = a
        'End Sub
        Public Property Hint As String
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
        Friend Function Dump(mpe As ObjectMappingEngine, Optional realType As Boolean = False) As String
            If _t IsNot Nothing Then
                Return "type: {0}. gethashcode: {1}".Format2(_t, _t.GetHashCode)
            ElseIf Not String.IsNullOrEmpty(_en) Then
                Return "entity: {0}. type gethashcode: {1}".Format2(_en, If(Not realType, GetRealType(mpe).GetHashCode, Nothing))
            ElseIf _a IsNot Nothing Then
                Return "alias: {0}. type gethashcode: {1}".Format2(_a, If(Not realType, GetRealType(mpe).GetHashCode, Nothing))
            Else
                Throw New NotImplementedException
            End If
        End Function
        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If mpe Is Nothing Then
                Throw New ArgumentNullException(NameOf(mpe))
            End If

            Dim t As Type = GetRealType(mpe)
            If t IsNot Nothing Then
                Return mpe.GetEntityKey(t)
            ElseIf _a IsNot Nothing Then
                Return _a.ToStaticString(mpe)
            End If

            Throw New NotSupportedException(String.Format("Invalid state {0}", Dump(mpe)))
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
        Private _mark As Guid

        Public Function GetRealType(ByVal mpe As ObjectMappingEngine) As Type
            If mpe Is Nothing Then
                Throw New ArgumentNullException(NameOf(mpe))
            End If
            Dim calc = _calc
            If calc Is Nothing OrElse _mark <> mpe.Mark Then
                Using New CSScopeMgrLite(_spin)
                    If _calc Is Nothing OrElse _mark <> mpe.Mark Then
                        _calc = AnyType
                        _mark = mpe.Mark
                        If _calc Is Nothing AndAlso Not String.IsNullOrEmpty(AnyEntityName) Then
                            _calc = mpe.GetTypeByEntityName(AnyEntityName)
                        ElseIf _calc Is Nothing AndAlso _a IsNot Nothing Then
                            _calc = _a.Query.GetSelectedOS.GetRealType(mpe)
                        End If

                        If _calc Is Nothing Then
                            Throw New ApplicationException(String.Format("EntityUnion {0} cannot be converted to type", Dump(mpe, True)))
                        End If

                    End If
                    calc = _calc
                End Using
            End If
            Return calc
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

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As EntityUnion
            Dim n As New EntityUnion()
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, EntityUnion))
        End Function

        Public Function CopyTo(target As EntityUnion) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._en = _en
            target._t = _t

            If _a IsNot Nothing Then
                target._a = _a.Clone
            End If

            target.Hint = Hint
            Return True
        End Function
    End Class

    <Serializable()> _
    Public Structure ObjectProperty
        Implements ICloneable

        Private _e As EntityUnion
        Private _pa As String
        Public Property Entity As EntityUnion
            Get
                Return _e
            End Get
            Private Set(value As EntityUnion)
                _e = value
            End Set
        End Property
        Public Property PropertyAlias As String
            Get
                Return _pa
            End Get
            Private Set(value As String)
                _pa = value
            End Set
        End Property
        Public Function IsEmpty() As Boolean
            If String.IsNullOrEmpty(PropertyAlias) AndAlso _e Is Nothing Then
                Return True
            End If

            Return False
        End Function
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
                Return mpe.GetPrimaryKey(Entity.GetRealType(mpe))
            End If

            Return PropertyAlias
        End Function

        Public Function GetPropertyAlias(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema) As String
            If mpe IsNot Nothing AndAlso PropertyAlias = PrimaryKeyReference Then
                Dim eu As EntityUnion = Entity
                Return oschema.GetSinglePK(Function() eu.GetRealType(mpe))
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

        Private Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function
        Public Function Clone() As ObjectProperty
            Dim n As New ObjectProperty With {
                ._pa = _pa
            }
            If _e IsNot Nothing Then
                n._e = _e.Clone
            End If
            Return n
        End Function

    End Structure

End Namespace