Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Database.Storedprocs

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
            Using dr As System.Data.Common.DbDataReader = mgr.ExecuteReaderCmd(cmd)
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

        MustInherit Class QueryStoredProcBaseList(Of T)
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
                        l.Add(New Pair(Of String, Object)(_names(i).Trim, _obj(i)))
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
        End Class

        Class QueryStoredProcSimpleList(Of T)
            Inherits QueryStoredProcBaseList(Of T)


            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object)
                MyBase.New(name, names, params)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean)
                MyBase.New(name, names, params, cache)
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan)
                MyBase.New(name, names, params, timeout)
            End Sub

            Protected Overloads Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
                Dim o As T = CType(dr.GetValue(0), T)
                Dim l As List(Of T) = CType(result, Global.System.Collections.Generic.List(Of T))
                l.Add(o)
            End Sub
        End Class

        Public Delegate Function TransformDataReaderDelegate(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader) As T

        Class QueryStoredProcList(Of T)
            Inherits QueryStoredProcBaseList(Of T)

            Private _d As TransformDataReaderDelegate(Of T)

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, transformDelegate As TransformDataReaderDelegate(Of T))
                MyBase.New(name, names, params)
                _d = transformDelegate
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal cache As Boolean, transformDelegate As TransformDataReaderDelegate(Of T))
                MyBase.New(name, names, params, cache)
                _d = transformDelegate
            End Sub

            Public Sub New(ByVal name As String, ByVal names() As String, ByVal params() As Object, ByVal timeout As TimeSpan, transformDelegate As TransformDataReaderDelegate(Of T))
                MyBase.New(name, names, params, timeout)
                _d = transformDelegate
            End Sub

            Protected Overloads Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
                Dim o As T = _d(mgr, dr)
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

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal timeout As TimeSpan) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing, timeout).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal cache As Boolean) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing, cache).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String) As List(Of T)
            Return CType(New QueryStoredProcSimpleList(Of T)(name, Nothing, Nothing).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
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

        Public Shared Function Exec(Of T)(getMgr As ICreateManager, ByVal name As String, ByVal cache As Boolean, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params, cache).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal timeout As TimeSpan, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params, timeout).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcSimpleList(Of T)(name, ss, params).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, cache, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal timeout As TimeSpan, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, timeout, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal cache As Boolean, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, cache, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, transformDelegate As TransformDataReaderDelegate(Of T)) As List(Of T)
            Return CType(New QueryStoredProcList(Of T)(name, Nothing, Nothing, timeout, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal cache As Boolean, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, cache, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal timeout As TimeSpan, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, timeout, transformDelegate).GetResult(mgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(getMgr As ICreateManager, ByVal name As String, ByVal cache As Boolean, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, cache, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal timeout As TimeSpan, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, timeout, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, transformDelegate As TransformDataReaderDelegate(Of T), ByVal paramNames As String, ByVal ParamArray params() As Object) As List(Of T)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Return CType(New QueryStoredProcList(Of T)(name, ss, params, transformDelegate).GetResult(getMgr), Global.System.Collections.Generic.List(Of T))
        End Function

    End Class
End Namespace