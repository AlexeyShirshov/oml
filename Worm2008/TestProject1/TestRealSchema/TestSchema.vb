Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Criteria
Imports Worm.Query

<TestClass()> _
Public Class TestSchema

    Public Shared Function CreateManager(ByVal schema As Worm.ObjectMappingEngine) As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\wormtest.mdf"))
        Return New OrmReadOnlyDBManager(New OrmCache, schema, New SQLGenerator, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        return New OrmDBManager(new ormCache, schema, New SQLGenerator, "Server=.\sqlexpress;Integrated security=true;Initial catalog=wormtest")
#End If
    End Function

    <TestMethod()> _
    Public Sub TestEnum()
        Assert.AreEqual(Enum1.first, CType(CByte(1), Enum1))

        'Assert.AreEqual(Enum1.first, Convert.ChangeType(CByte(1), GetType(Enum1)))

        Assert.AreEqual(Enum1.first, [Enum].ToObject(GetType(Enum1), CByte(1)))
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable1()

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)

            Assert.IsTrue(t1.InternalProperties.IsLoaded)

            Assert.AreEqual("first", t1.Name)
            Assert.AreEqual(Enum1.first, t1.EnumStr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable2()

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Assert.IsFalse(t2.Tbl.InternalProperties.IsLoaded)

            Dim t1 As Table1 = mgr.Find(Of Table1)(1)
            Assert.IsFalse(t1.InternalProperties.IsLoaded)
            Assert.AreEqual(t1, t2.Tbl)

            Dim t3 As ICollection(Of Table2) = Nothing
            'Try
            t3 = mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(t1), Nothing, False)
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

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t3 As ICollection(Of Table2) = mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine)), Nothing, True)

            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable4()

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t3 As ICollection(Of Table2) = mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine)), Nothing, False)

            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsFalse(t2.InternalProperties.IsLoaded)
            Next

            For Each t2 As Table2 In mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine)), Nothing, True)
                Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim tt() As Table1 = New Table1() {New Table1(10, mgr.Cache, mgr.MappingEngine), New Table1(11, mgr.Cache, mgr.MappingEngine), New Table1(15, mgr.Cache, mgr.MappingEngine)}
            Dim col As New Worm.ReadOnlyList(Of Table1)(New List(Of Table1)(tt))
            mgr.LoadObjects(col)

            col = mgr.LoadObjectsIds(Of Table1)(New Object() {1, 2, 10, 11, 34, 45, 20})

            Assert.AreEqual(2, col.Count)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjects2()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim l As New List(Of Object)
            l.Add(1)
            Dim rnd As New Random
            For i As Integer = 293485 To 293485 + 1000
                Dim j As Integer = rnd.Next(i)
                If Not l.Contains(j) Then l.Add(j)
            Next

            Dim uol As ICollection(Of Table1) = mgr.LoadObjectsIds(Of Table1)(l.ToArray)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjectsOnlyID()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim tt() As Table1 = New Table1() {New Table1(10, mgr.Cache, mgr.MappingEngine), New Table1(11, mgr.Cache, mgr.MappingEngine), New Table1(15, mgr.Cache, mgr.MappingEngine)}
            Dim col As New Worm.ReadOnlyList(Of Table1)(New List(Of Table1)(tt))
            col = mgr.LoadObjects(col, 0, col.Count, New List(Of Meta.EntityPropertyAttribute)(New Meta.EntityPropertyAttribute() {New Meta.EntityPropertyAttribute("ID")}))
            Assert.AreEqual(0, col.Count)

            col = mgr.ConvertIds2Objects(Of Table1)(New Object() {1, 2, 10, 11, 34, 45, 20}, True)

            Assert.AreEqual(2, col.Count)

            For Each c As Table1 In col
                Assert.IsFalse(c.InternalProperties.IsLoaded)
            Next
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(ArgumentException))> _
    Public Sub TestOrderWrong()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table2) = CType(mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine)), SCtor.prop(GetType(Table1), "DT").asc, True), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.AreEqual(4, t2(1).Identifier)

            t2 = CType(mgr.Find(Of Table2)(New PCtor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine)), SCtor.prop(GetType(Table2), "DTs").desc, True), Global.System.Collections.Generic.IList(Of Global.TestProject1.Table2))

            Assert.AreEqual(4, t2(0).Identifier)
            Assert.AreEqual(1, t2(1).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(10, Nothing, SCtor.prop(GetType(Table1), "DT").asc, False), IList(Of Table1))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsFalse(t2(0).InternalProperties.IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsFalse(t2(1).InternalProperties.IsLoaded)

            t2 = CType(mgr.FindTop(Of Table1)(10, Nothing, SCtor.prop(GetType(Table1), "DT").desc, False), IList(Of Table1))

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.IsFalse(t2(0).InternalProperties.IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsFalse(t2(1).InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder2()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(10, Nothing, SCtor.prop(GetType(Table1), "DT").asc, True), IList(Of Table1))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsTrue(t2(0).InternalProperties.IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsTrue(t2(1).InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder3()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(2, Nothing, SCtor.prop(GetType(Table1), "DT").asc, True), IList(Of Table1))
            Dim t1 As IList(Of Table1) = CType(mgr.FindTop(Of Table1)(2, Nothing, SCtor.prop(GetType(Table1), "Title").asc, True), IList(Of Table1))

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsTrue(t2(0).InternalProperties.IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsTrue(t2(1).InternalProperties.IsLoaded)


            Assert.AreEqual(1, t1(0).Identifier)
            Assert.IsTrue(t1(0).InternalProperties.IsLoaded)
            Assert.AreEqual(3, t1(1).Identifier)
            Assert.IsTrue(t1(1).InternalProperties.IsLoaded)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrderComposite()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As IList(Of Table1) = CType( _
                mgr.Find(Of Table1)( _
                    PCtor.prop(GetType(Table1), "EnumStr").eq("sec"), _
                    SCtor.prop(GetType(Table1), "EnumStr").asc, False), IList(Of Table1))

            Assert.AreEqual(2, t1(0).Identifier)
            Assert.AreEqual(3, t1(1).Identifier)

            Dim t2 As IList(Of Table1) = CType( _
                mgr.Find(Of Table1)( _
                    PCtor.prop(GetType(Table1), "EnumStr").eq("sec"), _
                    SCtor.prop(GetType(Table1), "EnumStr").next_prop("Enum").desc, False), IList(Of Table1))

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.AreEqual(2, t2(1).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrderCustom()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As IList(Of Table1) = CType( _
                mgr.Find(Of Table1)( _
                    PCtor.prop(GetType(Table1), "EnumStr").eq("sec"), _
                    SCtor.Custom("{0} asc", New FieldReference(GetType(Table1), "EnumStr")), False), IList(Of Table1))

            Assert.AreEqual(2, t1(0).Identifier)
            Assert.AreEqual(3, t1(1).Identifier)

            Dim t2 As IList(Of Table1) = CType( _
                mgr.Find(Of Table1)( _
                    PCtor.prop(GetType(Table1), "EnumStr").eq("sec"), _
                    SCtor.Custom("{0}", New FieldReference(GetType(Table1), "EnumStr")). _
                    NextCustom("{0} desc", New FieldReference(GetType(Table1), "Enum")), False), IList(Of Table1))

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.AreEqual(2, t2(1).Identifier)
        End Using
    End Sub
End Class
