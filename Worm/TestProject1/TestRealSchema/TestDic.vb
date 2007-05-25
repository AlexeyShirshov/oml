Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics

<TestClass()> _
Public Class TestDic

    <TestMethod()> _
    Public Sub TestStmt()

        Dim s As New Orm.DbSchema("1")
        Dim p As New Orm.ParamMgr(s, "p")
        Dim stmt As String = s.GetDictionarySelect(GetType(Table1), 1, p, Nothing, Nothing, Nothing)

        Assert.AreEqual("select left(t1.name,1) name,count(*) cnt from dbo.Table1 t1 group by left(t1.name,1) order by left(t1.name,1)", stmt)

    End Sub

    <TestMethod()> _
    Public Sub TestLike()

        Dim s As New Orm.DbSchema("1")
        Using mgr As Orm.OrmManagerBase = TestManagerRS.CreateManagerShared(s)
            Dim f As Orm.CriteriaLink = New Orm.Criteria(GetType(Table1)).Field("Title").Like("f%")
            Dim col As ICollection(Of Table1) = mgr.Find(Of Table1)(f, Nothing, False)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuild()

        Dim s As New Orm.DbSchema("1")
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As Orm.DicIndex(Of Table1) = mgr.BuildObjDictionary(Of Table1)(1, Nothing, Nothing)

            Assert.AreEqual(3, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr, Nothing, Orm.SortType.Asc)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildComplex()

        Dim s As New Orm.DbSchema("2")
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As Orm.DicIndex(Of Table1) = mgr.BuildObjDictionary(Of Table1)(1, Nothing, Nothing)

            Assert.AreEqual(6, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr, Nothing, Orm.SortType.Asc)

            Assert.AreEqual(3, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildComplex2()

        Dim s As New Orm.DbSchema("2")
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As Orm.DicIndex(Of Table1) = mgr.BuildObjDictionary(Of Table1)(1, Nothing, Nothing)

            Assert.AreEqual(6, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElementsLoadOnlyNames(mgr, Nothing, Orm.SortType.Asc)

            Assert.AreEqual(3, col.Count)
        End Using

    End Sub
End Class
