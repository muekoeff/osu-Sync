Class Application

#If Not DEBUG Then
    Private Sub Application_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        e.Handled = True
        MsgBox("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡" & vbNewLine & vbNewLine & "Sorry, it looks like an exception occured." & vbNewLine & "osu!Sync is going to shutdown now.", MsgBoxStyle.Critical, "Debug | osu!Sync")
        Process.Start(WriteCrashLog(e.Exception))
        Try
            Current.Shutdown()
        Catch ex As Exception
        End Try
    End Sub
#End If

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' Save Startup Arguments
        Dim i As Integer = 0
        If Not e.Args.Length = 0 Then I__StartUpArguments = e.Args

        ' Check if already running
        If Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Current.Shutdown()
            Exit Sub
        End If
    End Sub
End Class
