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
            Dim f As New OrmFilter(t, "DT", New Worm.TypeWrap(Of Object)(CDate("2007-01-01")), FilterOperation.GreaterEqualThan)
            Dim f2 As New OrmFilter(t, "Code", New Worm.TypeWrap(Of Object)(2), FilterOperation.NotEqual)
            Dim c As New OrmCondition(f, f2, ConditionOperator.And)

            Dim t1 As New Table1(1, mgr.Cache, mgr.ObjectSchema)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 2
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 3
            Assert.AreEqual(IOrmFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval2()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table1)
            Dim f As New OrmFilter(t, "DT", New Worm.TypeWrap(Of Object)(CDate("2007-01-01")), FilterOperation.Equal)
            Dim f2 As New OrmFilter(t, "Code", New Worm.TypeWrap(Of Object)(10), FilterOperation.LessThan)
            Dim c As New OrmCondition(f, f2, ConditionOperator.Or)

            Dim t1 As New Table1(1, mgr.Cache, mgr.ObjectSchema)
            t1.CreatedAt = CDate("2006-01-01")
            t1.Code = 2
            Assert.AreEqual(IOrmFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1))

            t1.CreatedAt = CDate("2008-01-01")
            t1.Code = 20
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.CreatedAt = CDate("2007-01-01")
            t1.Code = 30
            Assert.AreEqual(IOrmFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval3()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = mgr.Find(Of Table1)(1)
            Dim f As New OrmFilter(t, "Table1", tbl, FilterOperation.Equal)
            Dim f2 As New OrmFilter(t, "Money", New Worm.TypeWrap(Of Object)(CDec(10)), FilterOperation.GreaterThan)
            Dim c As New OrmCondition(f, f2, ConditionOperator.And)

            Dim t1 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            t1.Money = 4
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.Money = 40
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.Money = 40
            Assert.AreEqual(IOrmFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEval4()
        Dim tm As New TestManagerRS
        Using mgr As OrmManagerBase = tm.CreateManager(tm.GetSchema("1"))
            Dim t As Type = GetType(Table2)
            Dim tbl As Table1 = mgr.Find(Of Table1)(1)
            Dim f As New OrmFilter(t, "Table1", CType(Nothing, OrmBase), FilterOperation.Equal)
            Dim f2 As New OrmFilter(t, "Money", New Worm.TypeWrap(Of Object)(CDec(10)), FilterOperation.GreaterThan)
            Dim c As New OrmCondition(f, f2, ConditionOperator.And)

            Dim t1 As New Table2(1, mgr.Cache, mgr.ObjectSchema)
            t1.Money = 4
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.Money = 40
            Assert.AreEqual(IOrmFilter.EvalResult.Found, c.Eval(mgr.ObjectSchema, t1))

            t1.Tbl = tbl
            t1.Money = 4
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))

            t1.Money = 40
            Assert.AreEqual(IOrmFilter.EvalResult.NotFound, c.Eval(mgr.ObjectSchema, t1))
        End Using
    End Sub
End Class
