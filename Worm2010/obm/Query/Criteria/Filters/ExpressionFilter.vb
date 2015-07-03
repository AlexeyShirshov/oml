Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Expressions
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm.Criteria.Joins
Imports System.Linq

Namespace Criteria.Core

    <Serializable()> _
    Public Class ExpressionFilter
        Implements IFilter
        Implements IEvaluableFilter

        Private _fo As FilterOperation
        Private _left As UnaryExp
        Private _right As UnaryExp
        Private _eu As EntityUnion

        Public Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter Implements IFilter.SetUnion
            Throw New NotImplementedException
        End Function

        Public Sub New(ByVal left As UnaryExp, ByVal right As UnaryExp, ByVal fo As FilterOperation)
            _left = left
            _right = right
            _fo = fo
        End Sub

        Protected Sub New()

        End Sub
        Public ReadOnly Property Left() As UnaryExp
            Get
                Return _left
            End Get
        End Property

        Public ReadOnly Property Right() As UnaryExp
            Get
                Return _right
            End Get
        End Property

        Public ReadOnly Property Operation() As FilterOperation
            Get
                Return _fo
            End Get
        End Property

        Protected Overridable Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Private Function _CloneF() As IFilter Implements IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal executor As IExecutionContext, ByVal contextInfo As IDictionary,
                                      ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
            Return Left.MakeStmt(schema, fromClause, stmt, pname, almgr, contextInfo, False, executor) & stmt.Oper2String(Operation) & Right.MakeStmt(schema, fromClause, stmt, pname, almgr, contextInfo, False, executor)
        End Function

        Public Function Clone() As ExpressionFilter
            Dim n As New ExpressionFilter
            CopyTo(n)
            Return n
        End Function

        Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            If f Is Nothing Then
                Return False
            Else
                Return _ToString.Equals(f._ToString)
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, ExpressionFilter))
        End Function

        Public Function GetAllFilters() As IFilter() Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        'Public Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
        '    'Dim columns As List(Of String)
        '    'Return _left.MakeStmt(schema, pname, almgr, columns)
        '    Throw New NotSupportedException("Use MakeQueryStmt with columns parameter")
        'End Function

        Public Function ReplaceFilter(ByVal oldValue As IFilter, ByVal newValue As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If Equals(oldValue) Then
                Return newValue
            End If
            Return Nothing
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IFilter.GetStaticString
            Return _left.ToStaticString(mpe) & _fo.ToString & _right.ToStaticString(mpe)
        End Function

        Protected Function _ToString() As String Implements IFilter._ToString
            Return _left._ToString & _fo.ToString & _right._ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        'Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            If _left IsNot Nothing Then
                _left.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
            If _right IsNot Nothing Then
                _right.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public Function RemoveFilter(ByVal f As IFilter) As IFilter Implements IFilter.RemoveFilter
            If _left IsNot Nothing AndAlso _left.Equals(f) Then
                Return Nothing
                'Throw New InvalidOperationException("Cannot remove self")
            ElseIf _right IsNot Nothing AndAlso _right.Equals(f) Then
                Return Nothing
                'Throw New InvalidOperationException("Cannot remove self")
            End If
            Return Me
        End Function

        Public Function Eval(mpe As ObjectMappingEngine, d As GetObj4IEntityFilterDelegate,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As Values.IEvaluableValue.EvalResult Implements IEvaluableFilter.Eval
            If _right IsNot Nothing Then
                Dim le As IEvaluableValue = TryCast(_left.Value, IEvaluableValue)
                If le IsNot Nothing Then
                    Return le.Eval(_right.Value, mpe, New OrmFilterTemplate(Nothing, FilterOperation.Equal))
                End If
            End If

            Return IEvaluableValue.EvalResult.Unknown
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, ExpressionFilter))
        End Function

        Public Function CopyTo(target As ExpressionFilter) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If _left IsNot Nothing Then
                target._left = _left.Clone
            End If

            If _right IsNot Nothing Then
                target._right = _right.Clone
            End If

            If _eu IsNot Nothing Then
                target._eu = _eu.Clone
            End If

            target._fo = _fo

            Return True
        End Function
    End Class
End Namespace