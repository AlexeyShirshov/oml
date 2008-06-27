Imports System.Collections.Generic
Imports Worm.Orm

Namespace Cache
    Public Class ReadonlyCache

        Public ReadOnly DateTimeCreated As Date

        Protected _filters As IDictionary
        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        Private _m2m As New Dictionary(Of Integer, List(Of EditableListBase))

        Public Event RegisterEntityCreation(ByVal e As IEntity)
        Public Event RegisterObjectCreation(ByVal t As Type, ByVal id As Integer)

        Sub New()
            _filters = Hashtable.Synchronized(New Hashtable)
            DateTimeCreated = Now
        End Sub

        Public Function GetM2M(ByVal o As OrmBase) As List(Of EditableListBase)
            Dim l As New List(Of EditableListBase)
            Dim l2 As List(Of EditableListBase) = Nothing
            If _m2m.TryGetValue(o.Key, l2) Then
                For Each el As EditableListBase In l2
                    If el.MainType Is o.GetType Then
                        l.Add(el)
                    End If
                Next
            End If
            Return l
        End Function

        Public Function GetM2M(ByVal o As OrmBase, ByVal subType As Type, ByVal key As String) As EditableListBase
            For Each el As EditableListBase In GetM2M(o)
                If el.SubType Is subType AndAlso el.Key = key Then
                    Return el
                End If
            Next
            Return Nothing
        End Function

        Public Overridable Sub RegisterCreation(ByVal obj As IEntity)
            RaiseEvent RegisterEntityCreation(obj)
        End Sub

        Public Overridable Sub RegisterCreation(ByVal t As Type, ByVal id As Integer)
            RaiseEvent RegisterObjectCreation(t, id)
#If TraceCreation Then
            _added.add(new Pair(Of date,Pair(Of type,Integer))(Now,New Pair(Of type,Integer)(t,id)))
#End If
        End Sub

    End Class
End Namespace