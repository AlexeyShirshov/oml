Module Module1

    Sub Main()
        Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)
        Dim ctx As New Model.Entities(conn)

        Using ts As New Transactions.TransactionScope

            Dim t1 As Model.Table1 = ctx.Table1.First(Function(t) (t.id = 1))
            Dim t2 As Model.Table2 = ctx.Table2.First(Function(t) (t.id = 1))

            Try
                Console.WriteLine(t1.EntityState.ToString)

                t1.code = 10
                Console.WriteLine(t1.EntityState.ToString)

                t2.m = 1000

                ctx.SaveChanges()
            Finally
                Console.WriteLine(t1.code)
                Console.WriteLine(t1.EntityState.ToString)
            End Try
        End Using
    End Sub

    Sub Main1()
        Dim conn As New EntityClient.EntityConnection(Configuration.ConfigurationManager.ConnectionStrings("Entities").ConnectionString)

        Dim ctx As New Model.Entities(conn)

        For Each tbl In From t In ctx.Table1 Select t
            Console.WriteLine(tbl.name)
        Next
    End Sub

End Module
