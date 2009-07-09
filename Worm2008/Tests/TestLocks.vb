Imports Worm.Database
Imports Worm.Query

Module TestLocks
    Sub main()
        Using mgr As OrmDBManager = CreateManager()
            Dim r As New Random
            Randomize()
            Dim min As Integer, max As Integer
            GetMinMax(mgr, min, max)
            Using st As New ModificationsTracker(mgr)
                Do
                    Dim t As TestEditTable = New QueryCmd().GetByID(Of TestEditTable)(r.Next(min, max), mgr)
                    Do While t Is Nothing
                        t = New QueryCmd().GetByID(Of TestEditTable)(r.Next(min, max), mgr)
                    Loop
                    t.Name = Guid.NewGuid.ToString
                Loop While st.Saver.AffectedObjects.Count < 2
                st.AcceptModifications()
            End Using
        End Using
    End Sub
End Module
