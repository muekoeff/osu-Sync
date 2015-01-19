Imports System.IO
Imports System.Net
Imports Newtonsoft.Json.Linq

Public Class Window_Updater
    Private WithEvents Client As New WebClient
    Private DownloadMode As String = "Info"
    Private Update_Path As String
    Private Update_Version As String
    Private Update_FileExtension As String
    Private Update_TotalBytes As String

    Private Sub Button_Done_Click(sender As Object, e As RoutedEventArgs) Handles Button_Done.Click
        Me.Close()
    End Sub

    Private Sub Button_Update_Click(sender As Object, e As RoutedEventArgs) Handles Button_Update.Click
        Me.Cursor = Cursors.AppStarting
        Button_Done.IsEnabled = False
        Button_Update.IsEnabled = False
        DownloadMode = "DownloadUpdate"
        If Not Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\Update") Then
            Directory.CreateDirectory(Path.GetTempPath() & "naseweis520\osu!Sync\Update")
        End If
        Client.DownloadFileAsync(New Uri(Update_Path), Path.GetTempPath() & "naseweis520\osu!Sync\Update\osu!Sync Version " & Update_Version & Update_FileExtension & ".tmp")
    End Sub

    Private Sub Client_DownloadFileCompleted(sender As Object, e As System.ComponentModel.AsyncCompletedEventArgs) Handles Client.DownloadFileCompleted
        If DownloadMode = "DownloadUpdate" Then
            Me.Cursor = Cursors.Arrow
            TextBlock_Status.Text = "Download finished. (" & Update_TotalBytes & " Bytes)"
            Button_Done.IsEnabled = True
            If Not Directory.Exists(Setting_Tool_UpdateSavePath) Then
                Directory.CreateDirectory(Setting_Tool_UpdateSavePath)
            End If
            If Not File.Exists(Setting_Tool_UpdateSavePath & "\osu!Sync Version " & Update_Version & Update_FileExtension) Then
                File.Move(Path.GetTempPath() & "naseweis520\osu!Sync\Update\osu!Sync Version " & Update_Version & Update_FileExtension & ".tmp", _
                          Setting_Tool_UpdateSavePath & "\osu!Sync Version " & Update_Version & Update_FileExtension)
                If MessageBox.Show("Do you want to open the path where the updated files have been saved?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                    Process.Start(Setting_Tool_UpdateSavePath)
                End If
            Else
                MsgBox("Unable to move update to correct directory, because there's already a file with the same name.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
                Process.Start(Path.GetTempPath() & "naseweis520\osu!Sync\Update")
            End If
        End If
    End Sub

    Private Sub Client_DownloadProgressChanged(sender As Object, e As System.Net.DownloadProgressChangedEventArgs) Handles Client.DownloadProgressChanged
        If DownloadMode = "DownloadUpdate" Then
            Update_TotalBytes = CStr(e.TotalBytesToReceive)
            ProgressBar_Progress.Value = e.ProgressPercentage
            TextBlock_Status.Text = e.BytesReceived & " of " & e.TotalBytesToReceive & " Bytes"
        End If
    End Sub

    Private Sub Client_DownloadStringCompleted(sender As Object, e As Net.DownloadStringCompletedEventArgs) Handles Client.DownloadStringCompleted
        If DownloadMode = "Info" Then
            Dim Answer As JObject
            Try
                Answer = JObject.Parse(e.Result)
            Catch ex As Newtonsoft.Json.JsonReaderException
                Clipboard.SetText("https://osu.ppy.sh/forum/t/270446")
                MsgBox("Unable to check for updates!" & vbNewLine & "// Invalid Server response" & vbNewLine & vbNewLine & "If this problem persists you can visit the osu! forum at http://bit.ly/1Bbmn6E (in your clipboard).", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                'Console.WriteLine(e.Result)
                TextBlock_Header_VersionInfo.Text += " | Can't fetch latest version"
                TextBlock_Status.Text = "Can't fetch latest version"
                ProgressBar_Progress.IsIndeterminate = False
                Exit Sub
            Catch ex As System.Reflection.TargetInvocationException
                Clipboard.SetText("https://osu.ppy.sh/forum/t/270446")
                MsgBox("Unable to check for updates!" & vbNewLine & "// Can't connect to server" & vbNewLine & vbNewLine & "Try to close and reopen the updater. If this problem persists you can visit the osu! forum at http://bit.ly/1Bbmn6E (in your clipboard).", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                TextBlock_Header_VersionInfo.Text += " | Can't fetch latest version"
                TextBlock_Status.Text = "Can't fetch latest version"
                ProgressBar_Progress.IsIndeterminate = False
                Exit Sub
            End Try

            TextBlock_Header_VersionInfo.Text += " | Latest version: " & CStr(Answer.SelectToken("latestVersion"))

            Dim Paragraph As New Paragraph()
            Dim FlowDocument As New FlowDocument()
            With Paragraph
                .Inlines.Add(New Run("Date of Publication: " & CStr(Answer.SelectToken("dateOfPublication"))))
                .Inlines.Add(New LineBreak())
                .Inlines.Add(New Run("Published by: " & CStr(Answer.SelectToken("admin"))))
            End With
            FlowDocument.Blocks.Add(Paragraph)
            RichTextBox_Changelog.Document = FlowDocument

            If CStr(Answer.SelectToken("latestVersion")) = My.Application.Info.Version.ToString Then
                TextBlock_Status.Text = "You're using the latest version of osu!Sync"
                Me.Cursor = Cursors.Arrow
            Else
                TextBlock_Status.Text = "A new version of osu!Sync is available"

                Update_Path = CStr(Answer.SelectToken("downloadPath"))
                Update_Version = CStr(Answer.SelectToken("latestVersion"))
                Update_FileExtension = CStr(Answer.SelectToken("fileExtension"))
                Update_FileExtension = CStr(Answer.SelectToken("fileExtension"))

                Button_Update.IsEnabled = True
            End If

            DownloadMode = "Changelog"
            If Not CStr(Answer.SelectToken("pathToChangelog")) = "undefined" Then
                Client.DownloadStringAsync(New Uri(CStr(Answer.SelectToken("pathToChangelog")) & "?version=" & My.Application.Info.Version.ToString & "&from=Updater"))
            Else
                Paragraph = New Paragraph()
                With Paragraph
                    .Inlines.Add(New Run("No changelog available"))
                End With
                RichTextBox_Changelog.Document.Blocks.Add(Paragraph)
                ProgressBar_Progress.IsIndeterminate = False
                Me.Cursor = Cursors.Arrow
            End If
        ElseIf DownloadMode = "Changelog" Then
            Dim Results() As String
            Try
                Results = Split(e.Result, "\n")
            Catch ex As System.Reflection.TargetInvocationException
                MsgBox("Unable to dowload changelog!" & vbNewLine & "// Can't connect to server", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                ProgressBar_Progress.IsIndeterminate = False
                Me.Cursor = Cursors.Arrow
                Exit Sub
            End Try

            Dim Paragraph = New Paragraph()
            For Each Line As String In Results
                With Paragraph
                    .Inlines.Add(New Run(Line))
                    .Inlines.Add(New LineBreak())
                End With
            Next
            RichTextBox_Changelog.Document.Blocks.Add(Paragraph)
            ProgressBar_Progress.IsIndeterminate = False
            Me.Cursor = Cursors.Arrow
        End If
    End Sub

    Private Sub Window_Updater_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TextBlock_Header_VersionInfo.Text = "Your version: " & My.Application.Info.Version.ToString
        Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=Updater&updaterInterval=" & Setting_Tool_CheckForUpdates))
    End Sub
End Class
