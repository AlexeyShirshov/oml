Imports System.Web
Imports Worm.Web
Imports Worm.Entities
Imports System.Collections.Generic
Imports System.Collections
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Database
Imports Worm
Imports Worm.Criteria
Imports Worm.Query

#Const UseUserInstance = True

Public Class GetMgr
    Implements Worm.ICreateManager

    Public Function GetMgr(ctx As Object) As Worm.OrmManager Implements Worm.ICreateManager.CreateManager
#If UseUserInstance Then
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
        Dim m As OrmManager = New OrmDBManager(OrmCache, New ObjectMappingEngine("1"), New SQLGenerator, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
#Else
        Dim m As OrmManager = New OrmDBManager(OrmCache, New ObjectMappingEngine("1"), New SQLGenerator, "Data Source=.\sqlexpress;Integrated Security=true;Initial Catalog=test;")
#End If

        RaiseEvent CreateManagerEvent(Me, New ICreateManager.CreateManagerEventArgs(m, ctx))
        Return m
    End Function

    Protected ReadOnly Property OrmCache() As OrmCache
        Get
            Dim c As OrmCache = CType(HttpContext.Current.Application("ccc"), OrmCache)
            If c Is Nothing Then
                'Diagnostics.Debug.WriteLine("create cache")
                c = New OrmCache
                HttpContext.Current.Application("ccc") = c
            End If
            Return c
        End Get
    End Property

    Public Event CreateManagerEvent(sender As Worm.ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements Worm.ICreateManager.CreateManagerEvent
End Class

Public Class MyProfile
    Inherits ProfileBase

    'Public Sub New(ByVal getMgr As GetDBManager)
    '    MyBase.New(getMgr)
    'End Sub

    'Protected Overrides Sub DeleteUser(ByVal mgr As OrmDBManager, ByVal u As Worm.Orm.IOrmBase, ByVal cascade As Boolean)
    '    If cascade Then
    '        Throw New NotSupportedException("Cascade delete is not supported")
    '    End If
    '    Using mt As New ModificationsTracker(mgr)
    '        CType(u, ICachedEntity).Delete()
    '        mt.AcceptModifications()
    '    End Using
    'End Sub

    'Protected Overrides Sub DeleteProfile(ByVal mgr As OrmDBManager, ByVal u As Worm.Orm.IOrmBase)
    '    Throw New NotImplementedException
    'End Sub

    'Protected Overrides Function FindUsers(ByVal mgr As OrmManager, ByVal f As Worm.Criteria.CriteriaLink) As System.Collections.IList
    '    Return CType(mgr.Find(Of MyUser)(f, Nothing, False), System.Collections.IList)
    'End Function

    'Protected Overrides Function FindTopUsers(ByVal mgr As OrmDBManager, ByVal top As Integer) As System.Collections.IList
    '    Return CType(mgr.FindTop(Of MyUser)(top, Nothing, Nothing, SortType.Asc, False), System.Collections.IList)
    'End Function

    'Protected Overrides Function GetNow() As Date
    '    Return Now
    'End Function

    Protected Overrides Function GetUserByName(ByVal name As String, ByVal isAuthenticated As Boolean, ByVal createIfNotExist As Boolean) As Worm.Entities.ISinglePKEntity
        Dim t As Type = GetUserType()
        'Dim c As New OrmCondition.OrmConditionConstructor
        'c.AddFilter(New OrmFilter(t, _userNameField, New Worm.TypeWrap(Of Object)(name), FilterOperation.Equal))
        'c.AddFilter(New OrmFilter(t, "IsAnonymous", New Worm.TypeWrap(Of Object)(Not isAuthenticated), FilterOperation.Equal))
        Dim col As IList = FindUsers(New Ctor(t).prop(UserNameField).eq(name).[and]("IsAnonymous").eq(Not isAuthenticated))
        If col.Count > 1 Then
            Throw New ArgumentException("Duplicate user name " & name)
        ElseIf col.Count = 0 Then
            If isAuthenticated Then
                Throw New ArgumentException("User with a name " & name & " is not found")
            Else
                If createIfNotExist Then
                    Using mt As New ModificationsTracker(CreateManager)
                        Dim u As MyUser = mt.CreateNewKeyEntity(Of MyUser)()
                        u.LastActivity = GetNow()
                        u.IsAnonymous = True
                        u.UserName = name
                        u.Email = name
                        mt.AcceptModifications()
                        Return u
                    End Using
                Else
                    Return Nothing
                End If
            End If
        End If
        Return CType(col(0), ISinglePKEntity)
    End Function

    Protected Overrides Function GetUserType() As System.Type
        Return GetType(MyUser)
    End Function

    Protected Overrides Function CreateDBMgr(ByVal type As String) As ICreateManager
        Return CType(Reflection.Assembly.GetExecutingAssembly.CreateInstance(type), ICreateManager)
    End Function

    Protected Overrides Function GetAnonymousCookieName() As String
        Return ".TESTPROJECTANONYMCOOKIE"
    End Function

    Protected Overrides Function CreateUser(ByVal mt As ModificationsTracker, ByVal name As String, ByVal AnonymousId As String, ByVal context As Object) As Worm.Entities.ISinglePKEntity
        Dim u As MyUser = mt.CreateNewKeyEntity(Of MyUser)()
        u.UserName = name
        Return u
    End Function
End Class

<Entity(GetType(MyUserDef), "1")> _
Public Class MyUser
    Inherits SinglePKEntity
    Implements ICopyProperties

    Private _lastActivity As Date
    Private _isAnonymous As Boolean
    Private _field As String
    Private _username As String
    Private _psw As Byte()
    Private _email As String
    Private _failcnt As Integer
    Private _faildt As Nullable(Of Date)
    Private _islocked As Boolean
    Private _lastlocked As Nullable(Of Date)

    Public Sub New()
        'MyBase.New()
    End Sub

    Private _id As Integer

    <EntityProperty(Field2DbRelations.PrimaryKey)> _
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property

    Protected Sub CopyProperties(ByVal [to] As Object) Implements ICopyProperties.CopyTo
        With Me
            CType([to], MyUser)._id = ._id
            CType([to], MyUser)._lastActivity = ._lastActivity
            CType([to], MyUser)._field = ._field
            CType([to], MyUser)._isAnonymous = ._isAnonymous
            CType([to], MyUser)._username = ._username
            CType([to], MyUser)._psw = ._psw
            CType([to], MyUser)._email = ._email
            CType([to], MyUser)._failcnt = ._failcnt
            CType([to], MyUser)._faildt = ._faildt
            CType([to], MyUser)._islocked = ._islocked
            CType([to], MyUser)._lastlocked = ._lastlocked
        End With
    End Sub

    <EntityPropertyAttribute(PropertyAlias:="LastActivity")> _
    Public Property LastActivity() As Date
        Get
            Using Read("LastActivity")
                Return _lastActivity
            End Using
        End Get
        Set(ByVal value As Date)
            Using Write("LastActivity")
                _lastActivity = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="IsAnonymous")> _
    Public Property IsAnonymous() As Boolean
        Get
            Using Read("IsAnonymous")
                Return _isAnonymous
            End Using
        End Get
        Set(ByVal value As Boolean)
            Using Write("IsAnonymous")
                _isAnonymous = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="UserName")> _
    Public Property UserName() As String
        Get
            Using Read("UserName")
                Return _username
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("UserName")
                _username = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Field")> _
    Public Property Field() As String
        Get
            Using Read("Field")
                Return _field
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Field")
                _field = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Password")> _
    Public Property Password() As Byte()
        Get
            Using Read("Password")
                Return _psw
            End Using
        End Get
        Set(ByVal value As Byte())
            Using Write("Password")
                _psw = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Email")> _
    Public Property Email() As String
        Get
            Using Read("Email")
                Return _email
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Email")
                _email = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="FailedPasswordAttemtCount")> _
    Public Property FailedPswAttemtCount() As Integer
        Get
            Using Read("FailedPasswordAttemtCount")
                Return _failcnt
            End Using
        End Get
        Set(ByVal value As Integer)
            Using Write("FailedPasswordAttemtCount")
                _failcnt = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="FailedPasswordAttemtStart")> _
    Public Property FailedPswAttemtDate() As Nullable(Of Date)
        Get
            Using Read("FailedPasswordAttemtStart")
                Return _faildt
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using Write("FailedPasswordAttemtStart")
                _faildt = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="IsLocked")> _
    Public Property IsLocked() As Boolean
        Get
            Using Read("IsLocked")
                Return _islocked
            End Using
        End Get
        Set(ByVal value As Boolean)
            Using Write("IsLocked")
                _islocked = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="LastLockedAt")> _
    Public Property LastLockedAt() As Nullable(Of Date)
        Get
            Using Read("LastLockedAt")
                Return _lastlocked
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using Write("LastLockedAt")
                _lastlocked = value
            End Using
        End Set
    End Property
