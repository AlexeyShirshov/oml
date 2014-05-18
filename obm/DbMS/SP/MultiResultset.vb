Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Threading

Namespace Database.Storedprocs

    Public MustInherit Class MultiResultsetQueryEntityStoredProcBase
        Inherits QueryStoredProcBase

#Region " Descriptors "

        Public Interface IResultSetDescriptor
            Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String)
            Sub BeginProcess(ByVal mgr As OrmManager)
            Sub EndProcess(ByVal mgr As OrmManager)
        End Interface

        Public MustInherit Class EntityDescriptor(Of T As {_IEntity, New})
            Implements IResultSetDescriptor

            Private _l As List(Of T)
            Private _created As Boolean
            Private _o As Object
            Private _count As Integer
            Private _loaded As Integer
            Private _oschema As IEntitySchema
            Private _cm As Collections.IndexedCollection(Of String, MapField2Column)
            'Private _cols As List(Of EntityPropertyAttribute)
            Private _entityDictionary As IDictionary
            Private _sl As SpinLockRef

            Public Overridable Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal cmdtext As String) Implements IResultSetDescriptor.ProcessReader
                'Dim mgr As OrmReadOnlyDBManager = CType(OrmManager.CurrentManager, OrmReadOnlyDBManager)
                If mgr._externalFilter IsNot Nothing Then
                    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                End If
                Dim original_type As Type = GetType(T)
                If _l Is Nothing Then
                    _l = New List(Of T)
                    _sl = New SpinLockRef
                    Dim mpe As ObjectMappingEngine = mgr.MappingEngine
                    _oschema = mpe.GetEntitySchema(original_type)
                    _cm = _oschema.FieldColumnMap
                    '_cols = mpe.GetSortedFieldList(original_type, _oschema)
                    _entityDictionary = mgr.GetDictionary(original_type)
                End If
                Dim loaded As Integer
                mgr.LoadFromResultSet(Of T)(_l, GetColumns, New DataReaderAbstraction(dr, _l.Count, dr), _sl, _entityDictionary, loaded, _l.Count, _oschema, _cm)
                _loaded += loaded
            End Sub

            Protected MustOverride Function GetColumns() As List(Of SelectExpression)
            'Protected MustOverride Function GetWithLoad() As Boolean

            Public Function GetObjects(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
                If _o Is Nothing Then
                    Throw New InvalidOperationException("Stored procedure is not executed")
                End If
                Dim tt As Type = GetType(T)
                If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                    _count = mgr.ListConverter.GetCount(_o)
                    Dim mi As Reflection.MethodInfo = Nothing
                    If Not _fromWeakListNP.TryGetValue(tt, mi) Then
                        Dim tmi As Reflection.MethodInfo = GetType(IListObjectConverter).GetMethod("FromWeakList", New Type() {GetType(Object), GetType(OrmManager)})
                        mi = tmi.MakeGenericMethod(New Type() {tt})
                        _fromWeakListNP(tt) = mi
                    End If
                    Return CType(mi.Invoke(mgr.ListConverter, New Object() {_o, mgr}), Global.Worm.ReadOnlyObjectList(Of T))
                Else
                    Dim l As ReadOnlyObjectList(Of T) = CType(_o, Global.Worm.ReadOnlyObjectList(Of T))
                    _count = l.Count
                    Return l
                End If

                'Dim s As IListObjectConverter.ExtractListResult
                'Dim r As ReadOnlyList(Of T) = _ce.GetObjectList(Of T)(mgr, GetWithLoad, _created, s)
                'If s <> IListObjectConverter.ExtractListResult.Successed Then
                '    Throw New InvalidOperationException("External filter is not applicable for store procedures")
                'End If
                'Return r
            End Function

            Public ReadOnly Property Count() As Integer
                Get
                    Return _count
                End Get
            End Property

            Public ReadOnly Property LoadedInResultset() As Integer
                Get
                    Return _loaded
                End Get
            End Property

            Public Overridable Sub EndProcess(ByVal mgr As OrmManager) Implements IResultSetDescriptor.EndProcess
                Dim l As ReadOnlyObjectList(Of T) = CType(OrmManager._CreateReadOnlyList(GetType(T), _l), Global.Worm.ReadOnlyObjectList(Of T))
                If GetType(ICachedEntity).IsAssignableFrom(GetType(T)) Then
                    _o = mgr.ListConverter.ToWeakList(l)
                Else
                    _o = l
                End If
                _l = Nothing
            End Sub

            Public Overridable Sub BeginProcess(ByVal mgr As OrmManager) Implements IResultSetDescriptor.BeginProcess

            End Sub
        End Class

#End Region

        Protected Sub New(ByVal cache As Boolean)
            MyBase.New(cache)
        End Sub

        Protected Sub New(ByVal timeout As TimeSpan)
            MyBase.New(timeout)
        End Sub

        Protected Sub New()
            MyBase.New(True)
        End Sub

        Protected MustOverride Function CreateDescriptor(ByVal resultsetIdx As Integer) As IResultSetDescriptor

        Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal resultSet As Integer, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Dim desc As List(Of IResultSetDescriptor) = CType(result, List(Of IResultSetDescriptor))
            Dim rd As IResultSetDescriptor = Nothing
            If desc.Count - 1 < resultSet Then
                rd = CreateDescriptor(resultSet)
                rd.BeginProcess(mgr)
                desc.Add(rd)
            Else
                rd = desc(resultSet)
            End If

            If rd Is Nothing Then
                Throw New InvalidOperationException(String.Format("Resultset descriptor for resultset #{0} is nothing", resultSet))
            End If

            rd.ProcessReader(mgr, dr, GetName)
        End Sub

        Protected Overrides Sub ProcessReader(ByVal mgr As OrmReadOnlyDBManager, ByVal dr As System.Data.Common.DbDataReader, ByVal result As Object)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function InitResult() As Object
            Return New List(Of IResultSetDescriptor)
        End Function

        Public Overloads Function GetResult(ByVal mgr As OrmReadOnlyDBManager) As List(Of IResultSetDescriptor)
            Return CType(MyBase.GetResult(mgr), List(Of IResultSetDescriptor))
        End Function

        Protected Overrides Sub EndProcess(ByVal result As Object, ByVal mgr As OrmManager)
            For Each d As IResultSetDescriptor In CType(result, IList)
                d.EndProcess(mgr)
            Next
        End Sub
    End Class
End Namespace