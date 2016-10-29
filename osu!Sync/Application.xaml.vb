Class Application

    Private Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

#If Not DEBUG Then
    Private Sub Application_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        e.Handled = True
        MsgBox("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡" & vbNewLine & vbNewLine & "Sorry, it looks like an exception occured." & vbNewLine & "osu!Sync is going to shutdown now.", MsgBoxStyle.Critical, "Debug | " & AppName)
        Process.Start(CrashLogWrite(e.Exception))
        Try
            Current.Shutdown()
        Catch ex As Exception
        End Try
    End Sub
#End If

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' Save Startup Arguments
        If Not e.Args.Length = 0 Then AppStartArgs = e.Args
        If AppStartArgs Is Nothing Then
            FocusAndShutdown()
        Else
            With AppStartArgs
                If Not .Contains("--ignoreInstances") Then
                    FocusAndShutdown()
                End If
            End With
        End If

        ' Check if elevated
        Dim WinPri = New Security.Principal.WindowsPrincipal(Security.Principal.WindowsIdentity.GetCurrent())
        Tool_IsElevated = WinPri.IsInRole(Security.Principal.WindowsBuiltInRole.Administrator)

        ' Load language package
        If IO.Directory.Exists(Forms.Application.StartupPath & "\l10n") Then
            TranslationList = TranslationMap(Forms.Application.StartupPath & "\l10n")
            If TranslationList.ContainsKey(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_")) Then
                TranslationLoad(TranslationList.Item(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_")).Path)
            ElseIf TranslationList.ContainsKey(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)) _
                And Not Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2) = "en" Then     ' Don't load en_UD
                TranslationLoad(TranslationList.Item(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)).Path)
            End If
        End If
    End Sub

    Private Sub FocusAndShutdown()
        If Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Try
                Dim Pro As Process = Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).First
                AppActivate(Pro.Id)
                ShowWindow(Pro.MainWindowHandle, 1)
            Catch ex As ArgumentException
            End Try
            Current.Shutdown()
            Exit Sub
        End If
    End Sub
End Class
