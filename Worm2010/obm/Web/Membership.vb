Imports System.Web
Imports System.Web.Security
Imports System.Web.Profile
Imports Worm.Entities
Imports System.Configuration
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Query

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
                Return UserMapper.ApplicationName
            End Get
            Set(ByVal value As String)
                UserMapper.ApplicationName = value
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

            'If HttpContext.Current IsNot Nothing Then
            '    AddHandler HttpContext.Current.ApplicationInstance.PostAuthorizeRequest, AddressOf UpdateLastActivity
            'End If

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

            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(username, Nothing)
            Else
                u = FindUserByName(username, Nothing)
            End If
            If u IsNot Nothing Then
                Dim schema As ObjectMappingEngine = u.GetMappingEngine
                Dim oschema As IEntitySchema = schema.GetEntitySchema(u.GetType)
                Using st As New ModificationsTracker(UserMapper.CreateManager)
                    Using u.BeginEdit()
                        ObjectMappingEngine.SetPropertyValue(u, GetField("Password"), HashPassword(newPassword), oschema)
                        Dim lpcf As String = GetField("LastPasswordChangeDate")
                        If Schema.HasProperty(u.GetType, lpcf) Then
                            ObjectMappingEngine.SetPropertyValue(u, lpcf, UserMapper.GetNow, oschema)
                        End If
                    End Using
                    st.AcceptModifications()
                End Using
            End If
            'End Using

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

            'Using mgr As OrmManager = UserMapper.CreateManager
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
                If FindUserByName(username, Nothing) IsNot Nothing Then
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

            Dim u As ISinglePKEntity = Nothing

            Using mt As New ModificationsTracker(UserMapper.CreateManager)

                u = UserMapper.CreateUser(mt, username, Nothing, providerUserKey)
                Dim schema As ObjectMappingEngine = u.GetMappingEngine
                Dim oschema As IEntitySchema = schema.GetEntitySchema(u.GetType)

                ObjectMappingEngine.SetPropertyValue(u, GetField("Email"), email, oschema)

                If PasswordFormat = MembershipPasswordFormat.Hashed Then
                    ObjectMappingEngine.SetPropertyValue(u, GetField("Password"), HashPassword(password), oschema)
                ElseIf PasswordFormat = MembershipPasswordFormat.Clear Then
                    ObjectMappingEngine.SetPropertyValue(u, GetField("Password"), password, oschema)
                Else
                    Throw New NotImplementedException(PasswordFormat.ToString)
                End If

                Dim d As Date = UserMapper.GetNow()

                If Not String.IsNullOrEmpty(UserMapper.LastActivityField) Then
                    ObjectMappingEngine.SetPropertyValue(u, UserMapper.LastActivityField, d, oschema)
                End If

                If Not String.IsNullOrEmpty(UserMapper.IsAnonymousField) Then
                    ObjectMappingEngine.SetPropertyValue(u, UserMapper.IsAnonymousField, False, oschema)
                End If

                Dim llf As String = GetField("LastLoginDate")
                If schema.HasProperty(u.GetType, llf) Then
                    ObjectMappingEngine.SetPropertyValue(u, llf, d, oschema)
                End If

                Dim crf As String = GetField("CreationDate")
                If schema.HasProperty(u.GetType, crf) Then
                    ObjectMappingEngine.SetPropertyValue(u, crf, d, oschema)
                End If

                mt.AcceptModifications()
            End Using

            UserCreated(u)
            status = MembershipCreateStatus.Success
            Return CreateMembershipUser(u.GetMappingEngine, u)
            'End Using

        End Function

        Public Overrides Function DeleteUser(ByVal username As String, ByVal deleteAllRelatedData As Boolean) As Boolean
            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(username, Nothing)
            Else
                u = FindUserByName(username, Nothing)
            End If
            If u IsNot Nothing Then
                Using mt As New ModificationsTracker(UserMapper.CreateManager)
                    UserMapper.DeleteUser(mt, u, deleteAllRelatedData)
                    mt.AcceptModifications()
                End Using
            End If
            'End Using
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

            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(username, Nothing)
            Else
                u = FindUserByName(username, Nothing)
            End If
            If u IsNot Nothing Then
                'Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim oschema As IEntitySchema = u.GetMappingEngine.GetEntitySchema(u.GetType)
                Using st As New ModificationsTracker(UserMapper.CreateManager)
                    Using u.BeginEdit()
                        ObjectMappingEngine.SetPropertyValue(u, GetField("Password"), HashPassword(psw), oschema)
                        Dim lpcf As String = GetField("LastPasswordChangeDate")
                        If Schema.HasProperty(u.GetType, lpcf) Then
                            ObjectMappingEngine.SetPropertyValue(u, lpcf, UserMapper.GetNow, oschema)
                        End If
                    End Using
                    st.AcceptModifications()
                End Using
                PasswordChanged(u)
            End If
            'End Using

            Return psw
        End Function

        Public Overrides Function UnlockUser(ByVal userName As String) As Boolean
            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(userName, Nothing)
            Else
                u = FindUserByName(userName, Nothing)
            End If
            If u IsNot Nothing Then
                'Dim schema As ObjectMappingEngine = mgr.MappingEngine
                Dim lf As String = GetField("IsLockedOut")
                If schema.HasProperty(u.GetType, lf) Then
                    Dim oschema As IEntitySchema = u.GetMappingEngine.GetEntitySchema(u.GetType)
                    Using st As New ModificationsTracker(UserMapper.CreateManager)
                        Using u.BeginEdit()
                            ObjectMappingEngine.SetPropertyValue(u, lf, False, oschema)
                            'Dim llf As String = GetField("LastLockoutDate")
                            'If schema.HasField(u.GetType, llf) Then
                            '    schema.SetFieldValue(u, llf, ProfileProvider.GetNow)
                            'End If
                        End Using
                        st.AcceptModifications()
                    End Using
                    Return True
                End If
            End If
            Return False
            'End Using
        End Function

        Public Overrides Sub UpdateUser(ByVal user As System.Web.Security.MembershipUser)
            If _treatUsernameAsEmail Then
                'Using mgr As OrmManager = UserMapper.CreateManager
                Dim u As ISinglePKEntity = FindUserByEmail(user.Email, Nothing)
                If u IsNot Nothing Then
                    'Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IEntitySchema = u.GetMappingEngine.GetEntitySchema(u.GetType)
                    Using st As New ModificationsTracker(UserMapper.CreateManager)
                        Using u.BeginEdit()
                            ObjectMappingEngine.SetPropertyValue(u, UserMapper.UserNameField, user.Comment, oschema)
                        End Using
                        st.AcceptModifications()
                    End Using
                End If
                'End Using
            End If
        End Sub

        Public Overrides Function ValidateUser(ByVal username As String, ByVal password As String) As Boolean
            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(username, Nothing)
            Else
                u = FindUserByName(username, Nothing)
            End If
            If u Is Nothing Then
                Return False
            End If

            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim lf As String = GetField("IsLockedOut")
            Dim tt As System.Type = u.GetType
            Dim oschema As IEntitySchema = u.GetMappingEngine.GetEntitySchema(tt)
            If Schema.HasProperty(tt, lf) AndAlso CBool(ObjectMappingEngine.GetPropertyValue(u, lf, oschema)) Then
                Return False
            End If

            Dim c As Boolean
            If PasswordFormat = MembershipPasswordFormat.Clear Then
                c = String.Equals(CStr(ObjectMappingEngine.GetPropertyValue(u, GetField("Password"), oschema)), password)
            ElseIf PasswordFormat = MembershipPasswordFormat.Hashed Then
                c = ComparePasswords(CType(ObjectMappingEngine.GetPropertyValue(u, GetField("Password"), oschema), Byte()), HashPassword(password))
            End If

            If Not c Then
                If _throwExceptionInValidate Then
                    UpdateFailureCount(u)
                Else
                    Try
                        UpdateFailureCount(u)
                    Catch ex As Exception

                    End Try
                End If
                Return False
            End If

            Dim ret As Boolean = CanLogin(u)
            If ret Then
                Dim st As New ModificationsTracker(UserMapper.CreateManager)
                'Dim oschema As IObjectSchemaBase = schema.GetObjectSchema(u.GetType)
                Try
                    Using u.BeginEdit()
                        Dim llf As String = GetField("LastLoginDate")
                        If schema.HasProperty(tt, llf) Then
                            ObjectMappingEngine.SetPropertyValue(u, llf, UserMapper.GetNow, oschema)
                        End If
                        If schema.HasProperty(tt, GetField("FailedPasswordAttemtCount")) Then
                            ObjectMappingEngine.SetPropertyValue(u, GetField("FailedPasswordAttemtCount"), 0, oschema)
                        End If
                    End Using
                    st.AcceptModifications()
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
            'End Using
        End Function
