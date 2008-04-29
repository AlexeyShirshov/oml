<%@ Assembly Name="Worm.Orm" %>
<%@ Import Namespace="Worm.Cache" %>
<%@ Import Namespace="Worm.Database" %>
<%@ Import Namespace="Worm.Orm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Test simple page</title>
    <script runat="server" language="VB">
        
        Public Function GetTime() As Date
            Using mgr As OrmReadOnlyDBManager = CreateDBManager()
                Return Date.Now
            End Using
        End Function
      
        Public Function CreateDBManager() As OrmReadOnlyDBManager
#If UseUserInstance Then
            Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
            Return New OrmReadOnlyDBManager(New OrmCache, New SQLGenerator("1"), "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
            Return New OrmReadOnlyDBManager(New OrmCache, New SQLGenerator("1"), "Data Source=.\sqlexpress;Integrated Security=true;Initial Catalog=test;")
#End If
        End Function
    
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            If Request.QueryString.ToString = "GetAllRoles" Then
                Dim _roles() As String = Roles.GetAllRoles
            ElseIf Request.QueryString.ToString = "RoleExists" Then
                Dim b As Boolean = Roles.RoleExists("first")
                If Not b Then
                    Throw New Exception("cannot create role")
                End If
            ElseIf Request.QueryString.ToString = "AddUser2Role" Then
                Roles.AddUserToRole("kkk", "first")
                
                Try
                    If Roles.IsUserInRole("kkk", "second") Then
                        Throw New Exception("User must not be in role second")
                    End If
                    If Not Roles.IsUserInRole("kkk", "first") Then
                        Throw New Exception("User must be in role first")
                    End If
                    Dim users() As String = Roles.GetUsersInRole("first")
                    If users.Length <> 1 Then
                        Throw New Exception("There must be only one user in role first")
                    End If
                    users = Roles.GetUsersInRole("second")
                    If users.Length <> 0 Then
                        Throw New Exception("There must be zero users in role second")
                    End If
                    Dim _roles() As String = Roles.GetRolesForUser("kkk")
                    If _roles.Length <> 1 Then
                        Throw New Exception("There must be only one role for user kkk")
                    End If
                    _roles = Roles.GetRolesForUser("rosa")
                    If _roles.Length <> 0 Then
                        Throw New Exception("There must be zero roles for user rose")
                    End If
                    Roles.AddUserToRole("rosa", "first")
                    Try
                        users = Roles.FindUsersInRole("first", "k%")
                        If users.Length <> 1 Then
                            Throw New Exception("There must be only one user in role first with the name starts with k")
                        End If
                    Finally
                        Roles.RemoveUserFromRole("rosa", "first")
                    End Try
                Finally
                    Roles.RemoveUserFromRole("kkk", "first")
                End Try
            ElseIf Request.QueryString.ToString = "CreateRole" Then
                If Roles.RoleExists("third") Then
                    Throw New Exception("Role third must not be present")
                End If
                Try
                    Roles.CreateRole("third")
                Finally
                    Roles.DeleteRole("third", True)
                End Try
                'ElseIf Request.QueryString.ToString = "GetUserByEmail" Then
                '    Dim s As String = Membership.GetUserNameByEmail("12ef2363-7ec6-4bce-93f7-1a8fe6a74cd8")
                'ElseIf Request.QueryString.ToString = "UpdateUser" Then
                '    Dim u As MembershipUser = Membership.GetUser("12ef2363-7ec6-4bce-93f7-1a8fe6a74cd8")
                '    u.Comment = "asqwef89bh3e"
                '    Membership.UpdateUser(u)
                'ElseIf Request.QueryString.ToString = "CreateUser" Then
                '    Dim u As MembershipUser = Membership.CreateUser("asdf", "sadcnskojcn", "uibsdcfbnwf@fg.ru")
                '    Dim s As String = u.Email
                '    If s <> u.UserName Then
                '        Throw New ApplicationException
                '    End If
                
                '    Dim b As Boolean = Membership.ValidateUser("uibsdcfbnwf@fg.ru", "sadcnskojcn")
                '    If Not b Then
                '        Throw New ApplicationException
                '    End If
                
                '    b = Membership.ValidateUser("uibsdcfbnwf@fg.ru", "143g90nmsdrl;gm")
                '    If b Then
                '        Throw New ApplicationException
                '    End If

                '    Membership.DeleteUser("uibsdcfbnwf@fg.ru")
                'ElseIf Request.QueryString.ToString = "ResetPassword" Then
                '    Dim u As MembershipUser = Membership.GetUser("12ef2363-7ec6-4bce-93f7-1a8fe6a74cd8")
                '    lblInfo.Text = u.ResetPassword()
                'ElseIf Request.QueryString.ToString = "ValidatePassword" Then
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Membership.ValidateUser("a@a.ru", "iqer")
                '    Dim u As MembershipUser = Membership.GetUser("a@a.ru")
                '    If Not u.IsLockedOut Then
                '        Throw New ApplicationException
                '    End If
                
                '    u.UnlockUser()
            End If
        End Sub
</script>
</head>
<body>
    <%=GetTime() & "  test roles ok"%>
    <asp:Label runat="server" id="lblInfo" />
</body>
</html>
