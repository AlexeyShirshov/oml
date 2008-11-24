Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Orm
Imports Worm.Orm.Meta

<TestClass()> Public Class TestManager
    Implements Worm.OrmManager.INewObjects, Worm.ICreateManager

    Private _schemas As New System.Collections.Hashtable

    Protected Function GetSchema(ByVal v As String) As SQLGenerator
        Dim s As SQLGenerator = CType(_schemas(v), SQLGenerator)
        If s Is Nothing Then
            s = New SQLGenerator(v)
            _schemas.Add(v, s)
        End If
        Return s
    End Function

    Public Shared Function CreateManager(ByVal schema As SQLGenerator) As OrmReadOnlyDBManager
        Return CreateManager(New ReadonlyCache, schema)
    End Function

    Public Class CustomMgr
        Inherits OrmReadOnlyDBManager
        Implements Worm.ICreateManager

        Public Sub New(ByVal cache As ReadonlyCache, ByVal schema As SQLGenerator, ByVal connectionString As String)
            MyBase.New(cache, schema, connectionString)
        End Sub

        Public Function CreateMgr() As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
            Return CreateManager(New SQLGenerator("1"))
        End Function

    End Class

    Public Shared Function CreateManager(ByVal cache As ReadonlyCache, ByVal schema As SQLGenerator) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
        Return New CustomMgr(cache, schema, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        Return New CustomMgr(cache, schema, "Server=.\sqlexpress;Integrated security=true;Initial catalog=test")
#End If
    End Function

    Public Shared Function CreateWriteManager(ByVal schema As SQLGenerator) As OrmDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
        Return New OrmDBManager(New OrmCache, schema, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        Return New OrmDBManager(New OrmCache, schema, "Server=.\sqlexpress;Integrated security=true;Initial catalog=test")
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
    Public Sub TestM2M()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(1)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
            Assert.AreEqual(4, mgr.GetLastExecitionResult.Count)

            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

            c = e.M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
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
            Dim e As Entity = mgr.Find(Of Entity)(1)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next
            Dim l As IList(Of Entity4) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity4))
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)
            Assert.AreEqual(1, l(3).Identifier)
            Assert.AreEqual("first", l(3).Title)

            c = mgr.Find(Of Entity)(2).M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
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
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

            c = e.M2M.Find(Of Entity4)(2, Nothing, Worm.Orm.Sorting.Field("Title").Asc, False)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity4 = mgr.Find(Of Entity4)(1)

            Dim c As ICollection(Of Entity) = e.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
            For Each e4 As Entity In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

            c = e.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(4, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M5()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").Eq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(1, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

            c = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Worm.Orm.Sorting.Field("Title").Asc, False)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsFalse(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M6()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            mgr.Find(Of Entity4)(12).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, False)
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
            Dim e As Entity = mgr.Find(Of Entity)(2)

            mgr.Find(Of Entity4)(12).EnsureLoaded()

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Nothing, True)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2M8()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            'mgr.Find(Of Entity4)(12).Reload()

            mgr.ConvertIds2Objects(Of Entity4)(New Object() {1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12}, False)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c.Count)
            For Each e4 As Entity4 In c
                Assert.IsTrue(e4.InternalProperties.IsLoaded)
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)
            Dim e2 As Entity4 = mgr.Find(Of Entity4)(12)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(1, c3.Count)

            e.M2M.Delete(e2)

            c = e.M2M.Find(Of Entity4)(Nothing, Worm.Orm.Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c.Count)

            c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            c3 = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(0, c3.Count)

            e.RejectChanges()

            c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            c3 = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(1, c3.Count)

            mgr.BeginTransaction()
            Try
                e.M2M.Delete(e2)
                e.SaveChanges(True)

                c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
                Assert.AreEqual(10, c.Count)

                c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
                Assert.AreEqual(9, c2.Count)

                c3 = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
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

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
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

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.M2M.Add(mgr.Find(Of Entity4)(5))

            c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
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

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c3.Count)

            e.M2M.Add(e2)

            c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(12, c.Count)

            c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.SaveChanges(True)

                c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                    Sorting.Field("Title").Asc, True)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.M2M.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(4, c3.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.M2M.Add(mgr.Find(Of Entity4)(5))

            c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(12, c.Count)

            c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            e.M2M.Cancel(GetType(Entity4))

            c = e.M2M.Find(Of Entity4)(Nothing, Sorting.Field("Title").Asc, True)
            Assert.AreEqual(11, c.Count)

            c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MAdd4()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity = mgr.Find(Of Entity)(2)
            Dim e2 As Entity4 = mgr.Find(Of Entity4)(5)

            Dim c2 As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            Dim c3 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c3.Count)

            e.M2M.Add(e2)

            c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                Sorting.Field("Title").Asc, True)
            Assert.AreEqual(10, c2.Count)

            mgr.BeginTransaction()
            Try
                'mgr.Obj2ObjRelationSave(e, GetType(Entity4))
                e.SaveChanges(True)

                c2 = e.M2M.Find(Of Entity4)(New Criteria.Ctor(GetType(Entity4)).Field("Title").NotEq("bt"), _
                    Sorting.Field("Title").Asc, True)
                Assert.AreEqual(11, c2.Count)

                c3 = e2.M2M.Find(Of Entity)(Nothing, Nothing, True)
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
                e.RejectChanges()
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
            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.M2M.Add(mgr.Find(Of Entity4)(10))

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
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
            mgr.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(Me.GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.M2M.Add(e2)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(1, c.Count)

            Dim c2 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
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
    Private _l As New Dictionary(Of Integer, OrmBase)

    Private Function GetIdentity() As Integer
        Return CInt(GetIdentity(Nothing)(0).Value)
    End Function

    Private Function GetIdentity(ByVal t As Type) As Meta.PKDesc() Implements Worm.OrmManager.INewObjects.GetPKForNewObject
        Dim i As Integer = _id
        _id += -1
        Return New PKDesc() {New PKDesc("id", _id)}
    End Function

    Private Function GetNew(ByVal t As Type, ByVal id() As Meta.PKDesc) As _ICachedEntity Implements Worm.OrmManager.INewObjects.GetNew
        Dim o As OrmBase = Nothing
        _l.TryGetValue(CInt(id(0).Value), o)
        Return o
    End Function

    Private Sub AddNew(ByVal obj As _ICachedEntity) Implements Worm.OrmManager.INewObjects.AddNew
        _l.Add(CInt(CType(obj, OrmBase).Identifier), CType(obj, OrmBase))
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestAdd5()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.NewObjectManager = Me
            'mgr.FindNewDelegate = AddressOf GetNew
            Dim e As Entity = New Entity(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)
            Dim c2 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
            Assert.AreEqual(5, c2.Count)

            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(0, c.Count)

            e.M2M.Add(e2)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, False)
            Assert.AreEqual(1, c.Count)

            c2 = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)
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
            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(10)
            e2.Delete()
            mgr.BeginTransaction()
            Try
                e2.SaveChanges(True)

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
                e2.M2M.Delete(GetType(Entity))

                e2.SaveChanges(True)

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
                Assert.IsTrue(mgr.IsInCachePrecise(e2))

                e2.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e2))

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
                Assert.IsTrue(mgr.IsInCachePrecise(e2))

                Dim c2 As ICollection(Of Entity) = e2.M2M.Find(Of Entity)(Nothing, Nothing, False)

                Assert.AreEqual(2, c2.Count)

                e2.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e2))

                e2 = mgr.Find(Of Entity4)(11)

                Assert.IsNull(e2)
            Finally
                mgr.Rollback()
            End Try

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDeletaCacheVal()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As Worm.ReadOnlyList(Of Entity4) = mgr.Find(Of Entity4)(Criteria.Ctor.AutoTypeField("Title").Like("2%"), Nothing, False)

            Assert.AreEqual(3, c.Count)

            Dim e2 As TestProject1.Entity4 = mgr.Find(Of Entity4)(11)
            Assert.IsTrue(c.Contains(e2))

            e2.Delete()
            e2.M2M.Delete(GetType(Entity))

            mgr.BeginTransaction()
            Try
                e2.SaveChanges(True)
                c = mgr.Find(Of Entity4)(Criteria.Ctor.AutoTypeField("Title").Like("2%"), Nothing, False)

                Assert.AreEqual(2, c.Count)
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
                e.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e))
                Assert.AreEqual(ObjectState.Deleted, e.InternalProperties.ObjectState)

                e = mgr.Find(Of Entity2)(10)

                Assert.IsNull(e)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(OrmObjectException))> _
    Public Sub TestConcurrentDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e2 As TestProject1.Entity = mgr.Find(Of Entity)(1)

            Assert.IsTrue(e2.InternalProperties.CanEdit)
            Assert.IsTrue(e2.InternalProperties.CanLoad)

            e2.Delete()
            Assert.AreEqual(ObjectState.Deleted, e2.InternalProperties.ObjectState)
            Assert.IsFalse(e2.InternalProperties.CanEdit)
            Assert.IsFalse(e2.InternalProperties.CanLoad)

            e2 = mgr.Find(Of Entity)(1)
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
            Dim e As Entity5 = mgr.Find(Of Entity5)(1)

            Dim c As ICollection(Of Entity5) = e.M2M.Find(Of Entity5)(Nothing, Nothing, True, False)

            Assert.AreEqual(1, c.Count)

            c = e.M2M.Find(Of Entity5)(Nothing, Nothing, False, False)

            Assert.AreEqual(2, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MTag2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Entity5 = mgr.Find(Of Entity5)(2)

            Dim c As ICollection(Of Entity5) = e.M2M.Find(Of Entity5)(Nothing, Nothing, True, False)

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

            Dim c As ICollection(Of Entity5) = e.M2M.Find(Of Entity5)(Nothing, Nothing, False)

            Assert.AreEqual(1, c.Count)

            Dim l As IList(Of Entity5) = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(1)))
            'Dim e2 As TestProject1.Entity5 = mgr.Find(Of Entity5)(2)
            'Assert.IsTrue(l.Contains(e2))

            c = e.M2M.Find(Of Entity5)(Nothing, Nothing, False, False)
            Assert.AreEqual(2, c.Count)

            l = CType(c, IList(Of Entity5))

            Assert.IsTrue(l.Contains(mgr.Find(Of Entity5)(1)))
            Dim e2 As TestProject1.Entity5 = mgr.Find(Of Entity5)(2)
            Assert.IsTrue(l.Contains(e2))
            Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

            c = e2.M2M.Find(Of Entity5)(Nothing, Nothing, False, False)
            Assert.AreEqual(0, c.Count)

            c = e2.M2M.Find(Of Entity5)(Nothing, Nothing, True, False)
            Assert.AreEqual(2, c.Count)

            mgr.BeginTransaction()
            Try
                e2.M2M.Add(e, False)
                Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

                c = e2.M2M.Find(Of Entity5)(Nothing, Nothing, False, False)
                Assert.AreEqual(1, c.Count)

                c = e2.M2M.Find(Of Entity5)(Nothing, Nothing, False)
                Assert.AreEqual(2, c.Count)

                c = e.M2M.Find(Of Entity5)(Nothing, Nothing, True, False)
                Assert.AreEqual(2, c.Count)

                e2.SaveChanges(True)
                Assert.AreEqual(ObjectState.NotLoaded, e2.InternalProperties.ObjectState)

                c = e.M2M.Find(Of Entity5)(Nothing, Nothing, True, False)
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
            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            Dim c2 As ICollection(Of Entity) = e4.M2M.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(3, c2.Count)

            e.M2M.Add(e4)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(5, c.Count)

            c2 = e4.M2M.Find(Of Entity)(Nothing, Nothing, True)
            Assert.AreEqual(4, c2.Count)

            mgr.BeginTransaction()
            Try
                e.SaveChanges(False)

                c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(5, c.Count)

                c2 = e4.M2M.Find(Of Entity)(Nothing, Nothing, True)
                Assert.AreEqual(4, c2.Count)

                e.RejectChanges()

                c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
                Assert.AreEqual(4, c.Count)

                c2 = e4.M2M.Find(Of Entity)(Nothing, Nothing, True)
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
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id As Integer = e4.ID
            e4.Title = "90bu13n4gf0bh185g8b18bg81bg8b5gfvlojkqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            e.M2M.Add(e4)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
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
                c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
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
            mgr.NewObjectManager = Me
            Dim e As Entity = mgr.Find(Of Entity)(1)
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id As Integer = e4.ID
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c.Count)

            e.M2M.Add(e4)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(5, c.Count)

            mgr.BeginTransaction()
            Try
                Dim created As Boolean
                Using saver As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
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
            mgr.NewObjectManager = Me
            Dim e As New Entity(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e)
            Dim id As Integer = e.ID
            Dim e4 As New Entity4(GetIdentity, mgr.Cache, mgr.MappingEngine)
            AddNew(e4)
            Dim id4 As Integer = e4.ID
            e4.Title = "kqndrg90h5"
            Dim c As ICollection(Of Entity4) = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(0, c.Count)

            e.M2M.Add(e4)

            c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(1, c.Count)

            mgr.BeginTransaction()
            Try
                Dim created As Boolean
                Using saver As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    saver.Add(e)
                    saver.Add(e4)
                    saver.Commit()
                End Using

                c = e.M2M.Find(Of Entity4)(Nothing, Nothing, True)
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
                c = mgr.FindTop(Of Entity2)(100, Nothing, Nothing, True)
            End Using

            'Dim l As IList(Of Entity2) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Entity2))

            Dim e As Entity2 = c(0)
            Assert.IsNull(e.InternalProperties.OriginalCopy)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
            Dim oldv As String = e.Str
            e.Str = "ioquv"
            Assert.AreEqual(ObjectState.Modified, e.InternalProperties.ObjectState)

            'Using New Orm.OrmManager.CacheListSwitcher(mgr, False)
            c = mgr.FindTop(Of Entity2)(100, Nothing, Nothing, True)
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
            Dim col As ICollection(Of Entity) = mgr.ConvertIds2Objects(Of Entity)(New Object() {1, 2}, False)

            Dim rel As Meta.M2MRelation = mgr.MappingEngine.GetM2MRelation(GetType(Entity), GetType(Entity4), True)

            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)
            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)

            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            e1.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            e2.M2M.Find(Of Entity4)(Nothing, Nothing, False)

            mgr.LoadObjects(Of Entity4)(rel, Nothing, CType(col, System.Collections.ICollection), Nothing)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadM2M2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim col As ICollection(Of Entity) = mgr.ConvertIds2Objects(Of Entity)(New Object() {1, 2}, False)
            Dim col4 As New List(Of Entity4)
            mgr.LoadObjects(Of Entity4)(mgr.MappingEngine.GetM2MRelation(GetType(Entity), GetType(Entity4), True), Nothing, CType(col, System.Collections.ICollection), col4)
            Assert.AreEqual(15, col4.Count)

            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)
            'mgr.FindMany2Many(Of Entity4)(e1, Nothing, Nothing,  True)
            'mgr.FindMany2Many(Of Entity4)(e2, Nothing, Nothing,  False)
            Dim c1 As ICollection(Of Entity4) = e1.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            Assert.AreEqual(4, c1.Count)
            For Each o As Entity4 In c1
                Assert.IsTrue(o.InternalProperties.IsLoaded)
            Next
            Dim c2 As ICollection(Of Entity4) = e2.M2M.Find(Of Entity4)(Nothing, Nothing, False)
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
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim r As Worm.ReadOnlyList(Of Entity) = mgr.Find(Of Entity)(Criteria.Ctor.Field(GetType(Entity4), "Title").Like("b%"), Nothing, False)

            Assert.AreEqual(8, r.Count)

            r = mgr.FindDistinct(Of Entity)(Criteria.Ctor.Field(GetType(Entity4), "Title").Like("b%"), Nothing, False)

            Assert.AreEqual(5, r.Count)
        End Using
    End Sub

    <TestMethod()> Public Sub TestM2MSort()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New SQLGenerator("1"))
            Dim r As Worm.ReadOnlyList(Of Entity) = mgr.Find(Of Entity)(Criteria.Ctor.Field(GetType(Entity), "ID").LessThan(3), Sorting.Field(GetType(Entity4), "Title"), False)

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
            Dim e1 As Entity = mgr.Find(Of Entity)(1)
            Dim e2 As Entity = mgr.Find(Of Entity)(2)

            Dim c1 As ICollection(Of Entity4) = Nothing
            Using New Worm.OrmManager.CacheListBehavior(mgr, TimeSpan.FromMilliseconds(10))
                c1 = e1.M2M.Find(Of Entity4)(Nothing, Nothing, True)
            End Using
            System.Threading.Thread.Sleep(100)

            c1 = e1.M2M.Find(Of Entity4)(Nothing, Nothing, True)

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
                c = mgr.FindTop(Of Entity)(100, Nothing, Nothing, False)

                Assert.AreEqual(12, c.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPropertyChangedEvent()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity2 = mgr.Find(Of Entity2)(2)
            Dim c As New cls(e.Str)
            AddHandler e.PropertyChanged, AddressOf c.changed
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
            Dim e As Entity5 = mgr.Find(Of Entity5)(1)
            e.Load()
            Assert.IsTrue(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)

            mgr.FindTop(Of Entity5)(10, Nothing, Nothing, New String() {"Title"})

            Assert.IsTrue(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.None, e.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPartialLoad2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim r As Worm.ReadOnlyList(Of Entity5) = mgr.FindTop(Of Entity5)(10, Nothing, Nothing, New String() {"Title"})

            Assert.IsTrue(mgr.IsInCachePrecise(r(0)))

            Dim e As Entity5 = mgr.Find(Of Entity5)(1)

            Assert.IsFalse(e.InternalProperties.IsLoaded)
            Assert.AreEqual(ObjectState.NotLoaded, e.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod()> _
        Public Sub TestBeginEdit()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim e As Entity5 = mgr.Find(Of Entity5)(1)
            Using e.BeginEdit

            End Using
        End Using
    End Sub

    Public Class cls
        Private _prev As String
        Private _inv As Boolean

        Public Sub New(ByVal s As String)
            _prev = s
        End Sub

        Public Sub changed(ByVal sender As IEntity, ByVal args As Worm.Orm.Entity.PropertyChangedEventArgs)
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

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id() As Meta.PKDesc) Implements Worm.OrmManager.INewObjects.RemoveNew

    End Sub

    Public Sub RemoveNew(ByVal obj As _ICachedEntity) Implements Worm.OrmManager.INewObjects.RemoveNew

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

    Public Function CreateMgr() As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
        Return CreateManager(New SQLGenerator("1"))
    End Function

End Class
