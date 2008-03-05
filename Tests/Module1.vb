Imports Worm.Database
Imports Worm.Cache

Module Module1

    Class testctx
        Inherits Microsoft.VisualStudio.TestTools.UnitTesting.TestContext

        Public Overrides Sub AddResultFile(ByVal fileName As String)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub BeginTimer(ByVal timerName As String)
            Throw New NotImplementedException
        End Sub

        Public Overrides ReadOnly Property DataConnection() As System.Data.Common.DbConnection
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides ReadOnly Property DataRow() As System.Data.DataRow
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides Sub EndTimer(ByVal timerName As String)
            Throw New NotImplementedException
        End Sub

        Public Overrides ReadOnly Property Properties() As System.Collections.IDictionary
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides Sub WriteLine(ByVal format As String, ByVal ParamArray args() As Object)
            Throw New NotImplementedException
        End Sub

        Public Overrides ReadOnly Property TestDir() As String
            Get
                Return IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(IO.Directory.GetCurrentDirectory))) & "\TestResults\alex_ALEX-COMP 2007-01-10 10_45_39"
            End Get
        End Property
    End Class

    Sub main()
        Using mgr As OrmReadOnlyDBManager = New OrmDBManager(New OrmCache, New SQLGenerator("1"), "Data Source=vs2\sqlmain;Initial catalog=Wormtest;Integrated security=true;")
            For i As Integer = 0 To 10000
                mgr.Find(Of TestProject1.Table1)(Criteria.Ctor.Field(GetType(TestProject1.Table1), "ID").Eq(i + 1000), Nothing, False)
                If i Mod 1000 = 0 Then
                    Console.WriteLine(i / 1000)
                End If
            Next
            Dim t As New TestProject1.Table1(1000, mgr.Cache, mgr.DbSchema)
            t.CreatedAt = Now
            mgr.BeginTransaction()
            Try
                t.Save(True)
            Finally
                mgr.Rollback()
            End Try
        End Using
    End Sub

    Sub Main4()
        Dim tc As New TestProject1.TestCache
        tc.TestContext = New testctx
        tc.Write2Console = False
        For i As Integer = 0 To 10
            tc.StressTest()
            Console.WriteLine(i)
        Next
    End Sub

    Sub Main3()
        Dim t As New TestProject1.TestManagerRS
        Dim t2 As New TestProject1.TestManager
        t.WithLoad = True
        't.SharedCache = True
        For i As Integer = 0 To 1000
            t.TestSave()
            t.TestValidateCache()
            t.TestValidateCache2()
            t.TestFindField()
            t.TestXmlField()
            t.TestAddBlob()
            t.TestLoadGUID()
            t.TestAddWithPK()
            t.TestAddWithPK2()
            t.TestSwitchCache()
            't.TestSwitchCache2()
            t.TestDeleteFromCache()
            t.TestComplexM2M()
            t.TestComplexM2M2()
            t.TestComplexM2M3()
            t.TestLoadObjects()


            't2.TestAdd()
            't2.TestAdd2()
            't2.TestAdd3()
            't2.TestAdd4()
            't2.TestDelete2()
            't2.TestFilter()
            't2.TestLoad()
            't2.TestLoad2()
            't2.TestLoadWithAlter()
            't2.TestLoadWithAlter()
            't2.TestM2M()
            't2.TestM2M2()
            't2.TestM2M3()
            't2.TestM2M4()
            't2.TestM2M5()
            't2.TestM2M6()
            't2.TestM2M7()
            't2.TestM2M8()
            't2.TestM2MAdd()
            't2.TestM2MAdd2()
            't2.TestM2MAdd3()
            't2.TestM2MDelete()
            't2.TestM2MDelete2()
            't2.TestM2MReject()
            't2.TestM2MReject3()
            't2.TestM2MReject4()
            't2.TestM2MTag()
            't2.TestM2MTag2()
            't2.TestM2MTag3()

            Console.WriteLine(i)
        Next
    End Sub

    Sub Main2()
        withoutload()
        withload()
    End Sub

    Sub withoutload()
        Using mc As Worm.OrmManagerBase = TestProject1.TestManager.CreateManager(New SQLGenerator("1"))
            For i As Integer = 0 To 100
                Dim c As Worm.ReadOnlyList(Of TestProject1.Entity2) = mc.FindTop(Of TestProject1.Entity2)(100, Nothing, Nothing, False)
                mc.LoadObjects(c)
            Next
        End Using
    End Sub

    Sub withload()
        Using mc As Worm.OrmManagerBase = TestProject1.TestManager.CreateManager(New SQLGenerator("1"))
            For i As Integer = 0 To 100
                Dim c As Generic.ICollection(Of TestProject1.Entity2) = mc.FindTop(Of TestProject1.Entity2)(100, Nothing, Nothing, True)
            Next
        End Using
    End Sub

End Module
