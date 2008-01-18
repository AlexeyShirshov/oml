Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Namespace Database

    'Public Class SQL2005Schema
    '    Inherits DbSchema

    '    Public Sub New(ByVal version As String)
    '        MyBase.New(version)
    '    End Sub

    '    Protected Overrides Function FormInsert(ByVal inserted_tables As System.Collections.Generic.Dictionary(Of String, System.Collections.Generic.IList(Of OrmFilter)), ByVal ins_cmd As System.Text.StringBuilder, ByVal type As System.Type, ByVal sel_columns As System.Collections.Generic.List(Of ColumnAttribute), ByVal unions() As String, ByVal params As ICreateParam) As System.Collections.Generic.ICollection(Of System.Data.Common.DbParameter)
    '        If params Is Nothing Then
    '            params = New ParamMgr(Me, "p")
    '        End If

    '        If inserted_tables.Count > 0 Then
    '            Dim l As New List(Of Structures.Pair(Of ColumnAttribute, String))
    '            If sel_columns IsNot Nothing AndAlso sel_columns.Count > 0 Then
    '                sel_columns.Sort()

    '                For Each c As ColumnAttribute In sel_columns
    '                    l.Add(DeclareVariable(type, c, ins_cmd))
    '                Next

    '                If inserted_tables.Count > 1 Then
    '                    ins_cmd.Append(DeclareVariable("@err", "int"))
    '                    ins_cmd.Append(EndLine)
    '                End If
    '            End If

    '            Dim b As Boolean = False
    '            Dim os As IOrmObjectSchema = GetObjectSchema(type)
    '            Dim pk_table As String = os.GetTables(0)
    '            For Each item As Generic.KeyValuePair(Of String, IList(Of OrmFilter)) In inserted_tables
    '                If b Then
    '                    ins_cmd.Append(EndLine)
    '                    If SupportIf() Then
    '                        ins_cmd.Append("if @err = 0 ")
    '                    End If
    '                Else
    '                    b = True
    '                End If

    '                If item.Value Is Nothing OrElse item.Value.Count = 0 Then
    '                    ins_cmd.Append("insert into ").Append(item.Key).Append(" ")
    '                    If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
    '                        AddOutput(type, sel_columns, ins_cmd)
    '                    End If
    '                    ins_cmd.Append(" ").Append(DefaultValues)
    '                Else
    '                    ins_cmd.Append("insert into ").Append(item.Key).Append(" (")
    '                    Dim values_sb As New StringBuilder
    '                    values_sb.Append(") ")
    '                    If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
    '                        AddOutput(type, sel_columns, values_sb)
    '                    End If
    '                    values_sb.Append(" values(")
    '                    For Each f As OrmFilter In item.Value
    '                        Dim p As Structures.Pair(Of String) = f.MakeSignleStmt(Me, params)
    '                        ins_cmd.Append(p.First).Append(",")
    '                        values_sb.Append(p.Second).Append(",")
    '                    Next

    '                    ins_cmd.Length -= 1
    '                    values_sb.Length -= 1
    '                    ins_cmd.Append(values_sb.ToString).Append(")")
    '                End If

    '                If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
    '                    ins_cmd.Append(EndLine)
    '                    ins_cmd.Append("select @rcount = ").Append(RowCount)
    '                    If inserted_tables.Count > 1 Then
    '                        ins_cmd.Append(", @err = ").Append(LastError)
    '                    End If
    '                End If
    '            Next

    '            If sel_columns IsNot Nothing AndAlso sel_columns.Count > 0 Then
    '                ins_cmd.Append(EndLine)
    '                If SupportIf() Then
    '                    ins_cmd.Append("if @rcount > 0 ")
    '                End If
    '                ins_cmd.Append("select ")

    '            End If
    '        End If

    '        Return params.Params
    '    End Function

    '    Public Overrides ReadOnly Property Name() As String
    '        Get
    '            Return "SQL Server 2005"
    '        End Get
    '    End Property

    '    Protected sub AddOutput(ByVal type As Type, ByVal sel_column As ICollection(Of ColumnAttribute), ByVal sb As StringBuilder) As String
    '        For Each c As ColumnAttribute In sel_column
    '            sb.Append("inserted.")
    '            sb.Append(GetColumnNameByFieldName(type, c.FieldName))
    '            sb.Append(",")
    '        Next
    '        sb.Length -= 1
    '    End Sub

    '    Protected Overloads Function DeclareVariable(ByVal type As Type, ByVal c As ColumnAttribute, ByVal sb As StringBuilder) As Structures.Pair(Of ColumnAttribute, String)
    '        Dim p As Data.Common.DbParameter = CreateDBParameter("@" & c.FieldName, Convert.ChangeType(Nothing, GetFieldTypeByName(type, c.FieldName)))
    '        p.DbType.
    '    End Function

    'End Class

End Namespace