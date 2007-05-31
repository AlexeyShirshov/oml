Imports Worm.Orm

Module Module1

    ''' <summary>
    ''' Create database manager - the gateway to database
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>The function creates instance of OrmDBManager class and pass to ctor new Cache, new database schema with version 1 and connection string</remarks>
    Function GetDBManager() As OrmDBManager
        Return New OrmDBManager(New OrmCache, New DbSchema("1"), My.Settings.connectionString)
    End Function

    Sub Main()
        Using mgr As OrmDBManager = GetDBManager()
            'create in-memory object
            'it is a simple object that have no relation to database at all
            Dim someTempIdentifier As Integer = -100
            Dim firstAlbum As New test.Album(someTempIdentifier, mgr.Cache, mgr.ObjectSchema)

            'set properties
            firstAlbum.Name = "firstAlbum"
            firstAlbum.Release = CDate("2005-01-01")

            'create transaction
            mgr.BeginTransaction()
            Try
                'ok. save it
                'we pass true to Save parameter to accept changes immediately after saving into database
                firstAlbum.Save(True)
            Finally
                'rollback transaction to undo database changes
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Sub Main2()
        Using mgr As OrmDBManager = GetDBManager()

            Dim sort As Sort = Nothing
            Dim album_type As Type = GetType(test.Album)

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                New Criteria(album_type).Field("ID").Eq(20), sort, True)
        End Using
    End Sub

    Sub Main3()
        Using mgr As OrmDBManager = GetDBManager()

            Dim sort As Sort = Nothing
            Dim album_type As Type = GetType(test.Album)

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                New Criteria(album_type).Field("Name").Like("love%"), sort, True)
        End Using
    End Sub

    Sub Main4()
        Using mgr As OrmDBManager = GetDBManager()

            Dim sort As Sort = Nothing
            Dim album_type As Type = GetType(test.Album)

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                New Criteria(album_type).Field("Release").GreaterThan(New Date(Now.Year, 1, 1)), _
                Sorting.Field("Release").Desc, True)
        End Using
    End Sub
End Module
