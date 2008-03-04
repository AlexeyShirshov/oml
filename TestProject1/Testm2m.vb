Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Orm.Meta

<TestClass()> Public Class Testm2m

    <TestMethod()> _
    Public Sub TestSelectm2m()

        Dim schema As New SQLGenerator("1")

        Dim t As Type = GetType(Entity)
        Dim t2 As Type = GetType(Entity4)

        Dim pmgr As New ParamMgr(schema, "p")
        Dim almgr As AliasMgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent2_id Entity4ID,t1.ent1_id EntityID from dbo.[1to2] t1", schema.SelectM2M(t, t2, False, True, Nothing, pmgr, almgr, False, True))

        Dim e As New Entity(10, Nothing, schema)

        Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 where t1.ent1_id = @p1", schema.SelectM2M(almgr, e, t2, Nothing, Nothing, True, False, False, params, True))

        Assert.AreEqual(1, params.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestSelectm2m2()

        Dim schema As New SQLGenerator("2")

        Dim t As Type = GetType(Entity)
        Dim t2 As Type = GetType(Entity4)

        Dim pmgr As New ParamMgr(schema, "p")
        Dim almgr As AliasMgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent2_id Entity4ID,t1.ent1_id EntityID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent1_id = t2.i", schema.SelectM2M(t, t2, False, True, Nothing, pmgr, almgr, False, True))

        Dim e As New Entity(10, Nothing, schema)

        Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent2_id = t2.i where t1.ent1_id = @p1", schema.SelectM2M(almgr, e, t2, Nothing, Nothing, True, False, False, params, True))

        Assert.AreEqual(1, params.Count)
        Assert.AreEqual(10, params(0).Value)
    End Sub

    <TestMethod()> _
    Public Sub TestSelectm2m3()

        Dim schema As New SQLGenerator("2")

        Dim t As Type = GetType(Entity4)
        Dim t2 As Type = GetType(Entity)

        Dim pmgr As New ParamMgr(schema, "p")
        Dim almgr As AliasMgr = AliasMgr.Create

        'Try
        '    Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent2_id = t2.i", schema.SelectM2M(t, t2, False, pmgr, almgr))
        '    Assert.Fail()
        'Catch ex As Orm.OrmObjectException
        'Catch
        '    Assert.Fail()
        'End Try

        'Try
        '    almgr = Orm.AliasMgr.Create
        '    Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent2_id = t2.i", schema.SelectM2M(t, t2, True, pmgr, almgr))
        '    Assert.Fail()
        'Catch ex As Orm.OrmObjectException
        'Catch
        '    Assert.Fail()
        'End Try

        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.ent2 t2 on t1.ent2_id = t2.id join dbo.t1 t3 on t3.i = t2.id", schema.SelectM2M(t, t2, True, True, Nothing, pmgr, almgr, False, True))

        Dim e As New Entity4(10, Nothing, schema)

        Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.ent2_id Entity4ID,t1.ent1_id EntityID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent1_id = t2.i where t1.ent2_id = @p1", schema.SelectM2M(almgr, e, t2, Nothing, "a", True, False, False, params, True))

        Assert.AreEqual(1, params.Count)
        Assert.AreEqual(10, params(0).Value)
        'Assert.AreEqual("a", params(0).Value)
    End Sub

    <TestMethod()> _
    Public Sub TestSelectm2m4()
        Dim schema As New SQLGenerator("1")

        Dim t As Type = GetType(Entity)
        Dim t2 As Type = GetType(Entity4)

        Dim pmgr As New ParamMgr(schema, "p")

        Dim e As New Entity(10, Nothing, schema)

        Dim el As New EditableList(10, New Integer() {9, 10, 20}, Nothing, Nothing, Nothing)
        el.Delete(10)
        el.Add(234)

        Dim m As M2MRelation = schema.GetM2MRelation(t, t2, True)

        Assert.AreEqual("delete from dbo.[1to2] where ent1_id = @p1 and ent2_id in(10)" & vbCrLf & _
        "if @@error = 0 begin" & vbCrLf & _
            vbTab & "if @@error = 0 insert into dbo.[1to2](ent1_id,ent2_id) values(@p1,234)" & vbCrLf & _
        "end" & vbCrLf, schema.SaveM2M(e, m, el, pmgr))
    End Sub

    <TestMethod()> _
    Public Sub TestSelectm2m5()
        Dim schema As New SQLGenerator("1")

        Dim t As Type = GetType(Entity5)
        Dim t2 As Type = GetType(Entity5)

        Dim pmgr As New ParamMgr(schema, "p")

        Dim e As New Entity5(10, Nothing, schema)

        Dim el As New EditableList(10, New Integer() {9, 10, 20}, Nothing, Nothing, True, Nothing)
        el.Delete(10)
        el.Add(234)

        Dim m As M2MRelation = schema.GetM2MRelation(t, t2, True)

        Debug.WriteLine(schema.SaveM2M(e, m, el, pmgr))
    End Sub
End Class
