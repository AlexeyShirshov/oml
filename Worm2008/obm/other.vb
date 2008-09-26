Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Data
Imports Worm.Criteria.Core

''' <summary>
''' Модуль небольших функций для внутреннего использования по всему солюшену
''' </summary>
''' <remarks></remarks>
Public Module helper

    Public Sub WriteInfo(ByVal _tsStmt As TraceSource, ByVal str As String)
        If _tsStmt.Switch.ShouldTrace(TraceEventType.Information) Then
            Try
                For Each l As TraceListener In _tsStmt.Listeners
                    l.Write(str)
                    If Trace.AutoFlush Then _tsStmt.Flush()
                Next
            Catch ex As InvalidOperationException
            End Try
        End If
    End Sub

    Public Sub WriteLineInfo(ByVal _tsStmt As TraceSource, ByVal str As String)
        If _tsStmt.Switch.ShouldTrace(TraceEventType.Information) Then
            Try
                For Each l As TraceListener In _tsStmt.Listeners
                    l.WriteLine(str)
                    If Trace.AutoFlush Then _tsStmt.Flush()
                Next
            Catch ex As InvalidOperationException
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Метод определяет нужно ли добавлять псевдоним таблицы для поля в БД
    ''' </summary>
    ''' <param name="str">Название поля в БД</param>
    ''' <returns><b>true</b> если псевдоним необходим. В противном случае <b>false</b></returns>
    ''' <remarks>Для вычисляемых полей или скалярных подзапросов префикс (псевдоним) таблицы не нужен.</remarks>
    Public Function ShouldPrefix(ByVal str As String) As Boolean
        If str IsNot Nothing Then
            Dim pos As Integer = str.IndexOf("select ")
            If pos = -1 Then pos = str.IndexOf("case ")
            Return pos = -1
        Else
            Return True
        End If
    End Function

    ''' <summary>
    ''' Метод используется для подсчета кол-ва безымяных параметров в выражении
    ''' </summary>
    ''' <param name="stmt">Вырежение</param>
    ''' <returns>Кол-во безымянных параметров</returns>
    ''' <remarks></remarks>
    Public Function ExtractParamsCount(ByVal stmt As String) As Integer
        Dim pos As Integer = 0
        Dim cnt As Integer = 0

        If stmt IsNot Nothing Then

            Do
                pos = stmt.IndexOf("?", pos)
                If pos >= 0 Then
                    cnt += 1
                Else
                    Exit Do
                End If

                pos += 1
            Loop While True
        End If

        Return cnt
    End Function

    ''' <summary>
    ''' Сортирует словарь в соответствии с порядком ключей в коллекции
    ''' </summary>
    ''' <typeparam name="TKey">Ключ словаря</typeparam>
    ''' <typeparam name="TValue">Значение словаря</typeparam>
    ''' <param name="dic">Словарь</param>
    ''' <param name="model">Упорядоченная коллекция ключей</param>
    ''' <returns>Список пар ключ/значение из словаря, упорядоченный по коллекции <b>model</b></returns>
    ''' <exception cref="InvalidOperationException">Если ключ из словаря не найден в коллекции <b>model</b></exception>
    Public Function Sort(Of TKey, TValue)(ByVal dic As IDictionary(Of TKey, TValue), ByVal model() As TKey) As List(Of Pair(Of TKey, TValue))
        Dim l As New List(Of Pair(Of TKey, TValue))

        If dic IsNot Nothing Then
            Dim arr(model.Length - 1) As TValue
            For Each de As KeyValuePair(Of TKey, TValue) In dic

                Dim idx As Integer = Array.IndexOf(model, de.Key)

                If idx < 0 Then
                    Throw New InvalidOperationException("Unknown key " + Convert.ToString(de.Key))
                End If

                arr(idx) = de.Value
            Next

            For i As Integer = 0 To dic.Count - 1
                l.Add(New Pair(Of TKey, TValue)(model(i), arr(i)))
            Next
        End If

        Return l
    End Function

    ''' <summary>
    ''' Сравнение массива байт
    ''' </summary>
    ''' <param name="arr1">Первый массив</param>
    ''' <param name="arr2">Второй массив</param>
    ''' <returns><b>true</b> если массивы идентичны</returns>
    ''' <remarks></remarks>
    Public Function IsEqualByteArray(ByVal arr1() As Byte, ByVal arr2() As Byte) As Boolean
        If arr1 Is Nothing AndAlso arr2 Is Nothing Then
            Return True
        End If

        If (arr1 Is Nothing AndAlso arr2 IsNot Nothing) _
            OrElse (arr2 Is Nothing AndAlso arr1 IsNot Nothing) Then
            Return False
        End If

        If arr1.Length <> arr2.Length Then
            Return False
        End If

        For i As Integer = 0 To arr1.Length - 1
            Dim b1 As Byte = arr1(i)
            Dim b2 As Byte = arr2(i)
            If b1 <> b2 Then
                Return False
            End If
        Next

        Return True
    End Function

    ''' <summary>
    ''' Класс представляет собой результат склейки коллекции чисел
    ''' </summary>
    ''' <remarks></remarks>
    Public Class MergeResult
        Private _pairs As ICollection(Of Pair(Of Integer))
        Private _rest As ICollection(Of Integer)

        ''' <summary>
        ''' Конструктор класса
        ''' </summary>
        ''' <param name="pairs">Коллекция диапазонов чисел (от <see cref="Pair(Of Integer).First"/> до <see cref="Pair(Of Integer).Second"/>)</param>
        ''' <param name="rest">Остаток (числа сами по себе)</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal pairs As ICollection(Of Pair(Of Integer)), ByVal rest As ICollection(Of Integer))
            _pairs = pairs
            _rest = rest
        End Sub

        ''' <summary>
        ''' Диапазон чисел
        ''' </summary>
        ''' <returns>Коллекция диапазонов чисел (от <see cref="Pair(Of Integer).First"/> до <see cref="Pair(Of Integer).Second"/>)</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Pairs() As ICollection(Of Pair(Of Integer))
            Get
                Return _pairs
            End Get
        End Property

        ''' <summary>
        ''' Остаток
        ''' </summary>
        ''' <returns>Коллекция чисел</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Rest() As ICollection(Of Integer)
            Get
                Return _rest
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Cклейка коллекции чисел для оптимизации запросов
    ''' </summary>
    ''' <param name="ids">Коллекция чисел</param>
    ''' <param name="sort"><b>true</b> если коллекция <b>ids</b> уже упорядочена</param>
    ''' <returns>Экземпляр типа <see cref="MergeResult"/></returns>
    ''' <remarks>Метод выполняет оптимизацию коллекции чисел для уменьшения размер строки.
    ''' Используется для оптимизации условий в условии in (...). Например, вместо
    ''' in (1,2,3,4,5,6,7) получается between 1 and 7
    ''' </remarks>
    Public Function MergeIds(ByVal ids As Generic.List(Of Integer), ByVal sort As Boolean) As MergeResult
        If ids Is Nothing OrElse ids.Count = 0 Then
            Return Nothing
        End If

        If ids.Count = 1 Then
            Return New MergeResult(New Generic.List(Of Pair(Of Integer)), ids)
        End If

        If sort Then
            ids.Sort()
        End If

        Dim pairs As New Generic.List(Of Pair(Of Integer))
        Dim rest As New Generic.List(Of Integer)
        Dim start As Integer = 0
        For i As Integer = 1 To ids.Count - 1
            Dim d As Integer = ids(i) - ids(i - 1)
            If d = 1 Then
                Continue For
            ElseIf d > 1 Then
                If i - start > 1 Then
                    Dim p As New Pair(Of Integer)(ids(start), ids(i - 1))
                    pairs.Add(p)
                Else
                    rest.Add(ids(start))
                End If
                start = i
            ElseIf d = 0 Then
                Throw New ArgumentException(String.Format("Collection of integer countans duplicates of {0} at {1}", ids(i), i))
            ElseIf d < 0 Then
                Throw New ArgumentException(String.Format("Collection of integer is not sorted at {0} and {1}", ids(i - 1), ids(i)))
            End If
        Next

        If start < ids.Count - 1 Then
            Dim p As New Pair(Of Integer)(ids(start), ids(ids.Count - 1))
            pairs.Add(p)
        Else
            rest.Add(ids(start))
        End If

        Return New MergeResult(pairs, rest)
    End Function

End Module

Public Class ObjectWrap(Of T)
    Protected _o As T

    ''' <summary>
    ''' Конструктор
    ''' </summary>
    ''' <param name="o">Экземпляр типа</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal o As T)
        _o = o
    End Sub

    ''' <summary>
    ''' Экземпляр типа
    ''' </summary>
    ''' <value>Устанавливаемое значение</value>
    ''' <returns>Установленое значение</returns>
    ''' <remarks></remarks>
    Public Property Value() As T
        Get
            Return _o
        End Get
        Set(ByVal value As T)
            _o = value
        End Set
    End Property
End Class

''' <summary>
''' Обертка над типом
''' </summary>
''' <typeparam name="T">Тип</typeparam>
''' <remarks>Необходима для устранения операций неявного приведения типов</remarks>
Public Class TypeWrap(Of T)
    Inherits ObjectWrap(Of T)
    'Private _o As T

    ''' <summary>
    ''' Конструктор
    ''' </summary>
    ''' <param name="o">Экземпляр типа</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal o As T)
        MyBase.New(o)
    End Sub

    '''' <summary>
    '''' Экземпляр типа
    '''' </summary>
    '''' <value>Устанавливаемое значение</value>
    '''' <returns>Установленое значение</returns>
    '''' <remarks></remarks>
    'Public Property Value() As T
    '    Get
    '        Return _o
    '    End Get
    '    Set(ByVal value As T)
    '        _o = value
    '    End Set
    'End Property

    ''' <summary>
    ''' Определение равенства объектов
    ''' </summary>
    ''' <param name="obj">Объект</param>
    ''' <returns><b>true</b> если объекты равны</returns>
    ''' <remarks></remarks>
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        Dim tw As TypeWrap(Of T) = TryCast(obj, TypeWrap(Of T))
        Return Equals(tw)
    End Function

    ''' <summary>
    ''' Типизированое определение равенства объектов
    ''' </summary>
    ''' <param name="obj">Объект</param>
    ''' <returns><b>true</b> если объекты равны</returns>
    ''' <remarks>Операция сравнение с типом Т дает <b>false</b></remarks>
    Public Overloads Function Equals(ByVal obj As TypeWrap(Of T)) As Boolean
        If obj IsNot Nothing Then
            Return Object.Equals(_o, obj._o)
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Преобразование типа в строку
    ''' </summary>
    ''' <returns>Строка</returns>
    ''' <remarks>Делегирует вызов внутренему объекту</remarks>
    Public Overrides Function ToString() As String
        If _o IsNot Nothing Then
            Return _o.ToString
        Else
            Return String.Empty
        End If
    End Function

    ''' <summary>
    ''' Преобразование в число
    ''' </summary>
    ''' <returns>Число</returns>
    ''' <remarks>Делегирует вызов внутренему объекту</remarks>
    Public Overrides Function GetHashCode() As Integer
        If _o IsNot Nothing Then
            Return _o.GetHashCode
        Else
            Return 1
        End If
    End Function
