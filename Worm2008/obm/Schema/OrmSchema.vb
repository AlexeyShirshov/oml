Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Joins
Imports Worm.Orm
Imports Worm.Sorting
'Namespace Schema

''' <summary>
''' Данное исключение выбрасывается при определеных ошибках в <see cref="QueryGenerator" />
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public NotInheritable Class QueryGeneratorException
    Inherits Exception

    ''' <summary>
    ''' Конструктор по умолчанию
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

    ''' <summary>
    ''' Конструктор для создания объекта через сообщение
    ''' </summary>
    ''' <param name="message">Строка сообщения</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    ''' <summary>
    ''' Конструктор для создания объекта через сообщение и внутренее инсключение
    ''' </summary>
    ''' <param name="message">Строка сообщения</param>
    ''' <param name="inner">Исключение</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

    Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
End Class

''' <summary>
''' Интерфейс расширяющий схему для работы с объдинениями (joins)
''' </summary>
''' <remarks></remarks>
Public Interface ISchemaWithJoins
    'Function GetSharedTable(ByVal tableName As String) As SourceFragment
    'Function GetTables(ByVal type As Type) As SourceFragment()
    ''' <summary>
    ''' Метод используется для получения объекта типа <see cref="OrmJoin" /> для определеной таблицы определеного типа
    ''' </summary>
    ''' <param name="type">Тип объекта</param>
    ''' <param name="left">Левая таблица. Какая либо из списка <see cref="IObjectSchemaBase.GetTables"/></param>
    ''' <param name="right">Правая таблица. Какая либо из списка <see cref="IObjectSchemaBase.GetTables"/></param>
    ''' <param name="filterInfo">Произвольный объект, используемый реализацией. Передается из <see cref="OrmManagerBase.GetFilterInfo" /></param>
    ''' <returns>Объекта типа <see cref="OrmJoin" /></returns>
    ''' <remarks>Используется для генерации запросов</remarks>
    Function GetJoins(ByVal type As Type, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As OrmJoin
End Interface

''' <summary>
''' Интерфейс для "подготовки" таблицы перед генерацией запроса
''' </summary>
''' <remarks>Используется для реализации функций в качестве таблиц, разрешения схем таблицы (schema resolve)</remarks>
Public Interface IPrepareTable
    ''' <summary>
    ''' Словарь псевдонимов (aliases) таблиц
    ''' </summary>
    ''' <value></value>
    ''' <returns>Словарь где каждой таблице соответствует псевдоним</returns>
    ''' <remarks></remarks>
    ReadOnly Property Aliases() As IDictionary(Of SourceFragment, String)
    ''' <summary>
    ''' Добавляет таблицу в словарь и создает текстовое представление таблицы (псевдоним)
    ''' </summary>
    ''' <param name="table">Таблица</param>
    ''' <returns>Возвращает псевдоним таблицы</returns>
    ''' <remarks>Если таблица уже добавлена реализация может кинуть исключение</remarks>
    Function AddTable(ByRef table As SourceFragment) As String
    ''' <summary>
    ''' Заменяет в <see cref="StringBuilder"/> названия таблиц на псевдонимы
    ''' </summary>
    ''' <param name="schema">Схема</param>
    ''' <param name="table">Таблица</param>
    ''' <param name="sb">StringBuilder</param>
    ''' <remarks></remarks>
    Sub Replace(ByVal schema As QueryGenerator, ByVal table As SourceFragment, ByVal sb As StringBuilder)
End Interface

''' <summary>
''' Класс хранения и управления схемами объектов <see cref="IObjectSchemaBase"/>
''' </summary>
''' <remarks>Класс управляет версиями схем объектов, предоставляет удобные обертки для методов
''' <see cref="IObjectSchemaBase"/> через тип объекта.</remarks>
Public MustInherit Class QueryGenerator
    Implements ISchemaWithJoins

    Public Delegate Function ResolveEntity(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
    Public Delegate Function ResolveEntityName(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute

    Private _sharedTables As Hashtable = Hashtable.Synchronized(New Hashtable)
    Protected map As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected sel As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected _joins As IDictionary = Hashtable.Synchronized(New Hashtable)
    Private _version As String
    Private _mapv As ResolveEntity
    Private _mapn As ResolveEntityName

    Public Sub New(ByVal version As String)
        _version = version
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity)
        _version = version
        _mapv = resolveEntity
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveName As ResolveEntityName)
        _version = version
        _mapn = resolveName
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity, ByVal resolveName As ResolveEntityName)
        _version = version
        _mapv = resolveEntity
        _mapn = resolveName
    End Sub

#Region " reflection "

    Protected Friend Function GetProperties(ByVal t As Type) As IDictionary
        Return GetProperties(t, GetObjectSchema(t, False))
    End Function

    'Protected Friend Function GetProperties(ByVal t As Type, ByVal schema As IOrmObjectSchema) As IDictionary
    '    Return GetProperties(t, schema)
    'End Function

    Public Shared Function GetColumnProperties(ByVal t As Type, ByVal schema As IObjectSchemaBase) As IDictionary
        Dim result As New Hashtable

        Dim sup As Array = Nothing
        If schema IsNot Nothing Then
            sup = schema.GetSuppressedColumns()
        End If

        Dim idx As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
        If schema IsNot Nothing Then idx = schema.GetFieldColumnMap()

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)
            Dim column As ColumnAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(ColumnAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), ColumnAttribute)
            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.FieldName) Then
                    column.FieldName = pi.Name
                End If

                If (sup Is Nothing OrElse Array.IndexOf(sup, column) < 0) AndAlso (idx Is Nothing OrElse idx.ContainsKey(column.FieldName)) Then
                    result.Add(column, pi)
                End If
            End If
        Next

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
            Dim column As ColumnAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(ColumnAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), ColumnAttribute)
            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.FieldName) Then
                    column.FieldName = pi.Name
                End If
                If Not result.Contains(column) AndAlso (sup Is Nothing OrElse Array.IndexOf(sup, column) < 0) AndAlso (idx Is Nothing OrElse idx.ContainsKey(column.FieldName)) Then
                    result.Add(column, pi)
                End If
            End If
        Next
        Return result
    End Function

    Public Function GetProperties(ByVal t As Type, ByVal schema As IObjectSchemaBase) As IDictionary
        If t Is Nothing Then Throw New ArgumentNullException("original_type")
        Dim s As String = Nothing
        If schema Is Nothing Then
            s = t.ToString
        Else
            s = t.ToString & schema.GetType().ToString
        End If
        Dim key As String = "properties" & s
        Dim h As IDictionary = CType(map(key), IDictionary)
        If h Is Nothing Then
            SyncLock String.Intern(key)
                h = CType(map(key), IDictionary)
                If h Is Nothing Then
                    h = GetColumnProperties(t, schema)

                    map(key) = h
                End If
            End SyncLock
        End If
        Return h
    End Function

#End Region

