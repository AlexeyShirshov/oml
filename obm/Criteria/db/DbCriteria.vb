Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports cc = Worm.Criteria.Core

Imports Worm.Database.Criteria.Values
Imports Worm.Database.Criteria.Joins
Imports Worm.Database.Criteria.Conditions
Imports Worm.Database.Criteria.Core

Namespace Database
    Namespace Criteria

        Public Class Ctor
            Implements ICtor

            Private _en As String
            Private _t As Type
            Private _tbl As Worm.Orm.Meta.SourceFragment
            Private _stmtGen As SQLGenerator

            Public Sub New(ByVal t As Type)
                MyClass.new(Nothing, t)
            End Sub

            Public Sub New(ByVal tbl As Orm.Meta.SourceFragment)
                MyClass.New(Nothing, tbl)
            End Sub

            Public Sub New(ByVal entityName As String)
                MyClass.New(Nothing, entityName)
            End Sub

            Public Sub New(ByVal stmtGen As SQLGenerator, ByVal t As Type)
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                _t = t
                _stmtGen = stmtGen
            End Sub

            Public Sub New(ByVal stmtGen As SQLGenerator, ByVal tbl As Orm.Meta.SourceFragment)
                If tbl Is Nothing Then
                    Throw New ArgumentNullException("tbl")
                End If

                _tbl = tbl
                _stmtGen = stmtGen
            End Sub

            Public Sub New(ByVal stmtGen As SQLGenerator, ByVal entityName As String)
                _stmtGen = stmtGen
                _en = entityName
            End Sub

            Protected Function _Field(ByVal fieldName As String) As Worm.Criteria.CriteriaField Implements ICtor.Field
                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                If _t Is Nothing AndAlso Not String.IsNullOrEmpty(_en) Then
                    Return New CriteriaField(_stmtGen, _en, fieldName)
                Else
                    Return New CriteriaField(_stmtGen, _t, fieldName)
                End If
            End Function

            Public Function Field(ByVal fieldName As String) As Criteria.CriteriaField
                Return CType(_Field(fieldName), CriteriaField)
            End Function

            Public Shared Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField
                Return Field(Nothing, t, fieldName)
            End Function

            Public Shared Function Field(ByVal entityName As String, ByVal fieldName As String) As CriteriaField
                Return Field(Nothing, entityName, fieldName)
            End Function

            Public Shared Function Field(ByVal objectField As ObjectField) As CriteriaField
                Return Field(Nothing, objectField.EntityName, objectField.Field)
            End Function

            Public Shared Function Field(ByVal stmtGen As SQLGenerator, ByVal t As Type, ByVal fieldName As String) As CriteriaField
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(stmtGen, t, fieldName)
            End Function

            Public Shared Function Field(ByVal stmtGen As SQLGenerator, ByVal objField As ObjectField) As CriteriaField
                Return Field(stmtGen, objField.EntityName, objField.Field)
            End Function

            Public Shared Function Field(ByVal stmtGen As SQLGenerator, ByVal entityName As String, ByVal fieldName As String) As CriteriaField
                If String.IsNullOrEmpty(entityName) Then
                    Throw New ArgumentNullException("entityName")
                End If

                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(stmtGen, entityName, fieldName)
            End Function

            Public Shared Function ColumnOnInlineTable(ByVal columnName As String) As CriteriaColumn
                Return Column(Nothing, Nothing, columnName)
            End Function

            Public Shared Function Column(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
                Return Column(Nothing, table, columnName)
            End Function

            Public Shared Function Column(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
                'If table Is Nothing Then
                '    Throw New ArgumentNullException("table")
                'End If

                If String.IsNullOrEmpty(columnName) Then
                    Throw New ArgumentNullException("columnName")
                End If

                Return New CriteriaColumn(stmtGen, table, columnName)
            End Function

            Public Shared Function AutoTypeField(ByVal fieldName As String) As CriteriaField
                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(Nothing, CType(Nothing, Type), fieldName)
            End Function

            Public Shared Function Exists(ByVal t As Type, ByVal gf As cc.IGetFilter) As CriteriaLink
                Return New CriteriaLink(Nothing, New Condition.ConditionConstructor).AndExists(t, gf.Filter)
            End Function

            Public Shared Function NotExists(ByVal t As Type, ByVal gf As cc.IGetFilter) As CriteriaLink
                Return New CriteriaLink(Nothing, New Conditions.Condition.ConditionConstructor).AndNotExists(t, gf.Filter)
            End Function

            Public Shared Function Exists(ByVal t As Type, ByVal gf As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Return New CriteriaLink(Nothing, New Condition.ConditionConstructor).AndExists(t, gf.Filter, joins)
            End Function

            Public Shared Function NotExists(ByVal t As Type, ByVal gf As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Return New CriteriaLink(Nothing, New Conditions.Condition.ConditionConstructor).AndNotExists(t, gf.Filter, joins)
            End Function

            Public Shared Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaLink(Nothing, New Condition.ConditionConstructor).AndExists(cmd)
            End Function

            Public Shared Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaLink(Nothing, New Conditions.Condition.ConditionConstructor).AndNotExists(cmd)
            End Function

            Public Shared Function Custom(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(Nothing, format, values)
            End Function

            'Public Shared Function Custom(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format)
            'End Function

            Public Function Column(ByVal columnName As String) As CriteriaColumn
                Return CType(_Column(columnName), CriteriaColumn)
            End Function

            Protected Function _Column(ByVal columnName As String) As Worm.Criteria.CriteriaColumn Implements Worm.Criteria.ICtor.Column
                If String.IsNullOrEmpty(columnName) Then
                    Throw New ArgumentNullException("columnName")
                End If

                Return New CriteriaColumn(_stmtGen, _tbl, columnName)
            End Function
        End Class

        'Public Class SchemaCtor
        '    Implements IFullCtor
        '    Private _stmt As SQLGenerator

        '    Public Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField Implements IFullCtor.Field
        '        Return Field(_stmt, t, fieldName)
        '    End Function

        '    Public Function Field(ByVal entityName As String, ByVal fieldName As String) As CriteriaField Implements IFullCtor.Field
        '        Return Field(_stmt, entityName, fieldName)
        '    End Function

        '    Public Function Field(ByVal stmtGen As SQLGenerator, ByVal t As Type, ByVal fieldName As String) As CriteriaField
        '        If t Is Nothing Then
        '            Throw New ArgumentNullException("t")
        '        End If

        '        If String.IsNullOrEmpty(fieldName) Then
        '            Throw New ArgumentNullException("fieldName")
        '        End If

        '        Return New CriteriaField(stmtGen, t, fieldName)
        '    End Function

        '    Public Function Field(ByVal stmtGen As SQLGenerator, ByVal entityName As String, ByVal fieldName As String) As CriteriaField
        '        If String.IsNullOrEmpty(entityName) Then
        '            Throw New ArgumentNullException("entityName")
        '        End If

        '        If String.IsNullOrEmpty(fieldName) Then
        '            Throw New ArgumentNullException("fieldName")
        '        End If

        '        Return New CriteriaField(stmtGen, entityName, fieldName)
        '    End Function

        '    Public Function ColumnOnInlineTable(ByVal columnName As String) As CriteriaColumn
        '        Return Column(_stmt, Nothing, columnName)
        '    End Function

        '    Public Function Column(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
        '        Return Column(_stmt, table, columnName)
        '    End Function

        '    Public Function Column(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
        '        'If table Is Nothing Then
        '        '    Throw New ArgumentNullException("table")
        '        'End If

        '        If String.IsNullOrEmpty(columnName) Then
        '            Throw New ArgumentNullException("columnName")
        '        End If

        '        Return New CriteriaColumn(stmtGen, table, columnName)
        '    End Function

        '    Public Function AutoTypeField(ByVal fieldName As String) As CriteriaField
        '        If String.IsNullOrEmpty(fieldName) Then
        '            Throw New ArgumentNullException("fieldName")
        '        End If

        '        Return New CriteriaField(_stmt, CType(Nothing, Type), fieldName)
        '    End Function

        '    Public Function Exists(ByVal t As Type, ByVal gf As cc.IGetFilter) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Condition.ConditionConstructor).AndExists(t, gf.Filter)
        '    End Function

        '    Public Function NotExists(ByVal t As Type, ByVal gf As cc.IGetFilter) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Conditions.Condition.ConditionConstructor).AndNotExists(t, gf.Filter)
        '    End Function

        '    Public Function Exists(ByVal t As Type, ByVal gf As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Condition.ConditionConstructor).AndExists(t, gf.Filter, joins)
        '    End Function

        '    Public Function NotExists(ByVal t As Type, ByVal gf As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Conditions.Condition.ConditionConstructor).AndNotExists(t, gf.Filter, joins)
        '    End Function

        '    Public Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Condition.ConditionConstructor).AndExists(cmd)
        '    End Function

        '    Public Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
        '        Return New CriteriaLink(_stmt, New Conditions.Condition.ConditionConstructor).AndNotExists(cmd)
        '    End Function

        '    Public Function Custom(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
        '        Return New CustomCF(_stmt, format, values)
        '    End Function
        'End Class

        Public Class CriteriaColumn
            Inherits Worm.Criteria.CriteriaColumn

            Private _stmtGen As SQLGenerator

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment, ByVal column As String)
                MyBase.New(table, column)
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment, ByVal column As String, _
                ByVal con As Condition.ConditionConstructorBase, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(table, column, con, ct)
            End Sub

            Protected Overrides Function CreateFilter(ByVal v As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
                If Table Is Nothing Then
                    Return New TableFilter(Column, v, oper)
                Else
                    Return New TableFilter(Table, Column, v, oper)
                End If
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(_stmtGen, Table, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function
        End Class

        Public Class CriteriaField
            Inherits Worm.Criteria.CriteriaField

            Private _stmtGen As SQLGenerator

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal t As Type, ByVal fieldName As String)
                MyBase.New(t, fieldName)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal en As String, ByVal fieldName As String)
                MyBase.New(en, fieldName)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal t As Type, ByVal fieldName As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(t, fieldName, con, ct)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal entityName As String, ByVal fieldName As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(entityName, fieldName, con, ct)
                _stmtGen = stmtGen
            End Sub

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                If Type Is Nothing AndAlso Not String.IsNullOrEmpty(EntityName) Then
                    Return New CriteriaLink(_stmtGen, EntityName, CType(ConditionCtor, Condition.ConditionConstructor))
                Else
                    Return New CriteriaLink(_stmtGen, Type, CType(ConditionCtor, Condition.ConditionConstructor))
                End If
            End Function

            Protected Function GetLink2(ByVal fl As Worm.Criteria.Core.IFilter) As CriteriaLink
                Return CType(GetLink(fl), CriteriaLink)
            End Function

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As FilterOperation) As Worm.Criteria.Core.IFilter
                If Type Is Nothing AndAlso Not String.IsNullOrEmpty(EntityName) Then
                    Return New EntityFilter(EntityName, Field, v, oper)
                Else
                    Return New EntityFilter(Type, Field, v, oper)
                End If
            End Function

            Public Overloads Function [In](ByVal t As Type) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(_stmtGen, t, Nothing), FilterOperation.In))
            End Function

            Public Overloads Function NotIn(ByVal t As Type) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(_stmtGen, t, Nothing), FilterOperation.NotIn))
            End Function

            Public Overloads Function [In](ByVal t As Type, ByVal fieldName As String) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(_stmtGen, t, Nothing, fieldName), FilterOperation.In))
            End Function

            Public Overloads Function NotIn(ByVal t As Type, ByVal fieldName As String) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(_stmtGen, t, Nothing, fieldName), FilterOperation.NotIn))
            End Function

            Public Function Exists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
                Dim j As New JoinFilter(Type, Field, t, joinField, FilterOperation.Equal)
                Return GetLink2(New NonTemplateUnaryFilter(New SubQuery(_stmtGen, t, j), FilterOperation.Exists))
            End Function

            Public Function NotExists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
                Dim j As New JoinFilter(Type, Field, t, joinField, FilterOperation.Equal)
                Return GetLink2(New NonTemplateUnaryFilter(New SubQuery(_stmtGen, t, j), FilterOperation.NotExists))
            End Function

            Public Function Exists(ByVal t As Type) As CriteriaLink
                Return Exists(t, Orm.OrmBaseT.PKName)
            End Function

            Public Function NotExists(ByVal t As Type) As CriteriaLink
                Return NotExists(t, Orm.OrmBaseT.PKName)
            End Function

            Public Function Exists(ByVal t As Type, ByVal f As cc.IGetFilter) As CriteriaLink
                Return GetLink2(New NonTemplateUnaryFilter(New SubQuery(_stmtGen, t, f.Filter), FilterOperation.Exists))
            End Function

            Public Function NotExists(ByVal t As Type, ByVal f As cc.IGetFilter) As CriteriaLink
                Return GetLink2(New NonTemplateUnaryFilter(New SubQuery(_stmtGen, t, f.Filter), FilterOperation.NotExists))
            End Function

            Public Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return GetLink2(New NonTemplateUnaryFilter(New SubQueryCmd(_stmtGen, cmd), FilterOperation.Exists))
            End Function

            Public Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return GetLink2(New NonTemplateUnaryFilter(New SubQueryCmd(_stmtGen, cmd), FilterOperation.NotExists))
            End Function
        End Class

        Public Class CustomCF
            Inherits CriteriaBase

            Private _format As String
            Private _values() As Pair(Of Object, String)
            Private _stmtGen As SQLGenerator

            Protected Friend Sub New(ByVal stmtgen As SQLGenerator, ByVal format As String, ByVal values() As Pair(Of Object, String))
                MyBase.New(Nothing, Nothing)
                _format = format
                _values = values
                _stmtGen = stmtgen
            End Sub

            Protected Friend Sub New(ByVal stmtgen As SQLGenerator, ByVal format As String, ByVal values() As Pair(Of Object, String), _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(con, ct)
                _format = format
                _values = values
                _stmtGen = stmtgen
            End Sub

            'Protected Friend Sub New(ByVal t As Type, ByVal field As String, ByVal format As String)
            '    MyBase.New(t, field)
            '    _format = format
            'End Sub

            'Protected Friend Sub New(ByVal t As Type, ByVal field As String)
            '    MyBase.New(t, field)
            'End Sub

            'Protected Friend Sub New(ByVal t As Type, ByVal field As String, ByVal format As String, _
            '    ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            '    MyBase.New(t, field, con, ct)
            '    _format = format
            'End Sub

            'Protected Friend Sub New(ByVal t As Type, ByVal field As String, _
            '    ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            '    MyBase.New(t, field, con, ct)
            'End Sub

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As FilterOperation) As Worm.Criteria.Core.IFilter
                'If String.IsNullOrEmpty(_format) Then
                Return New CustomFilter(_format, v, oper, _values)
                'Else
                'Return New CustomFilter(Type, Field, _format, v, oper)
                'End If
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function
        End Class

        Public Class CriteriaNonField
            Inherits CriteriaBase
            'Private _con As Condition.ConditionConstructor
            'Private _ct As Worm.Criteria.Conditions.ConditionOperator

            Private _stmtGen As SQLGenerator

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(con, ct)
                _stmtGen = stmtGen
                '_con = con
                '_ct = ct
            End Sub

            'Protected Overrides Function GetLink(ByVal fl As IFilter) As CriteriaLink

            'End Function

            Public Function Exists(ByVal t As Type, ByVal joinFilter As cc.IFilter) As CriteriaLink
                Return Exists(t, joinFilter, Nothing)
            End Function

            Public Function NotExists(ByVal t As Type, ByVal joinFilter As cc.IFilter) As CriteriaLink
                Return NotExists(t, joinFilter, Nothing)
            End Function

            Public Function Exists(ByVal t As Type, ByVal joinFilter As cc.IFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Dim sq As New SubQuery(_stmtGen, t, joinFilter)
                sq.Joins = joins
                Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.Exists)), CriteriaLink)
            End Function

            Public Function NotExists(ByVal t As Type, ByVal joinFilter As cc.IFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Dim sq As New SubQuery(_stmtGen, t, joinFilter)
                sq.Joins = joins
                Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.NotExists)), CriteriaLink)
            End Function

            Public Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return CType(GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(_stmtGen, cmd), FilterOperation.Exists)), CriteriaLink)
            End Function

            Public Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return CType(GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(_stmtGen, cmd), FilterOperation.NotExists)), CriteriaLink)
            End Function

            'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
            '    Return GetLink(New CustomFilter(t, field, value, oper))
            'End Function

            'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal format As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
            '    Return GetLink(New CustomFilter(t, field, format, value, oper))
            'End Function

            Protected Overrides Function CreateFilter(ByVal v As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
                Throw New NotImplementedException
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Conditions.Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(_stmtGen, CType(ConditionCtor, Worm.Database.Criteria.Conditions.Condition.ConditionConstructor))
            End Function
        End Class

        Public Class CriteriaLink
            Inherits Worm.Criteria.CriteriaLink

            Private _stmtGen As SQLGenerator

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal con As Condition.ConditionConstructor)
                MyBase.New(con)
                _stmtGen = stmtGen
            End Sub

            Public Sub New()
            End Sub

            Public Sub New(ByVal stmtGen As SQLGenerator, ByVal t As Type)
                MyBase.New(t)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal t As Type, ByVal con As Condition.ConditionConstructor)
                MyBase.New(t, con)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal entityName As String, ByVal con As Condition.ConditionConstructor)
                MyBase.New(entityName, con)
                _stmtGen = stmtGen
            End Sub

            Public Sub New(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment)
                MyBase.New(table)
                _stmtGen = stmtGen
            End Sub

            Protected Friend Sub New(ByVal stmtGen As SQLGenerator, ByVal table As Orm.Meta.SourceFragment, ByVal con As Condition.ConditionConstructorBase)
                MyBase.New(table, con)
                _stmtGen = stmtGen
            End Sub

            Protected Overrides Function CreateField(ByVal entityName As String, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
                Return New CriteriaField(_stmtGen, entityName, fieldName, CType(con, Condition.ConditionConstructor), oper)
            End Function

            Protected Overrides Function CreateField(ByVal t As System.Type, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
                Return New CriteriaField(_stmtGen, t, fieldName, CType(con, Condition.ConditionConstructor), oper)
            End Function

            Public Function CustomAnd(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(_stmtGen, format, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
            End Function

            Public Function CustomOr(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(_stmtGen, format, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
            End Function

            'Public Function CustomAnd(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
            'End Function

            'Public Function CustomOr(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
            'End Function

            Public Function AndExists(ByVal t As Type, ByVal joinFilter As cc.IGetFilter) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).Exists(t, joinFilter.Filter)
            End Function

            Public Function AndNotExists(ByVal t As Type, ByVal joinFilter As cc.IGetFilter) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).NotExists(t, joinFilter.Filter)
            End Function

            Public Function AndExists(ByVal t As Type, ByVal joinFilter As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).Exists(t, joinFilter.Filter, joins)
            End Function

            Public Function AndNotExists(ByVal t As Type, ByVal joinFilter As cc.IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).NotExists(t, joinFilter.Filter, joins)
            End Function

            Public Function AndExistsM2M(ByVal t As Type, ByVal t2 As Type, ByVal filter As cc.IGetFilter, ByVal mpe As ObjectMappingEngine) As CriteriaLink
                Dim t12t2 As Orm.Meta.M2MRelation = mpe.GetM2MRelation(t, t2, True)
                Dim t22t1 As Orm.Meta.M2MRelation = mpe.GetM2MRelation(t2, t, True)
                Dim t2_pk As String = mpe.GetPrimaryKeys(t2)(0).PropertyAlias
                Dim t1_pk As String = mpe.GetPrimaryKeys(t)(0).PropertyAlias

                Return AndExists(t, JoinCondition.Create(t12t2.Table, t12t2.Column).Eq(t2, t2_pk), _
                                 JCtor.Join(t22t1.Table).On(t22t1.Table, t22t1.Column).Eq(t, t1_pk))
            End Function

            Public Function OrExists(ByVal t As Type, ByVal joinFilter As cc.IFilter) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).Exists(t, joinFilter)
            End Function

            Public Function OrNotExists(ByVal t As Type, ByVal joinFilter As cc.IFilter) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).NotExists(t, joinFilter)
            End Function

            Public Function AndExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).Exists(cmd)
            End Function

            Public Function AndNotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).NotExists(cmd)
            End Function

            Public Function OrExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).Exists(cmd)
            End Function

            Public Function OrNotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
                Return New CriteriaNonField(_stmtGen, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).NotExists(cmd)
            End Function

            Protected Overrides Function _Clone() As Object
                If Table IsNot Nothing Then
                    Return New CriteriaLink(_stmtGen, Table, CType(ConditionCtor.Clone, Condition.ConditionConstructor))
                Else
                    Return New CriteriaLink(_stmtGen, Type, CType(ConditionCtor.Clone, Condition.ConditionConstructor))
                End If
            End Function

            Protected Overrides Function CreateColumn(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaColumn
                Return New CriteriaColumn(_stmtGen, table, columnName, CType(con, Condition.ConditionConstructor), oper)
            End Function
        End Class
    End Namespace
End Namespace