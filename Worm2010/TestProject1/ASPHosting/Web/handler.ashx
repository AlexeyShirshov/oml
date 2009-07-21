<%@ WebHandler Language="VB" Class="Handler" %>

Imports System
Imports System.Web
Imports System.Text

Public Class Handler : Implements IHttpHandler
    
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        With context
            Dim s As String = Encoding.UTF8.GetString(.Request.BinaryRead(.Request.TotalBytes))
            Dim x As New Xml.XmlDocument
            x.LoadXml(s)
            .Response.ContentType = "text/xml"
            x.Save(.Response.OutputStream)
        End With
    End Sub
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

End Class