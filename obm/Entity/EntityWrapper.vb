Namespace Entities
    Public Class EntityWrapper(Of T As {New, ISinglePKEntity, Class})
        Private _ent As T
        Private ReadOnly _propGet As Func(Of Object)
        Private ReadOnly _propSet As Action(Of Object)
        Private ReadOnly _dx As Func(Of IDataContext)

        Public Sub New(propGet As Func(Of Object), propSet As Action(Of Object), getDx As Func(Of IDataContext))
            _propGet = propGet
            _propSet = propSet
            _dx = getDx
        End Sub

        Public Function GetEntity() As T
            If _ent IsNot Nothing Then
                If Not Equals(_propGet(), _ent.Identifier) Then
                    Unsibscribe_ent()
                Else
                    Return _ent
                End If
            End If

            _ent = _dx().GetByID(Of T)(_propGet())
            Subscribe_ent()

            Return _ent
        End Function
        Public Sub SetEntity(value As T)

            If value Is Nothing Then
                _propSet(Nothing)
                If _ent IsNot Nothing Then
                    Unsibscribe_ent()
                    _ent = Nothing
                End If
                Return
            End If

            _propSet(value.Identifier)

            If _ent IsNot value Then
                Unsibscribe_ent()
                _ent = value
                Subscribe_ent()
            End If

        End Sub
        Private Sub Unsibscribe_ent()
            If _ent IsNot Nothing Then
                RemoveHandler _ent.Saved, AddressOf _ent_saved
                RemoveHandler _ent.ChangesRejected, AddressOf _ent_rejected
            End If
        End Sub
        Private Sub Subscribe_ent()
            If _ent IsNot Nothing Then
                AddHandler _ent.Saved, AddressOf _ent_saved
                AddHandler _ent.ChangesRejected, AddressOf _ent_rejected
            End If
        End Sub
        Private Sub _ent_saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs)
            _propSet(_ent.Identifier)
        End Sub
        Private Sub _ent_rejected(ByVal sender As ICachedEntity, ByVal args As EventArgs)
            _propSet(_ent.Identifier)
        End Sub
    End Class
End Namespace