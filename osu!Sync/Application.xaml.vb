Class Application

    Private Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

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

        If I__StartUpArguments Is Nothing Then
            FocusAndShutdown()
        Else
            With I__StartUpArguments
                If Not .Contains("--ignoreInstances") Then
                    FocusAndShutdown()
                End If
            End With
        End If

        ' Check if elevated
        Dim principal = New Security.Principal.WindowsPrincipal(Security.Principal.WindowsIdentity.GetCurrent())
        Tool_IsElevated = principal.IsInRole(Security.Principal.WindowsBuiltInRole.Administrator)

        ' Load language library
        Action_PrepareData()
        If Not GetTranslationName(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_")) = "" Then     ' Check if full language code exists (e.g. de_DE)
            LoadLanguage(GetTranslationName(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_")), Globalization.CultureInfo.CurrentCulture.ToString())
        ElseIf Not GetTranslationName(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)) = "" Then ' Check if main language code exists (e.g. de)
            LoadLanguage(GetTranslationName(Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)), Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2))
        End If
    End Sub

    Private Sub FocusAndShutdown()
        If Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Try
                Dim SelectedProcess As Process = Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).First
                AppActivate(SelectedProcess.Id)
                ShowWindow(SelectedProcess.MainWindowHandle, 1)
            Catch ex As ArgumentException
            End Try

            Current.Shutdown()
            Exit Sub
        End If
    End Sub
End Class
