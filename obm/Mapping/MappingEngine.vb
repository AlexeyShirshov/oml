Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Criteria.Core
Imports Worm.Criteria
Imports Worm.Criteria.Conditions
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm.Cache
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Reflection
Imports Worm.Collections
Imports Worm.Database

'Namespace Schema

''' <summary>
''' Данное исключение выбрасывается при определеных ошибках в <see cref="ObjectMappingEngine" />
''' </summary>
''' <remarks></remarks>
<Serializable()>
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

''' <summary>
''' Класс хранения и управления схемами объектов <see cref="IEntitySchema"/>
''' </summary>
''' <remarks>Класс управляет версиями схем объектов, предоставляет удобные обертки для методов
''' <see cref="IEntitySchema"/> через тип объекта.</remarks>
Public Class ObjectMappingEngine

    Public Delegate Function ResolveSchemaForSingleType(ByVal currentVersion As String, ByVal entities() As EntityAttribute, ByVal objType As Type) As EntityAttribute
    Public Delegate Function ResolveEntityNameForHierarchy(ByVal currentVersion As String, ByVal existingType As Type, ByVal existingTypeEntityAttribute As EntityAttribute,
                                               ByVal type2add As Type, ByVal type2addEntityAttribute As EntityAttribute) As EntityAttribute
    Public Delegate Function CreateEntityDelegate(ByVal t As Type) As Object
    Public Delegate Function ConvertVersionToIntDelegate(version As String) As Integer
    Delegate Function FallBackDelegate(prop As String, s As String) As Object
    Private ReadOnly _sharedTables As Hashtable = Hashtable.Synchronized(New Hashtable)
    Protected map As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected sel As IDictionary = Hashtable.Synchronized(New Hashtable)
    Protected _joins As IDictionary = Hashtable.Synchronized(New Hashtable)
    Private ReadOnly _typeMap As IDictionary = Hashtable.Synchronized(New Hashtable)
    Private ReadOnly _version As String
    Private ReadOnly _mapv As ResolveSchemaForSingleType
    Private ReadOnly _mapn As ResolveEntityNameForHierarchy
    Private _idic As IDictionary
    Private _names As IDictionary
    Private ReadOnly _idicSpin As New SpinLockRef
    Private _ce As CreateEntityDelegate
    Private _c2int As ConvertVersionToIntDelegate
    Private ReadOnly _features As IList(Of String) = New CoreFramework.CFCollections.ConcurrentList(Of String)
    Public ReadOnly Mark As Guid = Guid.NewGuid
    Private Shared _entityTypes As IEnumerable(Of Type)
    Private Shared ReadOnly _entityTypesSpin As New SpinLockRef
    Public Const DefaultVersion As String = "1"
    Public Const NeedEntitySchemaMapping As String = "Worm:NeedEntitySchemaMapping"
    Public Shared SkipValue As Object = New Object
    Private ReadOnly _normTypesSpin As New SpinLockRef

    Public Event EndInitSchema(idic As IDictionary)
    Public Event StartInitSchema()

    'Private _convertTypeMap As New Concurrent.ConcurrentDictionary(Of IEntitySchema, Concurrent.BlockingCollection(Of ColumnTypeMap))

    Public Sub New()
        MyClass.New(DefaultVersion)
    End Sub

    Public Sub New(ByVal version As String)
        _version = version
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveSchema As ResolveSchemaForSingleType)
        _version = version
        _mapv = resolveSchema
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveEntityName As ResolveEntityNameForHierarchy)
        _version = version
        _mapn = resolveEntityName
    End Sub

    Public Sub New(ByVal version As String, ByVal resolveSchema As ResolveSchemaForSingleType, ByVal resolveEntityName As ResolveEntityNameForHierarchy)
        _version = version
        _mapv = resolveSchema
        _mapn = resolveEntityName
    End Sub

    '#Region " reflection "

    '    Protected Friend Function GetProperties(ByVal t As Type) As IDictionary
    '        Return GetProperties(t, GetEntitySchema(t, False))
    '    End Function

    'Private Shared Function GetMappedProperties(ByVal t As Type, ByVal raw As Boolean) As IDictionary
    '    Return GetMappedProperties(t, Nothing, Nothing, raw)
    'End Function

    'Private Shared Function GetMappedProperties(ByVal t As Type) As IDictionary
    '    Return GetMappedProperties(t, Nothing, Nothing, True)
    'End Function

    'Private Shared Function GetMappedProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
    '    Dim propertyMap As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
    '    If schema IsNot Nothing Then propertyMap = schema.GetFieldColumnMap()

    '    Return GetMappedProperties(t, schema, propertyMap, False)
    'End Function

    Public Shared Function ApplyAttributes2Schema(ByVal schema As IEntitySchema, ByVal attrs As List(Of EntityPropertyAttribute),
                                                  ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary,
                                                  ByVal names As IDictionary) As Collections.IndexedCollection(Of String, MapField2Column)

        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = schema.FieldColumnMap

        Dim pks As New List(Of EntityPropertyAttribute)
        Dim attr As Field2DbRelations
        Dim pkName As String = Nothing
        Dim pi As PropertyInfo = Nothing

        For Each ep As EntityPropertyAttribute In attrs

            Dim m As MapField2Column = Nothing
            If map.TryGetValue(ep.PropertyAlias, m) Then
                m.PropertyInfo = ep._pi
                m.Schema = schema
                If m.Attributes = Field2DbRelations.None Then
                    m.Attributes = ep.Behavior
                End If

                If m.SourceFields Is Nothing OrElse m.SourceFields.Count = 0 Then
                    If ep.SourceFields Is Nothing Then
                        m.SourceFields = {New SourceField(ep.PropertyAlias)}
                    Else
                        m.SourceFields = (From k In ep.SourceFields
                                          Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias
                                             }).ToArray
                    End If
                End If

                'mapPk = (m.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK
                m.ApplyForeignKey(mpe, idic, names)
            Else

                If (ep.Behavior And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    attr = ep.Behavior
                    If String.IsNullOrEmpty(pkName) Then
                        pkName = ep.PropertyAlias
                        pi = ep._pi
                    Else
                        pkName = MapField2Column.PK
                        pi = Nothing
                    End If

                    'If Not ep.SourceFields?.Any Then
                    '    ep.SourceFields = {New SourceFieldAttribute(ep.PropertyAlias)}
                    'End If

                    pks.Add(ep)
                Else

                    m = New MapField2Column(ep.PropertyAlias, schema.Table, ep.Behavior) With {
                        .PropertyInfo = ep._pi, .Schema = schema
                    }
                    map.Add(m)

                    If Not ep.SourceFields?.Any Then
                        m.SourceFields = {New SourceField(ep.PropertyAlias)}
                    Else
                        m.SourceFields = (From k In ep.SourceFields
                                          Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias
                                             }).ToArray
                    End If

                    m.ApplyForeignKey(mpe, idic, names)
                End If
            End If
        Next

        If pks.Any Then
            Dim m As New MapField2Column(pkName, schema.Table, attr) With {
                .Schema = schema,
                .SourceFields = (From pk In pks
                                 From k In pk.SourceFields
                                 Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias,
                                             .PropertyInfo = pk._pi
                                             }).ToArray,
                .PropertyInfo = pi
            }

            If Not m.SourceFields.Any Then
                m.SourceFields = pks.Select(Function(it) New SourceField(it.PropertyAlias)).ToArray
            End If

            map.Add(m)

        End If

        Invalidate(map)

        Return map
    End Function

    Private Shared Sub Invalidate(map As IndexedCollection(Of String, MapField2Column))
        If map.Values.Where(Function(it) it.IsPK).Count > 1 Then
            Throw New ObjectMappingException("Number of pks should not be greater than 1. Use SourceFields to specify multiple columns for pk")
        End If

        If map.Values.GroupBy(Function(it) it.PropertyAlias).ToDictionary(Function(it) it.Key).Any(Function(it) it.Value.Count > 1) Then
            Throw New ObjectMappingException("Duplicate property aliases")
        End If
    End Sub

    Public Shared Function ApplyAttributes2Schema(map As Collections.IndexedCollection(Of String, MapField2Column), ByVal attrs As List(Of EntityPropertyAttribute),
                                                  ByVal mpe As ObjectMappingEngine, tbl As SourceFragment) As Collections.IndexedCollection(Of String, MapField2Column)

        Dim pks As New List(Of EntityPropertyAttribute)

        For Each ep As EntityPropertyAttribute In attrs
            Dim m As MapField2Column = Nothing
            If map.TryGetValue(ep.PropertyAlias, m) Then
                m.PropertyInfo = ep._pi
                If m.Attributes = Field2DbRelations.None Then
                    m.Attributes = ep.Behavior
                End If

                If m.SourceFields Is Nothing OrElse m.SourceFields.Count = 0 Then
                    If ep.SourceFields Is Nothing Then
                        m.SourceFields = {New SourceField(ep.PropertyAlias)}
                    Else
                        m.SourceFields = (From k In ep.SourceFields
                                          Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias
                                             }).ToArray
                    End If
                End If

                m.ApplyForeignKey(mpe, mpe.GetIdic, mpe.GetNames)
            Else

                If (ep.Behavior And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    pks.Add(ep)
                Else

                    m = New MapField2Column(ep.PropertyAlias, tbl, ep.Behavior) With {
                                        .PropertyInfo = ep._pi, .Schema = Nothing}
                    map.Add(m)

                    If ep.SourceFields Is Nothing Then
                        m.SourceFields = {New SourceField(ep.PropertyAlias)}
                    Else
                        m.SourceFields = (From k In ep.SourceFields
                                          Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias
                                             }).ToArray
                    End If

                    m.ApplyForeignKey(mpe, mpe.GetIdic, mpe.GetNames)
                End If
            End If
        Next

        If pks.Any Then
            Dim m As New MapField2Column(tbl, Field2DbRelations.PK) With {
                .SourceFields = (From pk In pks
                                 From k In pk.SourceFields
                                 Select New SourceField(k.ColumnExpression) With {
                                             .DBType = New DBType(k.SourceFieldType, k.SourceFieldSize, k.IsNullable),
                                             .SourceFieldAlias = k.ColumnAlias
                                             }).ToArray}
            map.Add(m)

        End If

        Invalidate(map)

        Return map
    End Function

    Friend Function NormalType(t As Type) As Type
        Dim idic = GetIdic()
        If Not idic.Contains(t) Then
            Using New CSScopeMgrLite(_normTypesSpin)
                Dim rt = CType(_typeMap(t), Type)
                If rt Is Nothing Then

                    For Each kv As DictionaryEntry In idic
                        Dim ts = kv.Key.ToString
                        If ts = t.ToString Then
                            rt = CType(kv.Key, Type)
                            _typeMap(t) = rt
                        End If
                    Next
                End If

                If rt IsNot Nothing Then
                    Return rt
                End If
            End Using
#If nlog Then
            NLog.LogManager.GetCurrentClassLogger?.Trace("Normal type {1}. Hash: {0} is not found", t.GetHashCode, t)
#End If
            'Throw New ObjectMappingException($"Type {t} ")
        End If

        Return t
    End Function

    Public Function ConvertFromString(oschema As IPropertyMap, prop As String, s As String, Optional fallback As IStringValueConverter.FallBackDelegate = Nothing) As Object
        Dim sc = TryCast(oschema, IStringValueConverter)
        Dim v As Object = Nothing
        If sc IsNot Nothing Then
            If sc.Convert(Me, prop, s, v) Then
                Return v
            End If
        End If

        If oschema Is Nothing Then
            Return If(fallback IsNot Nothing, fallback(), s)
        End If

        If String.IsNullOrEmpty(prop) Then
            Return If(fallback IsNot Nothing, fallback(), s)
        End If

        Dim m As MapField2Column = Nothing
        If Not oschema.FieldColumnMap.TryGetValue(prop, m) OrElse map Is Nothing Then
            Return If(fallback IsNot Nothing, fallback(), s)
        End If

        Dim pt = m.PropertyType
        If pt Is Nothing Then
            Return If(fallback IsNot Nothing, fallback(), s)
        End If

        pt = If(Nullable.GetUnderlyingType(pt), pt)

        If String.IsNullOrEmpty(s) AndAlso pt IsNot GetType(String) Then
            Return Nothing
        Else

            If pt.IsEnum Then
                Return [Enum].Parse(pt, s)
            Else
                Return Convert.ChangeType(s, pt)
            End If
        End If
    End Function
    Public Sub LoadEntityValues(entity As Object, values As IDictionary(Of String, String), Optional fallBack As FallBackDelegate = Nothing,
                                Optional errors As IList(Of Exception) = Nothing)
        If entity Is Nothing Then
            Throw New ArgumentNullException(NameOf(entity))
        End If

        If values Is Nothing Then
            Return
        End If

        Dim osc = TryCast(entity, IStringValueConverter)
        Dim oschema = GetEntitySchema(entity.GetType)

        For Each de In values
            Dim prop = de.Key
            Dim s = de.Value
            Dim v As Object = Nothing
            If osc IsNot Nothing Then
                If osc.Convert(Me, prop, s, v) Then
                    SetPropertyValue(entity, prop, v, oschema)
                    Continue For
                End If
            End If

            Dim fb As IStringValueConverter.FallBackDelegate = Nothing
            If fallBack IsNot Nothing Then
                fb = Function() fallBack(prop, s)
            End If


            Try
                v = ConvertFromString(oschema, prop, s, fb)
            Catch ex As Exception
                Dim newEx As New ObjectMappingException(String.Format("cannot convert property {0} from value {1}", prop, s), ex)
                If errors IsNot Nothing Then
                    errors.Add(newEx)
                    Continue For
                Else
                    'CoreFramework.Debugging.Stack.PreserveStackTrace(ex)
                    'Throw ex
                    Throw newEx
                End If
            End Try

            If v IsNot SkipValue Then
                SetPropertyValue(entity, prop, v, oschema)
            End If
        Next
    End Sub
    Public Sub LoadEntityValues(entity As Object, values As Specialized.NameValueCollection, Optional fallBack As FallBackDelegate = Nothing,
                                Optional errors As IList(Of Exception) = Nothing)
        LoadEntityValues(entity, values?.AllKeys.ToDictionary(Function(it) it, Function(it2) values?(it2)), fallBack, errors)
    End Sub
    Public Shared Function ChooseOneVersionable(Of T As {Class, IVersionable})(objects As IEnumerable(Of T),
                                                                               mpeVersion As String, convertVersion As ConvertVersionToIntDelegate,
                                                                               features As IEnumerable(Of String)) As T
        Dim r = ChooseVersionable(mpeVersion, convertVersion, objects, features)

        Return r?.FirstOrDefault
    End Function
    Public Shared Function ChooseVersionable(Of T As IVersionable)(mpeVersion As String, convertVersion As ConvertVersionToIntDelegate,
                                                                   objects As IEnumerable(Of T), features As IEnumerable(Of String)) As IEnumerable(Of T)
        If objects.Count = 1 Then
            Dim obj = objects(0)

            If Not String.IsNullOrEmpty(obj.Feature) AndAlso Not features.Contains(obj.Feature) Then
                Return Nothing
            End If

            If Not String.IsNullOrEmpty(obj.SchemaVersion) AndAlso obj.SchemaVersion <> ObjectMappingEngine.NeedEntitySchemaMapping Then
                Select Case obj.SchemaVersionOperator
                    Case SchemaVersionOperatorEnum.Equal
                        If obj.SchemaVersion = mpeVersion Then
                            Return objects
                        End If
                    Case SchemaVersionOperatorEnum.GreaterEqual
                        Dim schemaVer As Integer = 0
                        If Not Integer.TryParse(mpeVersion, schemaVer) AndAlso convertVersion IsNot Nothing Then
                            schemaVer = convertVersion(mpeVersion)
                        End If
                        Dim colVer As Integer = Integer.MinValue
                        If Not Integer.TryParse(obj.SchemaVersion, colVer) AndAlso convertVersion IsNot Nothing Then
                            colVer = convertVersion(obj.SchemaVersion)
                        End If
                        If schemaVer >= colVer Then
                            Return objects
                        End If
                    Case SchemaVersionOperatorEnum.LessThan
                        Dim schemaVer As Integer = 0
                        If Not Integer.TryParse(mpeVersion, schemaVer) AndAlso convertVersion IsNot Nothing Then
                            schemaVer = convertVersion(mpeVersion)
                        End If
                        Dim colVer As Integer = Integer.MaxValue
                        If Not Integer.TryParse(obj.SchemaVersion, colVer) AndAlso convertVersion IsNot Nothing Then
                            colVer = convertVersion(obj.SchemaVersion)
                        End If
                        If schemaVer < colVer Then
                            Return objects
                        End If
                End Select
            Else
                Return objects
            End If
        ElseIf objects.Count > 1 Then

            Dim schemaMatchPredicate = Function(schemaVersionOperator As SchemaVersionOperatorEnum, schemaVersion As String) As Boolean
                                           Select Case schemaVersionOperator
                                               Case SchemaVersionOperatorEnum.Equal
                                                   If schemaVersion = mpeVersion Then
                                                       Return True
                                                   End If
                                               Case SchemaVersionOperatorEnum.GreaterEqual
                                                   Dim schemaVer As Integer = 0
                                                   If Not Integer.TryParse(mpeVersion, schemaVer) AndAlso convertVersion IsNot Nothing Then
                                                       schemaVer = convertVersion(mpeVersion)
                                                   End If
                                                   Dim colVer As Integer = Integer.MinValue
                                                   If Not Integer.TryParse(schemaVersion, colVer) AndAlso convertVersion IsNot Nothing Then
                                                       colVer = convertVersion(schemaVersion)
                                                   End If
                                                   If schemaVer >= colVer Then
                                                       Return True
                                                   End If
                                               Case SchemaVersionOperatorEnum.LessThan
                                                   Dim schemaVer As Integer = 0
                                                   If Not Integer.TryParse(mpeVersion, schemaVer) AndAlso convertVersion IsNot Nothing Then
                                                       schemaVer = convertVersion(mpeVersion)
                                                   End If
                                                   Dim colVer As Integer = Integer.MaxValue
                                                   If Not Integer.TryParse(schemaVersion, colVer) AndAlso convertVersion IsNot Nothing Then
                                                       colVer = convertVersion(schemaVersion)
                                                   End If
                                                   If schemaVer < colVer Then
                                                       Return True
                                                   End If
                                           End Select

                                           Return False
                                       End Function

            Return From k In objects
                   Where ((Not String.IsNullOrEmpty(k.SchemaVersion) AndAlso k.SchemaVersion <> NeedEntitySchemaMapping AndAlso schemaMatchPredicate(k.SchemaVersionOperator, k.SchemaVersion)) OrElse String.IsNullOrEmpty(k.SchemaVersion)) AndAlso
                              (String.IsNullOrEmpty(k.Feature) OrElse features.Contains(k.Feature))

        End If

        Return Nothing
    End Function
    Private Shared Function CreateProp(ByVal t As Type, props() As EntityPropertyAttribute, pi As Reflection.PropertyInfo, raw As Boolean, mpeVersion As String, convertVersion As ConvertVersionToIntDelegate, features As IEnumerable(Of String)) As EntityPropertyAttribute
        If props.Length = 0 AndAlso pi.CanRead AndAlso (pi.CanWrite OrElse GetType(IOptimizedValues).IsAssignableFrom(t)) Then
            If raw Then

                Dim bd As Reflection.MethodInfo = pi.GetGetMethod.GetBaseDefinition
                Do While bd IsNot Nothing AndAlso bd.IsVirtual
                    If Array.IndexOf(New Type() {GetType(Entity), GetType(CachedEntity), GetType(CachedLazyLoad), GetType(SinglePKEntityBase),
                                 GetType(SinglePKEntity), GetType(QueryCmd), GetType(RelationCmd)}, bd.DeclaringType) >= 0 Then ', GetType(EntityLazyLoad)
                        Return Nothing
                    ElseIf pi.DeclaringType Is bd.DeclaringType Then
                        Exit Do
                    End If
                    bd = bd.GetBaseDefinition
                Loop

                If Array.IndexOf(New Type() {GetType(QueryCmd), GetType(RelationCmd)}, bd.ReturnType) >= 0 Then ', GetType(EntityLazyLoad)
                    Return Nothing
                End If

                Return New EntityPropertyAttribute() With {.PropertyAlias = pi.Name, ._raw = True}
            Else
                Dim fields() As SourceFieldAttribute = CType(Attribute.GetCustomAttributes(pi, GetType(SourceFieldAttribute), False), SourceFieldAttribute())
                If fields.Length = 0 Then
                    Return Nothing
                Else
                    Return New EntityPropertyAttribute() With {.PropertyAlias = pi.Name, .SourceFields = ChooseVersionable(mpeVersion, convertVersion, fields, features).ToArray}
                End If
            End If
        Else
            Dim o = ChooseOneVersionable(props, mpeVersion, convertVersion, features)
            If o IsNot Nothing Then
                If Not o.SourceFields?.Any Then
                    Dim fields() As SourceFieldAttribute = CType(Attribute.GetCustomAttributes(pi, GetType(SourceFieldAttribute), False), SourceFieldAttribute())
                    If fields IsNot Nothing Then
                        o.SourceFields = If(ChooseVersionable(mpeVersion, convertVersion, fields, features)?.ToArray, {})
                    End If
                End If
                'If Not o.SourceFields?.Any Then
                '    o.SourceFields = {New SourceFieldAttribute(o.PropertyAlias)}
                'End If

                If String.IsNullOrEmpty(o.PropertyAlias) Then
                    o.PropertyAlias = pi.Name
                End If
            End If

            Return o
        End If
    End Function
    Friend Shared Function GetMappedProperties(ByVal t As Type, ByVal mpeVersion As String,
                                               ByVal raw As Boolean, ByVal includeBase As Boolean, convertVersion As ConvertVersionToIntDelegate, features As IEnumerable(Of String)) As List(Of EntityPropertyAttribute)

        Dim l As New List(Of EntityPropertyAttribute)

        For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly)

            Dim prop = CreateProp(t, CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), EntityPropertyAttribute()), pi, raw, mpeVersion, convertVersion, features)

            If prop IsNot Nothing Then
                'If String.IsNullOrEmpty(prop.PropertyAlias) Then
                '    prop.PropertyAlias = pi.Name
                '    'For Each sf In prop.SourceFields
                '    '    sf.ColumnExpression = pi.Name
                '    'Next
                'End If

                prop._pi = pi

                If l.Exists(Function(ep) ep.PropertyAlias = prop.PropertyAlias) Then
                    Throw New ObjectMappingException($"Duplicate property {prop.PropertyAlias}")
                End If

                l.Add(prop)
            End If
        Next

        If includeBase Then
            For Each pi As Reflection.PropertyInfo In t.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                If Array.IndexOf(New Type() {GetType(Entity), GetType(CachedEntity), GetType(CachedLazyLoad), GetType(SinglePKEntityBase), GetType(SinglePKEntity)}, pi.DeclaringType) >= 0 Then ', GetType(EntityLazyLoad)
                    Continue For
                End If

                Dim props() As EntityPropertyAttribute = CType(Attribute.GetCustomAttributes(pi, GetType(EntityPropertyAttribute)), EntityPropertyAttribute())
                Dim prop = CreateProp(t, props, pi, raw, mpeVersion, convertVersion, features)

                If prop IsNot Nothing Then
                    'If String.IsNullOrEmpty(prop.PropertyAlias) Then
                    '    prop.PropertyAlias = pi.Name
                    '    'For Each sf In prop.SourceFields
                    '    '    sf.ColumnExpression = pi.Name
                    '    'Next
                    'End If

                    If Not l.Exists(Function(ep) ep.PropertyAlias = prop.PropertyAlias) Then
                        prop._pi = pi
                        l.Add(prop)
                    End If
                End If
            Next
        End If

        Return l
    End Function

    '    Public Function GetRefProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IList
    '        If t Is Nothing Then Throw New ArgumentNullException("t")
    '        Dim s As String = Nothing
    '        If schema Is Nothing Then
    '            s = t.ToString
    '        Else
    '            s = t.ToString & schema.GetType().ToString
    '        End If
    '        Dim key As String = "refproperties" & s
    '        Dim d As IList = CType(map(key), IList)
    '        If d Is Nothing Then
    '            Using SyncHelper.AcquireDynamicLock(key)
    '                d = CType(map(key), IList)
    '                If d Is Nothing Then
    '                    Dim dic As IDictionary = GetProperties(t, schema)
    '                    d = New ArrayList
    '                    For Each tde As DictionaryEntry In dic
    '                        Dim pi As Reflection.PropertyInfo = CType(tde.Value, Reflection.PropertyInfo)
    '                        Dim pit As Type = pi.PropertyType
    '                        If ObjectMappingEngine.IsEntityType(pit, Me) Then
    '                            d.Add(tde)
    '                        End If
    '                    Next
    '                End If
    '            End Using
    '        End If
    '        Return d
    '    End Function

    '    Public Function GetProperties(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
    '        If t Is Nothing Then Throw New ArgumentNullException("t")
    '        Dim s As String = Nothing
    '        If schema Is Nothing Then
    '            s = t.ToString
    '        Else
    '            s = t.ToString & schema.GetType().ToString
    '        End If
    '        Dim key As String = "properties" & s
    '        Dim h As IDictionary = CType(map(key), IDictionary)
    '        If h Is Nothing Then
    '            Using SyncHelper.AcquireDynamicLock(key)
    '                h = CType(map(key), IDictionary)
    '                If h Is Nothing Then
    '                    h = GetMappedProperties(t, schema)

    '                    map(key) = h
    '                End If
    '            End Using
    '        End If
    '        Return h
    '    End Function

    '#End Region

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

    'Public Function GetMappedFields(ByVal schema As IEntitySchema) As ICollection(Of MapField2Column)
    '    Dim sup() As String = Nothing
    '    Dim s As IEntitySchemaBase = TryCast(schema, IEntitySchemaBase)
    '    If s IsNot Nothing Then sup = s.GetSuppressedFields()
    '    If sup Is Nothing OrElse sup.Length = 0 Then
    '        Return schema.GetFieldColumnMap.Values
    '    End If
    '    Dim l As New List(Of MapField2Column)
    '    For Each m As MapField2Column In schema.GetFieldColumnMap
    '        If Array.IndexOf(sup, m.PropertyAlias) < 0 Then
    '            l.Add(m)
    '        End If
    '    Next
    '    Return l
    'End Function

    Public Function GetEntityKey(ByVal t As Type) As String
        Dim schema As IEntitySchema = GetEntitySchema(t, False)

        Dim c As ICacheBehavior = TryCast(schema, ICacheBehavior)

        If c IsNot Nothing Then
            Return c.GetEntityKey()
        Else
            Return t.ToString
        End If
    End Function

    Public Function GetEntityTypeKey(ByVal t As Type) As Object
        Return GetEntityTypeKey(t, TryCast(GetEntitySchema(t), ICacheBehavior))
    End Function

    Public Shared Function GetEntityTypeKey(ByVal t As Type, ByVal c As ICacheBehavior) As Object
        If c IsNot Nothing Then
            Return c.GetEntityTypeKey()
        Else
            Return t
        End If
    End Function

    '<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    'Protected Function GetPropertyAliasByColumnName(ByVal type As Type, ByVal columnName As String) As String

    '    If String.IsNullOrEmpty(columnName) Then Throw New ArgumentNullException("columnName")

    '    Dim schema As IEntitySchema = GetEntitySchema(type)

    '    Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

    '    For Each p As MapField2Column In coll
    '        If p.ColumnExpression = columnName Then
    '            Return p.PropertyAlias
    '        End If
    '    Next

    '    Throw New ObjectMappingException("Cannot find column: " & columnName)
    'End Function

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



    Public Function GetM2MRelations(ByVal maintype As Type) As M2MRelationDesc()
        If maintype Is Nothing Then
            Throw New ArgumentNullException("maintype")
        End If

        Return GetEntitySchema(maintype).GetM2MRelations()
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
        Return GetM2MRel(oschema.GetM2MRelations(), subtype, key)
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
        Dim mr() As M2MRelationDesc = mainSchema.GetM2MRelations()
        Return GetM2MRel(mr, subtype, key)
    End Function

    Private Function GetM2MRel(ByVal mr() As M2MRelationDesc, ByVal subtype As Type, ByVal key As String) As M2MRelationDesc
        'If String.IsNullOrEmpty(key) Then key = M2MRelationDesc.DirKey
        For Each r As M2MRelationDesc In mr
            If r.Entity.GetRealType(Me) Is subtype AndAlso (String.Equals(r.Key, key) OrElse M2MRelationDesc.IsDirect(r.Key) = M2MRelationDesc.IsDirect(key)) Then
                Return r
            End If
        Next

        For Each r As M2MRelationDesc In mr
            Dim n As String = r.EntityName
            If String.IsNullOrEmpty(n) Then
                n = GetEntityNameByType(r.Entity.GetRealType(Me))
                If Not String.IsNullOrEmpty(n) Then
                    Dim n2 As String = GetEntityNameByType(subtype)
                    If String.Equals(n, n2) AndAlso (String.Equals(r.Key, key) OrElse M2MRelationDesc.IsDirect(r.Key) = M2MRelationDesc.IsDirect(key)) Then
                        Return r
                    End If
                End If
            Else
                Dim n2 As String = GetEntityNameByType(subtype)
                If String.Equals(n, n2) AndAlso (String.Equals(r.Key, key) OrElse M2MRelationDesc.IsDirect(r.Key) = M2MRelationDesc.IsDirect(key)) Then
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

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822")>
    Public Function ChangeValueType(ByVal type As Type, ByVal propertyAlias As String, ByVal o As Object) As Object
        If type Is Nothing Then
            Throw New ArgumentNullException("type")
        End If

        Return GetEntitySchema(type).ReplaceValueOnSave(propertyAlias, o)
    End Function

    Public Function GetPrimaryKey(ByVal t As Type, Optional ByVal oschema As IEntitySchema = Nothing) As String
        If oschema Is Nothing Then
            oschema = GetEntitySchema(t)
        End If
        'Dim pk As String = Nothing
        'For Each mp As MapField2Column In oschema.GetPKs
        '    If String.IsNullOrEmpty(pk) Then
        '        pk = mp.PropertyAlias
        '    Else
        '        Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", t))
        '    End If
        'Next
        'Return pk
        Return oschema.GetPK?.PropertyAlias
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

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")>
    Public Function GetPropertyTable(ByVal schema As IPropertyMap, ByVal propertAlias As String) As SourceFragment
        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.FieldColumnMap()
        Try
            Return coll(propertAlias).Table
        Catch ex As Exception
            Throw New ObjectMappingException("Unknown field name: " & propertAlias, ex)
        End Try
    End Function

    Public Function GetJoinSelectMapping(ByVal t As Type, ByVal subType As Type) As System.Data.Common.DataTableMapping
        Dim r As M2MRelationDesc = GetM2MRelation(t, subType, True)

        If r Is Nothing Then
            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", t.Name, subType.Name))
        End If

        Return r.Mapping
    End Function

    'Public Function GetAttributes(ByVal type As Type, ByVal c As EntityPropertyAttribute) As Field2DbRelations
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type")
    '    End If

    '    Dim schema As IEntitySchema = GetEntitySchema(type)

    '    Return GetAttributes(schema, c)
    'End Function

    'Public Function GetAttributes(ByVal schema As IEntitySchema, ByVal c As EntityPropertyAttribute) As Field2DbRelations
    '    If schema Is Nothing Then
    '        Throw New ArgumentNullException("schema")
    '    End If

    '    Return schema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c)
    'End Function
#End Region

#Region " Helpers "
    Public Function HasProperty(ByVal t As Type, ByVal propertyAlias As String) As Boolean
        If String.IsNullOrEmpty(propertyAlias) Then Return False

        Dim schema As IEntitySchema = GetEntitySchema(t, False)

        If schema IsNot Nothing Then
            Return schema.FieldColumnMap.ContainsKey(propertyAlias)
        Else
            For Each ep As EntityPropertyAttribute In GetMappedProperties(t, Version, False, True, _c2int, _features)
                If ep.PropertyAlias = propertyAlias Then
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

    'Protected Friend Function GetProperty(ByVal original_type As Type, ByVal c As EntityPropertyAttribute) As Reflection.PropertyInfo
    '    Return CType(GetProperties(original_type)(c), Reflection.PropertyInfo)
    'End Function

    'Protected Friend Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal c As EntityPropertyAttribute) As Reflection.PropertyInfo
    '    Return CType(GetProperties(t, schema)(c), Reflection.PropertyInfo)
    'End Function

    'Protected Friend Function GetProperty(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
    '    If String.IsNullOrEmpty(propertyAlias) Then
    '        Throw New ArgumentNullException("propertyAlias")
    '    End If

    '    Return GetProperty(original_type, New EntityPropertyAttribute() With {.PropertyAlias = propertyAlias})
    'End Function

    'Public Function GetProperty(ByVal t As Type, ByVal schema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
    '    If String.IsNullOrEmpty(propertyAlias) Then
    '        Throw New ArgumentNullException("propertyAlias")
    '    End If

    '    Return GetProperty(t, schema, New EntityPropertyAttribute() With {.PropertyAlias = propertyAlias})
    'End Function

    'Protected Friend Shared Function GetPropertyInt(ByVal original_type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
    '    If String.IsNullOrEmpty(propertyAlias) Then
    '        Throw New ArgumentNullException("propertyAlias")
    '    End If

    '    Return CType(GetMappedProperties(original_type, False)(New EntityPropertyAttribute() With {.PropertyAlias = propertyAlias}), Reflection.PropertyInfo)
    'End Function

    'Protected Friend Shared Function GetPropertyInt(ByVal t As Type, ByVal oschema As IEntitySchema, ByVal propertyAlias As String) As Reflection.PropertyInfo
    '    If String.IsNullOrEmpty(propertyAlias) Then
    '        Throw New ArgumentNullException("propertyAlias")
    '    End If

    '    Return CType(GetMappedProperties(t, oschema)(New EntityPropertyAttribute() With {.PropertyAlias = propertyAlias}), Reflection.PropertyInfo)
    'End Function

    'Public Function GetSortedFieldList(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As Generic.List(Of EntityPropertyAttribute)
    '    'If Not GetType(_ICachedEntity).IsAssignableFrom(original_type) Then
    '    '    Return Nothing
    '    'End If

    '    If schema Is Nothing Then
    '        schema = GetEntitySchema(original_type)
    '    End If
    '    Dim cl_type As String = "columnlist" & original_type.ToString & schema.GetType.ToString

    '    Dim arr As Generic.List(Of EntityPropertyAttribute) = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
    '    If arr Is Nothing Then
    '        Using SyncHelper.AcquireDynamicLock(cl_type)
    '            arr = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
    '            If arr Is Nothing Then
    '                arr = New Generic.List(Of EntityPropertyAttribute)

    '                For Each c As EntityPropertyAttribute In GetProperties(original_type, schema).Keys
    '                    arr.Add(c)
    '                Next

    '                arr.Sort()

    '                map.Add(cl_type, arr)
    '            End If
    '        End Using
    '    End If
    '    Return arr
    'End Function

    '<MethodImpl(MethodImplOptions.Synchronized)> _

    'Public Function GetPrimaryKeys(ByVal original_type As Type, Optional ByVal schema As IEntitySchema = Nothing) As List(Of EntityPropertyAttribute)
    '    Dim cl_type As String = "clm_pklist" & original_type.ToString

    '    Dim arr As Generic.List(Of EntityPropertyAttribute) = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))

    '    If arr Is Nothing Then
    '        Using SyncHelper.AcquireDynamicLock(cl_type)
    '            arr = CType(map(cl_type), Generic.List(Of EntityPropertyAttribute))
    '            If arr Is Nothing Then
    '                arr = New Generic.List(Of EntityPropertyAttribute)

    '                For Each c As EntityPropertyAttribute In GetSortedFieldList(original_type, schema)
    '                    Dim att As Field2DbRelations
    '                    If schema IsNot Nothing Then
    '                        att = GetAttributes(schema, c)
    '                    Else
    '                        att = GetAttributes(original_type, c)
    '                    End If
    '                    If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
    '                        arr.Add(c)
    '                    End If
    '                Next

    '                map.Add(cl_type, arr)
    '            End If
    '        End Using
    '    End If

    '    Return arr
    'End Function

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

    'Public Shared Function GetPropertyValueSchemaless(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal pi As Reflection.PropertyInfo) As Object
    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj")
    '    End If

    '    Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
    '    If ov Is Nothing Then
    '        If pi Is Nothing Then
    '            If schema Is Nothing Then
    '                pi = GetPropertyInt(obj.GetType, propertyAlias)
    '            Else
    '                pi = GetPropertyInt(obj.GetType, schema, propertyAlias)
    '            End If
    '        End If

    '        If pi Is Nothing Then
    '            Throw New ArgumentException(String.Format("{0} doesnot contain field {1}", CType(obj, _IEntity).ObjName, propertyAlias))
    '        End If

    '        Return pi.GetValue(obj, Nothing)
    '    Else
    '        If obj.IsPropertyLoaded(propertyAlias) Then
    '            Return ov.GetValueOptimized(propertyAlias, schema)
    '        Else
    '            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
    '            If ll IsNot Nothing Then
    '                Using ll.Read(propertyAlias)
    '                    Return ov.GetValueOptimized(propertyAlias, schema)
    '                End Using
    '            Else
    '                Return ov.GetValueOptimized(propertyAlias, schema)
    '            End If
    '        End If
    '    End If
    'End Function

    Public Function GetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException("propertyAlias")
        End If

        Dim schema As IEntitySchema = GetEntitySchema(obj.GetType)

        Return ObjectMappingEngine.GetPropertyValue(obj, propertyAlias, schema)
    End Function

    'Public Shared Function GetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, _
    '    ByVal schema As IEntitySchema, Optional ByVal pi As Reflection.PropertyInfo = Nothing) As Object
    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj")
    '    End If

    '    Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
    '    If ov Is Nothing Then
    '        If pi Is Nothing Then
    '            Dim m As MapField2Column = Nothing
    '            If Not schema.GetFieldColumnMap.TryGetValue(propertyAlias, m) Then
    '                Throw New ArgumentException(String.Format("Type {0} doesnot contain field {1}", obj.GetType, propertyAlias))
    '            End If
    '            pi = m.PropertyInfo
    '        End If

    '        Return pi.GetValue(obj, Nothing)
    '    Else
    '        Dim e As _IEntity = TryCast(obj, _IEntity)
    '        If e IsNot Nothing AndAlso e.IsPropertyLoaded(propertyAlias) Then
    '            Return ov.GetValueOptimized(propertyAlias, schema)
    '        Else
    '            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
    '            If ll IsNot Nothing Then
    '                Using ll.Read(propertyAlias)
    '                    Return ov.GetValueOptimized(propertyAlias, schema)
    '                End Using
    '            Else
    '                Return ov.GetValueOptimized(propertyAlias, schema)
    '            End If
    '        End If
    '    End If
    'End Function

    ''' <summary>
    ''' Возвращает значение данного свойства объекта
    ''' </summary>
    ''' <param name="obj">Объект</param>
    ''' <param name="propertyAlias">Имя поля</param>
    ''' <param name="oschema">Схема объекта</param>
    ''' <param name="pi"><see cref="Reflection.PropertyInfo"/> для поля</param>
    ''' <returns>Значение поля</returns>
    ''' <remarks>Если тип не реализует <see cref="IOptimizedValues"/>, то используется метод получения значения через рефлекшн. 
    ''' Если реализует и свойство загружено или тип не реализует <see cref="IPropertyLazyLoad"/>, вызывает <see cref="IOptimizedValues.GetValueOptimized"/>.
    ''' Если свойство не загружено и тип реализует <see cref="IPropertyLazyLoad"/>, 
    ''' вызов <see cref="IOptimizedValues.GetValueOptimized"/> обрамляется объектом, который возвращает метод <see cref="IPropertyLazyLoad.Read"/></remarks>
    ''' <exception cref="ArgumentException">Если тип не реализует интерфейс <see cref="IOptimizedValues"/> и параметр <paramref name="pi"/> не задан.</exception>
    Public Shared Function GetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, ByVal oschema As IEntitySchema, Optional ByVal pi As Reflection.PropertyInfo = Nothing) As Object
        If obj Is Nothing Then
            Throw New ArgumentNullException(NameOf(obj))
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
l1:
            If pi Is Nothing Then
                Dim m As MapField2Column = Nothing
                If oschema Is Nothing Then
                    Throw New ArgumentNullException("oschema")
                End If

                If Not oschema.FieldColumnMap.TryGetValue(propertyAlias, m) Then
                    Throw New ArgumentException(String.Format("Type {0} doesnot contain field {1}", obj.GetType, propertyAlias))
                End If
                pi = m.PropertyInfo
            End If

            If pi Is Nothing Then
                Dim t = obj.GetType
                pi = t.GetProperty(propertyAlias)
            End If

            Return pi?.GetValue(obj, Nothing)
        Else
            'If GetType(_IEntity).IsAssignableFrom(obj.GetType) AndAlso CType(obj, _IEntity).IsPropertyLoaded(propertyAlias) Then
            '    Return ov.GetValueOptimized(propertyAlias, oschema)
            'Else
            Dim r As Object
            Dim found As Boolean
            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
            If ll IsNot Nothing Then
                Using ll.Read(propertyAlias)
                    r = ov.GetValueOptimized(propertyAlias, oschema, found)
                End Using
            Else
                r = ov.GetValueOptimized(propertyAlias, oschema, found)
            End If

            If Not found Then
                GoTo l1
            Else
                Return r
            End If
            'End If
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

    Public Sub SetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, ByVal value As Object)
        If obj Is Nothing Then
            Throw New ArgumentNullException(NameOf(obj))
        End If

        Dim schema As IEntitySchema = Nothing
        Dim e = TryCast(obj, IEntity)
        If e IsNot Nothing Then
            schema = e.GetEntitySchema(Me)
        Else
            schema = GetEntitySchema(obj.GetType)
        End If

        SetPropertyValue(obj, propertyAlias, value, schema, Nothing)
    End Sub

    Public Shared Function SetPropertyValue(ByVal obj As Object, ByVal propertyAlias As String, ByVal value As Object,
                                            ByVal oschema As IEntitySchema,
                                            Optional ByVal pi As Reflection.PropertyInfo = Nothing) As Boolean

        If obj Is Nothing Then
            Throw New ArgumentNullException(NameOf(obj))
        End If

        Dim ov As IOptimizedValues = TryCast(obj, IOptimizedValues)
        If ov Is Nothing Then
