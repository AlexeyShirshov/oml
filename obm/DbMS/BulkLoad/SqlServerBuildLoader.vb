Imports Microsoft.VisualBasic.FileIO
Imports System.Data
Imports System.Linq

Namespace Database
    Public Class SqlServerBuildLoader
        Implements IBulkLoader

        Public Function Load(mgr As OrmReadOnlyDBManager, options As IBulkLoadOptions) As IBulkLoadResults Implements IBulkLoader.Load
            If options Is Nothing Then
                Throw New ArgumentNullException("options")
            End If

            If String.IsNullOrEmpty(options.Filename) Then
                Throw New ArgumentException("Filename is empty")
            End If

            If Not IO.File.Exists(options.Filename) Then
                Throw New ArgumentException("Filename '{0}' not found", options.Filename)
            End If

            Dim enc = Encoding.Default
            If Not String.IsNullOrEmpty(options.CharacterSet) Then
                enc = Encoding.GetEncoding(options.CharacterSet)
            End If

            Using parser As New TextFieldParser(options.Filename, enc)
                parser.CommentTokens = options.CommentTokens
                parser.Delimiters = options.Delimiters
                parser.FieldWidths = options.FieldWidths
                parser.HasFieldsEnclosedInQuotes = options.FieldQuotationOptional
                parser.TextFieldType = If(options.FieldWidths IsNot Nothing, FieldType.FixedWidth, FieldType.Delimited)
                parser.TrimWhiteSpace = options.TrimWhiteSpace

                Dim dt As New DataTable(options.TableName)
                If options.Columns IsNot Nothing Then
                    dt.Columns.AddRange(options.Columns.ToArray)
                End If

                Dim added = 0, row = 0
                Dim bz = If(options.BatchSize, 100000)

                Using conn = mgr.GetConnection(),
                    bulkcpy As New System.Data.SqlClient.SqlBulkCopy(CType(conn.Connection, SqlClient.SqlConnection))

                    If options.ColumnMappings IsNot Nothing Then
                        For Each cm In options.ColumnMappings
                            bulkcpy.ColumnMappings.Add(New SqlClient.SqlBulkCopyColumnMapping With {
                                                       .DestinationColumn = cm.DestinationColumn,
                                                       .DestinationOrdinal = cm.DestinationOrdinal,
                                                       .SourceColumn = cm.SourceColumn,
                                                       .SourceOrdinal = cm.SourceOrdinal
                                                       })
                        Next
                    End If

                    bulkcpy.DestinationTableName = options.TableName
                    Dim mapped = False
                    Do While Not parser.EndOfData

                        If options.NumberOfLinesToSkip > row Then
                            row += 1
                            Continue Do
                        End If

                        If added = 0 Then
                            If Not mapped AndAlso options.AutoColumns Then
                                Dim columns = parser.ReadFields
                                For Each clm In columns
                                    Dim idx = dt.Columns.IndexOf(clm)
                                    If idx < 0 Then
                                        dt.Columns.Add(New DataColumn(clm))
                                    Else
                                        Dim exist = dt.Columns(idx)
                                        dt.Columns.RemoveAt(idx)
                                        dt.Columns.Add(exist)
                                    End If
                                Next

                                mapped = True
                                row += 1
                                Continue Do
                            End If

                            If (options.ColumnMappings Is Nothing OrElse Not options.ColumnMappings.Any) AndAlso options.AutoMapColumns Then
                                For Each clm As DataColumn In dt.Columns
                                    bulkcpy.ColumnMappings.Add(New SqlClient.SqlBulkCopyColumnMapping With {
                                                   .DestinationColumn = clm.ColumnName,
                                                   .SourceColumn = clm.ColumnName
                                                   })
                                Next
                            End If

                        End If

                        Dim line = parser.ReadLine
                        If String.IsNullOrEmpty(line) Then
                            row += 1
                            Continue Do
                        End If

                        If options.CommentTokens IsNot Nothing Then
                            For Each ct In options.CommentTokens
                                If line.StartsWith(ct) Then
                                    row += 1
                                    Continue Do
                                End If
                            Next
                        End If


                        Dim ss = line.Split(options.Delimiters, StringSplitOptions.None)
                        Dim fields(ss.Length - 1) As Object
                        For i = 0 To ss.Length - 1
                            Dim v = ss(i)
                            If String.IsNullOrEmpty(v) Then
                                fields(i) = DBNull.Value
                            ElseIf options.FieldQuotationCharacter <> Chr(0) Then
                                If v.StartsWith(options.FieldQuotationCharacter) AndAlso v.EndsWith(options.FieldQuotationCharacter) Then
                                    fields(i) = v.Trim(options.FieldQuotationCharacter)
                                ElseIf Not options.FieldQuotationOptional Then
                                    Throw New BulkException(String.Format("Line {0} column {1} has no quotation", row, i))
                                Else
                                    fields(i) = v
                                End If
                            Else
                                fields(i) = v
                            End If
                        Next
                        dt.Rows.Add(fields)
                        added += 1
                        row += 1

                        If (added Mod bz) = 0 Then
                            Dim cnt = dt.Rows.Count
                            bulkcpy.WriteToServer(dt)
                            dt.Rows.Clear()

                            Dim args As New RowsCopiedEventArgs With {.CopiedInLastBatch = cnt, .TotalCopied = added, .Row = row}
                            RaiseEvent NotifyCopied(Me, args)

                            If args.Abort Then
                                GoTo [exit]
                            End If
                        End If
                    Loop

                    Dim cntx = dt.Rows.Count
                    If cntx > 0 Then
                        bulkcpy.WriteToServer(dt)
                        dt.Rows.Clear()

                        Dim argsx As New RowsCopiedEventArgs With {.CopiedInLastBatch = cntx, .TotalCopied = added}
                        RaiseEvent NotifyCopied(Me, argsx)
                    End If

[exit]:
                End Using
                Return New BulkResults(added)
            End Using
        End Function

        Public Event NotifyCopied(sender As IBulkLoader, args As RowsCopiedEventArgs) Implements IBulkLoader.NotifyCopied
    End Class
End Namespace