Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics
Imports CoreFramework.Structures

<TestClass()> _
Public Class TestProcs

    <TestMethod()> _
    Public Sub TestP1()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P1Proc
            Dim l As List(Of Pair(Of Table1, Integer)) = p.GetResult(mgr)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(t1, l(0).First)
            Assert.AreEqual(2, l(0).Second)

            l = p.GetResult(mgr)

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.ObjectSchema)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = mgr.Find(Of Table33)(2)
            mgr.BeginTransaction()
            Try
                r1.Save(True)

                l = p.GetResult(mgr)

                Assert.AreEqual(1, l.Count)
                Assert.AreEqual(t1, l(0).First)
                Assert.AreEqual(3, l(0).Second)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP11()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P1Proc
            Dim l As List(Of Pair(Of Table1, Integer)) = p.GetResult(mgr)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t2 As Table1 = mgr.Find(Of Table1)(2)
            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(t1, l(0).First)
            Assert.AreEqual(2, l(0).Second)

            l = p.GetResult(mgr)

            Dim r1 As Tables1to3 = mgr.Find(Of Tables1to3)(1)
            r1.Table1 = t2
            mgr.BeginTransaction()
            Try
                r1.Save(True)

                l = p.GetResult(mgr)

                Assert.AreEqual(2, l.Count)
                Assert.AreEqual(t1, l(0).First)
                Assert.AreEqual(1, l(0).Second)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP2()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P2Proc(1)
            Dim l As List(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(1, l.Count)

            l = p.GetResult(mgr)
            Assert.AreEqual(1, l.Count)

            p = New P2Proc(10)
            l = p.GetResult(mgr)
            Assert.AreEqual(0, l.Count)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP3()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P3Proc(1)

            Dim l As List(Of Pair(Of Date, Decimal)) = p.GetResult(mgr)

            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(Date.Parse("2007-01-30 15:28:18.477"), l(0).First)
            Assert.AreEqual(Of Decimal)(2, l(0).Second)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP4()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P4Proc(1)
            Dim s As String = p.GetResult(mgr)

            Assert.AreEqual("first", s)
            Dim t As Table1 = mgr.Find(Of Table1)(1)
            t.Name = "alex"
            mgr.BeginTransaction()
            Try
                t.Save(True)

                s = p.GetResult(mgr)

                Assert.AreEqual("alex", s)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP2Orm()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New P2OrmProc(2)

            Dim c As ICollection(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(1, c.Count)
            Dim t1 As Table1 = CType(c, IList(Of Table1))(0)

            Assert.IsNotNull(t1)
            Assert.AreEqual(Orm.ObjectState.None, t1.ObjectState)

            Assert.AreEqual(2, t1.Identifier)
            Assert.AreEqual("second", t1.Name)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestMulti()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New MultiR

            Dim l As List(Of Orm.MultiResultsetQueryOrmStoredProcBase.IResultSetDescriptor) = p.GetResult(mgr)

            Assert.IsNotNull(l)
            Assert.AreEqual(2, l.Count)

            Dim r0 As MultiR.r = CType(l(0), MultiR.r)
            Dim r1 As MultiR.r2 = CType(l(1), MultiR.r2)

            Assert.IsNotNull(r0.GetObjects(mgr))
            Assert.AreEqual(1, r0.GetObjects(mgr).Count)

            Dim t As Table1 = CType(r0.GetObjects(mgr), List(Of Table1))(0)
            Assert.IsNotNull(t)
            Assert.AreEqual(1, t.Identifier)
            Assert.AreEqual(2, t.Custom)

            Assert.AreEqual(2, r1.Sum)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestScalar()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New ScalarProc(10)
            Assert.AreEqual(20, p.GetResult(mgr))
            Assert.AreEqual(100, p.GetResult(90, mgr))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPartial()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            Dim p As New PartialLoadProc(1)
            Dim c As ICollection(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(1, c.Count)

            Dim t As Table1 = CType(c, IList(Of Table1))(0)

        End Using
    End Sub
End Class

#Region " procs "

Public Class P1Proc
    Inherits Orm.QueryStoredProcBase

    Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Worm.Orm.Dependency))
        Dim l As New List(Of Pair(Of Type, Orm.Dependency))
        l.Add(New Pair(Of Type, Orm.Dependency)(GetType(Tables1to3), Orm.Dependency.All))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return New List(Of Pair(Of String, Object))
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p1"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of Orm.OutParam)
        Return New List(Of Orm.OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Pair(Of Table1, Integer))
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As Orm.OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Pair(Of Table1, Integer)) = CType(result, Global.System.Collections.Generic.List(Of Pair(Of Global.TestProject1.Table1, Integer)))
        Dim t1 As Table1 = mgr.CreateDBObject(Of Table1)(dr.GetInt32(0))
        Dim cnt As Integer = dr.GetInt32(1)
        l.Add(New Pair(Of Table1, Integer)(t1, cnt))
    End Sub

    Public Shadows Function GetResult(ByVal mgr As Orm.OrmReadOnlyDBManager) As List(Of Pair(Of Table1, Integer))
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Pair(Of Global.TestProject1.Table1, Integer)))
    End Function
