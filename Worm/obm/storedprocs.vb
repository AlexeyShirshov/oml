Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm
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
        Protected MustOverride Function GetDepends() As IEnumerable(Of Pair(Of Type, Dependency))
        Protected MustOverride Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object

        Protected Function GetKey() As String
            Dim sb As New StringBuilder
            For Each p As Pair(Of String, Object) In GetInParams()
                sb.Append(p.Second.ToString).Append("$")
            Next
            Return sb.ToString
        End Function

        Protected Function Execute(ByVal mgr As OrmReadOnlyDBManager) As Object
            Dim schema As DbSchema = CType(mgr.ObjectSchema, DbSchema)
            Using cmd As System.Data.Common.DbCommand = schema.CreateDBCommand
                cmd.CommandType = System.Data.CommandType.StoredProcedure
                cmd.CommandText = GetName()
                For Each p As Pair(Of String, Object) In GetInParams()
                    cmd.Parameters.Add(schema.CreateDBParameter(p.First, p.Second))
                Next
                For Each p As OutParam In GetOutParams()
                    Dim pr As System.Data.Common.DbParameter = schema.CreateDBParameter()
                    pr.ParameterName = p.Name
                    pr.Direction = System.Data.ParameterDirection.Output
                    pr.DbType = p.DbType
                    pr.Size = p.Size
                    cmd.Parameters.Add(pr)
                Next
                Dim b As OrmReadOnlyDBManager.ConnAction = mgr.TestConn(cmd)
                Dim err As Boolean = True
                Try
                    Return Execute(cmd)
                Finally
                    mgr.CloseConn(b)
                End Try
            End Using
        End Function

        Public Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As Object
            Dim key As String = "StroredProcedure:" & GetName()

            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Dim dic As IDictionary = OrmReadOnlyDBManager.GetDic(mgr.Cache, key)
            Dim sync As String = key & id
            Dim _result As Object = dic(id)
            If _result Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(sync)
                    _result = dic(id)
                    If _result Is Nothing Then
                        mgr.Cache.AddStoredProc(Me)
                        _result = Execute(mgr)
                        dic(id) = _result
                    End If
                End Using
            End If
            Return _result
        End Function

        Public Sub ResetCache(ByVal c As OrmCacheBase)
            Dim key As String = "StroredProcedure:" & GetName()

            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Dim dic As IDictionary = OrmReadOnlyDBManager.GetDic(c, key)

            dic.Remove(id)
        End Sub

        Public Overridable Function ValidateOnUpdate(ByVal obj As OrmBase) As Boolean
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            For Each p As Pair(Of Type, Dependency) In GetDepends()
                If t Is p.First Then
                    Return (p.Second And Dependency.Update) = Dependency.Update
                End If
            Next
            Return False
        End Function

        Public Overridable Function ValidateOnInsertDelete(ByVal obj As OrmBase) As Boolean
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            For Each p As Pair(Of Type, Dependency) In GetDepends()
                If t Is p.First Then
                    Return (p.Second And Dependency.InsertDelete) = Dependency.InsertDelete
                End If
            Next
            Return False
        End Function
    End Class

    Public MustInherit Class NonQueryStoredProcBase
        Inherits StoredProcBase

        Protected Sub New()

        End Sub

        Protected Overloads Overrides Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim r As Integer = cmd.ExecuteNonQuery()
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
    End Class

    Public MustInherit Class QueryStoredProcBase
        Inherits StoredProcBase

        Protected MustOverride Sub ProcessReader(ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Protected MustOverride Function InitResult() As Object

        Protected Overloads Overrides Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim result As Object = InitResult()
            Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                Do While dr.Read
                    ProcessReader(dr, result)
                Loop
            End Using
            If result Is Nothing Then
                Throw New InvalidOperationException("result must be filled")
            End If
            Return result
        End Function
    End Class

    Public MustInherit Class QueryOrmStoredProcBase(Of T As {OrmBase, New})
        Inherits StoredProcBase

        Protected MustOverride Function GetColumns() As List(Of ColumnAttribute)
        Protected MustOverride Function GetWithLoad() As Boolean

        Protected Overloads Overrides Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim mgr As OrmReadOnlyDBManager = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
            Return mgr.LoadMultipleObjects(Of T)(GetType(T), cmd, GetWithLoad, Nothing, GetColumns)
        End Function

        Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As ICollection(Of T)
            Return CType(MyBase.GetResult(mgr), ICollection(Of T))
        End Function

        Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Worm.Orm.Dependency))
            Dim l As New List(Of Pair(Of Type, Orm.Dependency))
            l.Add(New Pair(Of Type, Orm.Dependency)(GetType(T), Orm.Dependency.All))
            Return l
        End Function

        Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of Worm.Orm.OutParam)
            Return New List(Of Orm.OutParam)
        End Function
    End Class
End Namespace
