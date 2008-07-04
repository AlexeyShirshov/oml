Imports Worm.Orm.Meta
Imports Worm.Cache

Namespace Orm

    Public Interface _IEntity
        Inherits IEntity
        Sub BeginLoading()
        Sub EndLoading()
        Sub Init(ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
        Function GetMgr() As IGetManager
        ReadOnly Property ObjName() As String
        Function GetOldState() As ObjectState
        Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
    End Interface

    Public Interface IEntity
        Inherits ICloneable
        Sub SetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal schema As IOrmObjectSchemaBase, ByVal value As Object)
        Function GetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal schema As IOrmObjectSchemaBase) As Object
        Function GetSyncRoot() As IDisposable
        ReadOnly Property ObjectState() As ObjectState
        Function CreateClone() As Entity
        Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity)
        Function IsFieldLoaded(ByVal fieldName As String) As Boolean
    End Interface

    Public Interface _ICachedEntity
        Inherits ICachedEntity
        Overloads Sub Init(ByVal pk() As Pair(Of String, Object), ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
        Sub PKLoaded(ByVal pkCount As Integer)
        Sub SetLoaded(ByVal value As Boolean)
        Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean) As Boolean
        Function CheckIsAllLoaded(ByVal schema As QueryGenerator, ByVal loadedColumns As Integer) As Boolean
        ReadOnly Property IsPKLoaded() As Boolean
        Sub CorrectStateAfterLoading()
        ReadOnly Property UpdateCtx() As UpdateCtx
        Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean
    End Interface

    Public Interface ICachedEntity
        Inherits _IEntity, IComparable, System.Xml.Serialization.IXmlSerializable
        ReadOnly Property Key() As Integer
        ReadOnly Property IsLoaded() As Boolean
        ReadOnly Property OriginalCopy() As ICachedEntity
        Sub CreateCopyForSaveNewEntry()
        Sub Load()
        Sub RemoveFromCache(ByVal cache As OrmCacheBase)
        Sub UpdateCache()
        Function GetPKValues() As Pair(Of String, Object)()
        Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean
        Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity
        Sub RejectChanges()
        Sub RejectRelationChanges()
    End Interface

    Public Interface IOrmBase
        Inherits _ICachedEntity
        Overloads Sub Init(ByVal id As Object, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
        Property Identifier() As Object
        Function GetOldName(ByVal id As Object) As String
        Function GetName() As String
        Function Find(Of T As {New, IOrmBase})() As Worm.Query.QueryCmd(Of T)
        Function Find(Of T As {New, IOrmBase})(ByVal key As String) As Worm.Query.QueryCmd(Of T)
        Function Find(ByVal t As Type) As Worm.Query.QueryCmdBase
        Function Find(ByVal t As Type, ByVal key As String) As Worm.Query.QueryCmdBase
    End Interface

    Public Interface _IOrmBase
        Inherits IOrmBase
        Function AddAccept(ByVal acs As AcceptState2) As Boolean
        Function GetAccept(ByVal m As OrmManagerBase.M2MCache) As AcceptState2
    End Interface

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)> _
        Public Class AcceptState2
        'Public ReadOnly el As EditableList
        'Public ReadOnly sort As Sort
        'Public added As Generic.List(Of Integer)

        Private _key As String
        Private _id As String
        Private _e As OrmManagerBase.M2MCache
        'Public Sub New(ByVal el As EditableList, ByVal sort As Sort, ByVal key As String, ByVal id As String)
        '    Me.el = el
        '    Me.sort = sort
        '    _key = key
        '    _id = id
        'End Sub

        Public ReadOnly Property CacheItem() As OrmManagerBase.M2MCache
            Get
                Return _e
            End Get
        End Property

        Public ReadOnly Property el() As EditableList
            Get
                Return _e.Entry
            End Get
        End Property

        Public Sub New(ByVal e As OrmManagerBase.M2MCache, ByVal key As String, ByVal id As String)
            _e = e
            _key = key
            _id = id
        End Sub

        Public Function Accept(ByVal obj As OrmBase, ByVal mgr As OrmManagerBase) As Boolean
            If _e IsNot Nothing Then
                Dim leave As Boolean = _e.Filter Is Nothing AndAlso _e.Entry.Accept(mgr)
                If Not leave Then
                    Dim dic As IDictionary = mgr.GetDic(mgr.Cache, _key)
                    dic.Remove(_id)
                End If
            End If
            'If el IsNot Nothing Then
            '    If Not el.Accept(mgr, Sort) Then
            '        Return False
            '    End If
            'End If
            'For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In mgr.Cache.GetM2MEtries(obj, Nothing)
            '    Dim m As OrmManagerBase.M2MCache = o.First
            '    If m.Entry.SubType Is el.SubType AndAlso m.Filter IsNot Nothing Then
            '        Dim dic As IDictionary = OrmManagerBase.GetDic(mgr.Cache, o.Second.First)
            '        dic.Remove(o.Second.Second)
            '    End If
            'Next
            Return True
        End Function
    End Class

    Public Class UpdateCtx
        Public UpdatedFields As Generic.IList(Of Worm.Criteria.Core.EntityFilterBase)
        Public Relation As M2MRelation
        Public Added As Boolean
        Public Deleted As Boolean
    End Class

    <Serializable()> _
    Public Class Entity
        Implements _IEntity

        Public Class ManagerRequiredArgs
            Inherits EventArgs

            Private _mgr As OrmManagerBase
            Public Property Manager() As OrmManagerBase
                Get
                    Return _mgr
                End Get
                Set(ByVal value As OrmManagerBase)
                    _mgr = value
                End Set
            End Property

        End Class

        Private Class ChangedEventHelper
            Implements IDisposable

            Private _value As Object
            Private _fieldName As String
            Private _obj As Entity
            Private _d As IDisposable

            Public Sub New(ByVal obj As Entity, ByVal fieldName As String, ByVal d As IDisposable)
                _fieldName = fieldName
                _obj = obj
                _value = obj.GetValue(fieldName)
                _d = d
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _d.Dispose()
                _obj.RaisePropertyChanged(_fieldName, _value)
            End Sub
        End Class

        Public Class PropertyChangedEventArgs
            Inherits EventArgs

            Private _prev As Object
            Public ReadOnly Property PreviousValue() As Object
                Get
                    Return _prev
                End Get
            End Property

            Private _current As Object
            Public ReadOnly Property CurrentValue() As Object
                Get
                    Return _current
                End Get
            End Property

            Private _fieldName As String
            Public ReadOnly Property FieldName() As String
                Get
                    Return _fieldName
                End Get
            End Property

            Public Sub New(ByVal fieldName As String, ByVal prevValue As Object, ByVal currentValue As Object)
                _fieldName = fieldName
                _prev = prevValue
                _current = currentValue
            End Sub
        End Class

        Private _state As ObjectState = ObjectState.Created

        <NonSerialized()> _
        Private _loading As Boolean
        <NonSerialized()> _
        Public _mgrStr As String
        <NonSerialized()> _
        Protected _dontRaisePropertyChange As Boolean
        <NonSerialized()> _
        Private _old_state As ObjectState


        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs)
        Public Event PropertyChanged(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs)

