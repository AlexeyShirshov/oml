Imports System.Web
Imports System.Web.Security
Imports Worm
Imports Worm.Entities
Imports Worm.Database
Imports Worm.Criteria
Imports Worm.Query

Namespace Web
    Public MustInherit Class RoleBase
        Inherits RoleProvider

        Protected _rolenameField As String

        Public Overrides Property ApplicationName() As String
            Get
                Return UserMapper.ApplicationName
            End Get
            Set(ByVal value As String)
                UserMapper.ApplicationName = value
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

        'Protected ReadOnly Property ProfileProvider() As ProfileBase
        '    Get
        '        Dim p As Profile.ProfileProvider = Profile.ProfileManager.Provider
        '        If p Is Nothing Then
        '            Throw New InvalidOperationException("Profile provider must be set")
        '        End If
        '        Return CType(p, ProfileBase)
        '    End Get
        'End Property

        Protected Overridable ReadOnly Property UserMapper() As IUserMapping
            Get
                Dim p As Profile.ProfileProvider = Profile.ProfileManager.Provider
                Return CType(p, IUserMapping)
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

            Using mgr As OrmManager = UserMapper.CreateManager
                Dim users As New Generic.List(Of String)
                Dim oschema As Meta.IEntitySchema = mgr.MappingEngine.GetEntitySchema(UserMapper.GetUserType)
                For Each u As IKeyEntity In FindUsersInRoleInternal(mgr, roleName, usernameToMatch)
                    users.Add(CStr(mgr.MappingEngine.GetPropertyValue(u, UserMapper.UserNameField, oschema)))
                Next
                Return users.ToArray
            End Using
        End Function

        Public Overrides Function GetAllRoles() As String()
            Using mgr As OrmManager = UserMapper.CreateManager
                Dim roles As New Generic.List(Of String)
                Dim oschema As Meta.IEntitySchema = mgr.MappingEngine.GetEntitySchema(GetRoleType)
                'Dim col As IEnumerable = FindRoles(mgr, CType(New Ctor(GetRoleType).Field(OrmBaseT.PKName).NotEq(-1), CriteriaLink))
                Dim col As IEnumerable = New Query.QueryCmd().Select(GetRoleType).ToList(mgr)
                For Each r As IKeyEntity In col
                    roles.Add(CStr(mgr.MappingEngine.GetPropertyValue(r, _rolenameField, oschema)))
                Next
                Return roles.ToArray
            End Using
        End Function

        Public Overrides Function GetRolesForUser(ByVal username As String) As String()
            If String.IsNullOrEmpty(username) Then
                Throw New ArgumentException("username")
            End If

            Using mgr As OrmManager = UserMapper.CreateManager
                Dim roles As New Generic.List(Of String)
                Dim oschema As Meta.IEntitySchema = mgr.MappingEngine.GetEntitySchema(GetRoleType)
                For Each r As IKeyEntity In GetRolesForUserInternal(mgr, username)
                    roles.Add(CStr(mgr.MappingEngine.GetPropertyValue(r, _rolenameField, oschema)))
                Next
                Return roles.ToArray
            End Using
        End Function

        Public Overrides Function GetUsersInRole(ByVal roleName As String) As String()
            If String.IsNullOrEmpty(roleName) Then
                Throw New ArgumentException("roleName")
            End If

            Using mgr As OrmManager = UserMapper.CreateManager
                Dim users As New Generic.List(Of String)
                Dim oschema As Meta.IEntitySchema = mgr.MappingEngine.GetEntitySchema(UserMapper.GetUserType)
                For Each u As IKeyEntity In FindUsersInRoleInternal(mgr, roleName, Nothing)
                    users.Add(CStr(mgr.MappingEngine.GetPropertyValue(u, UserMapper.UserNameField, oschema)))
                Next
                Return users.ToArray
            End Using
        End Function

        Public Overrides Function IsUserInRole(ByVal username As String, ByVal roleName As String) As Boolean
            Using mgr As OrmManager = UserMapper.CreateManager
                Dim oschema As Meta.IEntitySchema = mgr.MappingEngine.GetEntitySchema(GetRoleType)
                For Each r As IKeyEntity In GetRolesForUserInternal(mgr, username)
                    If CStr(mgr.MappingEngine.GetPropertyValue(r, _rolenameField, oschema)) = roleName Then
                        Return True
                    End If
                Next
            End Using
            Return False
        End Function

        Public Overrides Function RoleExists(ByVal roleName As String) As Boolean
            Using mgr As OrmManager = UserMapper.CreateManager
                Dim r As IKeyEntity = GetRoleByName(mgr, roleName, False)
                Return r IsNot Nothing
            End Using
        End Function
