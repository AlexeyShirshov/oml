Imports System.Web
Imports Worm.Web
Imports Worm.Orm
Imports System.Collections.Generic
Imports System.Collections

Public Class GetMgr
    Implements IGetDBMgr

    Public Function GetMgr() As Worm.Orm.OrmDBManager Implements Worm.Web.IGetDBMgr.GetMgr
        Return New OrmDBManager(OrmCache, New Worm.Orm.DbSchema("1"), "Data Source=vs2\sqlmain;Integrated Security=true;Initial Catalog=test;")
    End Function

    Protected ReadOnly Property OrmCache() As OrmCache
        Get
            Dim c As OrmCache = CType(HttpContext.Current.Application("ccc"), Worm.Orm.OrmCache)
            If c Is Nothing Then
                'Diagnostics.Debug.WriteLine("create cache")
                c = New Worm.Orm.OrmCache
                HttpContext.Current.Application("ccc") = c
            End If
            Return c
        End Get
    End Property

End Class

Public Class MyProfile
    Inherits ProfileBase

    'Public Sub New(ByVal getMgr As GetDBManager)
    '    MyBase.New(getMgr)
    'End Sub

    Protected Overrides Sub DeleteUser(ByVal mgr As OrmDBManager, ByVal u As Worm.Orm.OrmBase, ByVal cascade As Boolean)
        If cascade Then
            Throw New NotSupportedException("Cascade delete is not supported")
        End If
        u.Delete()
        u.Save(True)
    End Sub

    Protected Overrides Sub DeleteProfile(ByVal mgr As Worm.Orm.OrmDBManager, ByVal u As Worm.Orm.OrmBase)
        Throw New NotImplementedException
    End Sub

    Protected Overrides Function FindUsers(ByVal mgr As OrmDBManager, ByVal f As Worm.Orm.CriteriaLink) As System.Collections.IList
        Return CType(mgr.Find(Of MyUser)(f, Nothing, False), System.Collections.IList)
    End Function

    'Protected Overrides Function FindTopUsers(ByVal mgr As OrmDBManager, ByVal top As Integer) As System.Collections.IList
    '    Return CType(mgr.FindTop(Of MyUser)(top, Nothing, Nothing, SortType.Asc, False), System.Collections.IList)
    'End Function

    Protected Overrides Function GetNow() As Date
        Return Now
    End Function

    Protected Overrides Function GetUserByName(ByVal mgr As OrmDBManager, ByVal name As String, ByVal isAuthenticated As Boolean, ByVal createIfNotExist As Boolean) As Worm.Orm.OrmBase
        Dim t As Type = GetUserType()
        'Dim c As New OrmCondition.OrmConditionConstructor
        'c.AddFilter(New OrmFilter(t, _userNameField, New Worm.TypeWrap(Of Object)(name), FilterOperation.Equal))
        'c.AddFilter(New OrmFilter(t, "IsAnonymous", New Worm.TypeWrap(Of Object)(Not isAuthenticated), FilterOperation.Equal))
        Dim col As IList = FindUsers(mgr, New Criteria(t).Field(_userNameField).Eq(name).And("IsAnonymous").Eq(Not isAuthenticated))
        If col.Count > 1 Then
            Throw New ArgumentException("Duplicate user name " & name)
        ElseIf col.Count = 0 Then
            If isAuthenticated Then
                Throw New ArgumentException("User with a name " & name & " is not found")
            Else
                If createIfNotExist Then
                    Dim u As New MyUser(-100, mgr.Cache, mgr.ObjectSchema)
                    u.LastActivity = GetNow()
                    u.IsAnonymous = True
                    u.UserName = name
                    u.Email = name
                    u.Save(True)
                    Return u
                Else
                    Return Nothing
                End If
            End If
        End If
        Return CType(col(0), OrmBase)
    End Function

    Protected Overrides Function GetUserType() As System.Type
        Return GetType(MyUser)
    End Function

    Protected Overrides Function CreateDBMgr(ByVal type As String) As IGetDBMgr
        Return CType(Reflection.Assembly.GetExecutingAssembly.CreateInstance(type), IGetDBMgr)
    End Function

    Protected Overrides Function GetAnonymousCookieName() As String
        Return ".TESTPROJECTANONYMCOOKIE"
    End Function

    Protected Overrides Function CreateUser(ByVal mgr As OrmDBManager, ByVal name As String, ByVal AnonymousId As String) As Worm.Orm.OrmBase
        Dim u As New MyUser(-100, mgr.Cache, mgr.ObjectSchema)
        u.UserName = name
        Return u
    End Function
End Class

