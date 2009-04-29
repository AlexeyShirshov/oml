Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Entities
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports Worm.Criteria
Imports Worm.Criteria.Conditions
Imports Worm.Query

'Namespace Schema

''' <summary>
''' Данное исключение выбрасывается при определеных ошибках в <see cref="ObjectMappingEngine" />
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public NotInheritable Class ObjectMappingException
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

'''' <summary>
'''' Интерфейс расширяющий схему для работы с объдинениями (joins)
'''' </summary>
'''' <remarks></remarks>
'Public Interface ISchemaWithJoins
'    'Function GetSharedTable(ByVal tableName As String) As SourceFragment
'    'Function GetTables(ByVal type As Type) As SourceFragment()
'    ''' <summary>
'    ''' Метод используется для получения объекта типа <see cref="QueryJoin" /> для определеной таблицы определеного типа
'    ''' </summary>
'    ''' <param name="type">Тип объекта</param>
'    ''' <param name="left">Левая таблица. Какая либо из списка <see cref="IMultiTableObjectSchema.GetTables"/></param>
'    ''' <param name="right">Правая таблица. Какая либо из списка <see cref="IMultiTableObjectSchema.GetTables"/></param>
'    ''' <param name="filterInfo">Произвольный объект, используемый реализацией. Передается из <see cref="OrmManager.GetFilterInfo" /></param>
'    ''' <returns>Объекта типа <see cref="QueryJoin" /></returns>
'    ''' <remarks>Используется для генерации запросов</remarks>
'    Function GetJoins(ByVal type As Type, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
'End Interface

