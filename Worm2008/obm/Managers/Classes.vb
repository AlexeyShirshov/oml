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
        End If
    End Sub

    Public ReadOnly Property Manager() As OrmManager Implements IGetManager.Manager
        Get
            Return _mgr
        End Get
    End Property

#Region " IDisposable Support "
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

Namespace Orm.Query

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
        Public MustOverride Function MakeStmt(ByVal s As ObjectMappingEngine) As String

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

        Public Overrides Function MakeStmt(ByVal s As ObjectMappingEngine) As String
            Return "distinct "
        End Function
    End Class

    Public MustInherit Class TopAspect
        Inherits QueryAspect

        Private _top As Integer
        Private _sort As Worm.Sorting.Sort

        Public Sub New(ByVal top As Integer)
            MyBase.New(AspectType.Columns)
            _top = top
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort As Worm.Sorting.Sort)
            MyBase.New(AspectType.Columns)
            _top = top
            _sort = sort
        End Sub

        Public Overrides Function GetDynamicKey() As String
            Return "-top-" & Top.ToString & "-"
        End Function

        Public Overrides Function GetStaticKey() As String
            If _sort IsNot Nothing Then
                Return "-top-" & _sort.ToString
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
        Inherits Orm.Query.TopAspect

        Public Sub New(ByVal top As Integer)
            MyBase.New(top)
        End Sub

        Public Sub New(ByVal top As Integer, ByVal sort As Worm.Sorting.Sort)
            MyBase.New(top, sort)
        End Sub

        Public Overrides Function MakeStmt(ByVal s As ObjectMappingEngine) As String
            Return CType(s, SQLGenerator).TopStatement(Top)
        End Function
    End Class

End Namespace