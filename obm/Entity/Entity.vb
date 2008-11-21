﻿Imports Worm.Orm.Meta
Imports Worm.Cache
Imports Worm.Query

Namespace Orm

    <Serializable()> _
    Public Class Entity
        Implements _IEntity

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
        Private _mgrStr As String
        <NonSerialized()> _
        Protected _dontRaisePropertyChange As Boolean
        <NonSerialized()> _
        Private _old_state As ObjectState
        <NonSerialized()> _
        Protected _readRaw As Boolean
        <NonSerialized()> _
        Private _cm As ICreateManager
        <NonSerialized()> _
        Protected _schema As ObjectMappingEngine

        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs) Implements IEntity.ManagerRequired
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
                'ElseIf ObjectState = Orm.ObjectState.Created Then
                '    _PrepareLoadingUpdate()
            End If
        End Sub

        Protected Overridable Sub _PrepareUpdate()

        End Sub

        Protected Overridable Sub _PrepareLoadingUpdate()

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

        Protected Function Read(ByVal fieldName As String) As IDisposable
            Return SyncHelper(True, fieldName)
        End Function

        Protected Function Write(ByVal fieldName As String) As IDisposable
            Return SyncHelper(False, fieldName)
        End Function

        Protected Function GetMgr() As IGetManager Implements _IEntity.GetMgr
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If Not String.IsNullOrEmpty(_mgrStr) Then
                Do While mgr IsNot Nothing AndAlso mgr.IdentityString <> _mgrStr
                    mgr = mgr._prev
                Loop
            End If
            If mgr Is Nothing Then
                If _cm Is Nothing Then
                    Dim a As New ManagerRequiredArgs
                    RaiseEvent ManagerRequired(Me, a)
                    mgr = a.Manager
                    If mgr Is Nothing Then
                        Return Nothing
                    Else
                        If a.DisposeMgr Then
                            Return New GetManagerDisposable(mgr, _schema)
                        Else
                            Return New ManagerWrapper(mgr, _schema)
                        End If
                    End If
                Else
                    Return New GetManagerDisposable(_cm.CreateManager, _schema)
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, _schema)
            End If
        End Function

        Protected ReadOnly Property OrmSchema() As ObjectMappingEngine
            Get
                Using mc As IGetManager = GetMgr()
                    If mc Is Nothing Then
                        Return Nothing
                    Else
                        Return mc.Manager.MappingEngine
                    End If
                End Using
            End Get
        End Property

        Protected ReadOnly Property OrmCache() As CacheBase
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
                If mc Is Nothing Then
                    sb.Append("Cannot get object dump")
                Else
                    Dim oschema As IObjectSchemaBase = mc.Manager.MappingEngine.GetObjectSchema(Me.GetType)
                    Dim olr As Boolean = _readRaw
                    _readRaw = True
                    Try
                        For Each kv As DictionaryEntry In mc.Manager.MappingEngine.GetProperties(Me.GetType)
                            Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                            Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                            sb.Append(c.FieldName).Append("=").Append(ObjectMappingEngine.GetFieldValue(Me, c.FieldName, pi, oschema)).Append(";")
                        Next
                    Finally
                        _readRaw = olr
                    End Try
                End If
            End Using
            Return sb.ToString
        End Function

        Public Function GetSyncRoot() As System.IDisposable Implements _IEntity.GetSyncRoot
            Return SyncHelper(False)
        End Function

        Public Function GetValue(ByVal fieldName As String) As Object
            Return GetValue(Nothing, New ColumnAttribute(fieldName), Nothing)
        End Function

        Public Overridable Function GetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal oschema As IObjectSchemaBase) As Object Implements IEntity.GetValueOptimized
            If pi Is Nothing Then
                Dim s As ObjectMappingEngine = OrmSchema
                If s Is Nothing Then
                    Return ObjectMappingEngine.GetFieldValueSchemaless(Me, c.FieldName, oschema, pi)
                Else
                    Return s.GetFieldValue(Me, c.FieldName, oschema, pi)
                End If
            End If
            Return pi.GetValue(Me, Nothing)
        End Function

        Protected ReadOnly Property ObjectState() As ObjectState Implements _IEntity.ObjectState
            Get
                Return _state
            End Get
        End Property

        Protected Sub SetObjectStateClear(ByVal value As ObjectState)
            _state = value
        End Sub

        Protected Overridable Sub SetObjectState(ByVal value As ObjectState) Implements _IEntity.SetObjectState
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

        Public Overridable Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal schema As IObjectSchemaBase, ByVal value As Object) Implements IEntity.SetValueOptimized

            If pi Is Nothing Then
                Using m As IGetManager = GetMgr()
                    pi = m.Manager.MappingEngine.GetProperty(Me.GetType, schema, c)
                End Using
            End If

            pi.SetValue(Me, value, Nothing)
        End Sub

        Protected Overridable Sub Init(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine, ByVal mgrIdentityString As String) Implements _IEntity.Init
            _mgrStr = mgrIdentityString

            If cache IsNot Nothing Then cache.RegisterCreation(Me)

            _state = Orm.ObjectState.Created
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

        Protected Overridable Sub CopyProperties(ByVal [from] As _IEntity, ByVal [to] As _IEntity, ByVal mgr As OrmManager, ByVal oschema As IObjectSchemaBase)
            For Each kv As DictionaryEntry In mgr.MappingEngine.GetProperties(Me.GetType)
                Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                [to].SetValueOptimized(pi, c, oschema, [from].GetValueOptimized(pi, c, oschema))
            Next
        End Sub

        Protected Overridable Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity) Implements IEntity.CopyBody
            Using mc As IGetManager = GetMgr()
                Dim oschema As IObjectSchemaBase = mc.Manager.MappingEngine.GetObjectSchema(Me.GetType)
                [to].BeginLoading()
                CopyProperties([from], [to], mc.Manager, oschema)
                [to].EndLoading()
            End Using
        End Sub

        Protected Function CreateClone() As Entity Implements IEntity.CreateClone
            Dim clone As Entity = CreateObject()
            clone.SetObjectState(Orm.ObjectState.NotLoaded)
            CopyBody(Me, clone)
            clone._old_state = ObjectState
            clone.SetObjectState(Orm.ObjectState.Clone)
            Return clone
        End Function

        Private Function GetOldState() As ObjectState Implements _IEntity.GetOldState
            Return _old_state
        End Function

        Protected Overridable Function IsFieldLoaded(ByVal fieldName As String) As Boolean Implements IEntity.IsFieldLoaded
            Return True
        End Function

        Protected Overridable ReadOnly Property IsLoaded() As Boolean Implements IEntity.IsLoaded
            Get
                Return True
            End Get
        End Property

        Protected Overridable Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean) Implements _IEntity.CorrectStateAfterLoading
            If objectWasCreated Then
                If ObjectState = Orm.ObjectState.Modified OrElse ObjectState = Orm.ObjectState.Created Then
                    If IsLoaded Then
                        SetObjectState(ObjectState.None)
                    Else
                        SetObjectState(ObjectState.NotLoaded)
                    End If
                    'ElseIf ObjectState = Orm.ObjectState.Created Then
                    '    Debug.Assert(Not IsLoaded)
                    '    SetObjectState(ObjectState.NotLoaded)
                ElseIf ObjectState = ObjectState.NotLoaded Then
                    If IsLoaded Then SetObjectState(ObjectState.None)
                ElseIf ObjectState = Orm.ObjectState.None Then
                Else
                    Debug.Assert(False)
                End If
            Else
                If ObjectState = ObjectState.NotLoaded AndAlso IsLoaded Then SetObjectState(ObjectState.None)
            End If
        End Sub

        Public Shared Function IsGoodState(ByVal state As ObjectState) As Boolean
            Return state = ObjectState.Modified OrElse state = ObjectState.Created 'OrElse state = ObjectState.Deleted
        End Function

        Public Sub SetCreateManager(ByVal createManager As ICreateManager) Implements _IEntity.SetCreateManager
            _cm = createManager
        End Sub

        Public ReadOnly Property CreateManager() As ICreateManager Implements IEntity.CreateManager
            Get
                Return _cm
            End Get
        End Property

