Imports Worm.Database
Imports Worm.Cache
Imports Worm
Imports Worm.Database.Criteria

Module Module2

    <Orm.Meta.Entity("junk", "id", "1")> _
    Class TestEditTable
        Inherits Orm.OrmBaseT(Of TestEditTable)
        Implements Orm.Meta.IOrmEditable(Of TestEditTable)

        Private _name As String
        <Orm.Meta.Column(column:="name")> Public Property Name() As String
            Get
                Using Read("Name")
                    Return _name
                End Using
            End Get
            Set(ByVal value As String)
                Using Write("Name")
                    _name = value
                End Using
            End Set
        End Property

        Private _code As Nullable(Of Integer)
        <Orm.Meta.Column(column:="code")> Public Property Code() As Nullable(Of Integer)
            Get
                Using Read("Code")
                    Return _code
                End Using
            End Get
            Set(ByVal value As Nullable(Of Integer))
                Using Write("Code")
                    _code = value
                End Using
            End Set
        End Property

        Public Sub CopyBody(ByVal from As TestEditTable, ByVal [to] As TestEditTable) Implements Worm.Orm.Meta.IOrmEditable(Of TestEditTable).CopyBody
            With [to]
                ._name = from._name
                ._code = from._code
            End With
        End Sub
    End Class

    Private _cache As New OrmCache
    Private _schema As New Database.SQLGenerator("1")
    Private _exCount As Integer

    Sub main()
        'Dim arr As ArrayList = ArrayList.Synchronized(New ArrayList)
        'For i As Integer = 0 To 100
        '    Threading.ThreadPool.QueueUserWorkItem(AddressOf EditSub, arr)
        '    Threading.ThreadPool.QueueUserWorkItem(AddressOf QuerySub, arr)
        'Next
        'Threading.WaitHandle.WaitAll(CType(arr.ToArray(GetType(Threading.WaitHandle)), Threading.WaitHandle()))
        Dim n As Date = Now
        Dim trd As New List(Of Threading.Thread)
        Randomize()
        For i As Integer = 0 To 100
            Dim t As New Threading.Thread(AddressOf EditSub)
            trd.Add(t)
            t = New Threading.Thread(AddressOf QuerySub)
            trd.Add(t)
            t = New Threading.Thread(AddressOf QuerySub2)
            trd.Add(t)
        Next
        For i As Integer = 0 To trd.Count - 1
            Dim t As Threading.Thread = trd(i)
            t.Start(i)
        Next
        For Each t As Threading.Thread In trd
            t.Join()
        Next
        Console.WriteLine(Now.Subtract(n).ToString)
        Console.WriteLine(_exCount)
    End Sub

    Sub GetMinMax(ByVal mgr As OrmReadOnlyDBManager, ByRef min As Integer, ByRef max As Integer)
        Dim d As Dictionary(Of String, Object) = Storedprocs.NonQueryStoredProcBase.Exec(mgr, "GetMinMax", "min,max")
        min = CInt(d("min"))
        max = CInt(d("max"))
    End Sub

    Sub EditSub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("edit sub done")
        'e.Set()
        For i As Integer = 0 To 1000
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Dim min As Integer, max As Integer
                        GetMinMax(mgr, min, max)
                        Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                        If t IsNot Nothing Then
                            If r.NextDouble > 0.5 Then
                                Using st As New OrmReadOnlyDBManager.OrmTransactionalScope(mgr)
                                    t.Name = Guid.NewGuid.ToString
                                    st.Commit()
                                End Using
                            Else
                                t.Name = Guid.NewGuid.ToString
                                t.Save(True)
                            End If
                            done = True
                        End If
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} edit: {1}", o, i))
            End If
        Next
    End Sub

    Sub QuerySub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("query sub done")
        'e.Set()
        For i As Integer = 0 To 2000
            Using mgr As OrmReadOnlyDBManager = CreateManager()
                Using New OrmManagerBase.CacheListBehavior(mgr, False)
                    Dim r As New Random
                    Dim done As Boolean
                    Do
                        Try
                            If r.NextDouble > 0.5 Then
                                mgr.FindTop(Of TestEditTable)(100, Nothing, Nothing, False)
                            Else
                                mgr.FindTop(Of TestEditTable)(100, Nothing, Nothing, True)
                            End If
                            done = True
                        Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                            Threading.Interlocked.Increment(_exCount)
                        Catch ex As SqlClient.SqlException
                            Threading.Interlocked.Increment(_exCount)
                        End Try
                    Loop While Not done
                End Using
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} query: {1}", o, i))
            End If
        Next
    End Sub

    Sub QuerySub2(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("query sub done")
        'e.Set()
        For i As Integer = 0 To 2000
            Using mgr As OrmReadOnlyDBManager = CreateManager()
                Using New OrmManagerBase.CacheListBehavior(mgr, False)
                    Dim r As New Random
                    Dim done As Boolean
                    Do
                        Try
                            If r.NextDouble > 0.5 Then
                                mgr.Find(Of TestEditTable)(Ctor.AutoTypeField("Name").Like("e%"), Nothing, False)
                            Else
                                mgr.Find(Of TestEditTable)(Ctor.AutoTypeField("Name").Like("e%"), Nothing, True)
                            End If
                            done = True
                        Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                            Threading.Interlocked.Increment(_exCount)
                        Catch ex As SqlClient.SqlException
                            Threading.Interlocked.Increment(_exCount)
                        End Try
                    Loop While Not done
                End Using
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} query2: {1}", o, i))
            End If
        Next
    End Sub

    Function CreateManager() As OrmDBManager
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
        Return New OrmDBManager(_cache, _schema, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;")
    End Function
End Module
