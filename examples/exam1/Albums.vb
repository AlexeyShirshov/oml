'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:2.0.50727.3074
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On


'This file was generated by Worm.CodeGen.CodeGenerator v1.0.3463.31031 application(Worm.CodeGen.Core v1.0.3463.31013).
'
'By user 'Alex' at 26.06.2009 17:07:32.
'
'
Namespace test2
	
	<Worm.Entities.Meta.EntityAttribute(GetType(test2.Albums.AlbumsSchemaDef), "1", EntityName:=test2.Albums.Descriptor.EntityName),  _
	 System.SerializableAttribute()>  _
	Partial Public Class Albums
		Inherits Worm.Entities.KeyEntity
		Implements Worm.Entities.IOptimizedValues
		
		#Region "Private Fields"
		Private _iD As Integer
		
		Private _name As String
		
		Private _release_dt As System.Nullable(Of Date)
		#End Region
		
		#Region "Constructors"
		Public Sub New()
			MyBase.New
			Me._dontRaisePropertyChange = true
		End Sub
		
		Public Sub New(ByVal id As Integer, ByVal cache As Worm.Cache.CacheBase, ByVal schema As Worm.ObjectMappingEngine)
			MyBase.New
			MyBase.Init(id, cache, schema)
			Me._dontRaisePropertyChange = true
		End Sub
		#End Region
		
		#Region "Properties"
		Public Overrides Property Identifier() As Object
			Get
				Return Me._iD
			End Get
			Set
				Me._iD = CType(System.Convert.ChangeType(value, GetType(Integer)),Integer)
			End Set
		End Property
		
		<Worm.Entities.Meta.EntityPropertyAttribute(PropertyAlias:=test2.Albums.Properties.ID)>  _
		Public Overridable Property ID() As Integer
			Get
Using Me.Read(test2.Albums.Properties.ID)
    Return Me._iD
End Using

			End Get
			Set
Using Me.Write(test2.Albums.Properties.ID)
    Me._iD = value
End Using

			End Set
		End Property
		
		<Worm.Entities.Meta.EntityPropertyAttribute(PropertyAlias:=test2.Albums.Properties.Name)>  _
		Public Overridable Property Name() As String
			Get
Using Me.Read(test2.Albums.Properties.Name)
    Return Me._name
End Using

			End Get
			Set
Using Me.Write(test2.Albums.Properties.Name)
    Me._name = value
End Using

			End Set
		End Property
		
		<Worm.Entities.Meta.EntityPropertyAttribute(PropertyAlias:=test2.Albums.Properties.Release_dt)>  _
		Public Overridable Property Release_dt() As System.Nullable(Of Date)
			Get
Using Me.Read(test2.Albums.Properties.Release_dt)
    Return Me._release_dt
End Using

			End Get
			Set
Using Me.Write(test2.Albums.Properties.Release_dt)
    Me._release_dt = value
