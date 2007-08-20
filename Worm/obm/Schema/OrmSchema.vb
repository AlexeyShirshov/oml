Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm

    <Serializable()> _
    Public NotInheritable Class DBSchemaException
        Inherits Exception

        Public Sub New()
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
            ' Add other code for custom properties here.
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
            ' Insert code here for custom properties here.
        End Sub
    End Class

    Public Class OrmSchemaBase

        Public Delegate Function ResolveEntity(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
        Public Delegate Function ResolveEntityName(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute

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
            Return GetProperties(t, GetObjectSchema(t))
        End Function

        'Protected Friend Function GetProperties(ByVal t As Type, ByVal schema As IOrmObjectSchema) As IDictionary
        '    Return GetProperties(t, schema)
        'End Function

        Protected Friend Function GetProperties(ByVal t As Type, ByVal schema As IOrmObjectSchemaBase) As IDictionary
            If t Is Nothing Then Throw New ArgumentNullException("original_type")

            Dim key As String = "properties" & t.ToString
            Dim h As IDictionary = CType(map(key), IDictionary)
            If h Is Nothing Then
                SyncLock String.Intern(key)
                    h = CType(map(key), IDictionary)
                    If h Is Nothing Then
                        h = New Hashtable

                        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)
                            Dim cl As ColumnAttribute = Nothing
                            Dim cls() As Attribute = CType(pi.GetCustomAttributes(GetType(ColumnAttribute), True), Attribute())
                            If cls.Length > 0 Then cl = CType(cls(0), ColumnAttribute)
                            If cl IsNot Nothing Then
                                If String.IsNullOrEmpty(cl.FieldName) Then
                                    cl.FieldName = pi.Name
                                End If
                                If schema Is Nothing OrElse Array.IndexOf(schema.GetSuppressedColumns(), cl) < 0 Then
                                    h.Add(cl, pi)
                                End If
                            End If
                        Next

                        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                            Dim cl As ColumnAttribute = Nothing
                            Dim cls() As Attribute = CType(pi.GetCustomAttributes(GetType(ColumnAttribute), True), Attribute())
                            If cls.Length > 0 Then cl = CType(cls(0), ColumnAttribute)
                            If cl IsNot Nothing Then
                                If String.IsNullOrEmpty(cl.FieldName) Then
                                    cl.FieldName = pi.Name
                                End If
                                If Not h.Contains(cl) AndAlso (schema Is Nothing OrElse Array.IndexOf(schema.GetSuppressedColumns(), cl) < 0) Then
                                    h.Add(cl, pi)
                                End If
                            End If
                        Next

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

        '    Dim tbls() As OrmTable = schema.GetTables
        '    Dim tbl As OrmTable = tbls(0)
        '    Dim js As New List(Of OrmJoin)
        '    For i As Integer = 1 To tbls.Length - 1
        '        Dim j As OrmJoin = schema.GetJoins(tbl, tbls(i))
        '        js.Add(j)
        '    Next
        '    Return js.ToArray
        'End Function

        Public Function GetEntityKey(ByVal t As Type) As String
            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

            Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

            If c IsNot Nothing Then
                Return c.GetEntityKey
            Else
                Return t.ToString
            End If
        End Function

        Public Function GetEntityTypeKey(ByVal t As Type) As Object
            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)

            Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

            If c IsNot Nothing Then
                Return c.GetEntityTypeKey
            Else
                Return t
            End If
        End Function

        <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
        Protected Function GetFieldNameByColumnName(ByVal type As Type, ByVal columnName As String) As String

            If String.IsNullOrEmpty(columnName) Then Throw New ArgumentNullException("columnName")

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

            Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

            For Each p As MapField2Column In coll
                If p._columnName = columnName Then
                    Return p._fieldName
                End If
            Next

            Throw New DBSchemaException("Cannot find column: " & columnName)
        End Function

        Protected Function GetColumnNameByFieldNameInternal(ByVal type As Type, ByVal field As String, Optional ByVal add_alias As Boolean = True) As String
            If String.IsNullOrEmpty(field) Then Throw New ArgumentNullException("field")

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

            Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

            Dim p As MapField2Column = Nothing
            If coll.TryGetValue(field, p) Then
                If add_alias AndAlso ShouldPrefix(p._columnName) Then
                    Return p._tableName.TableName & "." & p._columnName
                Else
                    Return p._columnName
                End If
            End If

            Throw New DBSchemaException("Cannot find name: " & field)
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

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(maintype)

            Return schema.GetM2MRelations
        End Function

        Public Function GetM2MRelation(ByVal maintype As Type, ByVal subtype As Type, ByVal direct As Boolean) As M2MRelation
            For Each r As M2MRelation In GetM2MRelations(maintype)
                If r.Type Is subtype AndAlso (maintype IsNot subtype OrElse r.non_direct <> direct) Then
                    Return r
                End If
            Next

            Return Nothing
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

        Public Function GetConnectedType(ByVal maintype As Type, ByVal subtype As Type) As Type
            Dim r As M2MRelation = GetM2MRelation(maintype, subtype, True)

            If r Is Nothing Then
                Throw New ArgumentException("Relation is not exists")
            Else
                Return r.ConnectedType
            End If
        End Function

        Public Function GetConnectedTypeField(ByVal ct As Type, ByVal t As Type) As String
            Dim rel As IRelation = GetConnectedTypeRelation(ct)
            If rel Is Nothing Then
                Throw New ArgumentException("Type is not implement IRelation")
            End If
            Dim p As Pair(Of String, Type) = rel.GetFirstType
            If p.Second Is t Then
                p = rel.GetSecondType
            End If
            Return p.First
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

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

            If o Is Nothing Then Return DBNull.Value

            Dim ot As System.Type = o.GetType

            If ot Is GetType(Guid) AndAlso CType(o, Guid) = Guid.Empty Then
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

        <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
        Protected Function GetFieldTable(ByVal type As Type, ByVal field As String) As OrmTable
            If type Is Nothing Then
                Throw New ArgumentNullException("type")
            End If

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

            Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

            Try
                Return coll(field)._tableName
            Catch ex As Exception
                Throw New DBSchemaException("Unknown field name: " & field, ex)
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

        Public Function ExternalSort(Of T As {OrmBase, New})(ByVal sort As Sort, ByVal objs As ICollection(Of T)) As ICollection(Of T)
            If sort.Previous IsNot Nothing Then
                Throw New ArgumentException("Sort is linked")
            End If

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(GetType(T))
            Dim s As IOrmSorting = TryCast(schema, IOrmSorting)
            If s Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} is not support sorting", GetType(T)))
            End If
            Return s.ExternalSort(Of T)(sort, objs)
        End Function

        Public Function GetJoinSelectMapping(ByVal t As Type, ByVal subType As Type) As System.Data.Common.DataTableMapping
            Dim r As M2MRelation = GetM2MRelation(t, subType, True)

            If r Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} has no relation to {1}", t.Name, subType.Name))
            End If

            Return r.Mapping
        End Function

        Public Function GetAttributes(ByVal type As Type, ByVal c As ColumnAttribute) As Field2DbRelations
            If type Is Nothing Then
                Throw New ArgumentNullException("type")
            End If

            Dim schema As IOrmObjectSchemaBase = GetObjectSchema(type)

            Return schema.GetFieldColumnMap()(c.FieldName).GetAttributes(c)
        End Function
