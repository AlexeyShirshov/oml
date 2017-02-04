﻿Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query
Imports System.Linq

Namespace Criteria.Joins

    <Serializable()> _
    Public Class JoinFilter
        Implements Core.IFilter

        Private _l As FieldReference
        Private _r As FieldReference

        Friend _oper As FilterOperation

        Private _eu As EntityUnion
        Private _eu2 As EntityUnion

#Region " Ctors "

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If t IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(t), propertyAlias)
            'End If
            '_e1 = p

            'p = Nothing
            'If t2 IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            'End If
            '_e2 = p

            Dim f As FieldReference = Nothing
            If t IsNot Nothing Then
                f = New FieldReference(t, propertyAlias)
            End If
            _l = f

            If t2 IsNot Nothing Then
                f = New FieldReference(t2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If os IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(os, propertyAlias)
            'End If
            '_e1 = p

            'p = Nothing
            'If t2 IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            'End If
            '_e2 = p
            Dim f As FieldReference = Nothing
            If os IsNot Nothing Then
                f = New FieldReference(os, propertyAlias)
            End If
            _l = f

            If t2 IsNot Nothing Then
                f = New FieldReference(t2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, _
                       ByVal os2 As EntityUnion, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim f As FieldReference = Nothing
            If os IsNot Nothing Then
                f = New FieldReference(os, propertyAlias)
            End If
            _l = f

            If os2 IsNot Nothing Then
                f = New FieldReference(os2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            Dim f As FieldReference = Nothing
            If op.Entity IsNot Nothing Then
                f = New FieldReference(op)
            End If
            _l = f

            If t2 IsNot Nothing Then
                f = New FieldReference(t2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal op2 As ObjectProperty, ByVal operation As FilterOperation)
            Dim f As FieldReference = Nothing
            If op.Entity IsNot Nothing Then
                f = New FieldReference(op)
            End If
            _l = f

            If op2.Entity IsNot Nothing Then
                f = New FieldReference(op2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal cf As CustomValue, ByVal operation As FilterOperation)
            Dim f As FieldReference = Nothing
            If op.Entity IsNot Nothing Then
                f = New FieldReference(op)
            End If
            _l = f

            If cf IsNot Nothing Then
                f = New FieldReference(cf)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal entityName2 As String, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If Not String.IsNullOrEmpty(entityName) Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName), propertyAlias)
            'End If
            '_e1 = p

            'p = Nothing
            'If Not String.IsNullOrEmpty(entityName2) Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName2), propertyAlias2)
            'End If
            '_e2 = p
            Dim f As FieldReference = Nothing
            If Not String.IsNullOrEmpty(entityName) Then
                f = New FieldReference(entityName, propertyAlias)
            End If
            _l = f

            If Not String.IsNullOrEmpty(entityName2) Then
                f = New FieldReference(entityName2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal t2 As Type, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim t As Pair(Of SourceFragment, String) = Nothing
            'If table IsNot Nothing Then
            '    t = New Pair(Of SourceFragment, String)(table, column)
            'End If
            '_t1 = t

            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If t2 IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(t2), propertyAlias2)
            'End If
            '_e2 = p
            Dim f As FieldReference = Nothing
            If table IsNot Nothing Then
                f = New FieldReference(table, column)
            End If
            _l = f

            If t2 IsNot Nothing Then
                f = New FieldReference(t2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal os As EntityUnion, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim t As Pair(Of SourceFragment, String) = Nothing
            'If table IsNot Nothing Then
            '    t = New Pair(Of SourceFragment, String)(table, column)
            'End If
            '_t1 = t

            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If os IsNot Nothing Then
            '    p = New Pair(Of ObjectSource, String)(os, propertyAlias2)
            'End If
            '_e2 = p
            Dim f As FieldReference = Nothing
            If table IsNot Nothing Then
                f = New FieldReference(table, column)
            End If
            _l = f

            If os IsNot Nothing Then
                f = New FieldReference(os, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal prop As ObjectProperty, ByVal operation As FilterOperation)
            Dim f As FieldReference = Nothing
            If table IsNot Nothing Then
                f = New FieldReference(table, column)
            End If
            _l = f

            If prop.Entity IsNot Nothing Then
                f = New FieldReference(prop)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal entityName2 As String, ByVal propertyAlias2 As String, ByVal operation As FilterOperation)
            'Dim t As Pair(Of SourceFragment, String) = Nothing
            'If table IsNot Nothing Then
            '    t = New Pair(Of SourceFragment, String)(table, column)
            'End If
            '_t1 = t

            'Dim p As Pair(Of ObjectSource, String) = Nothing
            'If Not String.IsNullOrEmpty(entityName2) Then
            '    p = New Pair(Of ObjectSource, String)(New ObjectSource(entityName2), propertyAlias2)
            'End If
            '_e2 = p
            Dim f As FieldReference = Nothing
            If table IsNot Nothing Then
                f = New FieldReference(table, column)
            End If
            _l = f

            If Not String.IsNullOrEmpty(entityName2) Then
                f = New FieldReference(entityName2, propertyAlias2)
            End If
            _r = f

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal operation As FilterOperation)
            'Dim t As Pair(Of SourceFragment, String) = Nothing
            'If table IsNot Nothing Then
            '    t = New Pair(Of SourceFragment, String)(table, column)
            'End If
            '_t1 = t

            't = Nothing
            'If table2 IsNot Nothing Then
            '    t = New Pair(Of SourceFragment, String)(table2, column2)
            'End If
            '_t2 = t
            Dim f As FieldReference = Nothing
            If table IsNot Nothing Then
                f = New FieldReference(table, column)
            End If
            _l = f

            If table2 IsNot Nothing Then
                f = New FieldReference(table2, column2)
            End If
            _r = f

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

