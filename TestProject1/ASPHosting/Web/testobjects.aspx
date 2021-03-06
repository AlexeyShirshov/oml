<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm" %>
<%@ Import Namespace="Worm.Database" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Assembly Name="TestProject1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">

<script runat="server">
#Const UseUserInstance = True

    Protected Function CreateDBManager() As OrmReadOnlyDBManager
        Dim c As New OrmCache
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\TestProject1\Databases\wormtest.mdf"))
        Dim conn As String = "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;Database=wormtest;"
        Return New OrmReadOnlyDBManager(c, New ObjectMappingEngine("1"), New SQLGenerator, conn)
#Else
        Return New OrmReadOnlyDBManager(c, New objectmappingengine("1"), New SQLGenerator, "Data Source=.\sqlexpress;Integrated Security=true;Initial Catalog=wormtest;")
#End If
    End Function
    
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim sb As New StringBuilder
        Using mgr As OrmReadOnlyDBManager = CreateDBManager()
            Dim o As New WebCacheEntityDictionary(Of TestProject1.Table1)(mgr.Cache)
            
            For Each t As TestProject1.Table1 In mgr.FindTop(Of TestProject1.Table1)(100, Nothing, Nothing, True)
                o.Add(t.Identifier, t)
            Next
            
            For Each k As DictionaryEntry In Cache
                sb.Append(k.Key).Append(" = ").AppendLine(k.Value.ToString)
                sb.Append("<br/>")
            Next
            
            pre.InnerHtml = sb.ToString
            
        End Using
        
        
    End Sub
    
    Public Function GetTime() As Date
        Using mgr As OrmReadOnlyDBManager = CreateDBManager()
            Return Date.Now
        End Using
    End Function
</script>

<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test objects</title>
</head>
<body><pre runat="server" id="pre" />
<%=GetTime() & "  test objects ok"%>
</body>
</html>
