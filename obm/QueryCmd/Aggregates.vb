Imports Worm.Orm
Imports System.Collections.Generic
Imports Worm.Criteria.Values

Namespace Query

    Public Enum AggregateFunction
        Max
        Min
        Average
        Custom
    End Enum

    Public MustInherit Class AggregateBase
        Private _f As AggregateFunction
        Private _field As String

        Public MustOverride Function MakeStmt() As String
    End Class

    Public Class [Aggregate]
        Inherits AggregateBase

        Private _prop As OrmProperty

        Public Overrides Function MakeStmt() As String

        End Function
    End Class

    Public Class CustomAggregate
        Inherits AggregateBase

        Private _params As List(Of ScalarValue)

        Public Overrides Function MakeStmt() As String

        End Function
    End Class
End Namespace