Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Databases.MySQL
Imports Worm.Database
Imports Worm.Entities.Meta
Imports Worm.Query

<TestClass()> Public Class UnitTest1

    Private Function GetDX() As IDataContext
        Return New DataContext(New MySQLGenerator,
                               Function(stmt As StmtGenerator, mpe As ObjectMappingEngine, cache As Cache.CacheBase)
                                   Return New OrmReadOnlyDBManager(My.Settings.DBConn, mpe, CType(stmt, DbGenerator), cache)
                               End Function)
    End Function
    <TestMethod()> Public Sub TestSelectPages()
        Dim pages As New SourceFragment("pages")

        Dim dx = GetDX()

        Dim l = dx.CreateQuery.From(pages).Select(FCtor.column(pages, "pid").column(pages, "p_name")).ToAnonymList

        Assert.IsNotNull(l)
        Assert.IsTrue(l.Count > 0)
    End Sub

End Class