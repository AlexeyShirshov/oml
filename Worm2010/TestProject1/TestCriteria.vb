Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Query.Sorting
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Values

<TestClass()> Public Class TestCriteria

    <TestMethod()> _
    Public Sub TestComplexTypes()
        Dim cr As Worm.Criteria.PredicateLink = Ctor.prop(GetType(Entity4), "ID").eq(56). _
            [and](GetType(Entity4), "Title").eq("lsd")

        Dim cr2 As PredicateLink = CType(cr.Clone, PredicateLink)
        cr2.[and](GetType(Entity4), "Title").[in](New String() {"a"})

        Dim f As IEntityFilter = CType(cr.Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim gen As New SQL2000Generator
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and t2.name = @p2)", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))

        Dim f2 As IEntityFilter = CType(cr2.Filter, IEntityFilter)
        Assert.AreEqual("((t2.id = @p1 and t2.name = @p2) and t2.name in (@p3))", f2.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestComplexTypeless()
        Dim f As IEntityFilter = CType(Ctor.prop(GetType(Entity4), "ID").eq(56). _
            [and]("Title").eq("lsd").Filter(), IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and t2.name = @p2)", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub


    <TestMethod()> _
    Public Sub TestComplexTypes2()
        Dim f As IEntityFilter = CType(Ctor.prop(GetType(Entity4), "ID").eq(56). _
            [and](Ctor.prop(GetType(Entity4), "Title").eq(56).[or](GetType(Entity), "ID").eq(483)).Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and (t2.name = @p2 or t1.id = @p3))", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes()
        Dim f As IEntityFilter = CType(New Ctor(GetType(Entity4)).prop("ID").eq(56). _
            [and]("Title").eq("lsd").Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t1.id = @p1 and t1.name = @p2)", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes2()
        Dim f As IEntityFilter = CType(New Ctor(GetType(Entity4)).prop("ID").eq(56). _
            [and](New Ctor(GetType(Entity4)).prop("Title").eq(56).[or]("ID").eq(483)).Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQL2000Generator
        Dim pmgr As New ParamMgr(gen, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t1.id = @p1 and (t1.name = @p2 or t1.id = @p3))", f.MakeQueryStmt(schema, Nothing, gen, Nothing, Nothing, almgr, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Dim t As Type = GetType(Type)
        Dim mpe As New ObjectMappingEngine

        Dim s As SortExpression = SCtor.prop(t, "sdgfn").asc
        Assert.AreEqual("Asc$$System.Type$sdgfn", s.GetStaticString(mpe))
        Assert.AreEqual(SortExpression.SortType.Asc, s.Order)

        Dim s2 As SortExpression = SCtor.prop(t, "sdgfn").desc
        Assert.AreEqual("Desc$$System.Type$sdgfn", s2.GetDynamicString)
        Assert.AreEqual(SortExpression.SortType.Desc, s2.Order)

        Dim s3 As SortExpression = SCtor.prop(t, "sdgfn").Order(False)
        Assert.AreEqual("sdgfn", CType(s3.Operand, EntityExpression).ObjectProperty.PropertyAlias)
        Assert.AreEqual(SortExpression.SortType.Desc, s3.Order)

        Dim s4 As SortExpression = SCtor.prop(t, "sdgfn").Order(True)
        Assert.AreEqual(SortExpression.SortType.Asc, s4.Order)

        Dim s5 As SortExpression = SCtor.prop(t, "sdgfn").Order("desc")
        Assert.AreEqual(t, CType(s3.Operand, EntityExpression).ObjectProperty.Entity.GetRealType(mpe))
        Assert.AreEqual(SortExpression.SortType.Desc, s5.Order)
    End Sub

    <TestMethod()>
    Public Sub TestEval()
        Dim mpe As New ObjectMappingEngine
        Assert.AreEqual(IEvaluableValue.EvalResult.NotFound, CType(Ctor.param(1).eq(2).ConditionCtor.Condition, IEvaluableFilter).Eval(mpe, Nothing, Nothing, Nothing))

        Assert.AreEqual(IEvaluableValue.EvalResult.Found, CType(Ctor.param(2).eq(2).and(1).eq(1).ConditionCtor.Condition, IEvaluableFilter).Eval(mpe, Nothing, Nothing, Nothing))

        Dim t As Type = GetType(Type)
        Assert.AreEqual(IEvaluableValue.EvalResult.Unknown, CType(Ctor.param(1).eq(t, "x").ConditionCtor.Condition, IEvaluableFilter).Eval(mpe, Nothing, Nothing, Nothing))
    End Sub
End Class