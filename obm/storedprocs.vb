Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Orm
Imports Worm.Orm.Meta

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

        Private _cache As Boolean
        Private _reseted As New Dictionary(Of String, Boolean)
        Private _expireDate As Date
        Private _cacheHit As Boolean

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
            Dim schema As SQLGenerator = CType(mgr.ObjectSchema, SQLGenerator)
            Using cmd As System.Data.Common.DbCommand = schema.CreateDBCommand
                cmd.CommandType = System.Data.CommandType.StoredProcedure
                cmd.CommandText = GetName()
                InitInParams(schema, cmd)
                For Each p As OutParam In GetOutParams()
                    Dim pr As System.Data.Common.DbParameter = schema.CreateDBParameter()
                    pr.ParameterName = p.Name
                    pr.Direction = System.Data.ParameterDirection.Output
                    pr.DbType = p.DbType
                    pr.Size = p.Size
                    cmd.Parameters.Add(pr)
                Next
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
                            mgr.Cache.AddStoredProc(sync, Me)
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

        Public Sub ResetCache(ByVal c As OrmCacheBase, ByVal r As ValidateResult)
            Dim key As String = "StroredProcedure:" & GetName()

            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Dim dic As IDictionary = OrmReadOnlyDBManager._GetDic(c, key)
            If dic IsNot Nothing Then
                If r = ValidateResult.ResetAll Then
                    For Each kv As KeyValuePair(Of String, Boolean) In dic
                        _reseted(kv.Key) = True
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

        Public Overridable Function ValidateOnUpdate(ByVal obj As OrmBase, ByVal fields As ICollection(Of String)) As ValidateResult
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

        Public Overridable Function ValidateOnInsertDelete(ByVal obj As OrmBase) As ValidateResult
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
                For Each p As OutParam In GetOutParams()
                    out.Add(p.Name, cmd.Parameters(p.Name).Value)
                Next
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
            out.Add(New OutParam(outParamName, TypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParams As String) As Dictionary(Of String, Object)
            Dim ss() As String = outParams.Split(","c)
            Dim out As New List(Of OutParam)
            For Each pn As String In ss
                out.Add(New OutParam(pn, TypeConvertor.ToDbType(GetType(Integer)), 1000))
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
            out.Add(New OutParam(outParamName, TypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, ss, params, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function
    End Class

    Public MustInherit Class QueryStoredProcBase
        Inherits StoredProcBase

        Private _exec As TimeSpan
        Private _fecth As TimeSpan

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Public Sub New()
            MyBase.new(True)
        End Sub

        Protected MustOverride Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Protected MustOverride Function InitResult() As Object

        Protected Overridable Sub EndProcess(ByVal result As Object, ByVal mgr As OrmManagerBase)

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

    Public MustInherit Class QueryOrmStoredProcBase(Of T As {OrmBase, New})
        Inherits StoredProcBase

        Private _exec As TimeSpan
        Private _fecth As TimeSpan
        Private _count As Integer

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Sub New()
            MyBase.new(True)
        End Sub

        Protected MustOverride Function GetColumns() As List(Of ColumnAttribute)
        Protected MustOverride Function GetWithLoad() As Boolean

        Protected Overloads Overrides Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object
            'Dim mgr As OrmReadOnlyDBManager = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
            If mgr._externalFilter IsNot Nothing Then
                Throw New InvalidOperationException("External filter is not applicable for store procedures")
            End If
            Dim ce As New OrmManagerBase.CachedItem(Nothing, New ReadOnlyList(Of T)(mgr.LoadMultipleObjects(Of T)(cmd, GetWithLoad, Nothing, GetColumns)), mgr)
            _exec = ce.ExecutionTime
            _fecth = ce.FetchTime
            Return ce
        End Function

        Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As ReadOnlyList(Of T)
            Dim ce As OrmManagerBase.CachedItem = CType(MyBase.GetResult(mgr), OrmManagerBase.CachedItem)
            _count = ce.GetCount(mgr)
            Dim s As IListObjectConverter.ExtractListResult
            Dim r As ReadOnlyList(Of T) = ce.GetObjectList(Of T)(mgr, GetWithLoad, Not CacheHit, s)
            If s <> IListObjectConverter.ExtractListResult.Successed Then
                Throw New InvalidOperationException("External filter is not applicable for store procedures")
            End If
            Return r
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

        Protected Class QueryOrmStoredProcSimple(Of T2 As {OrmBase, New})
            Inherits QueryOrmStoredProcBase(Of T2)

            Private _name As String
            Private _obj() As Object
            Private _names() As String
            Private _cols() As String

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                MyClass.New(name, names, params, New String() {})
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyClass.New(name, names, params, New String() {}, cache)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyClass.New(name, names, params, New String() {}, timeout)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String)
                _name = name
                _obj = params
                _names = names
                _cols = columns
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String, ByVal cache As Boolean)
                MyBase.New(cache)
                _name = name
                _obj = params
                _names = names
                _cols = columns
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal columns() As String, ByVal timeout As TimeSpan)
                MyBase.New(timeout)
                _name = name
                _obj = params
                _names = names
                _cols = columns
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

            Protected Overrides Function GetColumns() As System.Collections.Generic.List(Of Orm.Meta.ColumnAttribute)
                Dim l As New List(Of ColumnAttribute)
                For Each c As String In _cols
                    l.Add(New ColumnAttribute(c))
                Next
                Return l
            End Function

            Protected Overrides Function GetWithLoad() As Boolean
                Return _cols.Length > 0
            End Function
        End Class

