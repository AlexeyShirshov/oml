Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Orm

<TestClass()> Public Class TestListManager

    <TestMethod()> _
    Public Sub TestAddWithSort()
        Dim schema As SQLGenerator = New SQLGenerator("1")

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(schema)
            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Table1) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

            Assert.AreEqual(2, l(0).Identifier)
            Assert.AreEqual(3, l(1).Identifier)

            mgr.BeginTransaction()
            Try
                Dim n As New Table1(-100, mgr.Cache, mgr.MappingEngine)
                n.EnumStr = Enum1.first
                n.CreatedAt = Now

                n.SaveChanges(True)

                c = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)
                Assert.AreEqual(2, c.Count)
                l = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

                Assert.AreEqual(2, l(0).Identifier)
                Assert.AreEqual(3, l(1).Identifier)

                n = New Table1(-100, mgr.Cache, mgr.MappingEngine)
                n.EnumStr = Enum1.sec
                n.CreatedAt = Now
                n.Enum = CType(3, Global.System.Nullable(Of Global.TestProject1.Enum1))

                n.SaveChanges(True)

                c = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)
                Assert.AreEqual(3, c.Count)
                l = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

                Assert.AreEqual(2, l(0).Identifier)
                Assert.AreEqual(n.Identifier, l(1).Identifier)
                Assert.AreEqual(3, l(2).Identifier)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete()
        Dim schema As SQLGenerator = New SQLGenerator("1")

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(schema)
            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)

            Assert.AreEqual(2, c.Count)

            Dim l As IList(Of Table1) = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

            Assert.AreEqual(2, l(0).Identifier)
            Assert.AreEqual(3, l(1).Identifier)

            mgr.BeginTransaction()
            Try
                Dim n As New Table1(-100, mgr.Cache, mgr.MappingEngine)
                n.EnumStr = Enum1.sec
                n.CreatedAt = Now
                n.Enum = CType(3, Global.System.Nullable(Of Global.TestProject1.Enum1))

                n.SaveChanges(True)

                c = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)
                Assert.AreEqual(3, c.Count)
                l = CType(c, Global.System.Collections.Generic.IList(Of Global.TestProject1.Table1))

                l(1).Delete()
                l(1).SaveChanges(True)

                c = mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec), Sorting.Field("Enum").Asc, True)
                Assert.AreEqual(2, c.Count)

            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
