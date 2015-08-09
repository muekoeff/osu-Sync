Public Class Window_About

    Private Sub Contact_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Contact.MouseUp
        Process.Start("mailto:me@naseweis520.ml?subject=Contact%20|%20osu!Sync")
    End Sub

    Private Sub Feedback_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Feedback.MouseUp
        MainWindow.Interface_ShowSettingsWindow(3)
    End Sub

    Private Sub GitHub_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles GitHub.MouseUp
        Process.Start("https://github.com/naseweis520/osu-Sync")
    End Sub

    Private Sub osuForum_MouseUp(sender As Object, e As MouseEventArgs) Handles osuForum.MouseUp
        Process.Start("https://osu.ppy.sh/forum/t/270446")
    End Sub

    Private Sub TextBlock_Version_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Version.MouseUp
        MainWindow.Interface_ShowUpdaterWindow()
    End Sub

    Private Sub Window_About_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TextBlock_Version.Text = _e("WindowAbout_version").Replace("%0", My.Application.Info.Version.ToString).Replace("%1", GetTranslationName(Setting_Tool_Language))
    End Sub
End Class
