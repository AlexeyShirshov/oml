Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Worm.Database
Imports Worm.Entities.Meta

<TestClass()> Public Class TestMerge

    <TestMethod()> _
    Public Sub TestMerge()

        Dim l As New List(Of Integer)
        l.Add(1)
        l.Add(2)

        Dim mr As Worm.MergeResult = Worm.helper.MergeIds(l, False)

        Assert.AreEqual(1, mr.Pairs.Count)
        Assert.AreEqual(0, mr.Rest.Count)



        l.Clear()

        l.Add(1)
        l.Add(2)
        l.Add(10)

        mr = Worm.helper.MergeIds(l, False)

        Assert.AreEqual(1, mr.Pairs.Count)
        Assert.AreEqual(1, mr.Rest.Count)




        l.Clear()

        l.Add(1)
        l.Add(2)
        l.Add(10)
        l.Add(11)

        mr = Worm.helper.MergeIds(l, False)

        Assert.AreEqual(2, mr.Pairs.Count)
        Assert.AreEqual(0, mr.Rest.Count)

        l.Clear()

        l.Add(1)
        l.Add(2)
        l.Add(5)
        l.Add(10)
        l.Add(11)

        mr = Worm.helper.MergeIds(l, False)

        Assert.AreEqual(2, mr.Pairs.Count)
        Assert.AreEqual(1, mr.Rest.Count)

    End Sub

    <TestMethod()> _
    Public Sub TestMerge2()

        Dim l As New List(Of Integer)
        l.Add(1)
        l.Add(2)
        l.Add(3)
        l.Add(4)
        l.Add(6)
        l.Add(7)
        l.Add(8)
        l.Add(9)
        l.Add(10)
        l.Add(11)
        l.Add(12)

        Dim mr As Worm.MergeResult = Worm.helper.MergeIds(l, False)

        Assert.AreEqual(2, mr.Pairs.Count)
    End Sub

    <TestMethod()> _
    Public Sub RealMergeTest()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New Worm.ObjectMappingEngine("1"))
            Dim pa As New Worm_Orm_OrmReadOnlyDBManagerAccessor(mgr)

            Dim l As New List(Of Object)
            Dim i As Integer = 0
            Do
                l.Add(i)
                l.Add(i + 1)
                l.Add(i + 2)
                i += 5
            Loop While i < 1000
            Do
                i += New Random(Environment.TickCount).Next(1, 5)
                l.Add(i)
            Loop While i < 10000
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(CType(mgr.SQLGenerator, SQLGenerator), "p")
            almgr.AddTable(mgr.MappingEngine.GetTables(GetType(Entity))(0), Nothing)
            pa.GetFilters(l, "ID", almgr, params, GetType(Entity), False)

            pa.GetFilters(l, mgr.MappingEngine.GetTables(GetType(Entity))(0), "ID", almgr, params, False)
        End Using
    End Sub

    <TestMethod()> _
    Public Sub RealMergeTest2()
        Using mgr As OrmReadOnlyDBManager = TestManager.CreateWriteManager(New Worm.ObjectMappingEngine("1"))
            Dim pa As New Worm_Orm_OrmReadOnlyDBManagerAccessor(mgr)

            Dim l As New List(Of Object)
            Dim i As Integer = 0
            Do
                l.Add(i)
                l.Add(i + 1)
                l.Add(i + 2)
                i += 5
            Loop While i < 1000
            Do
                i += New Random(Environment.TickCount).Next(1, 5)
                l.Add(i)
            Loop While i < 10000
            'Dim almgr As Worm.Orm.AliasMgr = Worm.Orm.AliasMgr.Create
            'Dim params As New Worm.Orm.ParamMgr(CType(mgr.DatabaseSchema, Orm.DbSchema), "p")
            'almgr.AddTable(mgr.DatabaseSchema.GetTables(GetType(Entity))(0))
            pa.GetObjects(GetType(Entity), l, Nothing, False, "ID", False)
        End Using
    End Sub
End Class
