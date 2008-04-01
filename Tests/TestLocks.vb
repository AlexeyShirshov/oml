Imports Worm.Database

Module TestLocks
    Sub main()
        Using mgr As OrmDBManager = CreateManager()
            Dim r As New Random
            Randomize()
            Dim min As Integer, max As Integer
            GetMinMax(mgr, min, max)
            Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                Do
                    Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                    Do While t Is Nothing
                        t = mgr.Find(Of TestEditTable)(r.Next(min, max))
                    Loop
                    t.Name = Guid.NewGuid.ToString
                Loop While st.Saver.AffectedObjects.Count < 2
                st.Commit()
            End Using
        End Using
    End Sub
End Module