#If DEBUG Then
        Protected Event ObjectStateChanged(ByVal oldState As ObjectState)
#End If

        Protected ReadOnly Property IsLoading() As Boolean
            Get
                Return _loading
            End Get
        End Property

        Private Sub BeginLoading() Implements _IEntity.BeginLoading
            _loading = True
        End Sub

        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
#If DebugLocks Then
            Return New CSScopeMgr_Debug(Me, "d:\temp\")
#Else
            Return New CSScopeMgr(Me)
#End If
        End Function

        Protected Sub RaisePropertyChanged(ByVal fieldName As String, ByVal oldValue As Object)
            Dim value As Object = GetValue(fieldName)
            If Not Object.Equals(value, oldValue) Then
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(fieldName, oldValue, value))
            End If
        End Sub

        Protected Overridable Sub PrepareRead(ByVal fieldName As String, ByRef d As IDisposable)
        End Sub

        Protected Sub PrepareUpdate()
            If _state = Orm.ObjectState.Clone Then
                Throw New OrmObjectException(ObjName & ": Altering clone is not allowed")
            End If

            If _state = Orm.ObjectState.Deleted Then
                Throw New OrmObjectException(ObjName & ": Altering deleted object is not allowed")
            End If

            If Not _loading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
                If OrmCache Is Nothing Then
                    Return
                End If

                _PrepareUpdate()
            End If
        End Sub

        Protected Overridable Sub _PrepareUpdate()

        End Sub

        Protected Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable Implements _IEntity.SyncHelper
            Dim err As Boolean = True
            Dim d As IDisposable = New BlankSyncHelper(Nothing)
            Try
                If reader Then
                    PrepareRead(fieldName, d)
                Else
                    d = SyncHelper(True)
                    PrepareUpdate()
                    If Not _dontRaisePropertyChange AndAlso Not _loading Then
                        d = New ChangedEventHelper(Me, fieldName, d)
                    End If
                End If
                err = False
            Finally
                If err Then
                    If d IsNot Nothing Then d.Dispose()
                End If
            End Try

            Return d
        End Function

        Protected Function GetMgr() As IGetManager Implements _IEntity.GetMgr
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            If Not String.IsNullOrEmpty(_mgrStr) Then
                Do While mgr IsNot Nothing AndAlso mgr.IdentityString <> _mgrStr
                    mgr = mgr._prev
                Loop
            End If
            If mgr Is Nothing Then
                Dim a As New ManagerRequiredArgs
                RaiseEvent ManagerRequired(Me, a)
                mgr = a.Manager
                Return mgr
            Else
                Return New ManagerWrapper(mgr)
            End If
        End Function

        Protected ReadOnly Property OrmSchema() As QueryGenerator
            Get
                Using mc As IGetManager = GetMgr()
                    If mc Is Nothing Then
                        Return Nothing
                    Else
                        Return mc.Manager.ObjectSchema
                    End If
                End Using
            End Get
        End Property

        Friend ReadOnly Property OrmCache() As OrmCacheBase
            Get
                Using mc As IGetManager = GetMgr()
                    If mc IsNot Nothing Then
                        Return mc.Manager.Cache
                    Else
                        Return Nothing
                    End If
                End Using
            End Get
        End Property

        Private Sub EndLoading() Implements _IEntity.EndLoading
            _loading = False
        End Sub

        Protected Overridable ReadOnly Property ObjName() As String Implements _IEntity.ObjName
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & DumpState() & "): "
            End Get
        End Property

        Protected Overridable Function DumpState() As String
            Dim sb As New StringBuilder
            Using mc As IGetManager = GetMgr()
                For Each kv As DictionaryEntry In mc.Manager.ObjectSchema.GetProperties(Me.GetType)
                    Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                    Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                    sb.Append(c.FieldName).Append("=").Append(QueryGenerator.GetFieldValue(Me, c.FieldName, pi)).Append(";")
                Next
            End Using
            Return sb.ToString
        End Function

        Public Function GetSyncRoot() As System.IDisposable Implements _IEntity.GetSyncRoot
            Return SyncHelper(False)
        End Function

        Public Function GetValue(ByVal fieldName As String) As Object
            Return GetValue(Nothing, New ColumnAttribute(fieldName), Nothing)
        End Function

        Public Overridable Function GetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal oschema As IOrmObjectSchemaBase) As Object Implements IEntity.GetValue
            Dim s As QueryGenerator = OrmSchema
            If s Is Nothing Then
                Return QueryGenerator.GetFieldValueSchemaless(Me, c.FieldName, oschema, pi)
            Else
                Return s.GetFieldValue(Me, c.FieldName, oschema, pi)
            End If
        End Function

        Public ReadOnly Property ObjectState() As ObjectState Implements _IEntity.ObjectState
            Get
                Return _state
            End Get
        End Property

        Protected Sub SetObjectStateClear(ByVal value As ObjectState)
            _state = value
        End Sub

        Protected Friend Overridable Sub SetObjectState(ByVal value As ObjectState)
            Using SyncHelper(False)
                Debug.Assert(_state <> Orm.ObjectState.Deleted)
                If _state = Orm.ObjectState.Deleted Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                Dim olds As ObjectState = _state
                _state = value
                Debug.Assert(_state = value)