l1:
            If pi Is Nothing Then
                Dim m As MapField2Column = Nothing
                If oschema IsNot Nothing AndAlso oschema.FieldColumnMap.TryGetValue(propertyAlias, m) Then

                    If m.OptimizedSetValue IsNot Nothing AndAlso m.OptimizedSetValue IsNot MapField2Column.EmptyOptimizedSetValue Then

                        Dim uc As IUndoChanges = TryCast(obj, IUndoChanges)
                        If uc IsNot Nothing Then
                            Using uc.Write(propertyAlias)
                                m.OptimizedSetValue(obj, value)
                            End Using
                        Else
                            m.OptimizedSetValue(obj, value)
                        End If

                        Return True
                    End If

                    pi = m.PropertyInfo
                End If
            End If

            If pi Is Nothing Then
                Dim t = obj.GetType
                pi = t.GetProperty(propertyAlias)
            End If

            If pi Is Nothing Then
                Return False
            End If

            pi.SetValue(obj, value, Nothing)

            Return True
        Else
            Dim uc As IUndoChanges = TryCast(obj, IUndoChanges)
            Dim r As Boolean
            If uc IsNot Nothing Then
                Using uc.Write(propertyAlias)
                    r = ov.SetValueOptimized(propertyAlias, oschema, value)
                End Using
            Else
                r = ov.SetValueOptimized(propertyAlias, oschema, value)
            End If

            If Not r Then
                GoTo l1
            End If

            Return True
        End If
    End Function

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
        'se.Column = c.Column
        Dim se As New SelectExpression(t, c.PropertyAlias) With {
            .Attributes = c.Behavior
        }
        Return se
    End Function

    Public Shared Function ConvertColumn2SelExp(ByVal c As EntityPropertyAttribute, ByVal os As EntityUnion) As SelectExpression
        'se.Column = c.Column
        Dim se As New SelectExpression(os, c.PropertyAlias) With {
            .Attributes = c.Behavior
        }
        Return se
    End Function

    Public Function GetPropertyTypeByName(ByVal type As Type, ByVal propertyAlias As String) As Type
        Return GetEntitySchema(type).GetPropertyTypeByName(type, propertyAlias)
    End Function

    'Public Function GetPropertyAliasByType(ByVal type As Type, ByVal propertyType As Type, _
    '    ByVal oschema As IEntitySchema) As List(Of String)
    '    If type Is Nothing Then
    '        Throw New ArgumentNullException("type")
    '    End If

    '    If propertyType Is Nothing Then
    '        Throw New ArgumentNullException("propertyType")
    '    End If

    '    'Dim key As String = type.ToString & propertyType.ToString
    '    'Dim l As List(Of String) = CType(_joins(key), List(Of String))
    '    'If l Is Nothing Then
    '    '    Using SyncHelper.AcquireDynamicLock(key)
    '    '        l = CType(_joins(key), List(Of String))
    '    '        If l Is Nothing Then
    '    '            l = New List(Of String)
    '    '            For Each de As DictionaryEntry In GetProperties(type, oschema)
    '    '                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
    '    '                If pi.PropertyType Is propertyType Then
    '    '                    Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
    '    '                    l.Add(c.PropertyAlias)
    '    '                End If
    '    '            Next
    '    '        End If
    '    '    End Using
    '    'End If
    '    'Return l
    '    Dim l As New List(Of String)
    '    For Each m As MapField2Column In oschema.GetFieldColumnMap
    '        If m.PropertyInfo.PropertyType Is propertyType Then
    '            l.Add(m.PropertyAlias)
    '        End If
    '    Next
    '    Return l
    'End Function

    'Protected Function GetColumnNameByFieldName4Filter(ByVal type As Type, ByVal field As String, Optional ByVal table_alias As Boolean = True, Optional ByVal pref As String = "") As String
    '    Return GetColumnNameByFieldNameInternal(type, field, table_alias)
    'End Function

    'Protected Function GetColumnNameByFieldName4Sort(ByVal type As Type, ByVal field As String, Optional ByVal table_alias As Boolean = True) As String
    '    Return GetColumnNameByFieldNameInternal(type, field, table_alias)
    'End Function

    'Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal propertyAlias As String) As String
    '    If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

    '    Dim schema As IEntitySchema = GetEntitySchema(type)

    '    Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

    '    Dim p As MapField2Column = Nothing
    '    If coll.TryGetValue(propertyAlias, p) Then
    '        Dim c As String = Nothing
    '        c = p.ColumnExpression
    '        Return c
    '    End If

    '    Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    'End Function

    'Protected Function GetColumnsFromPropertyAlias(ByVal main As Type, ByVal propertyType As Type) As EntityPropertyAttribute()
    '    If main Is Nothing Then Throw New ArgumentNullException("main")

    '    Dim l As New List(Of EntityPropertyAttribute)

    '    For Each de As DictionaryEntry In GetProperties(main)
    '        Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
    '        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
    '        If pi.PropertyType Is propertyType Then
    '            l.Add(c)
    '        End If
    '    Next
    '    Return l.ToArray
    'End Function

    'Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String) As EntityPropertyAttribute
    '    If main Is Nothing Then Throw New ArgumentNullException("main")

    '    'Dim l As New List(Of EntityPropertyAttribute)
    '    Return GetColumnByPropertyAlias(main, propertyAlias, GetEntitySchema(main))
    'End Function

    'Public Function GetColumnByPropertyAlias(ByVal main As Type, ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As EntityPropertyAttribute
    '    If main Is Nothing Then Throw New ArgumentNullException("main")

    '    For Each de As DictionaryEntry In GetProperties(main, oschema)
    '        Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
    '        If c.PropertyAlias = propertyAlias Then
    '            Return c
    '        End If
    '    Next
    '    Return Nothing
    'End Function

    'Public Shared Function GetColumnByMappedPropertyAlias(ByVal main As Type, ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As EntityPropertyAttribute
    '    If main Is Nothing Then Throw New ArgumentNullException("main")

    '    For Each de As DictionaryEntry In GetMappedProperties(main, oschema)
    '        Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
    '        If c.PropertyAlias = propertyAlias Then
    '            Return c
    '        End If
    '    Next
    '    Return Nothing
    'End Function
