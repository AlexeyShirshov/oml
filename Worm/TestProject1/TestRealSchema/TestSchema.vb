Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics

<TestClass()> _
Public Class TestSchema

    Public Shared Function CreateManager(ByVal schema As Orm.DbSchema) As Orm.OrmReadOnlyDBManager
        Return New Orm.OrmReadOnlyDBManager(New Orm.OrmCache, schema, "Data Source=vs2\sqlmain;Integrated Security=true;Initial Catalog=WormTest;")
    End Function

    <TestMethod()> _
    Public Sub TestEnum()
        Assert.AreEqual(Enum1.first, CType(CByte(1), Enum1))

        'Assert.AreEqual(Enum1.first, Convert.ChangeType(CByte(1), GetType(Enum1)))

        Assert.AreEqual(Enum1.first, [Enum].ToObject(GetType(Enum1), CByte(1)))
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable1()

        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)

            Assert.IsTrue(t1.IsLoaded)

            Assert.AreEqual("first", t1.Name)
            Assert.AreEqual(Enum1.first, t1.EnumStr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable2()

        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)

            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            Assert.IsTrue(t2.IsLoaded)
            Assert.IsFalse(t2.Tbl.IsLoaded)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Assert.IsFalse(t1.IsLoaded)
            Assert.AreEqual(t1, t2.Tbl)

            Dim t3 As ICollection(Of Table2) = Nothing
            'Try
            t3 = mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(t1), Nothing, False)
            '    Assert.Fail()
            'Catch ex As ArgumentException
            'Catch
            '    Assert.Fail()
            'End Try

            't3 = mgr.FindOrm(Of Table2)(t1, "Table1", Nothing, False)

            Assert.AreEqual(2, t3.Count)

            Assert.IsTrue(CType(t3, IList(Of Table2)).IndexOf(t2) >= 0)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable3()

        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)

            Dim t3 As ICollection(Of Table2) = mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Nothing, True)

            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsTrue(t2.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable4()

        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)

            Dim t3 As ICollection(Of Table2) = mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Nothing, False)

            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsFalse(t2.IsLoaded)
            Next

            For Each t2 As Table2 In mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Nothing, True)
                Assert.IsTrue(t2.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects()
        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim tt() As Table1 = New Table1() {New Table1(10, mgr.Cache, mgr.ObjectSchema), New Table1(11, mgr.Cache, mgr.ObjectSchema), New Table1(15, mgr.Cache, mgr.ObjectSchema)}

            mgr.LoadObjects(tt)

            Dim col As ICollection(Of Table1) = mgr.LoadObjectsIds(Of Table1)(New Integer() {1, 2, 10, 11, 34, 45, 20})

            Assert.AreEqual(2, col.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects2()
        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim l As New List(Of Integer)
            l.Add(1)
            Dim rnd As New Random
            For i As Integer = 293485 To 293485 + 1000
                Dim j As Integer = rnd.Next(i)
                If Not l.Contains(j) Then l.Add(j)
            Next

            Dim uol As ICollection(Of Table1) = mgr.LoadObjectsIds(Of Table1)(l.ToArray)
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentException))> _
    Public Sub TestOrderWrong()
        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table2) = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Orm.Sorting.Field("DT").Asc, True), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.AreEqual(4, t2(1).Identifier)

            t2 = CType(mgr.Find(Of Table2)(New Orm.Criteria(GetType(Table2)).Field("Table1").Eq(New Table1(1, mgr.Cache, mgr.ObjectSchema)), Orm.Sorting.Field("DTs").Desc, True), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))

            Assert.AreEqual(4, t2(0).Identifier)
            Assert.AreEqual(1, t2(1).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder()
        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(10, Nothing, Orm.Sorting.Field("DT").Asc, False), IList(Of Table1))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsFalse(t2(0).IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsFalse(t2(1).IsLoaded)

            t2 = CType(mgr.FindTop(Of Table1)(10, Nothing, Orm.Sorting.Field("DT").Desc, False), IList(Of Table1))

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.IsTrue(t2(0).IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsTrue(t2(1).IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder2()
        Dim schema As New Orm.DbSchema("1")

        Using mgr As Orm.OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(10, Nothing, Orm.Sorting.Field("DT").Asc, True), IList(Of Table1))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsTrue(t2(0).IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsTrue(t2(1).IsLoaded)
        End Using
    End Sub
End Class