#End Region

#Region " Query functions "
        Public Overrides Function FindUsersByEmail(ByVal emailToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, MapField("Email"), New TypeWrap(Of Object)(emailToMatch), FilterOperation.Like))
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim c As PredicateLink = CType(New Ctor(UserMapper.GetUserType).prop(MapField("Email")).[like](emailToMatch), PredicateLink)
            Dim users As IList = UserMapper.FindUsers(c)
            totalRecords = users.Count
            Return CreateUserCollection(users, schema, pageIndex, pageSize)
            'End Using
        End Function

        Public Overrides Function FindUsersByName(ByVal usernameToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            'Return FindUsersByEmail(usernameToMatch, pageIndex, pageSize, totalRecords)
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._userNameField, New TypeWrap(Of Object)(usernameToMatch), FilterOperation.Like))
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim users As IList = UserMapper.FindUsers(New Ctor(UserMapper.GetUserType).prop(UserMapper.UserNameField).[like](usernameToMatch))
            totalRecords = users.Count
            Return CreateUserCollection(users, schema, pageIndex, pageSize)
            'End Using
        End Function

        Public Overrides Function GetAllUsers(ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Security.MembershipUserCollection
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            'Dim f As New OrmFilter(ProfileProvider.GetUserType, "ID", New TypeWrap(Of Object)(-1), FilterOperation.NotEqual)
            'Dim users As IList = UserMapper.FindUsers(mgr, New Ctor(UserMapper.GetUserType).Field(OrmBaseT.PKName).NotEq(-1))
            Dim users As IList = New Query.QueryCmd(UserMapper.CreateManager).SelectEntity(UserMapper.GetUserType).ToList()
            totalRecords = users.Count
            Return CreateUserCollection(users, schema, pageIndex, pageSize)
            'End Using
        End Function

        Public Overrides Function GetNumberOfUsersOnline() As Integer
            If String.IsNullOrEmpty(UserMapper.LastActivityField) Then
                Throw New InvalidOperationException("LastActivity field is not specified")
            End If

            Dim onlineSpan As TimeSpan = New TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0)
            Dim compareTime As DateTime = UserMapper.GetNow.Subtract(onlineSpan)
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._lastActivityField, New TypeWrap(Of Object)(compareTime), FilterOperation.GreaterThan))
            Return UserMapper.FindUsers(New Ctor(UserMapper.GetUserType).prop(UserMapper.LastActivityField).greater_than(compareTime)).Count
            'End Using
        End Function

        Public Overrides Function GetPassword(ByVal username As String, ByVal answer As String) As String
            Throw New NotSupportedException
        End Function

        Public Overloads Overrides Function GetUser(ByVal providerUserKey As Object, ByVal userIsOnline As Boolean) As System.Web.Security.MembershipUser
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, "ID", New TypeWrap(Of Object)(providerUserKey), FilterOperation.Equal))
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim ut As Type = UserMapper.GetUserType
            Dim users As IList = UserMapper.FindUsers(New Ctor(ut).prop(Schema.GetSinglePK(ut)).eq(providerUserKey))
            If users.Count <> 1 Then
                Return Nothing
            End If
            Dim u As ISinglePKEntity = CType(users(0), ISinglePKEntity)
            If userIsOnline Then
                If Not IsUserOnline(u) Then
                    Return Nothing
                End If
            End If
            Return CreateMembershipUser(u.GetMappingEngine, u)
            'End Using
        End Function

        Public Overloads Overrides Function GetUser(ByVal username As String, ByVal userIsOnline As Boolean) As System.Web.Security.MembershipUser
            If Not String.IsNullOrEmpty(username) Then
                'Using mgr As OrmManager = UserMapper.CreateManager
                Dim u As ISinglePKEntity = Nothing
                If _treatUsernameAsEmail Then
                    u = FindUserByEmail(username, userIsOnline)
                Else
                    u = FindUserByName(username, userIsOnline)
                End If

                If u Is Nothing Then
                    Return Nothing
                End If
                Return CreateMembershipUser(u.GetMappingEngine, u)
                'End Using
            End If
            Return Nothing
        End Function

        Public Overrides Function GetUserNameByEmail(ByVal email As String) As String
            'Using mgr As OrmManager = UserMapper.CreateManager
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim u As ISinglePKEntity = FindUserByEmail(email, Nothing)
            If u Is Nothing Then
                Return Nothing
            End If
            Return CStr(u.GetMappingEngine.GetPropertyValue(u, GetField("Email")))
            'End Using
        End Function
