<%@ Assembly Name="Worm.Orm" %>
<%@ Assembly Name="TestProject1" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">

<script runat="server">

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim sb As New StringBuilder
        Dim c As New OrmCache
        Dim conn As String = "Data Source=vs2\sqlmain;Integrated Security=true;Initial Catalog=WormTest;"
        Using mgr As New OrmReadOnlyDBManager(c, New DbSchema("1"), conn)
            Dim o As New OrmDictionary(Of TestProject1.Table1)(mgr.Cache)
            
            For Each t As TestProject1.Table1 In mgr.FindTop(Of TestProject1.Table1)(100, Nothing, Nothing, SortType.Asc, True)
                o.Add(t.Identifier, t)
            Next
            
            For Each k As DictionaryEntry In Cache
                sb.Append(k.Key).Append(" = ").AppendLine(k.Value.ToString)
                sb.Append("<br/>")
            Next
            
            pre.InnerHtml = sb.ToString
        End Using
        
        
    End Sub
</script>

<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test objects</title>
</head>
<body><pre runat="server" id="pre" /></body>
</html>
