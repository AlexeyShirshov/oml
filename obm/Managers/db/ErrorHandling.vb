﻿Namespace Database
    Partial Public Class OrmReadOnlyDBManager

        Public Class ConnectionExceptionArgs
            Inherits EventArgs

            Enum ActionEnum
                Rethrow
                RetryOldConnection
                ''' <summary>
                ''' Create new connection and retry
                ''' </summary>
                ''' <remarks>New connection string in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RetryNewConnection
                ''' <summary>
                ''' Rethrow custom exception
                ''' </summary>
                ''' <remarks>Custom exception in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RethrowCustom
            End Enum

            Property Action As ActionEnum

            Property Context As Object

            Private _ex As Data.Common.DbException
            Public ReadOnly Property Exception() As Data.Common.DbException
                Get
                    Return _ex
                End Get
            End Property

            Private _conn As System.Data.Common.DbConnection
            Public ReadOnly Property Connection() As System.Data.Common.DbConnection
                Get
                    Return _conn
                End Get
            End Property

            Public Sub New(ex As Data.Common.DbException, conn As System.Data.Common.DbConnection)
                _ex = ex
                _conn = conn
            End Sub
        End Class

        Public Class CommandExceptionArgs
            Inherits EventArgs

            Enum ActionEnum
                Rethrow
                RetryOldConnection
                ''' <summary>
                ''' Create new connection and retry
                ''' </summary>
                ''' <remarks>New connection string in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RetryNewConnection
                ''' <summary>
                ''' Rethrow custom exception
                ''' </summary>
                ''' <remarks>Custom exception in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RethrowCustom
                RetryNewCommand
                'RetryNewCommandOnNewConnection
            End Enum

            Property Action As ActionEnum

            Property Context As Object

            Private _ex As Data.Common.DbException
            Public ReadOnly Property Exception() As Data.Common.DbException
                Get
                    Return _ex
                End Get
            End Property

            Private _cmd As System.Data.Common.DbCommand
            Public ReadOnly Property Command() As System.Data.Common.DbCommand
                Get
                    Return _cmd
                End Get
            End Property

            Public Sub New(ex As Data.Common.DbException, cmd As System.Data.Common.DbCommand)
                _ex = ex
                _cmd = cmd
            End Sub
        End Class
    End Class
End Namespace