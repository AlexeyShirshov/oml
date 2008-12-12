Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Entities

<TestClass()> Public Class OrmSchemaTest

    <TestMethod()> _
    Public Sub TestSelect()
        Dim schemaV1 As New SQLGenerator

        Assert.AreEqual("SQL Server 2000", schemaV1.Name)

        Assert.IsTrue(schemaV1.SupportFullTextSearch)

        Dim s As New Worm.ObjectMappingEngine("1")
        Dim o As New Entity(10, Nothing, s)
        Dim t As Type = GetType(Entity)

        Assert.AreEqual(1, s.GetTables(t).Length)

        Dim almgr As AliasMgr = AliasMgr.Create
        Dim params As New ParamMgr(schemaV1, "p")
        Assert.AreEqual("select t1.id from dbo.ent1 t1", schemaV1.Select(s, t, almgr, params, Nothing, Nothing, Nothing))

        Dim schemaV2 As New SQLGenerator
        Dim s2 As New Worm.ObjectMappingEngine("2")
        almgr = AliasMgr.Create
        Dim params2 As New ParamMgr(schemaV2, "p")
        Assert.AreEqual("select t1.id from dbo.ent1 t1 join dbo.t1 t2 on t2.i = t1.id", schemaV2.Select(s2, t, almgr, params2, Nothing, Nothing, Nothing))
        Assert.AreEqual(2, s2.GetTables(t).Length)

        t = GetType(Entity2)

        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.id, t2.s from dbo.ent1 t1 join dbo.t1 t2 on t2.i = t1.id", schemaV1.Select(s, t, almgr, params, Nothing, Nothing, Nothing))
        Assert.AreEqual(2, s.GetTables(t).Length)

        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.id, t2.s from dbo.ent1 t1 join dbo.t2 t2 on t2.i = t1.id", schemaV2.Select(s2, t, almgr, params2, Nothing, Nothing, Nothing))
        Assert.AreEqual(2, s2.GetTables(t).Length)

        Dim schemaV3 As New SQLGenerator
        Dim s3 As New Worm.ObjectMappingEngine("3")
        Dim params3 As New ParamMgr(schemaV3, "p")
        almgr = AliasMgr.Create
        t = GetType(Entity)
        Assert.AreEqual("select t1.id from dbo.ent1 t1 join dbo.t1 t2 on (t2.i = t1.id and t2.s = @p1)", schemaV3.Select(s3, t, almgr, params3, Nothing, Nothing, Nothing))
        Assert.AreEqual(2, s3.GetTables(t).Length)
    End Sub

    <TestMethod()> _
    Public Sub TestSelectID()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Dim o As New Entity(10, Nothing, schemaV1)
        Dim t As Type = GetType(Entity)

        Assert.AreEqual(1, schemaV1.GetTables(t).Length)

        Dim almgr As AliasMgr = AliasMgr.Create
        Dim params As New ParamMgr(gen, "p")
        Assert.AreEqual("select t1.id from dbo.ent1 t1", gen.SelectID(schemaV1, t, almgr, params, Nothing))


        Dim schemaV2 As New Worm.ObjectMappingEngine("2")
        almgr = AliasMgr.Create
        Dim params2 As New ParamMgr(gen, "p")

        Assert.AreEqual("select t1.id from dbo.ent1 t1 join dbo.t1 t2 on t2.i = t1.id", gen.SelectID(schemaV2, t, almgr, params2, Nothing))

        t = GetType(Entity2)

        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.id from dbo.ent1 t1 join dbo.t1 t2 on t2.i = t1.id", gen.SelectID(schemaV1, t, almgr, params, Nothing))

        almgr = AliasMgr.Create
        Assert.AreEqual("select t1.id from dbo.ent1 t1 join dbo.t2 t2 on t2.i = t1.id", gen.SelectID(schemaV2, t, almgr, params2, Nothing))

    End Sub

    <TestMethod()> _
    Public Sub TestInsert()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Dim o As New Entity(10, Nothing, schemaV1)
        Dim t As Type = GetType(Entity)

        Dim params As ICollection(Of Data.Common.DbParameter) = Nothing
        Dim sel As List(Of ColumnAttribute) = Nothing

        Dim expected As String = "declare @pk_id int" & vbCrLf & _
            "declare @rcount int" & vbCrLf & _
            "insert into dbo.ent1 default values" & vbCrLf & _
            "select @rcount = @@rowcount, @pk_id = scope_identity()" & vbCrLf & _
            "if @rcount > 0 select t1.id from dbo.ent1 t1 where t1.id = @pk_id"

        Assert.AreEqual(expected, gen.Insert(schemaV1, o, Nothing, params, sel))

        Assert.IsNotNull(params)
        Assert.IsNotNull(sel)

        Assert.AreEqual(0, params.Count)
        Assert.AreEqual(1, sel.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestInsert2()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Dim o As New Entity2(10, Nothing, schemaV1)
        Dim t As Type = GetType(Entity)

        Dim params As ICollection(Of Data.Common.DbParameter) = Nothing
        Dim sel As List(Of ColumnAttribute) = Nothing

        Dim expected As String = "declare @pk_id int" & vbCrLf & _
            "declare @rcount int" & vbCrLf & _
            "declare @err int" & vbCrLf & _
            "insert into dbo.ent1 default values" & vbCrLf & _
            "select @rcount = @@rowcount, @pk_id = scope_identity(), @err = @@error" & vbCrLf & _
            "if @err = 0 insert into dbo.t1 (s,i)  values(@p1,@pk_id)" & vbCrLf & _
            "if @rcount > 0 select t1.id from dbo.ent1 t1 where t1.id = @pk_id"

        Assert.AreEqual(expected, gen.Insert(schemaV1, o, Nothing, params, sel))

        Assert.IsNotNull(params)
        Assert.IsNotNull(sel)

        Assert.AreEqual(1, params.Count)
        Assert.AreEqual(1, sel.Count)
    End Sub

    '<TestMethod()> _
    'Public Sub TestInsertSQL2005()
    '    Dim schemaV1 As New DbSchema("1")
    '    Dim o As New Entity(10, Nothing, schemaV1)
    '    Dim t As Type = GetType(Entity)

    '    Dim params As ICollection(Of Data.Common.DbParameter) = Nothing
    '    Dim sel As IList(Of Orm.ColumnAttribute) = Nothing

    '    Dim expected As String = "declare @id int" & vbCrLf & _
    '        "insert into dbo.ent1 output @id = inserted.id default values" & vbCrLf & _
    '        "select @rcount = @@rowcount, @id = scope_identity()" & vbCrLf & _
    '        "if @rcount > 0 select dbo.ent1.id from dbo.ent1 where dbo.ent1.id = @id"

    '    Assert.AreEqual(expected, schemaV1.Insert(o, params, sel))

    '    Assert.IsNotNull(params)
    '    Assert.IsNotNull(sel)

    '    Assert.AreEqual(0, params.Count)
    '    Assert.AreEqual(1, sel.Count)
    'End Sub

    '<TestMethod()> _
    'Public Sub TestInsert2SQL2005()
    '    Dim schemaV1 As New DbSchema("1")
    '    Dim o As New Entity2(10, Nothing, schemaV1)
    '    Dim t As Type = GetType(Entity)

    '    Dim params As ICollection(Of Data.Common.DbParameter) = Nothing
    '    Dim sel As IList(Of Orm.ColumnAttribute) = Nothing

    '    Dim expected As String = "declare @id int" & vbCrLf & _
    '        "declare @rcount int" & vbCrLf & _
    '        "declare @err int" & vbCrLf & _
    '        "insert into dbo.ent1 default values" & vbCrLf & _
    '        "select @rcount = @@rowcount, @id = scope_identity(), @err = @@error" & vbCrLf & _
    '        "if @err = 0 insert into dbo.t1 (s,i) values(@p1,@id)" & vbCrLf & _
    '        "if @rcount > 0 select dbo.ent1.id from dbo.ent1 where dbo.ent1.id = @id"

    '    Assert.AreEqual(expected, schemaV1.Insert(o, params, sel))

    '    Assert.IsNotNull(params)
    '    Assert.IsNotNull(sel)

    '    Assert.AreEqual(1, params.Count)
    '    Assert.AreEqual(1, sel.Count)
    'End Sub

    <TestMethod()> _
    Public Sub TestDelete()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity(1, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity)

            o.Load()

            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing

            o.Delete()

            Dim expected As String = "declare @id_ID int" & vbCrLf & _
                "set @id_ID = @p1" & vbCrLf & _
                "delete from dbo.ent1 where id = @id_ID"
            Dim gen As New SQLGenerator
            Assert.AreEqual(expected, gen.Delete(schemaV1, o, params, Nothing))

            Assert.IsNotNull(params)

            For Each p As Data.Common.DbParameter In params
                Assert.AreEqual(1, p.Value)
            Next
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestDelete2()
        Dim schemaV1 As New Worm.ObjectMappingEngine("2")
        Dim gen As New SQLGenerator
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity(1, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity)

            o.Load()

            Assert.AreEqual(ObjectState.NotFoundInSource, o.InternalProperties.ObjectState)
            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing

            o.Delete()

            Dim expected As String = "declare @id_ID int" & vbCrLf & _
                "set @id_ID = @p1" & vbCrLf & _
                "delete from dbo.ent1 where id = @id_ID" & vbCrLf & _
                "delete from dbo.t1 where i = @id_ID"

            Assert.AreEqual(expected, gen.Delete(schemaV1, o, params, Nothing))

            Assert.IsNotNull(params)

            For Each p As Data.Common.DbParameter In params
                Assert.AreEqual(1, p.Value)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete3()
        Dim schemaV1 As New Worm.ObjectMappingEngine("2")
        Dim gen As New SQLGenerator
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity(2, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity)

            o.Load()

            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing

            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)

            o.Delete()

            Assert.AreEqual(ObjectState.Deleted, o.InternalProperties.ObjectState)

            Dim expected As String = "declare @id_ID int" & vbCrLf & _
                "set @id_ID = @p1" & vbCrLf & _
                "delete from dbo.t1 where i = @id_ID" & vbCrLf & _
                "delete from dbo.ent1 where id = @id_ID"

            Assert.AreEqual(expected, gen.Delete(schemaV1, o, params, Nothing))

            Assert.IsNotNull(params)

            For Each p As Data.Common.DbParameter In params
                Assert.AreEqual(2, p.Value)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete4()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity5(1, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity5)

            o.Load()

            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing

            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)

            o.Delete()

            Assert.AreEqual(ObjectState.Deleted, o.InternalProperties.ObjectState)

            Dim expected As String = "declare @id_ID int" & vbCrLf & _
                "set @id_ID = @p1" & vbCrLf & _
                "delete from dbo.ent3 where (id = @id_ID and version = @p2)"

            Assert.AreEqual(expected, gen.Delete(schemaV1, o, params, Nothing))

            Assert.IsNotNull(params)

            Dim i As Integer = 0
            For Each p As Data.Common.DbParameter In params
                If i = 0 Then
                    Assert.AreEqual(1, p.Value)
                ElseIf i = 1 Then
                End If
                i += 1
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestUpdate()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity(2, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity)

            o.Load()

            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing
            Dim sel As List(Of ColumnAttribute) = Nothing
            Dim upd As IList(Of Worm.Criteria.Core.EntityFilter) = Nothing
            Assert.AreEqual(String.Empty, gen.Update(schemaV1, o, Nothing, params, sel, upd))

            Dim o2 As New Entity2(2, mgr.Cache, schemaV1)

            o2.Load()

            Assert.IsTrue(o2.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, o2.InternalProperties.ObjectState)

            o2.Str = "oadfn"

            Assert.AreEqual(ObjectState.Modified, o2.InternalProperties.ObjectState)

            Dim expected As String = "update t1 set t1.s = @p1 from dbo.t1 t1 where t1.i = @p2" & vbCrLf & _
                "if @@rowcount = 0 insert into dbo.t1 (s,i)  values(@p1,@p2)"

            Assert.AreEqual(expected, gen.Update(schemaV1, o2, Nothing, params, sel, upd))

            Dim i As Integer = 0
            For Each p As Data.Common.DbParameter In params
                If i = 0 Then
                    Assert.AreEqual("oadfn", p.Value)
                ElseIf i = 1 Then
                    Assert.AreEqual(2, p.Value)
                    'ElseIf i = 2 Then
                    '    Assert.AreEqual(2, p.Value)
                End If
                i += 1
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestUpdate2()
        Dim schemaV1 As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(schemaV1)
            Dim o As New Entity5(1, mgr.Cache, schemaV1)
            Dim t As Type = GetType(Entity5)

            o.Load()

            Assert.IsTrue(o.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)

            o.Title = "oadfn"

            Assert.AreEqual(ObjectState.Modified, o.InternalProperties.ObjectState)

            Dim expected As String = "update t1 set t1.name = @p1 from dbo.ent3 t1 where (t1.id = @p2 and t1.version = @p3)" & vbCrLf & _
                "if @@rowcount > 0 select t1.version from dbo.ent3 t1 where t1.id = @p4"

            Dim params As IEnumerable(Of Data.Common.DbParameter) = Nothing
            Dim sel As List(Of ColumnAttribute) = Nothing
            Dim upd As IList(Of Worm.Criteria.Core.EntityFilter) = Nothing
            Assert.AreEqual(expected, gen.Update(schemaV1, o, Nothing, params, sel, upd))

            Dim i As Integer = 0
            For Each p As Data.Common.DbParameter In params
                If i = 0 Then
                    Assert.AreEqual("oadfn", p.Value)
                ElseIf i = 1 Then
                    Assert.AreEqual(1, p.Value)
                    'ElseIf i = 2 Then
                    '    Assert.AreEqual(2, p.Value)
                End If
                i += 1
            Next
        End Using
    End Sub