#Region " object functions "

    'Public Function GetAllJoins(ByVal t As Type) As ICollection(Of OrmJoin)
    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

    '    Dim tbls() As SourceFragment = schema.GetTables
    '    Dim tbl As SourceFragment = tbls(0)
    '    Dim js As New List(Of OrmJoin)
    '    For i As Integer = 1 To tbls.Length - 1
    '        Dim j As OrmJoin = schema.GetJoins(tbl, tbls(i))
    '        js.Add(j)
    '    Next
    '    Return js.ToArray
    'End Function

    Public Function GetEntityKey(ByVal filterInfo As Object, ByVal t As Type) As String
        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityKey(filterInfo)
        Else
            Return t.ToString
        End If
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object, ByVal t As Type) As Object
        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityTypeKey(filterInfo)
        Else
            Return t
        End If
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As IOrmObjectSchemaBase) As Object
        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityTypeKey(filterInfo)
        Else
            Return t
        End If
    End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    Protected Function GetFieldNameByColumnName(ByVal type As Type, ByVal columnName As String) As String

        If String.IsNullOrEmpty(columnName) Then Throw New ArgumentNullException("columnName")

        Dim schema As IObjectSchemaBase = GetObjectSchema(type)

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        For Each p As MapField2Column In coll
            If p._columnName = columnName Then
                Return p._fieldName
            End If
        Next

        Throw New QueryGeneratorException("Cannot find column: " & columnName)
    End Function

    Protected Friend Function GetColumnNameByFieldNameInternal(ByVal schema As IObjectSchemaBase, ByVal field As String, ByVal add_alias As Boolean, ByVal columnAliases As List(Of String)) As String
        If String.IsNullOrEmpty(field) Then Throw New ArgumentNullException("field")

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        Dim p As MapField2Column = Nothing
        If coll.TryGetValue(field, p) Then
            Dim c As String = Nothing
            If add_alias AndAlso ShouldPrefix(p._columnName) Then
                c = GetTableName(p._tableName) & Selector & p._columnName
            Else
                c = p._columnName
            End If
            If columnAliases IsNot Nothing Then
                columnAliases.Add(p._columnName)
            End If
            Return c
        End If

        Throw New QueryGeneratorException("Cannot find property: " & field)
    End Function

    Protected Friend Function GetColumnNameByFieldNameInternal(ByVal type As Type, ByVal field As String, ByVal add_alias As Boolean, Optional ByVal columnAliases As List(Of String) = Nothing) As String
        If String.IsNullOrEmpty(field) Then Throw New ArgumentNullException("field")

        Dim schema As IObjectSchemaBase = GetObjectSchema(type)

        Return GetColumnNameByFieldNameInternal(schema, field, add_alias, columnAliases)
    End Function

    '<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011")> _
    'Public Function GetM2MRelationTable(ByVal mainType As Type, ByVal subtype As Type) As String
    '    If mainType Is Nothing Then
    '        Throw New ArgumentNullException("mainType parameter cannot be nothing")
    '    End If

    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(mainType)

    '    Return schema.GetM2MRelationTable(subtype)
    'End Function

    '<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011")> _
    'Public Function GetM2MRelationColumn(ByVal mainType As Type, ByVal subtype As Type) As String
    '    If mainType Is Nothing Then
    '        Throw New ArgumentNullException("mainType parameter cannot be nothing")
    '    End If

    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(mainType)

    '    Return schema.GetM2MRelationColumn(subtype)
    'End Function

    Public Function GetM2MRelations(ByVal maintype As Type) As M2MRelation()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Dim schema As IOrmRelationalSchemaWithM2M = TryCast(GetObjectSchema(maintype), IOrmRelationalSchemaWithM2M)
        If schema IsNot Nothing Then
            Return schema.GetM2MRelations
        Else
            Return Nothing
        End If
    End Function

    Public Function GetM2MRelationsForEdit(ByVal maintype As Type) As M2MRelation()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Dim sch As IObjectSchemaBase = GetObjectSchema(maintype)
        Dim editable As IReadonlyObjectSchema = TryCast(sch, IReadonlyObjectSchema)
        Dim schema As IOrmRelationalSchemaWithM2M = Nothing
        If editable IsNot Nothing Then
            schema = editable.GetEditableSchema
        Else
            schema = TryCast(sch, IOrmRelationalSchemaWithM2M)
        End If
        If schema IsNot Nothing Then
            Dim m As M2MRelation() = schema.GetM2MRelations
            If m Is Nothing AndAlso editable IsNot Nothing Then
                schema = TryCast(sch, IOrmRelationalSchemaWithM2M)
                If schema IsNot Nothing Then
                    m = schema.GetM2MRelations
                End If
            End If
            Return m
        Else
            Return Nothing
        End If
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal subtype As Type, ByVal key As String) As M2MRelation
        If String.IsNullOrEmpty(key) Then key = M2MRelation.DirKey
        Dim mr() As M2MRelation = GetM2MRelations(maintype)
        For Each r As M2MRelation In mr
            If r.Type Is subtype AndAlso String.Equals(r.Key, key) Then
                Return r
            End If
        Next

        Dim en As String = GetEntityNameByType(maintype)
        For Each r As M2MRelation In mr
            Dim n As String = GetEntityNameByType(r.Type)
            If String.Equals(en, n) AndAlso String.Equals(r.Key, key) Then
                Return r
            End If
        Next

        Return Nothing
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal subtype As Type, ByVal direct As Boolean) As M2MRelation
        Dim mr() As M2MRelation = GetM2MRelations(maintype)
        For Each r As M2MRelation In mr
            If r.Type Is subtype AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
                Return r
            End If
        Next

        Dim en As String = GetEntityNameByType(maintype)
        For Each r As M2MRelation In mr
            Dim n As String = GetEntityNameByType(r.Type)
            If String.Equals(en, n) AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
                Return r
            End If
        Next

        Return Nothing
    End Function

    Public Function GetM2MRelationForEdit(ByVal maintype As Type, ByVal subtype As Type, ByVal direct As String) As M2MRelation
        For Each r As M2MRelation In GetM2MRelationsForEdit(maintype)
            If r.Type Is subtype AndAlso (maintype IsNot subtype OrElse r.Key = direct) Then
                Return r
            End If
        Next

        Return Nothing
    End Function

    Public Function GetRevM2MRelation(ByVal mainType As Type, ByVal subType As Type, ByVal key As String) As M2MRelation
        If mainType Is subType Then
            Return GetM2MRelation(subType, mainType, M2MRelation.GetRevKey(key))
        Else
            Return GetM2MRelation(subType, mainType, key)
        End If
    End Function

    Public Function GetRevM2MRelation(ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean) As M2MRelation
        If mainType Is subType Then
            Return GetM2MRelation(subType, mainType, Not direct)
        Else
            Return GetM2MRelation(subType, mainType, True)
        End If
    End Function

    Public Function IsMany2ManyReadonly(ByVal maintype As Type, ByVal subtype As Type) As Boolean
        Dim r As M2MRelation = GetM2MRelation(maintype, subtype, True)

        If r Is Nothing Then
            Throw New ArgumentException("Relation is not exists")
        Else
            Return r.ConnectedType IsNot Nothing
        End If
    End Function

    Public Function GetEntityNameByType(ByVal t As Type) As String
        Dim a() As Attribute = Attribute.GetCustomAttributes(t, GetType(EntityAttribute))
        For Each ea As EntityAttribute In a
            If ea.Version = _version Then
                Return ea.EntityName
            End If
        Next
        Return Nothing
    End Function

    Public Function GetConnectedType(ByVal maintype As Type, ByVal subtype As Type) As Type
        Dim r As M2MRelation = GetM2MRelation(maintype, subtype, True)

        If r Is Nothing Then
            Throw New ArgumentException("Relation is not exists")
        Else
            Return r.ConnectedType
        End If
    End Function

    Public Function GetConnectedTypeField(ByVal ct As Type, ByVal t As Type, ByVal key As String) As String
        Dim rel As IRelation = GetConnectedTypeRelation(ct)
        If rel Is Nothing Then
            Throw New ArgumentException("Type is not implement IRelation")
        End If
        Dim p As IRelation.RelationDesc = rel.GetFirstType
        Dim p2 As IRelation.RelationDesc = rel.GetSecondType
        If p.EntityType Is p2.EntityType Then
            If p.Key = key Then
                Return p.PropertyName
            Else
                Return p2.PropertyName
            End If
        Else
            If p.EntityType IsNot t Then
                Return p.PropertyName
            Else
                Return p2.PropertyName
            End If
        End If
    End Function

    Public Function GetConnectedTypeField(ByVal ct As Type, ByVal t As Type, ByVal direction As Boolean) As String
        Return GetConnectedTypeField(ct, t, M2MRelation.GetKey(direction))
        'Dim rel As IRelation = GetConnectedTypeRelation(ct)
        'If rel Is Nothing Then
        '    Throw New ArgumentException("Type is not implement IRelation")
        'End If
        'Dim p As IRelation.RelationDesc = rel.GetFirstType
        'Dim p2 As IRelation.RelationDesc = rel.GetSecondType
        'If p.EntityType Is p2.EntityType Then
        '    If p.Direction = direction Then
        '        Return p.PropertyName
        '    Else
        '        Return p2.PropertyName
        '    End If
        'Else
        '    If p.EntityType IsNot t Then
        '        Return p.PropertyName
        '    Else
        '        Return p2.PropertyName
        '    End If
        'End If
    End Function

    Public Function GetConnectedTypeRelation(ByVal ct As Type) As IRelation
        If ct Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(ct)

        Return TryCast(schema, IRelation)
    End Function

    'Public Function GetM2MDeleteCascade(ByVal maintype As Type, ByVal subtype As Type) As Boolean
    '    Return GetM2MRelation(maintype, subtype).DeleteCascade
    'End Function

    '<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011")> _
    'Public Function MapSort2FieldName(ByVal type As Type, ByVal sort As String) As String
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type parameter cannot be nothing")
    '    End If

    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

    '    Return schema.MapSort2FieldName(sort)
    'End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822")> _
    Public Function ChangeValueType(ByVal type As Type, ByVal c As ColumnAttribute, ByVal o As Object) As Object
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Return ChangeValueType(GetObjectSchema(type), c, o)
    End Function

    Public Function ChangeValueType(ByVal schema As IObjectSchemaBase, ByVal c As ColumnAttribute, ByVal o As Object) As Object
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        If o Is Nothing Then Return DBNull.Value

        Dim ot As System.Type = o.GetType

        If ot Is GetType(Guid) AndAlso CType(o, Guid) = Guid.Empty Then
            Return DBNull.Value
		End If

		' для всяческих addDate'ов, которые автоматом проставляются, 
		' ибо лишняя работа писать на каждый такой слушчай ChangeValueType
		If ot Is GetType(DateTime) AndAlso CType(o, DateTime) = DateTime.MinValue Then
			Return DBNull.Value
		End If

        Dim v As Object = o

        If schema.ChangeValueType(c, o, v) Then
            Return v
        End If

        If GetType(OrmBase).IsAssignableFrom(ot) Then
            Return CType(o, OrmBase).Identifier
        End If

        If GetType(System.Xml.XmlDocument) Is ot Then
            Return CType(o, System.Xml.XmlDocument).OuterXml
        End If

        Return v
    End Function

    '<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    'Protected Function GetFieldTable(ByVal type As Type, ByVal field As String) As SourceFragment
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type")
    '    End If

    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

    '    Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

    '    Try
    '        Return coll(field)._tableName
    '    Catch ex As Exception
    '        Throw New DBSchemaException("Unknown field name: " & field, ex)
    '    End Try
    'End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    Protected Function GetFieldTable(ByVal schema As IOrmPropertyMap, ByVal field As String) As SourceFragment
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
        Try
            Return coll(field)._tableName
        Catch ex As Exception
            Throw New QueryGeneratorException("Unknown field name: " & field, ex)
        End Try
    End Function
    'Public ReadOnly Property IsExternalSort(ByVal sort As String, ByVal type As Type) As Boolean
    '    Get
    '        If type Is Nothing Then
    '            Throw New ArgumentNullException("type parameter cannot be nothing")
    '        End If

    '        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

    '        Return schema.IsExternalSort(sort)
    '    End Get
    'End Property

    Public Function ExternalSort(Of T As {_IEntity})(ByVal mgr As OrmManagerBase, ByVal sort As Sort, ByVal objs As ReadOnlyObjectList(Of T)) As ReadOnlyObjectList(Of T)
        Return Sort.ExternalSort(Of T)(mgr, Me, objs)
        'Dim schema As IOrmObjectSchemaBase = GetObjectSchema(GetType(T))
        'Dim s As IOrmSorting = TryCast(schema, IOrmSorting)
        'If s Is Nothing Then
        '    Throw New OrmSchemaException(String.Format("Type {0} is not support sorting", GetType(T)))
        'End If
        'Return s.ExternalSort(Of T)(sort, objs)
    End Function

    Public Function GetJoinSelectMapping(ByVal t As Type, ByVal subType As Type) As System.Data.Common.DataTableMapping
        Dim r As M2MRelation = GetM2MRelation(t, subType, True)

        If r Is Nothing Then
            Throw New QueryGeneratorException(String.Format("Type {0} has no relation to {1}", t.Name, subType.Name))
        End If

        Return r.Mapping
    End Function

    Public Function GetAttributes(ByVal type As Type, ByVal c As ColumnAttribute) As Field2DbRelations
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

        Return GetAttributes(schema, c)
    End Function

    Public Function GetAttributes(ByVal schema As IOrmObjectSchemaBase, ByVal c As ColumnAttribute) As Field2DbRelations
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Return schema.GetFieldColumnMap()(c.FieldName).GetAttributes(c)
    End Function
