Option Infer On

Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm.Entities.Meta
Imports Worm
Imports System.Linq
Imports System.IO

<TestClass>
Public Class TestHidden
    <TestMethod>
    Public Sub TestLoad()
        Dim mpe As New Worm.ObjectMappingEngine("Hidden")
        Dim dx As New DataContext(Function() TestManagerRS.CreateManagerShared(mpe))

        Dim o = dx.GetByID(Of Table2)(1, GetByIDOptions.GetAsIs)

        Assert.IsNotNull(o)
        Assert.IsFalse(o.InternalProperties.IsLoaded)
        Assert.IsNull(o.Blob)
        Assert.IsFalse(o.InternalProperties.IsLoaded)

        o.Load()
        Assert.IsTrue(o.InternalProperties.IsLoaded)
        Assert.IsNull(o.Blob)

    End Sub
    <TestMethod>
    Public Sub TestQuery()
        Dim mpe As New Worm.ObjectMappingEngine("Hidden")
        Dim dx As New DataContext(Function() TestManagerRS.CreateManagerShared(mpe))

        Dim o = dx.CreateQuery.ToList(Of Table2)()

        Assert.IsTrue(o.All(Function(it) Not it.InternalProperties.IsLoaded))
        Assert.IsTrue(o.All(Function(it) Not it.InternalProperties.IsPropertyLoaded("Blob")))

        o = dx.CreateQuery.SelectEntity(GetType(Table2), True).ToList(Of Table2)()

        Assert.IsTrue(o.All(Function(it) it.InternalProperties.IsLoaded))
        Assert.IsTrue(o.All(Function(it) Not it.InternalProperties.IsPropertyLoaded("Blob")))
    End Sub
    <TestMethod>
    Public Sub TestLoadProperty()
        Dim mpe As New Worm.ObjectMappingEngine("Hidden")
        Dim dx As New DataContext(Function() TestManagerRS.CreateManagerShared(mpe))

        Dim o = dx.GetByID(Of Table2)(1, GetByIDOptions.GetAsIs)

        Assert.IsNotNull(o)
        Assert.IsFalse(o.InternalProperties.IsLoaded)
        Assert.IsNull(o.Blob)
        Assert.IsFalse(o.InternalProperties.IsLoaded)

        o.Load()
        Assert.IsTrue(o.InternalProperties.IsLoaded)
        Assert.IsNull(o.Blob)
        Assert.IsFalse(o.InternalProperties.IsPropertyLoaded("Blob"))

        Using ms As New IO.MemoryStream
            o.LoadProperty("Blob", ms, 4)
            'ms.Seek(0, SeekOrigin.Begin)

            'Using br As New BinaryReader(ms)
            '    Dim l = br.ReadInt64
            '    Dim newDt = Date.FromBinary(l)

            '    Assert.AreNotEqual(dt, newDt)
            'End Using
            Dim arr = ms.ToArray

            Assert.AreEqual(6, arr.Length)
        End Using
    End Sub
End Class

Public Class Table2Hidden
    Inherits Table2Implementation
    Private _idx As OrmObjectIndex
    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            _idx = CType(MyBase.FieldColumnMap, OrmObjectIndex)
            If _idx IsNot Nothing Then
                _idx("Blob").Attributes = Field2DbRelations.Hidden
            End If
            Return _idx
        End Get
    End Property
End Class
