Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Orm
Imports System.Diagnostics
Imports Worm.Database

<TestClass()> _
Public Class TestExternalFilter

    <TestMethod()> _
    Public Sub Test1()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New DbSchema("1"))
            Dim c As ICollection(Of Table1) = mgr.Find(Of Table1)(Criteria.Ctor.AutoTypeField("ID").NotEq(100), Nothing, False)

            Assert.AreEqual(3, c.Count)

            Using New Worm.OrmManagerBase.ApplyCriteria(New Criteria.Ctor(GetType(Table1)).Field("EnumStr").Eq(Enum1.sec))
                c = mgr.Find(Of Table1)(Criteria.Ctor.AutoTypeField("ID").NotEq(100), Nothing, False)
                Assert.AreEqual(2, c.Count)
            End Using
        End Using
    End Sub
End Class
