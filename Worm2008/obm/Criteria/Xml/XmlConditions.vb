Imports Worm.Criteria.Core

Namespace Xml
    Namespace Criteria.Conditions

        Public Class Condition
            Inherits Worm.Criteria.Conditions.Condition

            Public Class ConditionConstructor
                Inherits Worm.Criteria.Conditions.Condition.ConditionConstructorBase

                Protected Overrides Function CreateCondition(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.Conditions.Condition
                    Return New Condition(left, right, [operator])
                End Function

                Protected Overrides Function CreateEntityCondition(ByVal left As Worm.Criteria.Core.IEntityFilter, ByVal right As Worm.Criteria.Core.IEntityFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.Conditions.Condition
                    Return New EntityCondition(CType(left, IEntityFilter), CType(right, IEntityFilter), [operator])
                End Function
            End Class

            Private Class ConditionTemplate
                Inherits Worm.Criteria.Conditions.Condition.ConditionTemplateBase

                Public Sub New(ByVal con As Condition)
                    MyBase.New(con)
                End Sub

                Public Overrides ReadOnly Property OperToStmt() As String
                    Get
                        Return Worm.Xml.Criteria.Core.XmlEntityTemplate.Oper2String(Operation)
                    End Get
                End Property
            End Class

            Public Sub New(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(left, right, [operator])
            End Sub

            Protected Overrides Function CreateMe(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.Conditions.Condition
                Return New Condition(left, right, [operator])
            End Function

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As Orm.Meta.ICreateParam) As String
                If _right Is Nothing Then
                    Return _left.MakeSQLStmt(schema, pname)
                End If
                Return "(" & _left.MakeSQLStmt(schema, pname) & Condition2String() & _right.MakeSQLStmt(schema, pname) & ")"
            End Function

            Public Overrides ReadOnly Property Template() As Worm.Criteria.Core.ITemplate
                Get
                    Return New ConditionTemplate(Me)
                End Get
            End Property

            Protected Overrides Function Condition2String() As String
                If _oper = Worm.Criteria.Conditions.ConditionOperator.And Then
                    Return " && "
                Else
                    Return " || "
                End If
            End Function
        End Class

        Friend Class EntityCondition
            Inherits Worm.Criteria.Conditions.EntityCondition

            Protected Class ConditionTemplate
                Inherits Worm.Criteria.Conditions.EntityCondition.EntityConditionTemplateBase

                Public Sub New(ByVal con As EntityCondition)
                    MyBase.New(con)
                End Sub

                Public Overrides ReadOnly Property OperToStmt() As String
                    Get
                        Return Worm.Xml.Criteria.Core.XmlEntityTemplate.Oper2String(Operation)
                    End Get
                End Property
            End Class

            Public Sub New(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(CType(left, Worm.Criteria.Core.IEntityFilter), CType(right, Worm.Criteria.Core.IEntityFilter), [operator])
            End Sub

            Protected Overrides Function CreateMe(ByVal left As Worm.Criteria.Core.IFilter, ByVal right As Worm.Criteria.Core.IFilter, ByVal [operator] As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.Conditions.Condition
                Return New Condition(CType(left, IFilter), CType(right, IFilter), [operator])
            End Function

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As Orm.Meta.ICreateParam) As String
                If _right Is Nothing Then
                    Return CType(_left, IEntityFilter).MakeSQLStmt(schema, pname)
                End If

                Return "(" & CType(_left, IEntityFilter).MakeSQLStmt(schema, pname) & Condition2String() & CType(_right, IEntityFilter).MakeSQLStmt(schema, pname) & ")"
            End Function

            Public Overrides ReadOnly Property Template() As Worm.Criteria.Core.ITemplate
                Get
                    Return New ConditionTemplate(Me)
                End Get
            End Property
        End Class
    End Namespace
End Namespace