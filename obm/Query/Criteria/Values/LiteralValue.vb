Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class LiteralValue
        Implements IFilterValue

        Private _pname As String

        Public Sub New(ByVal literal As String)
            _pname = literal
        End Sub

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            Return _pname
        End Function

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _pname
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return "litval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine,
                           ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub
        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As LiteralValue
            Return New LiteralValue(_pname)
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, LiteralValue))
        End Function

        Public Function CopyTo(target As LiteralValue) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._pname = _pname

            Return True
        End Function
    End Class
End Namespace