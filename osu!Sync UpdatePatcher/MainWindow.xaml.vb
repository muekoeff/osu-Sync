Imports Ionic.Zip
Imports System.ComponentModel, System.IO

Class MainWindow

    Private Argument_deletePackageAfter As Boolean = True
    Private Argument_destinationVersion, Argument_sourceVersion, Argument_pathToApp,
        Argument_pathToUpdate As String
    Private KillAfterClose As Integer = 0
    Private Updater_ZipCount, Updater_ZipCurrentCount As Integer
    Private WithEvents Worker As New BackgroundWorker With {
        .WorkerReportsProgress = True}

    Private Sub Action_ZipProgress(ByVal sender As Object, e As ExtractProgressEventArgs)
        If Not e.TotalBytesToTransfer = 0 Then
            Dim Percentage As Integer = CInt(e.BytesTransferred / e.TotalBytesToTransfer * 100)
            With Worker
                .ReportProgress(Nothing, "Unzipping... | " & Percentage & " %")
                .ReportProgress(Nothing, "[PROGRESSBAR] " & (Updater_ZipCount * 100) & ";" & (Updater_ZipCurrentCount * 100 + Percentage))
            End With
        End If
    End Sub

    Private Sub Button_closeUpdater_Click(sender As Object, e As RoutedEventArgs) Handles Button_closeUpdater.Click
        Windows.Application.Current.Shutdown()
    End Sub

    Private Sub Button_startOsusync_Click(sender As Object, e As RoutedEventArgs) Handles Button_startOsusync.Click
        Process.Start(Argument_pathToApp & "\osu!Sync.exe")
        Windows.Application.Current.Shutdown()
    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Select Case KillAfterClose
            Case 1  ' Delete Itself
                Dim DeleteItself As New ProcessStartInfo() With {
                    .Arguments = "/C choice /C Y /N /D Y /T 3 & Del """ + Reflection.Assembly.GetExecutingAssembly().Location & """",
                    .CreateNoWindow = True,
                    .FileName = "cmd.exe",
                    .WindowStyle = ProcessWindowStyle.Hidden
                }
                Process.Start(DeleteItself)
            Case 2  ' Open folder
                Dim DoProcess As New ProcessStartInfo()
                With DoProcess
                    .Arguments = "/select," + Reflection.Assembly.GetExecutingAssembly().Location
                    .FileName = "Explorer.exe"
                End With
                Process.Start(DoProcess)
        End Select
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Cursor = Cursors.AppStarting
        TextBlock_CurrentProcess.Text = "Preparing..."
        ProgressBar.Visibility = Visibility.Visible

        ' Assign variables
        Worker.ReportProgress(Nothing, "Reading arguments...")
        If I__StartUpArguments Is Nothing Then  ' Check whether start up arguments are set (program has been run by osu!Sync) or not (ran by user)
            Dim Chooser_pathToApp As New Forms.FolderBrowserDialog() With {
                .Description = "Please specify the path the the root folder of osu!Sync (pathToApp)"
            }
            Dim Chooser_pathToUpdate As New Forms.OpenFileDialog() With {
                .Filter = "Update Packages (*.zip)|*.zip|All files (*.*)|*.*",
                .Title = "Please specify the path to the update package (pathToUpdate)"
            }
            Argument_deletePackageAfter = False
            Argument_destinationVersion = "another manually chosen version"
            If Chooser_pathToApp.ShowDialog() = Forms.DialogResult.OK Then
                Argument_pathToApp = Chooser_pathToApp.SelectedPath
            Else
                Windows.Application.Current.Shutdown()
                Exit Sub
            End If
            If Chooser_pathToUpdate.ShowDialog() = Forms.DialogResult.OK Then
                Argument_pathToUpdate = Chooser_pathToUpdate.FileName
            Else
                Windows.Application.Current.Shutdown()
                Exit Sub
            End If
            Argument_sourceVersion = "a manually chosen version"
        Else
            For Each Current In I__StartUpArguments
                Dim CurrentSplit() As String = Current.Split(CChar("="))
                Select Case CurrentSplit(0)
                    Case "-deletePackageAfter"
                        Argument_deletePackageAfter = CBool(CurrentSplit(1))
                    Case "-destinationVersion"
                        Argument_destinationVersion = CurrentSplit(1)
                    Case "-pathToApp"
                        Argument_pathToApp = CurrentSplit(1)
                    Case "-pathToUpdate"
                        Argument_pathToUpdate = CurrentSplit(1)
                    Case "-sourceVersion"
                        Argument_sourceVersion = CurrentSplit(1)
                    Case Else
                        Worker.ReportProgress(Nothing, "Update failed!")
                        MsgBox("Whoops, one of the arguments seems to be invalid." & vbNewLine & "Update failed.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
                        Worker.ReportProgress(Nothing, "[CANCEL]")
                        Exit Sub
                End Select
            Next
        End If

        Worker.RunWorkerAsync()
    End Sub

    Private Sub StackPanel_Paths_MouseEnter(sender As Object, e As MouseEventArgs) Handles StackPanel_Paths.MouseEnter
        TextBlock_Paths.Visibility = Visibility.Collapsed
        TextBlock_PathsFull.Visibility = Visibility.Visible
    End Sub

    Private Sub StackPanel_Paths_MouseLeave(sender As Object, e As MouseEventArgs) Handles StackPanel_Paths.MouseLeave
        TextBlock_Paths.Visibility = Visibility.Visible
        TextBlock_PathsFull.Visibility = Visibility.Collapsed
    End Sub

    Private Sub Worker_DoWork(sender As Object, e As DoWorkEventArgs) Handles Worker.DoWork
        Try
            ' Check if osu!Sync is running
            Worker.ReportProgress(Nothing, "Checking, if osu!Sync is running...")
            Dim MainProcess() As Process = Process.GetProcessesByName("osu!Sync")
            If MainProcess.Length > 0 Then
                Worker.ReportProgress(Nothing, "Killing osu!Sync...")
                MainProcess(0).Kill()
                MainProcess(0).WaitForExit()
            End If

            ' Set VersionInfo
            Worker.ReportProgress(Nothing, "[VERSIONINFO] Update from " & Argument_sourceVersion & " to " & Argument_destinationVersion)
            Worker.ReportProgress(Nothing, "[PATHS] " & Argument_pathToUpdate & ";" & Argument_pathToApp)

            ' Check if files exist
            Worker.ReportProgress(Nothing, "Checking...")
            If Not Directory.Exists(Argument_pathToApp) Then
                Worker.ReportProgress(Nothing, "Update failed!")
                MsgBox("The path to the application can't be found.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
                Worker.ReportProgress(Nothing, "[CANCEL]")
                Exit Sub
            ElseIf Not File.Exists(Argument_pathToUpdate) Then
                Worker.ReportProgress(Nothing, "Update failed!")
                MsgBox("The path to the update package can't be found.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
                Worker.ReportProgress(Nothing, "[CANCEL]")
                Exit Sub
            End If

            ' Unzip
            Worker.ReportProgress(Nothing, "Unzipping...")
            Using Zipper As ZipFile = ZipFile.Read(Argument_pathToUpdate)
                AddHandler Zipper.ExtractProgress, AddressOf Action_ZipProgress
                Updater_ZipCount = Zipper.Count
                For Each ZipperEntry As ZipEntry In Zipper
                    ZipperEntry.Extract(Argument_pathToApp, ExtractExistingFileAction.OverwriteSilently)
                    Updater_ZipCurrentCount += 1
                Next
            End Using

            ' Delete update package
            If Argument_deletePackageAfter Then
                Worker.ReportProgress(Nothing, "Deleting update package...")
                File.Delete(Argument_pathToUpdate)
            End If

            Worker.ReportProgress(Nothing, "[FINISHED]")
        Catch ex As Exception
            Worker.ReportProgress(Nothing, "[FAILED] " + ex.Message)
        End Try
    End Sub

    Private Sub Worker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles Worker.ProgressChanged
        If e.UserState.ToString().StartsWith("[VERSIONINFO] ") Then
            TextBlock_VersionInfo.Text = e.UserState.ToString().Substring("[VERSIONINFO] ".Length)
        ElseIf e.UserState.ToString().StartsWith("[PATHS] ") Then
            Dim Text() = e.UserState.ToString().Substring("[VERSIONINFO] ".Length).Split(CChar(";"))
            TextBlock_Paths.Text = Text(1)
            With TextBlock_PathsFull
                .Text = "Update Package: " & Text(0) & vbNewLine &
                    "osu!Sync: " & Text(1)
            End With
        ElseIf e.UserState.ToString().StartsWith("[PROGRESSBAR] ") Then
            Dim Text() = e.UserState.ToString().Substring("[VERSIONINFO] ".Length).Split(CChar(";"))
            With ProgressBar
                .IsIndeterminate = False
                .Maximum = CDbl(Text(0))
                .Value = CDbl(Text(1))
            End With
        ElseIf e.UserState.ToString().StartsWith("[FAILED] ") Then
            Dim Text = e.UserState.ToString().Substring("[FAILED] ".Length)
            KillAfterClose = 2
            TextBlock_CurrentProcess.Text = "Update failed!"
            Cursor = Cursors.Arrow
            Button_closeUpdater.Visibility = Visibility.Visible
            ProgressBar.Visibility = Visibility.Hidden
            MsgBox("An error occured!" & vbNewLine & vbNewLine & "// Message:" & vbNewLine & Text, MsgBoxStyle.Critical, "Debug | osu!Sync")
        ElseIf e.UserState.ToString() = "[CANCEL]" Then
            KillAfterClose = 2
            Button_closeUpdater.Visibility = Visibility.Visible
            With Button_startOsusync
                .IsEnabled = False
                .Visibility = Visibility.Visible
            End With
            ProgressBar.Visibility = Visibility.Hidden
            TextBlock_CurrentProcess.Text = "Aborted!"
        ElseIf e.UserState.ToString() = "[FINISHED]" Then
            KillAfterClose = 1
            TextBlock_CurrentProcess.Text = "Update successfully finished!"
            Cursor = Cursors.Arrow
            Button_closeUpdater.Visibility = Visibility.Visible
            Button_startOsusync.Visibility = Visibility.Visible
            ProgressBar.Visibility = Visibility.Hidden
        Else
            TextBlock_CurrentProcess.Text = e.UserState.ToString()
        End If
    End Sub
End Class
