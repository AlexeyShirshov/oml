Module Module1

    Sub Main()
        Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)
        Dim ctx As New wormtestEntities(conn)

        ctx.efTable1Set.Select(Function(t) (t.code <> 100)).ToList()

        ctx.efTable1Set.Select(Function(t) (t.code <> 100)).ToList()

        ctx.efTable1Set.Select(Function(t) (t.code <> 100)).ToList()
    End Sub

    Sub Main4()
        Dim ctx As New WormTestDataContext

        ctx.Log = Console.Out

        Dim q = From t In ctx.Table1s.ToList

        q = From t In ctx.Table1s.ToList

        q = From t In ctx.Table1s.ToList

        foo(New With {.fdf = 1})
    End Sub

    Function foo(Of Res)(ByVal f As Res) As Expressions.Expression(Of Func(Of Res))
        Return Function() f
    End Function

    Sub Main2()
        Dim ctx As New WormTestDataContext
        Dim t1 = ctx.Table1s.First(Function(t) (t.id = 1))
        Dim t2 = ctx.Table2s.First(Function(t) (t.id = 1))
        Using ts As New Transactions.TransactionScope
            t1.code = 10
            t2.m = 1000
            Try
                Console.WriteLine(ctx.GetChangeSet.Updates.Count)
                ctx.SubmitChanges()
            Finally
                Console.WriteLine(t1.code)

                Dim c = ctx.GetChangeSet()
                Console.WriteLine(c.Updates.Count)
            End Try
        End Using
    End Sub

    Sub Main3()
        Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)
        Dim ctx As New wormtestEntities(conn)

        Using ts As New Transactions.TransactionScope

            Dim t1 As efTable1 = ctx.efTable1Set.First(Function(t) (t.id = 1))
            Dim t2 As efTable2 = ctx.efTable2Set.First(Function(t) (t.id = 1))

            Try
                Console.WriteLine(t1.EntityState.ToString)

                t1.code = 10
                Console.WriteLine(t1.EntityState.ToString)

                t2.m = 1000

                Console.WriteLine(ctx.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Count)
                ctx.SaveChanges()
            Finally
                Console.WriteLine(ctx.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Count)
                Console.WriteLine(t1.code)
                Console.WriteLine(t1.EntityState.ToString)
            End Try
        End Using
    End Sub

    Sub Main1()
        Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)

        Dim ctx As New wormtestEntities(conn)

        For Each tbl In From t In ctx.efTable1Set Select t
            Console.WriteLine(tbl.name)
        Next
    End Sub

End Module
