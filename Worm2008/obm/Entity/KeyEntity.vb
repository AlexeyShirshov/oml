Imports System.Xml
Imports Worm.Query.Sorting
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Criteria.Core
Imports Worm.Query

#Const TraceSetState = False

Namespace Entities

    <Serializable()> _
    Public Class ObjectStateException
        Inherits System.Exception

        Public Sub New()
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    ''' <summary>
    ''' Базовый класс для всех типов
    ''' </summary>
    ''' <remarks>
    ''' Класс является потокобезопасным как на чтение так и на запись.
    ''' Предоставляет следующий функционал:
    ''' XML сериализация/десериализация. Реализована с некоторыми ограничениями. Для изменения поведения необходимо переопределить <see cref="KeyEntity.ReadXml" /> и <see cref="KeyEntity.WriteXml"/>.
    ''' <code lang="vb">Это код</code>
    ''' <example>Это пример</example>
    ''' </remarks>
    <Serializable()> _
    Public MustInherit Class KeyEntityBase
        Inherits CachedEntity
        Implements _IKeyEntity

#Region " Classes "

#If OLDM2M Then
        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class M2MClass
            Private _o As KeyEntityBase

            Friend Sub New(ByVal o As KeyEntityBase)
                _o = o
            End Sub

            Protected ReadOnly Property GetMgr() As IGetManager
                Get
                    Return _o.GetMgr
                End Get
            End Property

            Public Function Find(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, GetType(T), String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, GetType(T)))
                    End If
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, rel.Key, withLoad)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(direct), withLoad)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})() As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, GetType(T), String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, GetType(T)))
                    End If
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, Nothing, rel.Key, False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal direct As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, Nothing, M2MRelationDesc.GetKey(direct), False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal sort As Sort) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, GetType(T), String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, GetType(T)))
                    End If
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, sort, rel.Key, False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal sort As Sort, ByVal direct As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, sort, M2MRelationDesc.GetKey(direct), False)
                End Using
            End Function

            Public Function Find(ByVal top As Integer, ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {top, criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal top As Integer, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(True), withLoad, top)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal top As Integer, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(direct), withLoad, top)
                End Using
            End Function

            Public Function FindDistinct(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("FindDistinct", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function FindDistinct(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                'Dim rel As M2MRelation = GetMgr.ObjectSchema.GetM2MRelation(Me.GetType, GetType(T), True)
                'Return GetMgr.FindDistinct(Of T)(rel, criteria, sort, withLoad)
                Return FindDistinct(Of T)(criteria, sort, True, withLoad)
            End Function

            Public Function FindDistinct(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(GetType(T), _o.GetType, direct)
                    Dim pk As String = mc.Manager.MappingEngine.GetPrimaryKeys(_o.GetType, Nothing)(0).PropertyAlias
                    Dim crit As PredicateLink = New PropertyPredicate(New EntityUnion(_o.GetType), pk).eq(_o).[and](CType(criteria, PredicateLink))
                    Return mc.Manager.FindDistinct(Of T)(rel, crit, sort, withLoad)
                End Using
            End Function

            Public Sub Add(ByVal obj As _IKeyEntity)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, obj.GetType, String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, obj.GetType))
                    End If
                    mc.Manager.M2MAdd(_o, obj, rel.Key)
                End Using
            End Sub

            Public Sub Add(ByVal obj As _IKeyEntity, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MAdd(_o, obj, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Delete(ByVal t As Type)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, t, String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, t))
                    End If
                    mc.Manager.M2MDelete(_o, t, rel.Key)
                End Using
            End Sub

            Public Sub Delete(ByVal t As Type, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, t, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Delete(ByVal obj As _IKeyEntity)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(_o.GetType, obj.GetType, String.Empty)
                    If rel Is Nothing Then
                        Throw New OrmObjectException(String.Format("Relation between {0} and {1} not found", _o.GetType, obj.GetType))
                    End If
                    mc.Manager.M2MDelete(_o, obj, rel.Key)
                End Using
            End Sub

            Public Sub Delete(ByVal obj As _IKeyEntity, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, obj, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Cancel(ByVal t As Type)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MCancel(_o, t)
                End Using
            End Sub

            Public Sub Merge(Of T As {_IKeyEntity, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean)
                If removeNotInList Then
                    For Each o As T In Find(Of T)(Nothing, Nothing, False)
                        If Not col.Contains(o) Then
                            Delete(o)
                        End If
                    Next
                End If
                For Each o As T In col
                    If Not Find(Of T)(Nothing, Nothing, False).Contains(o) Then
                        Add(o)
                    End If
                Next
            End Sub

            Public Sub Merge(Of T As {_IKeyEntity, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean, ByVal direct As Boolean)
                If removeNotInList Then
                    For Each o As T In Find(Of T)(Nothing, Nothing, False)
                        If Not col.Contains(o) Then
                            Delete(o, direct)
                        End If
                    Next
                End If
                For Each o As T In col
                    If Not Find(Of T)(Nothing, Nothing, False).Contains(o) Then
                        Add(o, direct)
                    End If
                Next
            End Sub

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal sort As Sort) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function GetTable(ByVal t As Type, ByVal key As String) As SourceFragment
                Dim m2m As M2MRelationDesc = _o.GetMappingEngine.GetM2MRelation(_o.GetType, t, key)
                If m2m Is Nothing Then
                    Throw New ArgumentException(String.Format("Invalid type {0} or key {1}", t.ToString, key))
                Else
                    Return m2m.Table
                End If
            End Function

            Public Function GetTable(ByVal t As Type) As SourceFragment
                Return GetTable(t, Nothing)
            End Function
        End Class
#End If

        Public Class InternalClass2
            Inherits InternalClass

            Public Sub New(ByVal o As KeyEntityBase)
                MyBase.New(o)
            End Sub

            Public ReadOnly Property HasRelaionalChanges() As Boolean
                Get
                    Return CType(Obj, IRelations).HasChanges()
                End Get
            End Property
        End Class
#End Region

        <NonSerialized()> _
        Friend _loading As Boolean

#If OLDM2M Then
        <NonSerialized()> _
        Protected Friend _needAccept As New Generic.List(Of AcceptState2)
#End If

        <NonSerialized()> _
        Protected _saved As Boolean

        <NonSerialized()> _
        Protected Friend _relations As New List(Of Relation)

#Region " Protected functions "

        Protected Overridable ReadOnly Property _HasChanges() As Boolean Implements IRelations.HasChanges
            Get
#If OLDM2M Then
                If _needAccept IsNot Nothing AndAlso _needAccept.Count > 0 Then
                    Return True
                End If
#End If
                For Each r As Relation In _relations
                    'Dim el As M2MRelation = TryCast(r, M2MRelation)
                    'If el IsNot Nothing Then
                    If r.HasChanges Then
                        Return True
                    End If
                    'End If
                Next
                Return False
            End Get
        End Property

        Protected Overrides Sub Init(ByVal pk() As PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
            Throw New NotSupportedException
        End Sub

        Protected Overrides Sub Init()
            MyBase.Init()
        End Sub

        Protected Overridable Overloads Sub Init(ByVal id As Object, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) Implements _IKeyEntity.Init
            MyBase._Init(cache, schema)
            Identifier = id
            PKLoaded(1)
            CType(Me, _ICachedEntity).SetLoaded(GetPKValues(0).PropertyAlias, True, True, schema)
        End Sub

        <Runtime.Serialization.OnDeserialized()> _
        Private Overloads Sub Init(ByVal context As Runtime.Serialization.StreamingContext)
            Init()
            If ObjectState <> Entities.ObjectState.Created Then
                Using mc As IGetManager = GetMgr()
                    If mc IsNot Nothing Then
                        mc.Manager.RegisterInCashe(Me)
                    End If
                End Using
            End If
        End Sub

#If TraceSetState Then
        Public Structure SetState
            Private _dt As Date
            Private _r As ModifiedObject.ReasonEnum
            Private _modifiedStack As String
            Private _stack As String
            Private _oldState As ObjectState
            Private _newState As ObjectState
            Private _md As Date

            Public Sub New(ByVal dt As Date, ByVal r As ModifiedObject.ReasonEnum, ByVal mstack As String, _
                ByVal stack As String, ByVal os As ObjectState, ByVal ns As ObjectState, ByVal md As Date)
                _dt = dt
                _r = r
                _modifiedStack = mstack
                _stack = stack
                _oldState = os
                _newState = ns
                _md = md
            End Sub
        End Structure

        'Private _s As New List(Of Pair(Of Date, Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState)))))
        Private _s As New List(Of SetState)
#End If

        '#If TraceSetState Then
        '        Friend Sub SetObjectState(ByVal state As ObjectState, ByVal r As ModifiedObject.ReasonEnum, ByVal stack As String, ByVal md As Date)
        '            '_s.Add(New Pair(Of Date, Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState))))(Now, New Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState)))(r, New Pair(Of String, Pair(Of ObjectState, ObjectState))(Environment.StackTrace, New Pair(Of ObjectState, ObjectState)(_state, state)))))
        '            _s.Add(New SetState(Now, r, stack, Environment.StackTrace, _state, state, md))
        '#Else
        '        Friend Sub SetObjectState(ByVal state As ObjectState)
        '#End If
        '            _state = state
        '        End Sub

        Protected Sub CheckCash()
            'If OrmCache Is Nothing Then
            '    Throw New OrmObjectException(ObjName & "The object is floating and has not cashe that is needed to perform this operation")
            'End If
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    Throw New OrmObjectException(ObjName & "You have to create MediaContent object to perform this operation")
                End If
            End Using
        End Sub

        Protected Overrides Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mgr As OrmManager)
            Dim t As Type = Me.GetType
            Dim cache As OrmCache = TryCast(mgr.Cache, OrmCache)

