Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Query
Imports Worm
Imports System.Linq
Imports CoreFramework.Structures

<TestClass()> Public Class TestManager
    Implements INewObjectsStore, Worm.ICreateManager

    Private _schemas As New System.Collections.Hashtable

    Protected Function GetSchema(ByVal v As String) As Worm.ObjectMappingEngine
        Dim s As Worm.ObjectMappingEngine = CType(_schemas(v), Worm.ObjectMappingEngine)
        If s Is Nothing Then
            s = New Worm.ObjectMappingEngine(v)
            _schemas.Add(v, s)
        End If
        Return s
    End Function

    Public Shared Function CreateManager(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
        Return CreateManager(New ReadonlyCache, schema)
    End Function

    Public Shared Function CreateManager(ByVal schema As Worm.ObjectMappingEngine, ByVal gen As SQL2000Generator) As OrmReadOnlyDBManager
        Return CreateManager(New ReadonlyCache, schema, gen)
    End Function

    Public Class CustomMgr
        Inherits OrmReadOnlyDBManager
        Implements Worm.ICreateManager

        Public Sub New(ByVal cache As ReadonlyCache, ByVal schema As Worm.ObjectMappingEngine, ByVal connectionString As String)
            MyBase.New(connectionString, schema, New SQL2000Generator, cache)
        End Sub

        Public Sub New(ByVal cache As ReadonlyCache, ByVal schema As Worm.ObjectMappingEngine, ByVal gen As SQL2000Generator, ByVal connectionString As String)
            MyBase.New(connectionString, schema, gen, cache)
        End Sub

        Public Function CreateMgr(ctx As Object) As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
            Dim m As OrmManager = CreateManager(New Worm.ObjectMappingEngine("1"))
            RaiseEvent CreateManagerEvent(Me, New ICreateManager.CreateManagerEventArgs(m, ctx))
            Return m
        End Function

        Public Event CreateManagerEvent(sender As Worm.ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements Worm.ICreateManager.CreateManagerEvent
    End Class

    Public Shared Function CreateManager(ByVal cache As ReadonlyCache, ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
        Return CreateManager(cache, schema, New SQL2000Generator)
    End Function

    Public Shared Function CreateManager(ByVal cache As ReadonlyCache, ByVal schema As Worm.ObjectMappingEngine, _
                                         ByVal gen As SQL2000Generator) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\test.mdf"))
        Return New CustomMgr(cache, schema, gen, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        Return New CustomMgr(cache, schema, gen, "Server=.\sqlexpress;Integrated security=true;Initial catalog=test")
#End If
    End Function

    Public Shared Function CreateManagerWrong(ByVal cache As ReadonlyCache, ByVal schema As Worm.ObjectMappingEngine, _
                                             ByVal gen As SQL2000Generator) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\test.mdf"))
        Return New CustomMgr(cache, schema, gen, "Server=.\sqlexpressS;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        Return New CustomMgr(cache, schema, gen, "Server=.\sqlexpress;Integrated security=true;Initial catalog=test")
#End If
    End Function

    Public Shared Function CreateWriteManager(ByVal schema As Worm.ObjectMappingEngine) As OrmDBManager
        Return CreateWriteManager(schema, New SQL2000Generator)
    End Function

    Public Shared Function CreateWriteManager(ByVal schema As Worm.ObjectMappingEngine, ByVal gen As SQL2000Generator) As OrmDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\test.mdf"))
        Return New OrmDBManager("Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, gen, New OrmCache)
#Else
        Return New OrmDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=test", schema, gen, New OrmCache)
#End If
    End Function

    <TestMethod()> _
    Public Sub TestLoad()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim o As New Entity(1, mgr.Cache, mgr.MappingEngine)
            Assert.IsFalse(o.InternalProperties.IsLoaded)

            o.Load()

            Assert.IsTrue(o.InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoad2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim o As New Entity5(1, mgr.Cache, mgr.MappingEngine)
            Assert.IsFalse(o.InternalProperties.IsLoaded)

            o.Load()

            Assert.IsTrue(o.InternalProperties.IsLoaded)

            Assert.AreEqual("n", o.Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEditCreated()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim o As New Entity4(1, mgr.Cache, mgr.MappingEngine)
            Assert.AreEqual(ObjectState.Created, o.InternalProperties.ObjectState)
            o.Title = "asdfasdf"
            Assert.AreEqual(ObjectState.Created, o.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEditConcurent()
        Dim c As New OrmCache
        Using mgr As OrmReadOnlyDBManager = CreateManager(c, GetSchema("1"))
            Dim o As Entity4 = New QueryCmd().GetByID(Of Entity4)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Assert.AreEqual(ObjectState.None, o.InternalProperties.ObjectState)
            Assert.IsTrue(o.InternalProperties.IsLoaded)
        End Using

        Dim t1 As New System.Threading.Thread(AddressOf wt)
        Dim t2 As New System.Threading.Thread(AddressOf wt)

        t1.Start(New Pair(Of String, OrmCache)("1", c))
        t2.Start(New Pair(Of String, OrmCache)("2", c))
        t1.Join()
        t2.Join()
    End Sub

    Private Sub wt(ByVal o As Object)
        Dim c As OrmCache = CType(o, Pair(Of String, OrmCache)).Second
        Dim id As String = CType(o, Pair(Of String, OrmCache)).First

        Using mgr As OrmReadOnlyDBManager = CreateManager(c, GetSchema("1"))
            If id = "1" Then
                Dim l As Object = New QueryCmd().SelectEntity(GetType(Entity4), True).Where(Ctor.prop(GetType(Entity4), "ID").greater_than(0)).ToList(Of Entity4)(mgr)
            Else
                Dim l As Object = New QueryCmd().SelectEntity(GetType(Entity4), True).Where(Ctor.prop(GetType(Entity4), "ID").less_than(100000)).ToList(Of Entity4)(mgr)
            End If

            Dim e As Entity4 = CType(c.GetKeyEntityFromCacheOrCreate(1, GetType(Entity4), False, mgr.MappingEngine), Entity4)
            Assert.IsNotNull(e)
            Assert.AreNotEqual(ObjectState.Modified, e.InternalProperties.ObjectState)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestM2M()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c.Count)
            Assert.AreEqual(4, mgr.LastExecutionResult.RowCount)

            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

            c = e.GetCmd(GetType(Entity4)).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next
            Dim l As IList(Of Entity4) = CType(c, IList(Of TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(1, l(3).Identifier)
            Assert.AreEqual("first", l(3).Title)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M3()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next
            Dim l As IList(Of Entity4) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(1, l(3).Identifier)
            Assert.AreEqual("first", l(3).Title)

            c = New QueryCmd().GetByID(Of Entity)(2, mgr).GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
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
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

            c = e.GetCmd(GetType(Entity4)).Top(2).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity4 = New QueryCmd().GetByID(Of Entity4)(1, mgr)

            Dim c As ICollection(Of Entity) = e.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

            c = e.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(4, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M5()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

            c = e.GetCmd(GetType(Entity4)).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M6()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim j As Object = New QueryCmd().GetByID(Of Entity4)(12, mgr).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                If e4.ID = 12 Then
                    Assert.IsTrue(e4.InternalProperties.IsLoaded)
                Else
                    Assert.IsFalse(e4.InternalProperties.IsLoaded)
                End If

            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M7()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim j As Object = New QueryCmd().GetByID(Of Entity4)(12, mgr).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M8()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            'mgr.Find(Of Entity4)(12).Reload()
            Dim q As New QueryCmd()
            q.GetByIds(Of Entity4)(New Object() {1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12}, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)
            Dim e2 As Entity4 = New QueryCmd().GetByID(Of Entity4)(12, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(1, c3.Count)

            e.GetCmd(GetType(Entity4)).Remove(e2)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c.Count)

            c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(9, c2.Count)

            c3 = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(0, c3.Count)

            mgr.RejectChanges(e)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            c3 = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(1, c3.Count)

            mgr.BeginTransaction()
            Try
                e.GetCmd(GetType(Entity4)).Remove(e2)
                e.SaveChanges(True)

                c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
                Assert.AreEqual(10, c.Count)

                c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
                Assert.AreEqual(9, c2.Count)

                c3 = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
                Assert.AreEqual(0, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
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
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)

            e.GetCmd(GetType(Entity4)).Add(New QueryCmd().GetByID(Of Entity4)(5, mgr))

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(12, c.Count)
        End Using
    End Sub

    Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim c As ICollection(Of Entity4) = New QueryCmd().Where(New Ctor(GetType(Entity4)).prop("Title").eq("245g0nj")).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)
            Dim e2 As Entity4 = New QueryCmd().GetByID(Of Entity4)(5, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
            Assert.AreEqual(3, c3.Count)

            e.GetCmd(GetType(Entity4)).Add(e2)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(12, c.Count)

            c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.SaveChanges(True)

                c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
                Assert.AreEqual(4, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)

            e.GetCmd(GetType(Entity4)).Add(New QueryCmd().GetByID(Of Entity4)(5, mgr))

            c = e.GetCmd(GetType(Entity4)).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(12, c.Count)

            c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c2.Count)

            e.GetCmd(GetType(Entity4)).Reject(mgr)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c.Count)

            c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)
            Dim e2 As Entity4 = New QueryCmd().GetByID(Of Entity4)(5, mgr)

            Dim c2 As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)) _
                .WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
            Assert.AreEqual(3, c3.Count)

            e.GetCmd(GetType(Entity4)).Add(e2)

            c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.SaveChanges(True)

                c2 = e.GetCmd(GetType(Entity4)).WithLoad(True).Where(New Ctor(GetType(Entity4)).prop("Title").not_eq("bt")).OrderBy(SCtor.prop(GetType(Entity4), "Title").asc).ToList(Of Entity4)(mgr)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
                Assert.AreEqual(4, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.MappingEngine)
            Assert.IsNull(e.InternalProperties.OriginalCopy)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)
                Assert.IsNull(e.InternalProperties.OriginalCopy)

                Assert.IsTrue(e.ID <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.MappingEngine)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(False)
                Assert.IsNotNull(e.InternalProperties.OriginalCopy)

                Assert.IsTrue(e.ID <> -100)
            Finally
                Assert.IsNotNull(e.InternalProperties.OriginalCopy)
                mgr.RejectChanges(e)
                Assert.IsNull(e.InternalProperties.OriginalCopy)

                Assert.AreEqual(-100, e.Identifier)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = New Entity(-100, mgr.Cache, mgr.MappingEngine)
            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(0, c.Count)

            e.GetCmd(GetType(Entity4)).Add(New QueryCmd().GetByID(Of Entity4)(10, mgr))

            c = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)

                Assert.IsTrue(e.ID <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.Cache.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(Me.GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(10, mgr)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(0, c.Count)

            'Dim c2 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            'Assert.AreEqual(5, c2.Count)

            e.GetCmd(GetType(Entity4)).Add(e2)

            c = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)

            Dim c2 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(6, c2.Count)

            'mgr.Obj2ObjRelationAdd(e2, e)

            'c2 = mgr.FindMany2Many(Of Entity4, Entity)(e2, Nothing, Nothing,  False)
            'Assert.AreEqual(6, c2.Count)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)

                Assert.IsTrue(e.ID <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Private _id As Integer = -100
    Private _l As New Dictionary(Of Integer, SinglePKEntity)

    Private Function GetIdentity() As Integer
        Return CInt(GetIdentity(Nothing, Nothing)(0).Value)
    End Function

    Private Function GetIdentity(ByVal t As Type, ByVal mpe As ObjectMappingEngine) As Meta.PKDesc() Implements INewObjectsStore.GetPKForNewObject
        Dim i As Integer = _id
        _id += -1
        Return New PKDesc() {New PKDesc("id", _id)}
    End Function

    Private Function GetNew(ByVal t As Type, ByVal id As IEnumerable(Of Meta.PKDesc)) As _ICachedEntity Implements INewObjectsStore.GetNew
        Dim o As SinglePKEntity = Nothing
        _l.TryGetValue(CInt(id(0).Value), o)
        Return o
    End Function

    Private Sub AddNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.AddNew
        _l.Add(CInt(CType(obj, SinglePKEntity).Identifier), CType(obj, SinglePKEntity))
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestAdd5()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.Cache.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(10, mgr)
            Dim c2 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(5, c2.Count)

            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(0, c.Count)

            e.GetCmd(GetType(Entity4)).Add(e2)

            c = e.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)

            c2 = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)
            Assert.AreEqual(6, c2.Count)

            'mgr.Obj2ObjRelationAdd(e2, e)

            'c2 = mgr.FindMany2Many(Of Entity4, Entity)(e2, Nothing, Nothing,  False)
            'Assert.AreEqual(6, c2.Count)

            mgr.BeginTransaction()
            Try
                e2.SaveChanges(True)

                'Assert.IsTrue(e.Identifier <> -100)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(10, mgr)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                e2.SaveChanges(True)

                e2 = New QueryCmd().GetByID(Of Entity4)(10, mgr)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete2()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(10, mgr)
            e2.Delete()

            mgr.BeginTransaction()
            Try
                e2.GetCmd(GetType(Entity)).RemoveAll(mgr)

                e2.SaveChanges(True)

                e2 = New QueryCmd().GetByID(Of Entity4)(10, GetByIDOptions.EnsureExistsInStore, mgr)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(11, mgr)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCachePrecise(e2))

                e2.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e2))

                e2 = New QueryCmd().GetByID(Of Entity4)(11, GetByIDOptions.EnsureExistsInStore, mgr)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(System.Data.SqlClient.SqlException))> _
    Public Sub TestDelete4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(11, mgr)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCachePrecise(e2))

                Dim c2 As ICollection(Of Entity) = e2.GetCmd(GetType(Entity)).ToList(Of Entity)(mgr)

                Assert.AreEqual(2, c2.Count)

                e2.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e2))

                e2 = New QueryCmd().GetByID(Of Entity4)(11, mgr)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDeletaCacheVal()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As Worm.ReadOnlyList(Of Entity4) = New QueryCmd().Where(Ctor.prop(GetType(Entity4), "Title").[like]("2%")).ToOrmList(Of Entity4)(mgr)

            Assert.AreEqual(3, c.Count)

            Dim e2 As TestProject1.Entity4 = New QueryCmd().GetByID(Of Entity4)(11, mgr)
            Assert.IsTrue(c.Contains(e2))

            e2.Delete()
            e2.GetCmd(GetType(Entity)).RemoveAll(mgr)

            mgr.BeginTransaction()
            Try
                e2.SaveChanges(True)
                c = New QueryCmd().Where(Ctor.prop(GetType(Entity4), "Title").[like]("2%")).ToOrmList(Of Entity4)(mgr)

                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCompositeDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(10, mgr)
            Assert.AreEqual(10, e.ID)
            Assert.AreEqual("a", e.Str)

            e.Delete()
            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e))
                Assert.AreEqual(ObjectState.Deleted, e.InternalProperties.ObjectState)

                e = New QueryCmd().GetByID(Of Entity2)(10, GetByIDOptions.EnsureExistsInStore, mgr)

                Assert.IsNull(e)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestConcurrentDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)

            Assert.IsTrue(e2.InternalProperties.CanEdit)
            Assert.IsTrue(e2.InternalProperties.CanLoad)

            e2.Delete()
            Assert.AreEqual(ObjectState.Deleted, e2.InternalProperties.ObjectState)
            Assert.IsFalse(e2.InternalProperties.CanEdit)
            Assert.IsFalse(e2.InternalProperties.CanLoad)

            e2 = New QueryCmd().GetByID(Of Entity)(1, mgr)
            Assert.AreEqual(ObjectState.Deleted, e2.InternalProperties.ObjectState)
            Assert.IsFalse(e2.InternalProperties.CanEdit)
            Assert.IsFalse(e2.InternalProperties.CanLoad)

            e2.Load()
            'mgr.BeginTransaction()
            'Try
            '    e2.Save(True)

            '    e2 = mgr.Find(Of Entity)(1)

            '    Assert.IsNull(e2)
            'Finally
            '    mgr.Rollback()
            'End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(1, mgr)

            Dim c As ICollection(Of Entity5) = e.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)

            Assert.AreEqual(1, c.Count)

            c = e.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).ToList(Of Entity5)(mgr)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(2, mgr)

            Dim c As ICollection(Of Entity5) = e.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Entity5) = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(New QueryCmd().GetByID(Of Entity5)(1, mgr)))
            Assert.IsTrue(l.Contains(New QueryCmd().GetByID(Of Entity5)(3, mgr)))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(3, mgr)

            Dim c As ICollection(Of Entity5) = e.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)

            Assert.AreEqual(1, c.Count)

            Dim l As IList(Of Entity5) = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(New QueryCmd().GetByID(Of Entity5)(1, mgr)))
            'Dim e2 As TestProject1.Entity5 = mgr.Find(Of Entity5)(2)
            'Assert.IsTrue(l.Contains(e2))

            c = e.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).ToList(Of Entity5)(mgr)
            Assert.AreEqual(2, c.Count)

            l = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(New QueryCmd().GetByID(Of Entity5)(1, mgr)))
            Dim e2 As TestProject1.Entity5 = New QueryCmd().GetByID(Of Entity5)(2, mgr)
            Assert.IsTrue(l.Contains(e2))
            Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

            c = e2.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).ToList(Of Entity5)(mgr)
            Assert.AreEqual(0, c.Count)

            c = e2.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)
            Assert.AreEqual(2, c.Count)

            mgr.BeginTransaction()
            Try
                e2.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).Add(e)
                Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

                c = e2.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).ToList(Of Entity5)(mgr)
                Assert.AreEqual(1, c.Count)

                c = e2.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)
                Assert.AreEqual(2, c.Count)

                c = e.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)
                Assert.AreEqual(2, c.Count)

                c = e.GetCmd(GetType(Entity5), M2MRelationDesc.DirKey).ToList(Of Entity5)(mgr)
                Assert.AreEqual(2, c.Count)

                e2.SaveChanges(True)
                Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

                c = e.GetCmd(GetType(Entity5), M2MRelationDesc.RevKey).ToList(Of Entity5)(mgr)
                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MReject()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))

            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)
            Dim e4 As Entity4 = New QueryCmd().GetByID(Of Entity4)(2, mgr)
            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c.Count)

            Dim c2 As ICollection(Of Entity) = e4.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
            Assert.AreEqual(3, c2.Count)

            e.GetCmd(GetType(Entity4)).Add(e4)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(5, c.Count)

            c2 = e4.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
            Assert.AreEqual(4, c2.Count)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(False)

                c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
                Assert.AreEqual(5, c.Count)

                c2 = e4.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
                Assert.AreEqual(4, c2.Count)

                mgr.RejectChanges(e)

                c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
                Assert.AreEqual(4, c.Count)

                c2 = e4.GetCmd(GetType(Entity)).WithLoad(True).ToList(Of Entity)(mgr)
                Assert.AreEqual(3, c2.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestM2MReject2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.Cache.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id As Integer = e4.ID
            e4.Title = "90bu13n4gf0bh185g8b18bg81bg8b5gfvlojkqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c.Count)

            e.GetCmd(GetType(Entity4)).Add(e4)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(5, c.Count)

            mgr.BeginTransaction()
            Try
                Dim created As Boolean
                Using saver As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using
            Finally
                c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
                Assert.AreEqual(4, c.Count)

                Assert.AreEqual(id, e4.Identifier)
                Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
                Assert.AreEqual(ObjectState.Created, e4.InternalProperties.ObjectState)
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAddNew()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            'mgr.FindNewDelegate = AddressOf GetNew
            mgr.Cache.NewObjectManager = Me
            Dim e As Entity = New QueryCmd().GetByID(Of Entity)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id As Integer = e4.ID
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c.Count)

            e.GetCmd(GetType(Entity4)).Add(e4)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(5, c.Count)

            mgr.BeginTransaction()
            Try
                Dim created As Boolean
                Using saver As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
                Assert.AreEqual(5, c.Count)

                Assert.AreNotEqual(id, e4.Identifier)
                Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
                Assert.AreEqual(ObjectState.None, e4.InternalProperties.ObjectState)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAddNewBoth()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            'mgr.FindNewDelegate = AddressOf GetNew
            mgr.Cache.NewObjectManager = Me
            Dim e As New Entity(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim id As Integer = e.ID
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id4 As Integer = e4.ID
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(0, c.Count)

            e.GetCmd(GetType(Entity4)).Add(e4)

            c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(1, c.Count)

            mgr.BeginTransaction()
            Try
                Dim created As Boolean
                Using saver As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
                Assert.AreEqual(1, c.Count)

                Assert.AreNotEqual(id, e.Identifier)
                Assert.AreNotEqual(id4, e4.Identifier)

                Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
                Assert.AreEqual(ObjectState.None, e4.InternalProperties.ObjectState)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadWithAlter()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As Worm.ReadOnlyList(Of Entity2) = Nothing
            Using New Worm.OrmManager.CacheListBehavior(mgr, False)
                c = New QueryCmd().Top(100).SelectEntity(GetType(Entity2), True).ToOrmList(Of Entity2)(mgr)
            End Using

            'Dim l As IList(Of Entity2) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity2))

            Dim e As Entity2 = c(0)
            Assert.IsNull(e.InternalProperties.OriginalCopy)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
            Dim oldv As String = e.Str
            e.Str = "ioquv"
            Assert.AreEqual(ObjectState.Modified, e.InternalProperties.ObjectState)

            'Using New Orm.OrmManager.CacheListSwitcher(mgr, False)
            c = New QueryCmd().Top(100).SelectEntity(GetType(Entity2), True).ToOrmList(Of Entity2)(mgr)
            'End Using

            Assert.AreEqual(ObjectState.Modified, e.InternalProperties.ObjectState)
            Assert.AreEqual("ioquv", e.Str)

            'e.Load()

            'Assert.AreEqual(oldv, e.Str)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestListener()
        Dim l As New Worm.Web.TraceListener("sdfds")
    End Sub

    <TestMethod()> _
    Public Sub TestLoadM2M()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim col As ICollection(Of Entity) = New QueryCmd().GetByIds(Of Entity)(New Object() {1, 2}, mgr)

            Dim rel As Meta.M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(GetType(Entity), GetType(Entity4), True)

            rel.Load(Of Entity, Entity4)(col, False, mgr)
            rel.Load(Of Entity, Entity4)(col, False, mgr)

            'mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)
            'mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)

            Dim e1 As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)
            Dim e2 As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            e1.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            e2.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)

            'mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)
            rel.Load(Of Entity, Entity4)(col, False, mgr)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim col As ICollection(Of Entity) = New QueryCmd().GetByIds(Of Entity)(New Object() {1, 2}, mgr)
            Dim rel As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(GetType(Entity), GetType(Entity4), True)
            'mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), col4)
            Dim l As ReadOnlyList(Of Entity4) = rel.Load(Of Entity, Entity4)(col, False, mgr)
            Assert.AreEqual(15, l.Count)
            For Each item As Entity4 In l
                Assert.IsNotNull(item)
                Assert.IsFalse(item.InternalProperties.IsLoaded)
                Assert.AreEqual(ObjectState.NotLoaded, item.InternalProperties.ObjectState)
            Next

            Dim e1 As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)
            Dim e2 As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            Dim c1 As ICollection(Of Entity4) = e1.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            Assert.AreEqual(4, c1.Count)
            For Each o As Entity4 In c1
                Assert.IsTrue(o.InternalProperties.IsLoaded)
            Next
            Dim c2 As ICollection(Of Entity4) = e2.GetCmd(GetType(Entity4)).ToList(Of Entity4)(mgr)
            Assert.AreEqual(11, c2.Count)
            'Dim l As IList(Of Entity4) = CType(c1, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            'For Each o As Entity4 In c2
            '    If l.Contains(o) Then
            '        Assert.IsTrue(o.InternalProperties.IsLoaded)
            '    Else
            '        Assert.IsFalse(o.InternalProperties.IsLoaded)
            '    End If
            'Next

            'mgr.LoadObjects(Of Entity4)(mgr.ObjectSchema.GetM2MRelation(GetType(Entity), GetType(Entity4), True), Nothing, CType(col, Collections.ICollection), Nothing)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDistinct()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.AutoJoins = True

            Dim r As Worm.ReadOnlyList(Of Entity) = q.Where(Ctor.prop(GetType(Entity4), "Title").[like]("b%")).ToOrmList(Of Entity)(mgr)

            Assert.AreEqual(8, r.Count)

            q = New QueryCmd()
            q.AutoJoins = True

            r = q.Distinct(True).Where(Ctor.prop(GetType(Entity4), "Title").[like]("b%")).ToOrmList(Of Entity)(mgr)

            Assert.AreEqual(5, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New Worm.ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.AutoJoins = True

            Dim r As Worm.ReadOnlyList(Of Entity) = q.Where(Ctor.prop(GetType(Entity), "ID").less_than(3)).OrderBy(SCtor.prop(GetType(Entity4), "Title")).ToOrmList(Of Entity)(mgr)

            Assert.AreEqual(15, r.Count)

            Assert.AreEqual(1, r(0).Identifier)
            Assert.AreEqual(2, r(1).Identifier)
            Assert.AreEqual(2, r(2).Identifier)
            Assert.AreEqual(1, r(3).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExpireCache()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e1 As Entity = New QueryCmd().GetByID(Of Entity)(1, mgr)
            Dim e2 As Entity = New QueryCmd().GetByID(Of Entity)(2, mgr)

            Dim c1 As ICollection(Of Entity4) = Nothing
            Using New Worm.OrmManager.CacheListBehavior(mgr, TimeSpan.FromMilliseconds(10))
                c1 = e1.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)
            End Using
            System.Threading.Thread.Sleep(100)

            c1 = e1.GetCmd(GetType(Entity4)).WithLoad(True).ToList(Of Entity4)(mgr)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestTopValidate()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As ICollection(Of Entity) = New QueryCmd().Top(100).ToList(Of Entity)(mgr)

            Assert.AreEqual(13, c.Count)

            mgr.BeginTransaction()
            Try
                For Each t As Entity In c
                    Using st As New ModificationsTracker(mgr)
                        Using st2 As New ModificationsTracker(mgr)
                            t.Delete()
                            st2.AcceptModifications()
                        End Using
                        Assert.AreEqual(ObjectState.Deleted, t.InternalProperties.ObjectState)
                        Assert.IsTrue(st.Saver.AffectedObjects.Contains(t))
                        st.AcceptModifications()
                    End Using
                    Exit For
                Next
                c = New QueryCmd().Top(100).ToList(Of Entity)(mgr)

                Assert.AreEqual(12, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPropertyChangedEvent()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(2, mgr)
            Dim c As New cls(e.Str)
            AddHandler e.PropertyChangedEx, AddressOf c.changed
            e.Str = "34f0asdofmasdf"
            Assert.IsTrue(c.Invoked)

            c = New cls(e.Str)
            e.Str = e.Str
            Assert.IsFalse(c.Invoked)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPartialLoad()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(1, mgr)
            e.Load()
            Assert.IsTrue(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)

            Dim o As Object = New QueryCmd().Top(10).Select(FCtor.prop(GetType(Entity5), "Title")).ToList(Of Entity5)(mgr)

            Assert.IsTrue(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPartialLoad2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim r As Worm.ReadOnlyList(Of Entity5) = New QueryCmd().Top(10).Select(FCtor.prop(GetType(Entity5), "Title")).ToOrmList(Of Entity5)(mgr)

            Assert.IsTrue(mgr.IsInCachePrecise(r(0)))

            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(1, mgr)

            Assert.IsFalse(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.NotLoaded, e.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestBeginEdit()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity5 = New QueryCmd().GetByID(Of Entity5)(1, mgr)
            Using e.BeginEdit

            End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDefferedLoading()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1.1"))
            Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(10, mgr)

            Assert.IsNotNull(e)

            Assert.IsFalse(e.InternalProperties.IsLoaded)
            Assert.IsFalse(e.InternalProperties.IsPropertyLoaded("Str"))

            Dim s As String = e.Str

            Assert.IsTrue(e.InternalProperties.IsLoaded)
            Assert.IsTrue(e.InternalProperties.IsPropertyLoaded("Str"))

        End Using
    End Sub

    <TestMethod()>
    Public Sub TestErrorHandling()

        Dim handle As Boolean

        Using mgr As OrmReadOnlyDBManager = CreateManagerWrong(New ReadonlyCache, New Worm.ObjectMappingEngine("1"), New SQL2000Generator)
            AddHandler mgr.ConnectionException,
                Sub(sender As OrmReadOnlyDBManager, args As OrmReadOnlyDBManager.ConnectionExceptionArgs)
                    Assert.IsTrue(TypeOf args.Exception Is System.Data.SqlClient.SqlException)
                    Dim cb As New System.Data.SqlClient.SqlConnectionStringBuilder(args.Connection.ConnectionString)
                    cb.DataSource = ".\sqlexpress"
                    args.Context = cb.ToString
                    args.Action = OrmReadOnlyDBManager.ConnectionExceptionArgs.ActionEnum.RetryNewConnection
                    handle = True
                End Sub

            Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(10, mgr)

            Assert.IsTrue(handle)
            Assert.IsNotNull(e)

        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(InvalidCastException))>
    Public Sub TestErrorHandlingExc()
        Using mgr As OrmReadOnlyDBManager = CreateManagerWrong(New ReadonlyCache, New Worm.ObjectMappingEngine("1"), New SQL2000Generator)
            AddHandler mgr.ConnectionException,
                Sub(sender As OrmReadOnlyDBManager, args As OrmReadOnlyDBManager.ConnectionExceptionArgs)
                    Assert.IsTrue(TypeOf args.Exception Is System.Data.SqlClient.SqlException)
                    args.Action = OrmReadOnlyDBManager.ConnectionExceptionArgs.ActionEnum.RethrowCustom
                    args.Context = New InvalidCastException("my exception")
                End Sub

            Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(10, mgr)
        End Using

    End Sub

    <TestMethod()>
    Public Sub TestErrorHandlingExc2()
        Using mgr As OrmReadOnlyDBManager = CreateManagerWrong(New ReadonlyCache, New Worm.ObjectMappingEngine("1"), New SQL2000Generator)
            AddHandler mgr.ConnectionException,
                Sub(sender As OrmReadOnlyDBManager, args As OrmReadOnlyDBManager.ConnectionExceptionArgs)
                    Assert.IsTrue(TypeOf args.Exception Is System.Data.SqlClient.SqlException)
                    args.Action = OrmReadOnlyDBManager.ConnectionExceptionArgs.ActionEnum.Rethrow
                End Sub

            Try
                Dim e As Entity2 = New QueryCmd().GetByID(Of Entity2)(10, mgr)
            Catch ex As Data.SqlClient.SqlException
                Assert.IsTrue(ex.StackTrace.Contains("line 1461"), ex.StackTrace)
            End Try
        End Using

    End Sub

    <TestMethod()>
    Public Sub TestErrorHandlingNewCommand()
        Using mgr As OrmReadOnlyDBManager = CreateManager(New ReadonlyCache, New Worm.ObjectMappingEngine("1"), New SQL2000Generator)
            Dim cmd As Data.Common.DbCommand = mgr.CreateDBCommand()
            cmd.CommandText = "select * from "

            AddHandler mgr.CommandException,
                Sub(sender As OrmReadOnlyDBManager, args As OrmReadOnlyDBManager.CommandExceptionArgs)
                    args.Action = OrmReadOnlyDBManager.CommandExceptionArgs.ActionEnum.RetryNewCommand
                    args.Context = "select count(*) from ent1"
                End Sub

            Assert.AreEqual(13, CInt(mgr.ExecuteScalar(cmd)))
        End Using

    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))>
    Public Sub TestErrorHandlingNewCommandTran()
        Using mgr As OrmReadOnlyDBManager = CreateManager(New ReadonlyCache, New Worm.ObjectMappingEngine("1"), New SQL2000Generator)

            Dim cmd As Data.Common.DbCommand = mgr.CreateDBCommand()
            cmd.CommandText = "select count(*) from "

            AddHandler mgr.CommandException,
                Sub(sender As OrmReadOnlyDBManager, args As OrmReadOnlyDBManager.CommandExceptionArgs)
                    args.Action = OrmReadOnlyDBManager.CommandExceptionArgs.ActionEnum.RetryNewConnection
                    args.Context = args.Command.Connection.ConnectionString
                End Sub

            Assert.IsNotNull(mgr.BeginTransaction())

            mgr.ExecuteScalar(cmd)
        End Using

    End Sub

    Public Class cls
        Private _prev As String
        Private _inv As Boolean

        Public Sub New(ByVal s As String)
            _prev = s
        End Sub

        Public Sub changed(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs)
            Assert.AreEqual("34f0asdofmasdf", args.CurrentValue)
            Assert.AreEqual(_prev, args.PreviousValue)
            Assert.AreEqual("Str", args.PropertyAlias)
            _inv = True
        End Sub

        Public ReadOnly Property Invoked() As Boolean
            Get
                Return _inv
            End Get
        End Property
    End Class

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As IEnumerable(Of Meta.PKDesc)) Implements INewObjectsStore.RemoveNew

    End Sub

    Public Sub RemoveNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.RemoveNew

    End Sub

    '    Private disposedValue As Boolean = False        ' To detect redundant calls

    '    ' IDisposable
    '    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
    '        If Not Me.disposedValue Then
    '            If disposing Then
    '                ' TODO: free other state (managed objects).
    '            End If

    '            ' TODO: free your own state (unmanaged objects).
    '            ' TODO: set large fields to null.
    '        End If
    '        Me.disposedValue = True
    '    End Sub

    '#Region " IDisposable Support "
    '    ' This code added by Visual Basic to correctly implement the disposable pattern.
    '    Public Sub Dispose() Implements IDisposable.Dispose
    '        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
    '        Dispose(True)
    '        GC.SuppressFinalize(Me)
    '    End Sub
    '#End Region

    Public Function CreateMgr(ctx As Object) As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
        Dim m As OrmManager = CreateManager(New Worm.ObjectMappingEngine("1"))
        RaiseEvent CreateManagerEvent(Me, New ICreateManager.CreateManagerEventArgs(m, ctx))
        Return m
    End Function

    Public Event CreateManagerEvent(sender As Worm.ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements Worm.ICreateManager.CreateManagerEvent
End Class