End Class

Public Class MyUserDef
    Inherits ObjectSchemaBaseImplementationWeb
    Implements ISchemaWithM2M

    Private _idx As OrmObjectIndex

    Public Sub New()
        _tbl = New SourceFragment("dbo.users")
    End Sub

    'Public Enum Tables
    '    Main
    'End Enum

    'Private _tbls() As SourceFragment = {New SourceFragment("dbo.users")}

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                _idx = New OrmObjectIndex
                _idx.Add(New MapField2Column("LastActivity", "last_activity", Table))
                _idx.Add(New MapField2Column("IsAnonymous", "is_anonymous", Table))
                _idx.Add(New MapField2Column("UserName", "username", Table))
                _idx.Add(New MapField2Column("ID", "id", Table))
                _idx.Add(New MapField2Column("Field", "field", Table))
                _idx.Add(New MapField2Column("Password", "password", Table))
                _idx.Add(New MapField2Column("Email", "email", Table))
                _idx.Add(New MapField2Column("FailedPasswordAttemtCount", "failcnt", Table))
                _idx.Add(New MapField2Column("FailedPasswordAttemtStart", "faildt", Table))
                _idx.Add(New MapField2Column("IsLocked", "islocked", Table))
                _idx.Add(New MapField2Column("LastLockedAt", "lastlocked", Table))
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tbls
    'End Function

    Public Function GetM2MRelations() As M2MRelationDesc() Implements ISchemaWithM2M.GetM2MRelations
        Return New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(MyRole), _schema.GetSharedSourceFragment("dbo", "UserRoles"), "role_id", False, New System.Data.Common.DataTableMapping) _
            }
    End Function

