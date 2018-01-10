Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports CoreFramework.CFDebugging

Namespace Database.Storedprocs

    Public MustInherit Class NonQueryStoredProcBase
        Inherits StoredProcBase

        Private _exec As TimeSpan

        Protected Sub New(ByVal cache As Boolean)
            MyBase.New(cache)
        End Sub

        Protected Sub New()
            MyBase.New(False)
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

        Public Class NonQueryStoredProcSimple
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

        Public Shared Function GetManager(cm As ICreateManager, ctx As Object) As IGetManager
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If mgr Is Nothing Then
                If cm IsNot Nothing Then
                    Return New GetManagerDisposable(cm.CreateManager(ctx), Nothing)
                Else
                    Return Nothing
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, mgr.MappingEngine)
            End If
        End Function

        Public Shared Sub Exec(ByVal getMgr As ICreateManager, ByVal name As String)
            Using gm = GetManager(getMgr, Nothing)
                Using New SetManagerHelper(gm.Manager, getMgr, Nothing)
                    Exec(CType(gm.Manager, OrmReadOnlyDBManager), name)
                End Using
            End Using
        End Sub

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String)
            'Using New SetManagerHelper(mgr, mgr.GetCreateManager, Nothing)
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing)
            p.CommandTimeout = mgr.CommandTimeout
            p.GetResult(mgr)
            'End Using
        End Sub

        Public Shared Sub Exec(ByVal getMgr As ICreateManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object)
            Using gm = GetManager(getMgr, Nothing)
                Using New SetManagerHelper(gm.Manager, getMgr, Nothing)
                    Exec(CType(gm.Manager, OrmReadOnlyDBManager), name, paramNames, params)
                End Using
            End Using
        End Sub

        Public Shared Sub Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params)
            p.CommandTimeout = mgr.CommandTimeout
            p.GetResult(mgr)
        End Sub

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParamName As String) As T
            Using gm = GetManager(getMgr, Nothing)
                Using New SetManagerHelper(gm.Manager, getMgr, Nothing)
                    Return Exec(Of T)(CType(gm.Manager, OrmReadOnlyDBManager), name, outParamName)
                End Using
            End Using
        End Function

        Public Shared Function Exec(Of T)(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, ByVal outParamName As String) As T
            Dim out As New List(Of OutParam)
            out.Add(New OutParam(outParamName, DbTypeConvertor.ToDbType(GetType(T)), 1000))
            Dim p As New NonQueryStoredProcSimple(name, Nothing, Nothing, out)
            p.CommandTimeout = mgr.CommandTimeout
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function

        Public Shared Function Exec(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParams As String) As Dictionary(Of String, Object)
            Using gm = GetManager(getMgr, Nothing)
                Using New SetManagerHelper(gm.Manager, getMgr, Nothing)
                    Return Exec(CType(gm.Manager, OrmReadOnlyDBManager), name, outParams)
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
            p.CommandTimeout = mgr.CommandTimeout
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return dic
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, outParams As IEnumerable(Of OutParam), ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, outParams)
            p.CommandTimeout = mgr.CommandTimeout
            Return CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal crMgr As ICreateManager, ByVal name As String, outParams As IEnumerable(Of OutParam), ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, outParams)
            'p.CommandTimeout = mgr.CommandTimeout
            Return CType(p.GetResult(crMgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal mgr As OrmReadOnlyDBManager, ByVal name As String, cache As Boolean, outParams As IEnumerable(Of OutParam),
                                    ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, cache, outParams)
            p.CommandTimeout = mgr.CommandTimeout
            Return CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(ByVal crMgr As ICreateManager, ByVal name As String, cache As Boolean, outParams As IEnumerable(Of OutParam),
                                    ByVal paramNames As String, ByVal ParamArray params() As Object) As Dictionary(Of String, Object)
            Dim ss() As String = paramNames.Split(","c)
            If ss.Length <> params.Length Then
                Throw New ArgumentException("Number of parameter names is not equals to parameter values")
            End If
            Dim p As New NonQueryStoredProcSimple(name, ss, params, cache, outParams)
            'p.CommandTimeout = mgr.CommandTimeout
            Return CType(p.GetResult(crMgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        End Function

        Public Shared Function Exec(Of T)(ByVal getMgr As ICreateManager, ByVal name As String, ByVal outParamName As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Using gm = GetManager(getMgr, Nothing)
                Using New SetManagerHelper(gm.Manager, getMgr, Nothing)
                    Return Exec(Of T)(CType(gm.Manager, OrmReadOnlyDBManager), name, outParamName, paramNames, params)
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
            p.CommandTimeout = mgr.CommandTimeout
            Dim dic As Dictionary(Of String, Object) = CType(p.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
            Return CType(dic(outParamName), T)
        End Function
    End Class
End Namespace