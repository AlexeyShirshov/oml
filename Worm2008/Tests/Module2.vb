Imports Worm.Database
Imports Worm.Cache
Imports Worm
Imports Worm.Database.OrmReadOnlyDBManager
Imports Worm.Criteria
Imports Worm.Query

Module Module2

    Public Class States
        Public FromState As Entities.ObjectState
        Public ToState As Entities.ObjectState
        Public Dt As Date
    End Class

    Public Class StatesList
        Inherits List(Of States)

        Public Overloads Sub Add(ByVal oldState As Entities.ObjectState, ByVal newState As Entities.ObjectState)
            Dim os As New States
            os.Dt = Now
            os.FromState = oldState
            os.ToState = newState
            Add(os)
        End Sub
    End Class

    <Entities.Meta.Entity("junk", "id", "1")> _
    Public Class TestEditTable
        Inherits Entities.OrmBaseT(Of TestEditTable)
        Implements Entities.IOrmEditable(Of TestEditTable)

        Private _dt As Date
        Public ReadOnly Property Dt() As Date
            Get
                Return _dt
            End Get
        End Property

        Private _stack As String
        Public ReadOnly Property StackTrace() As String
            Get
                Return _stack
            End Get
        End Property

        Private _changes As New StatesList

        Public ReadOnly Property StateChanges() As StatesList
            Get
                Return _changes
            End Get
        End Property

        Protected Sub ChangeState(ByVal state As Worm.Entities.ObjectState)
            _changes.Add(state, ObjectState)
        End Sub

        Protected _deleted As Boolean
        Public ReadOnly Property ObjectDeleted() As Boolean
            Get
                Return _deleted
            End Get
        End Property

        Protected Sub OnDeleted(ByVal o As Entities.KeyEntity, ByVal e As EventArgs)
            _deleted = True
        End Sub

        Protected _s As Entities.ObjectState
        Public ReadOnly Property SavingState() As Entities.ObjectState
            Get
                Return _s
            End Get
        End Property

        Public Sub ObjectSaving(ByVal sender As ObjectListSaver, ByVal args As ObjectListSaver.CancelEventArgs)
            _s = args.SavedObject.ObjectState
        End Sub

        'Protected _rs As Orm.ObjectState
        'Public ReadOnly Property RejectedState() As Orm.ObjectState
        '    Get
        '        Return _rs
        '    End Get
        'End Property

        'Protected Sub Rejected(ByVal state As Worm.Orm.ObjectState)
        '    _rs = state
        'End Sub

        Public Sub New()

        End Sub

        'Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator)
        '    MyBase.New(id, cache, schema)
        'End Sub

        Protected Overrides Sub Init()
            _stack = Environment.StackTrace
            _dt = Now

#If DEBUG Then
            AddHandler ObjectStateChanged, AddressOf ChangeState
