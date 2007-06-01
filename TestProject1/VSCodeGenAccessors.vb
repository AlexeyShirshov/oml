﻿Imports CoreFramework.Structures
Imports Microsoft.VisualStudio.TestTools.UnitTesting

'------------------------------------------------------------------------------
'<autogenerated>
'        This code was generated by Microsoft Visual Studio Team System 2005.
'
'        Changes to this file may cause incorrect behavior and will be lost if
'        the code is regenerated.
'</autogenerated>
'------------------------------------------------------------------------------
<System.Diagnostics.DebuggerStepThrough(),  _
 System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TestTools.UnitTestGeneration", "1.0.0.0")>  _
Friend Class Worm_Orm_OrmReadOnlyDBManagerAccessor
    Inherits BaseAccessor
    
    Protected Shared m_privateType As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType = New Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType(GetType(Global.Worm.Orm.OrmReadOnlyDBManager))
    
    Friend Sub New(ByVal target As Worm.Orm.OrmReadOnlyDBManager)
        MyBase.New(target, m_privateType)
    End Sub
    
    Friend Property _connStr() As String
        Get
            Dim ret As String = CType(m_privateObject.GetFieldOrProperty("_connStr"),String)
            Return ret
        End Get
        Set
            m_privateObject.SetFieldOrProperty("_connStr", value)
        End Set
    End Property
    
    Friend Property _tran() As Global.System.Data.Common.DbTransaction
        Get
            Dim ret As Global.System.Data.Common.DbTransaction = CType(m_privateObject.GetFieldOrProperty("_tran"),Global.System.Data.Common.DbTransaction)
            Return ret
        End Get
        Set
            m_privateObject.SetFieldOrProperty("_tran", value)
        End Set
    End Property
    
    Friend Property _closeConnOnCommit() As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
        Get
            Dim _ret_val As Object = m_privateObject.GetFieldOrProperty("_closeConnOnCommit")
            Dim _ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = Nothing
            If (Not (_ret_val) Is Nothing) Then
                _ret = New Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor(_ret_val)
            End If
            Dim ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = _ret
            Return ret
        End Get
        Set
            m_privateObject.SetFieldOrProperty("_closeConnOnCommit", value)
        End Set
    End Property
    
    Friend Property _conn() As Global.System.Data.Common.DbConnection
        Get
            Dim ret As Global.System.Data.Common.DbConnection = CType(m_privateObject.GetFieldOrProperty("_conn"),Global.System.Data.Common.DbConnection)
            Return ret
        End Get
        Set
            m_privateObject.SetFieldOrProperty("_conn", value)
        End Set
    End Property
    
    Friend ReadOnly Property Schema() As Global.Worm.Orm.DbSchema
        Get
            Dim ret As Global.Worm.Orm.DbSchema = CType(m_privateObject.GetProperty("Schema"),Global.Worm.Orm.DbSchema)
            Return ret
        End Get
    End Property
    
    Friend Shared Function CreatePrivate(ByVal schema As Global.Worm.Orm.DbSchema, ByVal connectionString As String) As Global.Worm.Orm.OrmReadOnlyDBManager
        Dim args() As Object = New Object() {schema, connectionString}
        Dim priv_obj As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject = New Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(GetType(Global.Worm.Orm.OrmReadOnlyDBManager), New System.Type() {GetType(Global.Worm.Orm.DbSchema), GetType(String)}, args)
        Return CType(priv_obj.Target,Global.Worm.Orm.OrmReadOnlyDBManager)
    End Function
    
    Friend Function CreateConn() As Global.System.Data.Common.DbConnection
        Dim args(-1) As Object
        Dim ret As Global.System.Data.Common.DbConnection = CType(m_privateObject.Invoke("CreateConn", New System.Type(-1) {}, args),Global.System.Data.Common.DbConnection)
        Return ret
    End Function
    
    Friend Sub Dispose(ByVal disposing As Boolean)
        Dim args() As Object = New Object() {disposing}
        m_privateObject.Invoke("Dispose", New System.Type() {GetType(Boolean)}, args)
    End Sub
    
    Friend Function FindConnected(ByVal ct As Global.System.Type, ByVal selectedType As Global.System.Type, ByVal filterType As Global.System.Type, ByVal field As String, ByVal connectedFilter As Global.Worm.Orm.IOrmFilter, ByVal filter As Global.Worm.Orm.IOrmFilter, ByVal withLoad As Boolean, ByVal sort As String, ByVal sortType As Global.Worm.Orm.SortType, ByVal dosort As Boolean) As Global.System.Collections.IList
        Dim args() As Object = New Object() {ct, selectedType, filterType, field, connectedFilter, filter, withLoad, sort, sortType, dosort}
        Dim ret As Global.System.Collections.IList = CType(m_privateObject.Invoke("FindConnected", New System.Type() {GetType(Global.System.Type), GetType(Global.System.Type), GetType(Global.System.Type), GetType(String), GetType(Global.Worm.Orm.IOrmFilter), GetType(Global.Worm.Orm.IOrmFilter), GetType(Boolean), GetType(String), GetType(Global.Worm.Orm.SortType), GetType(Boolean)}, args),Global.System.Collections.IList)
        Return ret
    End Function
    
    Friend Overloads Function LoadMultipleObjects(ByVal firstType As Global.System.Type, ByVal secondType As Global.System.Type, ByVal cmd As Global.System.Data.Common.DbCommand) As Global.System.Collections.IList
        Dim args() As Object = New Object() {firstType, secondType, cmd}
        Dim ret As Global.System.Collections.IList = CType(m_privateObject.Invoke("LoadMultipleObjects", New System.Type() {GetType(Global.System.Type), GetType(Global.System.Type), GetType(Global.System.Data.Common.DbCommand)}, args),Global.System.Collections.IList)
        Return ret
    End Function
    
    Friend Overloads Function LoadMultipleObjects(ByVal t As Global.System.Type, ByVal cmd As Global.System.Data.Common.DbCommand, ByVal withLoad As Boolean, ByVal arr As System.Collections.Generic.List(Of Worm.Orm.ColumnAttribute), ByVal idx As Integer) As Global.System.Collections.IList
        Dim args() As Object = New Object() {t, cmd, withLoad, arr, idx}
        Dim ret As Global.System.Collections.IList = CType(m_privateObject.Invoke("LoadMultipleObjects", New System.Type() {GetType(Global.System.Type), GetType(Global.System.Data.Common.DbCommand), GetType(Boolean), GetType(System.Collections.Generic.List(Of Worm.Orm.ColumnAttribute)), GetType(Integer)}, args),Global.System.Collections.IList)
        Return ret
    End Function
    
    Friend Function GetObjects(ByVal ct As Global.System.Type, ByVal ids As System.Collections.Generic.IList(Of Integer), ByVal f As Global.Worm.Orm.IOrmFilter, ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Global.System.Collections.IList
        Dim args() As Object = New Object() {ct, ids, f, withLoad, fieldName, idsSorted}
        Dim ret As Global.System.Collections.IList = CType(m_privateObject.Invoke("GetObjects", New System.Type() {GetType(Global.System.Type), GetType(System.Collections.Generic.IList(Of Integer)), GetType(Global.Worm.Orm.IOrmFilter), GetType(Boolean), GetType(String), GetType(Boolean)}, args),Global.System.Collections.IList)
        Return ret
    End Function
    
    Friend Function GetStaticKey() As String
        Dim args(-1) As Object
        Dim ret As String = CType(m_privateObject.Invoke("GetStaticKey", New System.Type(-1) {}, args),String)
        Return ret
    End Function
    
    Friend Function GetFilterInfo() As Object
        Dim args(-1) As Object
        Dim ret As Object = CType(m_privateObject.Invoke("GetFilterInfo", New System.Type(-1) {}, args),Object)
        Return ret
    End Function
    
    Friend Sub LoadObject(ByVal obj As Global.Worm.Orm.OrmBase)
        Dim args() As Object = New Object() {obj}
        m_privateObject.Invoke("LoadObject", New System.Type() {GetType(Global.Worm.Orm.OrmBase)}, args)
    End Sub
    
    Friend Sub LoadSingleObject(ByVal cmd As Global.System.Data.Common.DbCommand, ByVal arr As System.Collections.Generic.IList(Of Worm.Orm.ColumnAttribute), ByVal obj As Global.Worm.Orm.OrmBase, ByVal check_pk As Boolean, ByVal load As Boolean, ByVal modifiedloaded As Boolean)
        Dim args() As Object = New Object() {cmd, arr, obj, check_pk, load, modifiedloaded}
        m_privateObject.Invoke("LoadSingleObject", New System.Type() {GetType(Global.System.Data.Common.DbCommand), GetType(System.Collections.Generic.IList(Of Worm.Orm.ColumnAttribute)), GetType(Global.Worm.Orm.OrmBase), GetType(Boolean), GetType(Boolean), GetType(Boolean)}, args)
    End Sub
    
    Friend Sub LoadFromDataReader(ByVal obj As Global.Worm.Orm.OrmBase, ByVal dr As Global.System.Data.IDataReader, ByVal arr As System.Collections.Generic.IList(Of Worm.Orm.ColumnAttribute), ByVal check_pk As Boolean, ByVal displacement As Integer)
        Dim args() As Object = New Object() {obj, dr, arr, check_pk, displacement}
        m_privateObject.Invoke("LoadFromDataReader", New System.Type() {GetType(Global.Worm.Orm.OrmBase), GetType(Global.System.Data.IDataReader), GetType(System.Collections.Generic.IList(Of Worm.Orm.ColumnAttribute)), GetType(Boolean), GetType(Integer)}, args)
    End Sub
    
    Friend Function TestConn(ByVal cmd As Global.System.Data.Common.DbCommand) As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
        Dim args() As Object = New Object() {cmd}
        Dim _ret_val As Object = m_privateObject.Invoke("TestConn", New System.Type() {GetType(Global.System.Data.Common.DbCommand)}, args)
        Dim _ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = Nothing
        If (Not (_ret_val) Is Nothing) Then
            _ret = New Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor(_ret_val)
        End If
        Dim ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = _ret
        Return ret
    End Function
    
    Friend Sub CloseConn(ByVal b As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor)
        Dim b_val_target As Object = Nothing
        If (Not (b) Is Nothing) Then
            b_val_target = b.Target
        End If
        Dim args() As Object = New Object() {b_val_target}
        Dim target As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType = New Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType("Worm.Orm", "Worm.Orm.OrmReadOnlyDBManager+ConnAction")
        m_privateObject.Invoke("CloseConn", New System.Type() {target.ReferencedType}, args)
    End Sub
    
    Friend Overloads Function GetFilters(ByVal ids As System.Collections.Generic.List(Of Integer), ByVal fieldName As String, ByVal almgr As Global.Worm.Orm.AliasMgr, ByVal params As Global.Worm.Orm.ParamMgr, ByVal original_type As Global.System.Type, ByVal idsSorted As Boolean) As System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer))
        Dim args() As Object = New Object() {ids, fieldName, almgr, params, original_type, idsSorted}
        Dim ret As System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer)) = CType(m_privateObject.Invoke("GetFilters", New System.Type() {GetType(System.Collections.Generic.List(Of Integer)), GetType(String), GetType(Global.Worm.Orm.AliasMgr), GetType(Global.Worm.Orm.ParamMgr), GetType(Global.System.Type), GetType(Boolean)}, args), System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer)))
        Return ret
    End Function
    
    Friend Overloads Function GetFilters(ByVal ids As System.Collections.Generic.List(Of Integer), ByVal table As Global.Worm.Orm.OrmTable, ByVal column As String, ByVal almgr As Global.Worm.Orm.AliasMgr, ByVal params As Global.Worm.Orm.ParamMgr, ByVal idsSorted As Boolean) As System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer))
        Dim args() As Object = New Object() {ids, table, column, almgr, params, idsSorted}
        Dim ret As System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer)) = CType(m_privateObject.Invoke("GetFilters", New System.Type() {GetType(System.Collections.Generic.List(Of Integer)), GetType(Global.Worm.Orm.OrmTable), GetType(String), GetType(Global.Worm.Orm.AliasMgr), GetType(Global.Worm.Orm.ParamMgr), GetType(Boolean)}, args), System.Collections.Generic.IEnumerable(Of Pair(Of String, Integer)))
        Return ret
    End Function
    
    Friend Sub SaveObject(ByVal obj As Global.Worm.Orm.OrmBase)
        Dim args() As Object = New Object() {obj}
        m_privateObject.Invoke("SaveObject", New System.Type() {GetType(Global.Worm.Orm.OrmBase)}, args)
    End Sub
    
    Friend Sub DeleteObject(ByVal obj As Global.Worm.Orm.OrmBase)
        Dim args() As Object = New Object() {obj}
        m_privateObject.Invoke("DeleteObject", New System.Type() {GetType(Global.Worm.Orm.OrmBase)}, args)
    End Sub
    
    Friend Sub M2MSave(ByVal obj As Global.Worm.Orm.OrmBase, ByVal t As Global.System.Type, ByVal direct As Boolean, ByVal el As Global.Worm.Orm.EditableList)
        Dim args() As Object = New Object() {obj, t, direct, el}
        m_privateObject.Invoke("M2MSave", New System.Type() {GetType(Global.Worm.Orm.OrmBase), GetType(Global.System.Type), GetType(Boolean), GetType(Global.Worm.Orm.EditableList)}, args)
    End Sub
    
    Friend Function GetSearchSection() As String
        Dim args(-1) As Object
        Dim ret As String = CType(m_privateObject.Invoke("GetSearchSection", New System.Type(-1) {}, args),String)
        Return ret
    End Function
