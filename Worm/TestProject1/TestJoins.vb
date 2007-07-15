Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics
Imports CoreFramework.Structures

<TestClass()> Public Class TestJoins

    <TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    Public Sub TestCreation()
        Dim j As Orm.OrmJoin = Nothing

        Assert.IsTrue(j.IsEmpty)

        j.MakeSQLStmt(Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    Public Sub TestMakeSQLStmt()
        Dim j As New Orm.OrmJoin(New Orm.OrmTable("table1"), Orm.JoinType.Join, Nothing)

        Assert.IsNull(j.Condition)

        Dim schema As New Orm.DbSchema("1")

        j.MakeSQLStmt(schema, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim j As New Orm.OrmJoin(New Orm.OrmTable("table1"), Orm.JoinType.Join, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")

        j.MakeSQLStmt(schema, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt3()
        Dim t As New Orm.OrmTable("table1")
        Dim j As New Orm.OrmJoin(t, Orm.JoinType.Join, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(t)
        j.MakeSQLStmt(schema, almgr.Aliases, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Collections.Generic.KeyNotFoundException))> _
    Public Sub TestMakeSQLStmt4()
        Dim t As New Orm.OrmTable("table1")
        Dim j As New Orm.OrmJoin(t, Orm.JoinType.Join, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(t)
        Dim pmgr As New Orm.ParamMgr(schema, "p")
        j.MakeSQLStmt(schema, almgr.Aliases, pmgr)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt5()
        Dim t As New Orm.OrmTable("table1")
        Dim j As New Orm.OrmJoin(t, Orm.JoinType.Join, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.id = @p1", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt6()
        Dim t As New Orm.OrmTable("table1")
        Dim j As New Orm.OrmJoin(t, Orm.JoinType.Join, _
            New Orm.OrmFilter(GetType(Entity), "ID", "1", Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.id = 1", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(0, pmgr.Params.Count)

        Assert.AreEqual(" join table1 t1 on t2.id = 1", j.MakeSQLStmt(schema, almgr.Aliases, Nothing))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt7()
        Dim t As New Orm.OrmTable("table1")
        Dim t2 As New Orm.OrmTable("table2")

        Dim j As New Orm.OrmJoin(t, Orm.JoinType.Join, _
            New Orm.OrmFilter(t2, "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(t2)
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.ID = @p1", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()> _
    Public Sub TestReplaceFilter()
        Dim cc As New Orm.OrmCondition.OrmConditionConstructor

        Assert.IsNull(cc.Condition)

        Dim t As Type = GetType(Entity)
        Dim tbl As New Orm.OrmTable("table1")
        Dim f As New Orm.OrmFilter(tbl, "id", t, "ID", Orm.FilterOperation.Equal)
        cc.AddFilter(f)

        Assert.AreEqual(cc.Condition, f)

        cc.AddFilter(New Orm.OrmFilter(tbl, "s", New TypeWrap(Of Object)("1"), Orm.FilterOperation.Equal))
        Dim j As New Orm.OrmJoin(tbl, Orm.JoinType.Join, cc.Condition)

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetTables(t)(0))
        almgr.AddTable(tbl)
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t2 on (t2.id = t1.id and t2.s = @p1)", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual("1", pmgr.GetParameter("@p1").Value)

        Dim f2 As New Orm.OrmFilter(tbl, "id", New TypeWrap(Of Object)(10), Orm.FilterOperation.Equal)
        j.ReplaceFilter(f, f2)

        Assert.AreEqual(" join table1 t2 on (t2.id = @p2 and t2.s = @p1)", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(2, pmgr.Params.Count)

        Assert.AreEqual(10, pmgr.GetParameter("@p2").Value)

        cc.AddFilter(f2)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt8()
        Dim tbl As New Orm.OrmTable("table1")
        Dim j As New Orm.OrmJoin(tbl, Orm.JoinType.FullJoin, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(tbl)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual(" full join table1 t1 on t2.id = @p1", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)

        j = New Orm.OrmJoin(tbl, Orm.JoinType.LeftOuterJoin, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Assert.AreEqual(" left join table1 t1 on t2.id = @p2", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        j = New Orm.OrmJoin(tbl, Orm.JoinType.RightOuterJoin, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Assert.AreEqual(" right join table1 t1 on t2.id = @p3", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        j = New Orm.OrmJoin(tbl, Orm.JoinType.CrossJoin, New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal))

        Assert.AreEqual(" cross join table1 t1 on t2.id = @p4", j.MakeSQLStmt(schema, almgr.Aliases, pmgr))
    End Sub
End Class

<TestClass()> Public Class TestCondition

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation()
    '    Dim c As Orm.OrmCondition = Nothing

    '    Assert.AreEqual(String.Empty, c.ToString)

    '    Assert.IsFalse(c.Equals("asdf"))

    '    Assert.AreEqual(String.Empty.GetHashCode, c.GetHashCode)

    '    c.GetStaticString()
    'End Sub

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation2()
    '    Dim c As Orm.OrmCondition = Nothing

    '    c.MakeSQLStmt(Nothing, Nothing, Nothing)
    'End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim c As New Orm.OrmCondition(f, Nothing, Orm.ConditionOperator.Or)

        Assert.AreEqual(f.GetStaticString & " or ", c.GetStaticString)
        Assert.AreEqual(f.ToString & " or ", c.ToString)
        Assert.IsTrue(c.Equals(New Orm.OrmCondition(f, Nothing, Orm.ConditionOperator.Or)))
        Assert.IsFalse(c.Equals(Nothing))

        Assert.AreEqual(1, c.GetAllFilters.Count)

        c.MakeSQLStmt(Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim c As New Orm.OrmCondition(f, Nothing, Orm.ConditionOperator.Or)

        Dim schema As New Orm.DbSchema("1")

        c.MakeSQLStmt(schema, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt3()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim c As New Orm.OrmCondition(f, Nothing, Orm.ConditionOperator.Or)

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        c.MakeSQLStmt(schema, almgr.Aliases, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt4()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim c As New Orm.OrmCondition(f, Nothing, Orm.ConditionOperator.Or)

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        Dim pmgr As New Orm.ParamMgr(schema, "p")
        Assert.AreEqual("t1.id = @p1", c.MakeSQLStmt(schema, almgr.Aliases, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt5()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim tbl As New Orm.OrmTable("table1")
        Dim f2 As New Orm.OrmFilter(tbl, "id", New TypeWrap(Of Object)(1), Orm.FilterOperation.GreaterThan)
        Dim c As New Orm.OrmCondition(f, f2, Orm.ConditionOperator.Or)

        Assert.AreEqual(f.GetStaticString & " or " & f2.GetStaticString, c.GetStaticString)
        Assert.AreEqual(2, c.GetAllFilters.Count)

        Assert.AreEqual(f.ToString & " or " & f2.ToString, c.ToString)
        Assert.AreEqual(c, New Orm.OrmCondition(f, f2, Orm.ConditionOperator.Or))

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        almgr.AddTable(tbl)
        Dim pmgr As New Orm.ParamMgr(schema, "p")
        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeSQLStmt(schema, almgr.Aliases, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace()

        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim tbl As New Orm.OrmTable("table1")
        Dim f2 As New Orm.OrmFilter(tbl, "id", New TypeWrap(Of Object)(1), Orm.FilterOperation.GreaterThan)
        Dim c As New Orm.OrmCondition(f, f2, Orm.ConditionOperator.Or)

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        almgr.AddTable(tbl)
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Dim f3 As New Orm.OrmFilter(tbl, "id", New TypeWrap(Of Object)(10), Orm.FilterOperation.NotEqual)

        Dim c2 As Orm.OrmCondition = c.ReplaceFilter(f2, f3)

        Assert.AreNotEqual(c, c2)

        Assert.AreEqual("(t1.id = @p1 or t2.id <> @p3)", c2.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Dim c3 As New Orm.OrmCondition(c, f3, Orm.ConditionOperator.And)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and t2.id <> @p3)", c3.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Dim c4 As Orm.OrmCondition = c3.ReplaceFilter(f2, f3)

        Assert.AreEqual("((t1.id = @p1 or t2.id <> @p3) and t2.id <> @p3)", c4.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Dim c5 As Orm.OrmCondition = c3.ReplaceFilter(f3, c4)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and ((t1.id = @p1 or t2.id <> @p3) and t2.id <> @p3))", c5.MakeSQLStmt(schema, almgr.Aliases, pmgr))

        Dim c6 As Orm.OrmCondition = c5.ReplaceFilter(f3, Nothing)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and (t1.id = @p1 or t2.id <> @p3))", c6.MakeSQLStmt(schema, almgr.Aliases, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace2()
        Dim schema As New Orm.DbSchema("1")
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", GetType(Entity2), "oqwef", Orm.FilterOperation.Equal)
        Dim tbl As New Orm.OrmTable("table1")
        Dim ct As New Orm.OrmCondition.OrmConditionConstructor
        ct.AddFilter(f, Orm.ConditionOperator.Or)
        Dim j As New Orm.OrmJoin(schema.GetTables(GetType(Entity))(0), Orm.JoinType.Join, f)
        Orm.OrmFilter.ChangeValueToLiteral(j, GetType(Entity), "ID", tbl, "onadg")

        Dim j2 As New Orm.OrmJoin(schema.GetTables(GetType(Entity))(0), Orm.JoinType.Join, f)
        Orm.OrmFilter.ChangeValueToLiteral(j2, GetType(Entity2), "oqwef", tbl, "onadg")
    End Sub
End Class

<TestClass()> Public Class TestFilters

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation()
    '    Dim f As Orm.OrmFilter = Nothing

    '    Assert.AreEqual(String.Empty, f.ToString)

    '    Assert.IsFalse(f.Equals("sdf"))

    '    f.MakeSQLStmt(Nothing, Nothing, Nothing)
    'End Sub

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation2()
    '    Dim f As Orm.OrmFilter = Nothing

    '    f.MakeSignleStmt(Nothing, Nothing)
    'End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)

        f.MakeSQLStmt(Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.Equal)
        Dim schema As New Orm.DbSchema("1")

        Dim e As New Entity(1, Nothing, schema)

        Assert.AreEqual(Orm.IOrmFilter.EvalResult.Found, f.Eval(schema, e, Nothing))
        Assert.AreEqual(Orm.IOrmFilter.EvalResult.NotFound, f.Eval(schema, New Entity(2, Nothing, schema), Nothing))

        f.MakeSQLStmt(schema, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt3()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.GreaterEqualThan)
        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        Assert.AreEqual("t1.id >= @p1", f.MakeSQLStmt(schema, almgr.Aliases, pmgr))
        Assert.AreEqual(1, pmgr.Params.Count)

        Dim p As Pair(Of String) = f.MakeSignleStmt(schema, pmgr)

        Assert.AreEqual("id", p.First)
        Assert.AreEqual("@p1", p.Second)
        Assert.AreEqual(1, pmgr.Params.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt4()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.GreaterEqualThan)

        Assert.AreEqual("EntityID >= ", f.GetStaticString)

        Assert.AreEqual(1, f.GetAllFilters.Count)

        f.MakeSignleStmt(Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt5()
        Dim f As New Orm.OrmFilter(GetType(Entity), "ID", New TypeWrap(Of Object)(1), Orm.FilterOperation.GreaterEqualThan)
        Dim schema As New Orm.DbSchema("1")
        f.MakeSignleStmt(schema, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestReplace()
        Dim schema As New Orm.DbSchema("1")
        Dim t As Type = GetType(Entity)
        Dim f As New Orm.OrmFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)

        Orm.OrmFilter.ChangeValueToParam(f, GetType(Entity2), "ID", 345)

        Dim f2 As New Orm.OrmFilter(t, "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToParam(f2, GetType(Entity2), "ID", 345)
        Orm.OrmFilter.ChangeValueToParam(f2, t, "ID", 345)

        Dim f3 As New Orm.OrmFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToParam(f3, GetType(Entity), "ID", 345)
    End Sub

    <TestMethod()> _
    Public Sub TestReplace2()
        Dim schema As New Orm.DbSchema("1")
        Dim t As Type = GetType(Entity)
        Dim f As New Orm.OrmFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)

        Orm.OrmFilter.ChangeValueToLiteral(f, GetType(Entity2), "ID", "pmqer")

        Dim f2 As New Orm.OrmFilter(t, "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToLiteral(f2, GetType(Entity2), "ID", "pmqer")
        Orm.OrmFilter.ChangeValueToLiteral(f2, t, "ID", "pmqer")

        Dim f3 As New Orm.OrmFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToLiteral(f3, GetType(Entity), "ID", "pmqer")
    End Sub

    <TestMethod()> _
    Public Sub TestReplace3()
        Dim schema As New Orm.DbSchema("1")
        Dim t As Type = GetType(Entity)
        Dim f As New Orm.OrmFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)

        Orm.OrmFilter.ChangeValueToLiteral(f, GetType(Entity2), "ID", "pmqer")

        Dim f2 As New Orm.OrmFilter(t, "ID", GetType(Entity2), "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToLiteral(f2, GetType(Entity2), "ID", "pmqer")
        Orm.OrmFilter.ChangeValueToLiteral(f2, t, "ID", "pmqer")

        Dim f3 As New Orm.OrmFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Orm.FilterOperation.GreaterEqualThan)
        Orm.OrmFilter.ChangeValueToLiteral(f3, GetType(Entity), "ID", "pmqer")
    End Sub
End Class