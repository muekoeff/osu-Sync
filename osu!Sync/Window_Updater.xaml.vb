Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Class Window_Updater
    Private WithEvents Client As New WebClient
    Private DownloadMode As DownloadModes = DownloadModes.Info
    Private Update_DownloadPatcherToPath As String = AppTempPath & "\Updater\UpdatePatcher.exe"
    Private Update_DownloadToPath As String
    Private Update_FileName As String
    Private Update_Path As String
    Private Update_Path_UpdatePatcher As String
    Private Update_Version As String
    Private Update_TotalBytes As String

    Enum DownloadModes
        Info = 0
        DownloadPatcher = 1
        DownloadUpdate = 2
    End Enum

    Sub Action_DownloadUpdate()
        DownloadMode = DownloadModes.DownloadUpdate
        Client.DownloadFileAsync(New Uri(Update_Path), Update_DownloadToPath & ".tmp")
    End Sub

    Sub Action_LoadUpdateInformation(ByRef Answer As JObject)
        Update_Path_UpdatePatcher = CStr(Answer.SelectToken("patcher").SelectToken("path"))

        For Each a In Answer.SelectToken("latestRepoRelease").SelectToken("assets")
            If CStr(a.SelectToken("name")).StartsWith("osu.Sync.") And CStr(a.SelectToken("name")).EndsWith(".zip") Then
                Update_FileName = CStr(a.SelectToken("name"))
                Update_Path = CStr(a.SelectToken("browser_download_url"))
            End If
        Next

        If Update_Path IsNot Nothing Then
            Bu_Update.IsEnabled = True
        Else
            MsgBox(_e("MainWindow_unableToGetUpdatePath"), MsgBoxStyle.Critical, AppName)
        End If
    End Sub

    Sub Bu_Done_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Done.Click
        Close()
    End Sub

    Sub Bu_Update_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Update.Click
        Cursor = Cursors.AppStarting
        Bu_Done.IsEnabled = False
        Bu_Update.IsEnabled = False

        Update_DownloadToPath = AppSettings.Tool_Update_SavePath & "\" & Update_FileName
        If File.Exists(Update_DownloadToPath) Then File.Delete(Update_DownloadToPath)
        Directory.CreateDirectory(Path.GetDirectoryName(Update_DownloadToPath))
        If AppSettings.Tool_Update_UseDownloadPatcher Then
            Directory.CreateDirectory(Path.GetDirectoryName(Update_DownloadPatcherToPath))
            If Not File.Exists(Update_DownloadPatcherToPath) Then
                If File.Exists(Update_DownloadPatcherToPath & ".tmp") Then File.Delete(Update_DownloadPatcherToPath & ".tmp")
                DownloadMode = DownloadModes.DownloadPatcher
                Client.DownloadFileAsync(New Uri(Update_Path_UpdatePatcher), Update_DownloadPatcherToPath & ".tmp")
            Else
                Action_DownloadUpdate()
            End If
        Else
            Action_DownloadUpdate()
        End If
    End Sub

    Sub Client_DownloadFileCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs) Handles Client.DownloadFileCompleted
        Select Case DownloadMode
            Case DownloadModes.DownloadPatcher
                File.Move(Update_DownloadPatcherToPath & ".tmp",
                              Update_DownloadPatcherToPath)
                Action_DownloadUpdate()
            Case DownloadModes.DownloadUpdate
                Cursor = Cursors.Arrow
                TB_Status.Text = _e("WindowUpdater_downloadFinished").Replace("%0", Update_TotalBytes)
                Bu_Done.IsEnabled = True
                File.Move(Update_DownloadToPath & ".tmp",
                              Update_DownloadToPath)
                If AppSettings.Tool_Update_UseDownloadPatcher Then
                    ' Run UpdatePatcher
                    Dim UpdatePatcher As New ProcessStartInfo()
                    With UpdatePatcher
                        .Arguments = "-deletePackageAfter=""" & AppSettings.Tool_Update_DeleteFileAfter.ToString & """"
                        .Arguments += " -destinationVersion=""" & Update_Version & """"
                        .Arguments += " -pathToApp=""" & Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location) & """"
                        .Arguments += " -pathToUpdate=""" & Update_DownloadToPath & """"
                        .Arguments += " -sourceVersion=""" & My.Application.Info.Version.ToString & """"
                        .FileName = Update_DownloadPatcherToPath
                    End With
                    Process.Start(UpdatePatcher)
                    Windows.Application.Current.Shutdown()
                    Exit Sub
                Else
                    If MessageBox.Show(_e("WindowUpdater_doYouWantToOpenPathWhereUpdatedFilesHaveBeenSaved"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then Process.Start(AppSettings.Tool_Update_SavePath)
                End If
        End Select
    End Sub

    Sub Client_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs) Handles Client.DownloadProgressChanged
        Select Case DownloadMode
            Case DownloadModes.DownloadPatcher
                Update_TotalBytes = CStr(e.TotalBytesToReceive)
                PB_Progress.Value = e.ProgressPercentage
                TB_Status.Text = _e("WindowUpdater_downloadingInstaller").Replace("%0", e.BytesReceived.ToString).Replace("%1", e.TotalBytesToReceive.ToString)
            Case DownloadModes.DownloadUpdate
                Update_TotalBytes = CStr(e.TotalBytesToReceive)
                PB_Progress.Value = e.ProgressPercentage
                TB_Status.Text = _e("WindowUpdater_downloadingUpdatePackage").Replace("%0", e.BytesReceived.ToString).Replace("%1", e.TotalBytesToReceive.ToString)
        End Select
    End Sub

    Sub Client_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs) Handles Client.DownloadStringCompleted
        Select Case DownloadMode
            Case DownloadModes.Info
                Dim Answer As JObject
                Try
                    Answer = JObject.Parse(e.Result)
                Catch ex As JsonReaderException
                    MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_invalidServerResponse") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, AppName)
                    TB_VersionInfo.Text += " | " & _e("WindowUpdater_unableToCommunicateWithServer")
                    TB_Status.Text = _e("WindowUpdater_unableToCommunicateWithServer")
                    PB_Progress.IsIndeterminate = False
                    Exit Sub
                Catch ex As Reflection.TargetInvocationException
                    Clipboard.SetText("https: //osu.ppy.sh/forum/t/270446")
                    MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsVisitTheOsuForum"), MsgBoxStyle.Critical, AppName)
                    TB_VersionInfo.Text += " | " & _e("WindowUpdater_unableToCommunicateWithServer")
                    TB_Status.Text = _e("WindowUpdater_unableToCommunicateWithServer")
                    PB_Progress.IsIndeterminate = False
                    Exit Sub
                End Try

                Update_Version = CStr(Answer.SelectToken("latestRepoRelease").SelectToken("tag_name"))
                TB_VersionInfo.Text += " | " & _e("WindowUpdater_latestVersion").Replace("%0", Update_Version)

                Dim Paragraph As New Paragraph()
                Dim FlowDocument As New FlowDocument()
                Try
                    With Paragraph.Inlines
                        .Add(New Run("# " & _e("WindowUpdater_dateOfPublication").Replace("%0", CStr(Answer.SelectToken("latestRepoRelease").SelectToken("published_at")).Substring(0, 10))))
                        .Add(New LineBreak())
                        .Add(New LineBreak())
                        .Add(New Run(CStr(Answer.SelectToken("latestRepoRelease").SelectToken("body")).Replace("```Indent" & vbNewLine, "").Replace(vbNewLine & "```", "")))
                    End With
                Catch ex As Exception
                    MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsVisitTheOsuForum"), MsgBoxStyle.Critical, AppName)
                    Close()
                    Exit Sub
                End Try
                FlowDocument.Blocks.Add(Paragraph)
                RTB_Changelog.Document = FlowDocument
                Cursor = Cursors.Arrow
                PB_Progress.IsIndeterminate = False

                If Update_Version = My.Application.Info.Version.ToString Then
                    TB_Status.Text = _e("WindowUpdater_yourUsingTheLatestVersion")
#If DEBUG Then
                    Console.WriteLine("[DEBUG] Enabled Download button")
                    Action_LoadUpdateInformation(Answer)
#End If
                Else
                    TB_Status.Text = _e("WindowUpdater_anUpdateIsAvailable")
                    Action_LoadUpdateInformation(Answer)
                End If
        End Select
    End Sub

    Sub Window_Updater_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If AppSettings.Tool_Update_UseDownloadPatcher = False Then Bu_Update.Content = _e("WindowUpdater_download")
#If DEBUG Then
        TB_VersionInfo.Text = _e("WindowUpdater_yourVersion").Replace("%0", My.Application.Info.Version.ToString & " (Dev)")
#Else
        TB_VersionInfo.Text = _e("WindowUpdater_yourVersion").Replace("%0", My.Application.Info.Version.ToString)
#End If
        Client.DownloadStringAsync(New Uri(WebNw520ApiRoot & "app/updater.latestVersion.json"))
    End Sub
End Class
