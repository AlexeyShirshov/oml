Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class ComputedValue
        Implements IFilterValue

        Private _alias As String

        Public Sub New(ByVal [alias] As String)
            _alias = [alias]
        End Sub

        Public ReadOnly Property [Alias]() As String
            Get
                Return _alias
            End Get
        End Property

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _alias
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                 ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable,
                                 ByVal prepare As PrepareValueDelegate, ByVal contextInfo As IDictionary, ByVal inSelect As Boolean,
                                 ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            Return [Alias]
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return "compval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As ComputedValue
            Return New ComputedValue([Alias])
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, ComputedValue))
        End Function

        Public Function CopyTo(computedValue As ComputedValue) As Boolean
            If computedValue Is Nothing Then
                Return False
            End If
            computedValue._alias = _alias

            Return True
        End Function

    End Class

End Namespace