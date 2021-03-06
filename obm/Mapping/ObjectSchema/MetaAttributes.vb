Imports System

Namespace Entities.Meta

    <AttributeUsage(AttributeTargets.Property, AllowMultiple:=True, Inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")>
    <Serializable()> _
    Public NotInheritable Class SourceFieldAttribute
        Inherits Attribute
        Implements IVersionable
        Private _version As String
        Private _verOper As SchemaVersionOperatorEnum
        Private _feature As String
        Private _columnName As String
        Private _columnExpression As String
        Private _type As String
        Private _size As Integer
        Private _isNotNullable As Boolean

        Friend Sub New()
        End Sub

        Public Sub New(ByVal column As String)
            Me.ColumnExpression = column
        End Sub

        Public Property ColumnAlias() As String
            Get
                Return _columnName
            End Get
            Set(ByVal value As String)
                _columnName = value
            End Set
        End Property

        ''' <summary>
        ''' Column name or expression 
        ''' </summary>
        ''' <returns></returns>
        Public Property ColumnExpression() As String
            Get
                Return _columnExpression
            End Get
            Set(ByVal value As String)
                _columnExpression = value
            End Set
        End Property

        'Private _pk As String
        'Public Property PrimaryKey() As String
        '    Get
        '        Return _pk
        '    End Get
        '    Set(ByVal value As String)
        '        _pk = value
        '    End Set
        'End Property

        Public Property SourceFieldType() As String
            Get
                Return _type
            End Get
            Set(ByVal value As String)
                _type = value
            End Set
        End Property

        Public Property SourceFieldSize() As Integer
            Get
                Return _size
            End Get
            Set(ByVal value As Integer)
                _size = value
            End Set
        End Property

        Public Property IsNullable() As Boolean
            Get
                Return Not _isNotNullable
            End Get
            Set(ByVal value As Boolean)
                _isNotNullable = Not value
            End Set
        End Property

        Public Property SchemaVersion() As String Implements IVersionable.SchemaVersion
            Get
                Return _version
            End Get
            Set(ByVal value As String)
                _version = value
            End Set
        End Property
        Public Property SchemaVersionOperator() As SchemaVersionOperatorEnum Implements IVersionable.SchemaVersionOperator
            Get
                Return _verOper
            End Get
            Set(ByVal value As SchemaVersionOperatorEnum)
                _verOper = value
            End Set
        End Property
        Public Property Feature As String Implements IVersionable.Feature
            Get
                Return _feature
            End Get
            Set(value As String)
                _feature = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.Property, Inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")>
    <Serializable()>
    Public NotInheritable Class EntityPropertyAttribute
        Inherits Attribute
        Implements IComparable(Of EntityPropertyAttribute), ICloneable, IVersionable

        Private _propertyAlias As String
        Private _behavior As Field2DbRelations

        Friend _pi As Reflection.PropertyInfo
        Friend _raw As Boolean

        Private _version As String
        Private _verOper As SchemaVersionOperatorEnum
        Private _feature As String

        Private _sf(-1) As SourceFieldAttribute

        Public Sub New()
        End Sub

        'Public Sub New(ByVal ParamArray sourceFields() As SourceFieldAttribute)
        '    _sf = sourceFields
        'End Sub

        Public Sub New(ByVal behavior As Field2DbRelations)
            Me._behavior = behavior
        End Sub

        'Public Sub New(ByVal columnExpression As String)
        '    _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
        '    Me._behavior = Field2DbRelations.None
        'End Sub

        'Public Sub New(ByVal columnExpression As String, ByVal behavior As Field2DbRelations)
        '    _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
        '    Me._behavior = behavior
        'End Sub

        'Public Sub New(ByVal columnExpression As String, ByVal behavior As Field2DbRelations, ByVal propertyAlias As String)
        '    _propertyAlias = propertyAlias
        '    _behavior = behavior
        '    _sf = New SourceFieldAttribute() {New SourceFieldAttribute With {.ColumnExpression = columnExpression}}
        'End Sub
        Public Sub New(ByVal propertyAlias As String)
            _propertyAlias = propertyAlias
        End Sub
        Public Sub New(ByVal behavior As Field2DbRelations, ByVal propertyAlias As String)
            _propertyAlias = propertyAlias
            _behavior = behavior
        End Sub
        'Public Sub New(ByVal behavior As Field2DbRelations, ByVal propertyAlias As String, sourceFields() As SourceFieldAttribute)
        '    _propertyAlias = propertyAlias
        '    _behavior = behavior
        '    _sf = sourceFields
        'End Sub
        Public Sub New(ByVal behavior As Field2DbRelations, ByVal propertyAlias As String, column As String)
            _propertyAlias = propertyAlias
            _behavior = behavior
            _sf = {New SourceFieldAttribute(column)}
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
        ''' ��� ���� ������, ������� ������� �� ������� � ��
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

        Public Property SchemaVersion() As String Implements IVersionable.SchemaVersion
            Get
                Return _version
            End Get
            Set(ByVal value As String)
                _version = value
            End Set
        End Property

        Public Property SchemaVersionOperator() As SchemaVersionOperatorEnum Implements IVersionable.SchemaVersionOperator
            Get
                Return _verOper
            End Get
            Set(ByVal value As SchemaVersionOperatorEnum)
                _verOper = value
            End Set
        End Property

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

        'Public Property Column() As String
        '    Get
        '        If _sf.Length = 1 Then
        '            Return _sf(0).ColumnExpression
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        '    Set(ByVal value As String)
        '        If _sf.Length = 0 Then
        '            ReDim _sf(0)
        '            _sf(0) = New SourceFieldAttribute
        '        ElseIf _sf.Length > 1 Then
        '            Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
        '        End If
        '        _sf(0).ColumnExpression = value
        '    End Set
        'End Property

        '''' <summary>
        '''' Column alias
        '''' </summary>
        '''' <value></value>
        '''' <returns>Column alias</returns>
        '''' <remarks></remarks>
        'Public Property ColumnName() As String
        '    Get
        '        If _sf.Length = 1 Then
        '            Return _sf(0).ColumnAlias
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        '    Set(ByVal value As String)
        '        If _sf.Length = 0 Then
        '            ReDim _sf(0)
        '        ElseIf _sf.Length > 1 Then
        '            Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
        '        End If
        '        _sf(0).ColumnAlias = value
        '    End Set
        'End Property

        'Public Property DBType() As String
        '    Get
        '        If _sf.Length = 1 Then
        '            Return _sf(0).SourceFieldType
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        '    Set(ByVal value As String)
        '        If _sf.Length = 0 Then
        '            ReDim _sf(0)
        '        ElseIf _sf.Length > 1 Then
        '            Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
        '        End If
        '        _sf(0).SourceFieldType = value
        '    End Set
        'End Property

        'Public Property DBSize() As Integer
        '    Get
        '        If _sf.Length = 1 Then
        '            Return _sf(0).SourceFieldSize
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        '    Set(ByVal value As Integer)
        '        If _sf.Length = 0 Then
        '            ReDim _sf(0)
        '        ElseIf _sf.Length > 1 Then
        '            Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
        '        End If
        '        _sf(0).SourceFieldSize = value
        '    End Set
        'End Property

        'Public Property Nullable() As Boolean
        '    Get
        '        If _sf.Length = 1 Then
        '            Return _sf(0).IsNullable
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        '    Set(ByVal value As Boolean)
        '        If _sf.Length = 0 Then
        '            ReDim _sf(0)
        '        ElseIf _sf.Length > 1 Then
        '            Throw New NotSupportedException("Use SourceFieldAttribute to add one more Column")
        '        End If
        '        _sf(0).IsNullable = value
        '    End Set
        'End Property

        Public Property Feature As String Implements IVersionable.Feature
            Get
                Return _feature
            End Get
            Set(value As String)
                _feature = value
            End Set
        End Property

        Public Function Clone() As EntityPropertyAttribute
            Return CType(_Clone(), EntityPropertyAttribute)
        End Function

        Private Function _Clone() As Object Implements System.ICloneable.Clone
            'c._idx = _idx
            Dim c As New EntityPropertyAttribute(Behavior) With {
                ._propertyAlias = _propertyAlias
            }
            Dim sf(_sf.Length - 1) As SourceFieldAttribute
            Array.Copy(_sf, sf, _sf.Length)
            c._sf = sf
            Return c
        End Function
        'Friend ReadOnly Property HasColumns As Boolean
        '    Get
        '        Return _sf.Length > 0
        '    End Get
        'End Property

    End Class

    <FlagsAttribute(), CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217")> _
    Public Enum SchemaVersionOperatorEnum
        Equal
        GreaterEqual
        LessThan
    End Enum

    <FlagsAttribute(), CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217")> _
    Public Enum Field2DbRelations
        None = 0
        SyncInsert = 1
        SyncUpdate = 2
        [ReadOnly] = 4
        InsertDefault = 8
        RV = 16
        PK = 32
        NotSerialized = 64
        Factory = 128
        Hidden = CInt(2 ^ 8)
        Identity = CInt(2 ^ 9)

#Region " Compound "
        ''' <summary>
        ''' RV or [ReadOnly] or SyncUpdate or SyncInsert
        ''' </summary>
        ''' <remarks></remarks>
        RowVersion = RV Or [ReadOnly] Or SyncUpdate Or SyncInsert
        ''' <summary>
        ''' PK or SyncInsert or [ReadOnly] or Identity
        ''' </summary>
        ''' <remarks></remarks>
        PrimaryKey = PK Or SyncInsert Or [ReadOnly] Or Identity
        Virtual = Hidden Or [ReadOnly] Or InsertDefault
        PartOfPK = [ReadOnly] Or SyncInsert
#End Region
    End Enum

    <AttributeUsage(AttributeTargets.Class, allowmultiple:=True, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    Public NotInheritable Class EntityAttribute
        Inherits Attribute
        Implements IVersionable

        Private ReadOnly _t As Type
        Private _entityName As String
        Private _table As String
        Private _schema As String
        'Private _pk As String()
        Private _rawProps As Boolean
        Private _notInheritBase As Boolean


        Friend _tbl As SourceFragment
        Private _v As String
        Private _feature As String
        Private _verOper As SchemaVersionOperatorEnum

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

        Public Property Version() As String Implements IVersionable.SchemaVersion
            Get
                Return _v
            End Get
            Set(value As String)
                _v = value
            End Set
        End Property
        Public Property SchemaVersionOperator() As SchemaVersionOperatorEnum Implements IVersionable.SchemaVersionOperator
            Get
                Return _verOper
            End Get
            Set(ByVal value As SchemaVersionOperatorEnum)
                _verOper = value
            End Set
        End Property
        Public Property Feature As String Implements IVersionable.Feature
            Get
                Return _feature
            End Get
            Set(value As String)
                _feature = value
            End Set
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
        ''' ������������� �� ��� ��������� ����� �������� ��� �������� <see cref="EntityPropertyAttribute" />
        ''' </summary>
        ''' <value>�������� ��������</value>
        ''' <returns>���� True, ��� �������� ����, � �������� ��������� ������ ������� ����� 
        ''' ����������� ��� ��������� �����</returns>
        ''' <remarks>�� ��������� False</remarks>
        Public Property RawProperties() As Boolean
            Get
                Return _rawProps
            End Get
            Set(ByVal value As Boolean)
                _rawProps = value
            End Set
        End Property
        'Public Property PKSourceFields As String()
        '    Get
        '        Return _pk
        '    End Get
        '    Set(value As String())
        '        _pk = value
        '    End Set
        'End Property

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
        ''' ������ ��������� ������, ����������� ����� �������
        ''' </summary>
        ''' <remarks>������� ���������� ������ ������ ���� ����� �� ��� � � <see cref="ForeignKeys" /></remarks>
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
        ''' ������ ������� ������, ����������� ����� �������
        ''' </summary>
        ''' <remarks>������� ���������� ������ ������ ���� ����� �� ��� � � <see cref="PrimaryKeys" /></remarks>
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