#End Region

#Region " Helpers "
        Protected Sub GetPKList(ByVal type As Type, ByVal ids As StringBuilder)
            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            For Each pk As String In GetPrimaryKeysName(type)
                ids.Append(pk).Append(",")
            Next
            ids.Length -= 1
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

        Protected Friend Function GetSortedFieldList(ByVal original_type As Type) As Generic.List(Of ColumnAttribute)
            'Dim cl_type As String = New StringBuilder().Append("columnlist").Append(type.ToString).ToString
            Dim cl_type As String = "columnlist" & original_type.ToString

            Dim arr As Generic.List(Of ColumnAttribute) = CType(map(cl_type), Generic.List(Of ColumnAttribute))
            If arr Is Nothing Then
                SyncLock String.Intern(cl_type)
                    arr = CType(map(cl_type), Generic.List(Of ColumnAttribute))
                    If arr Is Nothing Then
                        arr = New Generic.List(Of ColumnAttribute)

                        For Each c As ColumnAttribute In GetProperties(original_type).Keys
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
        Public Function GetPrimaryKeysName(ByVal original_type As Type, Optional ByVal add_alias As Boolean = True) As String()
            Dim cl_type As String = "pklist" & original_type.ToString & add_alias

            Dim arr As Generic.List(Of String) = CType(map(cl_type), Generic.List(Of String))

            If arr Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(cl_type)
                    arr = CType(map(cl_type), Generic.List(Of String))
                    If arr Is Nothing Then
                        arr = New Generic.List(Of String)

                        For Each c As ColumnAttribute In GetSortedFieldList(original_type)
                            If (GetAttributes(original_type, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                arr.Add(GetColumnNameByFieldNameInternal(original_type, c.FieldName, add_alias))
                            End If
                        Next

                        map.Add(cl_type, arr)
                    End If
                End Using
            End If

            Return arr.ToArray
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

        Public Function GetFieldValue(ByVal obj As OrmBase, ByVal fieldName As String, Optional ByVal schema As IOrmObjectSchemaBase = Nothing) As Object
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
                Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", obj.ObjName, fieldName))
            End If

            Using obj.SyncHelper(True, fieldName)
                Return pi.GetValue(obj, Nothing)
            End Using
        End Function

        Public Sub SetFieldValue(ByVal obj As OrmBase, ByVal fieldName As String, ByVal value As Object)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim pi As Reflection.PropertyInfo = GetProperty(obj.GetType, fieldName)

            If pi Is Nothing Then
                Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", obj.ObjName, fieldName))
            End If

            Using obj.SyncHelper(False, fieldName)
                obj.SetValue(pi, GetColumnByFieldName(obj.GetType, fieldName), value)
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

        Protected Friend Function GetSelectColumnList(ByVal original_type As Type, Optional ByVal arr As Generic.ICollection(Of ColumnAttribute) = Nothing) As String
            Dim add_c As Boolean = False
            If arr Is Nothing Then
                Dim s As String = CStr(sel(original_type))
                If Not String.IsNullOrEmpty(s) Then
                    Return s
                End If
                add_c = True
            End If
            Dim sb As New StringBuilder
            If arr Is Nothing Then arr = GetSortedFieldList(original_type)
            For Each c As ColumnAttribute In arr
                sb.Append(GetColumns4Select(original_type, c.FieldName)).Append(", ")
            Next

            sb.Length -= 2

            If add_c Then
                sel(original_type) = sb.ToString
            End If
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

        Protected Function GetColumnNameByFieldName(ByVal type As Type, ByVal field As String) As String
            Return GetColumnNameByFieldNameInternal(type, field)
        End Function

        Public Function GetFieldTypeByName(ByVal type As Type, ByVal field As String) As Type
            'Dim t As Type = map(
            For Each de As DictionaryEntry In GetProperties(type)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                If field = c.FieldName Then
                    Return CType(de.Value, Reflection.PropertyInfo).PropertyType
                End If
            Next
            Throw New DBSchemaException("Type " & type.Name & " doesnot contain property " & field)
        End Function

        Public Function GetFieldNameByType(ByVal type As Type, ByVal fieldType As Type) As ICollection(Of String)
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

        Protected Function GetColumns4Select(ByVal type As Type, ByVal field As String) As String
            Return GetColumnNameByFieldName(type, field)
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
            Dim t As Type = GetType(OrmBase)
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
                            Dim entities() As Orm.EntityAttribute = CType(tp.GetCustomAttributes(GetType(Orm.EntityAttribute), False), Orm.EntityAttribute())

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
                                            Throw New DBSchemaException(String.Format("Type {0} has neither table name nor schema", tp))
                                        End If
                                    Else
                                        Try
                                            schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                        Catch ex As Exception
                                            Throw New DBSchemaException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
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
                                        Throw New DBSchemaException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                                    End Try
                                End If
                            Next

                            If Not idic.Contains(tp) Then
                                Dim entities2() As Orm.EntityAttribute = CType(tp.GetCustomAttributes(GetType(Orm.EntityAttribute), True), Orm.EntityAttribute())

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
                                                Throw New DBSchemaException(String.Format("Type {0} has neither table name nor schema", tp))
                                            End If
                                        Else
                                            Try
                                                schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                            Catch ex As Exception
                                                Throw New DBSchemaException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
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
                                            Throw New DBSchemaException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
                                        End Try
                                    End If
                                Next

                                If Not idic.Contains(tp) Then
                                    'For Each ea As EntityAttribute In entities
                                    Dim ea1 As EntityAttribute = Nothing
                                    If entities.Length > 1 AndAlso _mapv IsNot Nothing Then
                                        ea1 = _mapv(_version, entities, tp)
                                    ElseIf entities.Length = 1 Then
                                        ea1 = entities(0)
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
                                                Throw New DBSchemaException(String.Format("Type {0} has neither table name nor schema", tp))
                                            End If
                                        Else
                                            Try
                                                schema = CType(ea1.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                            Catch ex As Exception
                                                Throw New DBSchemaException(String.Format("Cannot create type [{0}]", ea1.Type.ToString), ex)
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
                                            Throw New DBSchemaException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea1.Type), ex)
                                        End Try
                                    End If
                                    'Next

                                    If Not idic.Contains(tp) Then
                                        Dim ea2 As EntityAttribute = Nothing
                                        If entities2.Length > 1 AndAlso _mapv IsNot Nothing Then
                                            ea2 = _mapv(_version, entities2, tp)
                                        ElseIf entities2.Length = 1 Then
                                            ea2 = entities2(0)
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
                                                    Throw New DBSchemaException(String.Format("Type {0} has neither table name nor schema", tp))
                                                End If
                                            Else
                                                Try
                                                    schema = CType(ea2.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IOrmObjectSchemaBase)
                                                Catch ex As Exception
                                                    Throw New DBSchemaException(String.Format("Cannot create type [{0}]", ea2.Type.ToString), ex)
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
                                                Throw New DBSchemaException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea2.Type), ex)
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
            Return CType(idic(name), Pair(Of Type, EntityAttribute)).First
        End Function

        Public Function GetObjectSchema(ByVal t As Type) As IOrmObjectSchemaBase
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

            If schema Is Nothing Then
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
    End Class

End Namespace