#End Region

    Public Shared Function GetTable(ByVal t As Type, ByVal mpeVersion As String) As SourceFragment
        Dim tbl As SourceFragment = Nothing
        Dim entities() As EntityAttribute = CType(t.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())

        For Each ea As EntityAttribute In entities
            If ea.Version = mpeVersion AndAlso Not String.IsNullOrEmpty(ea.TableName) Then
                tbl = New SourceFragment(ea.TableSchema, ea.TableName)
                Exit For
            End If
        Next

        Dim entities2() As EntityAttribute = Nothing
        If tbl Is Nothing Then
            entities2 = CType(t.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())
            For Each ea As EntityAttribute In entities2
                If ea.Version = mpeVersion AndAlso Not String.IsNullOrEmpty(ea.TableName) Then
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
    Public Shared Iterator Function GetTypes2Scan() As IEnumerable(Of Type)
        For Each assembly As Reflection.Assembly In AppDomain.CurrentDomain.GetAssemblies
            If IsBadAssembly(assembly) Then
                Continue For
            End If

            Dim types() As Type = Nothing
            Try
                types = assembly.GetTypes
            Catch ex As Reflection.ReflectionTypeLoadException
                Debug.WriteLine("Worm error during loading types: " & ex.ToString)
            End Try

            If types Is Nothing Then Continue For

            Dim t As Type = GetType(_IEntity)

            For Each tp As Type In types
                If t.IsAssignableFrom(tp) Then
