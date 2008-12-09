Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Entities
Imports Worm.Sorting
'Namespace Schema

''' <summary>
''' ������ ���������� ������������� ��� ����������� ������� � <see cref="ObjectMappingEngine" />
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public NotInheritable Class ObjectMappingException
    Inherits Exception

    ''' <summary>
    ''' ����������� �� ���������
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

    ''' <summary>
    ''' ����������� ��� �������� ������� ����� ���������
    ''' </summary>
    ''' <param name="message">������ ���������</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    ''' <summary>
    ''' ����������� ��� �������� ������� ����� ��������� � ��������� �����������
    ''' </summary>
    ''' <param name="message">������ ���������</param>
    ''' <param name="inner">����������</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

    Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
End Class

'''' <summary>
'''' ��������� ����������� ����� ��� ������ � ������������ (joins)
'''' </summary>
'''' <remarks></remarks>
'Public Interface ISchemaWithJoins
'    'Function GetSharedTable(ByVal tableName As String) As SourceFragment
'    'Function GetTables(ByVal type As Type) As SourceFragment()
'    ''' <summary>
'    ''' ����� ������������ ��� ��������� ������� ���� <see cref="QueryJoin" /> ��� ����������� ������� ������������ ����
'    ''' </summary>
'    ''' <param name="type">��� �������</param>
'    ''' <param name="left">����� �������. ����� ���� �� ������ <see cref="IMultiTableObjectSchema.GetTables"/></param>
'    ''' <param name="right">������ �������. ����� ���� �� ������ <see cref="IMultiTableObjectSchema.GetTables"/></param>
'    ''' <param name="filterInfo">������������ ������, ������������ �����������. ���������� �� <see cref="OrmManager.GetFilterInfo" /></param>
'    ''' <returns>������� ���� <see cref="QueryJoin" /></returns>
'    ''' <remarks>������������ ��� ��������� ��������</remarks>
'    Function GetJoins(ByVal type As Type, ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
'End Interface

