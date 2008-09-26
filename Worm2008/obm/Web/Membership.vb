Imports System.Web
Imports System.Web.Security
Imports System.Web.Profile
Imports Worm.Orm
Imports System.Configuration
Imports Worm.Database
Imports Worm.Database.Criteria
Imports Worm.Orm.Meta

Namespace Web
    Public Class MembershipBase
        Inherits MembershipProvider

        Private _passwordLength As Integer
        Private _invalidAttemtWindow As Integer
        Private _invalidAttemtCount As Integer
        Private _treatUsernameAsEmail As Boolean
        Private _throwExceptionInValidate As Boolean

        Public Overrides Property ApplicationName() As String
            Get
                Return ProfileProvider.ApplicationName
            End Get
            Set(ByVal value As String)
                ProfileProvider.ApplicationName = value
            End Set
        End Property

        Public Overrides Sub Initialize(ByVal name As String, ByVal config As System.Collections.Specialized.NameValueCollection)
            If config("minRequiredPasswordLength") Is Nothing Then
                _passwordLength = 6
            Else
                _passwordLength = CInt(config("minRequiredPasswordLength"))
            End If

            If config("maxInvalidPasswordAttempts") Is Nothing Then
                _invalidAttemtCount = 5
            Else
                _invalidAttemtCount = CInt(config("maxInvalidPasswordAttempts"))
            End If

            If config("passwordAttemptWindow") Is Nothing Then
                _invalidAttemtWindow = _invalidAttemtCount * 1
            Else
                _invalidAttemtWindow = CInt(config("passwordAttemptWindow"))
            End If

            If config("treatUsernameAsEmail") Is Nothing Then
                _treatUsernameAsEmail = False
            Else
                _treatUsernameAsEmail = CBool(config("treatUsernameAsEmail"))
            End If

            If config("throwExceptionInValidate") Is Nothing Then
                _throwExceptionInValidate = False
            Else
                _throwExceptionInValidate = CBool(config("throwExceptionInValidate"))
            End If

            If HttpContext.Current IsNot Nothing Then
                AddHandler HttpContext.Current.ApplicationInstance.PostAuthorizeRequest, AddressOf UpdateLastActivity
            End If

            MyBase.Initialize(name, config)
        End Sub

#Region " Readonly properties "

        Public Overrides ReadOnly Property EnablePasswordReset() As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property EnablePasswordRetrieval() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MaxInvalidPasswordAttempts() As Integer
            Get
                Return _invalidAttemtCount
            End Get
        End Property

        Public Overrides ReadOnly Property MinRequiredNonAlphanumericCharacters() As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property MinRequiredPasswordLength() As Integer
            Get
                Return _passwordLength
            End Get
        End Property

        Public Overrides ReadOnly Property PasswordAttemptWindow() As Integer
            Get
                Return _invalidAttemtWindow
            End Get
        End Property

        Public Overrides ReadOnly Property PasswordFormat() As System.Web.Security.MembershipPasswordFormat
            Get
                Return MembershipPasswordFormat.Hashed
            End Get
        End Property

        Public Overrides ReadOnly Property PasswordStrengthRegularExpression() As String
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property RequiresQuestionAndAnswer() As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property RequiresUniqueEmail() As Boolean
            Get
                Return True
            End Get
        End Property
#End Region

