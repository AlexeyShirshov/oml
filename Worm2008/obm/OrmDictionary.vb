Imports System.Runtime.CompilerServices
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Entities.Meta
Imports Worm.Criteria.Conditions
Imports Worm.Query

Namespace Entities

    Class myCultureComparer
        Implements IEqualityComparer

        Dim myComparer As CaseInsensitiveComparer

        Public Sub New()
            myComparer = CaseInsensitiveComparer.DefaultInvariant
        End Sub

        Public Sub New(ByVal myCulture As System.Globalization.CultureInfo)
            myComparer = New CaseInsensitiveComparer(myCulture)
        End Sub

        Public Function Equals1(ByVal x As Object, ByVal y As Object) As Boolean Implements IEqualityComparer.Equals
            If (myComparer.Compare(x, y) = 0) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function GetHashCode1(ByVal obj As Object) As Integer Implements IEqualityComparer.GetHashCode
            Return obj.ToString().ToLower().GetHashCode()
        End Function
    End Class


    Public Class DicIndexBase

        Private _max As Integer = 1
        Private _id As Integer
        Private _name As String
        ''' <summary>
        ''' родительский элемент
        ''' </summary>
        Private _parent As DicIndexBase
        ''' <summary>
        ''' количество элементов на данном уровне
        ''' </summary>
        Private _count As Integer

        Private dic As IDictionary
        Private dic_id As IDictionary
        Protected childs As ArrayList
        Protected _filter As IFilter
        Protected _root As DicIndexBase
        Protected _joins() As QueryJoin
        'Protected _complex As Boolean

        'Protected Friend Property Complex() As Boolean
        '    Get
        '        Return _complex
        '    End Get
        '    Set(ByVal value As Boolean)
        '        _complex = value
        '    End Set
        'End Property

        Public Sub New(ByVal name As String, ByVal parent As DicIndexBase, ByVal count As Integer)
            _name = name
            _count = count
            _parent = parent
            If name = "ROOT" Then
                dic = Hashtable.Synchronized(New Hashtable(New myCultureComparer))
                dic_id = Hashtable.Synchronized(New Hashtable)
            End If
        End Sub

        Public ReadOnly Property IsLeaf() As Boolean
            Get
                Return childs Is Nothing
            End Get
        End Property

        ''' <returns>A hash code for the current System.Object.</returns>
        Public Overrides Function GetHashCode() As System.Int32
            Return Name.GetHashCode
        End Function

        Public ReadOnly Property ChildIndexes() As DicIndexBase()
            Get
                If childs Is Nothing Then Return Nothing
                Return CType(childs.ToArray(GetType(DicIndexBase)), DicIndexBase())
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub AddChild(ByVal child As DicIndexBase)
            If childs Is Nothing Then
                childs = New ArrayList
            End If
            childs.Add(child)
        End Sub

        Public ReadOnly Property Name() As String
            Get
                Return _name
            End Get
        End Property

        Public Property ID() As Integer
            Get
                Return _id
            End Get
            Protected Set(ByVal value As Integer)
                _id = value
            End Set
        End Property

        Public ReadOnly Property Count() As Integer
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return _count
            End Get
        End Property

        Public ReadOnly Property TotalCount() As Integer
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Dim count As Integer = 0
                If Not IsLeaf Then
                    Debug.Assert(childs IsNot Nothing)
                    For Each mi As DicIndexBase In childs
                        If mi.IsLeaf Then
                            count += mi.Count
                        Else
                            count += mi.TotalCount
                        End If
                    Next
                End If
                Return count
            End Get
        End Property

        Public ReadOnly Property Parent() As DicIndexBase
            Get
                Return _parent
            End Get
        End Property

        Public ReadOnly Property Dictionary() As IDictionary
            Get
                Return dic
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Add2Dictionary(ByVal item As DicIndexBase)

            If item Is Nothing Then
                Throw New ArgumentNullException("item")
            End If

            dic.Add(item.Name, item)
            dic_id.Add(_max, item)
            item.ID = _max
            item.Root = Me
            _max += 1
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function FindById(ByVal id As Integer) As DicIndexBase
            Return CType(dic_id(id), DicIndexBase)
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(CType(obj, DicIndexBase))
        End Function

        Public Overloads Function Equals(ByVal obj As DicIndexBase) As Boolean
            Return _id = obj._id
        End Function

        'Public Function FindElements(ByVal media As MediaContent, ByVal type As Type) As ObmBase()
        '    Dim strong As Boolean = Not IsLeaf
        '    If Name = " " Then strong = False
        '    If type Is GetType(Artist) Then
        '        Return media.FindArtistsByFirstLetters(Name, strong, ArtistSort.Alphabet, SortType.Asc)
        '    ElseIf type Is GetType(Album) Then
        '        Return media.FindAlbumsByFirstLetters(Name, strong, Root.filter_, AlbumSort.Alphabet, SortType.Asc)
        '    Else
        '        Throw New NotImplementedException()
        '    End If
        'End Function

        Public Property Filter() As IFilter
            Get
                Return _filter
            End Get
            Protected Friend Set(ByVal value As IFilter)
                _filter = value
            End Set
        End Property

        Public Property Root() As DicIndexBase
            Get
                Return _root
            End Get
            Protected Friend Set(ByVal value As DicIndexBase)
                _root = value
            End Set
        End Property

        Public Property Join() As QueryJoin()
            Get
                Return _joins
            End Get
            Protected Friend Set(ByVal value() As QueryJoin)
                _joins = value
            End Set
        End Property
    End Class

    Public Class DicIndex(Of T As {New, IKeyEntity})
        Inherits DicIndexBase

        Private _firstField As String
        Private _secField As String

        'Public Sub New(ByVal name As String, ByVal parent As DicIndex(Of T), ByVal count As Integer)
        '    MyBase.new(name, parent, count)
        'End Sub

        Public Sub New(ByVal name As String, ByVal parent As DicIndex(Of T), ByVal count As Integer, _
            ByVal firstField As String, ByVal secField As String)
            MyBase.new(name, parent, count)
            _firstField = firstField
            _secField = secField
        End Sub

        Public Overloads Function FindElements(ByVal mgr As OrmManager, ByVal sort As Worm.Sorting.Sort) As ReadOnlyList(Of T)
            Return FindElementsInternal(mgr, False, sort)
        End Function

        Public Overloads Function FindElements(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return FindElementsInternal(mgr, False, Nothing)
        End Function

        Public Function FindElementsLoadOnlyNames(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return FindElementsInternal(mgr, True, Nothing)
        End Function

        Private Function FindObjects(ByVal mgr As OrmManager, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal field As String, ByVal sec As String, ByVal sort As Worm.Sorting.Sort) As ReadOnlyList(Of T)

            If String.IsNullOrEmpty(field) Then
                Throw New ArgumentNullException("field")
            End If

            Dim s As ObjectMappingEngine = mgr.MappingEngine
            Dim cr As Criteria.PredicateLink = Nothing
            If strong Then
                cr = New PCtor(New ObjectSource(tt)).prop(field).eq(Name)
                If Not String.IsNullOrEmpty(sec) Then
                    cr.[or](tt, sec).eq(Name)
                End If
            Else
                cr = New PCtor(New ObjectSource(tt)).prop(field).[like](Name & "%")
                If Not String.IsNullOrEmpty(sec) Then
                    cr.[or](tt, sec).[like](Name & "%")
                End If
            End If

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(cr.Filter()).AddFilter(Root.Filter)
            Return mgr.FindWithJoins(Of T)(Nothing, Root.Join, con.Condition, sort, False)
        End Function

        Private Function FindObjects(ByVal mgr As OrmManager, ByVal loadName As Boolean, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal field As String) As ReadOnlyList(Of T)
            If String.IsNullOrEmpty(field) Then
                Throw New ArgumentNullException("field")
            End If

            Dim col As ReadOnlyList(Of T)
            Dim s As ObjectMappingEngine = mgr.MappingEngine
            Dim con As New Condition.ConditionConstructor
            con.AddFilter(Root.Filter)

            If strong Then
                con.AddFilter(New PCtor(New ObjectSource(tt)).prop(field).eq(Name).Filter())
            Else
                con.AddFilter(New PCtor(New ObjectSource(tt)).prop(field).[like](Name & "%").Filter())
            End If

            If loadName Then
                col = mgr.FindWithJoins(Of T)(Nothing, Root.Join, con.Condition, Nothing, True, New String() {field})
            Else
                col = mgr.FindWithJoins(Of T)(Nothing, Root.Join, con.Condition, Nothing, False)
            End If

            Return col
        End Function

        Protected Function FindElementsInternal(ByVal mgr As OrmManager, ByVal loadName As Boolean, ByVal sort As Worm.Sorting.Sort) As ReadOnlyList(Of T)

            Dim strong As Boolean = Not IsLeaf
            If Name = " " Then strong = False
            Dim tt As Type = GetType(T)
            Dim oschema As IObjectSchemaBase = mgr.MappingEngine.GetObjectSchema(tt)
            Dim odic As IOrmDictionary = TryCast(oschema, IOrmDictionary)
            Dim firstField As String = _firstField
            Dim secField As String = _secField
            If odic IsNot Nothing Then
                If String.IsNullOrEmpty(firstField) Then
                    firstField = odic.GetFirstDicField
                End If
                If String.IsNullOrEmpty(secField) Then
                    secField = odic.GetSecondDicField
                End If
            End If

            If sort Is Nothing Then
                Dim col As ReadOnlyList(Of T) = FindObjects(mgr, loadName, strong, tt, firstField)

                If Not String.IsNullOrEmpty(secField) Then
                    Dim col2 As ReadOnlyList(Of T) = FindObjects(mgr, loadName, strong, tt, secField)
                    If col.Count = 0 Then
                        col = col2
                    Else
                        Dim c As New System.Collections.SortedList
                        Dim fname As String = firstField
                        Dim sname As String = secField
                        Dim add As New Hashtable
                        For Each ar As T In col2
                            If col.Contains(ar) Then Continue For
                            Dim fv As String = CStr(mgr.MappingEngine.GetFieldValue(ar, sname, oschema, Nothing))
                            Dim ar2 As T = CType(c(fv), T)
                            If ar2 IsNot Nothing Then
                                If Object.Equals(ar2, ar) Then
                                    Throw New InvalidOperationException("Duplicate object " & fv)
                                Else
                                    'Throw New MediaContentException("Artists with equal names " & ar.ObjName & ", " & ar2.ObjName)
                                    Dim addt As Generic.List(Of T) = CType(add(fv), Generic.List(Of T))
                                    If addt Is Nothing Then
                                        addt = New Generic.List(Of T)
                                        add.Add(fv, addt)
                                    End If
                                    addt.Add(ar)
                                    Continue For
                                End If
                            End If
                            c.Add(fv, ar)
                        Next
                        For Each ar As T In col
                            'Dim fv As String = CStr(mgr.ObjectSchema.GetFieldValue(ar, fname))
                            Dim fv As String = CStr(mgr.MappingEngine.GetFieldValue(ar, fname, oschema, Nothing))
                            Dim ar2 As T = CType(c(fv), T)
                            If ar2 IsNot Nothing Then
                                If Object.Equals(ar2, ar) Then
                                    Continue For
                                Else
                                    'Throw New MediaContentException("Artists with equal names " & ar.ObjName & ", " & ar2.ObjName)
                                    Dim addt As Generic.List(Of T) = CType(add(fv), Generic.List(Of T))
                                    If addt Is Nothing Then
                                        addt = New Generic.List(Of T)
                                        add.Add(fv, addt)
                                    End If
                                    If addt.IndexOf(ar) < 0 Then addt.Add(ar)
                                    Continue For
                                End If
                            End If
                            c.Add(fv, ar)
                        Next
                        Dim result As New ReadOnlyList(Of T)
                        For Each ar As T In c.Values
                            CType(result, IListEdit).Add(ar)
                            'Dim fv As String = CStr(mgr.ObjectSchema.GetFieldValue(ar, fname))
                            Dim fv As String = CStr(mgr.MappingEngine.GetFieldValue(ar, fname, oschema, Nothing))
                            Dim addt As Generic.List(Of T) = CType(add(fv), Generic.List(Of T))
                            If addt IsNot Nothing Then
                                For Each kl As T In addt
                                    CType(result, IListEdit).Add(kl)
                                Next
                            End If
                        Next
                        col = result
                    End If
                End If

                Return col
            Else
                Return FindObjects(mgr, strong, tt, firstField, secField, sort)
            End If
        End Function

        Public Overloads ReadOnly Property Parent() As DicIndex(Of T)
            Get
                Return CType(MyBase.Parent, DicIndex(Of T))
            End Get
        End Property

        Public Overloads ReadOnly Property ChildIndexes() As DicIndex(Of T)()
            Get
                Return CType(childs.ToArray(GetType(DicIndex(Of T))), DicIndex(Of T)())
            End Get
        End Property

        Public Shadows Function FindById(ByVal id As Integer) As DicIndex(Of T)
            Return CType(MyBase.FindById(id), Global.Worm.Entities.DicIndex(Of T))
        End Function
    End Class

End Namespace