End Using

			End Set
		End Property
		#End Region
		
		#Region "Static members"
		Public Shared Function CreateAlias() As Albums.AlbumsAlias
			Return New test2.Albums.AlbumsAlias
		End Function
		
		Public Shared Function GetAlias(ByVal objectAlias As Worm.Query.QueryAlias) As Albums.AlbumsProperties
			Return New test2.Albums.AlbumsProperties(objectAlias)
		End Function
		#End Region
		
		#Region "Base type related members"
		Protected Overrides Sub CopyProperties(ByVal from As Worm.Entities._IEntity, ByVal [to] As Worm.Entities._IEntity, ByVal mgr As Worm.OrmManager, ByVal oschema As Worm.Entities.Meta.IEntitySchema)
			CType([to],test2.Albums)._iD = CType(from,test2.Albums)._iD
			CType([to],test2.Albums)._name = CType(from,test2.Albums)._name
			CType([to],test2.Albums)._release_dt = CType(from,test2.Albums)._release_dt
		End Sub
		
		Public Overridable Sub SetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema, ByVal value As Object) Implements Worm.Entities.IOptimizedValues.SetValueOptimized
			Dim fieldName As String = propertyAlias
			If test2.Albums.Properties.ID.Equals(fieldName) Then
				Me._iD = CType(System.Convert.ChangeType(value, GetType(Integer)),Integer)
				Return
			End If
			If test2.Albums.Properties.Name.Equals(fieldName) Then
				Me._name = CType(value,String)
				Return
			End If
			If test2.Albums.Properties.Release_dt.Equals(fieldName) Then
				Dim iconvVal As System.IConvertible = TryCast(value, System.IConvertible)
				If (iconvVal Is Nothing) Then
					Me._release_dt = CType(value,System.Nullable(Of Date))
				Else
					Me._release_dt = iconvVal.ToDateTime(System.Threading.Thread.CurrentThread.CurrentCulture)
				End If
				Return
			End If
		End Sub
		
		Public Overridable Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
			If test2.Albums.Properties.ID.Equals(propertyAlias) Then
				Return Me._iD
			End If
			If test2.Albums.Properties.Name.Equals(propertyAlias) Then
				Return Me._name
			End If
			If test2.Albums.Properties.Release_dt.Equals(propertyAlias) Then
				Return Me._release_dt
			End If
			Return Me.GetMappingEngine.GetPropertyValue(Nothing, propertyAlias)
		End Function
		
		Protected Overrides Sub SetPK(ByVal pks() As Worm.Entities.Meta.PKDesc, ByVal mpe As Worm.ObjectMappingEngine)
			Me._iD = CType(System.Convert.ChangeType(pks(0).Value, GetType(Integer)),Integer)
		End Sub
		
		Public Overrides Function GetPKValues() As Worm.Entities.Meta.PKDesc()
			Return New Worm.Entities.Meta.PKDesc() {New Worm.Entities.Meta.PKDesc("ID", Me._iD)}
		End Function
		#End Region
		
		#Region "Nested Types"
		Partial Public Class AlbumsSchemaDef
			Implements Worm.Entities.Meta.IEntitySchemaBase, Worm.Entities.Meta.ISchemaInit
			
			#Region "Private Fields"
			Private _table As Worm.Entities.Meta.SourceFragment
			
			Private _tableLock As Object = New Object
			
			Private _idx As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column)
			
			Private _forIdxLock As Object = New Object
			
			Protected _schema As Worm.ObjectMappingEngine
			
			Protected _entityType As System.Type
			#End Region
			
			#Region "Properties"
			Public Overridable ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
				Get
					If (Me._table Is Nothing) Then
SyncLock Me._tableLock
    If (Me._table Is Nothing) Then
        Me._table = New Worm.Entities.Meta.SourceFragment("dbo", "Albums")
    End If
End SyncLock

					End If
					Return Me._table
				End Get
			End Property
			#End Region
			
			Public Overridable Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column) Implements Worm.Entities.Meta.IPropertyMap.GetFieldColumnMap
				If (Me._idx Is Nothing) Then
SyncLock Me._forIdxLock
    If (Me._idx Is Nothing) Then
        Dim idx As Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column) = New Worm.Entities.Meta.OrmObjectIndex
        idx("ID") = New Worm.Entities.Meta.MapField2Column("ID", "id", Me.Table, Worm.Entities.Meta.Field2DbRelations.PK)
        idx("Name") = New Worm.Entities.Meta.MapField2Column("Name", "name", Me.Table, Worm.Entities.Meta.Field2DbRelations.None)
        idx("Release_dt") = New Worm.Entities.Meta.MapField2Column("Release_dt", "release_dt", Me.Table, Worm.Entities.Meta.Field2DbRelations.None)
        Me._idx = idx
    End If