<Entity(GetType(MyUserDef), "1")> _
Public Class MyUser
    Inherits OrmBase

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

    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyUser(CType(from, MyUser), CType([to], MyUser))
    End Sub

    Protected Shared Sub CopyUser(ByVal from As MyUser, ByVal [to] As MyUser)
        With from
            [to]._lastActivity = ._lastActivity
            [to]._field = ._field
            [to]._isAnonymous = ._isAnonymous
            [to]._username = ._username
            [to]._psw = ._psw
            [to]._email = ._email
            [to]._failcnt = ._failcnt
            [to]._faildt = ._faildt
            [to]._islocked = ._islocked
            [to]._lastlocked = ._lastlocked
        End With
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {New, Worm.Orm.OrmBase})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New MyUser(Identifier, OrmCache, OrmSchema)
    End Function

    <Column("LastActivity")> _
    Public Property LastActivity() As Date
        Get
            Using SyncHelper(True, "LastActivity")
                Return _lastActivity
            End Using
        End Get
        Set(ByVal value As Date)
            Using SyncHelper(False, "LastActivity")
                _lastActivity = value
            End Using
        End Set
    End Property

    <Column("IsAnonymous")> _
    Public Property IsAnonymous() As Boolean
        Get
            Using SyncHelper(True, "IsAnonymous")
                Return _isAnonymous
            End Using
        End Get
        Set(ByVal value As Boolean)
            Using SyncHelper(False, "IsAnonymous")
                _isAnonymous = value
            End Using
        End Set
    End Property

    <Column("UserName")> _
    Public Property UserName() As String
        Get
            Using SyncHelper(True, "UserName")
                Return _username
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "UserName")
                _username = value
            End Using
        End Set
    End Property

    <Column("Field")> _
    Public Property Field() As String
        Get
            Using SyncHelper(True, "Field")
                Return _field
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Field")
                _field = value
            End Using
        End Set
    End Property

    <Column("Password")> _
    Public Property Password() As Byte()
        Get
            Using SyncHelper(True, "Password")
                Return _psw
            End Using
        End Get
        Set(ByVal value As Byte())
            Using SyncHelper(False, "Password")
                _psw = value
            End Using
        End Set
    End Property

    <Column("Email")> _
    Public Property Email() As String
        Get
            Using SyncHelper(True, "Email")
                Return _email
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Email")
                _email = value
            End Using
        End Set
    End Property

    <Column("FailedPasswordAttemtCount")> _
    Public Property FailedPswAttemtCount() As Integer
        Get
            Using SyncHelper(True, "FailedPasswordAttemtCount")
                Return _failcnt
            End Using
        End Get
        Set(ByVal value As Integer)
            Using SyncHelper(False, "FailedPasswordAttemtCount")
                _failcnt = value
            End Using
        End Set
    End Property

    <Column("FailedPasswordAttemtStart")> _
    Public Property FailedPswAttemtDate() As Nullable(Of Date)
        Get
            Using SyncHelper(True, "FailedPasswordAttemtStart")
                Return _faildt
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using SyncHelper(False, "FailedPasswordAttemtStart")
                _faildt = value
            End Using
        End Set
    End Property

    <Column("IsLocked")> _
    Public Property IsLocked() As Boolean
        Get
            Using SyncHelper(True, "IsLocked")
                Return _islocked
            End Using
        End Get
        Set(ByVal value As Boolean)
            Using SyncHelper(False, "IsLocked")
                _islocked = value
            End Using
        End Set
    End Property

    <Column("LastLockedAt")> _
    Public Property LastLockedAt() As Nullable(Of Date)
        Get
            Using SyncHelper(True, "LastLockedAt")
                Return _lastlocked
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using SyncHelper(False, "LastLockedAt")
                _lastlocked = value
            End Using
        End Set
    End Property
End Class

Public Class MyUserDef
    Inherits ObjectSchemaBaseImplementationWeb

    Public Enum Tables
        Main
    End Enum

    Private _tbls() As OrmTable = {New OrmTable("dbo.users")}

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        Dim idx As New OrmObjectIndex
        idx.Add(New MapField2Column("LastActivity", "last_activity", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("IsAnonymous", "is_anonymous", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("UserName", "username", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("Field", "field", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("Password", "password", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("Email", "email", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("FailedPasswordAttemtCount", "failcnt", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("FailedPasswordAttemtStart", "faildt", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("IsLocked", "islocked", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("LastLockedAt", "lastlocked", GetTables()(Tables.Main)))
        Return idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tbls
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        Return New M2MRelation() { _
                New M2MRelation(GetType(MyRole), _schema.GetSharedTable("dbo.UserRoles"), "role_id", False, New System.Data.Common.DataTableMapping, CType(Nothing, Type)) _
            }
    End Function

End Class

<Entity(GetType(MyRoleDef), "1")> _
Public Class MyRole
    Inherits OrmBase

    Private _role As String

    Public Sub New()

    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyRole(CType(from, MyRole), CType([to], MyRole))
    End Sub

    Protected Shared Sub CopyRole(ByVal from As MyRole, ByVal [to] As MyRole)
        With from
            [to]._role = ._role
        End With
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {New, Worm.Orm.OrmBase})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New MyRole(Identifier, OrmCache, OrmSchema)
    End Function

    <Column("Name")> _
    Public Property RoleName() As String
        Get
            Using SyncHelper(True, "Name")
                Return _role
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Name")
                _role = value
            End Using
        End Set
    End Property

End Class

Public Class MyRoleDef
    Inherits ObjectSchemaBaseImplementationWeb

    Public Enum Tables
        Main
    End Enum

    Private _tbls() As OrmTable = {New OrmTable("dbo.roles")}

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        Dim idx As New OrmObjectIndex
        idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("Name", "roleName", GetTables()(Tables.Main)))
        Return idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tbls
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        Return New M2MRelation() { _
                New M2MRelation(GetType(MyUser), _schema.GetSharedTable("dbo.UserRoles"), "user_id", False, New System.Data.Common.DataTableMapping, CType(Nothing, Type)) _
            }
    End Function


End Class