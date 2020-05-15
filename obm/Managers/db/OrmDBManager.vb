Imports Worm
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports cc = Worm.Criteria.Core

Namespace Database
    Public Class OrmDBManager
        Inherits OrmReadOnlyDBManager

        Private _upd As New DBUpdater
        Private _idstr As String

        Public Sub New(ByVal createConnection As Func(Of Data.Common.DbConnection), ByVal mpe As ObjectMappingEngine, ByVal stmtGen As DbGenerator, ByVal cache As OrmCache)
            MyBase.New(createConnection, mpe, stmtGen, cache)
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal mpe As ObjectMappingEngine, ByVal stmtGen As DbGenerator, ByVal cache As OrmCache)
            MyBase.New(connectionString, mpe, stmtGen, cache)
        End Sub

        Protected Sub New(ByVal connectionString As String, ByVal mpe As ObjectMappingEngine, ByVal stmtgen As DbGenerator)
            MyBase.New(connectionString, mpe, stmtgen)
        End Sub

        Protected Friend Overrides ReadOnly Property IdentityString() As String
            Get
                If String.IsNullOrEmpty(_idstr) Then
                    If Not String.IsNullOrEmpty(_connStr) Then
                        _idstr = GetType(OrmReadOnlyDBManager).ToString & _connStr
                    Else
                        Return GetType(OrmReadOnlyDBManager).ToString '& _connStr
                    End If
                End If

                Return _idstr
            End Get
        End Property

        Public Overrides Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean
            Return _upd.UpdateObject(Me, obj)
        End Function

        Protected Overrides Function InsertObject(ByVal obj As _ICachedEntity) As Boolean
            Return _upd.InsertObject(Me, obj)
        End Function

        Protected Overrides Sub M2MSave(ByVal obj As ICachedEntity, ByVal t As Type, ByVal direct As String, ByVal el As M2MRelation)
            _upd.M2MSave(Me, obj, t, direct, el)
        End Sub

        Protected Friend Overrides Sub DeleteObject(ByVal obj As ICachedEntity)
            _upd.DeleteObject(Me, obj)
        End Sub

        Public Function Delete(f As cc.IEntityFilter, limit As Integer) As Integer
            Return _upd.Delete(Me, f, limit)
        End Function
        Public Function Delete(ByVal f As cc.IEntityFilter) As Integer
            Return _upd.Delete(Me, f)
        End Function

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace
