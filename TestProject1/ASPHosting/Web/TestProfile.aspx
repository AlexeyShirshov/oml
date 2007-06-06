<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test simple page</title>
    <script runat="server" language="VB">
        
        Public Function GetTime() As Date
            'Using mgr As OrmReadOnlyDBManager = CreateDBManager()
            Return Date.Now
            'End Using
        End Function
        
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim f As String = Profile.Field
            If f Is Nothing Then
                Profile.Field = "oadnfovnadf"
            End If
        End Sub
</script>
</head>
<body>
    <%=GetTime() & "  test profile ok"%>
</body>
</html>
