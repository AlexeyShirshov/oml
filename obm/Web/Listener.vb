Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Logging
Imports System.Security.Permissions
Imports System.Security

Namespace Web
    Public Class TraceListener
        Inherits Logging.FileLogTraceListener

        <HostProtection(SecurityAction.LinkDemand, Resources:=HostProtectionResource.ExternalProcessMgmt)> _
        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Me.Location = Logging.LogFileLocation.LocalUserApplicationDirectory
            Me.AutoFlush = False
            Me.Append = True
            Me.IncludeHostName = False
            Me.DiskSpaceExhaustedBehavior = DiskSpaceExhaustedOption.DiscardMessages
            'Me.BaseFileName = IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath)
            Me.LogFileCreationSchedule = LogFileCreationScheduleOption.None
            Me.MaxFileSize = &H4C4B40
            Me.ReserveDiskSpace = &H989680
            Me.Delimiter = ChrW(9)
            Me.Encoding = Encoding.UTF8
            'Me._CustomLocation = Application.UserAppDataPath
            SetPrivate()
        End Sub

        Private Sub SetPrivate()
            SetPrivate("m_Day", Now.Date)
            SetPrivate("m_FirstDayOfWeek", GetFirstDayOfWeek(DateAndTime.Now.Date))
            SetPrivate("m_PropertiesSet", New BitArray(12, False))
            SetPrivate("m_SupportedAttributes", New String() {"append", "Append", "autoflush", "AutoFlush", "autoFlush", "basefilename", "BaseFilename", "baseFilename", "BaseFileName", "baseFileName", "customlocation", "CustomLocation", "customLocation", "delimiter", "Delimiter", "diskspaceexhaustedbehavior", "DiskSpaceExhaustedBehavior", "diskSpaceExhaustedBehavior", "encoding", "Encoding", "includehostname", "IncludeHostName", "includeHostName", "location", "Location", "logfilecreationschedule", "LogFileCreationSchedule", "logFileCreationSchedule", "maxfilesize", "MaxFileSize", "maxFileSize", "reservediskspace", "ReserveDiskSpace", "reserveDiskSpace"})
        End Sub

        Protected Shared Function GetFirstDayOfWeek(ByVal checkDate As DateTime) As DateTime
            Return checkDate.AddDays(CDbl((0 - checkDate.DayOfWeek))).Date
        End Function

        Protected Sub SetPrivate(ByVal name As String, ByVal value As Object)
            Me.GetType.GetField(name, Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic).SetValue(Me, value)
        End Sub
    End Class
End Namespace