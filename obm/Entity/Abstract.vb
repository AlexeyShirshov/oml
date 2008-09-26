Imports Worm.Orm.Meta
Imports Worm.Cache

Namespace Orm
    Public Class ObjectSavedArgs
        Inherits EventArgs

        Private _sa As OrmManager.SaveAction

        Public Sub New(ByVal saveAction As OrmManager.SaveAction)
            _sa = saveAction
        End Sub

        Public ReadOnly Property SaveAction() As OrmManager.SaveAction
            Get
                Return _sa
            End Get
        End Property
    End Class


    Public Interface _IEntity
        Inherits IEntity
        Sub BeginLoading()
        Sub EndLoading()
        Sub Init(ByVal cache As OrmCacheBase, ByVal schema As ObjectMappingEngine, ByVal mgrIdentityString As String)
        Function GetMgr() As IGetManager
        ReadOnly Property ObjName() As String
        Function GetOldState() As ObjectState
        Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
        Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean)
        Sub SetObjectState(ByVal o As ObjectState)
        Sub SetCreateManager(ByVal createManager As ICreateManager)
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
        ReadOnly Property IsLoaded() As Boolean
        Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs)
        ReadOnly Property CreateManager() As ICreateManager
    End Interface

    Public Interface _ICachedEntity
        Inherits ICachedEntity
        Overloads Sub Init(ByVal pk() As PKDesc, ByVal cache As OrmCacheBase, ByVal schema As ObjectMappingEngine, ByVal mgrIdentityString As String)
        Sub PKLoaded(ByVal pkCount As Integer)
        Sub SetLoaded(ByVal value As Boolean)
        Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean
        Function CheckIsAllLoaded(ByVal schema As ObjectMappingEngine, ByVal loadedColumns As Integer) As Boolean
        ReadOnly Property IsPKLoaded() As Boolean
        ReadOnly Property UpdateCtx() As UpdateCtx
        Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean
        Sub RaiseCopyRemoved()
        Function Save(ByVal mc As OrmManager) As Boolean
        Sub RaiseSaved(ByVal sa As OrmManager.SaveAction)
    End Interface

    Public Interface ICachedEntity
        Inherits _IEntity, IComparable, System.Xml.Serialization.IXmlSerializable
        ReadOnly Property Key() As Integer
        ReadOnly Property OriginalCopy() As ICachedEntity
        Sub CreateCopyForSaveNewEntry(ByVal pk() As PKDesc)
        Sub Load()
        Sub RemoveFromCache(ByVal cache As OrmCacheBase)
        Sub UpdateCache()
        Function GetPKValues() As PKDesc()
        Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean
        Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity
        Sub RejectChanges()
        Sub RejectRelationChanges()
        ReadOnly Property HasChanges() As Boolean
        Function ValidateNewObject(ByVal mgr As OrmManager) As Boolean
        Function ValidateUpdate(ByVal mgr As OrmManager) As Boolean
        Function ValidateDelete(ByVal mgr As OrmManager) As Boolean
        ReadOnly Property ChangeDescription() As String
        Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs)
        Event Added(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event Deleted(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event Updated(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event OriginalCopyRemoved(ByVal sender As ICachedEntity)

    End Interface

    Public Interface IM2M
        Function Find(ByVal t As Type) As Worm.Query.QueryCmd
        Function Find(ByVal t As Type, ByVal key As String) As Worm.Query.QueryCmd
        Sub Add(ByVal o As _IOrmBase)
        Sub Add(ByVal o As _IOrmBase, ByVal key As String)
        Sub Delete(ByVal o As _IOrmBase)
        Sub Delete(ByVal o As _IOrmBase, ByVal key As String)
        'Sub Delete(ByVal t As Type)
        'Sub Delete(ByVal t As Type, ByVal key As String)
        Sub Cancel(ByVal t As Type)
        Sub Cancel(ByVal t As Type, ByVal key As String)
    End Interface

    Public Interface IOrmBase
        Inherits _ICachedEntity, IM2M
        Overloads Sub Init(ByVal id As Object, ByVal cache As OrmCacheBase, ByVal schema As ObjectMappingEngine, ByVal mgrIdentityString As String)
        Property Identifier() As Object
        Function GetOldName(ByVal id As Object) As String
        Function GetName() As String
        'Function Find(Of T As {New, IOrmBase})() As Worm.Query.QueryCmdBase
        'Function Find(Of T As {New, IOrmBase})(ByVal key As String) As Worm.Query.QueryCmdBase
    End Interface

    Public Interface _IOrmBase
        Inherits IOrmBase
        Function AddAccept(ByVal acs As AcceptState2) As Boolean
        Function GetAccept(ByVal m As OrmManager.M2MCache) As AcceptState2
        Function GetM2M(ByVal t As Type, ByVal key As String) As EditableListBase
        Function GetAllEditable() As Generic.IList(Of EditableListBase)
        Sub RejectM2MIntermidiate()
    End Interface

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)> _
        Public Class AcceptState2
        'Public ReadOnly el As EditableList
        'Public ReadOnly sort As Sort
        'Public added As Generic.List(Of Integer)

        Private _key As String
        Private _id As String
        Private _e As OrmManager.M2MCache
        'Public Sub New(ByVal el As EditableList, ByVal sort As Sort, ByVal key As String, ByVal id As String)
        '    Me.el = el
        '    Me.sort = sort
        '    _key = key
        '    _id = id
        'End Sub

        Public ReadOnly Property CacheItem() As OrmManager.M2MCache
            Get
                Return _e
            End Get
        End Property

        Public ReadOnly Property el() As EditableList
            Get
                Return _e.Entry
            End Get
        End Property

        Public Sub New(ByVal e As OrmManager.M2MCache, ByVal key As String, ByVal id As String)
            _e = e
            _key = key
            _id = id
        End Sub

        Public Function Accept(ByVal obj As IEntity, ByVal mgr As OrmManager) As Boolean
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
        Public Relations As New Generic.List(Of EditableListBase)
        Public Added As Boolean
        Public Deleted As Boolean
    End Class

    Public Class ManagerRequiredArgs
        Inherits EventArgs

        Private _mgr As OrmManager
        Public Property Manager() As OrmManager
            Get
                Return _mgr
            End Get
            Set(ByVal value As OrmManager)
                _mgr = value
            End Set
        End Property

        Private _notCreated As Boolean
        Public Property DisposeMgr() As Boolean
            Get
                Return Not _notCreated
            End Get
            Set(ByVal value As Boolean)
                _notCreated = Not value
            End Set
        End Property

    End Class
End Namespace