#End If

            'AddHandler ObjectRejected, AddressOf Rejected
        End Sub

        Private _name As String
        <Entities.Meta.EntityPropertyAttribute(column:="name")> Public Property Name() As String
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
        <Entities.Meta.EntityPropertyAttribute(column:="code")> Public Property Code() As Nullable(Of Integer)
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

        Public Sub CopyBodys(ByVal from As TestEditTable, ByVal [to] As TestEditTable) Implements Worm.Entities.IOrmEditable(Of TestEditTable).CopyBody
            With [to]
                ._name = from._name
                ._code = from._code
            End With
        End Sub
    End Class

    Private _cache As New OrmCache
    Private _schema As New Worm.ObjectMappingEngine("1")
    Private _exCount As Integer
    Private _identity As Integer = -100
    Private _deleted As ArrayList = ArrayList.Synchronized(New ArrayList)
    Private _gdeleted As ArrayList = ArrayList.Synchronized(New ArrayList)

    Private Const iterCount As Integer = 1000
    Private Const threadCount As Integer = 10

    <Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.Synchronized)> _
    Function GetIdentity() As Integer
        Dim i As Integer = _identity
        _identity -= 1
        Return i
    End Function

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
        For i As Integer = 0 To threadCount
            Dim t As New Threading.Thread(AddressOf EditSub)
            trd.Add(t)
            t = New Threading.Thread(AddressOf QuerySub)
            trd.Add(t)
            t = New Threading.Thread(AddressOf QuerySub2)
            trd.Add(t)
            t = New Threading.Thread(AddressOf Load)
            trd.Add(t)
            t = New Threading.Thread(AddressOf Unload)
            trd.Add(t)
            t = New Threading.Thread(AddressOf DeleteSub)
            trd.Add(t)
            t = New Threading.Thread(AddressOf AddSub)
            trd.Add(t)

            'Dim t As New Threading.Thread(AddressOf AddSub)
            'trd.Add(t)
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

    Public Sub GetMinMax(ByVal mgr As OrmReadOnlyDBManager, ByRef min As Integer, ByRef max As Integer)
        Dim d As Dictionary(Of String, Object) = Storedprocs.NonQueryStoredProcBase.Exec(mgr, "GetMinMax", "min,max")
        min = CInt(d("min"))
        max = CInt(d("max"))
    End Sub

    Sub Load(ByVal o As Object)
        For i As Integer = 0 To iterCount
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Dim min As Integer, max As Integer, cnt As Integer
                        GetMinMax(mgr, min, max)
                        Dim l As New List(Of Object)
                        Do
                            Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                            If t IsNot Nothing AndAlso r.NextDouble > 0.5 Then
                                l.Add(t.Identifier)
                                cnt += 1
                            End If
                        Loop While cnt < 10
                        mgr.LoadObjectsIds(Of TestEditTable)(l)
                        done = True
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} load: {1}", o, i))
            End If
        Next
    End Sub

    Sub Unload(ByVal o As Object)
        For i As Integer = 0 To iterCount
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Dim min As Integer, max As Integer, cnt As Integer
                        GetMinMax(mgr, min, max)
                        Do
                            Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                            If t IsNot Nothing AndAlso r.NextDouble > 0.5 Then
                                mgr.RemoveObjectFromCache(t)
                                cnt += 1
                            End If
                        Loop While cnt < 10
                        done = True
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} unload: {1}", o, i))
            End If
        Next
    End Sub

    Private _saved As ArrayList = ArrayList.Synchronized(New ArrayList)
    Sub ObjectSaved(ByVal sebder As ObjectListSaver, ByVal o As Entities.ICachedEntity)
        _saved.Add(o)
    End Sub

    Sub DeleteSub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("edit sub done")
        'e.Set()
        For i As Integer = 0 To CInt(iterCount * 1.5)
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Dim min As Integer, max As Integer
                        GetMinMax(mgr, min, max)
                        Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                        If t IsNot Nothing Then
                            Using sw As New IO.StringWriter
                                Dim ls As New TextWriterTraceListener(sw)
                                mgr.AddListener(ls)
                                Try
                                    Using st As New ModificationsTracker(mgr)
                                        Using t.BeginAlter
                                            'If t.InternalProperties.ObjectState <> Orm.ObjectState.Deleted Then
                                            done = t.Delete()
                                            Debug.Assert(Not done OrElse t.InternalProperties.ObjectState = Entities.ObjectState.Deleted)
                                            If Not done Then
                                                Debug.Assert(st.Saver.AffectedObjects.Count = 0)
                                                Dim oc As ObjectModification = mgr.Cache.ShadowCopy(t)
                                                Debug.Assert(oc IsNot Nothing OrElse t.InternalProperties.ObjectState = Entities.ObjectState.NotFoundInSource)
                                                Debug.Assert(oc Is Nothing OrElse oc.Reason = ObjectModification.ReasonEnum.Delete)
                                            End If
                                            'End If
                                        End Using
                                        If done Then
                                            Debug.Assert(st.Saver.AffectedObjects.Count > 0)
                                            Debug.Assert(t.InternalProperties.OriginalCopy IsNot Nothing)
                                        End If
                                        'Debug.Assert(Not done OrElse st.Saver.AffectedObjects.Count > 0)
                                        'Debug.Assert(Not done OrElse t.InternalProperties.ObjectState = Orm.ObjectState.Deleted)
                                        st.AcceptModifications()
                                        Debug.Assert(Not done OrElse t.InternalProperties.ObjectState = Entities.ObjectState.Deleted)
                                        AddHandler st.Saver.ObjectSaving, AddressOf t.ObjectSaving
                                        AddHandler st.Saver.ObjectSaved, AddressOf ObjectSaved
                                    End Using
                                    If done Then
                                        Debug.Assert(t.InternalProperties.OriginalCopy Is Nothing)
                                        Dim s, s2, s3 As String
                                        If t.InternalProperties.ObjectState <> Entities.ObjectState.Deleted Then
                                            s = sw.GetStringBuilder.ToString
                                        End If
                                        Debug.Assert(t.InternalProperties.ObjectState = Entities.ObjectState.Deleted)
                                        _deleted.Add(t)
                                        Dim b As Boolean = mgr.IsInCachePrecise(t)
                                        Debug.Assert(Not b)
                                    End If
                                Finally
                                    mgr.RemoveListener(ls)
                                End Try
                            End Using
                        End If
                        done = False
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} delete: {1}", o, i))
            End If
        Next
    End Sub

    Sub AddSub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("edit sub done")
        'e.Set()
        For i As Integer = 0 To CInt(iterCount * 0.5)
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Using st As New ModificationsTracker(mgr)
                            'Dim t As New TestEditTable(GetIdentity, mgr.Cache, mgr.ObjectSchema)
                            Dim t As TestEditTable = mgr.CreateOrmBase(Of TestEditTable)(GetIdentity)
                            t.Name = Guid.NewGuid.ToString
                            If r.NextDouble > 0.3 Then
                                t.Code = r.Next(1000)
                            End If
                            st.Add(t)
                            st.AcceptModifications()
                        End Using
                        done = True
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} add: {1}", o, i))
            End If
        Next
    End Sub

    Sub EditSub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("edit sub done")
        'e.Set()
        For i As Integer = 0 To iterCount
            Using mgr As OrmDBManager = CreateManager()
                Dim r As New Random
                Dim done As Boolean
                Do
                    Try
                        Dim min As Integer, max As Integer
                        GetMinMax(mgr, min, max)
                        Using st As New ModificationsTracker(mgr)
                            Do
                                Dim t As TestEditTable = mgr.Find(Of TestEditTable)(r.Next(min, max))
                                If t IsNot Nothing Then
                                    Using t.BeginAlter
                                        If t.InternalProperties.CanEdit Then
                                            If r.NextDouble > 0.5 Then
                                                t.Name = Guid.NewGuid.ToString
                                                If r.NextDouble > 0.5 Then
                                                    AddHandler st.Saver.ObjectSaved, AddressOf throwEx
                                                End If
                                            Else
                                                Dim str As String = Guid.NewGuid.ToString
                                                t.Name = str
                                                If t.InternalProperties.HasBodyChanges Then
                                                    t.SaveChanges(True)
                                                End If
                                            End If
                                        End If
                                    End Using
                                End If
                                If st.Saver.AffectedObjects.Count > 4 Then Exit For
                            Loop While True
                            st.AcceptModifications()
                        End Using
                        done = True
                    Catch ex As OrmManagerException When ex.InnerException IsNot Nothing AndAlso ex.InnerException.Message = "xxx"
                    Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
                        Threading.Interlocked.Increment(_exCount)
                    End Try
                Loop While Not done
            End Using
            If i Mod 10 = 0 Then
                Console.WriteLine(String.Format("thread: {0} edit: {1}", o, i))
            End If
        Next
    End Sub

    Sub throwEx(ByVal sender As ObjectListSaver, ByVal o As Entities.ICachedEntity)
        Throw New Exception("xxx")
    End Sub

    Sub QuerySub(ByVal o As Object)
        'Dim arr As ArrayList = CType(o, ArrayList)
        'Dim e As New Threading.AutoResetEvent(False)
        'arr.Add(e)
        'Console.WriteLine("query sub done")
        'e.Set()
        For i As Integer = 0 To iterCount * 2
            Using mgr As OrmReadOnlyDBManager = CreateManager()
                Using New OrmManager.CacheListBehavior(mgr, False)
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
                            Threading.Thread.Sleep(0)
                        Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                            Threading.Interlocked.Increment(_exCount)
                        Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
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
        For i As Integer = 0 To iterCount * 2
            Using mgr As OrmReadOnlyDBManager = CreateManager()
                Using New OrmManager.CacheListBehavior(mgr, False)
                    Dim r As New Random
                    Dim done As Boolean
                    Do
                        Try
                            If r.NextDouble > 0.5 Then
                                mgr.Find(Of TestEditTable)(Ctor.prop(GetType(TestEditTable), "Name").[like]("e%"), Nothing, False)
                            Else
                                mgr.Find(Of TestEditTable)(Ctor.prop(GetType(TestEditTable), "Name").[like]("e%"), Nothing, True)
                            End If
                            done = True
                            Threading.Thread.Sleep(0)
                        Catch ex As InvalidOperationException When ex.Message.Contains("Timeout expired")
                            Threading.Interlocked.Increment(_exCount)
                        Catch ex As SqlClient.SqlException When ex.Message.Contains("Timeout expired")
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

    Public Function CreateManager() As OrmDBManager
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(IO.Directory.GetCurrentDirectory, "..\..\..\TestProject1\Databases\test.mdf"))
        Return New OrmDBManager(_cache, _schema, New SQLGenerator, "Server=.\sqlexpress;AttachDBFileName='" & path & "';User Instance=true;Integrated security=true;Connection timeout=60;")
        'Return New OrmDBManager(_cache, _schema, "data source=.\sqlexpress;Initial catalog=test;Integrated security=true;Connection timeout=60;")
    End Function
End Module