#End Region

#Region " Helpers "
    Protected Friend Sub GetPKList(ByVal type As Type, ByVal schema As IOrmObjectSchemaBase, ByVal ids As StringBuilder, Optional ByVal columnAliases As List(Of String) = Nothing)
        If ids Is Nothing Then
            Throw New ArgumentNullException("ids")
        End If

        For Each pk As String In GetPrimaryKeysName(type, columnAliases:=columnAliases, schema:=schema)
            ids.Append(pk).Append(",")
        Next
        ids.Length -= 1

        'Dim p As MapField2Column = schema.GetFieldColumnMap("ID")
        'ids.Append(GetTableName(p._tableName)).Append(Selector).Append(p._columnName)
        'If columnAliases IsNot Nothing Then
        '    columnAliases.Add(p._columnName)
        'End If
    End Sub

    Public Function HasField(ByVal t As Type, ByVal field As String) As Boolean
        If String.IsNullOrEmpty(field) Then Return False

        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

        Return schema.GetFieldColumnMap.ContainsKey(field)
    End Function

    'Protected Sub GetPKList(ByVal type As Type, ByVal ids As StringBuilder, ByVal table As String)
    '    If ids Is Nothing Then
    '        Throw New ArgumentNullException("ids parameter cannot be nothing")
    '    End If

    '    Dim b As Boolean = False
    '    For Each pk As String In GetPrimaryKeysName(type, table)
    '        If b Then
    '            ids.Append(",")
    '        Else
    '            b = True
    '        End If
    '        ids.Append(pk)
    '    Next
    'End Sub

    Protected Friend Function GetProperty(ByVal original_type As Type, ByVal c As ColumnAttribute) As Reflection.PropertyInfo
        Return CType(GetProperties(original_type)(c), Reflection.PropertyInfo)
    End Function

    Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IOrmObjectSchemaBase, ByVal c As ColumnAttribute) As Reflection.PropertyInfo
        Return CType(GetProperties(t, schema)(c), Reflection.PropertyInfo)
    End Function

    Protected Friend Function GetProperty(ByVal original_type As Type, ByVal field As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(field) Then
            Throw New ArgumentNullException("field")
        End If

        Return GetProperty(original_type, New ColumnAttribute(field))
    End Function

    Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IOrmObjectSchemaBase, ByVal field As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(field) Then
            Throw New ArgumentNullException("field")
        End If

        Return GetProperty(t, schema, New ColumnAttribute(field))
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal original_type As Type, ByVal field As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(field) Then
            Throw New ArgumentNullException("field")
        End If

        Return CType(GetColumnProperties(original_type, Nothing)(New ColumnAttribute(field)), Reflection.PropertyInfo)
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal t As Type, ByVal oschema As IOrmObjectSchemaBase, ByVal field As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(field) Then
            Throw New ArgumentNullException("field")
        End If

        Return CType(GetColumnProperties(t, oschema)(New ColumnAttribute(field)), Reflection.PropertyInfo)
    End Function


    Public Function GetSortedFieldList(ByVal original_type As Type, Optional ByVal schema As IOrmObjectSchemaBase = Nothing) As Generic.List(Of ColumnAttribute)
        'Dim cl_type As String = New StringBuilder().Append("columnlist").Append(type.ToString).ToString
        Dim cl_type As String = "columnlist" & original_type.ToString

        Dim arr As Generic.List(Of ColumnAttribute) = CType(map(cl_type), Generic.List(Of ColumnAttribute))
        If arr Is Nothing Then
            SyncLock String.Intern(cl_type)
                arr = CType(map(cl_type), Generic.List(Of ColumnAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of ColumnAttribute)
                    If schema Is Nothing Then
                        schema = GetObjectSchema(original_type)
                    End If

                    For Each c As ColumnAttribute In GetProperties(original_type, schema).Keys
                        arr.Add(c)
                    Next

                    arr.Sort()

                    map.Add(cl_type, arr)
                End If
            End SyncLock
        End If
        Return arr
    End Function

    '<MethodImpl(MethodImplOptions.Synchronized)> _
    Public Function GetPrimaryKeysName(ByVal original_type As Type, Optional ByVal add_alias As Boolean = True, _
        Optional ByVal columnAliases As List(Of String) = Nothing, Optional ByVal schema As IOrmObjectSchemaBase = Nothing) As String()
        If original_type Is Nothing Then
            Throw New ArgumentNullException("original_type")
        End If

        Dim cl_type As String = "pklist" & original_type.ToString & add_alias

        Dim arr As Generic.List(Of String) = CType(map(cl_type), Generic.List(Of String))

        If arr Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(cl_type)
                arr = CType(map(cl_type), Generic.List(Of String))
                If arr Is Nothing Then
                    arr = New Generic.List(Of String)
                    If schema Is Nothing Then
                        schema = GetObjectSchema(original_type)
                    End If

                    For Each c As ColumnAttribute In GetSortedFieldList(original_type, schema)
                        If (GetAttributes(schema, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            arr.Add(GetColumnNameByFieldNameInternal(schema, c.FieldName, add_alias, columnAliases))
                        End If
                    Next

                    map.Add(cl_type, arr)
                End If
            End Using
        End If

        Return arr.ToArray
    End Function

    Public Function GetPrimaryKeys(ByVal original_type As Type) As List(Of ColumnAttribute)
        Dim cl_type As String = "clm_pklist" & original_type.ToString

        Dim arr As Generic.List(Of ColumnAttribute) = CType(map(cl_type), Generic.List(Of ColumnAttribute))

        If arr Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(cl_type)
                arr = CType(map(cl_type), Generic.List(Of ColumnAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of ColumnAttribute)

                    For Each c As ColumnAttribute In GetSortedFieldList(original_type)
                        If (GetAttributes(original_type, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            arr.Add(c)
                        End If
                    Next

                    map.Add(cl_type, arr)
                End If
            End Using
        End If

        Return arr
    End Function

    'Protected Function GetPrimaryKeysName(ByVal type As Type, ByVal table As String) As String()
    '    Dim arr As Generic.List(Of String)

    '    'Dim cl_type As String = "pklist" & type.ToString & add_alias
    '    'SyncLock cl_type
    '    'If Not map.Contains(cl_type) Then
    '    arr = New Generic.List(Of String)

    '    For Each c As ColumnAttribute In GetSortedFieldList(type)
    '        If (c.SyncBehavior And Field2DbRelations.PrimaryKey) = Field2DbRelations.PrimaryKey Then
    '            arr.Add(GetColumnNameByFieldName(type, c.FieldName, table))
    '        End If
    '    Next

    '    'map.Add(cl_type, arr)
    '    'Else
    '    'arr = CType(map(cl_type), Generic.List(Of String))
    '    'End If
    '    'End SyncLock
    '    Return arr.ToArray
    'End Function

    Public Shared Function GetFieldValueSchemaless(ByVal obj As _IEntity, ByVal fieldName As String, ByVal schema As IOrmObjectSchemaBase, ByVal pi As Reflection.PropertyInfo) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        If pi Is Nothing Then
            If schema Is Nothing Then
                pi = GetPropertyInt(obj.GetType, fieldName)
            Else
                pi = GetPropertyInt(obj.GetType, schema, fieldName)
            End If
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, fieldName))
        End If

        Return GetFieldValue(obj, fieldName, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal fieldName As String) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim schema As IOrmObjectSchemaBase = GetObjectSchema(obj.GetType)

        Dim pi As Reflection.PropertyInfo = Nothing

        If schema IsNot Nothing Then
            pi = GetProperty(obj.GetType, schema, fieldName)
        Else
            pi = GetProperty(obj.GetType, fieldName)
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, fieldName))
        End If

        Return GetFieldValue(obj, fieldName, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal fieldName As String, ByVal schema As IOrmObjectSchemaBase) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim pi As Reflection.PropertyInfo = Nothing

        If schema IsNot Nothing Then
            pi = GetProperty(obj.GetType, schema, fieldName)
        Else
            pi = GetProperty(obj.GetType, fieldName)
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, fieldName))
        End If

        Return GetFieldValue(obj, fieldName, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal fieldName As String, ByVal schema As IOrmObjectSchemaBase, ByVal pi As Reflection.PropertyInfo) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        If pi Is Nothing Then
            If schema IsNot Nothing Then
                pi = GetProperty(obj.GetType, schema, fieldName)
            Else
                pi = GetProperty(obj.GetType, fieldName)
            End If
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, fieldName))
        End If

        Return GetFieldValue(obj, fieldName, pi, schema)
    End Function

    Public Shared Function GetFieldValue(ByVal obj As _IEntity, ByVal fieldName As String, ByVal pi As Reflection.PropertyInfo, ByVal oschema As IOrmObjectSchemaBase) As Object
        If pi Is Nothing Then
            Throw New ArgumentNullException("pi")
        End If

        Using obj.SyncHelper(True, fieldName)
            'Return pi.GetValue(obj, Nothing)
            Return obj.GetValue(pi, New ColumnAttribute(fieldName), oschema)
        End Using
    End Function

    'Public Shared Function GetFieldValue(ByVal obj As _IEntity, ByVal c As ColumnAttribute, ByVal pi As Reflection.PropertyInfo, ByVal oschema As IOrmObjectSchemaBase) As Object
    '    If pi Is Nothing Then
    '        Throw New ArgumentNullException("pi")
    '    End If

    '    Using obj.SyncHelper(True, c.FieldName)
    '        'Return pi.GetValue(obj, Nothing)
    '        Return obj.GetValue(pi, c, oschema)
    '    End Using
    'End Function

    Public Sub SetFieldValue(ByVal obj As _IEntity, ByVal fieldName As String, ByVal value As Object, ByVal oschema As IOrmObjectSchemaBase)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim pi As Reflection.PropertyInfo = GetProperty(obj.GetType, fieldName)

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, fieldName))
        End If

        Using obj.SyncHelper(False, fieldName)
            obj.SetValue(pi, GetColumnByFieldName(obj.GetType, fieldName), oschema, value)
            'pi.SetValue(obj, value, Nothing)
        End Using
    End Sub

    'Protected Function GetPrimaryKeysValue(ByVal obj As OrmBase) As Object()
    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj parameter cannot be nothing")
    '    End If

    '    Dim arr As New ArrayList

    '    For Each pk_name As String In GetPrimaryKeysName(obj.GetType)
    '        arr.Add(GetPrimaryKeyValue(obj))
    '    Next

    '    Return arr.ToArray
    'End Function

    'Protected Function GetPrimaryKeyValue(ByVal obj As OrmBase) As Object
    '    Dim pk_name As String = "ID" 'GetPrimaryKeysName(t, original_type, False)(0)
    '    Return GetColumnValue(obj, pk_name)
    'End Function

    Protected Friend Function GetSelectColumns(ByVal props As IEnumerable(Of OrmProperty), ByVal columnAliases As List(Of String)) As String
        Dim sb As New StringBuilder
        For Each pr As OrmProperty In props
            If pr.Type Is Nothing Then
                If pr.Table Is Nothing Then
                    sb.Append(String.Format(pr.Computed, ExtractValues(Me, Nothing, pr.Values).ToArray)).Append(", ")
                Else
                    sb.Append(GetTableName(pr.Table)).Append(Selector).Append(pr.Column).Append(", ")
                End If
            Else
                sb.Append(GetColumnNameByFieldNameInternal(GetObjectSchema(pr.Type), pr.Field, True, columnAliases)).Append(", ")
            End If
        Next

        sb.Length -= 2

        Return sb.ToString
    End Function

    Protected Friend Function GetSelectColumnList(ByVal original_type As Type, ByVal arr As Generic.ICollection(Of ColumnAttribute), ByVal columnAliases As List(Of String), ByVal schema As IOrmObjectSchemaBase) As String
        'Dim add_c As Boolean = False
        'If arr Is Nothing Then
        '    Dim s As String = CStr(sel(original_type))
        '    If Not String.IsNullOrEmpty(s) Then
        '        Return s
        '    End If
        '    add_c = True
        'End If
        Dim sb As New StringBuilder
        If arr Is Nothing Then arr = GetSortedFieldList(original_type, schema)
        For Each c As ColumnAttribute In arr
            sb.Append(GetColumnNameByFieldNameInternal(schema, c.FieldName, True, columnAliases)).Append(", ")
        Next

        sb.Length -= 2

        'If add_c Then
        '    sel(original_type) = sb.ToString
        'End If
        Return sb.ToString
    End Function

    'Protected Function GetSelectColumnList(ByVal type As Type, ByVal table As String) As String
    '    Dim sb As New StringBuilder
    '    For Each c As ColumnAttribute In GetSortedFieldList(type)
    '        Try
    '            sb.Append(GetColumnNameByFieldName(type, c.FieldName, table))
    '            If c.FieldName = "ID" Then
    '                Dim p As Pair(Of Integer) = GetUnionScope(type, table)
    '                sb.Append("+").Append(p.First)
    '            End If
    '            sb.Append(", ")
    '        Catch ex As MissingMethodException
    '            Throw New DBSchemaException("Schema " & Me.GetType.Name & " doesnot support type " & type.Name, ex)
    '        End Try
    '    Next

    '    sb.Length -= 2

    '    Return sb.ToString
    'End Function

    Protected Friend Function GetJoinFieldNameByType(ByVal mainType As Type, ByVal subType As Type, ByVal oschema As IOrmObjectSchemaBase) As String
        Dim j As IJoinBehavior = TryCast(oschema, IJoinBehavior)
        Dim r As String = Nothing
        If j IsNot Nothing Then
            r = j.GetJoinField(subType)
        End If
        If String.IsNullOrEmpty(r) Then
            Dim c As ICollection(Of String) = GetFieldNameByType(mainType, subType)
            If c.Count = 1 Then
                For Each s As String In c
                    r = s
                Next
            End If
        End If
        Return r
    End Function

    Public Function GetJoinObj(ByVal oschema As IOrmObjectSchemaBase, _
        ByVal obj As _IEntity, ByVal subType As Type) As IOrmBase
        Dim c As String = GetJoinFieldNameByType(obj.GetType, subType, oschema)
        Dim r As IOrmBase = Nothing
        If Not String.IsNullOrEmpty(c) Then
            Dim id As Object = Nothing
            If obj.IsFieldLoaded(c) Then
                id = obj.GetValue(Nothing, New ColumnAttribute(c), oschema)
            Else
                id = GetFieldValue(obj, c, oschema)
            End If
            r = TryCast(id, OrmBase)
            If r Is Nothing AndAlso id IsNot Nothing Then
                Try
                    'Dim id As Integer = Convert.ToInt32(o)
                    r = OrmManagerBase.CurrentManager.GetOrmBaseFromCacheOrCreate(id, subType)
                Catch ex As InvalidCastException
                End Try
            End If
        End If
        Return r
    End Function

    Protected Function GetColumnNameByFieldName(ByVal type As Type, ByVal field As String, Optional ByVal columnAliases As List(Of String) = Nothing) As String
        Return GetColumnNameByFieldNameInternal(type, field, True, columnAliases)
    End Function

    Protected Function GetColumnNameByFieldName(ByVal os As IObjectSchemaBase, ByVal field As String, Optional ByVal columnAliases As List(Of String) = Nothing) As String
        Return GetColumnNameByFieldNameInternal(os, field, True, columnAliases)
    End Function

    Public Function GetFieldTypeByName(ByVal type As Type, ByVal field As String) As Type
        'Dim t As Type = map(
        For Each de As DictionaryEntry In GetProperties(type)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
            If field = c.FieldName Then
                Return CType(de.Value, Reflection.PropertyInfo).PropertyType
            End If
        Next
        Throw New QueryGeneratorException("Type " & type.Name & " doesnot contain property " & field)
    End Function

    Public Function GetFieldNameByType(ByVal type As Type, ByVal fieldType As Type) As ICollection(Of String)
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        If fieldType Is Nothing Then
            Throw New ArgumentNullException("fieldType")
        End If

        Dim key As String = type.ToString & fieldType.ToString
        Dim l As List(Of String) = CType(_joins(key), List(Of String))
        If l Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                l = CType(_joins(key), List(Of String))
                If l Is Nothing Then
                    l = New List(Of String)
                    For Each de As DictionaryEntry In GetProperties(type)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If pi.PropertyType Is fieldType Then
                            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                            l.Add(c.FieldName)
                        End If
                    Next
                End If
            End Using
        End If
        Return l
    End Function

    'Protected Function GetColumnNameByFieldName4Filter(ByVal type As Type, ByVal field As String, Optional ByVal table_alias As Boolean = True, Optional ByVal pref As String = "") As String
    '    Return GetColumnNameByFieldNameInternal(type, field, table_alias)
    'End Function

    'Protected Function GetColumnNameByFieldName4Sort(ByVal type As Type, ByVal field As String, Optional ByVal table_alias As Boolean = True) As String
    '    Return GetColumnNameByFieldNameInternal(type, field, table_alias)
    'End Function

    Protected Function GetColumns4Select(ByVal type As Type, ByVal field As String, ByVal columnAliases As List(Of String)) As String
        Return GetColumnNameByFieldName(type, field, columnAliases)
    End Function

    Protected Function GetColumnsFromFieldType(ByVal main As Type, ByVal fieldtype As Type) As ColumnAttribute()
        If main Is Nothing Then Throw New ArgumentNullException("main")

        Dim l As New List(Of ColumnAttribute)

        For Each de As DictionaryEntry In GetProperties(main)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
            Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
            If pi.PropertyType Is fieldtype Then
                l.Add(c)
            End If
        Next
        Return l.ToArray
    End Function

    Public Function GetColumnByFieldName(ByVal main As Type, ByVal fieldName As String) As ColumnAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        'Dim l As New List(Of ColumnAttribute)

        For Each de As DictionaryEntry In GetProperties(main)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
            If c.FieldName = fieldName Then
                Return c
            End If
        Next
        Return Nothing
    End Function
