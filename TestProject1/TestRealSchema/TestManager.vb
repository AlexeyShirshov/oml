Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm
Imports System.Linq

<TestClass()> _
Public Class TestManagerRS
    Implements INewObjectsStore, Worm.ICreateManager

    Private _schemas As New System.Collections.Hashtable
    Public Function GetSchema(ByVal v As String) As Worm.ObjectMappingEngine
        Dim s As Worm.ObjectMappingEngine = CType(_schemas(v), Worm.ObjectMappingEngine)
        If s Is Nothing Then
            s = New Worm.ObjectMappingEngine(v)
            _schemas.Add(v, s)
        End If
        Return s
    End Function

    Private _cache As CacheBase
    Protected Function GetCache() As CacheBase
        If _c Then
            If _cache Is Nothing Then
                _cache = New ReadonlyCache
            End If
            Return _cache
        Else
            Return New ReadonlyCache
        End If
    End Function

    Private _rwcache As OrmCache
    Protected Function GetRWCache() As OrmCache
        If _c Then
            If _rwcache Is Nothing Then
                _rwcache = New OrmCache
            End If
            Return _rwcache
        Else
            Return New OrmCache
        End If
    End Function

    Public Shared Function CreateManagerSharedFullText(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
        Return New OrmReadOnlyDBManager(My.Settings.FullTextEnabledConn, schema, New SQL2000Generator, New ReadonlyCache)
    End Function

    Public Shared Function CreateManagerShared(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
        Return CreateManagerShared(schema, New ReadonlyCache)
    End Function

    Public Shared Function CreateWriteManagerShared(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
        Return CreateWriteManagerShared(schema, New OrmCache)
    End Function

    Public Shared Function CreateManagerShared(ByVal schema As Worm.ObjectMappingEngine, ByVal cache As ReadonlyCache) As OrmReadOnlyDBManager
        Return CreateManagerShared(schema, cache, New SQL2000Generator)
    End Function

    Public Shared Function CreateManagerShared(ByVal schema As Worm.ObjectMappingEngine, ByVal cache As ReadonlyCache, ByVal stmt As SQL2000Generator) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Return New OrmReadOnlyDBManager("Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, stmt, cache)
#Else
        Return New OrmReadOnlyDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest", schema, stmt, cache)
#End If
    End Function

    Public Shared Function CreateManagerSharedWrong(ByVal schema As Worm.ObjectMappingEngine, ByVal cache As ReadonlyCache, ByVal stmt As SQL2000Generator) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Return New OrmReadOnlyDBManager("Data Source=.\sqlexpressS;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, stmt, cache)
#Else
        Return New OrmReadOnlyDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest", schema, stmt, cache)
#End If
    End Function

    Public Shared Function CreateWriteManagerShared(ByVal schema As Worm.ObjectMappingEngine, ByVal cache As OrmCache) As OrmDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Return New OrmDBManager("Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, New SQL2000Generator, cache)
#Else
        Return New OrmDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest", schema, New SQL2000Generator, cache)
#End If
    End Function

    Public Function CreateManager(ctx As Object) As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
        Dim m As OrmManager = CreateManagerShared(New Worm.ObjectMappingEngine("1"))
        RaiseEvent CreateManagerEvent(Me, New ICreateManager.CreateManagerEventArgs(m, ctx))
        Return m
    End Function

    Public Function CreateManager(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Dim mgr As New OrmReadOnlyDBManager("Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, New SQL2000Generator, GetCache)
#Else
        Dim mgr As New OrmReadOnlyDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest", schema, New SQL2000Generator, GetCache)
#End If
        mgr.Cache.NewObjectManager = Me
        Return mgr
    End Function

    Public Function CreateWriteManager(ByVal schema As Worm.ObjectMappingEngine) As OrmDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Dim mgr As New OrmDBManager("Data Source=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;", schema, New SQL2000Generator, GetRWCache)
#Else
        Dim mgr As New OrmDBManager("Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest", schema, New SQL2000Generator, GetRWCache)
#End If
        mgr.Cache.NewObjectManager = Me
        Return mgr
    End Function

    Private _l As Boolean
    Public Property WithLoad() As Boolean
        Get
            Return _l
        End Get
        Set(ByVal value As Boolean)
            _l = value
        End Set
    End Property

    Private _c As Boolean
    Public Property SharedCache() As Boolean
        Get
            Return _c
        End Get
        Set(ByVal value As Boolean)
            _c = value
        End Set
    End Property

    <TestMethod()> _
    Public Sub TestSave()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)

            t2.Tbl = New QueryCmd().GetByID(Of Table1)(2, mgr)

            mgr.BeginTransaction()
            Try
                t2.SaveChanges(True)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestSaveConcurrency()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t As Table3 = New QueryCmd().GetByID(Of Table3)(1, mgr)
            Assert.IsNotNull(t)
            t.Code = t.Code + CByte(10)

            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Ref"))

            Dim t2 As Table3 = Nothing
            Dim prev As Byte = 0
            Try
                Using mgr2 As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
                    t2 = New QueryCmd().GetByID(Of Table3)(1, mgr2)
                    prev = t2.Code
                    t2.Code = t.Code + CByte(10)
                    mgr2.SaveChanges(t2, True)
                End Using

                mgr.SaveChanges(t, True)
            Finally
                Using mgr2 As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
                    t2.Code = prev
                    mgr2.SaveChanges(t2, True)
                End Using
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Dim tt As IList(Of Table2) = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, tt.Count)

            t2.Tbl = New QueryCmd().GetByID(Of Table1)(2, mgr)

            mgr.BeginTransaction()
            Try
                t2.SaveChanges(True)

                tt = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))).ToList(Of Table2)(mgr)
                Assert.AreEqual(1, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            'Dim t1 As Table1 = New Table1(1, mgr.Cache, mgr.ObjectSchema)
            Dim t1 As Table1 = mgr.GetKeyEntityFromCacheOrCreate(Of Table1)(1)

            Dim tt As IList(Of Table2) = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, tt.Count)

            Dim t2 As New Table2(-100, mgr.Cache, mgr.MappingEngine)
            t2.Tbl = t1

            mgr.BeginTransaction()
            Try
                t2.SaveChanges(True)

                tt = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToList(Of Table2)(mgr)
                Assert.AreEqual(3, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache3()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t1 As Table1 = New Table1(1, mgr.Cache, mgr.MappingEngine)
            Dim tt As IList(Of Table2) = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToList(Of Table2)(mgr)
            Assert.AreEqual(2, tt.Count)

            Dim t2 As New Table2(-100, mgr.Cache, mgr.MappingEngine)
            t2.Tbl = New Table1(2, mgr.Cache, mgr.MappingEngine)

            mgr.BeginTransaction()
            Try
                t2.SaveChanges(True)

                tt = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToList(Of Table2)(mgr)
                Assert.AreEqual(2, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFindField()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim c As ICollection(Of Table1) = New QueryCmd().Where(New Ctor(GetType(Table1)).prop("Code").eq(2)).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestXmlField()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t As Table3 = New QueryCmd().GetByID(Of Table3)(2, mgr)

            Assert.AreEqual("root", t.Xml.DocumentElement.Name)
            Dim attr As System.Xml.XmlAttribute = t.Xml.CreateAttribute("first")
            attr.Value = "hi!"
            t.Xml.DocumentElement.Attributes.Append(attr)

            t.SaveChanges(True)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddBlob()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t2 As New Table2(-100, mgr.Cache, mgr.MappingEngine)

            mgr.BeginTransaction()

            Try
                t2.SaveChanges(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadGUID()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table4 = New QueryCmd().GetByID(Of Table4)(1, mgr)

            Assert.AreEqual(False, t.Col)

            Assert.AreEqual(New Guid("7c78c40a-fd96-44fe-861f-0f87b8d04bd5"), t.GUID)

            Dim cc As ICollection(Of Table4) = New QueryCmd().SelectEntity(GetType(Table4), True).Where(New Ctor(GetType(Table4)).prop("Col").eq(False)).ToList(Of Table4)(mgr)

            Assert.AreEqual(1, cc.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddWithPK()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t As New Table4(4, mgr.Cache, mgr.MappingEngine)
            Dim g As Guid = t.GUID
            mgr.BeginTransaction()

            Try
                t.SaveChanges(True)

                Assert.AreNotEqual(g, t.GUID)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddWithPK2()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("2"))
            Dim t As New Table4(4, mgr.Cache, mgr.MappingEngine)


            mgr.BeginTransaction()

            Try
                t.SaveChanges(True)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSwitchCache()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Using New Worm.OrmManager.CacheListBehavior(mgr, False)
                Dim c2 As ICollection(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), WithLoad).Where(New Ctor(GetType(Table1)).prop("Code").eq(2)).ToList(Of Table1)(mgr)

                Assert.AreEqual(1, c2.Count)
            End Using

            Dim c As ICollection(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), WithLoad).Where(New Ctor(GetType(Table1)).prop("Code").eq(2)).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)

            c = New QueryCmd().SelectEntity(GetType(Table1), WithLoad).Where(New Ctor(GetType(Table1)).prop("Code").eq(2)).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSwitchCache2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim n As Date = Now
            For i As Integer = 0 To 100000
                Dim need As Boolean = Now.Subtract(n).TotalSeconds < 1
                If Not need Then
                    n = Now
                End If

                Using New Worm.OrmManager.CacheListBehavior(mgr, need)
                    Dim c2 As ICollection(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), WithLoad).Where(New Ctor(GetType(Table1)).prop("Code").eq(2)).ToList(Of Table1)(mgr)

                    Assert.AreEqual(1, c2.Count)
                End Using
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDeleteFromCache()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)

            Dim t3 As Worm.ReadOnlyList(Of Table2) = New QueryCmd().SelectEntity(GetType(Table2), WithLoad).Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToOrmList(Of Table2)(mgr)
            t3.LoadObjects()
            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Next

            mgr.BeginTransaction()
            Try

                mgr.RemoveObjectFromCache(t1)

                t3 = New QueryCmd().SelectEntity(GetType(Table2), WithLoad).Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToOrmList(Of Table2)(mgr)

                Assert.AreEqual(2, t3.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(schema)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t3 As Table33 = New QueryCmd().GetByID(Of Table33)(1, mgr)
            Dim c As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).ToList(Of Table33)(mgr)

            Assert.AreEqual(3, c.Count)

            Dim c2 As ICollection(Of Table1) = t3.GetCmd(GetType(Table1)).WithLoad(WithLoad).OrderBy(SCtor.prop(GetType(Table1), "Enum").asc).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c2.Count)

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.MappingEngine)
            r1.Title = "913nv"
            r1.Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(True)

                c = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).ToList(Of Table33)(mgr)

                Assert.AreEqual(3, c.Count)

                c2 = t3.GetCmd(GetType(Table1)).WithLoad(WithLoad).OrderBy(SCtor.prop(GetType(Table1), "Enum").asc).ToList(Of Table1)(mgr)

                Assert.AreEqual(2, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M2()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(schema)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t3 As Table33 = New QueryCmd().GetByID(Of Table33)(1, mgr)
            Dim c As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).ToList(Of Table33)(mgr)

            Assert.AreEqual(3, c.Count)

            Dim c2 As ICollection(Of Table1) = t3.GetCmd(GetType(Table1)).WithLoad(WithLoad).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c2.Count)

            Dim r1 As Tables1to3 = New QueryCmd().GetByID(Of Tables1to3)(1, mgr)
            r1.Delete()
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(True)

                c = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).ToList(Of Table33)(mgr)

                Assert.AreEqual(2, c.Count)

                c2 = t3.GetCmd(GetType(Table1)).WithLoad(WithLoad).ToList(Of Table1)(mgr)

                Assert.AreEqual(0, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M3()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(schema)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t3 As Table33 = New QueryCmd().GetByID(Of Table33)(2, mgr)
            Dim t As Type = schema.GetTypeByEntityName("Table3")
            Dim f As PredicateLink = CType(New Ctor(t).prop("Code").eq(2), PredicateLink)
            Dim c As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).Where(f).ToList(Of Table33)(mgr)

            Assert.AreEqual(2, c.Count)

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.MappingEngine)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(True)

                Dim c2 As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).Where(f).ToList(Of Table33)(mgr)

                Assert.AreEqual(3, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M4()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(schema)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t3 As Table33 = New QueryCmd().GetByID(Of Table33)(1, mgr)
            Dim c As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(WithLoad).ToList(Of Table33)(mgr)

            Assert.AreEqual(3, c.Count)

            t1.GetCmd(GetType(Table33)).Add(t3)

            mgr.BeginTransaction()
            Try
                t1.SaveChanges(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M5()
        Dim schema As Worm.ObjectMappingEngine = GetSchema("1")

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(schema)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t3 As Table33 = New QueryCmd().GetByID(Of Table33)(1, mgr)
            Dim c As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(True).ToList(Of Table33)(mgr)

            Assert.AreEqual(3, c.Count)

            For Each o As Table33 In c
                Assert.IsTrue(o.InternalProperties.IsLoaded)
            Next

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.MappingEngine)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.SaveChanges(False)

                Assert.AreNotEqual(-100, r1.Identifier)

                Dim c2 As ICollection(Of Table33) = t1.GetCmd(GetType(Table33)).WithLoad(True).ToList(Of Table33)(mgr)
                Assert.IsTrue(mgr.LastExecutionResult.CacheHit)
                Assert.AreEqual(3, c2.Count)

                mgr.RejectChanges(r1)

                c2 = t1.GetCmd(GetType(Table33)).WithLoad(True).ToList(Of Table33)(mgr)
                Assert.AreEqual(3, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim tt2 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)

            Dim t1s As ICollection(Of Table1) = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2}, mgr)
            Dim t10s As ICollection(Of Table10) = New QueryCmd().Select(FCtor.prop(GetType(Table10), "Table1")).Where(Ctor.prop(GetType(Table10), "Table1").in(t1s)).ToList(Of Table10)(mgr)

            Assert.AreEqual(3, t10s.Count)

            Dim t1 As ICollection(Of Table10) = New QueryCmd().SelectEntity(GetType(Table10), WithLoad).Where(New Ctor(GetType(Table10)).prop("Table1").eq(tt1)).ToList(Of Table10)(mgr)
            Assert.AreEqual(2, t1.Count)

            Dim t2 As ICollection(Of Table10) = New QueryCmd().SelectEntity(GetType(Table10), WithLoad).Where(New Ctor(GetType(Table10)).prop("Table1").eq(tt2)).ToList(Of Table10)(mgr)
            Assert.AreEqual(1, t2.Count)

            t10s = New QueryCmd().Select(FCtor.prop(GetType(Table10), "Table1")).Where(Ctor.prop(GetType(Table10), "ID").in(t1s)).ToList(Of Table10)(mgr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects2()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim tt2 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)

            Dim t1 As ICollection(Of Table10) = New QueryCmd().SelectEntity(GetType(Table10), WithLoad).Where(New Ctor(GetType(Table10)).prop("Table1").eq(tt1)).ToList(Of Table10)(mgr)
            Assert.AreEqual(2, t1.Count)

            Dim t1s As ReadOnlyList(Of Table1) = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2}, mgr)
            Dim t10s As ReadOnlyList(Of Table10) = t1s.LoadChildren(Of Table10)(New RelationDesc(New EntityUnion(GetType(Table10)), "Table1"), True, mgr)
            Assert.AreEqual(3, t10s.Count)

            Dim t2 As ICollection(Of Table10) = New QueryCmd().SelectEntity(GetType(Table10), WithLoad).Where(New Ctor(GetType(Table10)).prop("Table1").eq(tt2)).ToList(Of Table10)(mgr)
            Assert.AreEqual(1, t2.Count)

        End Using
    End Sub

    '<TestMethod()> _
    'Public Sub TestLoadObjects3()
    '    Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
    '        Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
    '        Dim tt2 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)

    '        Dim t1s As ReadOnlyList(Of Table1) = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2}, False, mgr)
    '        Dim t10s As ICollection(Of Table10) = mgr.LoadObjects(Of Table10)("Table1", New Ctor(GetType(Table10)).prop("ID").eq(1), t1s)
    '        Assert.AreEqual(1, t10s.Count)

    '    End Using
    'End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects4()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.CreateKeyEntity(Of Table1)(1)
            Dim tt2 As Table1 = mgr.CreateKeyEntity(Of Table1)(1)

            Dim r As New Worm.ReadOnlyList(Of Table1)(New List(Of Table1)(New Table1() {tt1, tt2}))
            r.LoadObjects()
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects5()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table2 = mgr.CreateKeyEntity(Of Table2)(1)

            Dim t As ICollection(Of Table2) = mgr.LoadObjects(Of Table2)( _
                New Worm.ReadOnlyList(Of Table2)(New List(Of Table2)(New Table2() {tt1})), New String() {"Table1"}, 0, 1)

            Assert.AreEqual(1, t.Count)

            Assert.AreEqual(1, CType(t, IList(Of Table2))(0).Tbl.Identifier)
            Assert.IsTrue(CType(t, IList(Of Table2))(0).Tbl.InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjectsM2M()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t1s As ICollection(Of Table1) = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2}, mgr)
            Dim rel As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(GetType(Table1), GetType(Table33), True)
            rel.Load(Of Table1, Table33)(t1s, False, mgr)
            'mgr.LoadObjects(Of Table33)(rel, Nothing, CType(t1s, System.Collections.ICollection), Nothing)

            Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)

            tt1.GetCmd(GetType(Table33)).ToList(Of Table33)(mgr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MFilterValidation()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t As Type = mgr.MappingEngine.GetTypeByEntityName("Table3")
            'Dim con As New Orm.OrmCondition.OrmConditionConstructor
            'con.AddFilter(New Orm.OrmFilter(t, "Code", New TypeWrap(Of Object)(2), Orm.FilterOperation.Equal))
            Dim c As ICollection(Of Table33) = tt1.GetCmd(GetType(Table33)).WithLoad(WithLoad).Where(New Ctor(t).prop("Code").eq(2)).ToList(Of Table33)(mgr)

            Assert.AreEqual(2, c.Count)
            mgr.BeginTransaction()
            Try
                Dim tt2 As Table33 = New QueryCmd().GetByID(Of Table33)(1, mgr)
                Assert.AreEqual(Of Byte)(1, tt2.Code)
                tt2.Code = 2
                tt2.SaveChanges(True)

                c = tt1.GetCmd(GetType(Table33)).WithLoad(WithLoad).Where(New Ctor(t).prop("Code").eq(2)).ToList(Of Table33)(mgr)

                Assert.AreEqual(3, c.Count)
            Finally
                mgr.Rollback()

            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MSorting()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim tt1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t As Type = mgr.MappingEngine.GetTypeByEntityName("Table3")
            'Dim con As New Orm.OrmCondition.OrmConditionConstructor
            'con.AddFilter(New Orm.OrmFilter(t, "Code", New TypeWrap(Of Object)(2), Orm.FilterOperation.Equal))
            Dim s As OrderByClause = SCtor.prop(GetType(Table33), "Code").desc
            Dim c As Worm.ReadOnlyList(Of Table33) = tt1.GetCmd(GetType(Table33)).WithLoad(WithLoad).OrderBy(s).ToOrmList(Of Table33)(mgr)
            Assert.AreEqual(3, c.Count)
            'Assert.AreEqual(Of Byte)(2, c(0).Code)
            'Assert.AreEqual(Of Byte)(1, c(1).Code)

            mgr.BeginTransaction()
            Try
                Using st As New ModificationsTracker(mgr)
                    Dim tt2 As Table33 = New Table33(-100, mgr.Cache, mgr.MappingEngine)
                    st.Add(tt2)
                    tt2.RefObject = tt1
                    tt2.Code = 3

                    Dim t3 As New Tables1to3(-101, mgr.Cache, mgr.MappingEngine)
                    st.Add(t3)

                    t3.Table1 = tt1
                    t3.Table3 = tt2
                    t3.Title = "sdfpsdfm"
                    st.AcceptModifications()
                End Using

                c = tt1.GetCmd(GetType(Table33)).WithLoad(WithLoad).OrderBy(s).ToOrmList(Of Table33)(mgr)
                Assert.AreEqual(4, c.Count)
                Assert.AreEqual(Of Byte)(3, c(0).Code)
                Assert.AreEqual(Of Byte)(2, c(1).Code)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFuncs()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("2"))
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Assert.IsNotNull(t1)
        End Using

        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("3"))
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)
            Assert.IsNotNull(t1)

            t1 = New QueryCmd().GetByID(Of Table1)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Assert.IsNull(t1)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestMultipleDelete()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim f As New EntityFilter(GetType(Table3), "Code", New ScalarValue(1), Worm.Criteria.FilterOperation.LessEqualThan)
            mgr.BeginTransaction()

            Try
                Dim i As Integer = mgr.Delete(f)
                Assert.AreEqual(1, i)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDeleteNotLoaded()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table1 = mgr.GetKeyEntityFromCacheOrCreate(Of Table1)(1)
            Assert.AreEqual(ObjectState.NotLoaded, t.InternalProperties.ObjectState)
            t.Delete()
            Assert.AreEqual(ObjectState.Deleted, t.InternalProperties.ObjectState)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestSimpleObjects()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim s1 As SimpleObj = New QueryCmd().GetByID(Of SimpleObj)(1, mgr)

            Assert.AreEqual("first", s1.Title)

            mgr.BeginTransaction()
            Try
                s1.Delete()
                s1.SaveChanges(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleObjects2()
        Using mgr As OrmDBManager = CreateWriteManager(GetSchema("1"))
            Dim s1 As SimpleObj = New QueryCmd().GetByID(Of SimpleObj)(2, mgr)

            Assert.AreEqual("second", s1.Title)

            mgr.BeginTransaction()
            Try
                s1 = New SimpleObj
                s1.Title = "555"
                s1.SaveChanges(True)
            Finally
                Assert.IsTrue(CInt(s1.Identifier) > 0)
                Assert.AreEqual("555", s1.Title)
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPager()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim cc As ICollection(Of Table1) = New QueryCmd().Top(10).SelectEntity(GetType(Table1), True).ToList(Of Table1)(mgr)
            Assert.AreEqual(3, cc.Count)

            'Using New Worm.OrmManager.PagerSwitcher(mgr, 0, 1)
            cc = New QueryCmd().Top(10).Paging(0, 1).SelectEntity(GetType(Table1), True).ToList(Of Table1)(mgr)
            Assert.AreEqual(1, cc.Count)
            'End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExecResults()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim cc As ICollection(Of Table1) = New QueryCmd().Top(10).SelectEntity(GetType(Table1), True).ToList(Of Table1)(mgr)

            Assert.AreEqual(3, mgr.LastExecutionResult.RowCount)
            Assert.IsFalse(mgr.LastExecutionResult.CacheHit)

            System.Diagnostics.Trace.WriteLine(mgr.LastExecutionResult.ExecutionTime.ToString)
            System.Diagnostics.Trace.WriteLine(mgr.LastExecutionResult.FetchTime.ToString)

            Dim t As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            t.Load()

            Assert.AreEqual(1, mgr.Cache.GetLoadTime(GetType(Table1)).First)

            System.Diagnostics.Trace.WriteLine(mgr.Cache.GetLoadTime(GetType(Table1)).Second.ToString)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCompositeDelete()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Composite = New QueryCmd().GetByID(Of Composite)(1, mgr)
            Assert.AreEqual(1, e.ID)
            Assert.AreEqual("привет", e.Message)
            Assert.AreEqual("hi", e.Message2)

            e.Delete()
            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)

                Assert.IsFalse(mgr.IsInCachePrecise(e))

                e = New QueryCmd().GetByID(Of Composite)(1, GetByIDOptions.EnsureExistsInStore, mgr)

                Assert.IsNull(e)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCompositeUpdate()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As Composite = New QueryCmd().GetByID(Of Composite)(1, mgr)
            Assert.AreEqual(1, e.ID)
            Assert.AreEqual("привет", e.Message)
            Assert.AreEqual("hi", e.Message2)

            e.Message2 = "adfgopmi"

            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCompositeInsert()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim e As New Composite(1, mgr.Cache, mgr.MappingEngine)
            e.Message = "don"
            e.Message2 = "dionsd"
            mgr.BeginTransaction()
            Try
                e.SaveChanges(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestEntityM2M()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Tables1to1 = New QueryCmd().GetByID(Of Tables1to1)(1, mgr)
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t1back As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)

            Assert.AreEqual(1, t.Identifier)

            Assert.AreEqual(t1, t.Table1)
            Assert.AreEqual(t1back, t.Table1Back)

            Assert.AreEqual(1, t1.GetCmd(GetType(Table1), M2MRelationDesc.DirKey).ToList(Of Table1)(mgr).Count)

            Assert.AreEqual(2, t1.GetCmd(GetType(Table1), M2MRelationDesc.RevKey).ToList(Of Table1)(mgr).Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As ICollection(Of Table2) = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").eq(1)).ToList(Of Table2)(mgr)
            Dim c2 As ICollection(Of Table2) = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").eq(2)).ToList(Of Table2)(mgr)

            Assert.AreEqual(1, c.Count)
            Assert.AreEqual(1, c2.Count)

            mgr.BeginTransaction()
            Try
                Dim t As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
                Assert.AreEqual(1D, t.Money)

                t.Money = 2D
                t.SaveChanges(True)

                c = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").eq(1)).ToList(Of Table2)(mgr)
                Assert.IsTrue(mgr.LastExecutionResult.CacheHit)
                c2 = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").eq(2)).ToList(Of Table2)(mgr)
                Assert.IsTrue(mgr.LastExecutionResult.CacheHit)

                Assert.AreEqual(0, c.Count)
                Assert.AreEqual(2, c2.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSort()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt() As Table10 = New Table10() {New QueryCmd().GetByID(Of Table10)(2, mgr), New QueryCmd().GetByID(Of Table10)(1, mgr), New QueryCmd().GetByID(Of Table10)(3, mgr)}
            Dim c As IEnumerable(Of Table10) = Worm.OrmManager.ApplySort(tt, SCtor.prop(GetType(Table10), "Table1"), mgr.MappingEngine)
            Assert.AreEqual(1, GetList(Of Table10)(c)(0).Identifier)
            Assert.AreEqual(2, GetList(Of Table10)(c)(1).Identifier)
            Assert.AreEqual(3, GetList(Of Table10)(c)(2).Identifier)

            c = Worm.OrmManager.ApplySort(tt, SCtor.prop(GetType(Table10), "Table1").desc, mgr.MappingEngine)
            Assert.AreEqual(3, GetList(Of Table10)(c)(0).Identifier)
            'Assert.AreEqual(2, GetList(Of Table10)(c)(1).Identifier)
            'Assert.AreEqual(1, GetList(Of Table10)(c)(2).Identifier)

            c = Worm.OrmManager.ApplySort(tt, SCtor.prop(GetType(Table10), "Table1").prop(GetType(Table10), "ID"), mgr.MappingEngine)
            Assert.AreEqual(1, GetList(Of Table10)(c)(0).Identifier)
            Assert.AreEqual(2, GetList(Of Table10)(c)(1).Identifier)
            Assert.AreEqual(3, GetList(Of Table10)(c)(2).Identifier)

            c = Worm.OrmManager.ApplySort(tt, SCtor.prop(GetType(Table10), "Table1").prop(GetType(Table10), "ID").desc, mgr.MappingEngine)
            Assert.AreEqual(2, GetList(Of Table10)(c)(0).Identifier)
            Assert.AreEqual(1, GetList(Of Table10)(c)(1).Identifier)
            Assert.AreEqual(3, GetList(Of Table10)(c)(2).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSortEx()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt() As Table10 = New Table10() {New QueryCmd().GetByID(Of Table10)(2, mgr), New QueryCmd().GetByID(Of Table10)(1, mgr), New QueryCmd().GetByID(Of Table10)(3, mgr)}
            Dim c As IEnumerable(Of Table10) = Worm.OrmManager.ApplySort(tt, SCtor.prop(GetType(Table1), "Title"), mgr.MappingEngine)
            Assert.AreEqual(1, GetList(Of Table10)(c)(0).Identifier)
            Assert.AreEqual(2, GetList(Of Table10)(c)(1).Identifier)
            Assert.AreEqual(3, GetList(Of Table10)(c)(2).Identifier)

            Dim c2 As System.Collections.ICollection = Worm.OrmManager.ApplySortT(tt, SCtor.prop(GetType(Table1), "Title"), mgr.MappingEngine)
            Dim l2 As System.Collections.IList = CType(c2, System.Collections.IList)
            Assert.AreEqual(1, CType(l2(0), Table10).Identifier)
            Assert.AreEqual(2, CType(l2(1), Table10).Identifier)
            Assert.AreEqual(3, CType(l2(2), Table10).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestIsNull()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Worm.ReadOnlyList(Of Table3) = New QueryCmd().Where(Ctor.prop(GetType(Table3), "XML").is_null).ToOrmList(Of Table3)(mgr)

            Assert.AreEqual(1, t.Count)

            Dim t2 As ICollection(Of Table2) = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Table1").is_null).ToOrmList(Of Table2)(mgr)

            Assert.AreEqual(0, t2.Count)

            Dim r As Boolean
            Dim f As Worm.Criteria.Core.IFilter = Ctor.prop(GetType(Table3), "XML").is_null.Filter()
            Assert.AreEqual(1, mgr.ApplyFilter(t, f, r).Count)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestIn()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Worm.ReadOnlyObjectList(Of Table1) = New QueryCmd().Where(Ctor.prop(GetType(Table1), "EnumStr").[in]( _
                New String() {"first", "sec"})).ToOrmList(Of Table1)(mgr)

            Assert.AreEqual(3, t.Count)

            Dim r As Boolean
            t = mgr.ApplyFilter(t, CType(New Ctor(GetType(Table1)).prop("Code").[in]( _
                New Integer() {45, 8923}).Filter, Worm.Criteria.Core.IEntityFilter), r)

            Assert.AreEqual(2, t.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestNotIn()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As ICollection(Of Table1) = New QueryCmd().Where(Ctor.prop(GetType(Table1), "EnumStr").not_in( _
                New String() {"sec"})).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, t.Count)

            t = New QueryCmd().Where(Ctor.prop(GetType(Table1), "EnumStr").not_in( _
                New String() {})).ToList(Of Table1)(mgr)

            Assert.AreNotEqual(1, t.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestInSubQuery()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t As ICollection(Of Table2) = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Table1").[in]( _
                GetType(Table1))).ToList(Of Table2)(mgr)

            Assert.AreEqual(2, t.Count)

            t = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").[in]( _
                GetType(Table1), "Code")).ToList(Of Table2)(mgr)

            Assert.AreEqual(1, t.Count)
            Assert.AreEqual(2D, GetList(t)(0).Money)

            t = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").[in]( _
                GetType(Table1), "Code")).ToList(Of Table2)(mgr)

            Assert.AreEqual(1, t.Count)

            mgr.BeginTransaction()
            Try
                Dim t2 As New Table2(1934, mgr.Cache, mgr.MappingEngine)
                t2.Tbl = New QueryCmd().GetByID(Of Table1)(1, mgr)
                t2.Money = 2
                t2.SaveChanges(True)

                t = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Money").[in]( _
                    GetType(Table1), "Code")).ToList(Of Table2)(mgr)

                Assert.AreEqual(2, t.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExistSubQuery()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim tt2 As Type = GetType(Table2)

            'Dim t As ICollection(Of Table2) = mgr.Find(Of Table2)(New Ctor(tt2).prop("Table1").exists( _
            '    GetType(Table1)), Nothing, False)

            'Assert.AreEqual(2, t.Count)

            't = mgr.Find(Of Table2)(New Ctor(tt2).prop("Table1").not_exists( _
            '    GetType(Table1)), Nothing, False)

            'Assert.AreEqual(0, t.Count)

            Dim t As ICollection(Of Table2) = Nothing

            Dim c As New Condition.ConditionConstructor
            c.AddFilter(New JoinFilter(tt2, "Table1", GetType(Table1), "ID", Worm.Criteria.FilterOperation.Equal))
            c.AddFilter(New EntityFilter(GetType(Table1), "Code", New ScalarValue(45), Worm.Criteria.FilterOperation.Equal))
            Dim f As Worm.Criteria.Core.IFilter = CType(c.Condition, Worm.Criteria.Core.IFilter)

            t = New QueryCmd().Where(Ctor.not_exists( _
                GetType(Table1), f)).ToList(Of Table2)(mgr)

            Assert.AreEqual(2, t.Count)

            t = New QueryCmd().Where(Ctor.not_exists( _
                GetType(Table1), f)).ToList(Of Table2)(mgr)

            Assert.AreEqual(2, t.Count)

            mgr.BeginTransaction()
            Try
                Dim t2 As New Table2(1934, mgr.Cache, mgr.MappingEngine)
                t2.Tbl = New QueryCmd().GetByID(Of Table1)(1, mgr)
                t2.SaveChanges(True)

                t = New QueryCmd().Where(Ctor.prop(GetType(Table2), "Table1").not_exists( _
                    GetType(Table1), f)).ToList(Of Table2)(mgr)

                Assert.AreEqual(3, t.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestBetween()
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim c As ICollection(Of Table1) = New QueryCmd().Where(Ctor.prop(GetType(Table1), "Code").between(2, 45)).OrderBy(SCtor.prop(GetType(Table1), "Code")).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, c.Count)

            Assert.AreEqual(2, GetList(c)(0).Code)
            Assert.AreEqual(45, GetList(c)(1).Code)

            mgr.BeginTransaction()
            Try
                GetList(c)(0).Code = 100
                GetList(c)(0).SaveChanges(True)

                c = New QueryCmd().Where(Ctor.prop(GetType(Table1), "Code").between(2, 45)).OrderBy(SCtor.prop(GetType(Table1), "Code")).ToList(Of Table1)(mgr)

                Assert.AreEqual(1, c.Count)

                Dim t As New Table1(GetIdentity, mgr.Cache, mgr.MappingEngine)
                t.Code = 30
                t.CreatedAt = Now
                t.SaveChanges(True)

                c = New QueryCmd().Where(Ctor.prop(GetType(Table1), "Code").between(2, 45)).OrderBy(SCtor.prop(GetType(Table1), "Code")).ToList(Of Table1)(mgr)

                Assert.AreEqual(2, c.Count)
                Assert.AreEqual(30, GetList(c)(0).Code)
                Assert.AreEqual(45, GetList(c)(1).Code)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestCustomFilter()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim c As ICollection(Of Table1) = New QueryCmd().Where( _
                Ctor.custom("power({0},2)", ECtor.prop(GetType(Table1), "Code")).greater_than(1000)) _
                .OrderBy(SCtor.prop(GetType(Table1), "Code")).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, c.Count)

            Assert.AreEqual(3, GetList(c)(0).Identifier)
            Assert.AreEqual(2, GetList(c)(1).Identifier)

            c = New QueryCmd().SelectEntity(GetType(Table1), True).Where(CType(Ctor.prop(GetType(Table1), "Enum").eq(2), PredicateLink). _
                [and]("power({0},2)", ECtor.prop(GetType(Table1), "Code")).greater_than(1000)).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSaveNew()
        'OrmReadOnlyDBManager.StmtSource.Listeners.Add(New Diagnostics.DefaultTraceListener)
        OrmReadOnlyDBManager.StmtSource.Listeners(0).TraceOutputOptions = TraceOptions.None
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t1 As Table1 = New Table1(-345, mgr.Cache, mgr.MappingEngine)
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            t2.Tbl = t1

            Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
            mgr.BeginTransaction()
            Try
                Dim b As Boolean = t2.SaveChanges(True)
                Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
                Assert.IsTrue(b)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSaveNewSmart()
        'OrmReadOnlyDBManager.StmtSource.Listeners.Add(New Diagnostics.DefaultTraceListener)
        OrmReadOnlyDBManager.StmtSource.Listeners(0).TraceOutputOptions = TraceOptions.None
        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            Dim t1 As Table1 = New Table1(-345, mgr.Cache, mgr.MappingEngine)
            t1.CreatedAt = Now
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            t2.Tbl = t1

            Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
            mgr.BeginTransaction()
            Try
                Dim b As Boolean = t2.SaveChanges(True)
                Assert.AreEqual(ObjectState.Created, t1.InternalProperties.ObjectState)
                Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
                Assert.IsTrue(b)

                Using st As New ModificationsTracker(mgr)
                    st.Add(t2)
                    st.Add(t1)
                    st.AcceptModifications()
                End Using

                Assert.AreEqual(ObjectState.None, t2.InternalProperties.ObjectState)
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestInh()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim l As Table1_x = New QueryCmd().GetByID(Of Table1_x)(1, mgr)
            l.Load()

            Dim r As Worm.ReadOnlyList(Of Table1_x) = New QueryCmd() _
                .Distinct(True) _
                .Where(Ctor.prop(GetType(Table1_x), "ID").eq(1)) _
                .OrderBy(SCtor.prop(GetType(Table1_x), "Title")) _
                .SelectEntity(GetType(Table1_x), True) _
                .ToOrmList(Of Table1_x)(mgr)
            Assert.AreEqual(1, r.Count)

        End Using
    End Sub

    <TestMethod()> Public Sub TestRawObject()
        Dim t As Table1 = CType(Activator.CreateInstance(GetType(Table1)), Table1)

        t.Name = "kasdfn"
        t.CreatedAt = Now
        t.Identifier = -100

        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)

        Using mgr As OrmReadOnlyDBManager = CreateWriteManager(GetSchema("1"))
            mgr.BeginTransaction()
            Try
                Using st As New ModificationsTracker(mgr)
                    st.Add(t)

                    st.AcceptModifications()
                End Using

                Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestRawCommand()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim l As New List(Of Table1)
            Using cmd As New System.Data.SqlClient.SqlCommand("select id, code from table1 where id = 1")
                Dim s As List(Of SelectExpression) = FCtor.column(Nothing, "id").into("ID", Field2DbRelations.PK).column(Nothing, "code").into("Code").GetExpressions.ConvertAll(Function(e) CType(e, SelectExpression))

                Dim schema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(GetType(Table1))

                mgr.QueryObjects(Of Table1)(cmd, l, s, _
                    schema, schema.FieldColumnMap)

                Assert.AreEqual(1, l.Count)
                Assert.AreEqual(1, l(0).ID)
            End Using
        End Using
    End Sub

    <TestMethod()> Public Sub TestExecuteScalar()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Using cmd As New System.Data.SqlClient.SqlCommand("select max(id) from table1")
                Dim obj As Object = mgr.ExecuteScalar(cmd)
                Assert.AreEqual(3, obj)
            End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadNonCached()
        Using mgr As OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table1 = New QueryCmd().GetByID(Of Table1)(1, GetByIDOptions.EnsureExistsInStore, mgr)
            Assert.IsNotNull(t)
            Assert.IsTrue(t.InternalProperties.IsLoaded)
            Assert.IsTrue(mgr.IsInCachePrecise(t))

            Dim t2 As New Table1(1, mgr.Cache, mgr.MappingEngine)
            Assert.IsFalse(t2.InternalProperties.IsLoaded)
            Assert.IsFalse(mgr.IsInCachePrecise(t2))

            t2.Load()

            Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Assert.IsFalse(mgr.IsInCachePrecise(t2))
        End Using

    End Sub

    Private Function GetList(Of T As {SinglePKEntity})(ByVal col As IEnumerable(Of T)) As IList(Of T)
        Return CType(col, Global.System.Collections.Generic.IList(Of T))
    End Function

    Private _id As Integer = -100
    Private _new_objects As New Dictionary(Of Integer, SinglePKEntity)

    Public Sub AddNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.AddNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Add(CInt(CType(obj, ISinglePKEntity).Identifier), CType(obj, SinglePKEntity))
    End Sub

    Public Function GetIdentity(ByVal type As Type, ByVal mpe As Worm.ObjectMappingEngine) As PKDesc() Implements INewObjectsStore.GetPKForNewObject
        Dim i As Integer = _id
        _id += -1
        Return New PKDesc() {New PKDesc("id", _id)}
    End Function

    Public Function GetIdentity() As Integer
        Return CInt(GetIdentity(Nothing, Nothing)(0).Value)
    End Function

    Public Function GetNew(ByVal t As System.Type, ByVal id As IEnumerable(Of Meta.PKDesc)) As _ICachedEntity Implements INewObjectsStore.GetNew
        Dim o As SinglePKEntity = Nothing
        _new_objects.TryGetValue(CInt(id(0).Value), o)
        Return o
    End Function

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As IEnumerable(Of Meta.PKDesc)) Implements INewObjectsStore.RemoveNew
        _new_objects.Remove(CInt(id(0).Value))
    End Sub

    Public Sub RemoveNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.RemoveNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Remove(CInt(CType(obj, ISinglePKEntity).Identifier))
    End Sub

    Public Event CreateManagerEvent(sender As Worm.ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements Worm.ICreateManager.CreateManagerEvent
End Class
