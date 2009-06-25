Namespace Database

    Public Class MSSQL2008Generator
        Inherits MSSQL2005Generator

        Public Overrides Function FormatGroupBy(ByVal t As Expressions2.GroupExpression.SummaryValues, _
            ByVal fields As String, ByVal custom As String) As String
            Select Case t
                Case Expressions2.GroupExpression.SummaryValues.None
                    Return "group by " & fields
                Case Expressions2.GroupExpression.SummaryValues.Cube
                    Return "group by cube(" & fields & ")"
                Case Expressions2.GroupExpression.SummaryValues.Rollup
                    Return "group by rollup(" & fields & ")"
                Case Expressions2.GroupExpression.SummaryValues.GroupingSets
                    Return "group by grouping sets(" & fields & ")"
                Case Expressions2.GroupExpression.SummaryValues.Custom
                    Return "group by " & custom & "(" & fields & ")"
                Case Else
                    Throw New NotSupportedException(t.ToString)
            End Select
        End Function

        Public Overrides ReadOnly Property Name() As String
            Get
                Return "Microsoft SQL Server 2008"
            End Get
        End Property
    End Class

End Namespace