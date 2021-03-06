﻿Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Query
Imports System.Collections.Generic

Namespace Criteria.Joins
    Public Enum JoinType
        Join
        LeftOuterJoin
        RightOuterJoin
        FullJoin
        CrossJoin
        InnerApply
        OuterApply
    End Enum

    <Serializable()> _
    Public Class QueryJoin
        Implements IQueryElement

        Protected _table As SourceFragment
        Protected _joinType As Worm.Criteria.Joins.JoinType
        Protected _condition As Core.IFilter
        Private _tt As SourceFragment
        'Protected _en As String
        Protected _src As EntityUnion
        Private _jos As EntityUnion
        Private _key As String
        Protected Sub New()
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _table = table
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New EntityUnion(type)
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = os
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New EntityUnion([alias])
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal entityName As String, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New EntityUnion(entityName)
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal joinEntityType As Type)
            _src = New EntityUnion(type)
            _joinType = joinType
            _condition = Condition
            _jos = New EntityUnion(joinEntityType)
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal joinEntityName As String)
            _src = New EntityUnion(type)
            _joinType = joinType
            _condition = Condition
            _jos = New EntityUnion(joinEntityName)
        End Sub

        Public Shared Function IsEmpty(ByVal j As QueryJoin) As Boolean
            Return j Is Nothing
        End Function

        Public Function MakeSQLStmt(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal schema As StmtGenerator, ByVal executor As IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, _
            ByVal os As EntityUnion, ByVal sb As StringBuilder) As SourceFragment
            'If IsEmpty Then
            '    Throw New InvalidOperationException("Object must be created")
            'End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException(NameOf(almgr))
            End If

            Dim os_ As EntityUnion = ObjectSource
            If os_ Is Nothing Then
                os_ = os
            End If

            If _joinType < Joins.JoinType.CrossJoin Then
                If Condition Is Nothing Then
                    Throw New InvalidOperationException("Join condition must be specified")
                End If

                For Each f As IFilter In Condition.GetAllFilters
                    If os IsNot Nothing Then
                        f.SetUnion(os)
                    End If
                    If ObjectSource IsNot Nothing Then
                        f.SetUnion(ObjectSource)
                    End If
                Next

                If os_ Is Nothing OrElse Not os_.IsQuery Then
                    Dim tbl As SourceFragment = _table
                    Dim hint = CoreFramework.StringExtensions.Coalesce(Me.Hint, ObjectSource?.Hint, tbl?.Hint)
                    If tbl Is Nothing Then
                        'If _type IsNot Nothing Then
                        '    tbl = schema.GetTables(_type)(0)
                        'Else
                        '    tbl = schema.GetTables(schema.GetTypeByEntityName(_en))(0)
                        'End If
                        tbl = mpe.GetTables(ObjectSource.GetRealType(mpe))(0)
                    End If

                    Dim alTable As SourceFragment = tbl
                    'Dim tableAliases As IDictionary(Of SourceFragment, String) = almgr.Aliases
                    If Not almgr.ContainsKey(tbl, os_) Then
                        almgr.AddTable(tbl, os_, pname)
                    End If

                    sb.Append(JoinTypeString()).Append(schema.GetTableName(tbl, contextInfo))
                    sb.Append(" " & almgr.GetAlias(alTable, os_))
                    If Not String.IsNullOrEmpty(hint) Then
                        sb.Append(" ").Append(hint)
                    End If
                    sb.Append(" on ")
                    sb.Append(Condition.MakeQueryStmt(mpe, fromClause, schema, executor, contextInfo, almgr, pname))
                    Return alTable
                Else
                    sb.Append(JoinTypeString()).Append("(")

                    Dim al As QueryAlias = os_.ObjectAlias
                    Dim q As QueryCmd = al.Query
                    sb.Append(schema.MakeQueryStatement(mpe, q.FromClause, contextInfo, q, pname, AliasMgr.Create))

                    Dim tbl2 As SourceFragment = al.Tbl
                    If tbl2 Is Nothing Then
                        tbl2 = New SourceFragment
                        al.Tbl = tbl2
                    End If

                    Dim als As String = almgr.AddTable(tbl2, os_)

                    sb.Append(") as ").Append(als).Append(" on ")
                    sb.Append(Condition.MakeQueryStmt(mpe, fromClause, schema, New CombineExecutor(al.Query, executor), contextInfo, almgr, pname))
                    'almgr.Replace(mpe, schema, tbl2, os_, sb)
                    Return tbl2
                End If
            ElseIf JoinType = Joins.JoinType.CrossJoin Then
                Dim tbl As SourceFragment = _table
                Dim hint = CoreFramework.StringExtensions.Coalesce(Me.Hint, ObjectSource?.Hint, tbl?.Hint)
                If tbl Is Nothing Then
                    tbl = mpe.GetTables(ObjectSource.GetRealType(mpe))(0)
                End If

                Dim alTable As SourceFragment = tbl
                If Not almgr.ContainsKey(tbl, os_) Then
                    almgr.AddTable(tbl, os_, pname)
                End If

                sb.Append(JoinTypeString()).Append(schema.GetTableName(tbl, contextInfo))
                sb.Append(" ").Append(almgr.GetAlias(alTable, os_))
                If Not String.IsNullOrEmpty(hint) Then
                    sb.Append(" ").Append(hint)
                End If
                Return alTable
            ElseIf os_ IsNot Nothing AndAlso os_.IsQuery Then
                sb.Append(JoinTypeString()).Append("(")

                Dim al As QueryAlias = os_.ObjectAlias
                Dim q As QueryCmd = al.Query
                sb.Append(schema.MakeQueryStatement(mpe, q.FromClause, contextInfo, q, pname, almgr))

                Dim tbl2 As SourceFragment = al.Tbl
                If tbl2 Is Nothing Then
                    tbl2 = New SourceFragment
                    al.Tbl = tbl2
                End If

                Dim als As String = almgr.AddTable(tbl2, os_)

                sb.Append(") as ").Append(als)
                'almgr.Replace(mpe, schema, tbl2, os_, sb)
                Return tbl2
            Else
                Throw New NotSupportedException(JoinType.ToString)
            End If
        End Function

        Public Function JoinTypeString() As String
            Return JoinTypeString(_joinType)
        End Function

        Public Shared Function JoinTypeString(ByVal type As JoinType) As String
            Select Case type
                Case Worm.Criteria.Joins.JoinType.Join
                    Return " join "
                Case Worm.Criteria.Joins.JoinType.LeftOuterJoin
                    Return " left join "
                Case Worm.Criteria.Joins.JoinType.RightOuterJoin
                    Return " right join "
                Case Worm.Criteria.Joins.JoinType.FullJoin
                    Return " full join "
                Case Worm.Criteria.Joins.JoinType.CrossJoin
                    Return " cross join "
                Case Joins.JoinType.InnerApply
                    Return " cross apply "
                Case Joins.JoinType.OuterApply
                    Return " outer apply "
                Case Else
                    Throw New ObjectMappingException("invalid join type " & type.ToString)
            End Select
        End Function

        Public Property Condition() As Core.IFilter
            Get
                Return _condition
            End Get
            Protected Friend Set(ByVal value As Core.IFilter)
                _condition = value
            End Set
        End Property

        Public Sub ReplaceFilter(ByVal oldValue As Core.IFilter, ByVal newValue As Core.IFilter)
            _condition = _condition.ReplaceFilter(oldValue, newValue)
        End Sub

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition.GetStaticString(mpe)
                'ElseIf _type IsNot Nothing Then
                '    Return gs(_type.ToString, mpe)
                'Else
                '    Return gs(_en, mpe)
            Else
                Return gs(_src.ToStaticString(mpe), mpe)
            End If
        End Function

        Private Function gs(ByVal s As String, ByVal mpe As ObjectMappingEngine) As String
            If _condition IsNot Nothing Then
                Return s & JoinTypeString() & _condition.GetStaticString(mpe)
            ElseIf _jos IsNot Nothing Then
                Return s & JoinTypeString() & _jos.ToStaticString(mpe) & _key
            Else
                Return s & JoinTypeString()
            End If
        End Function

        Public Overrides Function ToString() As String
            Throw New NotImplementedException
        End Function

        Public Function _ToString() As String Implements IQueryElement._ToString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition._ToString
                'ElseIf _type IsNot Nothing Then
                '    Return gd(_type.ToString)
                'Else
                '    Return gd(_en)
            Else
                Return gd(_src._ToString)
            End If
        End Function

        Private Function gd(ByVal s As String) As String
            If _condition IsNot Nothing Then
                Return s & JoinTypeString() & _condition._ToString
            ElseIf _jos IsNot Nothing Then
                Return s & JoinTypeString() & _jos._ToString & _key
            Else
                Return s & JoinTypeString()
            End If
        End Function

        Public Property M2MKey() As String
            Get
                Return _key
            End Get
            Set(ByVal value As String)
                _key = value
            End Set
        End Property

        Public Property M2MObjectSource() As EntityUnion
            Get
                Return _jos
            End Get
            Set(ByVal value As EntityUnion)
                _jos = value
            End Set
        End Property

        Protected Friend Property TmpTable() As SourceFragment
            Get
                Return _tt
            End Get
            Set(ByVal value As SourceFragment)
                _tt = value
            End Set
        End Property

        'Public Property M2MJoinEntityName() As String
        '    Get
        '        Return _jos.EntityName
        '    End Get
        '    Set(ByVal value As String)
        '        _jos = New EntityUnion(value)
        '    End Set
        'End Property

        Public Property ObjectSource() As EntityUnion
            Get
                Return _src
            End Get
            Friend Set(ByVal value As EntityUnion)
                _src = value
            End Set
        End Property

        'Public Property Type() As Type
        '    Get
        '        Return _type
        '    End Get
        '    Friend Set(ByVal value As Type)
        '        _type = value
        '    End Set
        'End Property

        'Public Property EntityName() As String
        '    Get
        '        Return _en
        '    End Get
        '    Friend Set(ByVal value As String)
        '        _en = value
        '    End Set
        'End Property

        Public Property Table() As SourceFragment
            Get
                Return _table
            End Get
            Friend Set(ByVal value As SourceFragment)
                _table = value
            End Set
        End Property

        Public ReadOnly Property JoinType() As Worm.Criteria.Joins.JoinType
            Get
                Return _joinType
            End Get
        End Property

        Public Property Hint As String

        Public Sub InjectJoinFilter(ByVal schema As ObjectMappingEngine, ByVal t As Type, ByVal propertyAlias As String, ByVal table As SourceFragment, ByVal column As String)
            For Each _fl As Core.IFilter In _condition.GetAllFilters()
                Dim f As JoinFilter = Nothing
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                'Dim tm As Core.TemplateBase = Nothing
                If fl.Left.Property.Entity IsNot Nothing AndAlso fl.Left.Property.Entity.GetRealType(schema) Is t AndAlso fl.Left.Property.PropertyAlias = propertyAlias Then
                    If fl.Right.Property.Entity IsNot Nothing Then
                        f = CreateJoin(table, column, fl.Right.Property, fl._oper)
                        'tm = CreateOrmFilter(fl.Right.Property, fl._oper)
                    Else
                        f = CreateJoin(table, column, fl.Right.Table, fl.Right.Columns, fl._oper)
                        'tm = CreateTableFilter(fl.Right.Column, fl._oper)
                    End If
                End If
                If f Is Nothing Then
                    If fl.Right.Property.Entity IsNot Nothing AndAlso fl.Right.Property.Entity.GetRealType(schema) Is t AndAlso fl.Right.Property.PropertyAlias = propertyAlias Then
                        If fl.Left.Property.Entity IsNot Nothing Then
                            f = CreateJoin(table, column, fl.Left.Property, fl._oper)
                            'tm = CreateOrmFilter(fl.Left.Property, fl._oper)
                        Else
                            f = CreateJoin(table, column, fl.Left.Table, fl.Left.Columns, fl._oper)
                            'tm = CreateTableFilter(fl.Left.Column, fl._oper)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    ReplaceFilter(fl, f)
                    Return
                End If
            Next
            Return
        End Sub

        Protected Function CreateTableFilter(ByVal table As SourceFragment, ByVal column As String, ByVal oper As FilterOperation) As Core.TemplateBase
            Return New TableFilterTemplate(table, column, oper)
        End Function

        Protected Function CreateTableFilter(ByVal p As Pair(Of SourceFragment, String), ByVal oper As FilterOperation) As Core.TemplateBase
            Return New TableFilterTemplate(p.First, p.Second, oper)
        End Function

        Protected Function CreateOrmFilter(ByVal op As ObjectProperty, ByVal oper As FilterOperation) As Core.TemplateBase
            Return New OrmFilterTemplate(op, oper)
        End Function

        Protected Function CreateOrmFilter(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal oper As FilterOperation) As Core.TemplateBase
            Return New OrmFilterTemplate(os, propertyAlias, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, os, propertyAlias, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal op As ObjectProperty, ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, op, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, table2, column2, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, table2 As SourceFragment, columns As IEnumerable(Of ColumnPair), ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, table2, columns, oper)
        End Function

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As QueryJoin
            Dim j As New QueryJoin
            CopyTo(j)
            Return j
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            If _src IsNot Nothing AndAlso _src.AnyType Is Nothing AndAlso String.IsNullOrEmpty(_src.AnyEntityName) Then
                Dim root As QueryCmd = _src.ObjectAlias.Query
                'root._outer = New QueryCmd
                Try
                    For Each q As QueryCmd In New StmtQueryIterator(root)
                        Dim old As Boolean = q.AutoFields
                        Try
                            q.AutoFields = False
                            q.Prepare(executor, schema, contextInfo, stmt, isAnonym)
                        Finally
                            q.AutoFields = old
                        End Try
                    Next
                Finally
                    'root._outer = Nothing
                End Try
            End If
        End Sub

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, QueryJoin))
        End Function

        Public Function CopyTo(target As QueryJoin) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._joinType = _joinType

            If _table IsNot Nothing Then
                target._table = _table.Clone
            End If

            If _condition IsNot Nothing Then
                target._condition = _condition.Clone
            End If

            If _src IsNot Nothing Then
                target._src = _src.Clone
            End If

            If _jos IsNot Nothing Then
                target._jos = _jos.Clone
            End If

            target.Hint = Hint
            Return True
        End Function
    End Class

End Namespace