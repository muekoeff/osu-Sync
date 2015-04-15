Imports System.IO

Public Class Window_Settings

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
                              "Launch osu!Sync.") Then
            Else
                MsgBox("Unable to create shortcut!", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
        Else
            MsgBox("It looks like there's already a shortcut on your desktop.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Done_Click(sender As Object, e As RoutedEventArgs) Handles Button_Done.Click
        Action_ApplySettings()
        Me.Close()
    End Sub

    Private Sub Button_Feedback_Prepare_Click(sender As Object, e As RoutedEventArgs) Handles Button_Feedback_Prepare.Click
        ' TODO: Check if server is available.
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
            MsgBox("Your username seems to be quite short.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf Not ValidateEmail(TextBox_Feedback_eMail.Text) Then
            MsgBox("The selected email seems to be invalid.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf ComboBox_Feedback_Category.SelectedIndex = -1 Then
            MsgBox("You need to select a category.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        ElseIf Not RichTextBox_Feedback_Message_TextRange.Text.Length >= 5 Then
            MsgBox("Your message seems to be quite short. Please try to explain as detailed as possible.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        Else
            ' TODO: Submit form to server.
        End If
    End Sub

    Private Sub Button_osu_SongPathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Button_osu_SongPathDefault.Click
        TextBox_osu_SongsPath.Text = TextBox_osu_Path.Text & "\Songs"
    End Sub

    Private Sub Button_Tool_DeleteConfiguration_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_DeleteConfiguration.Click
        If MessageBox.Show("Are you really sure that you want to delete the configuration file (this cannot be undone)?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
            If File.Exists(I__Path_Programm & "\Settings\Settings.config") Then
                File.Delete(I__Path_Programm & "\Settings\Settings.config")

                If MessageBox.Show("Ok, that's done." & vbNewLine & "Do you want to restart osu!Sync now?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                    System.Windows.Forms.Application.Restart()
                End If
                Application.Current.Shutdown()
            Else
                MsgBox("Nope, there is no configuration file...", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
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
            MsgBox("File association successfully deleted, :/.", MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
        Else
            MsgBox("Unable to delete file association.", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_OpenDataFolder_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_OpenDataFolder.Click
        If Directory.Exists(I__Path_Programm) Then
            Process.Start(I__Path_Programm)
        Else
            MsgBox("Nope, this directory doesn't exist... yet.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_Reset_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_Reset.Click
        If MessageBox.Show("Are you really sure that you want to reset osu!Sync (this cannot be undone)?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes Then
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
                MsgBox("Unable to delete file association.", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
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
            If MessageBox.Show("Ok, that's done." & vbNewLine & "Do you want to restart osu!Sync now?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
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
            MsgBox("File association successfully registered, thank you :).", MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
        Else
            MsgBox("Unable to register file association.", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Private Sub Button_Tool_UpdatePathDefault_Click(sender As Object, e As RoutedEventArgs) Handles Button_Tool_UpdatePathDefault.Click
        TextBox_Tool_UpdatePath.Text = Path.GetTempPath() & "naseweis520\osu!Sync\Updater"
    End Sub

    Private Sub TextBox_osu_Path_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_Path.GotFocus
        Dim SelectFile As New Forms.OpenFileDialog With { _
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "exe",
            .FileName = "osu!",
            .Filter = "Executable Files (*.exe)|*.exe",
            .InitialDirectory = GetDetectedOsuPath(),
            .Multiselect = False,
            .Title = "Please open the osu!.exe"}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            If IO.Path.GetFileName(SelectFile.FileName) = "osu!.exe" Then
                TextBox_osu_Path.Text = IO.Path.GetDirectoryName(SelectFile.FileName)
            Else
                MsgBox("You selected the wrong file." & vbNewLine & "Please select the ""osu!.exe"".", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Private Sub TextBox_osu_SongsPath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_osu_SongsPath.GotFocus
        Dim SelectFile As New Forms.FolderBrowserDialog With {
            .Description = "Please select your songs folder."}

        If Not SelectFile.ShowDialog() = Forms.DialogResult.Cancel Then
            TextBox_osu_SongsPath.Text = SelectFile.SelectedPath
        End If
    End Sub

    Private Sub TextBox_Tool_UpdatePath_GotFocus(sender As Object, e As RoutedEventArgs) Handles TextBox_Tool_UpdatePath.GotFocus
        Dim SelectDirectory As New Forms.FolderBrowserDialog With { _
            .Description = "Please select the directory where to save updates.",
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
        TextBox_osu_Path.Text = Setting_osu_Path
        TextBox_osu_SongsPath.Text = Setting_osu_SongsPath
        TextBox_Tool_UpdatePath.Text = Setting_Tool_UpdateSavePath
    End Sub
End Class
