Imports System.IO
Imports System.Net
Imports Newtonsoft.Json.Linq

Public Class Window_Updater
    Private WithEvents Client As New WebClient
    Private DownloadMode As DownloadModes = DownloadModes.Info
    Private Update_DownloadToPath As String
    Private Update_Path As String
    Private Update_Path_UpdatePatcher As String
    Private Update_md5Hash As String
    Private Update_Version As String
    Private Update_FileExtension As String
    Private Update_TotalBytes As String

    Private Enum DownloadModes
        Info = 0
        DownloadPatcher = 1
        DownloadUpdate = 2
        Changelog = 3
    End Enum

    Private Sub Action_DownloadUpdate()
        DownloadMode = DownloadModes.DownloadUpdate
        Client.DownloadFileAsync(New Uri(Update_Path), RemoveIllegalCharactersFromPath(Path.GetTempPath() & "naseweis520\osu!Sync\Update\osu!Sync Version " & Update_Version & Update_FileExtension & ".tmp"))
    End Sub

    Private Sub Button_Done_Click(sender As Object, e As RoutedEventArgs) Handles Button_Done.Click
        Me.Close()
    End Sub

    Private Sub Button_Update_Click(sender As Object, e As RoutedEventArgs) Handles Button_Update.Click
        Me.Cursor = Cursors.AppStarting
        Button_Done.IsEnabled = False
        Button_Update.IsEnabled = False

        Update_DownloadToPath = Setting_Tool_Update_SavePath & "\osu!Sync Version " & Update_Version & Update_FileExtension

        If Setting_Tool_Update_UseDownloadPatcher Then
            If Not Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\Update") Then
                Directory.CreateDirectory(Path.GetTempPath() & "naseweis520\osu!Sync\Update")
            End If
            If Not File.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe") Then
                If File.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe.tmp") Then
                    File.Delete(Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe.tmp")
                End If
                DownloadMode = DownloadModes.DownloadPatcher
                Client.DownloadFileAsync(New Uri(Update_Path_UpdatePatcher), RemoveIllegalCharactersFromPath(Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe.tmp"))
            Else
                Action_DownloadUpdate()
            End If
        Else
            Action_DownloadUpdate()
        End If
    End Sub

    Private Sub Client_DownloadFileCompleted(sender As Object, e As System.ComponentModel.AsyncCompletedEventArgs) Handles Client.DownloadFileCompleted
        Select Case DownloadMode
            Case DownloadModes.DownloadPatcher
                Action_DownloadUpdate()
                File.Move(Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe.tmp",
                              Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe")
            Case DownloadModes.DownloadUpdate
                Me.Cursor = Cursors.Arrow
                TextBlock_Status.Text = _e("WindowUpdater_downloadFinished").Replace("%0", Update_TotalBytes)
                Button_Done.IsEnabled = True
                If Not Directory.Exists(Setting_Tool_Update_SavePath) Then
                    Directory.CreateDirectory(Setting_Tool_Update_SavePath)
                End If
                If Not File.Exists(Setting_Tool_Update_SavePath & "\osu!Sync Version " & Update_Version & Update_FileExtension) Then
                    File.Move(Path.GetTempPath() & "naseweis520\osu!Sync\Update\osu!Sync Version " & Update_Version & Update_FileExtension & ".tmp",
                              Update_DownloadToPath)
                    If Setting_Tool_Update_UseDownloadPatcher Then
                        ' Run UpdatePatcher
                        Dim UpdatePatcher As New ProcessStartInfo()
                        UpdatePatcher.Arguments = "-destinationVersion=""" & Update_Version & """ -sourceVersion=""" & My.Application.Info.Version.ToString & """ -pathToApp=""" & Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) & """ -pathToUpdate=""" & Update_DownloadToPath & """ -updateHash=""" & Update_md5Hash & """ -deletePackageAfter=""" & Setting_Tool_Update_DeleteFileAfter.ToString & """"
                        UpdatePatcher.FileName = Path.GetTempPath() & "naseweis520\osu!Sync\Update\UpdatePatcher.exe"
                        Process.Start(UpdatePatcher)
                        Application.Current.Shutdown()
                        Exit Sub
                    Else
                        If MessageBox.Show(_e("WindowUpdater_doYouWantToOpenPathWhereUpdatedFilesHaveBeenSaved"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                            Process.Start(Setting_Tool_Update_SavePath)
                        End If
                    End If
                Else
                    MsgBox(_e("WindowUpdater_unableToMoveUpdate"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
                    Process.Start(Path.GetTempPath() & "naseweis520\osu!Sync\Update")
                End If
        End Select
    End Sub

    Private Sub Client_DownloadProgressChanged(sender As Object, e As System.Net.DownloadProgressChangedEventArgs) Handles Client.DownloadProgressChanged
        Select Case DownloadMode
            Case DownloadModes.DownloadPatcher
                Update_TotalBytes = CStr(e.TotalBytesToReceive)
                ProgressBar_Progress.Value = e.ProgressPercentage
                TextBlock_Status.Text = _e("WindowUpdater_downloadingInstaller").Replace("%0", e.BytesReceived.ToString).Replace("%1", e.TotalBytesToReceive.ToString)
            Case DownloadModes.DownloadUpdate
                Update_TotalBytes = CStr(e.TotalBytesToReceive)
                ProgressBar_Progress.Value = e.ProgressPercentage
                TextBlock_Status.Text = _e("WindowUpdater_downloadingUpdatePackage").Replace("%0", e.BytesReceived.ToString).Replace("%1", e.TotalBytesToReceive.ToString)
        End Select
    End Sub

    Private Sub Client_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs) Handles Client.DownloadStringCompleted
        Select Case DownloadMode
            Case DownloadModes.Info
                Dim Answer As JObject
                Try
                    Answer = JObject.Parse(e.Result)
                Catch ex As Newtonsoft.Json.JsonReaderException
                    MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_invalidServerResponse") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                    TextBlock_Header_VersionInfo.Text += " | " & _e("WindowUpdater_unableToCommunicateWithServer")
                    TextBlock_Status.Text = _e("WindowUpdater_unableToCommunicateWithServer")
                    ProgressBar_Progress.IsIndeterminate = False
                    Exit Sub
                Catch ex As System.Reflection.TargetInvocationException
                    Clipboard.SetText("https://osu.ppy.sh/forum/t/270446")
                    MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsVisitTheOsuForum"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                    TextBlock_Header_VersionInfo.Text += " | " & _e("WindowUpdater_unableToCommunicateWithServer")
                    TextBlock_Status.Text = _e("WindowUpdater_unableToCommunicateWithServer")
                    ProgressBar_Progress.IsIndeterminate = False
                    Exit Sub
                End Try

                TextBlock_Header_VersionInfo.Text += " | " & _e("WindowUpdater_latestVersion").Replace("%0", CStr(Answer.SelectToken("latestVersion")))

                Dim Paragraph As New Paragraph()
                Dim FlowDocument As New FlowDocument()
                With Paragraph
                    .Inlines.Add(New Run(_e("WindowUpdater_dateOfPublication").Replace("%0", CStr(Answer.SelectToken("dateOfPublication")))))
                    .Inlines.Add(New LineBreak())
                    .Inlines.Add(New Run(_e("WindowUpdater_publishedBy").Replace("%0", CStr(Answer.SelectToken("admin")))))
                End With
                FlowDocument.Blocks.Add(Paragraph)
                RichTextBox_Changelog.Document = FlowDocument

                If CStr(Answer.SelectToken("latestVersion")) = My.Application.Info.Version.ToString Then
                    TextBlock_Status.Text = _e("WindowUpdater_yourUsingTheLatestVersion")
                    Me.Cursor = Cursors.Arrow
                Else
                    TextBlock_Status.Text = _e("WindowUpdater_anUpdateIsAvailable")

                    Update_Path = CStr(Answer.SelectToken("downloadPath"))
                    Update_Path_UpdatePatcher = CStr(Answer.SelectToken("downloadPath_updatePatcher"))
                    Update_md5Hash = CStr(Answer.SelectToken("md5Hash"))
                    Update_Version = CStr(Answer.SelectToken("latestVersion"))
                    Update_FileExtension = CStr(Answer.SelectToken("fileExtension"))
                    Update_FileExtension = CStr(Answer.SelectToken("fileExtension"))

                    Button_Update.IsEnabled = True
                End If

                DownloadMode = DownloadModes.Changelog
                If Not CStr(Answer.SelectToken("pathToChangelog")) = "undefined" Then
                    Client.DownloadStringAsync(New Uri(CStr(Answer.SelectToken("pathToChangelog")) & "?version=" & My.Application.Info.Version.ToString & "&from=Updater"))
                Else
                    Paragraph = New Paragraph()
                    With Paragraph
                        .Inlines.Add(New Run(_e("WindowUpdater_noChangelogAvailable")))
                    End With
                    RichTextBox_Changelog.Document.Blocks.Add(Paragraph)
                    ProgressBar_Progress.IsIndeterminate = False
                    Me.Cursor = Cursors.Arrow
                End If
            Case DownloadModes.Changelog
                Dim Results() As String
                Try
                    Results = Split(e.Result, "\n")
                Catch ex As System.Reflection.TargetInvocationException
                    MsgBox(_e("WindowUpdater_unableToDownloadChangelog") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
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
        End Select
    End Sub

    Private Sub Window_Updater_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Setting_Tool_Update_UseDownloadPatcher = False Then
            Button_Update.Content = _e("WindowUpdater_download")
        End If
        TextBlock_Header_VersionInfo.Text = _e("WindowUpdater_yourVersion").Replace("%0", My.Application.Info.Version.ToString)
        Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=Updater&updaterInterval=" & Setting_Tool_CheckForUpdates))
    End Sub
End Class
