Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm
Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins

Namespace Xml
    Public Class XPathGenerator
        Inherits StmtGenerator

        'Public Sub New(ByVal version As String)
        '    MyBase.New(version)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity)
        '    MyBase.New(version, resolveEntity)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveName)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveEntity, resolveName)
        'End Sub

        'Public Overrides Function CreateConditionCtor() As Condition.ConditionConstructor
        '    Return New Condition.ConditionConstructor
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal os As ObjectSource) As Worm.Criteria.ICtor
        '    Return New Criteria.Ctor(os)
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal os As ObjectSource, ByVal fieldName As String) As Worm.Criteria.CriteriaField
        '    Return New Criteria.Ctor(os).Field(fieldName)
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal table As Entities.Meta.SourceFragment) As Worm.Criteria.ICtor
        '    Throw New NotImplementedException
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal table As Entities.Meta.SourceFragment, ByVal columnName As String) As Worm.Criteria.CriteriaColumn
        '    Throw New NotImplementedException
        'End Function


        'Public Overrides Function CreateCriteriaLink(ByVal con As Condition.ConditionConstructor) As Worm.Criteria.CriteriaLink
        '    Return New Criteria.XmlCriteriaLink(con)
        'End Function

        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer) As Entities.Query.TopAspect
            Throw New NotImplementedException
        End Function

        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sort) As Entities.Query.TopAspect
            Throw New NotImplementedException
        End Function

        Protected Friend Sub AppendOrder(ByVal t As Type, ByVal s As Sort, ByVal sb As StringBuilder)
            Throw New NotImplementedException
        End Sub

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal sb As StringBuilder, ByVal filter_info As Object) As Boolean

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(filter)

            If t IsNot Nothing Then
                Dim schema As IContextObjectSchema = TryCast(mpe.GetEntitySchema(t), IContextObjectSchema)
                If schema IsNot Nothing Then
                    con.AddFilter(schema.GetContextFilter(filter_info))
                End If
            End If

            If Not con.IsEmpty Then
                Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = con.Condition
                sb.Append("[").Append(bf.MakeQueryStmt(mpe, Nothing, Me, Nothing, filter_info, Nothing, Nothing)).Append("]")
                Return True
            End If
            Return False
        End Function

        Public Function SelectID(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type) As String
            Dim selectcmd As New StringBuilder
            Dim s As IEntitySchema = mpe.GetEntitySchema(original_type)
            selectcmd.Append(GetTableName(s.Table))
            Return selectcmd.ToString
        End Function

        Public Overrides Function GetTableName(ByVal t As Entities.Meta.SourceFragment) As String
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

        Public Overrides Function CreateSelectExpressionFormater() As Entities.ISelectExpressionFormater
            Throw New NotImplementedException
        End Function

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

        Public Overrides Sub FormStmt(ByVal dbschema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal filterInfo As Object, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal sb As System.Text.StringBuilder, ByVal _t As System.Type, ByVal _tbl As Entities.Meta.SourceFragment, ByVal _joins() As QueryJoin, ByVal _field As String, ByVal _f As IFilter)
            Throw New NotImplementedException
        End Sub

        Public Overrides Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Worm.Query.QueryCmd.FromClauseDef, ByVal filterInfo As Object, ByVal query As Query.QueryCmd, ByVal params As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable) As String
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
    End Class
End Namespace