''' <summary>
''' Класс хранения и управления схемами объектов <see cref="IEntitySchema"/>
''' </summary>
''' <remarks>Класс управляет версиями схем объектов, предоставляет удобные обертки для методов
''' <see cref="IEntitySchema"/> через тип объекта.</remarks>
Public Class ObjectMappingEngine
    'Implements ISchemaWithJoins

    Public Delegate Function ResolveEntity(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
    Public Delegate Function ResolveEntityName(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute

    Private _sharedTables As Hashtable = Hashtable.Synchronized(New Hashtable)
    Protected map As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected sel As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected _joins As IDictionary = Hashtable.Synchronized(New Hashtable)
    Private _version As String
    Private _mapv As ResolveEntity
    Private _mapn As ResolveEntityName

    Public ReadOnly Mark As Guid = Guid.NewGuid

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
        Return GetProperties(t, GetEntitySchema(t, False))
    End Function

    'Protected Friend Function GetProperties(ByVal t As Type, ByVal schema As IOrmObjectSchema) As IDictionary
    '    Return GetProperties(t, schema)
    'End Function

    Public Shared Function GetMappedProperties(ByVal t As Type) As IDictionary
        Return GetMappedProperties(t, Nothing, Nothing, True)
    End Function

    Public Shared Function GetMappedProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
        Dim propertyMap As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
        If schema IsNot Nothing Then propertyMap = schema.GetFieldColumnMap()

        Return GetMappedProperties(t, schema, propertyMap, False)
    End Function

    Public Shared Function GetMappedProperties(ByVal t As Type, ByVal schema As IEntitySchema, _
        ByVal propertyMap As Collections.IndexedCollection(Of String, MapField2Column), ByVal raw As Boolean) As IDictionary
        Dim result As New Hashtable

        Dim sup() As String = Nothing
        If schema IsNot Nothing Then
            Dim s As IEntitySchemaBase = TryCast(schema, IEntitySchemaBase)
            If s IsNot Nothing Then sup = s.GetSuppressedFields()
        End If

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)
            Dim column As EntityPropertyAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), EntityPropertyAttribute)
            If column Is Nothing Then
                Dim propertyAlias As String = pi.Name
                If propertyMap IsNot Nothing Then
                    If propertyMap.ContainsKey(propertyAlias) Then
                        Dim mc As MapField2Column = propertyMap(propertyAlias)
                        column = New EntityPropertyAttribute(mc._newattributes)
                        column.Column = mc.Column
                        column.PropertyAlias = mc._propertyAlias
                    End If
                ElseIf raw AndAlso pi.CanWrite AndAlso pi.CanRead Then
                    column = New EntityPropertyAttribute(propertyAlias, String.Empty)
                    column.Column = propertyAlias
                End If
            End If

            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.PropertyAlias) Then
                    column.PropertyAlias = pi.Name
                End If

                If String.IsNullOrEmpty(column.Column) Then
                    column.Column = pi.Name
                End If

                If propertyMap IsNot Nothing Then
                    If (column.Behavior = Field2DbRelations.None) AndAlso propertyMap.ContainsKey(column.PropertyAlias) Then
                        column.Behavior = propertyMap(column.PropertyAlias)._newattributes
                    End If
                End If

                If (sup Is Nothing OrElse Array.IndexOf(sup, column.PropertyAlias) < 0) AndAlso (propertyMap Is Nothing OrElse propertyMap.ContainsKey(column.PropertyAlias)) Then
                    result.Add(column, pi)
                End If
            End If
        Next

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
            Dim column As EntityPropertyAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), EntityPropertyAttribute)

            If column Is Nothing Then
                Dim propertyAlias As String = pi.Name
                If propertyMap IsNot Nothing Then
                    If propertyMap.ContainsKey(propertyAlias) AndAlso (pi.Name <> OrmBaseT.PKName OrElse pi.DeclaringType.Name <> GetType(OrmBaseT(Of )).Name) Then
                        Dim mc As MapField2Column = propertyMap(propertyAlias)
                        column = New EntityPropertyAttribute(mc._newattributes)
                        column.Column = mc.Column
                        column.PropertyAlias = mc._propertyAlias
                    End If
                ElseIf raw AndAlso pi.CanWrite AndAlso pi.CanRead Then
                    column = New EntityPropertyAttribute(propertyAlias, String.Empty)
                    column.Column = propertyAlias
                End If
            End If

            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.PropertyAlias) Then
                    column.PropertyAlias = pi.Name
                End If
                If Not result.Contains(column) AndAlso (sup Is Nothing OrElse Array.IndexOf(sup, column.PropertyAlias) < 0) _
                    AndAlso (propertyMap Is Nothing OrElse propertyMap.ContainsKey(column.PropertyAlias)) Then

                    If propertyMap IsNot Nothing Then
                        If (column.Behavior = Field2DbRelations.None) AndAlso propertyMap.ContainsKey(column.PropertyAlias) Then
                            column.Behavior = propertyMap(column.PropertyAlias)._newattributes
                        End If
                    End If

                    result.Add(column, pi)
                End If
            End If
        Next

        Return result
    End Function

    Public Function GetRefProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IList
        If t Is Nothing Then Throw New ArgumentNullException("t")
        Dim s As String = Nothing
        If schema Is Nothing Then
            s = t.ToString
        Else
            s = t.ToString & schema.GetType().ToString
        End If
        Dim key As String = "refproperties" & s
        Dim d As IList = CType(map(key), IList)
        If d Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                d = CType(map(key), IList)
                If d Is Nothing Then
                    Dim dic As IDictionary = GetProperties(t, schema)
                    d = New ArrayList
                    For Each tde As DictionaryEntry In dic
                        Dim pi As Reflection.PropertyInfo = CType(tde.Value, Reflection.PropertyInfo)
                        Dim pit As Type = pi.PropertyType
                        If ObjectMappingEngine.IsEntityType(pit, Me) Then
                            d.Add(tde)
                        End If
                    Next
                End If
            End Using
        End If
        Return d
    End Function

    Public Function GetProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
        If t Is Nothing Then Throw New ArgumentNullException("t")
        Dim s As String = Nothing
        If schema Is Nothing Then
            s = t.ToString
        Else
            s = t.ToString & schema.GetType().ToString
        End If
        Dim key As String = "properties" & s
        Dim h As IDictionary = CType(map(key), IDictionary)
        If h Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                h = CType(map(key), IDictionary)
                If h Is Nothing Then
                    h = GetMappedProperties(t, schema)

                    map(key) = h
                End If
            End Using
        End If
        Return h
    End Function

#End Region

#Region " object functions "

    'Public Function GetAllJoins(ByVal t As Type) As ICollection(Of QueryJoin)
    '    Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

    '    Dim tbls() As SourceFragment = schema.GetTables
    '    Dim tbl As SourceFragment = tbls(0)
    '    Dim js As New List(Of QueryJoin)
    '    For i As Integer = 1 To tbls.Length - 1
    '        Dim j As QueryJoin = schema.GetJoins(tbl, tbls(i))
    '        js.Add(j)
    '    Next
    '    Return js.ToArray
    'End Function

    Public Function GetMappedFields(ByVal schema As IEntitySchema) As ICollection(Of MapField2Column)
        Dim sup() As String = Nothing
        Dim s As IEntitySchemaBase = TryCast(schema, IEntitySchemaBase)
        If s IsNot Nothing Then sup = s.GetSuppressedFields()
        If sup Is Nothing OrElse sup.Length = 0 Then
            Return schema.GetFieldColumnMap.Values
        End If
        Dim l As New List(Of MapField2Column)
        For Each m As MapField2Column In schema.GetFieldColumnMap
            If Array.IndexOf(sup, m._propertyAlias) < 0 Then
                l.Add(m)
            End If
        Next
        Return l
    End Function

    Public Function GetEntityKey(ByVal filterInfo As Object, ByVal t As Type) As String
        Dim schema As IEntitySchema = GetEntitySchema(t, False)

        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityKey(filterInfo)
        Else
            Return t.ToString
        End If
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object, ByVal t As Type) As Object
        Return GetEntityTypeKey(filterInfo, t, GetEntitySchema(t))
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As IEntitySchema) As Object
        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityTypeKey(filterInfo)
        Else
            Return t
        End If
    End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    Protected Function GetPropertyAliasByColumnName(ByVal type As Type, ByVal columnName As String) As String

        If String.IsNullOrEmpty(columnName) Then Throw New ArgumentNullException("columnName")

        Dim schema As IEntitySchema = GetEntitySchema(type)

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        For Each p As MapField2Column In coll
            If p.Column = columnName Then
                Return p._propertyAlias
            End If
        Next

        Throw New ObjectMappingException("Cannot find column: " & columnName)
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

    Public Function GetM2MRelations(ByVal s As IEntitySchema) As M2MRelationDesc()
        Dim schema As ISchemaWithM2M = TryCast(s, ISchemaWithM2M)
        If schema IsNot Nothing Then
            Return schema.GetM2MRelations
        Else
            Return New M2MRelationDesc() {}
        End If
    End Function

    Public Function GetM2MRelations(ByVal maintype As Type) As M2MRelationDesc()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Return GetM2MRelations(GetEntitySchema(maintype))
    End Function

    Public Function GetM2MRelationsForEdit(ByVal maintype As Type) As M2MRelationDesc()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Dim sch As IEntitySchema = GetEntitySchema(maintype)
        Dim editable As IReadonlyObjectSchema = TryCast(sch, IReadonlyObjectSchema)
        Dim schema As ISchemaWithM2M = Nothing
        If editable IsNot Nothing AndAlso (editable.SupportedOperation And IReadonlyObjectSchema.Operation.M2M) = IReadonlyObjectSchema.Operation.M2M Then
            schema = TryCast(editable.GetEditableSchema, ISchemaWithM2M)
        Else
            schema = TryCast(sch, ISchemaWithM2M)
        End If
        If schema IsNot Nothing Then
            Dim m As M2MRelationDesc() = schema.GetM2MRelations
            If m Is Nothing AndAlso editable IsNot Nothing Then
                schema = TryCast(sch, ISchemaWithM2M)
                If schema IsNot Nothing Then
                    m = schema.GetM2MRelations
                End If
            End If
            Return m
        Else
            Return Nothing
        End If
    End Function

    Public Function GetM2MRelation(ByVal oschema As IEntitySchema, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        Return GetM2MRel(GetM2MRelations(oschema), subtype, key)
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        'If String.IsNullOrEmpty(key) Then key = M2MRelation.DirKey
        'Dim mr() As M2MRelation = GetM2MRelations(maintype)
        'For Each r As M2MRelation In mr
        '    If r.Type Is subtype AndAlso String.Equals(r.Key, key) Then
        '        Return r
        '    End If
        'Next

        'Dim en As String = GetEntityNameByType(subtype)
        'If Not String.IsNullOrEmpty(en) Then
        '    Dim rt As Type = GetTypeByEntityName(en)
        '    For Each r As M2MRelation In mr
        '        If rt Is r.Type AndAlso String.Equals(r.Key, key) Then
        '            Return r
        '        End If
        '    Next
        'End If

        'Return Nothing
        Return GetM2MRel(GetM2MRelations(maintype), subtype, key)
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal mainSchema As IEntitySchema, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        'If String.IsNullOrEmpty(key) Then key = M2MRelationDesc.DirKey
        Dim mr() As M2MRelationDesc = GetM2MRelations(mainSchema)
        Return GetM2MRel(mr, subtype, key)
    End Function

    Private Function GetM2MRel(ByVal mr() As M2MRelationDesc, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        'If String.IsNullOrEmpty(key) Then key = M2MRelationDesc.DirKey
        For Each r As M2MRelationDesc In mr
            If r.Rel.GetRealType(Me) Is subtype AndAlso (String.Equals(r.Key, key) orelse M2MRelationDesc.IsDirect(r.key) = M2MRelationDesc.IsDirect(key)) Then
                Return r
            End If
        Next

        For Each r As M2MRelationDesc In mr
            Dim n As String = r.EntityName
            If String.IsNullOrEmpty(n) Then
                n = GetEntityNameByType(r.Rel.GetRealType(Me))
                If Not String.IsNullOrEmpty(n) Then
                    Dim n2 As String = GetEntityNameByType(subtype)
                    If String.Equals(n, n2) AndAlso String.Equals(r.Key, key) Then
                        Return r
                    End If
                End If
            Else
                Dim n2 As String = GetEntityNameByType(subtype)
                If String.Equals(n, n2) AndAlso String.Equals(r.Key, key) Then
                    Return r
                End If
            End If
        Next

        Return Nothing
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal subtype As Type, ByVal direct As Boolean) As M2MRelationDesc
        Return GetM2MRel(GetM2MRelations(maintype), subtype, M2MRelationDesc.GetKey(direct))
        'Dim mr() As M2MRelation = GetM2MRelations(maintype)
        'For Each r As M2MRelation In mr
        '    If r.Type Is subtype AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
        '        Return r
        '    End If
        'Next

        'Dim en As String = GetEntityNameByType(maintype)
        'If Not String.IsNullOrEmpty(en) Then
        '    For Each r As M2MRelation In mr
        '        Dim n As String = GetEntityNameByType(r.Type)
        '        If String.Equals(en, n) AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
        '            Return r
        '        End If
        '    Next
        'End If

        'Return Nothing
    End Function

    Public Function GetM2MRelationForEdit(ByVal maintype As Type, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        'For Each r As M2MRelation In GetM2MRelationsForEdit(maintype)
        '    If r.Type Is subtype AndAlso (maintype IsNot subtype OrElse r.Key = direct) Then
        '        Return r
        '    End If
        'Next

        'Return Nothing
        Return GetM2MRel(GetM2MRelationsForEdit(maintype), subtype, key)
    End Function

    Public Function GetRevM2MRelation(ByVal mainType As Type, ByVal subType As Type, ByVal key As String) As M2MRelationDesc
        If mainType Is subType Then
            Return GetM2MRelation(subType, mainType, M2MRelationDesc.GetRevKey(key))
        Else
            Return GetM2MRelation(subType, mainType, key)
        End If
    End Function

    Public Function GetRevM2MRelation(ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean) As M2MRelationDesc
        If mainType Is subType Then
            Return GetM2MRelation(subType, mainType, Not direct)
        Else
            Return GetM2MRelation(subType, mainType, True)
        End If
    End Function

    Public Function IsMany2ManyReadonly(ByVal maintype As Type, ByVal subtype As Type) As Boolean
        Dim r As M2MRelationDesc = GetM2MRelation(maintype, subtype, True)

        If r Is Nothing Then
            Throw New ArgumentException(String.Format("Relation between {0} and {1} is not exists", maintype, subtype))
        Else
            Return r.ConnectedType IsNot Nothing
        End If
    End Function

    Public Function GetEntityNameByType(ByVal t As Type) As String
        Dim dic As IDictionary = CType(map("mthdEntityByType"), System.Collections.IDictionary)
        If dic Is Nothing Then
            dic = Hashtable.Synchronized(New Hashtable)
            map("mthdEntityByType") = dic
        End If
        If dic.Contains(t) Then
            Return CStr(dic(t))
        End If
        Dim ver As String = Nothing
        Dim a() As Attribute = Attribute.GetCustomAttributes(t, GetType(EntityAttribute))
        For Each ea As EntityAttribute In a
            If ea.Version = _version Then
                ver = ea.EntityName
                Exit For
            End If
        Next

        If String.IsNullOrEmpty(ver) AndAlso _mapv IsNot Nothing Then
            Dim ea As EntityAttribute = _mapv(_version, CType(a, EntityAttribute()), t)
            If ea IsNot Nothing Then
                ver = ea.EntityName
            End If
        End If

        dic(t) = ver
        Return ver
    End Function

    Public Function GetConnectedType(ByVal maintype As Type, ByVal subtype As Type) As Type
        Dim r As M2MRelationDesc = GetM2MRelation(maintype, subtype, True)

        If r Is Nothing Then
            Throw New ArgumentException(String.Format("Relation between {0} and {1} is not exists", maintype, subtype))
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
        Return GetConnectedTypeField(ct, t, M2MRelationDesc.GetKey(direction))
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

        Dim schema As IEntitySchema = GetEntitySchema(ct)

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
    Public Function ChangeValueType(ByVal type As Type, ByVal c As EntityPropertyAttribute, ByVal o As Object) As Object
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Return ChangeValueType(GetEntitySchema(type), c, o)
    End Function

    Public Function ChangeValueType(ByVal s As IEntitySchema, ByVal c As EntityPropertyAttribute, ByVal o As Object) As Object
        If s Is Nothing Then
            Throw New ArgumentNullException("s")
        End If

        Dim schema As IEntitySchemaBase = TryCast(s, IEntitySchemaBase)

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

        If schema IsNot Nothing AndAlso schema.ChangeValueType(c, o, v) Then
            Return v
        End If

        If GetType(System.Xml.XmlDocument) Is ot Then
            Return CType(o, System.Xml.XmlDocument).OuterXml
        End If

        If GetType(System.Xml.XmlDocumentFragment) Is ot Then
            Return CType(o, System.Xml.XmlDocumentFragment).OuterXml
        End If

        If GetType(IKeyEntity).IsAssignableFrom(ot) Then
            Return CType(o, KeyEntity).Identifier
        ElseIf GetType(ICachedEntity).IsAssignableFrom(ot) Then
            Dim pks() As PKDesc = CType(o, ICachedEntity).GetPKValues
            If pks.Length <> 1 Then
                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", ot))
            End If
            Return pks(0).Value
        End If

        If IsEntityType(ot, Me) Then
            Dim pks As IList(Of EntityPropertyAttribute) = GetPrimaryKeys(ot)
            If pks.Count <> 1 Then
                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", ot))
            End If
            Return GetPropertyValue(o, pks(0).PropertyAlias, Nothing)
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
    Public Function GetFieldTable(ByVal schema As IPropertyMap, ByVal field As String) As SourceFragment
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
        Try
            Return coll(field).Table
        Catch ex As Exception
            Throw New ObjectMappingException("Unknown field name: " & field, ex)
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

    Public Function ExternalSort(Of T As {_IEntity})(ByVal mgr As OrmManager, _
        ByVal sort As Sort, ByVal objs As IList(Of T)) As ReadOnlyObjectList(Of T)
        Return sort.ExternalSort(Of T)(mgr, Me, objs)
        'Dim schema As IOrmObjectSchemaBase = GetObjectSchema(GetType(T))
        'Dim s As IOrmSorting = TryCast(schema, IOrmSorting)
        'If s Is Nothing Then
        '    Throw New OrmSchemaException(String.Format("Type {0} is not support sorting", GetType(T)))
        'End If
        'Return s.ExternalSort(Of T)(sort, objs)
    End Function

    Public Function GetJoinSelectMapping(ByVal t As Type, ByVal subType As Type) As System.Data.Common.DataTableMapping
        Dim r As M2MRelationDesc = GetM2MRelation(t, subType, True)

        If r Is Nothing Then
            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", t.Name, subType.Name))
        End If

        Return r.Mapping
    End Function

    Public Function GetDBType(ByVal type As Type, ByVal os As IEntitySchema, _
                                         ByVal c As EntityPropertyAttribute) As DBType
        Dim db As DBType = os.GetFieldColumnMap()(c.PropertyAlias).DBType
        If db.IsEmpty Then
            db = c.SourceType
        End If
        Return db
    End Function

    Public Function GetAttributes(ByVal type As Type, ByVal c As EntityPropertyAttribute) As Field2DbRelations
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IEntitySchema = GetEntitySchema(type)

        Return GetAttributes(schema, c)
    End Function

    Public Function GetAttributes(ByVal schema As IEntitySchema, ByVal c As EntityPropertyAttribute) As Field2DbRelations
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Return schema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c)
    End Function
#End Region

#Region " Helpers "
    Public Function HasProperty(ByVal t As Type, ByVal propertyAlias As String) As Boolean
        If String.IsNullOrEmpty(propertyAlias) Then Return False

        Dim schema As IEntitySchema = GetEntitySchema(t, False)

        If schema IsNot Nothing Then
            Return schema.GetFieldColumnMap.ContainsKey(propertyAlias)
        Else
            For Each de As DictionaryEntry In GetMappedProperties(t)
                If CType(de.Key, EntityPropertyAttribute).PropertyAlias = propertyAlias Then
                    Return True
                End If
            Next
        End If

        Return False
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

    Protected Friend Function GetProperty(ByVal original_type As Type, ByVal c As EntityPropertyAttribute) As Reflection.PropertyInfo
        Return CType(GetProperties(original_type)(c), Reflection.PropertyInfo)
    End Function

    Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal c As EntityPropertyAttribute) As Reflection.PropertyInfo
        Return CType(GetProperties(t, schema)(c), Reflection.PropertyInfo)
    End Function

    Protected Friend Function GetProperty(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return GetProperty(original_type, New EntityPropertyAttribute(propertyAlias, String.Empty))
    End Function

    Public Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return GetProperty(t, schema, New EntityPropertyAttribute(propertyAlias, String.Empty))
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return CType(GetMappedProperties(original_type, Nothing)(New EntityPropertyAttribute(propertyAlias, String.Empty)), Reflection.PropertyInfo)
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal t As Type, ByVal oschema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return CType(GetMappedProperties(t, oschema)(New EntityPropertyAttribute(propertyAlias, String.Empty)), Reflection.PropertyInfo)
    End Function

    Public Function GetSortedFieldList(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As Generic.List(Of EntityPropertyAttribute)
        'If Not GetType(_ICachedEntity).IsAssignableFrom(original_type) Then
        '    Return Nothing
        'End If

        If schema Is Nothing Then
            schema = GetEntitySchema(original_type)
        End If
        Dim cl_type As String = "columnlist" & original_type.ToString & schema.GetType.ToString

        Dim arr As Generic.List(Of EntityPropertyAttribute) = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
        If arr Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(cl_type)
                arr = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of EntityPropertyAttribute)

                    For Each c As EntityPropertyAttribute In GetProperties(original_type, schema).Keys
                        arr.Add(c)
                    Next

                    arr.Sort()

                    map.Add(cl_type, arr)
                End If
            End Using
        End If
        Return arr
    End Function

    '<MethodImpl(MethodImplOptions.Synchronized)> _

    Public Function GetPrimaryKeys(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As List(Of EntityPropertyAttribute)
        Dim cl_type As String = "clm_pklist" & original_type.ToString

        Dim arr As Generic.List(Of EntityPropertyAttribute) = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))

        If arr Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(cl_type)
                arr = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of EntityPropertyAttribute)

                    For Each c As EntityPropertyAttribute In GetSortedFieldList(original_type, schema)
                        Dim att As Field2DbRelations
                        If schema IsNot Nothing Then
                            att = GetAttributes(schema, c)
                        Else
                            att = GetAttributes(original_type, c)
                        End If
                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
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

    '    For Each c As EntityPropertyAttribute In GetSortedFieldList(type)
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

    Public Shared Function GetPropertyValueSchemaless(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal pi As Reflection.PropertyInfo) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
            If pi Is Nothing Then
                If schema Is Nothing Then
                    pi = GetPropertyInt(obj.GetType, propertyAlias)
                Else
                    pi = GetPropertyInt(obj.GetType, schema, propertyAlias)
                End If
            End If

            If pi Is Nothing Then
                Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
            End If

            Return pi.GetValue(obj, Nothing)
        Else
            If obj.IsPropertyLoaded(propertyAlias) Then
                Return ov.GetValueOptimized(propertyAlias, schema)
            Else
                Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
                If ll IsNot Nothing Then
                    Using ll.Read(propertyAlias)
                        Return ov.GetValueOptimized(propertyAlias, schema)
                    End Using
                Else
                    Return ov.GetValueOptimized(propertyAlias, schema)
                End If
            End If
        End If
    End Function

    Public Function GetPropertyValue(ByVal obj As _IEntity, ByVal propertyAlias As String) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim schema As IEntitySchema = GetEntitySchema(obj.GetType)

        Return GetPropertyValue(obj, propertyAlias, schema)
        'Dim pi As Reflection.PropertyInfo = Nothing

        'If schema IsNot Nothing Then
        '    pi = GetProperty(obj.GetType, schema, propertyAlias)
        'Else
        '    pi = GetProperty(obj.GetType, propertyAlias)
        'End If

        'If pi Is Nothing Then
        '    Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
        'End If

        'Return GetFieldValue(obj, propertyAlias, pi, schema)
    End Function

    'Public Function GetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema) As Object
    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj")
    '    End If

    '    Dim pi As Reflection.PropertyInfo = Nothing

    '    If schema IsNot Nothing Then
    '        pi = GetProperty(obj.GetType, schema, propertyAlias)
    '    Else
    '        pi = GetProperty(obj.GetType, propertyAlias)
    '    End If

    '    If pi Is Nothing Then
    '        Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
    '    End If

    '    Return GetFieldValue(obj, propertyAlias, pi, schema)
    'End Function

    Public Function GetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, _
        ByVal schema As IEntitySchema, Optional ByVal pi As Reflection.PropertyInfo = Nothing) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
            If pi Is Nothing Then
                'If schema IsNot Nothing Then
                pi = GetProperty(obj.GetType, schema, propertyAlias)
                'Else
                '    pi = GetProperty(obj.GetType, propertyAlias)
                'End If
            End If

            If pi Is Nothing Then
                Throw New ArgumentException(String.Format("Type {0} doesnot contain field {1}", obj.GetType, propertyAlias))
            End If

            Return pi.GetValue(obj, Nothing)
        Else
            Dim e As _IEntity = TryCast(obj, _IEntity)
            If e IsNot Nothing AndAlso e.IsPropertyLoaded(propertyAlias) Then
                Return ov.GetValueOptimized(propertyAlias, schema)
            Else
                Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
                If ll IsNot Nothing Then
                    Using ll.Read(propertyAlias)
                        Return ov.GetValueOptimized(propertyAlias, schema)
                    End Using
                Else
                    Return ov.GetValueOptimized(propertyAlias, schema)
                End If
            End If
        End If
    End Function

    Public Shared Function GetPropertyValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal pi As Reflection.PropertyInfo, ByVal oschema As IEntitySchema) As Object
        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
            If pi Is Nothing Then
                Throw New ArgumentNullException("pi")
            End If

            'Using obj.SyncHelper(True, propertyAlias)
            Return pi.GetValue(obj, Nothing)
            'Return obj.GetValueOptimized(pi, propertyAlias, oschema)
            'End Using
        Else
            If obj.IsPropertyLoaded(propertyAlias) Then
                Return ov.GetValueOptimized(propertyAlias, oschema)
            Else
                Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
                If ll IsNot Nothing Then
                    Using ll.Read(propertyAlias)
                        Return ov.GetValueOptimized(propertyAlias, oschema)
                    End Using
                Else
                    Return ov.GetValueOptimized(propertyAlias, oschema)
                End If
            End If
        End If
    End Function

    'Public Shared Function GetFieldValue(ByVal obj As _IEntity, ByVal c As EntityPropertyAttribute, ByVal pi As Reflection.PropertyInfo, ByVal oschema As IOrmObjectSchemaBase) As Object
    '    If pi Is Nothing Then
    '        Throw New ArgumentNullException("pi")
    '    End If

    '    Using obj.SyncHelper(True, c.FieldName)
    '        'Return pi.GetValue(obj, Nothing)
    '        Return obj.GetValue(pi, c, oschema)
    '    End Using
    'End Function

    Public Sub SetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, ByVal value As Object, ByVal oschema As IEntitySchema)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
            'Dim pi As Reflection.PropertyInfo = Nothing
            'If oschema Is Nothing Then

            'Else
            '    pi = GetProperty(obj.GetType, oschema, propertyAlias)
            'End If
            Dim pi As Reflection.PropertyInfo = GetProperty(obj.GetType, oschema, propertyAlias)

            If pi Is Nothing Then
                Throw New ArgumentException(String.Format("Type {0} doesnot contain field {1}", obj.GetType, propertyAlias))
            End If

            'Using obj.SyncHelper(False, propertyAlias)
            'obj.SetValueOptimized(pi, propertyAlias, oschema, value)
            pi.SetValue(obj, value, Nothing)
            'End Using
        Else
            'If obj.IsLoaded Then
            '    ov.SetValueOptimized(propertyAlias, oschema, value)
            'Else
            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
            If ll IsNot Nothing Then
                Using ll.Write(propertyAlias)
                    ov.SetValueOptimized(propertyAlias, oschema, value)
                End Using
            Else
                ov.SetValueOptimized(propertyAlias, oschema, value)
            End If
            'End If
        End If

    End Sub

    Public Shared Sub SetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, ByVal pi As Reflection.PropertyInfo, ByVal value As Object, ByVal oschema As IEntitySchema)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
            If pi Is Nothing Then
                Throw New ArgumentException(String.Format("Type {0} doesnot contain field {1}", obj.GetType, propertyAlias))
            End If

            'Using obj.SyncHelper(False, propertyAlias)
            'obj.SetValueOptimized(pi, propertyAlias, oschema, value)
            pi.SetValue(obj, value, Nothing)
            'End Using
        Else
            'If obj.IsLoaded Then
            '    ov.SetValueOptimized(propertyAlias, oschema, value)
            'Else
            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
            If ll IsNot Nothing Then
                Using ll.Write(propertyAlias)
                    ov.SetValueOptimized(propertyAlias, oschema, value)
                End Using
            Else
                ov.SetValueOptimized(propertyAlias, oschema, value)
            End If
            'End If
        End If
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

    'Protected Function GetSelectColumnList(ByVal type As Type, ByVal table As String) As String
    '    Dim sb As New StringBuilder
    '    For Each c As EntityPropertyAttribute In GetSortedFieldList(type)
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

    Public Shared Function ConvertColumn2SelExp(ByVal c As EntityPropertyAttribute, ByVal t As Type) As SelectExpression
        Dim se As New SelectExpression(t, c.PropertyAlias)
        se.Column = c.Column
        se.Attributes = c.Behavior
        Return se
    End Function

    Public Shared Function ConvertColumn2SelExp(ByVal c As EntityPropertyAttribute, ByVal os As EntityUnion) As SelectExpression
        Dim se As New SelectExpression(os, c.PropertyAlias)
        se.Column = c.Column
        se.Attributes = c.Behavior
        Return se
    End Function

    Protected Friend Function GetJoinFieldNameByType(ByVal mainType As Type, ByVal subType As Type, ByVal oschema As IEntitySchema) As String
        Dim j As IJoinBehavior = TryCast(oschema, IJoinBehavior)
        Dim r As String = Nothing
        If j IsNot Nothing Then
            r = j.GetJoinField(subType)
        End If
        If String.IsNullOrEmpty(r) Then
            Dim c As ICollection(Of String) = GetPropertyAliasByType(mainType, subType, oschema)
            If c.Count = 1 Then
                For Each s As String In c
                    r = s
                Next
            End If
        End If
        Return r
    End Function

    Public Function GetJoinObj(ByVal oschema As IEntitySchema, _
        ByVal obj As _IEntity, ByVal subType As Type) As _IEntity
        Dim c As String = GetJoinFieldNameByType(obj.GetType, subType, GetEntitySchema(obj.GetType))
        Dim r As _IEntity = Nothing
        If Not String.IsNullOrEmpty(c) Then
            Dim id As Object = GetPropertyValue(obj, c, oschema)
            'If obj.IsPropertyLoaded(c) Then
            '    id = obj.GetValueOptimized(Nothing, c, oschema)
            'Else
            '    id = GetPropertyValue(obj, c, oschema)
            'End If
            r = TryCast(id, _IEntity)
            If r Is Nothing AndAlso id IsNot Nothing Then
                Try
                    'Dim id As Integer = Convert.ToInt32(o)
                    r = OrmManager.CurrentManager.GetKeyEntityFromCacheOrCreate(id, subType)
                Catch ex As InvalidCastException
                End Try
            End If
        End If
        Return r
    End Function

    Public Function GetPropertyTypeByName(ByVal type As Type, ByVal propertyAlias As String) As Type
        Return GetPropertyTypeByName(type, GetEntitySchema(type), propertyAlias)
    End Function

    Public Function GetPropertyTypeByName(ByVal type As Type, ByVal oschema As IEntitySchema, ByVal propertyAlias As String) As Type
        'Dim t As Type = map(
        For Each de As DictionaryEntry In GetProperties(type, oschema)
            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
            If propertyAlias = c.PropertyAlias Then
                Return CType(de.Value, Reflection.PropertyInfo).PropertyType
            End If
        Next
        Throw New ObjectMappingException("Type " & type.Name & " doesnot contain property " & propertyAlias)
    End Function

    Public Function GetPropertyAliasByType(ByVal type As Type, ByVal propertyType As Type, ByVal oschema As IEntitySchema) As ICollection(Of String)
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        If propertyType Is Nothing Then
            Throw New ArgumentNullException("propertyType")
        End If

        Dim key As String = type.ToString & propertyType.ToString
        Dim l As List(Of String) = CType(_joins(key), List(Of String))
        If l Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                l = CType(_joins(key), List(Of String))
                If l Is Nothing Then
                    l = New List(Of String)
                    For Each de As DictionaryEntry In GetProperties(type, oschema)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If pi.PropertyType Is propertyType Then
                            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
                            l.Add(c.PropertyAlias)
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

    Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal propertyAlias As String) As String
        If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

        Dim schema As IEntitySchema = GetEntitySchema(type)

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        Dim p As MapField2Column = Nothing
        If coll.TryGetValue(propertyAlias, p) Then
            Dim c As String = Nothing
            c = p.Column
            Return c
        End If

        Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    End Function

    Protected Function GetColumnsFromPropertyAlias(ByVal main As Type, ByVal propertyType As Type) As EntityPropertyAttribute()
        If main Is Nothing Then Throw New ArgumentNullException("main")

        Dim l As New List(Of EntityPropertyAttribute)

        For Each de As DictionaryEntry In GetProperties(main)
            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
            Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
            If pi.PropertyType Is propertyType Then
                l.Add(c)
            End If
        Next
        Return l.ToArray
    End Function

    Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String) As EntityPropertyAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        'Dim l As New List(Of EntityPropertyAttribute)
        Return GetColumnByPropertyAlias(main, propertyAlias, GetEntitySchema(main))
    End Function

    Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As EntityPropertyAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        For Each de As DictionaryEntry In GetProperties(main, oschema)
            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
            If c.PropertyAlias = propertyAlias Then
                Return c
            End If
        Next
        Return Nothing
    End Function

    Public Shared Function GetColumnByMappedPropertyAlias(ByVal main As Type, ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As EntityPropertyAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        For Each de As DictionaryEntry In GetMappedProperties(main, oschema)
            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
            If c.PropertyAlias = propertyAlias Then
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

    Public Shared Function GetTable(ByVal mpe As ObjectMappingEngine, ByVal t As Type) As SourceFragment
        Dim tbl As SourceFragment = Nothing
        Dim entities() As EntityAttribute = CType(t.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())
        For Each ea As EntityAttribute In entities
            If ea.Version = mpe.Version AndAlso Not String.IsNullOrEmpty(ea.TableName) Then
                tbl = New SourceFragment(ea.TableSchema, ea.TableName)
                Exit For
            End If
        Next
        Dim entities2() As EntityAttribute = Nothing
        If tbl Is Nothing Then
            entities2 = CType(t.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())
            For Each ea As EntityAttribute In entities2
                If ea.Version = mpe.Version AndAlso Not String.IsNullOrEmpty(ea.TableName) Then
                    tbl = New SourceFragment(ea.TableSchema, ea.TableName)
                    Exit For
                End If
            Next
        End If

        If tbl Is Nothing Then
            If entities.Length = 1 AndAlso Not String.IsNullOrEmpty(entities(0).TableName) Then
                tbl = New SourceFragment(entities(0).TableSchema, entities(0).TableName)
            ElseIf entities2 IsNot Nothing AndAlso entities2.Length = 1 AndAlso Not String.IsNullOrEmpty(entities2(0).TableName) Then
                tbl = New SourceFragment(entities2(0).TableSchema, entities2(0).TableName)
            End If
        End If
        Return tbl
    End Function

    Protected Function CreateObjectSchema(ByRef names As IDictionary) As IDictionary
        Dim idic As New Specialized.HybridDictionary
        names = New Specialized.HybridDictionary
        For Each assembly As Reflection.Assembly In AppDomain.CurrentDomain.GetAssemblies
            If IsBadAssembly(assembly) Then
                Continue For
            End If

            Dim types() As Type = Nothing
            'Try
            types = assembly.GetTypes
            'Catch ex As Reflection.ReflectionTypeLoadException
            '    Debug.WriteLine("Worm error during loading types: " & ex.ToString)
            'End Try

            If types Is Nothing Then Continue For

            Dim t As Type = GetType(_IEntity)

            For Each tp As Type In types
                If t.IsAssignableFrom(tp) Then GetEntitySchema(tp, Me, idic, names)
            Next
        Next
        Return idic
    End Function

    Public Function AddEntitySchema(ByVal tp As Type, ByVal schema As IEntitySchema) As Boolean
        Dim idic As IDictionary = GetIdic()

        SyncLock idic.SyncRoot
            If Not idic.Contains(tp) Then
                idic.Add(tp, schema)
                Return True
            End If
        End SyncLock

        Return False
    End Function

    Public Function HasEntitySchema(ByVal tp As Type) As Boolean
        Return GetIdic.Contains(tp)
    End Function

    Public Shared Function GetEntitySchema(ByVal tp As Type, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary, ByRef names As IDictionary) As IEntitySchema
        Dim schema As IEntitySchema = Nothing

        If tp.IsClass Then
            Dim entities() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())

            For Each ea As EntityAttribute In entities
                If mpe Is Nothing OrElse ea.Version = mpe._version Then

                    If ea.Type Is Nothing Then
                        If tp.BaseType IsNot GetType(KeyEntity) AndAlso tp.BaseType IsNot GetType(KeyEntityBase) AndAlso tp.BaseType IsNot GetType(CachedEntity) AndAlso tp.BaseType IsNot GetType(CachedLazyLoad) AndAlso tp.BaseType.IsAbstract Then
                            Dim l As New OrmObjectIndex
                            Dim tbl As New SourceFragment(ea.TableSchema, ea.TableName)
                            For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                l.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl, c.Behavior, c.DBType, c.DBSize))
                            Next

                            Dim bsch As IEntitySchema = CType(idic(tp.BaseType), IEntitySchema)
                            If bsch Is Nothing Then
                                GetEntitySchema(tp.BaseType, mpe, idic, names)
                            End If
                            Dim l2 As Collections.IndexedCollection(Of String, MapField2Column) = bsch.GetFieldColumnMap

                            schema = New SimpleTwotableObjectSchema(l, l2)
                        Else
                            Dim l As New List(Of EntityPropertyAttribute)
                            For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                l.Add(c)
                            Next

                            schema = New SimpleObjectSchema(tp, ea.TableName, ea.TableSchema, l, ea.PrimaryKey)
                        End If

                    Else
                        Try
                            schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IEntitySchema)
                        Catch ex As Exception
                            Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
                        End Try
                    End If

                    Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                    If n IsNot Nothing Then
                        n.GetSchema(mpe, tp)
                    End If

                    If Not String.IsNullOrEmpty(ea.EntityName) AndAlso names IsNot Nothing Then
                        If names.Contains(ea.EntityName) Then
                            Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea.EntityName), Pair(Of Type, EntityAttribute))
                            If tt.First.IsAssignableFrom(tp) OrElse tt.Second.Version <> mpe._version Then
                                names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                            End If
                        Else
                            names.Add(ea.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea))
                        End If
                    End If

                    Try
                        If idic IsNot Nothing Then idic.Add(tp, schema)
                    Catch ex As ArgumentException
                        Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                    End Try
                End If
            Next

            If schema Is Nothing Then
                Dim entities2() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())

                For Each ea As EntityAttribute In entities2
                    If mpe Is Nothing OrElse ea.Version = mpe._version Then
                        'Dim schema As IEntitySchema = Nothing
                        If ea.Type Is Nothing Then
                            If tp.BaseType IsNot GetType(KeyEntity) AndAlso tp.BaseType IsNot GetType(KeyEntityBase) AndAlso tp.BaseType IsNot GetType(CachedEntity) AndAlso tp.BaseType IsNot GetType(CachedLazyLoad) AndAlso tp.BaseType.IsAbstract Then
                                Dim l As New OrmObjectIndex
                                Dim tbl As New SourceFragment(ea.TableSchema, ea.TableName)
                                For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                    l.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl, c.Behavior, c.DBType, c.DBSize))
                                Next

                                Dim bsch As IEntitySchema = CType(idic(tp.BaseType), IEntitySchema)
                                If bsch Is Nothing Then
                                    GetEntitySchema(tp.BaseType, mpe, idic, names)
                                End If
                                Dim l2 As Collections.IndexedCollection(Of String, MapField2Column) = bsch.GetFieldColumnMap

                                schema = New SimpleTwotableObjectSchema(l, l2)
                            Else
                                Dim l As New List(Of EntityPropertyAttribute)
                                For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                    l.Add(c)
                                Next

                                schema = New SimpleObjectSchema(tp, ea.TableName, ea.TableSchema, l, ea.PrimaryKey)
                            End If

                        Else
                            Try
                                schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IEntitySchema)
                            Catch ex As Exception
                                Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
                            End Try
                        End If

                        Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                        If n IsNot Nothing Then
                            n.GetSchema(mpe, tp)
                        End If

                        If names IsNot Nothing AndAlso Not String.IsNullOrEmpty(ea.EntityName) AndAlso entities.Length = 0 Then
                            If names.Contains(ea.EntityName) Then
                                Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea.EntityName), Pair(Of Type, EntityAttribute))
                                If tt.First.IsAssignableFrom(tp) OrElse tt.Second.Version <> mpe._version Then
                                    names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                                End If
                            Else
                                names.Add(ea.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea))
                            End If
                        End If

                        Try
                            If idic IsNot Nothing Then idic.Add(tp, schema)
                            Exit For
                        Catch ex As ArgumentException
                            Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                        End Try
                    End If
                Next

                If schema Is Nothing Then
                    'For Each ea As EntityAttribute In entities
                    Dim ea1 As EntityAttribute = Nothing
                    If entities.Length > 0 Then
                        If mpe IsNot Nothing AndAlso mpe._mapv IsNot Nothing Then
                            ea1 = mpe._mapv(mpe._version, entities, tp)
                        ElseIf entities.Length = 1 Then
                            ea1 = entities(0)
                        End If
                    End If
                    If ea1 IsNot Nothing Then
                        'Dim schema As IEntitySchema = Nothing
                        If ea1.Type Is Nothing Then
                            If tp.BaseType IsNot GetType(KeyEntity) AndAlso tp.BaseType IsNot GetType(KeyEntityBase) AndAlso tp.BaseType IsNot GetType(CachedEntity) AndAlso tp.BaseType IsNot GetType(CachedLazyLoad) AndAlso tp.BaseType.IsAbstract Then
                                Dim l As New OrmObjectIndex
                                Dim tbl As New SourceFragment(ea1.TableSchema, ea1.TableName)
                                For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                    l.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl, c.Behavior, c.DBType, c.DBSize))
                                Next

                                Dim bsch As IEntitySchema = CType(idic(tp.BaseType), IEntitySchema)
                                If bsch Is Nothing Then
                                    GetEntitySchema(tp.BaseType, mpe, idic, names)
                                End If
                                Dim l2 As Collections.IndexedCollection(Of String, MapField2Column) = bsch.GetFieldColumnMap

                                schema = New SimpleTwotableObjectSchema(l, l2)
                            Else
                                Dim l As New List(Of EntityPropertyAttribute)
                                For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                    l.Add(c)
                                Next

                                schema = New SimpleObjectSchema(tp, ea1.TableName, ea1.TableSchema, l, ea1.PrimaryKey)
                            End If

                        Else
                            Try
                                schema = CType(ea1.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IEntitySchema)
                            Catch ex As Exception
                                Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea1.Type.ToString), ex)
                            End Try
                        End If

                        Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                        If n IsNot Nothing Then
                            n.GetSchema(mpe, tp)
                        End If

                        If names IsNot Nothing AndAlso Not String.IsNullOrEmpty(ea1.EntityName) Then
                            If names.Contains(ea1.EntityName) Then
                                Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea1.EntityName), Pair(Of Type, EntityAttribute))
                                If tt.First.IsAssignableFrom(tp) OrElse (tt.Second.Version <> mpe._version AndAlso mpe._mapn IsNot Nothing) Then
                                    Dim e As EntityAttribute = Nothing
                                    If mpe._mapn IsNot Nothing Then
                                        e = mpe._mapn(mpe._version, New EntityAttribute() {ea1, tt.Second}, tp)
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
                            If idic IsNot Nothing Then idic.Add(tp, schema)
                        Catch ex As ArgumentException
                            Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea1.Type), ex)
                        End Try
                    End If
                    'Next

                    If schema Is Nothing Then
                        Dim ea2 As EntityAttribute = Nothing
                        If entities2.Length > 0 Then
                            If mpe IsNot Nothing AndAlso mpe._mapv IsNot Nothing Then
                                ea2 = mpe._mapv(mpe._version, entities2, tp)
                            ElseIf entities2.Length = 1 Then
                                ea2 = entities2(0)
                            End If
                        End If
                        If ea2 IsNot Nothing Then
                            'Dim schema As IEntitySchema = Nothing

                            If ea2.Type Is Nothing Then
                                If tp.BaseType IsNot GetType(KeyEntity) AndAlso tp.BaseType IsNot GetType(KeyEntityBase) AndAlso tp.BaseType IsNot GetType(CachedEntity) AndAlso tp.BaseType IsNot GetType(CachedLazyLoad) AndAlso tp.BaseType.IsAbstract Then
                                    Dim l As New OrmObjectIndex
                                    Dim tbl As New SourceFragment(ea2.TableSchema, ea2.TableName)
                                    For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                        l.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl, c.Behavior, c.DBType, c.DBSize))
                                    Next

                                    Dim bsch As IEntitySchema = CType(idic(tp.BaseType), IEntitySchema)
                                    If bsch Is Nothing Then
                                        GetEntitySchema(tp.BaseType, mpe, idic, names)
                                    End If
                                    Dim l2 As Collections.IndexedCollection(Of String, MapField2Column) = bsch.GetFieldColumnMap

                                    schema = New SimpleTwotableObjectSchema(l, l2)
                                Else
                                    Dim l As New List(Of EntityPropertyAttribute)
                                    For Each c As EntityPropertyAttribute In GetMappedProperties(tp, Nothing).Keys
                                        l.Add(c)
                                    Next

                                    schema = New SimpleObjectSchema(tp, ea2.TableName, ea2.TableSchema, l, ea2.PrimaryKey)
                                End If

                            Else
                                Try
                                    schema = CType(ea2.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IEntitySchema)
                                Catch ex As Exception
                                    Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea2.Type.ToString), ex)
                                End Try
                            End If

                            Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                            If n IsNot Nothing Then
                                n.GetSchema(mpe, tp)
                            End If

                            If names IsNot Nothing AndAlso Not String.IsNullOrEmpty(ea2.EntityName) AndAlso entities.Length = 0 Then
                                If names.Contains(ea2.EntityName) Then
                                    Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea2.EntityName), Pair(Of Type, EntityAttribute))
                                    If tt.First.IsAssignableFrom(tp) OrElse (tt.Second.Version <> mpe._version AndAlso mpe._mapn IsNot Nothing) Then
                                        Dim e As EntityAttribute = Nothing
                                        If mpe._mapn IsNot Nothing Then
                                            e = mpe._mapn(mpe._version, New EntityAttribute() {ea2, tt.Second}, tp)
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
                                If idic IsNot Nothing Then idic.Add(tp, schema)
                                'Exit For
                            Catch ex As ArgumentException
                                Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea2.Type), ex)
                            End Try
                        End If
                        'Next
                    End If
                End If
            End If
        End If
        Return schema
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

    Public Function GetEntitySchema(ByVal t As Type) As IEntitySchema
        Return GetEntitySchema(t, True)
    End Function

    Public Function GetEntitySchema(ByVal entityName As String) As IEntitySchema
        Return GetEntitySchema(GetTypeByEntityName(entityName), True)
    End Function

    Private Function GetIdic() As IDictionary
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
        Return idic
    End Function

    Public Function GetEntitySchema(ByVal t As Type, ByVal check As Boolean) As IEntitySchema
        If t Is Nothing Then
            If check Then
                Throw New ArgumentNullException("t")
            Else
                Return Nothing
            End If
        End If

        Dim idic As IDictionary = GetIdic()
        Dim schema As IEntitySchema = CType(idic(t), IEntitySchema)

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

    Protected Function GetJoins(ByVal type As Type, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IMultiTableObjectSchema = CType(GetEntitySchema(type), IMultiTableObjectSchema)

        Return GetJoins(schema, left, right, filterInfo)
    End Function

    Protected Friend Function GetJoins(ByVal schema As IMultiTableObjectSchema, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
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
            key = schema & Delimiter & tableName
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

        Return GetTables(GetEntitySchema(type))
    End Function

    Public Function GetTables(ByVal schema As IEntitySchema) As SourceFragment()
        If schema Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        If TryCast(schema, IMultiTableObjectSchema) IsNot Nothing Then
            Return TryCast(schema, IMultiTableObjectSchema).GetTables
        Else
            Return New SourceFragment() {schema.Table}
        End If
    End Function

    Public Function ApplyFilter(Of T As KeyEntity)(ByVal col As ICollection(Of T), ByVal filter As Criteria.Core.IFilter, ByRef r As Boolean) As ICollection(Of T)
        r = True
        Dim f As Criteria.Core.IEntityFilter = TryCast(filter, Criteria.Core.IEntityFilter)
        If f Is Nothing Then
            Return col
        Else
            Dim l As New List(Of T)
            Dim oschema As IEntitySchema = Nothing
            For Each o As T In col
                If oschema Is Nothing Then
                    oschema = Me.GetEntitySchema(o.GetType)
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

    'Public Shared Function ExtractValues(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal aliases As IPrepareTable, _
    '    ByVal _values() As FieldReference) As List(Of String)
    '    Dim values As New List(Of String)
    '    'Dim lastt As Type = Nothing
    '    Dim d As String = schema.Delimiter
    '    If stmt IsNot Nothing Then
    '        d = stmt.Selector
    '    End If
    '    For Each p As FieldReference In _values
    '        If p.Property.ObjectSource IsNot Nothing Then
    '            If p.Property.ObjectSource.IsQuery Then
    '                Dim tbl As SourceFragment = p.Property.ObjectSource.ObjectAlias.Tbl
    '                If tbl Is Nothing Then
    '                    tbl = New SourceFragment
    '                    p.Property.ObjectSource.ObjectAlias.Tbl = tbl
    '                End If
    '                Dim t As Type = p.Property.ObjectSource.GetRealType(schema)
    '                Dim oschema As IEntitySchema = schema.GetEntitySchema(t)
    '                Dim clm As String = p.Property.Field
    '                Dim map As MapField2Column = Nothing
    '                If oschema.GetFieldColumnMap.TryGetValue(clm, map) Then
    '                    clm = map._columnName
    '                End If
    '                Dim [alias] As String = Nothing
    '                If aliases IsNot Nothing Then
    '                    Debug.Assert(aliases.ContainsKey(tbl, p.Property.ObjectSource), "There is not alias for table " & tbl.RawName)
    '                    Try
    '                        [alias] = aliases.GetAlias(tbl, p.Property.ObjectSource)
    '                    Catch ex As KeyNotFoundException
    '                        Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
    '                    End Try
    '                End If
    '                If Not String.IsNullOrEmpty([alias]) Then
    '                    values.Add([alias] & d & clm)
    '                Else
    '                    values.Add(tbl.UniqueName(p.Property.ObjectSource) & d & clm)
    '                End If
    '            Else
    '                Dim t As Type = p.Property.ObjectSource.GetRealType(schema) 'CType(o, Type)
    '                'If Not GetType(IEntity).IsAssignableFrom(t) Then
    '                '    Throw New NotSupportedException(String.Format("Type {0} is not assignable from IEntity", t))
    '                'End If
    '                'lastt = t
    '                FormatType(t, stmt, p.Property.Field, schema, aliases, values, p.Property.ObjectSource)
    '            End If
    '        ElseIf p.Column IsNot Nothing Then
    '            Dim tbl As SourceFragment = p.Column.First 'CType(o, SourceFragment)
    '            Dim clm As String = p.Column.Second
    '            Dim [alias] As String = Nothing
    '            If aliases IsNot Nothing Then
    '                Debug.Assert(aliases.ContainsKey(tbl, Nothing), "There is not alias for table " & tbl.RawName)
    '                Try
    '                    [alias] = aliases.GetAlias(tbl, Nothing)
    '                Catch ex As KeyNotFoundException
    '                    Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
    '                End Try
    '            End If
    '            If Not String.IsNullOrEmpty([alias]) Then
    '                values.Add([alias] & d & clm)
    '            Else
    '                values.Add(clm)
    '            End If
    '            'ElseIf TypeOf o Is ObjectSource Then
    '            '    Dim src As ObjectSource = CType(o, ObjectSource)
    '            '    Dim t As Type = src.GetRealType(schema)

    '            '    FormatType(t, stmt, p, schema, aliases, values, src)
    '            'ElseIf o Is Nothing Then
    '            '    values.Add(p.Second)
    '        Else
    '            Throw New NotSupportedException '(String.Format("Type {0} is not supported", o.GetType))
    '        End If
    '    Next
    '    Return values
    'End Function

    Private Shared Sub FormatType(ByVal t As Type, ByVal stmt As StmtGenerator, ByVal fld As String, _
                                  ByVal schema As ObjectMappingEngine, ByVal aliases As IPrepareTable, _
                                  ByVal values As List(Of String), ByVal os As EntityUnion)
        If Not GetType(IEntity).IsAssignableFrom(t) Then
            Throw New NotSupportedException(String.Format("Type {0} is not assignable from IEntity", t))
        End If

        Dim d As String = schema.Delimiter
        If stmt IsNot Nothing Then
            d = stmt.Selector
        End If

        Dim oschema As IEntitySchema = schema.GetEntitySchema(t)
        Dim tbl As SourceFragment = Nothing
        Dim map As MapField2Column = Nothing
        'Dim fld As String = p.Second
        If oschema.GetFieldColumnMap.TryGetValue(fld, map) Then
            fld = map.Column
            tbl = map.Table
        Else
            tbl = oschema.Table
        End If

        If aliases IsNot Nothing Then
            Debug.Assert(aliases.ContainsKey(tbl, os), "There is not alias for table " & tbl.RawName)
            Try
                values.Add(aliases.GetAlias(tbl, os) & d & fld)
            Catch ex As KeyNotFoundException
                Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
            End Try
        Else
            values.Add(tbl.UniqueName(os) & d & fld)
        End If
    End Sub

    Public ReadOnly Property Delimiter() As String
        Get
            Return "_"
        End Get
    End Property

#Region " Gen "
    Public Function GetColumnNameByPropertyAlias(ByVal schema As IEntitySchema, ByVal propertyAlias As String, _
        ByVal add_alias As Boolean, ByVal os As EntityUnion) As String

        If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        Dim p As MapField2Column = Nothing
        If coll.TryGetValue(propertyAlias, p) Then
            Dim c As String = Nothing
            If add_alias AndAlso ShouldPrefix(p.Column) Then
                c = p.Table.UniqueName(os) & Delimiter & p.Column
            Else
                c = p.Column
            End If
            'If columnAliases IsNot Nothing Then
            '    columnAliases.Add(p._columnName)
            'End If
            Return c
        End If

        Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, _
        ByVal add_alias As Boolean, ByVal os As EntityUnion) As String

        If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

        Dim schema As IEntitySchema = mpe.GetEntitySchema(type)

        Return GetColumnNameByPropertyAlias(schema, propertyAlias, add_alias, os)
    End Function

    Public Function GetPrimaryKeysName(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, ByVal add_alias As Boolean, _
        ByVal schema As IEntitySchema, ByVal os As EntityUnion) As String()
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
                        schema = mpe.GetEntitySchema(original_type)
                    End If

                    For Each c As EntityPropertyAttribute In mpe.GetSortedFieldList(original_type, schema)
                        If (mpe.GetAttributes(schema, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            arr.Add(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, add_alias, os))
                        End If
                    Next

                    map.Add(cl_type, arr)
                End If
            End Using
        End If

        Return arr.ToArray
    End Function

    Protected Friend Sub GetPKList(ByVal type As Type, ByVal mpe As ObjectMappingEngine, _
        ByVal schema As IEntitySchema, ByVal ids As StringBuilder, ByVal os As EntityUnion)
        If ids Is Nothing Then
            Throw New ArgumentNullException("ids")
        End If

        For Each pk As String In GetPrimaryKeysName(type, mpe, True, schema, os)
            ids.Append(pk).Append(",")
        Next
        ids.Length -= 1

        'Dim p As MapField2Column = schema.GetFieldColumnMap("ID")
        'ids.Append(GetTableName(p._tableName)).Append(Selector).Append(p._columnName)
        'If columnAliases IsNot Nothing Then
        '    columnAliases.Add(p._columnName)
        'End If
    End Sub

    'Protected Friend Function GetSelectColumns(ByVal mpe As ObjectMappingEngine, ByVal props As IEnumerable(Of SelectExpression)) As String
    '    Dim sb As New StringBuilder
    '    For Each pr As SelectExpression In props
    '        If pr.PropType = PropType.TableColumn Then
    '            sb.Append(pr.Table.UniqueName(Nothing)).Append(Delimiter).Append(pr.Column).Append(", ")
    '        ElseIf pr.PropType = PropType.CustomValue Then
    '            'sb.Append(String.Format(pr.Computed, ObjectMappingEngine.ExtractValues(mpe, Nothing, Nothing, pr.Values).ToArray))
    '            sb.Append(pr.Custom.GetParam(mpe, stmt, param, almgr, Nothing, filterinfo, True))
    '            If Not String.IsNullOrEmpty(pr.Column) Then
    '                sb.Append(" ").Append(pr.Column)
    '            End If
    '            sb.Append(", ")
    '        Else
    '            sb.Append(GetColumnNameByPropertyAlias(mpe.GetEntitySchema(pr.ObjectSource.GetRealType(mpe)), pr.PropertyAlias, True, pr.ObjectSource)).Append(", ")
    '        End If
    '    Next

    '    sb.Length -= 2

    '    Return sb.ToString
    'End Function

    Protected Friend Function GetSelectColumnList(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, _
        ByVal arr As Generic.ICollection(Of EntityPropertyAttribute), ByVal schema As IEntitySchema, ByVal os As EntityUnion) As String
        'Dim add_c As Boolean = False
        'If arr Is Nothing Then
        '    Dim s As String = CStr(sel(original_type))
        '    If Not String.IsNullOrEmpty(s) Then
        '        Return s
        '    End If
        '    add_c = True
        'End If
        Dim sb As New StringBuilder
        If arr Is Nothing Then arr = mpe.GetSortedFieldList(original_type, schema)
        For Each c As EntityPropertyAttribute In arr
            sb.Append(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, True, os)).Append(", ")
        Next

        If sb.Length = 0 Then
            For Each m As MapField2Column In schema.GetFieldColumnMap
                sb.Append(m.Column).Append(", ")
            Next
        End If

        sb.Length -= 2

        'If add_c Then
        '    sel(original_type) = sb.ToString
        'End If
        Return sb.ToString
    End Function

    Protected Friend Function GetSelectColumnListWithoutPK(ByVal original_type As Type, _
        ByVal mpe As ObjectMappingEngine, ByVal arr As Generic.ICollection(Of EntityPropertyAttribute), _
        ByVal schema As IEntitySchema, ByVal os As EntityUnion) As String
        'Dim add_c As Boolean = False
        'If arr Is Nothing Then
        '    Dim s As String = CStr(sel(original_type))
        '    If Not String.IsNullOrEmpty(s) Then
        '        Return s
        '    End If
        '    add_c = True
        'End If
        Dim sb As New StringBuilder
        If arr Is Nothing Then arr = mpe.GetSortedFieldList(original_type, schema)
        For Each c As EntityPropertyAttribute In arr
            If (c.Behavior And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                sb.Append(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, True, os)).Append(", ")
            End If
        Next

        If sb.Length > 0 Then
            sb.Length -= 2
        End If
        'If add_c Then
        '    sel(original_type) = sb.ToString
        'End If
        Return sb.ToString
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal os As EntityUnion) As String
        Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, True, os)
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal os As IEntitySchema, ByVal propertyAlias As String, ByVal osrc As EntityUnion) As String
        Return GetColumnNameByPropertyAlias(os, propertyAlias, True, osrc)
    End Function

    Protected Function GetColumns4Select(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal os As EntityUnion) As String
        Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, os)
    End Function
#End Region

    Public Enum JoinFieldType
        Direct
        Reverse
        M2M
    End Enum

    Public Sub AppendJoin(ByVal selectOS As EntityUnion, ByVal selectType As Type, ByVal selSchema As IEntitySchema, _
        ByVal joinOS As EntityUnion, ByVal type2join As Type, ByVal sh As IEntitySchema, _
        ByRef filter As IFilter, ByVal l As List(Of QueryJoin), _
        ByVal filterInfo As Object)

        Dim jft As JoinFieldType

        Dim field As String = GetJoinFieldNameByType(selectType, type2join, selSchema)

        If String.IsNullOrEmpty(field) Then

            field = GetJoinFieldNameByType(type2join, selectType, sh)

            If String.IsNullOrEmpty(field) Then
                Dim m2m As M2MRelationDesc = GetM2MRelation(type2join, selectType, True)
                If m2m IsNot Nothing Then
                    jft = JoinFieldType.M2M
                Else
                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                End If
            Else
                jft = JoinFieldType.Reverse
            End If
        Else
            jft = JoinFieldType.Direct
        End If

        AppendJoin(selectOS, selectType, selSchema, joinOS, type2join, sh, filter, l, filterInfo, jft, field)
    End Sub

    Public Sub AppendJoin(ByVal selectOS As EntityUnion, ByVal selectType As Type, ByVal selSchema As IEntitySchema, _
        ByVal joinOS As EntityUnion, ByVal type2join As Type, ByVal sh As IEntitySchema, _
        ByRef filter As IFilter, ByVal l As List(Of QueryJoin), _
        ByVal filterInfo As Object, ByVal jft As JoinFieldType, ByVal propertyAlias As String)

        Select Case jft
            Case JoinFieldType.Direct
                l.Add(MakeJoin(joinOS, GetPrimaryKeys(type2join, sh)(0).PropertyAlias, selectOS, propertyAlias, FilterOperation.Equal, JoinType.Join, False))
            Case JoinFieldType.Reverse
                l.Add(MakeJoin(selectOS, GetPrimaryKeys(selectType, selSchema)(0).PropertyAlias, joinOS, propertyAlias, FilterOperation.Equal, JoinType.Join, True))
            Case JoinFieldType.M2M
                l.AddRange(JCtor.join(joinOS).onM2M(selectOS).ToList)
            Case Else
                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
        End Select

        Dim ts As IMultiTableObjectSchema = TryCast(sh, IMultiTableObjectSchema)
        If ts IsNot Nothing Then
            Dim pk_table As SourceFragment = sh.Table
            For i As Integer = 1 To ts.GetTables.Length - 1
                Dim joinableTs As IGetJoinsWithContext = TryCast(ts, IGetJoinsWithContext)
                Dim join As QueryJoin = Nothing
                If joinableTs IsNot Nothing Then
                    join = joinableTs.GetJoins(pk_table, ts.GetTables(i), filterInfo)
                Else
                    join = ts.GetJoins(pk_table, ts.GetTables(i))
                End If

                If Not QueryJoin.IsEmpty(join) Then
                    l.Add(join)
                End If
            Next

            Dim cfs As IContextObjectSchema = TryCast(sh, IContextObjectSchema)
            If cfs IsNot Nothing Then
                Dim newfl As IFilter = cfs.GetContextFilter(filterInfo)
                If newfl IsNot Nothing Then
                    Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                    con.AddFilter(filter)
                    con.AddFilter(newfl)
                    filter = con.Condition
                End If
            End If
        End If
    End Sub

    'Public Function MakeM2MJoin(ByVal m2m As M2MRelation, ByVal joinOS As ObjectSource) As Worm.Criteria.Joins.QueryJoin()
    '    Dim schema As ObjectMappingEngine = Me
    '    Dim jf As New JoinFilter(m2m.Table, m2m.Column, m2m.Type, schema.GetPrimaryKeys(m2m.Type)(0).PropertyAlias, Worm.Criteria.FilterOperation.Equal)
    '    Dim mj As New QueryJoin(m2m.Table, Joins.JoinType.Join, jf)
    '    m2m = schema.GetM2MRelation(m2m.Type, type2join, True)
    '    Dim jt As New JoinFilter(m2m.Table, m2m.Column, type2join, schema.GetPrimaryKeys(type2join)(0).PropertyAlias, Worm.Criteria.FilterOperation.Equal)
    '    Dim tj As New QueryJoin(schema.GetTables(type2join)(0), Joins.JoinType.Join, jt)
    '    Return New QueryJoin() {mj, tj}
    'End Function

    Public Function MakeJoin(ByVal joinOS As EntityUnion, ByVal pk As String, ByVal selectOS As EntityUnion, ByVal field As String, _
           ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Joins.JoinType, ByVal switchTable As Boolean) As Worm.Criteria.Joins.QueryJoin

        Dim schema As ObjectMappingEngine = Me

        Dim jf As New JoinFilter(joinOS, pk, selectOS, field, oper)

        Dim t As EntityUnion = joinOS
        If switchTable Then
            t = selectOS
        End If

        Return New QueryJoin(t, joinType, jf)
    End Function

    Public Shared Function SetValue(ByVal propType As Type, ByVal MappingEngine As ObjectMappingEngine, ByVal cache As Cache.CacheBase, _
                            ByVal value As Object, ByVal obj As Object, ByVal pi As Reflection.PropertyInfo, _
                            ByVal propertyAlias As String, ByVal objectLoaded As ObjectLoadedDelegate, ByVal contextInfo As Object) As Object
        Return SetValue(propType, MappingEngine, cache, value, obj, pi, propertyAlias, Nothing, Nothing, Nothing, objectLoaded, contextInfo)
    End Function

    Public Delegate Sub ObjectLoadedDelegate(ByVal obj As IEntity)

    Public Shared Function SetValue(ByVal propType As Type, ByVal MappingEngine As ObjectMappingEngine, ByVal cache As Cache.CacheBase, _
                        ByVal value As Object, ByVal obj As Object, ByVal pi As Reflection.PropertyInfo, _
                        ByVal propertyAlias As String, ByVal ce As _ICachedEntity, ByVal c As EntityPropertyAttribute, _
                        ByVal oschema As IEntitySchema, ByVal objectLoaded As ObjectLoadedDelegate, ByVal contextInfo As Object) As Object
        If value Is Nothing Then
            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, Nothing, oschema)
            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
        ElseIf GetType(System.Xml.XmlDocument) Is propType AndAlso TypeOf (value) Is String Then
            Dim o As New System.Xml.XmlDocument
            o.LoadXml(CStr(value))
            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, o, oschema)
            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
            Return o
        ElseIf propType.IsEnum AndAlso TypeOf (value) Is String Then
            Dim svalue As String = CStr(value).Trim
            If svalue = String.Empty Then
                value = 0
            Else
                value = [Enum].Parse(propType, svalue, True)
            End If
            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, value, oschema)
            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
            Return value
        ElseIf propType.IsGenericType AndAlso GetType(Nullable(Of )).Name = propType.Name Then
            Dim t As Type = propType.GetGenericArguments()(0)
            Dim v As Object = Nothing
            If t.IsPrimitive Then
                v = Convert.ChangeType(value, t)
            ElseIf t.IsEnum Then
                If TypeOf (value) Is String Then
                    Dim svalue As String = CStr(value).Trim
                    If svalue = String.Empty Then
                        v = [Enum].ToObject(t, 0)
                    Else
                        v = [Enum].Parse(t, svalue, True)
                    End If
                Else
                    v = [Enum].ToObject(t, value)
                End If
            ElseIf t Is value.GetType Then
                v = value
            Else
                Try
                    v = t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                        Nothing, Nothing, New Object() {value})
                Catch ex As MissingMethodException
                    'Debug.WriteLine(c.FieldName & ": " & original_type.Name)
                    'v = Convert.ChangeType(value, t)
                End Try
            End If
            Dim v2 As Object = propType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                Nothing, Nothing, New Object() {v})
            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, v2, oschema)
            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
            Return v2
        ElseIf (propType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (propType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
            Try
                Dim v As Object = Convert.ChangeType(value, propType)
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, v, oschema)
                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                Return v
            Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                Dim v As Object = Convert.ChangeType(value, propType)
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, v, oschema)
                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                Return v
            End Try
        ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
            Dim dt As DateTime = CDate(value)
            Dim l As Long = dt.ToBinary
            Using ms As New IO.MemoryStream
                Dim sw As New IO.StreamWriter(ms)
                sw.Write(l)
                sw.Flush()
                value = ms.ToArray
            End Using
            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, value, oschema)
            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
        Else
            If GetType(_IEntity).IsAssignableFrom(propType) Then
                Dim type_created As Type = propType
                Dim en As String = MappingEngine.GetEntityNameByType(type_created)
                If Not String.IsNullOrEmpty(en) Then
                    Dim cr As Type = MappingEngine.GetTypeByEntityName(en)
                    If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
                        type_created = cr
                    End If
                    If type_created Is Nothing Then
                        Throw New OrmManagerException("Cannot find type for entity " & en)
                    End If
                End If
                Dim o As _IEntity = Nothing
                If GetType(IKeyEntity).IsAssignableFrom(type_created) Then
                    o = Entity.CreateKeyEntity(value, type_created, cache, MappingEngine)
                    o.SetObjectState(ObjectState.NotLoaded)
                    o = cache.NormalizeObject(CType(o, _ICachedEntity), False, False, cache.GetOrmDictionary(contextInfo, type_created, MappingEngine), _
                                              True, Nothing, True, MappingEngine.GetEntitySchema(type_created))
                Else
                    Dim pks As IList(Of EntityPropertyAttribute) = MappingEngine.GetPrimaryKeys(type_created)
                    If pks.Count <> 1 Then
                        Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", type_created))
                    End If
                    If GetType(_ICachedEntity).IsAssignableFrom(type_created) Then
                        o = Entity.CreateEntity(New PKDesc() {New PKDesc(pks(0).PropertyAlias, value)}, type_created, cache, MappingEngine)
                        o.SetObjectState(ObjectState.NotLoaded)
                        o = cache.NormalizeObject(CType(o, _ICachedEntity), False, False, cache.GetOrmDictionary(contextInfo, type_created, MappingEngine), _
                                                  True, Nothing, True, MappingEngine.GetEntitySchema(type_created))
                    Else
                        o = Entity.CreateEntity(type_created, cache, MappingEngine)
                        MappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, value, Nothing)
                    End If
                End If

                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, o, oschema)
                If o IsNot Nothing Then
                    Dim eo As IEntity = TryCast(obj, IEntity)
                    If eo IsNot Nothing AndAlso eo.CreateManager IsNot Nothing Then o.SetCreateManager(eo.CreateManager)
                    If objectLoaded IsNot Nothing Then objectLoaded(o)
                End If
                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                Return o
            ElseIf ObjectMappingEngine.IsEntityType(propType, MappingEngine) Then
                Dim o As Object = Activator.CreateInstance(propType)
                Dim pks As IList(Of EntityPropertyAttribute) = MappingEngine.GetPrimaryKeys(propType)
                If pks.Count <> 1 Then
                    Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", propType))
                Else
                    MappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, value, Nothing)
                End If
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, o, oschema)
                Return o
            Else
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, value, oschema)
                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
            End If
        End If
        Return value
    End Function

    Public Function CreateObj(ByVal objType As Type, ByVal pkValue As Object, ByVal oschema As IEntitySchema) As Object
        If GetType(_IEntity).IsAssignableFrom(objType) Then
            Dim type_created As Type = objType
            Dim en As String = Me.GetEntityNameByType(type_created)
            If Not String.IsNullOrEmpty(en) Then
                Dim cr As Type = Me.GetTypeByEntityName(en)
                If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
                    type_created = cr
                End If
                If type_created Is Nothing Then
                    Throw New OrmManagerException("Cannot find type for entity " & en)
                End If
            End If
            Dim o As _IEntity = Nothing
            If GetType(IKeyEntity).IsAssignableFrom(type_created) Then
                o = Entity.CreateKeyEntity(pkValue, type_created, Nothing, Me)
                o.SetObjectState(ObjectState.NotLoaded)
            Else
                Dim pks As IList(Of EntityPropertyAttribute) = Me.GetPrimaryKeys(type_created, oschema)
                If pks.Count <> 1 Then
                    Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", type_created))
                End If
                If GetType(_ICachedEntity).IsAssignableFrom(type_created) Then
                    o = Entity.CreateEntity(New PKDesc() {New PKDesc(pks(0).PropertyAlias, pkValue)}, type_created, Nothing, Me)
                    o.SetObjectState(ObjectState.NotLoaded)
                Else
                    o = Entity.CreateEntity(type_created, Nothing, Me)
                    Me.SetPropertyValue(o, pks(0).PropertyAlias, pkValue, oschema)
                End If
            End If

            Return o
        ElseIf ObjectMappingEngine.IsEntityType(objType, Me) Then
            Dim o As Object = Activator.CreateInstance(objType)
            Dim pks As IList(Of EntityPropertyAttribute) = Me.GetPrimaryKeys(objType, oschema)
            If pks.Count <> 1 Then
                Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", objType))
            Else
                Me.SetPropertyValue(o, pks(0).PropertyAlias, pkValue, oschema)
            End If
            Return o
        Else
            Throw New NotSupportedException
        End If
    End Function

    Public Function GetPOCOEntitySchema(ByVal t As Type) As IEntitySchema
        Dim s As IEntitySchema = ObjectMappingEngine.GetEntitySchema(t, Me, Nothing, Nothing)
        If s IsNot Nothing AndAlso s.GetType IsNot GetType(SimpleObjectSchema) Then Return s
        Dim tbl As SourceFragment = ObjectMappingEngine.GetTable(Me, t)
        Dim selList As New OrmObjectIndex
        For Each de As DictionaryEntry In ObjectMappingEngine.GetMappedProperties(t)
            Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
            selList.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl))
        Next
        Return New SimpleObjectSchema(selList)
    End Function

    Public Shared Function IsEntityType(ByVal t As Type, ByVal mpe As ObjectMappingEngine) As Boolean
        If t.IsPrimitive OrElse t.IsValueType OrElse t.IsAbstract OrElse Not t.IsClass Then
            Return False
        End If

        If GetType(IEntity).IsAssignableFrom(t) Then
            Return True
        End If

        If IsBadAssembly(t.Assembly) Then
            Return False
        End If

        If t.IsClass Then
            Dim entities() As EntityAttribute = CType(t.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())

            If entities.Length > 0 Then
                Return True
            End If

            entities = CType(t.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())
            If entities.Length > 0 Then
                Return True
            End If

            For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)
                Dim column As EntityPropertyAttribute = Nothing
                Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), Attribute())
                If columns.Length > 0 Then
                    Return True
                End If
            Next

            For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                Dim column As EntityPropertyAttribute = Nothing
                Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), Attribute())
                If columns.Length > 0 Then
                    Return True
                End If
            Next
        End If

        Return False
    End Function

    Public Shared Function IsBadAssembly(ByVal assembly As Reflection.Assembly) As Boolean
        'assembly.GetName.GetPublicKeyToken()
        Dim moduleName As String = assembly.ManifestModule.Name
        If moduleName = "mscorlib.dll" OrElse moduleName = "System.Data.dll" OrElse moduleName = "System.Data.dll" _
                        OrElse moduleName = "System.Xml.dll" OrElse moduleName = "System.dll" _
                        OrElse moduleName = "System.Configuration.dll" OrElse moduleName = "System.Web.dll" _
                        OrElse moduleName = "System.Drawing.dll" OrElse moduleName = "System.Web.Services.dll" _
                        OrElse assembly.FullName.Contains("Microsoft") OrElse moduleName = "Worm.Orm.dll" _
                        OrElse moduleName = "CoreFramework.dll" OrElse moduleName = "ASPNETHosting.dll" _
                        OrElse moduleName = "System.Transactions.dll" OrElse moduleName = "System.EnterpriseServices.dll" Then
            Return True
        Else
            Dim tok As Byte() = assembly.GetName.GetPublicKeyToken()
            If IsEqualByteArray(New Byte() {&HB7, &H7A, &H5C, &H56, &H19, &H34, &HE0, &H89}, tok) Then
                Return True
            End If
            If IsEqualByteArray(New Byte() {&HB0, &H3F, &H5F, &H7F, &H11, &HD5, &HA, &H3A}, tok) Then
                Return True
            End If
        End If

        Return False
    End Function

    Public Function GetDerivedTypes(ByVal baseType As Type) As IList(Of Type)
        Dim l As New List(Of Type)
        For Each t As Type In GetIdic().Keys
            If baseType IsNot t AndAlso baseType.IsAssignableFrom(t) Then
                l.Add(t)
            End If
        Next
        Return l
    End Function
End Class

'End Namespace
