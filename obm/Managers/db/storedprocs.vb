Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

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

    Public MustInherit Class NonQueryStoredProcBase
        Inherits StoredProcBase

        Private _exec As TimeSpan

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New()
            MyBase.new(False)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Overloads Overrides Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim et As New PerfCounter
            Dim r As Integer = cmd.ExecuteNonQuery()
            _exec = et.GetTime
            Dim out As New Dictionary(Of String, Object)
            If CheckForReturnValue(r) Then
                Dim op As IEnumerable(Of OutParam) = GetOutParams()
                If op IsNot Nothing Then
                    For Each p As OutParam In op
                        out.Add(p.Name, cmd.Parameters(p.Name).Value)
                    Next
                End If
            End If
            Return out
        End Function

        Protected Overridable Function CheckForReturnValue(ByVal returnValue As Integer) As Boolean
            Return True
        End Function

        Public Overrides ReadOnly Property ExecutionTime() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Public Overrides ReadOnly Property FetchTime() As System.TimeSpan
            Get
                Return Nothing
            End Get
        End Property

        Protected Class NonQueryStoredProcSimple
            Inherits NonQueryStoredProcBase

            Private _name As String
            Private _obj() As Object
            Private _names() As String
            Private _out As IEnumerable(Of OutParam)

#Region " Ctors "

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                _name = name
                _obj = params
                _names = names
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyBase.New(cache)
                _name = name
                _obj = params
                _names = names
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyBase.New(timeout)
                _name = name
                _obj = params
                _names = names
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal outParams As IEnumerable(Of OutParam))
                _name = name
                _obj = params
                _names = names
                _out = outParams
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean, ByVal outParams As IEnumerable(Of OutParam))
                MyBase.New(cache)
                _name = name
                _obj = params
                _names = names
                _out = outParams
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan, ByVal outParams As IEnumerable(Of OutParam))
                MyBase.New(timeout)
                _name = name
                _obj = params
                _names = names
                _out = outParams
            End Sub

