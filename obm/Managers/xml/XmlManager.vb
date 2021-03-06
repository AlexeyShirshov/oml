﻿Imports Worm.Cache
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports System.Xml.XPath
Imports Worm.Entities
Imports Worm.Criteria.Joins
Imports Worm.Query.Sorting
Imports CoreFramework.CFDebugging
Imports System.Linq

Namespace Xml
    Partial Public Class QueryManager
        Inherits OrmManager

        Private ReadOnly _fileName As String
        Private ReadOnly _stream As IO.Stream
        Private _exec As TimeSpan
        Private _fetch As TimeSpan

        Public Sub New(ByVal cache As OrmCache, ByVal mpe As ObjectMappingEngine, ByVal gen As XPathGenerator, ByVal fileName As String)
            MyBase.New(cache, mpe)
            _fileName = fileName
            StmtGenerator = gen
        End Sub

        Public Sub New(ByVal mpe As ObjectMappingEngine, ByVal gen As XPathGenerator, ByVal filename As String)
            MyBase.New(mpe)
            StmtGenerator = gen
            _fileName = filename
        End Sub

        Public Sub New(ByVal cache As OrmCache, ByVal mpe As ObjectMappingEngine, ByVal gen As XPathGenerator, ByVal stream As IO.Stream)
            MyBase.New(cache, mpe)
            _stream = stream
            StmtGenerator = gen
        End Sub

        Public Sub New(ByVal mpe As ObjectMappingEngine, ByVal gen As XPathGenerator, ByVal stream As IO.Stream)
            MyBase.New(mpe)
            StmtGenerator = gen
            _stream = stream
        End Sub

        Public ReadOnly Property XPathGenerator() As XPathGenerator
            Get
                Return CType(StmtGenerator, XPathGenerator)
            End Get
        End Property

#Region " Overrides "

        'Public Overrides Function AddObject(ByVal obj As Orm.OrmBase) As Orm.OrmBase
        '    Throw New NotImplementedException
        'End Function

        Protected Friend Overrides Sub DeleteObject(ByVal obj As ICachedEntity)
            Throw New NotImplementedException
        End Sub

        Protected Overloads Overrides Sub M2MSave(ByVal obj As ICachedEntity, ByVal t As System.Type, ByVal direct As String, ByVal el As M2MRelation)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function InsertObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        'Public Overrides Function SaveChanges(ByVal obj As Orm.OrmBase, ByVal AcceptChanges As Boolean) As Boolean
        '    Throw New NotImplementedException
        'End Function

        Public Overrides Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException
        End Function

        Protected Friend Overrides ReadOnly Property Exec() As System.TimeSpan
            Get

            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Fecth() As System.TimeSpan
            Get

            End Get
        End Property

#If Not ExcludeFindMethods Then
        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})( _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sort, _
            ByVal key As String, ByVal id As String) As OrmManager.ICacheItemProvoder(Of T)
            Return New FilterCustDelegate(Of T)(Me, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})( _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sort, _
            ByVal key As String, ByVal id As String, ByVal cols() As String) As OrmManager.ICacheItemProvoder(Of T)
            If cols Is Nothing Then
                Throw New ArgumentNullException("cols")
            End If
            Dim l As New List(Of EntityPropertyAttribute)
            Dim has_id As Boolean = False
            For Each c As String In cols
                Dim col As EntityPropertyAttribute = MappingEngine.GetColumnByPropertyAlias(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If (MappingEngine.GetAttributes(GetType(T), col) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                'l.Add(SQLGenerator.GetColumnByFieldName(GetType(T), OrmBaseT.PKName))
                l.Add(MappingEngine.GetPrimaryKeys(GetType(T))(0))
            End If
            Return New FilterCustDelegate(Of T)(Me, filter, l, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})( _
            ByVal relation As Entities.Meta.M2MRelationDesc, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManager.ICacheItemProvoder(Of T)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IKeyEntity})( _
            ByVal obj As _IKeyEntity, ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sort, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManager.ICacheItemProvoder(Of T2)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IKeyEntity})( _
            ByVal obj As _IKeyEntity, ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sort, _
            ByVal queryAspect() As Entities.Query.QueryAspect, ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManager.ICacheItemProvoder(Of T2)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})( _
            ByVal aspect As Entities.Query.QueryAspect, ByVal join() As Worm.Criteria.Joins.QueryJoin, _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of EntityPropertyAttribute) = Nothing) As OrmManager.ICacheItemProvoder(Of T)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetObjects(Of T As {New, IKeyEntity})( _
            ByVal ids As System.Collections.Generic.IList(Of Object), ByVal f As Worm.Criteria.Core.IFilter, _
            ByVal objs As System.Collections.Generic.List(Of T), ByVal withLoad As Boolean, _
            ByVal fieldName As String, ByVal idsSorted As Boolean) As System.Collections.Generic.IList(Of T)

            Throw New NotImplementedException
        End Function

