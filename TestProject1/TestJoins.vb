Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database.Criteria.Joins
Imports Worm.Orm.Meta
Imports Worm.Database
Imports Worm.Database.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core

<TestClass()> Public Class TestJoins

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation()
    '    Dim j As OrmJoin = Nothing

    '    Assert.IsTrue(j.IsEmpty)

    '    j.MakeSQLStmt(Nothing, Nothing, Nothing)
    'End Sub

    <TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    Public Sub TestMakeSQLStmt()
        Dim j As New OrmJoin(New OrmTable("table1"), Worm.Criteria.Joins.JoinType.Join, Nothing)

        Assert.IsNull(j.Condition)

        Dim schema As New SQLGenerator("1")

        j.MakeSQLStmt(schema, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim j As New OrmJoin(New OrmTable("table1"), Worm.Criteria.Joins.JoinType.Join, _
            New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")

        j.MakeSQLStmt(schema, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt3()
        Dim schema As New SQLGenerator("1")
        Dim t As OrmTable = schema.GetTables(GetType(Entity))(0)
        Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, _
            New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t)
        j.MakeSQLStmt(schema, Nothing, almgr, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.QueryGeneratorException))> _
    Public Sub TestMakeSQLStmt4()
        Dim t As New OrmTable("table1")
        Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t)
        Dim pmgr As New ParamMgr(schema, "p")
        j.MakeSQLStmt(schema, Nothing, almgr, pmgr)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt5()
        Dim t As New OrmTable("table1")
        Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.id = @p1", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt6()
        Dim t As New OrmTable("table1")
        Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, _
            New EntityFilter(GetType(Entity), "ID", New LiteralValue("1"), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.id = 1", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(0, pmgr.Params.Count)

        Assert.AreEqual(" join table1 t1 on t2.id = 1", j.MakeSQLStmt(schema, Nothing, almgr, Nothing))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt7()
        Dim t As New OrmTable("table1")
        Dim t2 As New OrmTable("table2")

        Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, _
            New TableFilter(t2, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t)
        almgr.AddTable(t2)
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t1 on t2.ID = @p1", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()> _
    Public Sub TestReplaceFilter()
        Dim cc As New Criteria.Conditions.Condition.ConditionConstructor

        Assert.IsNull(cc.Condition)

        Dim t As Type = GetType(Entity)
        Dim tbl As New OrmTable("table1")
        Dim f As New JoinFilter(tbl, "id", t, "ID", Worm.Criteria.FilterOperation.Equal)
        cc.AddFilter(f)

        Assert.AreEqual(cc.Condition, f)

        cc.AddFilter(New TableFilter(tbl, "s", New ScalarValue("1"), Worm.Criteria.FilterOperation.Equal))
        Dim j As New OrmJoin(tbl, Worm.Criteria.Joins.JoinType.Join, CType(cc.Condition, Worm.Criteria.Core.IFilter))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetTables(t)(0))
        almgr.AddTable(tbl)
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual(" join table1 t2 on (t2.id = t1.id and t2.s = @p1)", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual("1", pmgr.GetParameter("@p1").Value)

        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(10), Worm.Criteria.FilterOperation.Equal)
        j.ReplaceFilter(f, f2)

        Assert.AreEqual(" join table1 t2 on (t2.id = @p2 and t2.s = @p1)", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(2, pmgr.Params.Count)

        Assert.AreEqual(10, pmgr.GetParameter("@p2").Value)

        cc.AddFilter(f2)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt8()
        Dim tbl As New OrmTable("table1")
        Dim j As New OrmJoin(tbl, Worm.Criteria.Joins.JoinType.FullJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(tbl)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual(" full join table1 t1 on t2.id = @p1", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)

        j = New OrmJoin(tbl, Worm.Criteria.Joins.JoinType.LeftOuterJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Assert.AreEqual(" left join table1 t1 on t2.id = @p2", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        j = New OrmJoin(tbl, Worm.Criteria.Joins.JoinType.RightOuterJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Assert.AreEqual(" right join table1 t1 on t2.id = @p3", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))

        j = New OrmJoin(tbl, Worm.Criteria.Joins.JoinType.CrossJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Assert.AreEqual(" cross join table1 t1 on t2.id = @p4", j.MakeSQLStmt(schema, Nothing, almgr, pmgr))
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
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Criteria.Conditions.Condition(f, Nothing, ConditionOperator.Or)

        Assert.AreEqual(f.Template.GetStaticString & " or ", c.Template.GetStaticString)
        Assert.AreEqual(f.ToString & " or ", c.ToString)
        Assert.IsTrue(c.Equals(New Criteria.Conditions.Condition(f, Nothing, ConditionOperator.Or)))
        Assert.IsFalse(c.Equals(Nothing))

        Assert.AreEqual(1, c.GetAllFilters.Count)

        c.MakeQueryStmt(Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Criteria.Conditions.Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New SQLGenerator("1")

        c.MakeQueryStmt(schema, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Criteria.Conditions.Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        c.MakeQueryStmt(schema, Nothing, almgr, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt4()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Criteria.Conditions.Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        Dim pmgr As New ParamMgr(schema, "p")
        Assert.AreEqual("t1.id = @p1", c.MakeQueryStmt(schema, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt5()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New OrmTable("table1")
        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterThan)
        Dim c As New Criteria.Conditions.Condition(f, f2, ConditionOperator.Or)

        Assert.AreEqual(f.Template.GetStaticString & " or " & f2.Template.GetStaticString, c.Template.GetStaticString)
        Assert.AreEqual(2, c.GetAllFilters.Count)

        Assert.AreEqual(f.ToString & " or " & f2.ToString, c.ToString)
        Assert.AreEqual(c, New Criteria.Conditions.Condition(f, f2, ConditionOperator.Or))

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        almgr.AddTable(tbl)
        Dim pmgr As New ParamMgr(schema, "p")
        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeQueryStmt(schema, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace()

        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New OrmTable("table1")
        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterThan)
        Dim c As New Criteria.Conditions.Condition(f, f2, ConditionOperator.Or)

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        almgr.AddTable(tbl)
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeQueryStmt(schema, Nothing, almgr, pmgr))

        Dim f3 As New TableFilter(tbl, "id", New ScalarValue(10), Worm.Criteria.FilterOperation.NotEqual)

        Dim c2 As IFilter = CType(c.ReplaceFilter(f2, f3), IFilter)

        Assert.AreNotEqual(c, c2)

        Assert.AreEqual("(t1.id = @p1 or t2.id <> @p3)", c2.MakeQueryStmt(schema, Nothing, almgr, pmgr))

        Dim c3 As New Criteria.Conditions.Condition(c, f3, ConditionOperator.And)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and t2.id <> @p3)", c3.MakeQueryStmt(schema, Nothing, almgr, pmgr))

        Dim c4 As IFilter = CType(c3.ReplaceFilter(f2, f3), IFilter)

        Assert.AreEqual("((t1.id = @p1 or t2.id <> @p3) and t2.id <> @p3)", c4.MakeQueryStmt(schema, Nothing, almgr, pmgr))

        Dim c5 As IFilter = CType(c3.ReplaceFilter(f3, c4), IFilter)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and ((t1.id = @p1 or t2.id <> @p3) and t2.id <> @p3))", c5.MakeQueryStmt(schema, Nothing, almgr, pmgr))

        Dim c6 As IFilter = CType(c5.ReplaceFilter(f3, Nothing), IFilter)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and (t1.id = @p1 or t2.id <> @p3))", c6.MakeQueryStmt(schema, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace2()
        Dim schema As New SQLGenerator("1")
        Dim f As New JoinFilter(GetType(Entity), "ID", GetType(Entity2), "oqwef", Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New OrmTable("table1")
        Dim ct As New Criteria.Conditions.Condition.ConditionConstructor
        ct.AddFilter(f, ConditionOperator.Or)
        Dim j As New OrmJoin(schema.GetTables(GetType(Entity))(0), Worm.Criteria.Joins.JoinType.Join, f)
        j.InjectJoinFilter(GetType(Entity), "ID", tbl, "onadg")

        Dim j2 As New OrmJoin(schema.GetTables(GetType(Entity))(0), Worm.Criteria.Joins.JoinType.Join, f)
        j2.InjectJoinFilter(GetType(Entity2), "oqwef", tbl, "onadg")
    End Sub

    <TestMethod()> _
    Public Sub TestReplace3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim f2 As New EntityFilter(GetType(Entity4), "Title", New ScalarValue("alex"), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Criteria.Conditions.Condition(f, f2, ConditionOperator.Or)

        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity)).GetTables(0))
        almgr.AddTable(schema.GetObjectSchema(GetType(Entity4)).GetTables(0))
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.name = @p2)", c.MakeQueryStmt(schema, Nothing, almgr, pmgr))
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
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)

        f.MakeQueryStmt(Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt2()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim schema As New SQLGenerator("1")

        Dim e As New Entity(1, Nothing, schema)

        Assert.AreEqual(IEvaluableValue.EvalResult.Found, f.Eval(schema, e, Nothing))
        Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, f.Eval(schema, New Entity(2, Nothing, schema), Nothing))

        f.MakeQueryStmt(schema, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestMakeSQLStmt3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)
        Dim schema As New SQLGenerator("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual("t1.id >= @p1", f.MakeQueryStmt(schema, Nothing, almgr, pmgr))
        Assert.AreEqual(1, pmgr.Params.Count)

        Dim p As Pair(Of String) = f.MakeSingleQueryStmt(schema, almgr, pmgr)

        Assert.AreEqual("id", p.First)
        Assert.AreEqual("@p1", p.Second)
        Assert.AreEqual(1, pmgr.Params.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt4()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)

        Assert.AreEqual("TestProject1.EntityIDGreaterEqualThan", f.Template.GetStaticString)

        Assert.AreEqual(1, f.GetAllFilters.Count)

        f.MakeSingleQueryStmt(Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt5()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)
        Dim schema As New SQLGenerator("1")
        f.MakeSingleQueryStmt(schema, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestReplace()
        Dim schema As New SQLGenerator("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToParam(f, GetType(Entity2), "ID", New Worm.TypeWrap(Of Object)(345))

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToParam(f2, GetType(Entity2), "ID", New Worm.TypeWrap(Of Object)(345))
        JoinFilter.ChangeEntityJoinToParam(f2, t, "ID", New Worm.TypeWrap(Of Object)(345))

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToParam(f3, GetType(Entity), "ID", New Worm.TypeWrap(Of Object)(345))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace2()
        Dim schema As New SQLGenerator("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToLiteral(f, GetType(Entity2), "ID", "pmqer")

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(f2, GetType(Entity2), "ID", "pmqer")
        JoinFilter.ChangeEntityJoinToLiteral(f2, t, "ID", "pmqer")

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(f3, GetType(Entity), "ID", "pmqer")
    End Sub

    <TestMethod()> _
    Public Sub TestReplace3()
        Dim schema As New SQLGenerator("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToLiteral(f, GetType(Entity2), "ID", "pmqer")

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(f2, GetType(Entity2), "ID", "pmqer")
        JoinFilter.ChangeEntityJoinToLiteral(f2, t, "ID", "pmqer")

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(f3, GetType(Entity), "ID", "pmqer")
    End Sub

    <TestMethod()> _
    Public Sub TestMakeHash()
        Dim schema As New SQLGenerator("1")
        Dim t As Type = GetType(Entity)

        Dim f As New EntityFilter(t, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Assert.AreEqual(f.ToString, f.MakeHash)

        Dim o As New Entity(1, Nothing, schema)

        Assert.AreEqual(f.MakeHash, f.Template.MakeHash(schema, Nothing, o))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeHash2()
        Dim schema As New SQLGenerator("1")
        Dim t As Type = GetType(Entity2)

        Dim f As New EntityFilter(t, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim f2 As New EntityFilter(t, "Str", New ScalarValue("d"), Worm.Criteria.FilterOperation.Like)
        Dim cAnd As New Criteria.Conditions.Condition.ConditionConstructor
        cAnd.AddFilter(f).AddFilter(f2)

        Dim cOr As New Criteria.Conditions.Condition.ConditionConstructor
        cOr.AddFilter(f).AddFilter(f2, ConditionOperator.Or)

        Assert.AreEqual(f.ToString, CType(cAnd.Condition, IEntityFilter).MakeHash)
        Assert.AreEqual(CType(cOr.Condition, IEntityFilter).MakeHash, EntityFilter.EmptyHash)

        Dim o As New Entity2(1, Nothing, schema)

        Assert.AreEqual(CType(cAnd.Condition, IEntityFilter).MakeHash, CType(cAnd.Condition, IEntityFilter).GetFilterTemplate.MakeHash(schema, Nothing, o))
        Assert.AreEqual(EntityFilter.EmptyHash, CType(cOr.Condition, IEntityFilter).GetFilterTemplate.MakeHash(schema, Nothing, o))
    End Sub
End Class
