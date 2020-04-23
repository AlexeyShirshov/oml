Imports comp = System.ComponentModel
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Query
Imports System.Collections.Generic
Imports System.Linq
Imports System.Collections.Concurrent

Namespace Entities

    Public Class ObjectSavedArgs
        Inherits EventArgs

        Private ReadOnly _sa As OrmManager.SaveAction

        Public Sub New(ByVal saveAction As OrmManager.SaveAction)
            _sa = saveAction
        End Sub

        Public ReadOnly Property SaveAction() As OrmManager.SaveAction
            Get
                Return _sa
            End Get
        End Property
    End Class

    Public Class PropertyChangedEventArgs
        Inherits EventArgs

        Private ReadOnly _prev As Object
        Public ReadOnly Property PreviousValue() As Object
            Get
                Return _prev
            End Get
        End Property

        Private ReadOnly _current As Object
        Public ReadOnly Property CurrentValue() As Object
            Get
                Return _current
            End Get
        End Property

        Private ReadOnly _fieldName As String
        Public ReadOnly Property PropertyAlias() As String
            Get
                Return _fieldName
            End Get
        End Property

        Public Sub New(ByVal propertyAlias As String, ByVal prevValue As Object, ByVal currentValue As Object)
            _fieldName = propertyAlias
            _prev = prevValue
            _current = currentValue
        End Sub
    End Class

    Public Class LoadingWrapper
        Implements IDisposable

        Private ReadOnly _oldL As Boolean
        Private ReadOnly _e As _IEntity

        Public Sub New(ByVal o As Object)
            Dim e As _IEntity = TryCast(o, _IEntity)
            If e IsNot Nothing Then
                _oldL = e.IsLoading
                _e = e
                e.IsLoading = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _e IsNot Nothing Then _e.IsLoading = _oldL
        End Sub

    End Class

    Public Interface IOptimizedValues
        Function SetValueOptimized(ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal value As Object) As Boolean
        Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByRef found As Boolean) As Object
    End Interface
    Public Interface IOptimizeSetValue
        Delegate Sub SetValueDelegate(entity As Object, value As Object)
        Function GetOptimizedDelegate(ByVal propertyAlias As String) As SetValueDelegate
    End Interface

    Public Interface _IEntity
        Inherits IEntity
        Sub BeginLoading()
        Sub EndLoading()
        Sub Init(ByVal mpe As ObjectMappingEngine)
        Function GetMgr() As IGetManager
        ReadOnly Property ObjName() As String
        'Function SyncHelper(ByVal reader As Boolean, ByVal propertyAlias As String) As IDisposable
        Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean)
        Sub SetObjectState(ByVal o As ObjectState)
        Sub SetObjectStateClear(ByVal o As ObjectState)
        Sub SetCreateManager(ByVal createManager As ICreateManager)
        Property IsLoading() As Boolean
        Sub SetMgrString(ByVal str As String)
        Sub RaisePropertyChanged(ByVal propertyChangedEventArgs As PropertyChangedEventArgs)
    End Interface

    Public Interface IEntity
        Inherits comp.INotifyPropertyChanged
        ''' <summary>
        ''' Объект блокировки сущности
        ''' </summary>
        ''' <returns>Возвращает объект в конструкторе которого создана блокировка на сущность. Блокировка снимается в методе <see cref="IDisposable.Dispose"/></returns>
        ''' <remarks>Необходимо использовать блокировку при дуступе к внутреним метаданными сущности, таким как <see cref="IEntity.ObjectState"/></remarks>
        Function AcquareLock() As IDisposable
        ReadOnly Property ObjectState() As ObjectState
        'Function CreateClone() As Entity
        'Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity)
        'Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
        Property IsLoaded() As Boolean
        Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs)
        ReadOnly Property GetICreateManager() As ICreateManager
        ReadOnly Property GetDataContext() As IDataContext
        Event PropertyChangedEx(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs)
        Property SpecificMappingEngine() As ObjectMappingEngine
        Function GetMappingEngine() As ObjectMappingEngine
        Function GetEntitySchema(ByVal mpe As ObjectMappingEngine) As IEntitySchema
    End Interface

    Public Interface IPropertyLazyLoad
        Function Read(ByVal propertyAlias As String) As IDisposable
        Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
        Property IsLoaded() As Boolean
        Property IsPropertyLoaded(propertyAlias As String) As Boolean
        Property LazyLoadDisabled As Boolean
    End Interface

    Public Interface IUndoChanges
        Inherits _ICachedEntity
        Function Write(ByVal propertyAlias As String) As IDisposable
        ReadOnly Property DontRaisePropertyChange As Boolean
        Property OriginalCopy() As ICachedEntity
        'Property OldObjectState As ObjectState
        'Sub RemoveOriginalCopy(ByVal cache As CacheBase)
        'Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity
        'Overloads Sub RejectChanges(ByVal mgr As OrmManager)
        'ReadOnly Property HasChanges() As Boolean
        'ReadOnly Property HasBodyChanges() As Boolean
        'ReadOnly Property ChangeDescription() As String
        Event OriginalCopyRemoved(ByVal sender As ICachedEntity)
        Sub RaiseOriginalCopyRemoved()
        'Sub CreateCopyForSaveNewEntry(ByVal mgr As OrmManager, ByVal pk() As PKDesc)
    End Interface

    Public Interface _ICachedEntity
        Inherits ICachedEntity
        'Overloads Sub Init(ByVal pk As IEnumerable(Of PKDesc), ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
        Sub PKLoaded(ByVal pkCount As Integer, oschema As IPropertyMap)
        Sub PKLoaded(ByVal pkCount As Integer, propertyAlias As String)
        ReadOnly Property IsPKLoaded() As Boolean
        Property UpdateCtx() As UpdateCtx
        Function ForseUpdate(ByVal propertyAlias As String) As Boolean
        Function Save(ByVal mc As OrmManager) As Boolean
        Sub RejectChanges()
        Sub RaiseSaved(ByVal sa As OrmManager.SaveAction)
        Sub UpdateCacheAfterUpdate(ByVal c As OrmCache)
        Sub UpdateCache(ByVal mgr As OrmManager, ByVal oldObj As ICachedEntity)
        Sub FillChangedObjectList(ByVal objectList As Generic.List(Of _ICachedEntity))
        Function GetChangedObjectList() As Generic.List(Of _ICachedEntity)
    End Interface

    Public Interface IKeyProvider
        ReadOnly Property Key() As Integer
        ReadOnly Property UniqueString() As String
    End Interface

    Public Interface IOptimizePK
        Function GetPKValues() As IEnumerable(Of PKDesc)
        Sub SetPK(ByVal pk As IEnumerable(Of PKDesc))
    End Interface

    Public Interface ICopyProperties
        Sub CopyTo(ByVal dst As Object)
    End Interface

    Public Interface ICachedEntity
        Inherits _IEntity, IKeyProvider
        ''' <summary>
        ''' Возвращает массив полей и значений первичный ключей
        ''' </summary>
        ''' <returns>Массив полей и значений первичный ключей</returns>

        Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean
        Sub RejectRelationChanges(ByVal mc As OrmManager)
        Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mc As OrmManager)
        Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs)
        Event Added(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event Deleted(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event Updated(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event ChangesAccepted(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Event ChangesRejected(ByVal sender As ICachedEntity, ByVal args As EventArgs)
        Function BeginEdit() As IDisposable
        Function BeginAlter() As IDisposable
        Sub CheckEditOrThrow()
        Function Delete(ByVal mgr As OrmManager) As Boolean
        Sub RaiseChangesAccepted(ByVal args As EventArgs)
        Sub RaiseAdded(ByVal args As EventArgs)
        Sub RaiseDeleted(ByVal args As EventArgs)
        Sub RaiseUpdated(ByVal args As EventArgs)
        Sub RaiseChangesRejected(args As EventArgs)
    End Interface

    Public Interface ICachedEntityEx
        Inherits ICachedEntity, System.Xml.Serialization.IXmlSerializable

        Sub ValidateNewObject(ByVal mgr As OrmManager)
        Sub ValidateUpdate(ByVal mgr As OrmManager)
        Sub ValidateDelete(ByVal mgr As OrmManager)
        ReadOnly Property CustomProperties As ConcurrentDictionary(Of String, Object)
    End Interface

    Public Interface IEntityFactory
        Function CreateContainingEntity(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal values As IEnumerable(Of PKDesc)) As Object
        Function ExtractValues(mpe As ObjectMappingEngine, oschema As IEntitySchema, ByVal propertyAlias As String) As IEnumerable(Of PKDesc)
    End Interface

    Public Interface IStorageValueConverter
        Function CreateValue(ByVal oschema As IEntitySchema, ByVal m As MapField2Column, ByVal propertyAlias As String, column As String, ByVal value As Object) As Object
    End Interface

    Public Interface _ICachedEntityEx
        Inherits ICachedEntityEx, _ICachedEntity

        'Overloads Sub Init(ByVal pk() As PKDesc, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
    End Interface

    Public Interface IRelations
        Function GetCmd(ByVal t As Type) As Worm.Query.RelationCmd
        Function GetCmd(ByVal t As Type, ByVal key As String) As Worm.Query.RelationCmd
        Function GetCmd(ByVal entityName As String) As Worm.Query.RelationCmd
        Function GetCmd(ByVal entityName As String, ByVal key As String) As Worm.Query.RelationCmd
        Function GetCmd(ByVal desc As RelationDesc) As Worm.Query.RelationCmd

        ReadOnly Property HasChanges() As Boolean

        Sub Add(ByVal o As ICachedEntity)
        Sub Add(ByVal o As ICachedEntity, ByVal key As String)
        Sub Add(ByVal o As ICachedEntity, ByVal relation As Relation)
        Sub Remove(ByVal o As ICachedEntity)
        Sub Remove(ByVal o As ICachedEntity, ByVal key As String)
        Sub Remove(ByVal o As ICachedEntity, ByVal relation As Relation)

        Sub Cancel(ByVal t As Type)
        Sub Cancel(ByVal t As Type, ByVal key As String)
        Sub Cancel(ByVal t As Type, ByVal relation As Relation)
        Sub Cancel(ByVal en As String)
        Sub Cancel(ByVal en As String, ByVal key As String)
        Sub Cancel(ByVal en As String, ByVal relation As Relation)
        Sub Cancel(ByVal desc As RelationDesc)

        Function GetRelationDesc(ByVal t As Type) As RelationDesc
        Function GetRelationDesc(ByVal t As Type, ByVal key As String) As RelationDesc
        Function GetRelationDesc(ByVal en As String) As RelationDesc
        Function GetRelationDesc(ByVal en As String, ByVal key As String) As RelationDesc

        Function GetRelation(ByVal t As Type) As Relation
        Function GetRelation(ByVal t As Type, ByVal key As String) As Relation
        Function GetRelation(ByVal desc As RelationDesc) As Relation
        Function GetRelation(ByVal en As String) As Relation
        Function GetRelation(ByVal en As String, ByVal key As String) As Relation
        Function GetAllRelation() As Generic.IList(Of Relation)

        Function NormalizeRelation(ByVal oldRel As Relation, ByVal newRel As Relation, ByVal schema As ObjectMappingEngine) As Relation
    End Interface

    Public Interface ISinglePKEntity
        Inherits _ICachedEntityEx, IRelations
        'Shadows Sub Init(ByVal id As Object, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
        Property Identifier() As Object
#If OLDM2M Then
        Function GetOldName(ByVal id As Object) As String
#End If
        Function GetName() As String
        'Function Find(Of T As {New, IOrmBase})() As Worm.Query.QueryCmdBase
        'Function Find(Of T As {New, IOrmBase})(ByVal key As String) As Worm.Query.QueryCmdBase
    End Interface

    Public Interface _ISinglePKEntity
        Inherits ISinglePKEntity

#If OLDM2M Then
        Function AddAccept(ByVal acs As AcceptState2) As Boolean
        Function GetAccept(ByVal m As M2MCache) As AcceptState2
        Function GetM2M() As Generic.IList(Of AcceptState2)
#End If

        Sub RejectM2MIntermidiate()

    End Interface

    <Obsolete()>
    Public Interface IOrmEditable(Of T As {SinglePKEntity})
        Sub CopyBody(ByVal from As T, ByVal [to] As T)
    End Interface

#If OLDM2M Then
    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)> _
    Public Class AcceptState2
        'Public ReadOnly el As EditableList
        'Public ReadOnly sort As Sort
        'Public added As Generic.List(Of Integer)

        Private _key As String
        Private _id As String
        Private _e As M2MCache
        'Public Sub New(ByVal el As EditableList, ByVal sort As Sort, ByVal key As String, ByVal id As String)
        '    Me.el = el
        '    Me.sort = sort
        '    _key = key
        '    _id = id
        'End Sub

        Public ReadOnly Property CacheItem() As M2MCache
            Get
                Return _e
            End Get
        End Property

        Public ReadOnly Property el() As CachedM2MRelation
            Get
                Return _e.Entry
            End Get
        End Property

        Public Sub New(ByVal e As M2MCache, ByVal key As String, ByVal id As String)
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
            'For Each o As Pair(Of M2MCache, Pair(Of String, String)) In mgr.Cache.GetM2MEtries(obj, Nothing)
            '    Dim m As M2MCache = o.First
            '    If m.Entry.SubType Is el.SubType AndAlso m.Filter IsNot Nothing Then
            '        Dim dic As IDictionary = OrmManager.GetDic(mgr.Cache, o.Second.First)
            '        dic.Remove(o.Second.Second)
            '    End If
            'Next
            Return True
        End Function
    End Class
#End If

    Public Class UpdateCtx
        Public UpdatedFields As Generic.IList(Of Worm.Criteria.Core.EntityFilter)
        Public Relations As New Generic.List(Of M2MRelation)
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

    ''' <summary>
    ''' Обертка над массивом полей и значений
    ''' </summary>
    ''' <remarks>Реализует операции сравнения и получения хэш-кода для 
    ''' массива полей и значений <see cref="PKDesc"/></remarks>
    <Serializable()> _
    Public Class PKWrapper
        Implements IKeyProvider

        Private ReadOnly _id As IEnumerable(Of PKDesc)
        Private _str As String

        ''' <summary>
        ''' Инициализация объекта
        ''' </summary>
        ''' <param name="pk">Массив полей и значений</param>
        Public Sub New(ByVal pk As IEnumerable(Of PKDesc))
            _id = pk
        End Sub

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, PKWrapper))
        End Function

        Public Overloads Function Equals(ByVal obj As PKWrapper) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            Dim ids As IEnumerable(Of PKDesc) = obj._id
            If _id.Count <> ids.Count Then Return False
            For i As Integer = 0 To _id.Count - 1
                Dim p As PKDesc = _id(i)
                Dim find As Boolean
                For j As Integer = 0 To ids.Count - 1
                    Dim p2 As PKDesc = ids(j)
                    If p.Column = p2.Column Then
                        If p.Value.GetType IsNot p2.Value.GetType Then
                            Dim o As Object = Nothing, o2 As Object = p.Value
                            Try
                                o = Convert.ChangeType(p2.Value, p.Value.GetType)
                            Catch ex As InvalidCastException
                                Try
                                    o = Convert.ChangeType(p.Value, p2.Value.GetType)
                                    o2 = p2.Value
                                Catch
                                    Exit For
                                End Try
                            End Try
                            If o2.Equals(o) Then
                                find = True
                                Exit For
                            End If
                        Else
                            If p.Value.Equals(p2.Value) Then
                                find = True
                                Exit For
                            End If
                        End If
                    End If
                Next
                If Not find Then
                    Return False
                End If
            Next
            Return True
        End Function

        Public Overrides Function ToString() As String
            If String.IsNullOrEmpty(_str) Then
                Dim sb As New StringBuilder
                For Each pk As PKDesc In _id
                    sb.Append(pk.Column).Append(" = ").Append(pk.Value)
                Next
                _str = sb.ToString
            End If
            Return _str
        End Function

        Public ReadOnly Property Key() As Integer Implements IKeyProvider.Key
            Get
                Return GetHashCode()
            End Get
        End Property

        Public ReadOnly Property UniqueString() As String Implements IKeyProvider.UniqueString
            Get
                Return ToString()
            End Get
        End Property

        Public Function GetPKs() As IEnumerable(Of PKDesc)
            Return _id
        End Function
    End Class

    Public Interface ISaveBehavior
        ReadOnly Property HasNewObjects As Boolean
    End Interface
End Namespace