End Class

<TestClass()> Public Class OrmSchemaUnitTest

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestGetTables()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Assert.AreEqual("1", schema.Version)

        schema.GetTables(CType(Nothing, Type))
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestGetObjectSchema()
        Dim schema As New Worm.ObjectMappingEngine("1")
        schema.GetObjectSchema(Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentException))> _
    Public Sub TestGetObjectSchema2()
        Dim schema As New Worm.ObjectMappingEngine("1")
        schema.GetObjectSchema(GetType(SByte))
    End Sub

    <TestMethod()> _
    Public Sub TestGetObjectSchema3()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Assert.IsNotNull(schema.GetObjectSchema(GetType(Entity3)))
    End Sub

    ', ExpectedException(GetType(ArgumentException))
    <TestMethod()> _
    Public Sub TestGetObjectSchema4()
        'Dim schema As New DbSchema("3")
        'Assert.IsNotNull(schema.GetObjectSchema(GetType(Entity2)))
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestGetUnions()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Worm.ObjectMappingEngine.GetUnions(Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestGetColumnValue()
        Dim schema As New Worm.ObjectMappingEngine("1")
        schema.GetFieldValue(Nothing, "ID")
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestGetColumnValue2()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Dim o As New Entity(10, Nothing, schema)

        schema.GetFieldValue(o, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestGetColumnValue3()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Dim o As New Entity(10, Nothing, schema)

        Assert.AreEqual(10, schema.GetFieldValue(o, "ID"))
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentException))> _
    Public Sub TestGetColumnValue4()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Dim o As New Entity(10, Nothing, schema)

        Assert.AreEqual(10, schema.GetFieldValue(o, "FASDCSDC"))
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestSelect()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        gen.Select(schema, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestSelect2()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        gen.Select(schema, GetType(Entity), Nothing, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestSelect3()
        Dim schema As New Worm.ObjectMappingEngine("1")
        Dim gen As New SQLGenerator
        gen.Select(schema, GetType(Entity), AliasMgr.Create, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentNullException))> _
    Public Sub TestSelect4()
        Dim schema As New Worm.ObjectMappingEngine("3")
        Dim gen As New SQLGenerator
        gen.Select(schema, GetType(Entity), AliasMgr.Create, Nothing, Nothing, Nothing, Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestAlter()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            'Dim c As IList(Of Entity4) = CType(mgr.ConvertIds2Objects(Of Entity4)(New Object() {2}, False), Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Dim e As Entity4 = mgr.GetOrmBaseFromCacheOrCreate(Of Entity4)(2)
            Assert.AreEqual(ObjectState.NotLoaded, e.InternalProperties.ObjectState)
            Dim expected As String = "wrtbg"
            e.Title = "345"
            Assert.AreEqual(ObjectState.Modified, e.InternalProperties.ObjectState)
            e.RejectChanges()

            Assert.AreEqual(expected, e.Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEquality()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            Dim e As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)
            Dim e3 As Entity4 = mgr.Find(Of Entity4)(1)

            Assert.IsTrue(e2 > e)
            Assert.IsFalse(e2 = e)
            Assert.IsTrue(e2 <> e)
            Assert.IsFalse(e3 = e)
            Assert.IsFalse(e2 < e)

            Assert.AreNotEqual(Nothing, e)
            Assert.IsFalse(Nothing > e)
            Assert.IsTrue(e > Nothing)

            Assert.IsTrue(e2.Equals(2))

            e3.GetHashCode()

            'e2.CreateObject(Nothing, 1)

            Assert.IsTrue(CType(e3, IComparable).CompareTo(1) > 0)
        End Using
    End Sub
End Class