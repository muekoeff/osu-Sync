Public Class Window_Welcome

    Private ShutdownAfterClose As Boolean = True

    Private Sub Bu_Continue_Click(sender As Object, e As RoutedEventArgs) Handles Bu_Continue.Click
        ShutdownAfterClose = False
        DirectCast(Windows.Application.Current.MainWindow, MainWindow).Activate()
        Close()
    End Sub

    Private Sub Window_Welcome_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        If ShutdownAfterClose Then Windows.Application.Current.Shutdown()
    End Sub
End Class
