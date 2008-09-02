﻿Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Database
Imports Worm.Query
Imports Worm.Database.Criteria

<TestClass()> Public Class Test

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
            testContextInstance = Value
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

    <TestMethod()> Public Sub TestAdd()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New SQLGenerator("1"))
            Dim q As New QueryCmdBase(GetType(Entity))
            Assert.IsNotNull(q)

            q.Filter = Ctor.AutoTypeField("ID").Eq(1)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Orm.IM2M).Find(GetType(Entity4)).ToEntityList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            Dim q2 As New QueryCmdBase(GetType(Entity4))
            q2.Filter = Ctor.AutoTypeField("ID").Eq(2)

            Dim e2 As Entity4 = q2.ToEntityList(Of Entity4)(mgr)(0)

            Assert.IsFalse(l.Contains(e2))

            Dim l2 As Worm.ReadOnlyEntityList(Of Entity) = CType(e2, Worm.Orm.IM2M).Find(GetType(Entity)).ToEntityList(Of Entity)(mgr)
            Assert.IsFalse(l2.Contains(e))

            mgr.BeginTransaction()
            Try
                CType(e, Worm.Orm.IM2M).Add(e2)
                e.SaveChanges(True)

                l = CType(e, Worm.Orm.IM2M).Find(GetType(Entity4)).ToEntityList(Of Entity4)(mgr)
                Assert.IsNotNull(l)
                Assert.AreEqual(5, l.Count)

                Assert.IsTrue(l.Contains(e2))

                l2 = CType(e2, Worm.Orm.IM2M).Find(GetType(Entity)).ToEntityList(Of Entity)(mgr)
                Assert.IsTrue(l2.Contains(e))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestAddNew()
        Dim t As New TestManager
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New SQLGenerator("1"))
            mgr.NewObjectManager = t
            Dim q As New QueryCmdBase(GetType(Entity))
            Assert.IsNotNull(q)

            q.Filter = Ctor.AutoTypeField("ID").Eq(1)

            Dim e As Entity = q.ToEntityList(Of Entity)(mgr)(0)

            Dim l As Worm.ReadOnlyEntityList(Of Entity4) = CType(e, Worm.Orm.IM2M).Find(GetType(Entity4)).ToEntityList(Of Entity4)(mgr)
            Assert.IsNotNull(l)
            Assert.AreEqual(4, l.Count)

            mgr.BeginTransaction()
            Try
                Dim e2 As Entity4 = Nothing

                Using s As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    s.Add(e)

                    e2 = s.CreateNewObject(Of Entity4)()

                    CType(e, Worm.Orm.IM2M).Add(e2)

                    s.Commit()
                End Using

                l = CType(e, Worm.Orm.IM2M).Find(GetType(Entity4)).ToEntityList(Of Entity4)(mgr)
                Assert.IsNotNull(l)
                Assert.AreEqual(5, l.Count)

                Assert.IsTrue(l.Contains(e2))

                Dim l2 As Worm.ReadOnlyEntityList(Of Entity) = CType(e2, Worm.Orm.IM2M).Find(GetType(Entity)).ToEntityList(Of Entity)(mgr)
                Assert.IsTrue(l2.Contains(e))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
