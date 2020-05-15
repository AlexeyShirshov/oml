Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Query
Imports Worm.Database
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm
Imports Worm.Criteria
Imports Worm.Cache

<TestClass()> Public Class Modification

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

    <TestMethod()> Public Sub TestModification()
        Dim q As New QueryCmd(Function() _
           TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1")))

        Dim t As Table1 = q.Where(Ctor.prop(GetType(Table1), "ID").eq(1)).Single(Of Table1)()

        Assert.IsNull(t.InternalProperties.OriginalCopy)
        Assert.AreEqual(ObjectState.NotLoaded, t.InternalProperties.ObjectState)

        t.Name = "sgfdfg"

        Assert.IsNotNull(t.InternalProperties.OriginalCopy)
        Assert.AreEqual(ObjectState.Modified, t.InternalProperties.ObjectState)

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    mt.Add(t)

                    mt.AcceptModifications()
                End Using

                Assert.AreEqual("sgfdfg", t.Name)

                Assert.IsNull(t.InternalProperties.OriginalCopy)
                Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <Entity("1", TableName:="dbo.guid_table")>
    Public Class RawObj
        Inherits SinglePKEntityBase

        Private _id As Guid
        <EntityProperty(Field2DbRelations.PrimaryKey)>
        <SourceField("pk", SourceFieldType:="uniqueidentifier")>
        Public Overrides Property Identifier() As Object
            Get
                Return _id
            End Get
            Set(ByVal value As Object)
                _id = CType(value, Guid)
            End Set
        End Property

        Private _code As Integer

        <SourceField("code")>
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

    End Class

    <Entity("1", TableName:="dbo.guid_table")>
    Public Class RawObj2
        Inherits CachedLazyLoad

        Private _id As Guid

        <EntityProperty(Field2DbRelations.PrimaryKey)>
        <SourceField("pk", SourceFieldType:="uniqueidentifier")>
        Public Property Identifier() As Object
            Get
                Return _id
            End Get
            Set(ByVal value As Object)
                _id = CType(value, Guid)
            End Set
        End Property

        Private _code As Integer

        <SourceField("code")>
        Public Property Code() As Integer
            Get
                Return _code
            End Get
            Set(ByVal value As Integer)
                _code = value
            End Set
        End Property

        Protected Overrides Function GetCacheKey() As Integer
            Return _id.GetHashCode
        End Function
    End Class

    <TestMethod()>
    Public Sub TestRawObjectUpdate()
        'Dim q As New QueryCmd(Function() _
        '   TestManager.CreateWriteManager(New ObjectMappingEngine("1")))

        'Dim t As RawObj = q.Top(1).SelectEntity(GetType(RawObj), True).Single(Of RawObj)()

        'Assert.IsNotNull(t)
        'Dim oldCode As Integer = t.Code

        'Assert.IsTrue(t.InternalProperties.IsLoaded)
        'Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Code"))
        'Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
        'Assert.IsNull(t.InternalProperties.OriginalCopy)
        ''Assert.IsNotNull(t.InternalProperties.OriginalCopy)
        ''Assert.AreEqual(ObjectState.Clone, t.InternalProperties.OriginalCopy.ObjectState)
        ''Assert.AreEqual(oldCode, CType(t.InternalProperties.OriginalCopy, RawObj).Code)

        't.Code += 2

        'Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
        'Assert.IsNull(t.InternalProperties.OriginalCopy)
        ''Assert.AreEqual(ObjectState.Clone, t.InternalProperties.OriginalCopy.ObjectState)
        'Assert.AreEqual(oldCode, q.Top(1).SelectEntity(GetType(RawObj), True).Single(Of RawObj)().Code)

        'Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"))
        '    mgr.BeginTransaction()
        '    Try
        '        Using mt As New ModificationsTracker(mgr)
        '            mt.Add(t)

        '            mt.AcceptModifications()
        '        End Using

        '        Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
        '        Assert.IsNull(t.InternalProperties.OriginalCopy)
        '        'Assert.IsNotNull(t.InternalProperties.OriginalCopy)
        '        'Assert.AreEqual(oldCode + 2, CType(t.InternalProperties.OriginalCopy, RawObj).Code)

        '        Assert.AreEqual(oldCode + 2, t.Code)
        '        Assert.AreEqual(oldCode + 2, q.Top(1).SelectEntity(GetType(RawObj), True).Single(Of RawObj)(mgr).Code)
        '    Finally
        '        mgr.Rollback()
        '    End Try
        'End Using
    End Sub

    <TestMethod()>
    Public Sub TestRawObjectCreate()
        Dim t As New RawObj
        t.Code = 100

        Assert.AreEqual(ObjectState.Created, t.InternalProperties.ObjectState)

        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New ObjectMappingEngine("1"), New MSSQL2005Generator)
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    mt.Add(t)

                    mt.AcceptModifications()
                End Using

                Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()>
    Public Sub TestRawObjectCreate2()
        Dim t As New RawObj

        Assert.AreEqual("RawObj - Created (Code=0;Identifier=00000000-0000-0000-0000-000000000000;): ", t.InternalProperties.ObjName)

        t.Code = 100

        Assert.AreEqual("RawObj - Created (Code=100;Identifier=00000000-0000-0000-0000-000000000000;): ", t.InternalProperties.ObjName)

        'Dim t2 As RawObj = CType(t.Clone, RawObj)

        'Assert.AreEqual("RawObj - Created (Identifier=00000000-0000-0000-0000-000000000000;Code=100;): ", t2.InternalProperties.ObjName)
    End Sub

    <TestMethod()>
    Public Sub TestRawObjectCreate3()
        Dim t As New RawObj2

        Assert.AreEqual("RawObj2 - Created (Code=0;Identifier=00000000-0000-0000-0000-000000000000;): ", t.InternalProperties.ObjName)

        t.Code = 100

        Assert.AreEqual("RawObj2 - Created (Code=100;Identifier=00000000-0000-0000-0000-000000000000;): ", t.InternalProperties.ObjName)

        'Dim t2 As RawObj2 = CType(t.Clone, RawObj2)

        'Assert.AreEqual("RawObj2 - Created (Identifier=00000000-0000-0000-0000-000000000000;Code=100;): ", t2.InternalProperties.ObjName)
    End Sub

    <TestMethod()>
    Public Sub TestRawObjectDelete()
        Dim q As New QueryCmd(Function() _
           TestManager.CreateWriteManager(New ObjectMappingEngine("1")))

        Dim t As RawObj = q.Top(1).SelectEntity(GetType(RawObj), True).Single(Of RawObj)()

        Assert.IsNotNull(t)
        Assert.IsTrue(t.InternalProperties.IsPropertyLoaded("Code"))
        Assert.AreEqual(ObjectState.None, t.InternalProperties.ObjectState)

        t.Delete()

        Assert.AreEqual(ObjectState.Deleted, t.InternalProperties.ObjectState)
    End Sub

    <TestMethod()>
    Public Sub TestModifyPOCO()
        Dim t As New SourceFragment("dbo", "table1")
        Dim c As New Cache.OrmCache

        Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

        Dim q As New QueryCmd(New CreateManager(Function() TestManagerRS.CreateManagerShared(mpe, c)))
        Dim pocoType As Type = GetType(Pod.cls)

        q.Select(FCtor.column(t, "code").into("Code").column(t, "name").into("Title").column(t, "id").into("ID", Field2DbRelations.PK)).
            OrderBy(SCtor.prop(pocoType, "Code")).From(t)

        Dim l As IList(Of Pod.cls) = q.ToPOCOList(Of Pod.cls)()

        Dim o As Pod.cls = l(0)

        Assert.AreEqual(1, o.ID)
        Assert.AreEqual(2, o.Code)

        Dim pocoSchema = q.GetEntitySchema(mpe, pocoType)

        Dim shadow = c.GetPOCO(mpe, pocoSchema, o)
        Dim undo As IUndoChanges = CType(shadow, IUndoChanges)
        Dim ce = TryCast(shadow, AnonymousEntity)

        Assert.AreEqual(o.ID, shadow.Key)
        Assert.AreEqual(o.ID, shadow.GetPKs.CalcHashCode)
        Assert.AreEqual(o.ID, shadow.GetHashCode)
        Assert.AreEqual(o.ID, New PKWrapper(shadow.GetPKs).GetHashCode)
        Assert.AreEqual(o.Code, ce("Code"))

        Assert.AreEqual(ObjectState.None, undo.ObjectState)
        Assert.IsNull(undo.OriginalCopy)

        o.Code = o.Code + 100

        'c.SyncPOCO(mpe, pocoSchema, o, Nothing)
        c.SyncPOCO(shadow, mpe, pocoSchema, o)

        Assert.AreEqual(ObjectState.Modified, undo.ObjectState)
        Assert.IsNotNull(undo.OriginalCopy)

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(mpe, c)
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    mt.Add(ce)
                    mt.AcceptModifications()
                End Using

                Assert.AreEqual(ObjectState.None, undo.ObjectState)
                Assert.IsNull(undo.OriginalCopy)

                l = q.ToPOCOList(Of Pod.cls)()
                Assert.IsTrue(q.LastExecutionResult.CacheHit)

                Assert.AreNotSame(l(0), o)

                Assert.AreEqual(l(0).Code, o.Code)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestModifyPOCO2()
        'Dim t As New SourceFragment("dbo", "table1")
        'Dim c As New Cache.OrmCache

        'Dim mpe As ObjectMappingEngine = New ObjectMappingEngine("1")

        Dim q As New QueryCmd(New CreateManager(Function() _
            TestManagerRS.CreateManagerShared(New ObjectMappingEngine("1"))))

        q.Select(FCtor _
                 .prop(GetType(Pod.cls5), "Code") _
                 .prop(GetType(Pod.cls5), "Title") _
                 .prop(GetType(Pod.cls5), "ID")). _
            OrderBy(SCtor.prop(GetType(Pod.cls5), "Code"))

        Dim l As IList(Of Pod.cls5) = q.ToPOCOList(Of Pod.cls5)()

        'Dim ce As _ICachedEntity = c.GetPOCO(mpe, q.GetEntitySchema(mpe, GetType(Pod.cls2)), l(0))

        Dim o As Pod.cls5 = l(0)

        'Assert.AreEqual(ObjectState.None, ce.ObjectState)
        'Assert.IsNull(ce.OriginalCopy)

        o.Code = o.Code + 100

        'c.SyncPOCO(mpe, q.GetEntitySchema(mpe, GetType(Pod.cls2)), l(0))

        'Assert.AreEqual(ObjectState.Modified, ce.ObjectState)
        'Assert.IsNotNull(ce.OriginalCopy)

        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"), New Cache.OrmCache)
            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    mt.Add(o)
                    mt.AcceptModifications()
                End Using

                'Assert.AreEqual(ObjectState.None, ce.ObjectState)
                'Assert.IsNull(ce.OriginalCopy)

                l = q.ToPOCOList(Of Pod.cls5)()
                Assert.IsTrue(q.LastExecutionResult.CacheHit)

                Assert.AreNotSame(l(0), o)

                'Assert.AreEqual(l(0).Code, o.Code)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
