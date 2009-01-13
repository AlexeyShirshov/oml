<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Import Namespace="System.Collections.Generic" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test Http dic</title>
    <script runat="server" language="VB">
        Protected Created As Boolean
        
        Protected Overridable Sub CacheItemRemovedCallback1(ByVal key As String, ByVal value As Object, _
            ByVal reason As Caching.CacheItemRemovedReason)
            Application("cnt") = CInt(Application("cnt")) + 1
        End Sub
        
        Protected Function GetDic() As IDictionary(Of String, Integer)
            Dim dic As IDictionary(Of String, Integer) = Application("dic")
            If dic Is Nothing Then
                Dim dic2 As New WebCacheDictionary(Of Integer)(Now.AddMinutes(1), _
                    Caching.Cache.NoSlidingExpiration, CacheItemPriority.Low, Nothing)
                dic2.CacheItemRemovedCallback = AddressOf CacheItemRemovedCallback1
                dic = dic2
                Application("dic") = dic
                Created = True
            End If
            Return dic
        End Function
        
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            If Request.QueryString.ToString <> "second" Then
                Dim sb As New StringBuilder
            
                GetDic("xxx") = 10
                GetDic("xxx") = 11
                GetDic("yyy") = 100
            
                pre.InnerHtml = sb.ToString
            Else
                'Dim i As Integer = GetDic("xxx")
            End If
        End Sub
</script>
</head>
<body>
    <pre runat="server" id="pre"></pre>test is ok. cnt = <%=CInt(Application("cnt"))%>.created = <%=Created%>
</body>
</html>
