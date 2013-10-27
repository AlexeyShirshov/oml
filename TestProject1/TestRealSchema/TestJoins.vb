Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Criteria.Values
Imports Worm.Database
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria
Imports Worm.Query
Imports Worm.Criteria.Joins
Imports Worm

<TestClass()> _
Public Class TestJoinsRS
    <TestMethod()> _
    Public Sub TestEval()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table1)
            Dim f As New EntityFilter(t, "DT", New ScalarValue(CDate("2007-01-01")), Worm.Criteria.FilterOperation.GreaterEqualThan)
            Dim f2 As New EntityFilter(t, "Code", New ScalarValue(2), Worm.Criteria.FilterOperation.NotEqual)
            'Dim c As New EntityCondition(f, f2, ConditionOperator.And)
            Dim cf As Worm.Criteria.Core.IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, Worm.Criteria.Conditions.ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table1(1, mgr.Cache, mgr.MappingEngine)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 2
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 3
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval2()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table1)
            Dim f As New EntityFilter(t, "DT", New ScalarValue(CDate("2007-01-01")), Worm.Criteria.FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Code", New ScalarValue(10), Worm.Criteria.FilterOperation.LessThan)
            Dim cf As Worm.Criteria.Core.IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, Worm.Criteria.Conditions.ConditionOperator.Or).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table1(1, mgr.Cache, mgr.MappingEngine)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 20
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.CreatedAt = CDate("2007-01-01")
            t1.Code = 30
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval3()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim f As New EntityFilter(t, "Table1", New EntityValue(tbl), Worm.Criteria.FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Money", New ScalarValue(CDec(10)), Worm.Criteria.FilterOperation.GreaterThan)
            Dim cf As Worm.Criteria.Core.IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, Worm.Criteria.Conditions.ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table2(1, mgr.Cache, mgr.MappingEngine)
            t1.Money = 4
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval4()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim f As New EntityFilter(t, "Table1", New EntityValue(Nothing), Worm.Criteria.FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Money", New ScalarValue(CDec(10)), Worm.Criteria.FilterOperation.GreaterThan)
            Dim cf As Worm.Criteria.Core.IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, Worm.Criteria.Conditions.ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table2(1, mgr.Cache, mgr.MappingEngine)
            t1.Money = 4
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.EvalObj(mgr.MappingEngine, t1, Nothing, Nothing, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinEval()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As IEntityFilter = CType(New Ctor(GetType(Table1)).prop("Title").eq("first").Filter(), IEntityFilter)
            Dim t2 As New Table2(1, mgr.Cache, mgr.MappingEngine)
            Assert.AreEqual(IEvaluableValue.EvalResult.Unknown, c.EvalObj(mgr.MappingEngine, t2, mgr.MappingEngine.GetEntitySchema(t), Nothing, Nothing))

            t2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Assert.AreEqual(IEvaluableValue.EvalResult.Found, c.EvalObj(mgr.MappingEngine, t2, mgr.MappingEngine.GetEntitySchema(t), Nothing, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinEval2()
        Dim tm As New TestManagerRS
        Using mgr As Worm.OrmManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As EntityFilter = CType(New Ctor(GetType(Table1)).prop("Title").eq("first").Filter(), EntityFilter)
            Dim t2 As New Table2(1, mgr.Cache, mgr.MappingEngine)
            Assert.AreEqual(IEvaluableValue.EvalResult.Unknown, c.Eval(mgr.MappingEngine, t2, mgr.MappingEngine.GetEntitySchema(t), Nothing, Nothing))

            Dim joins() As QueryJoin = JCtor.join(GetType(Table1)).on(GetType(Table1), "ID").eq(GetType(Table2), "Table1").
                and(Ctor.prop(GetType(Table1), "Code").eq(100))

            t2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, c.Eval(mgr.MappingEngine, t2, mgr.MappingEngine.GetEntitySchema(t),
                                                                         joins, GetType(Table2)))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateWriteManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim q As New QueryCmd()
            q.AutoJoins = True
            Dim c As ICollection(Of Table2) = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, c.Count)

            q = New QueryCmd()
            q.AutoJoins = True
            c = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first").[and]("Code").eq(2)).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, c.Count)

            Dim t2 As New Table2(tm.GetIdentity(), mgr.Cache, mgr.MappingEngine)
            t2.Tbl = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Assert.IsNull(t2.InternalProperties.OriginalCopy)
            mgr.BeginTransaction()
            Try
                t2.SaveChanges(True)
                q = New QueryCmd()
                q.AutoJoins = True
                c = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
                Assert.AreEqual(3, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache2()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateWriteManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim q As New QueryCmd()
            q.AutoJoins = True
            Dim c As ICollection(Of Table2) = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, c.Count)

            Dim t2 As New Table2(1, mgr.Cache, mgr.MappingEngine)
            Dim t1 As New Table1(110, mgr.Cache, mgr.MappingEngine)
            mgr.Cache.NewObjectManager.AddNew(t2)
            mgr.Cache.NewObjectManager.AddNew(t1)
            t1.Name = "bjkb"
            t1.CreatedAt = CDate("2006-01-01")
            Dim r As New SinglePKEntity.RelatedObject(t1, New String() {"ID"}, t2, New String() {"ID"})
            t2.Tbl = t1
            mgr.BeginTransaction()
            Try
                t1.SaveChanges(True)
                t2.SaveChanges(True)
                q = New QueryCmd()
                q.AutoJoins = True

                c = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache3()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateWriteManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim q As New QueryCmd()
            q.AutoJoins = True

            Dim c As ICollection(Of Table2) = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, c.Count)

            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            t1.Name = "sdfasdf"
            Assert.AreEqual(ObjectState.Modified, t1.InternalProperties.ObjectState)
            mgr.BeginTransaction()
            Try
                t1.SaveChanges(True)
                q = New QueryCmd()
                q.AutoJoins = True
                c = q.Where(New Ctor(GetType(Table1)).prop("Title").eq("first")).ToList(Of Table2)(mgr)
                Assert.AreEqual(0, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    '<TestMethod()> Public Sub TestXml()
    '    Dim params() As Object = New Object() {"skladjfn", 10, 14D, 19.4}
    '    Dim xs As New Xml.Serialization.XmlSerializer(params.GetType)
    '    Using sw As New IO.StringWriter
    '        xs.Serialize(sw, params)
    '        Debug.WriteLine(sw.GetStringBuilder.ToString)
    '    End Using
    'End Sub

    'Sub foo(ByVal serviceName As String, ByVal serviceMethod As String, ByVal params() As Object)
    '    Dim s As New mysoap
    '    s.Url = serviceName
    '    s.Call(serviceMethod, params)



    'End Sub

    'Class mysoap
    '    Inherits System.Web.Services.Protocols.SoapHttpClientProtocol

    '    Public Function [Call](ByVal methodName As String, ByVal parameters As Object()) As Object()
    '        Return Me.Invoke(methodName, parameters)
    '    End Function

    'End Class
End Class