#End Region

        Public Function SetUnion(ByVal eu As EntityUnion) As IFilter Implements IFilter.SetUnion
            If _eu Is Nothing Then
                _eu = eu
            Else
                _eu2 = eu
            End If
            Return Me
        End Function

        Public ReadOnly Property Left() As FieldReference
            Get
                Return _l
            End Get
        End Property

        Public Property Right() As FieldReference
            Get
                Return _r
            End Get
            Set(ByVal value As FieldReference)
                _r = value
            End Set
        End Property

        Public Function ReplaceFilter(ByVal oldValue As Core.IFilter, ByVal newValue As Core.IFilter) As Core.IFilter Implements Core.IFilter.ReplaceFilter
            If Not Equals(oldValue) Then
                Return Nothing
            End If
            Return newValue
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
            'Dim v1 As Object = _e1
            'Dim ve1 As Object = obj._e1
            'If _e1 Is Nothing Then
            '    If _t1 Is Nothing Then
            '        'v1 = _d1
            '        've1 = obj._d1
            '    Else
            '        v1 = _t1
            '        ve1 = obj._t1
            '    End If
            'End If

            'Dim v2 As Object = _e2
            'Dim ve2 As Object = obj._e2
            'If v2 Is Nothing Then
            '    If _t2 Is Nothing Then
            '        'v2 = _d2
            '        've2 = obj._d2
            '    Else
            '        v2 = _t2
            '        ve2 = obj._e2
            '    End If
            'End If

            'Dim b As Boolean = (Equals(v1, ve1) AndAlso Equals(v2, ve2)) _
            '    OrElse (Equals(v1, ve2) AndAlso Equals(v2, ve1))

            'Return b

            Return (_l.Equals(obj._l) AndAlso _r.Equals(obj._r)) OrElse _
                (_l.Equals(obj._r) AndAlso _r.Equals(obj._l))
        End Function

        Public Function GetAllFilters() As IFilter() Implements Core.IFilter.GetAllFilters
            Return New JoinFilter() {Me}
        End Function

        Private Function Equals1(ByVal f As Core.IFilter) As Boolean Implements Core.IFilter.Equals
            Return Equals(TryCast(f, JoinFilter))
        End Function

        Private Function _ToString() As String Implements Core.IFilter._ToString
            Dim sb As New StringBuilder

            'If _e1 IsNot Nothing Then
            '    sb.Append(_e1.First.ToStaticString).Append(_e1.Second).Append(" - ")
            'ElseIf _t1 IsNot Nothing Then
            '    sb.Append(_t1.First.RawName).Append(_t1.Second).Append(" - ")
            '    'ElseIf _d1 IsNot Nothing Then
            '    '    sb.Append(_d1.First).Append(_d1.Second).Append(" - ")
            '    'Else
            '    '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            'End If

            'If _e2 IsNot Nothing Then
            '    sb.Append(_e2.First.ToStaticString).Append(_e2.Second).Append(" - ")
            'ElseIf _t2 IsNot Nothing Then
            '    sb.Append(_t2.First.RawName).Append(_t2.Second).Append(" - ")
            '    'ElseIf _d2 IsNot Nothing Then
            '    '    sb.Append(_d2.First).Append(_d2.Second).Append(" - ")
            '    'Else
            '    '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            'End If

            sb.Append(_l._ToString).Append(" - ").Append(_r._ToString).Append(" - ")
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

        'Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String Implements Core.IFilter.MakeQueryStmt
        '    Return MakeQueryStmt(schema, stmt, filterInfo, almgr, pname)
        'End Function

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String Implements Core.IFilter.MakeQueryStmt

            Dim [alias] As String = String.Empty
            Dim alias2 As String = String.Empty

            Dim map As MapField2Column = Nothing
            Dim os As EntityUnion = Nothing
            If _l.Property.Entity IsNot Nothing Then
                Dim lt As Type = _l.Property.Entity.GetRealType(schema)
                'Dim oschema As IEntitySchema = schema.GetEntitySchema(lt, False)
                'If oschema Is Nothing Then
                '    oschema = executor.GetEntitySchema(lt)
                'End If
                Dim oschema As IEntitySchema = Nothing
                If executor Is Nothing Then
                    oschema = schema.GetEntitySchema(lt)
                Else
                    oschema = executor.GetEntitySchema(schema, lt)
                End If

                If _l.Property.Entity.IsQuery Then
                    Dim f As String = _l.Property.GetPropertyAlias(schema, oschema)
                    'If executor.GetFieldColumnMap(oschema, lt).ContainsKey(f) Then
                    '    If executor Is Nothing Then
                    '        map = oschema.GetFieldColumnMap(f)
                    '    Else
                    '        map = executor.GetFieldColumnMap(oschema, lt)(f)
                    '    End If
                    '    map = New MapField2Column(Nothing, map.ColumnExpression, _l.Property.Entity.ObjectAlias.Tbl)
                    'Else
                    '    map = New MapField2Column(Nothing, f, _l.Property.Entity.ObjectAlias.Tbl)
                    'End If
                    map = New MapField2Column(f, executor.FindColumn(schema, f), _l.Property.Entity.ObjectAlias.Tbl)
                    os = _l.Property.Entity
                Else 'If _l.Property.Entity IsNot Nothing AndAlso _eu IsNot Nothing Then
                    Dim f As String = _l.Property.GetPropertyAlias(schema, oschema)
                    If executor Is Nothing Then
                        map = oschema.FieldColumnMap(f)
                    Else
                        map = executor.GetFieldColumnMap(oschema, lt)(f)
                    End If
                    If almgr.ContainsKey(map.Table, _l.Property.Entity) Then
                        os = _l.Property.Entity
                    ElseIf _eu IsNot Nothing AndAlso almgr.ContainsKey(map.Table, _eu) Then
                        os = _eu
                    ElseIf _eu2 IsNot Nothing AndAlso almgr.ContainsKey(map.Table, _eu2) Then
                        os = _eu2
                    Else 'If almgr.ContainsKey(map2._tableName, _r.Property.Entity) Then
                        os = _l.Property.Entity
                    End If
                    'Else
                    '    Dim f As String = _l.Property.GetPropertyAlias(schema, oschema)
                    '    map = oschema.GetFieldColumnMap(f)
                    '    os = If(_eu IsNot Nothing, _eu, _l.Property.Entity)
                End If
                'ElseIf _d1 IsNot Nothing Then
                '    map = schema.GetObjectSchema(schema.GetTypeByEntityName(_d1.First)).GetFieldColumnMap(_d1.Second)
            ElseIf _l.Column IsNot Nothing Then
                map = New MapField2Column(Nothing, _l.Column.Second, _l.Column.First)
                If almgr.ContainsKey(map.Table, _eu) Then
                    os = _eu
                ElseIf almgr.ContainsKey(map.Table, _eu2) Then
                    os = _eu2
                End If
            ElseIf _l.CustomTemplate IsNot Nothing Then
                [alias] = _l.CustomTemplate.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
            Else
                Throw New InvalidOperationException
            End If

            Dim map2 As MapField2Column = Nothing
            Dim os2 As EntityUnion = Nothing
            If _r.Property.Entity IsNot Nothing Then
                Dim rt As Type = _r.Property.Entity.GetRealType(schema)
                'Dim oschema As IEntitySchema = schema.GetEntitySchema(rt, False)
                'If oschema Is Nothing Then
                '    oschema = executor.GetEntitySchema(rt)
                'End If
                Dim oschema As IEntitySchema = Nothing
                If executor Is Nothing Then
                    oschema = schema.GetEntitySchema(rt)
                Else
                    oschema = executor.GetEntitySchema(schema, rt)
                End If
                If _r.Property.Entity.IsQuery Then
                    Dim f As String = _r.Property.GetPropertyAlias(schema, oschema)
                    'If executor.GetFieldColumnMap(oschema, rt).ContainsKey(f) Then
                    '    If executor Is Nothing Then
                    '        map2 = oschema.GetFieldColumnMap(f)
                    '    Else
                    '        map2 = executor.GetFieldColumnMap(oschema, rt)(f)
                    '    End If
                    '    map2 = New MapField2Column(Nothing, map2.ColumnExpression, _r.Property.Entity.ObjectAlias.Tbl)
                    'Else
                    '    map2 = New MapField2Column(Nothing, f, _r.Property.Entity.ObjectAlias.Tbl)
                    'End If
                    map2 = New MapField2Column(f, executor.FindColumn(schema, f), _r.Property.Entity.ObjectAlias.Tbl)
                    os2 = _r.Property.Entity
                Else 'If _r.Property.Entity IsNot Nothing Then
                    Dim f As String = _r.Property.GetPropertyAlias(schema, oschema)
                    If executor Is Nothing Then
                        map2 = oschema.FieldColumnMap(f)
                    Else
                        map2 = executor.GetFieldColumnMap(oschema, rt)(f)
                    End If
                    If almgr.ContainsKey(map.Table, _r.Property.Entity) Then
                        os2 = _r.Property.Entity
                    ElseIf _eu IsNot Nothing AndAlso almgr.ContainsKey(map2.Table, _eu) Then
                        os2 = _eu
                    ElseIf _eu2 IsNot Nothing AndAlso almgr.ContainsKey(map2.Table, _eu2) Then
                        os2 = _eu2
                    Else 'If almgr.ContainsKey(map2._tableName, _r.Property.Entity) Then
                        os2 = _r.Property.Entity
                    End If
                    'Else
                    '    Dim f As String = _r.Property.GetPropertyAlias(schema, oschema)
                    '    map2 = oschema.GetFieldColumnMap(f)
                    '    os2 = If(_eu IsNot Nothing, _eu, _r.Property.Entity)
                End If
                'ElseIf _d2 IsNot Nothing Then
                '    map = schema.GetObjectSchema(schema.GetTypeByEntityName(_d2.First)).GetFieldColumnMap(_d2.Second)
            ElseIf _r.Column IsNot Nothing Then
                map2 = New MapField2Column(Nothing, _r.Column.Second, _r.Column.First)
                If almgr.ContainsKey(map2.Table, _eu) Then
                    os2 = _eu
                ElseIf almgr.ContainsKey(map2.Table, _eu2) Then
                    os2 = _eu2
                End If
            ElseIf _r.CustomTemplate IsNot Nothing Then
                [alias2] = _r.CustomTemplate.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
            Else
                Throw New InvalidOperationException
            End If

            If String.IsNullOrEmpty([alias]) AndAlso almgr IsNot Nothing Then
                'Debug.Assert(almgr.ContainsKey(map.Table, os), "There is not alias for table " & map.Table.RawName)
                Try
                    [alias] = almgr.GetAlias(map.Table, os) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & map.Table.RawName, ex)
                End Try
            End If


            If String.IsNullOrEmpty(alias2) AndAlso almgr IsNot Nothing Then
                'Debug.Assert(almgr.ContainsKey(map2.Table, os2), "There is not alias for table " & map2.Table.RawName)
                Try
                    alias2 = almgr.GetAlias(map2.Table, os2) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & map2.Table.RawName, ex)
                End Try
            End If

            Dim lp As String = [alias]
            If map IsNot Nothing Then
                lp &= map.SourceFieldExpression
            End If

            Dim rp As String = alias2
            If map2 IsNot Nothing Then
                rp &= map2.SourceFieldExpression
            End If

            Return lp & stmt.Oper2String(_oper) & rp
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
            Return Clone()
        End Function

        Private Function _CloneF() As Core.IFilter Implements Core.IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function
        Public Function Clone() As JoinFilter
            Dim c As New JoinFilter
            CopyTo(c)
            Return c
        End Function

        Protected Overridable Function CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, JoinFilter))
        End Function

        Public Function CopyTo(ByVal obj As JoinFilter) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            With obj
                '._e1 = _e1
                '._e2 = _e2
                ._oper = _oper
                '._t1 = _t1
                '._t2 = _t2
                '._d1 = _d1
                '._d2 = _d2
                '._l = _l
                '._r = _r
            End With

            If _l IsNot Nothing Then
                obj._l = _l.clone
            End If

            If _r IsNot Nothing Then
                obj._r = _r.clone
            End If

            If _eu IsNot Nothing Then
                obj._eu = _eu.Clone
            End If

            If _eu2 IsNot Nothing Then
                obj._eu2 = _eu2.Clone
            End If

            Return True
        End Function

        Protected Shared Function ChangeEntityJoinToValue(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal value As IFilterValue) As IFilter
            For Each _fl As IFilter In source.GetAllFilters()
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                If fl IsNot Nothing Then
                    Dim f As IFilter = Nothing
                    If fl._l.Property.Entity IsNot Nothing AndAlso fl._l.Property.Entity.GetRealType(schema) Is t AndAlso fl._l.Property.PropertyAlias = propertyAlias Then
                        f = SetJF(fl._r, value, fl._oper)
                    ElseIf fl._r.Property.Entity IsNot Nothing AndAlso fl._r.Property.Entity.GetRealType(schema) Is t AndAlso fl._r.Property.PropertyAlias = propertyAlias Then
                        f = SetJF(fl._l, value, fl._oper)
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
        Protected Shared Function ChangeTableJoinToValue(gen As StmtGenerator, ByVal schema As ObjectMappingEngine, ByVal source As IFilter,
                                                         ByVal sf As SourceFragment, ByVal fields As IEnumerable(Of SourceField),
                                                         ByVal values As IEnumerable(Of IFilterValue)) As IFilter
            Dim l As New List(Of Pair(Of JoinFilter, IFilter))
            For Each _fl As IFilter In source.GetAllFilters()
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                If fl IsNot Nothing Then
                    Dim f As IFilter = Nothing
                    For i = 0 To fields.Count - 1
                        Dim fld = fields(i).SourceFieldExpression
                        Dim v = values(i)
                        If fl._l.Column IsNot Nothing AndAlso fl._l.Column.First Is sf AndAlso fl._l.Column.Second.Equals(fld, If(gen.CaseSensitive, StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase)) Then
                            f = SetJF(fl._r, v, fl._oper)
                        ElseIf fl._r.Column IsNot Nothing AndAlso fl._r.Column.First Is sf AndAlso fl._r.Column.Second.Equals(fld, If(gen.CaseSensitive, StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase)) Then
                            f = SetJF(fl._l, v, fl._oper)
                        End If

                        If f IsNot Nothing Then
                            l.Add(New Pair(Of JoinFilter, IFilter)(fl, f))
                            Exit For
                        End If
                    Next
                End If

            Next

            If l.Count > 0 Then
                For Each p In l
                    source = source.ReplaceFilter(p.First, p.Second)
                Next

                Return source
            End If

            Return Nothing
        End Function
        Private Shared Function SetJF(ByVal fr As FieldReference, _
                               ByVal value As IFilterValue, ByVal oper As FilterOperation) As IFilter
            If fr.Property.Entity IsNot Nothing Then
                Return New EntityFilter(fr.Property, value, oper)
            Else
                Return New TableFilter(fr.Column.First, fr.Column.Second, value, oper)
            End If
        End Function

        'Private Shared Function SetJF(ByVal e As Pair(Of ObjectSource, String), ByVal t As Pair(Of SourceFragment, String), _
        '                       ByVal d As Pair(Of String), ByVal value As IParamFilterValue, ByVal oper As FilterOperation) As IFilter
        '    If e IsNot Nothing Then
        '        Return New EntityFilter(e.First, e.Second, value, oper)
        '    ElseIf t IsNot Nothing Then
        '        Return New TableFilter(t.First, t.Second, value, oper)
        '        'ElseIf d IsNot Nothing Then
        '        '    Return New Criteria.Core.EntityFilter(d.First, d.Second, value, oper)
        '    Else
        '        Throw New InvalidOperationException
        '    End If
        'End Function

        Public Shared Function ChangeEntityJoinToLiteral(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal literal As String) As IFilter
            Return ChangeEntityJoinToValue(schema, source, t, propertyAlias, New LiteralValue(literal))
        End Function
        Public Shared Function ChangeTableJoinToLiteral(gen As StmtGenerator, ByVal schema As ObjectMappingEngine, ByVal source As IFilter,
                                                        ByVal sf As SourceFragment, ByVal fields As IEnumerable(Of SourceField), ByVal literal As String) As IFilter
            Return ChangeTableJoinToValue(gen, schema, source, sf, fields, {New LiteralValue(literal)})
        End Function
        Public Shared Function ChangeEntityJoinToParam(ByVal schema As ObjectMappingEngine, ByVal source As IFilter, ByVal t As Type, ByVal propertyAlias As String, ByVal value As TypeWrap(Of Object)) As IFilter
            Return ChangeEntityJoinToValue(schema, source, t, propertyAlias, New ScalarValue(value.Value))
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            'do nothing
        End Sub

        Public Function RemoveFilter(ByVal f As Core.IFilter) As IFilter Implements Core.IFilter.RemoveFilter
            If Equals(f) Then
                Return Nothing
                'Throw New InvalidOperationException("Cannot remove self")
            End If
            Return Me
        End Function
    End Class
End Namespace