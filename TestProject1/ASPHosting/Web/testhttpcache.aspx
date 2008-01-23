<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test HttpCache Page</title>
    <script runat="server" language="VB">
        
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim sb As New StringBuilder
            
            'For Each s As String In Request.QueryString.AllKeys
            '    sb.Append(s).AppendLine()
            '    sb.Append("<br/>")
            'Next
            
            If Request.QueryString.ToString = "add" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)

                sb.Append("first = " & c("first")).AppendLine()
                sb.Append("<br/>")
                For Each k As String In c.Keys
                    sb.Append(k).AppendLine()
                    sb.Append("<br/>")
                Next
            
                For Each k As DictionaryEntry In Cache
                    sb.Append(k.Key).AppendLine()
                    sb.Append("<br/>")
                Next
            ElseIf Request.QueryString.ToString = "remove" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)

                sb.Append("cache has key first = " & c.ContainsKey("first")).AppendLine()
                sb.Append("<br/>")
                
                sb.Append("remove all keys").AppendLine()
                sb.Append("<br/>")
                
                For Each k As DictionaryEntry In Cache
                    Cache.Remove(k.Key)
                Next
                
                sb.Append("cache has key first = " & c.ContainsKey("first"))
            ElseIf Request.QueryString.ToString = "remove2" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)

                sb.Append("cache has key first = " & c.ContainsKey("first")).AppendLine()
                sb.Append("<br/>")
                
                sb.Append("remove key").AppendLine()
                sb.Append("<br/>")
                
                c.RemoveItem(New Generic.KeyValuePair(Of String, Integer)("first", 100))
                
                sb.Append("cache has key first = " & c.ContainsKey("first"))
            ElseIf Request.QueryString.ToString = "getvalues" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)
                c.Add("second", 10)
                c.Add("third", 100)
                
                For Each k As String In c.Values
                    sb.Append(k).AppendLine()
                    sb.Append("<br/>")
                Next
            ElseIf Request.QueryString.ToString = "getnonexistent" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)
                c.Add("second", 10)
                c.Add("third", 100)
                
                Dim k As Integer = c("lonasdg")
            ElseIf Request.QueryString.ToString = "getnonexistent2" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)
                c.Add("second", 10)
                c.Add("third", 100)
                Dim i As Integer = -19
                sb.Append("second exists ").Append(c.TryGetValue("second", i)).Append(" equals ").Append(i).AppendLine()
                sb.Append("<br/>")
                i = -19
                sb.Append("second exists ").Append(c.TryGetValue("lkasdfnv", i)).Append(" equals ").Append(i)
            ElseIf Request.QueryString.ToString = "readd" Then
                Dim c As New HttpCacheDictionary(Of Integer)
                c.Add("first", 10)
                
                c.AddItem(New Generic.KeyValuePair(Of String, Integer)("first", 100))
                
                For Each k As DictionaryEntry In Cache
                    sb.Append(k.Key).Append(" = ").AppendLine(k.Value)
                    sb.Append("<br/>")
                Next
            End If
            
            pre.InnerHtml = sb.ToString
        End Sub
</script>
</head>
<body>
    <pre runat="server" id="pre"></pre>
</body>
</html>
