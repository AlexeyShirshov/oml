Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Criteria.Joins
    Public Class JoinFilter
        Implements Core.IFilter

        'Friend _d1 As Pair(Of String)
        Friend _e1 As Pair(Of ObjectSource, String)
        Friend _t1 As Pair(Of SourceFragment, String)

        'Friend _d2 As Pair(Of String)
        Friend _e2 As Pair(Of ObjectSource, String)
        Friend _t2 As Pair(Of SourceFragment, String)

        Friend _oper As FilterOperation

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim p As Pair(Of ObjectSource, String) = Nothing
            If t IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(t), propertyAlias)
            End If
            _e1 = p

            p = Nothing
            If t2 IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim p As Pair(Of ObjectSource, String) = Nothing
            If os IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(os, propertyAlias)
            End If
            _e1 = p

            p = Nothing
            If t2 IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal entityName2 As String, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim p As Pair(Of ObjectSource, String) = Nothing
            If Not String.IsNullOrEmpty(entityName) Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName), propertyAlias)
            End If
            _e1 = p

            p = Nothing
            If Not String.IsNullOrEmpty(entityName2) Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName2), propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            Dim p As Pair(Of ObjectSource, String) = Nothing
            If t2 IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal os As ObjectSource, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            Dim p As Pair(Of ObjectSource, String) = Nothing
            If os IsNot Nothing Then
                p = New Pair(Of ObjectSource, String)(os, propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal entityName2 As String, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            Dim p As Pair(Of ObjectSource, String) = Nothing
            If Not String.IsNullOrEmpty(entityName2) Then
                p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName2), propertyAlias2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            t = Nothing
            If table2 IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table2, column2)
            End If
            _t2 = t

            _oper = operation
        End Sub

        'Public Sub New(ByVal t1 As Type, ByVal t2 As Type)
        '    MyClass.New(t1, t2, Nothing)
        'End Sub

        'Public Sub New(ByVal t1 As Type, ByVal t2 As Type, ByVal key As String)
        '    _types = New Pair(Of Type)(t1, t2)
        '    _key = key
        'End Sub

        Protected Sub New()
        End Sub

        'Public Function GetStaticString() As String Implements IFilter.GetStaticString
        '    Throw New NotSupportedException
        'End Function

        Public Function ReplaceFilter(ByVal replacement As Core.IFilter, ByVal replacer As Core.IFilter) As Core.IFilter Implements Core.IFilter.ReplaceFilter
            If Not Equals(replacement) Then
                Return Nothing
            End If
            Return replacer
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, JoinFilter))
        End Function

        Public Overloads Function Equals(ByVal obj As JoinFilter) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            'Return ( _
            '    (Equals(_e1, obj._e1) AndAlso Equals(_e2, obj._e2)) OrElse (Equals(_e1, obj._e2) AndAlso Equals(_e2, obj._e1)) _
            '    ) OrElse ( _
            '    (Equals(_t1, obj._t1) AndAlso Equals(_t2, obj._t2)) OrElse (Equals(_t1, obj._t2) AndAlso Equals(_t2, obj._t1)) _
            '    )
            Dim v1 As Object = _e1
            Dim ve1 As Object = obj._e1
            If _e1 Is Nothing Then
                If _t1 Is Nothing Then
                    'v1 = _d1
                    've1 = obj._d1
                Else
                    v1 = _t1
                    ve1 = obj._t1
                End If
            End If

            Dim v2 As Object = _e2
            Dim ve2 As Object = obj._e2
            If v2 Is Nothing Then
                If _t2 Is Nothing Then
                    'v2 = _d2
                    've2 = obj._d2
                Else
                    v2 = _t2
                    ve2 = obj._e2
                End If
            End If

            Dim b As Boolean = (Equals(v1, ve1) AndAlso Equals(v2, ve2)) _
                OrElse (Equals(v1, ve2) AndAlso Equals(v2, ve1))

            Return b
        End Function

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of Core.IFilter) Implements Core.IFilter.GetAllFilters
            Return New JoinFilter() {Me}
        End Function

        Private Function Equals1(ByVal f As Core.IFilter) As Boolean Implements Core.IFilter.Equals
            Equals(TryCast(f, JoinFilter))
        End Function

        Private Function _ToString() As String Implements Core.IFilter._ToString
            Dim sb As New StringBuilder

            If _e1 IsNot Nothing Then
                sb.Append(_e1.First.ToStaticString).Append(_e1.Second).Append(" - ")
            ElseIf _t1 IsNot Nothing Then
                sb.Append(_t1.First.RawName).Append(_t1.Second).Append(" - ")
                'ElseIf _d1 IsNot Nothing Then
                '    sb.Append(_d1.First).Append(_d1.Second).Append(" - ")
                'Else
                '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            End If

            If _e2 IsNot Nothing Then
                sb.Append(_e2.First.ToStaticString).Append(_e2.Second).Append(" - ")
            ElseIf _t2 IsNot Nothing Then
                sb.Append(_t2.First.RawName).Append(_t2.Second).Append(" - ")
                'ElseIf _d2 IsNot Nothing Then
                '    sb.Append(_d2.First).Append(_d2.Second).Append(" - ")
                'Else
                '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            End If

            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Core.IFilter.GetStaticString
            Return _ToString()
        End Function

        'Public MustOverride Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements Core.IFilter.MakeQueryStmt
        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
            Dim map As MapField2Column = Nothing
            Dim os As ObjectSource = Nothing
            If _e1 IsNot Nothing Then
                map = schema.GetObjectSchema(_e1.First.GetRealType(schema)).GetFieldColumnMap(_e1.Second)
                os = _e1.First
                'ElseIf _d1 IsNot Nothing Then
                '    map = schema.GetObjectSchema(schema.GetTypeByEntityName(_d1.First)).GetFieldColumnMap(_d1.Second)
            ElseIf _t1 IsNot Nothing Then
                map = New MapField2Column(Nothing, _t1.Second, _t1.First)
            Else
                Throw New InvalidOperationException
            End If

            Dim map2 As MapField2Column = Nothing
            Dim os2 As ObjectSource = Nothing
            If _e2 IsNot Nothing Then
                map2 = schema.GetObjectSchema(_e2.First.GetRealType(schema)).GetFieldColumnMap(_e2.Second)
                os2 = _e2.First
                'ElseIf _d2 IsNot Nothing Then
                '    map = schema.GetObjectSchema(schema.GetTypeByEntityName(_d2.First)).GetFieldColumnMap(_d2.Second)
            ElseIf _t2 IsNot Nothing Then
                map2 = New MapField2Column(Nothing, _t2.Second, _t2.First)
            Else
                Throw New InvalidOperationException
            End If

            Dim [alias] As String = String.Empty

            If almgr IsNot Nothing Then
                Debug.Assert(almgr.ContainsKey(map._tableName, os), "There is not alias for table " & map._tableName.RawName)
                Try
                    [alias] = almgr.GetAlias(map._tableName, os) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                End Try
            End If

            Dim alias2 As String = String.Empty
            'If map2._tableName IsNot Nothing AndAlso tableAliases IsNot Nothing AndAlso tableAliases.ContainsKey(map2._tableName) Then
            If almgr IsNot Nothing Then
                Debug.Assert(almgr.ContainsKey(map2._tableName, os2), "There is not alias for table " & map2._tableName.RawName)
                Try
                    alias2 = almgr.GetAlias(map2._tableName, os2) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & map2._tableName.RawName, ex)
                End Try
            End If

            Return [alias] & map._columnName & stmt.Oper2String(_oper) & alias2 & map2._columnName
        End Function

        Public ReadOnly Property Filter() As Core.IFilter Implements Core.IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        'Public ReadOnly Property Filter(ByVal t As System.Type) As Core.IFilter Implements Core.IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            Dim c As New JoinFilter
            CopyTo(c)
            Return c
        End Function

        Public Function Clone() As Core.IFilter Implements Core.IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

        Protected Sub CopyTo(ByVal obj As JoinFilter)
            With obj
                ._e1 = _e1
                ._e2 = _e2
                ._oper = _oper
                ._t1 = _t1
                ._t2 = _t2
                '._d1 = _d1
                '._d2 = _d2
            End With
        End Sub

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String Implements Core.IFilter.MakeQueryStmt
            Return MakeQueryStmt(schema, stmt, filterInfo, almgr, pname)
        End Function

        Protected Shared Function ChangeEntityJoinToValue(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal value As IParamFilterValue) As IFilter
            For Each _fl As IFilter In source.GetAllFilters()
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                If fl IsNot Nothing Then
                    Dim f As IFilter = Nothing
                    If fl._e1 IsNot Nothing AndAlso fl._e1.First.GetRealType(schema) Is t AndAlso fl._e1.Second = propertyAlias Then
                        f = SetJF(fl._e2, fl._t2, Nothing, value, fl._oper)
                    ElseIf fl._e2 IsNot Nothing AndAlso fl._e2.First.GetRealType(schema) Is t AndAlso fl._e2.Second = propertyAlias Then
                        f = SetJF(fl._e1, fl._t1, Nothing, value, fl._oper)
                        'ElseIf fl._d1 IsNot Nothing Then
                        '    Dim tt As Type = schema.GetTypeByEntityName(fl._d1.First)
                        '    If tt Is t AndAlso fl._d1.Second = propertyAlias Then
                        '        f = SetJF(fl._e2, fl._t2, fl._d2, value, fl._oper)
                        '    End If
                        'ElseIf fl._d2 IsNot Nothing Then
                        '    Dim tt As Type = schema.GetTypeByEntityName(fl._d2.First)
                        '    If tt Is t AndAlso fl._d2.Second = propertyAlias Then
                        '        f = SetJF(fl._e1, fl._t1, fl._d1, value, fl._oper)
                        '    End If
                    End If

                    If f IsNot Nothing Then
                        Return CType(source.ReplaceFilter(fl, f), IFilter)
                    End If
                End If
            Next
            Return Nothing
        End Function

        Private Shared Function SetJF(ByVal e As Pair(Of ObjectSource, String), ByVal t As Pair(Of SourceFragment, String), _
                               ByVal d As Pair(Of String), ByVal value As IParamFilterValue, ByVal oper As FilterOperation) As IFilter
            If e IsNot Nothing Then
                Return New EntityFilter(e.First, e.Second, value, oper)
            ElseIf t IsNot Nothing Then
                Return New TableFilter(t.First, t.Second, value, oper)
                'ElseIf d IsNot Nothing Then
                '    Return New Criteria.Core.EntityFilter(d.First, d.Second, value, oper)
            Else
                Throw New InvalidOperationException
            End If
        End Function

        Public Shared Function ChangeEntityJoinToLiteral(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal literal As String) As IFilter
            Return ChangeEntityJoinToValue(schema, source, t, propertyAlias, New LiteralValue(literal))
        End Function

        Public Shared Function ChangeEntityJoinToParam(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal value As TypeWrap(Of Object)) As IFilter
            Return ChangeEntityJoinToValue(schema, source, t, propertyAlias, New ScalarValue(value.Value))
        End Function
    End Class
End Namespace