End Class

Public Class P2Proc
    Inherits Orm.QueryStoredProcBase

    Private _params As List(Of Pair(Of String, Object))

    Public Sub New(ByVal i As Integer)
        _params = New List(Of Pair(Of String, Object))
        _params.Add(New Pair(Of String, Object)("i", i))
    End Sub

    Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Worm.Orm.Dependency))
        Dim l As New List(Of Pair(Of Type, Orm.Dependency))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p2"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of Orm.OutParam)
        Return New List(Of Orm.OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Table1)
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As Orm.OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Table1) = CType(result, Global.System.Collections.Generic.List(Of Global.TestProject1.Table1))
        Dim t1 As Table1 = mgr.CreateDBObject(Of Table1)(dr.GetInt32(0))
        l.Add(t1)
    End Sub

    Public Shadows Function GetResult(ByVal mgr As Orm.OrmReadOnlyDBManager) As List(Of Table1)
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Global.TestProject1.Table1))
    End Function
End Class

Public Class P2OrmProc
    Inherits Orm.QueryOrmStoredProcBase(Of Table1)

    Private _params As List(Of Pair(Of String, Object))

    Public Sub New(ByVal i As Integer)
        _params = New List(Of Pair(Of String, Object))
        _params.Add(New Pair(Of String, Object)("i", i))
    End Sub

    Protected Overrides Function GetColumns() As System.Collections.Generic.List(Of Worm.Orm.ColumnAttribute)
        Dim l As New List(Of Orm.ColumnAttribute)
        l.Add(New Orm.ColumnAttribute("ID"))
        l.Add(New Orm.ColumnAttribute("Title"))
        l.Add(New Orm.ColumnAttribute("Code"))
        l.Add(New Orm.ColumnAttribute("Enum"))
        l.Add(New Orm.ColumnAttribute("EnumStr"))
        l.Add(New Orm.ColumnAttribute("DT"))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p2"
    End Function

    Protected Overrides Function GetWithLoad() As Boolean
        Return True
    End Function
End Class

Public Class P3Proc
    Inherits Orm.QueryStoredProcBase

    Private _params As List(Of Pair(Of String, Object))

    Public Sub New(ByVal i As Integer)
        _params = New List(Of Pair(Of String, Object))
        _params.Add(New Pair(Of String, Object)("i", i))
    End Sub

    Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Worm.Orm.Dependency))
        Dim l As New List(Of Pair(Of Type, Orm.Dependency))
        l.Add(New Pair(Of Type, Orm.Dependency)(GetType(Table1), Orm.Dependency.All))
        l.Add(New Pair(Of Type, Orm.Dependency)(GetType(Table2), Orm.Dependency.All))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p3"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of Orm.OutParam)
        Return New List(Of Orm.OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Pair(Of Date, Decimal))
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As Orm.OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Pair(Of Date, Decimal)) = CType(result, Global.System.Collections.Generic.List(Of Pair(Of Date, Decimal)))
        Dim dt As Date = dr.GetDateTime(0)
        Dim m As Decimal = dr.GetDecimal(1)
        l.Add(New Pair(Of Date, Decimal)(dt, m))
    End Sub

    Public Shadows Function GetResult(ByVal mgr As Orm.OrmReadOnlyDBManager) As List(Of Pair(Of Date, Decimal))
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Pair(Of Date, Decimal)))
    End Function
End Class

