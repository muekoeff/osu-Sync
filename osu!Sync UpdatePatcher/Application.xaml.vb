Class Application

    Private Sub Application_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        e.Handled = True
        Clipboard.SetDataObject(e.Exception.ToString)
        MsgBox("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡" & vbNewLine & vbNewLine & "Sorry, it looks like an exception occured." & vbNewLine & "osu!Sync is goint to shutdown now.", MsgBoxStyle.Critical, "Debug | osu!Sync Software Update Patcher")
        MsgBox("This message has been copied to your clipboard, if the problem persists please report it:" & vbNewLine & vbNewLine & e.Exception.ToString, MsgBoxStyle.OkOnly, "Debug | osu!Sync Software Update Patcher")
        Try
            Application.Current.Shutdown()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' Save Startup Arguments
        Dim i As Integer = 0
        If Not e.Args.Length = 0 Then
            I__StartUpArguments = e.Args
        End If

        ' Check if already running
        If Diagnostics.Process.GetProcessesByName(Diagnostics.Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Application.Current.Shutdown()
            Exit Sub
        End If
    End Sub
End Class
