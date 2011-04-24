Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Expressions2

<Serializable()> _
Public NotInheritable Class OrmManagerException
    Inherits Exception

    Public Sub New()
        ' Add other code for custom properties here.
    End Sub

    Public Sub New(ByVal message As String)
        MyBase.New(message)
        ' Add other code for custom properties here.
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
        ' Add other code for custom properties here.
    End Sub

    Private Sub New( _
        ByVal info As System.Runtime.Serialization.SerializationInfo, _
        ByVal context As System.Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
        ' Insert code here for custom properties here.
    End Sub
End Class

Public Interface IGetManager
    Inherits IDisposable

    ReadOnly Property Manager() As OrmManager
End Interface

Public Interface ICreateManager
    Function CreateManager() As OrmManager
End Interface

Public Interface IAdminManager
    Function UpdateObject(ByVal obj As Entities._ICachedEntity) As Boolean
    Function InsertObject(ByVal obj As Entities._ICachedEntity) As Boolean
    Sub DeleteObject(ByVal obj As Entities.ICachedEntity)
    Function Delete(ByVal f As Criteria.Core.IEntityFilter) As Integer
    Sub M2MSave(ByVal obj As Entities.ISinglePKEntity, ByVal t As Type, ByVal direct As String, ByVal el As Entities.M2MRelation)
End Interface

Class GetManagerDisposable
    Implements IDisposable, IGetManager

    Private _mgr As OrmManager
    Private _oldSchema As ObjectMappingEngine

    Public Sub New(ByVal mgr As OrmManager, ByVal schema As ObjectMappingEngine)
        _mgr = mgr
        If Not mgr.MappingEngine.Equals(schema) AndAlso schema IsNot Nothing Then
            _oldSchema = mgr.MappingEngine
            mgr.SetSchema(schema)
        End If
    End Sub

    Private ReadOnly Property Manager() As OrmManager Implements IGetManager.Manager
        Get
            Return _mgr
        End Get
    End Property

    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free other state (managed objects).
            End If

            If _oldSchema IsNot Nothing Then
                _mgr.SetSchema(_oldSchema)
            End If

            _mgr.Dispose()
        End If
        Me.disposedValue = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class

Class ManagerWrapper
    Implements IGetManager

    Private _mgr As OrmManager
    Private _oldSchema As ObjectMappingEngine

    Public Sub New(ByVal mgr As OrmManager, ByVal schema As ObjectMappingEngine)
        _mgr = mgr
        If Not mgr.MappingEngine.Equals(schema) AndAlso schema IsNot Nothing Then
            _oldSchema = mgr.MappingEngine
            mgr.SetSchema(schema)
            AddHandler mgr.ObjectLoaded, AddressOf OnObjectLoaded
        End If
    End Sub

    Public ReadOnly Property Manager() As OrmManager Implements IGetManager.Manager
        Get
            Return _mgr
        End Get
    End Property

    Protected Sub OnObjectLoaded(ByVal sender As OrmManager, ByVal o As IEntity)
        CType(o, _IEntity).SpecificMappingEngine = sender.MappingEngine
    End Sub

#Region " IDisposable Support "
    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free other state (managed objects).
            End If

            RemoveHandler _mgr.ObjectLoaded, AddressOf OnObjectLoaded

            If _oldSchema IsNot Nothing Then
                _mgr.SetSchema(_oldSchema)
            End If
        End If
        Me.disposedValue = True
    End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class

Public Class SetManagerHelper
    Implements IDisposable

    Private _m As CreateManagerDelegate
    Private _gm As ICreateManager
    Private _mgr As OrmManager
    Private _schema As ObjectMappingEngine

    Public Sub New(ByVal mgr As OrmManager, ByVal getMgr As CreateManagerDelegate, ByVal schema As ObjectMappingEngine)
        _m = getMgr
        _mgr = mgr
        _schema = schema
        Subscribe()
    End Sub

    Public Sub New(ByVal mgr As OrmManager, ByVal getMgr As ICreateManager, ByVal schema As ObjectMappingEngine)
        _gm = getMgr
        _mgr = mgr
        _schema = schema
        Subscribe()
    End Sub

    Protected Sub Subscribe()
        AddHandler _mgr.ObjectRestoredFromCache, AddressOf ObjectRestored
        AddHandler _mgr.ObjectLoaded, AddressOf ObjectCreated
    End Sub

    Public Sub ObjectRestored(ByVal mgr As OrmManager, ByVal created As Boolean, ByVal o As IEntity)
        Dim e As _IEntity = CType(o, _IEntity)
        If Not Equals(e.SpecificMappingEngine, _schema) Then
            e.SpecificMappingEngine = _schema
        End If
        ObjectCreated(mgr, o)
    End Sub

    Public Sub ObjectCreated(ByVal mgr As OrmManager, ByVal o As IEntity)
        If _m Is Nothing Then
            CType(o, _IEntity).SetCreateManager(_gm)
        Else
            CType(o, _IEntity).SetCreateManager(New CreateManager(_m))
        End If
    End Sub

#Region " IDisposable Support "
    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free other state (managed objects).
            End If

            RemoveHandler _mgr.ObjectLoaded, AddressOf ObjectCreated
            RemoveHandler _mgr.ObjectRestoredFromCache, AddressOf ObjectRestored
        End If
        Me.disposedValue = True
    End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class

Public Delegate Function CreateManagerDelegate() As OrmManager

Public Class CreateManager
    Implements ICreateManager

    Private _del As CreateManagerDelegate

    Public Sub New(ByVal createManDelegate As CreateManagerDelegate)
        _del = createManDelegate
    End Sub

    Public Function CreateManager() As OrmManager Implements ICreateManager.CreateManager
        Return _del()
    End Function
End Class

#If Not ExcludeFindMethods Then
Namespace Entities.Query

    Public MustInherit Class QueryAspect
        Public Enum AspectType
            Columns
        End Enum

        Private _type As AspectType

        Public ReadOnly Property AscpectType() As AspectType
            Get
                Return _type
            End Get
        End Property

        Public MustOverride Function GetStaticKey() As String
        Public MustOverride Function GetDynamicKey() As String
        Public MustOverride Function MakeStmt(ByVal s As StmtGenerator) As String

        Public Sub New(ByVal type As AspectType)
            _type = type
        End Sub
    End Class

    Public Class DistinctAspect
        Inherits QueryAspect

        Public Sub New()
            MyBase.New(AspectType.Columns)
        End Sub

        Public Overrides Function GetDynamicKey() As String
            Return String.Empty
        End Function

        Public Overrides Function GetStaticKey() As String
            Return "distinct"
        End Function

        Public Overrides Function MakeStmt(ByVal s As StmtGenerator) As String
            Return "distinct "
        End Function
    End Class

    Public MustInherit Class TopAspect
        Inherits QueryAspect

        Private _top As Integer
        Private _sort() As SortExpression

        Public Sub New(ByVal top As Integer)
            MyBase.New(AspectType.Columns)
            _top = top
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort() As SortExpression)
            MyBase.New(AspectType.Columns)
            _top = top
            _sort = sort
        End Sub

        Public Overrides Function GetDynamicKey() As String
            Return "-top-" & Top.ToString & "-"
        End Function

        Public Overrides Function GetStaticKey() As String
            If _sort IsNot Nothing Then
                Return "-top-" & BinaryExpressionBase.CreateFromEnumerable(_sort).GetStaticString(Nothing, Nothing)
            End If
            Return "-top-"
        End Function

        Protected ReadOnly Property Top() As Integer
            Get
                Return _top
            End Get
        End Property
    End Class

End Namespace

Namespace Database
    Public Class TopAspect
        Inherits Entities.Query.TopAspect

        Public Sub New(ByVal top As Integer)
            MyBase.New(top)
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort As SortExpression)
            MyBase.New(top, sort)
        End Sub

        Public Overrides Function MakeStmt(ByVal s As StmtGenerator) As String
            Return s.TopStatement(Top)
        End Function
    End Class

End Namespace

#End If
