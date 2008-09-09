<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Database" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Assembly Name="TestProject1" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">

<script runat="server">
#Const UseUserInstance = True
    
    Public Class webcache
        Inherits Worm.Cache.WebCache
        
        Protected Overrides Function GetPolicy(ByVal t As System.Type) As Worm.Cache.DictionatyCachePolicy
            Return DictionatyCachePolicy.CreateDefault
        End Function
    End Class
    
    Protected _wc As New webcache
    
    Protected Function CreateDBManager() As OrmReadOnlyDBManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\wormtest.mdf"))
        Dim conn As String = "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;"
        Return New OrmReadOnlyDBManager(_wc, New SQLGenerator("1"), conn)
#Else
        Return New OrmReadOnlyDBManager(_wc, New SQLGenerator("1"), "Data Source=.\sqlexpress;Integrated Security=true;Initial Catalog=wormtest;")
#End If
    End Function
    
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim sb As New StringBuilder
        Using mgr As OrmReadOnlyDBManager = CreateDBManager()
            Dim o As New OrmDictionary(Of TestProject1.Table1)(mgr.Cache)
            
            'mgr.FindTop(Of TestProject1.Table1)(100, Nothing, Nothing, True)
            'Dim a As New ArrayList
            'For i As Integer = 0 To 5500000
            '    a.Add(New Data.DataSet)
            'Next
            'GC.Collect(3, GCCollectionMode.Forced)
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
<%=GetTime() & "  test is ok"%>
</body>
</html>