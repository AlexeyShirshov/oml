Imports Worm.Entities.Meta

Friend Module Converters
    Function [Default](ByVal propType As Type, dbType As Type, value As Object) As Object
        Return value
    End Function
    Function Standart(ByVal propType As Type, dbType As Type, value As Object) As Object
        Return Convert.ChangeType(value, propType)
    End Function
    Function Date2Bytes(ByVal propType As Type, dbType As Type, value As Object) As Object
        Dim dt = CDate(value)
        Dim l As Long = dt.ToBinary
        Using ms As New IO.MemoryStream
            Using sw As New IO.StreamWriter(ms)
                sw.Write(l)
                sw.Flush()
                Return ms.ToArray
            End Using
        End Using
    End Function
    Function String2Xml(ByVal propType As Type, dbType As Type, value As Object) As Object
        Dim o As New System.Xml.XmlDocument
        o.LoadXml(CStr(value))
        Return o
    End Function
    Function String2Enum(ByVal propType As Type, dbType As Type, value As Object) As Object
        Dim svalue As String = CStr(value).Trim
        If svalue = String.Empty Then
            Return 0
        Else
            Return [Enum].Parse(propType, svalue, True)
        End If
    End Function
    Function Int2Enum(ByVal propType As Type, dbType As Type, value As Object) As Object
        Return [Enum].ToObject(propType, value)
    End Function
    Function GetConverter(ByVal propType As Type, dbType As Type) As MapField2Column.ConverterDelegate
        If propType Is dbType Then
            Return AddressOf [Default]
        ElseIf GetType(System.Xml.XmlDocument) Is propType AndAlso dbtype Is GetType(String) Then
            Return AddressOf String2Xml
        ElseIf propType.IsEnum Then
            If dbType Is propType.GetEnumUnderlyingType Then
                Return AddressOf Int2Enum
            ElseIf dbtype Is GetType(String) Then
                Return AddressOf String2Enum
            ElseIf dbType Is GetType(Byte) OrElse dbType Is GetType(Short) OrElse dbType Is GetType(Integer) OrElse dbType Is GetType(UShort) Then
                Dim t = propType.GetEnumUnderlyingType
                Return Function(ByVal _x_ As Type, _y_ As Type, value As Object) As Object
                           Return [Enum].ToObject(_x_, Convert.ChangeType(value, t))
                       End Function
            End If
        ElseIf propType.IsGenericType AndAlso GetType(Nullable(Of )).Name = propType.Name Then
            'Return AddressOf (New Converter4Generic(GetConverter(propType.GetGenericArguments()(0), dbType))).Default
            Dim t = propType.GetGenericArguments()(0)
            Dim _del = GetConverter(t, dbType)
            Return Function(ByVal _x_ As Type, _y_ As Type, value As Object) As Object
                       Dim v = _del(t, _y_, value)
                       Return _x_.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, New Object() {v})
                   End Function
        ElseIf propType.IsPrimitive Then
            Return AddressOf Standart
        ElseIf propType Is GetType(Long) AndAlso dbtype Is GetType(Decimal) Then
            Return AddressOf Standart
        ElseIf propType Is GetType(Long) AndAlso dbtype Is GetType(Integer) Then
            Return AddressOf Standart
        End If

        Return AddressOf [Default]
    End Function

    'Class Converter4Generic
    '    Public _del As MapField2Column.ConverterDelegate
    '    Public Sub New(del As MapField2Column.ConverterDelegate)
    '        _del = del
    '    End Sub
    '    Function [Default](ByVal propType As Type, dbType As Type, value As Object) As Object
    '        Dim t = propType.GetGenericArguments()(0)
    '        Dim v = _del(t, dbType, value)
    '        Return propType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, New Object() {v})
    '    End Function
    'End Class
End Module
