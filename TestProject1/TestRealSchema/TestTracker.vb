Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports System.Diagnostics

<TestClass()> _
Public Class TestTracker
    Implements Orm.OrmManagerBase.INewObjects

    Private _id As Integer = -100
    Private _new_objects As New Dictionary(Of Integer, Orm.OrmBase)

    Public Sub AddNew(ByVal obj As Worm.Orm.OrmBase) Implements Worm.Orm.OrmManagerBase.INewObjects.AddNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Add(obj.Identifier, obj)
    End Sub

    Public Function GetIdentity() As Integer Implements Worm.Orm.OrmManagerBase.INewObjects.GetIdentity
        Dim i As Integer = _id
        _id += -1
        Return i
    End Function

    Public Function GetNew(ByVal t As System.Type, ByVal id As Integer) As Worm.Orm.OrmBase Implements Worm.Orm.OrmManagerBase.INewObjects.GetNew
        Dim o As Orm.OrmBase = Nothing
        _new_objects.TryGetValue(id, o)
        Return o
    End Function

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As Integer) Implements Worm.Orm.OrmManagerBase.INewObjects.RemoveNew
        _new_objects.Remove(id)
    End Sub

    Public Sub RemoveNew(ByVal obj As Worm.Orm.OrmBase) Implements Worm.Orm.OrmManagerBase.INewObjects.RemoveNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Remove(obj.Identifier)
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestCreateObjects()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            mgr.NewObjectManager = Me

            mgr.BeginTransaction()
            Try
                Using tracker As New Orm.OrmReadOnlyDBManager.ObjectStateTracker
                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.Identifier))

                    Dim t2 As Table2 = tracker.CreateNewObject(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsTrue(_new_objects.ContainsKey(t2.Identifier))
                    t2.Money = 1000
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Data.SqlClient.SqlException))> _
    Public Sub TestUpdate()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            mgr.NewObjectManager = Me

            Dim tt As Table1 = mgr.Find(Of Table1)(1)
            mgr.BeginTransaction()
            Try
                Using tracker As New Orm.OrmReadOnlyDBManager.ObjectStateTracker
                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.Identifier))

                    Dim t2 As Table2 = tracker.CreateNewObject(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsTrue(_new_objects.ContainsKey(t2.Identifier))
                    t2.Money = 1000

                    tt.Code = 10
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)
                Assert.AreNotEqual(10, tt.Code.Value)
                Assert.AreEqual(Orm.ObjectState.None, tt.ObjectState)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestNormal()
        Using mgr As Orm.OrmReadOnlyDBManager = TestManagerRS.CreateManagerShared(New Orm.DbSchema("1"))
            mgr.NewObjectManager = Me

            Dim tt As Table1 = mgr.Find(Of Table1)(1)
            mgr.BeginTransaction()
            Try
                Using tracker As New Orm.OrmReadOnlyDBManager.ObjectStateTracker
                    Dim t As Table1 = tracker.CreateNewObject(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsTrue(_new_objects.ContainsKey(t.Identifier))

                    tt.Code = 10
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)
                Assert.AreEqual(10, tt.Code.Value)
                Assert.AreEqual(Orm.ObjectState.None, tt.ObjectState)

                mgr.Rollback()
            End Try
        End Using
    End Sub
End Class
