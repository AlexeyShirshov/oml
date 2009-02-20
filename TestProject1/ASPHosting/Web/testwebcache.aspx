<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm" %>
<%@ Import Namespace="Worm.Database" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Assembly Name="TestProject1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">

<script runat="server">
#Const UseUserInstance = True
    
    Public Class webcache
        Inherits Worm.Cache.WebCache
        
        Protected Overrides Function GetPolicy(ByVal t As System.Type) As Worm.Cache.WebCacheDictionaryPolicy
            Return WebCacheDictionaryPolicy.CreateDefault
        End Function
    End Class
    
    Protected _wc As New webcache
    
    Protected Function _CreateDBManager() As OrmManager
        Return CreateDBManager
    End Function
    
    Protected Function CreateDBManager() As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\wormtest.mdf"))
        Dim conn As String = "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;"
        Return New OrmReadOnlyDBManager(_wc, New ObjectMappingEngine("1"), New SQLGenerator, conn)
#Else
        Return New OrmReadOnlyDBManager(_wc, New ObjectMappingEngine("1"), New SQLGenerator, "Data Source=.\sqlexpress;Integrated Security=true;Initial Catalog=wormtest;")
#End If
    End Function
    
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim sb As New StringBuilder
        
        If Request.QueryString.ToString = "reset" Then
            Using mgr As OrmReadOnlyDBManager = CreateDBManager()
                Dim q As Query.QueryCmd = New Query.QueryCmd().Select(GetType(TestProject1.Table1), True)
                Dim r As ReadOnlyList(Of TestProject1.Table1) = q.ToList(Of TestProject1.Table1)(mgr)
                
                q.ResetObjects(mgr)
                
                r = q.ToList(Of TestProject1.Table1)(mgr)

                For Each t As TestProject1.Table1 In r
                    If t.InternalProperties.ObjectState <> Entities.ObjectState.None Then
                        Throw New ApplicationException
                    End If
                    If Not t.InternalProperties.IsLoaded Then
                        Throw New ApplicationException
                    End If
                    If t.Name Is Nothing Then
                        Throw New ApplicationException
                    End If
                Next
            End Using
        ElseIf Request.QueryString.ToString = "resetCmd" Then
            Dim q As Query.QueryCmd = New Worm.Query.QueryCmd(AddressOf _CreateDBManager).Select(GetType(TestProject1.Table1))
            Dim r As ReadOnlyList(Of TestProject1.Table1) = q.ToList(Of TestProject1.Table1)()
            
            q.ResetObjects()
            
            r = q.ToList(Of TestProject1.Table1)()
            
            Dim s As String = r(0).Name
        Else
            Using mgr As OrmReadOnlyDBManager = CreateDBManager()
    
                Dim o As New WebCacheEntityDictionary(Of TestProject1.Table1)(_wc)
    
                For Each t As TestProject1.Table1 In mgr.FindTop(Of TestProject1.Table1)(100, Nothing, Nothing, True)
                    o.Add(t.Identifier, t)
                Next
            
                For Each k As DictionaryEntry In Cache
                    sb.Append(k.Key).Append(" = ").AppendLine(k.Value.ToString)
                    sb.Append("<br/>")
                Next
            End Using
        End If
        
        pre.InnerHtml = sb.ToString
            
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
<%=GetTime() & "  test is ok"%>
</body>
</html>
