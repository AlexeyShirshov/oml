Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities
Imports Worm.Database.Storedprocs
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Expressions2
Imports CoreFramework.cfStructures

<TestClass()> _
Public Class TestProcs

    <TestMethod()> _
    Public Sub TestP1()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P1Proc
            Dim l As List(Of Pair(Of Table1, Integer)) = p.GetResult(mgr)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(t1, l(0).First)
            Assert.AreEqual(3, l(0).Second)

            l = p.GetResult(mgr)

            Dim r1 As New Tables1to3(-100)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = New QueryCmd().GetByID(Of Table33)(2, mgr)
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(True)

                l = p.GetResult(mgr)

                Assert.AreEqual(1, l.Count)
                Assert.AreEqual(t1, l(0).First)
                Assert.AreEqual(4, l(0).Second)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP11()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P1Proc
            Dim l As List(Of Pair(Of Table1, Integer)) = p.GetResult(mgr)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t2 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)
            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(t1, l(0).First)
            Assert.AreEqual(3, l(0).Second)

            l = p.GetResult(mgr)

            Dim r1 As Tables1to3 = New QueryCmd().GetByID(Of Tables1to3)(1, mgr)
            r1.Table1 = t2
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(True)

                l = p.GetResult(mgr)

                Assert.AreEqual(2, l.Count)
                Assert.AreEqual(t1, l(0).First)
                Assert.AreEqual(2, l(0).Second)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P3Proc(1)

            Dim l = p.GetResult(mgr)

            Assert.AreEqual(1, l.Count)
            Assert.AreEqual(Date.Parse("2007-01-30 15:28:18.477"), l(0).First)
            Assert.AreEqual(Of Decimal)(2, CDec(l(0).Second))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestNonQuery_P4()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P4Proc(1)
            Dim s As String = p.GetResult(mgr)

            Assert.AreEqual("first", s)
            Assert.AreEqual("first", NonQueryStoredProcBase.Exec(Of String)(mgr, "dbo.p4", "n", "i", 1))

            Dim t As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            t.Name = "alex"
            mgr.BeginTransaction()
            Try
                t.SaveChanges(True)

                s = p.GetResult(mgr)

                Assert.AreEqual("alex", s)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestP2Orm()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P2OrmProc(2)

            Dim c As Worm.ReadOnlyObjectList(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(1, c.Count)
            Dim t1 As Table1 = c(0)

            Assert.IsNotNull(t1)
            Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)

            Assert.AreEqual(2, t1.Identifier)
            Assert.AreEqual("second", t1.Name)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestP2OrmPaging()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P2OrmProc(2)
            p.ClientPaging = New Paging(0, 0)
            Dim c As Worm.ReadOnlyObjectList(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(0, c.Count)
        End Using

    End Sub

    Class sp_pager
        Implements IPager

        Public Count As Integer

        Public Function GetCurrentPageOffset() As Integer Implements Worm.Query.IPager.GetCurrentPageOffset
            Return 0
        End Function

        Public Function GetPageSize() As Integer Implements Worm.Query.IPager.GetPageSize
            Return 0
        End Function

        Public Function GetReverse() As Boolean Implements Worm.Query.IPager.GetReverse
            Return False
        End Function

        Public Sub SetTotalCount(cnt As Integer) Implements Worm.Query.IPager.SetTotalCount
            Count = cnt
        End Sub
    End Class

    <TestMethod()> _
    Public Sub TestP2ServerPaging()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New P2OrmProc(2)
            Dim pager As sp_pager = New sp_pager
            p.Pager = pager
            Dim c As Worm.ReadOnlyObjectList(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(0, c.Count)
            Assert.AreEqual(1, pager.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestP2OrmSimple()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim c As Worm.ReadOnlyObjectList(Of Table1) = Worm.Database.Storedprocs.QueryEntityStoredProcBase(Of Table1).Exec(mgr, "dbo.p2", _
                New String() {"ID", "Title", "Code", "Enum", "EnumStr", "DT"}, New Integer() {0}, "i", 2)

            Assert.AreEqual(1, c.Count)
            Dim t1 As Table1 = c(0)

            Assert.IsNotNull(t1)
            Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Assert.IsTrue(t1.InternalProperties.IsLoaded)
            Assert.AreEqual(2, t1.Identifier)
            Assert.AreEqual("second", t1.Name)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestMulti()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New MultiR

            Dim l As List(Of MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor) = p.GetResult(mgr)

            Assert.IsNotNull(l)
            Assert.AreEqual(2, l.Count)

            Dim r0 As MultiR.r = CType(l(0), MultiR.r)
            Dim r1 As MultiR.r2 = CType(l(1), MultiR.r2)

            Assert.IsNotNull(r0.GetObjects(mgr))
            Assert.AreEqual(1, r0.GetObjects(mgr).Count)

            Dim t As Table1 = r0.GetObjects(mgr)(0)
            Assert.IsNotNull(t)
            Assert.AreEqual(1, t.Identifier)
            Assert.AreEqual(2, t.Custom)

            Assert.AreEqual(2, r1.Sum)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestScalar()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New ScalarProc(10)
            Assert.AreEqual(20, p.GetResult(mgr))
            p.ResetCache(mgr.Cache)
            Assert.AreEqual(100, p.GetResult(90, mgr))
            Assert.AreEqual(20, ScalarProc.Exec(mgr, "dbo.ScalarProc", "i", 10))
            Assert.AreEqual(30, ScalarProc.Exec(mgr, "dbo.ScalarProc", "i", 20))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPartial()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim p As New PartialLoadProc(1)
            Dim c As ICollection(Of Table1) = p.GetResult(mgr)

            Assert.AreEqual(1, c.Count)

            Dim t As Table1 = CType(c, IList(Of Table1))(0)

        End Using
    End Sub
End Class

#Region " procs "

Public Class P1Proc
    Inherits QueryStoredProcBase

    'Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
    '    Dim l As New List(Of Pair(Of Type, Dependency))
    '    l.Add(New Pair(Of Type, Dependency)(GetType(Tables1to3), Dependency.All))
    '    Return l
    'End Function

    Protected Overrides Function ProvideStaticValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As System.Type()
        Return New Type() {GetType(Tables1to3)}
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
        Return New List(Of ParamValue)
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p1"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
        Return New List(Of OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Pair(Of Table1, Integer))
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Pair(Of Table1, Integer)) = CType(result, Global.System.Collections.Generic.List(Of Pair(Of Global.TestProject1.Table1, Integer)))
        Dim t1 As Table1 = mgr.GetKeyEntityFromCacheOrCreate(Of Table1)(dr.GetInt32(0))
        Dim cnt As Integer = dr.GetInt32(1)
        l.Add(New Pair(Of Table1, Integer)(t1, cnt))
    End Sub

    Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of Pair(Of Table1, Integer))
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Pair(Of Global.TestProject1.Table1, Integer)))
    End Function
