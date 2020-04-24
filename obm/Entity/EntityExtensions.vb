Imports Worm.Entities.Meta
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Namespace Entities
    Public Module EntityExtensions
        <Extension>
        Public Function GetJoinObj(ByVal obj As _IEntity, ByVal subType As Type, ByVal oschema As IEntitySchema) As _IEntity
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If subType Is Nothing Then
                Throw New ArgumentNullException("subType")
            End If

            If oschema Is Nothing Then
                Throw New ArgumentNullException("oschema")
            End If

            Dim c As String = oschema.GetJoinFieldNameByType(subType)
            Dim r As _IEntity = Nothing
            If Not String.IsNullOrEmpty(c) Then
                Dim id As Object = ObjectMappingEngine.GetPropertyValue(obj, c, oschema)
                'If obj.IsPropertyLoaded(c) Then
                '    id = obj.GetValueOptimized(Nothing, c, oschema)
                'Else
                '    id = GetPropertyValue(obj, c, oschema)
                'End If
                r = TryCast(id, _IEntity)
                If r Is Nothing AndAlso id IsNot Nothing Then
                    Try
                        Using gm = obj.GetMgr
                            r = gm.Manager.GetKeyEntityFromCacheOrCreate(id, subType)
                        End Using
                    Catch ex As InvalidCastException
                    End Try
                End If
            End If
            Return r
        End Function
        <Extension>
        Public Function HasBodyChanges(ByVal e As IEntity) As Boolean
            Return e.ObjectState = Entities.ObjectState.Modified OrElse e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Created
        End Function
        <Extension>
        Public Function HasChanges(ByVal e As IEntity) As Boolean
            If e.ObjectState = Entities.ObjectState.Modified OrElse e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Created Then
                Return True
            Else
                Dim r As IRelations = TryCast(e, IRelations)
                If r IsNot Nothing Then
                    Return r.HasChanges
                End If
            End If
            Return False
        End Function
        <Extension>
        Public Function GetPKValues(ByVal e As IEntity, ByVal oschema As IEntitySchema) As IPKDesc
            Dim op As IOptimizePK = TryCast(e, IOptimizePK)
            If op IsNot Nothing Then
                Return op.GetPKValues()
            Else
                If oschema Is Nothing Then
                    Dim mpe As ObjectMappingEngine = e.GetMappingEngine
                    oschema = e.GetEntitySchema(mpe)
                End If
                Return oschema.GetPKs(CType(e, Object))
            End If
        End Function
        <Extension>
        Public Function Clone(ByVal e As _IEntity, ByVal oschema As IEntitySchema) As IEntity
            If e Is Nothing Then
                Throw New ArgumentNullException(NameOf(e))
            End If

            Using mc As IGetManager = e.GetMgr()
                Dim t As IEntity = Nothing
                If mc Is Nothing Then
                    t = CType(Activator.CreateInstance(e.GetType), _IEntity)
                Else
                    t = mc.Manager.CreateEntity(e.GetType)
                End If

                OrmManager.CopyBody(e, t, oschema)

                Return t
            End Using

        End Function

        <Extension>
        Public Function Clone(ByVal e As _ICachedEntity, pk As IPKDesc, ByVal oschema As IEntitySchema) As _ICachedEntity
            If e Is Nothing Then
                Throw New ArgumentNullException(NameOf(e))
            End If

            Using mc As IGetManager = e.GetMgr()
                Dim t As _ICachedEntity = Nothing
                Dim mpe As ObjectMappingEngine = Nothing
                If mc Is Nothing Then
                    t = CType(Activator.CreateInstance(e.GetType), _ICachedEntity)
                Else
                    t = CType(mc.Manager.CreateObject(pk, e.GetType), _ICachedEntity)
                    mpe = mc.Manager.MappingEngine
                End If

                OrmManager.CopyBody(e, t, oschema)

                OrmManager.SetPK(t, pk, oschema, mpe)

                Return t
            End Using

        End Function

        <Extension>
        Public Function Clone(Of T As {New, _ICachedEntity})(ByVal e As T, pk As IPKDesc, ByVal oschema As IEntitySchema) As T
            If e Is Nothing Then
                Throw New ArgumentNullException(NameOf(e))
            End If

            Using mc As IGetManager = e.GetMgr()
                Dim target As T = Nothing
                Dim mpe As ObjectMappingEngine = Nothing
                If mc Is Nothing Then
                    target = New T
                Else
                    target = mc.Manager.CreateObject(Of T)(pk)
                    mpe = mc.Manager.MappingEngine
                End If

                OrmManager.CopyBody(e, target, oschema)

                OrmManager.SetPK(target, pk, oschema, mpe)

                Return target
            End Using

        End Function
    End Module
End Namespace