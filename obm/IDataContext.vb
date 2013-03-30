﻿Imports Worm.Query

Public Delegate Function CreateCmdDelegate() As Query.QueryCmd

Public Interface IDataContext

    Function Create() As QueryCmd

End Interface

Public Class DataContext
    Implements IDataContext

    Dim _del As CreateCmdDelegate

    Public Sub New()

    End Sub

    Public Sub New(del As CreateCmdDelegate)
        _del = del
    End Sub

    Public Function Create() As Query.QueryCmd Implements IDataContext.Create
        If _del Is Nothing Then
            Throw New InvalidOperationException
        End If

        Return _del()
    End Function
End Class