Imports System.Collections.Generic

Namespace Expressions2

    <Serializable()> _
    Public Class ParameterExpression
        Implements IParameterExpression

        Private _v As Object
        Private _pname As String

        Protected Sub New()
        End Sub

        Public Sub New(ByVal value As Object)
            _v = value
        End Sub

        Public Overridable Function GetDynamicString() As String Implements IParameterExpression.GetDynamicString
            If _v IsNot Nothing Then
                Return _v.ToString
            End If
            Return String.Empty
        End Function

        Public Overridable ReadOnly Property Value() As Object Implements IParameterExpression.Value
            Get
                Return _v
            End Get
        End Property

        'Public Overridable Function Test(ByVal oper As BinaryOperationType, ByVal evaluatedValue As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
        '    Return Helper.Test(_v, evaluatedValue, oper, _case, mpe)
        'End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IParameterExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return "scalarval"
        End Function

        Public Overridable Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Overridable Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim v As Object = _v
            Dim args As New IParameterExpression.ModifyValueArgs
            RaiseEvent ModifyValue(Me, args)
            If args.Modified Then
                v = args.NewValue
            End If

            If stmt.SupportParams Then
                If paramMgr Is Nothing Then
                    Throw New ArgumentNullException("paramMgr")
                End If
                _pname = paramMgr.AddParam(_pname, v)
                Return _pname
            Else
                Return "'" & v.ToString & "'"
            End If
        End Function

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, ParameterExpression))
        End Function

        Public Overloads Function Equals(ByVal f As ParameterExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return Object.Equals(Me, f)
        End Function

        Protected Sub RaiseModifyValue(ByVal args As IParameterExpression.ModifyValueArgs)
            RaiseEvent ModifyValue(Me, args)
        End Sub
    End Class

    <Serializable()> _
   Public Class InExpression
        Inherits ParameterExpression

        Private _l As New List(Of String)
        Private _str As String

        Public Sub New(ByVal value As ICollection)
            MyBase.New(value)
        End Sub

        'Public Sub New(ByVal value As ICollection, ByVal caseSensitive As Boolean)
        '    MyBase.New(value, caseSensitive)
        'End Sub

        Public Overrides Function GetDynamicString() As String
            If String.IsNullOrEmpty(_str) Then
                Dim l As New List(Of String)
                For Each o As Object In Value
                    l.Add(o.ToString)
                Next
                l.Sort()
                Dim sb As New StringBuilder
                For Each s As String In l
                    sb.Append(s).Append("$")
                Next
                _str = sb.ToString
            End If
            Return _str
        End Function

        'Public Overrides Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult
        '    Dim r As IParameterExpression.EvalResult = IParameterExpression.EvalResult.NotFound

        '    Select Case oper
        '        Case BinaryOperationType.In
        '            For Each o As Object In Value
        '                If Object.Equals(o, v) Then
        '                    r = IParameterExpression.EvalResult.Found
        '                    Exit For
        '                End If
        '            Next
        '        Case BinaryOperationType.NotIn
        '            For Each o As Object In Value
        '                If Object.Equals(o, v) Then
        '                    r = IParameterExpression.EvalResult.NotFound
        '                    Exit For
        '                End If
        '            Next
        '        Case Else
        '            Throw New InvalidOperationException(String.Format("Invalid operation {0} for InValue", oper))
        '    End Select

        '    Return r
        'End Function

        Public Shadows ReadOnly Property Value() As ICollection
            Get
                Return CType(MyBase.Value, Global.System.Collections.ICollection)
            End Get
        End Property

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String

            Dim sb As New StringBuilder
            Dim idx As Integer
            For Each o As Object In Value
                Dim v As Object = o
                Dim args As New IParameterExpression.ModifyValueArgs
                RaiseModifyValue(args)
                If args.Modified Then
                    v = args.NewValue
                End If

                If paramMgr Is Nothing Then
                    Throw New ArgumentNullException("paramMgr")
                End If

                Dim pname As String = Nothing
                Dim add As Boolean
                If _l.Count < idx Then
                    pname = _l(idx)
                Else
                    add = True
                End If

                pname = paramMgr.AddParam(pname, v)
                If add Then
                    _l.Add(pname)
                End If

                sb.Append(pname).Append(",")
                idx += 1
            Next
            sb.Length -= 1
            sb.Insert(0, "(").Append(")")
            Return sb.ToString
        End Function

        Public Overrides ReadOnly Property ShouldUse() As Boolean
            Get
                Return Value IsNot Nothing AndAlso Value.Count > 0
            End Get
        End Property

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return "inval"
        End Function
    End Class

End Namespace