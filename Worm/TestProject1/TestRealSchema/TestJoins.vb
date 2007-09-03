Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Orm
Imports System.Diagnostics

<TestClass()> _
Public Class TestJoinsRS
    <TestMethod()> _
    Public Sub TestEval()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table1)
            Dim f As New EntityFilter(t, "DT", New SimpleValue(CDate("2007-01-01")), FilterOperation.GreaterEqualThan)
            Dim f2 As New EntityFilter(t, "Code", New SimpleValue(2), FilterOperation.NotEqual)
            'Dim c As New EntityCondition(f, f2, ConditionOperator.And)
            Dim cf As IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table1(1, mgr.Cache, mgr.ObjectSchema)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 2
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 3
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval2()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table1)
            Dim f As New EntityFilter(t, "DT", New SimpleValue(CDate("2007-01-01")), FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Code", New SimpleValue(10), FilterOperation.LessThan)
            Dim cf As IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, ConditionOperator.Or).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table1(1, mgr.Cache, mgr.ObjectSchema)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 20
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.CreatedAt = CDate("2007-01-01")
            t1.Code = 30
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval3()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = mgr.Find(Of Table1)(1)
            Dim f As New EntityFilter(t, "Table1", New EntityValue(tbl), FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Money", New SimpleValue(CDec(10)), FilterOperation.GreaterThan)
            Dim cf As IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            t1.Money = 4
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval4()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = mgr.Find(Of Table1)(1)
            Dim f As New EntityFilter(t, "Table1", New EntityValue(Nothing), FilterOperation.Equal)
            Dim f2 As New EntityFilter(t, "Money", New SimpleValue(CDec(10)), FilterOperation.GreaterThan)
            Dim cf As IFilter = New Condition.ConditionConstructor().AddFilter(f).AddFilter(f2, ConditionOperator.And).Condition
            Dim c As IEntityFilter = CType(cf, IEntityFilter)

            Dim t1 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            t1.Money = 4
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))

            t1.Money = 40
            Assert.AreEqual(IEntityFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1, Nothing))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinEval()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As IEntityFilter = New Criteria(GetType(Table1)).Field("Title").Eq("first").Filter(t)
            Dim t2 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            Assert.AreEqual(IEntityFilter.EvalResult.Unknown, c.Eval(mgr.ObjectSchema, t2, mgr.ObjectSchema.GetObjectSchema(t)))

            t2 = mgr.Find(Of Table2)(1)
            Assert.AreEqual(IEntityFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t2, mgr.ObjectSchema.GetObjectSchema(t)))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As ICollection(Of Table2) = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
            Assert.AreEqual(2, c.Count)

            Dim t2 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            t2.Tbl = mgr.Find(Of Table1)(1)
            mgr.BeginTransaction()
            Try
                t2.Save(True)
                c = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
                Assert.AreEqual(3, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache2()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As ICollection(Of Table2) = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
            Assert.AreEqual(2, c.Count)

            Dim t2 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            Dim t1 As New Table1(110, mgr.Cache, mgr.ObjectSchema)
            mgr.NewObjectManager.AddNew(t2)
            mgr.NewObjectManager.AddNew(t1)
            t1.Name = "bjkb"
            t1.CreatedAt = CDate("2006-01-01")
            Dim r As New OrmBase.RelatedObject(t1, t2, New String() {"ID"})
            t2.Tbl = t1
            mgr.BeginTransaction()
            Try
                t1.Save(True)
                c = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestJoinUpdateCache3()
        Dim tm As New TestManagerRS
        Using mgr As OrmReadOnlyDBManager = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim c As ICollection(Of Table2) = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
            Assert.AreEqual(2, c.Count)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            t1.Name = "sdfasdf"
            mgr.BeginTransaction()
            Try
                t1.Save(True)
                c = mgr.Find(Of Table2)(New Criteria(GetType(Table1)).Field("Title").Eq("first"), Nothing, False)
                Assert.AreEqual(0, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
