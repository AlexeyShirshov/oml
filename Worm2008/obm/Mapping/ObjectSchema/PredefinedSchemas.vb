Imports Worm.Cache
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Query

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

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
            Return _cols
        End Function

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
        Private _bpkTable As SourceFragment
        Private _ownPKs() As MapField2Column
        Private _basePKs() As MapField2Column
        Private _joins() As JoinAttribute
        Private _t As Type

        Friend Sub New(ByVal type As Type, ByVal ownTable As SourceFragment, ByVal ownProperties As List(Of EntityPropertyAttribute), _
                       ByVal baseSchema As IEntitySchema, ByVal version As String)

            _t = type

            Dim tables As New List(Of SourceFragment)
            Dim qj As New List(Of QueryJoin)

            tables.Add(ownTable)
            ObjectMappingEngine.ApplyAttributes2Schema(Me, ownProperties)

            If baseSchema IsNot Nothing Then
                Dim l As New List(Of MapField2Column)
                For Each m As MapField2Column In _cols
                    If m.IsPK Then
                        l.Add(m)
                    End If
                Next
                _ownPKs = l.ToArray

                _baseSchema = baseSchema

                Dim mt As IMultiTableObjectSchema = TryCast(baseSchema, IMultiTableObjectSchema)
                If mt IsNot Nothing Then
                    tables.AddRange(mt.GetTables)
                Else
                    tables.Add(baseSchema.Table)
                End If

                l.Clear()
                For Each m As MapField2Column In baseSchema.GetFieldColumnMap
                    If m.IsPK Then
                        l.Add(m)
                        _bpkTable = m.Table
                    Else
                        _cols(m.PropertyAlias).Table = m.Table
                    End If
                Next
                _basePKs = l.ToArray

                If _basePKs.Length > 1 Then
                    _joins = Array.FindAll(CType(type.GetCustomAttributes(GetType(JoinAttribute), False), JoinAttribute()), Function(ja As JoinAttribute) _
                        String.IsNullOrEmpty(ja.SchemaVersion) OrElse ja.SchemaVersion = version)

                    If _joins.Length = 0 Then
                        Throw New ObjectMappingException(String.Format("Entity {0} has multiple tables with complex pk. You have to use JoinAttribute to define joins for version {1}", type, version))
                    End If
                End If
            End If

            _tables = tables.ToArray
        End Sub

        Public Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
            Return _cols
        End Function

        Public ReadOnly Property Table() As SourceFragment Implements IEntitySchema.Table
            Get
                Return _tables(0)
            End Get
        End Property

        Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Criteria.Joins.QueryJoin Implements IMultiTableObjectSchema.GetJoins
            If _bpkTable IsNot Nothing Then
                If left Is Table AndAlso right Is _bpkTable Then
                    Dim j As JoinCondition = JCtor.join(right)
                    If _basePKs.Length = 1 Then
                        Return j.on(right, _basePKs(0).PropertyAlias).eq(left, _ownPKs(0).PropertyAlias)
                    Else
                        Dim joina As JoinAttribute = Array.Find(_joins, Function(ja As JoinAttribute) _
                            ja.TableSchema = left.Schema AndAlso ja.TableName = left.Name AndAlso _
                            ja.JoinTableSchema = right.Schema AndAlso ja.JoinTableName = right.Name)
                        If joina Is Nothing Then
                            joina = Array.Find(_joins, Function(ja As JoinAttribute) _
                                ja.TableSchema = right.Schema AndAlso ja.TableName = right.Name AndAlso _
                                ja.JoinTableSchema = left.Schema AndAlso ja.JoinTableName = left.Name)
                        End If

                        If joina Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Cannot find JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                        End If

                        Dim pks As String() = joina.PrimaryKeys.Split(","c)
                        Dim fks As String() = joina.ForeignKeys.Split(","c)
                        Dim jl As JoinLink = Nothing
                        For i As Integer = 0 To pks.Length - 1
                            If jl Is Nothing Then
                                jl = j.on(right, fks(i)).eq(left, pks(i))
                            Else
                                jl = jl.and(right, fks(i)).eq(left, pks(i))
                            End If
                        Next

                        If jl Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Cannot find primary keys in JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                        End If

                        Return jl
                    End If
                ElseIf right Is Table AndAlso left Is _bpkTable Then
                    Dim j As JoinCondition = JCtor.join(right)
                    If _basePKs.Length = 1 Then
                        Return j.on(right, _ownPKs(0).PropertyAlias).eq(left, _basePKs(0).PropertyAlias)
                    Else
                        Dim joina As JoinAttribute = Array.Find(_joins, Function(ja As JoinAttribute) _
                            ja.TableSchema = left.Schema AndAlso ja.TableName = left.Name AndAlso _
                            ja.JoinTableSchema = right.Schema AndAlso ja.JoinTableName = right.Name)
                        If joina Is Nothing Then
                            joina = Array.Find(_joins, Function(ja As JoinAttribute) _
                                ja.TableSchema = right.Schema AndAlso ja.TableName = right.Name AndAlso _
                                ja.JoinTableSchema = left.Schema AndAlso ja.JoinTableName = left.Name)
                        End If

                        If joina Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Cannot find JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                        End If

                        Dim pks As String() = joina.PrimaryKeys.Split(","c)
                        Dim fks As String() = joina.ForeignKeys.Split(","c)
                        Dim jl As JoinLink = Nothing
                        For i As Integer = 0 To pks.Length - 1
                            If jl Is Nothing Then
                                jl = j.on(left, fks(i)).eq(right, pks(i))
                            Else
                                jl = jl.and(left, fks(i)).eq(right, pks(i))
                            End If
                        Next

                        If jl Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Cannot find primary keys in JoinAttribute for table {0}.{1} and {2}.{3} in entity {4}", left.Schema, left.Name, right.Schema, right.Name, _t))
                        End If

                        Return jl
                    End If
                End If
                Dim mt As IMultiTableObjectSchema = TryCast(_baseSchema, IMultiTableObjectSchema)
                If mt IsNot Nothing Then
                    Return mt.GetJoins(left, right)
                End If
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

        Public Function GetEntityKey(ByVal filterInfo As Object) As String Implements ICacheBehavior.GetEntityKey
            Return _t.ToString
        End Function

        Public Function GetEntityTypeKey(ByVal filterInfo As Object) As Object Implements ICacheBehavior.GetEntityTypeKey
            Return _t
        End Function

        'Public ReadOnly Property EntityType() As System.Type Implements ITypedSchema.EntityType
        '    Get
        '        Return _t
        '    End Get
        'End Property
    End Class
End Namespace