Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Linq

Namespace Entities.Meta
    Public Module SchemaExtensions
        <Extension>
        Public Function GetPKs(ByVal obj As Object, Optional mpe As ObjectMappingEngine = Nothing) As IPKDesc
            Dim op As IOptimizePK = TryCast(obj, IOptimizePK)
            If op IsNot Nothing Then
                Return op.GetPKValues()
            Else
                Dim oschema As IEntitySchema = Nothing
                Dim o = TryCast(obj, IEntity)
                If o IsNot Nothing Then
                    oschema = o.GetEntitySchema(mpe)
                End If

                If oschema IsNot Nothing Then
                    Dim l As New PKDesc

                    Dim pk = oschema.GetPK
                    If pk.SourceFields.Count > 1 Then
                        For Each sf In pk.SourceFields
                            l.Add(New ColumnValue(sf.SourceFieldExpression, ObjectMappingEngine.GetPropertyValue(obj, MakePKName(pk.PropertyAlias, sf.SourceFieldExpression), oschema, sf.PropertyInfo)))
                        Next
                    Else
                        l.Add(New ColumnValue(pk.SourceFields(0).SourceFieldExpression, ObjectMappingEngine.GetPropertyValue(obj, pk.PropertyAlias, oschema, pk.PropertyInfo)))
                    End If
                    Return l
                End If
            End If

            Return New PKDesc From {}
        End Function
        <Extension>
        Public Function GetPKs(ByVal oschema As IEntitySchema, ByVal obj As Object) As IPKDesc
            Dim op As IOptimizePK = TryCast(obj, IOptimizePK)
            If op IsNot Nothing Then
                Return op.GetPKValues()
            Else
                If oschema Is Nothing Then
                    Throw New ArgumentNullException("oschema")
                End If

                Dim l As New PKDesc

                Dim pk = oschema.GetPK

                If pk.SourceFields.Count > 1 Then
                    For Each sf In pk.SourceFields
                        l.Add(New ColumnValue(sf.SourceFieldExpression, ObjectMappingEngine.GetPropertyValue(obj, MakePKName(pk.PropertyAlias, sf.SourceFieldExpression), oschema, sf.PropertyInfo)))
                    Next
                Else
                    l.Add(New ColumnValue(pk.SourceFields(0).SourceFieldExpression, ObjectMappingEngine.GetPropertyValue(obj, pk.PropertyAlias, oschema, pk.PropertyInfo)))
                End If

                Return l
            End If
        End Function
        <Extension>
        Public Function GetPK(ByVal oschema As IPropertyMap) As MapField2Column
            If oschema Is Nothing Then
                Throw New ArgumentNullException("oschema")
            End If

            For Each mp As MapField2Column In oschema.FieldColumnMap
                If mp.IsPK Then
                    Return mp
                End If
            Next

            Return Nothing
        End Function
        <Extension>
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
        <Extension>
        Public Function GetPKTable(ByVal schema As IPropertyMap, Optional ByVal t As Type = Nothing) As SourceFragment
            Return schema.GetPK?.Table
            'For Each m As MapField2Column In schema.GetPKs
            '    Return m.Table
            'Next
            'For Each ea As EntityPropertyAttribute In GetProperties(t, schema).Keys
            '    If (GetAttributes(schema, ea) And Field2DbRelations.PK) = Field2DbRelations.PK Then
            '        Return GetPropertyTable(schema, ea.PropertyAlias)
            '    End If
            'Next
            Return Nothing
        End Function
        <Extension>
        Public Function GetPropertyAliasByType(ByVal oschema As IPropertyMap, ByVal propertyType As Type) As List(Of String)
            If oschema Is Nothing Then
                Throw New ArgumentNullException("oschema")
            End If

            If propertyType Is Nothing Then
                Throw New ArgumentNullException("propertyType")
            End If

            Dim l As New List(Of String)
            For Each m As MapField2Column In oschema.FieldColumnMap
                If m.PropertyInfo.PropertyType Is propertyType Then
                    l.Add(m.PropertyAlias)
                End If
            Next
            Return l
        End Function
        <Extension>
        Public Function GetJoinFieldNameByType(ByVal oschema As IPropertyMap, ByVal subType As Type) As String
            Dim j As IJoinBehavior = TryCast(oschema, IJoinBehavior)
            Dim r As String = Nothing
            If j IsNot Nothing Then
                r = j.GetJoinField(subType)
            End If
            If String.IsNullOrEmpty(r) Then
                Dim c As ICollection(Of String) = oschema.GetPropertyAliasByType(subType)
                If c.Count = 1 Then
                    For Each s As String In c
                        r = s
                    Next
                End If
            End If
            Return r
        End Function
        <Extension>
        Public Function GetM2MRelations(ByVal s As IEntitySchema) As M2MRelationDesc()
            Dim schema As ISchemaWithM2M = TryCast(s, ISchemaWithM2M)
            If schema IsNot Nothing Then
                Return schema.GetM2MRelations
            Else
                Return New M2MRelationDesc() {}
            End If
        End Function
        <Extension>
        Public Function ChangeValueType(ByVal s As IEntitySchema, ByVal propertyAlias As String, ByVal o As Object) As Object
            If s Is Nothing Then
                Throw New ArgumentNullException(NameOf(s))
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

            If schema IsNot Nothing AndAlso schema.ChangeValueType(propertyAlias, o, v) Then
                Return v
            End If

            If GetType(System.Xml.XmlDocument) Is ot Then
                Return CType(o, System.Xml.XmlDocument).OuterXml
            End If

            If GetType(System.Xml.XmlDocumentFragment) Is ot Then
                Return CType(o, System.Xml.XmlDocumentFragment).OuterXml
            End If

            If GetType(ISinglePKEntity).IsAssignableFrom(ot) Then
                Return CType(o, SinglePKEntity).Identifier
            ElseIf GetType(ICachedEntity).IsAssignableFrom(ot) Then
                Dim pks = CType(o, ICachedEntity).GetPKValues(s)
                If pks.Count <> 1 Then
                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", ot))
                End If
                Return pks(0).Value
            End If

            If ObjectMappingEngine.IsEntityType(ot) Then
                Return ObjectMappingEngine.GetPropertyValue(o, schema.GetSinglePK(Function() ot), s)
            End If
            Return v
        End Function
        <Extension>
        Public Function GetSinglePK(ByVal oschema As IPropertyMap, ByVal getTypeFunc As Func(Of Type)) As String
            If oschema Is Nothing Then
                Throw New ArgumentNullException(NameOf(oschema))
            End If
            Return oschema.GetPK.PropertyAlias
            'Dim pk As String = Nothing
            'For Each mp As MapField2Column In oschema.GetPKs
            '    If String.IsNullOrEmpty(pk) Then
            '        pk = mp.PropertyAlias
            '    Else
            '        Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", If(getTypeFunc Is Nothing, oschema.GetType, getTypeFunc())))
            '    End If
            'Next
            'Return pk
        End Function
        <Extension>
        Public Function GetPropertyTypeByName(ByVal oschema As IPropertyMap, ByVal type As Type, ByVal propertyAlias As String) As Type
            If oschema Is Nothing Then
                Throw New ArgumentNullException(NameOf(oschema))
            End If
            Dim m As MapField2Column = Nothing
            If Not oschema.FieldColumnMap.TryGetValue(propertyAlias, m) Then
                Throw New ObjectMappingException("Type " & type.Name & " doesnot contain property " & propertyAlias)
            End If
            Return m.PropertyInfo.PropertyType
        End Function
        <Extension>
        Public Function GetPropertyByAlias(ByVal oschema As IPropertyMap, ByVal type As Type, ByVal propertyAlias As String) As Reflection.PropertyInfo
            If oschema Is Nothing Then
                Throw New ArgumentNullException(NameOf(oschema))
            End If
            Dim m As MapField2Column = Nothing
            If Not oschema.FieldColumnMap.TryGetValue(propertyAlias, m) Then
                Throw New ObjectMappingException("Type " & type.Name & " doesnot contain property " & propertyAlias)
            End If
            Return m.PropertyInfo
        End Function
        <Extension>
        Public Iterator Function GetAutoLoadFields(ByVal oschema As IPropertyMap) As IEnumerable(Of MapField2Column)
            If oschema Is Nothing Then
                Throw New ArgumentNullException(NameOf(oschema))
            End If

            For Each mp As MapField2Column In oschema.FieldColumnMap
                If (mp.Attributes And Field2DbRelations.Hidden) = 0 Then
                    Yield mp
                End If
            Next
        End Function
        <Extension>
        Public Function GetAutoLoadMap(ByVal oschema As IPropertyMap) As Collections.IndexedCollection(Of String, MapField2Column)
            If oschema Is Nothing Then
                Throw New ArgumentNullException(NameOf(oschema))
            End If

            Dim r As New OrmObjectIndex()
            For Each mp As MapField2Column In oschema.GetAutoLoadFields
                'If (mp.Attributes And Field2DbRelations.Hidden) = 0 Then
                r.Add(mp)
                'End If
            Next

            Return r
        End Function
        <Extension>
        Public Function ConvertFromString(ByVal oschema As IPropertyMap, mpe As ObjectMappingEngine, prop As String, s As String) As Object
            Return mpe.ConvertFromString(oschema, prop, s)
        End Function
    End Module
End Namespace