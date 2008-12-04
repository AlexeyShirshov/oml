Imports System.Collections.Generic
Imports Worm.Orm.Meta

Namespace Orm.Meta

    Public Interface ITableFunction
        ReadOnly Property GetRealTable() As String
    End Interface

    Public Interface ISearchTable

    End Interface

    Public Interface ICreateParam
        Function CreateParam(ByVal value As Object) As String
        Function AddParam(ByVal pname As String, ByVal value As Object) As String
        ReadOnly Property NamedParams() As Boolean
        ReadOnly Property Params() As IList(Of System.Data.Common.DbParameter)
        Function GetParameter(ByVal name As String) As System.Data.Common.DbParameter
    End Interface

End Namespace

''' <summary>
''' Интерфейс для "подготовки" таблицы перед генерацией запроса
''' </summary>
''' <remarks>Используется для реализации функций в качестве таблиц, разрешения схем таблицы (schema resolve)</remarks>
Public Interface IPrepareTable
    '''' <summary>
    '''' Словарь псевдонимов (aliases) таблиц
    '''' </summary>
    '''' <value></value>
    '''' <returns>Словарь где каждой таблице соответствует псевдоним</returns>
    '''' <remarks></remarks>
    'ReadOnly Property Aliases() As IDictionary(Of SourceFragment, String)
    ''' <summary>
    ''' Добавляет таблицу в словарь и создает текстовое представление таблицы (псевдоним)
    ''' </summary>
    ''' <param name="table">Таблица</param>
    ''' <returns>Возвращает псевдоним таблицы</returns>
    ''' <remarks>Если таблица уже добавлена реализация может кинуть исключение</remarks>
    Function AddTable(ByRef table As SourceFragment, ByVal os As Orm.ObjectSource) As String
    Function AddTable(ByRef table As SourceFragment, ByVal os As Orm.ObjectSource, ByVal pmgr As ICreateParam) As String
    ''' <summary>
    ''' Заменяет в <see cref="StringBuilder"/> названия таблиц на псевдонимы
    ''' </summary>
    ''' <param name="schema">Схема</param>
    ''' <param name="table">Таблица</param>
    ''' <param name="sb">StringBuilder</param>
    ''' <remarks></remarks>
    Sub Replace(ByVal schema As ObjectMappingEngine, ByVal table As SourceFragment, ByVal os As Orm.ObjectSource, ByVal sb As StringBuilder)
    Function GetAlias(ByVal table As SourceFragment, ByVal os As Orm.ObjectSource) As String
    Function ContainsKey(ByVal table As SourceFragment, ByVal os As Orm.ObjectSource) As Boolean
End Interface