#If nlog Then
                    NLog.LogManager.GetCurrentClassLogger?.Trace("Scanned type {1}. Hash: {0}. Assembly: {2}. Hash: {3}. Path: {4}", tp.GetHashCode, tp, assembly, assembly.GetHashCode, assembly.Location)
#End If
                    Yield tp
                End If
            Next
        Next
    End Function
    Public Shared Function GetEntityTypes() As IEnumerable(Of Type)
        If _entityTypes Is Nothing Then
            Using New CSScopeMgrLite(_entityTypesSpin)
                If _entityTypes Is Nothing Then
                    Dim cache = System.Configuration.ConfigurationManager.AppSettings("worm:types-cache")
                    Dim cachePath = cache
                    If Not String.IsNullOrEmpty(cache) Then
                        If Not IO.Path.IsPathRooted(cachePath) Then
                            If Hosting.HostingEnvironment.IsHosted Then
                                cachePath = IO.Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, cachePath)
                            Else
                                cachePath = IO.Path.Combine(IO.Path.GetDirectoryName(New Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), cachePath)
                            End If
                        End If
                        If IO.File.Exists(cachePath) Then
                            _entityTypes = LoadFromCache(cachePath).ToArray
                        End If
                    End If

                    If _entityTypes Is Nothing OrElse Not _entityTypes.Any Then
                        Dim l As New List(Of Type)()
                        For Each t In GetTypes2Scan()

                            Dim exist = l.FirstOrDefault(Function(it) it.ToString = t.ToString)
                            If exist IsNot Nothing Then
                                If String.IsNullOrEmpty(exist.Assembly.Location) Then
                                    l.Remove(exist)
                                Else
                                    Continue For
                                End If
                            End If

                            l.Add(t)
                        Next

                        Dim arr As New System.Collections.Concurrent.ConcurrentBag(Of Type)()
                        For Each t In l
                            arr.Add(t)
                        Next

                        _entityTypes = arr

                        If Not String.IsNullOrEmpty(cache) Then
                            SaveToCache(cachePath)
                        End If
                    End If
                End If
            End Using
        End If

        Return _entityTypes
    End Function
    Public Shared Sub StartLoadEntityTypes()
        Threading.Tasks.Task.Factory.StartNew(AddressOf GetEntityTypes)
    End Sub
    Protected Function CreateObjectSchema(ByRef names As IDictionary) As IDictionary
        RaiseEvent StartInitSchema()
        Dim idic As New Specialized.HybridDictionary
        names = New Specialized.HybridDictionary
        For Each tp As Type In GetEntityTypes()
            If Not idic.Contains(tp) Then
                GetEntitySchema(tp, Me, idic, names)
            End If
        Next
        RaiseEvent EndInitSchema(idic)
        Return idic
    End Function

    Public Function AddEntitySchema(ByVal tp As Type, ByVal schema As IEntitySchema) As Boolean
        Dim idic As IDictionary = GetIdic()

        SyncLock idic.SyncRoot
            If Not idic.Contains(tp) Then
                '#If DEBUG Then
                '                For Each kv As DictionaryEntry In idic
                '                    Dim t = kv.Key.ToString
                '                    If t = tp.ToString Then
                '                        NLog.LogManager.GetCurrentClassLogger?.Trace("Duplicate type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, t, kv.Key.GetHashCode)
                '                        Exit For
                '                    End If
                '                Next
                '#End If
                idic.Add(tp, schema)
