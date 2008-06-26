Imports System

Namespace Orm.Meta

    <AttributeUsage(AttributeTargets.Property, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    Public NotInheritable Class ColumnAttribute
        Inherits Attribute
        Implements IComparable(Of ColumnAttribute)

        Private _fieldName As String
        Public ReadOnly _behavior As Field2DbRelations
        'Private _table As String
        Private _column As String
        Private _idx As Integer = -1

        Public Sub New()
        End Sub

        Public Sub New(ByVal behavior As Field2DbRelations)
            Me._behavior = behavior
        End Sub

        Public Sub New(ByVal fieldName As String)
            _fieldName = fieldName
            Me._behavior = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal behavior As Field2DbRelations)
            _fieldName = fieldName
            Me._behavior = behavior
        End Sub

        ''' <summary>
        ''' Имя поля класса, которое мапится на колонку в БД
        ''' </summary>
        Public Property FieldName() As String
            Get
                Return _fieldName
            End Get
            Set(ByVal value As String)
                _fieldName = value
            End Set
        End Property

        'Public ReadOnly Property SyncBehavior() As Field2DbRelations
        '    Get
        '        Return _behavior
        '    End Get
        '    'Set(ByVal value As Field2DbRelations)
        '    '    behavior = value
        '    'End Set
        'End Property

        Public Property Column() As String
            Get
                Return _column
            End Get
            Set(ByVal value As String)
                _column = value
            End Set
        End Property

        'Public Property TableName() As String
        '    Get
        '        Return _table
        '    End Get
        '    Set(ByVal value As String)
        '        _table = value
        '    End Set
        'End Property

        Public Property Index() As Integer
            Get
                Return _idx
            End Get
            Set(ByVal value As Integer)
                _idx = value
            End Set
        End Property

        Public Function CompareTo(ByVal other As ColumnAttribute) As Integer Implements System.IComparable(Of ColumnAttribute).CompareTo
            Return _fieldName.CompareTo(other._fieldName)
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, ColumnAttribute))
        End Function

        Public Overloads Function Equals(ByVal obj As ColumnAttribute) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return FieldName.Equals(obj._fieldName)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _fieldName.GetHashCode
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
        [Private] = 64
        Factory = 128
    End Enum

    <AttributeUsage(AttributeTargets.Class, allowmultiple:=True, inherited:=True), CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019")> _
    Public NotInheritable Class EntityAttribute
        Inherits Attribute

        Private _t As Type
        Private _v As String
        Private _entityName As String
        Private _table As String
        Private _pk As String

        Public Sub New(ByVal primaryKeyColumn As String, ByVal version As String)
            _v = version
            _pk = primaryKeyColumn
        End Sub

        Public Sub New(ByVal tableName As String, ByVal primaryKeyColumn As String, ByVal version As String)
            _table = tableName
            _v = version
            _pk = primaryKeyColumn
        End Sub

        Public Sub New(ByVal type As Type, ByVal version As String)
            _t = type
            _v = version
        End Sub

        Public Property PrimaryKey() As String
            Get
                Return _pk
            End Get
            Set(ByVal value As String)
                _pk = value
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
    End Class
End Namespace