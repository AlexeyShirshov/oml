Namespace Web
    Public Interface IPager
        Sub SetTotalCount(ByVal cnt As Integer)
        Function GetCurrentPageOffset() As Integer
        Function GetPageSize() As Integer
    End Interface
End Namespace