End Class

''' <summary>
''' Класс, повзволяющий точно замерять промежутки времени
''' </summary>
''' <remarks></remarks>
Public Class PerfCounter
    Private _start As Long

    ''' <summary>
    ''' The QueryPerformanceCounter function retrieves the current value of the high-resolution performance counter
    ''' </summary>
    ''' <param name="X">Variable that receives the current performance-counter value, in counts</param>
    ''' <returns>If the function succeeds, the return value is <b>true</b></returns>
    ''' <remarks>Делегация системному вызову</remarks>
    Declare Function QueryPerformanceCounter Lib "Kernel32" (ByRef X As Long) As Boolean
    ''' <summary>
    ''' The QueryPerformanceFrequency function retrieves the frequency of the high-resolution performance counter, if one exists. The frequency cannot change while the system is running
    ''' </summary>
    ''' <param name="X">variable that receives the current performance-counter frequency, in counts per second. If the installed hardware does not support a high-resolution performance counter, this parameter can be zero.</param>
    ''' <returns>If the function succeeds, the return value is <b>true</b></returns>
    ''' <remarks>Делегация системному вызову</remarks>
    Declare Function QueryPerformanceFrequency Lib "Kernel32" (ByRef X As Long) As Boolean

    ''' <summary>
    ''' Констуктор
    ''' </summary>
    ''' <remarks>Начала отсчета</remarks>
    Public Sub New()
        QueryPerformanceCounter(_start)
    End Sub

    ''' <summary>
    ''' Функция окончания отсчета времени
    ''' </summary>
    ''' <returns>Временой промежуток прошедщий с момента создания данного экземпляра</returns>
    ''' <remarks></remarks>
    Public Function GetTime() As TimeSpan
        Dim [end] As Long
        QueryPerformanceCounter([end])
        Dim f As Long
        QueryPerformanceFrequency(f)
        Return TimeSpan.FromSeconds(([end] - _start) / f)
    End Function
End Class

Public NotInheritable Class DbTypeConvertor
    ' Methods
    Shared Sub New()
        Dim dbTypeMapEntry As New DbTypeMapEntry(GetType(Boolean), DbType.Boolean, SqlDbType.Bit)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Byte), DbType.Double, SqlDbType.TinyInt)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Byte()), DbType.Binary, SqlDbType.Image)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(DateTime), DbType.DateTime, SqlDbType.DateTime)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Decimal), DbType.Decimal, SqlDbType.Decimal)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Double), DbType.Double, SqlDbType.Float)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Guid), DbType.Guid, SqlDbType.UniqueIdentifier)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Short), DbType.Int16, SqlDbType.SmallInt)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Integer), DbType.Int32, SqlDbType.Int)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Long), DbType.Int64, SqlDbType.BigInt)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Object), DbType.Object, SqlDbType.Variant)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(String), DbType.String, SqlDbType.VarChar)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Byte), DbType.Byte, SqlDbType.VarBinary)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
        dbTypeMapEntry = New DbTypeMapEntry(GetType(Single), DbType.Single, SqlDbType.Real)
        DbTypeConvertor._DbTypeList.Add(dbTypeMapEntry)
    End Sub

    Private Sub New()
    End Sub

    Private Shared Function Find(ByVal dbType As DbType) As DbTypeMapEntry
        Dim retObj As Object = Nothing
        Dim i As Integer
        For i = 0 To DbTypeConvertor._DbTypeList.Count - 1
            Dim entry As DbTypeMapEntry = DirectCast(DbTypeConvertor._DbTypeList.Item(i), DbTypeMapEntry)
            If (entry.DbType = dbType) Then
                retObj = entry
                Exit For
            End If
        Next i
        If (retObj Is Nothing) Then
            Throw New ApplicationException("Referenced an unsupported DbType " & dbType.ToString)
        End If
        Return DirectCast(retObj, DbTypeMapEntry)
    End Function

    Private Shared Function Find(ByVal sqlDbType As SqlDbType) As DbTypeMapEntry
        Dim retObj As Object = Nothing
        Dim i As Integer
        For i = 0 To DbTypeConvertor._DbTypeList.Count - 1
            Dim entry As DbTypeMapEntry = DirectCast(DbTypeConvertor._DbTypeList.Item(i), DbTypeMapEntry)
            If (entry.SqlDbType = sqlDbType) Then
                retObj = entry
                Exit For
            End If
        Next i
        If (retObj Is Nothing) Then
            Throw New ApplicationException("Referenced an unsupported SqlDbType")
        End If
        Return DirectCast(retObj, DbTypeMapEntry)
    End Function

    Private Shared Function Find(ByVal type As Type) As DbTypeMapEntry
        Dim retObj As Object = Nothing
        Dim i As Integer
        For i = 0 To DbTypeConvertor._DbTypeList.Count - 1
            Dim entry As DbTypeMapEntry = DirectCast(DbTypeConvertor._DbTypeList.Item(i), DbTypeMapEntry)
            If (entry.Type Is type) Then
                retObj = entry
                Exit For
            End If
        Next i
        If (retObj Is Nothing) Then
            Throw New ApplicationException("Referenced an unsupported Type " & type.ToString)
        End If
        Return DirectCast(retObj, DbTypeMapEntry)
    End Function

    Public Shared Function ToDbType(ByVal sqlDbType As SqlDbType) As DbType
        Return DbTypeConvertor.Find(sqlDbType).DbType
    End Function

    Public Shared Function ToDbType(ByVal type As Type) As DbType
        Return DbTypeConvertor.Find(type).DbType
    End Function

    Public Shared Function ToNetType(ByVal dbType As DbType) As Type
        Return DbTypeConvertor.Find(dbType).Type
    End Function

    Public Shared Function ToNetType(ByVal sqlDbType As SqlDbType) As Type
        Return DbTypeConvertor.Find(sqlDbType).Type
    End Function

    Public Shared Function ToSqlDbType(ByVal dbType As DbType) As SqlDbType
        Return DbTypeConvertor.Find(dbType).SqlDbType
    End Function

    Public Shared Function ToSqlDbType(ByVal type As Type) As SqlDbType
        Return DbTypeConvertor.Find(type).SqlDbType
    End Function


    ' Fields
    Private Shared _DbTypeList As ArrayList = New ArrayList

    ' Nested Types
    <StructLayout(LayoutKind.Sequential)> _
    Private Structure DbTypeMapEntry
        Public Type As Type
        Public DbType As DbType
        Public SqlDbType As SqlDbType
        Public Sub New(ByVal type As Type, ByVal dbType As DbType, ByVal sqlDbType As SqlDbType)
            Me.Type = type
            Me.DbType = dbType
            Me.SqlDbType = sqlDbType
        End Sub
    End Structure
End Class

