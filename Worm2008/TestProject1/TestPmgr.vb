Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Worm
Imports Worm.Database

<TestClass()> Public Class TestParamMgr

    '<TestMethod()> _
    'Public Sub TestParamManager()
    '    Dim pmgr As Orm.ParamMgr = Nothing

    '    Dim ipmgr As Orm.ICreateParam = pmgr

    '    Assert.IsNotNull(ipmgr)

    '    Assert.IsTrue(pmgr.IsEmpty)
    'End Sub

    '<TestMethod(), ExpectedException(GetType(InvalidOperationException))> _
    'Public Sub TestCreateParam()
    '    Dim pmgr As Orm.ParamMgr = Nothing

    '    pmgr.CreateParam(Nothing)
    'End Sub

    <TestMethod()> _
    Public Sub TestGetParameter()
        Dim schema As New SQLGenerator("1")

        Dim pmgr As New ParamMgr(schema, "p")

        Assert.AreEqual("p", pmgr.Prefix)

        Assert.AreEqual(0, pmgr.Params.Count)

        'Assert.IsFalse(pmgr.IsEmpty)

        pmgr.CreateParam("ldkg")

        Assert.IsTrue(pmgr.NamedParams)

        pmgr.GetParameter(Nothing)
    End Sub

    <TestMethod()> _
    Public Sub TestGetParameter2()
        Dim schema As New SQLGenerator("1")

        Dim pmgr As New ParamMgr(schema, "p")

        Dim pname As String = pmgr.CreateParam("ldkg")

        Assert.AreEqual("ldkg", pmgr.GetParameter(pname).Value)

        Assert.IsNull(pmgr.GetParameter("adlvn"))
    End Sub

End Class
