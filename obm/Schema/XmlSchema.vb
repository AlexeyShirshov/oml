Imports Worm.Sorting
Imports Worm.Xml.Criteria.Conditions
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core

Namespace Xml
    Public Class XPathGenerator
        Inherits QueryGenerator

        Public Sub New(ByVal version As String)
            MyBase.New(version)
        End Sub

        Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity)
            MyBase.New(version, resolveEntity)
        End Sub

        Public Sub New(ByVal version As String, ByVal resolveName As ResolveEntityName)
            MyBase.New(version, resolveName)
        End Sub

        Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity, ByVal resolveName As ResolveEntityName)
            MyBase.New(version, resolveEntity, resolveName)
        End Sub

        Public Overrides Function CreateConditionCtor() As Criteria.Conditions.Condition.ConditionConstructorBase
            Return New Criteria.Conditions.Condition.ConditionConstructor
        End Function

        Public Overloads Overrides Function CreateCriteria(ByVal t As System.Type) As Worm.Criteria.ICtor
            Return New Criteria.Ctor(t)
        End Function

        Public Overloads Overrides Function CreateCriteria(ByVal t As System.Type, ByVal fieldName As String) As Worm.Criteria.CriteriaField
            Return New Criteria.Ctor(t).Field(fieldName)
        End Function

        Public Overrides Function CreateCriteriaLink(ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase) As Worm.Criteria.CriteriaLink
            Return New Criteria.XmlCriteriaLink(con)
        End Function

        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer) As Orm.Query.TopAspect
            Throw New NotImplementedException
        End Function

        Public Overloads Overrides Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sorting.Sort) As Orm.Query.TopAspect
            Throw New NotImplementedException
        End Function

        Protected Friend Sub AppendOrder(ByVal t As Type, ByVal s As Sort, ByVal sb As StringBuilder)
            Throw New NotImplementedException
        End Sub

        Public Overridable Function AppendWhere(ByVal t As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal sb As StringBuilder, ByVal filter_info As Object) As Boolean

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(filter)

            If t IsNot Nothing Then
                Dim schema As IOrmObjectSchemaBase = GetObjectSchema(t)
                con.AddFilter(schema.GetFilter(filter_info))
            End If

            If Not con.IsEmpty Then
                Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = con.Condition
                sb.Append("[").Append(bf.MakeQueryStmt(Me, Nothing, Nothing)).Append("]")
                Return True
            End If
            Return False
        End Function

        Public Function SelectID(ByVal original_type As Type) As String
            Dim selectcmd As New StringBuilder
            Dim s As IOrmObjectSchema = CType(GetObjectSchema(original_type), IOrmObjectSchema)
            selectcmd.Append(GetTableName(s.GetTables(0)))
            Return selectcmd.ToString
        End Function

        Public Overrides Function GetTableName(ByVal t As Orm.Meta.OrmTable) As String
            If String.IsNullOrEmpty(t.Schema) Then
                Return t.Table
            Else
                Return t.Schema & ":" & t.Table
            End If
        End Function

        Public Overrides Function CreateExecutor() As Query.IExecutor
            Throw New NotImplementedException
        End Function
    End Class
End Namespace