#End Region

#Region " Helpers "

        Protected ReadOnly Property Schema() As ObjectMappingEngine
            Get
                Using mgr As OrmManager = UserMapper.CreateManager.CreateManager
                    Return mgr.MappingEngine
                End Using
            End Get
        End Property

        Protected Overridable ReadOnly Property UserMapper() As IUserMapping
            Get
                Dim p As Profile.ProfileProvider = Profile.ProfileManager.Provider
                Return CType(p, IUserMapping)
            End Get
        End Property

        Protected Function CreateMembershipUser(ByVal schema As ObjectMappingEngine, ByVal u As ISinglePKEntity) As MembershipUser
            Dim lf As String = GetField("IsLockedOut")
            Dim islockedout As Boolean = False
            Dim ut As System.Type = u.GetType
            Dim oschema As IEntitySchema = schema.GetEntitySchema(ut)
            If schema.HasProperty(ut, lf) Then
                islockedout = CBool(ObjectMappingEngine.GetPropertyValue(u, lf, oschema))
            End If

            Dim crf As String = GetField("CreationDate")
            Dim created As Date = Date.MinValue
            If schema.HasProperty(ut, crf) Then
                created = CDate(ObjectMappingEngine.GetPropertyValue(u, crf, oschema))
            End If

            Dim llf As String = GetField("LastLoginDate")
            Dim lastlogin As Date = Date.MinValue
            If schema.HasProperty(ut, llf) Then
                lastlogin = CDate(ObjectMappingEngine.GetPropertyValue(u, llf, oschema))
            End If

            Dim lpcf As String = GetField("LastPasswordChangedDate")
            Dim lastpsw As Date = Date.MinValue
            If schema.HasProperty(ut, lpcf) Then
                lastpsw = CDate(ObjectMappingEngine.GetPropertyValue(u, lpcf, oschema))
            End If

            Dim lld As String = GetField("LastLockoutDate")
            Dim lastlockout As Date = Date.MinValue
            If schema.HasProperty(ut, lld) Then
                lastlockout = CDate(ObjectMappingEngine.GetPropertyValue(u, lld, oschema))
            End If

            Dim lastact As Date = Date.MinValue
            If Not String.IsNullOrEmpty(UserMapper.LastActivityField) Then
                lastact = CDate(ObjectMappingEngine.GetPropertyValue(u, UserMapper.LastActivityField, oschema))
            End If

            Dim uname As String = Nothing
            If Not String.IsNullOrEmpty(UserMapper.UserNameField) Then
                uname = CStr(ObjectMappingEngine.GetPropertyValue(u, UserMapper.UserNameField, oschema))
            End If

            Dim username As String = Nothing
            If _treatUsernameAsEmail Then
                username = CStr(ObjectMappingEngine.GetPropertyValue(u, GetField("Email"), oschema))
            Else
                username = uname
                uname = Nothing
            End If

            Dim mu As New MembershipUser(Me.Name, _
                username, _
                u.Identifier, _
                CStr(ObjectMappingEngine.GetPropertyValue(u, GetField("Email"), oschema)), _
                Nothing, uname, _
                True, islockedout, created, lastlogin, _
                lastact, _
                lastpsw, lastlockout)
            Return mu
        End Function

        Protected Function CreateUserCollection(ByVal users As IList, ByVal schema As ObjectMappingEngine) As MembershipUserCollection
            Dim uc As New MembershipUserCollection
            For Each u As ISinglePKEntity In users
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
                    Dim u As ISinglePKEntity = CType(users(i), ISinglePKEntity)
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
        '    'using mgr As OrmManager = ProfileProvider.CreateManager
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

        Protected Function FindUserByEmail(ByVal email As String, ByVal userIsOnline As Nullable(Of Boolean)) As ISinglePKEntity
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, GetField("Email"), New TypeWrap(Of Object)(email), FilterOperation.Equal))
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim users As IList = UserMapper.FindUsers(New Ctor(UserMapper.GetUserType).prop(GetField("Email")).eq(email))
            If users.Count <> 1 Then
                Throw New InvalidOperationException(String.Format("The number of users with {0} email is {1}", email, users.Count))
            End If
            Dim u As ISinglePKEntity = CType(users(0), ISinglePKEntity)
            If userIsOnline.HasValue AndAlso Not String.IsNullOrEmpty(UserMapper.LastActivityField) _
                AndAlso IsUserOnline(u) <> userIsOnline.Value Then
                Return Nothing
            End If
            Return u
        End Function

        Protected Friend Function FindUserByName(ByVal username As String, ByVal userIsOnline As Nullable(Of Boolean)) As ISinglePKEntity
            'Dim c As New OrmCondition.OrmConditionConstructor
            'c.AddFilter(New OrmFilter(ProfileProvider.GetUserType, ProfileProvider._userNameField, New TypeWrap(Of Object)(username), FilterOperation.Equal))
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            If String.IsNullOrEmpty(username) Then
                Return Nothing
            End If

            Dim users As IList = UserMapper.FindUsers(New Ctor(UserMapper.GetUserType).prop(UserMapper.UserNameField).eq(username))
            If users.Count <> 1 Then
                Throw New InvalidOperationException(String.Format("The number of users with {0} username is {1}", username, users.Count))
            End If
            Dim u As ISinglePKEntity = CType(users(0), ISinglePKEntity)
            If userIsOnline.HasValue AndAlso Not String.IsNullOrEmpty(UserMapper.LastActivityField) _
                AndAlso IsUserOnline(u) <> userIsOnline.Value Then
                Return Nothing
            End If
            Return u
        End Function

        Protected Sub UpdateLastActivity(ByVal sender As Object, ByVal e As EventArgs)
            UpdateLastActivity()
        End Sub

        Public Overridable Sub UpdateLastActivity()
            'Using mgr As OrmManager = UserMapper.CreateManager
            Dim u As ISinglePKEntity = Nothing
            If _treatUsernameAsEmail Then
                u = FindUserByEmail(HttpContext.Current.User.Identity.Name, Nothing)
            Else
                u = FindUserByName(HttpContext.Current.User.Identity.Name, Nothing)
            End If

            If u IsNot Nothing Then
                Dim schema As ObjectMappingEngine = u.GetMappingEngine
                Dim laf As String = UserMapper.LastActivityField
                If Not String.IsNullOrEmpty(laf) Then
                    Dim dt As Date = CDate(schema.GetPropertyValue(u, laf))
                    Dim n As Date = UserMapper.GetNow
                    If n.Subtract(dt).TotalSeconds > 10 Then
                        Dim oschema As IEntitySchema = schema.GetEntitySchema(u.GetType)
                        Using st As New ModificationsTracker(UserMapper.CreateManager)
                            Using u.BeginEdit
                                ObjectMappingEngine.SetPropertyValue(u, laf, n, oschema, Nothing)
                            End Using
                            st.AcceptModifications()
                            Dim onlineSpan As TimeSpan = New TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0)
                            If dt <= n.Subtract(onlineSpan) Then
                                OnUserComeback(u)
                            End If
                        End Using
                    End If
                End If
            End If
            'End Using
        End Sub

        Protected Function IsUserOnline(ByVal u As ISinglePKEntity) As Boolean
            If String.IsNullOrEmpty(UserMapper.LastActivityField) Then
                Throw New InvalidOperationException("LastActivity field is not specified")
            End If

            Dim onlineSpan As TimeSpan = New TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0)
            Dim compareTime As DateTime = UserMapper.GetNow.Subtract(onlineSpan)
            Dim last As Date = CDate(u.GetMappingEngine.GetPropertyValue(u, UserMapper.LastActivityField))
            Return last > compareTime
        End Function

        Protected Sub UpdateFailureCount(ByVal u As ISinglePKEntity)
            'Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim ut As System.Type = u.GetType

            If Schema.HasProperty(ut, GetField("IsLockedOut")) Then
                Dim oschema As IEntitySchema = Schema.GetEntitySchema(u.GetType)
                Dim failCnt As Integer = CInt(ObjectMappingEngine.GetPropertyValue(u, GetField("FailedPasswordAttemtCount"), oschema))
                Dim startFail As Date = CDate(ObjectMappingEngine.GetPropertyValue(u, GetField("FailedPasswordAttemtStart"), oschema))
                Dim endFail As Date = startFail.AddMinutes(PasswordAttemptWindow)
                Dim nowd As Date = UserMapper.GetNow
                failCnt += 1
                Using st As New ModificationsTracker(UserMapper.CreateManager)
                    Using u.BeginEdit
                        If failCnt < MaxInvalidPasswordAttempts Then
