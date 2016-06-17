Imports System.Drawing

Public Class Window_GenericMsgBox

    Public Class MsgBoxButtonHolder
        Property Action As ButtonAction
        Property Label As String
        Property ResultId As Integer

        Public Sub New(_label As String, _resultId As Integer, Optional _action As ButtonAction = ButtonAction.None)
            Label = _label
            ResultId = _resultId
            Action = _action
        End Sub

        Public Sub New(_coreButton As MsgBoxResult)
            Select Case _coreButton
                Case MsgBoxResult.OK
                    Label = _e("MainWindow_buttons_ok")
                    ResultId = MsgBoxResult.OK
                    Action = ButtonAction.OK
                Case MsgBoxResult.Cancel
                    Label = _e("MainWindow_buttons_cancel")
                    ResultId = MsgBoxResult.Cancel
                    Action = ButtonAction.Cancel
                Case MsgBoxResult.Yes
                    Label = _e("MainWindow_buttons_yes")
                    ResultId = MsgBoxResult.Yes
                    Action = ButtonAction.OK
                Case MsgBoxResult.YesAll
                    Label = _e("MainWindow_buttons_yesForAll")
                    ResultId = MsgBoxResult.YesAll
                Case MsgBoxResult.No
                    Label = _e("MainWindow_buttons_no")
                    ResultId = MsgBoxResult.No
                    Action = ButtonAction.Cancel
                Case MsgBoxResult.NoAll
                    Label = _e("MainWindow_buttons_noForAll")
                    ResultId = MsgBoxResult.NoAll
            End Select

        End Sub
    End Class

    Public Enum ButtonAction
        None = 0
        OK = 1
        Cancel = 2
    End Enum

    Public Enum MsgBoxResult
        None = 0
        OK = 1
        Cancel = 2
        Yes = 6
        YesAll = 61
        No = 7
        NoAll = 71
    End Enum

    Public Enum SimpleMsgBoxButton
        Ok
        OkCancel
    End Enum

    WriteOnly Property Buttons As List(Of MsgBoxButtonHolder)
        Set(value As List(Of MsgBoxButtonHolder))
            SP_ButtonWrapper.Children.Clear()
            For Each i As MsgBoxButtonHolder In value
                Dim NewButton As New Button
                With NewButton
                    .Content = i.Label
                    .Margin = New Thickness(5, 0, 0, 0)
                    .Padding = New Thickness(10, 10, 10, 10)
                    .Tag = i.ResultId
                End With
                If i.Action = ButtonAction.Cancel Then
                    NewButton.IsCancel = True
                ElseIf i.Action = ButtonAction.OK Then
                    NewButton.IsDefault = True
                End If
                AddHandler NewButton.Click, AddressOf Bu_Click
                SP_ButtonWrapper.Children.Add(NewButton)
            Next
        End Set
    End Property
    Property Caption As String
        Get
            Return Me.Title
        End Get
        Set(value As String)
            If value Is Nothing Then Title = My.Application.Info.Title Else Title = value
        End Set
    End Property
    WriteOnly Property MessageBoxIcon As Icon
        Set(value As Icon)
            If value Is Nothing Then
                CD_Icon.Width = New GridLength(0)   ' Hide Icon column
            Else
                CD_Icon.Width = New GridLength(55)   ' Show Icon column
                Im_Icon.Source = Interop.Imaging.CreateBitmapSourceFromHBitmap(
                value.ToBitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions())
            End If
        End Set
    End Property
    Property MessageBoxText As String
        Get
            Return TB_Text.Text
        End Get
        Set(value As String)
            TB_Text.Text = value
        End Set
    End Property
    Property Result As Integer = 0
    WriteOnly Property SimpleButton As SimpleMsgBoxButton
        Set(value As SimpleMsgBoxButton)
            Select Case value
                Case SimpleMsgBoxButton.Ok
                    Buttons = New List(Of MsgBoxButtonHolder) From {
                        New MsgBoxButtonHolder(_e("MainWindow_buttons_ok"), MessageBoxResult.OK)
                    }
                Case SimpleMsgBoxButton.OkCancel
                    Buttons = New List(Of MsgBoxButtonHolder) From {
                        New MsgBoxButtonHolder(_e("MainWindow_buttons_ok"), MessageBoxResult.OK),
                        New MsgBoxButtonHolder(_e("MainWindow_buttons_cancel"), MessageBoxResult.Cancel)
                    }
            End Select
        End Set
    End Property

    Public Sub New(_messageBoxText As String, Optional _simpleButton As SimpleMsgBoxButton = SimpleMsgBoxButton.Ok, Optional _caption As String = Nothing, Optional _messageBoxIcon As Icon = Nothing)
        InitializeComponent()
        MessageBoxText = _messageBoxText
        SimpleButton = _simpleButton
        Caption = _caption
        MessageBoxIcon = _messageBoxIcon
    End Sub

    Public Sub New(_messageBoxText As String, _buttons As List(Of MsgBoxButtonHolder), Optional _caption As String = Nothing, Optional _messageBoxIcon As Icon = Nothing)
        InitializeComponent()
        MessageBoxText = _messageBoxText
        Buttons = _buttons
        Caption = _caption
        MessageBoxIcon = _messageBoxIcon
    End Sub

    Private Sub Bu_Click(sender As Object, e As RoutedEventArgs)
        Result = CInt(CType(sender, Button).Tag)
        Close()
    End Sub

    Private Sub Window_GenericMsgBox_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        With TB_Text
            .MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75
            .MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75
        End With
    End Sub
End Class
