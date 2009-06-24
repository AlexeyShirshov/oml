Imports Worm.Query
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Expressions2
    Public Class EntityExpression
        Implements IEntityPropertyExpression

        Private _op As ObjectProperty
        Private _eu As EntityUnion

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

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Return New EntityExpression(_op).SetEntity(_eu)
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

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, _
            ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement

            Dim map As MapField2Column = Nothing
            Dim tbl As SourceFragment = Nothing
            Dim [alias] As String = String.Empty

            Try
                Dim t As Type = _op.Entity.GetRealType(mpe)
                Dim oschema As IEntitySchema = mpe.GetEntitySchema(t)
                If executor Is Nothing Then
                    map = oschema.GetFieldColumnMap(_op.PropertyAlias)
                Else
                    map = executor.GetFieldColumnMap(oschema, t)(_op.PropertyAlias)
                End If
                tbl = map.Table
            Catch ex As KeyNotFoundException
                Throw New ObjectMappingException(String.Format("There is not column for property {0} ", _op.Entity.ToStaticString(mpe, contextFilter) & "." & _op.PropertyAlias, ex))
            End Try

            If almgr IsNot Nothing Then
                Try
                    [alias] = almgr.GetAlias(tbl, _op.Entity) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & tbl.RawName, ex)
                End Try
            End If

            Return [alias] & map.ColumnExpression
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

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return GetDynamicString()
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Property ObjectProperty() As Query.ObjectProperty Implements IEntityPropertyExpression.ObjectProperty
            Get
                Return _op
            End Get
            Set(ByVal value As Query.ObjectProperty)
                _op = value
            End Set
        End Property
    End Class
End Namespace