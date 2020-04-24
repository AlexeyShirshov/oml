Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic
Imports System.Linq

Namespace Cache
    ''' <summary>
    ''' Интерфейс работы с новыми объектами
    ''' </summary>
    ''' <remarks>Так как новые объект не хранятся в кеше, система работает с ними с помощью внешнего 
    ''' хранилища, реализующего данный интерфейс</remarks>
    Public Interface INewObjectsStore
        ''' <summary>
        ''' Возвращает набор полей и значений первичного ключа
        ''' </summary>
        ''' <param name="t">Тип объекта</param>
        ''' <param name="mpe">Движок маппинга</param>
        ''' <returns>Набор полей и значений первичного ключа</returns>
        ''' <exception cref="ArgumentNullException">Если t или mpe пустая ссылка</exception>
        Function GetPKForNewObject(ByVal t As Type, ByVal mpe As ObjectMappingEngine) As IPKDesc
        ''' <summary>
        ''' Возвращает экземпляр нового объекта данного типа по первичному ключу
        ''' </summary>
        ''' <param name="t">Тип объекта</param>
        ''' <param name="pk">Набор полей и значений первичного ключа</param>
        ''' <returns>Экземпляр нового объекта данного типа по первичному ключу или Nothing, 
        ''' если не найден</returns>
        ''' <exception cref="ArgumentNullException">Если t или pk пустая ссылка</exception>
        Function GetNew(ByVal t As Type, ByVal pk As IPKDesc) As _ICachedEntity
        ''' <summary>
        ''' Добавляет объект в хранилище новых объектов
        ''' </summary>
        ''' <param name="obj">Объект</param>
        ''' <exception cref="ArgumentException">Если объект уже добавлен</exception>
        ''' <exception cref="ArgumentNullException">Если obj пустая ссылка</exception>
        Sub AddNew(ByVal obj As _ICachedEntity)
        ''' <summary>
        ''' Удаляет объект из хранилища новых объектов
        ''' </summary>
        ''' <param name="obj">Объект</param>
        ''' <exception cref="ArgumentNullException">Если obj пустая ссылка</exception>
        Sub RemoveNew(ByVal obj As _ICachedEntity)
        ''' <summary>
        ''' Удаляет объект из хранилища новых объектов
        ''' </summary>
        ''' <param name="t">Тип объекта</param>
        ''' <param name="pk">Набор полей и значений первичного ключа</param>
        ''' <exception cref="ArgumentNullException">Если t или pk пустая ссылка</exception>
        Sub RemoveNew(ByVal t As Type, ByVal pk As IPKDesc)
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
                    _dic = New Concurrent.ConcurrentDictionary(Of Type, IDictionary(Of PKWrapper, _ICachedEntity))
                End If
                Dim d As IDictionary(Of PKWrapper, _ICachedEntity) = Nothing
                If Not _dic.TryGetValue(t, d) Then
                    d = New Concurrent.ConcurrentDictionary(Of PKWrapper, _ICachedEntity)
                    _dic.Add(t, d)
                End If
                Return d
            End SyncLock
        End Function

        Public Sub AddNew(ByVal obj As Entities._ICachedEntity) Implements INewObjectsStore.AddNew
            Dim pk As New PKWrapper(obj.GetPKValues(Nothing))
            GetDic(obj.GetType).Add(pk, obj)
        End Sub

        Public Overloads Function GetNew(ByVal t As System.Type, ByVal pk As IPKDesc) As Entities._ICachedEntity Implements INewObjectsStore.GetNew
            Dim pks As New PKWrapper(pk)
            Dim o As _ICachedEntity = Nothing
            GetDic(t).TryGetValue(pks, o)
            Return o
        End Function

        ''' <summary>
        ''' Возвращает набор полей и значений первичного ключа
        ''' </summary>
        ''' <param name="t">Тип объекта</param>
        ''' <param name="mpe">Движок маппинга</param>
        ''' <returns>Набор полей и значений первичного ключа</returns>
        ''' <exception cref="ArgumentNullException">Если t или mpe пустая ссылка</exception>
        Public Function GetPKForNewObject(ByVal t As System.Type, ByVal mpe As ObjectMappingEngine) As IPKDesc Implements INewObjectsStore.GetPKForNewObject
            If mpe Is Nothing Then
                Throw New ArgumentNullException("mpe")
            End If

            Dim l As New PKDesc
            Dim pk = mpe.GetEntitySchema(t).GetPK
            If pk.SourceFields.Count = 1 Then
                Dim id As Object = GetPKValue(t, pk.PropertyAlias, pk.PropertyType, pk.PropertyInfo.DeclaringType)
                l.Add(New ColumnValue(pk.SourceFields(0).SourceFieldExpression, id))
            Else
                For Each sf In pk.SourceFields
                    Dim id As Object = GetPKValue(t, pk.PropertyAlias, sf.PropertyInfo.PropertyType, sf.PropertyInfo.DeclaringType)
                    l.Add(New ColumnValue(sf.SourceFieldExpression, id))
                Next
            End If
            Return l
        End Function

        Protected Overridable Function GetPKValue(ByVal objectType As Type, ByVal propertyAlias As String, _
            ByVal propertyType As Type, ByVal declaringType As Type) As Object

            'If declaringType.Name = GetType(OrmBaseT(Of )).Name AndAlso propertyAlias = OrmBaseT.PKName Then
            '    Return GetNext()
            'End If

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

        Public Sub RemoveNew(ByVal t As System.Type, ByVal pk As IPKDesc) Implements INewObjectsStore.RemoveNew
            GetDic(t).Remove(New PKWrapper(pk))
        End Sub

        Public Sub RemoveNew(ByVal obj As Entities._ICachedEntity) Implements INewObjectsStore.RemoveNew
            RemoveNew(obj.GetType, obj.GetPKValues(Nothing))
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