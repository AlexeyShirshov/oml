Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities
Imports Worm.Criteria
Imports Worm.Query
Imports Worm.Misc

<TestClass()> _
Public Class TestDic

    '<TestMethod()> _
    'Public Sub TestStmt()

    '    Dim s As New Worm.ObjectMappingEngine("1")
    '    Dim gen As New SQLGenerator
    '    Dim p As New ParamMgr(gen, "p")
    '    Dim stmt As String = gen.GetDictionarySelect(s, GetType(Table1), 1, p, Nothing, Nothing, Nothing)
    '    Dim checkedStmt As String = "select left(t1.name,1) name,count(*) cnt from dbo.Table1 t1 group by left(t1.name,1) order by left(t1.name,1)"
    '    Assert.AreEqual(checkedStmt, stmt)

    '    stmt = gen.GetDictionarySelect(s, GetType(Table1), 1, p, Nothing, Nothing, Nothing, "Title", Nothing)

    '    Assert.AreEqual(checkedStmt, stmt)
    'End Sub

    <TestMethod()> _
    Public Sub TestLike()

        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As Worm.OrmManager = TestManagerRS.CreateManagerShared(s)
            Dim f As Worm.Criteria.PredicateLink = New Ctor(GetType(Table1)).prop("Title").[like]("f%")
            Dim col As ICollection(Of Table1) = New QueryCmd().Where(f).ToList(Of Table1)(mgr)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuild()

        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As DicIndexT(Of Table1) = New QueryCmd().BuildDictionary(Of Table1)(mgr, 1)

            Assert.AreEqual(3, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildComplex()

        Dim s As New Worm.ObjectMappingEngine("2")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As DicIndexT(Of Table1) = New QueryCmd().BuildDictionary(Of Table1)(mgr, 1)

            Assert.AreEqual(6, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildComplex2()

        Dim s As New Worm.ObjectMappingEngine("2")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As DicIndexT(Of Table1) = New QueryCmd().BuildDictionary(Of Table1)(mgr, 1)

            Assert.AreEqual(6, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElementsLoadOnlyNames(mgr)

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildWithSort()

        Dim s As New Worm.ObjectMappingEngine("1")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As DicIndexT(Of Table1) = New QueryCmd().BuildDictionary(Of Table1)(mgr, 1)

            Assert.AreEqual(3, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr, SCtor.prop(GetType(Table1), "Code"))

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

    <TestMethod()> _
    Public Sub TestBuildComplexWithSort()

        Dim s As New Worm.ObjectMappingEngine("2")
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(s)

            Dim idx As DicIndexT(Of Table1) = New QueryCmd().BuildDictionary(Of Table1)(mgr, 1)

            Assert.AreEqual(6, idx.TotalCount)

            Dim col As ICollection(Of Table1) = idx.ChildIndexes(0).FindElements(mgr, SCtor.prop(GetType(Table1), "Code"))

            Assert.AreEqual(2, col.Count)
        End Using

    End Sub

End Class
