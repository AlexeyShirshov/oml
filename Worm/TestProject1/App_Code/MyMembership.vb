Imports Worm.Web
Imports Worm.Orm
Imports System.Collections.Generic
Imports System.Collections

Public Class MyMembership
    Inherits MembershipBase

    Protected Overrides Function MapField(ByVal membershipUserField As String) As String
        If membershipUserField = "IsLockedOut" Then
            Return "IsLocked"
        ElseIf membershipUserField = "LastLockoutDate" Then
            Return "LastLockedAt"
        End If
        Return MyBase.MapField(membershipUserField)
    End Function
End Class
