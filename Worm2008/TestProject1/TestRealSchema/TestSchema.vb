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

            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)

            Assert.IsTrue(t1.InternalProperties.IsLoaded)

            Assert.AreEqual("first", t1.Name)
            Assert.AreEqual(Enum1.first, t1.EnumStr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadTable2()

        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)

            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Assert.IsTrue(t2.InternalProperties.IsLoaded)
            Assert.IsFalse(t2.Tbl.InternalProperties.IsLoaded)

            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Assert.IsFalse(t1.InternalProperties.IsLoaded)
            Assert.AreEqual(t1, t2.Tbl)

            Dim t3 As ICollection(Of Table2) = Nothing
            'Try
            t3 = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(t1)).ToList(Of Table2)(mgr)
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

            Dim t3 As ICollection(Of Table2) = New QueryCmd().SelectEntity(GetType(Table2), True).Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))).ToList(Of Table2)(mgr)

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

            Dim t3 As ICollection(Of Table2) = New QueryCmd().Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))).ToList(Of Table2)(mgr)

            Assert.AreEqual(2, t3.Count)

            For Each t2 As Table2 In t3
                Assert.IsFalse(t2.InternalProperties.IsLoaded)
            Next

            For Each t2 As Table2 In New QueryCmd().SelectEntity(GetType(Table2), True).Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))).ToList(Of Table2)(mgr)
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
            col.LoadObjects()

            col = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2, 10, 11, 34, 45, 20}, True, mgr)

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

            Dim uol As ICollection(Of Table1) = New QueryCmd().GetByIds(Of Table1)(l.ToArray, True, mgr)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestLoadObjectsOnlyID()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim tt() As Table1 = New Table1() {New Table1(10, mgr.Cache, mgr.MappingEngine), New Table1(11, mgr.Cache, mgr.MappingEngine), New Table1(15, mgr.Cache, mgr.MappingEngine)}
            Dim col As New Worm.ReadOnlyList(Of Table1)(New List(Of Table1)(tt))
            Dim ea As New Meta.EntityPropertyAttribute()
            ea.PropertyAlias = "ID"
            col = mgr.LoadObjects(col, 0, col.Count, New List(Of Meta.EntityPropertyAttribute)(New Meta.EntityPropertyAttribute() {ea}))
            Assert.AreEqual(0, col.Count)

            col = New QueryCmd().GetByIds(Of Table1)(New Object() {1, 2, 10, 11, 34, 45, 20}, True, mgr)

            Assert.AreEqual(2, col.Count)

            For Each c As Table1 In col
                Assert.IsFalse(c.InternalProperties.IsLoaded)
                If c.ID = 1 OrElse c.ID = 2 Then
                    Assert.AreEqual(ObjectState.NotLoaded, c.InternalProperties.ObjectState)
                Else
                    Assert.AreEqual(ObjectState.NotFoundInSource, c.InternalProperties.ObjectState)
                End If
            Next
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.ObjectMappingException))> _
    Public Sub TestOrderWrong()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim q As New QueryCmd()
            q.AutoJoins = True
            Dim t2 As IList(Of Table2) = q.SelectEntity(GetType(Table2), True).Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))) _
                .OrderBy(SCtor.prop(GetType(Table1), "DT").asc).ToList(Of Table2)(mgr)

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.AreEqual(4, t2(1).Identifier)

            t2 = New QueryCmd().SelectEntity(GetType(Table2), True).Where(New Ctor(GetType(Table2)).prop("Table1").eq(New Table1(1, mgr.Cache, mgr.MappingEngine))) _
                .OrderBy(SCtor.prop(GetType(Table2), "DTs").desc).ToList(Of Table2)(mgr)

            Assert.AreEqual(4, t2(0).Identifier)
            Assert.AreEqual(1, t2(1).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrder()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t2 As IList(Of Table1) = New QueryCmd().Top(10).OrderBy(SCtor.prop(GetType(Table1), "DT").asc).ToList(Of Table1)(mgr)

            Assert.AreEqual(1, t2(0).Identifier)
            Assert.IsFalse(t2(0).InternalProperties.IsLoaded)
            Assert.AreEqual(2, t2(1).Identifier)
            Assert.IsFalse(t2(1).InternalProperties.IsLoaded)

            t2 = New QueryCmd().Top(10).OrderBy(SCtor.prop(GetType(Table1), "DT").desc).ToList(Of Table1)(mgr)

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
            Dim t2 As IList(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), True).Top(10).OrderBy(SCtor.prop(GetType(Table1), "DT").asc).ToList(Of Table1)(mgr)

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
            Dim t2 As IList(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), True).Top(2).OrderBy(SCtor.prop(GetType(Table1), "DT").asc).ToList(Of Table1)(mgr)
            Dim t1 As IList(Of Table1) = New QueryCmd().SelectEntity(GetType(Table1), True).Top(2).OrderBy(SCtor.prop(GetType(Table1), "Title").asc).ToList(Of Table1)(mgr)

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
            Dim t1 As IList(Of Table1) = New QueryCmd().Where( _
                    Ctor.prop(GetType(Table1), "EnumStr").eq("sec")) _
                    .OrderBy(SCtor.prop(GetType(Table1), "EnumStr").asc).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, t1(0).Identifier)
            Assert.AreEqual(3, t1(1).Identifier)

            Dim t2 As IList(Of Table1) = New QueryCmd() _
                .Where(Ctor.prop(GetType(Table1), "EnumStr").eq("sec")) _
                .OrderBy(SCtor.prop(GetType(Table1), "EnumStr").prop(GetType(Table1), "Enum").desc) _
                .ToList(Of Table1)(mgr)

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.AreEqual(2, t2(1).Identifier)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestOrderCustom()
        Dim schema As New Worm.ObjectMappingEngine("1")

        Using mgr As OrmReadOnlyDBManager = CreateManager(schema)
            Dim t1 As IList(Of Table1) = New QueryCmd().Where( _
                    Ctor.prop(GetType(Table1), "EnumStr").eq("sec")) _
                    .OrderBy(SCtor.custom("{0} asc", FCtor.prop(GetType(Table1), "EnumStr"))).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, t1(0).Identifier)
            Assert.AreEqual(3, t1(1).Identifier)

            Dim t2 As IList(Of Table1) = New QueryCmd().Where( _
                    Ctor.prop(GetType(Table1), "EnumStr").eq("sec")) _
                    .OrderBy(SCtor.custom("{0}", FCtor.prop(GetType(Table1), "EnumStr")). _
                    custom("{0} desc", FCtor.prop(GetType(Table1), "Enum"))).ToList(Of Table1)(mgr)

            Assert.AreEqual(3, t2(0).Identifier)
            Assert.AreEqual(2, t2(1).Identifier)
        End Using
    End Sub
End Class
