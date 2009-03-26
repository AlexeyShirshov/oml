Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Database.Storedprocs
    '<Flags()> _
    'Public Enum Dependency
    '    Update = 1
    '    InsertDelete = 2
    '    All = 3
    'End Enum

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

#Region " Classes "
        Class svp
            Private _name As String
            Private _s_updMethod As Reflection.MethodInfo
            Private _s_insDelMethod As Reflection.MethodInfo

            Private _updMethod As Reflection.MethodInfo
            Private _insDelMethod As Reflection.MethodInfo

            Public ReadOnly Property Name() As String
                Get
                    Return _name
                End Get
            End Property

            Public Sub New(ByVal name As String, ByVal upd As Reflection.MethodInfo, ByVal ins As Reflection.MethodInfo, ByVal c As OrmCache)
                _name = name
                _s_insDelMethod = ins
                _s_updMethod = upd

                AddHandler c.OnObjectUpdated, AddressOf ValidateSPOnUpdate
                AddHandler c.OnObjectAdded, AddressOf ValidateSPOnInsertDelete
                AddHandler c.OnObjectDeleted, AddressOf ValidateSPOnInsertDelete

            End Sub

            Public Property DynamicUpdateMethod() As Reflection.MethodInfo
                Get
                    Return _updMethod
                End Get
                Set(ByVal value As Reflection.MethodInfo)
                    _updMethod = value
                End Set
            End Property

            Public Property DynamicInsertDeleteMethod() As Reflection.MethodInfo
                Get
                    Return _insDelMethod
                End Get
                Set(ByVal value As Reflection.MethodInfo)
                    _insDelMethod = value
                End Set
            End Property

            Private Function NeedResetStatic(ByVal obj As ICachedEntity) As Boolean
                If _s_insDelMethod Is Nothing Then
                    Return True
                Else
                    Return CBool(_s_insDelMethod.Invoke(Nothing, Reflection.BindingFlags.Static, Nothing, New Object() {obj}, Nothing))
                End If
            End Function

            Private Function NeedResetDynamic(ByVal obj As ICachedEntity, ByVal par As IList(Of Object)) As Boolean
                If _insDelMethod IsNot Nothing Then
                    Return CBool(_insDelMethod.Invoke(Nothing, Reflection.BindingFlags.Static, Nothing, New Object() {par, obj}, Nothing))
                Else
                    Return False
                End If
            End Function

            Private Function NeedResetStatic(ByVal obj As ICachedEntity, ByVal fields As ICollection(Of String)) As Boolean
                If _s_updMethod Is Nothing Then
                    Return True
                Else
                    Return CBool(_s_updMethod.Invoke(Nothing, Reflection.BindingFlags.Static, Nothing, New Object() {obj, fields}, Nothing))
                End If
            End Function

            Private Function NeedResetDynamic(ByVal obj As ICachedEntity, ByVal fields As ICollection(Of String), ByVal par As IList(Of Object)) As Boolean
                If _updMethod IsNot Nothing Then
                    Return CBool(_updMethod.Invoke(Nothing, Reflection.BindingFlags.Static, Nothing, New Object() {par, obj, fields}, Nothing))
                Else
                    Return False
                End If
            End Function

            Private Function Validate(ByVal cache As OrmCache, ByVal dv As Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))), ByVal obj As ICachedEntity) As Boolean
                If NeedResetStatic(obj) Then
                    Dim k As String = ResetCache(cache, Nothing, Name)
                    If "all" = k Then
                        dv.Remove(Name)
                        Unsubscribe(cache)
                    End If
                    Return True
                End If

                Dim p As Pair(Of svp, Dictionary(Of String, IList(Of Object))) = Nothing
                Dim rv As Boolean
                If dv.TryGetValue(Name, p) Then
                    Dim l As New List(Of String)
                    For Each par As IList(Of Object) In p.Second.Values
                        If NeedResetDynamic(obj, par) Then
                            Dim k As String = ResetCache(cache, par, Name)
                            If "all" = k Then
                                dv.Remove(Name)
                                Unsubscribe(cache)
                                Return True
                            ElseIf Not String.IsNullOrEmpty(k) Then
                                l.Add(k)
                            End If
                        End If
                    Next
                    For Each k As String In l
                        p.Second.Remove(k)
                    Next
                    If p.Second.Count = 0 Then
                        dv.Remove(Name)
                        Unsubscribe(cache)
                        rv = True
                    End If
                End If
                Return rv
            End Function

            Private Function Validate(ByVal cache As OrmCache, ByVal dv As Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))), ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String)) As Boolean
                If NeedResetStatic(obj, fields) Then
                    Dim k As String = ResetCache(cache, Nothing, Name)
                    If "all" = k Then
                        dv.Remove(Name)
                        Unsubscribe(cache)
                    End If
                    Return True
                End If

                Dim p As Pair(Of svp, Dictionary(Of String, IList(Of Object))) = Nothing
                Dim rv As Boolean
                If dv.TryGetValue(Name, p) Then
                    Dim l As New List(Of String)
                    For Each par As IList(Of Object) In p.Second.Values
                        If NeedResetDynamic(obj, fields, par) Then
                            Dim k As String = ResetCache(cache, par, Name)
                            If "all" = k Then
                                dv.Remove(Name)
                                Unsubscribe(cache)
                                Return True
                            ElseIf Not String.IsNullOrEmpty(k) Then
                                l.Add(k)
                            End If
                        End If
                    Next
                    For Each k As String In l
                        p.Second.Remove(k)
                    Next
                    If p.Second.Count = 0 Then
                        dv.Remove(Name)
                        Unsubscribe(cache)
                        rv = True
                    End If
                End If
                Return rv
            End Function

            Private Sub Unsubscribe(ByVal c As OrmCache)
                RemoveHandler c.OnObjectUpdated, AddressOf ValidateSPOnUpdate
                RemoveHandler c.OnObjectAdded, AddressOf ValidateSPOnInsertDelete
                RemoveHandler c.OnObjectDeleted, AddressOf ValidateSPOnInsertDelete
            End Sub

            Public Shared Function ResetCache(ByVal c As CacheBase, ByVal params As IEnumerable(Of Object), ByVal name As String) As String
                Dim key As String = "StroredProcedure:" & name
                Dim dic As IDictionary = OrmReadOnlyDBManager._GetDic(c, key)
                If dic IsNot Nothing Then
                    If params Is Nothing Then
                        dic.Clear()
                    Else
                        Dim id As String = StoredProcBase.GetKey(params)
                        dic.Remove(id)
                        Return If(dic.Count = 0, "all", id)
                    End If
                    Return If(dic.Count = 0, "all", Nothing)
                End If
                Return Nothing
            End Function

            Private Sub ValidateSPOnInsertDelete(ByVal cache As OrmCache, ByVal obj As ICachedEntity)
