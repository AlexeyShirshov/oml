Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class CustomValue
        Inherits TemplateBase
        Implements IFilterValue

        Private _f As String
        Public ReadOnly Property Format() As String
            Get
                Return _f
            End Get
        End Property

        Private _exp As IEnumerable(Of IExpression)
        Public ReadOnly Property Values As IEnumerable(Of IExpression)
            Get
                Return _exp
            End Get
        End Property

        Private _filter As Boolean

        Public Sub New(ByVal format As String)
            _f = format
            _filter = True
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As IExpression)
            _f = format
            _exp = values
            _filter = True
        End Sub

        'Public Sub New(ByVal format As String, ByVal ParamArray values() As SelectExpressionOld)
        '    _f = format
        '    _v = Array.ConvertAll(values, Function(se As SelectExpressionOld) New SelectExpressionValue(se))
        '    _filter = True
        'End Sub

        Public Sub New(ByVal oper As FilterOperation, ByVal format As String, ByVal values As IEnumerable(Of IExpression))
            MyBase.New(oper)
            _f = format
            _exp = values
        End Sub

        Public Overrides Function _ToString() As String Implements IQueryElement._ToString
            If _exp IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _exp
                    l.Add(v.GetDynamicString)
                Next
                If _filter Then
                    Return String.Format(_f, l.ToArray)
                Else
                    Return String.Format(_f, l.ToArray) & OperationString
                End If
            Else
                If _filter Then
                    Return _f & OperationString
                Else
                    Return _f
                End If
            End If
        End Function

        Protected ReadOnly Property OperationString() As String
            Get
                Return TemplateBase.OperToStringInternal(Operation)
            End Get
        End Property

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            'Dim values As List(Of String) = ObjectMappingEngine.ExtractValues(schema, stmt, almgr, _v)

            'Return String.Format(_f, values.ToArray)
            If _exp IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _exp
                    Dim s As String = v.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextInfo, MakeStatementMode.None, executor)
                    l.Add(s)
                Next
                Return String.Format(_f, l.ToArray)
                'Return String.Format(_f, _exp.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, filterInfo, MakeStatementMode.None, executor))
            Else
                Return _f
            End If
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If _exp IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _exp
                    l.Add(v.GetStaticString(mpe))
                Next
                If _filter Then
                    Return String.Format(_f, l.ToArray)
                Else
                    Return String.Format(_f, l.ToArray) & OperationString
                End If
            Else
                If _filter Then
                    Return _f
                Else
                    Return _f & OperationString
                End If
            End If
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _exp IsNot Nothing Then
                For Each e As IExpression In _exp
                    e.Prepare(executor, schema, contextInfo, stmt, isAnonym)
                Next
            End If
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Overrides Function Equals(ByVal f As Object) As Boolean
            Return Equals(TryCast(f, CustomValue))
        End Function

        Public Overloads Function Equals(ByVal f As CustomValue) As Boolean
            If f IsNot Nothing Then
                Return _ToString.Equals(f._ToString)
            Else
                Return False
            End If
        End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As CustomValue
            Dim n As New CustomValue(Format)
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, CustomValue))
        End Function

        Public Function CopyTo(target As CustomValue) As Boolean
            If target Is Nothing Then
                Return False
            End If

            Dim l As List(Of IExpression) = Nothing
            If Values IsNot Nothing Then
                For Each e In Values
                    l.Add(CType(e.Clone, IExpression))
                Next
            End If

            target._exp = l
            target._f = _f

            Return True
        End Function
    End Class

End Namespace