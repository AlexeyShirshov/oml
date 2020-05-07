Imports Worm.Cache
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Query
Imports System.Linq

Namespace Entities.Meta
    Friend Class SimpleObjectSchema
        Implements IEntitySchema

        Private _table As SourceFragment
        Private _cols As Collections.IndexedCollection(Of String, MapField2Column) = New OrmObjectIndex

        Friend Sub New(ByVal cols As Collections.IndexedCollection(Of String, MapField2Column))
            _cols = cols
            _table = _cols(0).Table
        End Sub

        Friend Sub New(ByVal sourceFragment As SourceFragment)
            _table = sourceFragment
        End Sub

        Public ReadOnly Property FieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.FieldColumnMap
            Get
                Return _cols
            End Get
        End Property

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _table
            End Get
        End Property
    End Class

    Public NotInheritable Class SimpleMultiTableObjectSchema
        Implements IEntitySchema
        Implements IMultiTableObjectSchema

        Private _cols As Collections.IndexedCollection(Of String, MapField2Column) = New OrmObjectIndex
        Private _tables() As SourceFragment
        Private _baseSchema As IEntitySchema
        'Private _ownTable As SourceFragment
        Private _ownPK As MapField2Column
        Private _basePK As MapField2Column
        Private _joins() As JoinAttribute
        Private _t As Type

        Friend Sub New(ByVal type As Type, ByVal ownTable As SourceFragment, ByVal ownProperties As List(Of EntityPropertyAttribute),
                       ByVal baseSchema As IEntitySchema, ByVal version As String, ByVal mpe As ObjectMappingEngine, ByVal idic As IDictionary,
                       ByVal names As IDictionary)

            _baseSchema = baseSchema
            _t = type
            '_ownTable = ownTable

            Dim tables As New List(Of SourceFragment)
            Dim qj As New List(Of QueryJoin)

            tables.Add(ownTable)
            _tables = tables.ToArray

            'init _cols collection
            ObjectMappingEngine.ApplyAttributes2Schema(Me, ownProperties, mpe, idic, names)

            If baseSchema IsNot Nothing Then

                For Each m As MapField2Column In _cols
                    If m.IsPK Then

                        If (m.Attributes Or Field2DbRelations.Identity) = Field2DbRelations.Identity Then
                            'descendant should not have identity pk
                            Return
                        End If

                        If m.SourceFields.Count <> baseSchema.GetPK.SourceFields.Count Then
                            'descendant should have the same number of pk fields
                            Return
                        End If

                        _ownPK = m
                        Exit For
                    End If
                Next

                If _ownPK Is Nothing Then
                    'descendant should have pk
                    Return
                End If

                _basePK = baseSchema.GetPK

                Dim mt As IMultiTableObjectSchema = TryCast(baseSchema, IMultiTableObjectSchema)
                If mt IsNot Nothing Then
                    'tables.InsertRange(0, mt.GetTables)
                    tables.AddRange(mt.GetTables)
                Else
                    'tables.Insert(0, baseSchema.Table)
                    tables.Add(baseSchema.Table)
                End If

                For Each m As MapField2Column In baseSchema.FieldColumnMap
                    If m.IsPK Then
                    Else
                        Dim f = _cols(m.PropertyAlias)
                        If f IsNot Nothing Then
                            f.Table = m.Table
                        End If
                    End If
                Next

                'If _basePKs.Length > 1 Then
                '    _joins = Array.FindAll(CType(type.GetCustomAttributes(GetType(JoinAttribute), False), JoinAttribute()),
                '                           Function(ja As JoinAttribute) String.IsNullOrEmpty(ja.SchemaVersion) OrElse ja.SchemaVersion = version)

                '    If _joins.Length = 0 Then
                '        Throw New ObjectMappingException(String.Format("Entity {0} has multiple tables with complex pk. You have to use JoinAttribute to define joins for version {1}", type, version))
                '    End If
                'End If
            End If

            _tables = tables.ToArray
        End Sub

        Public ReadOnly Property FieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.FieldColumnMap
            Get
                Return _cols
            End Get
        End Property

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _tables(0)
            End Get
        End Property

        Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Criteria.Joins.QueryJoin Implements IMultiTableObjectSchema.GetJoins
            If _basePK IsNot Nothing Then
                Dim tbl As SourceFragment = Nothing
                If left Is Table Then
                    tbl = right
                    'If _basePKs.Length = 1 Then
                    '    Return j.on(right, _baseSchema.FieldColumnMap(_basePKs(0).PropertyAlias).SourceFieldExpression).eq(left, FieldColumnMap(_ownPKs(0).PropertyAlias).SourceFieldExpression)
                    'Else
                    '    Dim joina As JoinAttribute = Array.Find(_joins, Function(ja As JoinAttribute) _
                    '        ja.TableSchema = left.Schema AndAlso ja.TableName = left.Name AndAlso
                    '        ja.JoinTableSchema = right.Schema AndAlso ja.JoinTableName = right.Name)
                    '    If joina Is Nothing Then
                    '        joina = Array.Find(_joins, Function(ja As JoinAttribute) _
                    '            ja.TableSchema = right.Schema AndAlso ja.TableName = right.Name AndAlso
                    '            ja.JoinTableSchema = left.Schema AndAlso ja.JoinTableName = left.Name)
                    '    End If

                    '    If joina Is Nothing Then
                    '        Throw New ObjectMappingException(String.Format("Cannot find JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                    '    End If

                    '    Dim pks As String() = joina.PrimaryKeys.Split(","c)
                    '    Dim fks As String() = joina.ForeignKeys.Split(","c)
                    '    Dim jl As JoinLink = Nothing
                    '    For i As Integer = 0 To pks.Length - 1
                    '        If jl Is Nothing Then
                    '            jl = j.on(right, _baseSchema.FieldColumnMap(fks(i)).SourceFieldExpression).eq(left, FieldColumnMap(pks(i)).SourceFieldExpression)
                    '        Else
                    '            jl = jl.and(right, _baseSchema.FieldColumnMap(fks(i)).SourceFieldExpression).eq(left, FieldColumnMap(pks(i)).SourceFieldExpression)
                    '        End If
                    '    Next

                    '    If jl Is Nothing Then
                    '        Throw New ObjectMappingException(String.Format("Cannot find primary keys in JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                    '    End If

                    '    Return jl
                    'End If
                ElseIf right Is Table Then
                    tbl = left
                Else
                    Dim mt As IMultiTableObjectSchema = TryCast(_baseSchema, IMultiTableObjectSchema)
                    If mt IsNot Nothing Then
                        Return mt.GetJoins(left, right)
                    End If
                End If

                Dim j As JoinCondition = JCtor.join(tbl)
                Dim jl As JoinLink = Nothing
                For i As Integer = 0 To _ownPK.SourceFields.Count - 1
                    If jl Is Nothing Then
                        jl = j.on(Table, _ownPK.SourceFields(i).SourceFieldExpression).eq(tbl, _basePK.SourceFields(i).SourceFieldExpression)
                    Else
                        jl = jl.and(Table, _ownPK.SourceFields(i).SourceFieldExpression).eq(tbl, _basePK.SourceFields(i).SourceFieldExpression)
                    End If
                Next

                Return jl
            End If
            Return Nothing
        End Function

        Public Function GetTables() As SourceFragment() Implements IMultiTableObjectSchema.GetTables
            Return _tables
        End Function
    End Class

    Friend NotInheritable Class SimpleTypedEntitySchema
        Inherits SimpleObjectSchema
        Implements ICacheBehavior ', ITypedSchema

        Private _t As Type

        Friend Sub New(ByVal t As Type, ByVal cols As Collections.IndexedCollection(Of String, MapField2Column))
            MyBase.New(cols)
            _t = t
        End Sub

        Public Function GetEntityKey() As String Implements ICacheBehavior.GetEntityKey
            Return _t.ToString
        End Function

        Public Function GetEntityTypeKey() As Object Implements ICacheBehavior.GetEntityTypeKey
            Return _t
        End Function

        'Public ReadOnly Property EntityType() As System.Type Implements ITypedSchema.EntityType
        '    Get
        '        Return _t
        '    End Get
        'End Property
    End Class
End Namespace