#End Region

#Region " Control functions "

        Public Overrides Sub AddUsersToRoles(ByVal usernames() As String, ByVal roleNames() As String)
            Using mgr As OrmManager = UserMapper.CreateManager
                For Each username As String In usernames
                    Dim u As IKeyEntity = MembershipProvider.FindUserByName(mgr, username, Nothing)
                    If u IsNot Nothing Then
                        For Each role As String In roleNames
                            Dim r As IKeyEntity = GetRoleByName(mgr, role, False)
                            If r IsNot Nothing Then
                                u.Add(r)
                            End If
                        Next
                        u.SaveChanges(True)
                    End If
                Next
            End Using
        End Sub

        Public Overrides Sub CreateRole(ByVal roleName As String)
            Using mgr As OrmManager = UserMapper.CreateManager
                GetRoleByName(mgr, roleName, True)
            End Using
        End Sub

        Public Overrides Function DeleteRole(ByVal roleName As String, ByVal throwOnPopulatedRole As Boolean) As Boolean
            Using mgr As OrmManager = UserMapper.CreateManager
                Dim r As IKeyEntity = GetRoleByName(mgr, roleName, False)
                DeleteRole(CType(mgr, OrmDBManager), r, Not throwOnPopulatedRole)
            End Using
        End Function

        Public Overrides Sub RemoveUsersFromRoles(ByVal usernames() As String, ByVal roleNames() As String)
            Using mgr As OrmManager = UserMapper.CreateManager
                For Each username As String In usernames
                    Dim u As IKeyEntity = MembershipProvider.FindUserByName(mgr, username, Nothing)
                    If u IsNot Nothing Then
                        For Each role As String In roleNames
                            Dim r As IKeyEntity = GetRoleByName(mgr, role, False)
                            If r IsNot Nothing Then
                                CType(u, IRelations).Delete(r)
                            End If
                        Next
                    End If
                    u.SaveChanges(True)
                Next
            End Using
        End Sub

#End Region

        Protected Friend Function FindUsersInRoleInternal(ByVal mgr As OrmManager, ByVal roleName As String, ByVal usernameToMatch As String) As IList
            Dim r As IKeyEntity = GetRoleByName(mgr, roleName, False)
            'Dim users As New Generic.List(Of String)
            If r IsNot Nothing Then
                Dim f As PredicateLink = Nothing
                If usernameToMatch IsNot Nothing Then
                    f = CType(New Ctor(UserMapper.GetUserType).prop(UserMapper.UserNameField).[like](usernameToMatch), PredicateLink)
                End If
                Dim cmd As New Query.RelationCmd(r)
                cmd.Where(f).Select(UserMapper.GetUserType, WithLoad)
                Return cmd.ToList(mgr)
                'Return CType(r.Find(ProfileProvider.GetUserType, f, Nothing, WithLoad), IList)
            End If
            Return New IKeyEntity() {}
        End Function

        Protected Friend Function GetRolesForUserInternal(ByVal mgr As OrmManager, ByVal username As String) As IList
            Dim u As IKeyEntity = MembershipProvider.FindUserByName(mgr, username, Nothing)
            If u IsNot Nothing Then
                Return CType(u.GetCmd(GetRoleType).ToList(mgr), IList)
            End If
            Return New IKeyEntity() {}
        End Function

        Protected MustOverride Function GetRoleType() As Type
        Protected MustOverride Function GetRoleByName(ByVal mgr As OrmManager, ByVal name As String, ByVal createIfNotExist As Boolean) As IKeyEntity
        Protected MustOverride ReadOnly Property WithLoad() As Boolean

        Protected Overridable Function FindRoles(ByVal mgr As OrmManager, ByVal f As PredicateLink) As IList
            Dim cmd As New Query.QueryCmd()
            cmd.Where(f).Select(GetRoleType)
            Return cmd.ToList(mgr)
        End Function

        Protected Overridable Overloads Sub DeleteRole(ByVal mgr As OrmDBManager, ByVal role As IKeyEntity, ByVal cascade As Boolean)
            If cascade Then
                Throw New NotSupportedException("Cascade delete is not supported")
            End If
            CType(role, ICachedEntity).Delete(mgr)
            role.SaveChanges(True)
        End Sub

    End Class
End Namespace