End Class

Public Class P2Proc
    Inherits QueryStoredProcBase

    Private _params As List(Of ParamValue)

    Public Sub New(ByVal i As Integer)
        _params = New List(Of ParamValue)
        _params.Add(New ParamValue("i") With {.Value = i})
    End Sub

    'Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
    '    Dim l As New List(Of Pair(Of Type, Dependency))
    '    Return l
    'End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p2"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
        Return New List(Of OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Table1)
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Table1) = CType(result, Global.System.Collections.Generic.List(Of Global.TestProject1.Table1))
        Dim t1 As Table1 = mgr.GetKeyEntityFromCacheOrCreate(Of Table1)(dr.GetInt32(0))
        l.Add(t1)
    End Sub

    Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of Table1)
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Global.TestProject1.Table1))
    End Function
End Class

Public Class P2OrmProc
    Inherits QueryEntityStoredProcBase(Of Table1)

    Private _params As List(Of ParamValue)

    Public Sub New(ByVal i As Integer)
        _params = New List(Of ParamValue)
        _params.Add(New ParamValue("i") With {.Value = i})
    End Sub

    Protected Overrides Function GetColumns() As List(Of SelectExpression)
        Dim l As New List(Of SelectExpression)
        l.Add(New SelectExpression(GetType(Table1), "ID"))
        l(0).Attributes = Field2DbRelations.PK
        l.Add(New SelectExpression(GetType(Table1), "Title"))
        l.Add(New SelectExpression(GetType(Table1), "Code"))
        l.Add(New SelectExpression(GetType(Table1), "Enum"))
        l.Add(New SelectExpression(GetType(Table1), "EnumStr"))
        l.Add(New SelectExpression(GetType(Table1), "DT"))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
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
    Inherits QueryStoredProcBase

    Private _params As List(Of ParamValue)

    Public Sub New(ByVal i As Integer)
        _params = New List(Of ParamValue)
        _params.Add(New ParamValue("i") With {.Value = i})
    End Sub

    'Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
    '    Dim l As New List(Of Pair(Of Type, Dependency))
    '    l.Add(New Pair(Of Type, Dependency)(GetType(Table1), Dependency.All))
    '    l.Add(New Pair(Of Type, Dependency)(GetType(Table2), Dependency.All))
    '    Return l
    'End Function

    Protected Overrides Function ProvideStaticValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As System.Type()
        Return New Type() {GetType(Table1), GetType(Table2)}
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p3"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
        Return New List(Of OutParam)
    End Function

    Protected Overrides Function InitResult() As Object
        Return New List(Of Pair(Of Date, Decimal))
    End Function

    Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
        Dim l As List(Of Pair(Of Date, Decimal)) = CType(result, Global.System.Collections.Generic.List(Of Pair(Of Date, Decimal)))
        Dim dt As Date = dr.GetDateTime(0)
        Dim m As Decimal = dr.GetDecimal(1)
        l.Add(New Pair(Of Date, Decimal)(dt, m))
    End Sub

    Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of Pair(Of Date, Decimal))
        Return CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.List(Of Pair(Of Date, Decimal)))
    End Function
End Class

