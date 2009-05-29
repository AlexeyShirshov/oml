Imports Worm.Cache

Namespace Query
    Public MustInherit Class WormContext
        Implements ICreateManager

        Public Delegate Function CreateManagerDelegate(ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine, ByVal stmtGen As StmtGenerator) As OrmManager

        Protected _del As CreateManagerDelegate

        Private _schema As ObjectMappingEngine
        Public Property MappingEngine() As ObjectMappingEngine
            Get
                Return _schema
            End Get
            Set(ByVal value As ObjectMappingEngine)
                _schema = value
            End Set
        End Property

        Private _cache As Cache.CacheBase
        Public Property Cache() As Cache.CacheBase
            Get
                Return _cache
            End Get
            Set(ByVal value As Cache.CacheBase)
                _cache = value
            End Set
        End Property

        Private _gen As StmtGenerator
        Public Property StmtGenerator() As StmtGenerator
            Get
                Return _gen
            End Get
            Set(ByVal value As StmtGenerator)
                _gen = value
            End Set
        End Property

        Public Sub New()
            MyClass.New(New ReadonlyCache)
        End Sub

        Public Sub New(ByVal cache As CacheBase)
            MyClass.New(cache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine)
            MyClass.New(cache, New ObjectMappingEngine("1"), New Worm.Database.SQLGenerator)
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate)
            MyClass.New(createDelegate, New ReadonlyCache)
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate, ByVal cache As CacheBase)
            MyClass.New(createDelegate, cache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine)
            MyClass.New(cache, New ObjectMappingEngine("1"), New Worm.Database.SQLGenerator)
            _del = createDelegate
        End Sub

        Public Sub New(ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine, ByVal stmtGen As StmtGenerator)
            _cache = cache
            _gen = stmtGen
            _schema = mpe
        End Sub

        Public Sub New(ByVal stmtGen As StmtGenerator)
            _gen = stmtGen
        End Sub

        Public MustOverride Function CreateManager() As OrmManager Implements ICreateManager.CreateManager
    End Class

    Public Class WormDBContext
        Inherits WormContext

        Private _conn As String

        Public Sub New(ByVal connectionString As String)
            MyClass.New(connectionString, New ReadonlyCache)
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal cache As CacheBase)
            MyClass.New(connectionString, cache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal stmtGen As Worm.Database.SQLGenerator)
            MyBase.New(stmtGen)
            _conn = connectionString
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine)
            MyClass.New(connectionString, cache, New ObjectMappingEngine("1"), New Worm.Database.SQLGenerator)
        End Sub

        Public Sub New(ByVal connectionString As String, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine, ByVal stmtGen As Worm.Database.SQLGenerator)
            MyBase.New(cache, mpe, stmtGen)
            _conn = connectionString
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate)
            MyBase.New(createDelegate, New ReadonlyCache)
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate, ByVal cache As CacheBase)
            MyBase.New(createDelegate, cache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal createDelegate As CreateManagerDelegate, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine)
            MyBase.New(cache, New ObjectMappingEngine("1"), New Worm.Database.SQLGenerator)
        End Sub

        Public Overrides Function CreateManager() As OrmManager
            If _del IsNot Nothing Then
                Return _del(Cache, MappingEngine, StmtGenerator)
            Else
                Return New Worm.Database.OrmReadOnlyDBManager(Cache, MappingEngine, CType(StmtGenerator, Worm.Database.SQLGenerator), _conn)
            End If
        End Function

        Public ReadOnly Property ConnectionString() As String
            Get
                Return _conn
            End Get
        End Property
    End Class
End Namespace