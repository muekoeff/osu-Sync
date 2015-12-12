Public Class Window_Welcome

    Private ShutdownAfterClose As Boolean = True

    Private Sub Button_SkipLogin_Click(sender As Object, e As RoutedEventArgs) Handles Button_SkipLogin.Click
        ShutdownAfterClose = False
        DirectCast(Windows.Application.Current.MainWindow, MainWindow).Activate()
        Close()
    End Sub

    Private Sub Window_Welcome_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        If ShutdownAfterClose Then Windows.Application.Current.Shutdown()
    End Sub
End Class
