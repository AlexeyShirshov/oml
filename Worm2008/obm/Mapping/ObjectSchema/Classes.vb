Namespace Orm.Meta

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

        Public Function GetAttributes(ByVal c As ColumnAttribute) As Field2DbRelations
            If _newattributes = Field2DbRelations.None Then
                Return c._behavior
            Else
                Return _newattributes
            End If
        End Function
    End Class

    Public Class M2MRelation
        Public ReadOnly Table As SourceFragment
        Public ReadOnly Column As String
        Public ReadOnly DeleteCascade As Boolean
        Public ReadOnly Mapping As System.Data.Common.DataTableMapping
        Public ReadOnly ConnectedType As Type
        Public ReadOnly Key As String

        Public Const RevKey As String = "xxx%rev$"
        Public Const DirKey As String = "xxx%direct$"

        Private _entityName As String
        Private _type As Type

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            _type = type
            Me.Table = table
            Me.Column = column
            Me.DeleteCascade = delete
            Me.Mapping = mapping
            Me.Key = key
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(generator, entityName, table, column, delete, mapping, DirKey)
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(generator, entityName, table, column, delete, mapping, GetKey(direct))
        End Sub

        Public Sub New(ByVal generator As ObjectMappingEngine, ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            _entityName = entityName
            _type = generator.GetTypeByEntityName(entityName)
            Me.Table = table
            Me.Column = column
            Me.DeleteCascade = delete
            Me.Mapping = mapping
            Me.Key = key
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(type, table, column, delete, mapping, DirKey)
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping)
            Key = GetKey(direct)
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub

        <Obsolete("Connected type is obsolete")> _
        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, _
            ByVal connectedType As Type, ByVal direct As Boolean)
            MyClass.New(type, table, column, delete, mapping, direct)
            Me.ConnectedType = connectedType
        End Sub

        Public ReadOnly Property EntityName() As String
            Get
                Return _entityName
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _type
            End Get
        End Property

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