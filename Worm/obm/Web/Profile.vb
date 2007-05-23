Imports System.Web
Imports System.Web.Profile
Imports Worm.Orm
Imports System.Configuration

Namespace Web
    Public MustInherit Class ProfileBase
        Inherits ProfileProvider

        Friend Delegate Function GetDBManager() As OrmDBManager

        Private _appName As String
        Friend _getMgr As GetDBManager
        Friend _lastActivityField As String
        Private _lastUpdateField As String
        Friend _isAnonymousField As String
        Protected Friend _userNameField As String
        Private _getm As IGetDBMgr
        Private _anonymStoreDb As Boolean

        Public Overrides Property ApplicationName() As String
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

            If Not String.IsNullOrEmpty(config("AnonymousStore")) Then
                _anonymStoreDb = CBool(config("AnonymousStore"))
            End If

            If String.IsNullOrEmpty(config("GetDBMgr")) Then
                Throw New ArgumentException("Implementation of IGetDBMgr is requred under GetDBMgr key")
            Else
                'Dim t As Type = Type.GetType(config("GetDBMgr"))
                'If t Is Nothing Then
                '    Throw New ArgumentException("Cannot create type from " & config("GetDBMgr"))
                'End If

                'Dim gm As IGetDBMgr = CType(t.InvokeMember(Nothing, _
                '    Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IGetDBMgr)
                Dim gm As IGetDBMgr = CreateDBMgr(config("GetDBMgr"))
                If gm Is Nothing Then
                    Throw New ArgumentException("Cannot create instance of IGetDBMgr from " & config("GetDBMgr"))
                End If

                _getMgr = AddressOf gm.GetMgr
            End If
            MyBase.Initialize(name, config)
        End Sub

        Public Overrides Function DeleteInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date) As Integer
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                Dim col As ICollection = FindUsers(mgr, c.Condition)
                For Each u As OrmBase In col
                    DeleteProfile(mgr, u)
                Next
                Return col.Count
            End Using
        End Function

        Public Overloads Overrides Function DeleteProfiles(ByVal usernames() As String) As Integer
            Using mgr As OrmDBManager = _getMgr()
                Dim cnt As Integer = 0
                For Each u As String In usernames
                    Dim user As OrmBase = GetUserByName(mgr, u, True, _anonymStoreDb)
                    If user IsNot Nothing Then
                        DeleteProfile(mgr, user)
                    Else
                        System.Web.Security.AnonymousIdentificationModule.ClearAnonymousIdentifier()
                        RemoveAnonymousStoreCookie
                    End If
                    cnt += 1
                Next
                Return cnt
            End Using
        End Function

        Public Overloads Overrides Function DeleteProfiles(ByVal profiles As System.Web.Profile.ProfileInfoCollection) As Integer
            Using mgr As OrmDBManager = _getMgr()
                Dim cnt As Integer = 0
                For Each p As ProfileInfo In profiles
                    Dim user As OrmBase = GetUserByName(mgr, p.UserName, Not p.IsAnonymous, _anonymStoreDb)
                    If user IsNot Nothing Then
                        DeleteProfile(mgr, user)
                    Else
                        System.Web.Security.AnonymousIdentificationModule.ClearAnonymousIdentifier()
                        RemoveAnonymousStoreCookie
                    End If
                    cnt += 1
                Next
                Return cnt
            End Using
        End Function

        Protected Function CreateProfileCollection(ByVal pageIndex As Integer, ByVal pageSize As Integer, ByVal mgr As OrmDBManager, ByVal col As IList) As ProfileInfoCollection
            Dim profiles As New ProfileInfoCollection
            Dim schema As OrmSchemaBase = mgr.ObjectSchema
            Dim start As Integer = pageIndex * pageSize
            If start < col.Count Then
                Dim [end] As Integer = Math.Min((pageIndex - 1) * pageSize, col.Count)
                For i As Integer = start To [end] - 1
                    Dim u As OrmBase = CType(col(i), OrmBase)

                    Dim upd As Date
                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
                        upd = CDate(schema.GetFieldValue(u, _lastUpdateField))
                    End If

                    Dim p As New ProfileInfo( _
                        CStr(schema.GetFieldValue(u, _userNameField)), _
                        CBool(schema.GetFieldValue(u, _isAnonymousField)), _
                        CDate(schema.GetFieldValue(u, _lastActivityField)), upd, 0)

                    profiles.Add(p)
                Next
            End If
            Return profiles
        End Function

        Public Overrides Function FindInactiveProfilesByUserName(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal usernameToMatch As String, ByVal userInactiveSinceDate As Date, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) OrElse String.IsNullOrEmpty(_userNameField) Then
                Throw New InvalidOperationException("Username, LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                c.AddFilter(New OrmFilter(GetUserType, _userNameField, usernameToMatch & "%", FilterOperation.Like))
                Dim col As IList = FindUsers(mgr, c.Condition)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function FindProfilesByUserName(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal usernameToMatch As String, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_userNameField) Then
                Throw New InvalidOperationException("UserName or IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                c.AddFilter(New OrmFilter(GetUserType, _userNameField, usernameToMatch & "%", FilterOperation.Like))
                Dim col As IList = FindUsers(mgr, c.Condition)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetAllInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                Dim col As IList = FindUsers(mgr, c.Condition)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetAllProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal pageIndex As Integer, ByVal pageSize As Integer, ByRef totalRecords As Integer) As System.Web.Profile.ProfileInfoCollection
            If String.IsNullOrEmpty(_isAnonymousField) Then
                Throw New InvalidOperationException("IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                Dim col As IList = FindUsers(mgr, c.Condition)
                totalRecords = col.Count
                Return CreateProfileCollection(pageIndex, pageSize, mgr, col)
            End Using
        End Function

        Public Overrides Function GetNumberOfInactiveProfiles(ByVal authenticationOption As System.Web.Profile.ProfileAuthenticationOption, ByVal userInactiveSinceDate As Date) As Integer
            If String.IsNullOrEmpty(_isAnonymousField) OrElse String.IsNullOrEmpty(_lastActivityField) Then
                Throw New InvalidOperationException("LastActivity or IsAnonymous field is not specified")
            End If

            Using mgr As OrmDBManager = _getMgr()
                Dim c As New OrmCondition.OrmConditionConstructor
                c.AddFilter(New OrmFilter(GetUserType, _lastActivityField, New TypeWrap(Of Object)(userInactiveSinceDate), FilterOperation.LessEqualThan))
                Select Case authenticationOption
                    Case ProfileAuthenticationOption.Anonymous
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(True), FilterOperation.Equal))
                    Case ProfileAuthenticationOption.Authenticated
                        c.AddFilter(New OrmFilter(GetUserType, _isAnonymousField, New TypeWrap(Of Object)(False), FilterOperation.Equal))
                End Select
                Dim col As IList = FindUsers(mgr, c.Condition)
                Return col.Count
            End Using
        End Function

        Public Overrides Function GetPropertyValues(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyCollection) As System.Configuration.SettingsPropertyValueCollection
            Using mgr As OrmDBManager = _getMgr()
                Dim col As New SettingsPropertyValueCollection

                Dim user As OrmBase = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _anonymStoreDb)
                If user Is Nothing Then
                    If _anonymStoreDb Then
                        Throw New ArgumentException("Cannot find user " & GetUserName(context))
                    End If
                    Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
                    For Each p As SettingsProperty In collection
                        Dim newp As New SettingsPropertyValue(p)
                        If cok IsNot Nothing Then
                            newp.PropertyValue = cok(p.Name)
                        ElseIf p.DefaultValue IsNot Nothing Then
                            newp.PropertyValue = p.DefaultValue
                        End If
                        col.Add(newp)
                    Next
                    'If Not String.IsNullOrEmpty(_lastActivityField) AndAlso cok IsNot Nothing Then
                    '    cok(_lastActivityField) = GetNow().ToString
                    'End If
                    'cok.Expires = Date.Now.AddDays(7)
                    'HttpContext.Current.Response.Cookies.Add(cok)
                Else
                    Dim schema As OrmSchemaBase = mgr.ObjectSchema
                    For Each p As SettingsProperty In collection
                        Dim newp As New SettingsPropertyValue(p)
                        'If p.Attributes.Contains("CustomProviderData") Then
                        '    If p.Attributes.Item("CustomProviderData") IsNot Nothing Then
                        '        Dim s As String = CStr(p.Attributes.Item("CustomProviderData"))
                        '        If s.Contains("storeInCookie") Then
                        '            'System.Web.Security.FormsAuthentication.
                        '        End If
                        '    End If
                        'End If
                        newp.PropertyValue = schema.GetFieldValue(user, p.Name)
                        col.Add(newp)
                    Next

                    If Not String.IsNullOrEmpty(_lastActivityField) Then
                        schema.SetFieldValue(user, _lastActivityField, GetNow)
                    End If
                End If
                Return col
            End Using
        End Function

        Public Overrides Sub SetPropertyValues(ByVal context As System.Configuration.SettingsContext, ByVal collection As System.Configuration.SettingsPropertyValueCollection)
            Using mgr As OrmDBManager = _getMgr()
                Dim user As OrmBase = GetUserByName(mgr, GetUserName(context), IsAuthenticated(context), _anonymStoreDb)

                If user Is Nothing Then
                    If _anonymStoreDb Then
                        Throw New ArgumentException("Cannot find user " & GetUserName(context))
                    End If
                    'Dim cok As HttpCookie = HttpContext.Current.Response.Cookies(GetAnonymousCookieName)
                    'If cok Is Nothing OrElse cok.Expires > Date.Now Then
                    Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
                    If Not context.ContainsKey("remove_profile") Then
                        If cok Is Nothing Then
                            cok = New HttpCookie(GetAnonymousCookieName)
                        End If
                        Dim created As Boolean = False
                        For Each p As SettingsPropertyValue In collection
                            If Not p.Property.IsReadOnly Then
                                If p.PropertyValue IsNot Nothing AndAlso Not p.PropertyValue.Equals(p.Property.DefaultValue) Then
                                    cok.Values(p.Name) = p.PropertyValue.ToString
                                    created = True
                                End If
                            End If
                        Next
                        If created Then
                            Dim d As Date = GetNow()
                            If Not String.IsNullOrEmpty(_lastActivityField) Then
                                cok.Values(_lastActivityField) = d.ToString
                            End If
                            If Not String.IsNullOrEmpty(_lastUpdateField) Then
                                cok.Values(_lastUpdateField) = d.ToString
                            End If

                            cok.Expires = Date.Now.AddDays(7)
                            HttpContext.Current.Response.Cookies.Add(cok)
                        End If
                    End If
                Else
                    CopyValuesToUser(collection, mgr, user)
                End If
            End Using
        End Sub

        Protected Sub CopyValuesToUser(ByVal collection As System.Configuration.SettingsPropertyValueCollection, ByVal mgr As OrmDBManager, ByVal user As OrmBase)
            Dim schema As OrmSchemaBase = mgr.ObjectSchema
            For Each p As SettingsPropertyValue In collection
                If Not p.Property.IsReadOnly Then
                    schema.SetFieldValue(user, p.Name, p.PropertyValue)
                End If
                'Debug.Write(p.Name & ": ")
                'If p.PropertyValue IsNot Nothing Then
                '    Debug.WriteLine(p.PropertyValue.ToString)
                'Else
                '    Debug.WriteLine(String.Empty)
                'End If
            Next
            Dim d As Date = GetNow()
            If Not String.IsNullOrEmpty(_lastActivityField) Then
                schema.SetFieldValue(user, _lastActivityField, d)
            End If
            If Not String.IsNullOrEmpty(_lastUpdateField) Then
                schema.SetFieldValue(user, _lastUpdateField, d)
            End If
            user.Save(True)
        End Sub

        Public Sub MigrateAnonymous(ByVal AnonymousId As String)
            Dim cok As HttpCookie = HttpContext.Current.Request.Cookies(GetAnonymousCookieName)
            Using mgr As OrmDBManager = _getMgr()
                Dim user As OrmBase = Nothing
                Try
                    user = GetUserByName(mgr, HttpContext.Current.Profile.UserName, True, False) 'CreateUser(mgr, HttpContext.Current.Profile.UserName)
                Catch ex As ArgumentException When ex.Message.Contains("not found")
                    user = CreateUser(mgr, HttpContext.Current.Profile.UserName, AnonymousId)
                    Dim schema As OrmSchemaBase = mgr.ObjectSchema
                    For Each p As SettingsProperty In System.Web.Profile.ProfileBase.Properties
                        If Not p.IsReadOnly Then
                            If cok IsNot Nothing Then
                                schema.SetFieldValue(user, p.Name, cok(p.Name))
                            ElseIf p.DefaultValue IsNot Nothing Then
                                schema.SetFieldValue(user, p.Name, p.DefaultValue)
                            End If
                        End If
                    Next
                    Dim d As Date = GetNow()
                    If Not String.IsNullOrEmpty(_lastActivityField) Then
                        schema.SetFieldValue(user, _lastActivityField, d)
                    End If
                    If Not String.IsNullOrEmpty(_lastUpdateField) Then
                        schema.SetFieldValue(user, _lastUpdateField, d)
                    End If
                    user.Save(True)
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
                cok.Expires = Date.Now.AddDays(-1)
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

        Protected Friend MustOverride Function GetUserByName(ByVal mgr As OrmDBManager, ByVal name As String, ByVal isAuthenticated As Boolean, ByVal createIfNotExist As Boolean) As OrmBase
        Protected Friend MustOverride Function GetUserType() As Type
        Protected Friend MustOverride Function FindUsers(ByVal mgr As OrmDBManager, ByVal f As IOrmFilter) As IList
        'Protected Friend MustOverride Function FindTopUsers(ByVal mgr As OrmDBManager, ByVal top As Integer) As IList
        Protected Friend MustOverride Sub DeleteUser(ByVal mgr As OrmDBManager, ByVal u As OrmBase, ByVal cascase As Boolean)
        Protected Friend MustOverride Sub DeleteProfile(ByVal mgr As OrmDBManager, ByVal u As OrmBase)
        Protected Friend MustOverride Function GetNow() As Date
        Protected Friend MustOverride Function CreateDBMgr(ByVal type As String) As IGetDBMgr
        Protected Friend MustOverride Function GetAnonymousCookieName() As String
        Protected Friend MustOverride Function CreateUser(ByVal mgr As OrmDBManager, ByVal name As String, ByVal AnonymousId As String) As OrmBase

    End Class

    Public Interface IGetDBMgr
        Function GetMgr() As OrmDBManager
    End Interface
End Namespace
