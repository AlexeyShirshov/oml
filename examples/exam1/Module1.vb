Imports Worm.Entities
Imports Worm.Database
Imports Worm.Cache
Imports Worm
Imports Worm.Query

Module Module1

    ''' <summary>
    ''' Create database manager - the gateway to database
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>The function creates instance of OrmDBManager class and pass to ctor new Cache, new database schema with version 1 and connection string</remarks>
    Function GetDBManager() As OrmDBManager
        Return New OrmDBManager(New OrmCache, New ObjectMappingEngine("1"), New SQLGenerator, My.Settings.connectionString)
    End Function

    Sub Main()
        Using mgr As OrmDBManager = GetDBManager()
            'create in-memory object
            'it is a simple object that have no relation to database at all
            Dim firstAlbum As New test.Album

            'set properties
            firstAlbum.Name = "firstAlbum"
            firstAlbum.Release = CDate("2005-01-01")

            'create transaction
            mgr.BeginTransaction()
            Try
                'ok. save it
                'we pass true to Save parameter to accept changes immediately after saving into database
                firstAlbum.SaveChanges(True)
            Finally
                'rollback transaction to undo database changes
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Sub Main2()
        Using mgr As OrmDBManager = GetDBManager()

            Dim sort As Sorting.Sort = Nothing
            Dim album_type As Type = GetType(test.Album)

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                Ctor.prop(album_type, "ID").eq(20), sort, True)
        End Using
    End Sub

    Sub Main3()
        Using mgr As OrmDBManager = GetDBManager()

            Dim sort As Sorting.Sort = Nothing
            Dim album_type As Type = GetType(test.Album)

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                Ctor.prop(album_type, "Name").like("love%"), sort, True)
        End Using
    End Sub

    Sub Main4()
        Using mgr As OrmDBManager = GetDBManager()

            Dim album_type As Type = GetType(test.Album)
            Dim sort As Sorting.Sort = SCtor.prop(album_type, "Release").desc

            Dim albums As ICollection(Of test.Album) = mgr.Find(Of test.Album)( _
                Ctor.prop(album_type, "Release").greater_than(New Date(Now.Year, 1, 1)), _
                sort, True)
        End Using
    End Sub
End Module
