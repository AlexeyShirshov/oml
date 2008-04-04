Imports System.IO
Imports System.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> _
Public Class TestCache

    Private _testContext As TestContext
    Private _write2console As Boolean = True

    Public Property Write2Console() As Boolean
        Get
            Return _write2console
        End Get
        Set(ByVal value As Boolean)
            _write2console = value
        End Set
    End Property

    Public Property TestContext() As TestContext
        Get
            Return _testContext
        End Get
        Set(ByVal value As TestContext)
            _testContext = value
        End Set
    End Property

    <TestMethod()> _
    Public Sub TestCache()
        Dim h As ASPNETHosting.ASPNetHost = GetHost()

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "add", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "remove", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "remove2", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "getvalues", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "getnonexistent", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("Key lonasdg not found"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "getnonexistent2", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("second exists False equals -19"))
        End Using

        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpcache.aspx", "readd", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("The key is already in collection: first"))
        End Using

    End Sub

    <TestMethod()> _
    Public Sub StressTest()
        Dim h As ASPNETHosting.ASPNetHost = GetHost()
        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/httpcache-stresstest.aspx", "add=true", sw)
            If Write2Console Then
                Debug.WriteLine(sw.GetStringBuilder.ToString)
            End If
        End Using

        'Dim l As New Collections.Generic.List(Of String)
        'For i As Integer = 0 To 1000 - 1
        '    Dim key As String = Guid.NewGuid.ToString
        '    h.ProcessRequest("ASPHosting/Web/httpcache-stresstest.aspx", "add=true&key=" & key & "&value=" & i, Nothing)
        '    l.Add(key)
        'Next

        'Using sw As New StringWriter()
        '    For Each key As String In l
        '        h.ProcessRequest("ASPHosting/Web/httpcache-stresstest.aspx", "query=true&key=" & key, sw)
        '    Next
        '    If Write2Console Then
        '        Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    End If
        'End Using
    End Sub

    <TestMethod()> _
    Public Sub TestObjects()
        Dim h As ASPNETHosting.ASPNetHost = GetHost()
        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testobjects.aspx", String.Empty, sw)
            If Write2Console Then
                Debug.WriteLine(sw.GetStringBuilder.ToString)
            End If
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test objects ok"))
        End Using
    End Sub

    <TestMethod()> _
    Public Sub TestExpire()
        Dim h As ASPNETHosting.ASPNetHost = GetHost()
        Using sw As New StringWriter()
            h.ProcessRequest("ASPHosting/Web/testhttpdic.aspx", String.Empty, sw)
            If Write2Console Then
                Debug.WriteLine(sw.GetStringBuilder.ToString)
            End If
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("cnt = 0"))
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("created = True"))

            Threading.Thread.Sleep(1000 * 60 * 2)

            sw.GetStringBuilder.Length = 0
            h.ProcessRequest("ASPHosting/Web/testhttpdic.aspx", "second", sw)
            If Write2Console Then
                Debug.WriteLine(sw.GetStringBuilder.ToString)
            End If
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test is ok"))
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("cnt = 2"))
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("created = False"))
        End Using
    End Sub

    Protected Function GetHost() As ASPNETHosting.ASPNetHost
        Dim dir As String = Path.GetDirectoryName(_testContext.TestDir).Replace("TestResults", "TestProject1")
        Dim pr As New ASPNETHosting.ASPNetHost(dir, "/") '& "\ASPHosting\web"
        pr.Start()
        Return pr
    End Function
End Class
