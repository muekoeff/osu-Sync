Public Class Window_MessageWindow
    Public Sub SetMessage(ByRef Message As String, Optional ByRef Title As String = "", Optional ByRef SubTitle As String = "osu!Sync")
        If Title = "" Then
            Title = _e("WindowMessage_message")
        End If
        TextBlock_Title.Text = Title
        TextBlock_SubTitle.Text = SubTitle

        Dim Paragraph = New Paragraph()
        Paragraph.Inlines.Add(New Run(Message))
        RichTextBox_Message.Document.Blocks.Clear()
        RichTextBox_Message.Document.Blocks.Add(Paragraph)
    End Sub

    Private Sub Button_Close_Click(sender As Object, e As RoutedEventArgs) Handles Button_Close.Click
        Me.Close()
    End Sub
End Class
