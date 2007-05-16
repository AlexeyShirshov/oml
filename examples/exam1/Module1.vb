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
            Dim firstAlbum As New test.Albums(someTempIdentifier, mgr.Cache, mgr.ObjectSchema)

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


End Module
