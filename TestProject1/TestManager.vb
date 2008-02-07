Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Orm

<TestClass()> Public Class TestManager
    Implements Worm.OrmManagerBase.INewObjects

    Private _schemas As New System.Collections.Hashtable

    Protected Function GetSchema(ByVal v As String) As DbSchema
        Dim s As DbSchema = CType(_schemas(v), DbSchema)
        If s Is Nothing Then
            s = New DbSchema(v)
            _schemas.Add(v, s)
        End If
        Return s
    End Function

    Public Shared Function CreateManager(ByVal schema As DbSchema) As OrmReadOnlyDBManager
        Return New OrmReadOnlyDBManager(New OrmCache, schema, "Server=.\sqlexpress;AttachDBFileName='" & My.Settings.WormRoot & "\TestProject1\Databases\test.mdf';User Instance=true;Integrated security=true;")
    End Function

    Public Shared Function CreateWriteManager(ByVal schema As DbSchema) As OrmDBManager
        Return New OrmDBManager(New OrmCache, schema, "Server=.\sqlexpress;AttachDBFileName='" & My.Settings.WormRoot & "\TestProject1\Databases\test.mdf';User Instance=true;Integrated security=true;")
    End Function

    <TestMethod()> _
    Public Sub TestLoad()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim o As New Entity(1, mgr.Cache, mgr.ObjectSchema)
            Assert.IsFalse(o.IsLoaded)

            o.Load()

            Assert.IsTrue(o.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoad2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim o As New Entity5(1, mgr.Cache, mgr.ObjectSchema)
            Assert.IsFalse(o.IsLoaded)

            o.Load()

            Assert.IsTrue(o.IsLoaded)

            Assert.AreEqual("n", o.Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(1)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
            Assert.AreEqual(4, mgr.GetLastExecitionResult.Count)

            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.IsLoaded)
            Next

            c = e.Find(Of Entity4)(Nothing, Nothing, True)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next

            c = e.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next
            Dim l As IList(Of Entity4) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(1, l(3).Identifier)
            Assert.AreEqual("first", l(3).Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M3()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(1)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.IsLoaded)
            Next
            Dim l As IList(Of Entity4) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(1, l(3).Identifier)
            Assert.AreEqual("first", l(3).Title)

            c = mgr.Find(Of Entity)(2).Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next
            l = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(2, l(10).Identifier)
            Assert.AreEqual("wrtbg", l(10).Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M4()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity4 = mgr.Find(Of Entity4)(1)

            Dim c As ICollection(Of Entity) = e.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity In c
                Assert.IsFalse(e4.IsLoaded)
            Next

            c = e.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M5()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").Eq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(1, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next

            c = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Worm.Orm.Sorting.Field("Title").Asc, False)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M6()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            mgr.Find(Of Entity4)(12).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, False)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                If e4.Identifier = 12 Then
                    Assert.IsTrue(e4.IsLoaded)
                Else
                    Assert.IsFalse(e4.IsLoaded)
                End If

            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M7()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            mgr.Find(Of Entity4)(12).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Nothing, True)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M8()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            'mgr.Find(Of Entity4)(12).Reload()

            mgr.ConvertIds2Objects(Of Entity4)(New Integer() {1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12}, False)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)
            Dim e2 As Entity4 = mgr.Find(Of Entity4)(12)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(1, c3.Count)

            e.Delete(e2)

            c = e.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c.Count)

            c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            c3 = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(0, c3.Count)

            e.RejectChanges()

            c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            c3 = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(1, c3.Count)

            mgr.BeginTransaction()
            Try
                e.Delete(e2)
                e.Save(True)

                c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
                Assert.AreEqual(10, c.Count)

                c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
                Assert.AreEqual(9, c2.Count)

                c3 = e2.Find(Of Entity)(Nothing, Nothing, False)
                Assert.AreEqual(0, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            'mgr.Obj2ObjRelationDelete(e, GetType(Entity4))

            'c = mgr.Find(Of Entity, Entity4)(e, Nothing, Entity4.Entity4Sort.Name.ToString,  True)
            'Assert.AreEqual(0, c.Count)
        End Using
    End Sub

    '<TestMethod()> _
    'Public Sub TestM2MReset()
    '    Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
    '        Dim e As Entity = mgr.Find(Of Entity)(2)

    '        Dim c As ICollection(Of Entity4) = mgr.Find(Of Entity, Entity4)(e, Nothing, Entity4.Entity4Sort.Name.ToString,  True)
    '        Assert.AreEqual(11, c.Count)

    '        Dim e4 As Entity4 = mgr.Find(Of Entity4)(2)

    '        Dim c2 As ICollection(Of Entity) = mgr.Find(Of Entity4, Entity)(e4, Nothing, Nothing,  True)
    '        Assert.AreEqual(3, c2.Count)

    '        mgr.Obj2ObjRelationReset(e, GetType(Entity4))
    '    End Using
    'End Sub

    '<TestMethod()> _
    'Public Sub TestM2MUpdate()
    '    Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
    '        Dim e As Entity = mgr.Find(Of Entity)(2)
    '        Dim e2 As Entity = mgr.Find(Of Entity)(1)

    '        Dim c As ICollection(Of Entity4) = mgr.Find(Of Entity, Entity4)(e, Nothing, Entity4.Entity4Sort.Name.ToString,  True)
    '        Assert.AreEqual(11, c.Count)

    '        Dim e4 As Entity4 = mgr.Find(Of Entity4)(2)

    '        Dim c2 As ICollection(Of Entity) = mgr.Find(Of Entity4, Entity)(e4, Nothing, Nothing,  True)
    '        Assert.AreEqual(3, c2.Count)

    '        mgr.ObjRelationUpdate(e2, 2, GetType(Entity4))
    '    End Using
    'End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.Add(mgr.Find(Of Entity4)(5))

            c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(12, c.Count)
        End Using
    End Sub

    Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim c As ICollection(Of Entity4) = mgr.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").Eq("245g0nj"), Nothing, False)
            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)
            Dim e2 As Entity4 = mgr.Find(Of Entity4)(5)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c3.Count)

            e.Add(e2)

            c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(12, c.Count)

            c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.Save(True)

                c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                    Sorting.Field("Title").Asc, True)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(4, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd3()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.Add(mgr.Find(Of Entity4)(5))

            c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(12, c.Count)

            c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.Cancel(GetType(Entity4))

            c = e.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)
            Dim e2 As Entity4 = mgr.Find(Of Entity4)(5)

            Dim c2 As ICollection(Of Entity4) = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c3.Count)

            e.Add(e2)

            c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.Save(True)

                c2 = e.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                    Sorting.Field("Title").Asc, True)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(4, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                e.Save(True)

                Assert.IsTrue(e.Identifier <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                e.Save(False)

                Assert.IsTrue(e.Identifier <> -100)
            Finally
                e.RejectChanges()

                Assert.AreEqual(-100, e.Identifier)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.ObjectSchema)
            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.Add(mgr.Find(Of Entity4)(10))

            c = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(1, c.Count)

            mgr.BeginTransaction()
            Try
                e.Save(True)

                Assert.IsTrue(e.Identifier <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(Me.GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.Add(e2)

            c = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(1, c.Count)

            Dim c2 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(6, c2.Count)

            'mgr.Obj2ObjRelationAdd(e2, e)

            'c2 = mgr.FindMany2Many(Of Entity4, Entity)(e2, Nothing, Nothing,  False)
            'Assert.AreEqual(6, c2.Count)

            mgr.BeginTransaction()
            Try
                e.Save(True)

                Assert.IsTrue(e.Identifier <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Private _id As Integer = -100
    Private _l As New Dictionary(Of Integer, OrmBase)

    Private Function GetIdentity() As Integer Implements Worm.OrmManagerBase.INewObjects.GetIdentity
        Dim i As Integer = _id
        _id += -1
        Return i
    End Function

    Private Function GetNew(ByVal t As Type, ByVal id As Integer) As OrmBase Implements Worm.OrmManagerBase.INewObjects.GetNew
        Dim o As OrmBase = Nothing
        _l.TryGetValue(id, o)
        Return o
    End Function

    Private Sub AddNew(ByVal obj As OrmBase) Implements Worm.OrmManagerBase.INewObjects.AddNew
        _l.Add(obj.Identifier, obj)
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestAdd5()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)
            Dim c2 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(5, c2.Count)

            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.Add(e2)

            c = e.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(1, c.Count)

            c2 = e2.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(6, c2.Count)

            'mgr.Obj2ObjRelationAdd(e2, e)

            'c2 = mgr.FindMany2Many(Of Entity4, Entity)(e2, Nothing, Nothing,  False)
            'Assert.AreEqual(6, c2.Count)

            mgr.BeginTransaction()
            Try
                e2.Save(True)

                'Assert.IsTrue(e.Identifier <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                e2.Save(True)

                e2 = mgr.Find(Of Entity4)(10)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete2()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)
            e2.Delete()

            mgr.BeginTransaction()
            Try
                e2.Delete(GetType(Entity))

                e2.Save(True)

                e2 = mgr.Find(Of Entity4)(10)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(11)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCache(e2))

                e2.Save(True)

                Assert.IsFalse(mgr.IsInCache(e2))

                e2 = mgr.Find(Of Entity4)(11)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(11)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCache(e2))

                Dim c2 As ICollection(Of Entity) = e2.Find(Of Entity)(Nothing, Nothing, False)

                Assert.AreEqual(2, c2.Count)

                e2.Save(True)

                Assert.IsFalse(mgr.IsInCache(e2))

                e2 = mgr.Find(Of Entity4)(11)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCompositeDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e As Entity2 = mgr.Find(Of Entity2)(10)
            Assert.AreEqual(10, e.ID)
            Assert.AreEqual("a", e.Str)

            e.Delete()
            mgr.BeginTransaction()
            Try
                e.Save(True)

                Assert.IsFalse(mgr.IsInCache(e))

                e = mgr.Find(Of Entity2)(10)

                Assert.IsNull(e)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = mgr.Find(Of Entity5)(1)

            Dim c As ICollection(Of Entity5) = e.Find(Of Entity5)(Nothing, Nothing, True, False)

            Assert.AreEqual(1, c.Count)

            c = e.Find(Of Entity5)(Nothing, Nothing, False, False)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = mgr.Find(Of Entity5)(2)

            Dim c As ICollection(Of Entity5) = e.Find(Of Entity5)(Nothing, Nothing, True, False)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Entity5) = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(1)))
            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(3)))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = mgr.Find(Of Entity5)(3)

            Dim c As ICollection(Of Entity5) = e.Find(Of Entity5)(Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)

            Dim l As IList(Of Entity5) = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(1)))
            'Dim e2 As TestProject1.Entity5 = mgr.Find(Of Entity5)(2)
            'Assert.IsTrue(l.Contains(e2))

            c = e.Find(Of Entity5)(Nothing, Nothing, False, False)
            Assert.AreEqual(2, c.Count)

            l = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(1)))
            Dim e2 As TestProject1.Entity5 = mgr.Find(Of Entity5)(2)
            Assert.IsTrue(l.Contains(e2))

            c = e2.Find(Of Entity5)(Nothing, Nothing, False, False)
            Assert.AreEqual(0, c.Count)

            c = e2.Find(Of Entity5)(Nothing, Nothing, True, False)
            Assert.AreEqual(2, c.Count)

            mgr.BeginTransaction()
            Try
                e2.Add(e, False)

                c = e2.Find(Of Entity5)(Nothing, Nothing, False, False)
                Assert.AreEqual(1, c.Count)

                c = e2.Find(Of Entity5)(Nothing, Nothing, False)
                Assert.AreEqual(2, c.Count)

                c = e.Find(Of Entity5)(Nothing, Nothing, True, False)
                Assert.AreEqual(2, c.Count)

                e2.Save(True)

                c = e.Find(Of Entity5)(Nothing, Nothing, True, False)
                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MReject()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))

            Dim e As Entity = mgr.Find(Of Entity)(1)
            Dim e4 As Entity4 = mgr.Find(Of Entity4)(2)
            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            Dim c2 As ICollection(Of Entity) = e4.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c2.Count)

            e.Add(e4)

            c = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(5, c.Count)

            c2 = e4.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(4, c2.Count)

            mgr.BeginTransaction()
            Try
                e.Save(False)

                c = e.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(5, c.Count)

                c2 = e4.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(4, c2.Count)

                e.RejectChanges()

                c = e.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(4, c.Count)

                c2 = e4.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(3, c2.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestM2MReject2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = mgr.Find(Of Entity)(1)
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e4)
            Dim id As Integer = e4.Identifier
            e4.Title = "90bu13n4gf0bh185g8b18bg81bg8b5gfvlojkqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            e.Add(e4)

            c = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(5, c.Count)

            mgr.BeginTransaction()
            Try
                Using saver As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using
            Finally
                c = e.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(4, c.Count)

                Assert.AreEqual(id, e4.Identifier)
                Assert.AreEqual(ObjectState.None, e.ObjectState)
                Assert.AreEqual(ObjectState.Created, e4.ObjectState)
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAddNew()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            'mgr.FindNewDelegate = AddressOf GetNew
            mgr.NewObjectManager = Me
            Dim e As Entity = mgr.Find(Of Entity)(1)
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e4)
            Dim id As Integer = e4.Identifier
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            e.Add(e4)

            c = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(5, c.Count)

            mgr.BeginTransaction()
            Try
                Using saver As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(5, c.Count)

                Assert.AreNotEqual(id, e4.Identifier)
                Assert.AreEqual(ObjectState.None, e.ObjectState)
                Assert.AreEqual(ObjectState.None, e4.ObjectState)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAddNewBoth()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            'mgr.FindNewDelegate = AddressOf GetNew
            mgr.NewObjectManager = Me
            Dim e As New Entity(GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e)
            Dim id As Integer = e.Identifier
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.ObjectSchema)
            AddNew(e4)
            Dim id4 As Integer = e4.Identifier
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(0, c.Count)

            e.Add(e4)

            c = e.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(1, c.Count)

            mgr.BeginTransaction()
            Try
                Using saver As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(1, c.Count)

                Assert.AreNotEqual(id, e.Identifier)
                Assert.AreNotEqual(id4, e4.Identifier)

                Assert.AreEqual(ObjectState.None, e.ObjectState)
                Assert.AreEqual(ObjectState.None, e4.ObjectState)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadWithAlter()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As ICollection(Of Entity2) = Nothing
            Using New Worm.OrmManagerBase.CacheListBehavior(mgr, False)
                c = mgr.FindTop(Of Entity2)(100, Nothing, Nothing, True)
            End Using

            Dim l As IList(Of Entity2) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity2))

            Dim e As Entity2 = l(0)

            Assert.AreEqual(ObjectState.None, e.ObjectState)
            Dim oldv As String = e.Str
            e.Str = "ioquv"
            Assert.AreEqual(ObjectState.Modified, e.ObjectState)

            'Using New Orm.OrmManagerBase.CacheListSwitcher(mgr, False)
            c = mgr.FindTop(Of Entity2)(100, Nothing, Nothing, True)
            'End Using

            Assert.AreEqual(ObjectState.Modified, e.ObjectState)
            Assert.AreEqual("ioquv", e.Str)

            e.Load()

            Assert.AreEqual(oldv, e.Str)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestListener()
        Dim l As New Worm.Web.TraceListener("sdfds")
    End Sub

    <TestMethod()> _
    Public Sub TestLoadM2M()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim col As ICollection(Of Entity) = mgr.ConvertIds2Objects(Of Entity)(New Integer() {1, 2}, False)

            Dim rel As Meta.M2MRelation = mgr.ObjectSchema.GetM2MRelation(GetType(Entity), GetType(Entity4), True)

            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)
            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)

            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            e1.Find(Of Entity4)(Nothing, Nothing, True)
            e2.Find(Of Entity4)(Nothing, Nothing, False)

            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim col As ICollection(Of Entity) = mgr.ConvertIds2Objects(Of Entity)(New Integer() {1, 2}, False)
            Dim col4 As New List(Of Entity4)
            mgr.LoadObjects(Of Entity4)(mgr.ObjectSchema.GetM2MRelation(GetType(Entity), GetType(Entity4), True), Nothing, CType(col, System.Collections.ICollection), col4)
            Assert.AreEqual(15, col4.Count)

            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            Dim c1 As ICollection(Of Entity4) = e1.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c1.Count)
            For Each o As Entity4 In c1
                Assert.IsTrue(o.IsLoaded)
            Next
            Dim c2 As ICollection(Of Entity4) = e2.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(11, c2.Count)
            'Dim l As IList(Of Entity4) = CType(c1, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            'For Each o As Entity4 In c2
            '    If l.Contains(o) Then
            '        Assert.IsTrue(o.IsLoaded)
            '    Else
            '        Assert.IsFalse(o.IsLoaded)
            '    End If
            'Next

            'mgr.LoadObjects(Of Entity4)(mgr.ObjectSchema.GetM2MRelation(GetType(Entity), GetType(Entity4), True), Nothing, CType(col, Collections.ICollection), Nothing)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExpireCache()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)

            Dim c1 As ICollection(Of Entity4) = Nothing
            Using New Worm.OrmManagerBase.CacheListBehavior(mgr, TimeSpan.FromMilliseconds(10))
                c1 = e1.Find(Of Entity4)(Nothing, Nothing, True)
            End Using
            System.Threading.Thread.Sleep(100)

            c1 = e1.Find(Of Entity4)(Nothing, Nothing, True)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestTopValidate()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As ICollection(Of Entity) = mgr.FindTop(Of Entity)( _
                100, Nothing, Nothing, False)

            Assert.AreEqual(13, c.Count)

            mgr.BeginTransaction()
            Try
                For Each t As Entity In c
                    Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                        t.Delete()
                        st.Commit()
                    End Using
                    Exit For
                Next
                c = mgr.FindTop(Of Entity)(100, Nothing, Nothing, False)

                Assert.AreEqual(12, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As Integer) Implements Worm.OrmManagerBase.INewObjects.RemoveNew

    End Sub

    Public Sub RemoveNew(ByVal obj As Worm.Orm.OrmBase) Implements Worm.OrmManagerBase.INewObjects.RemoveNew

    End Sub
End Class
