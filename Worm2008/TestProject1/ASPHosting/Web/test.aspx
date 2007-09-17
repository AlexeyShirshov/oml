<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test simple page</title>
    <script runat="server" language="VB">
        'imports Worm.Orm
        
        Public Function GetTime() As Date
            'Using mgr As OrmReadOnlyDBManager = CreateDBManager()
            '    Return Date.Now
            'End Using
        End Function
        
        'Public Function CreateDBManager() As OrmReadOnlyDBManager
        '    Return New OrmReadOnlyDBManager(New Worm.Orm.OrmCache, New Worm.Orm.DbSchema("1"), "Data Source=vs2\sqlmain;Integrated Security=true;Initial Catalog=music2;")
        'End Function
    
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)

        End Sub
</script>
</head>
<body>
    <%=GetTime() & "  pmwdfmopadgmo"%>
</body>
</html>
