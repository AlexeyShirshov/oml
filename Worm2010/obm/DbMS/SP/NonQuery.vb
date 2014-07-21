Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Database.Storedprocs

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
                        l.Add(New Pair(Of String, Object)(_names(i).Trim, _obj(i)))
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

        Public Shared Sub Exec(ByVal getMgr As ICreateManager, ByVal name As String)
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Exec(mgr, name)
                End Using
            End Using
        End Sub

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String)
            'Using New SetManagerHelper(mgr, mgr.GetCreateManager, Nothing)
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing)
            p.GetResult(mgr)
            'End Using
        End Sub

        Public Shared Sub Exec(ByVal getMgr As ICreateManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object)
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Exec(mgr, name, paramNames, params)
                End Using
            End Using
        End Sub

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params)
            p.GetResult(mgr)
        End Sub

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParamName As String) As T
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return Exec(Of T)(mgr, name, outParamName)
                End Using
            End Using
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParamName As String) As T
            Dim out As New List(Of OutParam)
            out.Add(New OutParam(outParamName, DbTypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function

        Public Shared Function Exec(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParams As String) As Dictionary(Of String, Object)
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return Exec(mgr, name, outParams)
                End Using
            End Using
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParams As String) As Dictionary(Of String, Object)
            Dim ss() As String = outParams.Split(","c)
            Dim out As New List(Of OutParam)
            For Each pn As String In ss
                out.Add(New OutParam(pn.Trim, DbTypeConvertor.ToDbType(GetType(Integer)), 1000))
            Next
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return dic
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, outParams As IEnumerable(Of OutParam), ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, outParams)
            Return CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal crMgr As ICreateManager, ByVal name As String, outParams As IEnumerable(Of OutParam), ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, outParams)
            Return CType(p.GetResult(crMgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, cache As Boolean, outParams As IEnumerable(Of OutParam),
                                    ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, cache, outParams)
            Return CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal crMgr As ICreateManager, ByVal name As String, cache As Boolean, outParams As IEnumerable(Of OutParam),
                                    ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, cache, outParams)
            Return CType(p.GetResult(crMgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParamName As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return Exec(Of T)(mgr, name, outParamName, paramNames, params)
                End Using
            End Using
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
End Namespace