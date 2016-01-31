Imports System.Data.Common

Namespace Database
    Public Class ConnectionMgr
        Implements IDisposable

        Private _mgr As OrmReadOnlyDBManager
        Private _b As OrmReadOnlyDBManager.ConnAction

        Public Sub New(mgr As OrmReadOnlyDBManager)
            _mgr = mgr
            _b = mgr.TestConn(Nothing)
        End Sub

        Public ReadOnly Property Connection As DbConnection
            Get
                Return _mgr._conn
            End Get
        End Property
        Public ReadOnly Property Transaction As DbTransaction
            Get
                Return _mgr._tran
            End Get
        End Property
        Public ReadOnly Property Manager As OrmReadOnlyDBManager
            Get
                Return _mgr
            End Get
        End Property
#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    _mgr.CloseConn(_b)
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class
End Namespace