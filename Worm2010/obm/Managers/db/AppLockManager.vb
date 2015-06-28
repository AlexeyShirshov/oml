Namespace Database
    Public Class AppLockManager
        Implements IDisposable

        Private _name As String
        Private _mgr As OrmReadOnlyDBManager
        Private _lockResult As Integer?
        Private _msg As EventArgs
        Public Sub New(mgr As OrmReadOnlyDBManager, name As String,
                                      Optional lockTimeout As Integer? = Nothing,
                                      Optional lockType As LockTypeEnum = LockTypeEnum.Exclusive)
            _name = name
            _mgr = mgr
            AddHandler mgr.InfoMessage, Sub(s, e)
                                            _msg = e
                                        End Sub
            _lockResult = mgr.AquireAppLock(name, lockTimeout, lockType)
        End Sub

        Public ReadOnly Property LockAquired As Boolean
            Get
                Return _lockResult.HasValue AndAlso _lockResult.Value >= 0
            End Get
        End Property

        Public ReadOnly Property LockResult As Integer?
            Get
                Return _lockResult
            End Get
        End Property

        Public Function ReleaseLock() As Integer?
            If LockAquired Then
                Return _mgr.ReleaseAppLock(_name)
            End If

            Return Nothing
        End Function

        Public ReadOnly Property Messages As EventArgs
            Get
                Return _msg
            End Get
        End Property


#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ReleaseLock()
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