#If DEBUG Then
                RaiseEvent ObjectStateChanged(olds)
#End If
            End Using
        End Sub

        Public Overridable Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal schema As IOrmObjectSchemaBase, ByVal value As Object) Implements IEntity.SetValue
            If pi Is Nothing Then
                Throw New ArgumentNullException("pi")
            End If

            pi.SetValue(Me, value, Nothing)
        End Sub

        Protected Overridable Sub Init(ByVal cache As Cache.OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String) Implements _IEntity.Init
            _mgrStr = mgrIdentityString

            If cache IsNot Nothing Then cache.RegisterCreation(Me)

            _state = Orm.ObjectState.NotLoaded
        End Sub

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim o As Entity = CreateObject()
            Using SyncHelper(True)
#If TraceSetState Then
                o.SetObjectState(ObjectState, ModifiedObject.ReasonEnum.Unknown, String.Empty, Nothing)
#Else
                o.SetObjectStateClear(_state)
#End If
                CopyBody(Me, o)
            End Using
            Return o
        End Function

        Protected Overridable Function CreateObject() As Entity
            Using gm As IGetManager = GetMgr()
                Return CType(gm.Manager.CreateEntity(Me.GetType), Entity)
            End Using
        End Function

        Protected Overridable Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity) Implements IEntity.CopyBody
            Using mc As IGetManager = GetMgr()
                Dim oschema As IOrmObjectSchemaBase = mc.Manager.ObjectSchema.GetObjectSchema(Me.GetType)
                [to].BeginLoading()
                For Each kv As DictionaryEntry In mc.Manager.ObjectSchema.GetProperties(Me.GetType)
                    Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                    Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                    [to].SetValue(pi, c, oschema, [from].GetValue(pi, c, oschema))
                Next
                [to].EndLoading()
            End Using
        End Sub

        Protected Function CreateClone() As Entity Implements IEntity.CreateClone
            Dim clone As Entity = CreateObject()
            CopyBody(Me, clone)
            clone._old_state = ObjectState
            clone.SetObjectState(Orm.ObjectState.Clone)
            Return clone
        End Function

        Private Function GetOldState() As ObjectState Implements _IEntity.GetOldState
            Return _old_state
        End Function

        Public Overridable Function IsFieldLoaded(ByVal fieldName As String) As Boolean Implements IEntity.IsFieldLoaded
            Return True
        End Function
    End Class

End Namespace