End Class


<System.Diagnostics.DebuggerStepThrough(),  _
 System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TestTools.UnitTestGeneration", "1.0.0.0")>  _
Friend Class BaseAccessor
    
    Protected m_privateObject As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject
    
    Protected Sub New(ByVal target As Object, ByVal type As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType)
        MyBase.New
        m_privateObject = New Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(target, type)
    End Sub
    
    Protected Sub New(ByVal type As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType)
        Me.New(Nothing, type)
    End Sub
    
    Friend Overridable ReadOnly Property Target() As Object
        Get
            Return m_privateObject.Target
        End Get
    End Property
    
    Public Overrides Function ToString() As String
        Return Me.Target.ToString
    End Function
    
    Public Overloads Overrides Function Equals(ByVal obj As Object) As Boolean
        If GetType(BaseAccessor).IsInstanceOfType(obj) Then
            obj = CType(obj,BaseAccessor).Target
        End If
        Return Me.Target.Equals(obj)
    End Function
    
    Public Overrides Function GetHashCode() As Integer
        Return Me.Target.GetHashCode
    End Function
End Class

<System.Diagnostics.DebuggerStepThrough(),  _
 System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TestTools.UnitTestGeneration", "1.0.0.0")>  _
Friend Class Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
    Inherits BaseAccessor
    
    Protected Shared m_privateType As Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType = New Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType("Worm.Orm", "Worm.Orm.OrmReadOnlyDBManager+ConnAction")
    
    Friend Sub New(ByVal target As Object)
        MyBase.New(target, m_privateType)
    End Sub
    
    Friend Shared ReadOnly Property Leave() As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
        Get
            Dim _ret_val As Object = m_privateType.GetStaticFieldOrProperty("Leave")
            Dim _ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = Nothing
            If (Not (_ret_val) Is Nothing) Then
                _ret = New Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor(_ret_val)
            End If
            Dim ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = _ret
            Return ret
        End Get
    End Property
    
    Friend Shared ReadOnly Property Destroy() As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
        Get
            Dim _ret_val As Object = m_privateType.GetStaticFieldOrProperty("Destroy")
            Dim _ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = Nothing
            If (Not (_ret_val) Is Nothing) Then
                _ret = New Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor(_ret_val)
            End If
            Dim ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = _ret
            Return ret
        End Get
    End Property
    
    Friend Shared ReadOnly Property Close() As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor
        Get
            Dim _ret_val As Object = m_privateType.GetStaticFieldOrProperty("Close")
            Dim _ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = Nothing
            If (Not (_ret_val) Is Nothing) Then
                _ret = New Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor(_ret_val)
            End If
            Dim ret As Global.TestProject1.Worm_Orm_OrmReadOnlyDBManager_ConnActionAccessor = _ret
            Return ret
        End Get
    End Property
End Class