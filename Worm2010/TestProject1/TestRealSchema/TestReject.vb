Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Entities
Imports Worm.Criteria
Imports Worm.Query
Imports Worm

<TestClass()> _
Public Class TestReject

    <TestMethod()> _
    Public Sub TestAdd()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.MappingEngine)

            mgr.BeginTransaction()
            Try
                t1.Tbl = New QueryCmd().GetByID(Of Table1)(1, mgr)
                t1.Money = 10
                t1.SaveChanges(False)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))

                mgr.AcceptChanges(t1)
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.MappingEngine)

            mgr.BeginTransaction()
            Try
                t1.Tbl = New QueryCmd().GetByID(Of Table1)(1, mgr)
                t1.Money = 10
                Assert.AreEqual(ObjectState.Created, t1.InternalProperties.ObjectState)
                t1.SaveChanges(False)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Modified, t1.InternalProperties.ObjectState)

                Assert.AreNotEqual(-100, t1.Identifier)
                mgr.RejectChanges(t1)
                Assert.AreEqual(-100, t1.Identifier)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Created, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(3, mgr)

            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                t1.Delete()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                t1.SaveChanges(False)
                Assert.IsTrue(mgr.IsInCachePrecise(t1))

                mgr.AcceptChanges(t1)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As Table1 = New QueryCmd().GetByID(Of Table1)(3, GetByIDOptions.EnsureExistsInStore, mgr)

            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
                t1.Delete()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Deleted, t1.InternalProperties.ObjectState)
                t1.SaveChanges(False)
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Deleted, t1.InternalProperties.ObjectState)

                mgr.RejectChanges(t1)
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestSaver()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t2 As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim t1 As New Table2(-100, mgr.Cache, mgr.MappingEngine)
            Assert.IsNull(t2.InternalProperties.OriginalCopy)

            mgr.BeginTransaction()
            Try
                t2.Delete()
                Dim created As Boolean
                Using s As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Accept()
                End Using
            Finally
                mgr.Rollback()
                Assert.AreEqual(-100, t1.Identifier)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Deleted, t2.InternalProperties.ObjectState)
                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                mgr.RejectChanges(t2)
                Assert.AreEqual(ObjectState.None, t2.InternalProperties.ObjectState)
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestUpdate()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.MappingEngine)
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            mgr.BeginTransaction()
            Try
                t2.Money = 1000
                Dim created As Boolean
                Using s As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Accept()
                End Using
            Finally
                mgr.Rollback()
                Assert.AreEqual(-100, t1.Identifier)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                Assert.AreEqual(Of Decimal)(1000, t2.Money)
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestUpdate2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As Table3 = New QueryCmd().GetByID(Of Table3)(1, mgr)
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            mgr.BeginTransaction()
            Dim a As Byte() = Nothing
            Try
                Dim xdoc As New System.Xml.XmlDocument
                xdoc.LoadXml("<root a='b'/>")
                t1.Xml = xdoc
                t2.Money = 1000
                a = t1.Version
                Dim created As Boolean
                Using s As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Accept()
                End Using
            Finally
                mgr.Rollback()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                For i As Integer = 0 To a.Length - 1
                    Assert.AreEqual(a(i), t1.Version(i))
                Next

                Assert.AreEqual(ObjectState.Modified, t1.InternalProperties.ObjectState)

                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                Assert.AreEqual(Of Decimal)(1000, t2.Money)
                Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestUpdate3()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t1 As Table3 = New QueryCmd().GetByID(Of Table3)(1, mgr)
            Assert.IsNotNull(t1)
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Assert.IsNotNull(t2)

            mgr.BeginTransaction()
            Dim a As Byte() = Nothing
            Try
                Dim xdoc As New System.Xml.XmlDocument
                xdoc.LoadXml("<root a='b'/>")
                t1.Xml = xdoc
                t2.Money = 10
                a = t1.Version
                Dim created As Boolean
                Using s As ObjectListSaver = mgr.CreateBatchSaver(Of ObjectListSaver)(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Accept()
                End Using
            Finally
                mgr.Rollback()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.IsFalse(Worm.helper.IsEqualByteArray(a, t1.Version))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)

                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                Assert.AreEqual(Of Decimal)(10, t2.Money)
                Assert.AreEqual(ObjectState.None, t2.InternalProperties.ObjectState)
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> Public Sub TestDeleteOrderWrong()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Dim t3 As Table3 = New QueryCmd().GetByID(Of Table3)(2, mgr)

            Assert.AreEqual(t3.RefObject, t2)

            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    t2.Delete()
                    t3.Delete()
                    mt.Saver.ResolveDepends = False

                    mt.AcceptModifications()
                End Using
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> Public Sub TestDeleteOrder()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim t2 As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Dim t3 As Table3 = New QueryCmd().GetByID(Of Table3)(2, mgr)

            Assert.AreEqual(t3.RefObject, t2)

            mgr.BeginTransaction()
            Try
                Using mt As New ModificationsTracker(mgr)
                    t2.Delete()
                    t3.Delete()

                    For Each tt As Tables1to3 In New Worm.Query.QueryCmd(). _
                        Where(Ctor.prop(GetType(Tables1to3), "Table3").eq(t3)).ToList(Of Tables1to3)(mgr)
                        tt.Delete()
                    Next

                    Dim i As Table4 = New QueryCmd().GetByID(Of Table4)(t2.Identifier, mgr)
                    i.Delete()

                    mt.AcceptModifications()
                End Using
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
