Imports Worm.Web
Imports Worm.Orm
Imports System.Collections.Generic
Imports System.Collections
Imports Worm.Database
Imports Worm.Database.Criteria

Public Class MyRoles
    Inherits RoleBase

    Protected Overloads Overrides Sub DeleteRole(ByVal mgr As OrmDBManager, ByVal role As Worm.Orm.OrmBase, ByVal cascase As Boolean)
        If cascase Then
            Throw New NotSupportedException("Cascade delete is not supported")
        End If
        role.Delete()
        role.SaveChanges(True)
    End Sub

    Protected Overrides Function FindRoles(ByVal mgr As OrmDBManager, ByVal f As CriteriaLink) As System.Collections.IList
        Return CType(mgr.Find(Of MyRole)(f, Nothing, WithLoad), System.Collections.IList)
    End Function

    Protected Overrides Function GetRoleByName(ByVal mgr As OrmDBManager, ByVal name As String, ByVal createIfNotExist As Boolean) As Worm.Orm.OrmBase
        Dim t As Type = GetRoleType()
        'Dim c As New OrmCondition.OrmConditionConstructor
        'c.AddFilter(New OrmFilter(t, _rolenameField, New Worm.TypeWrap(Of Object)(name), FilterOperation.Equal))
        Dim col As IList = FindRoles(mgr, CType(New Criteria.Ctor(t).Field(_rolenameField).Eq(name), CriteriaLink))
        If col.Count > 1 Then
            Throw New ArgumentException("Duplicate role name " & name)
        ElseIf col.Count = 0 Then
            If createIfNotExist Then
                Dim r As New MyRole(-100, mgr.Cache, mgr.ObjectSchema)
                r.RoleName = name
                r.SaveChanges(True)
                Return r
            Else
                Return Nothing
            End If
        End If
        Return CType(col(0), OrmBase)
    End Function

    Protected Overrides Function GetRoleType() As System.Type
        Return GetType(MyRole)
    End Function

    Protected Overrides ReadOnly Property WithLoad() As Boolean
        Get
            Return True
        End Get
    End Property
End Class
