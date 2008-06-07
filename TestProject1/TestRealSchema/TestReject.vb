Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Database
Imports Worm.Orm

<TestClass()> _
Public Class TestReject

    <TestMethod()> _
    Public Sub TestAdd()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                t1.Tbl = mgr.Find(Of Table1)(1)
                t1.Money = 10
                t1.SaveChanges(False)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))

                t1.AcceptChanges()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestAdd2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                t1.Tbl = mgr.Find(Of Table1)(1)
                t1.Money = 10
                Assert.AreEqual(ObjectState.Created, t1.InternalProperties.ObjectState)
                t1.SaveChanges(False)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Modified, t1.InternalProperties.ObjectState)

                Assert.AreNotEqual(-100, t1.Identifier)
                t1.RejectChanges()
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As Table1 = mgr.Find(Of Table1)(3)

            mgr.BeginTransaction()
            Try
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                t1.Delete()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                t1.SaveChanges(False)
                Assert.IsTrue(mgr.IsInCachePrecise(t1))

                t1.AcceptChanges()
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestDelete2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As Table1 = mgr.Find(Of Table1)(3)

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

                t1.RejectChanges()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.None, t1.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestSaver()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t2 As Table1 = mgr.Find(Of Table1)(1)
            Dim t1 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)

            mgr.BeginTransaction()
            Try
                t2.Delete()
                Dim created As Boolean
                Using s As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Commit()
                End Using
            Finally
                mgr.Rollback()
                Assert.AreEqual(-100, t1.Identifier)
                Assert.IsFalse(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(ObjectState.Deleted, t2.InternalProperties.ObjectState)
                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                t2.RejectChanges()
                Assert.AreEqual(ObjectState.None, t2.InternalProperties.ObjectState)
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestUpdate()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As New Table2(-100, mgr.Cache, mgr.ObjectSchema)
            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            mgr.BeginTransaction()
            Try
                t2.Money = 1000
                Dim created As Boolean
                Using s As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Commit()
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As Table3 = mgr.Find(Of Table3)(1)
            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            mgr.BeginTransaction()
            Dim a As Byte() = Nothing
            Try
                Dim xdoc As New Xml.XmlDocument
                xdoc.LoadXml("<root a='b'/>")
                t1.Xml = xdoc
                t2.Money = 1000
                a = t1.Version
                Dim created As Boolean
                Using s As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Commit()
                End Using
            Finally
                mgr.Rollback()
                Assert.IsTrue(mgr.IsInCachePrecise(t1))
                Assert.AreEqual(a, t1.Version)
                Assert.AreEqual(ObjectState.Modified, t1.InternalProperties.ObjectState)

                Assert.IsTrue(mgr.IsInCachePrecise(t2))
                Assert.AreEqual(Of Decimal)(1000, t2.Money)
                Assert.AreEqual(ObjectState.Modified, t2.InternalProperties.ObjectState)
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestUpdate3()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            Dim t1 As Table3 = mgr.Find(Of Table3)(1)
            Dim t2 As Table2 = mgr.Find(Of Table2)(1)
            mgr.BeginTransaction()
            Dim a As Byte() = Nothing
            Try
                Dim xdoc As New Xml.XmlDocument
                xdoc.LoadXml("<root a='b'/>")
                t1.Xml = xdoc
                t2.Money = 10
                a = t1.Version
                Dim created As Boolean
                Using s As OrmReadOnlyDBManager.BatchSaver = mgr.CreateBatchSaver(created)
                    Assert.IsTrue(created)
                    s.Add(t1)
                    s.Add(t2)
                    s.Commit()
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

End Class
