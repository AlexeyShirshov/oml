Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Orm
Imports Worm.Database

<TestClass()> _
Public Class TestTracker
    Implements Worm.OrmManager.INewObjects

    Private _id As Integer = -100
    Private _new_objects As New Dictionary(Of Integer, OrmBase)

    Public Sub AddNew(ByVal obj As _ICachedEntity) Implements Worm.OrmManager.INewObjects.AddNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Add(CInt(CType(obj, OrmBase).Identifier), CType(obj, OrmBase))
    End Sub

    Public Function GetIdentity() As Integer
        Return CInt(GetIdentity(Nothing))
    End Function

    Public Function GetIdentity(ByVal t As Type) As Object Implements Worm.OrmManager.INewObjects.GetIdentity
        Dim i As Integer = _id
        _id += -1
        Return i
    End Function

    Public Function GetNew(ByVal t As System.Type, ByVal id As Object) As _ICachedEntity Implements Worm.OrmManager.INewObjects.GetNew
        Dim o As OrmBase = Nothing
        _new_objects.TryGetValue(CInt(id), o)
        Return o
    End Function

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As Object) Implements Worm.OrmManager.INewObjects.RemoveNew
        _new_objects.Remove(CInt(id))
        Debug.WriteLine("removed: " & id.ToString)
    End Sub

    Public Sub RemoveNew(ByVal obj As _ICachedEntity) Implements Worm.OrmManager.INewObjects.RemoveNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Remove(CInt(CType(obj, OrmBase).Identifier))
        Debug.WriteLine("removed: " & CType(obj, OrmBase).Identifier.ToString)
    End Sub

    Protected Sub Objr(ByVal o As ICachedEntity, ByVal inloaq As Boolean)
        Debug.WriteLine(o.ObjName)
    End Sub

    Protected Sub br(ByVal cnt As Integer)
        Debug.WriteLine(cnt)
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestCreateObjects()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            _new_objects.Clear()
            mgr.NewObjectManager = Me

            mgr.BeginTransaction()
            Try
                Using tracker As New OrmReadOnlyDBManager.OrmTransactionalScope
                    AddHandler tracker.Saver.ObjectRejected, AddressOf Objr
                    'AddHandler tracker.BeginRollback, AddressOf br

                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.ID))

                    Dim t2 As Table2 = tracker.CreateNewObject(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsTrue(_new_objects.ContainsKey(t2.ID))
                    t2.Money = 1000

                    tracker.Commit()
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestUpdate()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            _new_objects.Clear()
            mgr.NewObjectManager = Me

            Dim tt As Table1 = mgr.Find(Of Table1)(1)
            mgr.BeginTransaction()
            Try
                Using tracker As New OrmReadOnlyDBManager.OrmTransactionalScope
                    AddHandler tracker.Saver.ObjectRejected, AddressOf Objr
                    'AddHandler tracker.BeginRollback, AddressOf br

                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.ID))

                    Dim t2 As Table2 = tracker.CreateNewObject(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsTrue(_new_objects.ContainsKey(t2.ID))
                    t2.Money = 1000

                    tt.Code = 10

                    tracker.Commit()
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)
                Assert.AreNotEqual(10, tt.Code.Value)
                Assert.AreEqual(ObjectState.None, tt.InternalProperties.ObjectState)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestNormal()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            mgr.NewObjectManager = Me

            Dim tt As Table1 = mgr.Find(Of Table1)(1)
            mgr.BeginTransaction()
            Try
                Using tracker As New OrmReadOnlyDBManager.OrmTransactionalScope
                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.ID))

                    tt.Code = 10

                    tracker.Commit()
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)
                Assert.AreEqual(10, tt.Code.Value)
                Assert.AreEqual(ObjectState.None, tt.InternalProperties.ObjectState)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestBatch()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New SQLGenerator("1"))
            mgr.NewObjectManager = Me

            Dim tt As Table1 = mgr.Find(Of Table1)(1)
            Dim tt2 As Table1 = mgr.Find(Of Table1)(2)

            mgr.Find(Of Table1)(10)
            mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("Code").Eq(100), Nothing, True)
            mgr.Find(Of Table1)(New Criteria.Ctor(GetType(Table1)).Field("DT").Eq(Now), Nothing, True)

            mgr.BeginTransaction()
            Try
                Using tracker As New OrmReadOnlyDBManager.OrmTransactionalScope
                    tracker.Saver.AcceptInBatch = True

                    tt.Code = 10
                    tt2.Code = 100

                    tracker.Commit()
                End Using
            Finally
                Assert.AreEqual(10, tt.Code.Value)
                Assert.AreEqual(ObjectState.None, tt.InternalProperties.ObjectState)

                Assert.AreEqual(100, tt2.Code.Value)
                Assert.AreEqual(ObjectState.None, tt2.InternalProperties.ObjectState)

                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