#Region " Control functions "

        Public Overrides Function ChangePassword(ByVal username As String, ByVal oldPassword As String, ByVal newPassword As String) As Boolean

            If Not ValidateUser(username, oldPassword) Then Return False

            Dim args As ValidatePasswordEventArgs = New ValidatePasswordEventArgs(username, newPassword, True)

            OnValidatingPassword(args)

            If args.Cancel Then
                If Not args.FailureInformation Is Nothing Then
                    Throw args.FailureInformation
                Else
                    Throw New Provider.ProviderException("Change password canceled due to New password validation failure.")
                End If
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, username, Nothing)
                Else
                    u = FindUserByName(mgr, username, Nothing)
                End If
                If u IsNot Nothing Then
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                    Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                        Using u.BeginEdit()
                            schema.SetFieldValue(u, GetField("Password"), HashPassword(newPassword), oschema)
                            Dim lpcf As String = GetField("LastPasswordChangeDate")
                            If schema.HasField(u.GetType, lpcf) Then
                                schema.SetFieldValue(u, lpcf, ProfileProvider.GetNow, oschema)
                            End If
                        End Using
                        st.Commit()
                    End Using
                End If
            End Using

            Return True
        End Function

        Public Overrides Function ChangePasswordQuestionAndAnswer(ByVal username As String, ByVal password As String, ByVal newPasswordQuestion As String, ByVal newPasswordAnswer As String) As Boolean
            Return False
        End Function

        Public Overrides Function CreateUser(ByVal username As String, ByVal password As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String, _
            ByVal isApproved As Boolean, ByVal providerUserKey As Object, ByRef status As System.Web.Security.MembershipCreateStatus) As System.Web.Security.MembershipUser

            Dim Args As ValidatePasswordEventArgs = New ValidatePasswordEventArgs(username, password, True)

            OnValidatingPassword(Args)

            If Args.Cancel Then
                status = MembershipCreateStatus.InvalidPassword
                Return Nothing
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                If RequiresUniqueEmail Then
                    If Not EmptyEmail AndAlso String.IsNullOrEmpty(email) Then
                        status = MembershipCreateStatus.InvalidEmail
                        Return Nothing
                    End If

                    If Not String.IsNullOrEmpty(email) AndAlso GetUserNameByEmail(email) IsNot Nothing Then
                        status = MembershipCreateStatus.DuplicateEmail
                        Return Nothing
                    End If
                Else
                    If FindUserByName(mgr, username, Nothing) IsNot Nothing Then
                        status = MembershipCreateStatus.DuplicateUserName
                        Return Nothing
                    End If
                End If

                If String.IsNullOrEmpty(username) Then
                    If _treatUsernameAsEmail Then
                        username = email
                    Else
                        status = MembershipCreateStatus.InvalidUserName
                        Return Nothing
                    End If
                End If


                Dim u As OrmBase = ProfileProvider.CreateUser(mgr, username, Nothing)
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)

                schema.SetFieldValue(u, GetField("Email"), email, oschema)
                schema.SetFieldValue(u, GetField("Password"), HashPassword(password), oschema)

                Dim d As Date = ProfileProvider.GetNow()

                If Not String.IsNullOrEmpty(ProfileProvider._lastActivityField) Then
                    schema.SetFieldValue(u, ProfileProvider._lastActivityField, d, oschema)
                End If

                If Not String.IsNullOrEmpty(ProfileProvider._isAnonymousField) Then
                    schema.SetFieldValue(u, ProfileProvider._isAnonymousField, False, oschema)
                End If

                Dim llf As String = GetField("LastLoginDate")
                If schema.HasField(u.GetType, llf) Then
                    schema.SetFieldValue(u, llf, d, oschema)
                End If

                Dim crf As String = GetField("CreationDate")
                If schema.HasField(u.GetType, crf) Then
                    schema.SetFieldValue(u, crf, d, oschema)
                End If

                u.SaveChanges(True)

                UserCreated(u)
                status = MembershipCreateStatus.Success
                Return CreateMembershipUser(schema, u)
            End Using

        End Function

        Public Overrides Function DeleteUser(ByVal username As String, ByVal deleteAllRelatedData As Boolean) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, username, Nothing)
                Else
                    u = FindUserByName(mgr, username, Nothing)
                End If
                If u IsNot Nothing Then
                    ProfileProvider.DeleteUser(mgr, u, deleteAllRelatedData)
                End If
            End Using
        End Function

        Public Overrides Function ResetPassword(ByVal username As String, ByVal answer As String) As String
            Dim psw As String = Membership.GeneratePassword(MinRequiredPasswordLength, MinRequiredNonAlphanumericCharacters)

            Dim Args As ValidatePasswordEventArgs = New ValidatePasswordEventArgs(username, psw, True)

            OnValidatingPassword(Args)

            If Args.Cancel Then
                If Not Args.FailureInformation Is Nothing Then
                    Throw Args.FailureInformation
                Else
                    Throw New MembershipPasswordException("Reset password canceled due to password validation failure.")
                End If
            End If

            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, username, Nothing)
                Else
                    u = FindUserByName(mgr, username, Nothing)
                End If
                If u IsNot Nothing Then
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                    Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                        Using u.BeginEdit()
                            schema.SetFieldValue(u, GetField("Password"), HashPassword(psw), oschema)
                            Dim lpcf As String = GetField("LastPasswordChangeDate")
                            If schema.HasField(u.GetType, lpcf) Then
                                schema.SetFieldValue(u, lpcf, ProfileProvider.GetNow, oschema)
                            End If
                        End Using
                        st.Commit()
                    End Using
                    PasswordChanged(u)
                End If
            End Using

            Return psw
        End Function

        Public Overrides Function UnlockUser(ByVal userName As String) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, userName, Nothing)
                Else
                    u = FindUserByName(mgr, userName, Nothing)
                End If
                If u IsNot Nothing Then
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim lf As String = GetField("IsLockedOut")
                    If schema.HasField(u.GetType, lf) Then
                        Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                        Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                            Using u.BeginEdit()
                                schema.SetFieldValue(u, lf, False, oschema)
                                'Dim llf As String = GetField("LastLockoutDate")
                                'If schema.HasField(u.GetType, llf) Then
                                '    schema.SetFieldValue(u, llf, ProfileProvider.GetNow)
                                'End If
                            End Using
                            st.Commit()
                        End Using
                        Return True
                    End If
                End If
                Return False
            End Using
        End Function

        Public Overrides Sub UpdateUser(ByVal user As System.Web.Security.MembershipUser)
            If _treatUsernameAsEmail Then
                Using mgr As OrmDBManager = ProfileProvider._getMgr()
                    Dim u As OrmBase = Nothing
                    FindUserByEmail(mgr, user.Email, Nothing)
                    If u IsNot Nothing Then
                        Dim schema As ObjectMappingEngine = mgr.MappingEngine
                        Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                        Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                            Using u.BeginEdit()
                                schema.SetFieldValue(u, ProfileProvider._userNameField, user.Comment, oschema)
                            End Using
                            st.Commit()
                        End Using
                    End If
                End Using
            End If
        End Sub

        Public Overrides Function ValidateUser(ByVal username As String, ByVal password As String) As Boolean
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, username, Nothing)
                Else
                    u = FindUserByName(mgr, username, Nothing)
                End If
                If u Is Nothing Then
                    Return False
                End If

                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim lf As String = GetField("IsLockedOut")
                Dim tt As System.Type = u.GetType

                If schema.HasField(tt, lf) AndAlso CBool(u.GetValue(lf)) Then
                    Return False
                End If

                If Not ComparePasswords(CType(u.GetValue(GetField("Password")), Byte()), HashPassword(password)) Then
                    If _throwExceptionInValidate Then
                        UpdateFailureCount(mgr, u)
                    Else
                        Try
                            UpdateFailureCount(mgr, u)
                        Catch ex As Exception

                        End Try
                    End If
                    Return False
                End If

                Dim ret As Boolean = CanLogin(mgr, u)
                If ret Then
                    Dim st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                    Try
                        Using u.BeginEdit()
                            Dim llf As String = GetField("LastLoginDate")
                            If schema.HasField(tt, llf) Then
                                schema.SetFieldValue(u, llf, ProfileProvider.GetNow, oschema)
                            End If
                            If schema.HasField(tt, GetField("FailedPasswordAttemtCount")) Then
                                schema.SetFieldValue(u, GetField("FailedPasswordAttemtCount"), 0, oschema)
                            End If
                        End Using
                        st.Commit()
                    Finally
                        If _throwExceptionInValidate Then
                            st.Dispose()
                        Else
                            Try
                                st.Dispose()
                            Catch ex As Exception
                            End Try
                        End If
                    End Try
                End If
                Return ret
            End Using
        End Function
