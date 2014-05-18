Imports System.Data.Common

Namespace Database
    Public Interface IDataReaderAbstraction
        Function GetValue(idx As Integer) As Object
        Function IsDBNull(idx As Integer) As Boolean
        Function GetName(idx As Integer) As String
        Function GetOrdinal(name As String) As Integer
        ReadOnly Property FieldCount As Integer
        ReadOnly Property RowNum As Integer
        ReadOnly Property LockObject As Object
    End Interface

    Public Class DataReaderAbstraction
        Implements IDataReaderAbstraction

        Private _dr As DbDataReader
        Private _values() As Object
        Private _rn As Integer
        Private _lockObject As Object
        Public Sub New(dr As DbDataReader, rowNum As Integer, lockObject As Object)
            _values = New Object(dr.FieldCount - 1) {}
            dr.GetValues(_values)
            _dr = dr
            _rn = rowNum
            _lockObject = lockObject
        End Sub

        Public Function GetName(idx As Integer) As String Implements IDataReaderAbstraction.GetName
            Return _dr.GetName(idx)
        End Function

        Public Function GetOrdinal(name As String) As Integer Implements IDataReaderAbstraction.GetOrdinal
            Return _dr.GetOrdinal(name)
        End Function

        Public Function GetValue(idx As Integer) As Object Implements IDataReaderAbstraction.GetValue
            Return _values(idx)
        End Function

        Public Function IsDBNull(idx As Integer) As Boolean Implements IDataReaderAbstraction.IsDBNull
            Return _values(idx) Is DBNull.Value
        End Function

        Public ReadOnly Property FieldCount As Integer Implements IDataReaderAbstraction.FieldCount
            Get
                Return _values.Length
            End Get
        End Property

        Public ReadOnly Property RowNum As Integer Implements IDataReaderAbstraction.RowNum
            Get
                Return _rn
            End Get
        End Property

        Public ReadOnly Property LockObject As Object Implements IDataReaderAbstraction.LockObject
            Get
                Return _lockObject
            End Get
        End Property
    End Class
End Namespace