#If nlog Then
                NLog.LogManager.GetCurrentClassLogger?.Trace("Added es type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, tp, tp.GetHashCode)
#End If
                Return True
            End If
        End SyncLock

        Return False
    End Function

    Public Overridable Function HasEntitySchema(ByVal tp As Type) As Boolean
        Return GetIdic.Contains(tp)
    End Function

    Public Function CreateAndInitSchemaAndNames(ByVal tp As Type, ByVal ea As EntityAttribute) As IEntitySchema
        Return ObjectMappingEngine.CreateAndInitSchemaAndNames(tp, Me, GetIdic, GetNames, ea)
    End Function

    Private Shared Function CreateAndInitSchema(ByVal tp As Type, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary,
                                                ByVal names As IDictionary, ByVal ea As EntityAttribute) As IEntitySchema

        Dim schema As IEntitySchema = Nothing
        Dim mpeVersion As String = Nothing
        Dim c2int As ObjectMappingEngine.ConvertVersionToIntDelegate = Nothing
        If mpe IsNot Nothing Then
            mpeVersion = mpe.Version
            c2int = mpe.ConvertVersionToInt
        End If


        If ea.Type Is Nothing Then
            If tp.BaseType IsNot Nothing AndAlso tp.BaseType IsNot GetType(Object) _
                AndAlso ea.InheritBaseTable AndAlso tp.BaseType IsNot GetType(SinglePKEntity) AndAlso tp.BaseType IsNot GetType(SinglePKEntityBase) AndAlso tp.BaseType IsNot GetType(CachedEntity) AndAlso tp.BaseType IsNot GetType(CachedLazyLoad) Then
                Dim ownTable As SourceFragment = ea._tbl
                If ownTable Is Nothing Then
                    ownTable = New SourceFragment(ea.TableSchema, ea.TableName)
                End If

                Dim bsch As IEntitySchema = Nothing
                If idic IsNot Nothing Then
                    bsch = CType(idic(tp.BaseType), IEntitySchema)
                End If
                If bsch Is Nothing Then
                    bsch = GetEntitySchema(tp.BaseType, mpe, idic, names)
                End If
                If bsch IsNot Nothing Then
                    schema = New SimpleMultiTableObjectSchema(tp, ownTable,
                                                              GetMappedProperties(tp, mpeVersion, ea.RawProperties, True, c2int, mpe?._features), bsch, mpeVersion, mpe, idic, names)
                    Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                    If n IsNot Nothing Then
                        n.InitSchema(mpe, tp)
                    End If
                Else
                    schema = New SimpleObjectSchema(ownTable)
                    Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                    If n IsNot Nothing Then
                        n.InitSchema(mpe, tp)
                    End If
                    ApplyAttributes2Schema(schema, GetMappedProperties(tp, mpeVersion, ea.RawProperties, True, c2int, mpe?._features), mpe, idic, names)
                End If
            Else
                Dim tbl As SourceFragment = ea._tbl
                If tbl Is Nothing Then
                    tbl = New SourceFragment(ea.TableSchema, ea.TableName)
                End If

                schema = New SimpleObjectSchema(tbl)
                Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                If n IsNot Nothing Then
                    n.InitSchema(mpe, tp)
                End If
                Dim l As List(Of EntityPropertyAttribute) = GetMappedProperties(tp, mpeVersion, ea.RawProperties, True, c2int, mpe?._features)
                ApplyAttributes2Schema(schema, l, mpe, idic, names)
            End If
        Else
            Try
                schema = CType(ea.Type.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IEntitySchema)
                Dim n As ISchemaInit = TryCast(schema, ISchemaInit)
                If n IsNot Nothing Then
                    n.InitSchema(mpe, tp)
                End If
                Dim l As List(Of EntityPropertyAttribute) = GetMappedProperties(tp, mpeVersion, ea.RawProperties, True, c2int, mpe?._features)
                If Not ea.RawProperties AndAlso l.Count < schema.FieldColumnMap.Count Then
                    l = GetMappedProperties(tp, mpeVersion, True, True, c2int, mpe?._features)
                End If
                ApplyAttributes2Schema(schema, l, mpe, idic, names)

                If GetType(ISinglePKEntity).IsAssignableFrom(tp) Then
                    Dim pk = schema.GetPK
                    If pk.PropertyInfo Is Nothing Then
                        pk.PropertyInfo = tp.GetProperty("Identifier")
                    End If
                End If
            Catch ex As Exception
                Throw New ObjectMappingException(String.Format("Cannot create type [{0}]", ea.Type.ToString), ex)
            End Try
        End If

        Return schema
    End Function

    Public Shared Function CreateAndInitSchemaAndNames(ByVal tp As Type, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary,
                                                       ByVal names As IDictionary, ByVal ea As EntityAttribute) As IEntitySchema

        Dim schema As IEntitySchema = CreateAndInitSchema(tp, mpe, idic, names, ea)

        If Not String.IsNullOrEmpty(ea.EntityName) AndAlso names IsNot Nothing Then
            If names.Contains(ea.EntityName) Then
                Dim tt As Pair(Of Type, EntityAttribute) = CType(names(ea.EntityName), Pair(Of Type, EntityAttribute))
                If tt.First.IsAssignableFrom(tp) Then
                    If mpe IsNot Nothing Then
                        Dim existingNotMatch As Boolean = tt.Second.Version <> mpe._version
                        Dim newNotMatch As Boolean = ea.Version <> mpe._version
                        If existingNotMatch OrElse Not newNotMatch Then
                            names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                        End If
                    Else
                        names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                    End If
                ElseIf (mpe IsNot Nothing AndAlso tt.Second.Version <> mpe._version) Then
                    names(ea.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ea)
                End If
            Else
                names.Add(ea.EntityName, New Pair(Of Type, EntityAttribute)(tp, ea))
            End If
        End If

        Try
            If idic IsNot Nothing Then
                If idic.Contains(tp) Then
                    Dim addedSchema As IEntitySchema = CType(idic(tp), IEntitySchema)
                    If schema.GetType.IsAssignableFrom(addedSchema.GetType) Then
                        Return schema
                    ElseIf addedSchema.GetType.IsAssignableFrom(schema.GetType) Then
                        idic(tp) = schema
                    End If
                End If

                '#If DEBUG Then
                '                For Each kv As DictionaryEntry In idic
                '                    Dim t = kv.Key.ToString
                '                    If t = tp.ToString Then
                '                        NLog.LogManager.GetCurrentClassLogger?.Trace("Duplicate type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, t, kv.Key.GetHashCode)
                '                        Exit For
                '                    End If
                '                Next
                '#End If

                idic.Add(tp, schema)
#If nlog Then
                NLog.LogManager.GetCurrentClassLogger?.Trace("Added es type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, tp, tp.GetHashCode)
#End If
            End If
        Catch ex As ArgumentException
            Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ea.Type), ex)
        End Try

        Return schema
    End Function

    Private Shared Function CreateAndInitSchemaAndNamesWithMapping(ByVal tp As Type, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary,
                                                                   ByVal names As IDictionary, ByVal ownEntityAttr As EntityAttribute) As IEntitySchema

        Dim schema As IEntitySchema = CreateAndInitSchema(tp, mpe, idic, names, ownEntityAttr)

        If names IsNot Nothing AndAlso Not String.IsNullOrEmpty(ownEntityAttr.EntityName) Then
            If names.Contains(ownEntityAttr.EntityName) Then
                Dim currentType As Pair(Of Type, EntityAttribute) = CType(names(ownEntityAttr.EntityName), Pair(Of Type, EntityAttribute))
                Dim e As EntityAttribute = currentType.Second
                If currentType.Second.Version = mpe._version Then
                    If ownEntityAttr.Version = mpe._version Then
                        If currentType.First.IsAssignableFrom(tp) Then
                            e = ownEntityAttr
                        ElseIf mpe._mapn IsNot Nothing Then
                            e = mpe._mapn(mpe._version, currentType.First, currentType.Second, tp, ownEntityAttr)
                        End If
                    End If
                Else
                    If ownEntityAttr.Version = mpe._version OrElse currentType.First.IsAssignableFrom(tp) Then
                        e = ownEntityAttr
                    ElseIf mpe._mapn IsNot Nothing Then
                        e = mpe._mapn(mpe._version, currentType.First, currentType.Second, tp, ownEntityAttr)
                    End If
                End If

                If e IsNot currentType.Second Then
                    names(ownEntityAttr.EntityName) = New Pair(Of Type, EntityAttribute)(tp, ownEntityAttr)
                End If
            Else
                names.Add(ownEntityAttr.EntityName, New Pair(Of Type, EntityAttribute)(tp, ownEntityAttr))
            End If
        End If

        Try
            If idic IsNot Nothing Then

                '#If DEBUG Then
                '                For Each kv As DictionaryEntry In idic
                '                    Dim t = kv.Key.ToString
                '                    If t = tp.ToString Then
                '                        NLog.LogManager.GetCurrentClassLogger?.Trace("Duplicate type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, t, kv.Key.GetHashCode)
                '                        Exit For
                '                    End If
                '                Next
                '#End If
                idic.Add(tp, schema)
#If nlog Then
                NLog.LogManager.GetCurrentClassLogger?.Trace("Added es type {1}. Hash: {2}. Stack: {0}", Environment.StackTrace, tp, tp.GetHashCode)
#End If
            End If

        Catch ex As ArgumentException
            Throw New ObjectMappingException(String.Format("Invalid Entity attribute({0}). Multiple Entity attributes must have different versions.", ownEntityAttr.Type), ex)
        End Try

        Return schema
    End Function

    Public Shared Function GetEntitySchema(ByVal tp As Type, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary, ByVal names As IDictionary) As IEntitySchema
        Dim schema As IEntitySchema = Nothing

        If tp.IsClass Then
            Dim ownEntityAttrs() As EntityAttribute = CType(tp.GetCustomAttributes(GetType(EntityAttribute), False), EntityAttribute())

            For Each ea As EntityAttribute In ownEntityAttrs
                If mpe Is Nothing OrElse ea.Version = mpe._version Then
                    schema = CreateAndInitSchemaAndNames(tp, mpe, idic, names, ea)
                End If
            Next

            'If schema Is Nothing AndAlso mpe IsNot Nothing AndAlso mpe._mapv IsNot Nothing AndAlso ownEntityAttrs.Length > 0 Then
            '    Dim ownEntityAttr As EntityAttribute = mpe._mapv(mpe._version, ownEntityAttrs, tp)
            '    If ownEntityAttr IsNot Nothing Then
            '        schema = CreateAndInitSchemaAndNames(tp, mpe, idic, names, ownEntityAttr)
            '    End If
            'End If
            Dim baseEntityAttrs() As EntityAttribute = Nothing
            If schema Is Nothing AndAlso ownEntityAttrs.Length = 0 Then
                baseEntityAttrs = CType(tp.GetCustomAttributes(GetType(EntityAttribute), True), EntityAttribute())

                For Each ea As EntityAttribute In baseEntityAttrs
                    If mpe Is Nothing OrElse ea.Version = mpe._version Then
                        schema = CreateAndInitSchemaAndNames(tp, mpe, idic, names, ea)
                    End If
                Next
            End If

            If schema Is Nothing Then
                'For Each ea As EntityAttribute In entities
                Dim ownEntityAttr As EntityAttribute = Nothing
                If ownEntityAttrs.Length > 0 Then
                    If mpe IsNot Nothing AndAlso mpe._mapv IsNot Nothing Then
                        ownEntityAttr = mpe._mapv(mpe._version, ownEntityAttrs, tp)
                    End If
                End If

                If ownEntityAttr IsNot Nothing Then
                    schema = CreateAndInitSchemaAndNamesWithMapping(tp, mpe, idic, names, ownEntityAttr)
                End If
                'Next

                If schema Is Nothing Then
                    Dim baseEntityAttr As EntityAttribute = Nothing
                    If baseEntityAttrs IsNot Nothing AndAlso baseEntityAttrs.Length > 0 Then
                        If mpe IsNot Nothing AndAlso mpe._mapv IsNot Nothing Then
                            baseEntityAttr = mpe._mapv(mpe._version, baseEntityAttrs, tp)
                        End If
                    End If

                    If baseEntityAttr IsNot Nothing Then
                        schema = CreateAndInitSchemaAndNamesWithMapping(tp, mpe, idic, names, baseEntityAttr)
                    End If
                End If
            End If

            'If schema Is Nothing AndAlso ownEntityAttrs.Length = 1 Then
            '    schema = CreateAndInitSchemaAndNamesWithMapping(tp, mpe, idic, names, ownEntityAttrs(0))
            'End If

            'If schema Is Nothing AndAlso baseEntityAttrs IsNot Nothing AndAlso baseEntityAttrs.Length = 1 Then
            '    schema = CreateAndInitSchemaAndNamesWithMapping(tp, mpe, idic, names, baseEntityAttrs(0))
            'End If
        End If
        Return schema
    End Function