#End Region

#Region " Query functions "
        Public Overrides Function FindUsersByEmail(ByVal emailToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, MapField("Email"), New TypeWrap(Of Object)(emailToMatch), FilterOperation.Like))
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim c As CriteriaLink = CType(New Worm.Database.Criteria.Ctor(ProfileProvider.GetUserType).Field(MapField("Email")).Like(emailToMatch), CriteriaLink)
                Dim users As IList = ProfileProvider.FindUsers(mgr, c)
                totalRecords = users.Count
                Return CreateUserCollection(users, schema, pageIndex, pageSize)
            End Using
        End Function

        Public Overrides Function FindUsersByName(ByVal usernameToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            'Return FindUsersByEmail(usernameToMatch, pageIndex, pageSize, totalRecords)
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._userNameField, New TypeWrap(Of Object)(usernameToMatch), FilterOperation.Like))
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim users As IList = ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field(ProfileProvider._userNameField).Like(usernameToMatch))
                totalRecords = users.Count
                Return CreateUserCollection(users, schema, pageIndex, pageSize)
            End Using
        End Function

        Public Overrides Function GetAllUsers(ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                'Dim f As New OrmFilter(ProfileProvider.GetUserType, "ID", New TypeWrap(Of Object)(-1), FilterOperation.NotEqual)
                Dim users As IList = ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field("ID").NotEq(-1))
                totalRecords = users.Count
                Return CreateUserCollection(users, schema, pageIndex, pageSize)
            End Using
        End Function

        Public Overrides Function GetNumberOfUsersOnline() As Integer
            If String.IsNullOrEmpty(ProfileProvider._lastActivityField) Then
                Throw New InvalidOperationException("LastActivity field is not specified")
            End If

            Dim onlineSpan As TimeSpan = New TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0)
            Dim compareTime As DateTime = ProfileProvider.GetNow.Subtract(onlineSpan)
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._lastActivityField, New TypeWrap(Of Object)(compareTime), FilterOperation.GreaterThan))
                Return ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field(ProfileProvider._lastActivityField).GreaterThan(compareTime)).Count
            End Using
        End Function

        Public Overrides Function GetPassword(ByVal username As String, ByVal answer As String) As String
            Throw New NotSupportedException
        End Function

        Public Overloads Overrides Function GetUser(ByVal providerUserKey As Object, ByVal userIsOnline As Boolean) As System.Web.Security.MembershipUser
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, "ID", New TypeWrap(Of Object)(providerUserKey), FilterOperation.Equal))
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim users As IList = ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field("ID").Eq(providerUserKey))
                If users.Count <> 1 Then
                    Return Nothing
                End If
                Dim u As OrmBase = CType(users(0), OrmBase)
                If userIsOnline Then
                    If Not IsUserOnline(schema, u) Then
                        Return Nothing
                    End If
                End If
                Return CreateMembershipUser(schema, u)
            End Using
        End Function

        Public Overloads Overrides Function GetUser(ByVal username As String, ByVal userIsOnline As Boolean) As System.Web.Security.MembershipUser
            If Not String.IsNullOrEmpty(username) Then
                Using mgr As OrmDBManager = ProfileProvider._getMgr()
                    Dim u As OrmBase = Nothing
                    If _treatUsernameAsEmail Then
                        u = FindUserByEmail(mgr, username, userIsOnline)
                    Else
                        u = FindUserByName(mgr, username, userIsOnline)
                    End If

                    If u Is Nothing Then
                        Return Nothing
                    End If
                    Return CreateMembershipUser(mgr.MappingEngine, u)
                End Using
            End If
            Return Nothing
        End Function

        Public Overrides Function GetUserNameByEmail(ByVal email As String) As String
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim u As OrmBase = FindUserByEmail(mgr, email, Nothing)
                If u Is Nothing Then
                    Return Nothing
                End If
                Return CStr(u.GetValue(GetField("Email")))
            End Using
        End Function
