Imports System.Collections.Generic
Imports Worm.Query
Imports Worm.Entities.Meta
Imports System.Linq

Namespace Expressions2
    <Serializable()> _
    Public Class SelectExpression
        Implements Expressions2.IExpression

        Private _exp As Expressions2.IExpression
        Private _falias As String

        Private _attr As Field2DbRelations
        Private _dstProp As String
        Private _dst As EntityUnion
        Private _correctIdx As Boolean

#Region " Cache "
        'Friend _c As EntityPropertyAttribute
        'Friend _pi As Reflection.PropertyInfo
        Friend _realAtt As Field2DbRelations
        Friend _m As MapField2Column
        Friend _tempMark As String
#End Region

        Protected Sub New()
        End Sub

#Region " Public ctors "

#Region " Type ctors "
        Public Sub New(ByVal op As ObjectProperty, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.EntityExpression(op)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal intoPropertyAlias As String, ByVal into As Type)
            _exp = New Expressions2.EntityExpression(op)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(into)
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.EntityExpression(op)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub

        Public Sub New(ByVal op As ObjectProperty)
            _exp = New Expressions2.EntityExpression(op)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, eu)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, eu)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, t)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, entityName)
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, [alias])
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, t)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, entityName)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, [alias])
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            _exp = New Expressions2.EntityExpression(propertyAlias, t)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(into)
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, t)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            _exp = New Expressions2.EntityExpression(propertyAlias, entityName)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(into)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, entityName)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal into As Type)
            _exp = New Expressions2.EntityExpression(propertyAlias, [alias])
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(into)
        End Sub

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.EntityExpression(propertyAlias, [alias])
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub
#End Region

        Public Sub New(ByVal exp As Expressions2.IExpression)
            _exp = exp
        End Sub

        Public Sub New(ByVal exp As Expressions2.IExpression, ByVal intoPropertyAlias As String)
            _exp = exp
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal exp As Expressions2.IExpression, ByVal intoPropertyAlias As String, ByVal intoType As Type)
            _exp = exp
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoType)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            _exp = New Expressions2.TableExpression(t, column)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.TableExpression(t, column)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal intoPropertyAlias As String, ByVal intoType As Type)
            _exp = New Expressions2.TableExpression(t, column)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoType)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, _
                       ByVal intoPropertyAlias As String, ByVal intoType As EntityUnion)
            _exp = New Expressions2.TableExpression(t, column)
            _dstProp = intoPropertyAlias
            _dst = intoType
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.TableExpression(t, column)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.CustomExpression(format, values)
            _dstProp = intoPropertyAlias
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression, ByVal intoPropertyAlias As String, ByVal intoType As Type)
            _exp = New Expressions2.CustomExpression(format, values)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoType)
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression, ByVal intoPropertyAlias As String, ByVal intoEntityName As String)
            _exp = New Expressions2.CustomExpression(format, values)
            _dstProp = intoPropertyAlias
            _dst = New EntityUnion(intoEntityName)
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression, _
            ByVal prop As ObjectProperty)
            _exp = New Expressions2.CustomExpression(format, values)
            _dstProp = prop.PropertyAlias
            _dst = prop.Entity
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression, _
            ByVal prop As ObjectProperty, ByVal attr As Field2DbRelations)
            _exp = New Expressions2.CustomExpression(format, values)
            _dstProp = prop.PropertyAlias
            _dst = prop.Entity
            _attr = attr
        End Sub

        Public Sub New(ByVal format As String, ByVal values() As Expressions2.IExpression)
            MyClass.New(format, values, If(values Is Nothing, format, Nothing))
        End Sub

        Public Sub New(ByVal q As QueryCmd)
            _exp = New Expressions2.QueryExpression(q)
        End Sub

        Public Sub New(ByVal q As QueryCmd, ByVal intoPropertyAlias As String)
            _exp = New Expressions2.QueryExpression(q)
            _dstProp = intoPropertyAlias
        End Sub

