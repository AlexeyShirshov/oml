Imports Worm.Web
Imports Worm.Entities
Imports System.Collections.Generic
Imports System.Collections
Imports Worm.Database
Imports Worm.Criteria
Imports Worm.Query

Public Class MyRoles
    Inherits RoleBase

    'Protected Overloads Overrides Sub DeleteRole(ByVal mgr As OrmDBManager, ByVal role As Worm.Orm.IOrmBase, ByVal cascase As Boolean)
    '    If cascase Then
    '        Throw New NotSupportedException("Cascade delete is not supported")
    '    End If
    '    CType(role, ICachedEntity).Delete()
    '    role.SaveChanges(True)
    'End Sub

    'Protected Overrides Function FindRoles(ByVal mgr As Worm.OrmManager, ByVal f As CriteriaLink) As System.Collections.IList
    '    Return CType(mgr.Find(Of MyRole)(f, Nothing, WithLoad), System.Collections.IList)
    'End Function

    Protected Overrides Function GetRoleByName(ByVal name As String, ByVal createIfNotExist As Boolean) As Worm.Entities.ISinglePKEntity
        Dim t As Type = GetRoleType()
        'Dim c As New OrmCondition.OrmConditionConstructor
        'c.AddFilter(New OrmFilter(t, _rolenameField, New Worm.TypeWrap(Of Object)(name), FilterOperation.Equal))
        Dim col As IList = FindRoles(CType(New Ctor(t).prop(RoleNameField).eq(name), PredicateLink))
        If col.Count > 1 Then
            Throw New ArgumentException("Duplicate role name " & name)
        ElseIf col.Count = 0 Then
            If createIfNotExist Then
                Using mt As New ModificationsTracker(UserMapper.CreateManager)
                    Dim r As MyRole = mt.CreateNewKeyEntity(Of MyRole)()
                    r.RoleName = name
                    mt.AcceptModifications()
                    Return r
                End Using
            Else
                Return Nothing
            End If
        End If
        Return CType(col(0), SinglePKEntity)
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
