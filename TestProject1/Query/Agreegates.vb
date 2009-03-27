Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm.Entities.Meta
Imports System.Collections
Imports Worm.Entities
Imports Worm
Imports Worm.Criteria.Joins
Imports Worm.Sorting
Imports Worm.Misc

'Imports Worm.Database.Sorting

<TestClass()> Public Class TestAgreegates

    Private testContextInstance As TestContext

    '''<summary>
    '''Gets or sets the test context which provides
    '''information about and functionality for the current test run.
    '''</summary>
    Public Property TestContext() As TestContext
        Get
            Return testContextInstance
        End Get
        Set(ByVal value As TestContext)
            testContextInstance = value
        End Set
    End Property

#Region "Additional test attributes"
    '
    ' You can use the following additional attributes as you write your tests:
    '
    ' Use ClassInitialize to run code before running the first test in the class
    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
    ' End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    ' <TestInitialize()> Public Sub MyTestInitialize()
    ' End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region

    <TestMethod()> Public Sub TestMax()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(New SelectExpression() { _
                New SelectExpression(New Aggregate(AggregateFunction.Max, GetType(Entity4), "ID")) _
            })

            Dim i As Integer = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)
            Assert.IsFalse(q.LastExecutionResult.CacheHit)

            i = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)
            Assert.IsTrue(q.LastExecutionResult.CacheHit)

        End Using
    End Sub

    <TestMethod()> Public Sub TestMaxCustom()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()

            Dim cust As SelectExpression = FCtor.custom("isnull({0},1)", FCtor.prop(GetType(Entity4), "ID"))

            q.Select(FCtor.max(cust, "s")).From(GetType(Entity4))

            Dim i As Integer = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)
            Assert.IsFalse(q.LastExecutionResult.CacheHit)

            i = q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)
            Assert.IsTrue(q.LastExecutionResult.CacheHit)

        End Using
    End Sub

    <TestMethod()> Public Sub TestCount()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(New SelectExpression() { _
                New SelectExpression(New Aggregate(AggregateFunction.Count)) _
            })
            q.From(GetType(Entity4))

            Dim i As Integer = q.SingleSimple(Of Integer)(mgr) 'q.ToSimpleList(Of Integer)(mgr)(0)

            Assert.AreEqual(12, i)

        End Using
    End Sub

    <TestMethod()> Public Sub TestOrder()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            Dim r As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(t, GetType(Entity), True)
            Dim r2 As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(GetType(Entity), t, True)
            Assert.IsNotNull(r)

            Dim table As SourceFragment = r.Table

            Dim inner As QueryCmd = New QueryCmd()
            inner.From(table)
            inner.Filter = New JoinFilter(table, r2.Column, t, "ID", Worm.Criteria.FilterOperation.Equal)
            inner.SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(New SelectExpression() { _
                New SelectExpression(New Aggregate(AggregateFunction.Count)) _
            })

            Dim q As New QueryCmd()
            q.Sort = SCtor.query(inner).desc
            q.Select(GetType(Entity4))

            Dim l As ReadOnlyEntityList(Of Entity4) = q.ToList(Of Entity4)(mgr)

            Assert.AreEqual(12, l.Count)
            Assert.AreEqual(10, l(0).Identifier)
            Assert.AreEqual("2gwrbwrb", l(0).Title)

            Assert.AreEqual(11, l(1).Identifier)
            Assert.AreEqual("2tb2b25b", l(1).Title)

        End Using
    End Sub

    <TestMethod()> Public Sub TestFilter()

    End Sub

    <TestMethod()> Public Sub TestGroup()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim q As New QueryCmd()
            q.SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(New SelectExpression() { _
                New SelectExpression(New Aggregate(AggregateFunction.Count, "cnt")) _
            })
            q.From(GetType(Entity4))
            'q.Aggregates(0).Alias = "cnt"

            Dim t As Type = GetType(Entity4)
            Dim r As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(t, GetType(Entity), CStr(Nothing))
            Dim r2 As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(GetType(Entity), t, CStr(Nothing))
            Dim table As SourceFragment = r.Table
            Dim jf As New JoinFilter(table, r2.Column, t, "ID", Worm.Criteria.FilterOperation.Equal)
            q.Joins = New QueryJoin() {New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, jf)}

            Assert.IsNotNull(q.SelectList)
            Assert.AreEqual(39, q.ToSimpleList(Of Integer)(mgr)(0))
            Assert.IsNotNull(q.SelectList)

            q.Group = New ObjectModel.ReadOnlyCollection(Of Grouping)( _
                New Grouping() {New Grouping(table, r.Column)} _
            )

            Assert.IsNotNull(q.SelectList)
            Dim l As IList(Of Integer) = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)

            q.Sort = SCtor.custom("cnt").desc
            l = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)
            Assert.AreEqual(11, l(0))
            Assert.AreEqual(4, l(1))

            q.Sort = New Worm.Sorting.Sort(q.SelectList(0).Aggregate, SortType.Desc)
            'q.propSort = sCTOR.
            l = q.ToSimpleList(Of Integer)(mgr)

            Assert.AreEqual(11, l.Count)
            Assert.AreEqual(11, l(0))
            Assert.AreEqual(4, l(1))
        End Using
    End Sub

    <TestMethod()> Public Sub TestGroup2()
        Dim q As New QueryCmd(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        q.Select(FCtor.prop(GetType(Table1), "Title")).From(GetType(Table1))

        Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList

        Assert.AreEqual(3, l.Count)

    End Sub

    <TestMethod()> Public Sub TestDic()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            'Dim tbl As SourceFragment = mgr.ObjectSchema.GetTables(t)(0)
            Dim q As New QueryCmd()
            'Dim o As New Grouping("left({0},1)", "Pref", FCtor.prop(t, "Title"))
            Dim o As Grouping = FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title"))
            q.SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(New SelectExpression() { _
                o, _
                New SelectExpression(New Aggregate(AggregateFunction.Count, "Count")) _
            })
            q.From(t).GroupBy(New Grouping() {o}).OrderBy(SCtor.custom("Count").desc)

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList(mgr)

            Assert.AreEqual(5, l.Count)

            Assert.AreEqual("2", l(0)("Pref"))
            Assert.AreEqual(3, l(0)("Count"))

            Assert.AreEqual("b", l(1)("Pref"))
            Assert.AreEqual(3, l(1)("Count"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestDic2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            'Dim tbl As SourceFragment = mgr.ObjectSchema.GetTables(t)(0)
            Dim q As New QueryCmd()
            q.Select(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title")).count("Count")) _
                .From(t) _
                .GroupBy(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title"))) _
                .OrderBy(SCtor.custom("Count").desc)

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList(mgr)

            Assert.AreEqual(5, l.Count)

            Assert.AreEqual("2", l(0)("Pref"))
            Assert.AreEqual(3, l(0)("Count"))

            Assert.AreEqual("b", l(1)("Pref"))
            Assert.AreEqual(3, l(1)("Count"))

        End Using
    End Sub

    <TestMethod()> Public Sub TestBuildDic()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            'Dim tbl As SourceFragment = mgr.ObjectSchema.GetTables(t)(0)
            Dim q As New QueryCmd()
            q.Select(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title")).count("Count")) _
                .From(t) _
                .GroupBy(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title"))) _
                .OrderBy(SCtor.custom("Count").desc)

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList(mgr)

            Assert.AreEqual(5, l.Count)

            Dim last As DicIndexT(Of Entity4) = DicIndexT(Of Entity4).CreateRoot("Title", q)
            Dim root As DicIndexT(Of Entity4) = last
            Dim first As Boolean = True

            For Each a As AnonymousEntity In l
                OrmManager.BuildDic(Of DicIndexT(Of Entity4), Entity4)(CStr(a("Pref")), CInt(a("Count")), 1, root, last, first, "Title", Nothing)
            Next

            Assert.AreEqual(5, root.ChildIndexes.Length)

            Assert.AreEqual("2", root.ChildIndexes(0).Name)
            Assert.AreEqual(3, root.ChildIndexes(0).Count)

        End Using
    End Sub

    <TestMethod()> Public Sub TestBuildDic2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            'Dim tbl As SourceFragment = mgr.ObjectSchema.GetTables(t)(0)
            Dim q As New QueryCmd()
            q.Select(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title")).count("Count")) _
                .From(t) _
                .GroupBy(FCtor.custom("Pref", "left({0},1)", FCtor.prop(t, "Title"))) _
                .OrderBy(SCtor.custom("Count").desc)

            Dim l As ReadOnlyObjectList(Of AnonymousEntity) = q.ToAnonymList(mgr)

            Assert.AreEqual(5, l.Count)

            Dim root As DicIndexT(Of Entity4) = q.BuildDictionary(Of Entity4)(mgr, 1)

            Assert.AreEqual(5, root.ChildIndexes.Length)

            Assert.AreEqual("2", root.ChildIndexes(0).Name)
            Assert.AreEqual(3, root.ChildIndexes(0).Count)

        End Using
    End Sub

    <TestMethod()> Public Sub TestBuildDic3()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim t As Type = GetType(Entity4)
            'Dim tbl As SourceFragment = mgr.ObjectSchema.GetTables(t)(0)
            Dim q As New QueryCmd()

            Dim root As DicIndexT(Of Entity4) = q.BuildDictionary(Of Entity4)(mgr, "Title", 1)

            Assert.AreEqual(5, root.ChildIndexes.Length)

            Assert.AreEqual("2", root.ChildIndexes(0).Name)
            Assert.AreEqual(3, root.ChildIndexes(0).Count)

            Assert.AreEqual(3, root.ChildIndexes(0).FindElements( _
                            New CreateManager(Function() TestManager.CreateManager(New ObjectMappingEngine("1")))).Count)
            For Each e As Entity4 In root.ChildIndexes(0).FindElements( _
                            New CreateManager(Function() TestManager.CreateManager(New ObjectMappingEngine("1"))))
                Assert.IsTrue(e.Title.StartsWith("2"))
            Next

        End Using
    End Sub

    <TestMethod()> Public Sub TestBuildDicUnion()
        Dim c As New Cache.ReadonlyCache

        Dim q As New QueryCmd( _
                        New CreateManager(Function() TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"), c)))

        Dim root As DicIndexT(Of Table1) = q.BuildDictionary(Of Table1)("Title", "EnumStr", 1)

        Assert.AreEqual(2, root.ChildIndexes.Length)

        Assert.AreEqual("s", root.ChildIndexes(0).Name)
        Assert.AreEqual(3, root.ChildIndexes(0).Count)

        Assert.AreEqual("f", root.ChildIndexes(1).Name)
        Assert.AreEqual(3, root.ChildIndexes(1).Count)

        Assert.AreEqual(2, root.ChildIndexes(1).FindElements.Count)

        For Each e As Table1 In root.ChildIndexes(1).FindElements
            Assert.IsTrue(e.Name.StartsWith("f") OrElse e.EnumStr.ToString.StartsWith("f"))
        Next
    End Sub

    <TestMethod()> Public Sub TestOrder2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateManager(New ObjectMappingEngine("1"))
            Dim typeE As Type = GetType(Entity)
            Dim typeE4 As Type = GetType(Entity4)

            Dim r As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(typeE4, typeE, True)
            Dim r2 As M2MRelationDesc = mgr.MappingEngine.GetM2MRelation(typeE, typeE4, True)
            Assert.IsNotNull(r)

            Dim table As SourceFragment = r.Table

            Dim q As New QueryCmd()
            q.Select(typeE4)
            q.Sort = New Worm.Sorting.Sort( _
                New QueryCmd().From(table). _
                    Select(FCtor.count). _
                    Where(Ctor.column(table, r2.Column).eq(typeE4, "ID")), SortType.Desc)

            Assert.AreEqual(12, q.ToList(Of Entity4)(mgr).Count)
        End Using
    End Sub
End Class
