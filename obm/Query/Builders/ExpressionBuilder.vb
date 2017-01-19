Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Expressions
Imports Worm.Criteria.Values
Imports Worm.Expressions2
Imports System.ComponentModel

Namespace Query
    Public Class ExpCtorBase(Of T As {New, IntBase})

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class IntBase
            Implements IGetExpression

            Protected _l As List(Of IExpression)

            Public Function AppendExpression(ByVal exp As IExpression) As T
                GetExpressions.Add(Wrap(exp))
                Return CType(Me, T)
            End Function

            Protected Overridable Function Wrap(ByVal exp As IExpression) As IExpression
                Return exp
            End Function

            Protected Function GetExpressions() As List(Of IExpression)
                If _l Is Nothing Then
                    _l = New List(Of IExpression)
                End If
                Return _l
            End Function

            Public Function ToArray() As IExpression()
                Return _l.ToArray
            End Function
            Public Function AsEnumerable() As IEnumerable(Of IExpression)
                Return _l
            End Function
#Region " Members "
            Public Function prop(ByVal propertyAlias As String) As T
                Return Exp(New PropertyAliasExpression(propertyAlias))
            End Function

            Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion(t), propertyAlias)
            End Function

            Public Function prop(ByVal entityName As String, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion(entityName), propertyAlias)
            End Function

            Public Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion([alias]), propertyAlias)
            End Function

            Public Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As T
                Return Exp(New EntityExpression(propertyAlias, os))
            End Function

            Public Function prop(ByVal op As ObjectProperty) As T
                Return Exp(New EntityExpression(op))
            End Function

            'Public Function prop(ByVal exp As IGetExpression) As T
            '    AddExpression(exp.Expression)
            '    Return CType(Me, T)
            'End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As T
                AppendExpression(New TableExpression(table, tableColumn))
                Return CType(Me, T)
            End Function

            Public Function column(ByVal inner As QueryCmd) As T
                AppendExpression(New QueryExpression(inner))
                Return CType(Me, T)
            End Function

            Public Function custom(ByVal expression As String) As T
                AppendExpression(New CustomExpression(expression))
                Return CType(Me, T)
            End Function

            Public Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As T
                AppendExpression(New CustomExpression(expression, params))
                Return CType(Me, T)
            End Function

            Public Function Exp(ByVal expression As IGetExpression) As T
                If expression IsNot Nothing Then
                    AppendExpression(expression.Expression)
                End If
                Return CType(Me, T)
            End Function
