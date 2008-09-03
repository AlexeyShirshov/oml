Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Orm
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Sorting
Imports Worm.Criteria.Core

<TestClass()> Public Class TestComplexEntities

    <TestMethod()> _
    Public Sub TestGuid()
        Using mgr As OrmManagerBase = TestManager.CreateManager(New SQLGenerator("1"))
            Dim o As GuidPK = mgr.Find(Of GuidPK)(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"))

            Assert.IsNotNull(o)

            Assert.IsTrue(o.IsLoaded)

            Assert.AreEqual(New Guid("127ed64d-c7b9-448b-ab67-390808e636ee"), o.Guid)

            Assert.AreEqual(4, o.Code)
        End Using
    End Sub

End Class