#Region " CreateCmd "
        Public Function CreateCmd(ByVal table As SourceFragment) As QueryCmd
            If _cm IsNot Nothing Then
                Return New QueryCmd(table, _cm)
            Else
                Return QueryCmd.Create(table)
            End If
        End Function

        Public Function CreateCmd(ByVal selectType As Type) As QueryCmd
            If _cm IsNot Nothing Then
                Return New QueryCmd(selectType, _cm)
            Else
                Return QueryCmd.Create(selectType)
            End If
        End Function

        Public Function CreateCmdByEntityName(ByVal entityName As String) As QueryCmd
            If _cm IsNot Nothing Then
                Return New QueryCmd(entityName, _cm)
            Else
                Return QueryCmd.CreateByEntityName(entityName)
            End If
        End Function

        Public Function CreateCmd(ByVal name As String, ByVal table As SourceFragment) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(table, _cm)
                q.Name = name
                Return q
            Else
                Return QueryCmd.Create(name, table)
            End If
        End Function

        Public Function CreateCmd(ByVal name As String, ByVal selectType As Type) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(selectType, _cm)
                q.Name = name
                Return q
            Else
                Return QueryCmd.Create(name, selectType)
            End If
        End Function

        Public Function CreateCmdByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(entityName, _cm)
                q.Name = name
                Return q
            Else
                Return QueryCmd.CreateByEntityName(name, entityName)
            End If
        End Function
#End Region
    End Class

End Namespace