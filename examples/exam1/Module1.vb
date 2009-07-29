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
        Return New OrmDBManager(New OrmCache, New ObjectMappingEngine("1"), New SQLGenerator, _
            "Server=.\sqlexpress;AttachDBFileName='" & _
            IO.Path.GetFullPath(String.Format(My.Settings.pathToDatabase, Environment.CurrentDirectory)) & _
            "';User Instance=true;Integrated security=true;")
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
        Dim albums As ICollection(Of test.Album) = New QueryCmd(AddressOf GetDBManager) _
            .Where(Ctor.prop(GetType(test.Album), "Identifier").eq(20)) _
            .ToList(Of test.Album)()
    End Sub

    Sub Main3()
        Dim albums As ICollection(Of test.Album) = New QueryCmd(AddressOf GetDBManager) _
                .Where(Ctor.prop(GetType(test.Album), "Name").like("love%")) _
                .ToList(Of test.Album)()
    End Sub

    Sub Main4()
        Dim album_type As Type = GetType(test.Album)

        Dim albums As ICollection(Of test.Album) = New QueryCmd(AddressOf GetDBManager) _
            .Where(Ctor.prop(album_type, "Release").greater_than(New Date(Now.Year, 1, 1))) _
            .OrderBy(SCtor.prop(album_type, "Release").desc) _
            .ToList(Of test.Album)()
    End Sub
End Module