''' <summary>
''' ����� �������� � ���������� ������� �������� <see cref="IObjectSchemaBase"/>
''' </summary>
''' <remarks>����� ��������� �������� ���� ��������, ������������� ������� ������� ��� �������
''' <see cref="IObjectSchemaBase"/> ����� ��� �������.</remarks>
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
            sup = schema.GetSuppressedFields()
        End If

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)
            Dim column As ColumnAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(ColumnAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), ColumnAttribute)
            If column Is Nothing Then
                Dim propertyAlias As String = pi.Name
                If propertyMap IsNot Nothing Then
                    If propertyMap.ContainsKey(propertyAlias) Then
                        Dim mc As MapField2Column = propertyMap(propertyAlias)
                        column = New ColumnAttribute(mc._newattributes)
                        column.Column = mc._columnName
                        column.PropertyAlias = mc._propertyAlias
                    End If
                ElseIf raw AndAlso pi.CanWrite AndAlso pi.CanRead Then
                    column = New ColumnAttribute(propertyAlias)
                    column.Column = propertyAlias
                End If
            End If

            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.PropertyAlias) Then
                    column.PropertyAlias = pi.Name
                End If

                If (sup Is Nothing OrElse Array.IndexOf(sup, column.PropertyAlias) < 0) AndAlso (propertyMap Is Nothing OrElse propertyMap.ContainsKey(column.PropertyAlias)) Then
                    result.Add(column, pi)
                End If
            End If
        Next

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
            Dim column As ColumnAttribute = Nothing
            Dim columns() As Attribute = CType(Attribute.GetCustomAttributes(pi, GetType(ColumnAttribute)), Attribute())
            If columns.Length > 0 Then column = CType(columns(0), ColumnAttribute)

            If column Is Nothing Then
                Dim propertyAlias As String = pi.Name
                If propertyMap IsNot Nothing Then
                    If propertyMap.ContainsKey(propertyAlias) AndAlso (pi.Name <> OrmBaseT.PKName OrElse pi.DeclaringType.Name <> GetType(OrmBaseT(Of )).Name) Then
                        Dim mc As MapField2Column = propertyMap(propertyAlias)
                        column = New ColumnAttribute(mc._newattributes)
                        column.Column = mc._columnName
                        column.PropertyAlias = mc._propertyAlias
                    End If
                ElseIf raw AndAlso pi.CanWrite AndAlso pi.CanRead Then
                    column = New ColumnAttribute(propertyAlias)
                    column.Column = propertyAlias
                End If
            End If

            If column IsNot Nothing Then
                If String.IsNullOrEmpty(column.PropertyAlias) Then
                    column.PropertyAlias = pi.Name
                End If
                If Not result.Contains(column) AndAlso (sup Is Nothing OrElse Array.IndexOf(sup, column.PropertyAlias) < 0) _
                    AndAlso (propertyMap Is Nothing OrElse propertyMap.ContainsKey(column.PropertyAlias)) Then
                    result.Add(column, pi)
                End If
            End If
        Next

        Return result
    End Function

    Public Function GetProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
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
                    h = GetMappedProperties(t, schema)

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

    Public Function GetMappedFields(ByVal schema As IEntitySchema) As ICollection(Of MapField2Column)
        Dim sup() As String = schema.GetSuppressedFields()
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
        Dim schema As IEntitySchema = GetObjectSchema(t)

        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityKey(filterInfo)
        Else
            Return t.ToString
        End If
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object, ByVal t As Type) As Object
        Return GetEntityTypeKey(filterInfo, t, GetObjectSchema(t))
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

        Dim schema As IEntitySchema = GetObjectSchema(type)

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        For Each p As MapField2Column In coll
            If p._columnName = columnName Then
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

    Public Function GetM2MRelations(ByVal s As IEntitySchema) As M2MRelation()
        Dim schema As IMultiTableWithM2MSchema = TryCast(s, IMultiTableWithM2MSchema)
        If schema IsNot Nothing Then
            Return schema.GetM2MRelations
        Else
            Return New M2MRelation() {}
        End If
    End Function

    Public Function GetM2MRelations(ByVal maintype As Type) As M2MRelation()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Return GetM2MRelations(GetObjectSchema(maintype))
    End Function

    Public Function GetM2MRelationsForEdit(ByVal maintype As Type) As M2MRelation()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Dim sch As IEntitySchema = GetObjectSchema(maintype)
        Dim editable As IReadonlyObjectSchema = TryCast(sch, IReadonlyObjectSchema)
        Dim schema As IMultiTableWithM2MSchema = Nothing
        If editable IsNot Nothing Then
            schema = editable.GetEditableSchema
        Else
            schema = TryCast(sch, IMultiTableWithM2MSchema)
        End If
        If schema IsNot Nothing Then
            Dim m As M2MRelation() = schema.GetM2MRelations
            If m Is Nothing AndAlso editable IsNot Nothing Then
                schema = TryCast(sch, IMultiTableWithM2MSchema)
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

        Dim en As String = GetEntityNameByType(subtype)
        If Not String.IsNullOrEmpty(en) Then
            Dim rt As Type = GetTypeByEntityName(en)
            For Each r As M2MRelation In mr
                If rt Is r.Type AndAlso String.Equals(r.Key, key) Then
                    Return r
                End If
            Next
        End If

        Return Nothing
    End Function

    Public Function GetM2MRelation(ByVal maintype As Type, ByVal mainSchema As IEntitySchema, ByVal subtype As Type, ByVal key As String) As M2MRelation
        If String.IsNullOrEmpty(key) Then key = M2MRelation.DirKey
        Dim mr() As M2MRelation = GetM2MRelations(mainSchema)
        For Each r As M2MRelation In mr
            If r.Type Is subtype AndAlso String.Equals(r.Key, key) Then
                Return r
            End If
        Next

        Dim en As String = GetEntityNameByType(maintype)
        If Not String.IsNullOrEmpty(en) Then
            For Each r As M2MRelation In mr
                Dim n As String = GetEntityNameByType(r.Type)
                If String.Equals(en, n) AndAlso String.Equals(r.Key, key) Then
                    Return r
                End If
            Next
        End If

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
        If Not String.IsNullOrEmpty(en) Then
            For Each r As M2MRelation In mr
                Dim n As String = GetEntityNameByType(r.Type)
                If String.Equals(en, n) AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
                    Return r
                End If
            Next
        End If

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

        Dim schema As IEntitySchema = GetObjectSchema(ct)

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

    Public Function ChangeValueType(ByVal schema As IEntitySchema, ByVal c As ColumnAttribute, ByVal o As Object) As Object
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        If o Is Nothing Then Return DBNull.Value

        Dim ot As System.Type = o.GetType

        If ot Is GetType(Guid) AndAlso CType(o, Guid) = Guid.Empty Then
            Return DBNull.Value
        End If

        ' ��� ��������� addDate'��, ������� ��������� �������������, 
        ' ��� ������ ������ ������ �� ������ ����� ������� ChangeValueType
        If ot Is GetType(DateTime) AndAlso CType(o, DateTime) = DateTime.MinValue Then
            Return DBNull.Value
        End If

        Dim v As Object = o

        If schema.ChangeValueType(c, o, v) Then
            Return v
        End If

        If GetType(KeyEntity).IsAssignableFrom(ot) Then
            Return CType(o, KeyEntity).Identifier
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
    Public Function GetFieldTable(ByVal schema As IPropertyMap, ByVal field As String) As SourceFragment
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
        Try
            Return coll(field)._tableName
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
        Dim r As M2MRelation = GetM2MRelation(t, subType, True)

        If r Is Nothing Then
            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", t.Name, subType.Name))
        End If

        Return r.Mapping
    End Function

    Public Function GetDBType(ByVal type As Type, ByVal os As IEntitySchema, _
                                         ByVal c As ColumnAttribute) As DBType
        Dim db As DBType = os.GetFieldColumnMap()(c.PropertyAlias).DBType
        If db.IsEmpty Then
            db = c.SourceType
        End If
        Return db
    End Function

    Public Function GetAttributes(ByVal type As Type, ByVal c As ColumnAttribute) As Field2DbRelations
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Dim schema As IEntitySchema = GetObjectSchema(type)

        Return GetAttributes(schema, c)
    End Function

    Public Function GetAttributes(ByVal schema As IEntitySchema, ByVal c As ColumnAttribute) As Field2DbRelations
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Return schema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c)
    End Function
#End Region

#Region " Helpers "
    Public Function HasField(ByVal t As Type, ByVal field As String) As Boolean
        If String.IsNullOrEmpty(field) Then Return False

        Dim schema As IEntitySchema = GetObjectSchema(t)

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

    Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal c As ColumnAttribute) As Reflection.PropertyInfo
        Return CType(GetProperties(t, schema)(c), Reflection.PropertyInfo)
    End Function

    Protected Friend Function GetProperty(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return GetProperty(original_type, New ColumnAttribute(propertyAlias))
    End Function

    Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return GetProperty(t, schema, New ColumnAttribute(propertyAlias))
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return CType(GetMappedProperties(original_type, Nothing)(New ColumnAttribute(propertyAlias)), Reflection.PropertyInfo)
    End Function

    Protected Friend Shared Function GetPropertyInt(ByVal t As Type, ByVal oschema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Return CType(GetMappedProperties(t, oschema)(New ColumnAttribute(propertyAlias)), Reflection.PropertyInfo)
    End Function

    Public Function GetSortedFieldList(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As Generic.List(Of ColumnAttribute)
        'Dim cl_type As String = New StringBuilder().Append("columnlist").Append(type.ToString).ToString
        If schema Is Nothing Then
            schema = GetObjectSchema(original_type)
        End If
        Dim cl_type As String = "columnlist" & original_type.ToString & schema.GetType.ToString

        Dim arr As Generic.List(Of ColumnAttribute) = CType(map(cl_type), Generic.List(Of ColumnAttribute))
        If arr Is Nothing Then
            SyncLock String.Intern(cl_type)
                arr = CType(map(cl_type), Generic.List(Of ColumnAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of ColumnAttribute)

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

    Public Function GetPrimaryKeys(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As List(Of ColumnAttribute)
        Dim cl_type As String = "clm_pklist" & original_type.ToString

        Dim arr As Generic.List(Of ColumnAttribute) = CType(map(cl_type), Generic.List(Of ColumnAttribute))

        If arr Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(cl_type)
                arr = CType(map(cl_type), Generic.List(Of ColumnAttribute))
                If arr Is Nothing Then
                    arr = New Generic.List(Of ColumnAttribute)

                    For Each c As ColumnAttribute In GetSortedFieldList(original_type, schema)
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

    Public Shared Function GetFieldValueSchemaless(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal pi As Reflection.PropertyInfo) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

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

        Return GetFieldValue(obj, propertyAlias, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim schema As IEntitySchema = GetObjectSchema(obj.GetType)

        Dim pi As Reflection.PropertyInfo = Nothing

        If schema IsNot Nothing Then
            pi = GetProperty(obj.GetType, schema, propertyAlias)
        Else
            pi = GetProperty(obj.GetType, propertyAlias)
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
        End If

        Return GetFieldValue(obj, propertyAlias, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim pi As Reflection.PropertyInfo = Nothing

        If schema IsNot Nothing Then
            pi = GetProperty(obj.GetType, schema, propertyAlias)
        Else
            pi = GetProperty(obj.GetType, propertyAlias)
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
        End If

        Return GetFieldValue(obj, propertyAlias, pi, schema)
    End Function

    Public Function GetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal pi As Reflection.PropertyInfo) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        If pi Is Nothing Then
            If schema IsNot Nothing Then
                pi = GetProperty(obj.GetType, schema, propertyAlias)
            Else
                pi = GetProperty(obj.GetType, propertyAlias)
            End If
        End If

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
        End If

        Return GetFieldValue(obj, propertyAlias, pi, schema)
    End Function

    Public Shared Function GetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal pi As Reflection.PropertyInfo, ByVal oschema As IEntitySchema) As Object
        If pi Is Nothing Then
            Throw New ArgumentNullException("pi")
        End If

        Using obj.SyncHelper(True, propertyAlias)
            'Return pi.GetValue(obj, Nothing)
            Return obj.GetValueOptimized(pi, propertyAlias, oschema)
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

    Public Sub SetFieldValue(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal value As Object, ByVal oschema As IEntitySchema)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim pi As Reflection.PropertyInfo = GetProperty(obj.GetType, propertyAlias)

        If pi Is Nothing Then
            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
        End If

        Using obj.SyncHelper(False, propertyAlias)
            obj.SetValueOptimized(pi, propertyAlias, oschema, value)
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

    Protected Friend Function GetJoinFieldNameByType(ByVal mainType As Type, ByVal subType As Type, ByVal oschema As IEntitySchema) As String
        Dim j As IJoinBehavior = TryCast(oschema, IJoinBehavior)
        Dim r As String = Nothing
        If j IsNot Nothing Then
            r = j.GetJoinField(subType)
        End If
        If String.IsNullOrEmpty(r) Then
            Dim c As ICollection(Of String) = GetPropertyAliasByType(mainType, subType)
            If c.Count = 1 Then
                For Each s As String In c
                    r = s
                Next
            End If
        End If
        Return r
    End Function

    Public Function GetJoinObj(ByVal oschema As IEntitySchema, _
        ByVal obj As _IEntity, ByVal subType As Type) As IKeyEntity
        Dim c As String = GetJoinFieldNameByType(obj.GetType, subType, oschema)
        Dim r As IKeyEntity = Nothing
        If Not String.IsNullOrEmpty(c) Then
            Dim id As Object = Nothing
            If obj.IsPropertyLoaded(c) Then
                id = obj.GetValueOptimized(Nothing, c, oschema)
            Else
                id = GetFieldValue(obj, c, oschema)
            End If
            r = TryCast(id, KeyEntity)
            If r Is Nothing AndAlso id IsNot Nothing Then
                Try
                    'Dim id As Integer = Convert.ToInt32(o)
                    r = OrmManager.CurrentManager.GetOrmBaseFromCacheOrCreate(id, subType)
                Catch ex As InvalidCastException
                End Try
            End If
        End If
        Return r
    End Function

    Public Function GetPropertyTypeByName(ByVal type As Type, ByVal propertyAlias As String) As Type
        Return GetPropertyTypeByName(type, GetObjectSchema(type), propertyAlias)
    End Function

    Public Function GetPropertyTypeByName(ByVal type As Type, ByVal oschema As IEntitySchema, ByVal propertyAlias As String) As Type
        'Dim t As Type = map(
        For Each de As DictionaryEntry In GetProperties(type, oschema)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
            If propertyAlias = c.PropertyAlias Then
                Return CType(de.Value, Reflection.PropertyInfo).PropertyType
            End If
        Next
        Throw New ObjectMappingException("Type " & type.Name & " doesnot contain property " & propertyAlias)
    End Function

    Public Function GetPropertyAliasByType(ByVal type As Type, ByVal propertyType As Type) As ICollection(Of String)
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
                    For Each de As DictionaryEntry In GetProperties(type)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If pi.PropertyType Is propertyType Then
                            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
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

        Dim schema As IEntitySchema = GetObjectSchema(type)

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        Dim p As MapField2Column = Nothing
        If coll.TryGetValue(propertyAlias, p) Then
            Dim c As String = Nothing
            c = p._columnName
            Return c
        End If

        Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    End Function

    Protected Function GetColumnsFromPropertyAlias(ByVal main As Type, ByVal propertyType As Type) As ColumnAttribute()
        If main Is Nothing Then Throw New ArgumentNullException("main")

        Dim l As New List(Of ColumnAttribute)

        For Each de As DictionaryEntry In GetProperties(main)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
            Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
            If pi.PropertyType Is propertyType Then
                l.Add(c)
            End If
        Next
        Return l.ToArray
    End Function

    Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String) As ColumnAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        'Dim l As New List(Of ColumnAttribute)
        Return GetColumnByPropertyAlias(main, propertyAlias, GetObjectSchema(main))
    End Function

    Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As ColumnAttribute
        If main Is Nothing Then Throw New ArgumentNullException("main")

        For Each de As DictionaryEntry In GetProperties(main, oschema)
            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
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
                                Dim schema As IEntitySchema = Nothing

                                If ea.Type Is Nothing Then
                                    Dim l As New List(Of ColumnAttribute)
                                    For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                        l.Add(c)
                                    Next

                                    schema = New SimpleObjectSchema(tp, ea.TableName, l, ea.PrimaryKey)

                                    'If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                    '    Throw New ObjectMappingException(String.Format("Type {0} has neither table name nor schema", tp))
                                    'End If
                                Else
                                    Try
                                        schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IContextObjectSchema)
                                    Catch ex As Exception
                                        Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
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
                                    Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                                End Try
                            End If
                        Next

                        If Not idic.Contains(tp) Then
                            Dim entities2() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())

                            For Each ea As EntityAttribute In entities2
                                If ea.Version = _version Then
                                    Dim schema As IEntitySchema = Nothing
                                    If ea.Type Is Nothing Then
                                        Dim l As New List(Of ColumnAttribute)
                                        For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                            l.Add(c)
                                        Next

                                        schema = New SimpleObjectSchema(tp, ea.TableName, l, ea.PrimaryKey)

                                        'If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                        '    Throw New ObjectMappingException(String.Format("Type {0} has neither table name nor schema", tp))
                                        'End If
                                    Else
                                        Try
                                            schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IContextObjectSchema)
                                        Catch ex As Exception
                                            Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
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
                                        Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
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
                                    Dim schema As IEntitySchema = Nothing
                                    If ea1.Type Is Nothing Then
                                        Dim l As New List(Of ColumnAttribute)
                                        For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                            l.Add(c)
                                        Next

                                        schema = New SimpleObjectSchema(tp, ea1.TableName, l, ea1.PrimaryKey)

                                        'If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                        '    Throw New ObjectMappingException(String.Format("Type {0} has neither table name nor schema", tp))
                                        'End If
                                    Else
                                        Try
                                            schema = CType(ea1.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IContextObjectSchema)
                                        Catch ex As Exception
                                            Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea1.Type.ToString), ex)
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
                                        Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea1.Type), ex)
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
                                        Dim schema As IEntitySchema = Nothing

                                        If ea2.Type Is Nothing Then
                                            Dim l As New List(Of ColumnAttribute)
                                            For Each c As ColumnAttribute In GetProperties(tp, Nothing).Keys
                                                l.Add(c)
                                            Next

                                            schema = New SimpleObjectSchema(tp, ea2.TableName, l, ea2.PrimaryKey)

                                            'If CType(schema, IOrmObjectSchema).GetTables.Length = 0 Then
                                            '    Throw New ObjectMappingException(String.Format("Type {0} has neither table name nor schema", tp))
                                            'End If
                                        Else
                                            Try
                                                schema = CType(ea2.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IContextObjectSchema)
                                            Catch ex As Exception
                                                Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea2.Type.ToString), ex)
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
                                            Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea2.Type), ex)
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

    Public Function GetObjectSchema(ByVal t As Type) As IEntitySchema
        Return GetObjectSchema(t, True)
    End Function

    Protected Friend Function GetObjectSchema(ByVal t As Type, ByVal check As Boolean) As IEntitySchema
        If t Is Nothing Then
            If check Then
                Throw New ArgumentNullException("t")
            Else
                Return Nothing
            End If
        End If

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

        Dim schema As IOrmObjectSchema = CType(GetObjectSchema(type), IOrmObjectSchema)

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

        Return GetTables(GetObjectSchema(type))
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

    Public Shared Function ExtractValues(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal aliases As IPrepareTable, _
        ByVal _values() As Pair(Of Object, String)) As List(Of String)
        Dim values As New List(Of String)
        'Dim lastt As Type = Nothing
        Dim d As String = schema.Delimiter
        If stmt IsNot Nothing Then
            d = stmt.Selector
        End If
        For Each p As Pair(Of Object, String) In _values
            Dim o As Object = p.First
            If o Is Nothing Then
                Throw New NullReferenceException
            End If

            If TypeOf o Is Type Then
                Dim t As Type = CType(o, Type)
                'If Not GetType(IEntity).IsAssignableFrom(t) Then
                '    Throw New NotSupportedException(String.Format("Type {0} is not assignable from IEntity", t))
                'End If
                'lastt = t

                FormatType(t, stmt, p, schema, aliases, values, Nothing)
            ElseIf TypeOf o Is SourceFragment Then
                Dim tbl As SourceFragment = CType(o, SourceFragment)
                Dim [alias] As String = Nothing
                If aliases IsNot Nothing Then
                    Debug.Assert(aliases.ContainsKey(tbl, Nothing), "There is not alias for table " & tbl.RawName)
                    Try
                        [alias] = aliases.GetAlias(tbl, Nothing)
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
                    End Try
                End If
                If Not String.IsNullOrEmpty([alias]) Then
                    values.Add([alias] & d & p.Second)
                Else
                    values.Add(p.Second)
                End If
            ElseIf TypeOf o Is ObjectSource Then
                Dim src As ObjectSource = CType(o, ObjectSource)
                Dim t As Type = src.GetRealType(schema)

                FormatType(t, stmt, p, schema, aliases, values, src)
            ElseIf o Is Nothing Then
                values.Add(p.Second)
            Else
                Throw New NotSupportedException(String.Format("Type {0} is not supported", o.GetType))
            End If
        Next
        Return values
    End Function

    Private Shared Sub FormatType(ByVal t As Type, ByVal stmt As StmtGenerator, ByVal p As Pair(Of Object, String), _
                                  ByVal schema As ObjectMappingEngine, ByVal aliases As IPrepareTable, _
                                  ByVal values As List(Of String), ByVal os As ObjectSource)
        If Not GetType(IEntity).IsAssignableFrom(t) Then
            Throw New NotSupportedException(String.Format("Type {0} is not assignable from IEntity", t))
        End If

        Dim d As String = schema.Delimiter
        If stmt IsNot Nothing Then
            d = stmt.Selector
        End If

        Dim oschema As IEntitySchema = schema.GetObjectSchema(t)
        Dim tbl As SourceFragment = Nothing
        Dim map As MapField2Column = Nothing
        Dim fld As String = p.Second
        If oschema.GetFieldColumnMap.TryGetValue(fld, map) Then
            fld = map._columnName
            tbl = map._tableName
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
        ByVal add_alias As Boolean, ByVal columnAliases As List(Of String), ByVal os As ObjectSource) As String

        If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

        Dim p As MapField2Column = Nothing
        If coll.TryGetValue(propertyAlias, p) Then
            Dim c As String = Nothing
            If add_alias AndAlso ShouldPrefix(p._columnName) Then
                c = p._tableName.UniqueName(os) & Delimiter & p._columnName
            Else
                c = p._columnName
            End If
            If columnAliases IsNot Nothing Then
                columnAliases.Add(p._columnName)
            End If
            Return c
        End If

        Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, _
        ByVal add_alias As Boolean, ByVal os As ObjectSource, Optional ByVal columnAliases As List(Of String) = Nothing) As String

        If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

        Dim schema As IEntitySchema = mpe.GetObjectSchema(type)

        Return GetColumnNameByPropertyAlias(schema, propertyAlias, add_alias, columnAliases, os)
    End Function

    Public Function GetPrimaryKeysName(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, ByVal add_alias As Boolean, _
        ByVal columnAliases As List(Of String), ByVal schema As IEntitySchema, ByVal os As ObjectSource) As String()
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
                        schema = mpe.GetObjectSchema(original_type)
                    End If

                    For Each c As ColumnAttribute In mpe.GetSortedFieldList(original_type, schema)
                        If (mpe.GetAttributes(schema, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            arr.Add(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, add_alias, columnAliases, os))
                        End If
                    Next

                    map.Add(cl_type, arr)
                End If
            End Using
        End If

        Return arr.ToArray
    End Function

    Protected Friend Sub GetPKList(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal schema As IEntitySchema, ByVal ids As StringBuilder, ByVal columnAliases As List(Of String), ByVal os As ObjectSource)
        If ids Is Nothing Then
            Throw New ArgumentNullException("ids")
        End If

        For Each pk As String In GetPrimaryKeysName(type, mpe, True, columnAliases, schema, os)
            ids.Append(pk).Append(",")
        Next
        ids.Length -= 1

        'Dim p As MapField2Column = schema.GetFieldColumnMap("ID")
        'ids.Append(GetTableName(p._tableName)).Append(Selector).Append(p._columnName)
        'If columnAliases IsNot Nothing Then
        '    columnAliases.Add(p._columnName)
        'End If
    End Sub

    Protected Friend Function GetSelectColumns(ByVal mpe As ObjectMappingEngine, ByVal props As IEnumerable(Of SelectExpression), ByVal columnAliases As List(Of String)) As String
        Dim sb As New StringBuilder
        For Each pr As SelectExpression In props
            If pr.PropType = PropType.TableColumn Then
                sb.Append(pr.Table.UniqueName(Nothing)).Append(Delimiter).Append(pr.Column).Append(", ")
            ElseIf pr.PropType = PropType.CustomValue Then
                sb.Append(String.Format(pr.Computed, ObjectMappingEngine.ExtractValues(mpe, Nothing, Nothing, pr.Values).ToArray))
                If Not String.IsNullOrEmpty(pr.Column) Then
                    sb.Append(" ").Append(pr.Column)
                End If
                sb.Append(", ")
            Else
                sb.Append(GetColumnNameByPropertyAlias(mpe.GetObjectSchema(pr.ObjectSource.GetRealType(mpe)), pr.PropertyAlias, True, columnAliases, pr.ObjectSource)).Append(", ")
            End If
        Next

        sb.Length -= 2

        Return sb.ToString
    End Function

    Protected Friend Function GetSelectColumnList(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, ByVal arr As Generic.ICollection(Of ColumnAttribute), ByVal columnAliases As List(Of String), ByVal schema As IEntitySchema, ByVal os As ObjectSource) As String
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
        For Each c As ColumnAttribute In arr
            sb.Append(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, True, columnAliases, os)).Append(", ")
        Next

        sb.Length -= 2

        'If add_c Then
        '    sel(original_type) = sb.ToString
        'End If
        Return sb.ToString
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal os As ObjectSource, Optional ByVal columnAliases As List(Of String) = Nothing) As String
        Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, True, os, columnAliases)
    End Function

    Public Function GetColumnNameByPropertyAlias(ByVal os As IEntitySchema, ByVal propertyAlias As String, ByVal osrc As ObjectSource, Optional ByVal columnAliases As List(Of String) = Nothing) As String
        Return GetColumnNameByPropertyAlias(os, propertyAlias, True, columnAliases, osrc)
    End Function

    Protected Function GetColumns4Select(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal columnAliases As List(Of String), ByVal os As ObjectSource) As String
        Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, os, columnAliases)
    End Function
#End Region
End Class

'End Namespace
