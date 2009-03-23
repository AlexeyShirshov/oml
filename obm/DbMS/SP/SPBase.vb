Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Database.Storedprocs
    <Flags()> _
    Public Enum Dependency
        Update = 1
        InsertDelete = 2
        All = 3
    End Enum

    Public Class OutParam
        Public ReadOnly Name As String
        Public ReadOnly DbType As System.Data.DbType
        Public ReadOnly Size As Integer

        Public Sub New(ByVal name As String, ByVal type As System.Data.DbType, ByVal size As Integer)
            Me.Name = name
            Me.DbType = type
            Me.Size = size
        End Sub
    End Class

    Public MustInherit Class StoredProcBase
        Protected MustOverride Function GetInParams() As IEnumerable(Of Pair(Of String, Object))
        Protected MustOverride Function GetOutParams() As IEnumerable(Of OutParam)
        Protected MustOverride Function GetName() As String
        Protected MustOverride Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object

        Public MustOverride ReadOnly Property ExecutionTime() As TimeSpan
        Public MustOverride ReadOnly Property FetchTime() As TimeSpan
        Public Const StoreName As String = "Worm.StoredP"

        Private _cache As Boolean
        Private _reseted As New Dictionary(Of String, Boolean)
        Private _expireDate As Date
        Private _cacheHit As Boolean
        Protected Shared _fromWeakList As New Dictionary(Of Type, Reflection.MethodInfo)
        Protected _clientPage As Paging
        Protected _pager As IPager

        Public Enum ValidateResult
            DontReset
            ResetCache
            ResetAll
        End Enum

        Protected Sub New(ByVal cache As Boolean)
            _cache = cache
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            _cache = True
            _expireDate = Now.Add(timeout)
        End Sub

        Public Property Pager() As IPager
            Get
                Return _pager
            End Get
            Set(ByVal value As IPager)
                _pager = value
            End Set
        End Property

        Public Property ClientPaging() As Paging
            Get
                Return _clientPage
            End Get
            Set(ByVal value As Paging)
                _clientPage = value
            End Set
        End Property

        Public ReadOnly Property Name() As String
            Get
                Return GetName()
            End Get
        End Property

        Public ReadOnly Property CacheHit() As Boolean
            Get
                Return _cacheHit
            End Get
        End Property

        Public Property Cached() As Boolean
            Get
                Return _cache
            End Get
            Set(ByVal value As Boolean)
                _cache = value
            End Set
        End Property

        Public ReadOnly Property IsReseted() As Boolean
            Get
                Dim r As Boolean
                _reseted.TryGetValue(GetKey, r)
                Return r
            End Get
        End Property

        Protected Function GetKey() As String
            Dim sb As New StringBuilder
            For Each p As Pair(Of String, Object) In GetInParams()
                If p.Second IsNot Nothing Then
                    sb.Append(p.Second.ToString).Append("$")
                Else
                    sb.Append("null").Append("$")
                End If
            Next
            If sb.Length = 0 Then
                Return "xxx"
            End If
            Return sb.ToString
        End Function

        Protected Overridable Sub InitInParams(ByVal schema As SQLGenerator, ByVal cmd As System.Data.Common.DbCommand)
            For Each p As Pair(Of String, Object) In GetInParams()
                cmd.Parameters.Add(schema.CreateDBParameter(p.First, p.Second))
            Next
        End Sub

        Protected Function Execute(ByVal mgr As OrmReadOnlyDBManager) As Object
            _reseted(GetKey) = False
            Dim schema As SQLGenerator = mgr.SQLGenerator
            Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand
                cmd.CommandType = System.Data.CommandType.StoredProcedure
                cmd.CommandText = GetName()
                InitInParams(schema, cmd)
                Dim op As IEnumerable(Of OutParam) = GetOutParams()
                If op IsNot Nothing Then
                    For Each p As OutParam In op
                        Dim pr As System.Data.Common.DbParameter = schema.CreateDBParameter()
                        pr.ParameterName = p.Name
                        pr.Direction = System.Data.ParameterDirection.Output
                        pr.DbType = p.DbType
                        pr.Size = p.Size
                        cmd.Parameters.Add(pr)
                    Next
                End If
                Dim b As OrmReadOnlyDBManager.ConnAction = mgr.TestConn(cmd)
                'Dim err As Boolean = True
                Try
                    Return Execute(mgr, cmd)
                Finally
                    mgr.CloseConn(b)
                End Try
            End Using
        End Function

        Public Function GetResult(ByVal getMgr As ICreateManager) As Object
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager, OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr)
                    Return GetResult(mgr)
                End Using
            End Using
        End Function

        Public Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As Object
            If _cache Then
                Dim key As String = "StroredProcedure:" & GetName()
                _cacheHit = True
                Dim id As String = GetKey()
                If String.IsNullOrEmpty(id) Then id = "empty"
                Dim dic As IDictionary = GetDic(mgr, key)
                Dim sync As String = key & id
                'Dim result As Object = GetFromCache(dic, id)
                Dim result As Object = dic(id)
                If result Is Nothing OrElse Expires() Then
                    Using SyncHelper.AcquireDynamicLock(sync)
                        'result = GetFromCache(dic, id)
                        result = dic(id)
                        If result Is Nothing OrElse Expires() Then
                            Expire()
                            AddStoredProc(sync, Me, mgr.Cache)
                            result = Execute(mgr)
                            'PutInCache(dic, id, result)
                            dic(id) = result
                            _cacheHit = False
                        End If
                    End Using
                End If
                Return result
            Else
                _cacheHit = False
                Return Execute(mgr)
            End If
        End Function

        Protected Overridable Function GetFromCache(ByVal dic As IDictionary, ByVal id As String) As Object
            Return dic(id)
        End Function

        Protected Overridable Sub PutInCache(ByVal dic As IDictionary, ByVal id As String, ByVal result As Object)
            dic(id) = result
        End Sub

        Public Function Expires() As Boolean
            If _expireDate <> Date.MinValue Then
                Return _expireDate < Now
            End If
            Return False
        End Function

        Public Sub Expire()
            _expireDate = Nothing
        End Sub

        Protected Overridable Function GetDic(ByVal mgr As OrmReadOnlyDBManager, ByVal key As String) As IDictionary
            Return mgr.GetDic(mgr.Cache, key)
        End Function

        Public Sub ResetCache(ByVal c As CacheBase, ByVal r As ValidateResult)
            Dim key As String = "StroredProcedure:" & GetName()

            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Dim dic As IDictionary = OrmReadOnlyDBManager._GetDic(c, key)
            If dic IsNot Nothing Then
                If r = ValidateResult.ResetAll Then
                    For Each kv As DictionaryEntry In dic
                        _reseted(CStr(kv.Key)) = True
                    Next
                    dic.Clear()
                Else
                    dic.Remove(id)
                    _reseted(id) = True
                End If
            End If
        End Sub

        Protected Overridable Function GetDepends() As IEnumerable(Of Pair(Of Type, Dependency))
            Dim l As New List(Of Pair(Of Type, Dependency))
            Return l
        End Function

        Public Overridable Function ValidateOnUpdate(ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String)) As ValidateResult
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            Dim en As IEnumerable(Of Pair(Of Type, Dependency)) = GetDepends()
            If en IsNot Nothing Then
                For Each p As Pair(Of Type, Dependency) In en
                    If t Is p.First Then
                        If (p.Second And Dependency.Update) = Dependency.Update Then
                            Return ValidateResult.ResetCache
                        End If
                    End If
                Next
            End If
            Return ValidateResult.DontReset
        End Function

        Public Overridable Function ValidateOnInsertDelete(ByVal obj As ICachedEntity) As ValidateResult
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            Dim en As IEnumerable(Of Pair(Of Type, Dependency)) = GetDepends()
            If en IsNot Nothing Then
                For Each p As Pair(Of Type, Dependency) In GetDepends()
                    If t Is p.First Then
                        If (p.Second And Dependency.InsertDelete) = Dependency.InsertDelete Then
                            Return ValidateResult.ResetCache
                        End If
                    End If
                Next
            End If
            Return ValidateResult.DontReset
        End Function

        Protected Friend Overridable Function GetTypesToValidate() As ICollection(Of Type)
            Return New Type() {}
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, StoredProcBase))
        End Function

        Public Overloads Function Equals(ByVal obj As StoredProcBase) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return GetSync() = obj.GetSync
        End Function

        Protected Function GetSync() As String
            Dim key As String = "StroredProcedure:" & GetName()
            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Return key & id
        End Function

        Private Sub AddStoredProcType(ByVal sp As StoredProcBase, ByVal t As Type, ByVal cache As CacheBase)
            Dim l As List(Of StoredProcBase) = cache.GetExternalObject(StoreName, _
                Function() New List(Of StoredProcBase))

            SyncLock l
                Dim pos As Integer = l.IndexOf(sp)
                If pos < 0 Then
                    l.Add(sp)
                Else
                    l(pos) = sp
                End If
            End SyncLock

            Dim c As OrmCache = TryCast(cache, OrmCache)
            If c IsNot Nothing Then
                AddHandler c.OnObjectUpdated, AddressOf sp.ValidateSPOnUpdate
                AddHandler c.OnObjectAdded, AddressOf sp.ValidateSPOnInsertDelete
                AddHandler c.OnObjectDeleted, AddressOf sp.ValidateSPOnInsertDelete
            End If
        End Sub

        Protected Friend Sub AddStoredProc(ByVal key As String, ByVal sp As StoredProcBase, ByVal cache As CacheBase)
            If sp.Cached Then
                Dim types As ICollection(Of Type) = sp.GetTypesToValidate
                If types IsNot Nothing AndAlso types.Count > 0 Then
                    For Each t As Type In types
                        AddStoredProcType(sp, t, cache)
                    Next
                Else
                    AddStoredProcType(sp, GetType(Object), cache)
                End If
            End If
        End Sub

        Private Sub ValidateSPByType(ByVal cache As OrmCache, ByVal t As Type, ByVal obj As ICachedEntity)
            Dim l As List(Of StoredProcBase) = cache.GetExternalObject(Of List(Of StoredProcBase))(StoreName)
            If l IsNot Nothing Then
                SyncLock l
                    For Each sp As StoredProcBase In l
                        Try
                            If Not sp.IsReseted Then
                                Dim r As StoredProcBase.ValidateResult = sp.ValidateOnInsertDelete(obj)
                                If r <> StoredProcBase.ValidateResult.DontReset Then
                                    sp.ResetCache(cache, r)
                                End If
                            End If
                        Catch ex As Exception
                            Throw New OrmCacheException(String.Format("Fail to validate sp {0}", sp.Name), ex)
                        End Try
                    Next
                End SyncLock
            End If
        End Sub

        Private Sub ValidateUpdateSPByType(ByVal cache As OrmCache, ByVal t As Type, ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
            Dim l As List(Of StoredProcBase) = cache.GetExternalObject(Of List(Of StoredProcBase))(StoreName)
            If l IsNot Nothing Then
                SyncLock l
                    For Each sp As StoredProcBase In l
                        If Not sp.IsReseted Then
                            Dim r As StoredProcBase.ValidateResult = sp.ValidateOnUpdate(obj, fields)
                            If r <> StoredProcBase.ValidateResult.DontReset Then
                                sp.ResetCache(cache, r)
                            End If
                        End If
                    Next
                End SyncLock
            End If
        End Sub

        Protected Sub ValidateSPOnInsertDelete(ByVal cache As OrmCache, ByVal obj As ICachedEntity)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("olnfv9807b45gnpoweg01j3g","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
#End If
                ValidateSPByType(cache, obj.GetType, obj)
                ValidateSPByType(cache, GetType(Object), obj)
            End Using
        End Sub

        Protected Friend Sub ValidateSPOnUpdate(ByVal cache As OrmCache, ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("olnfv9807b45gnpoweg01j3g","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
#End If
                ValidateUpdateSPByType(cache, obj.GetType, obj, fields)
                ValidateUpdateSPByType(cache, GetType(Object), obj, fields)
            End Using
        End Sub
    End Class

End Namespace