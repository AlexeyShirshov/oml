Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics

<TestClass()> _
Public Class TestManagerRS

    Private _schemas As New Collections.Hashtable
    Public Function GetSchema(ByVal v As String) As Orm.DbSchema
        Dim s As Orm.DbSchema = CType(_schemas(v), Orm.DbSchema)
        If s Is Nothing Then
            s = New Orm.DbSchema(v)
            _schemas.Add(v, s)
        End If
        Return s
    End Function

    Private _cache As Orm.OrmCache
    Protected Function GetCache() As Orm.OrmCache
        If _c Then
            If _cache Is Nothing Then
                _cache = New Orm.OrmCache
            End If
            Return _cache
        Else
            Return New Orm.OrmCache
        End If
    End Function

    Public Shared Function CreateManagerSharedFullText(ByVal schema As Orm.DbSchema) As Orm.OrmReadOnlyDBManager
        Return New Orm.OrmDBManager(New Orm.OrmCache, schema, My.Settings.FullTextEnabledConn)
    End Function

    Public Shared Function CreateManagerShared(ByVal schema As Orm.DbSchema) As Orm.OrmReadOnlyDBManager
        Return New Orm.OrmDBManager(New Orm.OrmCache, schema, "Data Source=.\sqlexpress;AttachDBFileName='" & My.Settings.WormRoot & "\TestProject1\Databases\wormtest.mdf';User Instance=true;Integrated security=true;")
    End Function

    Public Function CreateManager(ByVal schema As Orm.DbSchema) As Orm.OrmReadOnlyDBManager
        Return New Orm.OrmDBManager(GetCache, schema, "Data Source=.\sqlexpress;AttachDBFileName='" & My.Settings.WormRoot & "\TestProject1\Databases\wormtest.mdf';User Instance=true;Integrated security=true;")
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
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t2 As Table2 = mgr.Find(Of Table2)(1)

            t2.Tbl = mgr.Find(Of Table1)(2)

            mgr.BeginTransaction()
            Try
                t2.Save(True)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Orm.OrmManagerException))> _
    Public Sub TestSaveConcurrency()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table3 = mgr.Find(Of Table3)(1)
            Assert.IsNotNull(t)
            t.Code = t.Code + CByte(10)

            Dim t2 As Table3 = Nothing
            Dim prev As Byte = 0
            Try
                Using mgr2 As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
                    t2 = mgr2.Find(Of Table3)(1)
                    prev = t2.Code
                    t2.Code = t.Code + CByte(10)
                    mgr2.SaveAll(t2, True)
                End Using

                mgr.SaveAll(t, True)
            Finally
                Using mgr2 As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
                    t2.Code = prev
                    mgr2.SaveAll(t2, True)
                End Using
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            Dim tt As IList(Of Table2) = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
            Assert.AreEqual(2, tt.Count)

            t2.Tbl = mgr.Find(Of Table1)(2)

            mgr.BeginTransaction()
            Try
                t2.Save(True)

                tt = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
                Assert.AreEqual(1, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache2()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t1 As Table1 = New Table1(1, mgr.Cache, mgr.ObjectSchema)
            Dim tt As IList(Of Table2) = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
            Assert.AreEqual(2, tt.Count)

            Dim t2 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)
            t2.Tbl = t1

            mgr.BeginTransaction()
            Try
                t2.Save(True)

                tt = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
                Assert.AreEqual(3, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestValidateCache3()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t1 As Table1 = New Table1(1, mgr.Cache, mgr.ObjectSchema)
            Dim tt As IList(Of Table2) = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
            Assert.AreEqual(2, tt.Count)

            Dim t2 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)
            t2.Tbl = New Table1(2, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                t2.Save(True)

                tt = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))
                Assert.AreEqual(2, tt.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFindField()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(New Orm.Criteria(GetType(Table1)).Field("Code").Eq(2), Nothing, WithLoad)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestXmlField()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table3 = mgr.Find(Of Table3)(2)

            Assert.AreEqual("root", t.Xml.DocumentElement.Name)
            Dim attr As Xml.XmlAttribute = t.Xml.CreateAttribute("first")
            attr.Value = "hi!"
            t.Xml.DocumentElement.Attributes.Append(attr)

            t.Save(True)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddBlob()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t2 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()

            Try
                t2.Save(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadGUID()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As Table4 = mgr.Find(Of Table4)(1)

            Assert.AreEqual(False, t.Col)

            Assert.AreEqual(New Guid("7c78c40a-fd96-44fe-861f-0f87b8d04bd5"), t.GUID)

            Dim cc As ICollection(Of Table4) = mgr.Find(Of Table4)(New Orm.Criteria(GetType(Table4)).Field("Col").Eq(False), Nothing, True)

            Assert.AreEqual(1, cc.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddWithPK()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t As New Table4(4, mgr.Cache, mgr.ObjectSchema)
            Dim g As Guid = t.GUID
            mgr.BeginTransaction()

            Try
                t.Save(True)

                Assert.AreNotEqual(g, t.GUID)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAddWithPK2()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("2"))
            Dim t As New Table4(4, mgr.Cache, mgr.ObjectSchema)


            mgr.BeginTransaction()

            Try
                t.Save(True)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSwitchCache()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Using New Orm.OrmManagerBase.CacheListSwitcher(mgr, False)
                Dim c2 As ICollection(Of Table1) = mgr.Find(Of Table1)(New Orm.Criteria(GetType(Table1)).Field("Code").Eq(2), Nothing, WithLoad)

                Assert.AreEqual(1, c2.Count)
            End Using

            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(New Orm.Criteria(GetType(Table1)).Field("Code").Eq(2), Nothing, WithLoad)

            Assert.AreEqual(1, c.Count)

            c = mgr.Find(Of Table1)(New Orm.Criteria(GetType(Table1)).Field("Code").Eq(2), Nothing, WithLoad)

            Assert.AreEqual(1, c.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSwitchCache2()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim n As Date = Now
            For i As Integer = 0 To 100000
                Dim need As Boolean = Now.Subtract(n).TotalSeconds < 1
                If Not need Then
                    n = Now
                End If

                Using New Orm.OrmManagerBase.CacheListSwitcher(mgr, need)
                    Dim c2 As ICollection(Of Table1) = mgr.Find(Of Table1)(New Orm.Criteria(GetType(Table1)).Field("Code").Eq(2), Nothing, WithLoad)

                    Assert.AreEqual(1, c2.Count)
                End Using
            Next

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDeleteFromCache()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)

            Dim t3 As ICollection(Of Table2) = mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad)
            mgr.LoadObjects(t3)
            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsTrue(t2.IsLoaded)
            Next

            mgr.BeginTransaction()
            Try

                mgr.RemoveObjectFromCache(t1)

                t3 = mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, WithLoad)

                Assert.AreEqual(2, t3.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t3 As Table33 = mgr.Find(Of Table33)(1)
            Dim c As ICollection(Of Table33) = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)

            Assert.AreEqual(2, c.Count)

            Dim c2 As ICollection(Of Table1) = t3.Find(Of Table1)(Nothing, Orm.Sorting.Field("Enum").Asc, WithLoad)

            Assert.AreEqual(1, c2.Count)

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.ObjectSchema)
            r1.Title = "913nv"
            r1.Table1 = mgr.Find(Of Table1)(2)
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.Save(True)

                c = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)

                Assert.AreEqual(2, c.Count)

                c2 = t3.Find(Of Table1)(Nothing, Orm.Sorting.Field("Enum").Asc, WithLoad)

                Assert.AreEqual(2, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M2()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t3 As Table33 = mgr.Find(Of Table33)(1)
            Dim c As ICollection(Of Table33) = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)

            Assert.AreEqual(2, c.Count)

            Dim c2 As ICollection(Of Table1) = t3.Find(Of Table1)(Nothing, Nothing, WithLoad)

            Assert.AreEqual(1, c2.Count)

            Dim r1 As Tables1to3 = mgr.Find(Of Tables1to3)(1)
            r1.Delete()
            mgr.BeginTransaction()
            Try
                r1.Save(True)

                c = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)

                Assert.AreEqual(1, c.Count)

                c2 = t3.Find(Of Table1)(Nothing, Nothing, WithLoad)

                Assert.AreEqual(0, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M3()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t3 As Table33 = mgr.Find(Of Table33)(2)
            Dim t As Type = schema.GetTypeByEntityName("Table3")
            Dim f As Orm.CriteriaLink = New Orm.Criteria(t).Field("Code").Eq(2)
            Dim c As ICollection(Of Table33) = t1.Find(Of Table33)(f, _
                Nothing, WithLoad)

            Assert.AreEqual(1, c.Count)

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.ObjectSchema)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.Save(True)

                Dim c2 As ICollection(Of Table33) = t1.Find(Of Table33)(f, Nothing, WithLoad)

                Assert.AreEqual(2, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    Public Sub TestComplexM2M4()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t3 As Table33 = mgr.Find(Of Table33)(1)
            Dim c As ICollection(Of Table33) = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)

            Assert.AreEqual(2, c.Count)

            t1.Add(t3)

            mgr.BeginTransaction()
            Try
                t1.Save(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestComplexM2M5()
        Dim schema As Orm.DbSchema = GetSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t3 As Table33 = mgr.Find(Of Table33)(1)
            Dim c As ICollection(Of Table33) = t1.Find(Of Table33)(Nothing, Nothing, True)

            Assert.AreEqual(2, c.Count)

            For Each o As Table33 In c
                Assert.IsTrue(o.IsLoaded)
            Next

            Dim r1 As New Tables1to3(-100, mgr.Cache, mgr.ObjectSchema)
            r1.Title = "913nv"
            r1.Table1 = t1
            r1.Table3 = t3
            mgr.BeginTransaction()
            Try
                r1.Save(False)

                Assert.AreNotEqual(-100, r1.Identifier)

                Dim c2 As ICollection(Of Table33) = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)
                Assert.AreEqual(3, c2.Count)

                r1.RejectChanges()

                c2 = t1.Find(Of Table33)(Nothing, Nothing, WithLoad)
                Assert.AreEqual(2, c2.Count)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)
            Dim tt2 As Table1 = mgr.Find(Of Table1)(2)

            Dim t1s As ICollection(Of Table1) = mgr.ConvertIds2Objects(Of Table1)(New Integer() {1, 2}, False)
            Dim t10s As ICollection(Of Table10) = mgr.LoadObjects(Of Table10)("Table1", Nothing, CType(t1s, Collections.ICollection))

            Assert.AreEqual(3, t10s.Count)

            Dim t1 As ICollection(Of Table10) = mgr.Find(Of Table10)(New Orm.Criteria(GetType(Table10)).Field("Table1").Eq(tt1), Nothing, WithLoad)
            Assert.AreEqual(2, t1.Count)

            Dim t2 As ICollection(Of Table10) = mgr.Find(Of Table10)(New Orm.Criteria(GetType(Table10)).Field("Table1").Eq(tt2), Nothing, WithLoad)
            Assert.AreEqual(1, t2.Count)

            t10s = mgr.LoadObjects(Of Table10)("Table1", Nothing, CType(t1s, Collections.ICollection))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects2()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)
            Dim tt2 As Table1 = mgr.Find(Of Table1)(2)

            Dim t1 As ICollection(Of Table10) = mgr.Find(Of Table10)(New Orm.Criteria(GetType(Table10)).Field("Table1").Eq(tt1), Nothing, WithLoad)
            Assert.AreEqual(2, t1.Count)

            Dim t1s As ICollection(Of Table1) = mgr.ConvertIds2Objects(Of Table1)(New Integer() {1, 2}, False)
            Dim t10s As ICollection(Of Table10) = mgr.LoadObjects(Of Table10)("Table1", Nothing, CType(t1s, Collections.ICollection))
            Assert.AreEqual(3, t10s.Count)

            Dim t2 As ICollection(Of Table10) = mgr.Find(Of Table10)(New Orm.Criteria(GetType(Table10)).Field("Table1").Eq(tt2), Nothing, WithLoad)
            Assert.AreEqual(1, t2.Count)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects3()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)
            Dim tt2 As Table1 = mgr.Find(Of Table1)(2)

            Dim t1s As ICollection(Of Table1) = mgr.ConvertIds2Objects(Of Table1)(New Integer() {1, 2}, False)
            Dim t10s As ICollection(Of Table10) = mgr.LoadObjects(Of Table10)("Table1", New Orm.Criteria(GetType(Table10)).Field("ID").Eq(1), CType(t1s, Collections.ICollection))
            Assert.AreEqual(1, t10s.Count)

        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects4()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.CreateObject(Of Table1)(1)
            Dim tt2 As Table1 = mgr.CreateObject(Of Table1)(1)

            mgr.LoadObjects(New List(Of Table1)(New Table1() {tt1, tt2}))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects5()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table2 = mgr.CreateObject(Of Table2)(1)

            Dim t As ICollection(Of Table2) = mgr.LoadObjects(Of Table2)(New Table2() {tt1}, New String() {"Table1"}, 0, 1)

            Assert.AreEqual(1, t.Count)

            Assert.AreEqual(1, CType(t, IList(Of Table2))(0).Tbl.Identifier)
            Assert.IsTrue(CType(t, IList(Of Table2))(0).Tbl.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjectsM2M()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim t1s As ICollection(Of Table1) = mgr.ConvertIds2Objects(Of Table1)(New Integer() {1, 2}, False)

            mgr.LoadObjects(Of Table33)(mgr.ObjectSchema.GetM2MRelation(GetType(Table1), GetType(Table33), True), Nothing, CType(t1s, Collections.ICollection), Nothing)

            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)

            tt1.Find(Of Table33)(Nothing, Nothing, False)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MFilterValidation()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t As Type = mgr.ObjectSchema.GetTypeByEntityName("Table3")
            'Dim con As New Orm.OrmCondition.OrmConditionConstructor
            'con.AddFilter(New Orm.OrmFilter(t, "Code", New TypeWrap(Of Object)(2), Orm.FilterOperation.Equal))
            Dim c As ICollection(Of Table33) = tt1.Find(Of Table33)(New Orm.Criteria(t).Field("Code").Eq(2), Nothing, WithLoad)

            Assert.AreEqual(1, c.Count)
            mgr.BeginTransaction()
            Try
                Dim tt2 As Table33 = mgr.Find(Of Table33)(1)
                Assert.AreEqual(Of Byte)(1, tt2.Code)
                tt2.Code = 2
                tt2.Save(True)

                c = tt1.Find(Of Table33)(New Orm.Criteria(t).Field("Code").Eq(2), Nothing, WithLoad)

                Assert.AreEqual(2, c.Count)
            Finally
                mgr.Rollback()

            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestM2MSoring()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim tt1 As Table1 = mgr.Find(Of Table1)(1)
            Dim t As Type = mgr.ObjectSchema.GetTypeByEntityName("Table3")
            'Dim con As New Orm.OrmCondition.OrmConditionConstructor
            'con.AddFilter(New Orm.OrmFilter(t, "Code", New TypeWrap(Of Object)(2), Orm.FilterOperation.Equal))
            Dim s As Orm.Sort = Orm.Sorting.Field("Code").Desc
            Dim c As ICollection(Of Table33) = tt1.Find(Of Table33)(Nothing, s, WithLoad)
            Assert.AreEqual(2, c.Count)
            Assert.AreEqual(Of Byte)(2, CType(c, List(Of Table33))(0).Code)
            Assert.AreEqual(Of Byte)(1, CType(c, List(Of Table33))(1).Code)

            mgr.BeginTransaction()
            Try
                Using st As New Orm.OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim tt2 As Table33 = New Table33(-100, mgr.Cache, mgr.ObjectSchema)
                    st.Add(tt2)
                    tt2.RefObject = tt1
                    tt2.Code = 3

                    Dim t3 As New Tables1to3(-101, mgr.Cache, mgr.ObjectSchema)
                    st.Add(t3)

                    t3.Table1 = tt1
                    t3.Table3 = tt2
                    t3.Title = "sdfpsdfm"
                    st.Commit()
                End Using

                c = tt1.Find(Of Table33)(Nothing, s, WithLoad)
                Assert.AreEqual(3, c.Count)
                Assert.AreEqual(Of Byte)(3, CType(c, List(Of Table33))(0).Code)
                Assert.AreEqual(Of Byte)(2, CType(c, List(Of Table33))(1).Code)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestFuncs()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("2"))
            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Assert.IsNotNull(t1)
        End Using

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("3"))
            Dim t1 As Table1 = mgr.Find(Of Table1)(2)
            Assert.IsNotNull(t1)

            t1 = mgr.Find(Of Table1)(1)
            Assert.IsNull(t1)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestMultipleDelete()
        Using mgr As Orm.OrmDBManager = CType(CreateManager(GetSchema("1")), Orm.OrmDBManager)
            Dim f As New Orm.OrmFilter(GetType(Table3), "Code", New TypeWrap(Of Object)(1), Orm.FilterOperation.LessEqualThan)
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
        Using mgr As Orm.OrmDBManager = CType(CreateManager(GetSchema("1")), Orm.OrmDBManager)
            Dim t As Table1 = mgr.CreateDBObject(Of Table1)(1)
            Assert.AreEqual(Orm.ObjectState.NotLoaded, t.ObjectState)
            t.Delete()
            Assert.AreEqual(Orm.ObjectState.Deleted, t.ObjectState)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestSimpleObjects()
        Using mgr As Orm.OrmDBManager = CType(CreateManager(GetSchema("1")), Orm.OrmDBManager)
            Dim s1 As SimpleObj = mgr.Find(Of SimpleObj)(1)

            Assert.AreEqual("first", s1.Title)

            mgr.BeginTransaction()
            Try
                s1.Delete()
                s1.Save(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestSimpleObjects2()
        Using mgr As Orm.OrmDBManager = CType(CreateManager(GetSchema("1")), Orm.OrmDBManager)
            Dim s1 As SimpleObj2 = mgr.Find(Of SimpleObj2)(2)

            Assert.AreEqual("second", s1.Title)

            mgr.BeginTransaction()
            Try
                s1 = New SimpleObj2
                s1.Title = "555"
                s1.Save(True)
            Finally
                Assert.IsTrue(s1.Identifier > 0)
                Assert.AreEqual("555", s1.Title)
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestPager()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim cc As ICollection(Of Table1) = mgr.FindTop(Of Table1)(10, Nothing, Nothing, True)
            Assert.AreEqual(3, cc.Count)

            Using New Orm.OrmManagerBase.PagerSwitcher(mgr, 0, 1)
                cc = mgr.FindTop(Of Table1)(10, Nothing, Nothing, True)
                Assert.AreEqual(1, cc.Count)
            End Using
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExecResults()
        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(GetSchema("1"))
            Dim cc As ICollection(Of Table1) = mgr.FindTop(Of Table1)(10, Nothing, Nothing, True)

            Assert.AreEqual(3, mgr.GetLastExecitionResult.Count)
            Assert.IsFalse(mgr.GetLastExecitionResult.CacheHit)

            System.Diagnostics.Trace.WriteLine(mgr.GetLastExecitionResult.ExecutionTime.ToString)
            System.Diagnostics.Trace.WriteLine(mgr.GetLastExecitionResult.FetchTime.ToString)

            Dim t As Table1 = mgr.Find(Of Table1)(1)
            t.Load()

            Assert.AreEqual(1, mgr.Cache.GetLoadTime(GetType(Table1)).First)

            System.Diagnostics.Trace.WriteLine(mgr.Cache.GetLoadTime(GetType(Table1)).Second.ToString)
        End Using
    End Sub
End Class
