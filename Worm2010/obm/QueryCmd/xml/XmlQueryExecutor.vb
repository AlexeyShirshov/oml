Imports Worm.Entities

Namespace Query.Xml
    Partial Public Class XmlQueryExecutor
        Implements IExecutor

        Public Event OnGetCacheItem(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs) Implements IExecutor.OnGetCacheItem
        Public Event OnRestoreDefaults(ByVal sender As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs) Implements IExecutor.OnRestoreDefaults

        Public Function Exec(Of ReturnType As ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            Throw New NotImplementedException
        End Function

        Public Function Exec(Of CreateType As {New, ICachedEntity}, ReturnType As ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            Throw New NotImplementedException
        End Function

        Public Function ExecEntity(Of ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Throw New NotImplementedException
        End Function

        Public Function ExecEntity1(Of CreateType As {New, Entities._IEntity}, ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Throw New NotImplementedException
        End Function

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Throw New NotImplementedException
        End Function

        'Public Function ExecSimple1(Of CreateType As {New, Entities._ICachedEntity}, ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
        '    Throw New NotImplementedException
        'End Function

        'Public Sub Reset(Of ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
        '    Throw New NotImplementedException
        'End Sub

        'Public Sub Reset1(Of CreateType As {New, Entities._ICachedEntity}, ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
        '    Throw New NotImplementedException
        'End Sub

        'Public Sub ResetEntity(Of ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
        '    Throw New NotImplementedException
        'End Sub

        'Public Sub ResetEntity1(Of CreateType As {New, Entities._IEntity}, ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
        '    Throw New NotImplementedException
        'End Sub

        Public Function Exec1(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadonlyMatrix Implements IExecutor.Exec
            Throw New NotImplementedException
        End Function

        Public Sub ClearCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ClearCache
            Throw New NotImplementedException
        End Sub

        Public Sub RenewCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal v As Boolean) Implements IExecutor.RenewCache
            Throw New NotImplementedException
        End Sub

        Public ReadOnly Property IsInCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean Implements IExecutor.IsInCache
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Sub ResetObjects(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetObjects
            Throw New NotImplementedException
        End Sub

        Public Sub SetCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal l As System.Collections.ICollection) Implements IExecutor.SetCache
            Throw New NotImplementedException
        End Sub

        Public Function SubscribeToErrorHandling(mgr As OrmManager, query As QueryCmd) As System.IDisposable Implements IExecutor.SubscribeToErrorHandling
            Return New EmptyDisposable
        End Function
    End Class
End Namespace