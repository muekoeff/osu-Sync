Class Window_MessageWindow

    Sub Bu_Close_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Close.Click
        Close()
    End Sub

    Public Sub SetMessage(Message As String, Optional Title As String = "", Optional SubTitle As String = "osu!Sync")
        If Title = "" Then Title = _e("WindowMessage_message")
        TB_Title.Text = Title
        TB_SubTitle.Text = SubTitle

        Dim Paragraph = New Paragraph()
        Paragraph.Inlines.Add(New Run(Message))
        With RTB_Message.Document.Blocks
            .Clear()
            .Add(Paragraph)
        End With
    End Sub
End Class
