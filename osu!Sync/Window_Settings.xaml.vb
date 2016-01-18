Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Window_Settings

    Private Function CreateShortcut(ByVal sLinkFile As String,
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

    Private Sub Action_ApplySettings()
        Setting_Api_Enabled_BeatmapPanel = CBool(CheckBox_Api_EnableInBeatmapPanel.IsChecked)
        Setting_Api_Key = TextBox_Api_ApiKey.Text
        Setting_osu_Path = TextBox_osu_Path.Text
        Setting_osu_SongsPath = TextBox_osu_SongsPath.Text
        Setting_Tool_CheckFileAssociation = CBool(CheckBox_Tool_CheckFileAssociation.IsChecked)
        Setting_Tool_CheckForUpdates = ComboBox_Tool_CheckForUpdates.SelectedIndex
        Setting_Tool_DownloadMirror = ComboBox_Tool_DownloadMirror.SelectedIndex
        Setting_Tool_EnableNotifyIcon = ComboBox_Tool_EnableNotifyIcon.SelectedIndex
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_Importer_AutoInstallCounter.Text, Val) Then Setting_Tool_Importer_AutoInstallCounter = Val
        If Integer.TryParse(TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text, Val) AndAlso Val >= 5 And Val <= 95 Then Setting_Tool_Interface_BeatmapDetailPanelWidth = Val
        Setting_Tool_SyncOnStartup = CBool(CheckBox_Tool_SyncOnStartup.IsChecked)
        ' Load Language
        Dim LanguageCode_Short As String = ComboBox_Tool_Languages.Text.Substring(0, ComboBox_Tool_Languages.Text.IndexOf(" "))
        If Not ComboBox_Tool_Languages.Text = "" And Not Setting_Tool_Language = LanguageCode_Short Then
            If Not GetTranslationName(LanguageCode_Short) = "" Then
                LoadLanguage(GetTranslationName(LanguageCode_Short), LanguageCode_Short)
                MsgBox(_e("WindowSettings_languageUpdated"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
            End If
        End If
        Setting_Tool_RequestElevationOnStartup = CBool(CheckBox_Tool_RequestElevationOnStartup.IsChecked)
        Setting_Tool_Update_SavePath = TextBox_Tool_Update_Path.Text
        Setting_Tool_Update_DeleteFileAfter = CBool(CheckBox_Tool_UpdateDeleteFileAfter.IsChecked)
        Setting_Tool_Update_UseDownloadPatcher = CBool(CheckBox_Tool_Update_UseDownloadPatcher.IsChecked)
        Setting_Messages_Importer_AskOsu = CBool(CheckBox_Messages_Importer_AskOsu.IsChecked)
        Setting_Messages_Updater_OpenUpdater = CBool(CheckBox_Messages_Updater_OpenUpdater.IsChecked)
        Setting_Messages_Updater_UnableToCheckForUpdates = CBool(CheckBox_Messages_Updater_UnableToCheckForUpdates.IsChecked)
        Action_SaveSettings()
    End Sub

    Private Sub ApiClient_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs)
        Dim JSON_Array As JArray
        Try
            WriteToApiLog("/api/get_beatmaps", e.Result)
            JSON_Array = CType(JsonConvert.DeserializeObject(e.Result), JArray)
            If Not CType(JSON_Array.First, JObject).SelectToken("beatmapset_id") Is Nothing Then
                With Button_Api_ApiKey_Validate
                    .Content = _e("WindowSettings_valid")
                    .IsEnabled = True
                End With
                TextBox_Api_ApiKey.IsEnabled = True
            Else
                Throw New ArgumentException("Unexpected value")
            End If
        Catch ex As Exception
            WriteToApiLog("/api/get_beatmaps")
            With Button_Api_ApiKey_Validate
                .Content = _e("WindowSettings_invalid")
                .IsEnabled = True
            End With
            TextBox_Api_ApiKey.IsEnabled = True
        End Try
    End Sub

    Private Sub Button_Api_ApiKey_Validate_Click(sender As Object, e As RoutedEventArgs) Handles Button_Api_ApiKey_Validate.Click
        With Button_Api_ApiKey_Validate
            .Content = "..."
            .IsEnabled = False
        End With
        TextBox_Api_ApiKey.IsEnabled = False
        Dim ApiClient As New WebClient
        AddHandler ApiClient.DownloadStringCompleted, AddressOf ApiClient_DownloadStringCompleted
        ApiClient.DownloadStringAsync(New Uri(I__Path_Web_osuApi & "get_beatmaps?k=" & TextBox_Api_ApiKey.Text))
    End Sub

    Private Sub Button_Api_OpenLog_Click(sender As Object, e As RoutedEventArgs) Handles Button_Api_OpenLog.Click
        If File.Exists(I__Path_Programm & "\Logs\ApiAccess.txt") Then
            Process.Start(I__Path_Programm & "\Logs\ApiAccess.txt")
        Else
            MsgBox(_e("WindowSettings_nopeDirectoryDoesNotExit"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Api_Request_Click(sender As Object, e As RoutedEventArgs) Handles Button_Api_Request.Click
        Process.Start("https://osu.ppy.sh/p/api")
    End Sub

    Private Sub Button_Apply_Click(sender As Object, e As RoutedEventArgs) Handles Button_Apply.Click
        Action_ApplySettings()
    End Sub

    Private Sub Button_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Button_Cancel.Click
        Close()
    End Sub

    Private Sub Button_CreateShortcut_Click(sender As Object, e As RoutedEventArgs) Handles Button_CreateShortcut.Click
        If Not File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk") Then
            If CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk",
                              Reflection.Assembly.GetExecutingAssembly().Location.ToString, "",
                              _e("WindowSettings_launchOsuSync")) Then
            Else
                MsgBox(_e("WindowSettings_unableToCreateShortcut"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
        Else
            MsgBox(_e("WindowSettings_theresAlreadyAShortcut"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Done_Click(sender As Object, e As RoutedEventArgs) Handles Button_Done.Click
        Action_ApplySettings()
        Close()
    End Sub

    Private Sub Button_Feedback_Prepare_Click(sender As Object, e As RoutedEventArgs) Handles Button_Feedback_Prepare.Click
        Run_Feedback_FurtherInfo.Text = JsonConvert.SerializeObject(GetProgramInfoJson(), Formatting.None)

        With Button_Feedback_Prepare
            .IsEnabled = False
            .Visibility = Visibility.Collapsed
        End With
        With StackPanel_Feedback
            .IsEnabled = True
            .Margin = New Thickness(0, 0, 0, 0)
            .Visibility = Visibility.Visible
        End With
    End Sub

    Private Sub Button_Feedback_Submit_Click(sender As Object, e As RoutedEventArgs) Handles Button_Feedback_Submit.Click
        Dim RichTextBox_Feedback_Message_TextRange As New TextRange(RichTextBox_Feedback_Message.Document.ContentStart, RichTextBox_Feedback_Message.Document.ContentEnd)

        If TextBox_Feedback_Username.Text.Length <= 1 Then
            MsgBox(_e("WindowSettings_yourNameIsTooShort"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf Not ValidateEmail(TextBox_Feedback_eMail.Text) Then
            MsgBox(_e("WindowSettings_yourEmailInvalid"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf ComboBox_Feedback_Category.SelectedIndex = -1 Then
            MsgBox(_e("WindowSettings_youHaveToSelectACategory"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf RichTextBox_Feedback_Message_TextRange.Text.Length < 30 Then
            MsgBox(_e("WindowSettings_yourMessageSeemsToBeQuiteShort"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        Else
            StackPanel_Feedback.IsEnabled = False
            Grid_Feedback_Overlay.Visibility = Visibility.Visible

            Using SubmitClient As New WebClient
                Dim ReqParam As New Specialized.NameValueCollection
                With ReqParam
                    .Add("category", ComboBox_Feedback_Category.Text)
                    .Add("debugData", Run_Feedback_FurtherInfo.Text)
                    .Add("email", TextBox_Feedback_eMail.Text)
                    .Add("message", RichTextBox_Feedback_Message_TextRange.Text)
                    .Add("username", TextBox_Feedback_Username.Text)
                    .Add("version", My.Application.Info.Version.ToString)
                End With
                Dim ResponseBytes = SubmitClient.UploadValues(I__Path_Web_nw520OsySyncApi & "app/feedback.submitReport.php", "POST", ReqParam)
                Dim ResponseBody = (New Text.UTF8Encoding).GetString(ResponseBytes)

                Try
                    MsgBox(_e("WindowSettings_serverSideAnswer") & vbNewLine & ResponseBody, MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
                Catch ex As Reflection.TargetInvocationException
                    MsgBox(_e("WindowSettings_unableToSubmitFeedback") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("WindowSettings_pleaseTryAgainLaterOrContactUs"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                    Exit Sub
                End Try
                Grid_Feedback_Overlay.Visibility = Visibility.Collapsed
            End Using
        End If
    End Sub

    Private Sub Button_osu_SongPathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Button_osu_SongPathDefault.Click
        TextBox_osu_SongsPath.Text = TextBox_osu_Path.Text & "\Songs"
    End Sub

    Private Sub Button_Tool_DeleteConfiguration_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_DeleteConfiguration.Click
        If MessageBox.Show(_e("WindowSettings_areYouSureYouWantToDeleteConfig"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            If File.Exists(I__Path_Programm & "\Settings\Settings.config") Then
                File.Delete(I__Path_Programm & "\Settings\Settings.config")

                If MessageBox.Show(_e("WindowSettings_okDoneDoYouWantToRestart"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                    Forms.Application.Restart()
                End If
                Windows.Application.Current.Shutdown()
            Else
                MsgBox(_e("WindowSettings_nopeNoConfig"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Private Sub Button_Tool_DeleteFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_DeleteFileAssociation.Click
        Dim RegisterError As Boolean = False
        Dim RegisterCounter As Integer = 0
        For Each Extension As String In Application_FileExtensions
            If DeleteFileAssociation(Extension, Application_FileExtensionsLong(RegisterCounter)) Then
                RegisterCounter += 1
            Else
                RegisterError = True
                Exit For
            End If
        Next

        If Not RegisterError Then
            MsgBox(_e("MainWindow_extensionDeleteDone"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
        Else
            MsgBox(_e("MainWindow_extensionDeleteFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_ImporterAuto_InstallCounter_Down_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_ImporterAuto_InstallCounter_Down.Click
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_Importer_AutoInstallCounter.Text, Val) Then
            If Val > 0 Then Textbox_Tool_Importer_AutoInstallCounter.Text = CStr(Val - 1)
        Else
            Textbox_Tool_Importer_AutoInstallCounter.Text = "10"
        End If
    End Sub

    Private Sub Button_Tool_Importer_AutoInstallCounter_Up_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Importer_AutoInstallCounter_Up.Click
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_Importer_AutoInstallCounter.Text, Val) Then
            Textbox_Tool_Importer_AutoInstallCounter.Text = CStr(Val + 1)
        Else
            Textbox_Tool_Importer_AutoInstallCounter.Text = "10"
        End If
    End Sub

    Private Sub Button_Tool_Interface_BeatmapDetailPanelWidth_Down_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Interface_BeatmapDetailPanelWidth_Down.Click
        Dim Val As Integer
        If Integer.TryParse(TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text, Val) Then
            If Val > 5 Then TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = CStr(Val - 1)
        Else
            TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = "40"
        End If
    End Sub

    Private Sub Button_Tool_Interface_BeatmapDetailPanelWidth_Up_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Interface_BeatmapDetailPanelWidth_Up.Click
        Dim Val As Integer
        If Integer.TryParse(TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text, Val) Then
            If Val < 95 Then TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = CStr(Val + 1)
        Else
            TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = "40"
        End If
    End Sub

    Private Sub Button_Tool_OpenDataFolder_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_OpenDataFolder.Click
        If Directory.Exists(I__Path_Programm) Then
            Process.Start(I__Path_Programm)
        Else
            MsgBox(_e("WindowSettings_nopeDirectoryDoesNotExit"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_Reset_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Reset.Click
        If MessageBox.Show(_e("WindowSettings_areYouSureYouWantToReset"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            Dim RegisterError As Boolean = False
            Dim RegisterCounter As Integer = 0
            For Each Extension As String In Application_FileExtensions
                If DeleteFileAssociation(Extension, Application_FileExtensionsLong(RegisterCounter)) Then
                    RegisterCounter += 1
                Else
                    RegisterError = True
                    Exit For
                End If
            Next

            If RegisterError Then MsgBox(_e("MainWindow_extensionDeleteFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            If Directory.Exists(I__Path_Programm) Then
                Try
                    Directory.Delete(I__Path_Programm, True)
                Catch ex As IOException
                End Try
            End If
            If Directory.Exists(I__Path_Temp) Then
                Try
                    Directory.Delete(I__Path_Temp, True)
                Catch ex As IOException
                End Try
            End If
            If MessageBox.Show(_e("WindowSettings_okDoneDoYouWantToRestart"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then Forms.Application.Restart()
            Windows.Application.Current.Shutdown()
        End If
    End Sub

    Private Sub Button_Tool_RestartElevated_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_RestartElevated.Click
        If Action_RequestElevation() Then
            Tool_DontApplySettings = True
            Windows.Application.Current.Shutdown()
            Exit Sub
        Else
            MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_UpdateFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_UpdateFileAssociation.Click
        Dim RegisterError As Boolean = False
        Dim RegisterCounter As Integer = 0
        For Each Extension As String In Application_FileExtensions
            If CreateFileAssociation(Extension,
                                                     Application_FileExtensionsLong(RegisterCounter),
                                                     Application_FileExtensionsDescription(RegisterCounter),
                                                     Application_FileExtensionsIcon(RegisterCounter),
                                                     Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
                RegisterCounter += 1
            Else
                RegisterError = True
                Exit For
            End If
        Next

        If Not RegisterError Then
            MsgBox(_e("MainWindow_extensionDone"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
        Else
            MsgBox(_e("MainWindow_extensionFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_Update_PathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Update_PathDefault.Click
        TextBox_Tool_Update_Path.Text = I__Path_Temp & "\Updater"
    End Sub

    Private Sub TextBox_Api_ApiKey_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBox_Api_ApiKey.TextChanged
        Button_Api_ApiKey_Validate.Content = _e("WindowSettings_validate")
    End Sub

    Private Sub TextBox_osu_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_Path.GotFocus
        Dim SelectFile As New Forms.OpenFileDialog With {
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "exe",
            .FileName = "osu!",
            .Filter = _e("WindowSettings_executableFiles") & " (*.exe)|*.exe",
            .InitialDirectory = GetDetectedOsuPath(),
            .Multiselect = False,
            .Title = _e("WindowSettings_pleaseOpenOsu")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            If Path.GetFileName(SelectFile.FileName) = "osu!.exe" Then
                TextBox_osu_Path.Text = Path.GetDirectoryName(SelectFile.FileName)
            Else
                MsgBox(_e("WindowSettings_youSelectedTheWrongFile"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Private Sub TextBox_osu_SongsPath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_SongsPath.GotFocus
        Dim SelectFile As New Forms.FolderBrowserDialog With {
            .Description = _e("WindowSettings_pleaseSelectSongsFolder")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then TextBox_osu_SongsPath.Text = SelectFile.SelectedPath
    End Sub

    Private Sub Textbox_Tool_Importer_AutoInstallCounter_LostFocus(sender As Object, e As RoutedEventArgs) Handles Textbox_Tool_Importer_AutoInstallCounter.LostFocus
        Dim Val As Integer
        If Not Integer.TryParse(Textbox_Tool_Importer_AutoInstallCounter.Text, Val) Then Textbox_Tool_Importer_AutoInstallCounter.Text = _e("WindowSettings_invalidValue")
    End Sub

    Private Sub TextBox_Tool_Interface_BeatmapDetailPanelWidth_LostFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_Tool_Interface_BeatmapDetailPanelWidth.LostFocus
        Dim Val As Integer
        If Not Integer.TryParse(TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text, Val) Or Val < 5 Or Val > 95 Then TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = _e("WindowSettings_invalidValue")
    End Sub

    Private Sub TextBox_Tool_Update_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_Tool_Update_Path.GotFocus
        Dim SelectDirectory As New Forms.FolderBrowserDialog With {
            .Description = _e("WindowSettings_pleaseSelectDirectoryWhereToSaveUpdates"),
            .ShowNewFolderButton = False}
        If Directory.Exists(Setting_Tool_Update_SavePath) Then
            SelectDirectory.SelectedPath = Setting_Tool_Update_SavePath
        Else
            SelectDirectory.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        End If

        If Not SelectDirectory.ShowDialog() = Forms.DialogResult.Cancel Then TextBox_Tool_Update_Path.Text = SelectDirectory.SelectedPath
    End Sub

    Private Sub Window_Settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Tool_IsElevated Then
            Button_Tool_RestartElevated.IsEnabled = False
            TextBlock_WarnNotElevated.Visibility = Visibility.Collapsed
        Else
            Button_Tool_DeleteFileAssociation.IsEnabled = False
            Button_Tool_Reset.IsEnabled = False
            Button_Tool_UpdateFileAssociation.IsEnabled = False
        End If

        CheckBox_Api_EnableInBeatmapPanel.IsChecked = Setting_Api_Enabled_BeatmapPanel
        CheckBox_Messages_Importer_AskOsu.IsChecked = Setting_Messages_Importer_AskOsu
        CheckBox_Messages_Updater_OpenUpdater.IsChecked = Setting_Messages_Updater_OpenUpdater
        CheckBox_Messages_Updater_UnableToCheckForUpdates.IsChecked = Setting_Messages_Updater_UnableToCheckForUpdates
        CheckBox_Tool_CheckFileAssociation.IsChecked = Setting_Tool_CheckFileAssociation
        CheckBox_Tool_RequestElevationOnStartup.IsChecked = Setting_Tool_RequestElevationOnStartup
        CheckBox_Tool_SyncOnStartup.IsChecked = Setting_Tool_SyncOnStartup
        CheckBox_Tool_UpdateDeleteFileAfter.IsChecked = Setting_Tool_Update_DeleteFileAfter
        CheckBox_Tool_Update_UseDownloadPatcher.IsChecked = Setting_Tool_Update_UseDownloadPatcher
        ComboBox_Tool_CheckForUpdates.SelectedIndex = Setting_Tool_CheckForUpdates
        ' Load mirrors and select current one
        For Each a In Application_Mirrors
            ComboBox_Tool_DownloadMirror.Items.Add(a.Value.DisplayName)
            If a.Key = 0 Then ComboBox_Tool_DownloadMirror.Items.Add(New Separator)
        Next
        ComboBox_Tool_DownloadMirror.SelectedIndex = Setting_Tool_DownloadMirror
        ComboBox_Tool_EnableNotifyIcon.SelectedIndex = Setting_Tool_EnableNotifyIcon
        ' Load languages and select current one
        Dim InsertedCodes As New List(Of String)
        Dim Counter As Integer = 0
        Dim IndexUserLanguage As Integer = -1
        Dim IndexEN As Integer = 0
        For Each a In Application_Languages
            If Not InsertedCodes.Contains(a.Value.Code) Then
                If a.Value.Code = "en_US" Then IndexEN = Counter
                If a.Key = Setting_Tool_Language Then IndexUserLanguage = Counter
                InsertedCodes.Add(a.Value.Code)
                ComboBox_Tool_Languages.Items.Add(a.Key & " | " & a.Value.DisplayName_English & "/" & a.Value.DisplayName)
                Counter += 1
            End If
        Next
        If Not IndexUserLanguage = -1 Then
            ComboBox_Tool_Languages.SelectedIndex = IndexUserLanguage
        Else
            ComboBox_Tool_Languages.SelectedIndex = IndexEN
        End If
        TextBox_Api_ApiKey.Text = Setting_Api_Key
        TextBox_osu_Path.Text = Setting_osu_Path
        TextBox_osu_SongsPath.Text = Setting_osu_SongsPath
        Textbox_Tool_Importer_AutoInstallCounter.Text = Setting_Tool_Importer_AutoInstallCounter.ToString
        TextBox_Tool_Interface_BeatmapDetailPanelWidth.Text = Setting_Tool_Interface_BeatmapDetailPanelWidth.ToString
        TextBox_Tool_Update_Path.Text = Setting_Tool_Update_SavePath
    End Sub
End Class