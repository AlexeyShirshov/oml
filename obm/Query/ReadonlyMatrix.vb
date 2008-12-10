Imports System.Collections.ObjectModel
Imports Worm.Entities
Imports System.Collections.Generic

Public Class ReadonlyMatrix
    Inherits ReadOnlyCollection(Of ReadOnlyCollection(Of _IEntity))

    Protected _l As List(Of ReadOnlyCollection(Of _IEntity))

    Public Sub New(ByVal l As List(Of ReadOnlyCollection(Of _IEntity)))
        MyBase.New(l)
        _l = l
    End Sub

    Public Sub New()
        MyClass.New(New List(Of ReadOnlyCollection(Of _IEntity)))
    End Sub
End Class
