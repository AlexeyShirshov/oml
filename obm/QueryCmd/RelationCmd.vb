Imports Worm.Entities
Imports Worm.Entities.Meta

Namespace Query
    Public Class RelationCmd
        Inherits QueryCmd

        Private _desc As RelationDesc

#Region " Ctors "
        Public Sub New()
        End Sub

        Public Sub New(ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity)
            MyBase.New(obj)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
            MyBase.New(obj, key)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(obj, getMgr)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(obj, key, getMgr)
        End Sub

#End Region

        Public Shared Shadows Function Create(ByVal obj As IKeyEntity) As RelationCmd
            Return Create(obj, OrmManager.CurrentManager)
        End Function

        Public Shared Shadows Function Create(ByVal obj As IKeyEntity, ByVal key As String) As RelationCmd
            Return Create(obj, key, OrmManager.CurrentManager)
        End Function

        Public Shared Shadows Function Create(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj)
            Else
                Throw New NotImplementedException
                'q = f.Create(obj)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Shadows Function Create(ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, key)
            Else
                Throw New NotImplementedException
                'q = f.Create(obj, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

    End Class
End Namespace