Public Class P4Proc
    Inherits NonQueryStoredProcBase

    Private _params As List(Of ParamValue)

    Public Sub New(ByVal i As Integer)
        _params = New List(Of ParamValue)
        _params.Add(New ParamValue("i") With {.Value = i})
    End Sub

    Public Shared Function ValidateOnUpdate(ByVal params As IList(Of Object), ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String)) As Boolean
        Dim t1 As Table1 = TryCast(obj, Table1)
        If t1 IsNot Nothing AndAlso t1.ID = CInt(params(0)) Then
            Return True
        End If
        Return False
    End Function

    'Protected Overrides Function GetDepends() As System.Collections.Generic.IEnumerable(Of Pair(Of System.Type, Dependency))
    '    Return New List(Of Pair(Of Type, Dependency))
    'End Function

    Protected Overrides Function ProvideStaticValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As System.Type()
        OnUpdateStaticMethodName = DontNeedResetCacheOnUpdate
        OnInsertDeleteStaticMethodName = DontNeedResetCacheOnInsertDelete
        Return New Type() {GetType(Table1)}
    End Function

    Protected Overrides Function ProvideDynamicValidateInfo(ByRef OnUpdateStaticMethodName As String, ByRef OnInsertDeleteStaticMethodName As String) As System.Collections.Generic.IList(Of Object)
        OnUpdateStaticMethodName = "ValidateOnUpdate"
        Return New Object() {_params(0).Value}
    End Function

    Protected Overrides Function GetInParams() As IEnumerable(Of ParamValue)
        Return _params
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.p4"
    End Function

    Protected Overrides Function GetOutParams() As System.Collections.Generic.IEnumerable(Of OutParam)
        Dim p As New List(Of OutParam)
        p.Add(New OutParam("n", System.Data.DbType.AnsiString, 100))
        Return p
    End Function

    Public Shadows Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As String
        Dim dic As Dictionary(Of String, Object) = CType(MyBase.GetResult(mgr), Global.System.Collections.Generic.Dictionary(Of String, Object))
        Return CStr(dic("n"))
    End Function
End Class

Public Class MultiR
    Inherits MultiResultsetQueryEntityStoredProcBase

    Class r
        Inherits MultiResultsetQueryEntityStoredProcBase.EntityDescriptor(Of Table1)

        Protected Overrides Function GetColumns() As List(Of SelectExpression)
            Dim l As New List(Of SelectExpression)
            Dim mgr As Worm.OrmManager = Worm.OrmManager.CurrentManager
            l.Add(New SelectExpression(GetType(Table1), "ID"))
            l(0).Attributes = Field2DbRelations.PK
            l.Add(New SelectExpression(GetType(Table1), "Custom"))
            Return l
        End Function

        'Protected Overrides Function GetWithLoad() As Boolean
        '    Return True
        'End Function
    End Class

    Public Class r2
        Implements MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor

        Private _sum As Integer

        Public Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor.ProcessReader
            _sum = dr.GetInt32(0)
        End Sub

        Public ReadOnly Property Sum() As Integer
            Get
                Return _sum
            End Get
        End Property

        Public Sub EndProcess(ByVal mgr As Worm.OrmManager) Implements MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor.EndProcess

        End Sub

        Public Sub BeginProcess(ByVal mgr As Worm.OrmManager) Implements Worm.Database.Storedprocs.MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor.BeginProcess

        End Sub
    End Class

    Public Sub New()

    End Sub

    Protected Overrides Function createDescriptor(ByVal resultsetIdx As Integer) As MultiResultsetQueryEntityStoredProcBase.IResultSetDescriptor
        Select Case resultsetIdx
            Case 0
                Return New r
            Case 1
                Return New r2
            Case Else
                Throw New NotImplementedException
        End Select
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
        Return New List(Of ParamValue)
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.MultiR"
    End Function
End Class

Public Class ScalarProc
    Inherits ScalarStoredProc(Of Integer)

    Private _i As Integer

    Public Sub New(ByVal i As Integer)
        _i = i
    End Sub

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
        Dim l As New List(Of ParamValue)
        l.Add(New ParamValue("i") With {.Value = _i})
        Return l
    End Function

    Protected Overrides Function GetName() As String
        Return "dbo.ScalarProc"
    End Function

    Public Overloads Function GetResult(ByVal i As Integer, ByVal mgr As OrmReadOnlyDBManager) As Integer
        _i = i
        Return MyBase.GetResult(mgr)
    End Function
End Class

Public Class PartialLoadProc
    Inherits QueryEntityStoredProcBase(Of Table1)

    Private _params As List(Of ParamValue)

    Public Sub New(ByVal i As Integer)
        _params = New List(Of ParamValue)
        _params.Add(New ParamValue("id") With {.Value = i})
    End Sub

    Protected Overrides Function GetColumns() As List(Of SelectExpression)
        Dim l As New List(Of SelectExpression)
        l.Add(New SelectExpression(GetType(Table1), "ID"))
        l(0).Attributes = Field2DbRelations.PK
        l.Add(New SelectExpression(GetType(Table1), "ddd"))
        Return l
    End Function

    Protected Overrides Function GetInParams() As System.Collections.Generic.IEnumerable(Of ParamValue)
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