#End Region

            Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
                Dim l As New List(Of Pair(Of String, Object))
                If _obj IsNot Nothing AndAlso _obj.Length > 0 Then
                    For i As Integer = 0 To _obj.Length - 1
                        l.Add(New Pair(Of String, Object)(_names(i), _obj(i)))
                    Next
                End If
                Return l
            End Function

            Protected Overrides Function GetName() As String
                Return _name
            End Function

            Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
                Return _out
            End Function
        End Class

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String)
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing)
            p.GetResult(mgr)
        End Sub

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params)
            p.GetResult(mgr)
        End Sub

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParamName As String) As T
            Dim out As New List(Of OutParam)
            out.Add(New OutParam(outParamName, DbTypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParams As String) As Dictionary(Of String, Object)
            Dim ss() As String = outParams.Split(","c)
            Dim out As New List(Of OutParam)
            For Each pn As String In ss
                out.Add(New OutParam(pn, DbTypeConvertor.ToDbType(GetType(Integer)), 1000))
            Next
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return dic
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParamName As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim out As New List(Of OutParam)
            out.Add(New OutParam(outParamName, DbTypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, ss, params, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function
    End Class

    Public MustInherit Class QueryStoredProcBase
        Inherits StoredProcBase

        Private _exec As TimeSpan
        Private _fecth As TimeSpan
        Private _out As Dictionary(Of String, Object)

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Public Sub New()
            MyBase.new(True)
        End Sub

        Public ReadOnly Property OutParams() As Dictionary(Of String, Object)
            Get
                Return _out
            End Get
        End Property

        Protected MustOverride Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Protected MustOverride Function InitResult() As Object

        Protected Overridable Sub EndProcess(ByVal result As Object, ByVal mgr As OrmManager)

        End Sub

        Protected Overloads Overrides Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim result As Object = InitResult()
            Dim et As New PerfCounter
            Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                _exec = et.GetTime
                Dim i As Integer = 0
                Dim ft As New PerfCounter
                Do While dr.Read
                    ProcessReader(mgr, i, dr, result)
                Loop
                Do While dr.NextResult()
                    i += 1
                    Do While dr.Read
                        ProcessReader(mgr, i, dr, result)
                    Loop
                Loop
                _fecth = ft.GetTime
                EndProcess(result, mgr)
                For Each p As OutParam In GetOutParams()
                    If _out Is Nothing Then
                        _out = New Dictionary(Of String, Object)
                    End If
                    _out.Add(p.Name, cmd.Parameters(p.Name).Value)
                Next
            End Using
            If result Is Nothing Then
                Throw New InvalidOperationException("result must be filled")
            End If
            Return result
        End Function

        Protected Overridable Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal resultSet As Integer, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            If resultSet = 0 Then
                ProcessReader(mgr, dr, result)
            Else
                Throw New NotImplementedException("I must implement custom logic to process results sets")
            End If
        End Sub

        Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
            Return New List(Of OutParam)
        End Function

        Public Overrides ReadOnly Property ExecutionTime() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Public Overrides ReadOnly Property FetchTime() As System.TimeSpan
            Get
                Return _fecth
            End Get
        End Property

        Class QueryStoredProcSimpleList(Of T)
            Inherits QueryStoredProcBase

            Private _name As String
            Private _obj() As Object
            Private _names() As String

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                _name = name
                _names = names
                _obj = params
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyBase.New(cache)
                _name = name
                _names = names
                _obj = params
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyBase.New(timeout)
                _name = name
                _names = names
                _obj = params
            End Sub

            Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
                Dim l As New List(Of Pair(Of String, Object))
                If _obj IsNot Nothing Then
                    For i As Integer = 0 To _obj.Length - 1
                        l.Add(New Pair(Of String, Object)(_names(i), _obj(i)))
                    Next
                End If
                Return l
            End Function

            Protected Overrides Function GetName() As String
                Return _name
            End Function

            Protected Overrides Function InitResult() As Object
                Return New List(Of T)
            End Function

            Protected Overloads Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
                Dim o As T = CType(dr.GetValue(0), T)
                Dim l As List(Of T) = CType(result, Global.System.Collections.Generic.List(Of T))
                l.Add(o)
            End Sub
        End Class

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing, cache).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing, timeout).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params, cache).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params, timeout).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function
    End Class

    Public MustInherit Class QueryOrmStoredProcBase(Of T As {_IEntity, New})
        Inherits StoredProcBase

        Private _exec As TimeSpan
        Private _fecth As TimeSpan
        Private _count As Integer
        Private _donthit As Boolean
        Private _out As Dictionary(Of String, Object)

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Sub New()
            MyBase.new(True)
        End Sub

        Public ReadOnly Property OutParams() As Dictionary(Of String, Object)
            Get
                Return _out
            End Get
        End Property

        Protected MustOverride Function GetColumns() As List(Of SelectExpression)
        Protected MustOverride Function GetWithLoad() As Boolean

        Protected Overloads Overrides Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object
            'Dim mgr As OrmReadOnlyDBManager = CType(OrmManager.CurrentManager, OrmReadOnlyDBManager)
            If mgr._externalFilter IsNot Nothing Then
                Throw New InvalidOperationException("External filter is not applicable for store procedures")
            End If
            _donthit = True
            'Dim ce As New CachedItem(Nothing, OrmManager.CreateReadonlyList(GetType(T), mgr.LoadMultipleObjects(Of T)(cmd, GetWithLoad, Nothing, GetColumns)), mgr)
            Dim rr As New List(Of T)
            mgr.LoadMultipleObjects(Of T)(cmd, rr, GetColumns)
            Dim l As IListEdit = OrmManager.CreateReadonlyList(GetType(T), rr)
            _exec = mgr.Exec 'ce.ExecutionTime
            _fecth = mgr.Fecth 'ce.FetchTime

            Dim wl As Object = Nothing
            If GetType(ICachedEntity).IsAssignableFrom(GetType(T)) Then
                wl = mgr.ListConverter.ToWeakList(l)
            Else
                wl = l
            End If

            For Each p As OutParam In GetOutParams()
                If _out Is Nothing Then
                    _out = New Dictionary(Of String, Object)
                End If
                _out.Add(p.Name, cmd.Parameters(p.Name).Value)
            Next
            Return wl
        End Function

        Public Shadows Function GetResult(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Return GetResult(CType(mgr, OrmReadOnlyDBManager))
            End Using
        End Function

        Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As ReadOnlyObjectList(Of T)
            'Dim ce As CachedItem = CType(MyBase.GetResult(mgr), CachedItem)
            '_count = ce.GetCount(mgr)
            Dim wl As Object = MyBase.GetResult(mgr)
            Dim tt As Type = GetType(T)
            If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                _count = mgr.ListConverter.GetCount(wl)
            Else
                _count = CType(wl, IList).Count
            End If
            mgr.RaiseOnDataAvailable(_count, _exec, _fecth, Not _donthit)
            If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                Dim mi As Reflection.MethodInfo = Nothing
                If Not _fromWeakList.TryGetValue(tt, mi) Then
                    Dim tmi As Reflection.MethodInfo = GetType(IListObjectConverter).GetMethod("FromWeakList", New Type() {GetType(Object), GetType(OrmManager)})
                    mi = tmi.MakeGenericMethod(New Type() {tt})
                    _fromWeakList(tt) = mi
                End If
                Return CType(mi.Invoke(mgr.ListConverter, New Object() {wl, mgr}), Global.Worm.ReadOnlyObjectList(Of T))
            Else
                Return CType(wl, Global.Worm.ReadOnlyObjectList(Of T))
            End If
            'Dim s As IListObjectConverter.ExtractListResult
            'Dim r As ReadOnlyObjectList(Of T) = Nothing
            'mgr.ListConverter.FromWeakList(wl, mgr) 'ce.GetObjectList(Of T)(mgr, GetWithLoad, Not CacheHit, s)
            'If s <> IListObjectConverter.ExtractListResult.Successed Then
            '    Throw New InvalidOperationException("External filter is not applicable for store procedures")
            'End If
            'Return r
        End Function

        Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
            Dim l As New List(Of Pair(Of Type, Dependency))
            l.Add(New Pair(Of Type, Dependency)(GetType(T), Dependency.All))
            Return l
        End Function

        Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
            Return New List(Of OutParam)
        End Function

        Public Overrides ReadOnly Property ExecutionTime() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Public Overrides ReadOnly Property FetchTime() As System.TimeSpan
            Get
                Return _fecth
            End Get
        End Property

        Public ReadOnly Property Count() As Integer
            Get
                Return _count
            End Get
        End Property

        Protected Class QueryOrmStoredProcSimple(Of T2 As {_IEntity, New})
            Inherits QueryOrmStoredProcBase(Of T2)

            Private _name As String
            Private _obj() As Object
            Private _names() As String
            Private _cols() As String
            Private _pk() As Integer

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                MyClass.New(name, names, params, New String() {}, New Integer() {})
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyClass.New(name, names, params, New String() {}, New Integer() {}, cache)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyClass.New(name, names, params, New String() {}, New Integer() {}, timeout)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String, ByVal pk() As Integer)
                _name = name
                _obj = params
                _names = names
                _cols = columns
                _pk = pk
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String, ByVal pk() As Integer, ByVal cache As Boolean)
                MyBase.New(cache)
                _name = name
                _obj = params
                _names = names
                _cols = columns
                _pk = pk
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String, ByVal pk() As Integer, ByVal timeout As TimeSpan)
                MyBase.New(timeout)
                _name = name
                _obj = params
                _names = names
                _cols = columns
                _pk = pk
            End Sub

            Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
                Dim l As New List(Of Pair(Of String, Object))
                For i As Integer = 0 To _obj.Length - 1
                    l.Add(New Pair(Of String, Object)(_names(i), _obj(i)))
                Next
                Return l
            End Function

            Protected Overrides Function GetName() As String
                Return _name
            End Function

            Protected Overrides Function GetColumns() As List(Of SelectExpression)
                Dim l As New List(Of SelectExpression)
                For i As Integer = 0 To _cols.Length - 1
                    Dim c As String = _cols(i)
                    Dim se As New SelectExpression(GetType(T2), c)
                    If Array.IndexOf(_pk, i) >= 0 Then
                        se.Attributes = Field2DbRelations.PK
                    End If
                    l.Add(se)
                Next
                Return l
            End Function

            Protected Overrides Function GetWithLoad() As Boolean
                Return _cols.Length > 0
            End Function
        End Class

