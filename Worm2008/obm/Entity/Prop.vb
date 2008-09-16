Imports Worm.Orm.Meta
Imports System.Collections.Generic

Namespace Orm
    Public Class FCtor
        Public Shared Function Field(ByVal t As Type, ByVal typeField As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(t, typeField))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(table, tableColumn))
            Return f
        End Function

        Public Shared Function Column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New OrmProperty(table, tableColumn, [alias]))
            Return f
        End Function

        Private _l As List(Of OrmProperty)

        Public Function Add(ByVal t As Type, ByVal typeField As String) As FCtor
            GetAllProperties.Add(New OrmProperty(t, typeField))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            GetAllProperties.Add(New OrmProperty(table, tableColumn))
            Return Me
        End Function

        Public Function GetAllProperties() As List(Of OrmProperty)
            If _l Is Nothing Then
                _l = New List(Of OrmProperty)
            End If
            Return _l
        End Function

        Public Shared Widening Operator CType(ByVal f As FCtor) As OrmProperty()
            Return f.GetAllProperties.ToArray
        End Operator
    End Class

    Public Class OrmProperty

        Private _field As String
        Private _type As Type
        Private _table As SourceFragment
        Private _column As String
        Private _custom As String
        Private _values() As Pair(Of Object, String)

        Public Sub New(ByVal t As Type, ByVal field As String)
            _field = field
            _type = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            _column = column
            _table = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal field As String)
            _column = column
            _table = t
            _field = field
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As Pair(Of Object, String), ByVal [alias] As String)
            _custom = computed
            _values = values
            _column = [alias]
        End Sub

        Public Property Column() As String
            Get
                Return _column
            End Get
            Protected Friend Set(ByVal value As String)
                _column = value
            End Set
        End Property

        Public Property Field() As String
            Get
                Return _field
            End Get
            Protected Friend Set(ByVal value As String)
                _field = value
            End Set
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _type
            End Get
        End Property

        Public ReadOnly Property Table() As SourceFragment
            Get
                Return _table
            End Get
        End Property

        Public ReadOnly Property Computed() As String
            Get
                Return _custom
            End Get
        End Property


        Public ReadOnly Property Values() As Pair(Of Object, String)()
            Get
                Return _values
            End Get
        End Property

        Public Overrides Function ToString() As String
            If _type IsNot Nothing Then
                Return _type.ToString & "$" & _field
            Else
                If _table IsNot Nothing Then
                    Return _table.RawName & "$" & _column
                Else
                    Return _custom
                End If
            End If
        End Function

    End Class

End Namespace