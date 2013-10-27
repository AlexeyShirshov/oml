Imports Worm.Cache
Imports Worm.Database

Namespace Query.Database
    Public Class MSSQLQueryCmd
        Inherits QueryCmd

        Public Sub New(ByVal connectionString As String, ByVal mpe As ObjectMappingEngine)
            MyBase.new()
            _getMgr = New CreateManager(Function() New OrmReadOnlyDBManager(connectionString, mpe))
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal mpe As ObjectMappingEngine, ByVal cache As CacheBase)
            MyBase.new()
            _getMgr = New CreateManager(Function() New OrmReadOnlyDBManager(connectionString, mpe, New Worm.Database.SQL2000Generator, cache))
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal mpe As ObjectMappingEngine, ByVal cache As CacheBase, generator As Worm.Database.DbGenerator)
            MyBase.new()
            _getMgr = New CreateManager(Function() New OrmReadOnlyDBManager(connectionString, mpe, generator, cache))
        End Sub

        Public Sub New(ByVal createConnection As Func(Of Data.Common.DbConnection), ByVal mpe As ObjectMappingEngine, ByVal cache As CacheBase, generator As Worm.Database.DbGenerator)
            MyBase.new()
            _getMgr = New CreateManager(Function() New OrmReadOnlyDBManager(createConnection, mpe, generator, cache))
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal cache As CacheBase)
            MyBase.new()
            _getMgr = New CreateManager(Function() New OrmReadOnlyDBManager(connectionString, Worm.Database.OrmReadOnlyDBManager.DefaultMappingEngine, New Worm.Database.SQL2000Generator, cache))
        End Sub

        Public Sub New(ByVal connectionString As String)
            MyClass.New(connectionString, New ReadonlyCache)
        End Sub
    End Class

End Namespace