#End Region

            Public ReadOnly Property Expression() As Expressions2.IExpression Implements Expressions2.IGetExpression.Expression
                Get
                    Dim l As List(Of IExpression) = GetExpressions()
                    If l.Count = 1 Then
                        Return l(0)
                    Else
                        Return New ExpressionsArray(l.ToArray)
                    End If
                End Get
            End Property
        End Class

    End Class

    Public Class ExpCtor(Of T As {New, Int})

        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Class Int
            Inherits ExpCtorBase(Of T).IntBase
            Public Function param(value As Object) As T
                If value Is Nothing Then
                    AppendExpression(New DBNullExpression())
                Else
                    AppendExpression(New ParameterExpression(value))
                End If
                Return CType(Me, T)
            End Function

            Public Function literal(s As Object) As T
                If s IsNot Nothing Then
                    AppendExpression(LiteralExpression.Create(s))
                End If
                Return CType(Me, T)
            End Function

            Public Function count() As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
                Return CType(Me, T)
            End Function

            Public Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As T
                Return count_distinct(New TableExpression(tbl, col))
            End Function

            Public Function count_distinct(ByVal op As ObjectProperty) As T
                Return count_distinct(New EntityExpression(op))
            End Function

            Public Function count_distinct(ByVal pa As String) As T
                Return count_distinct(New PropertyAliasExpression(pa))
            End Function

            Public Function count_distinct(ByVal exp As IGetExpression) As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
                Return CType(Me, T)
            End Function

            Public Function sum(ByVal table As SourceFragment, ByVal column As String) As T
                Return sum(New TableExpression(table, column))
            End Function

            Public Function sum(ByVal op As ObjectProperty) As T
                Return sum(New EntityExpression(op))
            End Function

            Public Function sum(ByVal pa As String) As T
                Return sum(New PropertyAliasExpression(pa))
            End Function

            Public Function sum(ByVal exp As IGetExpression) As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
                Return CType(Me, T)
            End Function

            Public Function max(ByVal exp As IGetExpression) As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
                Return CType(Me, T)
            End Function

            Public Function max(ByVal op As ObjectProperty) As T
                Return max(New EntityExpression(op))
            End Function

            Public Function max(ByVal pa As String) As T
                Return max(New PropertyAliasExpression(pa))
            End Function

            Public Function max(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return max(New TableExpression(tbl, column))
            End Function

            Public Function min(ByVal exp As IGetExpression) As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
                Return CType(Me, T)
            End Function

            Public Function min(ByVal pa As String) As T
                Return min(New PropertyAliasExpression(pa))
            End Function

            Public Function min(ByVal op As ObjectProperty) As T
                Return min(New EntityExpression(op))
            End Function

            Public Function min(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return min(New TableExpression(tbl, column))
            End Function

            Public Function avg(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return avg(New TableExpression(tbl, column))
            End Function

            Public Function avg(ByVal op As ObjectProperty) As T
                Return avg(New EntityExpression(op))
            End Function

            Public Function avg(ByVal pa As String) As T
                Return avg(New PropertyAliasExpression(pa))
            End Function

            Public Function avg(ByVal exp As IGetExpression) As T
                AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
                Return CType(Me, T)
            End Function

            Public Function query(ByVal q As QueryCmd) As T
                AppendExpression(New QueryExpression(q))
                Return CType(Me, T)
            End Function

#Region " Arithmetic "
            Public Function Add(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return Add(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return Add(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return Add(New ParameterExpression(value))
                End If
            End Function

            Public Function AddLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return Add(New LiteralExpression(value.ToString))
            End Function

            Public Function Add(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.Add,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function Subtruct(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return Subtract(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return Subtract(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return Subtract(New ParameterExpression(value))
                End If
            End Function

            Public Function SubtructLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return Subtract(New LiteralExpression(value.ToString))
            End Function

            Public Function Subtract(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.Subtract,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function Divide(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return Divide(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return Divide(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return Divide(New ParameterExpression(value))
                End If
            End Function

            Public Function DivideLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return Divide(New LiteralExpression(value.ToString))
            End Function

            Public Function Divide(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.Divide,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function Multiply(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return Multiply(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return Multiply(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return Multiply(New ParameterExpression(value))
                End If
            End Function

            Public Function MultiplyLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return Multiply(New LiteralExpression(value.ToString))
            End Function

            Public Function Multiply(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.Multiply,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function Modulo(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return Modulo(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return Modulo(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return Modulo(New ParameterExpression(value))
                End If
            End Function

            Public Function ModuloLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return Modulo(New LiteralExpression(value.ToString))
            End Function

            Public Function Modulo(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.Modulo,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function [Xor](ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return [Xor](CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return [Xor](New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return [Xor](New ParameterExpression(value))
                End If
            End Function

            Public Function [XorLiteral](ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return [Xor](New LiteralExpression(value.ToString))
            End Function

            Public Function [Xor](ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.ExclusiveOr,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function BitAnd(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return BitAnd(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return BitAnd(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return BitAnd(New ParameterExpression(value))
                End If
            End Function

            Public Function BitAndLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return BitAnd(New LiteralExpression(value.ToString))
            End Function

            Public Function BitAnd(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.BitAnd,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Function BitOr(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                If GetType(IGetExpression).IsAssignableFrom(value.GetType) Then
                    Return BitOr(CType(value, IGetExpression))
                ElseIf GetType(ObjectProperty).IsAssignableFrom(value.GetType) Then
                    Return BitOr(New EntityExpression(CType(value, ObjectProperty)))
                Else
                    Return BitOr(New ParameterExpression(value))
                End If
            End Function

            Public Function BitOrLiteral(ByVal value As Object) As T
                If value Is Nothing Then
                    Throw New ArgumentNullException("value")
                End If

                Return BitOr(New LiteralExpression(value.ToString))
            End Function

            Public Function BitOr(ByVal value As IGetExpression) As T
                If value IsNot Nothing Then
                    Dim lastIdx As Integer = GetExpressions.Count - 1
                    GetExpressions(lastIdx) = Wrap(New BinaryExpression(GetExpressions(lastIdx),
                        BinaryOperationType.BitOr,
                        value.Expression))
                End If

                Return CType(Me, T)
            End Function

            Public Shared Operator +(ByVal a As Int, ByVal b As Object) As T
                Return a.Add(b)
            End Operator

            Public Shared Operator -(ByVal a As Int, ByVal b As Object) As T
                Return a.Subtruct(b)
            End Operator

            Public Shared Operator /(ByVal a As Int, ByVal b As Object) As T
                Return a.Divide(b)
            End Operator

            Public Shared Operator *(ByVal a As Int, ByVal b As Object) As T
                Return a.Multiply(b)
            End Operator

            Public Shared Operator Mod(ByVal a As Int, ByVal b As Object) As T
                Return a.Modulo(b)
            End Operator

            Public Shared Operator Xor(ByVal a As Int, ByVal b As Object) As T
                Return a.Xor(b)
            End Operator

            Public Shared Operator And(ByVal a As Int, ByVal b As Object) As T
                Return a.BitAnd(b)
            End Operator

            Public Shared Operator Or(ByVal a As Int, ByVal b As Object) As T
                Return a.BitOr(b)
            End Operator

            'Public Shared Operator =(ByVal a As Int, ByVal b As Object) As Criteria.PredicateLink
            '    Return Ctor.Exp(a).eq(b)
            'End Operator

            'Public Shared Operator <>(ByVal a As Int, ByVal b As Object) As Criteria.PredicateLink
            '    Return a.not_eq(b)
            'End Operator

            'Public Shared Operator >(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            '    Return a.greater_than(b)
            'End Operator

            'Public Shared Operator <(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            '    Return a.less_than(b)
            'End Operator

            'Public Shared Operator >=(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            '    Return a.greater_than_eq(b)
            'End Operator

            'Public Shared Operator <=(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            '    Return a.less_than_eq(b)
            'End Operator
#End Region

            Public Shared Narrowing Operator CType(ByVal f As Int) As IExpression()
                Return f.GetExpressions.ToArray
            End Operator

            Public Shared Widening Operator CType(ByVal f As Int) As BinaryExpression
                Return CType(f.GetExpressions(f.GetExpressions.Count - 1), BinaryExpression)
            End Operator

        End Class
    End Class

    Public Class ECtor

#Region " Shared "
        Public Shared Function prop(ByVal propertyAlias As String) As Int
            Return prop(New PropertyAliasExpression(propertyAlias))
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As Int
            Return prop(New EntityExpression(propertyAlias, os))
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As Int
            Return prop(New EntityExpression(op))
        End Function

        Public Shared Function prop(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(exp.Expression)
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As Int
            Dim f As New Int
            f.AppendExpression(New TableExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd) As Int
            Dim f As New Int
            f.AppendExpression(New QueryExpression(inner))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As Int
            Dim f As New Int
            f.AppendExpression(New CustomExpression(expression))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New CustomExpression(expression, params))
            Return f
        End Function

        Public Shared Function count() As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
            Return f
        End Function

        Public Shared Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As Int
            Return count_distinct(New TableExpression(tbl, col))
        End Function

        Public Shared Function count_distinct(ByVal op As ObjectProperty) As Int
            Return count_distinct(New EntityExpression(op))
        End Function

        Public Shared Function count_distinct(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
            Return f
        End Function

        Public Shared Function sum(ByVal table As SourceFragment, ByVal column As String) As Int
            Return sum(New TableExpression(table, column))
        End Function

        Public Shared Function sum(ByVal op As ObjectProperty) As Int
            Return sum(New EntityExpression(op))
        End Function

        Public Shared Function sum(ByVal exp As IExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
            Return f
        End Function

        Public Shared Function max(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
            Return f
        End Function

        Public Shared Function max(ByVal op As ObjectProperty) As Int
            Return max(New EntityExpression(op))
        End Function

        Public Shared Function max(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return max(New TableExpression(tbl, column))
        End Function

        Public Shared Function min(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
            Return f
        End Function

        Public Shared Function min(ByVal op As ObjectProperty) As Int
            Return min(New EntityExpression(op))
        End Function

        Public Shared Function min(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return min(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return avg(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal op As ObjectProperty) As Int
            Return avg(New EntityExpression(op))
        End Function

        Public Shared Function avg(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
            Return f
        End Function

        Public Shared Function query(ByVal q As QueryCmd) As Int
            Dim f As New Int
            f.AppendExpression(New QueryExpression(q))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As IGetExpression) As Int
            Dim f As New Int
            If expression IsNot Nothing Then
                f.AppendExpression(expression.Expression)
            End If
            Return f
        End Function

        Public Shared Function Param(ByVal value As Object) As Int
            Dim f As New Int
            If value Is Nothing Then
                f.AppendExpression(New DBNullExpression())
            Else
                f.AppendExpression(New ParameterExpression(value))
            End If
            Return f
        End Function

        Public Shared Function Literal(ByVal value As Object) As Int
            Dim f As New Int
            If value Is Nothing Then
                f.AppendExpression(New DBNullExpression())
            Else
                f.AppendExpression(New LiteralExpression(value.ToString))
            End If
            Return f
        End Function
#End Region

        Class Int
            Inherits ExpCtor(Of Int).Int

            Public Overloads Shared Narrowing Operator CType(ByVal f As Int) As IExpression()
                Return f.GetExpressions.ToArray
            End Operator

            Public Overloads Shared Widening Operator CType(ByVal f As Int) As BinaryExpression
                Return CType(f.GetExpressions(f.GetExpressions.Count - 1), BinaryExpression)
            End Operator
        End Class

    End Class
End Namespace