#Region " Public members "

    Public Function GetTypeByEntityName(ByVal name As String) As Type
        If name Is Nothing Then Throw New ArgumentNullException(NameOf(name))
        Dim p As Pair(Of Type, EntityAttribute) = CType(GetNames(name), Pair(Of Type, EntityAttribute))
        If p Is Nothing Then
            Return Nothing
        Else
            Return p.First
        End If
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="t"></param>
    ''' <returns></returns>
    ''' <exception cref="ArgumentNullException">Если t пустая ссылка</exception>
    Public Function GetEntitySchema(ByVal t As Type) As IEntitySchema
        Return GetEntitySchema(t, True)
    End Function

    Public Function GetEntitySchema(ByVal eu As EntityUnion) As IEntitySchema
        Return GetEntitySchema(eu.GetRealType(Me), True)
    End Function

    Public Function GetEntitySchema(ByVal entityName As String) As IEntitySchema
        Return GetEntitySchema(GetTypeByEntityName(entityName), True)
    End Function
    Public Sub Init()
        GetIdic()
    End Sub
    Private Function GetIdic() As IDictionary
        If _idic Is Nothing Then
            Using New CSScopeMgrLite(_idicSpin)
                If _idic Is Nothing Then
                    _idic = CreateObjectSchema(_names)
                End If
            End Using
        End If
        Return _idic
    End Function
    Public Function InitType(tp As Type) As IEntitySchema
        Using New CSScopeMgrLite(_idicSpin)
            If _idic Is Nothing Then
                _idic = CreateObjectSchema(_names)
                Return GetEntitySchema(tp, False)
            Else
                Return GetEntitySchema(tp, Me, _idic, _names)
            End If
        End Using

    End Function

    Private Function GetNames() As IDictionary
        If _names Is Nothing Then
            Using New CSScopeMgrLite(_idicSpin)
                If _names Is Nothing Then
                    _idic = CreateObjectSchema(_names)
                End If
            End Using
        End If
        Return _names
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="t"></param>
    ''' <param name="throwNotFound"></param>
    ''' <returns></returns>
    ''' <exception cref="ArgumentNullException">Если t пустая ссылка</exception>
    Public Overridable Function GetEntitySchema(ByVal t As Type, ByVal throwNotFound As Boolean) As IEntitySchema
        If t Is Nothing Then
            If throwNotFound Then
                Throw New ArgumentNullException(NameOf(t))
            Else
                Return Nothing
            End If
        End If

        Dim idic As IDictionary = GetIdic()
        Dim schema As IEntitySchema = CType(idic(t), IEntitySchema)

        If schema Is Nothing Then
            schema = CType(idic(NormalType(t)), IEntitySchema)
        End If

        If schema Is Nothing Then
            schema = InitType(t)
            If schema Is Nothing AndAlso throwNotFound Then
                Throw New ArgumentException(String.Format("Cannot find schema for type {0}", t))
            End If
        End If

        Return schema
    End Function

    Public ReadOnly Property Version() As String
        Get
            Return _version
        End Get
    End Property

    Public Property CreateEntityDel() As CreateEntityDelegate
        Get
            Return _ce
        End Get
        Set(ByVal value As CreateEntityDelegate)
            _ce = value
        End Set
    End Property

    Public Property ConvertVersionToInt() As ConvertVersionToIntDelegate
        Get
            Return _c2int
        End Get
        Set(ByVal value As ConvertVersionToIntDelegate)
            _c2int = value
        End Set
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

    <Obsolete("Use GetSharedSourceFragment method")>
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

        Return GetEntitySchema(type).GetTables()
    End Function
    Public Function ApplyFilter(Of T As SinglePKEntity)(ByVal col As ICollection(Of T), ByVal filter As Criteria.Core.IFilter, ByRef r As Boolean) As ICollection(Of T)
        Return ApplyFilter(Of T)(col, filter, Nothing, Nothing, r)
    End Function

    Public Function ApplyFilter(Of T As SinglePKEntity)(ByVal col As ICollection(Of T), ByVal filter As Criteria.Core.IFilter,
                              joins() As Joins.QueryJoin, objEU As EntityUnion, ByRef r As Boolean) As ICollection(Of T)
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
                Dim er As Criteria.Values.IEvaluableValue.EvalResult = f.EvalObj(Me, o, oschema, joins, objEU)
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

    'Private Shared Sub FormatType(ByVal t As Type, ByVal stmt As StmtGenerator, ByVal fld As String, _
    '                              ByVal schema As ObjectMappingEngine, ByVal aliases As IPrepareTable, _
    '                              ByVal values As List(Of String), ByVal os As EntityUnion)
    '    If Not GetType(IEntity).IsAssignableFrom(t) Then
    '        Throw New NotSupportedException(String.Format("Type {0} is not assignable from IEntity", t))
    '    End If

    '    Dim d As String = schema.Delimiter
    '    If stmt IsNot Nothing Then
    '        d = stmt.Selector
    '    End If

    '    Dim oschema As IEntitySchema = schema.GetEntitySchema(t)
    '    Dim tbl As SourceFragment = Nothing
    '    Dim map As MapField2Column = Nothing
    '    'Dim fld As String = p.Second
    '    If oschema.GetFieldColumnMap.TryGetValue(fld, map) Then
    '        fld = map.ColumnExpression
    '        tbl = map.Table
    '    Else
    '        tbl = oschema.Table
    '    End If

    '    If aliases IsNot Nothing Then
    '        Debug.Assert(aliases.ContainsKey(tbl, os), "There is not alias for table " & tbl.RawName)
    '        Try
    '            values.Add(aliases.GetAlias(tbl, os) & d & fld)
    '        Catch ex As KeyNotFoundException
    '            Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
    '        End Try
    '    Else
    '        values.Add(tbl.UniqueName(os) & d & fld)
    '    End If
    'End Sub

    Public ReadOnly Property Delimiter() As String
        Get
            Return "_"
        End Get
    End Property

#Region " Gen "
    'Public Function GetColumnNameByPropertyAlias(ByVal schema As IEntitySchema, ByVal propertyAlias As String, _
    '    ByVal add_alias As Boolean, ByVal os As EntityUnion) As String

    '    If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

    '    Dim coll As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

    '    Dim p As MapField2Column = Nothing
    '    If coll.TryGetValue(propertyAlias, p) Then
    '        Dim c As String = Nothing
    '        If add_alias AndAlso ShouldPrefix(p.ColumnExpression) Then
    '            c = p.Table.UniqueName(os) & Delimiter & p.ColumnExpression
    '        Else
    '            c = p.ColumnExpression
    '        End If
    '        'If columnAliases IsNot Nothing Then
    '        '    columnAliases.Add(p._columnName)
    '        'End If
    '        Return c
    '    End If

    '    Throw New ObjectMappingException("Cannot find property: " & propertyAlias)
    'End Function

    'Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, _
    '    ByVal add_alias As Boolean, ByVal os As EntityUnion) As String

    '    If String.IsNullOrEmpty(propertyAlias) Then Throw New ArgumentNullException("propertyAlias")

    '    Dim schema As IEntitySchema = mpe.GetEntitySchema(type)

    '    Return GetColumnNameByPropertyAlias(schema, propertyAlias, add_alias, os)
    'End Function

    'Public Function GetPrimaryKeysName(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, ByVal add_alias As Boolean, _
    '    ByVal schema As IEntitySchema, ByVal os As EntityUnion) As List(Of String)

    '    If original_type Is Nothing Then
    '        Throw New ArgumentNullException("original_type")
    '    End If

    '    'Dim cl_type As String = "pklist" & original_type.ToString & add_alias

    '    'Dim arr As Generic.List(Of String) = CType(map(cl_type), Generic.List(Of String))

    '    'If arr Is Nothing Then
    '    '    Using SyncHelper.AcquireDynamicLock(cl_type)
    '    '        arr = CType(map(cl_type), Generic.List(Of String))
    '    '        If arr Is Nothing Then
    '    '            arr = New Generic.List(Of String)
    '    '            If schema Is Nothing Then
    '    '                schema = mpe.GetEntitySchema(original_type)
    '    '            End If

    '    '            For Each c As EntityPropertyAttribute In mpe.GetSortedFieldList(original_type, schema)
    '    '                If (mpe.GetAttributes(schema, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
    '    '                    arr.Add(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, add_alias, os))
    '    '                End If
    '    '            Next

    '    '            map.Add(cl_type, arr)
    '    '        End If
    '    '    End Using
    '    'End If

    '    'Return arr.ToArray

    '    Dim l As New List(Of String)
    '    For Each m As MapField2Column In schema.GetFieldColumnMap
    '        If m.IsPK Then
    '            l.Add(GetColumnNameByPropertyAlias(schema, m.PropertyAlias, add_alias, os))
    '        End If
    '    Next
    '    Return l
    'End Function

    'Protected Friend Sub GetPKList(ByVal type As Type, ByVal mpe As ObjectMappingEngine, _
    '    ByVal schema As IEntitySchema, ByVal ids As StringBuilder, ByVal os As EntityUnion)
    '    If ids Is Nothing Then
    '        Throw New ArgumentNullException("ids")
    '    End If

    '    For Each pk As String In GetPrimaryKeysName(type, mpe, True, schema, os)
    '        ids.Append(pk).Append(",")
    '    Next
    '    ids.Length -= 1

    '    'Dim p As MapField2Column = schema.GetFieldColumnMap("ID")
    '    'ids.Append(GetTableName(p._tableName)).Append(Selector).Append(p._columnName)
    '    'If columnAliases IsNot Nothing Then
    '    '    columnAliases.Add(p._columnName)
    '    'End If
    'End Sub

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

    'Protected Friend Function GetSelectColumnList(ByVal original_type As Type, ByVal mpe As ObjectMappingEngine, _
    '    ByVal arr As Generic.ICollection(Of EntityPropertyAttribute), ByVal schema As IEntitySchema, ByVal os As EntityUnion) As String
    '    'Dim add_c As Boolean = False
    '    'If arr Is Nothing Then
    '    '    Dim s As String = CStr(sel(original_type))
    '    '    If Not String.IsNullOrEmpty(s) Then
    '    '        Return s
    '    '    End If
    '    '    add_c = True
    '    'End If
    '    Dim sb As New StringBuilder
    '    If arr Is Nothing Then arr = mpe.GetSortedFieldList(original_type, schema)
    '    For Each c As EntityPropertyAttribute In arr
    '        sb.Append(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, True, os)).Append(", ")
    '    Next

    '    If sb.Length = 0 Then
    '        For Each m As MapField2Column In schema.GetFieldColumnMap
    '            sb.Append(m.ColumnExpression).Append(", ")
    '        Next
    '    End If

    '    sb.Length -= 2

    '    'If add_c Then
    '    '    sel(original_type) = sb.ToString
    '    'End If
    '    Return sb.ToString
    'End Function

    'Protected Friend Function GetSelectColumnListWithoutPK(ByVal original_type As Type, _
    '    ByVal mpe As ObjectMappingEngine, ByVal arr As Generic.ICollection(Of EntityPropertyAttribute), _
    '    ByVal schema As IEntitySchema, ByVal os As EntityUnion) As String
    '    'Dim add_c As Boolean = False
    '    'If arr Is Nothing Then
    '    '    Dim s As String = CStr(sel(original_type))
    '    '    If Not String.IsNullOrEmpty(s) Then
    '    '        Return s
    '    '    End If
    '    '    add_c = True
    '    'End If
    '    Dim sb As New StringBuilder
    '    If arr Is Nothing Then arr = mpe.GetSortedFieldList(original_type, schema)
    '    For Each c As EntityPropertyAttribute In arr
    '        If (c.Behavior And Field2DbRelations.PK) <> Field2DbRelations.PK Then
    '            sb.Append(GetColumnNameByPropertyAlias(schema, c.PropertyAlias, True, os)).Append(", ")
    '        End If
    '    Next

    '    If sb.Length > 0 Then
    '        sb.Length -= 2
    '    End If
    '    'If add_c Then
    '    '    sel(original_type) = sb.ToString
    '    'End If
    '    Return sb.ToString
    'End Function

    'Public Function GetColumnNameByPropertyAlias(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal os As EntityUnion) As String
    '    Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, True, os)
    'End Function

    'Public Function GetColumnNameByPropertyAlias(ByVal os As IEntitySchema, ByVal propertyAlias As String, ByVal osrc As EntityUnion) As String
    '    Return GetColumnNameByPropertyAlias(os, propertyAlias, True, osrc)
    'End Function

    'Protected Function GetColumns4Select(ByVal type As Type, ByVal mpe As ObjectMappingEngine, ByVal propertyAlias As String, ByVal os As EntityUnion) As String
    '    Return GetColumnNameByPropertyAlias(type, mpe, propertyAlias, os)
    'End Function
#End Region

    Public Enum JoinFieldType
        Direct
        Reverse
        M2M
    End Enum

    Public Sub AppendJoin(ByVal selectOS As EntityUnion, ByVal selectType As Type, ByVal selSchema As IEntitySchema,
        ByVal joinOS As EntityUnion, ByVal type2join As Type, ByVal sh As IEntitySchema,
        ByRef filter As IFilter, ByVal l As List(Of QueryJoin),
        ByVal filterInfo As IDictionary, ByVal jt As JoinType)

        Dim jft As JoinFieldType

        Dim field As String = selSchema.GetJoinFieldNameByType(type2join)

        If String.IsNullOrEmpty(field) Then

            field = sh.GetJoinFieldNameByType(selectType)

            If String.IsNullOrEmpty(field) Then
                Dim m2m As M2MRelationDesc = GetM2MRelation(type2join, selectType, True)
                If m2m IsNot Nothing Then
                    jft = JoinFieldType.M2M
                Else
                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Specify joins explicit", selectType, type2join))
                End If
            Else
                jft = JoinFieldType.Reverse
            End If
        Else
            jft = JoinFieldType.Direct
        End If

        AppendJoin(selectOS, selectType, selSchema, joinOS, type2join, sh, filter, l, jt, filterInfo, jft, field)
    End Sub

    Public Sub AppendJoin(ByVal selectOS As EntityUnion, ByVal selectType As Type, ByVal selSchema As IEntitySchema,
        ByVal joinOS As EntityUnion, ByVal type2join As Type, ByVal sh As IEntitySchema,
        ByRef filter As IFilter, ByVal l As List(Of QueryJoin), ByVal jt As JoinType,
        ByVal filterInfo As IDictionary, ByVal jft As JoinFieldType, ByVal propertyAlias As String)

        Select Case jft
            Case JoinFieldType.Direct
                'Dim pks As List(Of EntityPropertyAttribute) = GetPrimaryKeys(type2join, sh)
                'If pks.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Type {0} has {1} primary key column instead of one", type2join, pks.Count))
                'End If
                l.Add(MakeJoin(joinOS, GetPrimaryKey(type2join, sh), selectOS, propertyAlias, FilterOperation.Equal, jt, False))
            Case JoinFieldType.Reverse
                'Dim pks As List(Of EntityPropertyAttribute) = GetPrimaryKeys(selectType, selSchema)
                'If pks.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Type {0} has {1} primary key columns instead of one", selectType, pks.Count))
                'End If
                l.Add(MakeJoin(selectOS, GetPrimaryKey(selectType, selSchema), joinOS, propertyAlias, FilterOperation.Equal, jt, True))
            Case JoinFieldType.M2M
                l.AddRange(JCtor.join(joinOS).onM2M(selectOS).ToList)
            Case Else
                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
        End Select

        'Dim ts As IMultiTableObjectSchema = TryCast(sh, IMultiTableObjectSchema)
        'If ts IsNot Nothing Then
        '    Dim pk_table As SourceFragment = sh.Table
        '    For i As Integer = 1 To ts.GetTables.Length - 1
        '        Dim joinableTs As IGetJoinsWithContext = TryCast(ts, IGetJoinsWithContext)
        '        Dim join As QueryJoin = Nothing
        '        If joinableTs IsNot Nothing Then
        '            join = joinableTs.GetJoins(pk_table, ts.GetTables(i), filterInfo)
        '        Else
        '            join = ts.GetJoins(pk_table, ts.GetTables(i))
        '        End If

        '        If Not QueryJoin.IsEmpty(join) Then
        '            l.Add(join)
        '        End If
        '    Next
        'End If

        Dim cfs As IContextObjectSchema = TryCast(sh, IContextObjectSchema)
        If cfs IsNot Nothing Then
            Dim newfl As IFilter = cfs.GetContextFilter(filterInfo)
            If newfl IsNot Nothing Then
                Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                con.AddFilter(filter)
                con.AddFilter(newfl.SetUnion(joinOS))
                filter = con.Condition
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

    Public Function MakeJoin(ByVal joinOS As EntityUnion, ByVal pk As String, ByVal selectOS As EntityUnion, ByVal field As String,
           ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Joins.JoinType, ByVal switchTable As Boolean) As Worm.Criteria.Joins.QueryJoin

        Dim schema As ObjectMappingEngine = Me

        Dim jf As New JoinFilter(joinOS, pk, selectOS, field, oper)

        Dim t As EntityUnion = joinOS
        If switchTable Then
            t = selectOS
        End If

        Return New QueryJoin(t, joinType, jf)
    End Function

    Public Delegate Sub ObjectLoadedDelegate(ByVal obj As IEntity)

    'Public Shared Function AssignValue2Property(ByVal propType As Type, ByVal MappingEngine As ObjectMappingEngine, ByVal cache As Cache.CacheBase, _
    '    ByVal value() As PKDesc, ByVal obj As Object, ByVal map As Collections.IndexedCollection(Of String, MapField2Column), _
    '    ByVal propertyAlias As String, ByVal objectLoaded As ObjectLoadedDelegate, ByVal contextInfo As Object) As Object

    '    Return AssignValue2Property(propType, MappingEngine, cache, value, obj, map, propertyAlias, Nothing, Nothing, Nothing, objectLoaded)
    'End Function

    '''' <summary>
    '''' Присваевает свойству объекта значение.
    '''' </summary>
    '''' <param name="propType"></param>
    '''' <param name="MappingEngine"></param>
    '''' <param name="cache"></param>
    '''' <param name="sv"></param>
    '''' <param name="obj"></param>
    '''' <param name="map"></param>
    '''' <param name="propertyAlias"></param>
    '''' <param name="ll"></param>
    '''' <param name="m"></param>
    '''' <param name="oschema"></param>
    '''' <param name="objectLoaded"></param>
    '''' <returns>Значение, присвоенное свойству объекта</returns>
    '''' <remarks>
    '''' Тип значения и тип свойства не обязательно должны совпадать. Например, тип свойства может быть <see cref="System.Xml.XmlDocument"/>, а тип значения <see cref="String"/>. 
    '''' Функция пытается выполнить преобразование и возвращает преобразованное значение.
    '''' Поддерживаются следующие преобразования
    '''' <list type="table">
    '''' <item>
    '''' <term><see cref="String"/></term>
    '''' <description><see cref="System.Xml.XmlDocument"/></description>
    '''' </item>
    '''' <item>
    '''' <term><see cref="String"/></term>
    '''' <description><see cref="System.Enum"/></description>
    '''' </item>
    '''' <item>
    '''' <term>Любой</term>
    '''' <description>Nullable(Of )</description>
    '''' </item>
    '''' <item>
    '''' <term><see cref="Decimal"/></term>
    '''' <description><see cref="Long"/></description>
    '''' </item>
    '''' <item>
    '''' <term><see cref="Date"/></term>
    '''' <description><see cref="Byte()"/></description>
    '''' </item>
    '''' </list>
    '''' </remarks>
    'Public Shared Function AssignValue2Property(ByVal propType As Type, ByVal MappingEngine As ObjectMappingEngine, ByVal cache As Cache.CacheBase,
    '                                            ByVal sv As PKDesc(), ByVal obj As Object, ByVal map As Collections.IndexedCollection(Of String, MapField2Column),
    '                                            ByVal propertyAlias As String, ByVal ll As IPropertyLazyLoad, ByVal m As MapField2Column, ByVal oschema As IEntitySchema,
    '                                            ByVal objectLoaded As ObjectLoadedDelegate, crMan As ICreateManager) As Object

    '    Dim pi As Reflection.PropertyInfo = m.PropertyInfo
    '    If sv Is Nothing Then
    '        ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, Nothing, oschema, pi)
    '        If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '    Else
    '        Dim value As Object = sv(0).Value

    '        If value.GetType Is propType Then
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema, pi)
    '            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '        ElseIf GetType(System.Xml.XmlDocument) Is propType AndAlso TypeOf (value) Is String Then
    '            Dim o As New System.Xml.XmlDocument
    '            o.LoadXml(CStr(value))
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, o, oschema, pi)
    '            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            Return o
    '        ElseIf propType.IsEnum AndAlso TypeOf (value) Is String Then
    '            Dim svalue As String = CStr(value).Trim
    '            If svalue = String.Empty Then
    '                value = 0
    '            Else
    '                value = [Enum].Parse(propType, svalue, True)
    '            End If
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema, pi)
    '            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            Return value
    '        ElseIf propType.IsGenericType AndAlso GetType(Nullable(Of )).Name = propType.Name Then
    '            Dim t As Type = propType.GetGenericArguments()(0)
    '            Dim v As Object = Nothing
    '            If t.IsPrimitive Then
    '                v = Convert.ChangeType(value, t)
    '            ElseIf t.IsEnum Then
    '                If TypeOf (value) Is String Then
    '                    Dim svalue As String = CStr(value).Trim
    '                    If svalue = String.Empty Then
    '                        v = [Enum].ToObject(t, 0)
    '                    Else
    '                        v = [Enum].Parse(t, svalue, True)
    '                    End If
    '                Else
    '                    v = [Enum].ToObject(t, value)
    '                End If
    '            ElseIf t Is value.GetType Then
    '                v = value
    '            Else
    '                Try
    '                    v = t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance,
    '                        Nothing, Nothing, New Object() {value})
    '                Catch ex As MissingMethodException
    '                    'Debug.WriteLine(c.FieldName & ": " & original_type.Name)
    '                    'v = Convert.ChangeType(value, t)
    '                End Try
    '            End If
    '            Dim v2 As Object = propType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance,
    '                Nothing, Nothing, New Object() {v})
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, v2, oschema, pi)
    '            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            Return v2
    '        ElseIf (propType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (propType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
    '            Try
    '                Dim v As Object = Convert.ChangeType(value, propType)
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, v, oschema, pi)
    '                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '                Return v
    '            Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
    '                Dim v As Object = Convert.ChangeType(value, propType)
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, v, oschema, pi)
    '                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '                Return v
    '            End Try
    '        ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
    '            Dim dt As DateTime = CDate(value)
    '            Dim l As Long = dt.ToBinary
    '            Using ms As New IO.MemoryStream
    '                Dim sw As New IO.StreamWriter(ms)
    '                sw.Write(l)
    '                sw.Flush()
    '                value = ms.ToArray
    '            End Using
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema, pi)
    '            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '        Else
    '            If GetType(_IEntity).IsAssignableFrom(propType) OrElse ObjectMappingEngine.IsEntityType(propType) Then
    '                Dim type_created As Type = propType
    '                Dim en As String = MappingEngine.GetEntityNameByType(type_created)
    '                If Not String.IsNullOrEmpty(en) Then
    '                    Dim cr As Type = MappingEngine.GetTypeByEntityName(en)
    '                    If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
    '                        type_created = cr
    '                    End If
    '                    If type_created Is Nothing Then
    '                        Throw New OrmManagerException("Cannot find type for entity " & en)
    '                    End If
    '                End If
    '                'If GetType(IKeyEntity).IsAssignableFrom(type_created) Then
    '                '    o = Entity.CreateKeyEntity(value, type_created, cache, MappingEngine)
    '                '    o.SetObjectState(ObjectState.NotLoaded)
    '                '    Dim cb As ICacheBehavior = CType(MappingEngine.GetEntitySchema(type_created), ICacheBehavior)
    '                '    o = CType(cache.FindObjectInCache(type_created, o, New CacheKey(CType(o, ICachedEntity)), cb, cache.GetOrmDictionary(type_created, cb), True, True), _IEntity)
    '                'Else
    '                '    Dim pks As IList(Of EntityPropertyAttribute) = MappingEngine.GetPrimaryKeys(type_created)
    '                '    If pks.Count <> 1 Then
    '                '        Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", type_created))
    '                '    End If
    '                '    If GetType(_ICachedEntity).IsAssignableFrom(type_created) Then
    '                '        o = Entity.CreateEntity(New PKDesc() {New PKDesc(pks(0).PropertyAlias, value)}, type_created, cache, MappingEngine)
    '                '        o.SetObjectState(ObjectState.NotLoaded)
    '                '        Dim cb As ICacheBehavior = CType(MappingEngine.GetEntitySchema(type_created), ICacheBehavior)
    '                '        o = CType(cache.FindObjectInCache(type_created, o, New CacheKey(CType(o, ICachedEntity)), cb, cache.GetOrmDictionary(type_created, cb), True, True), _IEntity)
    '                '    Else
    '                '        o = Entity.CreateEntity(type_created, cache, MappingEngine)
    '                '        MappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, value)
    '                '    End If
    '                'End If
    '                Dim o As Object = Entity.CreateObject(sv, type_created, cache, MappingEngine)
    '                Dim e As _IEntity = TryCast(o, _IEntity)
    '                Dim cce As ICachedEntity = TryCast(o, ICachedEntity)
    '                Dim pkw As PKWrapper = Nothing
    '                If e IsNot Nothing Then
    '                    e.SetObjectState(ObjectState.NotLoaded)
    '                End If
    '                If cce IsNot Nothing Then
    '                    pkw = New CacheKey(CType(o, ICachedEntity))
    '                    Dim cb As ICacheBehavior = TryCast(MappingEngine.GetEntitySchema(type_created), ICacheBehavior)
    '                    o = cache.FindObjectInCache(type_created, o, pkw, cb, cache.GetOrmDictionary(type_created, cb), True, True)
    '                    'Else
    '                    '    pkw = New PKWrapper(sv)
    '                End If
    '                e = TryCast(o, _IEntity)
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, o, oschema, pi)
    '                If e IsNot Nothing Then
    '                    Dim eo As IEntity = TryCast(obj, IEntity)
    '                    If eo IsNot Nothing AndAlso eo.GetICreateManager IsNot Nothing Then
    '                        e.SetCreateManager(eo.GetICreateManager)
    '                    ElseIf crMan IsNot Nothing Then
    '                        e.SetCreateManager(crMan)
    '                    End If
    '                    If objectLoaded IsNot Nothing Then objectLoaded(e)
    '                End If
    '                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '                Return o
    '                'ElseIf ObjectMappingEngine.IsEntityType(propType, MappingEngine) Then
    '                '    Dim o As Object = Activator.CreateInstance(propType)
    '                '    Dim pks As IList(Of EntityPropertyAttribute) = MappingEngine.GetPrimaryKeys(propType)
    '                '    If pks.Count <> 1 Then
    '                '        Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", propType))
    '                '    Else
    '                '        MappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, value)
    '                '    End If
    '                '    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, o, oschema, pi)
    '                '    If ce IsNot Nothing Then OrmManager.SetLoaded(ce, propertyAlias, True, True)
    '                '    Return o
    '            Else
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema, pi)
    '                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            End If
    '        End If
    '        Return value
    '    End If
    '    Return Nothing
    'End Function

    'Public Function CreateObj(ByVal objType As Type, ByVal pkValue As Object, ByVal oschema As IEntitySchema) As Object
    '    If GetType(_IEntity).IsAssignableFrom(objType) Then
    '        Dim type_created As Type = objType
    '        Dim en As String = Me.GetEntityNameByType(type_created)
    '        If Not String.IsNullOrEmpty(en) Then
    '            Dim cr As Type = Me.GetTypeByEntityName(en)
    '            If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
    '                type_created = cr
    '            End If
    '            If type_created Is Nothing Then
    '                Throw New OrmManagerException("Cannot find type for entity " & en)
    '            End If
    '        End If
    '        Dim o As _IEntity = Nothing
    '        If GetType(IKeyEntity).IsAssignableFrom(type_created) Then
    '            o = Entity.CreateKeyEntity(pkValue, type_created, Nothing, Me)
    '            o.SetObjectState(ObjectState.NotLoaded)
    '        Else
    '            Dim pks As IList(Of EntityPropertyAttribute) = Me.GetPrimaryKeys(type_created, oschema)
    '            If pks.Count <> 1 Then
    '                Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", type_created))
    '            End If
    '            If GetType(_ICachedEntity).IsAssignableFrom(type_created) Then
    '                o = Entity.CreateEntity(New PKDesc() {New PKDesc(pks(0).PropertyAlias, pkValue)}, type_created, Nothing, Me)
    '                o.SetObjectState(ObjectState.NotLoaded)
    '            Else
    '                o = Entity.CreateEntity(type_created, Nothing, Me)
    '                ObjectMappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, pkValue, oschema, Nothing)
    '            End If
    '        End If

    '        Return o
    '    ElseIf ObjectMappingEngine.IsEntityType(objType, Me) Then
    '        Dim o As Object = Activator.CreateInstance(objType)
    '        Dim pks As IList(Of EntityPropertyAttribute) = Me.GetPrimaryKeys(objType, oschema)
    '        If pks.Count <> 1 Then
    '            Throw New ObjectMappingException(String.Format("Type {0} has no single primary key", objType))
    '        Else
    '            ObjectMappingEngine.SetPropertyValue(o, pks(0).PropertyAlias, pkValue, oschema, Nothing)
    '        End If
    '        Return o
    '    Else
    '        Throw New NotSupportedException
    '    End If
    'End Function
    Public Function ParseValueFromStorage(ByVal att As Field2DbRelations,
                                          ByVal obj As Object, ByVal m As MapField2Column, ByVal propertyAlias As String,
                                          ByVal oschema As IEntitySchema, cache As CacheBase, crMan As ICreateManager,
                                          ByVal sv As IEnumerable(Of ColumnValue), ByVal ll As IPropertyLazyLoad, ByVal fac As List(Of Pair(Of String, IPKDesc))) As Object
        Dim pi As Reflection.PropertyInfo = m?.PropertyInfo

        If sv Is Nothing OrElse sv.Count = 0 OrElse sv.All(Function(it) it Is Nothing) Then
            If m IsNot Nothing AndAlso m.OptimizedSetValue IsNot Nothing AndAlso m.OptimizedSetValue IsNot MapField2Column.EmptyOptimizedSetValue Then
                m.OptimizedSetValue(obj, Nothing)
            Else
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, Nothing, oschema, pi)
            End If
            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
        Else
            If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                fac?.Add(New Pair(Of String, IPKDesc)(propertyAlias, New PKDesc(sv)))
                Return Nothing
            End If

            If pi Is Nothing Then

                If m IsNot Nothing AndAlso m.OptimizedSetValue IsNot Nothing AndAlso m.OptimizedSetValue IsNot MapField2Column.EmptyOptimizedSetValue Then
                    m.OptimizedSetValue(obj, sv(0).Value)
                Else
                    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, sv(0).Value, oschema)
                End If
                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
                Return Nothing
            End If

            Dim propType = pi.PropertyType
            If GetType(_IEntity).IsAssignableFrom(propType) OrElse ObjectMappingEngine.IsEntityType(propType) Then
                Dim type_created As Type = propType
                Dim en As String = GetEntityNameByType(type_created)
                If Not String.IsNullOrEmpty(en) Then
                    Dim cr As Type = GetTypeByEntityName(en)
                    If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
                        type_created = cr
                    End If
                    If type_created Is Nothing Then
                        Throw New OrmManagerException("Cannot find type for entity " & en)
                    End If
                End If

                Dim o As Object = Entity.CreateObject(New PKDesc(sv), type_created, Me)
                Dim cce As ICachedEntity = TryCast(o, ICachedEntity)
                Dim pkw As PKWrapper = Nothing
                Dim wasCreated = True
                If cce IsNot Nothing Then
                    pkw = New CacheKey(CType(o, ICachedEntity))
                    Dim cb As ICacheBehavior = TryCast(GetEntitySchema(type_created), ICacheBehavior)
                    Dim o2 = cache.FindObjectInCache(type_created, o, pkw, cb, cache.GetOrmDictionary(type_created, cb), True, True)
                    wasCreated = o2 Is o
                    o = o2
                    'Else
                    '    pkw = New PKWrapper(sv)
                End If

                Dim e = TryCast(o, _IEntity)
                If e IsNot Nothing Then
                    Dim eo As IEntity = TryCast(obj, IEntity)
                    If eo IsNot Nothing AndAlso eo.GetICreateManager IsNot Nothing Then
                        e.SetCreateManager(eo.GetICreateManager)
                    ElseIf crMan IsNot Nothing Then
                        e.SetCreateManager(crMan)
                    End If
                    e.CorrectStateAfterLoading(wasCreated)
                End If

                If m.OptimizedSetValue IsNot Nothing AndAlso m.OptimizedSetValue IsNot MapField2Column.EmptyOptimizedSetValue Then
                    m.OptimizedSetValue(obj, o)
                Else
                    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, o, oschema, pi)
                End If

                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)

                Return o
            ElseIf sv.Count > 1 Then
                Throw New OrmManagerException($"Multiple column property {propertyAlias} in {obj.GetType} property should be factory or entity")
            Else
                If m.OptimizedSetValue IsNot Nothing AndAlso m.OptimizedSetValue IsNot MapField2Column.EmptyOptimizedSetValue Then
                    m.OptimizedSetValue(obj, sv(0).Value)
                Else
                    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, sv(0).Value, oschema, pi)
                End If

                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
            End If
        End If

        Return Nothing
    End Function

    Public Function GetPOCOEntitySchema(ByVal t As Type) As IEntitySchema
        Dim s As IEntitySchema = GetEntitySchema(t, False)

        If s Is Nothing Then
            s = ObjectMappingEngine.GetEntitySchema(t, Me, GetIdic, GetNames)
            If s IsNot Nothing Then
                AddEntitySchema(t, s)
            End If
        End If

        Return s
    End Function

    Public Shared Function IsEntityType(ByVal t As Type) As Boolean
        If t Is Nothing Then
            Return False
        End If

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
        Dim regex As Regex = Nothing
        Dim wormPattern = System.Configuration.ConfigurationManager.AppSettings("worm:assembly-pattern")
        If Not String.IsNullOrEmpty(wormPattern) Then
            regex = New Regex(wormPattern)
        End If

        Dim moduleName As String = assembly.ManifestModule.Name
        If regex IsNot Nothing AndAlso regex.IsMatch(moduleName) Then
            Return False
        End If

        Dim excludedAssemblies = {"mscorlib.dll", "System.Data.dll", "System.Data.dll",
                                  "System.Xml.dll", "System.dll", "System.Configuration.dll",
                                  "System.Web.dll", "System.Drawing.dll", "System.Web.Services.dll",
                                  "Worm.Orm.dll", "CoreFramework.dll", "ASPNETHosting.dll",
                                  "System.Transactions.dll", "System.EnterpriseServices.dll",
                                  "System.Data.SqlServerCe.dll", "MySql.Web.dll",
                                  "Xceed.Wpf.Toolkit.dll", "FontAwesome.WPF.dll",
                                  "Antlr3.Runtime.dll", "NLog.dll"}
        If excludedAssemblies.Any(Function(it) it.ToLower = moduleName.ToLower) OrElse
            assembly.FullName.Contains("Microsoft") OrElse
            assembly.FullName.StartsWith("DotNetOpenAuth") Then
            Return True
        Else
            Dim tok As Byte() = assembly.GetName.GetPublicKeyToken()
            If IsEqualByteArray(New Byte() {&HB7, &H7A, &H5C, &H56, &H19, &H34, &HE0, &H89}, tok) Then
                Return True
            End If
            If IsEqualByteArray(New Byte() {&HB0, &H3F, &H5F, &H7F, &H11, &HD5, &HA, &H3A}, tok) Then
                Return True
            End If
            If IsEqualByteArray(New Byte() {&H31, &HBF, &H38, &H56, &HAD, &H36, &H4E, &H35}, tok) Then
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

    Friend Shared Sub InitPOCO(ByVal rt As Type, ByVal oschema As IEntitySchema,
                               ByVal ctd As ComponentModel.ICustomTypeDescriptor, ByVal mpe As ObjectMappingEngine,
                               ByVal e As _IEntity, ByVal ro As Object, ByVal cache As Cache.CacheBase, ByVal contextInfo As Object,
                               Optional ByVal pref As String = Nothing, Optional crMan As ICreateManager = Nothing)

        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
        For Each m As MapField2Column In map
            Dim pa As String = m.PropertyAlias
            If ctd.GetProperties.Find(pa, False) Is Nothing Then
                pa = rt.Name & "-" & pa
                If Not String.IsNullOrEmpty(pref) Then
                    pa = "%" & pref & "-" & pa
                End If
                If ctd.GetProperties.Find(pa, False) Is Nothing Then
                    Continue For
                End If
            End If

            Dim pi As Reflection.PropertyInfo = m.PropertyInfo
            Dim v As Object = ObjectMappingEngine.GetPropertyValue(e, pa, oschema, pi)
            If v Is DBNull.Value Then
                v = Nothing
            End If

            Dim o = mpe.ParseValueFromStorage(m.Attributes, ro, m, pa, oschema, cache, crMan, {New ColumnValue(pa, v)}, TryCast(ro, IPropertyLazyLoad), Nothing)
            Dim pit As Type = o?.GetType
            'v = ObjectMappingEngine.AssignValue2Property(pit, mpe, cache, New PKDesc() {New PKDesc(pa, v)}, ro, map, m.PropertyAlias, TryCast(ro, IPropertyLazyLoad), m, oschema, Nothing, crMan)
            If IsEntityType(pit) Then
                Dim schema As IEntitySchema = mpe.GetEntitySchema(pit, False)
                If schema Is Nothing Then
                    schema = ObjectMappingEngine.GetEntitySchema(pit, mpe, Nothing, Nothing)
                    mpe.AddEntitySchema(pit, schema)
                End If

                InitPOCO(pit, schema, ctd, mpe, e, o, cache, contextInfo, m.PropertyAlias, crMan)
            End If
        Next
    End Sub

    Public Overridable ReadOnly Property CaseSensitive() As Boolean
        Get
            Return False
        End Get
    End Property
    Friend Shared Sub SetPK(ByVal e As Object, ByVal pk As IEnumerable(Of PKDesc2), ByVal oschema As IEntitySchema, pkName As String)
#If DEBUG Then
        If pk Is Nothing Then
            Throw New ArgumentNullException(NameOf(pk))
        End If

        If String.IsNullOrEmpty(pkName) Then
            Throw New ArgumentNullException(NameOf(pkName))
        End If
#End If
        Dim op As IOptimizePK = TryCast(e, IOptimizePK)
        If op IsNot Nothing Then
            op.SetPK(New PKDesc(pk) With {.PKName = pkName})
        Else
            'Dim m = oschema.GetPK
            For Each p In pk
                'Dim sf = m.SourceFields.FirstOrDefault(Function(it) it.SourceFieldExpression = p.Column)
                ObjectMappingEngine.SetPropertyValue(e, pkName, p.Value, oschema, p.pi)
            Next
        End If
    End Sub
    'Public Shared Sub AssignValue2PK(ByVal obj As Object, ByVal oschema As IEntitySchema,
    '                                 ByVal pi As PropertyInfo, ByVal propertyAlias As String,
    '                                 ByVal value As Object)
    '    Try
    '        If pi Is Nothing Then
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema)
    '            'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '        Else
    '            Dim propType As Type = pi.PropertyType
    '            If (propType Is GetType(Boolean) AndAlso value.GetType Is GetType(Short)) OrElse (propType Is GetType(Integer) AndAlso value.GetType Is GetType(Long)) Then
    '                Dim v As Object = Convert.ChangeType(value, propType)
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, v, oschema, pi)
    '                'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
    '                Dim dt As DateTime = CDate(value)
    '                Dim l As Long = dt.ToBinary
    '                Using ms As New IO.MemoryStream
    '                    Dim sw As New IO.StreamWriter(ms)
    '                    sw.Write(l)
    '                    sw.Flush()
    '                    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, ms.ToArray, oschema, pi)
    '                    'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '                End Using
    '            Else
    '                'If c.FieldName = "ID" Then
    '                '    obj.Identifier = CInt(value)
    '                'Else
    '                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema, pi)
    '                'End If
    '                'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '            End If
    '        End If
    '    Catch ex As ArgumentException When ex.Message.Contains("'System.DateTime'") AndAlso ex.Message.Contains("'System.Byte[]'")
    '        Dim dt As DateTime = CDate(value)
    '        Dim l As Long = dt.ToBinary
    '        Using ms As New IO.MemoryStream
    '            Dim sw As New IO.StreamWriter(ms)
    '            sw.Write(l)
    '            sw.Flush()
    '            ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, ms.ToArray, oschema, pi)
    '            'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '        End Using
    '    Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0 AndAlso pi IsNot Nothing
    '        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
    '        ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, v, oschema, pi)
    '        'If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
    '    End Try

    'End Sub

    Public Overridable Function CreateEntity(ByVal t As Type) As Object
        If _ce IsNot Nothing Then
            Return _ce(t)
        Else
            Return Activator.CreateInstance(t)
        End If
    End Function

    Public Function CloneIdentity(ByVal e As Object, ByVal oschema As IEntitySchema) As Object
        Dim o As Object = CreateEntity(e.GetType)

        OrmManager.SetPK(o, oschema.GetPKs(e), oschema, Me)

        Return o
    End Function

    Public Function CloneIdentity(ByVal e As IEntity, ByVal oschema As IEntitySchema) As IEntity
        Dim o As IEntity = CType(CreateEntity(e.GetType), IEntity)
        OrmManager.SetPK(o, e.GetPKValues(oschema), oschema, Me)
        If Not Object.Equals(e.SpecificMappingEngine, o.SpecificMappingEngine) Then
            o.SpecificMappingEngine = e.SpecificMappingEngine
        End If
        Return o
    End Function

    Friend Function CloneFullEntity(ByVal e As _IEntity, ByVal oschema As IEntitySchema) As _IEntity
        Dim clone As _IEntity = CType(CloneIdentity(e, oschema), _IEntity)
        clone.SetObjectState(Entities.ObjectState.NotLoaded)
        OrmManager.CopyBody(e, clone, oschema)
        Dim ll As IPropertyLazyLoad = TryCast(clone, IPropertyLazyLoad)
        If ll IsNot Nothing Then
            ll.LazyLoadDisabled = True
            For Each m In oschema.FieldColumnMap
                ll.IsPropertyLoaded(m.PropertyAlias) = CType(e, IPropertyLazyLoad).IsPropertyLoaded(m.PropertyAlias)
            Next
        End If
        clone.SetObjectStateClear(e.ObjectState)
        clone.IsLoaded = e.IsLoaded

        Return clone
    End Function

    Public ReadOnly Property Features As IList(Of String)
        Get
            Return _features
        End Get
    End Property

    Private Shared Sub SaveToCache(cachePath As String)
        Using sw As New IO.StreamWriter(cachePath, False, Encoding.UTF8)
            For Each t In _entityTypes
                sw.WriteLine(t.AssemblyQualifiedName)
            Next
        End Using
    End Sub

    Private Shared Iterator Function LoadFromCache(cachePath As String) As IEnumerable(Of Type)
        Using sr As New IO.StreamReader(cachePath, Encoding.UTF8)
            Dim line = sr.ReadLine
            Do While Not String.IsNullOrEmpty(line)
                Dim entype As Type = Nothing
                Try
                    entype = Type.GetType(line)
                Catch ex As Exception
                    Debug.WriteLine("Error during loading type {0}: {1}", line, ex.ToString)
                End Try
                If entype IsNot Nothing Then
                    Yield entype
                End If
                line = sr.ReadLine
            Loop
        End Using
    End Function

End Class

'Public Class ColumnTypeMap
'    Public Property Column As String
'    Public PropType As Type
'    Public DBType As Type

'    Public Function ChangeType(v As Object) As Object
'        Return Convert.ChangeType(v, PropType)
'    End Function

'    Public Shared Function NeedMap(propType As Type, dbType As Type) As ColumnTypeMap
'        If propType Is dbType Then
'            Return Nothing
'        End If


'    End Function
'End Class
'End Namespace

Friend Class PKDesc2
    Inherits ColumnValue

    Public pi As PropertyInfo
    Public svo As IOptimizeSetValue.SetValueDelegate
    Public fldNull As Boolean

    Public Sub New(column As String, value As Object, pi As PropertyInfo)
        MyBase.New(column, value)
        Me.pi = pi
    End Sub
    Public Sub New(column As String, value As Object, pi As PropertyInfo, svo As IOptimizeSetValue.SetValueDelegate)
        MyBase.New(column, value)
        Me.pi = pi
        Me.svo = svo
    End Sub
    Public Sub New(column As String, value As Object, pi As PropertyInfo, svo As IOptimizeSetValue.SetValueDelegate, fldNull As Boolean)
        MyBase.New(column, value)
        Me.pi = pi
        Me.svo = svo
        Me.fldNull = fldNull
    End Sub
End Class