#If DebugLocks Then
            SyncLock SyncHelper.AcquireDynamicLock("013hgoadngvb;kla","d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock("013hgoadngvb;kla")
#End If
                    Dim td As Dictionary(Of Type, List(Of svp)) = cache.GetExternalObject(Of Dictionary(Of Type, List(Of svp)))(StoreName & "$Dic")
                    Dim dv As Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))) = cache.GetExternalObject(Of Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))))(StoreName)
                    Dim l As List(Of svp) = Nothing
                    If td.TryGetValue(obj.GetType, l) Then
                        Dim toRemove As List(Of svp) = Nothing
                        For Each s As svp In l
                            If s.Validate(cache, dv, obj) Then
                                If toRemove Is Nothing Then
                                    toRemove = New List(Of svp)
                                End If
                                toRemove.Add(s)
                            End If
                        Next
                        If toRemove IsNot Nothing Then
                            For Each s As svp In toRemove
                                l.Remove(s)
                            Next
                        End If
                    End If
                End Using
            End Sub

            Private Sub ValidateSPOnUpdate(ByVal cache As OrmCache, ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
#If DebugLocks Then
            using SyncHelper.AcquireDynamicLock("013hgoadngvb;kla","d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock("013hgoadngvb;kla")
#End If
                    Dim td As Dictionary(Of Type, List(Of svp)) = cache.GetExternalObject(Of Dictionary(Of Type, List(Of svp)))(StoreName & "$Dic")
                    Dim dv As Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))) = cache.GetExternalObject(Of Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))))(StoreName)
                    Dim l As List(Of svp) = Nothing
                    If td.TryGetValue(obj.GetType, l) Then
                        Dim toRemove As List(Of svp) = Nothing
                        For Each s As svp In l
                            If s.Validate(cache, dv, obj, fields) Then
                                If toRemove Is Nothing Then
                                    toRemove = New List(Of svp)
                                End If
                                toRemove.Add(s)
                            End If
                        Next
                        If toRemove IsNot Nothing Then
                            For Each s As svp In toRemove
                                l.Remove(s)
                            Next
                        End If
                    End If
                End Using
            End Sub
        End Class

