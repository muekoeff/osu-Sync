Imports System.IO, System.Net
Imports Newtonsoft.Json

Public Class Window_Settings
    Private WithEvents Client As New WebClient

    Private Function CreateShortcut(ByVal sLinkFile As String, _
                                   ByVal sTargetFile As String, _
                                   Optional ByVal sArguments As String = "", _
                                   Optional ByVal sDescription As String = "", _
                                   Optional ByVal sWorkingDir As String = "") As Boolean

        'Quelle: http://www.vbarchiv.net/tipps/details.php?id=1601
        Try
            Dim oShell As New Shell32.Shell
            Dim oFolder As Shell32.Folder
            Dim oLink As Shell32.ShellLinkObject

            ' Ordner und Dateinamen extrahieren
            Dim sPath As String = sLinkFile.Substring(0, sLinkFile.LastIndexOf("\"))
            Dim sFile As String = sLinkFile.Substring(sLinkFile.LastIndexOf("\") + 1)

            ' Wichtig! Link-Datei erstellen (0 Bytes)
            Dim F As Short = CShort(FreeFile())
            FileOpen(F, sLinkFile, OpenMode.Output)
            FileClose(F)

            oFolder = oShell.NameSpace(sPath)
            oLink = CType(oFolder.Items.Item(sFile).GetLink, Shell32.ShellLinkObject)

            ' Eigenschaften der Verknüpfung
            With oLink
                If sArguments.Length > 0 Then .Arguments = sArguments
                If sDescription.Length > 0 Then .Description = sDescription
                If sWorkingDir.Length > 0 Then .WorkingDirectory = sWorkingDir
                .Path = sTargetFile

                ' Verknüpfung speichern
                .Save()
            End With

            ' Objekte zerstören
            oLink = Nothing
            oFolder = Nothing
            oShell = Nothing

            Return True

        Catch ex As Exception
            ' Fehler! ggf. Link-Datei löschen, falls bereit erstellt
            If File.Exists(sLinkFile) Then Kill(sLinkFile)
            Return False
        End Try
    End Function

    Function ValidateEmail(ByVal email As String) As Boolean
        Dim emailRegex As New System.Text.RegularExpressions.Regex(
            "^(?<user>[^@]+)@(?<host>.+)$")
        Dim emailMatch As System.Text.RegularExpressions.Match =
           emailRegex.Match(email)
        Return emailMatch.Success
    End Function

    Private Sub Action_ApplySettings()
        Setting_osu_Path = TextBox_osu_Path.Text
        Setting_osu_SongsPath = TextBox_osu_SongsPath.Text
        Setting_Tool_AutoLoadCacheOnStartup = CType(CheckBox_Tool_AutoLoadCacheOnStartup.IsChecked, Boolean)
        Setting_Tool_CheckFileAssociation = CType(CheckBox_Tool_CheckFileAssociation.IsChecked, Boolean)
        Setting_Tool_CheckForUpdates = ComboBox_Tool_CheckForUpdates.SelectedIndex
        Setting_Tool_DownloadMirror = ComboBox_Tool_DownloadMirror.SelectedIndex
        Setting_Tool_EnableNotifyIcon = ComboBox_Tool_EnableNotifyIcon.SelectedIndex
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_ImporterAutoInstallCounter.Text, Val) Then
            Setting_Tool_ImporterAutoInstallCounter = Val
        End If
        ' Load Language
        If Not ComboBox_Tool_Languages.Text = "" And Not Setting_Tool_Language = ComboBox_Tool_Languages.Text.Substring(0, 2) Then
            If Not GetTranslationName(ComboBox_Tool_Languages.Text.Substring(0, 2)) = "" Then
                LoadLanguage(GetTranslationName(ComboBox_Tool_Languages.Text.Substring(0, 2)))
                MsgBox(_e("WindowSettings_languageUpdated"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
            End If
        End If
        Setting_Tool_Language = ComboBox_Tool_Languages.Text.Substring(0, 2)
        Setting_Tool_UpdateSavePath = TextBox_Tool_UpdatePath.Text
        Setting_Tool_UpdateDeleteFileAfter = CType(CheckBox_Tool_UpdateDeleteFileAfter.IsChecked, Boolean)
        Setting_Tool_UpdateUseDownloadPatcher = CType(CheckBox_Tool_UpdateUseDownloadPatcher.IsChecked, Boolean)
        Setting_Messages_Sync_MoreThan1000Sets = CType(CheckBox_Messages_Sync_MoreThan1000Sets.IsChecked, Boolean)
        Setting_Messages_Updater_OpenUpdater = CType(CheckBox_Messages_Updater_OpenUpdater.IsChecked, Boolean)
        Setting_Messages_Updater_UnableToCheckForUpdates = CType(CheckBox_Messages_Updater_UnableToCheckForUpdates.IsChecked, Boolean)
        Action_SaveSettings()
    End Sub

    Private Sub Button_Apply_Click(sender As Object, e As RoutedEventArgs) Handles Button_Apply.Click
        Action_ApplySettings()
    End Sub

    Private Sub Button_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Button_Cancel.Click
        Me.Close()
    End Sub

    Private Sub Button_CreateShortcut_Click(sender As Object, e As RoutedEventArgs) Handles Button_CreateShortcut.Click
        If Not File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk") Then
            If CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\osu!Sync.lnk", _
                              System.Reflection.Assembly.GetExecutingAssembly().Location.ToString, "", _
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
        Me.Close()
    End Sub

    Private Sub Button_Feedback_Prepare_Click(sender As Object, e As RoutedEventArgs) Handles Button_Feedback_Prepare.Click
        Dim Content As New Dictionary(Of String, String)
        With Content
            .Add("downloadMirror", Setting_Tool_DownloadMirror.ToString)
            .Add("lastCheckForUpdates", Setting_Tool_LastCheckForUpdates)
            .Add("operatingSystem", Environment.OSVersion.Version.ToString)
            .Add("programVersion", My.Application.Info.Version.ToString)
            .Add("systemArchitecture_is64bit", CStr(Environment.Is64BitOperatingSystem))
            .Add("updateInterval", Setting_Tool_CheckForUpdates.ToString)
        End With
        TextBox_Feedback_FurtherInfo.Text = Newtonsoft.Json.JsonConvert.SerializeObject(Content)
        With Button_Feedback_Prepare
            .IsEnabled = False
            .Visibility = Windows.Visibility.Collapsed
        End With
        With StackPanel_Feedback
            .IsEnabled = True
            .Margin = New Thickness(0, 0, 0, 0)
            .Visibility = Windows.Visibility.Visible
        End With
    End Sub

    Private Sub Button_Feedback_Submit_Click(sender As Object, e As RoutedEventArgs) Handles Button_Feedback_Submit.Click
        Dim RichTextBox_Feedback_Message_TextRange As New TextRange(RichTextBox_Feedback_Message.Document.ContentStart, RichTextBox_Feedback_Message.Document.ContentEnd)

        If Not TextBox_Feedback_Username.Text.Length >= 5 Then
            MsgBox(_e("WindowSettings_yourNameIsTooShort"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf Not ValidateEmail(TextBox_Feedback_eMail.Text) Then
            MsgBox(_e("WindowSettings_yourEmailInvalid"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf ComboBox_Feedback_Category.SelectedIndex = -1 Then
            MsgBox(_e("WindowSettings_youHaveToSelectACategory"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf Not RichTextBox_Feedback_Message_TextRange.Text.Length >= 5 Then
            MsgBox(_e("WindowSettings_yourMessageSeemsToBeQuiteShort"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        Else
            Dim Message As New Dictionary(Of String, String)
            With Message
                .Add("username", TextBox_Feedback_Username.Text)
                .Add("email", TextBox_Feedback_eMail.Text)
                .Add("category", ComboBox_Feedback_Category.Text)
                .Add("message", RichTextBox_Feedback_Message_TextRange.Text)
                .Add("debugData", TextBox_Feedback_FurtherInfo.Text)
            End With
            With StackPanel_Feedback
                .IsEnabled = False
            End With
            Grid_Feedback_Overlay.Visibility = Windows.Visibility.Visible
            Client.DownloadStringAsync(New Uri("http://naseweis520.ml/osuSync/data/files/software/FeedbackReport.php?message=" & JsonConvert.SerializeObject(Message)))
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
                    System.Windows.Forms.Application.Restart()
                End If
                Application.Current.Shutdown()
            Else
                MsgBox(_e("WindowSettings_nopeNoConfig"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Private Sub Button_Tool_DeleteFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_DeleteFileAssociation.Click
        Dim RegisterError As Boolean = False
        Dim RegisterCounter As Integer = 0
        For Each Extension As String In FileExtensions
            If DeleteFileAssociation(Extension, FileExtensionsLong(RegisterCounter)) Then
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

    Private Sub Button_Tool_ImporterAutoInstallCounter_Down_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_ImporterAutoInstallCounter_Down.Click
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_ImporterAutoInstallCounter.Text, Val) Then
            Textbox_Tool_ImporterAutoInstallCounter.Text = CStr(Val - 1)
        Else
            Textbox_Tool_ImporterAutoInstallCounter.Text = "10"
        End If
    End Sub

    Private Sub Button_Tool_ImporterAutoInstallCounter_Up_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_ImporterAutoInstallCounter_Up.Click
        Dim Val As Integer
        If Integer.TryParse(Textbox_Tool_ImporterAutoInstallCounter.Text, Val) Then
            Textbox_Tool_ImporterAutoInstallCounter.Text = CStr(Val + 1)
        Else
            Textbox_Tool_ImporterAutoInstallCounter.Text = "10"
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
        If MessageBox.Show(_e("WindowSettings_areYouSureYouWantToResetOsuSync"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            Dim RegisterError As Boolean = False
            Dim RegisterCounter As Integer = 0
            For Each Extension As String In FileExtensions
                If DeleteFileAssociation(Extension, FileExtensionsLong(RegisterCounter)) Then
                    RegisterCounter += 1
                Else
                    RegisterError = True
                    Exit For
                End If
            Next

            If RegisterError Then
                MsgBox(_e("MainWindow_extensionDeleteFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
            If Directory.Exists(I__Path_Programm) Then
                Try
                    Directory.Delete(I__Path_Programm, True)
                Catch ex As IOException
                End Try
            End If
            If Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync") Then
                Try
                    Directory.Delete(Path.GetTempPath() & "naseweis520\osu!Sync", True)
                Catch ex As IOException
                End Try
            End If
            If MessageBox.Show(_e("WindowSettings_okDoneDoYouWantToRestart"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                System.Windows.Forms.Application.Restart()
            End If
            Application.Current.Shutdown()
        End If
    End Sub

    Private Sub Button_Tool_UpdateFileAssociation_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_UpdateFileAssociation.Click
        Dim RegisterError As Boolean = False
        Dim RegisterCounter As Integer = 0
        For Each Extension As String In FileExtensions
            If CreateFileAssociation(Extension, _
                                                     FileExtensionsLong(RegisterCounter), _
                                                     FileExtensionsDescription(RegisterCounter), _
                                                     FileExtensionsIcon(RegisterCounter), _
                                                     System.Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
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

    Private Sub Button_Tool_UpdatePathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_UpdatePathDefault.Click
        TextBox_Tool_UpdatePath.Text = Path.GetTempPath() & "naseweis520\osu!Sync\Updater"
    End Sub

    Private Sub Client_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs) Handles Client.DownloadStringCompleted
        Try
            MsgBox(_e("WindowSettings_serverSideAnswer") & vbNewLine & e.Result, MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
        Catch ex As System.Reflection.TargetInvocationException
            MsgBox(_e("WindowSettings_unableToSubmitFeedback") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("WindowSettings_pleaseTryAgainLaterOrContactUs"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            Exit Sub
        End Try
        Grid_Feedback_Overlay.Visibility = Windows.Visibility.Collapsed
    End Sub

    Private Sub TextBox_osu_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_Path.GotFocus
        Dim SelectFile As New Forms.OpenFileDialog With { _
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "exe",
            .FileName = "osu!",
            .Filter = _e("WindowSettings_executableFiles") & " (*.exe)|*.exe",
            .InitialDirectory = GetDetectedOsuPath(),
            .Multiselect = False,
            .Title = _e("WindowSettings_pleaseOpenOsu")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            If IO.Path.GetFileName(SelectFile.FileName) = "osu!.exe" Then
                TextBox_osu_Path.Text = IO.Path.GetDirectoryName(SelectFile.FileName)
            Else
                MsgBox(_e("WindowSettings_youSelectedTheWrongFile"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Private Sub TextBox_osu_SongsPath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_SongsPath.GotFocus
        Dim SelectFile As New Forms.FolderBrowserDialog With {
            .Description = _e("WindowSettings_pleaseSelectSongsFolder")}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            TextBox_osu_SongsPath.Text = SelectFile.SelectedPath
        End If
    End Sub

    Private Sub TextBox_Tool_UpdatePath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_Tool_UpdatePath.GotFocus
        Dim SelectDirectory As New Forms.FolderBrowserDialog With { _
            .Description = _e("WindowSettings_pleaseSelectDirectoryWhereToSaveUpdates"),
            .ShowNewFolderButton = False}
        If IO.Directory.Exists(Setting_Tool_UpdateSavePath) Then
            SelectDirectory.SelectedPath = Setting_Tool_UpdateSavePath
        Else
            SelectDirectory.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        End If

        If Not SelectDirectory.ShowDialog() = Forms.DialogResult.Cancel Then
            TextBox_Tool_UpdatePath.Text = SelectDirectory.SelectedPath
        End If
    End Sub

    Private Sub Window_Settings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CheckBox_Messages_Sync_MoreThan1000Sets.IsChecked = Setting_Messages_Sync_MoreThan1000Sets
        CheckBox_Messages_Updater_OpenUpdater.IsChecked = Setting_Messages_Updater_OpenUpdater
        CheckBox_Messages_Updater_UnableToCheckForUpdates.IsChecked = Setting_Messages_Updater_UnableToCheckForUpdates
        CheckBox_Tool_AutoLoadCacheOnStartup.IsChecked = Setting_Tool_AutoLoadCacheOnStartup
        CheckBox_Tool_CheckFileAssociation.IsChecked = Setting_Tool_CheckFileAssociation
        CheckBox_Tool_UpdateDeleteFileAfter.IsChecked = Setting_Tool_UpdateDeleteFileAfter
        CheckBox_Tool_UpdateUseDownloadPatcher.IsChecked = Setting_Tool_UpdateUseDownloadPatcher
        ComboBox_Tool_CheckForUpdates.SelectedIndex = Setting_Tool_CheckForUpdates
        ComboBox_Tool_DownloadMirror.SelectedIndex = Setting_Tool_DownloadMirror
        ComboBox_Tool_EnableNotifyIcon.SelectedIndex = Setting_Tool_EnableNotifyIcon
        ' Select Language
        Dim Counter As Integer = 0
        Dim IndexEN As Integer = 0
        For Each Item As ComboBoxItem In ComboBox_Tool_Languages.Items
            If Item.Content.ToString.Substring(0, 2) = Setting_Tool_Language Then
                ComboBox_Tool_Languages.SelectedIndex = Counter
                Counter = -1
                Exit For
            ElseIf Item.Content.ToString.Substring(0, 2) = "en" Then
                IndexEN = Counter
            End If
            Counter += 1
        Next
        If Not Counter = -1 Then
            ComboBox_Tool_Languages.SelectedIndex = IndexEN
        End If
        TextBox_osu_Path.Text = Setting_osu_Path
        TextBox_osu_SongsPath.Text = Setting_osu_SongsPath
        Textbox_Tool_ImporterAutoInstallCounter.Text = Setting_Tool_ImporterAutoInstallCounter.ToString
        TextBox_Tool_UpdatePath.Text = Setting_Tool_UpdateSavePath
    End Sub
End Class
