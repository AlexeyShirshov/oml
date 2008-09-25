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

    ReadOnly Property Manager() As OrmManagerBase
End Interface

Public Interface ICreateManager
    Function CreateManager() As OrmManagerBase
End Interface

Class GetManagerDisposable
    Implements IDisposable, IGetManager

    Private _mgr As OrmManagerBase

    Public Sub New(ByVal mgr As OrmManagerBase)
        _mgr = mgr
    End Sub

    Private ReadOnly Property Manager() As OrmManagerBase Implements IGetManager.Manager
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

    Private _mgr As OrmManagerBase

    Public Sub New(ByVal mgr As OrmManagerBase)
        _mgr = mgr
    End Sub

    Public ReadOnly Property Manager() As OrmManagerBase Implements IGetManager.Manager
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

Public Delegate Function CreateManagerDelegate() As OrmManagerBase

Public Class CreateManager
    Implements ICreateManager

    Private _del As CreateManagerDelegate

    Public Sub New(ByVal createManDelegate As CreateManagerDelegate)
        _del = createManDelegate
    End Sub

    Public Function CreateManager() As OrmManagerBase Implements ICreateManager.CreateManager
        Return _del()
    End Function
End Class