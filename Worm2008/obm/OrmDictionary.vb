Imports System.Runtime.CompilerServices
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Entities.Meta
Imports Worm.Criteria.Conditions
Imports Worm.Query
Imports Worm.Criteria
Imports Worm.Query.Sorting
Imports Worm.Expressions2

Namespace Misc

    <Serializable()> _
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

    <Serializable()> _
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
        Protected _childs As ArrayList
        Private _root As DicIndexBase
        'Protected _complex As Boolean

        'Protected Friend Property Complex() As Boolean
        '    Get
        '        Return _complex
        '    End Get
        '    Set(ByVal value As Boolean)
        '        _complex = value
        '    End Set
        'End Property

        Protected Friend Sub New()
        End Sub

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
                Return _childs Is Nothing
            End Get
        End Property

        ''' <returns>A hash code for the current System.Object.</returns>
        Public Overrides Function GetHashCode() As System.Int32
            Return Name.GetHashCode
        End Function

        Public ReadOnly Property ChildIndexes() As DicIndexBase()
            Get
                If _childs Is Nothing Then Return Nothing
                Return CType(_childs.ToArray(GetType(DicIndexBase)), DicIndexBase())
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub AddChild(ByVal child As DicIndexBase)
            If _childs Is Nothing Then
                _childs = New ArrayList
            End If
            _childs.Add(child)
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
                    Debug.Assert(_childs IsNot Nothing)
                    For Each mi As DicIndexBase In _childs
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

        Public Property Root() As DicIndexBase
            Get
                Return _root
            End Get
            Protected Friend Set(ByVal value As DicIndexBase)
                _root = value
            End Set
        End Property

        Friend Shared Sub Init(ByVal d As DicIndexBase, ByVal name As String, ByVal parent As DicIndexBase, ByVal count As Integer)
            d._name = name
            d._parent = parent
            d._count = count
        End Sub
    End Class

    <Serializable()> _
    Public Class DicIndexT(Of T As {New, _IEntity})
        Inherits DicIndexBase

        Private _firstField As String
        Private _secField As String
        Private _cmd As QueryCmd
        Private _getMgr As ICreateManager

        Public Sub New()
        End Sub

        Protected Sub New(ByVal name As String, ByVal parent As DicIndexT(Of T), ByVal count As Integer, _
            ByVal firstField As String, ByVal secField As String)
            MyBase.new(name, parent, count)
            _firstField = firstField
            _secField = secField
        End Sub

        Public Sub New(ByVal name As String, ByVal parent As DicIndexT(Of T), ByVal count As Integer, _
            ByVal firstField As String, ByVal secField As String, ByVal cmd As QueryCmd)
            MyClass.new(name, parent, count, firstField, secField)
            _cmd = cmd
        End Sub

        Friend Shared Shadows Sub Init(ByVal d As DicIndexT(Of T), ByVal name As String, ByVal parent As DicIndexT(Of T), ByVal count As Integer, _
            ByVal firstField As String, ByVal secField As String, ByVal cmd As QueryCmd)
            DicIndexBase.Init(d, name, parent, count)
            d._firstField = firstField
            d._secField = secField
            d._cmd = cmd
        End Sub

        Public ReadOnly Property FirstField() As String
            Get
                Return _firstField
            End Get
        End Property

        Public ReadOnly Property SecondField() As String
            Get
                Return _secField
            End Get
        End Property

        Public ReadOnly Property Cmd() As QueryCmd
            Get
                Return _cmd
            End Get
        End Property

        Public Shared Function CreateRoot(ByVal firstField As String, ByVal secondField As String, ByVal cmd As QueryCmd) As DicIndexT(Of T)
            Return New DicIndexT(Of T)("ROOT", Nothing, 0, firstField, secondField, cmd)
        End Function

        Public Shared Function CreateRoot(ByVal firstField As String, ByVal cmd As QueryCmd) As DicIndexT(Of T)
            Return New DicIndexT(Of T)("ROOT", Nothing, 0, firstField, Nothing, cmd)
        End Function

        Protected Function FindElementsInternal(ByVal mgr As OrmManager, ByVal loadName As Boolean, ByVal sort As OrderByClause) As ReadOnlyObjectList(Of T)

            Dim strong As Boolean = Not IsLeaf
            If Name = " " Then strong = False
            Dim tt As Type = GetType(T)
            Dim oschema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(tt)
            Dim odic As ISupportAlphabet = TryCast(oschema, ISupportAlphabet)
            Dim firstField As String = Me.FirstField
            Dim secField As String = SecondField
            If odic IsNot Nothing Then
                If String.IsNullOrEmpty(firstField) Then
                    firstField = odic.GetFirstDicField
                End If
                If String.IsNullOrEmpty(secField) Then
                    secField = odic.GetSecondDicField
                End If
            End If

            If sort Is Nothing Then
                loadName = Not String.IsNullOrEmpty(secField)

                Dim col As ReadOnlyObjectList(Of T) = FindObjects(mgr, loadName, strong, tt, firstField)

                If Not String.IsNullOrEmpty(secField) Then
                    Dim col2 As ReadOnlyObjectList(Of T) = FindObjects(mgr, loadName, strong, tt, secField)
                    If col.Count = 0 Then
                        col = col2
                    Else
                        Dim c As New System.Collections.SortedList
                        Dim fname As String = firstField
                        Dim sname As String = secField
                        Dim add As New Hashtable
                        For Each ar As T In col2
                            If col.Contains(ar) Then Continue For
                            Dim fv As String = CStr(mgr.MappingEngine.GetPropertyValue(ar, sname, oschema, Nothing))
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
                            Dim fv As String = CStr(mgr.MappingEngine.GetPropertyValue(ar, fname, oschema, Nothing))
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
                        Dim result As ReadOnlyObjectList(Of T) = CType(OrmManager._CreateReadOnlyList(GetType(T)), ReadOnlyObjectList(Of T))
                        For Each ar As T In c.Values
                            CType(result, IListEdit).Add(ar)
                            'Dim fv As String = CStr(mgr.ObjectSchema.GetFieldValue(ar, fname))
                            Dim fv As String = CStr(mgr.MappingEngine.GetPropertyValue(ar, fname, oschema, Nothing))
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

        Protected Overridable Function FindObjects(ByVal mgr As OrmManager, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal firstPropertyAlias As String, _
            ByVal secPropertyAlias As String, ByVal sort As OrderByClause) As ReadOnlyObjectList(Of T)

            Dim cmd As New QueryCmd(_getMgr)

            Dim pp As PredicateLink = Nothing

            cmd.SelectEntity(tt).OrderBy(sort)

            If strong Then
                pp = Ctor.prop(tt, firstPropertyAlias).eq(Name)

                If Not String.IsNullOrEmpty(secPropertyAlias) Then
                    pp.or(tt, secPropertyAlias).eq(Name)
                End If
            Else
                pp = Ctor.prop(tt, firstPropertyAlias).like(Name & "%")

                If Not String.IsNullOrEmpty(secPropertyAlias) Then
                    pp.or(tt, secPropertyAlias).like(Name & "%")
                End If
            End If

            Return cmd.Where(pp.and(_cmd.Filter)).Join(_cmd.Joins).ToObjectList(Of T)()
        End Function

        Protected Overridable Function FindObjects(ByVal mgr As OrmManager, ByVal loadName As Boolean, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal propertyAlias As String) As ReadOnlyObjectList(Of T)

            Dim cmd As New QueryCmd(_getMgr)

            Dim pp As PredicateLink = Nothing

            If loadName Then
                cmd.Select(FCtor.prop(tt, propertyAlias))
            Else
                cmd.SelectEntity(tt)
            End If

            If strong Then
                cmd.Where(Ctor.prop(tt, propertyAlias).eq(Name).and(_cmd.Filter))
            Else
                cmd.Where(Ctor.prop(tt, propertyAlias).like(Name & "%").and(_cmd.Filter))
            End If

            Return cmd.Join(_cmd.Joins).ToObjectList(Of T)()
        End Function

        Public Overloads ReadOnly Property Parent() As DicIndexT(Of T)
            Get
                Return CType(MyBase.Parent, DicIndexT(Of T))
            End Get
        End Property

        Public Overloads ReadOnly Property ChildIndexes() As DicIndexT(Of T)()
            Get
                If _childs Is Nothing Then
                    Return New DicIndexT(Of T)() {}
                Else
                    Return CType(_childs.ToArray(GetType(DicIndexT(Of T))), DicIndexT(Of T)())
                End If
            End Get
        End Property

        Public Shadows Function FindById(ByVal id As Integer) As DicIndexT(Of T)
            Return CType(MyBase.FindById(id), DicIndexT(Of T))
        End Function

        Public Overloads Function FindElements(ByVal sort As OrderByClause) As ReadOnlyObjectList(Of T)
            Dim icm As ICreateManager = _cmd.CreateManager
            If icm Is Nothing Then
                Throw New ArgumentException("OrmManager required")
            End If

            Return FindElements(icm, sort)
        End Function

        Public Overloads Function FindElements() As ReadOnlyObjectList(Of T)
            Dim icm As ICreateManager = _cmd.CreateManager
            If icm Is Nothing Then
                Throw New ArgumentException("OrmManager required")
            End If

            Return FindElements(icm)
        End Function

        Public Function FindElementsLoadOnlyNames() As ReadOnlyObjectList(Of T)
            Dim icm As ICreateManager = _cmd.CreateManager
            If icm Is Nothing Then
                Throw New ArgumentException("OrmManager required")
            End If

            Return FindElementsLoadOnlyNames(icm)
        End Function

        Public Overloads Function FindElements(ByVal getMgr As ICreateManager, ByVal sort As OrderByClause) As ReadOnlyObjectList(Of T)
            _getMgr = getMgr
            Using mgr As OrmManager = getMgr.CreateManager
                Return FindElementsInternal(mgr, False, sort)
            End Using
        End Function

        Public Overloads Function FindElements(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of T)
            _getMgr = getMgr
            Using mgr As OrmManager = getMgr.CreateManager
                Return FindElementsInternal(mgr, False, Nothing)
            End Using
        End Function

        Public Function FindElementsLoadOnlyNames(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of T)
            _getMgr = getMgr
            Using mgr As OrmManager = getMgr.CreateManager
                Return FindElementsInternal(mgr, True, Nothing)
            End Using
        End Function
    End Class

    <Serializable()> _
    Public Class DicIndex(Of T As {New, IKeyEntity})
        Inherits DicIndexT(Of T)

        Protected _filter As IFilter
        Protected _joins() As QueryJoin

        Public Property Filter() As IFilter
            Get
                Return _filter
            End Get
            Protected Friend Set(ByVal value As IFilter)
                _filter = value
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

        Protected Overloads ReadOnly Property Root() As DicIndex(Of T)
            Get
                Return CType(MyBase.Root, DicIndex(Of T))
            End Get
        End Property

        Public Overloads ReadOnly Property ChildIndexes() As DicIndex(Of T)()
            Get
                Return CType(_childs.ToArray(GetType(DicIndex(Of T))), DicIndex(Of T)())
            End Get
        End Property

        Public Sub New()
        End Sub

        Public Sub New(ByVal name As String, ByVal parent As DicIndex(Of T), ByVal count As Integer, _
            ByVal firstField As String, ByVal secField As String)
            MyBase.New(name, parent, count, firstField, secField)
        End Sub

        Public Overloads Function FindElements(ByVal mgr As OrmManager, ByVal sort As OrderByClause) As ReadOnlyList(Of T)
            Return CType(FindElementsInternal(mgr, False, sort), Global.Worm.ReadOnlyList(Of T))
        End Function

        Public Overloads Function FindElements(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return CType(FindElementsInternal(mgr, False, Nothing), Global.Worm.ReadOnlyList(Of T))
        End Function

        Public Overloads Function FindElementsLoadOnlyNames(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return CType(FindElementsInternal(mgr, True, Nothing), Global.Worm.ReadOnlyList(Of T))
        End Function

#If Not ExcludeFindMethods Then
        Protected Overrides Function FindObjects(ByVal mgr As OrmManager, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal field As String, ByVal sec As String, ByVal sort As Sort) As ReadOnlyObjectList(Of T)

            If String.IsNullOrEmpty(field) Then
                Throw New ArgumentNullException("field")
            End If

            Dim s As ObjectMappingEngine = mgr.MappingEngine
            Dim cr As Criteria.PredicateLink = Nothing
            If strong Then
                cr = New Ctor(New EntityUnion(tt)).prop(field).eq(Name)
                If Not String.IsNullOrEmpty(sec) Then
                    cr.[or](tt, sec).eq(Name)
                End If
            Else
                cr = New Ctor(New EntityUnion(tt)).prop(field).[like](Name & "%")
                If Not String.IsNullOrEmpty(sec) Then
                    cr.[or](tt, sec).[like](Name & "%")
                End If
            End If
            cr.and(Root.Filter)
            Return mgr.FindWithJoins(Of T)(Nothing, Root.Join, cr, sort, False)
        End Function

        Protected Overrides Function FindObjects(ByVal mgr As OrmManager, ByVal loadName As Boolean, _
            ByVal strong As Boolean, ByVal tt As Type, ByVal field As String) As ReadOnlyObjectList(Of T)
            If String.IsNullOrEmpty(field) Then
                Throw New ArgumentNullException("field")
            End If

            Dim col As ReadOnlyList(Of T)
            Dim s As ObjectMappingEngine = mgr.MappingEngine
            Dim con As New Condition.ConditionConstructor
            con.AddFilter(Root.Filter)

            If strong Then
                con.AddFilter(New Ctor(New EntityUnion(tt)).prop(field).eq(Name).Filter())
            Else
                con.AddFilter(New Ctor(New EntityUnion(tt)).prop(field).[like](Name & "%").Filter())
            End If

            If loadName Then
                col = mgr.FindWithJoins(Of T)(Nothing, Root.Join, con.Condition, Nothing, True, New String() {field})
            Else
                col = mgr.FindWithJoins(Of T)(Nothing, Root.Join, con.Condition, Nothing, False)
            End If

            Return col
        End Function
#End If

        Public Shared Shadows Function CreateRoot(ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)
            Return New DicIndex(Of T)("ROOT", Nothing, 0, firstField, secondField)
        End Function

        Public Shared Shadows Function CreateRoot(ByVal firstField As String) As DicIndex(Of T)
            Return New DicIndex(Of T)("ROOT", Nothing, 0, firstField, Nothing)
        End Function
    End Class

End Namespace