End SyncLock

				End If
				Return Me._idx
			End Function
			
			Public Overridable Function ChangeValueType(ByVal c As Worm.Entities.Meta.EntityPropertyAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements Worm.Entities.Meta.IEntitySchemaBase.ChangeValueType
				If (((c.Behavior And Worm.Entities.Meta.Field2DbRelations.InsertDefault)  _
							= Worm.Entities.Meta.Field2DbRelations.InsertDefault)  _
							AndAlso ((value Is Nothing)  _
							OrElse System.Activator.CreateInstance(value.GetType).Equals(value))) Then
					newvalue = System.DBNull.Value
					Return true
				End If
				newvalue = value
				Return false
			End Function
			
			Public Overridable Function GetSuppressedFields() As String() Implements Worm.Entities.Meta.IEntitySchemaBase.GetSuppressedFields
				Return New String(-1) {}
			End Function
			
			Public Sub GetSchema(ByVal schema As Worm.ObjectMappingEngine, ByVal t As System.Type) Implements Worm.Entities.Meta.ISchemaInit.GetSchema
				Me._schema = schema
				Me._entityType = t
			End Sub
			
			#Region "Nested Types"
			Public Enum TablesLink
				
				tbldboAlbums = 0
			End Enum
			#End Region
		End Class
		
		'''<summary>
		'''Алиасы свойств сущностей испльзуемые в объектной модели.
		'''</summary>
		Partial Public Class Properties
			
			#Region "Private Fields"
			Public Const ID As String = "ID"
			
			Public Const Name As String = "Name"
			
			Public Const Release_dt As String = "Release_dt"
			#End Region
			
			#Region "Constructors"
			Protected Sub New()
				MyBase.New
			End Sub
			#End Region
		End Class
		
		'''<summary>
		'''Ссылки на поля сущностей.
		'''</summary>
		Partial Public Class props
			
			#Region "Static members"
			Private Shared _iD As Worm.Query.ObjectProperty = New Worm.Query.ObjectProperty(test2.Albums.Descriptor.EntityName, test2.Albums.Properties.ID)
			
			Private Shared _name As Worm.Query.ObjectProperty = New Worm.Query.ObjectProperty(test2.Albums.Descriptor.EntityName, test2.Albums.Properties.Name)
			
			Private Shared _release_dt As Worm.Query.ObjectProperty = New Worm.Query.ObjectProperty(test2.Albums.Descriptor.EntityName, test2.Albums.Properties.Release_dt)
			
			#Region "Constructors"
			Protected Sub New()
				MyBase.New
			End Sub
			#End Region
			
			Public Shared ReadOnly Property ID() As Worm.Query.ObjectProperty
				Get
					Return test2.Albums.props._iD
				End Get
			End Property
			
			Public Shared ReadOnly Property Name() As Worm.Query.ObjectProperty
				Get
					Return test2.Albums.props._name
				End Get
			End Property
			
			Public Shared ReadOnly Property Release_dt() As Worm.Query.ObjectProperty
				Get
					Return test2.Albums.props._release_dt
				End Get
			End Property
			#End Region
		End Class
		
		'''<summary>
		'''Описатель сущности.
		'''</summary>
		Partial Public Class Descriptor
			
			#Region "Private Fields"
			'''<summary>
			'''Имя сущности в объектной модели.
			'''</summary>
			Public Const EntityName As String = "Albums"
			#End Region
			
			#Region "Constructors"
			Protected Sub New()
				MyBase.New
			End Sub
			#End Region
		End Class
		
		Public Class AlbumsAlias
			Inherits Worm.Query.QueryAlias
			
			#Region "Constructors"
			Public Sub New()
				MyBase.New(test2.Albums.Descriptor.EntityName)
			End Sub
			#End Region
			
			#Region "Properties"
			Public ReadOnly Property ID() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me, test2.Albums.Properties.ID)
				End Get
			End Property
			
			Public ReadOnly Property Name() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me, test2.Albums.Properties.Name)
				End Get
			End Property
			
			Public ReadOnly Property Release_dt() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me, test2.Albums.Properties.Release_dt)
				End Get
			End Property
			#End Region
		End Class
		
		Public Class AlbumsProperties
			
			#Region "Private Fields"
			Private _objectAlias As Worm.Query.QueryAlias
			#End Region
			
Public Shared Widening Operator CType(ByVal entityAlias As Albums.AlbumsProperties) As Worm.Query.QueryAlias
    Return entityAlias._objectAlias
End Operator

			
			#Region "Constructors"
			Public Sub New(ByVal objectAlias As Worm.Query.QueryAlias)
				MyBase.New
				Me._objectAlias = objectAlias
			End Sub
			#End Region
			
			#Region "Properties"
			Public ReadOnly Property ID() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me._objectAlias, test2.Albums.Properties.ID)
				End Get
			End Property
			
			Public ReadOnly Property Name() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me._objectAlias, test2.Albums.Properties.Name)
				End Get
			End Property
			
			Public ReadOnly Property Release_dt() As Worm.Query.ObjectProperty
				Get
					Return New Worm.Query.ObjectProperty(Me._objectAlias, test2.Albums.Properties.Release_dt)
				End Get
			End Property
			#End Region
		End Class
		#End Region
	End Class
End Namespace
