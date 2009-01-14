Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

Namespace Query
    Public Class RelationCmd
        Inherits QueryCmd

        Private _rel As Relation
        Private _desc As RelationDesc

#Region " Ctors "
        Public Sub New(ByVal rel As Relation)
            _rel = rel
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _desc = desc
        End Sub

        Public Sub New(ByVal desc As RelationDesc)
            _desc = desc
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _rel = rel
        End Sub

        Public Sub New(ByVal obj As IKeyEntity)
            MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(obj.GetType), Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
            MyBase.New(obj, key)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(obj.GetType), Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As IKeyEntity)
            MyBase.New(obj, desc.Key)
            _rel = New Relation(obj, desc)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _desc = desc
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(obj, getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(obj.GetType), Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(obj, key, getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(obj.GetType), Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(obj, desc.Key, getMgr)
            _rel = New Relation(obj, desc)
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