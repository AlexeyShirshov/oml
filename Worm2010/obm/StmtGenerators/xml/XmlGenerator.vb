Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm
Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports Worm.Expressions2

Namespace Xml
    Public Class XPathGenerator
        Inherits StmtGenerator

#If Not ExcludeFindMethods Then
        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer) As Entities.Query.TopAspect
            Throw New NotImplementedException
        End Function

        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sort) As Entities.Query.TopAspect
            Throw New NotImplementedException
        End Function

#End If

        Protected Friend Sub AppendOrder(ByVal t As Type, ByVal s As OrderByClause, ByVal sb As StringBuilder)
            Throw New NotImplementedException
        End Sub

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal sb As StringBuilder, ByVal contextInfo As IDictionary) As Boolean

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(filter)

            If t IsNot Nothing Then
                Dim schema As IContextObjectSchema = TryCast(mpe.GetEntitySchema(t), IContextObjectSchema)
                If schema IsNot Nothing Then
                    con.AddFilter(schema.GetContextFilter(contextInfo))
                End If
            End If

            If Not con.IsEmpty Then
                Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = con.Condition
                sb.Append("[").Append(bf.MakeQueryStmt(mpe, Nothing, Me, Nothing, contextInfo, Nothing, Nothing)).Append("]")
                Return True
            End If
            Return False
        End Function

        Public Function SelectID(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal contextInfo As IDictionary) As String
            Dim selectcmd As New StringBuilder
            Dim s As IEntitySchema = mpe.GetEntitySchema(original_type)
            selectcmd.Append(GetTableName(s.Table, contextInfo))
            Return selectcmd.ToString
        End Function

        Public Overrides Function GetTableName(ByVal t As Entities.Meta.SourceFragment, ByVal contextInfo As IDictionary) As String
            If String.IsNullOrEmpty(t.Schema) Then
                Return t.Name
            Else
                Return t.Schema & ":" & t.Name
            End If
        End Function

        Public Overrides ReadOnly Property Selector() As String
            Get
                Return String.Empty
            End Get
        End Property

        Public Overrides Function CreateExecutor() As Query.IExecutor
            Return New Query.Xml.XmlQueryExecutor
        End Function

        'Protected Friend Overrides Function MakeJoin(ByVal type2join As System.Type, ByVal selectType As System.Type, ByVal field As String, ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Worm.Criteria.Joins.JoinType, Optional ByVal switchTable As Boolean = False) As Worm.Criteria.Joins.OrmJoin
        '    Throw New NotImplementedException
        'End Function

        'Protected Friend Overrides Function MakeM2MJoin(ByVal m2m As Entities.Meta.M2MRelation, ByVal type2join As System.Type) As Worm.Criteria.Joins.OrmJoin()
        '    Throw New NotImplementedException
        'End Function

        'Public Overrides Function CreateCustom(ByVal format As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String)) As Worm.Criteria.Core.CustomFilter
        '    Throw New NotImplementedException
        'End Function

        'Public Overrides Function CreateSelectExpressionFormater() As Entities.ISelectExpressionFormater
        '    Throw New NotImplementedException
        'End Function

        Public Overrides Function TopStatement(ByVal top As Integer) As String
            Throw New NotImplementedException
        End Function

        Public Overrides ReadOnly Property GetDate() As String
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides ReadOnly Property GetYear() As String
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides Function ParamName(ByVal name As String, ByVal i As Integer) As String
            Throw New NotImplementedException
        End Function

        Public Overrides Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String
            Select Case oper
                Case Worm.Criteria.FilterOperation.Equal
                    Return " = "
                Case Worm.Criteria.FilterOperation.GreaterEqualThan
                    Return " >= "
                Case Worm.Criteria.FilterOperation.GreaterThan
                    Return " > "
                Case Worm.Criteria.FilterOperation.NotEqual
                    Return " != "
                Case Worm.Criteria.FilterOperation.LessEqualThan
                    Return " <= "
                Case Worm.Criteria.FilterOperation.LessThan
                    Return " < "
                Case Else
                    Throw New ObjectMappingException("invalid opration " & oper.ToString)
            End Select
        End Function

        Public Overrides Sub FormStmt(ByVal dbschema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                      ByVal contextInfo As IDictionary, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal sb As System.Text.StringBuilder, ByVal _t As System.Type, ByVal _tbl As Entities.Meta.SourceFragment, ByVal _joins() As QueryJoin, ByVal _field As String, ByVal _f As IFilter)
            Throw New NotImplementedException
        End Sub

        Public Overrides Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Worm.Query.QueryCmd.FromClauseDef,
                                                     ByVal contextInfo As IDictionary, ByVal query As Query.QueryCmd,
                                                     ByVal params As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable) As String
            Throw New NotImplementedException
        End Function

        Public Overrides ReadOnly Property SupportParams() As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property FTSKey() As String
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public Overrides ReadOnly Property Left() As String
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides Function Comment(ByVal s As String) As String
            Throw New NotImplementedException
        End Function

        Public Overrides ReadOnly Property PlanHint() As String
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function BinaryOperator2String(ByVal oper As Expressions2.BinaryOperationType) As String
            Select Case oper
                Case Expressions2.BinaryOperationType.Equal
                    Return " = "
                Case Expressions2.BinaryOperationType.GreaterEqualThan
                    Return " >= "
                Case Expressions2.BinaryOperationType.GreaterThan
                    Return " > "
                Case Expressions2.BinaryOperationType.NotEqual
                    Return " != "
                Case Expressions2.BinaryOperationType.LessEqualThan
                    Return " <= "
                Case Expressions2.BinaryOperationType.LessThan
                    Return " < "
                Case Else
                    Throw New ObjectMappingException("invalid opration " & oper.ToString)
            End Select
        End Function

        Public Overrides Function UnaryOperator2String(ByVal oper As Expressions2.UnaryOperationType) As String
            Throw New ObjectMappingException("invalid opration " & oper.ToString)
        End Function

        Public Overrides Function FormatGroupBy(ByVal t As Expressions2.GroupExpression.SummaryValues, ByVal fields As String, ByVal custom As String) As String
            Throw New NotSupportedException
        End Function

        Public Overrides Function FormatOrderBy(ByVal t As Expressions2.SortExpression.SortType, ByVal fields As String, ByVal collation As String) As String
            Throw New NotSupportedException
        End Function

        Public Overrides Function FormatAggregate(ByVal t As Expressions2.AggregateExpression.AggregateFunction, ByVal fields As String, ByVal custom As String, ByVal distinct As Boolean) As String
            Throw New NotSupportedException
        End Function
    End Class
End Namespace