#End Region

        'Public Sub CopyTo(ByVal s As SelectExpression)
        '    With s
        '        ._exp = _exp
        '        ._attr = _attr
        '        ._dst = _dst
        '        ._falias = _falias
        '        '._c = _c
        '        '._pi = _pi
        '        ._realAtt = _realAtt
        '        ._dstProp = _dstProp
        '        ._m = _m
        '    End With
        'End Sub

        'Protected Sub RaiseOnChange()
        '    RaiseEvent OnChange()
        'End Sub

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal selectList As IEnumerable(Of T), ByVal mpe As ObjectMappingEngine, ByVal executor As Query.IExecutionContext) As Collections.IndexedCollection(Of String, MapField2Column)
            If selectList Is Nothing Then
                Throw New ArgumentNullException(NameOf(selectList))
            End If

            Dim c As New OrmObjectIndex
            Return GetMapping(c, selectList, mpe, executor)
        End Function

        Public Shared Function GetMapping(Of T As SelectExpression)(ByVal c As OrmObjectIndex, ByVal selectList As IEnumerable(Of T), ByVal mpe As ObjectMappingEngine, ByVal executor As Query.IExecutionContext) As Collections.IndexedCollection(Of String, MapField2Column)
            For Each s As T In If(selectList, {})
                Dim pa As String = s.GetIntoPropertyAlias
                'If String.IsNullOrEmpty(pa) Then
                '    Throw New OrmManagerException("Alias for property in custom type is not specified")
                'End If
                Dim te As TableExpression = TryCast(s.Operand, TableExpression)
                If te IsNot Nothing Then
                    Dim m As MapField2Column = New MapField2Column(If(pa, te.SourceField), te.SourceFragment, s.Attributes, New SourceField(te.SourceField) With {.SourceFieldAlias = s.ColumnAlias})
                    c.Add(m)
                Else
                    Dim ee As EntityExpression = TryCast(s.Operand, EntityExpression)
                    If ee IsNot Nothing Then
                        Dim m As MapField2Column = New MapField2Column(If(pa, ee.ObjectProperty.PropertyAlias), Nothing, s.Attributes) With {.SourceFields = ee.GetMap(mpe, executor).SourceFields}
                        c.Add(m)
                        'm.SourceFields(0).SourceFieldAlias = s.ColumnAlias
                    Else
                        Dim pe As PropertyAliasExpression = TryCast(s.Operand, PropertyAliasExpression)
                        Dim m As MapField2Column = New MapField2Column(If(pa, pe.PropertyAlias), Nothing, s.Attributes)
                        c.Add(m)

                        If executor IsNot Nothing Then
                            m.SourceFields = executor.FindColumn(mpe, m.PropertyAlias).Select(Function(it) New SourceField(it))
                        End If
                        'm.SourceFields(0).SourceFieldAlias = s.ColumnAlias
                    End If
                End If
            Next
            Return c
        End Function

        Public Property CorrectFieldIndex() As Boolean
            Get
                Return _correctIdx
            End Get
            Set(ByVal value As Boolean)
                _correctIdx = value
            End Set
        End Property

        Public Property Attributes() As Field2DbRelations
            Get
                Return _attr
            End Get
            Set(ByVal value As Field2DbRelations)
                _attr = value
            End Set
        End Property

        Public Property Into() As EntityUnion
            Get
                Return _dst
            End Get
            Set(ByVal value As EntityUnion)
                _dst = value
            End Set
        End Property

        Public Property IntoPropertyAlias() As String
            Get
                Return _dstProp
            End Get
            Set(ByVal value As String)
                _dstProp = value
            End Set
        End Property

        Public Property ColumnAlias() As String
            Get
                Return _falias
            End Get
            Protected Friend Set(ByVal value As String)
                _falias = value
            End Set
        End Property

        Public ReadOnly Property Operand() As IExpression
            Get
                Return _exp
            End Get
        End Property

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Overridable Function GetDynamicString() As String Implements Expressions2.IQueryElement.GetDynamicString
            Return _exp.GetDynamicString
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return GetDynamicString.GetHashCode
        End Function

        'Public Overridable Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
        '    If _q IsNot Nothing Then
        '        Return CType(_q, Cache.IQueryDependentTypes).Get(mpe)
        '    End If
        '    Return New Cache.EmptyDependentTypes
        'End Function

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Expressions2.IExpression.GetStaticString
            Return _exp.GetStaticString(mpe)
        End Function

        Public Sub Prepare(ByVal executor As IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Expressions2.IExpression.Prepare
            _exp.Prepare(executor, mpe, contextInfo, stmt, isAnonym)
        End Sub

        Friend _intoPA As String
        Public Function GetIntoPropertyAlias(Optional withoutColumn As Boolean = False) As String
            If Not String.IsNullOrEmpty(_intoPA) Then
                Return _intoPA
            End If

            If Not String.IsNullOrEmpty(IntoPropertyAlias) Then
                Return IntoPropertyAlias
            End If

            If Not String.IsNullOrEmpty(ColumnAlias) AndAlso Not withoutColumn Then
                Return ColumnAlias
            End If

            Dim propertyAlias As String = Nothing
            Dim ee As Expressions2.IEntityPropertyExpression = TryCast(_exp, Expressions2.IEntityPropertyExpression)
            If ee IsNot Nothing Then
                propertyAlias = ee.ObjectProperty.PropertyAlias
            Else
                Dim pe As PropertyAliasExpression = TryCast(_exp, PropertyAliasExpression)
                If pe IsNot Nothing Then
                    propertyAlias = pe.PropertyAlias
                Else
                    For Each e As IExpression In _exp.GetExpressions
                        ee = TryCast(e, Expressions2.IEntityPropertyExpression)
                        If ee IsNot Nothing Then
                            propertyAlias = ee.ObjectProperty.PropertyAlias
                            Exit For
                        Else
                            pe = TryCast(e, PropertyAliasExpression)
                            If pe IsNot Nothing Then
                                propertyAlias = pe.PropertyAlias
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If
            _intoPA = propertyAlias
            Return propertyAlias
        End Function

        Private _intoEU As EntityUnion
        Public Function GetIntoEntityUnion() As EntityUnion
            Dim eu As EntityUnion = _intoEU
            If eu Is Nothing Then
                eu = Into
                If eu Is Nothing Then
                    Dim ee As Expressions2.IEntityPropertyExpression = TryCast(_exp, Expressions2.IEntityPropertyExpression)
                    If ee IsNot Nothing Then
                        eu = ee.ObjectProperty.Entity
                    Else
                        For Each e As IExpression In _exp.GetExpressions
                            ee = TryCast(e, Expressions2.IEntityPropertyExpression)
                            If ee IsNot Nothing Then
                                eu = ee.ObjectProperty.Entity
                                Exit For
                            End If
                        Next
                    End If
                End If
                _intoEU = eu
            End If
            Return eu
        End Function

        Public Function GetExpressions() As Expressions2.IExpression() Implements Expressions2.IExpression.GetExpressions
            Dim l As New List(Of Expressions2.IExpression) From {
                Me
            }
            l.AddRange(_exp.GetExpressions)
            Return l.ToArray
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, _
            ByVal executor As IExecutionContext) As String Implements Expressions2.IExpression.MakeStatement

            If Not String.IsNullOrEmpty(_falias) AndAlso ((stmtMode And MakeStatementMode.AddColumnAlias) = MakeStatementMode.AddColumnAlias) Then
                Return _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode And Not MakeStatementMode.AddColumnAlias, executor) & " as " & _falias
            Else
                Return _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor)
            End If
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements Expressions2.IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As Expressions2.IExpression Implements Expressions2.IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As Expressions2.IQueryElement) As Boolean Implements Expressions2.IQueryElement.Equals
            Return Equals(TryCast(f, SelectExpression))
        End Function

        Public Overloads Function Equals(ByVal f As SelectExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return Object.Equals(_dst, f._dst) AndAlso _dstProp = f._dstProp AndAlso _falias = f._falias AndAlso _exp.Equals(f._exp)
        End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As SelectExpression
            Dim n As New SelectExpression(_exp)
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, SelectExpression))
        End Function

        Public Function CopyTo(target As SelectExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._falias = _falias
            target._attr = _attr
            target._dstProp = _dstProp
            target._correctIdx = _correctIdx

            If _dst IsNot Nothing Then
                target._dst = _dst.Clone
            End If

            If _exp IsNot Nothing Then
                target._exp = CType(_exp.Clone, IExpression)
            End If

            Return True
        End Function
    End Class

    Public Class PropertyAliasExpression
        Implements IExpression

        Private _pa As String

        Public Sub New(ByVal propertyAlias As String)
            _pa = propertyAlias
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, _
            ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement

            Dim al As String = String.Empty

            If fromClause IsNot Nothing Then
                Dim tbl As SourceFragment = fromClause.Table
                Dim os As EntityUnion = Nothing
                If tbl Is Nothing Then
                    Dim oal As QueryAlias = fromClause.QueryEU.ObjectAlias
                    If oal Is Nothing Then
                        Throw New InvalidOperationException(String.Format("PropertyAliasExpression with {0} property must be used for referencing inner query fields. Now it references to {1}", _pa, fromClause.QueryEU._ToString))
                    End If
                    tbl = oal.Tbl
                    os = fromClause.QueryEU
                End If

                'If (stmtMode And MakeStatementMode.WithoutTables) = 0 Then
                '    al = tbl.UniqueName(os)
                'Else
                al = almgr.GetAlias(tbl, os)
                'End If
            End If

            'If (stmtMode Or MakeStatementMode.Select) = MakeStatementMode.Select Then
            '    Return al & mpe.Delimiter & executor.FindColumn(mpe, _pa)
            'Else
            Dim sb As New StringBuilder
            For Each s As String In executor.FindColumn(mpe, _pa)
                sb.Append(al).Append(stmt.Selector).Append(s)
                sb.Append(",")
            Next
            sb.Length -= 1
            Return sb.ToString
            'End If
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, PropertyAliasExpression))
        End Function

        Public Overloads Function Equals(ByVal f As PropertyAliasExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _pa = f._pa
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _pa
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return _pa
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Property PropertyAlias() As String
            Get
                Return _pa
            End Get
            Set(ByVal value As String)
                _pa = value
            End Set
        End Property

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function
        Public Function Clone() As PropertyAliasExpression
            Return New PropertyAliasExpression(_pa)
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, PropertyAliasExpression))
        End Function

        Public Function CopyTo(target As PropertyAliasExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._pa = _pa

            Return True
        End Function
    End Class
End Namespace
