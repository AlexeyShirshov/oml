Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Query

<TestClass()> Public Class Testm2m

    <TestMethod()> _
    Public Sub TestM2MOuterExists()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schema)
            Dim o As New Entity(1)

            'Dim rel As M2MRelationDesc = CType(schema.GetEntitySchema(GetType(Entity)), ISchemaWithM2M).GetM2MRelations()(0)

            Dim outerAlias As New QueryAlias(GetType(Entity4))

            Dim innerCmd As QueryCmd = o.GetCmd(GetType(Entity4)).
                Where(Ctor.prop(GetType(Entity4), "ID").eq(outerAlias, "ID"))

            Dim outerCmd As New QueryCmd()
            outerCmd.SelectEntity(outerAlias).
                Where(Ctor.exists(innerCmd))

            Assert.AreEqual(4, outerCmd.Count(mgr))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MSelect()

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schema)
            Dim o As New Entity(1)

            Dim cmd As QueryCmd = o.GetCmd(GetType(Entity4)).
                Select(FCtor.prop(GetType(Entity4), "Title"))

            Assert.AreEqual(4, cmd.Count(mgr))
            For Each e As Entity4 In cmd.ToList(Of Entity4)(mgr)
                Assert.IsTrue(e.InternalProperties.IsPropertyLoaded("Title"))
            Next
        End Using
    End Sub

    '<TestMethod()> _
    'Public Sub TestSelectm2m3()

    '    Dim schema As New Worm.ObjectMappingEngine("2")

    '    Dim t As Type = GetType(Entity4)
    '    Dim t2 As Type = GetType(Entity)
    '    Dim gen As New SQLGenerator
    '    Dim pmgr As New ParamMgr(gen, "p")
    '    Dim almgr As AliasMgr = AliasMgr.Create

    '    'Try
    '    '    Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent2_id = t2.i", schema.SelectM2M(t, t2, False, pmgr, almgr))
    '    '    Assert.Fail()
    '    'Catch ex As Orm.OrmObjectException
    '    'Catch
    '    '    Assert.Fail()
    '    'End Try

    '    'Try
    '    '    almgr = Orm.AliasMgr.Create
    '    '    Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent2_id = t2.i", schema.SelectM2M(t, t2, True, pmgr, almgr))
    '    '    Assert.Fail()
    '    'Catch ex As Orm.OrmObjectException
    '    'Catch
    '    '    Assert.Fail()
    '    'End Try

    '    almgr = AliasMgr.Create
    '    Assert.AreEqual("select t1.ent1_id EntityID,t1.ent2_id Entity4ID from dbo.[1to2] t1 join dbo.ent2 t2 on t1.ent2_id = t2.id join dbo.t1 t3 on t3.i = t2.id", gen.SelectM2M(schema, t, t2, New Worm.Entities.Query.QueryAspect() {}, True, True, Nothing, pmgr, almgr, False, M2MRelationDesc.DirKey))

    '    Dim e As New Entity4(10, Nothing, schema)

    '    Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
    '    almgr = AliasMgr.Create
    '    Assert.AreEqual("select t1.ent2_id Entity4ID,t1.ent1_id EntityID from dbo.[1to2] t1 join dbo.t1 t2 on t1.ent1_id = t2.i where t1.ent2_id = @p1", gen.SelectM2M(schema, almgr, e, t2, Nothing, "a", True, False, False, params, M2MRelationDesc.DirKey))

    '    Assert.AreEqual(1, params.Count)
    '    Assert.AreEqual(10, params(0).Value)
    '    'Assert.AreEqual("a", params(0).Value)
    'End Sub

    '<TestMethod()> _
    'Public Sub TestSelectm2m4()
    '    Dim schema As New Worm.ObjectMappingEngine("1")

    '    Dim t As Type = GetType(Entity)
    '    Dim t2 As Type = GetType(Entity4)
    '    Dim gen As New SQLGenerator
    '    Dim pmgr As New ParamMgr(gen, "p")

    '    Dim e As New Entity(10, Nothing, schema)

    '    Dim el As New CachedM2MRelation(10, New Object() {9, 10, 20}, GetType(Entity), GetType(Entity4), Nothing)
    '    el.Delete(New Entity4(10, Nothing, schema))
    '    el.Add(New Entity4(234, Nothing, schema))

    '    Dim m As M2MRelationDesc = schema.GetM2MRelation(t, t2, True)

    '    Assert.AreEqual("delete t1 from dbo.[1to2] t1 where t1.ent1_id = @p1 and t1.ent2_id in(10)" & vbCrLf & _
    '    "if @@error = 0 begin" & vbCrLf & _
    '        vbTab & "if @@error = 0 insert into dbo.[1to2](ent1_id,ent2_id) values(@p1,234)" & vbCrLf & _
    '    "end" & vbCrLf, gen.SaveM2M(schema, e, m, el, pmgr))
    'End Sub

    '<TestMethod()> _
    'Public Sub TestSelectm2m5()
    '    Dim schema As New Worm.ObjectMappingEngine("1")

    '    Dim t As Type = GetType(Entity5)
    '    Dim t2 As Type = GetType(Entity5)
    '    Dim gen As New SQLGenerator
    '    Dim pmgr As New ParamMgr(gen, "p")

    '    Dim e As New Entity5(10, Nothing, schema)

    '    Dim el As New CachedM2MRelation(10, New Object() {9, 10, 20}, GetType(Entity5), GetType(Entity5), True, Nothing)
    '    el.Delete(New Entity5(10, Nothing, schema))
    '    el.Add(New Entity5(234, Nothing, schema))

    '    Dim m As M2MRelationDesc = schema.GetM2MRelation(t, t2, True)

    '    Debug.WriteLine(gen.SaveM2M(schema, e, m, el, pmgr))
    'End Sub
End Class
