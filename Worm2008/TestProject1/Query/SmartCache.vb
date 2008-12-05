Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Database.Criteria
Imports Worm.Entities
Imports Worm
Imports Worm.Criteria
Imports Worm.Criteria.Joins

<TestClass()> Public Class CacheBehaviourWhatCan

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

    <TestMethod()> Public Sub TestSort()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)).Sort(Sorting.Custom("name"))

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestTable()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table1)).Table

            Dim q As QueryCmd = New QueryCmd(tbl).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec))

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestFilter()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table1)).Table

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                PCtor.column(tbl, "enum_str").eq(Enum1.sec.ToString))

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    <TestMethod()> Public Sub TestJoins()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table3)).Table

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec))
            q.SetJoins(JCtor.join(tbl).[on](tbl, "ref_id").eq(GetType(Table1), "ID"))

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

End Class

<TestClass()> Public Class CacheBehaviourThrowException

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

    <TestMethod()> Public Sub TestSort()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)).Sort(Sorting.Custom("name"))

            Dim dic As New System.Collections.Hashtable

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheOrThrowException
            AddHandler q.CacheDictionaryRequired, Function(qr As QueryCmd, args As QueryCmd.CacheDictionaryRequiredEventArgs) _
                                                     s(args, dic)

            Assert.AreEqual(0, dic.Count)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            Assert.AreEqual(1, dic.Count)

            q.ToList(Of Table1)(mgr)
            Assert.IsTrue(q.LastExecitionResult.CacheHit)

            Assert.AreEqual(1, dic.Count)
        End Using
    End Sub

    Private Function s(ByVal args As QueryCmd.CacheDictionaryRequiredEventArgs, ByVal dic As System.Collections.IDictionary) As Boolean
        args.GetDictionary = Function(key As String) dic
    End Function

    <TestMethod(), ExpectedException(GetType(QueryCmdException))> Public Sub TestSortThrow()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New ObjectMappingEngine("1"))

            Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
                PCtor.prop(GetType(Table1), "EnumStr").eq(Enum1.sec)).Sort(Sorting.Custom("name"))

            Dim dic As New System.Collections.Hashtable

            mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheOrThrowException

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)

            q.ToList(Of Table1)(mgr)
            Assert.IsFalse(q.LastExecitionResult.CacheHit)
        End Using
    End Sub

    '<TestMethod()> Public Sub TestTable()
    '    Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))

    '        Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table1)).Table

    '        Dim q As QueryCmd = New QueryCmd(tbl).Where( _
    '            Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec))

    '        mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    '    End Using
    'End Sub

    '<TestMethod()> Public Sub TestFilter()
    '    Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))

    '        Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table1)).Table

    '        Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
    '            Ctor.Column(tbl, "enum_str").Eq(Enum1.sec.ToString))

    '        mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    '    End Using
    'End Sub

    '<TestMethod()> Public Sub TestJoins()
    '    Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New SQLGenerator("1"))

    '        Dim tbl As SourceFragment = mgr.MappingEngine.GetObjectSchema(GetType(Table3)).Table

    '        Dim q As QueryCmd = New QueryCmd(GetType(Table1)).Where( _
    '            Ctor.AutoTypeField("EnumStr").Eq(Enum1.sec))
    '        q.AddJoins(JCtor.Join(tbl).On(tbl, "ref_id").Eq(GetType(Table1), "ID"))

    '        mgr.Cache.CacheListBehavior = Cache.CacheListBehavior.CacheWhatCan

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)

    '        q.ToOrmList(Of Table1)(mgr)
    '        Assert.IsFalse(q.LastExecitionResult.CacheHit)
    '    End Using
    'End Sub

End Class
