Imports System.Web
Imports System.Web.Security
Imports Worm
Imports Worm.Orm
Imports Worm.Database
Imports Worm.Database.Criteria

Namespace Web
    Public MustInherit Class RoleBase
        Inherits RoleProvider

        Protected _rolenameField As String

        Public Overrides Property ApplicationName() As String
            Get
                Return ProfileProvider.ApplicationName
            End Get
            Set(ByVal value As String)
                ProfileProvider.ApplicationName = value
            End Set
        End Property

        Public Overrides Sub Initialize(ByVal name As String, ByVal config As System.Collections.Specialized.NameValueCollection)
            If Not String.IsNullOrEmpty(config("RolenameField")) Then
                _rolenameField = config("RolenameField")
            Else
                Throw New ArgumentException("RolenameField attribute must be specified")
            End If

            MyBase.Initialize(name, config)
        End Sub

        Protected ReadOnly Property ProfileProvider() As ProfileBase
            Get
                Dim p As Profile.ProfileProvider = Profile.ProfileManager.Provider
                If p Is Nothing Then
                    Throw New InvalidOperationException("Profile provider must be set")
                End If
                Return CType(p, ProfileBase)
            End Get
        End Property

        Protected ReadOnly Property MembershipProvider() As MembershipBase
            Get
                Dim p As MembershipBase = TryCast(Membership.Provider, MembershipBase)
                If p Is Nothing Then
                    Throw New InvalidOperationException("Membership provider must be set")
                End If
                Return CType(p, MembershipBase)
            End Get
        End Property

#Region " Query function "

        Public Overrides Function FindUsersInRole(ByVal roleName As String, ByVal usernameToMatch As String) As String()
            If usernameToMatch Is Nothing Then
                Throw New ArgumentNullException("usernameToMatch")
            End If

            If roleName Is Nothing Then
                Throw New ArgumentNullException("roleName")
            End If

            If String.IsNullOrEmpty(usernameToMatch) Then
                Throw New ArgumentException("usernameToMatch")
            End If

            If String.IsNullOrEmpty(roleName) Then
                Throw New ArgumentException("roleName")
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim users As New Generic.List(Of String)
                For Each u As OrmBase In FindUsersInRoleInternal(mgr, roleName, usernameToMatch)
                    users.Add(CStr(u.GetValue(ProfileProvider._userNameField)))
                Next
                Return users.ToArray
            End Using
        End Function

        Public Overrides Function GetAllRoles() As String()
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim roles As New Generic.List(Of String)
                For Each r As OrmBase In FindRoles(mgr, CType(New Ctor(GetRoleType).Field(OrmBaseT.PKName).NotEq(-1), CriteriaLink))
                    roles.Add(CStr(r.GetValue(_rolenameField)))
                Next
                Return roles.ToArray
            End Using
        End Function

        Public Overrides Function GetRolesForUser(ByVal username As String) As String()
            If String.IsNullOrEmpty(username) Then
                Throw New ArgumentException("username")
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim roles As New Generic.List(Of String)
                For Each r As OrmBase In GetRolesForUserInternal(mgr, username)
                    roles.Add(CStr(r.GetValue(_rolenameField)))
                Next
                Return roles.ToArray
            End Using
        End Function

        Public Overrides Function GetUsersInRole(ByVal roleName As String) As String()
            If String.IsNullOrEmpty(roleName) Then
                Throw New ArgumentException("roleName")
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim users As New Generic.List(Of String)
                For Each u As OrmBase In FindUsersInRoleInternal(mgr, roleName, Nothing)
                    users.Add(CStr(u.GetValue(ProfileProvider._userNameField)))
                Next
                Return users.ToArray
            End Using
        End Function

        Public Overrides Function IsUserInRole(ByVal username As String, ByVal roleName As String) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                For Each r As OrmBase In GetRolesForUserInternal(mgr, username)
                    If CStr(r.GetValue(_rolenameField)) = roleName Then
                        Return True
                    End If
                Next
            End Using
            Return False
        End Function

        Public Overrides Function RoleExists(ByVal roleName As String) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim r As OrmBase = GetRoleByName(mgr, roleName, False)
                Return r IsNot Nothing
            End Using
        End Function
#End Region

#Region " Control functions "

        Public Overrides Sub AddUsersToRoles(ByVal usernames() As String, ByVal roleNames() As String)
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                For Each username As String In usernames
                    Dim u As OrmBase = MembershipProvider.FindUserByName(mgr, username, Nothing)
                    If u IsNot Nothing Then
                        For Each role As String In roleNames
                            Dim r As OrmBase = GetRoleByName(mgr, role, False)
                            If r IsNot Nothing Then
                                u.M2M.Add(r)
                            End If
                        Next
                        u.SaveChanges(True)
                    End If
                Next
            End Using
        End Sub

        Public Overrides Sub CreateRole(ByVal roleName As String)
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                GetRoleByName(mgr, roleName, True)
            End Using
        End Sub

        Public Overrides Function DeleteRole(ByVal roleName As String, ByVal throwOnPopulatedRole As Boolean) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim r As OrmBase = GetRoleByName(mgr, roleName, False)
                DeleteRole(mgr, r, Not throwOnPopulatedRole)
            End Using
        End Function

        Public Overrides Sub RemoveUsersFromRoles(ByVal usernames() As String, ByVal roleNames() As String)
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                For Each username As String In usernames
                    Dim u As OrmBase = MembershipProvider.FindUserByName(mgr, username, Nothing)
                    If u IsNot Nothing Then
                        For Each role As String In roleNames
                            Dim r As OrmBase = GetRoleByName(mgr, role, False)
                            If r IsNot Nothing Then
                                u.M2M.Delete(r)
                            End If
                        Next
                    End If
                    u.SaveChanges(True)
                Next
            End Using
        End Sub

#End Region

        Protected Friend Function FindUsersInRoleInternal(ByVal mgr As OrmDBManager, ByVal roleName As String, ByVal usernameToMatch As String) As IList
            Dim r As OrmBase = GetRoleByName(mgr, roleName, False)
            'Dim users As New Generic.List(Of String)
            If r IsNot Nothing Then
                Dim f As CriteriaLink = Nothing
                If usernameToMatch IsNot Nothing Then
                    f = CType(New Ctor(ProfileProvider.GetUserType).Field(ProfileProvider._userNameField).Like(usernameToMatch), CriteriaLink)
                End If
                Return CType(r.M2M.Find(ProfileProvider.GetUserType, f, Nothing, WithLoad), IList)
            End If
            Return New OrmBase() {}
        End Function

        Protected Friend Function GetRolesForUserInternal(ByVal mgr As OrmDBManager, ByVal username As String) As IList
            Dim u As OrmBase = MembershipProvider.FindUserByName(mgr, username, Nothing)
            If u IsNot Nothing Then
                Return CType(u.M2M.Find(GetRoleType, Nothing, Nothing, WithLoad), IList)
            End If
            Return New OrmBase() {}
        End Function

        Protected MustOverride Function GetRoleType() As Type
        Protected MustOverride Function GetRoleByName(ByVal mgr As OrmDBManager, ByVal name As String, ByVal createIfNotExist As Boolean) As OrmBase
        Protected Friend MustOverride Function FindRoles(ByVal mgr As OrmDBManager, ByVal f As CriteriaLink) As IList
        Protected MustOverride Overloads Sub DeleteRole(ByVal mgr As OrmDBManager, ByVal role As OrmBase, ByVal cascade As Boolean)
        Protected MustOverride ReadOnly Property WithLoad() As Boolean
    End Class
End Namespace