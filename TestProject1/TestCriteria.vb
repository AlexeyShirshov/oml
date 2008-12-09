Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Query

<TestClass()> Public Class TestCriteria

    <TestMethod()> _
    Public Sub TestComplexTypes()
        Dim cr As Worm.Criteria.PredicateLink = PCtor.prop(GetType(Entity4), "ID").eq(56). _
            [and](GetType(Entity4), "Title").eq("lsd")

        Dim cr2 As PredicateLink = CType(cr.Clone, PredicateLink)
        cr2.[and](GetType(Entity4), "Title").[in](New String() {"a"})

        Dim f As IEntityFilter = CType(cr.Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and t2.name = @p2)", f.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))

        Dim f2 As IEntityFilter = CType(cr2.Filter, IEntityFilter)
        Assert.AreEqual("((t2.id = @p1 and t2.name = @p2) and t2.name in (@p3))", f2.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestComplexTypeless()
        Dim f As IEntityFilter = CType(PCtor.prop(GetType(Entity4), "ID").eq(56). _
            [and]("Title").eq("lsd").Filter(), IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQLGenerator
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and t2.name = @p2)", f.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub


    <TestMethod()> _
    Public Sub TestComplexTypes2()
        Dim f As IEntityFilter = CType(PCtor.prop(GetType(Entity4), "ID").eq(56). _
            [and](PCtor.prop(GetType(Entity4), "Title").eq(56).[or](GetType(Entity), "ID").eq(483)).Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQLGenerator
        Dim pmgr As New ParamMgr(gen, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0), Nothing)
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t2.id = @p1 and (t2.name = @p2 or t1.id = @p3))", f.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes()
        Dim f As IEntityFilter = CType(New PCtor(GetType(Entity4)).prop("ID").eq(56). _
            [and]("Title").eq("lsd").Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQLGenerator
        Dim pmgr As New ParamMgr(gen, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t1.id = @p1 and t1.name = @p2)", f.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes2()
        Dim f As IEntityFilter = CType(New PCtor(GetType(Entity4)).prop("ID").eq(56). _
            [and](New PCtor(GetType(Entity4)).prop("Title").eq(56).[or]("ID").eq(483)).Filter, IEntityFilter)

        Dim schema As New ObjectMappingEngine("1")
        Dim almgr As AliasMgr = AliasMgr.Create
        Dim gen As New SQLGenerator
        Dim pmgr As New ParamMgr(gen, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0), Nothing)

        Assert.AreEqual("(t1.id = @p1 and (t1.name = @p2 or t1.id = @p3))", f.MakeQueryStmt(schema, gen, Nothing, almgr, pmgr, Nothing))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Dim t As Type = GetType(Type)
        Dim s As Sort = SCtor.prop(t, "sdgfn").asc
        Assert.AreEqual("sdgfn", s.SortBy)
        Assert.AreEqual(SortType.Asc, s.Order)

        Dim s2 As Sort = SCtor.prop(t, "sdgfn").desc
        Assert.AreEqual("sdgfn", s2.SortBy)
        Assert.AreEqual(SortType.Desc, s2.Order)

        Dim s3 As Sort = SCtor.prop(t, "sdgfn").Order(False)
        Assert.AreEqual("sdgfn", s3.SortBy)
        Assert.AreEqual(SortType.Desc, s3.Order)

        Dim s4 As Sort = SCtor.prop(t, "sdgfn").Order(True)
        Assert.AreEqual("sdgfn", s4.SortBy)
        Assert.AreEqual(SortType.Asc, s4.Order)

        Dim s5 As Sort = SCtor.prop(t, "sdgfn").Order("desc")
        Assert.AreEqual("sdgfn", s5.SortBy)
        Assert.AreEqual(SortType.Desc, s5.Order)
    End Sub
End Class