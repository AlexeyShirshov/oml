Imports System.Web
Imports System.Web.Management
Imports System.Collections.Generic
Imports System.Xml
Imports System.Security.Permissions
Imports Worm.Cache

Namespace Web
    Public Interface IRelationalEventData
        ReadOnly Property Columns() As ICollection(Of String)
        ReadOnly Property CreatedAt() As Date
        Function GetValue(ByVal idx As Integer) As String
    End Interface

    <AspNetHostingPermission(SecurityAction.LinkDemand, Level:=AspNetHostingPermissionLevel.Minimal), AspNetHostingPermission(SecurityAction.InheritanceDemand, Level:=AspNetHostingPermissionLevel.Minimal)> _
    Public Class RelationalProvider
        Inherits BufferedWebEventProvider

        Private _data As List(Of List(Of Pair(Of String)))
        Private _file As String
        Private _useUtc As Boolean

        Private Const PredefinedData As String = "##data##"

        Public Overrides Sub Initialize(ByVal name As String, ByVal config As System.Collections.Specialized.NameValueCollection)
            Dim s As String = config("File")
            If Not String.IsNullOrEmpty(s) Then
                config.Remove("File")
                _file = Hosting.HostingEnvironment.MapPath(s)
            Else
                Throw New System.Configuration.ConfigurationErrorsException("DataSetProvider: attribute File is mandatory")
            End If

            Dim permission As New FileIOPermission(FileIOPermissionAccess.Write Or _
                    FileIOPermissionAccess.Append, GetFileName())
            permission.Demand()

            s = config("UseUtc")
            If Not String.IsNullOrEmpty(s) Then
                config.Remove("Utc")
                Dim b As Boolean
                If Boolean.TryParse(s, b) Then
                    _useUtc = b
                End If
            End If

            ResetData()

            MyBase.Initialize(name, config)
        End Sub

        Public Overrides Sub ProcessEvent(ByVal eventRaised As System.Web.Management.WebBaseEvent)
            For Each ev As WebBaseEvent In TransformEvent(eventRaised)
                If UseBuffering Then
                    MyBase.ProcessEvent(ev)
                Else
                    LogEvent(ev)
                End If
            Next
            If Not UseBuffering Then
                StoreEvents()
            End If
        End Sub

        Public Overrides Sub ProcessEventFlush(ByVal flushInfo As System.Web.Management.WebEventBufferFlushInfo)
            For Each webEvent As WebBaseEvent In flushInfo.Events
                LogEvent(webEvent)
            Next
            StoreEvents()
        End Sub

        Public Overrides Sub Shutdown()
            Flush()
            MyBase.Shutdown()
        End Sub

        Protected Sub ResetData()
            _data = New List(Of List(Of Pair(Of String)))
        End Sub

        Protected Sub StoreEvents()
            Dim xdoc As XmlDocument = GetDocument()
            Dim root As XmlElement = xdoc.DocumentElement
            For Each l As List(Of Pair(Of String)) In _data
                Dim row As XmlElement = xdoc.CreateElement("row")
                root.AppendChild(row)
                AppendRow(row, l)
            Next
            SaveXDoc(xdoc)
            ResetData()
        End Sub

        Protected Sub AppendRow(ByVal row As XmlElement, ByVal l As List(Of Pair(Of String)))
            For Each p As Pair(Of String) In l
                If p.First = PredefinedData Then
                    Dim cdata As XmlCDataSection = row.OwnerDocument.CreateCDataSection(p.Second)
                    row.AppendChild(cdata)
                Else
                    Dim a As XmlAttribute = row.OwnerDocument.CreateAttribute(p.First)
                    a.Value = p.Second
                    row.Attributes.Append(a)
                End If
            Next
        End Sub

        Protected Sub LogEvent(ByVal eventRaised As WebBaseEvent)
            Dim last_row As New List(Of Pair(Of String))
            _data.Add(last_row)
            Dim rwe As IRelationalEventData = TryCast(eventRaised, IRelationalEventData)
            If rwe IsNot Nothing Then
                Dim dt As Date = rwe.CreatedAt
                If Not _useUtc Then
                    dt = dt.ToLocalTime
                End If
                last_row.Add(New Pair(Of String)("time", dt.ToString))

                Dim i As Integer = 0
                For Each c As String In rwe.Columns
                    last_row.Add(New Pair(Of String)(c, rwe.GetValue(i)))
                    i += 1
                Next
            Else
                Dim dt As Date = eventRaised.EventTime
                If _useUtc Then
                    dt = eventRaised.EventTimeUtc
                End If
                last_row.Add(New Pair(Of String)("time", dt.ToString))

                last_row.Add(New Pair(Of String)(PredefinedData, eventRaised.Message))
            End If
        End Sub

        Protected Function GetDocument() As XmlDocument
            Dim path As String = GetFileName()
            Dim xdoc As New XmlDocument
            If IO.File.Exists(path) Then
                Try
                    xdoc.Load(path)
                Catch ex As xmlexception
                    xdoc = New xmldocument
                    Dim r As XmlElement = xdoc.CreateElement("root")
                    xdoc.AppendChild(r)
                End Try
            Else
                Dim r As XmlElement = xdoc.CreateElement("root")
                xdoc.AppendChild(r)
            End If
            Return xdoc
        End Function

        Protected Sub SaveXDoc(ByVal xdoc As XmlDocument)
            Dim path As String = GetFileName()
            xdoc.Save(path)
        End Sub

        Protected Overridable Function GetFileName() As String
            Return _file.Replace("yyyy", Now.Year.ToString).Replace("MM", Now.Month.ToString).Replace("dd", Now.Day.ToString)
        End Function

        Protected Overridable Function TransformEvent(ByVal eventRaised As WebBaseEvent) As ICollection(Of WebBaseEvent)
            Return New WebBaseEvent() {eventRaised}
        End Function
    End Class

    Public MustInherit Class WebEventBase
        Inherits WebBaseEvent
        Implements IRelationalEventData

        Private _dt As Date

        Protected Sub New(ByVal message As String, ByVal eventSource As Object, ByVal eventCode As Integer)
            MyBase.New(message, eventSource, eventCode)
            CollectStat()
        End Sub

        Protected Sub New(ByVal message As String, ByVal eventSource As Object, ByVal eventCode As Integer, ByVal eventDetails As Integer)
            MyBase.New(message, eventSource, eventCode, eventDetails)
            CollectStat()
        End Sub

        Public Sub New(ByVal e As WebBaseEvent)
            MyBase.New(Nothing, Nothing, 0)
            _dt = e.EventTimeUtc
            CollectStat()
        End Sub

        Public ReadOnly Property CreatedAt() As Date Implements IRelationalEventData.CreatedAt
            Get
                Return _dt
            End Get
        End Property

        Protected MustOverride Sub CollectStat()
        Public MustOverride ReadOnly Property Columns() As System.Collections.Generic.ICollection(Of String) Implements IRelationalEventData.Columns
        Public MustOverride Function GetValue(ByVal idx As Integer) As String Implements IRelationalEventData.GetValue
    End Class

    <AspNetHostingPermission(SecurityAction.LinkDemand, Level:=AspNetHostingPermissionLevel.Minimal), AspNetHostingPermission(SecurityAction.InheritanceDemand, Level:=AspNetHostingPermissionLevel.Minimal)> _
    Public Class WebProcessStatistic
        Inherits WebEventBase

        Private _appdomainCount As Integer
        Private _managedHeapSize As Long
        Private _peakWorkingSet As Long
        Private _requestsExecuting As Integer
        Private _requestsQueued As Integer
        Private _requestsRejected As Integer
        Private _startTime As DateTime
        Private _threadCount As Integer
        Private _workingSet As Long

        Public Sub New(ByVal e As WebBaseEvent)
            MyBase.New(e)
        End Sub

        Public Overrides ReadOnly Property Columns() As System.Collections.Generic.ICollection(Of String)
            Get
                Return New String() {"AppDomainCount", "ManagedHeapSize", "PeakWorkingSet", "ProcessStartTime", _
                    "RequestsExecuting", "RequestsQueued", "RequestsRejected", "ThreadCount", "WorkingSet"}
            End Get
        End Property

        Protected Overrides Sub CollectStat()
            With New WebProcessStatistics
                _appdomainCount = .AppDomainCount
                _managedHeapSize = .ManagedHeapSize
                _peakWorkingSet = .PeakWorkingSet
                _requestsExecuting = .RequestsExecuting
                _requestsQueued = .RequestsQueued
                _requestsRejected = .RequestsRejected
                _startTime = .ProcessStartTime
                _threadCount = .ThreadCount
                _workingSet = .WorkingSet
            End With
        End Sub

        Public Overrides Function GetValue(ByVal idx As Integer) As String
            Select Case idx
                Case 0
                    Return _appdomainCount.ToString
                Case 1
                    Return _managedHeapSize.ToString
                Case 2
                    Return _peakWorkingSet.ToString
                Case 3
                    Return _startTime.ToString
                Case 4
                    Return _requestsExecuting.ToString
                Case 5
                    Return _requestsQueued.ToString
                Case 6
                    Return _requestsRejected.ToString
                Case 7
                    Return _threadCount.ToString
                Case 8
                    Return _workingSet.ToString
                Case Else
                    Throw New NotSupportedException(String.Format("Cannot find column for {0} index", idx))
            End Select
        End Function

    End Class

    Public MustInherit Class OrmEntityStatBase
        Inherits WebEventBase

        Private _l As List(Of Pair(Of String, Integer))

        Public Sub New(ByVal e As WebBaseEvent)
            MyBase.New(e)
        End Sub

        Protected Overrides Sub CollectStat()
            _l = New List(Of Pair(Of String, Integer))
            Dim ce As IExploreCache = TryCast(Cache, IExploreCache)
            If ce IsNot Nothing Then
                For Each key As Object In ce.GetAllKeys
                    _l.Add(New Pair(Of String, Integer)(key.ToString, ce.GetDictionary(key).Count))
                Next
            End If
        End Sub

        Public Overrides ReadOnly Property Columns() As System.Collections.Generic.ICollection(Of String)
            Get
                Dim l As New List(Of String)
                For Each p As Pair(Of String, Integer) In _l
                    l.Add(p.First)
                Next
                Return l
            End Get
        End Property

        Public Overrides Function GetValue(ByVal idx As Integer) As String
            Return _l(idx).Second.ToString
        End Function

        Protected MustOverride ReadOnly Property Cache() As OrmCacheBase
    End Class
End Namespace