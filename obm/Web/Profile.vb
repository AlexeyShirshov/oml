Imports System.Web
Imports System.Web.Profile
Imports Worm.Entities
Imports System.Configuration
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Query

Namespace Web
    Public Interface IUserMapping
        Function CreateManager() As OrmManager
        Property ApplicationName() As String
        ReadOnly Property LastActivityField() As String
        ReadOnly Property IsAnonymousField() As String
        ReadOnly Property UserNameField() As String
        ReadOnly Property GetNow() As Date
        Function CreateUser(ByVal mgr As OrmDBManager, ByVal name As String, ByVal AnonymousId As String, ByVal context As Object) As IKeyEntity
        Sub DeleteUser(ByVal mgr As OrmDBManager, ByVal user As IKeyEntity, ByVal cascade As Boolean)
        Function GetUserType() As Type
        Function FindUsers(ByVal mgr As OrmManager, ByVal criteria As Worm.Criteria.PredicateLink) As IList
    End Interface

    Public MustInherit Class UserMapping
        Implements IUserMapping

        Protected _appName As String
        Protected _lastActivityField As String
        Protected _lastUpdateField As String
        Protected _isAnonymousField As String
        Protected _userNameField As String

        Public Property ApplicationName() As String Implements IUserMapping.ApplicationName
            Get
                Return _appName
            End Get
            Set(ByVal value As String)
                _appName = value
            End Set
        End Property

        Public Sub DeleteUser(ByVal mgr As Database.OrmDBManager, ByVal user As Entities.IKeyEntity, ByVal cascade As Boolean) Implements IUserMapping.DeleteUser
            If cascade Then
                Throw New NotSupportedException("Cascade delete is not supported")
            End If
            Using mt As New ModificationsTracker(mgr)
                CType(user, ICachedEntity).Delete()
                mt.AcceptModifications()
            End Using
        End Sub

        Public Function FindUsers(ByVal mgr As OrmManager, ByVal criteria As Criteria.PredicateLink) As System.Collections.IList Implements IUserMapping.FindUsers
            Dim cmd As New Query.QueryCmd()
            cmd.Where(criteria).Select(GetUserType)
            Return cmd.ToList(mgr)
        End Function

        Public ReadOnly Property GetNow() As Date Implements IUserMapping.GetNow
            Get
                Return Now
            End Get
        End Property

        Public ReadOnly Property IsAnonymousField() As String Implements IUserMapping.IsAnonymousField
            Get
                Return _isAnonymousField
            End Get
        End Property

        Public ReadOnly Property LastActivityField() As String Implements IUserMapping.LastActivityField
            Get
                Return _lastActivityField
            End Get
        End Property

        Public ReadOnly Property UserNameField() As String Implements IUserMapping.UserNameField
            Get
                Return _userNameField
            End Get
        End Property

        Public MustOverride Function GetUserType() As System.Type Implements IUserMapping.GetUserType
        Public MustOverride Function CreateManager() As OrmManager Implements IUserMapping.CreateManager
        Public MustOverride Function CreateUser(ByVal mgr As Database.OrmDBManager, ByVal name As String, ByVal AnonymousId As String, ByVal context As Object) As Entities.IKeyEntity Implements IUserMapping.CreateUser

    End Class

    Public MustInherit Class ProfileBase
        Inherits ProfileProvider
        Implements IUserMapping

        Private _appName As String
        Private _lastActivityField As String
        Private _lastUpdateField As String
        Private _isAnonymousField As String
        Private _userNameField As String
        Private _getm As ICreateManager
        Private _autoCreateProfileInDB As Boolean
        Private _updateLastActivity As Boolean
        Private _profileCookieTimeout As Integer = 7

        Protected Overridable ReadOnly Property GetNow() As Date Implements IUserMapping.GetNow
            Get
                Return Now
            End Get
        End Property

        Protected ReadOnly Property LastActivityField() As String Implements IUserMapping.LastActivityField
            Get
                Return _lastActivityField
            End Get
        End Property

        Protected ReadOnly Property IsAnonymousField() As String Implements IUserMapping.IsAnonymousField
            Get
                Return _isAnonymousField
            End Get
        End Property

        Protected ReadOnly Property UserNameField() As String Implements IUserMapping.UserNameField
            Get
                Return _userNameField
            End Get
        End Property

        Protected Function _getMgr() As OrmManager Implements IUserMapping.CreateManager
            Return _getm.CreateManager()
        End Function

        Public Overrides Property ApplicationName() As String Implements IUserMapping.ApplicationName
            Get
                Return _appName
            End Get
            Set(ByVal value As String)
                _appName = value
            End Set
        End Property

        Public Overrides Sub Initialize(ByVal name As String, ByVal config As System.Collections.Specialized.NameValueCollection)

            If Not String.IsNullOrEmpty(config("LastActivityField")) Then
                _lastActivityField = config("LastActivityField")
            End If

            If Not String.IsNullOrEmpty(config("LastUpdateField")) Then
                _lastUpdateField = config("LastUpdateField")
            End If

            If Not String.IsNullOrEmpty(config("IsAnonymousField")) Then
                _isAnonymousField = config("IsAnonymousField")
            End If

            If Not String.IsNullOrEmpty(config("UsernameField")) Then
                _userNameField = config("UsernameField")
            End If

            If Not String.IsNullOrEmpty(config("AutoCreateProfileInDB")) Then
                _autoCreateProfileInDB = CBool(config("AutoCreateProfileInDB"))
            End If

            If Not String.IsNullOrEmpty(config("UpdateLastActivity")) Then
                _updateLastActivity = CBool(config("UpdateLastActivity"))
            End If

            If Not String.IsNullOrEmpty(config("ProfileCookieTimeout")) Then
                _profileCookieTimeout = CInt(config("ProfileCookieTimeout"))
            End If

            If String.IsNullOrEmpty(config("GetDBMgr")) Then
                Throw New ArgumentException("Implementation of ICreateManager is requred under GetDBMgr key")
            Else
                'Dim t As Type = Type.GetType(config("GetDBMgr"))
                'If t Is Nothing Then
                '    Throw New ArgumentException("Cannot create type from " & config("GetDBMgr"))
                'End If

                'Dim gm As IGetDBMgr = CType(t.InvokeMember(Nothing, _
                '    Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IGetDBMgr)
                _getm = CreateDBMgr(config("GetDBMgr"))
                If _getm Is Nothing Then
                    Throw New ArgumentException("Cannot create instance of ICreateManager from " & config("GetDBMgr"))
                End If
            End If
            MyBase.Initialize(name, config)
        End Sub

        Public Overrides Function DeleteInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date) As Integer
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Dim cl As Worm.Criteria.PredicateLink = New Ctor(GetUserType).prop(_lastActivityField).less_than_eq(userInactiveSinceDate)
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(True))
                    Case ProfileAuthenticationOption.Authenticated
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(False))
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                Dim col As ICollection = FindUsers(mgr, cl)
                For Each u As IKeyEntity In col
                    DeleteProfile(CType(mgr, OrmDBManager), u)
                Next
                Return col.Count
            End Using
        End Function

        Public Overloads Overrides Function DeleteProfiles(ByVal usernames() As String) As Integer
            Using mgr As OrmManager = _getMgr()
                Dim cnt As Integer = 0
                For Each u As String In usernames
                    Dim user As IKeyEntity = GetUserByName(mgr, u, True, _autoCreateProfileInDB)
                    If user IsNot Nothing Then
                        DeleteProfile(CType(mgr, OrmDBManager), user)
                    Else
                        System.Web.Security.AnonymousIdentificationModule.ClearAnonymousIdentifier()
                        RemoveAnonymousStoreCookie()
                    End If
                    cnt += 1
                Next
                Return cnt
            End Using
        End Function

        Public Overloads Overrides Function DeleteProfiles(ByVal profiles As System.Web.Profile.ProfileInfoCollection) As Integer
            Using mgr As OrmManager = _getMgr()
                Dim cnt As Integer = 0
                For Each p As ProfileInfo In profiles
                    Dim user As IKeyEntity = GetUserByName(mgr, p.UserName, Not p.IsAnonymous, _autoCreateProfileInDB)
                    If user IsNot Nothing Then
                        DeleteProfile(CType(mgr, OrmDBManager), user)
                    Else
                        System.Web.Security.AnonymousIdentificationModule.ClearAnonymousIdentifier()
                        RemoveAnonymousStoreCookie()
                    End If
                    cnt += 1
                Next
                Return cnt
            End Using
        End Function

        Protected Function CreateProfileCollection(ByVal pageIndex As Integer, ByVal pageSize As Integer, ByVal mgr As OrmManager, ByVal col As IList) As ProfileInfoCollection
            Dim profiles As New ProfileInfoCollection
            Dim schema As ObjectMappingEngine = mgr.MappingEngine
            Dim start As Integer = pageIndex * pageSize
            If start < col.Count Then
                Dim [end] As Integer = Math.Min((pageIndex - 1) * pageSize, col.Count)
                Dim oschema As IEntitySchema = schema.GetObjectSchema(GetUserType)
                For i As Integer = start To [end] - 1
                    Dim u As KeyEntity = CType(col(i), KeyEntity)

                    Dim upd As Date
                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
                        upd = CDate(schema.GetPropertyValue(u, _lastUpdateField, oschema))
                    End If

                    Dim p As New ProfileInfo( _
                        CStr(schema.GetPropertyValue(u, _userNameField, oschema)), _
                        CBool(schema.GetPropertyValue(u, _isAnonymousField, oschema)), _
                        CDate(schema.GetPropertyValue(u, _lastActivityField, oschema)), upd, 0)

                    profiles.Add(p)
                Next
            End If
            Return profiles
        End Function

        Public Overrides Function FindInactiveProfilesByUserName(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal usernameToMatch As String, ByVal userInactiveSinceDate As Date, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) OrElse String.IsNullOrEmpty(_userNameField) Then
                Throw New InvalidOperationException("Username, LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Dim cl As Worm.Criteria.PredicateLink = New Ctor(GetUserType).prop(_lastActivityField).less_than_eq(userInactiveSinceDate)
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(True))
                    Case ProfileAuthenticationOption.Authenticated
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(False))
                End Select
                'c.AddFilter(New OrmFilter(GetUserType, _userNameField, usernameToMatch & "%", FilterOperation.Like))
                cl.[and](New Ctor(GetUserType).prop(_userNameField).[like](usernameToMatch & "%"))
                Dim col As IList = FindUsers(mgr, cl)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function FindProfilesByUserName(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal usernameToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_userNameField) Then
                Throw New InvalidOperationException("UserName or IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                Dim cl As Worm.Criteria.PredicateLink = New Ctor(GetUserType).prop(_userNameField).[like](usernameToMatch & "%")
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(True))
                    Case ProfileAuthenticationOption.Authenticated
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(False))
                End Select
                Dim col As IList = FindUsers(mgr, cl)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetAllInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Dim cl As Worm.Criteria.PredicateLink = New Ctor(GetUserType).prop(_lastActivityField).less_than_eq(userInactiveSinceDate)
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(True))
                    Case ProfileAuthenticationOption.Authenticated
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(False))
                End Select
                Dim col As IList = FindUsers(mgr, cl)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetAllProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) Then
                Throw New InvalidOperationException("IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                Dim cl As Worm.Criteria.PredicateLink = Nothing
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl = New Ctor(GetUserType).prop(_isAnonymousField).eq(True)
                    Case ProfileAuthenticationOption.Authenticated
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                        cl = New Ctor(GetUserType).prop(_isAnonymousField).eq(False)
                End Select
                Dim col As IList = FindUsers(mgr, cl)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetNumberOfInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date) As Integer
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmManager = _getMgr()
                'Dim c As New OrmCondition.OrmConditionConstructor
                'c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Dim cl As Worm.Criteria.PredicateLink = New Ctor(GetUserType).prop(_lastActivityField).less_than_eq(userInactiveSinceDate)
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(True))
                    Case ProfileAuthenticationOption.Authenticated
                        'c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                        cl.[and](New Ctor(GetUserType).prop(_isAnonymousField).eq(False))
                End Select
                Dim col As IList = FindUsers(mgr, cl)
                Return col.Count
            End Using
        End Function

        'Private Function GetPropertyValuesOld(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyCollection) As System.Configuration.SettingsPropertyValueCollection
        '    Using mgr As OrmManager = _getMgr
        '        Dim col As New SettingsPropertyValueCollection

        '        Dim user As OrmBase = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _autoCreateProfileInDB)
        '        If user Is Nothing Then
        '            If _autoCreateProfileInDB Then
        '                Throw New ArgumentException("Cannot find user " & GetUserName(context))
        '            End If
        '            Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
        '            For Each p As SettingsProperty In collection
        '                Dim newp As New SettingsPropertyValue(p)
        '                If cok IsNot Nothing Then
        '                    newp.PropertyValue = cok(p.Name)
        '                ElseIf p.DefaultValue IsNot Nothing Then
        '                    newp.PropertyValue = p.DefaultValue
        '                End If
        '                col.Add(newp)
        '            Next
        '            'If Not String.IsNullOrEmpty(_lastActivityField) AndAlso cok IsNot Nothing Then
        '            '    cok(_lastActivityField) = GetNow().ToString
        '            'End If
        '            'cok.Expires = Date.Now.AddDays(7)
        '            'HttpContext.Current.Response.Cookies.Add(cok)
        '        Else
        '            Dim schema As OrmSchemaBase = mgr.ObjectSchema
        '            For Each p As SettingsProperty In collection
        '                Dim newp As New SettingsPropertyValue(p)
        '                'If p.Attributes.Contains("CustomProviderData") Then
        '                '    If p.Attributes.Item("CustomProviderData") IsNot Nothing Then
        '                '        Dim s As String = CStr(p.Attributes.Item("CustomProviderData"))
        '                '        If s.Contains("storeInCookie") Then
        '                '            'System.Web.Security.FormsAuthentication.
        '                '        End If
        '                '    End If
        '                'End If
        '                newp.PropertyValue = schema.GetFieldValue(user, p.Name)
        '                col.Add(newp)
        '            Next

        '            If Not String.IsNullOrEmpty(_lastActivityField) Then
        '                schema.SetFieldValue(user, _lastActivityField, GetNow)
        '            End If
        '        End If
        '        Return col
        '    End Using
        'End Function

        Public Overrides Function GetPropertyValues(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyCollection) As System.Configuration.SettingsPropertyValueCollection
            Dim cok As HttpCookie = Nothing, cookieChecked As Boolean = False
            Dim user As IKeyEntity = Nothing, userChecked As Boolean = False
            Dim col As New SettingsPropertyValueCollection
            Dim oschema As IEntitySchema = Nothing
            If user IsNot Nothing Then
                Using mgr As OrmManager = _getMgr()
                    oschema = mgr.MappingEngine.GetObjectSchema(user.GetType)
                End Using
            End If

            For Each p As SettingsProperty In collection
                Dim incok As Boolean = False
                If p.Attributes.Contains("CustomProviderData") Then
                    Dim s As String = CStr(p.Attributes.Item("CustomProviderData"))
                    If Not String.IsNullOrEmpty(s) AndAlso s.IndexOf("storeInCookie", StringComparison.InvariantCultureIgnoreCase) >= 0 Then
                        incok = True
                    End If
                End If
                If Not incok Then
                    If Not userChecked Then
                        Using mgr As OrmManager = _getMgr()
                            user = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _autoCreateProfileInDB)
                        End Using
                        If user Is Nothing AndAlso _autoCreateProfileInDB Then
                            Throw New ArgumentException("Cannot find user " & GetUserName(context))
                        End If
                        userChecked = True
                    End If
                    incok = user Is Nothing
                End If
                Dim newp As New SettingsPropertyValue(p)
                If incok Then
                    If Not cookieChecked Then
                        cok = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
                        cookieChecked = True
                    End If
                    If cok IsNot Nothing Then
                        newp.PropertyValue = cok(p.Name)
                    ElseIf p.DefaultValue IsNot Nothing Then
                        newp.PropertyValue = p.DefaultValue
                    End If
                Else
                    Using mgr As OrmManager = _getMgr()
                        newp.PropertyValue = mgr.MappingEngine.GetPropertyValue(user, p.Name, oschema)
                    End Using
                End If
                col.Add(newp)
            Next

            If _updateLastActivity AndAlso Not String.IsNullOrEmpty(_lastActivityField) Then
                If user IsNot Nothing Then
                    Using mgr As OrmManager = _getMgr()
                        'Dim oschema As IObjectSchemaBase = mgr.MappingEngine.GetObjectSchema()
                        mgr.MappingEngine.SetPropertyValue(user, _lastActivityField, GetNow, oschema)
                    End Using
                End If
                If cok IsNot Nothing Then
                    cok.Values(_lastActivityField) = GetNow().ToString
                    cok.Expires = Date.Now.AddDays(_profileCookieTimeout)
                    HttpContext.Current.Response.Cookies.Add(cok)
                End If
            End If

            Return col
        End Function

        Public Overrides Sub SetPropertyValues(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyValueCollection)
            If Not context.ContainsKey("remove_profile") Then
                Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
                Dim user As IKeyEntity = Nothing, userChecked As Boolean
                Dim saveCookie As Boolean = False
                For Each p As SettingsPropertyValue In collection
                    If Not p.Property.IsReadOnly Then
                        Dim incok As Boolean = False
                        If p.Property.Attributes.Contains("CustomProviderData") Then
                            Dim s As String = CStr(p.Property.Attributes.Item("CustomProviderData"))
                            If Not String.IsNullOrEmpty(s) AndAlso s.IndexOf("storeInCookie", StringComparison.InvariantCultureIgnoreCase) >= 0 Then
                                incok = True
                            End If
                        End If
                        If Not incok Then
                            If Not userChecked Then
                                Using mgr As OrmManager = _getMgr()
                                    user = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _autoCreateProfileInDB)
                                End Using
                                If user Is Nothing AndAlso _autoCreateProfileInDB Then
                                    Throw New ArgumentException("Cannot find user " & GetUserName(context))
                                End If
                                userChecked = True
                            End If
                            incok = user Is Nothing
                        End If
                        If incok Then
                            If cok Is Nothing Then
                                cok = New HttpCookie(GetAnonymousCookieName)
                            End If
                            If p.PropertyValue IsNot Nothing AndAlso Not p.PropertyValue.Equals(p.Property.DefaultValue) Then
                                cok.Values(p.Name) = p.PropertyValue.ToString
                                saveCookie = True
                            End If
                        Else
                            Using mgr As OrmManager = _getMgr()
                                Using user.BeginEdit
                                    Dim oschema As IEntitySchema = mgr.MappingEngine.GetObjectSchema(user.GetType)
                                    mgr.MappingEngine.SetPropertyValue(user, p.Name, p.PropertyValue, oschema)
                                End Using
                            End Using
                        End If
                    End If
                Next
                Dim d As Date = GetNow()
                If saveCookie Then
                    If Not String.IsNullOrEmpty(_lastActivityField) AndAlso _updateLastActivity Then
                        cok.Values(_lastActivityField) = d.ToString
                    End If
                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
                        cok.Values(_lastUpdateField) = d.ToString
                    End If

                    cok.Expires = Date.Now.AddDays(_profileCookieTimeout)
                    HttpContext.Current.Response.Cookies.Add(cok)
                End If
                If user IsNot Nothing Then
                    Using mgr As OrmManager = _getMgr()
                        Dim oschema As IEntitySchema = mgr.MappingEngine.GetObjectSchema(user.GetType)
                        Using st As New ModificationsTracker(CType(mgr, OrmReadOnlyDBManager))
                            Using user.BeginEdit
                                If Not String.IsNullOrEmpty(_lastActivityField) Then
                                    mgr.MappingEngine.SetPropertyValue(user, _lastActivityField, d, oschema)
                                End If
                                If Not String.IsNullOrEmpty(_lastUpdateField) Then
                                    mgr.MappingEngine.SetPropertyValue(user, _lastUpdateField, d, oschema)
                                End If
                            End Using
                            st.Add(user)
                            st.AcceptModifications()
                        End Using
                    End Using
                End If
            End If
        End Sub

        'Private Sub SetPropertyValuesOld(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyValueCollection)
        '    Using mgr As OrmManager = _getMgr
        '        Dim user As OrmBase = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _autoCreateProfileInDB)

        '        If user Is Nothing Then
        '            If _autoCreateProfileInDB Then
        '                Throw New ArgumentException("Cannot find user " & GetUserName(context))
        '            End If
        '            'Dim cok As HttpCookie = HttpContext.Current.Response.Cookies(GetAnonymousCookieName)
        '            'If cok Is Nothing OrElse cok.Expires > Date.Now Then
        '            Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
        '            If Not context.ContainsKey("remove_profile") Then
        '                If cok Is Nothing Then
        '                    cok = New HttpCookie(GetAnonymousCookieName)
        '                End If
        '                Dim created As Boolean = False
        '                For Each p As SettingsPropertyValue In collection
        '                    If Not p.Property.IsReadOnly Then
        '                        If p.PropertyValue IsNot Nothing AndAlso Not p.PropertyValue.Equals(p.Property.DefaultValue) Then
        '                            cok.Values(p.Name) = p.PropertyValue.ToString
        '                            created = True
        '                        End If
        '                    End If
        '                Next
        '                If created Then
        '                    Dim d As Date = GetNow()
        '                    If Not String.IsNullOrEmpty(_lastActivityField) Then
        '                        cok.Values(_lastActivityField) = d.ToString
        '                    End If
        '                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
        '                        cok.Values(_lastUpdateField) = d.ToString
        '                    End If

        '                    cok.Expires = Date.Now.AddDays(7)
        '                    HttpContext.Current.Response.Cookies.Add(cok)
        '                End If
        '            End If
        '        Else
        '            CopyValuesToUser(collection, mgr, user)
        '        End If
        '    End Using
        'End Sub

        'Protected Sub CopyValuesToUser(ByVal collection As System.Configuration.SettingsPropertyValueCollection, ByVal mgr As OrmDBManager, ByVal user As OrmBase)
        '    Dim schema As OrmSchemaBase = mgr.ObjectSchema
        '    For Each p As SettingsPropertyValue In collection
        '        If Not p.Property.IsReadOnly Then
        '            schema.SetFieldValue(user, p.Name, p.PropertyValue)
        '        End If
        '        'Debug.Write(p.Name & ": ")
        '        'If p.PropertyValue IsNot Nothing Then
        '        '    Debug.WriteLine(p.PropertyValue.ToString)
        '        'Else
        '        '    Debug.WriteLine(String.Empty)
        '        'End If
        '    Next
        '    Dim d As Date = GetNow()
        '    If Not String.IsNullOrEmpty(_lastActivityField) Then
        '        schema.SetFieldValue(user, _lastActivityField, d)
        '    End If
        '    If Not String.IsNullOrEmpty(_lastUpdateField) Then
        '        schema.SetFieldValue(user, _lastUpdateField, d)
        '    End If
        '    user.Save(True)
        'End Sub

        Public Sub MigrateAnonymous(ByVal AnonymousId As String)
            Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
            Using mgr As OrmManager = _getMgr()
                Dim user As IKeyEntity = Nothing
                Try
                    user = GetUserByName(mgr, HttpContext.Current.Profile.UserName, True, False) 'CreateUser(mgr, HttpContext.Current.Profile.UserName)
                Catch ex As ArgumentException When ex.Message.Contains("not found")
                    user = CreateUser(CType(mgr, OrmDBManager), HttpContext.Current.Profile.UserName, AnonymousId, Nothing)
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IEntitySchema = mgr.MappingEngine.GetObjectSchema(user.GetType)
                    For Each p As SettingsProperty In System.Web.Profile.ProfileBase.Properties
                        If Not p.IsReadOnly Then
                            If cok IsNot Nothing Then
                                schema.SetPropertyValue(user, p.Name, cok(p.Name), oschema)
                            ElseIf p.DefaultValue IsNot Nothing Then
                                schema.SetPropertyValue(user, p.Name, p.DefaultValue, oschema)
                            End If
                        End If
                    Next
                    Dim d As Date = GetNow()
                    If Not String.IsNullOrEmpty(_lastActivityField) Then
                        schema.SetPropertyValue(user, _lastActivityField, d, oschema)
                    End If
                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
                        schema.SetPropertyValue(user, _lastUpdateField, d, oschema)
                    End If
                    user.SaveChanges(True)
                End Try
            End Using
            System.Web.Security.AnonymousIdentificationModule.ClearAnonymousIdentifier()
            RemoveAnonymousStoreCookie()
        End Sub

        Public Sub RemoveAnonymousStoreCookie()
            Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
            If cok IsNot Nothing Then
                HttpContext.Current.Profile.Context.Add("remove_profile", "true")
                cok = New HttpCookie(GetAnonymousCookieName)
                cok.Expires = Date.Now.AddDays(-2)
                HttpContext.Current.Response.Cookies.Remove(GetAnonymousCookieName)
                HttpContext.Current.Response.Cookies.Add(cok)
            End If
        End Sub

        Protected Function GetUserName(ByVal ctx As SettingsContext) As String
            Return CStr(ctx("UserName"))
        End Function

        Protected Function IsAuthenticated(ByVal ctx As SettingsContext) As Boolean
            Return CBool(ctx("IsAuthenticated"))
        End Function

        Protected Overridable Function CreateDBMgr(ByVal type As String) As ICreateManager
            Return CType(Reflection.Assembly.GetExecutingAssembly.CreateInstance(type), ICreateManager)
        End Function

        Protected Overridable Sub DeleteProfile(ByVal mgr As OrmDBManager, ByVal u As IKeyEntity)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Function GetAnonymousCookieName() As String
            Throw New NotImplementedException
        End Function

        Protected Overridable Function FindUsers(ByVal mgr As OrmManager, ByVal criteria As Worm.Criteria.PredicateLink) As IList Implements IUserMapping.FindUsers
            Dim cmd As New Query.QueryCmd()
            cmd.Where(criteria).Select(GetUserType)
            Return (cmd.ToList(mgr))
        End Function

        Protected Overridable Sub DeleteUser(ByVal mgr As OrmDBManager, ByVal user As IKeyEntity, ByVal cascade As Boolean) Implements IUserMapping.DeleteUser
            If cascade Then
                Throw New NotSupportedException("Cascade delete is not supported")
            End If
            Using mt As New ModificationsTracker(mgr)
                CType(user, ICachedEntity).Delete()
                mt.AcceptModifications()
            End Using
        End Sub

        Protected MustOverride Function GetUserType() As Type Implements IUserMapping.GetUserType
        Protected MustOverride Function CreateUser(ByVal mgr As OrmDBManager, ByVal name As String, ByVal AnonymousId As String, ByVal context As Object) As IKeyEntity Implements IUserMapping.CreateUser
        Protected MustOverride Function GetUserByName(ByVal mgr As OrmManager, ByVal name As String, ByVal isAuthenticated As Boolean, ByVal createIfNotExist As Boolean) As IKeyEntity

    End Class
End Namespace
