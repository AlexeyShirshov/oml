Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Orm.Meta

Namespace Orm

    <DefaultProperty("Item")> _
    Public Class AnonymousEntity
        Inherits Entity

        Private _props As New Dictionary(Of String, Object)

        Public Overrides Function GetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal oschema As Meta.IObjectSchemaBase) As Object
            Return _props(c.FieldName)
        End Function

        Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal schema As Meta.IObjectSchemaBase, ByVal value As Object)
            _props(c.FieldName) = value
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

        Public Overloads Sub Init(ByVal pk() As Meta.PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine, ByVal mgrIdentityString As String) Implements _ICachedEntity.Init

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

        Public Function SetLoaded(ByVal c As Meta.ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

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

        Public Sub CreateCopyForSaveNewEntry(ByVal pk() As Meta.PKDesc) Implements ICachedEntity.CreateCopyForSaveNewEntry

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

        Public Sub Load() Implements ICachedEntity.Load

        End Sub

        Public ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get

            End Get
        End Property

        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges

        End Sub

        Public Sub RejectRelationChanges() Implements ICachedEntity.RejectRelationChanges

        End Sub

        Public Sub RemoveFromCache(ByVal cache As Cache.CacheBase) Implements ICachedEntity.RemoveFromCache

        End Sub

        Public Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean Implements ICachedEntity.SaveChanges

        End Function

        Public Sub UpdateCache(ByVal oldObj As ICachedEntity) Implements _ICachedEntity.UpdateCache

        End Sub

        Public Sub SetSpecificSchema(ByVal mpe As ObjectMappingEngine) Implements _ICachedEntity.SetSpecificSchema

        End Sub
    End Class
End Namespace