Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Criteria
Imports Worm.Query

<TestClass()> _
Public Class TestExternalFilter

    <TestMethod()> _
    Public Sub Test1()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(Ctor.prop(GetType(Table1), "ID").not_eq(100), Nothing, False)

            Assert.AreEqual(3, c.Count)

            Using New Worm.OrmManager.ApplyCriteria(New Ctor(GetType(Table1)).prop("EnumStr").eq(Enum1.sec))
                c = mgr.Find(Of Table1)(Ctor.prop(GetType(Table1), "ID").not_eq(100), Nothing, False)
                Assert.AreEqual(2, c.Count)
            End Using
        End Using
    End Sub
End Class
