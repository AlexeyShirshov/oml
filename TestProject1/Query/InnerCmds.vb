Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm
Imports Worm.Entities

<TestClass()> Public Class InnerCmds

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

    '<TestMethod(), ExpectedException(GetType(QueryCmdException))> Public Sub TestInner()

    '    Dim inner As New QueryCmd(Function() _
    '        TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

    '    inner.SelectEntity(GetType(Table1))

    '    Dim q As New QueryCmd(Function() _
    '        TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

    '    Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).SelectEntity(GetType(Table1), True).ToList(Of Table1)()

    '    Assert.AreEqual(3, r.Count)

    '    For Each t As Table1 In r
    '        Assert.IsTrue(t.InternalProperties.IsLoaded)
    '    Next
    'End Sub

    <TestMethod(), ExpectedException(GetType(QueryCmdException))> Public Sub TestInnerWrongLoad()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.SelectEntity(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).SelectEntity(GetType(Table1), True).ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsTrue(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInner2()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.SelectEntity(GetType(Table1), True)

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner).ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerWrongCols()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.Where(Ctor.prop(GetType(Table1), "Code").not_eq(2))
        inner.AutoFields = False

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From( _
            inner.Select(FCtor.prop(GetType(Table1), "Code").into("ID"))). _
        ToList(Of Table1)()

        Assert.AreEqual(2, r.Count)

        For Each t As Table1 In r
            Assert.AreEqual(Entities.ObjectState.NotLoaded, t.InternalProperties.ObjectState)
            t.Load()
            Assert.AreEqual(Entities.ObjectState.NotFoundInSource, t.InternalProperties.ObjectState)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerWithoutLoadID()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        'inner.SelectEntity(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner.Select(FCtor.prop(GetType(Table1), "ID"))). _
            ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestInnerID()

        Dim inner As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))
        inner.SelectEntity(GetType(Table1))

        Dim q As New QueryCmd(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1")))

        Dim r As ReadOnlyEntityList(Of Table1) = q.From(inner.Select(FCtor.prop(GetType(Table1), "ID"))). _
            ToList(Of Table1)()

        Assert.AreEqual(3, r.Count)

        For Each t As Table1 In r
            Assert.IsFalse(t.InternalProperties.IsLoaded)
        Next
    End Sub

    <TestMethod()> Public Sub TestSelectInner()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)).Select(FCtor.prop(GetType(Entity4), "Title")). _
            Where(Ctor.prop(GetType(Entity), "ID").eq(GetType(Entity4), "ID"))

        inner.AutoFields = False

        Dim q As New QueryCmd(Function() _
                    TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.Select(FCtor.prop(GetType(Entity), "ID").query(inner).into("Title"))

        Dim l As ReadOnlyObjectList(Of Entities.AnonymousEntity) = q.ToAnonymList

        Assert.AreEqual(13, l.Count)

        For Each e As Entities.AnonymousEntity In l
            If CInt(e("ID")) = 13 Then
                Assert.IsNull(e("Title"))
            Else
                Assert.IsNotNull(e("Title"))
            End If
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSubquery()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)).SelectEntity(GetType(Entity4))

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.SelectEntity(GetType(Entity)) _
            .Join(JCtor.join(al).on(GetType(Entity), "ID").eq(New ObjectProperty(al, "ID")))

        Dim r As ReadOnlyList(Of Entity) = q.ToOrmList(Of Entity)()

        Assert.AreEqual(12, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSubqueryLJ()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)).SelectEntity(GetType(Entity4))

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.SelectEntity(GetType(Entity)) _
            .Join(JCtor.left_join(al).on(GetType(Entity), "ID").eq(New ObjectProperty(al, "ID")))

        Dim r As ReadOnlyList(Of Entity) = q.ToOrmList(Of Entity)()

        Assert.AreEqual(13, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSubquerySel()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)).SelectEntity(GetType(Entity4), True)

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.Select(FCtor.prop(GetType(Entity), "ID").prop(al, "Title")) _
            .Join(JCtor.join(al).on(GetType(Entity), "ID").eq(al, "ID"))

        Dim r As ReadOnlyObjectList(Of Entities.AnonymousEntity) = q.ToAnonymList

        Assert.AreEqual(12, r.Count)

        For Each e As Entities.AnonymousEntity In r
            Assert.IsNotNull(e("ID"))
            Assert.IsNotNull(e("Title"))
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestJoinSubqueryWhere()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)).SelectEntity(GetType(Entity4), True)

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.Select(FCtor.prop(GetType(Entity), "ID").prop(al, "Title")) _
            .Join(JCtor.join(al).on(GetType(Entity), "ID").eq(New ObjectProperty(al, "ID"))) _
            .Where(Ctor.prop(al, "Title").like("b%"))

        Dim r As ReadOnlyObjectList(Of Entities.AnonymousEntity) = q.ToAnonymList

        Assert.AreEqual(3, r.Count)

        For Each e As Entities.AnonymousEntity In r
            Assert.IsNotNull(e("ID"))
            Assert.IsNotNull(e("Title"))
        Next
    End Sub

    <TestMethod()> _
    Public Sub TestSubqueryWhere()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)) _
            .Select(FCtor.count) _
            .Where(Ctor.custom("left({0},1)", ECtor.prop(GetType(Entity4), "Title")).eq(GetType(Entity5), "Title"))

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.SelectEntity(GetType(Entity5)) _
            .Where(Ctor.query(inner).greater_than(2))

        Dim r As ReadOnlyEntityList(Of Entity5) = q.ToList(Of Entity5)()

        Assert.AreEqual(1, r.Count)

        Assert.AreEqual(2, r(0).ID)
    End Sub

    <TestMethod()> _
    Public Sub TestSubqueryWhere2()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)) _
            .Select(FCtor.count) _
            .Where(Ctor.custom("left({0},1)", ECtor.prop(GetType(Entity4), "Title")).eq(GetType(Entity5), "Title"))

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.SelectEntity(GetType(Entity5)) _
            .Where(Ctor.prop(GetType(Entity5), "ID").greater_than(inner))

        Dim r As ReadOnlyEntityList(Of Entity5) = q.ToList(Of Entity5)()

        Assert.AreEqual(2, r.Count)
    End Sub

    <TestMethod()> _
    Public Sub TestProj()
        Dim inner As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        inner.From(GetType(Entity4)) _
            .Select(FCtor.prop(GetType(Entity4), "Title").prop(GetType(Entity4), "ID"))

        Dim al As New QueryAlias(inner)

        Dim q As New QueryCmd(Function() _
            TestManager.CreateManager(New ObjectMappingEngine("1")))

        q.Select(FCtor.prop(al, "ID")) _
            .Where(Ctor.prop(al, "Title").eq("45t4"))

        Assert.AreEqual(9, q.SingleSimple(Of Integer))
    End Sub
End Class