#End Region

#Region " Unions "

    Public Shared Function GetUnions(ByVal type As Type) As String()
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim s() As String = Nothing
        'If GetType(Partner).IsAssignableFrom(type) Then
        '    Return New String() {TablePrefix & "sites"}
        'End If
        Return s
    End Function

    'Public Overridable Function GetUnionScope(ByVal type As Type, ByVal table As String) As Pair(Of Integer)
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type parameter cannot be nothing")
    '    End If

    '    'If GetType(Partner).IsAssignableFrom(type) Then
    '    '    If table = GetUnions(type)(0) Then
    '    '        Return New Utility.Pair(Of Integer)(1000000, 2000000)
    '    '    ElseIf table = Partner_GetTables(0) Then
    '    '        Return New Utility.Pair(Of Integer)(0, 1000000)
    '    '    End If
    '    'End If

    '    Throw New DBSchemaException("Unknown union " & table & " or type " & type.Name & " doesnot support unions")
    'End Function

    'Public Overridable Function MapUnionType2Table(ByVal type As Type, ByVal uniontype As String) As String
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type parameter cannot be nothing")
    '    End If

    '    'If GetType(Partner).IsAssignableFrom(type) Then
    '    '    If uniontype = PartnerType.Owner.ToString Then
    '    '        Return Partner_GetTables(0)
    '    '    ElseIf uniontype = PartnerType.Site.ToString Then
    '    '        Return GetUnions(type)(0)
    '    '    End If
    '    'End If

    '    Throw New DBSchemaException("Type " & type.Name & " doesnot support unions or unknown uniontype " & uniontype)
    'End Function

    'Public Overridable Function MapID2UnionType(ByVal type As Type, ByVal id As Integer) As String
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type parameter cannot be nothing")
    '    End If

    '    'If GetType(Partner).IsAssignableFrom(type) Then
    '    '    If 0 < id AndAlso id < 1000000 Then
    '    '        Return PartnerType.Owner.ToString
    '    '    ElseIf 1000000 < id AndAlso id < 2000000 Then
    '    '        Return PartnerType.Site.ToString
    '    '    End If
    '    'End If

    '    Throw New DBSchemaException("Type " & type.Name & " doesnot support unions or id " & id & " is out of scope")
    'End Function

    'Public Overridable Function GetUnionType(ByVal obj As OrmBase) As String
    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj parameter cannot be nothing")
    '    End If

    '    Dim type As Type = obj.GetType
    '    'If GetType(Partner).IsAssignableFrom(type) Then
    '    '    Return CType(obj, Partner).PartnerType.ToString
    '    'End If
    '    Throw New DBSchemaException("Type " & type.Name & " doesnot support unions")
    'End Function

