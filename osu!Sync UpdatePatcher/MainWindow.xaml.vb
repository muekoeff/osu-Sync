Imports System.ComponentModel, System.IO

Class MainWindow

    Private Argument_deletePackageAfter As Boolean = True
    Private Argument_destinationVersion, Argument_sourceVersion, Argument_pathToApp, _
        Argument_pathToUpdate, Argument_updateHash As String
    Private Updater_TempPath As String
    Private WithEvents Worker As New BackgroundWorker With { _
        .WorkerReportsProgress = True}

    Public Function MD5FileHash(ByVal sFile As String) As String
        Dim MD5 As New Security.Cryptography.MD5CryptoServiceProvider
        Dim Hash As Byte()
        Dim Result As String = ""
        Dim Tmp As String = ""

        Dim FN As New IO.FileStream(sFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, 8192)
        MD5.ComputeHash(FN)
        FN.Close()

        Hash = MD5.Hash
        Result = Strings.Replace(BitConverter.ToString(Hash), "-", "")
        Return Result
    End Function

    Private Sub Button_closeUpdater_Click(sender As Object, e As RoutedEventArgs) Handles Button_closeUpdater.Click
        Application.Current.Shutdown()
    End Sub

    Private Sub Button_startOsusync_Click(sender As Object, e As RoutedEventArgs) Handles Button_startOsusync.Click
        Process.Start(Argument_pathToApp & "\osu!Sync.exe")
        Application.Current.Shutdown()
    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        ' Delete Itself
        Dim DeleteItself As New ProcessStartInfo()
        DeleteItself.Arguments = "/C choice /C Y /N /D Y /T 3 & Del """ + System.Reflection.Assembly.GetExecutingAssembly().Location & """"
        DeleteItself.WindowStyle = ProcessWindowStyle.Hidden
        DeleteItself.CreateNoWindow = True
        DeleteItself.FileName = "cmd.exe"
        Process.Start(DeleteItself)
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Check if arguments valid
        If I__StartUpArguments Is Nothing Then
            MsgBox("Whoops, this programm is supposed to be run only by another application." & vbNewLine & "It can't be started by an user.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync UpdatePatcher")
            Application.Current.Shutdown()
            Exit Sub
        End If

        Me.Cursor = Cursors.AppStarting
        TextBlock_CurrentProcess.Text = "Preparing..."
        ProgressBar.Visibility = Windows.Visibility.Visible
        Worker.RunWorkerAsync()
    End Sub

    Private Sub Worker_DoWork(sender As Object, e As DoWorkEventArgs) Handles Worker.DoWork
        ' Check if osu!Sync is running
        Dim MainProcess() As Process = Process.GetProcessesByName("osu!Sync")
        If MainProcess.Length > 0 Then
            Worker.ReportProgress(Nothing, "Closing osu!Sync...")
            MainProcess(0).Kill()
            MainProcess(0).WaitForExit()
        End If

        ' Assign variables
        For Each Current In I__StartUpArguments
            Dim CurrentSplit() As String = Current.Split(CChar("="))
            CurrentSplit(1).Replace("\\", "\")      ' In order to correct a mistake from version 0.0.0.7
            Select Case CurrentSplit(0)
                Case "-deletePackageAfter"
                    Argument_deletePackageAfter = CType(CurrentSplit(1), Boolean)
                Case "-destinationVersion"
                    Argument_destinationVersion = CurrentSplit(1)
                Case "-pathToApp"
                    Argument_pathToApp = CurrentSplit(1)
                Case "-pathToUpdate"
                    Argument_pathToUpdate = CurrentSplit(1)
                Case "-sourceVersion"
                    Argument_sourceVersion = CurrentSplit(1)
                Case "-updateHash"
                    Argument_updateHash = CurrentSplit(1)
                Case Else
                    Worker.ReportProgress(Nothing, "Update failed!")
                    MsgBox("Whoops, one of the arguments seems to be invalid." & vbNewLine & "Update failed.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
                    Worker.ReportProgress(Nothing, "[CANCEL]")
                    Exit Sub
            End Select
        Next

        ' Set VersionInfo
        Worker.ReportProgress(Nothing, "[VERSIONINFO] Update from " & Argument_sourceVersion & " to " & Argument_destinationVersion)

        ' Check if files exist
        If Not IO.Directory.Exists(Argument_pathToApp) Then
            Worker.ReportProgress(Nothing, "Update failed!")
            MsgBox("The path to the application can't be found.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
            Worker.ReportProgress(Nothing, "[CANCEL]")
            Exit Sub
        ElseIf Not IO.File.Exists(Argument_pathToUpdate) Then
            Worker.ReportProgress(Nothing, "Update failed!")
            MsgBox("The path to the update package can't be found.", MsgBoxStyle.Exclamation, "Dialog | osu!Sync Software Update Patcher")
            Worker.ReportProgress(Nothing, "[CANCEL]")
            Exit Sub
        End If

        ' Verify update package
        Worker.ReportProgress(Nothing, "Verifying update package...")
        If Not MD5FileHash(Argument_pathToUpdate) = Argument_updateHash Then
            Worker.ReportProgress(Nothing, "[NOT VERIFIED]")
            If MessageBox.Show("This update package seems to be different from the original update package online." & vbNewLine & "Do you still want to proceed?", "Dialog | osu!Sync Software Update Patcher", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.No Then
                Worker.ReportProgress(Nothing, "[CANCEL]")
                Exit Sub
            End If
        Else
            Worker.ReportProgress(Nothing, "[VERIFIED]")
        End If

        ' Ask to clear folder
        If MessageBox.Show("Do you want to clear the folder where osu!Sync is installed?" & vbNewLine & "This isn't necessary but will clean up no longer needed files (if there're some). Don't do that if there're other files except osu!Sync's files inside this folder." & vbNewLine & vbNewLine & "// Path: " & Argument_pathToApp, "Dialog | osu!Sync Software Update Patcher", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
            For Each SelectedFile In Directory.GetFiles(Argument_pathToApp)
                Worker.ReportProgress(Nothing, "Clearing folder...")
                File.Delete(SelectedFile)
            Next
        End If

        ' Unzip update packag
        Worker.ReportProgress(Nothing, "Unzipping update package...")
        Dim sc As New Shell32.Shell()
        Dim output As Shell32.Folder = sc.NameSpace(Argument_pathToApp)
        Dim input As Shell32.Folder = sc.NameSpace(Argument_pathToUpdate)
        Worker.ReportProgress(Nothing, "Moving files...")
        output.CopyHere(input.Items, 4)

        ' Delete update package
        If Argument_deletePackageAfter Then
            Worker.ReportProgress(Nothing, "Deleting update package...")
            File.Delete(Argument_pathToUpdate)
        End If

        Worker.ReportProgress(Nothing, "Update successfully finished!")
    End Sub

    Private Sub Worker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles Worker.ProgressChanged
        If e.UserState.ToString().StartsWith("[VERSIONINFO] ") Then
            TextBlock_VersionInfo.Text = e.UserState.ToString().Substring("[VERSIONINFO] ".Length)
        ElseIf e.UserState.ToString() = "[VERIFIED]" Then
            TextBlock_VersionInfo.Text += " | Update package verified"
        ElseIf e.UserState.ToString() = "[NOT VERIFIED]" Then
            TextBlock_VersionInfo.Text += " | Update package unverified"
        ElseIf e.UserState.ToString() = "[CANCEL]" Then
            TextBlock_VersionInfo.Text = "Aborted!"
            Application.Current.Shutdown()
            Exit Sub
        Else
            TextBlock_CurrentProcess.Text = e.UserState.ToString()
        End If

        If e.UserState.ToString() = "Update successfully finished!" Then
            Me.Cursor = Cursors.Arrow
            Button_closeUpdater.Visibility = Windows.Visibility.Visible
            Button_startOsusync.Visibility = Windows.Visibility.Visible
            ProgressBar.Visibility = Windows.Visibility.Hidden
        End If
    End Sub
End Class