Public Class P4Proc
    Inherits Orm.NonQueryStoredProcBase

    Private _params As List(Of Pair(Of String, Object))

    Public Sub New(ByVal i As Integer)
        _params = New List(Of Pair(Of String, Object))
        _params.Add(New Pair(Of String, Object)("i", i))
    End Sub

    Public Overrides Function ValidateOnUpdate(ByVal obj As Worm.Orm.OrmBase, ByVal fields As ICollection(Of String)) As Boolean
        Dim t1 As Table1 = TryCast(obj, Table1)
        If t1 IsNot Nothing AndAlso t1.Identifier = CInt(_params(0).Second) Then
            Return True
        End If
        Return MyBase.ValidateOnUpdate(obj, fields)
    End Function

    Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Worm.Orm.Dependency))
        Return New List(Of Pair(Of Type, Orm.Dependency))
    End Function

    Protected Overrides Function GetInParams() As IEnumerable(Of Pair(Of String, Object))
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p4"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of Orm.OutParam)
        Dim p As New List(Of Orm.OutParam)
        p.Add(New Orm.OutParam("n", System.Data.DbType.AnsiString, 100))
        Return p
    End Function

    Public Shadows Function GetResult(ByVal mgr As Orm.OrmReadOnlyDBManager) As String
        Dim dic As Dictionary(Of String, Object) = CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        Return CStr(dic("n"))
    End Function
End Class

Public Class MultiR
    Inherits Orm.MultiResultsetQueryOrmStoredProcBase

    Class r
        Inherits Orm.MultiResultsetQueryOrmStoredProcBase.OrmDescriptor(Of Table1)

        Protected Overrides Function GetColumns() As System.Collections.Generic.List(Of Worm.Orm.ColumnAttribute)
            Dim l As New List(Of Orm.ColumnAttribute)
            Dim mgr As Orm.OrmManagerBase = Orm.OrmManagerBase.CurrentManager
            l.Add(New Orm.ColumnAttribute("ID"))
            l.Add(New Orm.ColumnAttribute("Custom"))
            Return l
        End Function

        Protected Overrides Function GetWithLoad() As Boolean
            Return True
        End Function

        Protected Overrides Function GetPrimaryKeyIndex() As Integer
            Return 0
        End Function
    End Class

    Public Class r2
        Implements Orm.MultiResultsetQueryOrmStoredProcBase.IResultSetDescriptor

        Private _sum As Integer

        Public Sub ProcessReader(ByVal mgr As Orm.OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements Worm.Orm.MultiResultsetQueryOrmStoredProcBase.IResultSetDescriptor.ProcessReader
            _sum = dr.GetInt32(0)
        End Sub

        Public ReadOnly Property Sum() As Integer
            Get
                Return _sum
            End Get
        End Property

        Public Sub EndProcess(ByVal mgr As Worm.Orm.OrmManagerBase) Implements Worm.Orm.MultiResultsetQueryOrmStoredProcBase.IResultSetDescriptor.EndProcess

        End Sub
    End Class

    Public Sub New()

    End Sub

    Protected Overrides Function createDescriptor(ByVal resultsetIdx As Integer) As Worm.Orm.MultiResultsetQueryOrmStoredProcBase.IResultSetDescriptor
        Select Case resultsetIdx
            Case 0
                Return New r
            Case 1
                Return New r2
            Case Else
                Throw New NotImplementedException
        End Select
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return New List(Of Pair(Of String, Object))
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.MultiR"
    End Function
End Class

Public Class ScalarProc
    Inherits Orm.ScalarStoredProc(Of Integer)

    Private _i As Integer

    Public Sub New(ByVal i As Integer)
        _i = i
    End Sub

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of CoreFramework.Structures.Pair(Of String, Object))
        Dim l As New List(Of Pair(Of String, Object))
        l.Add(New Pair(Of String, Object)("i", _i))
        Return l
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.ScalarProc"
    End Function

    Public Overloads Function GetResult(ByVal i As Integer, ByVal mgr As Orm.OrmReadOnlyDBManager) As Integer
        _i = i
        Return MyBase.GetResult(mgr)
    End Function
End Class

Public Class PartialLoadProc
    Inherits Orm.QueryOrmStoredProcBase(Of Table1)

    Private _params As List(Of Pair(Of String, Object))

    Public Sub New(ByVal i As Integer)
        _params = New List(Of Pair(Of String, Object))
        _params.Add(New Pair(Of String, Object)("id", i))
    End Sub

    Protected Overrides Function GetColumns() As System.Collections.Generic.List(Of Worm.Orm.ColumnAttribute)
        Dim l As New List(Of Orm.ColumnAttribute)
        l.Add(New Orm.ColumnAttribute("ID"))
        l.Add(New Orm.ColumnAttribute("ddd"))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of Pair(Of String, Object))
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.partialload"
    End Function

    Protected Overrides Function GetWithLoad() As Boolean
        Return True
    End Function
End Class

#End Region