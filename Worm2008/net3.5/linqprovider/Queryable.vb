Imports System.Linq.Expressions
Imports Worm.Database
Imports System.Runtime.CompilerServices

Namespace Linq
    Public MustInherit Class WormContext

        Public MustOverride Function CreateReadonlyManager() As OrmManager
        Public MustOverride Function CreateManager() As OrmManager

        Private _schema As ObjectMappingEngine
        Public Property Schema() As ObjectMappingEngine
            Get
                Return _schema
            End Get
            Set(ByVal value As ObjectMappingEngine)
                _schema = value
            End Set
        End Property

        Private _cache As Cache.CacheBase
        Public Property Cache() As Cache.CacheBase
            Get
                Return _cache
            End Get
            Set(ByVal value As Cache.CacheBase)
                _cache = value
            End Set
        End Property

        Public Function CreateQueryWrapper(ByVal t As Type) As QueryWrapper
            Return New QueryWrapper(t, Me)
        End Function

        Public Function CreateQueryWrapper(ByVal t As Type, ByVal provider As WormLinqProvider, ByVal exp As Expression) As QueryWrapper
            Return New QueryWrapper(provider, exp, t)
        End Function

        Public Function CreateQueryWrapper(Of T)() As QueryWrapperT(Of T)
            Return New QueryWrapperT(Of T)(Me)
        End Function

        Public Function CreateQueryWrapper(Of T)(ByVal provider As WormLinqProvider, ByVal exp As Expression) As QueryWrapperT(Of T)
            Return New QueryWrapperT(Of T)(provider, exp)
        End Function
    End Class

    Public Class WormDBContext
        Inherits WormContext

        Private _conn As String

        Public Sub New(ByVal conn As String)
            MyClass.New(New Cache.ReadonlyCache, New SQLGenerator("1"), conn)
        End Sub

        Public Sub New(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine, ByVal conn As String)
            _conn = conn
            Me.Cache = cache
            Me.Schema = schema
        End Sub

        Public Overrides Function CreateManager() As OrmManager
            Return New OrmDBManager(CType(Cache, Worm.Cache.OrmCache), CType(Schema, SQLGenerator), _conn)
        End Function

        Public Overrides Function CreateReadonlyManager() As OrmManager
            Return New OrmReadOnlyDBManager(Cache, CType(Schema, SQLGenerator), _conn)
        End Function
    End Class

    Public Class WormMSSQL2005DBContext
        Inherits WormDBContext

        Public Sub New(ByVal conn As String)
            MyBase.New(New Cache.OrmCache, New MSSQL2005Generator("1"), conn)
        End Sub
    End Class

    Public Class QueryWrapper
        Implements IQueryable, IOrderedQueryable

        Private _provider As WormLinqProvider
        Protected _expression As Expression
        Private _t As Type

        Public Sub New(ByVal t As Type, ByVal ctx As WormContext)
            MyClass.New(ctx)
            _t = t
        End Sub

        Protected Sub New(ByVal ctx As WormContext)
            _provider = New WormLinqProvider(ctx)
            _expression = System.Linq.Expressions.Expression.Constant(Me)
        End Sub

        Protected Sub New(ByVal provider As WormLinqProvider, ByVal exp As Expression)
            _provider = provider
            _expression = exp
        End Sub

        Public Sub New(ByVal provider As WormLinqProvider, ByVal exp As Expression, ByVal t As Type)
            _provider = provider
            _expression = exp
            _t = t
        End Sub

        Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return CType(Provider.Execute(Expression), System.Collections.IEnumerator)
        End Function

        Public Overridable ReadOnly Property ElementType() As System.Type Implements System.Linq.IQueryable.ElementType
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property Expression() As System.Linq.Expressions.Expression Implements System.Linq.IQueryable.Expression
            Get
                Return _expression
            End Get
        End Property

        Public ReadOnly Property Provider() As System.Linq.IQueryProvider Implements System.Linq.IQueryable.Provider
            Get
                Return _provider
            End Get
        End Property

    End Class

    Public Class QueryWrapperT(Of T)
        Inherits QueryWrapper
        Implements IQueryable(Of T), IOrderedQueryable(Of T)

        Public Sub New(ByVal ctx As WormContext)
            MyBase.new(ctx)
        End Sub

        Public Sub New(ByVal provider As WormLinqProvider, ByVal exp As Expression)
            MyBase.New(provider, exp)
        End Sub

        Public Overloads Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
            Return Provider.Execute(Of IEnumerator(Of T))(Expression)
        End Function

        Public Overrides ReadOnly Property ElementType() As System.Type
            Get
                Return GetType(T)
            End Get
        End Property

        Public Function WithLoad() As QueryWrapperT(Of T)
            _expression = New WithLoadExpression(GetType(QueryWrapperT(Of T)), _expression)
            Return Me
        End Function

    End Class

    Public Class WithLoadExpression
        Inherits Expression

        Private _exp As Expression
        Public Property InnerExpression() As Expression
            Get
                Return _exp
            End Get
            Set(ByVal value As Expression)
                _exp = value
            End Set
        End Property

        Public Sub New(ByVal t As Type, ByVal exp As Expression)
            MyBase.New(CType(1000, ExpressionType), t)
            _exp = exp
            'ExpressionType.m()
        End Sub
    End Class

    'Public Module t
    '    <ExtensionAttribute()> _
    '    Public Function WithLoad(Of TSource As Orm.OrmBase)(ByVal source As IQueryable(Of TSource)) As IQueryable(Of TSource)
    '        Return source
    '    End Function
    'End Module
End Namespace