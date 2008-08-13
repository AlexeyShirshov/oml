Module Module1

    Sub Main()
        'Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)
        'Dim ctx As New Model.Entities(conn)

        'ctx.Table1.Select(Function(t) (t.code <> 100)).ToList()

        'ctx.Table1.Select(Function(t) (t.code <> 100)).ToList()

        'ctx.Table1.Select(Function(t) (t.code <> 100)).ToList()
    End Sub

    Sub Main4()
        Dim ctx As New WormTestDataContext

        ctx.Log = Console.Out

        Dim q = From t In ctx.Table1s.ToList

        q = From t In ctx.Table1s.ToList

        q = From t In ctx.Table1s.ToList

    End Sub

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
        'Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)
        'Dim ctx As New Model.Entities(conn)

        'Using ts As New Transactions.TransactionScope

        '    Dim t1 As Model.Table1 = ctx.Table1.First(Function(t) (t.id = 1))
        '    Dim t2 As Model.Table2 = ctx.Table2.First(Function(t) (t.id = 1))

        '    Try
        '        Console.WriteLine(t1.EntityState.ToString)

        '        t1.code = 10
        '        Console.WriteLine(t1.EntityState.ToString)

        '        t2.m = 1000

        '        Console.WriteLine(ctx.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Count)
        '        ctx.SaveChanges()
        '    Finally
        '        Console.WriteLine(ctx.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Count)
        '        Console.WriteLine(t1.code)
        '        Console.WriteLine(t1.EntityState.ToString)
        '    End Try
        'End Using
    End Sub

    Sub Main1()
        'Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)

        'Dim ctx As New Model.Entities(conn)

        'For Each tbl In From t In ctx.Table1 Select t
        '    Console.WriteLine(tbl.name)
        'Next
    End Sub

End Module
