Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Database
    Public Class ParamMgr
        Implements ICreateParam

        Private _params As List(Of System.Data.Common.DbParameter)
        Private _stmt As DbGenerator
        Private _prefix As String
        Private _named_params As Boolean

        Public Sub New(ByVal stmtGen As DbGenerator, ByVal prefix As String)
            _stmt = stmtGen
            _params = New List(Of System.Data.Common.DbParameter)
            _prefix = prefix
            _named_params = stmtGen.ParamName("p", 1) <> stmtGen.ParamName("p", 2)
        End Sub

        ', ByVal mpe As ObjectMappingEngine
        Public Function AddParam(ByVal pname As String, ByVal value As Object) As String Implements ICreateParam.AddParam
            Dim p As System.Data.Common.DbParameter = GetParameter(pname)
            If p Is Nothing Then
                Return CreateParam(value, pname)
            Else
                If p.Value Is Nothing OrElse p.Value.Equals(value) Then

                Else
                    p.Value = value
                End If

                Return pname
            End If
        End Function

        Public Function CreateParam(ByVal value As Object, Optional pname As String = Nothing) As String Implements ICreateParam.CreateParam
            If _stmt Is Nothing Then
                Throw New InvalidOperationException("Object must be created")
            End If

            If String.IsNullOrEmpty(pname) OrElse Not NamedParams Then
                pname = _stmt.ParamName(_prefix, _params.Count + 1)
            End If

            _params.Add(_stmt.CreateDBParameter(pname, value))
            Return pname
        End Function

        Public ReadOnly Property Params() As IList(Of System.Data.Common.DbParameter) Implements ICreateParam.Params
            Get
                Return _params
            End Get
        End Property

        Public Function GetParameter(ByVal name As String) As System.Data.Common.DbParameter Implements ICreateParam.GetParameter
            If Not String.IsNullOrEmpty(name) Then
                For Each p As System.Data.Common.DbParameter In _params
                    If p.ParameterName = name Then
                        Return p
                    End If
                Next
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Prefix() As String
            Get
                Return _prefix
            End Get
            'Set(ByVal value As String)
            '    _prefix = value
            'End Set
        End Property

        'Public ReadOnly Property IsEmpty() As Boolean Implements ICreateParam.IsEmpty
        '    Get
        '        Return _params Is Nothing
        '    End Get
        'End Property

        Public Property NamedParams() As Boolean Implements ICreateParam.NamedParams
            Get
                Return _named_params
            End Get
            Set(value As Boolean)
                _named_params = value
            End Set
        End Property

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection)
            For Each p As System.Data.Common.DbParameter In _params
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection, ByVal start As Integer, ByVal count As Integer)
            For i As Integer = start To Math.Min(_params.Count, start + count) - 1
                Dim p As System.Data.Common.DbParameter = _params(i)
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub Clear(ByVal preserve As Integer)
            If preserve > 0 Then
                _params.RemoveRange(preserve - 1, _params.Count - preserve)
            End If
        End Sub
    End Class

End Namespace