#End Region

    Protected Function CreateObjectSchema(ByRef names As IDictionary) As IDictionary
        Dim t As Type = GetType(_IEntity)
        Dim idic As New Specialized.HybridDictionary
        names = New Specialized.HybridDictionary
        For Each assembly As Reflection.Assembly In AppDomain.CurrentDomain.GetAssemblies
            If assembly.ManifestModule.Name = "mscorlib.dll" OrElse assembly.ManifestModule.Name = "System.Data.dll" _
                OrElse assembly.ManifestModule.Name = "System.Xml.dll" OrElse assembly.ManifestModule.Name = "System.dll" _
                OrElse assembly.ManifestModule.Name = "System.Configuration.dll" OrElse assembly.ManifestModule.Name = "System.Web.dll" _
                OrElse assembly.ManifestModule.Name = "System.Drawing.dll" OrElse assembly.ManifestModule.Name = "System.Web.Services.dll" _
                OrElse assembly.FullName.Contains("Microsoft") Then
            Else
                For Each tp As Type In assembly.GetTypes
                    If tp.IsClass AndAlso t.IsAssignableFrom(tp) Then
                        Dim entities() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())

                        For Each ea As EntityAttribute In entities
                            If ea.Version = _version Then
                                Dim schema As IOrmObjectSchemaBase = Nothing

                                If ea.Type Is Nothing Then
                                    Dim l As New List(Of ColumnAttribute)
                                    For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                        l.Add(c)
                                    Next

                                    schema = New SimpleObjectSchema(tp, ea.TableName, l, ea.PrimaryKey)

                                    If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                        Throw New QueryGeneratorException(String.Format("Type {0} has neither table name nor schema", tp))
                                    End If
                                Else
                                    Try
                                        schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                    Catch ex As Exception
                                        Throw New QueryGeneratorException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
                                    End Try
                                End If

                                Dim n As IOrmSchemaInit = TryCast(schema, IOrmSchemaInit)
                                If n IsNot Nothing Then
                                    n.GetSchema(Me, tp)
                                End If

                                If Not String.IsNullOrEmpty(ea.EntityName) Then
                                    If names.Contains(ea.EntityName) Then
                                        Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea.EntityName), Pair(Of Type, EntityAttribute))
                                        If tt.First.IsAssignableFrom(tp) OrElse tt.Second.Version <> _version Then
                                            names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                                        End If
                                    Else
                                        names.Add(ea.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea))
                                    End If
                                End If

                                Try
                                    idic.Add(tp, schema)
                                Catch ex As ArgumentException
                                    Throw New QueryGeneratorException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                                End Try
                            End If
                        Next

                        If Not idic.Contains(tp) Then
                            Dim entities2() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())

                            For Each ea As EntityAttribute In entities2
                                If ea.Version = _version Then
                                    Dim schema As IOrmObjectSchemaBase = Nothing
                                    If ea.Type Is Nothing Then
                                        Dim l As New List(Of ColumnAttribute)
                                        For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                            l.Add(c)
                                        Next

                                        schema = New SimpleObjectSchema(tp, ea.TableName, l, ea.PrimaryKey)

                                        If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                            Throw New QueryGeneratorException(String.Format("Type {0} has neither table name nor schema", tp))
                                        End If
                                    Else
                                        Try
                                            schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                        Catch ex As Exception
                                            Throw New QueryGeneratorException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
                                        End Try
                                    End If

                                    Dim n As IOrmSchemaInit = TryCast(schema, IOrmSchemaInit)
                                    If n IsNot Nothing Then
                                        n.GetSchema(Me, tp)
                                    End If

                                    If Not String.IsNullOrEmpty(ea.EntityName) AndAlso entities.Length = 0 Then
                                        If names.Contains(ea.EntityName) Then
                                            Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea.EntityName), Pair(Of Type, EntityAttribute))
                                            If tt.First.IsAssignableFrom(tp) OrElse tt.Second.Version <> _version Then
                                                names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                                            End If
                                        Else
                                            names.Add(ea.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea))
                                        End If
                                    End If

                                    Try
                                        idic.Add(tp, schema)
                                        Exit For
                                    Catch ex As ArgumentException
                                        Throw New QueryGeneratorException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                                    End Try
                                End If
                            Next

                            If Not idic.Contains(tp) Then
                                'For Each ea As EntityAttribute In entities
                                Dim ea1 As EntityAttribute = Nothing
                                If entities.Length > 0 Then
                                    If _mapv IsNot Nothing Then
                                        ea1 = _mapv(_version, entities, tp)
                                    ElseIf entities.Length = 1 Then
                                        ea1 = entities(0)
                                    End If
                                End If
                                If ea1 IsNot Nothing Then
                                    Dim schema As IOrmObjectSchemaBase = Nothing
                                    If ea1.Type Is Nothing Then
                                        Dim l As New List(Of ColumnAttribute)
                                        For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                            l.Add(c)
                                        Next

                                        schema = New SimpleObjectSchema(tp, ea1.TableName, l, ea1.PrimaryKey)

                                        If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                            Throw New QueryGeneratorException(String.Format("Type {0} has neither table name nor schema", tp))
                                        End If
                                    Else
                                        Try
                                            schema = CType(ea1.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                        Catch ex As Exception
                                            Throw New QueryGeneratorException(String.Format("Cannot create type [{0}]", ea1.Type.ToString), ex)
                                        End Try
                                    End If

                                    Dim n As IOrmSchemaInit = TryCast(schema, IOrmSchemaInit)
                                    If n IsNot Nothing Then
                                        n.GetSchema(Me, tp)
                                    End If

                                    If Not String.IsNullOrEmpty(ea1.EntityName) Then
                                        If names.Contains(ea1.EntityName) Then
                                            Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea1.EntityName), Pair(Of Type, EntityAttribute))
                                            If tt.First.IsAssignableFrom(tp) OrElse (tt.Second.Version <> _version AndAlso _mapn IsNot Nothing) Then
                                                Dim e As EntityAttribute = Nothing
                                                If _mapn IsNot Nothing Then
                                                    e = _mapn(_version, New EntityAttribute() {ea1, tt.Second}, tp)
                                                End If
                                                If e IsNot tt.Second Then
                                                    names(ea1.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea1)
                                                End If
                                            End If
                                        Else
                                            names.Add(ea1.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea1))
                                        End If
                                    End If

                                    Try
                                        idic.Add(tp, schema)
                                    Catch ex As ArgumentException
                                        Throw New QueryGeneratorException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea1.Type), ex)
                                    End Try
                                End If
                                'Next

                                If Not idic.Contains(tp) Then
                                    Dim ea2 As EntityAttribute = Nothing
                                    If entities2.Length > 0 Then
                                        If _mapv IsNot Nothing Then
                                            ea2 = _mapv(_version, entities2, tp)
                                        ElseIf entities2.Length = 1 Then
                                            ea2 = entities2(0)
                                        End If
                                    End If
                                    If ea2 IsNot Nothing Then
                                        Dim schema As IOrmObjectSchemaBase = Nothing

                                        If ea2.Type Is Nothing Then
                                            Dim l As New List(Of ColumnAttribute)
                                            For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                                l.Add(c)
                                            Next

                                            schema = New SimpleObjectSchema(tp, ea2.TableName, l, ea2.PrimaryKey)

                                            If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                                Throw New QueryGeneratorException(String.Format("Type {0} has neither table name nor schema", tp))
                                            End If
                                        Else
                                            Try
                                                schema = CType(ea2.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                            Catch ex As Exception
                                                Throw New QueryGeneratorException(String.Format("Cannot create type [{0}]", ea2.Type.ToString), ex)
                                            End Try
                                        End If

                                        Dim n As IOrmSchemaInit = TryCast(schema, IOrmSchemaInit)
                                        If n IsNot Nothing Then
                                            n.GetSchema(Me, tp)
                                        End If

                                        If Not String.IsNullOrEmpty(ea2.EntityName) AndAlso entities.Length = 0 Then
                                            If names.Contains(ea2.EntityName) Then
                                                Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea2.EntityName), Pair(Of Type, EntityAttribute))
                                                If tt.First.IsAssignableFrom(tp) OrElse (tt.Second.Version <> _version AndAlso _mapn IsNot Nothing) Then
                                                    Dim e As EntityAttribute = Nothing
                                                    If _mapn IsNot Nothing Then
                                                        e = _mapn(_version, New EntityAttribute() {ea2, tt.Second}, tp)
                                                    End If
                                                    If e IsNot tt.Second Then
                                                        names(ea2.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea2)
                                                    End If
                                                End If
                                            Else
                                                names.Add(ea2.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea2))
                                            End If
                                        End If

                                        Try
                                            idic.Add(tp, schema)
                                            'Exit For
                                        Catch ex As ArgumentException
                                            Throw New QueryGeneratorException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea2.Type), ex)
                                        End Try
                                    End If
                                    'Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Next
        Return idic
    End Function

