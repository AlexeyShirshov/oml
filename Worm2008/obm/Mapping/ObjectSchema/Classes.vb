Imports Worm.Query
Imports Worm.Criteria.Core

Namespace Entities.Meta

    Public Structure DBType
        Public Size As Integer
        Public Type As String
        Public Nullable As Boolean

        Public Sub New(ByVal type As String)
            Me.Type = type
        End Sub

        Public Sub New(ByVal type As String, ByVal size As Integer)
            Me.Type = type
            Me.Size = size
        End Sub

        Public Sub New(ByVal type As String, ByVal size As Integer, ByVal nullable As Boolean)
            Me.Type = type
            Me.Size = size
            Me.Nullable = nullable
        End Sub

        Public Sub New(ByVal type As String, ByVal nullable As Boolean)
            Me.Type = type
            Me.Nullable = nullable
        End Sub

        Public Function IsEmpty() As Boolean
            Return String.IsNullOrEmpty(Type)
        End Function
    End Structure

    Public Class MapField2Column
        Public ReadOnly _propertyAlias As String
        Public ReadOnly _columnName As String
        Public ReadOnly _tableName As SourceFragment
        Public ReadOnly DBType As DBType
        Public ReadOnly _newattributes As Field2DbRelations

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment)
            _propertyAlias = propertyAlias
            _columnName = columnName
            _tableName = tableName
            _newattributes = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations)
            _propertyAlias = propertyAlias
            _columnName = columnName
            _tableName = tableName
            _newattributes = newAttributes
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As DBType)
            MyClass.New(propertyAlias, columnName, tableName, newAttributes)
            Me.DBType = dbType
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String)
            MyClass.New(propertyAlias, columnName, tableName, newAttributes)
            Me.DBType = New DBType(dbType)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal size As Integer)
            MyClass.New(propertyAlias, columnName, tableName, newAttributes)
            Me.DBType = New DBType(dbType, size)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal size As Integer, ByVal nullable As Boolean)
            MyClass.New(propertyAlias, columnName, tableName, newAttributes)
            Me.DBType = New DBType(dbType, size, nullable)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnName As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal nullable As Boolean)
            MyClass.New(propertyAlias, columnName, tableName, newAttributes)
            Me.DBType = New DBType(dbType, nullable)
        End Sub

        Public Function GetAttributes(ByVal c As EntityPropertyAttribute) As Field2DbRelations
            If _newattributes = Field2DbRelations.None Then
                Return c.Behavior()
            Else
                Return _newattributes
            End If
        End Function
    End Class

    Public Class RelationDesc
        Public ReadOnly Column As String
        Public ReadOnly Key As String
        Private _eu As EntityUnion

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String)
            Me.Column = propertyAlias
            _eu = eu
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String, ByVal key As String)
            Me.Column = propertyAlias
            Me.Key = key
            _eu = eu
        End Sub

        Public Property Rel() As EntityUnion
            Get
                Return _eu
            End Get
            Friend Set(ByVal value As EntityUnion)
                _eu = value
            End Set
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return _eu.EntityName
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _eu.Type
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, RelationDesc))
        End Function

        Public Overloads Function Equals(ByVal obj As RelationDesc) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return Object.Equals(_eu, obj._eu) AndAlso M2MRelationDesc.CompareKeys(Key, obj.Key)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _eu.GetHashCode Xor If(String.IsNullOrEmpty(Key), 0, Key.GetHashCode)
        End Function
    End Class

    Public Class M2MRelationDesc
        Inherits RelationDesc
        Public ReadOnly Table As SourceFragment
        Public ReadOnly DeleteCascade As Boolean
        Public ReadOnly Mapping As System.Data.Common.DataTableMapping
        Public ReadOnly ConnectedType As Type

        Private _const() As EntityFilter

        Public Const RevKey As String = "xxx%rev$"
        Public Const DirKey As String = "xxx%direct$"

        Public Sub New(ByVal type As Type)
            MyBase.New(New EntityUnion(type), Nothing, Nothing)
        End Sub

        Public Sub New(ByVal type As Type, ByVal key As String)
            MyBase.New(New EntityUnion(type), Nothing, key)
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            MyBase.New(New EntityUnion(type), column, key)
            Me.Table = table
            Me.DeleteCascade = delete
            Me.Mapping = mapping
        End Sub

        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("direct parameter is obsolete")> _
        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
        End Sub

        <Obsolete("direct parameter is obsolete")> _
        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
        End Sub

        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            MyBase.New(New EntityUnion(entityName), column, key)
            Me.Table = table
            Me.DeleteCascade = delete
            Me.Mapping = mapping
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(type, table, column, delete, mapping, DirKey)
        End Sub

        <Obsolete("direct parameter is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping, GetKey(direct))
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("direct parameter is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, _
            ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping, GetKey(direct))
            Me.ConnectedType = connectedType
        End Sub

        Public ReadOnly Property non_direct() As Boolean
            Get
                Return Key = RevKey
            End Get
        End Property

        Public Shared Function GetKey(ByVal direct As Boolean) As String
            If direct Then
                Return DirKey
            Else
                Return RevKey
            End If
        End Function

        Public Shared Function GetRevKey(ByVal key As String) As String
            If key = DirKey Then
                Return RevKey
            ElseIf key = RevKey Then
                Return DirKey
            Else
                Return key
            End If
        End Function

        Public Shared Function CompareKeys(ByVal key1 As String, ByVal key2 As String) As Boolean
            If key1 Is Nothing AndAlso key2 Is Nothing Then
                Return True
            Else
                If (key1 Is Nothing AndAlso key2 = DirKey) OrElse _
                    (key2 Is Nothing AndAlso key1 = DirKey) Then
                    Return True
                Else
                    Return String.Equals(key1, key2)
                End If
            End If
        End Function
    End Class

    ''' <summary>
    ''' Индексированая по полю <see cref="MapField2Column._propertyAlias"/> колекция объектов типа <see cref="MapField2Column"/>
    ''' </summary>
    ''' <remarks>
    ''' Наследник абстрактного класс <see cref="Collections.IndexedCollection(Of string, MapField2Column)"/>, реализующий метод <see cref="Collections.IndexedCollection(Of string, MapField2Column).GetKeyForItem" />
    ''' </remarks>
    Public Class OrmObjectIndex
        Inherits Collections.IndexedCollection(Of String, MapField2Column)

        ''' <summary>
        ''' Возвращает ключ коллекции MapField2Column
        ''' </summary>
        ''' <param name="item">Элемент коллекции</param>
        ''' <returns>Возвращает <see cref="MapField2Column._propertyAlias"/></returns>
        ''' <remarks>Используется при индексации коллекции</remarks>
        Protected Overrides Function GetKeyForItem(ByVal item As MapField2Column) As String
            Return item._propertyAlias
        End Function
    End Class
End Namespace