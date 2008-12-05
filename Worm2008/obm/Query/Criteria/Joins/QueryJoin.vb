Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Criteria.Core

Namespace Criteria.Joins
    Public Enum JoinType
        Join
        LeftOuterJoin
        RightOuterJoin
        FullJoin
        CrossJoin
    End Enum

    Public Class QueryJoin
        Implements IQueryElement, ICloneable

        Protected _table As SourceFragment
        Protected _joinType As Worm.Criteria.Joins.JoinType
        Protected _condition As Core.IFilter
        'Protected _type As Type
        'Protected _en As String
        Protected _src As ObjectSource
        Private _jt As Type
        Private _jen As String
        Private _key As String

        Public Sub New(ByVal table As SourceFragment, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _table = table
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New ObjectSource(type)
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New ObjectSource([alias])
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal entityName As String, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _src = New ObjectSource(entityName)
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal joinEntityType As Type)
            _src = New ObjectSource(type)
            _joinType = joinType
            _condition = Condition
            _jt = joinEntityType
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal joinEntityName As String)
            _src = New ObjectSource(type)
            _joinType = joinType
            _condition = Condition
            _jen = joinEntityName
        End Sub

        Public Shared Function IsEmpty(ByVal j As QueryJoin) As Boolean
            Return j Is Nothing
        End Function

        Public Function MakeSQLStmt(ByVal mpe As ObjectMappingEngine, ByVal schema As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'If IsEmpty Then
            '    Throw New InvalidOperationException("Object must be created")
            'End If

            If Condition Is Nothing Then
                Throw New InvalidOperationException("Join condition must be specified")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("almgr")
            End If

            Dim tbl As SourceFragment = _table
            If tbl Is Nothing Then
                'If _type IsNot Nothing Then
                '    tbl = schema.GetTables(_type)(0)
                'Else
                '    tbl = schema.GetTables(schema.GetTypeByEntityName(_en))(0)
                'End If
                tbl = mpe.GetTables(ObjectSource.GetRealType(mpe))(0)
            End If

            Dim alTable As SourceFragment = tbl
            'Dim tableAliases As IDictionary(Of SourceFragment, String) = almgr.Aliases
            If Not almgr.ContainsKey(tbl, ObjectSource) Then
                almgr.AddTable(tbl, ObjectSource, pname)
            End If
            'Dim table As String = _table
            'Dim sch as IOrmObjectSchema = schema.GetObjectSchema(
            Return JoinTypeString() & schema.GetTableName(tbl) & " " & almgr.GetAlias(alTable, ObjectSource) & " on " & Condition.MakeQueryStmt(mpe, schema, filterInfo, almgr, pname, Nothing)
        End Function

        Public Function JoinTypeString() As String
            Select Case _joinType
                Case Worm.Criteria.Joins.JoinType.Join
                    Return " join "
                Case Worm.Criteria.Joins.JoinType.LeftOuterJoin
                    Return " left join "
                Case Worm.Criteria.Joins.JoinType.RightOuterJoin
                    Return " right join "
                Case Worm.Criteria.Joins.JoinType.FullJoin
                    Return " full join "
                Case Worm.Criteria.Joins.JoinType.CrossJoin
                    Return " cross join "
                Case Else
                    Throw New ObjectMappingException("invalid join type " & _joinType.ToString)
            End Select
        End Function

        Public Property Condition() As Core.IFilter
            Get
                Return _condition
            End Get
            Protected Friend Set(ByVal value As Core.IFilter)
                _condition = value
            End Set
        End Property

        Public Sub ReplaceFilter(ByVal replacement As Core.IFilter, ByVal replacer As Core.IFilter)
            _condition = _condition.ReplaceFilter(replacement, replacer)
        End Sub

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition.GetStaticString(mpe)
                'ElseIf _type IsNot Nothing Then
                '    Return gs(_type.ToString, mpe)
                'Else
                '    Return gs(_en, mpe)
            Else
                Return gs(_src.ToStaticString, mpe)
            End If
        End Function

        Private Function gs(ByVal s As String, ByVal mpe As ObjectMappingEngine) As String
            If _condition IsNot Nothing Then
                Return s & JoinTypeString() & _condition.GetStaticString(mpe)
            Else
                If _jt IsNot Nothing Then
                    Return s & JoinTypeString() & _jt.ToString & _key
                Else
                    Return s & JoinTypeString() & _jen & _key
                End If
            End If
        End Function

        Public Overrides Function ToString() As String
            Throw New NotImplementedException
        End Function

        Public Function _ToString() As String Implements IQueryElement._ToString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition._ToString
                'ElseIf _type IsNot Nothing Then
                '    Return gd(_type.ToString)
                'Else
                '    Return gd(_en)
            Else
                Return gd(_src.ToStaticString)
            End If
        End Function

        Private Function gd(ByVal s As String) As String
            If _condition IsNot Nothing Then
                Return s & JoinTypeString() & _condition._ToString
            Else
                If _jt IsNot Nothing Then
                    Return s & JoinTypeString() & _jt.ToString & _key
                Else
                    Return s & JoinTypeString() & _jen & _key
                End If
            End If
        End Function

        Public Property M2MKey() As String
            Get
                Return _key
            End Get
            Set(ByVal value As String)
                _key = value
            End Set
        End Property

        Public Property M2MJoinType() As Type
            Get
                Return _jt
            End Get
            Set(ByVal value As Type)
                _jt = value
            End Set
        End Property

        Public Property M2MJoinEntityName() As String
            Get
                Return _jen
            End Get
            Set(ByVal value As String)
                _jen = value
            End Set
        End Property

        Public Property ObjectSource() As ObjectSource
            Get
                Return _src
            End Get
            Friend Set(ByVal value As ObjectSource)
                _src = value
            End Set
        End Property

        'Public Property Type() As Type
        '    Get
        '        Return _type
        '    End Get
        '    Friend Set(ByVal value As Type)
        '        _type = value
        '    End Set
        'End Property

        'Public Property EntityName() As String
        '    Get
        '        Return _en
        '    End Get
        '    Friend Set(ByVal value As String)
        '        _en = value
        '    End Set
        'End Property

        Public Property Table() As SourceFragment
            Get
                Return _table
            End Get
            Friend Set(ByVal value As SourceFragment)
                _table = value
            End Set
        End Property

        Public ReadOnly Property JoinType() As Worm.Criteria.Joins.JoinType
            Get
                Return _joinType
            End Get
        End Property

        Public Function InjectJoinFilter(ByVal schema As ObjectMappingEngine, ByVal t As Type, ByVal propertyAlias As String, ByVal table As SourceFragment, ByVal column As String) As Core.TemplateBase
            For Each _fl As Core.IFilter In _condition.GetAllFilters()
                Dim f As JoinFilter = Nothing
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                Dim tm As Core.TemplateBase = Nothing
                If fl._e1 IsNot Nothing AndAlso fl._e1.First.GetRealType(schema) Is t AndAlso fl._e1.Second = propertyAlias Then
                    If fl._e2 IsNot Nothing Then
                        f = CreateJoin(table, column, fl._e2.First, fl._e2.Second, fl._oper)
                        tm = CreateOrmFilter(fl._e2.First, fl._e2.Second, fl._oper)
                    Else
                        f = CreateJoin(table, column, fl._t2.First, fl._t2.Second, fl._oper)
                        tm = CreateTableFilter(fl._t2.First, fl._t2.Second, fl._oper)
                    End If
                End If
                If f Is Nothing Then
                    If fl._e2 IsNot Nothing AndAlso fl._e2.First.GetRealType(schema) Is t AndAlso fl._e2.Second = propertyAlias Then
                        If fl._e1 IsNot Nothing Then
                            f = CreateJoin(table, column, fl._e1.First, fl._e1.Second, fl._oper)
                            tm = CreateOrmFilter(fl._e1.First, fl._e1.Second, fl._oper)
                        Else
                            f = CreateJoin(table, column, fl._t1.First, fl._t1.Second, fl._oper)
                            tm = CreateTableFilter(fl._t1.First, fl._t1.Second, fl._oper)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    ReplaceFilter(fl, f)
                    Return tm
                End If
            Next
            Return Nothing
        End Function

        Protected Function CreateTableFilter(ByVal table As SourceFragment, ByVal column As String, ByVal oper As FilterOperation) As Core.TemplateBase
            Return New TableFilterTemplate(table, column, oper)
        End Function

        Protected Function CreateOrmFilter(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal oper As FilterOperation) As Core.TemplateBase
            Return New OrmFilterTemplate(os, propertyAlias, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, os, propertyAlias, oper)
        End Function

        Protected Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal oper As FilterOperation) As JoinFilter
            Return New JoinFilter(table, column, table2, column2, oper)
        End Function

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            Dim j As New QueryJoin(_table, _joinType, _condition)
            j._src = _src
            j.M2MKey = M2MKey
            j.M2MJoinType = M2MJoinType
            j.M2MJoinEntityName = M2MJoinEntityName
            Return j
        End Function

        Public Function Clone() As QueryJoin
            Return CType(_Clone(), QueryJoin)
        End Function
    End Class

End Namespace