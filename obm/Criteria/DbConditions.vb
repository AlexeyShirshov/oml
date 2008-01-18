Imports System.Collections.Generic
Imports cc = Worm.Criteria.Conditions
Imports Worm.Database.Criteria.Core
Imports Worm.Orm.Meta

Namespace Database
    Namespace Criteria.Conditions
        Public Class Condition
            Inherits Worm.Criteria.Conditions.Condition
            Implements Worm.Database.Criteria.Core.ITemplateFilter

            Public Class ConditionConstructor
                Inherits Worm.Criteria.Conditions.Condition.ConditionConstructorBase

                Protected Overrides Function CreateCondition(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As cc.ConditionOperator) As cc.Condition
                    Return New Condition(CType(left, IFilter), CType(right, IFilter), [operator])
                End Function

                Protected Overrides Function CreateEntityCondition(ByVal left As Worm.Criteria.Core.IEntityFilter, ByVal right As Worm.Criteria.Core.IEntityFilter, ByVal [operator] As cc.ConditionOperator) As cc.Condition
                    Return New EntityCondition(CType(left, IEntityFilter), CType(right, IEntityFilter), [operator])
                End Function

                Public Overloads ReadOnly Property Condition() As Worm.Database.Criteria.Core.IFilter
                    Get
                        Return CType(MyBase.Condition, Core.IFilter)
                    End Get
                End Property
            End Class

            Private Class ConditionTemplate
                Inherits Worm.Criteria.Conditions.Condition.ConditionTemplateBase

                Public Sub New(ByVal con As Condition)
                    MyBase.New(con)
                End Sub

                Public Overrides ReadOnly Property OperToStmt() As String
                    Get
                        Return Worm.Database.Criteria.Core.TemplateBase.Oper2String(Operation)
                    End Get
                End Property
            End Class

            Public Sub New(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As cc.ConditionOperator)
                MyBase.New(left, right, [operator])
            End Sub

            Protected Overrides Function CreateMe(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As cc.ConditionOperator) As cc.Condition
                Return New Condition(CType(left, IFilter), CType(right, IFilter), [operator])
            End Function

            Public Overrides ReadOnly Property Template() As Worm.Criteria.Core.ITemplate
                Get
                    Return New ConditionTemplate(Me)
                End Get
            End Property

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
                Throw New NotImplementedException("")
            End Function

            Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements Core.IFilter.MakeSQLStmt
                If _right Is Nothing Then
                    Return CType(_left, IFilter).MakeSQLStmt(schema, almgr, pname)
                End If

                Return "(" & CType(_left, IFilter).MakeSQLStmt(schema, almgr, pname) & Condition2String() & CType(_right, IFilter).MakeSQLStmt(schema, almgr, pname) & ")"
            End Function
        End Class

        Friend Class EntityCondition
            Inherits Worm.Criteria.Conditions.EntityCondition
            Implements Worm.Database.Criteria.Core.IEntityFilter

            Protected Class ConditionTemplate
                Inherits Worm.Criteria.Conditions.EntityCondition.ConditionTemplateBase

                Public Sub New(ByVal con As EntityCondition)
                    MyBase.New(con)
                End Sub

                Public Overrides ReadOnly Property OperToStmt() As String
                    Get
                        Return Worm.Database.Criteria.Core.TemplateBase.Oper2String(Operation)
                    End Get
                End Property
            End Class

            Public Sub New(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As cc.ConditionOperator)
                MyBase.New(CType(left, Worm.Criteria.Core.IEntityFilter), CType(right, Worm.Criteria.Core.IEntityFilter), [operator])
            End Sub

            Protected Overrides Function CreateMe(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As cc.ConditionOperator) As cc.Condition
                Return New Condition(CType(left, IFilter), CType(right, IFilter), [operator])
            End Function

            Protected Overrides Function CreateMeE(ByVal left As Worm.Criteria.Core.IEntityFilter, ByVal right As Worm.Criteria.Core.IEntityFilter, ByVal [operator] As cc.ConditionOperator) As cc.Condition
                Return New EntityCondition(CType(left, IEntityFilter), CType(right, IEntityFilter), [operator])
            End Function

            Public Overrides ReadOnly Property Template() As Worm.Criteria.Core.ITemplate
                Get
                    Return New ConditionTemplate(Me)
                End Get
            End Property

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
                Throw New NotImplementedException("")
            End Function

            Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements Worm.Database.Criteria.Core.ITemplateFilter.MakeSQLStmt
                If _right Is Nothing Then
                    Return CType(_left, IEntityFilter).MakeSQLStmt(schema, almgr, pname)
                End If

                Return "(" & CType(_left, IEntityFilter).MakeSQLStmt(schema, almgr, pname) & Condition2String() & CType(_right, IEntityFilter).MakeSQLStmt(schema, almgr, pname) & ")"
            End Function
        End Class
    End Namespace
End Namespace