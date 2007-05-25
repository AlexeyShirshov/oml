Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Orm
Imports System.Diagnostics
Imports CoreFramework.Structures

<TestClass()> Public Class TestCriteria

    <TestMethod()> _
    Public Sub TestComplexTypes()
        Dim f As IOrmFilter = Criteria.Field(GetType(Entity4), "ID").Eq(56). _
            [And](GetType(Entity4), "Title").Eq("lsd").Filter

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0))

        Assert.AreEqual("(t2.id = @p1 and t2.name = @p2)", f.MakeSQLStmt(schema, almgr.Aliases, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestComplexTypes2()
        Dim f As IOrmFilter = Criteria.Field(GetType(Entity4), "ID").Eq(56). _
            [And](Criteria.Field(GetType(Entity4), "Title").Eq(56).[Or](GetType(Entity), "ID").Eq(483)).Filter

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0))

        Assert.AreEqual("(t2.id = @p1 and (t2.name = @p2 or t1.id = @p3))", f.MakeSQLStmt(schema, almgr.Aliases, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes()
        Dim f As IOrmFilter = New Criteria(GetType(Entity4)).Field("ID").Eq(56). _
            [And]("Title").Eq("lsd").Filter

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0))

        Assert.AreEqual("(t1.id = @p1 and t1.name = @p2)", f.MakeSQLStmt(schema, almgr.Aliases, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleTypes2()
        Dim f As IOrmFilter = New Criteria(GetType(Entity4)).Field("ID").Eq(56). _
            [And](New Criteria(GetType(Entity4)).Field("Title").Eq(56).[Or]("ID").Eq(483)).Filter

        Dim schema As New Orm.DbSchema("1")
        Dim almgr As Orm.AliasMgr = Orm.AliasMgr.Create
        Dim pmgr As New Orm.ParamMgr(schema, "p")

        'almgr.AddTable(schema.GetTables(GetType(Entity))(0))
        almgr.AddTable(schema.GetTables(GetType(Entity4))(0))

        Assert.AreEqual("(t1.id = @p1 and (t1.name = @p2 or t1.id = @p3))", f.MakeSQLStmt(schema, almgr.Aliases, pmgr))
        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And]("sdfln").Eq("lsd")

        'new Criteria(GetType(Entity)).Field("sdf").Eq(56). _
        '    [And](new Criteria(GetType(Entity)).Field("sdf").Eq(56).[Or]("sdfln").Eq("lsd"))
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Dim s As Sort = Sorting.Field("sdgfn").Asc
        Assert.AreEqual("sdgfn", s.FieldName)
        Assert.AreEqual(SortType.Asc, s.Order)

        Dim s2 As Sort = Sorting.Field("sdgfn").Desc
        Assert.AreEqual("sdgfn", s2.FieldName)
        Assert.AreEqual(SortType.Desc, s2.Order)

        Dim s3 As Sort = Sorting.Field("sdgfn").Order(False)
        Assert.AreEqual("sdgfn", s3.FieldName)
        Assert.AreEqual(SortType.Desc, s3.Order)

        Dim s4 As Sort = Sorting.Field("sdgfn").Order(True)
        Assert.AreEqual("sdgfn", s4.FieldName)
        Assert.AreEqual(SortType.Asc, s4.Order)

        Dim s5 As Sort = Sorting.Field("sdgfn").Order("desc")
        Assert.AreEqual("sdgfn", s5.FieldName)
        Assert.AreEqual(SortType.Desc, s5.Order)
    End Sub
End Class