#If OLDM2M Then
            for each acs as acceptstate2 in _needaccept
                acs.accept(me, mgr)
            next
            _needAccept.Clear()
#End If

            Dim rel As IRelation = mgr.MappingEngine.GetConnectedTypeRelation(t)
#If OLDM2M Then
            If rel IsNot Nothing AndAlso cache IsNot Nothing Then
                Dim c As New OrmManager.M2MEnum(rel, Me, mgr.MappingEngine)
                cache.ConnectedEntityEnum(mgr, t, AddressOf c.Accept)
            End If
#End If

            For Each rl As Relation In _relations
                Dim el As M2MRelation = TryCast(rl, M2MRelation)
                If el Is Nothing Then Continue For
                SyncLock "1efb139gf8bh"
                    For Each o As _IKeyEntity In el.Added
                        'Dim o As _IOrmBase = CType(mgr.GetOrmBaseFromCacheOrCreate(id, el.SubType), _IOrmBase)
                        Dim m As M2MRelation = CType(o.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                        m.Added.Remove(Me)
                        m._savedIds.Remove(Me)
                        If updateCache AndAlso cache IsNot Nothing Then
                            cache.RemoveM2MQueries(m)
                        Else
                            Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                            If Not l.Contains(m) Then
                                l.Add(m)
                            End If
                        End If
                    Next
                    el.Added.Clear()

                    For Each o As _IKeyEntity In el.Deleted
                        'Dim o As _IOrmBase = CType(mgr.GetOrmBaseFromCacheOrCreate(id, el.SubType), _IOrmBase)
                        Dim m As M2MRelation = CType(o.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                        m.Deleted.Remove(Me)
                        If updateCache AndAlso cache IsNot Nothing Then
                            cache.RemoveM2MQueries(el)
                        Else
                            Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                            If Not l.Contains(m) Then
                                l.Add(m)
                            End If
                        End If
                    Next
                    el.Deleted.Clear()
                    el.Reject2()

                    If updateCache AndAlso cache IsNot Nothing Then
                        cache.RemoveM2MQueries(el)
                    Else
                        Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                        If Not l.Contains(el) Then
                            l.Add(el)
                        End If
                    End If
                End SyncLock
            Next
        End Sub

        Private Function GetName() As String Implements _IKeyEntity.GetName
            Return Me.GetType.Name & Identifier.ToString
        End Function

#If OLDM2M Then
        Private Function GetOldName(ByVal id As Object) As String Implements _IKeyEntity.GetOldName
            Return Me.GetType.Name & id.ToString
        End Function

        Private Function AddAccept(ByVal acs As AcceptState2) As Boolean Implements _IKeyEntity.AddAccept
            Using SyncHelper(False)
                For Each a As AcceptState2 In _needAccept
                    If a.el Is acs.el Then
                        Return False
                    End If
                Next
                _needAccept.Add(acs)
            End Using
            Return True
        End Function

        Private Function GetM2M() As Generic.IList(Of AcceptState2) Implements _IKeyEntity.GetM2M
            Return _needAccept
        End Function



        Private Function GetAccept(ByVal m As M2MCache) As AcceptState2 Implements _IKeyEntity.GetAccept
            Using SyncHelper(False)
                For Each a As AcceptState2 In _needAccept
                    If a.CacheItem Is m Then
                        Return a
                    End If
                Next
            End Using
            Return Nothing
        End Function
#End If


#End Region

#Region " Public properties "
        Public Shadows ReadOnly Property InternalProperties() As InternalClass2
            Get
                Return New InternalClass2(Me)
            End Get
        End Property

        Protected Overrides ReadOnly Property HasChanges() As Boolean
            Get
                Return MyBase.HasChanges OrElse _HasChanges
            End Get
        End Property

        'Public Overrides ReadOnly Property InternalProperties() As InternalClass
        '    Get
        '        Return New InternalClass2(Me)
        '    End Get
        'End Property

#If OLDM2M Then
        Public ReadOnly Property M2M() As M2MClass
            Get
                Return New M2MClass(Me)
            End Get
        End Property
#End If

        Public MustOverride Property Identifier() As Object Implements IKeyEntity.Identifier

#End Region

#Region " Synchronization "
        Protected Friend Property ObjSaved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

#End Region

#Region " Public functions "

        ''' <param name="obj">The System.Object to compare with the current System.Object.</param>
        ''' <returns>true if the specified System.Object is equal to the current System.Object; otherwise, false.</returns>
        Public Overloads Overrides Function Equals(ByVal obj As Object) As System.Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Equals(CType(obj, KeyEntity))
            Else
                Return False 'Identifier.Equals(obj)
            End If
        End Function

        'Public Overridable Overloads Function Equals(ByVal other_id As Integer) As Boolean
        '    Return Me.Identifier = other_id
        'End Function

        Public Overridable Overloads Function Equals(ByVal obj As KeyEntity) As Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Me.Identifier.Equals(obj.Identifier)
            Else
                Return False
            End If
        End Function

        ''' <returns>A hash code for the current System.Object.</returns>
        Public Overrides Function GetHashCode() As System.Int32
            Return Identifier.GetHashCode
        End Function

        Public Overrides Function ToString() As String
            Return Identifier.ToString()
        End Function

        'Public MustOverride ReadOnly Property HasChanges() As Boolean

        'Protected MustOverride Function GetNew() As OrmBase

        Private Sub _RejectM2MIntermidiate() Implements _IKeyEntity.RejectM2MIntermidiate
            Using SyncHelper(False)
#If OLDM2M Then
                For Each acs As AcceptState2 In _needAccept
                    If acs.el IsNot Nothing Then
                        acs.el.Reject2()
                    End If
                Next
#End If


                For Each rl As Relation In _relations
                    Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    If el IsNot Nothing Then el.Reject2()
                Next
            End Using
        End Sub

        Public Overrides Sub RejectRelationChanges(ByVal mc As OrmManager)
            Using SyncHelper(False)
                Dim t As Type = Me.GetType
                'Using gmc As IGetManager = GetMgr()
                'Dim mc As OrmManager = gmc.Manager
                Dim rel As IRelation = mc.MappingEngine.GetConnectedTypeRelation(t)
                Dim cache As OrmCache = TryCast(mc.Cache, OrmCache)
#If OLDM2M Then
                If rel IsNot Nothing AndAlso cache IsNot Nothing Then
                    Dim c As New OrmManager.M2MEnum(rel, Me, mc.MappingEngine)
                    cache.ConnectedEntityEnum(mc, t, AddressOf c.Reject)
                End If
                For Each acs As AcceptState2 In _needAccept
                    If acs.el IsNot Nothing Then
                        acs.el.Reject(mc, True)
                    End If
                Next
                _needAccept.Clear()
#End If


                For Each rl As Relation In _relations
                    'Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    'If el IsNot Nothing Then el.Reject(mc, True)
                    rl.Reject(mc)
                Next
                'End Using
            End Using
        End Sub
#End Region

        Public Shared Shadows Operator <>(ByVal obj1 As KeyEntityBase, ByVal obj2 As KeyEntityBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Shadows Operator =(ByVal obj1 As KeyEntityBase, ByVal obj2 As KeyEntityBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator

        Protected Overrides Function GetCacheKey() As Integer
            Return Identifier.GetHashCode
        End Function

        Protected Overrides Sub InitNewEntity(ByVal mgr As OrmManager, ByVal en As Entity)
            If mgr Is Nothing Then
                CType(en, KeyEntityBase).Init(Identifier, Nothing, Nothing)
            Else
                CType(en, KeyEntityBase).Init(Identifier, mgr.Cache, mgr.MappingEngine)
            End If
        End Sub

        Protected Overrides Sub PKLoaded(ByVal pkCount As Integer)
            If pkCount <> 1 Then
                Throw New OrmObjectException(String.Format("KeyEntity derived class must have only one PK. The value is {0}", pkCount))
            End If
            MyBase.PKLoaded(pkCount)
        End Sub


        Public Overrides ReadOnly Property UniqueString() As String
            Get
                Return "PK:" & Identifier.ToString
            End Get
        End Property
#Region " Relations "
        Protected Overrides Function GetChangedRelationObjects() As System.Collections.Generic.List(Of ICachedEntity)
            Dim l As List(Of ICachedEntity) = MyBase.GetChangedRelationObjects()
            For Each rl As Relation In _relations
                For Each e As ICachedEntity In GetCmd(rl.Relation).ToEntityList(Of _ICachedEntity)()
                    l.Add(e)
                Next
            Next
            Return l
        End Function

        Protected Function NormalizeRelation(ByVal oldRel As Relation, ByVal newRel As Relation, ByVal schema As ObjectMappingEngine) As Relation Implements IRelations.NormalizeRelation
            Using GetSyncRoot()
                If _relations.Count > 0 Then
                    For Each rl As Relation In _relations
                        If rl.MainType Is newRel.MainType AndAlso rl.MainId.Equals(newRel.MainId) AndAlso Object.Equals(rl.Relation.Entity, newRel.Relation.Entity) AndAlso Object.Equals(rl.Relation.Key, newRel.Relation.Key) AndAlso Object.Equals(rl.Relation.Column, newRel.Relation.Column) Then
                            Return rl
                        ElseIf Relation.MetaEquals(rl, oldRel, schema) Then
                            _relations.Remove(rl)
                            _relations.Add(newRel)
                            Return newRel
                        End If
                    Next
                    If oldRel.Relation Is Nothing Then
                        _relations.Add(newRel)
                    Else
                        Throw New KeyNotFoundException("Relation is not found")
                    End If
                Else
                    _relations.Add(newRel)
                End If
            End Using

            Return newRel
        End Function

        Protected Sub AddRel(ByVal rel As Relation)
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    If rel.Equals(rl) Then
                        Return
                    End If
                Next
                _relations.Add(rel)
            End Using
        End Sub

        Public Function CreateRelCmd(ByVal eu As EntityUnion) As Worm.Query.RelationCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            If CreateManager IsNot Nothing Then
                q = New Worm.Query.RelationCmd(Me, eu, CreateManager)
            Else
                q = Worm.Query.RelationCmd.Create(Me, eu)
            End If
            q.SpecificMappingEngine = SpecificMappingEngine
            AddRel(q._rel)
            Return q
        End Function

        Public Function CreateRelCmd(ByVal eu As EntityUnion, ByVal key As String) As Worm.Query.QueryCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            If CreateManager IsNot Nothing Then
                q = New Worm.Query.RelationCmd(Me, eu, key, CreateManager)
            Else
                q = Worm.Query.RelationCmd.Create(Me, eu, key)
            End If
            q.SpecificMappingEngine = SpecificMappingEngine
            AddRel(q._rel)
            Return q
        End Function

        Public Function CreateRelCmd(ByVal desc As RelationDesc) As Worm.Query.QueryCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            'Dim mr As M2MRelationDesc = TryCast(desc, M2MRelationDesc)
            If CreateManager IsNot Nothing Then
                'If mr IsNot Nothing Then
                '    q = New Worm.Query.RelationCmd(New M2MRelation(Me, mr), CreateManager)
                'Else
                q = New Worm.Query.RelationCmd(GetRelation(desc), CreateManager)
                'End If
            Else
                'If mr IsNot Nothing Then
                '    q = Worm.Query.RelationCmd.Create(New M2MRelation(Me, mr))
                'Else
                q = Worm.Query.RelationCmd.Create(GetRelation(desc))
                'End If
            End If
            q.SpecificMappingEngine = SpecificMappingEngine
            AddRel(q._rel)
            Return q
        End Function

        Public Function GetCmd(ByVal t As System.Type) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(t)).SelectEntity(t), RelationCmd)
        End Function

        Public Function GetCmd(ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(t), key).SelectEntity(t), RelationCmd)
        End Function

        Public Function GetCmd(ByVal entityName As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(entityName)).SelectEntity(entityName), RelationCmd)
        End Function

        Public Function GetCmd(ByVal entityName As String, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(entityName), key).SelectEntity(entityName), RelationCmd)
        End Function

        Public Function GetCmd(ByVal desc As RelationDesc) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(desc).SelectEntity(desc.Entity), RelationCmd)
        End Function

        'Protected Function GetM2M(ByVal t As Type, ByVal key As String) As M2MRelation 'Implements _IOrmBase.GetM2M
        '    Dim el As M2MRelation = Nothing
        '    Using GetSyncRoot()
        '        For Each rl As Relation In _relations
        '            Dim e As M2MRelation = TryCast(rl, M2MRelation)
        '            If e IsNot Nothing AndAlso M2MRelationDesc.CompareKeys(e.Key, key) Then
        '                If e.Relation.Type Is Nothing Then
        '                    If e.Relation.EntityName = MappingEngine.GetEntityNameByType(t) Then
        '                        el = e
        '                        Exit For
        '                    End If
        '                ElseIf e.Relation.Type Is t Then
        '                    el = e
        '                    Exit For
        '                End If
        '            End If
        '        Next
        '        If el Is Nothing Then
        '            el = New M2MRelation(Me, t, key)
        '            _relations.Add(el)
        '        End If
        '    End Using
        '    Return el
        'End Function

        Protected Sub _Add(ByVal obj As ICachedEntity) Implements IRelations.Add
            _Add(obj, Nothing)
        End Sub

        Protected Sub _Add(ByVal obj As ICachedEntity, ByVal key As String) Implements IRelations.Add
            Dim el As Relation = GetRelation(obj.GetType, key)
            Using el.SyncRoot
                If Not el.Added.Contains(obj) Then
                    If TypeOf el Is M2MRelation Then
                        Dim ke As IKeyEntity = CType(obj, IKeyEntity)
                        Dim el2 As M2MRelation = CType(ke.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                        SyncLock "1efb139gf8bh"
                            If Not el2.Added.Contains(Me) Then
                                If el.Deleted.Contains(obj) Then
                                    el.Deleted.Remove(obj)
                                    el2.Deleted.Remove(Me)
                                Else
                                    el.Add(obj)
                                    el2.Add(Me)
                                    Dim mc As OrmManager = GetCurrent()
                                    If mc IsNot Nothing Then
                                        mc.RaiseBeginUpdate(Me)
                                        mc.RaiseBeginUpdate(obj)
                                        mc.Cache.RaiseBeginUpdate(Me)
                                        mc.Cache.RaiseBeginUpdate(obj)
                                    Else
                                        Using gm As IGetManager = GetMgr()
                                            If gm IsNot Nothing Then
                                                gm.Manager.Cache.RaiseBeginUpdate(Me)
                                                gm.Manager.Cache.RaiseBeginUpdate(obj)
                                            End If
                                        End Using
                                    End If
                                End If
                            End If
                        End SyncLock
                    Else
                        SyncLock "1efb139gf8bh"
                            If el.Deleted.Contains(obj) Then
                                el.Deleted.Remove(obj)
                            Else
                                el.Add(obj)
                            End If
                            Dim mc As OrmManager = GetCurrent()
                            If mc IsNot Nothing Then
                                mc.RaiseBeginUpdate(Me)
                                mc.RaiseBeginUpdate(obj)
                                mc.Cache.RaiseBeginUpdate(Me)
                                mc.Cache.RaiseBeginUpdate(obj)
                            Else
                                Using gm As IGetManager = GetMgr()
                                    If gm IsNot Nothing Then
                                        gm.Manager.Cache.RaiseBeginUpdate(Me)
                                        gm.Manager.Cache.RaiseBeginUpdate(obj)
                                    End If
                                End Using
                            End If
                        End SyncLock
                    End If
                End If
            End Using
        End Sub

        'Protected Sub _Delete(ByVal t As Type) Implements IM2M.Delete
        '    Using mc As IGetManager = GetMgr
        '        mc.Manager.M2MDelete(_o, t, M2MRelation.DirKey)
        '    End Using
        'End Sub

        'Protected Sub _Delete(ByVal t As Type, ByVal key As String) Implements IM2M.Delete
        '    Using mc As IGetManager = GetMgr()
        '        Dim el As EditableListBase = GetM2M(t, key)

        '    End Using
        'End Sub

        Protected Sub _DeleteM2M(ByVal obj As ICachedEntity) Implements IRelations.Remove
            _DeleteM2M(obj, Nothing)
        End Sub

        Protected Sub _DeleteM2M(ByVal obj As ICachedEntity, ByVal key As String) Implements IRelations.Remove
            Dim el As Relation = GetRelation(obj.GetType, key)
            Using el.SyncRoot
                If Not el.Deleted.Contains(obj) Then
                    If TypeOf el Is M2MRelation Then
                        Dim ke As IKeyEntity = CType(obj, IKeyEntity)
                        Dim el2 As M2MRelation = CType(ke.GetRelation(New M2MRelationDesc(Me.GetType, key)), M2MRelation)
                        SyncLock "1efb139gf8bh"
                            If Not el2.Deleted.Contains(Me) Then
                                If el.Added.Contains(obj) Then
                                    el.Added.Remove(obj)
                                    el2.Added.Remove(Me)
                                Else
                                    el.Delete(obj)
                                    el2.Delete(Me)
                                    Dim mc As OrmManager = GetCurrent()
                                    If mc IsNot Nothing Then
                                        mc.RaiseBeginDelete(Me)
                                        mc.RaiseBeginDelete(obj)
                                        mc.Cache.RaiseBeginUpdate(Me)
                                        mc.Cache.RaiseBeginUpdate(obj)
                                    Else
                                        Using gm As IGetManager = GetMgr()
                                            If gm IsNot Nothing Then
                                                gm.Manager.Cache.RaiseBeginUpdate(Me)
                                                gm.Manager.Cache.RaiseBeginUpdate(obj)
                                            End If
                                        End Using
                                    End If
                                End If
                            End If
                        End SyncLock
                    Else
                        SyncLock "1efb139gf8bh"
                            If el.Added.Contains(obj) Then
                                el.Added.Remove(obj)
                            Else
                                el.Delete(obj)
                                Dim mc As OrmManager = GetCurrent()
                                If mc IsNot Nothing Then
                                    mc.RaiseBeginDelete(Me)
                                    mc.RaiseBeginDelete(obj)
                                    mc.Cache.RaiseBeginUpdate(Me)
                                    mc.Cache.RaiseBeginUpdate(obj)
                                Else
                                    Using gm As IGetManager = GetMgr()
                                        If gm IsNot Nothing Then
                                            gm.Manager.Cache.RaiseBeginUpdate(Me)
                                            gm.Manager.Cache.RaiseBeginUpdate(obj)
                                        End If
                                    End Using
                                End If
                            End If
                        End SyncLock
                    End If
                End If
            End Using
        End Sub

        Protected Sub _Cancel(ByVal en As String) Implements IRelations.Cancel
            _Cancel(en, Nothing)
        End Sub

        Protected Sub _Cancel(ByVal en As String, ByVal key As String) Implements IRelations.Cancel
#If OLDM2M Then
            Using mc As IGetManager = GetMgr()
                Dim el As Relation = GetRelation(en, key)
                el.Reject(mc.Manager)
            End Using
#Else
            Dim el As Relation = GetRelation(en, key)
            el.Reject(nothing)
#End If
        End Sub

        Protected Sub _Cancel(ByVal t As Type) Implements IRelations.Cancel
            _Cancel(t, Nothing)
        End Sub

        Protected Sub _Cancel(ByVal t As Type, ByVal key As String) Implements IRelations.Cancel
#If OLDM2M Then
            Using mc As IGetManager = GetMgr()
                Dim el As Relation = GetRelation(t, key)
                el.Reject(mc.Manager)
            End Using
#Else
            Dim el As Relation = GetRelation(t, key)
            el.Reject(nothing)
#End If
        End Sub

        Protected Sub _Cancel(ByVal desc As RelationDesc) Implements IRelations.Cancel
            If desc.Type Is Nothing Then
                _Cancel(desc.EntityName)
            Else
                _Cancel(desc.Type)
            End If
        End Sub

        Public ReadOnly Property Relations() As IRelations
            Get
                Return Me
            End Get
        End Property

        Public Function GetRelation(ByVal desc As RelationDesc) As Entities.Relation Implements IRelations.GetRelation
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    If rl.Relation.Equals(desc) Then
                        Return rl
                    End If
                Next
                Dim nrl As Relation = Nothing
                If TypeOf desc Is M2MRelationDesc Then
                    nrl = New M2MRelation(Me, CType(desc, M2MRelationDesc))
                Else
                    nrl = New Relation(Me, desc)
                End If
                _relations.Add(nrl)
                Return nrl
            End Using
        End Function

        Public Function GetRelation(ByVal en As String) As Entities.Relation Implements IRelations.GetRelation
            Return GetRelation(en, Nothing)
        End Function

        Public Function GetRelation(ByVal en As String, ByVal key As String) As Entities.Relation Implements IRelations.GetRelation
            Dim el As Relation = Nothing
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    Dim e As M2MRelation = TryCast(rl, M2MRelation)
                    If e IsNot Nothing AndAlso M2MRelationDesc.CompareKeys(e.Key, key) Then
                        If e.Relation.Type Is Nothing Then
                            If e.Relation.EntityName = en Then
                                el = e
                                Exit For
                            End If
                        ElseIf e.Relation.Type Is GetMappingEngine.GetTypeByEntityName(en) Then
                            el = e
                            Exit For
                        End If
                    Else
                        Dim r As Relation = TryCast(rl, Relation)
                        If r IsNot Nothing Then
                            If r.Relation.Type Is Nothing Then
                                If r.Relation.EntityName = en Then
                                    el = r
                                    Exit For
                                End If
                            ElseIf r.Relation.Type Is GetMappingEngine.GetTypeByEntityName(en) Then
                                el = r
                                Exit For
                            End If
                        End If
                    End If
                Next
                If el Is Nothing Then
                    Dim mpe As ObjectMappingEngine = GetMappingEngine()
                    Dim d As M2MRelationDesc = mpe.GetM2MRelation(Me.GetType, mpe.GetTypeByEntityName(en), key)
                    If d Is Nothing Then
                        el = New Relation(Me, en)
                    Else
                        el = New M2MRelation(Me, en, key)
                    End If
                    _relations.Add(el)
                End If
            End Using
            Return el
        End Function

        Public Function GetRelation(ByVal t As System.Type) As Entities.Relation Implements IRelations.GetRelation
            Return GetRelation(t, Nothing)
        End Function

        Public Function GetRelation(ByVal t As System.Type, ByVal key As String) As Entities.Relation Implements IRelations.GetRelation
            Dim el As Relation = Nothing
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    Dim e As M2MRelation = TryCast(rl, M2MRelation)
                    If e IsNot Nothing AndAlso M2MRelationDesc.CompareKeys(e.Key, key) Then
                        If e.Relation.Type Is Nothing Then
                            If e.Relation.EntityName = GetMappingEngine.GetEntityNameByType(t) Then
                                el = e
                                Exit For
                            End If
                        ElseIf e.Relation.Type Is t Then
                            el = e
                            Exit For
                        End If
                    Else
                        Dim r As Relation = TryCast(rl, Relation)
                        If r IsNot Nothing Then
                            If r.Relation.Type Is Nothing Then
                                If r.Relation.EntityName = GetMappingEngine.GetEntityNameByType(t) Then
                                    el = r
                                    Exit For
                                End If
                            ElseIf r.Relation.Type Is t Then
                                el = r
                                Exit For
                            End If
                        End If
                    End If
                Next
                If el Is Nothing Then
                    Dim d As M2MRelationDesc = GetMappingEngine.GetM2MRelation(Me.GetType, t, key)
                    If d Is Nothing Then
                        el = New Relation(Me, t)
                    Else
                        el = New M2MRelation(Me, t, key)
                    End If
                    _relations.Add(el)
                End If
            End Using
            Return el
        End Function

        Public Function GetRelationSchema(ByVal en As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Return GetRelationSchema(GetMappingEngine.GetTypeByEntityName(en))
        End Function

        Public Function GetRelationSchema(ByVal en As String, ByVal key As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Return GetRelationSchema(GetMappingEngine.GetTypeByEntityName(en), key)
        End Function

        Public Function GetRelationSchema(ByVal t As System.Type) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Return GetRelationSchema(t, Nothing)
        End Function

        Public Function GetRelationSchema(ByVal t As System.Type, ByVal key As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Dim s As ObjectMappingEngine = GetMappingEngine()
            Dim m2m As M2MRelationDesc = s.GetM2MRelation(Me.GetType, t, key)
            If m2m Is Nothing Then
                Throw New ArgumentException(String.Format("Invalid type {0} or key {1}", t.ToString, key))
            Else
                Return m2m
            End If
        End Function

        Public Function GetAllRelation() As System.Collections.Generic.IList(Of Entities.Relation) Implements IRelations.GetAllRelation
            Return _relations
        End Function

#End Region

    End Class

    <Serializable()> _
    Public MustInherit Class KeyEntity
        Inherits KeyEntityBase
        Implements IPropertyLazyLoad

        Protected Function Read(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return _Read(propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return _Read(propertyAlias, checkEntity)
        End Function

        Protected Function Write(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Write
            Return _Write(propertyAlias)
        End Function

        'Public Function Read() As System.IDisposable Implements IPropertyLazyLoad.Read

        'End Function

        'Public Function Write() As System.IDisposable Implements IPropertyLazyLoad.Write

        'End Function
        Public Shared Shadows Operator <>(ByVal obj1 As KeyEntity, ByVal obj2 As KeyEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Shadows Operator =(ByVal obj1 As KeyEntity, ByVal obj2 As KeyEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator
    End Class

    Public Enum ObjectState

        Created
        Modified
        ''' <summary>
        ''' Объект загружен из БД
        ''' </summary>
        None
        ''' <summary>
        ''' Попытка загрузить данный обьект из БД не удалась. Это может быть из-за того, что, например, он был удален.
        ''' </summary>
        NotFoundInSource
        ''' <summary>
        ''' Объект является копией редактируемого объекта
        ''' </summary>
        Clone
        Deleted
        ''' <summary>
        ''' Специальное состояние, между Created и None, когда объект ожидается что есть в базе, но еще не загружен
        ''' </summary>
        NotLoaded
    End Enum

    '    Public Module OrmBaseT
    '        Public Const PKName As String = "ID"
    '    End Module

    '    <Serializable()> _
    '    Public MustInherit Class OrmBaseT(Of T As {New, OrmBaseT(Of T)})
    '        Inherits KeyEntity
    '        Implements IComparable(Of T), IPropertyConverter, IComparable

    '        Private _id As Object

    '        Protected Sub New()

    '        End Sub

    '        Protected Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
    '            MyBase.New()
    '            MyBase.Init(id, cache, schema)
    '            'SetObjectStateClear(Orm.ObjectState.Created)
    '            'Throw New NotSupportedException
    '        End Sub

    '        Protected Overrides Sub Init(ByVal id As Object, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
    '            MyBase.Init(id, cache, schema)
    '        End Sub

    '        Protected Overrides Sub Init()
    '            MyBase.Init()
    '        End Sub
    '        'Protected Overridable Function GetNew() As T
    '        '    Return New T()
    '        'End Function

    '        Public Function CompareTo(ByVal other As T) As Integer Implements System.IComparable(Of T).CompareTo
    '            If other Is Nothing Then
    '                'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
    '                Return 1
    '            End If
    '            Return Math.Sign(ID - other.ID)
    '            'Return Math.Sign(Identifier - other.Identifier)
    '        End Function

    '        Public ReadOnly Property ID() As Integer 'Implements IOrmProxy(Of T).ID
    '            Get
    '                Return CInt(Identifier)
    '            End Get
    '        End Property

    '        ''' <summary>
    '        ''' Идентификатор объекта
    '        ''' </summary>
    '        ''' <remarks>Если производный класс имеет составной первичный ключ, это свойство лучше переопределить</remarks>
    '        <EntityPropertyAttribute(Propertyalias:=OrmBaseT.PKName, Behavior:=Field2DbRelations.PrimaryKey)> _
    '        Public Overrides Property Identifier() As Object
    '            Get
    '                'Using SyncHelper(True)
    '                Return _id
    '                'End Using
    '            End Get
    '            Set(ByVal value As Object)
    '                'Using SyncHelper(False)
    '                'If _id <> value Then
    '                'If _state = Orm.ObjectState.Created Then
    '                '    CreateModified(value, ModifiedObject.ReasonEnum.Unknown)
    '                'End If
    '                _id = value
    '                If Not CType(Me, _ICachedEntity).IsPKLoaded Then
    '                    PKLoaded(1)
    '                    Dim schema As ObjectMappingEngine = GetMappingEngine()
    '                    If schema IsNot Nothing Then
    '                        CType(Me, _ICachedEntity).SetLoaded(GetPKValues()(0).PropertyAlias, True, True, schema)
    '                    End If
    '                End If
    '                'Debug.Assert(_id.Equals(value))
    '                'End If
    '                'End Using
    '            End Set
    '        End Property

    '        Public Overrides Function GetPKValues() As PKDesc()
    '            Return New PKDesc() {New PKDesc(OrmBaseT.PKName, _id)}
    '        End Function

    '        Protected Overrides Sub SetPK(ByVal pk() As PKDesc, ByVal schema As ObjectMappingEngine)
    '            _id = pk(0).Value
    '        End Sub

    '        Protected Overrides Sub CopyBody(ByVal from As _IEntity, ByVal [to] As _IEntity)
    '            Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
    '            If editable IsNot Nothing Then
    '                editable.CopyBody(CType(from, T), CType([to], T))
    '            Else
    '                MyBase.CopyBody(from, [to])
    '            End If
    '        End Sub

    '        'Public Overridable Overloads Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object)
    '        '    MyBase.SetValue(pi, c, Nothing, value)
    '        'End Sub

    '        Public Overridable Overloads Function CreateObject(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal value As Object) As _IEntity Implements IPropertyConverter.CreateObject
    '            Return Nothing
    '        End Function

    '        Protected Overrides Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
    '            If propertyAlias = OrmBaseT.PKName Then
    '                Return True
    '            Else
    '                Return MyBase.IsPropertyLoaded(propertyAlias)
    '            End If
    '        End Function

    '#Region " Operators "

    '        Public Shared Operator <(ByVal obj1 As OrmBaseT(Of T), ByVal obj2 As OrmBaseT(Of T)) As Boolean
    '            If obj1 Is Nothing Then
    '                If obj2 Is Nothing Then
    '                    Return False
    '                End If
    '                Return True
    '            End If
    '            Return obj1.CompareTo(CType(obj2, T)) < 0
    '        End Operator

    '        Public Shared Operator >(ByVal obj1 As OrmBaseT(Of T), ByVal obj2 As OrmBaseT(Of T)) As Boolean
    '            If obj1 Is Nothing Then
    '                If obj2 Is Nothing Then
    '                    Return False
    '                End If
    '                Return False
    '            End If
    '            Return obj1.CompareTo(CType(obj2, T)) > 0
    '        End Operator

    '#End Region

    '        Protected Function _CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
    '            Return CompareTo(TryCast(obj, T))
    '        End Function
    '    End Class

End Namespace
