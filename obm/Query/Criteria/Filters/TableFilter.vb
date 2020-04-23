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
    Public Class TableFilter
        Inherits Worm.Criteria.Core.TemplatedFilterBase

        Private Const TempTable As String = "calculated"
        'Implements ITemplateFilter

        'Private _templ As TableFilterTemplate

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(table, column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Public Sub New(ByVal column As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(New SourceFragment(TempTable), column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Friend Sub New(ByVal v As IFilterValue, ByVal template As TemplateBase)
            MyBase.New(v, template)
        End Sub

        Protected Overrides Function _ToString() As String
            'Return _templ.Table.TableName & _templ.Column & Value._ToString & _templ.OperToString
            Return Value._ToString & Template._ToString()
        End Function

        'Public Overrides Function GetStaticString() As String
        '    Return _templ.Table.TableName() & _templ.Column & _templ.Oper2String
        'End Function

        Public Shadows ReadOnly Property Template() As TableFilterTemplate
            Get
                Return CType(MyBase.Template, TableFilterTemplate)
            End Get
        End Property

        'Public Overloads Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
        '    Return MakeQueryStmt(schema, stmt, filterInfo, almgr, pname, Nothing)
        'End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext,
                                                ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If Value.ShouldUse Then
                If Template.Table.Name = TempTable Then
                    Return Template.Column & Template.OperToStmt(stmt) & GetParam(schema, fromClause, stmt, pname, False, almgr, contextInfo, executor)
                Else
                    'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                    'If almgr IsNot Nothing Then
                    '    tableAliases = almgr.Aliases
                    'End If

                    'Dim map As New MapField2Column(String.Empty, Template.Table, Template.Column)
                    Dim [alias] As String = String.Empty

                    If almgr IsNot Nothing Then
                        'Debug.Assert(almgr.ContainsKey(map.Table, _eu), "There is not alias for table " & map.Table.RawName)
                        Try
                            [alias] = almgr.GetAlias(Template.Table, _eu) & stmt.Selector
                        Catch ex As KeyNotFoundException
                            Throw New ObjectMappingException("There is not alias for table " & Template.Table.RawName, ex)
                        End Try
                    End If

                    Return [alias] & Template.Column & Template.OperToStmt(stmt) & GetParam(schema, fromClause, stmt, pname, False, almgr, contextInfo, executor)
                End If
            Else
                Return String.Empty
            End If
        End Function

        Public Overrides Function GetAllFilters() As IFilter()
            Return New TableFilter() {Me}
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator,
                                                      ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As IEnumerable(Of ITemplateFilterBase.ColParam)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim prname As String = Value.GetParam(schema, Nothing, stmt, pname, Nothing, Nothing, Nothing, False, Nothing)

            Return {New ITemplateFilterBase.ColParam With {.Column = Template.Column, .Param = prname}}
        End Function

        'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases
        '    Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
        '    Dim [alias] As String = String.Empty

        '    If tableAliases IsNot Nothing Then
        '        [alias] = tableAliases(map._tableName) & "."
        '    End If

        '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
        'End Function

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As TableFilter
            Dim vc As IFilterValue = Nothing
            If val IsNot Nothing Then
                vc = CType(val.Clone, IFilterValue)
            End If
            Return New TableFilter(vc, Template)
        End Function
    End Class
End Namespace