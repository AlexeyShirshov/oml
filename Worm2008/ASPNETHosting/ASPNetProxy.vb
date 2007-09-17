Imports System.IO
Imports System.Web
Imports System.Web.Hosting
Imports System.Reflection

<Serializable()> _
Public Class ASPNetProxy
    Inherits MarshalByRefObject
    Implements IRegisteredObject

    'Private _appDomain As AppDomain

    Public Sub New()

    End Sub

    Public Shared Function CreateAppHost(ByVal virtualPath As String, ByVal phisicalPath As String) As ASPNetProxy
        Dim targetDir As String = phisicalPath & "bin"

        If Not Directory.Exists(targetDir) Then
            Directory.CreateDirectory(targetDir)
        End If

        Dim mypath As String = Assembly.GetExecutingAssembly.Location
        Dim myfilename As String = Path.GetFileName(Assembly.GetExecutingAssembly.Location)

        'File.Copy(mypath, targetDir & "\" & myfilename, True)

        Dim mgr As ApplicationManager = ApplicationManager.GetApplicationManager
        Dim AppId As String = "scripts_" + Guid.NewGuid().GetHashCode().ToString("x")
        Dim proxy As ASPNetProxy = CType(mgr.CreateObject(AppId, GetType(ASPNetProxy), virtualPath, phisicalPath, False), ASPNetProxy)

        Return proxy
    End Function

    Public Sub StopHosting()
        HttpRuntime.UnloadAppDomain()
        'AppDomain.Unload(_appDomain)
    End Sub

    Public Sub ProcessRequest(ByVal page As String, ByVal query As String, ByVal output As IO.TextWriter)
        'Dim wr As New ASPNetWorkerRequest(_virtualPath, _phisicalPath, page, query, output)
        Dim wr As New ASPNetWorkerRequest(page, query, output)
        'Dim wr As New SimpleWorkerRequest(page, query, output)
        HttpRuntime.ProcessRequest(wr)
    End Sub

    Public Sub [Stop](ByVal immediate As Boolean) Implements System.Web.Hosting.IRegisteredObject.Stop
        HostingEnvironment.UnregisterObject(Me)
    End Sub
End Class
