Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Expressions2

Namespace Database.Storedprocs

    Public MustInherit Class QueryEntityStoredProcBase(Of T As {New, _IEntity})
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
            Dim cols As List(Of SelectExpression) = GetColumns()
            If cols Is Nothing OrElse cols.Count = 0 Then
                cols = New List(Of SelectExpression)
                'Dim pks As List(Of EntityPropertyAttribute) = mgr.MappingEngine.GetPrimaryKeys(GetType(T))
                Dim oschema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(GetType(T))
                For Each m As MapField2Column In oschema.FieldColumnMap
                    If m.IsPK Then
                        Dim exp As New TableExpression(m.SourceFieldExpression)
                        Dim se As New SelectExpression(exp, m.PropertyAlias, GetType(T))
                        se.Attributes = m.Attributes
                        se.CorrectFieldIndex = True
                        cols.Add(se)
                    End If
                Next
            End If
            mgr.LoadMultipleObjects(Of T)(cmd, rr, cols)
            Dim l As IListEdit = OrmManager._CreateReadOnlyList(GetType(T), rr)
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
            Using mgr As OrmManager = getMgr.CreateManager(Me)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return GetResult(CType(mgr, OrmReadOnlyDBManager))
                End Using
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
            Dim start As Integer = 0
            Dim length As Integer = Integer.MaxValue
            If _pager IsNot Nothing Then
                _pager.SetTotalCount(_count)
                start = _pager.GetCurrentPageOffset
                length = _pager.GetPageSize
            ElseIf Not _clientPage.IsEmpty Then
                start = _clientPage.Start
                length = _clientPage.Length
            End If
            If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                Dim mi As Reflection.MethodInfo = Nothing
                If Not _fromWeakList.TryGetValue(tt, mi) Then
                    'Dim pm As New Reflection.ParameterModifier(7)
                    'pm(6) = True
                    Dim tmi As Reflection.MethodInfo = GetType(IListObjectConverter).GetMethod("FromWeakList", _
                        New Type() {GetType(Object), GetType(OrmManager), GetType(Integer), GetType(Integer), GetType(Boolean), GetType(Boolean), Type.GetType("Worm.Cache.IListObjectConverter+ExtractListResult&")})
                    mi = tmi.MakeGenericMethod(New Type() {tt})
                    _fromWeakList(tt) = mi
                End If
                Return CType(mi.Invoke(mgr.ListConverter, New Object() {wl, mgr, start, length, GetWithLoad(), Not CacheHit, Nothing}), Global.Worm.ReadOnlyObjectList(Of T))
            Else
                Dim r As ReadOnlyObjectList(Of T) = CType(wl, Global.Worm.ReadOnlyObjectList(Of T))
                Return r.GetRange(start, Math.Min(r.Count - start, length))
            End If
            'Dim s As IListObjectConverter.ExtractListResult
            'Dim r As ReadOnlyObjectList(Of T) = Nothing
            'mgr.ListConverter.FromWeakList(wl, mgr) 'ce.GetObjectList(Of T)(mgr, GetWithLoad, Not CacheHit, s)
            'If s <> IListObjectConverter.ExtractListResult.Successed Then
            '    Throw New InvalidOperationException("External filter is not applicable for store procedures")
            'End If
            'Return r
        End Function

        'Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
        '    Dim l As New List(Of Pair(Of Type, Dependency))
        '    l.Add(New Pair(Of Type, Dependency)(GetType(T), Dependency.All))
        '    Return l
        'End Function

        Protected Overrides Function ProvideStaticValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As System.Type()
            Return New Type() {GetType(T)}
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
            Inherits QueryEntityStoredProcBase(Of T2)

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
End Namespace