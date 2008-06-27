Imports Worm.Orm.Meta
Imports Worm.Cache

Namespace Orm

    Public Interface _IEntity
        Inherits IEntity
        Sub BeginLoading()
        Sub EndLoading()
        Sub Init(ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
        Function GetMgr() As IGetManager
    End Interface

    Public Interface IEntity
        Inherits ICloneable
        Sub SetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal schema As IOrmObjectSchemaBase, ByVal value As Object)
        Function GetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal schema As IOrmObjectSchemaBase) As Object
        Function GetSyncRoot() As IDisposable
        ReadOnly Property ObjectState() As ObjectState
    End Interface

    Public Interface _ICachedEntity
        Inherits ICachedEntity
        Sub PKLoaded()
        Sub SetLoaded(ByVal value As Boolean)
        Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean) As Boolean
        Function CheckIsAllLoaded(ByVal schema As QueryGenerator, ByVal loadedColumns As Integer) As Boolean
        ReadOnly Property IsPKLoaded() As Boolean
    End Interface

    Public Interface ICachedEntity
        Inherits _IEntity, IComparable, System.Xml.Serialization.IXmlSerializable
        ReadOnly Property Key() As Integer
        ReadOnly Property IsLoaded() As Boolean
        Sub CreateCopyForSaveNewEntry()
        Sub Load()
        Sub RemoveFromCache(ByVal cache As OrmCacheBase)
        Function GetPKValues() As Pair(Of String, Object)()
    End Interface

    Public Interface IOrmBase
        Inherits _ICachedEntity
        Overloads Sub Init(ByVal id As Object, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
        Property Identifier() As Object
    End Interface

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


        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs)
        Public Event PropertyChanged(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs)

#If DEBUG Then
        Protected Event ObjectStateChanged(ByVal oldState As ObjectState)
#End If


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

        Protected Friend Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
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

        Friend Overridable ReadOnly Property ObjName() As String
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & DumpState & "): "
            End Get
        End Property

        Protected Function DumpState() As String
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

        Protected Overridable Sub SetObjectState(ByVal value As ObjectState)
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
            Dim o As Entity = CType(Activator.CreateInstance(Me.GetType), Entity)
            o.Init(OrmCache, OrmSchema, _mgrStr)
            Using SyncHelper(True)
#If TraceSetState Then
                o.SetObjectState(ObjectState, ModifiedObject.ReasonEnum.Unknown, String.Empty, Nothing)
#Else
                o.SetObjectStateClear(_state)
#End If
                CopyBodyInternal(Me, o)
            End Using
            Return o
        End Function

        Protected Overridable Sub CopyBodyInternal(ByVal [from] As _IEntity, ByVal [to] As _IEntity)
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
    End Class

End Namespace