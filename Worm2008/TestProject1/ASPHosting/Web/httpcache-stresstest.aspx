<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test HttpCache Page</title>
    <script runat="server" language="VB">
        
        Protected Function GetDic() As Generic.IDictionary(Of String, Integer)
            Dim key As String = "mydic"
            If Cache(key) Is Nothing Then
                Cache(key) = New HttpCacheDictionary(Of Integer)
            End If
            
            Return Cache(key)
        End Function
        
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim sb As New StringBuilder
            
            'For Each s As String In Request.QueryString.AllKeys
            '    sb.Append(s).AppendLine()
            '    sb.Append("<br/>")
            'Next
            
            If Not String.IsNullOrEmpty(Request.QueryString("add")) Then
                Dim c As Generic.IDictionary(Of String, Integer) = GetDic()
                
                For i As Integer = 0 To 10000 - 1
                    Dim key As String = Guid.NewGuid.ToString
                    c.Add(key, i)
                Next

                For Each key As String In c.Keys
                    sb.Append(c(key)).AppendLine()
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
