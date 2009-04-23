Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Entities.Meta

Namespace Entities

    <Serializable()> _
    <DefaultProperty("Item")> _
    Public Class AnonymousEntity
        Inherits Entity
        Implements IOptimizedValues, ICustomTypeDescriptor

        '<TypeConverter(GetType(AnonymousEntity.AnonymTypeConverter))> _
        'Public Class AnonymTypeConverter
        '    Inherits TypeConverter

        'End Class
        Protected Class PropDesc
            Inherits PropertyDescriptor

            Private _pi As Reflection.PropertyInfo
            Private _name As String
            Private _t As Type

            Public Sub New(ByVal pi As Reflection.PropertyInfo)
                MyBase.New(pi.Name, Nothing)
            End Sub

            Public Sub New(ByVal name As String, ByVal t As Type)
                MyBase.New(name, Nothing)
                _name = name
                _t = t
            End Sub

            Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
                Return False
            End Function

            Public Overrides ReadOnly Property ComponentType() As System.Type
                Get
                    Return GetType(AnonymousEntity)
                End Get
            End Property

            Public Overrides Function GetValue(ByVal component As Object) As Object
                If _pi Is Nothing Then
                    Return CType(component, AnonymousEntity)(_name)
                Else
                    Return _pi.GetValue(component, Nothing)
                End If
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    If _pi Is Nothing Then
                        Return _t
                    Else
                        Return _pi.PropertyType
                    End If
                End Get
            End Property

            Public Overrides Sub ResetValue(ByVal component As Object)

            End Sub

            Public Overrides Sub SetValue(ByVal component As Object, ByVal value As Object)
                If _pi Is Nothing Then
                    CType(component, AnonymousEntity)(_name) = value
                Else
                    _pi.SetValue(component, value, Nothing)
                End If
            End Sub

            Public Overrides Function ShouldSerializeValue(ByVal component As Object) As Boolean
                Return False
            End Function
        End Class

        Private _props As New Dictionary(Of String, Object)

        Public Overridable Overloads Function GetValue( _
            ByVal propertyAlias As String, ByVal oschema As Meta.IEntitySchema) As Object Implements IOptimizedValues.GetValueOptimized
            Return _props(propertyAlias)
        End Function

        Public Overridable Sub SetValue( _
            ByVal propertyAlias As String, ByVal oschema As Meta.IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
            _props(propertyAlias) = value
        End Sub

        Default Public Property Item(ByVal field As String) As Object
            Get
                Return GetValue(field, Nothing)
            End Get
            Set(ByVal value As Object)
                SetValue(field, Nothing, value)
            End Set
        End Property

#Region " ICustomTypeDescriptor "
        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return New AttributeCollection(Nothing)
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return Nothing
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return Nothing
        End Function

        Public Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return Nothing
        End Function

        Public Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return Nothing
        End Function

        Public Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return New PropDesc(Me.GetType.GetProperty("Item"))
        End Function

        Public Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return Nothing
        End Function

        Public Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(Nothing)
        End Function

        Public Function GetEvents(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(Nothing)
        End Function

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return GetProperties(Nothing)
        End Function

        Private _pdc As PropertyDescriptorCollection

        Public Function GetProperties(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            If _pdc Is Nothing Then
                Dim props(_props.Count - 1) As PropertyDescriptor
                Dim i As Integer = 0
                For Each kv As KeyValuePair(Of String, Object) In _props
                    Dim t As Type = Nothing
                    Dim v As Object = kv.Value
                    If v IsNot Nothing Then
                        t = v.GetType
                    End If
                    props(i) = New PropDesc(kv.Key, t)
                    i += 1
                Next
                _pdc = New PropertyDescriptorCollection(props)
            End If

            Return _pdc
        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function

#End Region

    End Class

    <Serializable()> _
    Public Class AnonymousCachedEntity
        Inherits AnonymousEntity
        Implements _ICachedEntity

        Friend _pk() As String
        Private _hasPK As Boolean

        Public Event Added(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Added
        Public Event Deleted(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Deleted
        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements ICachedEntity.OriginalCopyRemoved
        Public Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs) Implements ICachedEntity.Saved
        Public Event Updated(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Updated

        Public Function CheckIsAllLoaded(ByVal schema As ObjectMappingEngine, ByVal loadedColumns As Integer, ByVal arr As Generic.List(Of EntityPropertyAttribute)) As Boolean Implements _ICachedEntity.CheckIsAllLoaded

        End Function

        Public Function ForseUpdate(ByVal c As Meta.EntityPropertyAttribute) As Boolean Implements _ICachedEntity.ForseUpdate

        End Function

        Public Overloads Sub Init(ByVal pk() As Meta.PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine) Implements _ICachedEntity.Init

        End Sub

        Public ReadOnly Property IsPKLoaded() As Boolean Implements _ICachedEntity.IsPKLoaded
            Get
                Return _hasPK
            End Get
        End Property

        Public Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntity.PKLoaded
            If _pk Is Nothing Then
                Throw New OrmObjectException("PK is not loaded")
            End If
            _hasPK = True
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

        Public Function SetLoaded(ByVal c As EntityPropertyAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

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
            'Dim schema As Worm.ObjectMappingEngine = MappingEngine
            'Dim oschema As IEntitySchema = schema.GetEntitySchema(Me.GetType)
            Dim l As New List(Of PKDesc)
            For Each pk As String In _pk
                l.Add(New PKDesc(pk, Me(pk)))
            Next
            Return l.ToArray
        End Function

        Public ReadOnly Property HasChanges() As Boolean Implements ICachedEntity.HasChanges
            Get

            End Get
        End Property

        Public ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                'Dim schema As Worm.ObjectMappingEngine = MappingEngine
                'Dim oschema As IEntitySchema = schema.GetEntitySchema(Me.GetType)
                Dim k As Integer
                For Each pk As String In _pk
                    k = k Xor Me(pk).GetHashCode
                Next
                Return k
            End Get
        End Property

        Public Overloads Sub Load(ByVal propertyAlias As String) Implements ICachedEntity.Load

        End Sub

        Public ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get

            End Get
        End Property

        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges

        End Sub

        Public Sub RejectRelationChanges(ByVal mgr As OrmManager) Implements ICachedEntity.RejectRelationChanges

        End Sub

        Public Sub RemoveFromCache(ByVal cache As Cache.CacheBase) Implements ICachedEntity.RemoveOriginalCopy

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

        Public Function Delete(ByVal mgr As OrmManager) As Boolean Implements ICachedEntity.Delete

        End Function

        Private Overloads Sub _RejectChanges(ByVal mgr As OrmManager) Implements _ICachedEntity.RejectChanges

        End Sub

        'Public Overloads ReadOnly Property OriginalCopy1(ByVal cache As Cache.CacheBase) As ICachedEntity Implements _ICachedEntity.OriginalCopy
        '    Get

        '    End Get
        'End Property

        Public Overloads Sub Load(ByVal mgr As OrmManager, Optional ByVal propertyAlias As String = Nothing) Implements _ICachedEntity.Load

        End Sub
    End Class
End Namespace