Imports System.Configuration
Imports Worm.Orm.Meta

Namespace Configuration
    Public MustInherit Class SearchSection
        Inherits ConfigurationSection

        'Protected Class StopsElement
        '    Inherits ConfigurationElement

        '    Protected _stops As String

        '    Protected Overrides Sub DeserializeElement(ByVal reader As System.Xml.XmlReader, ByVal serializeCollectionKey As Boolean)
        '        _stops = reader.ReadString()
        '    End Sub

        '    Public ReadOnly Property Stops() As String
        '        Get
        '            Return _stops
        '        End Get
        '    End Property
        'End Class

        Public Sub New()
        End Sub

        Public Function Replace(ByVal str As String) As String
            For Each ce As CharElement In charMappingSection
                For Each cr As Char In ce.Value.ToCharArray
                    Dim i As Integer = str.IndexOf(cr.ToString, StringComparison.InvariantCultureIgnoreCase)
                    Dim s As Integer = 0
                    Do While i >= 0
                        str = str.Remove(i, 1).Insert(i, ce.Map)
                        s += i + 1
                        If s >= str.Length Then Exit Do
                        i = str.IndexOf(cr.ToString, s, StringComparison.InvariantCultureIgnoreCase)
                    Loop
                    'str = str.Replace(cr, ce.Map.Chars(0))
                Next
            Next
            Return str
        End Function

        <ConfigurationProperty("charMapping")> _
        Public ReadOnly Property charMappingSection() As CharsCollection
            Get
                Return CType(Me("charMapping"), CharsCollection)
            End Get
        End Property

        <ConfigurationProperty("replace")> _
        Public ReadOnly Property ReplaceSection() As SearchReplaceCollection
            Get
                Return CType(Me("replace"), SearchReplaceCollection)
            End Get
        End Property

        <ConfigurationProperty("minTokenLength", Defaultvalue:=1, Isrequired:=False)> _
        Public Property minTokenLength() As Integer
            Get
                Return CInt(Me("minTokenLength"))
            End Get
            Set(ByVal value As Integer)
                Me("minTokenLength") = value
            End Set
        End Property

        Protected MustOverride Function GetStops(ByVal t As Type) As String()

        Public Shared Function GetSection(ByVal name As String) As SearchSection
            Return CType(System.Configuration.ConfigurationManager.GetSection(name), SearchSection)
        End Function

        Public Shared Function GetValueForContains(ByVal tokens() As String, ByVal sectionName As String, _
            ByVal f As IOrmFullTextSupport, ByVal contextkey As Object) As String
            Dim value As New StringBuilder

            'Dim l As Integer = value.Length
            'Dim ss() As String = Nothing

            Dim sc As Configuration.SearchSection = Configuration.SearchSection.GetSection(sectionName)

            'If sc IsNot Nothing Then
            '    ss = sc.GetStops(t)
            'End If

            If tokens.Length = 1 AndAlso tokens(0).IndexOf(" "c) < 0 Then
                Dim tok As String = tokens(0)

                If sc IsNot Nothing Then
                    If sc.minTokenLength > tok.Length Then GoTo l2
                End If

                value.Append("""").Append(tok)
                If f Is Nothing OrElse f.ApplayAsterisk Then
                    value.Append("*""")
                Else
                    value.Append("""")
                End If
l2:
            Else
                Dim f2 As IOrmFullTextSupportEx = TryCast(f, IOrmFullTextSupportEx)
                If f2 IsNot Nothing Then
                    f2.MakeSearchString(contextkey, tokens, value)
                Else
                    value.Append("""")
                    For Each s As String In tokens
                        value.Append(s).Append(" ")
                    Next
                    value.Append("""")
                End If
            End If

            If value.Length < 2 Then
                Return Nothing
            End If

            Return value.ToString
        End Function

        Public Shared Function GetValueForFreeText(ByVal t As Type, ByVal tokens() As String, ByVal sectionName As String) As String
            Dim value As New StringBuilder

            'Dim l As Integer = value.Length
            Dim ss() As String = Nothing

            Dim sc As Configuration.SearchSection = Configuration.SearchSection.GetSection(sectionName)

            If sc IsNot Nothing Then
                ss = sc.GetStops(t)
            End If

            If tokens.Length = 1 AndAlso tokens(0).IndexOf(" "c) < 0 Then
                Throw New InvalidOperationException("use GetValueForContains")
            Else
                For Each s As String In tokens

                    If sc IsNot Nothing Then
                        If sc.minTokenLength > s.Length Then GoTo l1
                    End If

                    If ss IsNot Nothing Then
                        For Each stop_token As String In ss
                            If s.Equals(stop_token, StringComparison.InvariantCultureIgnoreCase) Then
                                GoTo l1
                            End If
                        Next
                    End If

                    value.Append(s).Append(" ")
l1:
                Next
            End If

            If value.Length < 2 Then
                Return Nothing
            End If

            Return value.ToString
        End Function

    End Class

    Public Class CharsCollection
        Inherits ConfigurationElementCollection

        Protected Overloads Overrides Function CreateNewElement() As System.Configuration.ConfigurationElement
            Return New CharElement
        End Function

        Protected Overrides Function CreateNewElement(ByVal elementName As String) As System.Configuration.ConfigurationElement
            Return New CharElement(elementName)
        End Function

        Protected Overrides Function GetElementKey(ByVal element As System.Configuration.ConfigurationElement) As Object
            Return CType(element, CharElement).Value
        End Function

        Public Overrides ReadOnly Property CollectionType() As System.Configuration.ConfigurationElementCollectionType
            Get
                Return ConfigurationElementCollectionType.AddRemoveClearMap
            End Get
        End Property

        Default Public Shadows Property Item(ByVal index As Integer) As CharElement
            Get
                Return CType(BaseGet(index), CharElement)
            End Get
            Set(ByVal value As CharElement)
                If Not (BaseGet(index) Is Nothing) Then
                    BaseRemoveAt(index)
                End If
                BaseAdd(index, value)
            End Set
        End Property

        Default Public Shadows ReadOnly Property Item(ByVal Name As String) As CharElement
            Get
                Return CType(BaseGet(Name), CharElement)
            End Get
        End Property

    End Class

    Public Class SearchReplaceCollection
        Inherits ConfigurationElementCollection

        Protected Overloads Overrides Function CreateNewElement() As System.Configuration.ConfigurationElement
            Return New SearchReplaceElement
        End Function

        Protected Overrides Function CreateNewElement(ByVal elementName As String) As System.Configuration.ConfigurationElement
            Return New SearchReplaceElement(elementName)
        End Function

        Protected Overrides Function GetElementKey(ByVal element As System.Configuration.ConfigurationElement) As Object
            Return CType(element, SearchReplaceElement).From
        End Function

        Public Overrides ReadOnly Property CollectionType() As System.Configuration.ConfigurationElementCollectionType
            Get
                Return ConfigurationElementCollectionType.AddRemoveClearMap
            End Get
        End Property

        Default Public Shadows Property Item(ByVal index As Integer) As SearchReplaceElement
            Get
                Return CType(BaseGet(index), SearchReplaceElement)
            End Get
            Set(ByVal value As SearchReplaceElement)
                If Not (BaseGet(index) Is Nothing) Then
                    BaseRemoveAt(index)
                End If
                BaseAdd(index, value)
            End Set
        End Property

        Default Public Shadows ReadOnly Property Item(ByVal Name As String) As SearchReplaceElement
            Get
                Return CType(BaseGet(Name), SearchReplaceElement)
            End Get
        End Property

    End Class

    Public Class CharElement
        Inherits ConfigurationElement

        Public Sub New()
        End Sub

        Public Sub New(ByVal a1 As String)
            Value = a1
        End Sub

        Public Sub New(ByVal a1 As String, ByVal a2 As String)
            Value = a1
            Map = a2
        End Sub

        <ConfigurationProperty("value", IsRequired:=True)> _
        Public Property Value() As String
            Get
                Return CStr(Me("value"))
            End Get
            Set(ByVal value As String)
                Me("value") = value
            End Set
        End Property

        <ConfigurationProperty("map", defaultvalue:="", IsRequired:=False)> _
        Public Property Map() As String
            Get
                Return CStr(Me("map"))
            End Get
            Set(ByVal value As String)
                Me("map") = value
            End Set
        End Property

    End Class

    Public Class SearchReplaceElement
        Inherits ConfigurationElement

        Public Sub New()
        End Sub

        Public Sub New(ByVal a1 As String)
            [From] = a1
        End Sub

        Public Sub New(ByVal a1 As String, ByVal a2 As String)
            [From] = a1
            [To] = a2
        End Sub

        <ConfigurationProperty("from", IsRequired:=True)> _
        Public Property [From]() As String
            Get
                Return CStr(Me("from"))
            End Get
            Set(ByVal value As String)
                Me("from") = value
            End Set
        End Property

        <ConfigurationProperty("to", defaultvalue:="", IsRequired:=False)> _
        Public Property [To]() As String
            Get
                Return CStr(Me("to"))
            End Get
            Set(ByVal value As String)
                Me("to") = value
            End Set
        End Property

    End Class
End Namespace