#Region " Exec "

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As ReadOnlyList(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return New QueryOrmStoredProcSimple(Of T)(name, ss, params, columns, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, timeout).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal cache As Boolean) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns, cache).GetResult(mgr)
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal columns() As String, ByVal timeout As TimeSpan) As ReadOnlyList(Of T)
            Return New QueryOrmStoredProcSimple(Of T)(name, New String() {}, New Object() {}, columns, timeout).GetResult(mgr)
        End Function

#End Region

    End Class

    Public MustInherit Class MultiResultsetQueryOrmStoredProcBase
        Inherits QueryStoredProcBase

#Region " Descriptors "

        Public Interface IResultSetDescriptor
            Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String)
            Sub EndProcess(ByVal mgr As OrmManagerBase)
        End Interface

        Public MustInherit Class OrmDescriptor(Of T As {OrmBase, New})
            Implements IResultSetDescriptor

            Private _l As List(Of T)
            Private _created As Boolean
            Private _ce As OrmManagerBase.CachedItem
            Private _count As Integer
            Private _loaded As Integer

            Public Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements IResultSetDescriptor.ProcessReader
                'Dim mgr As OrmReadOnlyDBManager = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
                If mgr._externalFilter IsNot Nothing Then
                    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                End If
                If _l Is Nothing Then
                    _l = New List(Of T)
                End If
                Dim dic As Generic.IDictionary(Of Integer, T) = mgr.GetDictionary(Of T)()
                Dim loaded As Integer
                mgr.LoadFromResultSet(Of T)(GetWithLoad, _l, GetColumns, dr, GetPrimaryKeyIndex, dic, loaded)
                _loaded += loaded
            End Sub

            Protected MustOverride Function GetColumns() As List(Of ColumnAttribute)
            Protected MustOverride Function GetWithLoad() As Boolean
            Protected MustOverride Function GetPrimaryKeyIndex() As Integer

            Public Function GetObjects(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of T)
                If _ce Is Nothing Then
                    Throw New InvalidOperationException("Stored procedure is not executed")
                End If
                _count = _ce.GetCount(mgr)
                Dim s As IListObjectConverter.ExtractListResult
                Dim r As ReadOnlyList(Of T) = _ce.GetObjectList(Of T)(mgr, GetWithLoad, _created, s)
                If s <> IListObjectConverter.ExtractListResult.Successed Then
                    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                End If
                Return r
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

            Public Sub EndProcess(ByVal mgr As OrmManagerBase) Implements IResultSetDescriptor.EndProcess
                _ce = New OrmManagerBase.CachedItem(Nothing, New ReadOnlyList(Of T)(_l), mgr)
                _l = Nothing
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

        Protected Overrides Sub EndProcess(ByVal result As Object, ByVal mgr As OrmManagerBase)
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
