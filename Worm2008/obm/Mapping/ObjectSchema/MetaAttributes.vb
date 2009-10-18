Imports System

Namespace Entities.Meta

    <AttributeUsage(AttributeTargets.Property, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    <Serializable()> _
    Public NotInheritable Class SourceFieldAttribute
        Inherits Attribute

        Friend Sub New()
        End Sub

        Public Sub New(ByVal columnExpression As String, ByVal primaryKeyPropertyAlias As String)
            Me.ColumnExpression = columnExpression
            PrimaryKey = primaryKeyPropertyAlias
        End Sub

        Private _columnName As String
        Public Property ColumnName() As String
            Get
                Return _columnName
            End Get
            Set(ByVal value As String)
                _columnName = value
            End Set
        End Property

        Private _columnExpression As String
        Public Property ColumnExpression() As String
            Get
                Return _columnExpression
            End Get
            Set(ByVal value As String)
                _columnExpression = value
            End Set
        End Property

        Private _pk As String
        Public Property PrimaryKey() As String
            Get
                Return _pk
            End Get
            Set(ByVal value As String)
                _pk = value
            End Set
        End Property

        Private _type As String
        Public Property SourceFieldType() As String
            Get
                Return _type
            End Get
            Set(ByVal value As String)
                _type = value
            End Set
        End Property

        Private _size As Integer
        Public Property SourceFieldSize() As Integer
            Get
                Return _size
            End Get
            Set(ByVal value As Integer)
                _size = value
            End Set
        End Property

        Private _isNotNullable As Boolean
        Public Property IsNullable() As Boolean
            Get
                Return Not _isNotNullable
            End Get
            Set(ByVal value As Boolean)
                _isNotNullable = Not value
            End Set
        End Property

        Private _version As String
        Public Property SchemaVersion() As String
            Get
                Return _version
            End Get
            Set(ByVal value As String)
                _version = value
            End Set
        End Property

    End Class

    <AttributeUsage(AttributeTargets.Property, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    <Serializable()> _
    Public NotInheritable Class EntityPropertyAttribute
        Inherits Attribute
        Implements IComparable(Of EntityPropertyAttribute), ICloneable

        Private _propertyAlias As String
        Private _behavior As Field2DbRelations

        Friend _pi As Reflection.PropertyInfo
        Friend _raw As Boolean

        Private _version As String

        Private _sf() As SourceFieldAttribute

        Public Sub New()
        End Sub

        'Public Sub New(ByVal ParamArray sourceFields() As SourceFieldAttribute)
        '    _sf = sourceFields
        'End Sub

        Public Sub New(ByVal behavior As Field2DbRelations)
            Me._behavior = behavior
        End Sub

        Public Sub New(ByVal columnExpression As String)
            _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
            Me._behavior = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal columnExpression As String, ByVal behavior As Field2DbRelations)
            _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
            Me._behavior = behavior
        End Sub

        Public Sub New(ByVal columnExpression As String, ByVal behavior As Field2DbRelations, ByVal propertyAlias As String)
            _propertyAlias = propertyAlias
            _behavior = behavior
            _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
        End Sub

        'Friend Sub New(ByVal propertAlias As String, ByVal columnExpression As String)
        '    _propertyAlias = propertAlias
        'End Sub

        'Friend Sub New(ByVal propertAlias As String, ByVal behavior As Field2DbRelations, ByVal columnExpression As String)
        '    _propertyAlias = propertAlias
        '    _behavior = behavior
        'End Sub

        Public Property SourceFields() As SourceFieldAttribute()
            Get
                Return _sf
            End Get
            Set(ByVal value As SourceFieldAttribute())
                _sf = value
            End Set
        End Property

        ''' <summary>
        ''' Имя поля класса, которое мапится на колонку в БД
        ''' </summary>
        Public Property PropertyAlias() As String
            Get
                Return _propertyAlias
            End Get
            Set(ByVal value As String)
                _propertyAlias = value
            End Set
        End Property

        Public Property Behavior() As Field2DbRelations
            Get
                Return _behavior
            End Get
            Set(ByVal value As Field2DbRelations)
                _behavior = value
            End Set
        End Property

        Public Property SchemaVersion() As String
            Get
                Return _version
            End Get
            Set(ByVal value As String)
                _version = value
            End Set
        End Property

        'Public Property Index() As Integer
        '    Get
        '        Return _idx
        '    End Get
        '    Set(ByVal value As Integer)
        '        _idx = value
        '    End Set
        'End Property

        Public Function CompareTo(ByVal other As EntityPropertyAttribute) As Integer Implements System.IComparable(Of EntityPropertyAttribute).CompareTo
            Return _propertyAlias.CompareTo(other._propertyAlias)
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, EntityPropertyAttribute))
        End Function

        Public Overloads Function Equals(ByVal obj As EntityPropertyAttribute) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return PropertyAlias.Equals(obj._propertyAlias)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _propertyAlias.GetHashCode
        End Function

        Public Property Column() As String
            Get
                If _sf.Length = 1 Then
                    Return _sf(0).ColumnExpression
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal value As String)
                If _sf.Length = 0 Then
                    ReDim _sf(0)
                ElseIf _sf.Length > 1 Then
                    Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
                End If
                _sf(0).ColumnExpression = value
            End Set
        End Property

        ''' <summary>
        ''' Column alias
        ''' </summary>
        ''' <value></value>
        ''' <returns>Column alias</returns>
        ''' <remarks></remarks>
        Public Property ColumnName() As String
            Get
                If _sf.Length = 1 Then
                    Return _sf(0).ColumnName
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal value As String)
                If _sf.Length = 0 Then
                    ReDim _sf(0)
                ElseIf _sf.Length > 1 Then
                    Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
                End If
                _sf(0).ColumnName = value
            End Set
        End Property

        Public Property DBType() As String
            Get
                If _sf.Length = 1 Then
                    Return _sf(0).SourceFieldType
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal value As String)
                If _sf.Length = 0 Then
                    ReDim _sf(0)
                ElseIf _sf.Length > 1 Then
                    Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
                End If
                _sf(0).SourceFieldType = value
            End Set
        End Property

        Public Property DBSize() As Integer
            Get
                If _sf.Length = 1 Then
                    Return _sf(0).SourceFieldSize
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal value As Integer)
                If _sf.Length = 0 Then
                    ReDim _sf(0)
                ElseIf _sf.Length > 1 Then
                    Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
                End If
                _sf(0).SourceFieldSize = value
            End Set
        End Property

        Public Property Nullable() As Boolean
            Get
                If _sf.Length = 1 Then
                    Return _sf(0).IsNullable
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal value As Boolean)
                If _sf.Length = 0 Then
                    ReDim _sf(0)
                ElseIf _sf.Length > 1 Then
                    Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
                End If
                _sf(0).IsNullable = value
            End Set
        End Property

        'Public ReadOnly Property SourceType() As DBType
        '    Get
        '        Return New DBType(_type, _sz, _n)
        '    End Get
        'End Property

        Public Function Clone() As EntityPropertyAttribute
            Return CType(_Clone(), EntityPropertyAttribute)
        End Function

        Private Function _Clone() As Object Implements System.ICloneable.Clone
            Dim c As New EntityPropertyAttribute(Behavior)
            'c._idx = _idx
            c._propertyAlias = _propertyAlias
            Dim sf(_sf.Length - 1) As SourceFieldAttribute
            Array.Copy(_sf, sf, _sf.Length)
            c._sf = sf
            Return c
        End Function
    End Class

    <FlagsAttribute(), CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217")> _
    Public Enum Field2DbRelations
        None = 0
        SyncInsert = 1
        SyncUpdate = 2
        [ReadOnly] = 4
        InsertDefault = 8
        RV = 16
        ''' <summary>
        ''' RV or [ReadOnly] or SyncUpdate or SyncInsert
        ''' </summary>
        ''' <remarks></remarks>
        RowVersion = 23
        PK = 32
        ''' <summary>
        ''' PK or SyncInsert or [ReadOnly]
        ''' </summary>
        ''' <remarks></remarks>
        PrimaryKey = 37
        NotSerialized = 64
        Factory = 128
    End Enum

    <AttributeUsage(AttributeTargets.Class, allowmultiple:=True, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    Public NotInheritable Class EntityAttribute
        Inherits Attribute

        Private _t As Type
        Private _v As String
        Private _entityName As String
        Private _table As String
        Private _schema As String
        Private _pk As String
        Private _rawProps As Boolean
        Private _notInheritBase As Boolean

        Public Sub New(ByVal tableSchema As String, ByVal tableName As String, ByVal version As String)
            _v = version
            _table = tableName
            _schema = tableSchema
        End Sub

        Public Sub New(ByVal schemaType As Type, ByVal version As String)
            _t = schemaType
            _v = version
        End Sub

        Public Sub New(ByVal version As String)
            _v = version
        End Sub

        Public Property InheritBaseTable() As Boolean
            Get
                Return Not _notInheritBase
            End Get
            Set(ByVal value As Boolean)
                _notInheritBase = Not value
            End Set
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property Version() As String
            Get
                Return _v
            End Get
        End Property

        Public Property EntityName() As String
            Get
                Return _entityName
            End Get
            Set(ByVal value As String)
                _entityName = value
            End Set
        End Property

        Public Property TableName() As String
            Get
                Return _table
            End Get
            Set(ByVal value As String)
                _table = value
            End Set
        End Property

        Public Property TableSchema() As String
            Get
                Return _schema
            End Get
            Set(ByVal value As String)
                _schema = value
            End Set
        End Property

        ''' <summary>
        ''' Рассматривать ли при генерации схемы свойства без атрибута <see cref="EntityPropertyAttribute" />
        ''' </summary>
        ''' <value>Значение свойства</value>
        ''' <returns>Если True, все свойства типа, к которому относится данный атрибут будут 
        ''' участвовать при генерации схемы</returns>
        ''' <remarks>По умолчанию False</remarks>
        Public Property RawProperties() As Boolean
            Get
                Return _rawProps
            End Get
            Set(ByVal value As Boolean)
                _rawProps = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.Property, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    <Serializable()> _
    Public NotInheritable Class JoinAttribute
        Inherits Attribute

        Public Sub New(ByVal tableSchema As String, ByVal tableName As String, _
                        ByVal joinTableSchema As String, ByVal joinTableName As String)
            _table = tableName
            _schema = tableSchema
            _joinTable = joinTableName
            _joinSchema = joinTableSchema
        End Sub

        Private _table As String
        Public Property TableName() As String
            Get
                Return _table
            End Get
            Set(ByVal value As String)
                _table = value
            End Set
        End Property

        Private _schema As String
        Public Property TableSchema() As String
            Get
                Return _schema
            End Get
            Set(ByVal value As String)
                _schema = value
            End Set
        End Property

        Private _joinTable As String
        Public Property JoinTableName() As String
            Get
                Return _joinTable
            End Get
            Set(ByVal value As String)
                _joinTable = value
            End Set
        End Property

        Private _joinSchema As String
        Public Property JoinTableSchema() As String
            Get
                Return _joinSchema
            End Get
            Set(ByVal value As String)
                _joinSchema = value
            End Set
        End Property

        Private _pks As String

        ''' <summary>
        ''' Список первичных ключей, разделенный через запятую
        ''' </summary>
        ''' <remarks>Порядок следования ключей должен быть таким же как и в <see cref="ForeignKeys" /></remarks>
        Public Property PrimaryKeys() As String
            Get
                Return _pks
            End Get
            Set(ByVal value As String)
                _pks = value
            End Set
        End Property

        Private _fks As String
        ''' <summary>
        ''' Список внешних ключей, разделенных через запятую
        ''' </summary>
        ''' <remarks>Порядок следования ключей должен быть таким же как и в <see cref="PrimaryKeys" /></remarks>
        Public Property ForeignKeys() As String
            Get
                Return _fks
            End Get
            Set(ByVal value As String)
                _fks = value
            End Set
        End Property

        Private _version As String
        Public Property SchemaVersion() As String
            Get
                Return _version
            End Get
            Set(ByVal value As String)
                _version = value
            End Set
        End Property

    End Class
End Namespace