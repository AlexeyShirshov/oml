Namespace Database
    Public Class RowsCopiedEventArgs
        Inherits EventArgs

        Public Property Row As Integer

        Public Property Abort As Boolean
        Public Property CopiedInLastBatch As Integer
        Public Property TotalCopied As Integer
    End Class
End Namespace