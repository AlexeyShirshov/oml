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
        Protected MustOverride Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object

        Private _cache As Boolean
        Private _reseted As Boolean

        Protected Sub New(ByVal cache As Boolean)
            _cache = cache
        End Sub

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
                Return _reseted
            End Get
        End Property

        Protected Function GetKey() As String
            Dim sb As New StringBuilder
            For Each p As Pair(Of String, Object) In GetInParams()
                sb.Append(p.Second.ToString).Append("$")
            Next
            Return sb.ToString
        End Function

        Protected Function Execute(ByVal mgr As OrmReadOnlyDBManager) As Object
            _reseted = False
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
                'Dim err As Boolean = True
                Try
                    Return Execute(cmd)
                Finally
                    mgr.CloseConn(b)
                End Try
            End Using
        End Function

        Public Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As Object
            If _cache Then
                Dim key As String = "StroredProcedure:" & GetName()

                Dim id As String = GetKey()
                If String.IsNullOrEmpty(id) Then id = "empty"
                Dim dic As IDictionary = GetDic(mgr, key)
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
            Else
                Return Execute(mgr)
            End If
        End Function

        Protected Overridable Function GetDic(ByVal mgr As OrmReadOnlyDBManager, ByVal key As String) As IDictionary
            Return OrmReadOnlyDBManager.GetDic(mgr.Cache, key)
        End Function

        Public Sub ResetCache(ByVal c As OrmCacheBase)
            Dim key As String = "StroredProcedure:" & GetName()

            Dim id As String = GetKey()
            If String.IsNullOrEmpty(id) Then id = "empty"
            Dim dic As IDictionary = OrmReadOnlyDBManager.GetDic(c, key)
            If dic IsNot Nothing Then
                _reseted = True
                dic.Remove(id)
            End If
        End Sub

        Protected Overridable Function GetDepends() As IEnumerable(Of Pair(Of Type, Dependency))
            Dim l As New List(Of Pair(Of Type, Dependency))
            Return l
        End Function

        Public Overridable Function ValidateOnUpdate(ByVal obj As OrmBase, ByVal fields As ICollection(Of String)) As Boolean
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

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New()
            MyBase.new(False)
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

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Public Sub New()
            MyBase.new(True)
        End Sub

        Protected MustOverride Sub ProcessReader(ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Protected MustOverride Function InitResult() As Object

        Protected Overloads Overrides Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim result As Object = InitResult()
            Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                Dim i As Integer = 0
                Do While dr.Read
                    ProcessReader(i, dr, result)
                Loop
                Do While dr.NextResult()
                    i += 1
                    Do While dr.Read
                        ProcessReader(i, dr, result)
                    Loop
                Loop
            End Using
            If result Is Nothing Then
                Throw New InvalidOperationException("result must be filled")
            End If
            Return result
        End Function

        Protected Overridable Sub ProcessReader(ByVal resultSet As Integer, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            If resultSet = 0 Then
                ProcessReader(dr, result)
            Else
                Throw New NotImplementedException("I must implement custom logic to process results sets")
            End If
        End Sub

        Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
            Return New List(Of Orm.OutParam)
        End Function
    End Class

    Public MustInherit Class QueryOrmStoredProcBase(Of T As {OrmBase, New})
        Inherits StoredProcBase

        Protected Sub New(ByVal cache As Boolean)
            MyBase.new(cache)
        End Sub

        Protected Sub New()
            MyBase.new(True)
        End Sub

        Protected MustOverride Function GetColumns() As List(Of ColumnAttribute)
        Protected MustOverride Function GetWithLoad() As Boolean

        Protected Overloads Overrides Function Execute(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim mgr As OrmReadOnlyDBManager = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
            Return mgr.LoadMultipleObjects(Of T)(cmd, GetWithLoad, Nothing, GetColumns)
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

    Public MustInherit Class MultiResultsetQueryOrmStoredProcBase
        Inherits QueryStoredProcBase

#Region " Descriptors "

        Public Interface IResultSetDescriptor
            Sub ProcessReader(ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String)
        End Interface

        Public MustInherit Class OrmDescriptor(Of T As {OrmBase, New})
            Implements IResultSetDescriptor

            Private _l As List(Of T)

            Public Sub ProcessReader(ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements IResultSetDescriptor.ProcessReader
                _l = New List(Of T)
                Dim mgr As OrmReadOnlyDBManager = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
                mgr.LoadFromResultSet(GetType(T), GetWithLoad, _l, GetColumns, dr, GetPrimaryKeyIndex)
            End Sub

            Protected MustOverride Function GetColumns() As List(Of ColumnAttribute)
            Protected MustOverride Function GetWithLoad() As Boolean
            Protected MustOverride Function GetPrimaryKeyIndex() As Integer

            Public Function GetObjects() As ICollection(Of T)
                Return _l
            End Function
        End Class

#End Region

        Protected MustOverride Function CreateDescriptor(ByVal resultsetIdx As Integer) As IResultSetDescriptor

        Protected Overrides Sub ProcessReader(ByVal resultSet As Integer, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Dim rd As IResultSetDescriptor = CreateDescriptor(resultSet)
            If rd Is Nothing Then
                Throw New InvalidOperationException(String.Format("Resultset descriptor for resultset #{0} is nothing", resultSet))
            End If
            rd.ProcessReader(dr, GetName)
            CType(result, List(Of IResultSetDescriptor)).Add(rd)
        End Sub

        Protected Overrides Sub ProcessReader(ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function InitResult() As Object
            Return New List(Of IResultSetDescriptor)
        End Function

        Public Overloads Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of IResultSetDescriptor)
            Return CType(MyBase.GetResult(mgr), List(Of IResultSetDescriptor))
        End Function
    End Class
End Namespace