#Region " Exec "

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns, pk).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns, pk, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyObjectList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns, pk, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns, pk).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer, ByVal cache As Boolean) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns, pk, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal pk() As Integer, ByVal timeout As TimeSpan) As ReadOnlyObjectList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns, pk, timeout).GetResult(mgr)
        End Function

#End Region

    End Class

    Public MustInherit Class MultiResultsetQueryOrmStoredProcBase
        Inherits QueryStoredProcBase

#Region " Descriptors "

        Public Interface IResultSetDescriptor
            Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String)
            Sub BeginProcess(ByVal mgr As OrmManager)
            Sub EndProcess(ByVal mgr As OrmManager)
        End Interface

        Public MustInherit Class OrmDescriptor(Of T As {_IEntity, New})
            Implements IResultSetDescriptor

            Private _l As List(Of T)
            Private _created As Boolean
            Private _o As Object
            Private _count As Integer
            Private _loaded As Integer
            Private _oschema As IEntitySchema
            'Private _props As IDictionary
            Private _cm As Collections.IndexedCollection(Of String, MapField2Column)

            Public Overridable Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements IResultSetDescriptor.ProcessReader
                'Dim mgr As OrmReadOnlyDBManager = CType(OrmManager.CurrentManager, OrmReadOnlyDBManager)
                If mgr._externalFilter IsNot Nothing Then
                    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                End If
                Dim original_type As Type = GetType(T)
                If _l Is Nothing Then
                    _l = New List(Of T)
                    _oschema = mgr.MappingEngine.GetEntitySchema(original_type)
                    '_props = mgr.MappingEngine.GetProperties(original_type, _oschema)
                    _cm = _oschema.GetFieldColumnMap
                End If
                'Dim dic As Generic.IDictionary(Of Object, T) = mgr.GetDictionary(Of T)()
                Dim dic As IDictionary = mgr.GetDictionary(original_type)
                Dim loaded As Integer
                Dim cols As IList(Of SelectExpression) = GetColumns() '.ConvertAll(Of SelectExpression)(Function(col As EntityPropertyAttribute) _
                'New SelectExpression(New ObjectSource(original_type), col.PropertyAlias))
                mgr.LoadFromResultSet(Of T)(_l, cols, dr, dic, loaded, _l.Count, _oschema, _cm)
                _loaded += loaded
            End Sub

            Protected MustOverride Function GetColumns() As List(Of SelectExpression)
            'Protected MustOverride Function GetWithLoad() As Boolean

            Public Function GetObjects(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
                If _o Is Nothing Then
                    Throw New InvalidOperationException("Stored procedure is not executed")
                End If
                Dim tt As Type = GetType(T)
                If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                    _count = mgr.ListConverter.GetCount(_o)
                    Dim mi As Reflection.MethodInfo = Nothing
                    If Not _fromWeakList.TryGetValue(tt, mi) Then
                        Dim tmi As Reflection.MethodInfo = GetType(IListObjectConverter).GetMethod("FromWeakList", New Type() {GetType(Object), GetType(OrmManager)})
                        mi = tmi.MakeGenericMethod(New Type() {tt})
                        _fromWeakList(tt) = mi
                    End If
                    Return CType(mi.Invoke(mgr.ListConverter, New Object() {_o, mgr}), Global.Worm.ReadOnlyObjectList(Of T))
                Else
                    Dim l As ReadOnlyObjectList(Of T) = CType(_o, Global.Worm.ReadOnlyObjectList(Of T))
                    _count = l.Count
                    Return l
                End If

                'Dim s As IListObjectConverter.ExtractListResult
                'Dim r As ReadOnlyList(Of T) = _ce.GetObjectList(Of T)(mgr, GetWithLoad, _created, s)
                'If s <> IListObjectConverter.ExtractListResult.Successed Then
                '    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                'End If
                'Return r
            End Function

            Public ReadOnly Property Count() As Integer
                Get
                    Return _count
                End Get
            End Property

            Public ReadOnly Property LoadedInResultset() As Integer
                Get
                    Return _loaded
                End Get
            End Property

            Public Overridable Sub EndProcess(ByVal mgr As OrmManager) Implements IResultSetDescriptor.EndProcess
                Dim l As ReadOnlyObjectList(Of T) = CType(OrmManager.CreateReadonlyList(GetType(T), _l), Global.Worm.ReadOnlyObjectList(Of T))
                If GetType(ICachedEntity).IsAssignableFrom(GetType(T)) Then
                    _o = mgr.ListConverter.ToWeakList(l)
                Else
                    _o = l
                End If
                _l = Nothing
            End Sub

            Public Overridable Sub BeginProcess(ByVal mgr As OrmManager) Implements IResultSetDescriptor.BeginProcess

            End Sub
        End Class

#End Region

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Sub New()
            MyBase.new(True)
        End Sub

        Protected MustOverride Function CreateDescriptor(ByVal resultsetIdx As Integer) As IResultSetDescriptor

        Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal resultSet As Integer, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Dim desc As List(Of IResultSetDescriptor) = CType(result, List(Of IResultSetDescriptor))
            Dim rd As IResultSetDescriptor = Nothing
            If desc.Count - 1 < resultSet Then
                rd = CreateDescriptor(resultSet)
                rd.BeginProcess(mgr)
                desc.Add(rd)
            Else
                rd = desc(resultSet)
            End If

            If rd Is Nothing Then
                Throw New InvalidOperationException(String.Format("Resultset descriptor for resultset #{0} is nothing", resultSet))
            End If

            rd.ProcessReader(mgr, dr, GetName)
        End Sub

        Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function InitResult() As Object
            Return New List(Of IResultSetDescriptor)
        End Function

        Public Overloads Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of IResultSetDescriptor)
            Return CType(MyBase.GetResult(mgr), List(Of IResultSetDescriptor))
        End Function

        Protected Overrides Sub EndProcess(ByVal result As Object, ByVal mgr As OrmManager)
            For Each d As IResultSetDescriptor In CType(result, IList)
                d.EndProcess(mgr)
            Next
        End Sub
    End Class

    Public MustInherit Class ScalarStoredProc(Of T As Structure)
        Inherits QueryStoredProcBase

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Sub New()
            MyBase.new(True)
        End Sub

        Protected Overrides Function InitResult() As Object
            Return New TypeWrap(Of T)(Nothing)
        End Function

        Protected Overloads Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            CType(result, TypeWrap(Of T)).Value = CType(Convert.ChangeType(dr.GetValue(0), GetType(T)), T)
        End Sub

        Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As T
            Return CType(MyBase.GetResult(mgr), TypeWrap(Of T)).Value
        End Function

        Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
            Return New List(Of Pair(Of String, Object))
        End Function

        Protected Class ScalarProcSimple(Of T2 As Structure)
            Inherits ScalarStoredProc(Of T2)

            Private _name As String
            Private _obj() As Object
            Private _names() As String

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                _name = name
                _obj = params
                _names = names
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyBase.New(cache)
                _name = name
                _obj = params
                _names = names
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyBase.New(timeout)
                _name = name
                _obj = params
                _names = names
            End Sub

            Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
                Dim l As New List(Of Pair(Of String, Object))
                If _obj IsNot Nothing Then
                    For i As Integer = 0 To _obj.Length - 1
                        l.Add(New Pair(Of String, Object)(_names(i), _obj(i)))
                    Next
                End If
                Return l
            End Function

            Protected Overrides Function GetName() As String
                Return _name
            End Function

            'Protected Overrides Sub InitInParams(ByVal schema As DbSchema, ByVal cmd As System.Data.Common.DbCommand)
            '    For Each p As Pair(Of String, Object) In GetInParams()
            '        Dim par As System.Data.Common.DbParameter = schema.CreateDBParameter
            '        par.Value = p.Second
            '        par.
            '        cmd.Parameters.Add(par)
            '    Next
            'End Sub
        End Class

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String) As T
            Return New ScalarProcSimple(Of T)(name, New String() {}, New Object() {}).GetResult(mgr)
        End Function

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean) As T
            Return New ScalarProcSimple(Of T)(name, New String() {}, New Object() {}, cache).GetResult(mgr)
        End Function

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan) As T
            Return New ScalarProcSimple(Of T)(name, New String() {}, New Object() {}, timeout).GetResult(mgr)
        End Function

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New ScalarProcSimple(Of T)(name, ss, params).GetResult(mgr)
        End Function

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New ScalarProcSimple(Of T)(name, ss, params, cache).GetResult(mgr)
        End Function

        Public Shared Shadows Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New ScalarProcSimple(Of T)(name, ss, params, timeout).GetResult(mgr)
        End Function
    End Class
End Namespace