End Class

<Entity(GetType(MyRoleDef), "1")> _
Public Class MyRole
    Inherits SinglePKEntity
    Implements ICopyProperties

    Private _role As String

    Public Sub New()

    End Sub

    Private _id As Integer

    <EntityProperty(Field2DbRelations.PrimaryKey)> _
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property

    Protected Sub CopyProperties(ByVal [to] As Object) Implements ICopyProperties.CopyTo
        CType([to], MyRole)._role = _role
        CType([to], MyRole)._id = _id
    End Sub

    <EntityPropertyAttribute(PropertyAlias:="Name")> _
    Public Property RoleName() As String
        Get
            Using Read("Name")
                Return _role
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Name")
                _role = value
            End Using
        End Set
    End Property

End Class

Public Class MyRoleDef
    Inherits ObjectSchemaBaseImplementationWeb
    Implements ISchemaWithM2M

    Private _idx As OrmObjectIndex

    Public Sub New()
        _tbl = New SourceFragment("dbo.roles")
    End Sub
    'Public Enum Tables
    '    Main
    'End Enum

    'Private _tbls() As SourceFragment = {New SourceFragment("dbo.roles")}

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                _idx = New OrmObjectIndex
                _idx.Add(New MapField2Column("ID", "id", Table))
                _idx.Add(New MapField2Column("Name", "roleName", Table))
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tbls
    'End Function

    Public Function GetM2MRelations() As M2MRelationDesc() Implements ISchemaWithM2M.GetM2MRelations
        Return New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(MyUser), _schema.GetSharedSourceFragment("dbo", "UserRoles"), "user_id", False, New System.Data.Common.DataTableMapping) _
            }
    End Function


End Class