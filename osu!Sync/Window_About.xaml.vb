Public Class Window_About

    Private Sub Contact_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Contact.MouseUp
        Process.Start("mailto:me@naseweis520.ml?subject=Kontakt%20|%20osu!Sync")
    End Sub

    Private Sub Project_naseweis520_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Project_naseweis520.MouseUp
        Process.Start("http://naseweis520.ml/osu!Sync/project_info")
    End Sub

    Private Sub GitHub_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles GitHub.MouseUp
        Process.Start("https://github.com/naseweis520/osu-Sync")
    End Sub

    Private Sub osuForum_MouseUp(sender As Object, e As MouseEventArgs) Handles osuForum.MouseUp
        Process.Start("https://osu.ppy.sh/forum/t/270446")
    End Sub
End Class
