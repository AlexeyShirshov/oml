Imports System.Web
Imports System.Web.Hosting
Imports System.Reflection

Public Class ASPNetWorkerRequest
    Inherits SimpleWorkerRequest

    Private Shared _page As FieldInfo
    Private Shared _pathInfo As FieldInfo
    Private _post() As Byte
    Private Const PostMime As String = "application/x-www-form-urlencoded"

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


    Public Overrides Function GetHttpVerbName() As String
        If _post Is Nothing Then
            Return MyBase.GetHttpVerbName()
        Else
            Return "POST"
        End If
    End Function

    Public Overrides Function GetKnownRequestHeader(ByVal index As Integer) As String
        If index = HttpWorkerRequest.HeaderContentLength Then
            If _post IsNot Nothing Then
                Return _post.Length.ToString
            End If
        ElseIf index = HttpWorkerRequest.HeaderContentType Then
            If _post IsNot Nothing Then
                Return PostMime
            End If
        End If
        Return MyBase.GetKnownRequestHeader(index)
    End Function

    Public Overrides Function GetPreloadedEntityBody() As Byte()
        If _post IsNot Nothing Then
            Return _post
        End If
        Return MyBase.GetPreloadedEntityBody()
    End Function

    Friend Property PostData() As Byte()
        Get
            Return _post
        End Get
        Set(ByVal value As Byte())
            _post = value
        End Set
    End Property
End Class
