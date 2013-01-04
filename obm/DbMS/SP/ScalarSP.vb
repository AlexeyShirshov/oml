Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Database.Storedprocs

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

        Public Shadows Function GetResult(ByVal getMgr As ICreateManager) As T
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Me), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return GetResult(mgr)
                End Using
            End Using
        End Function

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

        Public Shared Shadows Function Exec(ByVal getMgr As ICreateManager, ByVal name As String, ByVal paramNames As String, ByVal ParamArray params() As Object) As T
            Using mgr As OrmReadOnlyDBManager = CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager)
                Using New SetManagerHelper(mgr, getMgr, Nothing)
                    Return Exec(mgr, name, paramNames, params)
                End Using
            End Using
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