#Region " Public members "

    Public Function GetTypeByEntityName(ByVal name As String) As Type
        If name Is Nothing Then Throw New ArgumentNullException("name")
        Dim idic As IDictionary = CType(map("ObjectSchemaNames"), System.Collections.IDictionary)
        If idic Is Nothing Then
            Using SyncHelper.AcquireDynamicLock("ObjectSchemaNames")
                idic = CType(map("GetObjectSchema"), System.Collections.IDictionary)
                If idic Is Nothing Then
                    Dim names As IDictionary = Nothing
                    idic = CreateObjectSchema(names)
                    map("GetObjectSchema") = idic
                    map("ObjectSchemaNames") = names
                    idic = names
                End If
            End Using
        End If
        Dim p As Pair(Of Type, EntityAttribute) = CType(idic(name), Pair(Of Type, EntityAttribute))
        If p Is Nothing Then
            Return Nothing
        Else
            Return p.First
        End If
    End Function

    Public Function GetObjectSchema(ByVal t As Type) As IOrmObjectSchemaBase
        Return GetObjectSchema(t, True)
    End Function

    Protected Friend Function GetObjectSchema(ByVal t As Type, ByVal check As Boolean) As IOrmObjectSchemaBase
        If t Is Nothing Then Throw New ArgumentNullException("t")

        Dim idic As IDictionary = CType(map("GetObjectSchema"), System.Collections.IDictionary)
        If idic Is Nothing Then
            Using SyncHelper.AcquireDynamicLock("GetObjectSchema")
                idic = CType(map("GetObjectSchema"), System.Collections.IDictionary)
                If idic Is Nothing Then
                    Dim names As IDictionary = Nothing
                    idic = CreateObjectSchema(names)
                    map("GetObjectSchema") = idic
                    map("ObjectSchemaNames") = names
                End If
            End Using
        End If
        Dim schema As IOrmObjectSchemaBase = CType(idic(t), IOrmObjectSchemaBase)

        If schema Is Nothing AndAlso check Then
            Throw New ArgumentException(String.Format("Cannot find schema for type {0}", t))
        End If

        Return schema
    End Function

    Public ReadOnly Property Version() As String
        Get
            Return _version
        End Get
    End Property
