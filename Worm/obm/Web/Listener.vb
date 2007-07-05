Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.compilerservices
Imports Microsoft.VisualBasic.Logging
Imports System.Security.Permissions
Imports System.Security
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Collections.Generic
Imports System.Globalization
Imports System.ComponentModel

Namespace Web
    Public Class TraceListener
        Inherits System.Diagnostics.TraceListener

        <HostProtection(SecurityAction.LinkDemand, Resources:=HostProtectionResource.ExternalProcessMgmt)> _
            Public Sub New()
            Me.New("FileLogTraceListener")
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Resources:=HostProtectionResource.ExternalProcessMgmt)> _
        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Me.m_Location = LogFileLocation.LocalUserApplicationDirectory
            Me.m_AutoFlush = False
            Me.m_Append = True
            Me.m_IncludeHostName = False
            Me.m_DiskSpaceExhaustedBehavior = DiskSpaceExhaustedOption.DiscardMessages
            'Me.m_BaseFileName = Path.GetFileNameWithoutExtension(Application.ExecutablePath)
            Me.m_LogFileDateStamp = LogFileCreationScheduleOption.None
            Me.m_MaxFileSize = &H4C4B40
            Me.m_ReserveDiskSpace = &H989680
            Me.m_Delimiter = ChrW(9)
            Me.m_Encoding = Encoding.UTF8
            'Me.m_CustomLocation = Application.UserAppDataPath
            Me.m_Day = DateAndTime.Now.Date
            Me.m_FirstDayOfWeek = GetFirstDayOfWeek(DateAndTime.Now.Date)
            Me.m_PropertiesSet = New BitArray(12, False)
            Me.m_SupportedAttributes = New String() {"append", "Append", "autoflush", "AutoFlush", "autoFlush", "basefilename", "BaseFilename", "baseFilename", "BaseFileName", "baseFileName", "customlocation", "CustomLocation", "customLocation", "delimiter", "Delimiter", "diskspaceexhaustedbehavior", "DiskSpaceExhaustedBehavior", "diskSpaceExhaustedBehavior", "encoding", "Encoding", "includehostname", "IncludeHostName", "includeHostName", "location", "Location", "logfilecreationschedule", "LogFileCreationSchedule", "logFileCreationSchedule", "maxfilesize", "MaxFileSize", "maxFileSize", "reservediskspace", "ReserveDiskSpace", "reserveDiskSpace"}
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub Close()
            Me.Dispose(True)
        End Sub

        Private Sub CloseCurrentStream()
            If (Not Me.m_Stream Is Nothing) Then
                Dim streams As Dictionary(Of String, ReferencedStream) = m_Streams
                SyncLock streams
                    Me.m_Stream.CloseStream()
                    If Not Me.m_Stream.IsInUse Then
                        m_Streams.Remove(Me.m_FullFileName.ToUpper(CultureInfo.InvariantCulture))
                    End If
                    Me.m_Stream = Nothing
                End SyncLock
            End If
        End Sub

        Private Function DayChanged() As Boolean
            Return (DateTime.Compare(Me.m_Day.Date, DateAndTime.Now.Date) <> 0)
        End Function

        Private Sub DemandWritePermission()
            Dim path As String = IO.Path.GetDirectoryName(Me.LogFileName)
            Dim k As New FileIOPermission(FileIOPermissionAccess.Write, path)
            k.Demand()
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                Me.CloseCurrentStream()
            End If
        End Sub

        Private Sub EnsureStreamIsOpen()
            If (Me.m_Stream Is Nothing) Then
                Me.m_Stream = Me.GetStream
            End If
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub Flush()
            If (Not Me.m_Stream Is Nothing) Then
                Me.m_Stream.Flush()
            End If
        End Sub

        Private Function GetFileEncoding(ByVal fileName As String) As Encoding
            If File.Exists(fileName) Then
                Dim reader As StreamReader = Nothing
                Try
                    reader = New StreamReader(fileName, Me.Encoding, True)
                    If (reader.BaseStream.Length > 0) Then
                        reader.ReadLine()
                        Return reader.CurrentEncoding
                    End If
                Finally
                    If (Not reader Is Nothing) Then
                        reader.Close()
                    End If
                End Try
            End If
            Return Nothing
        End Function

        Private Shared Function GetFirstDayOfWeek(ByVal checkDate As DateTime) As DateTime
            Return checkDate.AddDays(CDbl((0 - checkDate.DayOfWeek))).Date
        End Function

        Private Function GetFreeDiskSpace() As Long
            Dim totalFreeSpace As Long
            Dim totalUserSpace As Long
            Dim path As String = IO.Path.GetPathRoot(IO.Path.GetFullPath(Me.FullLogFileName))
            Dim userSpaceFree As Long = -1
            Dim k As New FileIOPermission(FileIOPermissionAccess.PathDiscovery, path)
            k.Demand()
            If (Not GetDiskFreeSpaceEx(path, userSpaceFree, totalUserSpace, totalFreeSpace) OrElse (userSpaceFree <= -1)) Then
                'Throw ExceptionUtils.GetWin32Exception("ApplicationLog_FreeSpaceError", New String(0 - 1) {})
                Throw New InvalidOperationException("ApplicationLog_FreeSpaceError")
            End If
            Return userSpaceFree
        End Function

        <DllImport("Kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)> _
        Friend Shared Function GetDiskFreeSpaceEx(ByVal Directory As String, ByRef UserSpaceFree As Long, ByRef TotalUserSpace As Long, ByRef TotalFreeSpace As Long) As <MarshalAs(UnmanagedType.Bool)> Boolean

        End Function

        Private Function GetStream() As ReferencedStream
            Dim num As Integer = 0
            Dim stream2 As ReferencedStream = Nothing
            Dim fullPath As String = Path.GetFullPath((Me.LogFileName & ".log"))
            Do While ((stream2 Is Nothing) AndAlso (num < &H7FFFFFFF))
                Dim path As String
                If (num = 0) Then
                    path = IO.Path.GetFullPath((Me.LogFileName & ".log"))
                Else
                    path = IO.Path.GetFullPath((Me.LogFileName & "-" & num.ToString(CultureInfo.InvariantCulture) & ".log"))
                End If
                Dim key As String = path.ToUpper(CultureInfo.InvariantCulture)
                Dim streams As Dictionary(Of String, ReferencedStream) = m_Streams
                SyncLock streams
                    If m_Streams.ContainsKey(key) Then
                        stream2 = m_Streams.Item(key)
                        If Not stream2.IsInUse Then
                            m_Streams.Remove(key)
                            stream2 = Nothing
                        Else
                            If Me.Append Then
                                Dim k As New FileIOPermission(FileIOPermissionAccess.Write, path)
                                k.Demand()
                                stream2.AddReference()
                                Me.m_FullFileName = path
                                Return stream2
                            End If
                            num += 1
                            stream2 = Nothing
                            Continue Do
                        End If
                    End If
                    Dim fileEncoding As Encoding = Me.Encoding
                    Try
                        If Me.Append Then
                            fileEncoding = Me.GetFileEncoding(path)
                            If (fileEncoding Is Nothing) Then
                                fileEncoding = Me.Encoding
                            End If
                        End If
                        Dim stream As New StreamWriter(path, Me.Append, fileEncoding)
                        stream2 = New ReferencedStream(stream)
                        stream2.AddReference()
                        m_Streams.Add(key, stream2)
                        Me.m_FullFileName = path
                        Return stream2
                    Catch exception As IOException
                    End Try
                    num += 1
                    Continue Do
                End SyncLock
            Loop
            'Throw ExceptionUtils.GetInvalidOperationException("ApplicationLog_ExhaustedPossibleStreamNames", New String() {fullPath})
            Throw New InvalidOperationException("ApplicationLog_ExhaustedPossibleStreamNames")
        End Function

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Protected Overrides Function GetSupportedAttributes() As String()
            Return Me.m_SupportedAttributes
        End Function

        Private Sub HandleDateChange()
            If (Me.LogFileCreationSchedule = LogFileCreationScheduleOption.Daily) Then
                If Me.DayChanged Then
                    Me.CloseCurrentStream()
                End If
            ElseIf ((Me.LogFileCreationSchedule = LogFileCreationScheduleOption.Weekly) AndAlso Me.WeekChanged) Then
                Me.CloseCurrentStream()
            End If
        End Sub

        Private Function ResourcesAvailable(ByVal newEntrySize As Long) As Boolean
            If ((Me.ListenerStream.FileSize + newEntrySize) > Me.MaxFileSize) Then
                If (Me.DiskSpaceExhaustedBehavior = DiskSpaceExhaustedOption.ThrowException) Then
                    Throw New InvalidOperationException(Utils.GetResourceString("ApplicationLog_FileExceedsMaximumSize"))
                End If
                Return False
            End If
            If ((Me.GetFreeDiskSpace - newEntrySize) >= Me.ReserveDiskSpace) Then
                Return True
            End If
            If (Me.DiskSpaceExhaustedBehavior = DiskSpaceExhaustedOption.ThrowException) Then
                Throw New InvalidOperationException(Utils.GetResourceString("ApplicationLog_ReservedSpaceEncroached"))
            End If
            Return False
        End Function

        Private Shared Function StackToString(ByVal stack As Stack) As String
            Dim length As Integer = ", ".Length
            Dim builder As New StringBuilder
            Try
                Dim obj2 As Object
                For Each obj2 In stack
                    builder.Append((obj2.ToString & ", "))
                Next
            Finally
                Dim enumerator As IEnumerator = Nothing
                If TypeOf enumerator Is IDisposable Then
                    TryCast(enumerator, IDisposable).Dispose()
                End If
            End Try
            builder.Replace("""", """""")
            If (builder.Length >= length) Then
                builder.Remove((builder.Length - length), length)
            End If
            Return ("""" & builder.ToString & """")
        End Function

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub TraceData(ByVal eventCache As TraceEventCache, ByVal source As String, ByVal eventType As TraceEventType, ByVal id As Integer, ByVal ParamArray data As Object())
            Dim builder As New StringBuilder
            If (Not data Is Nothing) Then
                Dim num As Integer = (data.Length - 1)
                Dim num3 As Integer = num
                Dim i As Integer = 0
                Do While (i <= num3)
                    builder.Append(data(i).ToString)
                    If (i <> num) Then
                        builder.Append(Me.Delimiter)
                    End If
                    i += 1
                Loop
            End If
            Me.TraceEvent(eventCache, source, eventType, id, builder.ToString)
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub TraceData(ByVal eventCache As TraceEventCache, ByVal source As String, ByVal eventType As TraceEventType, ByVal id As Integer, ByVal data As Object)
            Dim message As String = ""
            If (Not data Is Nothing) Then
                message = data.ToString
            End If
            Me.TraceEvent(eventCache, source, eventType, id, message)
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub TraceEvent(ByVal eventCache As TraceEventCache, ByVal source As String, ByVal eventType As TraceEventType, ByVal id As Integer, ByVal message As String)
            If ((Me.Filter Is Nothing) OrElse Me.Filter.ShouldTrace(eventCache, source, eventType, id, message, Nothing, Nothing, Nothing)) Then
                Dim builder As New StringBuilder
                builder.Append((source & Me.Delimiter))
                builder.Append(([Enum].GetName(GetType(TraceEventType), eventType) & Me.Delimiter))
                builder.Append((id.ToString(CultureInfo.InvariantCulture) & Me.Delimiter))
                builder.Append(message)
                If ((Me.TraceOutputOptions And TraceOptions.Callstack) = TraceOptions.Callstack) Then
                    builder.Append((Me.Delimiter & eventCache.Callstack))
                End If
                If ((Me.TraceOutputOptions And TraceOptions.LogicalOperationStack) = TraceOptions.LogicalOperationStack) Then
                    builder.Append((Me.Delimiter & StackToString(eventCache.LogicalOperationStack)))
                End If
                If ((Me.TraceOutputOptions And TraceOptions.DateTime) = TraceOptions.DateTime) Then
                    builder.Append((Me.Delimiter & eventCache.DateTime.ToString("u", CultureInfo.InvariantCulture)))
                End If
                If ((Me.TraceOutputOptions And TraceOptions.ProcessId) = TraceOptions.ProcessId) Then
                    builder.Append((Me.Delimiter & eventCache.ProcessId.ToString(CultureInfo.InvariantCulture)))
                End If
                If ((Me.TraceOutputOptions And TraceOptions.ThreadId) = TraceOptions.ThreadId) Then
                    builder.Append((Me.Delimiter & eventCache.ThreadId))
                End If
                If ((Me.TraceOutputOptions And TraceOptions.Timestamp) = TraceOptions.Timestamp) Then
                    builder.Append((Me.Delimiter & eventCache.Timestamp.ToString(CultureInfo.InvariantCulture)))
                End If
                If Me.IncludeHostName Then
                    builder.Append((Me.Delimiter & Me.HostName))
                End If
                Me.WriteLine(builder.ToString)
            End If
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub TraceEvent(ByVal eventCache As TraceEventCache, ByVal source As String, ByVal eventType As TraceEventType, ByVal id As Integer, ByVal format As String, ByVal ParamArray args As Object())
            Dim message As String = Nothing
            If (Not args Is Nothing) Then
                message = String.Format(CultureInfo.InvariantCulture, format, args)
            Else
                message = format
            End If
            Me.TraceEvent(eventCache, source, eventType, id, message)
        End Sub

        Private Sub ValidateDiskSpaceExhaustedOptionEnumValue(ByVal value As DiskSpaceExhaustedOption, ByVal paramName As String)
            If ((value < DiskSpaceExhaustedOption.ThrowException) OrElse (value > DiskSpaceExhaustedOption.DiscardMessages)) Then
                Throw New InvalidEnumArgumentException(paramName, CInt(value), GetType(DiskSpaceExhaustedOption))
            End If
        End Sub

        Private Sub ValidateLogFileCreationScheduleOptionEnumValue(ByVal value As LogFileCreationScheduleOption, ByVal paramName As String)
            If ((value < LogFileCreationScheduleOption.None) OrElse (value > LogFileCreationScheduleOption.Weekly)) Then
                Throw New InvalidEnumArgumentException(paramName, CInt(value), GetType(LogFileCreationScheduleOption))
            End If
        End Sub

        Private Sub ValidateLogFileLocationEnumValue(ByVal value As LogFileLocation, ByVal paramName As String)
            If ((value < LogFileLocation.TempDirectory) OrElse (value > LogFileLocation.Custom)) Then
                Throw New InvalidEnumArgumentException(paramName, CInt(value), GetType(LogFileLocation))
            End If
        End Sub

        Private Function WeekChanged() As Boolean
            Return (DateTime.Compare(Me.m_FirstDayOfWeek.Date, GetFirstDayOfWeek(DateAndTime.Now.Date)) <> 0)
        End Function

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub Write(ByVal message As String)
            Try
                Me.HandleDateChange()
                Dim newEntrySize As Long = Me.Encoding.GetByteCount(message)
                If Me.ResourcesAvailable(newEntrySize) Then
                    Me.ListenerStream.Write(message)
                    If Me.AutoFlush Then
                        Me.ListenerStream.Flush()
                    End If
                End If
            Catch exception1 As Exception
                Me.CloseCurrentStream()
                Throw
            End Try
        End Sub

        <HostProtection(SecurityAction.LinkDemand, Synchronization:=True)> _
        Public Overrides Sub WriteLine(ByVal message As String)
            Try
                Me.HandleDateChange()
                Dim newEntrySize As Long = Me.Encoding.GetByteCount((message & ChrW(13) & ChrW(10)))
                If Me.ResourcesAvailable(newEntrySize) Then
                    Me.ListenerStream.WriteLine(message)
                    If Me.AutoFlush Then
                        Me.ListenerStream.Flush()
                    End If
                End If
            Catch exception1 As Exception
                Me.CloseCurrentStream()
                Throw
            End Try
        End Sub


        ' Properties
        Public Property Append() As Boolean
            Get
                If (Not Me.m_PropertiesSet.Item(0) AndAlso Me.Attributes.ContainsKey("append")) Then
                    Me.Append = Convert.ToBoolean(Me.Attributes.Item("append"), CultureInfo.InvariantCulture)
                End If
                Return Me.m_Append
            End Get
            Set(ByVal value As Boolean)
                Me.DemandWritePermission()
                If (value <> Me.m_Append) Then
                    Me.CloseCurrentStream()
                End If
                Me.m_Append = value
                Me.m_PropertiesSet.Item(0) = True
            End Set
        End Property

        Public Property AutoFlush() As Boolean
            Get
                If (Not Me.m_PropertiesSet.Item(1) AndAlso Me.Attributes.ContainsKey("autoflush")) Then
                    Me.AutoFlush = Convert.ToBoolean(Me.Attributes.Item("autoflush"), CultureInfo.InvariantCulture)
                End If
                Return Me.m_AutoFlush
            End Get
            Set(ByVal value As Boolean)
                Me.DemandWritePermission()
                Me.m_AutoFlush = value
                Me.m_PropertiesSet.Item(1) = True
            End Set
        End Property

        Public Property BaseFileName() As String
            Get
                If (Not Me.m_PropertiesSet.Item(2) AndAlso Me.Attributes.ContainsKey("basefilename")) Then
                    Me.BaseFileName = Me.Attributes.Item("basefilename")
                End If
                Return Me.m_BaseFileName
            End Get
            Set(ByVal value As String)
                If (Operators.CompareString(value, "", False) = 0) Then
                    'Throw ExceptionUtils.GetArgumentNullException("value", "ApplicationLogBaseNameNull", New String(0 - 1) {})
                    Throw New ArgumentException("value")
                End If
                Path.GetFullPath(value)
                If (String.Compare(value, Me.m_BaseFileName, StringComparison.OrdinalIgnoreCase) <> 0) Then
                    Me.CloseCurrentStream()
                    Me.m_BaseFileName = value
                End If
                Me.m_PropertiesSet.Item(2) = True
            End Set
        End Property

        Public Property CustomLocation() As String
            Get
                If (Not Me.m_PropertiesSet.Item(3) AndAlso Me.Attributes.ContainsKey("customlocation")) Then
                    Me.CustomLocation = Me.Attributes.Item("customlocation")
                End If
                Dim path As String = IO.Path.GetFullPath(Me.m_CustomLocation)
                Dim k As New FileIOPermission(FileIOPermissionAccess.PathDiscovery, path)
                k.Demand()
                Return path
            End Get
            Set(ByVal value As String)
                Dim path As String = IO.Path.GetFullPath(value)
                If Not Directory.Exists(path) Then
                    Directory.CreateDirectory(path)
                End If
                If ((Me.Location = LogFileLocation.Custom) And (String.Compare(path, Me.m_CustomLocation, StringComparison.OrdinalIgnoreCase) <> 0)) Then
                    Me.CloseCurrentStream()
                End If
                Me.Location = LogFileLocation.Custom
                Me.m_CustomLocation = path
                Me.m_PropertiesSet.Item(3) = True
            End Set
        End Property

        Public Property Delimiter() As String
            Get
                If (Not Me.m_PropertiesSet.Item(4) AndAlso Me.Attributes.ContainsKey("delimiter")) Then
                    Me.Delimiter = Me.Attributes.Item("delimiter")
                End If
                Return Me.m_Delimiter
            End Get
            Set(ByVal value As String)
                Me.m_Delimiter = value
                Me.m_PropertiesSet.Item(4) = True
            End Set
        End Property

        Public Property DiskSpaceExhaustedBehavior() As DiskSpaceExhaustedOption
            Get
                If (Not Me.m_PropertiesSet.Item(5) AndAlso Me.Attributes.ContainsKey("diskspaceexhaustedbehavior")) Then
                    Dim converter As TypeConverter = TypeDescriptor.GetConverter(GetType(DiskSpaceExhaustedOption))
                    Me.DiskSpaceExhaustedBehavior = DirectCast(converter.ConvertFromInvariantString(Me.Attributes.Item("diskspaceexhaustedbehavior")), DiskSpaceExhaustedOption)
                End If
                Return Me.m_DiskSpaceExhaustedBehavior
            End Get
            Set(ByVal value As DiskSpaceExhaustedOption)
                Me.DemandWritePermission()
                Me.ValidateDiskSpaceExhaustedOptionEnumValue(value, "value")
                Me.m_DiskSpaceExhaustedBehavior = value
                Me.m_PropertiesSet.Item(5) = True
            End Set
        End Property

        Public Property Encoding() As Encoding
            Get
                If (Not Me.m_PropertiesSet.Item(6) AndAlso Me.Attributes.ContainsKey("encoding")) Then
                    Me.Encoding = Encoding.GetEncoding(Me.Attributes.Item("encoding"))
                End If
                Return Me.m_Encoding
            End Get
            Set(ByVal value As Encoding)
                If (value Is Nothing) Then
                    Throw New ArgumentNullException("value")
                End If
                Me.m_Encoding = value
                Me.m_PropertiesSet.Item(6) = True
            End Set
        End Property

        Public ReadOnly Property FullLogFileName() As String
            Get
                Me.EnsureStreamIsOpen()
                Dim path As String = Me.m_FullFileName
                Dim k As New FileIOPermission(FileIOPermissionAccess.PathDiscovery, path)
                k.Demand()
                Return path
            End Get
        End Property

        Private ReadOnly Property HostName() As String
            Get
                If (Operators.CompareString(Me.m_HostName, "", False) = 0) Then
                    Me.m_HostName = Environment.MachineName
                End If
                Return Me.m_HostName
            End Get
        End Property

        Public Property IncludeHostName() As Boolean
            Get
                If (Not Me.m_PropertiesSet.Item(7) AndAlso Me.Attributes.ContainsKey("includehostname")) Then
                    Me.IncludeHostName = Convert.ToBoolean(Me.Attributes.Item("includehostname"), CultureInfo.InvariantCulture)
                End If
                Return Me.m_IncludeHostName
            End Get
            Set(ByVal value As Boolean)
                Me.DemandWritePermission()
                Me.m_IncludeHostName = value
                Me.m_PropertiesSet.Item(7) = True
            End Set
        End Property

        Private ReadOnly Property ListenerStream() As ReferencedStream
            Get
                Me.EnsureStreamIsOpen()
                Return Me.m_Stream
            End Get
        End Property

        Public Property Location() As LogFileLocation
            Get
                If (Not Me.m_PropertiesSet.Item(8) AndAlso Me.Attributes.ContainsKey("location")) Then
                    Dim converter As TypeConverter = TypeDescriptor.GetConverter(GetType(LogFileLocation))
                    Me.Location = DirectCast(converter.ConvertFromInvariantString(Me.Attributes.Item("location")), LogFileLocation)
                End If
                Return Me.m_Location
            End Get
            Set(ByVal value As LogFileLocation)
                Me.ValidateLogFileLocationEnumValue(value, "value")
                If (Me.m_Location <> value) Then
                    Me.CloseCurrentStream()
                End If
                Me.m_Location = value
                Me.m_PropertiesSet.Item(8) = True
            End Set
        End Property

        Public Property LogFileCreationSchedule() As LogFileCreationScheduleOption
            Get
                If (Not Me.m_PropertiesSet.Item(9) AndAlso Me.Attributes.ContainsKey("logfilecreationschedule")) Then
                    Dim converter As TypeConverter = TypeDescriptor.GetConverter(GetType(LogFileCreationScheduleOption))
                    Me.LogFileCreationSchedule = DirectCast(converter.ConvertFromInvariantString(Me.Attributes.Item("logfilecreationschedule")), LogFileCreationScheduleOption)
                End If
                Return Me.m_LogFileDateStamp
            End Get
            Set(ByVal value As LogFileCreationScheduleOption)
                Me.ValidateLogFileCreationScheduleOptionEnumValue(value, "value")
                If (value <> Me.m_LogFileDateStamp) Then
                    Me.CloseCurrentStream()
                    Me.m_LogFileDateStamp = value
                End If
                Me.m_PropertiesSet.Item(9) = True
            End Set
        End Property

        Private ReadOnly Property LogFileName() As String
            Get
                Dim tempPath As String
                Select Case Me.Location
                    Case LogFileLocation.TempDirectory
                        tempPath = Path.GetTempPath
                        Exit Select
                    Case LogFileLocation.LocalUserApplicationDirectory
                        'tempPath = Application.UserAppDataPath
                        Throw New NotImplementedException
                        Exit Select
                    Case LogFileLocation.CommonApplicationDirectory
                        'tempPath = Application.CommonAppDataPath
                        Throw New NotImplementedException
                        Exit Select
                    Case LogFileLocation.ExecutableDirectory
                        'tempPath = Path.GetDirectoryName(Application.ExecutablePath)
                        Throw New NotImplementedException
                        Exit Select
                    Case LogFileLocation.Custom
                        If (Operators.CompareString(Me.CustomLocation, "", False) <> 0) Then
                            tempPath = Me.CustomLocation
                            Exit Select
                        End If
                        'tempPath = Application.UserAppDataPath
                        Throw New NotImplementedException
                        Exit Select
                    Case Else
                        'tempPath = Application.UserAppDataPath
                        Throw New NotImplementedException
                        Exit Select
                End Select
                Dim baseFileName As String = Me.BaseFileName
                Select Case Me.LogFileCreationSchedule
                    Case LogFileCreationScheduleOption.Daily
                        baseFileName = (baseFileName & "-" & DateAndTime.Now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        Exit Select
                    Case LogFileCreationScheduleOption.Weekly
                        Me.m_FirstDayOfWeek = DateAndTime.Now.AddDays(CDbl((0 - DateAndTime.Now.DayOfWeek)))
                        baseFileName = (baseFileName & "-" & Me.m_FirstDayOfWeek.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        Exit Select
                End Select
                Return Path.Combine(tempPath, baseFileName)
            End Get
        End Property

        Public Property MaxFileSize() As Long
            Get
                If (Not Me.m_PropertiesSet.Item(10) AndAlso Me.Attributes.ContainsKey("maxfilesize")) Then
                    Me.MaxFileSize = Convert.ToInt64(Me.Attributes.Item("maxfilesize"), CultureInfo.InvariantCulture)
                End If
                Return Me.m_MaxFileSize
            End Get
            Set(ByVal value As Long)
                Me.DemandWritePermission()
                If (value < &H3E8) Then
                    'Throw ExceptionUtils.GetArgumentExceptionWithArgName("value", "ApplicationLogNumberTooSmall", New String() {"MaxFileSize"})
                    Throw New ArgumentException("ApplicationLogNumberTooSmall")
                End If
                Me.m_MaxFileSize = value
                Me.m_PropertiesSet.Item(10) = True
            End Set
        End Property

        Public Property ReserveDiskSpace() As Long
            Get
                If (Not Me.m_PropertiesSet.Item(11) AndAlso Me.Attributes.ContainsKey("reservediskspace")) Then
                    Me.ReserveDiskSpace = Convert.ToInt64(Me.Attributes.Item("reservediskspace"), CultureInfo.InvariantCulture)
                End If
                Return Me.m_ReserveDiskSpace
            End Get
            Set(ByVal value As Long)
                Me.DemandWritePermission()
                If (value < 0) Then
                    'Throw ExceptionUtils.GetArgumentExceptionWithArgName("value", "ApplicationLog_NegativeNumber", New String() {"ReserveDiskSpace"})
                    Throw New ArgumentException("ApplicationLog_NegativeNumber")
                End If
                Me.m_ReserveDiskSpace = value
                Me.m_PropertiesSet.Item(11) = True
            End Set
        End Property


        ' Fields
        Private Const APPEND_INDEX As Integer = 0
        Private Const AUTOFLUSH_INDEX As Integer = 1
        Private Const BASEFILENAME_INDEX As Integer = 2
        Private Const CUSTOMLOCATION_INDEX As Integer = 3
        Private Const DATE_FORMAT As String = "yyyy-MM-dd"
        Private Const DEFAULT_NAME As String = "FileLogTraceListener"
        Private Const DELIMITER_INDEX As Integer = 4
        Private Const DISKSPACEEXHAUSTEDBEHAVIOR_INDEX As Integer = 5
        Private Const ENCODING_INDEX As Integer = 6
        Private Const FILE_EXTENSION As String = ".log"
        Private Const INCLUDEHOSTNAME_INDEX As Integer = 7
        Private Const KEY_APPEND As String = "append"
        Private Const KEY_APPEND_PASCAL As String = "Append"
        Private Const KEY_AUTOFLUSH As String = "autoflush"
        Private Const KEY_AUTOFLUSH_CAMEL As String = "autoFlush"
        Private Const KEY_AUTOFLUSH_PASCAL As String = "AutoFlush"
        Private Const KEY_BASEFILENAME As String = "basefilename"
        Private Const KEY_BASEFILENAME_CAMEL As String = "baseFilename"
        Private Const KEY_BASEFILENAME_CAMEL_ALT As String = "baseFileName"
        Private Const KEY_BASEFILENAME_PASCAL As String = "BaseFilename"
        Private Const KEY_BASEFILENAME_PASCAL_ALT As String = "BaseFileName"
        Private Const KEY_CUSTOMLOCATION As String = "customlocation"
        Private Const KEY_CUSTOMLOCATION_CAMEL As String = "customLocation"
        Private Const KEY_CUSTOMLOCATION_PASCAL As String = "CustomLocation"
        Private Const KEY_DELIMITER As String = "delimiter"
        Private Const KEY_DELIMITER_PASCAL As String = "Delimiter"
        Private Const KEY_DISKSPACEEXHAUSTEDBEHAVIOR As String = "diskspaceexhaustedbehavior"
        Private Const KEY_DISKSPACEEXHAUSTEDBEHAVIOR_CAMEL As String = "diskSpaceExhaustedBehavior"
        Private Const KEY_DISKSPACEEXHAUSTEDBEHAVIOR_PASCAL As String = "DiskSpaceExhaustedBehavior"
        Private Const KEY_ENCODING As String = "encoding"
        Private Const KEY_ENCODING_PASCAL As String = "Encoding"
        Private Const KEY_INCLUDEHOSTNAME As String = "includehostname"
        Private Const KEY_INCLUDEHOSTNAME_CAMEL As String = "includeHostName"
        Private Const KEY_INCLUDEHOSTNAME_PASCAL As String = "IncludeHostName"
        Private Const KEY_LOCATION As String = "location"
        Private Const KEY_LOCATION_PASCAL As String = "Location"
        Private Const KEY_LOGFILECREATIONSCHEDULE As String = "logfilecreationschedule"
        Private Const KEY_LOGFILECREATIONSCHEDULE_CAMEL As String = "logFileCreationSchedule"
        Private Const KEY_LOGFILECREATIONSCHEDULE_PASCAL As String = "LogFileCreationSchedule"
        Private Const KEY_MAXFILESIZE As String = "maxfilesize"
        Private Const KEY_MAXFILESIZE_CAMEL As String = "maxFileSize"
        Private Const KEY_MAXFILESIZE_PASCAL As String = "MaxFileSize"
        Private Const KEY_RESERVEDISKSPACE As String = "reservediskspace"
        Private Const KEY_RESERVEDISKSPACE_CAMEL As String = "reserveDiskSpace"
        Private Const KEY_RESERVEDISKSPACE_PASCAL As String = "ReserveDiskSpace"
        Private Const LOCATION_INDEX As Integer = 8
        Private Const LOGFILECREATIONSCHEDULE_INDEX As Integer = 9
        Private m_Append As Boolean
        Private m_AutoFlush As Boolean
        Private m_BaseFileName As String
        Private m_CustomLocation As String
        Private m_Day As DateTime
        Private m_Delimiter As String
        Private m_DiskSpaceExhaustedBehavior As DiskSpaceExhaustedOption
        Private m_Encoding As Encoding
        Private m_FirstDayOfWeek As DateTime
        Private m_FullFileName As String
        Private m_HostName As String
        Private m_IncludeHostName As Boolean
        Private m_Location As LogFileLocation
        Private m_LogFileDateStamp As LogFileCreationScheduleOption
        Private m_MaxFileSize As Long
        Private m_PropertiesSet As BitArray
        Private m_ReserveDiskSpace As Long
        Private m_Stream As ReferencedStream
        Private Shared m_Streams As Dictionary(Of String, ReferencedStream) = New Dictionary(Of String, ReferencedStream)
        Private m_SupportedAttributes As String()
        Private Const MAX_OPEN_ATTEMPTS As Integer = &H7FFFFFFF
        Private Const MAXFILESIZE_INDEX As Integer = 10
        Private Const MIN_FILE_SIZE As Integer = &H3E8
        Private Const PROPERTY_COUNT As Integer = 12
        Private Const RESERVEDISKSPACE_INDEX As Integer = 11
        Private Const STACK_DELIMITER As String = ", "

        ' Nested Types
        Public Class ReferencedStream
            Implements IDisposable
            ' Methods
            Friend Sub New(ByVal stream As StreamWriter)
                Me.m_Stream = stream
            End Sub

            Friend Sub AddReference()
                Dim expression As Object = Me.m_SyncObject
                ObjectFlowControl.CheckForSyncLockOnValueType(expression)
                SyncLock expression
                    Me.m_ReferenceCount += 1
                End SyncLock
            End Sub

            Friend Sub CloseStream()
                Dim expression As Object = Me.m_SyncObject
                ObjectFlowControl.CheckForSyncLockOnValueType(expression)
                SyncLock expression
                    Try
                        Me.m_ReferenceCount -= 1
                        Me.m_Stream.Flush()
                    Finally
                        If (Me.m_ReferenceCount <= 0) Then
                            Me.m_Stream.Close()
                            Me.m_Stream = Nothing
                        End If
                    End Try
                End SyncLock
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                Me.Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

            Private Sub Dispose(ByVal disposing As Boolean)
                If (disposing AndAlso Not Me.m_Disposed) Then
                    If (Not Me.m_Stream Is Nothing) Then
                        Me.m_Stream.Close()
                    End If
                    Me.m_Disposed = True
                End If
            End Sub

            Protected Overrides Sub Finalize()
                Me.Dispose(False)
                MyBase.Finalize()
            End Sub

            Friend Sub Flush()
                Dim expression As Object = Me.m_SyncObject
                ObjectFlowControl.CheckForSyncLockOnValueType(expression)
                SyncLock expression
                    Me.m_Stream.Flush()
                End SyncLock
            End Sub

            Friend Sub Write(ByVal message As String)
                Dim expression As Object = Me.m_SyncObject
                ObjectFlowControl.CheckForSyncLockOnValueType(expression)
                SyncLock expression
                    Me.m_Stream.Write(message)
                End SyncLock
            End Sub

            Friend Sub WriteLine(ByVal message As String)
                Dim expression As Object = Me.m_SyncObject
                ObjectFlowControl.CheckForSyncLockOnValueType(expression)
                SyncLock expression
                    Me.m_Stream.WriteLine(message)
                End SyncLock
            End Sub


            ' Properties
            Friend ReadOnly Property FileSize() As Long
                Get
                    Return Me.m_Stream.BaseStream.Length
                End Get
            End Property

            Friend ReadOnly Property IsInUse() As Boolean
                Get
                    Return (Not Me.m_Stream Is Nothing)
                End Get
            End Property


            ' Fields
            Private m_Disposed As Boolean = False
            Private m_ReferenceCount As Integer = 0
            Private m_Stream As StreamWriter
            Private m_SyncObject As Object = New Object
        End Class
    End Class
End Namespace