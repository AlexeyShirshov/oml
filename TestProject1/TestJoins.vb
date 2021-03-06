Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Entities.Meta
Imports Worm.Database
Imports Worm.Criteria.Values
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports System.Text
Imports CoreFramework.cfStructures
Imports System.Linq

<TestClass()> Public Class TestJoins

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreation()
    '    Dim j As OrmJoin = Nothing

    '    Assert.IsTrue(j.IsEmpty)

    '    j.MakeSQLStmt(Nothing, Nothing, Nothing)
    'End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt()
        Dim j As New QueryJoin(New SourceFragment("table1"), Worm.Criteria.Joins.JoinType.Join, Nothing)

        Assert.IsNull(j.Condition)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQL2000Generator
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, Nothing, Nothing, Nothing, New StringBuilder)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt2()
        Dim j As New QueryJoin(New SourceFragment("table1"), Worm.Criteria.Joins.JoinType.Join,
            New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQL2000Generator
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, Nothing, Nothing, Nothing, New StringBuilder)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt3()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim t As SourceFragment = schema.GetTables(GetType(Entity))(0)
        Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join,
            New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t, Nothing)
        Dim gen As New SQL2000Generator
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, Nothing, Nothing, New StringBuilder)
    End Sub

    '<TestMethod(), ExpectedException(GetType(Worm.ObjectMappingException))> _
    'Public Sub TestMakeSQLStmt4()
    '    Dim t As New SourceFragment("table1")
    '    Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

    '    Dim schema As New Worm.ObjectMappingEngine("1")
    '    Dim almgr As AliasMgr = AliasMgr.Create
    '    almgr.AddTable(t, Nothing)
    '    Dim gen As New SQLGenerator
    '    Dim pmgr As New ParamMgr(gen, "p")
    '    j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, New StringBuilder)
    'End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt5()
        Dim t As New SourceFragment("table1")
        Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t, Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Dim sb As New StringBuilder
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" join table1 t1 on t2.id = @p1", sb.ToString)

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt6()
        Dim t As New SourceFragment("table1")
        Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join,
            New EntityFilter(GetType(Entity), "ID", New LiteralValue("1"), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t, Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Dim sb As New StringBuilder
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" join table1 t1 on t2.id = 1", sb.ToString)

        Assert.AreEqual(0, pmgr.Params.Count)
        sb.Length = 0
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, Nothing, Nothing, sb)
        Assert.AreEqual(" join table1 t1 on t2.id = 1", sb.ToString)
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt7()
        Dim t As New SourceFragment("table1")
        Dim t2 As New SourceFragment("table2")

        Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join,
            New TableFilter(t2, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(t, Nothing)
        almgr.AddTable(t2, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Dim sb As New StringBuilder
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" join table1 t1 on t2.ID = @p1", sb.ToString)

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)
    End Sub

    <TestMethod()>
    Public Sub TestReplaceFilter()
        Dim cc As New Condition.ConditionConstructor

        Assert.IsNull(cc.Condition)

        Dim t As Type = GetType(Entity)
        Dim tbl As New SourceFragment("table1")
        Dim f As New JoinFilter(tbl, "id", t, "ID", Worm.Criteria.FilterOperation.Equal)
        cc.AddFilter(f)

        Assert.AreEqual(cc.Condition, f)

        cc.AddFilter(New TableFilter(tbl, "s", New ScalarValue("1"), Worm.Criteria.FilterOperation.Equal))
        Dim j As New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.Join, CType(cc.Condition, Worm.Criteria.Core.IFilter))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetTables(t)(0), Nothing)
        almgr.AddTable(tbl, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Dim sb As New StringBuilder
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" join table1 t2 on (t2.id = t1.id and t2.s = @p1)", sb.ToString)

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual("1", pmgr.GetParameter("@p1").Value)

        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(10), Worm.Criteria.FilterOperation.Equal)
        j.ReplaceFilter(f, f2)
        sb.Length = 0
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" join table1 t2 on (t2.id = @p2 and t2.s = @p3)", sb.ToString)

        Assert.AreEqual(3, pmgr.Params.Count)

        Assert.AreEqual(10, pmgr.GetParameter("@p2").Value)

        cc.AddFilter(f2)
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt8()
        Dim tbl As New SourceFragment("table1")
        Dim j As New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.FullJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(tbl, Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Dim sb As New StringBuilder
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" full join table1 t1 on t2.id = @p1", sb.ToString)

        Assert.AreEqual(1, pmgr.Params.Count)

        Assert.AreEqual(1, pmgr.GetParameter("@p1").Value)

        j = New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.LeftOuterJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))
        sb.Length = 0
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" left join table1 t1 on t2.id = @p2", sb.ToString)

        j = New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.RightOuterJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))
        sb.Length = 0
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" right join table1 t1 on t2.id = @p3", sb.ToString)

        j = New QueryJoin(tbl, Worm.Criteria.Joins.JoinType.CrossJoin, New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal))

        sb.Length = 0
        j.MakeSQLStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr, Nothing, sb)
        Assert.AreEqual(" cross join table1 t1", sb.ToString)
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

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Condition(f, Nothing, ConditionOperator.Or)

        Assert.AreEqual(f.Template.GetStaticString(Nothing) & " or ", c.Template.GetStaticString(Nothing))
        Assert.AreEqual(f.ToString & " or ", c.ToString)
        Assert.IsTrue(c.Equals(New Condition(f, Nothing, ConditionOperator.Or)))
        Assert.IsFalse(c.Equals(Nothing))

        Assert.AreEqual(1, c.GetAllFilters.Length)

        c.MakeQueryStmt(Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt2()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQL2000Generator
        c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        Dim gen As New SQL2000Generator
        c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, Nothing)
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt4()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Condition(f, Nothing, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Assert.AreEqual("t1.id = @p1", c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt5()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New SourceFragment("table1")
        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterThan)
        Dim c As New Condition(f, f2, ConditionOperator.Or)
        Dim schema As New Worm.ObjectMappingEngine("1")

        Assert.AreEqual(f.Template.GetStaticString(schema) & " or " & f2.Template.GetStaticString(Nothing), c.Template.GetStaticString(schema))
        Assert.AreEqual(2, c.GetAllFilters.Length)

        Assert.AreEqual(f.ToString & " or " & f2.ToString, c.ToString)
        Assert.AreEqual(c, New Condition(f, f2, ConditionOperator.Or))

        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        almgr.AddTable(tbl, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")
        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()>
    Public Sub TestReplace()

        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New SourceFragment("table1")
        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterThan)
        Dim c As New Condition(f, f2, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        almgr.AddTable(tbl, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim f3 As New TableFilter(tbl, "id", New ScalarValue(10), Worm.Criteria.FilterOperation.NotEqual)

        Dim c2 As IFilter = CType(c.ReplaceFilter(f2, f3), IFilter)

        Assert.AreNotEqual(c, c2)

        Assert.AreEqual("(t1.id = @p3 or t2.id <> @p4)", c2.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim c3 As New Condition(c, f3, ConditionOperator.And)

        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and t2.id <> @p5)", c3.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim c4 As IFilter = CType(c3.ReplaceFilter(f2, f3), IFilter)

        Assert.AreEqual("((t1.id = @p6 or t2.id <> @p7) and t2.id <> @p8)", c4.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim c5 As IFilter = CType(c3.ReplaceFilter(f3, c4), IFilter)

        Assert.AreEqual("((t1.id = @p9 or t2.id > @p10) and ((t1.id = @p11 or t2.id <> @p12) and t2.id <> @p13))", c5.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim c6 As IFilter = CType(c5.ReplaceFilter(f3, Nothing), IFilter)

        Assert.AreEqual("((t1.id = @p14 or t2.id > @p15) and (t1.id = @p16 or t2.id <> @p17))", c6.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()>
    Public Sub TestRemove()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New SourceFragment("table1")
        Dim f2 As New TableFilter(tbl, "id", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterThan)
        Dim c As New Condition(f, f2, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        almgr.AddTable(tbl, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.id > @p2)", c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim tf As IFilter = c.RemoveFilter(f)
        Assert.AreEqual("(1 = 0 or t2.id > @p2)", tf.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim f3 As New TableFilter(tbl, "id", New ScalarValue(10), Worm.Criteria.FilterOperation.NotEqual)

        Dim c3 As New Condition(New Condition(f, f2, ConditionOperator.Or), f3, ConditionOperator.And)
        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and t2.id <> @p3)", c3.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        tf = c3.RemoveFilter(f3)
        Assert.AreEqual("((t1.id = @p1 or t2.id > @p2) and 1 = 1)", tf.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        c3 = New Condition(New Condition(f, f2, ConditionOperator.Or), f3, ConditionOperator.And)
        tf = c3.RemoveFilter(f2)
        Assert.AreEqual("((t1.id = @p1 or 1 = 0) and t2.id <> @p3)", tf.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
    End Sub

    <TestMethod()>
    Public Sub TestReplace2()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim f As New JoinFilter(GetType(Entity), "ID", GetType(Entity2), "oqwef", Worm.Criteria.FilterOperation.Equal)
        Dim tbl As New SourceFragment("table1")
        Dim ct As New Condition.ConditionConstructor
        ct.AddFilter(f, ConditionOperator.Or)
        Dim j As New QueryJoin(schema.GetTables(GetType(Entity))(0), Worm.Criteria.Joins.JoinType.Join, f)
        j.InjectJoinFilter(schema, GetType(Entity), "ID", tbl, "onadg")

        Dim j2 As New QueryJoin(schema.GetTables(GetType(Entity))(0), Worm.Criteria.Joins.JoinType.Join, f)
        j2.InjectJoinFilter(schema, GetType(Entity2), "oqwef", tbl, "onadg")
    End Sub

    <TestMethod()>
    Public Sub TestReplace3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim f2 As New EntityFilter(GetType(Entity4), "Title", New ScalarValue("alex"), Worm.Criteria.FilterOperation.Equal)
        Dim c As New Condition(f, f2, ConditionOperator.Or)

        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity)).Table, Nothing)
        almgr.AddTable(schema.GetEntitySchema(GetType(Entity4)).Table, Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        Assert.AreEqual("(t1.id = @p1 or t2.name = @p2)", c.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
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

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)

        f.MakeQueryStmt(CType(Nothing, Worm.ObjectMappingEngine), Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))>
    Public Sub TestMakeSQLStmt2()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim schema As New Worm.ObjectMappingEngine("1")

        Dim e As New Entity(1)

        Assert.AreEqual(IEvaluableValue.EvalResult.Found, f.Eval(schema, e, Nothing, Nothing, Nothing))
        Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, f.Eval(schema, New Entity(2), Nothing, Nothing, Nothing))

        f.MakeQueryStmt(schema, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod()>
    Public Sub TestMakeSQLStmt3()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        Assert.AreEqual("t1.id >= @p1", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        Assert.AreEqual(1, pmgr.Params.Count)

        Dim p = f.MakeSingleQueryStmt(schema, gen, almgr, pmgr, Nothing)

        Assert.AreEqual("id", p.First.Column)
        Assert.AreEqual("@p1", p.First.Param)
        Assert.AreEqual(1, pmgr.Params.Count)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt4()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)

        Assert.AreEqual("TestProject1.EntityIDGreaterEqualThan", f.Template.GetStaticString(Nothing))

        Assert.AreEqual(1, f.GetAllFilters.Length)
        Dim gen As New SQL2000Generator
        f.MakeSingleQueryStmt(Nothing, gen, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestMakeSQLStmt5()
        Dim f As New EntityFilter(GetType(Entity), "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.GreaterEqualThan)
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQL2000Generator
        f.MakeSingleQueryStmt(schema, gen, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestReplace()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToParam(schema, f, GetType(Entity2), "ID", New Worm.TypeWrap(Of Object)(345))

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToParam(schema, f2, GetType(Entity2), "ID", New Worm.TypeWrap(Of Object)(345))
        JoinFilter.ChangeEntityJoinToParam(schema, f2, t, "ID", New Worm.TypeWrap(Of Object)(345))

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToParam(schema, f3, GetType(Entity), "ID", New Worm.TypeWrap(Of Object)(345))
    End Sub

    <TestMethod()> _
    Public Sub TestReplace2()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToLiteral(schema, f, GetType(Entity2), "ID", {"pmqer"})

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(schema, f2, GetType(Entity2), "ID", {"pmqer"})
        JoinFilter.ChangeEntityJoinToLiteral(schema, f2, t, "ID", {"pmqer"})

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(schema, f3, GetType(Entity), "ID", {"pmqer"})
    End Sub

    <TestMethod()> _
    Public Sub TestReplace3()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim t As Type = GetType(Entity)
        Dim f As New JoinFilter(schema.GetTables(t)(0), "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)

        JoinFilter.ChangeEntityJoinToLiteral(schema, f, GetType(Entity2), "ID", {"pmqer"})

        Dim f2 As New JoinFilter(t, "ID", GetType(Entity2), "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(schema, f2, GetType(Entity2), "ID", {"pmqer"})
        JoinFilter.ChangeEntityJoinToLiteral(schema, f2, t, "ID", {"pmqer"})

        Dim f3 As New JoinFilter(schema.GetTables(GetType(Entity2))(0), "ID", t, "ID", Worm.Criteria.FilterOperation.GreaterEqualThan)
        JoinFilter.ChangeEntityJoinToLiteral(schema, f3, GetType(Entity), "ID", {"pmqer"})
    End Sub

    <TestMethod()> _
    Public Sub TestMakeHash()
        Dim mpe As New Worm.ObjectMappingEngine("1")
        Dim t As Type = GetType(Entity)

        Dim f As New EntityFilter(t, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Assert.AreEqual(f.ToString, f.MakeHash)

        Dim o As New Entity(1)

        Assert.AreEqual(f.MakeHash, f.Template.MakeHash(mpe, mpe.GetEntitySchema(t), o))
    End Sub

    <TestMethod()> _
    Public Sub TestMakeHash2()
        Dim mpe As New Worm.ObjectMappingEngine("1")
        Dim t As Type = GetType(Entity2)

        Dim f As New EntityFilter(t, "ID", New ScalarValue(1), Worm.Criteria.FilterOperation.Equal)
        Dim f2 As New EntityFilter(t, "Str", New ScalarValue("d"), Worm.Criteria.FilterOperation.Like)
        Dim cAnd As New Condition.ConditionConstructor
        cAnd.AddFilter(f).AddFilter(f2)

        Dim cOr As New Condition.ConditionConstructor
        cOr.AddFilter(f).AddFilter(f2, ConditionOperator.Or)

        Assert.AreEqual("1TestProject1.Entity2", f.ToString)
        Assert.AreEqual(EntityFilter.EmptyHash, CType(cOr.Condition, IEntityFilter).MakeHash)

        Dim o As New Entity2(1)
        Dim oschema As IEntitySchema = mpe.GetEntitySchema(t)

        Assert.AreEqual(f.MakeHash, f.GetFilterTemplate.MakeHash(mpe, oschema, o))
        'Assert.AreEqual(EntityFilter.EmptyHash, CType(cOr.Condition, IEntityFilter).GetFilterTemplate.MakeHash(mpe, oschema, o))
    End Sub
End Class
