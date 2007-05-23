Imports System.Web
Imports System.Web.Hosting
Imports System.Reflection

Public Class ASPNetWorkerRequest
    Inherits SimpleWorkerRequest

    Private Shared _page As FieldInfo
    Private Shared _pathInfo As FieldInfo

    Shared Sub New()
        _page = GetType(SimpleWorkerRequest).GetField("_page", BindingFlags.Instance Or BindingFlags.NonPublic)
        _pathInfo = GetType(SimpleWorkerRequest).GetField("_pathInfo", BindingFlags.Instance Or BindingFlags.NonPublic)
    End Sub

    Public Sub New(ByVal page As String, ByVal query As String, ByVal output As IO.TextWriter)
        MyBase.New(page, query, output)

        Dim idx As Integer = page.IndexOf("/"c)
        If idx >= 0 Then
            _pathInfo.SetValue(Me, Nothing)
            _page.SetValue(Me, page.Replace("~/", ""))
        End If

    End Sub

    Public Sub New(ByVal virtualDir As String, ByVal phisicalDir As String, ByVal page As String, ByVal query As String, ByVal output As IO.TextWriter)
        MyBase.New(virtualDir, phisicalDir, page, query, output)

        Dim idx As Integer = page.IndexOf("/"c)
        If idx >= 0 Then
            _pathInfo.SetValue(Me, Nothing)
            _page.SetValue(Me, page.Replace("~/", ""))
        End If
    End Sub

End Class
