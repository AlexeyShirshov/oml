Imports Worm.Cache
Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports System.Xml.XPath
Imports Worm.Orm
Imports Worm.Criteria.Joins

Namespace Xml
    Partial Public Class QueryManager
        Inherits OrmManagerBase

        Private _fileName As String
        Private _stream As IO.Stream

        Private _exec As TimeSpan
        Private _fetch As TimeSpan

        Public Sub New(ByVal cache As OrmCacheBase, ByVal schema As XPathGenerator, ByVal fileName As String)
            MyBase.New(cache, schema)
            _fileName = fileName
        End Sub

        Public Sub New(ByVal schema As XPathGenerator, ByVal filename As String)
            MyBase.New(schema)

            _fileName = filename
        End Sub

        Public Sub New(ByVal cache As OrmCacheBase, ByVal schema As XPathGenerator, ByVal stream As IO.Stream)
            MyBase.New(cache, schema)
            _stream = stream
        End Sub

        Public Sub New(ByVal schema As XPathGenerator, ByVal stream As IO.Stream)
            MyBase.New(schema)

            _stream = stream
        End Sub

#Region " Overrides "

        'Public Overrides Function AddObject(ByVal obj As Orm.OrmBase) As Orm.OrmBase
        '    Throw New NotImplementedException
        'End Function

        Protected Friend Overrides Sub DeleteObject(ByVal obj As ICachedEntity)
            Throw New NotImplementedException
        End Sub

        Protected Overloads Overrides Sub M2MSave(ByVal obj As IOrmBase, ByVal t As System.Type, ByVal direct As String, ByVal el As EditableListBase)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function InsertObject(ByVal obj As ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        'Public Overrides Function SaveChanges(ByVal obj As Orm.OrmBase, ByVal AcceptChanges As Boolean) As Boolean
        '    Throw New NotImplementedException
        'End Function

        Protected Friend Overrides Function UpdateObject(ByVal obj As ICachedEntity) As Boolean
            Throw New NotImplementedException
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, Orm.OrmBase})(ByVal level As Integer, ByVal filter As Worm.Criteria.Core.IFilter, ByVal join() As Worm.Criteria.Joins.OrmJoin) As Orm.DicIndex(Of T)
            Throw New NotImplementedException
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, Orm.OrmBase})(ByVal level As Integer, ByVal filter As Worm.Criteria.Core.IFilter, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal firstField As String, ByVal secondField As String) As Orm.DicIndex(Of T)
            Throw New NotImplementedException
        End Function

        Protected Overrides ReadOnly Property Exec() As System.TimeSpan
            Get

            End Get
        End Property

        Protected Overrides ReadOnly Property Fecth() As System.TimeSpan
            Get

            End Get
        End Property

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})( _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sorting.Sort, _
            ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New FilterCustDelegate(Of T)(Me, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})( _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sorting.Sort, _
            ByVal key As String, ByVal id As String, ByVal cols() As String) As OrmManagerBase.ICustDelegate(Of T)
            If cols Is Nothing Then
                Throw New ArgumentNullException("cols")
            End If
            Dim l As New List(Of ColumnAttribute)
            Dim has_id As Boolean = False
            For Each c As String In cols
                Dim col As ColumnAttribute = ObjectSchema.GetColumnByFieldName(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If c = "ID" Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                l.Add(ObjectSchema.GetColumnByFieldName(GetType(T), "ID"))
            End If
            Return New FilterCustDelegate(Of T)(Me, filter, l, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})( _
            ByVal relation As Orm.Meta.M2MRelation, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal sort As Sorting.Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IOrmBase})( _
            ByVal obj As IOrmBase, ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sorting.Sort, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManagerBase.ICustDelegate(Of T2)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IOrmBase})( _
            ByVal obj As IOrmBase, ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sorting.Sort, _
            ByVal queryAspect() As Orm.Query.QueryAspect, ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManagerBase.ICustDelegate(Of T2)

            Throw New NotImplementedException
        End Function


        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})( _
            ByVal aspect As Orm.Query.QueryAspect, ByVal join() As Worm.Criteria.Joins.OrmJoin, _
            ByVal filter As Worm.Criteria.Core.IFilter, ByVal sort As Sorting.Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of ColumnAttribute) = Nothing) As OrmManagerBase.ICustDelegate(Of T)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetObjects(Of T As {New, IOrmBase})( _
            ByVal ids As System.Collections.Generic.IList(Of Object), ByVal f As Worm.Criteria.Core.IFilter, _
            ByVal objs As System.Collections.Generic.List(Of T), ByVal withLoad As Boolean, _
            ByVal fieldName As String, ByVal idsSorted As Boolean) As System.Collections.Generic.IList(Of T)

            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function GetObjects(Of T As {New, IOrmBase})( _
            ByVal type As System.Type, ByVal ids As System.Collections.Generic.IList(Of Object), _
            ByVal f As Worm.Criteria.Core.IFilter, ByVal relation As Orm.Meta.M2MRelation, _
            ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As System.Collections.Generic.IDictionary(Of Object, Cache.EditableList)

            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetSearchSection() As String
            Throw New NotImplementedException
        End Function

        Protected Friend Overrides Function GetStaticKey() As String
            Return Nothing
        End Function

        Protected Friend Overrides Sub LoadObject(ByVal obj As ICachedEntity)
            Throw New NotImplementedException
        End Sub

        'Protected Friend Overloads Overrides Function LoadObjectsInternal(Of T As {New, Orm.OrmBase})( _
        '    ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)
        '    Throw New NotImplementedException
        'End Function

        Protected Friend Overloads Overrides Function LoadObjectsInternal(Of T As {New, IOrmBase})( _
            ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As System.Collections.Generic.List(Of Orm.Meta.ColumnAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T)
            Throw New NotImplementedException
        End Function

        'Protected Overrides Function MakeJoin(ByVal type2join As System.Type, ByVal selectType As System.Type, ByVal field As String, ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Worm.Criteria.Joins.JoinType, Optional ByVal switchTable As Boolean = False) As Worm.Criteria.Joins.OrmJoin
        '    Throw New NotImplementedException
        'End Function

        'Protected Overrides Function MakeM2MJoin(ByVal m2m As Orm.Meta.M2MRelation, ByVal type2join As System.Type) As Worm.Criteria.Joins.OrmJoin()
        '    Throw New NotImplementedException
        'End Function

        Protected Overloads Overrides Function Search(Of T As {New, IOrmBase})(ByVal type2search As System.Type, ByVal contextKey As Object, ByVal sort As Sorting.Sort, ByVal filter As Worm.Criteria.Core.IFilter, ByVal frmt As Orm.Meta.IFtsStringFormater, Optional ByVal joins() As OrmJoin = Nothing) As ReadOnlyList(Of T)
            Throw New NotImplementedException
        End Function

        Protected Overrides Function SearchEx(Of T As {New, IOrmBase})(ByVal type2search As System.Type, ByVal contextKey As Object, ByVal sort As Sorting.Sort, ByVal filter As Worm.Criteria.Core.IFilter, ByVal ftsText As String, ByVal limit As Integer, ByVal frmt As Orm.Meta.IFtsStringFormater) As ReadOnlyList(Of T)
            Throw New NotImplementedException
        End Function

#End Region

        Protected Friend Function LoadMultipleObjects(Of T As {New, _ICachedEntity})(ByVal xpath As String, ByVal withLoad As Boolean, _
            ByRef values As Generic.IList(Of T)) As ReadOnlyEntityList(Of T)

            Dim original_type As Type = GetType(T)
            Dim nav As XPathNavigator = GetNavigator()
            Dim et As New PerfCounter
            Dim nodes As XPathNodeIterator = nav.Select(xpath)
            _exec = et.GetTime

            If values Is Nothing Then
                values = New Generic.List(Of T)
            End If

            Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)()
            Dim oschema As IOrmObjectSchema = CType(_schema.GetObjectSchema(original_type), IOrmObjectSchema)
            Dim ft As New PerfCounter
            Do While nodes.MoveNext
                LoadFromNodeIterator(Of T)(nodes.Current.Clone, dic, values, _loadedInLastFetch, oschema)
            Loop
            _fetch = ft.GetTime
            Return CType(CreateReadonlyList(original_type, CType(values, System.Collections.IList)), Global.Worm.ReadOnlyEntityList(Of T))
            'Return New ReadOnlyList(Of T)(CType(values, List(Of T)))
        End Function

        Protected Function GetNavigator() As XPathNavigator
            Dim d As New System.Xml.XmlDocument
            If Not String.IsNullOrEmpty(_fileName) Then
                d.Load(_fileName)
            Else
                d.Load(_stream)
            End If
            Return d.CreateNavigator
        End Function

        Protected Sub LoadFromNodeIterator(Of T As {New, _ICachedEntity})(ByVal node As XPathNavigator, ByVal dic As Generic.IDictionary(Of Object, T), _
            ByVal values As IList(Of T), ByRef loaded As Integer, ByVal oschema As IOrmObjectSchema)
            'Dim id As Integer = CInt(dr.GetValue(idx))
            Dim obj As T = CreateEntity(Of T)() '= CType(CreateDBObject(Of T)(id, dic, False), T)
            Using obj.GetSyncRoot()
                obj.BeginLoading()
                If LoadPK(oschema, node, obj) Then
                    obj = GetObjectFromCache(Of T)(obj, dic, False, False, True)
                    If obj.ObjectState = ObjectState.NotLoaded Then
                        Using obj.GetSyncRoot()
                            obj.RaiseBeginModification(ModifiedObject.ReasonEnum.Unknown)
                            'If obj.IsLoaded Then obj.IsLoaded = False
                            LoadData(oschema, node, obj)
                            If obj.ObjectState = ObjectState.Modified AndAlso obj.IsLoaded Then obj.ObjectState = ObjectState.None
                            values.Add(obj)
                            loaded += 1
                        End Using
                    Else
                        values.Add(obj)
                        If obj.IsLoaded Then
                            loaded += 1
                        End If
                    End If
                Else
                    If _mcSwitch.TraceVerbose Then
                        WriteLine("Attempt to load unallowed object " & GetType(T).Name & " (" & node.InnerXml & ")")
                    End If
                End If
                obj.EndLoading()
            End Using
        End Sub

        Protected Function LoadPK(ByVal oschema As IOrmObjectSchema, ByVal node As XPathNavigator, ByVal obj As ICachedEntity) As Boolean
            Dim original_type As Type = obj.GetType
            Dim seted As Boolean
            For Each c As ColumnAttribute In _schema.GetSortedFieldList(original_type)
                If (_schema.GetAttributes(original_type, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    Dim attr As String = _schema.GetColumnNameByFieldNameInternal(original_type, c.FieldName, False)
                    Dim n As XPathNavigator = node.Clone
                    Dim nodes As XPathNodeIterator = n.Select(attr)
                    Dim sn As Boolean
                    Do While nodes.MoveNext
                        If sn Then
                            Throw New OrmManagerException(String.Format("Field {0} selects more than one node", attr))
                        End If
                        obj.SetValue(Nothing, c, oschema, nodes.Current.Value)
                        seted = True
                        sn = True
                    Loop
                End If
            Next
            Return seted
        End Function

        Protected Function LoadData(ByVal oschema As IOrmObjectSchema, ByVal node As XPathNavigator, ByVal obj As _ICachedEntity) As Boolean
            Dim original_type As Type = obj.GetType
            Dim columns As List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)
            For Each c As ColumnAttribute In columns
                If (_schema.GetAttributes(original_type, c) And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                    Dim attr As String = _schema.GetColumnNameByFieldNameInternal(original_type, c.FieldName, False)
                    Dim n As XPathNavigator = node.Clone
                    Dim nodes As XPathNodeIterator = n.Select(attr)
                    Dim sn As Boolean
                    Do While nodes.MoveNext
                        If sn Then
                            Throw New OrmManagerException(String.Format("Field {0} selects more than one node", attr))
                        End If
                        obj.SetValue(_schema.GetProperty(original_type, c), c, oschema, nodes.Current.Value)
                        obj.SetLoaded(c, True, True)
                        sn = True
                    Loop
                Else
                    obj.SetLoaded(c, True, True)
                End If
            Next
            obj.CheckIsAllLoaded(ObjectSchema, columns.Count)
        End Function
    End Class
End Namespace