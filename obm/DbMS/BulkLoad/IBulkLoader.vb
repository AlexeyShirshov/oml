Namespace Database
    Public Interface IBulkLoader
        Function Load(mgr As OrmReadOnlyDBManager, options As IBulkLoadOptions) As IBulkLoadResults
        Event NotifyCopied(sender As IBulkLoader, args As RowsCopiedEventArgs)

    End Interface
End Namespace