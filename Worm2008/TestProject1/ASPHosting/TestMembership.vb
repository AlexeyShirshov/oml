Imports System.IO
Imports System.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> _
Public Class TestMembership

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
    Public Sub TestCreateMembership()
        Dim dir As String = Path.GetDirectoryName(_testContext.TestDir).Replace("TestResults", "TestProject1")
        Dim pr As New ASPNETHosting.ASPNetHost(dir, "/") '& "\ASPHosting\web"
        'pr.ShadowAssembliesBasePath = dir & "\bin\"
        'pr.ShadowAssemblies = "Worm.Orm.dll;CoreFramework.dll;Worm.Orm.pdb;CoreFramework.pdb"
        pr.Start()

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "GetAllUsers", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "FindUsersByName", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "GetNumberOfUsersOnline", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "GetUser", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "GetUserByEmail", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "UpdateUser", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "CreateUser", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "ResetPassword", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using

        Using sw As New StringWriter()
            pr.ProcessRequest("ASPHosting/Web/testmembership.aspx", "ValidatePassword", sw)
            sw.Flush()
            Debug.WriteLine(sw.GetStringBuilder.ToString)
            Assert.IsTrue(sw.GetStringBuilder.ToString.Contains("test membership ok"))
        End Using
    End Sub

    '<TestMethod()> _
    'Public Sub TestHttpContext()
    '    Dim ctx As System.Web.HttpContext = System.Web.HttpContext.Current

    '    Assert.IsNotNull(ctx)
    'End Sub
End Class