#End Region

#Region " Helpers "

        Protected ReadOnly Property ProfileProvider() As ProfileBase
            Get
                Dim p As Profile.ProfileProvider = Profile.ProfileManager.Provider
                If p Is Nothing Then
                    Throw New InvalidOperationException("Profile provider must be set")
                End If
                Return CType(p, ProfileBase)
            End Get
        End Property

        Protected Function CreateMembershipUser(ByVal schema As ObjectMappingEngine, ByVal u As OrmBase) As MembershipUser
            Dim lf As String = GetField("IsLockedOut")
            Dim islockedout As Boolean = False
            Dim ut As System.Type = u.GetType

            If schema.HasField(ut, lf) Then
                islockedout = CBool(u.GetValue(lf))
            End If

            Dim crf As String = GetField("CreationDate")
            Dim created As Date = Date.MinValue
            If schema.HasField(ut, crf) Then
                created = CDate(u.GetValue(crf))
            End If

            Dim llf As String = GetField("LastLoginDate")
            Dim lastlogin As Date = Date.MinValue
            If schema.HasField(ut, llf) Then
                lastlogin = CDate(u.GetValue(llf))
            End If

            Dim lpcf As String = GetField("LastPasswordChangedDate")
            Dim lastpsw As Date = Date.MinValue
            If schema.HasField(ut, lpcf) Then
                lastpsw = CDate(u.GetValue(lpcf))
            End If

            Dim lld As String = GetField("LastLockoutDate")
            Dim lastlockout As Date = Date.MinValue
            If schema.HasField(ut, lld) Then
                lastlockout = CDate(u.GetValue(lld))
            End If

            Dim lastact As Date = Date.MinValue
            If Not String.IsNullOrEmpty(ProfileProvider._lastActivityField) Then
                lastact = CDate(u.GetValue(ProfileProvider._lastActivityField))
            End If

            Dim uname As String = Nothing
            If Not String.IsNullOrEmpty(ProfileProvider._userNameField) Then
                uname = CStr(u.GetValue(ProfileProvider._userNameField))
            End If

            Dim username As String = Nothing
            If _treatUsernameAsEmail Then
                username = CStr(u.GetValue(GetField("Email")))
            Else
                username = uname
                uname = Nothing
            End If

            Dim mu As New MembershipUser(Me.Name, _
                username, _
                u.Identifier, _
                CStr(u.GetValue(GetField("Email"))), _
                Nothing, uname, _
                True, islockedout, created, lastlogin, _
                lastact, _
                lastpsw, lastlockout)
            Return mu
        End Function

        Protected Function CreateUserCollection(ByVal users As IList, ByVal schema As ObjectMappingEngine) As MembershipUserCollection
            Dim uc As New MembershipUserCollection
            For Each u As OrmBase In users
                uc.Add(CreateMembershipUser(schema, u))
            Next
            Return uc
        End Function

        Protected Function CreateUserCollection(ByVal users As IList, ByVal schema As ObjectMappingEngine, ByVal pageIndex As Integer, ByVal pageSize As Integer) As MembershipUserCollection
            Dim uc As New MembershipUserCollection
            Dim start As Integer = Math.Max(0, (pageIndex - 1) * pageSize)
            If start < users.Count Then
                Dim [end] As Integer = users.Count
                If pageIndex <> 0 Then
                    [end] = Math.Min(pageIndex * pageSize, users.Count)
                End If
                For i As Integer = start To [end] - 1
                    Dim u As OrmBase = CType(users(i), OrmBase)
                    uc.Add(CreateMembershipUser(schema, u))
                Next
            End If
            Return uc
        End Function

        Protected Function GetField(ByVal field As String) As String
            Dim uf As String = MapField(field)
            If String.IsNullOrEmpty(uf) Then
                Throw New InvalidOperationException(String.Format("Cannot map {0} field", field))
            End If
            Return uf
        End Function

        'Protected Function GetUserByEmail(ByVal email As String, ByVal mgr As OrmDBManager) As OrmBase
        '    'Using mgr As OrmDBManager = ProfileProvider._getMgr()
        '    Dim c As New OrmCondition.OrmConditionConstructor
        '    c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, GetField("Email"), email, FilterOperation.Equal))
        '    Dim schema As OrmSchemaBase = mgr.DatabaseSchema
        '    Dim users As IList = ProfileProvider.FindUsers(mgr, c.Condition)
        '    If users.Count <> 1 Then
        '        Return Nothing
        '    End If
        '    Dim u As OrmBase = CType(users(0), OrmBase)
        '    Return u
        '    'End Using
        'End Function

        Protected Function FindUserByEmail(ByVal mgr As OrmDBManager, ByVal email As String, ByVal userIsOnline As Nullable(Of Boolean)) As OrmBase
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, GetField("Email"), New TypeWrap(Of Object)(email), FilterOperation.Equal))
            Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim users As IList = ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field(GetField("Email")).Eq(email))
            If users.Count <> 1 Then
                Return Nothing
            End If
            Dim u As OrmBase = CType(users(0), OrmBase)
            If userIsOnline.HasValue AndAlso IsUserOnline(schema, u) <> userIsOnline.Value Then
                Return Nothing
            End If
            Return u
        End Function

        Protected Friend Function FindUserByName(ByVal mgr As OrmDBManager, ByVal username As String, ByVal userIsOnline As Nullable(Of Boolean)) As OrmBase
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._userNameField, New TypeWrap(Of Object)(username), FilterOperation.Equal))
            Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim users As IList = ProfileProvider.FindUsers(mgr, New Ctor(ProfileProvider.GetUserType).Field(ProfileProvider._userNameField).Eq(username))
            If users.Count <> 1 Then
                Return Nothing
            End If
            Dim u As OrmBase = CType(users(0), OrmBase)
            If userIsOnline.HasValue AndAlso IsUserOnline(schema, u) <> userIsOnline.Value Then
                Return Nothing
            End If
            Return u
        End Function

        Protected Sub UpdateLastActivity(ByVal sender As Object, ByVal e As EventArgs)
            UpdateLastActivity()
        End Sub

        Protected Overridable Sub UpdateLastActivity()
            Using mgr As OrmDBManager = ProfileProvider._getMgr()
                Dim u As OrmBase = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(mgr, HttpContext.Current.User.Identity.Name, Nothing)
                Else
                    u = FindUserByName(mgr, HttpContext.Current.User.Identity.Name, Nothing)
                End If

                If u IsNot Nothing Then
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim laf As String = ProfileProvider._lastActivityField
                    If Not String.IsNullOrEmpty(laf) Then
                        Dim dt As Date = CDate(u.GetValue(laf))
                        Dim n As Date = ProfileProvider.GetNow
                        If n.Subtract(dt).TotalSeconds > 1 Then
                            Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                            Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                                Using u.BeginEdit
                                    schema.SetFieldValue(u, laf, n, oschema)
                                End Using
                                st.Commit()
                            End Using
                        End If
                    End If
                End If
            End Using
        End Sub

        Protected Function IsUserOnline(ByVal schema As ObjectMappingEngine, ByVal u As OrmBase) As Boolean
            If String.IsNullOrEmpty(ProfileProvider._lastActivityField) Then
                Throw New InvalidOperationException("LastActivity field is not specified")
            End If

            Dim onlineSpan As TimeSpan = New TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0)
            Dim compareTime As DateTime = ProfileProvider.GetNow.Subtract(onlineSpan)
            Dim last As Date = CDate(u.GetValue(ProfileProvider._lastActivityField))
            Return last > compareTime
        End Function

        Protected Sub UpdateFailureCount(ByVal mgr As OrmDBManager, ByVal u As OrmBase)
            Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim ut As System.Type = u.GetType

            If schema.HasField(ut, GetField("IsLockedOut")) Then

                Dim failCnt As Integer = CInt(u.GetValue(GetField("FailedPasswordAttemtCount")))
                Dim startFail As Date = CDate(u.GetValue(GetField("FailedPasswordAttemtStart")))
                Dim endFail As Date = startFail.AddMinutes(PasswordAttemptWindow)
                Dim nowd As Date = ProfileProvider.GetNow
                failCnt += 1
                Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                    Using u.BeginEdit
                        If failCnt < MaxInvalidPasswordAttempts Then
