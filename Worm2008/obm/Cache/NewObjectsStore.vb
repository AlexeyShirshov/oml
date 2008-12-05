Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic

Namespace Cache
    Public Interface INewObjectsStore
        Function GetPKForNewObject(ByVal t As Type) As PKDesc()
        Function GetNew(ByVal t As Type, ByVal pk() As PKDesc) As _ICachedEntity
        Sub AddNew(ByVal obj As _ICachedEntity)
        Sub RemoveNew(ByVal obj As _ICachedEntity)
        Sub RemoveNew(ByVal t As Type, ByVal pk() As PKDesc)
    End Interface

    Public Interface INewObjectsStoreEx
        Inherits INewObjectsStore
        Overloads Function GetNewObjects(ByVal t As Type) As IList(Of _ICachedEntity)
        Overloads Function GetNewObjects(Of T As _ICachedEntity)() As IList(Of T)
    End Interface

    Public Class NewObjectStore
        Implements INewObjectsStoreEx

        Private _syncObj As New Object
        Protected ReadOnly Property SyncObj() As Object
            Get
                Return _syncObj
            End Get
        End Property

        Private _id As Integer = -100
        Private Function GetNext() As Integer
            SyncLock _syncObj
                Dim i As Integer = _id
                _id -= 1
                Return i
            End SyncLock
        End Function

        Private _dic As IDictionary(Of Type, IDictionary(Of PKWrapper, _ICachedEntity))
        Private Function GetDic(ByVal t As Type) As IDictionary(Of PKWrapper, _ICachedEntity)
            SyncLock _syncObj
                If _dic Is Nothing Then
                    _dic = New Dictionary(Of Type, IDictionary(Of PKWrapper, _ICachedEntity))
                End If
                Dim d As IDictionary(Of PKWrapper, _ICachedEntity) = Nothing
                If Not _dic.TryGetValue(t, d) Then
                    d = New Dictionary(Of PKWrapper, _ICachedEntity)
                    _dic.Add(t, d)
                End If
                Return d
            End SyncLock
        End Function

        Public Sub AddNew(ByVal obj As Entities._ICachedEntity) Implements INewObjectsStore.AddNew
            Dim pk As New PKWrapper(obj.GetPKValues)
            GetDic(obj.GetType).Add(pk, obj)
        End Sub

        Public Overloads Function GetNew(ByVal t As System.Type, ByVal pk() As Entities.Meta.PKDesc) As Entities._ICachedEntity Implements INewObjectsStore.GetNew
            Dim pks As New PKWrapper(pk)
            Dim o As _ICachedEntity = Nothing
            GetDic(t).TryGetValue(pks, o)
            Return o
        End Function

        Public Function GetPKForNewObject(ByVal t As System.Type) As Entities.Meta.PKDesc() Implements INewObjectsStore.GetPKForNewObject
            Dim dic As IDictionary = ObjectMappingEngine.GetMappedProperties(t)
            Dim l As New List(Of PKDesc)
            For Each de As DictionaryEntry In dic
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                If (c._behavior And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    Dim id As Object = GetPKValue(t, c.PropertyAlias, pi.PropertyType, pi.DeclaringType)
                    l.Add(New PKDesc(c.PropertyAlias, id))
                End If
            Next
            Return l.ToArray
        End Function

        Protected Overridable Function GetPKValue(ByVal objectType As Type, ByVal propertyAlias As String, _
            ByVal propertyType As Type, ByVal declaringType As Type) As Object

            If declaringType.Name = GetType(OrmBaseT(Of )).Name AndAlso propertyAlias = OrmBaseT.PKName Then
                Return GetNext()
            End If

            If propertyType Is GetType(Integer) Then
                Return GetNext()
            ElseIf propertyType Is GetType(Short) Then
                Return GetNext() Mod Short.MaxValue
            ElseIf propertyType Is GetType(Byte) Then
                Return GetNext() Mod Byte.MaxValue
            ElseIf propertyType Is GetType(Guid) Then
                Return Guid.NewGuid
            ElseIf propertyType Is GetType(String) Then
                Return Guid.NewGuid.ToString
            ElseIf propertyType Is GetType(Long) Then
                Return GetNext()
            Else
                Throw New NotSupportedException(propertyType.ToString)
            End If
        End Function

        Public Sub RemoveNew(ByVal t As System.Type, ByVal pk() As Entities.Meta.PKDesc) Implements INewObjectsStore.RemoveNew
            GetDic(t).Remove(New PKWrapper(pk))
        End Sub

        Public Sub RemoveNew(ByVal obj As Entities._ICachedEntity) Implements INewObjectsStore.RemoveNew
            RemoveNew(obj.GetType, obj.GetPKValues)
        End Sub

        Public Overloads Function GetNew(ByVal t As System.Type) As System.Collections.Generic.IList(Of Entities._ICachedEntity) Implements INewObjectsStoreEx.GetNewObjects
            Return New List(Of _ICachedEntity)(GetDic(t).Values)
        End Function

        Public Overloads Function GetNew(Of T As Entities._ICachedEntity)() As System.Collections.Generic.IList(Of T) Implements INewObjectsStoreEx.GetNewObjects
            Dim l As New List(Of T)()
            For Each o As T In GetDic(GetType(T)).Values
                l.Add(o)
            Next
            Return l
        End Function
    End Class
End Namespace