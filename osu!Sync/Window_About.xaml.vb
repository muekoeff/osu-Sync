Class Window_About

    Sub TB_Contact_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles TB_Contact.MouseUp
        Process.Start("mailto:team@nw520.de?subject=Contact%20|%20osu!Sync")
    End Sub

    Sub TB_Feedback_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles TB_Feedback.MouseUp
        MainWindow.Interface_ShowSettingsWindow(4)
    End Sub

    Sub TB_GitHub_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles TB_GitHub.MouseUp
        Process.Start("https://github.com/naseweis520/osu-Sync")
    End Sub

    Sub TB_osuForum_MouseUp(sender As Object, e As MouseEventArgs) Handles TB_osuForum.MouseUp
        Process.Start("https://osu.ppy.sh/forum/t/270446")
    End Sub

    Sub TB_Version_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles TB_Version.MouseUp
        MainWindow.Interface_ShowUpdaterWindow()
    End Sub

    Sub Window_About_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
#If DEBUG Then
        TB_Version.Text = _e("WindowAbout_version").Replace("%0", My.Application.Info.Version.ToString & " (Dev)").Replace("%1", TranslationNameGet(AppSettings.Tool_Language))
#Else
        TB_Version.Text = _e("WindowAbout_version").Replace("%0", My.Application.Info.Version.ToString).Replace("%1", TranslationNameGet(AppSettings.Tool_Language))
#End If
    End Sub
End Class
