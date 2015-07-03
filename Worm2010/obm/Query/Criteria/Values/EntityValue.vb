Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class EntityValue
        Inherits ScalarValue

        Private _t As Type

        Protected Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal o As ISinglePKEntity)
            MyBase.New()
            If o IsNot Nothing Then
                _t = o.GetType
                SetValue(o.Identifier)
            Else
                _t = GetType(ISinglePKEntity)
            End If
        End Sub

        Public Sub New(ByVal o As ISinglePKEntity, ByVal caseSensitive As Boolean)
            MyClass.New(o)
            Me.CaseSensitive = caseSensitive
        End Sub

        Public Function GetOrmValue(ByVal mgr As OrmManager) As ISinglePKEntity
            Return mgr.GetKeyEntityFromCacheOrCreate(Value, _t)
        End Function

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        Protected Overrides Function GetValue(ByVal v As Object, ByVal template As OrmFilterTemplate, ByRef r As IEvaluableValue.EvalResult) As Object
            r = IEvaluableValue.EvalResult.Unknown
            Dim orm As ISinglePKEntity = TryCast(v, ISinglePKEntity)
            If orm IsNot Nothing Then
                Dim ov As EntityValue = TryCast(Me, EntityValue)
                If ov Is Nothing Then
                    Throw New InvalidOperationException(String.Format("Field {0} is Entity but param is not", template.PropertyAlias))
                End If
                Dim tt As Type = v.GetType
                If Not tt.IsAssignableFrom(ov.OrmType) Then
                    If Value Is Nothing Then
                        r = IEvaluableValue.EvalResult.NotFound
                        Return Nothing
                    Else
                        Throw New InvalidOperationException(String.Format("Field {0} is type of {1} but param is type of {2}", template.PropertyAlias, tt.ToString, ov.OrmType.ToString))
                    End If
                End If
                Return ov.GetOrmValue(OrmManager.CurrentManager)
            End If
            Return Value
        End Function

        'Public Overrides ReadOnly Property Value() As Object
        '    Get
        '        Return GetOrmValue(OrmManager.CurrentManager)
        '    End Get
        'End Property

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As EntityValue
            Dim n As New EntityValue()
            _CopyTo(n)
            Return n
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, EntityValue))
        End Function

        Public Overloads Function CopyTo(target As EntityValue) As Boolean
            If MyBase._CopyTo(target) Then
                If target Is Nothing Then
                    Return False
                End If

                target._t = _t

                Return True

            End If

            Return False
        End Function
    End Class
End Namespace