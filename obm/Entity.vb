Imports Worm.Orm.Meta

Namespace Orm

    Public Interface IEntity
        Sub SetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object)
        Function GetValue(ByVal propAlias As String) As Object
        Function GetValue(ByVal propAlias As String, ByVal schema As IOrmObjectSchemaBase) As Object
        Function GetSyncRoot() As IDisposable
        ReadOnly Property ObjectState() As ObjectState
        Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean) As Boolean
        Function CheckIsAllLoaded(ByVal schema As QueryGenerator, ByVal loadedColumns As Integer) As Boolean
        Sub BeginLoading()
        Sub EndLoading()
        Sub CreateCopyForLoad()
    End Interface

    Public Interface ICachedEntity
        Inherits IEntity
        ReadOnly Property Key() As Integer
    End Interface

End Namespace