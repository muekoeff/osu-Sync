Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Class Window_Settings

    Function CreateShortcut(ByVal sLinkFile As String,
                                   ByVal sTargetFile As String,
                                   Optional ByVal sArguments As String = "",
                                   Optional ByVal sDescription As String = "",
                                   Optional ByVal sWorkingDir As String = "") As Boolean    'Source: http://www.vbarchiv.net/tipps/details.php?id=1601
        Try
            Dim oShell As New Shell32.Shell
            Dim oFolder As Shell32.Folder
            Dim oLink As Shell32.ShellLinkObject
            Dim sPath As String = sLinkFile.Substring(0, sLinkFile.LastIndexOf("\"))
            Dim sFile As String = sLinkFile.Substring(sLinkFile.LastIndexOf("\") + 1)
            Dim F As Short = CShort(FreeFile())
            FileOpen(F, sLinkFile, OpenMode.Output)
            FileClose(F)
            oFolder = oShell.NameSpace(sPath)
            oLink = CType(oFolder.Items.Item(sFile).GetLink, Shell32.ShellLinkObject)
            With oLink
                If sArguments.Length > 0 Then .Arguments = sArguments
                If sDescription.Length > 0 Then .Description = sDescription
                If sWorkingDir.Length > 0 Then .WorkingDirectory = sWorkingDir
                .Path = sTargetFile
                .Save()
            End With
            oLink = Nothing
            oFolder = Nothing
            oShell = Nothing
            Return True
        Catch ex As Exception
            If File.Exists(sLinkFile) Then File.Delete(sLinkFile)
            Return False
        End Try
    End Function

    Function ValidateEmail(ByVal email As String) As Boolean
        Dim emailRegex As New Text.RegularExpressions.Regex("^(?<user>[^@]+)@(?<host>.+)$")
        Dim emailMatch As Text.RegularExpressions.Match = emailRegex.Match(email)
        Return emailMatch.Success
    End Function

    Sub ApplySettings()
        With AppSettings
            .Api_Enabled_BeatmapPanel = CBool(CB_ApiEnableInBeatmapPanel.IsChecked)
            .Api_Key = TB_ApiKey.Text
            .osu_Path = TB_osu_Path.Text
            .osu_SongsPath = TB_osu_SongsPath.Text
            .Tool_CheckFileAssociation = CBool(CB_ToolCheckFileAssociation.IsChecked)
            .Tool_CheckForUpdates = CB_ToolCheckForUpdates.SelectedIndex
            .Tool_DownloadMirror = CB_ToolDownloadMirror.SelectedIndex
            .Tool_EnableNotifyIcon = CB_ToolEnableNotifyIcon.SelectedIndex
            If IsNumeric(TB_ToolImporterAutoInstallCounter.Text) Then .Tool_Importer_AutoInstallCounter = CInt(TB_ToolImporterAutoInstallCounter.Text)
            If IsNumeric(TB_ToolInterface_BeatmapDetailPanelWidth.Text) AndAlso
                CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) >= 5 And CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) <= 95 Then .Tool_Interface_BeatmapDetailPanelWidth = CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text)
            .Tool_SyncOnStartup = CBool(CB_ToolSyncOnStartup.IsChecked)
            ' Load Language
            Dim LanguageCode_Short As String = CB_ToolLanguages.Text.Substring(0, CB_ToolLanguages.Text.IndexOf(" "))
            If Not CB_ToolLanguages.Text = "" And Not .Tool_Language = LanguageCode_Short Then
                If Not TranslationNameGet(LanguageCode_Short) = "" Then
                    LanguageLoad(TranslationNameGet(LanguageCode_Short), LanguageCode_Short)
                    MsgBox(_e("WindowSettings_languageUpdated"), MsgBoxStyle.Information, AppName)
                End If
            End If
            .Tool_RequestElevationOnStartup = CBool(CB_ToolRequestElevationOnStartup.IsChecked)
            .Tool_Update_SavePath = TB_ToolUpdate_Path.Text
            .Tool_Update_DeleteFileAfter = CBool(CB_ToolUpdateDeleteFileAfter.IsChecked)
            .Tool_Update_UseDownloadPatcher = CBool(CB_ToolUpdate_UseDownloadPatcher.IsChecked)
            .Messages_Importer_AskOsu = CBool(CB_MessagesImporterAskOsu.IsChecked)
            .Messages_Updater_OpenUpdater = CBool(CB_MessagesUpdaterOpenUpdater.IsChecked)
            .Messages_Updater_UnableToCheckForUpdates = CBool(CB_MessagesUpdaterUnableToCheckForUpdates.IsChecked)
            .SaveSettings()
        End With
    End Sub

    Sub ApiClient_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs)
        Dim JSON_Array As JArray
        Try
            WriteToApiLog("/api/get_beatmaps", e.Result)
            JSON_Array = CType(JsonConvert.DeserializeObject(e.Result), JArray)
            If Not CType(JSON_Array.First, JObject).SelectToken("beatmapset_id") Is Nothing Then
                With Bu_ApiKey_Validate
                    .Content = _e("WindowSettings_valid")
                    .IsEnabled = True
                End With
                TB_ApiKey.IsEnabled = True
            Else
                Throw New ArgumentException("Unexpected value")
            End If
        Catch ex As Exception
            WriteToApiLog("/api/get_beatmaps")
            With Bu_ApiKey_Validate
                .Content = _e("WindowSettings_invalid")
                .IsEnabled = True
            End With
            TB_ApiKey.IsEnabled = True
        End Try
    End Sub

    Sub Bu_ApiKey_Validate_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ApiKey_Validate.Click
        With Bu_ApiKey_Validate
            .Content = "..."
            .IsEnabled = False
        End With
        TB_ApiKey.IsEnabled = False
        Dim ApiClient As New WebClient
        AddHandler ApiClient.DownloadStringCompleted, AddressOf ApiClient_DownloadStringCompleted
        ApiClient.DownloadStringAsync(New Uri(WebOsuApiRoot & "get_beatmaps?k=" & TB_ApiKey.Text))
    End Sub

    Sub Bu_ApiOpenLog_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ApiOpenLog.Click
        If File.Exists(AppDataPath & "\Logs\ApiAccess.txt") Then
            Process.Start(AppDataPath & "\Logs\ApiAccess.txt")
        Else
            MsgBox(_e("WindowSettings_nopeDirectoryDoesNotExit"), MsgBoxStyle.Exclamation, AppName)
        End If
    End Sub

    Sub Bu_ApiRequest_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ApiRequest.Click
        Process.Start("https://osu.ppy.sh/p/api")
    End Sub

    Sub Bu_Apply_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Apply.Click
        ApplySettings()
    End Sub

    Sub Bu_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Cancel.Click
        Close()
    End Sub

    Sub Bu_CreateShortcut_Click(sender As Object, e As RoutedEventArgs) Handles Bu_CreateShortcut.Click
        If Not File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk") Then
            If CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk",
                              Reflection.Assembly.GetExecutingAssembly().Location.ToString, "",
                              _e("WindowSettings_launchOsuSync")) Then
            Else
                MsgBox(_e("WindowSettings_unableToCreateShortcut"), MsgBoxStyle.Critical, AppName)
            End If
        Else
            MsgBox(_e("WindowSettings_theresAlreadyAShortcut"), MsgBoxStyle.Exclamation, AppName)
        End If
    End Sub

    Sub Bu_Done_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Done.Click
        ApplySettings()
        Close()
    End Sub

    Sub Bu_FeedbackSubmit_Click(sender As Object, e As RoutedEventArgs) Handles Bu_FeedbackSubmit.Click
        Dim RTB_FeedbackMessage_TextRange As New TextRange(RTB_FeedbackMessage.Document.ContentStart, RTB_FeedbackMessage.Document.ContentEnd)

        If TB_FeedbackUsername.Text.Length <= 1 Then
            MsgBox(_e("WindowSettings_yourNameIsTooShort"), MsgBoxStyle.Exclamation, AppName)
        ElseIf Not ValidateEmail(TB_FeedbackeMail.Text) Then
            MsgBox(_e("WindowSettings_yourEmailInvalid"), MsgBoxStyle.Exclamation, AppName)
        ElseIf CB_FeedbackCategory.SelectedIndex = -1 Then
            MsgBox(_e("WindowSettings_youHaveToSelectACategory"), MsgBoxStyle.Exclamation, AppName)
        ElseIf RTB_FeedbackMessage_TextRange.Text.Length < 30 Then
            MsgBox(_e("WindowSettings_yourMessageSeemsToBeQuiteShort"), MsgBoxStyle.Exclamation, AppName)
        Else
            StackPanel_Feedback.IsEnabled = False
            Gr_FeedbackOverlay.Visibility = Visibility.Visible

            Using SubmitClient As New WebClient
                Dim ReqParam As New Specialized.NameValueCollection
                With ReqParam
                    .Add("category", CB_FeedbackCategory.Tag.ToString)
                    .Add("debugData", Ru_FeedbackInfo.Text)
                    .Add("email", TB_FeedbackeMail.Text)
                    .Add("message", RTB_FeedbackMessage_TextRange.Text)
                    .Add("username", TB_FeedbackUsername.Text)
                    .Add("version", My.Application.Info.Version.ToString)
                End With
                Dim ResponseBytes = SubmitClient.UploadValues(WebNw520ApiRoot & "app/feedback.submitReport.php", "POST", ReqParam)
                Dim ResponseBody = (New Text.UTF8Encoding).GetString(ResponseBytes)

                Try
                    MsgBox(_e("WindowSettings_serverSideAnswer") & vbNewLine & ResponseBody, MsgBoxStyle.Information, AppName)
                Catch ex As Reflection.TargetInvocationException
                    MsgBox(_e("WindowSettings_unableToSubmitFeedback") & vbNewLine &
                           "> " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("WindowSettings_pleaseTryAgainLaterOrContactUs"), MsgBoxStyle.Critical, AppName)
                    Exit Sub
                End Try
                Gr_FeedbackOverlay.Visibility = Visibility.Collapsed
            End Using
        End If
    End Sub

    Sub Bu_osuSongPathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Bu_osuSongPathDefault.Click
        TB_osu_SongsPath.Text = TB_osu_Path.Text & "\Songs"
    End Sub

    Sub Bu_ToolDeleteConfiguration_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolDeleteConfiguration.Click
        If MessageBox.Show(_e("WindowSettings_areYouSureYouWantToDeleteConfig"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            If File.Exists(AppDataPath & "\Settings\Settings.config") Then
                File.Delete(AppDataPath & "\Settings\Settings.config")

                If MessageBox.Show(_e("WindowSettings_okDoneDoYouWantToRestart"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                    Forms.Application.Restart()
                End If
                Windows.Application.Current.Shutdown()
            Else
                MsgBox(_e("WindowSettings_nopeNoConfig"), MsgBoxStyle.Exclamation, AppName)
            End If
        End If
    End Sub

    Sub Bu_ToolDeleteFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolDeleteFileAssociation.Click
        If FileAssociationsDelete() Then MsgBox(_e("MainWindow_extensionDeleteDone"), MsgBoxStyle.Information, AppName)
    End Sub

    Sub Bu_ToolImporterAutoInstallCounterDown_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolImporterAutoInstallCounterDown.Click
        If IsNumeric(TB_ToolImporterAutoInstallCounter.Text) AndAlso CInt(TB_ToolImporterAutoInstallCounter.Text) > 0 Then
            TB_ToolImporterAutoInstallCounter.Text = CStr(CInt(TB_ToolImporterAutoInstallCounter.Text) - 1)
        Else
            TB_ToolImporterAutoInstallCounter.Text = "10"
        End If
    End Sub

    Sub Bu_ToolImporterAutoInstallCounterUp_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolImporterAutoInstallCounterUp.Click
        If IsNumeric(TB_ToolImporterAutoInstallCounter.Text) AndAlso CInt(TB_ToolImporterAutoInstallCounter.Text) > 0 Then
            TB_ToolImporterAutoInstallCounter.Text = CStr(CInt(TB_ToolImporterAutoInstallCounter.Text) + 1)
        Else
            TB_ToolImporterAutoInstallCounter.Text = "10"
        End If
    End Sub

    Sub Bu_ToolInterface_BeatmapDetailPanelWidth_Down_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolInterface_BeatmapDetailPanelWidth_Down.Click
        If IsNumeric(TB_ToolInterface_BeatmapDetailPanelWidth.Text) AndAlso CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) > 5 Then
            TB_ToolInterface_BeatmapDetailPanelWidth.Text = CStr(CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) - 1)
        Else
            TB_ToolInterface_BeatmapDetailPanelWidth.Text = "40"
        End If
    End Sub

    Sub Bu_ToolInterface_BeatmapDetailPanelWidth_Up_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolInterface_BeatmapDetailPanelWidth_Up.Click
        If IsNumeric(TB_ToolInterface_BeatmapDetailPanelWidth.Text) AndAlso CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) < 95 Then
            TB_ToolInterface_BeatmapDetailPanelWidth.Text = CStr(CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) + 1)
        Else
            TB_ToolInterface_BeatmapDetailPanelWidth.Text = "40"
        End If
    End Sub

    Sub Bu_ToolOpenDataFolder_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolOpenDataFolder.Click
        If Directory.Exists(AppDataPath) Then
            Process.Start(AppDataPath)
        Else
            MsgBox(_e("WindowSettings_nopeDirectoryDoesNotExit"), MsgBoxStyle.Exclamation, AppName)
        End If
    End Sub

    Sub Bu_ToolReset_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolReset.Click
        If MessageBox.Show(_e("WindowSettings_areYouSureYouWantToReset"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            FileAssociationsDelete()
            If Directory.Exists(AppDataPath) Then
                Try
                    Directory.Delete(AppDataPath, True)
                Catch ex As IOException
                End Try
            End If
            If Directory.Exists(AppTempPath) Then
                Try
                    Directory.Delete(AppTempPath, True)
                Catch ex As IOException
                End Try
            End If
            If MessageBox.Show(_e("WindowSettings_okDoneDoYouWantToRestart"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then Forms.Application.Restart()
            Windows.Application.Current.Shutdown()
        End If
    End Sub

    Sub Bu_ToolRestartElevated_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolRestartElevated.Click
        If RequestElevation() Then
            Tool_DontApplySettings = True
            Windows.Application.Current.Shutdown()
            Exit Sub
        Else
            MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, AppName)
        End If
    End Sub

    Sub Bu_ToolUpdateFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolUpdateFileAssociation.Click
        FileAssociationsCreate()
    End Sub

    Sub Bu_ToolUpdate_PathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ToolUpdate_PathDefault.Click
        TB_ToolUpdate_Path.Text = AppTempPath & "\Updater"
    End Sub

    Sub TB_ApiKey_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TB_ApiKey.TextChanged
        Bu_ApiKey_Validate.Content = _e("WindowSettings_validate")
    End Sub

    Sub TB_osu_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TB_osu_Path.GotFocus
        Dim SelectFile As New Forms.OpenFileDialog With {
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "exe",
            .FileName = "osu!",
            .Filter = _e("WindowSettings_executableFiles") & " (*.exe)|*.exe",
            .InitialDirectory = AppSettings.OsuPathDetect(),
            .Multiselect = False,
            .Title = _e("WindowSettings_pleaseOpenOsu")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            If Path.GetFileName(SelectFile.FileName) = "osu!.exe" Then
                TB_osu_Path.Text = Path.GetDirectoryName(SelectFile.FileName)
            Else
                MsgBox(_e("WindowSettings_youSelectedTheWrongFile"), MsgBoxStyle.Exclamation, AppName)
            End If
        End If
    End Sub

    Sub TB_osu_SongsPath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TB_osu_SongsPath.GotFocus
        Dim SelectFile As New Forms.FolderBrowserDialog With {
            .Description = _e("WindowSettings_pleaseSelectSongsFolder")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then TB_osu_SongsPath.Text = SelectFile.SelectedPath
    End Sub

    Sub TB_ToolImporterAutoInstallCounter_LostFocus(sender As Object, e As RoutedEventArgs) Handles TB_ToolImporterAutoInstallCounter.LostFocus
        If Not IsNumeric(TB_ToolImporterAutoInstallCounter.Text) Then TB_ToolImporterAutoInstallCounter.Text = _e("WindowSettings_invalidValue")
    End Sub

    Sub TB_ToolInterface_BeatmapDetailPanelWidth_LostFocus(sender As Object, e As RoutedEventArgs) Handles TB_ToolInterface_BeatmapDetailPanelWidth.LostFocus
        If Not IsNumeric(TB_ToolInterface_BeatmapDetailPanelWidth.Text) Or
            CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) < 5 Or CInt(TB_ToolInterface_BeatmapDetailPanelWidth.Text) > 95 Then TB_ToolInterface_BeatmapDetailPanelWidth.Text = _e("WindowSettings_invalidValue")
    End Sub

    Sub TB_ToolUpdate_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TB_ToolUpdate_Path.GotFocus
        Dim SelectDirectory As New Forms.FolderBrowserDialog With {
            .Description = _e("WindowSettings_pleaseSelectDirectoryWhereToSaveUpdates"),
            .ShowNewFolderButton = False}
        If Directory.Exists(AppSettings.Tool_Update_SavePath) Then
            SelectDirectory.SelectedPath = AppSettings.Tool_Update_SavePath
        Else
            SelectDirectory.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        End If

        If Not SelectDirectory.ShowDialog() = Forms.DialogResult.Cancel Then TB_ToolUpdate_Path.Text = SelectDirectory.SelectedPath
    End Sub

    Sub TC_Main_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles TC_Main.SelectionChanged
        Select Case TC_Main.SelectedIndex
            Case 4
                ' Prepare Feedback form
                Ru_FeedbackInfo.Text = JsonConvert.SerializeObject(ProgramInfoJsonGet(), Formatting.None)
                With StackPanel_Feedback
                    .IsEnabled = True
                    .Margin = New Thickness(0, 0, 0, 0)
                    .Visibility = Visibility.Visible
                End With
        End Select
    End Sub

    Sub Window_Settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Tool_IsElevated Then
            Bu_ToolRestartElevated.IsEnabled = False
            TB_NotElevated.Visibility = Visibility.Collapsed
        Else
            Bu_ToolDeleteFileAssociation.IsEnabled = False
            Bu_ToolReset.IsEnabled = False
            Bu_ToolUpdateFileAssociation.IsEnabled = False
        End If

        CB_ApiEnableInBeatmapPanel.IsChecked = AppSettings.Api_Enabled_BeatmapPanel
        CB_MessagesImporterAskOsu.IsChecked = AppSettings.Messages_Importer_AskOsu
        CB_MessagesUpdaterOpenUpdater.IsChecked = AppSettings.Messages_Updater_OpenUpdater
        CB_MessagesUpdaterUnableToCheckForUpdates.IsChecked = AppSettings.Messages_Updater_UnableToCheckForUpdates
        CB_ToolCheckFileAssociation.IsChecked = AppSettings.Tool_CheckFileAssociation
        CB_ToolRequestElevationOnStartup.IsChecked = AppSettings.Tool_RequestElevationOnStartup
        CB_ToolSyncOnStartup.IsChecked = AppSettings.Tool_SyncOnStartup
        CB_ToolUpdateDeleteFileAfter.IsChecked = AppSettings.Tool_Update_DeleteFileAfter
        CB_ToolUpdate_UseDownloadPatcher.IsChecked = AppSettings.Tool_Update_UseDownloadPatcher
        CB_ToolCheckForUpdates.SelectedIndex = AppSettings.Tool_CheckForUpdates
        ' Load mirrors and select current one
        For Each a In Application_Mirrors
            CB_ToolDownloadMirror.Items.Add(a.Value.DisplayName)
            If a.Key = 0 Then CB_ToolDownloadMirror.Items.Add(New Separator)
        Next
        CB_ToolDownloadMirror.SelectedIndex = AppSettings.Tool_DownloadMirror
        CB_ToolEnableNotifyIcon.SelectedIndex = AppSettings.Tool_EnableNotifyIcon
        ' Load languages and select current one
        Dim InsertedCodes As New List(Of String)
        Dim i As Integer = 0
        Dim IndexUserLanguage As Integer = -1
        Dim IndexEN As Integer = 0
        For Each a In Application_Languages
            If Not InsertedCodes.Contains(a.Value.Code) Then
                If a.Value.Code = "en_US" Then IndexEN = i
                If a.Key = AppSettings.Tool_Language Then IndexUserLanguage = i
                InsertedCodes.Add(a.Value.Code)
                CB_ToolLanguages.Items.Add(a.Key & " | " & a.Value.DisplayName_English & "/" & a.Value.DisplayName)
                i += 1
            End If
        Next
        If Not IndexUserLanguage = -1 Then
            CB_ToolLanguages.SelectedIndex = IndexUserLanguage
        Else
            CB_ToolLanguages.SelectedIndex = IndexEN
        End If
        TB_ApiKey.Text = AppSettings.Api_Key
        TB_osu_Path.Text = AppSettings.osu_Path
        TB_osu_SongsPath.Text = AppSettings.osu_SongsPath
        TB_ToolImporterAutoInstallCounter.Text = AppSettings.Tool_Importer_AutoInstallCounter.ToString
        TB_ToolInterface_BeatmapDetailPanelWidth.Text = AppSettings.Tool_Interface_BeatmapDetailPanelWidth.ToString
        TB_ToolUpdate_Path.Text = AppSettings.Tool_Update_SavePath
    End Sub
End Class