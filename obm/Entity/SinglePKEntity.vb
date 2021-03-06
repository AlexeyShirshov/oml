Imports System.Xml
Imports Worm.Query.Sorting
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Criteria.Core
Imports Worm.Query
Imports System.Linq
Imports System.Threading

#Const TraceSetState = False
#Const TraceRemoveCopy = True

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
    ''' ������� ����� ��� ���� �����
    ''' </summary>
    ''' <remarks>
    ''' ����� �������� ���������������� ��� �� ������ ��� � �� ������.
    ''' ������������� ��������� ����������:
    ''' XML ������������/��������������. ����������� � ���������� �������������. ��� ��������� ��������� ���������� �������������� <see cref="SinglePKEntity.ReadXml" /> � <see cref="SinglePKEntity.WriteXml"/>.
    ''' <code lang="vb">��� ���</code>
    ''' <example>��� ������</example>
    ''' </remarks>
    <Serializable()>
    Public MustInherit Class SinglePKEntityBase
        Inherits CachedEntity
        Implements _ISinglePKEntity

#Region " Classes "

#If OLDM2M Then
        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class M2MClass
            Private _o As SinglePKEntityBase

            Friend Sub New(ByVal o As SinglePKEntityBase)
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
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Class InternalClass2
            Inherits InternalClass

            Public Sub New(ByVal o As SinglePKEntityBase)
                MyBase.New(o)
            End Sub

            Public ReadOnly Property HasRelaionalChanges() As Boolean
                Get
                    Return CType(Obj, IRelations).HasChanges()
                End Get
            End Property
        End Class
#End Region

        <NonSerialized()>
        Friend _loading As Boolean

#If OLDM2M Then
        <NonSerialized()> _
        Protected Friend _needAccept As New Generic.List(Of AcceptState2)
#End If

        <NonSerialized()>
        Protected _saved As Boolean

#Region " Protected functions "

        'Protected Overrides Sub Init(ByVal pk As IPKDesc, ByVal cache As Cache.CacheBase, ByVal mpe As ObjectMappingEngine)
        '    Init(pk.First.Value, cache, mpe)
        'End Sub

        'Protected Overrides Sub Init()
        '    MyBase.Init()
        'End Sub

        'Protected Overridable Overloads Sub Init(ByVal id As Object, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) Implements _ISinglePKEntity.Init
        '    MyBase._Init(cache, mpe)
        '    Identifier = id
        '    PKLoaded(1, GetEntitySchema(mpe))
        'End Sub

        <Runtime.Serialization.OnDeserialized()>
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

        'Protected Overrides Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mgr As OrmManager)

        'End Sub

        Private Function GetName() As String Implements _ISinglePKEntity.GetName
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

        Public MustOverride Property Identifier() As Object Implements ISinglePKEntity.Identifier

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
                Return Equals(CType(obj, SinglePKEntityBase))
            Else
                Return False 'Identifier.Equals(obj)
            End If
        End Function

        'Public Overridable Overloads Function Equals(ByVal other_id As Integer) As Boolean
        '    Return Me.Identifier = other_id
        'End Function

        Public Overridable Overloads Function Equals(ByVal obj As SinglePKEntity) As Boolean
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

#End Region

        Public Shared Shadows Operator <>(ByVal obj1 As SinglePKEntityBase, ByVal obj2 As SinglePKEntityBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Shadows Operator =(ByVal obj1 As SinglePKEntityBase, ByVal obj2 As SinglePKEntityBase) As Boolean
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

        'Protected Overrides Sub InitNewEntity(ByVal mgr As OrmManager, ByVal en As Entity)
        '    If mgr Is Nothing Then
        '        CType(en, SinglePKEntityBase).Init(Identifier, Nothing, Nothing)
        '    Else
        '        CType(en, SinglePKEntityBase).Init(Identifier, mgr.Cache, mgr.MappingEngine)
        '    End If
        'End Sub

        'Protected Overrides Sub PKLoaded(ByVal pkCount As Integer, props As IPropertyMap)
        '    If pkCount <> 1 Then
        '        Throw New OrmObjectException(String.Format("SinglePKEntity derived class must have only one PK. The value is {0}", pkCount))
        '    End If
        '    MyBase.PKLoaded(pkCount, props)
        'End Sub


        Public Overrides ReadOnly Property UniqueString() As String
            Get
                Return "PK:" & Identifier.ToString
            End Get
        End Property

    End Class

    <Serializable()> _
    Public MustInherit Class SinglePKEntity
        Inherits SinglePKEntityBase
        Implements IPropertyLazyLoad, IUndoChanges

        Private _loaded As Boolean
        Private _loaded_members As IDictionary(Of String, Boolean)
        <NonSerialized()>
        Private ReadOnly _sl As New SpinLockRef

        <NonSerialized()> _
        Private _readRaw As Boolean
        <NonSerialized()> _
        Private _copy As ICachedEntity
        '<NonSerialized()> _
        'Private _old_state As ObjectState

        'Public Event ChangesAccepted(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements IUndoChanges.ChangesAccepted
        <NonSerialized()>
        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements IUndoChanges.OriginalCopyRemoved

        Protected Friend Sub RaiseCopyRemoved() Implements IUndoChanges.RaiseOriginalCopyRemoved
            RaiseEvent OriginalCopyRemoved(Me)
        End Sub

        Protected Function Read(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return OrmManager.RegisterRead(Me, propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return OrmManager.RegisterRead(Me, propertyAlias, checkEntity)
        End Function

        Protected Function Write(ByVal propertyAlias As String) As System.IDisposable Implements IUndoChanges.Write
            Return OrmManager.RegisterChange(Me, propertyAlias)
        End Function

        Public Shared Shadows Operator <>(ByVal obj1 As SinglePKEntity, ByVal obj2 As SinglePKEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Shadows Operator =(ByVal obj1 As SinglePKEntity, ByVal obj2 As SinglePKEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator

        'Protected Overrides Sub Init(ByVal id As Object, ByVal cache As Cache.CacheBase, ByVal mpe As ObjectMappingEngine)
        '    MyBase.Init(id, cache, mpe)
        'End Sub

        Protected Overrides Property IsLoaded As Boolean Implements IPropertyLazyLoad.IsLoaded
            Get
                Return _loaded
            End Get
            Set(ByVal value As Boolean)
                _loaded = value
            End Set
        End Property

        'Public Property PropertyLoadState As System.Collections.BitArray Implements IPropertyLazyLoad.PropertyLoadState
        '    Get
        '        Return _loaded_members
        '    End Get
        '    Set(ByVal value As System.Collections.BitArray)
        '        _loaded_members = value
        '    End Set
        'End Property

        Public Property LazyLoadDisabled As Boolean Implements IPropertyLazyLoad.LazyLoadDisabled
            Get
                Return _readRaw
            End Get
            Set(ByVal value As Boolean)
                _readRaw = value
            End Set
        End Property

        Public Overridable ReadOnly Property DontRaisePropertyChange As Boolean Implements IUndoChanges.DontRaisePropertyChange
            Get
                Return False
            End Get
        End Property

        'Protected Overridable Sub RemoveOriginalCopy(ByVal cache As CacheBase) Implements IUndoChanges.RemoveOriginalCopy
        '    _copy = Nothing
        'End Sub
#If TraceRemoveCopy Then
        Private _remCopyStack As String
#End If
        Protected Property OriginalCopy() As ICachedEntity Implements IUndoChanges.OriginalCopy
            Get
                Return _copy
            End Get
            Set(ByVal value As ICachedEntity)
#If TraceRemoveCopy Then
                If value Is Nothing AndAlso _copy IsNot Nothing Then
                    _remCopyStack = Environment.StackTrace
                End If
#End If
                _copy = value
            End Set
        End Property

        'Public Property OldObjectState As ObjectState Implements IUndoChanges.OldObjectState
        '    Get
        '        Return _old_state
        '    End Get
        '    Set(ByVal value As ObjectState)
        '        _old_state = value
        '    End Set
        'End Property


        Public Property IsPropertyLoaded(propertyAlias As String) As Boolean Implements IPropertyLazyLoad.IsPropertyLoaded
            Get
                Using New CoreFramework.CFThreading.CSScopeMgrLite(_sl)
                    If _loaded_members Is Nothing Then
                        _loaded_members = New Dictionary(Of String, Boolean)
                    End If

                    Dim v As Boolean = False

                    If _loaded_members.TryGetValue(propertyAlias, v) AndAlso v Then
                        Return True
                    End If

                    Return False
                End Using
            End Get
            Set(value As Boolean)
                Using New CoreFramework.CFThreading.CSScopeMgrLite(_sl)
                    If _loaded_members Is Nothing Then
                        _loaded_members = New Dictionary(Of String, Boolean)
                    End If

                    _loaded_members(propertyAlias) = value
                End Using
            End Set
        End Property
    End Class

    Public Enum ObjectState

        Created
        Modified
        ''' <summary>
        ''' ������ �������� �� ��
        ''' </summary>
        None
        ''' <summary>
        ''' ������� ��������� ������ ������ �� �� �� �������. ��� ����� ���� ��-�� ����, ���, ��������, �� ��� ������.
        ''' </summary>
        NotFoundInSource
        ' ''' <summary>
        ' ''' ������ �������� ������ �������������� �������
        ' ''' </summary>
        'Clone
        Deleted
        ''' <summary>
        ''' ����������� ���������, ����� Created � None, ����� ������ ��������� ��� ���� � ����, �� ��� �� ��������
        ''' </summary>
        NotLoaded
    End Enum

    Public Enum EntityState
        Flag_HasIdentity = 1
        Flag_NotExistsInStore = 2
        Flag_ExistsInStore = 4
        Flag_Modify = 8
        Flag_Delete = 16

        Created = 0
        NotExistsInStore = Flag_HasIdentity + Flag_NotExistsInStore
        ExistsInStore = Flag_HasIdentity + Flag_ExistsInStore
        Modifing = ExistsInStore + Flag_Modify
        Deleting = ExistsInStore + Flag_Delete
        Deleted = NotExistsInStore + Flag_Delete
        SyncStore = Flag_NotExistsInStore + Flag_ExistsInStore
    End Enum
    '    Public Module OrmBaseT
    '        Public Const PKName As String = "ID"
    '    End Module

    '    <Serializable()> _
    '    Public MustInherit Class OrmBaseT(Of T As {New, OrmBaseT(Of T)})
    '        Inherits SinglePKEntity
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
    '        ''' ������������� �������
    '        ''' </summary>
    '        ''' <remarks>���� ����������� ����� ����� ��������� ��������� ����, ��� �������� ����� ��������������</remarks>
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
