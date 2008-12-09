Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Entities.Meta

Namespace Entities

    <DefaultProperty("Item")> _
    Public Class AnonymousEntity
        Inherits Entity

        Private _props As New Dictionary(Of String, Object)

        Public Overrides Function GetValue(ByVal pi As System.Reflection.PropertyInfo, _
            ByVal propertyAlias As String, ByVal oschema As Meta.IEntitySchema) As Object
            Return _props(propertyAlias)
        End Function

        Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, _
            ByVal propertyAlias As String, ByVal schema As Meta.IEntitySchema, ByVal value As Object)
            _props(propertyAlias) = value
        End Sub

        Default Public ReadOnly Property Item(ByVal field As String) As Object
            Get
                Return GetValue(field)
            End Get
        End Property
    End Class

    Public Class AnonymousCachedEntity
        Inherits AnonymousEntity
        Implements _ICachedEntity

        Private _pk() As String

        Public Event Added(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Added
        Public Event Deleted(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Deleted
        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements ICachedEntity.OriginalCopyRemoved
        Public Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs) Implements ICachedEntity.Saved
        Public Event Updated(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Updated

        Public Function CheckIsAllLoaded(ByVal schema As ObjectMappingEngine, ByVal loadedColumns As Integer) As Boolean Implements _ICachedEntity.CheckIsAllLoaded

        End Function

        Public Function ForseUpdate(ByVal c As Meta.ColumnAttribute) As Boolean Implements _ICachedEntity.ForseUpdate

        End Function

        Public Overloads Sub Init(ByVal pk() As Meta.PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine) Implements _ICachedEntity.Init

        End Sub

        Public ReadOnly Property IsPKLoaded() As Boolean Implements _ICachedEntity.IsPKLoaded
            Get

            End Get
        End Property

        Public Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntity.PKLoaded

        End Sub

        Public Sub RaiseCopyRemoved() Implements _ICachedEntity.RaiseCopyRemoved

        End Sub

        Public Sub RaiseSaved(ByVal sa As OrmManager.SaveAction) Implements _ICachedEntity.RaiseSaved

        End Sub

        Public Function Save(ByVal mc As OrmManager) As Boolean Implements _ICachedEntity.Save

        End Function

        Public Sub SetLoaded(ByVal value As Boolean) Implements _ICachedEntity.SetLoaded

        End Sub

        Public Function SetLoaded(ByVal fieldName As String, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

        End Function

        Public Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

        End Function

        Public ReadOnly Property UpdateCtx() As UpdateCtx Implements _ICachedEntity.UpdateCtx
            Get

            End Get
        End Property

        Public Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity Implements ICachedEntity.AcceptChanges

        End Function

        Public ReadOnly Property ChangeDescription() As String Implements ICachedEntity.ChangeDescription
            Get

            End Get
        End Property

        Public Sub CreateCopyForSaveNewEntry(ByVal mgr As OrmManager, ByVal pk() As Meta.PKDesc) Implements _ICachedEntity.CreateCopyForSaveNewEntry

        End Sub

        Public Function GetPKValues() As Meta.PKDesc() Implements ICachedEntity.GetPKValues
            Dim l As New List(Of PKDesc)
            For Each pk As String In _pk
                l.Add(New PKDesc(pk, GetValue(pk)))
            Next
            Return l.ToArray
        End Function

        Public ReadOnly Property HasChanges() As Boolean Implements ICachedEntity.HasChanges
            Get

            End Get
        End Property

        Public ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                Dim k As Integer
                For Each pk As String In _pk
                    k = k Xor GetValue(pk).GetHashCode
                Next
                Return k
            End Get
        End Property

        Public Overloads Sub Load() Implements ICachedEntity.Load

        End Sub

        Public ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get

            End Get
        End Property

        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges

        End Sub

        Public Sub RejectRelationChanges(ByVal mgr As OrmManager) Implements ICachedEntity.RejectRelationChanges

        End Sub

        Public Sub RemoveFromCache(ByVal cache As Cache.CacheBase) Implements ICachedEntity.RemoveFromCache

        End Sub

        Public Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean Implements ICachedEntity.SaveChanges

        End Function

        Public Sub UpdateCache(ByVal mgr As OrmManager, ByVal oldObj As ICachedEntity) Implements _ICachedEntity.UpdateCache

        End Sub

        'Public Sub SetSpecificSchema(ByVal mpe As ObjectMappingEngine) Implements _ICachedEntity.SetSpecificSchema

        'End Sub

        Public Function BeginAlter() As System.IDisposable Implements ICachedEntity.BeginAlter

        End Function

        Public Function BeginEdit() As System.IDisposable Implements ICachedEntity.BeginEdit

        End Function

        Public Sub CheckEditOrThrow() Implements ICachedEntity.CheckEditOrThrow

        End Sub

        Public Function Delete() As Boolean Implements ICachedEntity.Delete

        End Function

        Public Overloads Sub RejectChanges1(ByVal mgr As OrmManager) Implements _ICachedEntity.RejectChanges

        End Sub

        'Public Overloads ReadOnly Property OriginalCopy1(ByVal cache As Cache.CacheBase) As ICachedEntity Implements _ICachedEntity.OriginalCopy
        '    Get

        '    End Get
        'End Property

        Public Overloads Sub Load(ByVal mgr As OrmManager) Implements _ICachedEntity.Load

        End Sub
    End Class
End Namespace