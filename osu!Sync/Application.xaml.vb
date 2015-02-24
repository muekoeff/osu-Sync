Class Application

    Private Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

    Private Sub Application_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        'e.Handled = True
        'Clipboard.SetDataObject(e.Exception.ToString)
        'MsgBox("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡" & vbNewLine & vbNewLine & "Sorry, it looks like an exception occured." & vbNewLine & "osu!Sync is going to shutdown now.", MsgBoxStyle.Critical, "Debug | osu!Sync")
        'MsgBox("This message has been copied to your clipboard, if the problem persists please report it:" & vbNewLine & vbNewLine & e.Exception.ToString, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
        'Try
        '    Application.Current.Shutdown()
        'Catch ex As Exception
        'End Try
    End Sub

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' Save Startup Arguments
        Dim i As Integer = 0
        If Not e.Args.Length = 0 Then
            I__StartUpArguments = e.Args
        End If

        ' Check if already running
        If Diagnostics.Process.GetProcessesByName(Diagnostics.Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Dim SelectedProcess As Process = Process.GetProcessesByName(Diagnostics.Process.GetCurrentProcess.ProcessName).First
            AppActivate(SelectedProcess.Id)
            ShowWindow(SelectedProcess.MainWindowHandle, 1)
            Application.Current.Shutdown()
            Exit Sub
        End If
    End Sub
End Class
