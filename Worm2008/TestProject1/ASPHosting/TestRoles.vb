Imports System.IO
Imports System.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> _
Public Class TestRoles

    Private _testContext As TestContext

    Public Property TestContext() As TestContext
        Get
            Return _testContext
        End Get
        Set(ByVal value As TestContext)
            _testContext = value
        End Set
    End Property

    <TestMethod()> _
    Public Sub TestCreateRoles()
        Dim dir As String = Path.GetDirectoryName(_testContext.TestDir).Replace("TestResults", "TestProject1")
        Dim pr As New ASPNETHosting.ASPNetHost(dir, "/") '& "\ASPHosting\web"
        'pr.ShadowAssembliesBasePath = dir & "\bin\"
        'pr.ShadowAssemblies = "Worm.Orm.dll;CoreFramework.dll;Worm.Orm.pdb;CoreFramework.pdb"
        pr.Start()

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "GetAllRoles", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "RoleExists", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "AddUser2Role", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "CreateRole", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        End Using

        'Using sw As New StringWriter()
        '    pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "GetUserByEmail", sw)
        '    sw.Flush()
        '    Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        'End Using

        'Using sw As New StringWriter()
        '    pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "UpdateUser", sw)
        '    sw.Flush()
        '    Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        'End Using

        'Using sw As New StringWriter()
        '    pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "CreateUser", sw)
        '    sw.Flush()
        '    Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        'End Using

        'Using sw As New StringWriter()
        '    pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "ResetPassword", sw)
        '    sw.Flush()
        '    Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        'End Using

        'Using sw As New StringWriter()
        '    pr.ProcessRequest("ASPHosting/Web/testroles.aspx", "ValidatePassword", sw)
        '    sw.Flush()
        '    Debug.WriteLine(sw.GetStringBuilder.ToString)
        '    Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test roles ok"))
        'End Using
    End Sub

    '<TestMethod()> _
    'Public Sub TestHttpContext()
    '    Dim ctx As System.Web.HttpContext = System.Web.HttpContext.Current

    '    Assert.IsNotNull(ctx)
    'End Sub
End Class