#End Region

    Protected Function GetJoins(ByVal type As Type, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As OrmJoin Implements ISchemaWithJoins.GetJoins
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IOrmObjectSchema = CType(GetObjectSchema(type), IOrmObjectSchema)

        Return GetJoins(schema, left, right, filterInfo)
    End Function

    Protected Friend Function GetJoins(ByVal schema As IOrmRelationalSchema, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As OrmJoin
        If schema Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim adj As IGetJoinsWithContext = TryCast(schema, IGetJoinsWithContext)
        If adj IsNot Nothing Then
            Return adj.GetJoins(left, right, filterInfo)
        Else
            Return schema.GetJoins(left, right)
        End If
    End Function

    <Obsolete("Use GetSharedSourceFragment method")> _
    Public Function GetSharedTable(ByVal tableName As String) As SourceFragment 'Implements IDbSchema.GetSharedTable
        Dim t As SourceFragment = CType(_sharedTables(tableName), SourceFragment)
        If t Is Nothing Then
            SyncLock Me
                t = CType(_sharedTables(tableName), SourceFragment)
                If t Is Nothing Then
                    t = New SourceFragment(tableName)
                    _sharedTables.Add(tableName, t)
                End If
            End SyncLock
        End If
        Return t
    End Function

    Public Function GetSharedSourceFragment(ByVal schema As String, ByVal tableName As String) As SourceFragment
        Return GetSharedSourceFragment(schema, tableName, Nothing)
    End Function

    Public Function GetSharedSourceFragment(ByVal schema As String, ByVal tableName As String, ByVal key As String) As SourceFragment 'Implements IDbSchema.GetSharedTable
        If String.IsNullOrEmpty(key) Then
            key = schema & Selector & tableName
        End If
        Dim t As SourceFragment = CType(_sharedTables(key), SourceFragment)
        If t Is Nothing Then
            SyncLock Me
                t = CType(_sharedTables(key), SourceFragment)
                If t Is Nothing Then
                    t = New SourceFragment(schema, tableName)
                    _sharedTables.Add(key, t)
                End If
            End SyncLock
        End If
        Return t
    End Function

    Public Function GetTables(ByVal type As Type) As SourceFragment() 'Implements IDbSchema.GetTables
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IOrmObjectSchema = CType(GetObjectSchema(type), IOrmObjectSchema)

        Return schema.GetTables
    End Function

    Public Function GetTables(ByVal schema As IObjectSchemaBase) As SourceFragment()
        If schema Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Return schema.GetTables
    End Function

    Public Function ApplyFilter(Of T As OrmBase)(ByVal col As ICollection(Of T), ByVal filter As Criteria.Core.IFilter, ByRef r As Boolean) As ICollection(Of T)
        r = True
        Dim f As Criteria.Core.IEntityFilter = TryCast(filter, Criteria.Core.IEntityFilter)
        If f Is Nothing Then
            Return col
        Else
            Dim l As New List(Of T)
            Dim oschema As IOrmObjectSchemaBase = Nothing
            For Each o As T In col
                If oschema Is Nothing Then
                    oschema = Me.GetObjectSchema(o.GetType)
                End If
                Dim er As Criteria.Values.IEvaluableValue.EvalResult = f.Eval(Me, o, oschema)
                Select Case er
                    Case Criteria.Values.IEvaluableValue.EvalResult.Found
                        l.Add(o)
                    Case Criteria.Values.IEvaluableValue.EvalResult.Unknown
                        r = False
                        Exit For
                End Select
            Next
            Return l
        End If
    End Function

    Public Shared Function ExtractValues(ByVal schema As QueryGenerator, ByVal tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String), _
        ByVal _values() As Pair(Of Object, String)) As List(Of String)
        Dim values As New List(Of String)
        Dim lastt As Type = Nothing
        For Each p As Pair(Of Object, String) In _values
            Dim o As Object = p.First
            If o Is Nothing Then
                Throw New NullReferenceException
            End If

            If TypeOf o Is Type Then
                Dim t As Type = CType(o, Type)
                If Not GetType(OrmBase).IsAssignableFrom(t) Then
                    Throw New NotSupportedException(String.Format("Type {0} is not assignable from OrmBase", t))
                End If
                lastt = t

                Dim oschema As IOrmObjectSchema = CType(schema.GetObjectSchema(t), IOrmObjectSchema)
                Dim tbl As SourceFragment = Nothing
                Dim map As MapField2Column = Nothing
                Dim fld As String = p.Second
                If oschema.GetFieldColumnMap.TryGetValue(fld, map) Then
                    fld = map._columnName
                    tbl = map._tableName
                Else
                    tbl = oschema.GetTables(0)
                End If

                If tableAliases IsNot Nothing Then
                    Debug.Assert(tableAliases.ContainsKey(tbl), "There is not alias for table " & tbl.RawName)
                    Try
                        values.Add(tableAliases(tbl) & schema.Selector & fld)
                    Catch ex As KeyNotFoundException
                        Throw New QueryGeneratorException("There is not alias for table " & tbl.RawName, ex)
                    End Try
                Else
                    values.Add(schema.GetTableName(tbl) & schema.Selector & fld)
                End If
            ElseIf TypeOf o Is SourceFragment Then
                Dim tbl As SourceFragment = CType(o, SourceFragment)
                Dim [alias] As String = Nothing
                If tableAliases IsNot Nothing Then
                    Debug.Assert(tableAliases.ContainsKey(tbl), "There is not alias for table " & tbl.RawName)
                    Try
                        [alias] = tableAliases(tbl)
                    Catch ex As KeyNotFoundException
                        Throw New QueryGeneratorException("There is not alias for table " & tbl.RawName, ex)
                    End Try
                End If
                If Not String.IsNullOrEmpty([alias]) Then
                    values.Add([alias] & schema.Selector & p.Second)
                Else
                    values.Add(p.Second)
                End If
            ElseIf o Is Nothing Then
                values.Add(p.Second)
            Else
                Throw New NotSupportedException(String.Format("Type {0} is not supported", o.GetType))
            End If
        Next
        Return values
    End Function

    Public MustOverride ReadOnly Property Selector() As String
    Public MustOverride Function CreateCriteria(ByVal t As Type) As Criteria.ICtor
    Public MustOverride Function CreateCriteria(ByVal t As Type, ByVal fieldName As String) As Criteria.CriteriaField
    Public MustOverride Function CreateCriteria(ByVal table As SourceFragment) As Criteria.ICtor
    Public MustOverride Function CreateCriteria(ByVal table As SourceFragment, ByVal columnName As String) As Criteria.CriteriaColumn
    Public MustOverride Function CreateCustom(ByVal format As String, ByVal value As Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String)) As Worm.Criteria.Core.CustomFilterBase
    Public MustOverride Function CreateConditionCtor() As Criteria.Conditions.Condition.ConditionConstructorBase
    Public MustOverride Function CreateCriteriaLink(ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase) As Criteria.CriteriaLink
    Public MustOverride Function CreateTopAspect(ByVal top As Integer) As Worm.Orm.Query.TopAspect
    Public MustOverride Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sorting.Sort) As Worm.Orm.Query.TopAspect
    Public MustOverride Function GetTableName(ByVal t As SourceFragment) As String
    Public MustOverride Function CreateExecutor() As Worm.Query.IExecutor

    Protected Friend MustOverride Function MakeJoin(ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
        ByVal oper As Criteria.FilterOperation, ByVal joinType As JoinType, Optional ByVal switchTable As Boolean = False) As OrmJoin
    Protected Friend MustOverride Function MakeM2MJoin(ByVal m2m As M2MRelation, ByVal type2join As Type) As OrmJoin()
End Class

'End Namespace
