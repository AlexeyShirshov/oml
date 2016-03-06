Imports System.Collections.Generic
Imports System.Data

Namespace Database
    Public Interface IBulkLoadOptions
        Property CharacterSet As String
        ReadOnly Property Columns As IEnumerable(Of DataColumn)
        Property EscapeCharacter As Char
        Property FieldQuotationCharacter As Char
        Property FieldQuotationOptional As Boolean
        Property Delimiters As String()
        Property LinePrefix As String
        Property LineTerminator As String
        Property NumberOfLinesToSkip As Integer
        Property TableName As String
        Property BatchSize As Integer?
        Property Filename As String
        Property CommentTokens As String()

        Property FieldWidths As Integer()

        Property TrimWhiteSpace As Boolean
        Property ColumnMappings As IEnumerable(Of ColumnMapping)
        Property AutoColumns As Boolean
        Property AutoMapColumns As Boolean
        Property Timeout As Integer?
    End Interface

    Public Class BulkLoadOptions
        Implements IBulkLoadOptions

        Private _cols As List(Of DataColumn)
        Public Sub New()
            _cols = New List(Of DataColumn)
            FieldQuotationCharacter = "'"c
            EscapeCharacter = "\"c
            FieldQuotationOptional = True
        End Sub
        Public Property AutoColumns As Boolean Implements IBulkLoadOptions.AutoColumns

        Public Property AutoMapColumns As Boolean Implements IBulkLoadOptions.AutoMapColumns

        Public Property BatchSize As Integer? Implements IBulkLoadOptions.BatchSize

        Public Property CharacterSet As String Implements IBulkLoadOptions.CharacterSet

        Public Property ColumnMappings As IEnumerable(Of ColumnMapping) Implements IBulkLoadOptions.ColumnMappings
        Public ReadOnly Property Columns As List(Of DataColumn)
            Get
                Return _cols
            End Get
        End Property
        Private ReadOnly Property _Columns As IEnumerable(Of DataColumn) Implements IBulkLoadOptions.Columns
            Get
                Return _cols
            End Get
        End Property

        Public Property CommentTokens As String() Implements IBulkLoadOptions.CommentTokens

        Public Property Delimiters As String() Implements IBulkLoadOptions.Delimiters

        Public Property EscapeCharacter As Char Implements IBulkLoadOptions.EscapeCharacter

        Public Property FieldQuotationCharacter As Char Implements IBulkLoadOptions.FieldQuotationCharacter

        Public Property FieldQuotationOptional As Boolean Implements IBulkLoadOptions.FieldQuotationOptional

        Public Property FieldWidths As Integer() Implements IBulkLoadOptions.FieldWidths

        Public Property Filename As String Implements IBulkLoadOptions.Filename

        Public Property LinePrefix As String Implements IBulkLoadOptions.LinePrefix

        Public Property LineTerminator As String Implements IBulkLoadOptions.LineTerminator

        Public Property NumberOfLinesToSkip As Integer Implements IBulkLoadOptions.NumberOfLinesToSkip

        Public Property TableName As String Implements IBulkLoadOptions.TableName

        Public Property TrimWhiteSpace As Boolean Implements IBulkLoadOptions.TrimWhiteSpace

        Public Property Timeout As Integer? Implements IBulkLoadOptions.Timeout
    End Class
End Namespace