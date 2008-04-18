Imports System.IO

Public Class ASPNetHost
    Private _basePath As String = ""
    Private _shadowAssemblies As String
    Private _phisicalPath As String
    Private _virtualPath As String
    Private _proxy As ASPNetProxy
    Private _encoding As System.Text.Encoding = System.Text.Encoding.UTF8

    Public Sub New(ByVal phisicalPath As String, ByVal virtualPath As String)
        If Not phisicalPath.EndsWith("\") Then
            phisicalPath &= "\"
        End If
        _phisicalPath = phisicalPath
        _virtualPath = virtualPath
    End Sub

    Public Sub CopyAssemblies()
        If Not String.IsNullOrEmpty(_shadowAssemblies) Then
            For Each asm As String In _shadowAssemblies.Split(";"c, ","c)
                Dim targetDir As String = PhisicalPath & "bin\" & Path.GetFileName(asm)
                Dim src As String = _basePath & asm
                If File.Exists(targetDir) Then
                    Dim dt As Date = File.GetLastWriteTime(targetDir)
                    If dt >= File.GetLastWriteTime(src) Then
                        Continue For
                    End If
                End If
                File.Copy(src, targetDir, True)
            Next
        End If
    End Sub

    Public Sub Start()
        CopyAssemblies()

        If _proxy IsNot Nothing Then
            _proxy.StopHosting()
        End If

        _proxy = ASPNetProxy.CreateAppHost(_virtualPath, _phisicalPath)
    End Sub

    Public Sub AddPost(ByVal data As String)
        AddPost(_encoding.GetBytes(data))
    End Sub

    Public Sub AddPost(ByVal data() As Byte)
        If _proxy IsNot Nothing Then
            _proxy.AddPost(data)
        End If
    End Sub

    Public Sub ProcessRequest(ByVal page As String, ByVal query As String, ByVal output As IO.TextWriter)
        If _proxy IsNot Nothing Then
            _proxy.ProcessRequest(page, query, output)
        End If
    End Sub

    Public Property VirtualPath() As String
        Get
            Return _virtualPath
        End Get
        Set(ByVal value As String)
            _virtualPath = value
        End Set
    End Property

    Public Property PhisicalPath() As String
        Get
            Return _phisicalPath
        End Get
        Set(ByVal value As String)
            If Not value.EndsWith("\") Then
                value &= "\"
            End If
            _phisicalPath = value
        End Set
    End Property

    Public Property ShadowAssembliesBasePath() As String
        Get
            Return _basePath
        End Get
        Set(ByVal value As String)
            _basePath = value
        End Set
    End Property

    Public Property ShadowAssemblies() As String
        Get
            Return _shadowAssemblies
        End Get
        Set(ByVal value As String)
            _shadowAssemblies = value
        End Set
    End Property

End Class
