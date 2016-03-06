Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Diagnostics
Imports Worm.Entities
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria
Imports Worm.Query
Imports Worm
Imports System.Linq

<TestClass()> _
Public Class TestTracker
    Implements INewObjectsStore

    Private _id As Integer = -100
    Private _new_objects As New Dictionary(Of Integer, SinglePKEntity)

    Public Sub AddNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.AddNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Add(CInt(CType(obj, SinglePKEntity).Identifier), CType(obj, SinglePKEntity))
    End Sub

    Public Function GetIdentity() As Integer
        Return CInt(GetIdentity(Nothing, Nothing)(0).Value)
    End Function

    Public Function GetIdentity(ByVal t As Type, ByVal mpe As Worm.ObjectMappingEngine) As PKDesc() Implements INewObjectsStore.GetPKForNewObject
        Dim i As Integer = _id
        _id += -1
        Return New PKDesc() {New PKDesc("ID", i)}
    End Function

    Public Function GetNew(ByVal t As System.Type, ByVal id As IEnumerable(Of Meta.PKDesc)) As _ICachedEntity Implements INewObjectsStore.GetNew
        Dim o As SinglePKEntity = Nothing
        _new_objects.TryGetValue(CInt(id(0).Value), o)
        Return o
    End Function

    Public Sub RemoveNew(ByVal t As System.Type, ByVal id As IEnumerable(Of Meta.PKDesc)) Implements INewObjectsStore.RemoveNew
        _new_objects.Remove(CInt(id(0).Value))
        Debug.WriteLine("removed: " & id.ToString)
    End Sub

    Public Sub RemoveNew(ByVal obj As _ICachedEntity) Implements INewObjectsStore.RemoveNew
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        _new_objects.Remove(CInt(CType(obj, SinglePKEntity).Identifier))
        Debug.WriteLine("removed: " & CType(obj, SinglePKEntity).Identifier.ToString)
    End Sub

    Protected Sub Objr(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity, ByVal inloaq As Boolean)
        Debug.WriteLine(o.ObjName)
    End Sub

    Protected Sub br(ByVal cnt As Integer)
        Debug.WriteLine(cnt)
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestCreateObjects()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            _new_objects.Clear()
            mgr.Cache.NewObjectManager = Me

            mgr.BeginTransaction()
            Try
                Using tracker As New ModificationsTracker
                    AddHandler tracker.Saver.ObjectRejected, AddressOf Objr
                    'AddHandler tracker.BeginRollback, AddressOf br

                    Dim t As Table1 = tracker.CreateNewKeyEntity(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsNotNull(mgr.Cache.NewObjectManager.GetNew(t.GetType, t.GetPKValues(Nothing)))

                    Dim t2 As Table2 = tracker.CreateNewKeyEntity(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsNotNull(mgr.Cache.NewObjectManager.GetNew(t2.GetType, t2.GetPKValues(Nothing)))

                    t2.Money = 1000

                    tracker.AcceptModifications()
                End Using
            Finally
                Assert.AreEqual(0, _new_objects.Count)

                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod(), ExpectedException(GetType(Worm.OrmManagerException))> _
    Public Sub TestUpdate()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            _new_objects.Clear()
            mgr.Cache.NewObjectManager = Me

            Dim tt As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            mgr.BeginTransaction()
            Try
                Using tracker As New ModificationsTracker
                    AddHandler tracker.Saver.ObjectRejected, AddressOf Objr
                    'AddHandler tracker.BeginRollback, AddressOf br

                    Dim t As Table1 = tracker.CreateNewKeyEntity(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsNotNull(mgr.Cache.NewObjectManager.GetNew(t.GetType, t.GetPKValues(Nothing)))

                    Dim t2 As Table2 = tracker.CreateNewKeyEntity(Of Table2)()
                    Assert.IsNotNull(t2)
                    Assert.IsNotNull(mgr.Cache.NewObjectManager.GetNew(t2.GetType, t2.GetPKValues(Nothing)))

                    t2.Money = 1000

                    tt.Code = 10

                    tracker.AcceptModifications()
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = Me

            Dim tt As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            mgr.BeginTransaction()
            Try
                Using tracker As New ModificationsTracker
                    Dim t As Table1 = tracker.CreateNewKeyEntity(Of Table1)()
                    t.CreatedAt = Now

                    Assert.IsNotNull(t)
                    Assert.IsNotNull(mgr.Cache.NewObjectManager.GetNew(t.GetType, t.GetPKValues(Nothing)))

                    tt.Code = 10

                    tracker.AcceptModifications()
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
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = Me

            Dim tt As Table1 = New QueryCmd().GetByID(Of Table1)(1, mgr)
            Dim tt2 As Table1 = New QueryCmd().GetByID(Of Table1)(2, mgr)

            Dim o As Object = New QueryCmd() _
                .Where(New Ctor(GetType(Table1)).prop("Code").eq(100)) _
                .SelectEntity(GetType(Table1), True) _
                .ToList(Of Table1)(mgr)

            o = New QueryCmd() _
                .Where(New Ctor(GetType(Table1)).prop("DT").eq(Now)) _
                .SelectEntity(GetType(Table1), True) _
                .ToList(Of Table1)(mgr)

            mgr.BeginTransaction()
            Try
                Using tracker As New ModificationsTracker
                    tracker.Saver.AcceptInBatch = True

                    tt.Code = 10
                    tt2.Code = 100

                    tracker.AcceptModifications()
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

    <TestMethod()> _
    Public Sub TestRecover()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            Dim tt As Table2 = New QueryCmd().GetByID(Of Table2)(1, mgr)
            Assert.AreEqual(Of Decimal)(1, tt.Money)

            mgr.BeginTransaction()
            Try
                Using tracker As New ModificationsTracker
                    AddHandler tracker.Saver.ObjectSavingError, AddressOf er
                    tt.Money = 200

                    tracker.AcceptModifications()
                End Using

                Assert.AreEqual(Of Decimal)(20, tt.Money)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestRecover2()
        Using mgr As OrmReadOnlyDBManager = TestManagerRS.CreateWriteManagerShared(New Worm.ObjectMappingEngine("1"))
            mgr.Cache.NewObjectManager = Me
            mgr.BeginTransaction()
            Try
                Dim tt As Table2
                Using tracker As New ModificationsTracker
                    AddHandler tracker.Saver.ObjectSavingError, AddressOf er

                    tt = tracker.CreateNewKeyEntity(Of Table2)()
                    tt.Money = 200
                    tt.Tbl = New QueryCmd().GetByID(Of Table1)(2, mgr)

                    tracker.AcceptModifications()
                End Using

                Assert.IsNotNull(tt)
                Assert.AreEqual(Of Decimal)(20, tt.Money)
                Assert.AreEqual(ObjectState.None, tt.InternalProperties.ObjectState)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Private Sub er(ByVal sender As ObjectListSaver, ByVal args As ObjectListSaver.SaveErrorEventArgs)
        Dim t As Table2 = CType(args.Entity, Table2)
        t.Money = 20
        args.FurtherAction = ObjectListSaver.FurtherActionEnum.Retry
    End Sub
End Class
