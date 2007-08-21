Imports System
Imports System.Configuration.Install
Imports System.Diagnostics
Imports System.ComponentModel

Namespace Orm

    Public Interface IListObjectConverter
        Function ToWeakList(ByVal objects As IEnumerable) As Object
        Function FromWeakList(Of T As {OrmBase, New})(ByVal weak_list As Object, ByVal mgr As OrmManagerBase, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean) As Generic.ICollection(Of T)
        Function Add(ByVal weak_list As Object, ByVal mc As OrmManagerBase, ByVal obj As OrmBase, ByVal sort As Sort) As Boolean
        Sub Delete(ByVal weak_list As Object, ByVal obj As OrmBase)
    End Interface

    Public Class FakeListConverter
        Implements IListObjectConverter

        Public Function FromWeakList(Of T As {OrmBase, New})(ByVal weak_list As Object, ByVal mc As OrmManagerBase, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean) As Generic.ICollection(Of T) Implements IListObjectConverter.FromWeakList
            Dim c As Generic.ICollection(Of T) = CType(weak_list, Generic.ICollection(Of T))
            If withLoad AndAlso Not created Then
                mc.LoadObjects(c, start, length)
            End If
            If start < c.Count Then
                If Not (start = 0 AndAlso length = c.Count) Then
                    length = Math.Min(c.Count, length)
                    Dim ar As Generic.IList(Of T) = TryCast(c, Generic.IList(Of T))
                    Dim l As New Generic.List(Of T)
                    If ar IsNot Nothing Then
                        For i As Integer = start To length - 1
                            l.Add(ar(i))
                        Next
                    Else
                        Dim cnt As Integer = 0, s As Integer = 0
                        For Each o As T In c
                            If cnt >= start Then
                                l.Add(o)
                                s += 1
                            End If
                            If s >= length Then
                                Exit For
                            End If
                        Next
                    End If
                    c = l
                End If
            End If
            Return c
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            Return objects
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManagerBase, ByVal obj As OrmBase, _
            ByVal sort As Sort) As Boolean Implements IListObjectConverter.Add
            Dim l As IList = CType(weak_list, IList)
            Dim st As IOrmSorting = Nothing
            If sort Is Nothing Then
                l.Add(obj)
                Return True
            ElseIf mc.CanSortOnClient(obj.GetType, l, sort, st) Then
                Dim c As IComparer = st.CreateSortComparer(sort)
                If c IsNot Nothing Then
                    Dim pos As Integer = ArrayList.Adapter(l).BinarySearch(obj, c)
                    If pos < 0 Then
                        l.Insert(Not pos, obj)
                    End If
                    Return True
                End If
            End If

            Return False
        End Function

        Public Sub Delete(ByVal weak_list As Object, ByVal obj As OrmBase) Implements IListObjectConverter.Delete
            Dim l As IList = CType(weak_list, IList)
            l.Remove(obj)
        End Sub
    End Class

    Public Class ListConverter
        Implements IListObjectConverter

        Class ListObjectEntry
            Private id As Integer
            Private t As Type
            Private ref As WeakReference

            Public Sub New(ByVal o As OrmBase)
                id = o.Identifier
                t = o.GetType
                ref = New WeakReference(o)
            End Sub

            Public Function GetObject(Of T As {OrmBase, New})(ByVal mc As OrmManagerBase) As T
                Dim o As T = CType(ref.Target, T)
                If o Is Nothing Then
                    o = mc.CreateDBObject(Of T)(id) 'mc.FindObject(id, t)
                    If o Is Nothing AndAlso mc.NewObjectManager IsNot Nothing Then
                        o = CType(mc.NewObjectManager.GetNew(GetType(T), id), T)
                    End If
                End If
                Return o
            End Function

            Public Function GetObject(ByVal mc As OrmManagerBase) As OrmBase
                Dim o As OrmBase = CType(ref.Target, OrmBase)
                If o Is Nothing Then
                    o = mc.CreateDBObject(id, t) 'mc.FindObject(id, t)
                    If o Is Nothing AndAlso mc.NewObjectManager IsNot Nothing Then
                        o = mc.NewObjectManager.GetNew(t, id)
                    End If
                End If
                Return o
            End Function

            Public ReadOnly Property ObjName() As String
                Get
                    Return t.ToString & " - " & id
                End Get
            End Property

            Public ReadOnly Property IsLoaded() As Boolean
                Get
                    Return ref.IsAlive AndAlso CType(ref.Target, OrmBase).IsLoaded
                End Get
            End Property

            Public ReadOnly Property IsEqual(ByVal obj As OrmBase) As Boolean
                Get
                    Return obj.GetType Is t AndAlso obj.Identifier = id
                End Get
            End Property
        End Class

        Public Class ListObject
            Public l As Generic.List(Of ListObjectEntry)
            Public t As Type

            Public Function CanSort(ByVal mc As OrmManagerBase, ByRef arr As ArrayList, ByVal sort As Sort) As Boolean
                If sort.Previous IsNot Nothing Then
                    Return False
                End If

                arr = New ArrayList
                For Each le As ListObjectEntry In l
                    If Not le.IsLoaded Then
                        Return False
                    Else
                        arr.Add(le.GetObject(mc))
                    End If
                Next
                Return True
            End Function

            Sub Remove(ByVal obj As OrmBase)
                For i As Integer = 0 To l.Count - 1
                    Dim le As ListObjectEntry = l(i)
                    If le.IsEqual(obj) Then
                        l.RemoveAt(i)
                        Exit For
                    End If
                Next
            End Sub
        End Class

        Public Function FromWeakList(Of T As {OrmBase, New})(ByVal weak_list As Object, ByVal mc As OrmManagerBase, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean) As Generic.ICollection(Of T) Implements IListObjectConverter.FromWeakList
            If weak_list Is Nothing Then Return Nothing
            Dim lo As ListObject = CType(weak_list, ListObject)
            Dim l As Generic.List(Of ListObjectEntry) = lo.l
            Dim objects As New Generic.List(Of T)
            If start < l.Count Then
                length = Math.Min(length, l.Count)
                For i As Integer = start To length - 1
                    Dim loe As ListObjectEntry = l(i)
                    Dim o As T = loe.GetObject(Of T)(mc)
                    If o IsNot Nothing Then
                        objects.Add(o)
                    Else
                        OrmManagerBase.WriteWarning("Unable to create " & loe.ObjName)
                    End If
                Next
                If withLoad AndAlso Not created Then
                    mc.LoadObjects(objects)
                End If
            End If
            Return objects
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            If objects Is Nothing Then Return Nothing
            Dim l As New Generic.List(Of ListObjectEntry)
            Dim t As Type = Nothing
            For Each o As OrmBase In objects
                If t Is Nothing Then t = o.GetType
                l.Add(New ListObjectEntry(o))
            Next
            Dim lo As New ListObject
            lo.l = l
            lo.t = t
            Return lo
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManagerBase, ByVal obj As OrmBase, _
            ByVal sort As Sort) As Boolean Implements IListObjectConverter.Add
            Dim lo As ListObject = CType(weak_list, ListObject)
            Dim l As Generic.List(Of ListObjectEntry) = lo.l

            If sort Is Nothing Then
                l.Add(New ListObjectEntry(obj))
            Else
                Dim arr As ArrayList = Nothing
                If lo.CanSort(mc, arr, sort) Then
                    Dim schema As IOrmObjectSchemaBase = mc.ObjectSchema.GetObjectSchema(obj.GetType)
                    Dim st As IOrmSorting = TryCast(schema, IOrmSorting)
                    If st IsNot Nothing Then
                        Dim c As IComparer = st.CreateSortComparer(sort)
                        If c IsNot Nothing Then
                            Dim pos As Integer = ArrayList.Adapter(arr).BinarySearch(obj, c)
                            If pos < 0 Then
                                l.Insert(Not pos, New ListObjectEntry(obj))
                            End If
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Public Sub Delete(ByVal weak_list As Object, ByVal obj As OrmBase) Implements IListObjectConverter.Delete
            Dim lo As ListObject = CType(weak_list, ListObject)

            lo.Remove(obj)
        End Sub
    End Class

End Namespace
