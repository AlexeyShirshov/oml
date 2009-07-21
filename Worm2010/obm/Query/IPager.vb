Namespace Query
    Public Interface IPager
        Sub SetTotalCount(ByVal cnt As Integer)
        Function GetCurrentPageOffset() As Integer
        Function GetPageSize() As Integer
        Function GetReverse() As Boolean
    End Interface
End Namespace