l1:
                            schema.SetFieldValue(u, GetField("FailedPasswordAttemtCount"), failCnt, oschema)
                            If failCnt = 1 Then
                                schema.SetFieldValue(u, GetField("FailedPasswordAttemtStart"), nowd, oschema)
                            End If
                        Else
                            If nowd > endFail Then
                                failCnt = 1
                                GoTo l1
                            Else
                                Dim ldf As String = GetField("LastLockoutDate")
                                schema.SetFieldValue(u, GetField("IsLockedOut"), True, oschema)
                                If schema.HasField(ut, ldf) Then
                                    schema.SetFieldValue(u, ldf, nowd, oschema)
                                End If
                                schema.SetFieldValue(u, GetField("FailedPasswordAttemtCount"), 0, oschema)
                                schema.SetFieldValue(u, GetField("FailedPasswordAttemtStart"), Nothing, oschema)

                                UserBlocked(u)
                            End If
                        End If
                    End Using

                    st.Commit()
                End Using
            End If
        End Sub

        Private Shared Function ComparePasswords(ByVal psw1 As Byte(), ByVal psw2() As Byte) As Boolean
            Return IsEqualByteArray(psw1, psw2)
        End Function

        Protected Overridable Function MapField(ByVal membershipUserField As String) As String
            Return membershipUserField
        End Function

        Protected Overridable Function HashPassword(ByVal password As String) As Byte()
            Using md5 As New System.Security.Cryptography.MD5CryptoServiceProvider()
                Using ms As New IO.MemoryStream
                    Using sw As New IO.StreamWriter(ms)
                        sw.Write(password)
                    End Using
                    Return md5.ComputeHash(ms.ToArray)
                End Using
            End Using
        End Function

        Protected Overridable Function CanLogin(ByVal mgr As OrmDBManager, ByVal user As OrmBase) As Boolean
            Return True
        End Function

        Public Overridable ReadOnly Property EmptyEmail() As Boolean
            Get
                Return False
            End Get
        End Property
#End Region

        Protected Overridable Sub PasswordChanged(ByVal user As OrmBase)

        End Sub

        Protected Overridable Sub UserCreated(ByVal user As OrmBase)

        End Sub

        Protected Overridable Sub UserBlocked(ByVal user As OrmBase)

        End Sub
    End Class
End Namespace