l1:
                            ObjectMappingEngine.SetPropertyValue(u, GetField("FailedPasswordAttemtCount"), failCnt, oschema, Nothing)
                            If failCnt = 1 Then
                                ObjectMappingEngine.SetPropertyValue(u, GetField("FailedPasswordAttemtStart"), nowd, oschema, Nothing)
                            End If
                        Else
                            If nowd > endFail Then
                                failCnt = 1
                                GoTo l1
                            Else
                                Dim ldf As String = GetField("LastLockoutDate")
                                ObjectMappingEngine.SetPropertyValue(u, GetField("IsLockedOut"), True, oschema)
                                If Schema.HasProperty(ut, ldf) Then
                                    ObjectMappingEngine.SetPropertyValue(u, ldf, nowd, oschema)
                                End If
                                ObjectMappingEngine.SetPropertyValue(u, GetField("FailedPasswordAttemtCount"), 0, oschema)
                                ObjectMappingEngine.SetPropertyValue(u, GetField("FailedPasswordAttemtStart"), Nothing, oschema)

                                UserBlocked(u)
                            End If
                        End If
                    End Using

                    st.AcceptModifications()
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

        Protected Overridable Function CanLogin(ByVal user As ISinglePKEntity) As Boolean
            Return True
        End Function

        Public Overridable ReadOnly Property EmptyEmail() As Boolean
            Get
                Return False
            End Get
        End Property
#End Region

        Protected Overridable Sub PasswordChanged(ByVal user As ISinglePKEntity)

        End Sub

        Protected Overridable Sub UserCreated(ByVal user As ISinglePKEntity)

        End Sub

        Protected Overridable Sub UserBlocked(ByVal user As ISinglePKEntity)

        End Sub

        Protected Overridable Sub OnUserComeback(ByVal user As ISinglePKEntity)

        End Sub

        Public Sub SubscribeUpdateLastActivity()
            If HttpContext.Current IsNot Nothing Then
                AddHandler HttpContext.Current.ApplicationInstance.PostAuthorizeRequest, AddressOf UpdateLastActivity
            End If
        End Sub
    End Class
End Namespace