#End If

#If OLDM2M Then
        Protected Overloads Overrides Function GetObjects(Of T As {New, IKeyEntity})( _
            ByVal type As System.Type, ByVal ids As System.Collections.Generic.IList(Of Object), _
            ByVal f As Worm.Criteria.Core.IFilter, ByVal relation As Entities.Meta.M2MRelationDesc, _
            ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As System.Collections.Generic.IDictionary(Of Object, Entities.CachedM2MRelation)

            Throw New NotImplementedException
        End Function
#End If

        Protected Overrides Function GetSearchSection() As String
            Throw New NotImplementedException
        End Function

        Protected Friend Overrides Function GetStaticKey() As String
            Return Nothing
        End Function

        Protected Friend Overrides Sub LoadObject(ByVal obj As _IEntity, ByVal propertyAlias As String)
            Throw New NotImplementedException
        End Sub

        'Protected Friend Overloads Overrides Function LoadObjectsInternal(Of T As {New, Orm.OrmBase})( _
        '    ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)
        '    Throw New NotImplementedException
        'End Function

        'Public Overloads Overrides Function LoadObjectsInternal(Of T As {New, IKeyEntity}, T2 As IKeyEntity)( _
        '    ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean, ByVal columns As System.Collections.Generic.List(Of Entities.Meta.EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
        '    Throw New NotImplementedException
        'End Function

        'Public Overloads Overrides Function LoadObjectsInternal(Of T2 As IKeyEntity)(ByVal realType As Type, _
        '    ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean, ByVal columns As System.Collections.Generic.List(Of Entities.Meta.EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
        '    Throw New NotImplementedException
        'End Function

#If Not ExcludeFindMethods Then
        Protected Overloads Overrides Function Search(Of T As {New, IKeyEntity})(ByVal type2search As System.Type, ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As Worm.Criteria.Core.IFilter, ByVal frmt As Entities.Meta.IFtsStringFormatter, Optional ByVal joins() As QueryJoin = Nothing) As ReadOnlyList(Of T)
            Throw New NotImplementedException
        End Function

        Protected Overrides Function SearchEx(Of T As {New, IKeyEntity})(ByVal type2search As System.Type, ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As Worm.Criteria.Core.IFilter, ByVal ftsText As String, ByVal limit As Integer, ByVal frmt As Entities.Meta.IFtsStringFormatter) As ReadOnlyList(Of T)
            Throw New NotImplementedException
        End Function
#End If

#End Region

        Protected Friend Sub LoadMultipleObjects(Of T As {New, _IEntity})( _
            ByVal xpath As String, _
            ByVal values As IList)

            Dim original_type As Type = GetType(T)
            Dim nav As XPathNavigator = GetNavigator()
            'Dim et As New PerfCounter
            Dim et As New Stopwatch
            Dim nodes As XPathNodeIterator = nav.Select(xpath)
            et.Stop()
            _exec = et.Elapsed

            'If values Is Nothing Then
            '    values = New Generic.List(Of T)
            'End If

            'Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)()
            Dim dic As IDictionary = GetDictionary(original_type)
            Dim oschema As IEntitySchema = MappingEngine.GetEntitySchema(original_type)
            'Dim ft As New PerfCounter
            Dim ft As New Stopwatch
            Do While nodes.MoveNext
                LoadFromNodeIterator(Of T)(nodes.Current.Clone, dic, values, _loadedInLastFetch, oschema)
            Loop
            ft.Stop()
            _fetch = ft.Elapsed
            'Return CType(CreateReadonlyList(original_type, CType(values, System.Collections.IList)), Global.Worm.ReadOnlyEntityList(Of T))
            'Return New ReadOnlyList(Of T)(CType(values, List(Of T)))
        End Sub

        Protected Function GetNavigator() As XPathNavigator
            Dim d As New System.Xml.XmlDocument
            If Not String.IsNullOrEmpty(_fileName) Then
                d.Load(_fileName)
            Else
                d.Load(_stream)
            End If
            Return d.CreateNavigator
        End Function

        Protected Sub LoadFromNodeIterator(Of T As {New, _IEntity})(ByVal node As XPathNavigator, _
            ByVal dic As IDictionary, _
            ByVal values As IList, ByRef loaded As Integer, ByVal oschema As IEntitySchema)
            'Dim id As Integer = CInt(dr.GetValue(idx))
            Dim obj As T = New T '= CType(CreateDBObject(Of T)(id, dic, False), T)
            Dim oo As T = obj
            Dim orm As _ICachedEntity = TryCast(obj, _ICachedEntity)
            Using obj.AcquareLock()
                obj.BeginLoading()
                Dim pk = orm.GetPKValues(oschema)
                If LoadPK(oschema, node, orm) Then
                    obj = CType(NormalizeObject(orm, dic, True, True, oschema), T)
                    If obj.ObjectState = ObjectState.Created Then
                        CreateCopyForSaveNewEntry(orm, oschema, pk)
                        'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                    End If

                    'If withLoad Then
                    Using obj.AcquareLock()
                        'obj.RaiseBeginModification(ModifiedObject.ReasonEnum.Unknown)
                        'If obj.IsLoaded Then obj.IsLoaded = False
                        LoadData(oschema, node, obj)
                        obj.CorrectStateAfterLoading(Object.ReferenceEquals(oo, obj))
                    End Using
                    'End If
                    values.Add(obj)
                    If obj.IsLoaded Then
                        loaded += 1
                    End If
                Else
                    If _mcSwitch.TraceVerbose Then
                        WriteLine("Attempt to load unallowed object " & GetType(T).Name & " (" & node.InnerXml & ")")
                    End If
                End If
                obj.EndLoading()
            End Using
        End Sub

        Protected Function LoadPK(ByVal oschema As IEntitySchema, ByVal node As XPathNavigator, ByVal obj As _ICachedEntity) As Boolean
            Dim original_type As Type = obj.GetType
            Dim cnt As Integer
            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
            Dim fv As IStorageValueConverter = TryCast(obj, IStorageValueConverter)
            Dim svo = TryCast(oschema, IOptimizeSetValue)
            Dim pk = oschema.GetPK
            Dim pks As New List(Of PKDesc2)

            For Each sf In pk.SourceFields
                Dim attr As String = sf.SourceFieldExpression
                Dim n As XPathNavigator = node.Clone
                Dim nodes As XPathNodeIterator = n.Select(attr)
                Dim sn As Boolean
                Do While nodes.MoveNext
                    If sn Then
                        Throw New OrmManagerException(String.Format("Field {0} selects more than one node", attr))
                    End If

                    Dim value As Object = nodes.Current.Value

                    If fv IsNot Nothing Then
                        value = fv.CreateValue(oschema, pk, pk.PropertyAlias, sf.SourceFieldAlias, value)
                    End If

                    Dim pi = sf.PropertyInfo

                    If pi Is Nothing Then
                    Else
                        If sf.Converter Is Nothing Then
                            If pi.PropertyType Is value.GetType Then
                                sf.Converter = MapField2Column.EmptyConverter
                            Else
                                sf.Converter = Converters.GetConverter(pi.PropertyType, value.GetType)
                            End If
                        End If

                        If sf.Converter IsNot MapField2Column.EmptyConverter Then
                            value = sf.Converter(pi.PropertyType, value.GetType, value)
                        End If
                    End If

                    If sf.OptimizedSetValue Is Nothing Then
                        If svo IsNot Nothing Then
                            sf.OptimizedSetValue = svo.GetOptimizedDelegate(sf.SourceFieldExpression)
                        Else
                            sf.OptimizedSetValue = MapField2Column.EmptyOptimizedSetValue
                        End If
                    End If

                    pks.Add(New PKDesc2(sf.SourceFieldExpression, value, sf.PropertyInfo, sf.OptimizedSetValue))

                    sn = True
                    cnt += 1
                Loop
            Next

            If pks.All(Function(it) it.svo IsNot Nothing AndAlso it.svo IsNot MapField2Column.EmptyOptimizedSetValue) Then
                For Each pkasd In pks
                    pkasd.svo(obj, pkasd.Value)
                Next
            Else
                ObjectMappingEngine.SetPK(obj, pks, oschema, pk.PropertyAlias)
            End If


            obj.PKLoaded(pk.PropertyAlias)
            Return cnt > 0
        End Function

        Protected Function LoadData(ByVal oschema As IEntitySchema, ByVal node As XPathNavigator, ByVal obj As _IEntity) As Boolean
            Dim original_type As Type = obj.GetType
            Dim orm As _ICachedEntity = TryCast(obj, _ICachedEntity)
            Dim cnt As Integer
            Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
            Dim ll As IPropertyLazyLoad = TryCast(obj, IPropertyLazyLoad)
            For Each m As MapField2Column In map
                If Not m.IsPK Then
                    Dim l As New List(Of ColumnValue)
                    For Each sf In m.SourceFields
                        Dim attr As String = sf.SourceFieldExpression
                        Dim n As XPathNavigator = node.Clone
                        Dim nodes As XPathNodeIterator = n.Select(attr)
                        Dim sn As Boolean
                        Do While nodes.MoveNext
                            If sn Then
                                Throw New OrmManagerException(String.Format("Field {0} selects more than one node", attr))
                            End If

                            l.Add(New ColumnValue(sf.SourceFieldExpression, nodes.Current.Value))

                            'ObjectMappingEngine.SetPropertyValue(obj, m.PropertyAlias, nodes.Current.Value, oschema, m.PropertyInfo)
                            'If orm IsNot Nothing Then orm.SetLoaded(m.PropertyAlias, True, True, map, MappingEngine)
                            sn = True
                            cnt += 1
                        Loop
                    Next

                    MappingEngine.ParseValueFromStorage(m.Attributes, obj, m, m.PropertyAlias, oschema, Cache, GetCreateManager, l, ll, Nothing)
                Else
                    If ll IsNot Nothing Then SetLoaded(ll, m.PropertyAlias, True)
                End If
            Next

            'If ll IsNot Nothing Then
            Return CheckIsAllLoaded(obj, MappingEngine, cnt, map)
            'End If

            Return False
        End Function

        Public Overrides Function GetEntityCloneFromStorage(ByVal obj As Entities._ICachedEntity) As Entities.ICachedEntity
            Throw New NotImplementedException
        End Function
        Public Overrides Sub LoadProperty(cachedEntity As _IEntity, propertyAlias As String, stream As IO.Stream, Optional bufSize As Integer = 4096)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub LoadObjectProperties(obj As _IEntity, ParamArray properties() As String)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace