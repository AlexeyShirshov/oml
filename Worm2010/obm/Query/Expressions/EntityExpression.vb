﻿Imports Worm.Query
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Expressions2
    <Serializable>
    Public Class EntityExpression
        Implements IEntityPropertyExpression

        Private _op As ObjectProperty
        Private _eu As EntityUnion

        Protected Sub New()

        End Sub
        Public Sub New(ByVal op As ObjectProperty)
            _op = op
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal eu As EntityUnion)
            _op = New ObjectProperty(eu, propertyAlias)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal t As Type)
            _op = New ObjectProperty(t, propertyAlias)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal entityName As String)
            _op = New ObjectProperty(entityName, propertyAlias)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal ea As QueryAlias)
            _op = New ObjectProperty(ea, propertyAlias)
        End Sub

        Protected Overridable Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As EntityExpression
            Dim n As New EntityExpression
            CopyTo(n)
            Return n
        End Function
        'Public Property Entity() As Query.EntityUnion Implements IEntityPropertyExpression.Entity
        '    Get
        '        If _eu IsNot Nothing Then
        '            Return _eu
        '        Else
        '            Return _op.Entity
        '        End If
        '    End Get
        '    Set(ByVal value As Query.EntityUnion)
        '        _eu = value
        '    End Set
        'End Property

        'Public Property PropertyAlias() As String Implements IEntityPropertyExpression.PropertyAlias
        '    Get
        '        Return _op.PropertyAlias
        '    End Get
        '    Set(ByVal value As String)
        '        _op = New ObjectProperty(Entity, _op.PropertyAlias)
        '    End Set
        'End Property

        Public Function SetEntity(ByVal eu As Query.EntityUnion) As IContextable Implements IContextable.SetEntity
            _eu = eu
            Return Me
        End Function

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
            ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode,
            ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement

            Dim map As MapField2Column = Nothing
            Dim tbl As SourceFragment = Nothing
            Dim [alias] As String = String.Empty

            If _op.Entity.IsQuery Then
                tbl = _op.Entity.ObjectAlias.Tbl
                If tbl Is Nothing Then
                    tbl = New SourceFragment
                    _op.Entity.ObjectAlias.Tbl = tbl
                End If
                map = New MapField2Column(_op.PropertyAlias, executor.FindColumn(mpe, _op.PropertyAlias), tbl)
            Else
                Try
                    Dim t As Type = _op.Entity.GetRealType(mpe)
                    Dim oschema As IEntitySchema = Nothing

                    If executor Is Nothing Then
                        oschema = mpe.GetEntitySchema(t)
                        map = oschema.FieldColumnMap(_op.PropertyAlias)
                    Else
                        oschema = executor.GetEntitySchema(mpe, t)
                        map = executor.GetFieldColumnMap(oschema, t)(_op.PropertyAlias)
                    End If
                    tbl = map.Table
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException(String.Format("There is not column for property {0} ", _op.Entity.ToStaticString(mpe) & "." & _op.PropertyAlias, ex))
                End Try
            End If

            If almgr IsNot Nothing AndAlso (stmtMode And MakeStatementMode.WithoutTables) <> MakeStatementMode.WithoutTables Then
                'Debug.Assert(almgr.ContainsKey(tbl, _op.Entity))
                Try
                    [alias] = almgr.GetAlias(tbl, _op.Entity) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
                End Try
            Else
                [alias] = tbl.UniqueName(_op.Entity) & mpe.Delimiter
            End If

            Dim sb As New StringBuilder
            Dim lastPostfix As Integer = 1
            For Each sf As SourceField In map.SourceFields
                Dim idx_beg As Integer = sb.Length
                sb.Append([alias] & sf.SourceFieldExpression)

                Dim args As New IEntityPropertyExpression.FormatBehaviourArgs
                RaiseEvent FormatBehaviour(Me, args)

                If args.NeedAlias Then
                    If Not String.IsNullOrEmpty(sf.SourceFieldAlias) AndAlso (stmtMode And MakeStatementMode.AddColumnAlias) = MakeStatementMode.AddColumnAlias Then
                        sb.Append(" ").Append(sf.SourceFieldAlias)
                    End If
                End If

                If args.CustomStatement IsNot Nothing Then
                    If args.CustomStatement.FromLeft Then
                        sb.Insert(idx_beg, args.CustomStatement.MakeStatement(mpe, fromClause, stmt, paramMgr,
                            almgr, contextInfo, stmtMode, executor, sf))
                    Else
                        sb.Append(args.CustomStatement.MakeStatement(mpe, fromClause, stmt, paramMgr,
                            almgr, contextInfo, stmtMode, executor, sf))
                    End If
                    sb.Append(" and ")
                    lastPostfix = 5
                Else
                    sb.Append(",")
                End If

            Next

            sb.Length -= lastPostfix

            Return sb.ToString
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, EntityExpression))
        End Function

        Public Overloads Function Equals(ByVal f As EntityExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _op.Equals(f._op)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _op.Entity._ToString & "$" & _op.PropertyAlias
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return GetDynamicString()
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator,
                           ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _op.Entity.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub

        Public Property ObjectProperty() As Query.ObjectProperty Implements IEntityPropertyExpression.ObjectProperty
            Get
                Return _op
            End Get
            Set(ByVal value As Query.ObjectProperty)
                _op = value
            End Set
        End Property

        <NonSerialized()>
        Public Event FormatBehaviour(ByVal sender As IEntityPropertyExpression, ByVal args As IEntityPropertyExpression.FormatBehaviourArgs) Implements IEntityPropertyExpression.FormatBehaviour

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, EntityExpression))
        End Function

        Public Function CopyTo(target As EntityExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._op = _op.Clone
            If _eu IsNot Nothing Then
                target._eu = _eu.Clone
            End If

            Return True
        End Function
    End Class
End Namespace