#End Region

        Protected MustOverride Function GetInParams() As IEnumerable(Of Pair(Of String, Object))
        Protected MustOverride Function GetOutParams() As IEnumerable(Of OutParam)
        Protected MustOverride Function GetName() As String
        Protected MustOverride Function Execute(ByVal mgr As OrmReadOnlyDBManager, ByVal cmd As System.Data.Common.DbCommand) As Object

        Public MustOverride ReadOnly Property ExecutionTime() As TimeSpan
        Public MustOverride ReadOnly Property FetchTime() As TimeSpan
        Public Const StoreName As String = "Worm.StoredP"
        Public Const DontNeedResetCacheOnUpdate As String = "DontNeedResetCacheOnUpdateMethod"
        Public Const DontNeedResetCacheOnInsertDelete As String = "DontNeedResetCacheOnInsertDeleteMethod"

        Private _cache As Boolean
        'Private _reseted As New Dictionary(Of String, Boolean)
        Private _expireDate As Date
        Private _cacheHit As Boolean
        Protected Shared _fromWeakList As New Dictionary(Of Type, Reflection.MethodInfo)
        Protected Shared _fromWeakListNP As New Dictionary(Of Type, Reflection.MethodInfo)
        Protected _clientPage As Paging
        Protected _pager As IPager
        Private _key As String

        'Public Enum ValidateResult
        '    DontReset
        '    ResetCache
        '    ResetAll
        'End Enum

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

        'Public ReadOnly Property IsReseted() As Boolean
        '    Get
        '        Dim r As Boolean
        '        _reseted.TryGetValue(GetKey, r)
        '        Return r
        '    End Get
        'End Property

        Protected Function GetKey() As String
            If String.IsNullOrEmpty(_key) Then
                Dim upd As String = Nothing, ins As String = Nothing
                Dim p As IList(Of Object) = ProvideDynamicValidateInfo(upd, ins)
                If p Is Nothing Then
                    Dim par As IEnumerable(Of Pair(Of String, Object)) = GetInParams()
                    If par IsNot Nothing Then
                        p = New List(Of Object)
                        For Each pr As Pair(Of String, Object) In par
                            p.Add(pr.Second)
                        Next
                    End If
                End If
                _key = GetKey(p)
            End If
            Return _key
        End Function

        Private Shared Function GetKey(ByVal params As IEnumerable(Of Object)) As String
            Dim sb As New StringBuilder
            If params IsNot Nothing Then
                For Each p As Object In params
                    If p IsNot Nothing Then
                        sb.Append(p.ToString).Append("$")
                    Else
                        sb.Append("null").Append("$")
                    End If
                Next
            End If
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
            '_reseted(GetKey) = False
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
                'If String.IsNullOrEmpty(id) Then id = "empty"
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
                            AddValidationInfo(sync, Me, mgr.Cache)
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

        Public Sub ResetCache(ByVal c As CacheBase)
            Dim key As String = "StroredProcedure:" & Name
            _key = Nothing
            Dim dic As IDictionary = OrmReadOnlyDBManager._GetDic(c, key)
            If dic IsNot Nothing Then
                Dim upd As String = Nothing, ins As String = Nothing
                Dim p As IList(Of Object) = ProvideDynamicValidateInfo(upd, ins)
                If p Is Nothing Then
                    Dim par As IEnumerable(Of Pair(Of String, Object)) = GetInParams()
                    If par IsNot Nothing Then
                        p = New List(Of Object)
                        For Each pr As Pair(Of String, Object) In par
                            p.Add(pr.Second)
                        Next
                    End If
                End If
                Dim id As String = StoredProcBase.GetKey(p)
                dic.Remove(id)
            End If
        End Sub

        'Public Sub ResetCache(ByVal c As CacheBase, ByVal r As ValidateResult)
        '    Dim key As String = "StroredProcedure:" & GetName()

        '    Dim dic As IDictionary = OrmReadOnlyDBManager._GetDic(c, key)
        '    If dic IsNot Nothing Then
        '        If r = ValidateResult.ResetAll Then
        '            For Each kv As DictionaryEntry In dic
        '                _reseted(CStr(kv.Key)) = True
        '            Next
        '            dic.Clear()
        '        Else
        '            Dim id As String = GetKey()
        '            If String.IsNullOrEmpty(id) Then id = "empty"
        '            dic.Remove(id)
        '            _reseted(id) = True
        '        End If
        '    End If
        'End Sub

        'Protected Overridable Function GetDepends() As IEnumerable(Of Pair(Of Type, Dependency))
        '    Dim l As New List(Of Pair(Of Type, Dependency))
        '    Return l
        'End Function

        'Public Overridable Function ValidateOnUpdate(ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String)) As ValidateResult
        '    If obj Is Nothing Then
        '        Throw New ArgumentNullException("obj")
        '    End If

        '    Dim t As Type = obj.GetType
        '    Dim en As IEnumerable(Of Pair(Of Type, Dependency)) = GetDepends()
        '    If en IsNot Nothing Then
        '        For Each p As Pair(Of Type, Dependency) In en
        '            If t Is p.First Then
        '                If (p.Second And Dependency.Update) = Dependency.Update Then
        '                    Return ValidateResult.ResetCache
        '                End If
        '            End If
        '        Next
        '    End If
        '    Return ValidateResult.DontReset
        'End Function

        'Public Overridable Function ValidateOnInsertDelete(ByVal obj As ICachedEntity) As ValidateResult
        '    If obj Is Nothing Then
        '        Throw New ArgumentNullException("obj")
        '    End If

        '    Dim t As Type = obj.GetType
        '    Dim en As IEnumerable(Of Pair(Of Type, Dependency)) = GetDepends()
        '    If en IsNot Nothing Then
        '        For Each p As Pair(Of Type, Dependency) In GetDepends()
        '            If t Is p.First Then
        '                If (p.Second And Dependency.InsertDelete) = Dependency.InsertDelete Then
        '                    Return ValidateResult.ResetCache
        '                End If
        '            End If
        '        Next
        '    End If
        '    Return ValidateResult.DontReset
        'End Function

        'Protected Friend Overridable Function GetTypesToValidate() As ICollection(Of Type)
        '    Return New Type() {}
        'End Function

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
            'If String.IsNullOrEmpty(id) Then id = "empty"
            Return key & id
        End Function

        'Private Sub AddStoredProcType(ByVal sp As StoredProcBase, ByVal t As Type, ByVal cache As CacheBase)
        '    Dim l As List(Of StoredProcBase) = cache.GetExternalObject(StoreName, _
        '        Function() New List(Of StoredProcBase))

        '    SyncLock l
        '        Dim pos As Integer = l.IndexOf(sp)
        '        If pos < 0 Then
        '            l.Add(sp)
        '        Else
        '            l(pos) = sp
        '        End If
        '    End SyncLock

        '    Dim c As OrmCache = TryCast(cache, OrmCache)
        '    If c IsNot Nothing Then
        '        AddHandler c.OnObjectUpdated, AddressOf sp.ValidateSPOnUpdate
        '        AddHandler c.OnObjectAdded, AddressOf sp.ValidateSPOnInsertDelete
        '        AddHandler c.OnObjectDeleted, AddressOf sp.ValidateSPOnInsertDelete
        '    End If
        'End Sub

        Protected Friend Sub AddValidationInfo(ByVal key As String, ByVal sp As StoredProcBase, ByVal cache As CacheBase)
            If sp.Cached Then
                Dim c As OrmCache = TryCast(cache, OrmCache)

                If c IsNot Nothing Then

                    Using SyncHelper.AcquireDynamicLock("013hgoadngvb;kla")
                        Dim l As Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))) = cache.GetExternalObject(StoreName, _
                            Function() New Dictionary(Of String, Pair(Of svp, Dictionary(Of String, IList(Of Object)))))

                        'SyncLock l
                        Dim k As Pair(Of svp, Dictionary(Of String, IList(Of Object))) = Nothing
                        If Not l.TryGetValue(sp.GetName, k) Then
                            Dim types() As Type = Nothing
                            Dim s As svp = sp.GetStaticValidate(types, c)
                            If s IsNot Nothing Then
                                k = New Pair(Of svp, Dictionary(Of String, IList(Of Object)))(s, New Dictionary(Of String, IList(Of Object)))
                                l(s.Name) = k
                                Dim td As Dictionary(Of Type, List(Of svp)) = cache.GetExternalObject(StoreName & "$Dic", _
                                    Function() New Dictionary(Of Type, List(Of svp)))
                                For Each t As Type In types
                                    Dim sl As List(Of svp) = Nothing
                                    If Not td.TryGetValue(t, sl) Then
                                        sl = New List(Of svp)
                                        td(t) = sl
                                    End If
                                    sl.Add(s)
                                Next
                            End If
                        End If
                        If k IsNot Nothing Then
                            Dim p As IList(Of Object) = Nothing
                            If Not k.Second.TryGetValue(sp.GetKey, p) Then
                                p = sp.GetDynamicValidate(k.First)
                                If p IsNot Nothing Then
                                    k.Second(sp.GetKey) = p
                                End If
                            End If
                        End If

                        'End SyncLock
                    End Using
                End If
            End If
        End Sub

        Private Function GetStaticValidate(ByRef types() As Type, ByVal c As OrmCache) As svp
            Dim upd As String = Nothing, ins As String = Nothing
            types = ProvideStaticValidateInfo(upd, ins)
            If types IsNot Nothing Then
                Dim u As Reflection.MethodInfo = Nothing
                If Not String.IsNullOrEmpty(upd) Then
                    If upd = DontNeedResetCacheOnUpdate Then
                        u = GetType(StoredProcBase).GetMethod(upd, Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase Or Reflection.BindingFlags.Public, Nothing, _
                            New Type() {GetType(_ICachedEntity), GetType(ICollection(Of String))}, Nothing)
                    Else
                        u = Me.GetType.GetMethod(upd, Reflection.BindingFlags.FlattenHierarchy Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase, Nothing, _
                            New Type() {GetType(_ICachedEntity), GetType(ICollection(Of String))}, Nothing)
                    End If
                    If u Is Nothing Then
                        Throw New InvalidOperationException("Method " & upd & " not found or has invalid signature")
                    End If
                End If

                Dim i As Reflection.MethodInfo = Nothing
                If Not String.IsNullOrEmpty(ins) Then
                    i = Me.GetType.GetMethod(ins, Reflection.BindingFlags.FlattenHierarchy Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase Or Reflection.BindingFlags.Public, Nothing, _
                        New Type() {GetType(ICachedEntity)}, Nothing)
                    If i Is Nothing Then
                        Throw New InvalidOperationException("Method " & ins & " not found or has invalid signature")
                    End If
                End If

                Return New svp(GetName, u, i, c)
            Else
                Return Nothing
            End If
        End Function

        Private Function GetDynamicValidate(ByVal s As svp) As IList(Of Object)
            Dim upd As String = Nothing, ins As String = Nothing
            Dim p As IList(Of Object) = ProvideDynamicValidateInfo(upd, ins)
            If p IsNot Nothing AndAlso p.Count > 0 Then
                Dim u As Reflection.MethodInfo = Me.GetType.GetMethod(upd, _
                    New Type() {GetType(IList(Of Object)), GetType(_ICachedEntity), GetType(ICollection(Of String))})
                If u Is Nothing Then
                    Throw New InvalidOperationException("Method " & upd & " not found or has invalid signature")
                End If

                Dim i As Reflection.MethodInfo = Me.GetType.GetMethod(ins, _
                    New Type() {GetType(IList(Of Object)), GetType(ICachedEntity)})
                If i Is Nothing Then
                    Throw New InvalidOperationException("Method " & ins & " not found or has invalid signature")
                End If

                s.DynamicUpdateMethod = u
                s.DynamicInsertDeleteMethod = i
            End If
            Return p
        End Function

        Protected Overridable Function ProvideStaticValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As Type()
            Return Nothing
        End Function

        Protected Overridable Function ProvideDynamicValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As IList(Of Object)
            Return Nothing
        End Function

        Public Shared Function DontNeedResetCacheOnUpdateMethod(ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String)) As Boolean
            Return False
        End Function

        Public Shared Function DontNeedResetCacheOnInsertDeleteMethod(ByVal obj As ICachedEntity) As Boolean
            Return False
        End Function

        'Private Sub ValidateSPByType(ByVal cache As OrmCache, ByVal t As Type, ByVal obj As ICachedEntity)
        '    Dim l As List(Of StoredProcBase) = cache.GetExternalObject(Of List(Of StoredProcBase))(StoreName)
        '    If l IsNot Nothing Then
        '        SyncLock l
        '            For Each sp As StoredProcBase In l
        '                Try
        '                    If Not sp.IsReseted Then
        '                        Dim r As StoredProcBase.ValidateResult = sp.ValidateOnInsertDelete(obj)
        '                        If r <> StoredProcBase.ValidateResult.DontReset Then
        '                            sp.ResetCache(cache, r)
        '                        End If
        '                    End If
        '                Catch ex As Exception
        '                    Throw New OrmCacheException(String.Format("Fail to validate sp {0}", sp.Name), ex)
        '                End Try
        '            Next
        '        End SyncLock
        '    End If
        'End Sub

        'Private Sub ValidateUpdateSPByType(ByVal cache As OrmCache, ByVal t As Type, ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
        '    Dim l As List(Of StoredProcBase) = cache.GetExternalObject(Of List(Of StoredProcBase))(StoreName)
        '    If l IsNot Nothing Then
        '        SyncLock l
        '            For Each sp As StoredProcBase In l
        '                If Not sp.IsReseted Then
        '                    Dim r As StoredProcBase.ValidateResult = sp.ValidateOnUpdate(obj, fields)
        '                    If r <> StoredProcBase.ValidateResult.DontReset Then
        '                        sp.ResetCache(cache, r)
        '                    End If
        '                End If
        '            Next
        '        End SyncLock
        '    End If
        'End Sub

    End Class

End Namespace