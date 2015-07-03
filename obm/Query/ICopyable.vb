Namespace Query
    Public Interface ICopyable
        Inherits ICloneable
        Function CopyTo(target As ICopyable) As Boolean
    End Interface
End Namespace