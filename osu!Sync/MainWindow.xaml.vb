Imports Hardcodet.Wpf.TaskbarNotification
Imports Ionic.Zip
Imports System.IO
Imports System.Net
Imports System.Windows.Media.Animation
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Enum NotifyNextAction
    None = 0
    OpenUpdater = 1
End Enum

Public Enum UpdateBmDisplayDestinations
    Installed = 0
    Importer = 1
    Exporter = 2
End Enum

Public Class Beatmap
    Enum OnlineApprovedStatuses
        Graveyard = -2
        WIP = -1
        Pending = 0
        Ranked = 1
        Approved = 2
        Qualified = 3
    End Enum

    Public Property Artist As String = ""
    Public Property Creator As String = "Unknown"
    Public Property ID As Integer
    Public Property IsUnplayed As Boolean = True
    Public Property MD5 As String
    Public Property RankedStatus As Byte = Convert.ToByte(1)
    Public Property SongSource As String = ""
    Public Property SongTags As String = ""
    Public Property Title As String
End Class

Public Class BmDPDetails
    Public Property Artist As String = "Unknown"
    Public Property Creator As String = "Unknown"
    Public Property IsUnplayed As Boolean = True
    Public Property RankedStatus As Byte = Convert.ToByte(0)
    Public Property SongSource As String = "Unknown"
    Public Property Title As String
End Class

Public Class BGWcallback_SyncGetIDs

    Enum ArgModes
        Sync = 0
    End Enum

    Enum ProgressCurrentActions
        Sync = 0
        Done = 2
        CountingTotalFolders = 4
    End Enum

    Enum ReturnStatuses
        Cancelled
        Exception
        FolderDoesNotExist
        Success
    End Enum

    Property Arg__Mode As ArgModes
    Property Func_Invalid As List(Of String)
    Property Func_InvalidId As List(Of String)
    Property Progress__Current As Integer
    Property Progress__CurrentAction As ProgressCurrentActions
    Property Return__Status As ReturnStatuses = ReturnStatuses.Success
    Property Return__Sync_BmDic_Installed As New Dictionary(Of Integer, Beatmap)
    Property Return__Sync_Warnings As String
End Class

Public Class Importer
    Public Class TagData
        Property Beatmap As Beatmap
        Property IsInstalled As Boolean
        Property UI_Checkbox_IsSelected As CheckBox
        Property UI_DecoBorderLeft As Rectangle
        Property UI_Grid As Grid
        Property UI_TextBlock_Title As TextBlock
        Property UI_TextBlock_Caption As TextBlock
        Property UI_Thumbnail As Image
    End Class

    Public BmList_TagsDone As New List(Of TagData)
    Public BmList_TagsFailed As New List(Of TagData)
    Public BmList_TagsLeftOut As New List(Of TagData)
    Public BmList_TagsToInstall As New List(Of TagData)
    Public BmTotal As Integer
    Public Counter As Integer
    Public CurrentFileName As String
    Public Downloader As New WebClient
    Public FilePath As String
    Public Pref_FetchFail_SkipAlways As Boolean = False
End Class

Public Class StandardColors
    Public Shared BlueLight As Brush = DirectCast(New BrushConverter().ConvertFrom("#3498DB"), Brush)
    Public Shared GrayDark As Brush = DirectCast(New BrushConverter().ConvertFrom("#555555"), Brush)
    Public Shared GrayLight As Brush = DirectCast(New BrushConverter().ConvertFrom("#999999"), Brush)
    Public Shared GrayLighter As Brush = DirectCast(New BrushConverter().ConvertFrom("#DDDDDD"), Brush)
    Public Shared GreenDark As Brush = DirectCast(New BrushConverter().ConvertFrom("#008136"), Brush)
    Public Shared GreenLight As Brush = DirectCast(New BrushConverter().ConvertFrom("#27AE60"), Brush)
    Public Shared OrangeLight As Brush = DirectCast(New BrushConverter().ConvertFrom("#E67E2E"), Brush)
    Public Shared PurpleDark As Brush = DirectCast(New BrushConverter().ConvertFrom("#8E44AD"), Brush)
    Public Shared RedLight As Brush = DirectCast(New BrushConverter().ConvertFrom("#E74C3C"), Brush)
End Class

' Bm = Beatmap
' BmDP = Beatmap Detail Panel

Class MainWindow
    Private WithEvents BmDP_Client As New WebClient
    Private WithEvents FadeOut As New DoubleAnimation()

    Private BmDic_Installed As New Dictionary(Of Integer, Beatmap)
    Private Exporter_BmList_SelectedTags As New List(Of Importer.TagData)
    Private Exporter_BmList_UnselectedTags As New List(Of Importer.TagData)
    Private ImporterContainer As New Importer
    Private Interface_LoaderText As New TextBlock
    Private Interface_LoaderProgressBar As New ProgressBar
    Private Sync_Done As Boolean = False
    Private Sync_Done_ImporterRequest As Boolean = False
    Private Sync_Done_ImporterRequest_SaveValue As New Dictionary(Of Integer, Beatmap)

    Private WithEvents BGW_SyncGetIDs As New ComponentModel.BackgroundWorker With {
        .WorkerReportsProgress = True,
        .WorkerSupportsCancellation = True}

    Function BalloonShow(Content As String, Optional Title As String = "osu!Sync", Optional Icon As BalloonIcon = BalloonIcon.Info, Optional BallonNextAction As NotifyNextAction = NotifyNextAction.None) As Boolean
        If AppSettings.Tool_EnableNotifyIcon = 0 Then
            With TI_Notify
                .Tag = BallonNextAction
                .ShowBalloonTip(Title, Content, Icon)
            End With
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a CSV-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as CSV-String.</returns>
    ''' <remarks></remarks>
    Function ConvertBmListToCSV(Source As Dictionary(Of Integer, Beatmap)) As String
        Dim Content As String = "sep=;" & vbNewLine
        Content += "ID;Artist;Creator;Title" & vbNewLine
        For Each SelBm As KeyValuePair(Of Integer, Beatmap) In Source
            Content += SelBm.Value.ID & ";" & """" & SelBm.Value.Artist & """;""" & SelBm.Value.Creator & """;""" & SelBm.Value.Title & """" & vbNewLine
        Next
        Return Content
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to HTML-Code.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as HTML and possible warnings together in a String().</returns>
    ''' <remarks></remarks>
    Function ConvertBmListToHTML(Source As Dictionary(Of Integer, Beatmap)) As String()
        Dim Failed As String = ""
        Dim HTML_Source As String = "<!doctype html>" & vbNewLine &
            "<html>" & vbNewLine &
            "<head><meta charset=""utf-8""><meta name=""author"" content=""osu!Sync""/><meta name=""generator"" content=""osu!Sync " & My.Application.Info.Version.ToString & """/><meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=yes""/><title>Beatmap List | osu!Sync</title><link rel=""icon"" type=""image/png"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Favicon.png""/><link href=""http://fonts.googleapis.com/css?family=Open+Sans:400,300,600,700"" rel=""stylesheet"" type=""text/css"" /><link href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/style.css"" rel=""stylesheet"" type=""text/css""/><link rel=""stylesheet"" type=""text/css"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/css/tooltipster.css""/></head>" & vbNewLine &
            "<body>" & vbNewLine &
            "<div id=""Wrapper"">" & vbNewLine &
            vbTab & "<header><p>Beatmap List | osu!Sync</p></header>" & vbNewLine &
            vbTab & "<div id=""Sort""><ul><li><strong>Sort by...</strong></li><li><a class=""SortParameter"" href=""#Sort_Artist"">Artist</a></li><li><a class=""SortParameter"" href=""#Sort_Creator"">Creator</a></li><li><a class=""SortParameter"" href=""#Sort_SetName"">Name</a></li><li><a class=""SortParameter"" href=""#Sort_SetID"">Set ID</a></li></ul></div>" & vbNewLine &
            vbTab & "<div id=""ListWrapper"">"

        For Each SelBm As KeyValuePair(Of Integer, Beatmap) In Source
            If SelBm.Value.ID = -1 Then
                Failed += vbNewLine & "* " & SelBm.Value.ID.ToString & " / " & SelBm.Value.Artist & " / " & SelBm.Value.Title
            Else
                SelBm.Value.Artist.Replace("""", "'")
                SelBm.Value.Creator.Replace("""", "'")
                SelBm.Value.Title.Replace("""", "'")
                HTML_Source += vbNewLine & vbTab & vbTab & "<article id=""beatmap-" & SelBm.Value.ID & """ data-artist=""" & SelBm.Value.Artist & """ data-creator=""" & SelBm.Value.Creator & """ data-setName=""" & SelBm.Value.Title & """ data-setID=""" & SelBm.Value.ID & """><a class=""DownloadArrow"" href=""https://osu.ppy.sh/d/" & SelBm.Value.ID & """ target=""_blank"">&#8250;</a><h1><span title=""Beatmap Set Name"">" & SelBm.Value.Title & "</span></h1><h2><span title=""Beatmap Set ID"">" & SelBm.Value.ID & "</span></h2><p><a class=""InfoTitle"" data-function=""artist"" href=""https://osu.ppy.sh/p/beatmaplist?q=" & SelBm.Value.Artist & """ target=""_blank"">Artist.</a> " & SelBm.Value.Artist & " <a class=""InfoTitle"" data-function=""creator"" href=""https://osu.ppy.sh/p/beatmaplist?q=" & SelBm.Value.Creator & """ target=""_blank"">Creator.</a> " & SelBm.Value.Creator & " <a class=""InfoTitle"" data-function=""overview"" href=""https://osu.ppy.sh/s/" & SelBm.Value.ID & """ target=""_blank"">Overview.</a> <a class=""InfoTitle"" data-function=""discussion"" href=""https://osu.ppy.sh/s/" & SelBm.Value.ID & "#disqus_thread"" target=""_blank"">Discussion.</a></p></article>"
            End If
        Next
        HTML_Source += "</div>" & vbNewLine &
        "</div>" & vbNewLine &
        "<footer><p>Generated with osu!Sync, an open-source tool made by <a href=""http://nw520.de/"" target=""_blank"">naseweis520</a>.</p></footer>" & vbNewLine &
        "<script src=""http://code.jquery.com/jquery-latest.min.js""></script><script src=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/js/jquery.tooltipster.min.js""></script><script src=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/script.js""></script>" & vbNewLine &
        "</body>" & vbNewLine &
        "</html>"

        Dim Answer As String() = {HTML_Source, Failed}
        Return Answer
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to OSBL.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as OSBL and possible warnings together in a String().</returns>
    ''' <remarks></remarks>
    Function ConvertBmListToJSON(Source As Dictionary(Of Integer, Beatmap)) As String()
        Dim Failed_Unsubmitted As String = ""
        Dim Failed_Alread_Assigned As String = ""
        Dim Content As New Dictionary(Of String, Dictionary(Of String, String))
        Content.Add("_info", New Dictionary(Of String, String) From {
                    {"_date", Date.Now.ToString("yyyyMMdd")},
                    {"_version", My.Application.Info.Version.ToString}})
        For Each SelBm As KeyValuePair(Of Integer, Beatmap) In Source
            If SelBm.Value.ID = -1 Then
                Failed_Unsubmitted += vbNewLine & "* " & SelBm.Value.ID.ToString & " / " & SelBm.Value.Artist & " / " & SelBm.Value.Title
            ElseIf Content.ContainsKey(SelBm.Value.ID.ToString) Then
                Failed_Alread_Assigned += vbNewLine & "* " & SelBm.Value.ID.ToString & " / " & SelBm.Value.Artist & " / " & SelBm.Value.Title
            Else
                Content.Add(SelBm.Value.ID.ToString, New Dictionary(Of String, String) From {
                            {"artist", SelBm.Value.Artist},
                            {"creator", SelBm.Value.Creator},
                            {"id", SelBm.Value.ID.ToString},
                            {"title", SelBm.Value.Title}})
            End If
        Next
        Dim Content_Json As String = JsonConvert.SerializeObject(Content)

        Dim Failed As String = ""
        If Not Failed_Unsubmitted = "" Then Failed += "# " & _e("MainWindow_unsubmittedBeatmapSets") & vbNewLine & _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") & vbNewLine & vbNewLine & "> " & _e("MainWindow_beatmaps") & ":" & Failed_Unsubmitted & vbNewLine & vbNewLine
        If Not Failed_Alread_Assigned = "" Then Failed += "# " & _e("MainWindow_idAlreadyAssigned") & vbNewLine & _e("MainWindow_beatmapsIdsCanBeUsedOnlyOnce") & vbNewLine & vbNewLine & "> " & _e("MainWindow_beatmaps") & ":" & Failed_Alread_Assigned
        Dim Answer As String() = {Content_Json, Failed}
        Return Answer
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a TXT-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as TXT-String.</returns>
    ''' <remarks></remarks>
    Function ConvertBmListToTXT(Source As Dictionary(Of Integer, Beatmap)) As String
        Dim Content As String = "// osu!Sync (" & My.Application.Info.Version.ToString & ") | " & Date.Now.ToString("dd.MM.yyyy") & vbNewLine & vbNewLine
        For Each SelBm As KeyValuePair(Of Integer, Beatmap) In Source
            Content += "# " & SelBm.Value.ID & vbNewLine &
                "* Creator: " & vbTab & SelBm.Value.Creator & vbNewLine &
                "* Artist: " & vbTab & SelBm.Value.Artist & vbNewLine &
                "* ID: " & vbTab & vbTab & vbTab & SelBm.Value.ID & vbNewLine &
                "* Title: " & vbTab & vbTab & SelBm.Value.Title & vbNewLine & vbNewLine
        Next
        Return Content
    End Function

    Function ConvertSavedJSONtoListBeatmap(Source As JObject) As Dictionary(Of Integer, Beatmap)
        Dim BeatmapList As New Dictionary(Of Integer, Beatmap)

        For Each SelectedToken As JToken In Source.Values
            If Not SelectedToken.Path.StartsWith("_") Then
                Dim CurrentBeatmap As New Beatmap With {
                    .ID = CInt(SelectedToken.SelectToken("id")),
                    .Title = CStr(SelectedToken.SelectToken("title")),
                    .Artist = CStr(SelectedToken.SelectToken("artist"))}
                If Not SelectedToken.SelectToken("artist") Is Nothing Then CurrentBeatmap.Creator = CStr(SelectedToken.SelectToken("creator"))
                BeatmapList.Add(CurrentBeatmap.ID, CurrentBeatmap)
            End If
        Next
        Return BeatmapList
    End Function

    Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

    Sub ApplySettings()
        With La_FooterWarn
            .Content = ""
            .ToolTip = ""
        End With

        ' NotifyIcon
        Select Case AppSettings.Tool_EnableNotifyIcon
            Case 0, 2
                MI_AppToTray.Visibility = Visibility.Visible
                TI_Notify.Visibility = Visibility.Visible
            Case 3
                MI_AppToTray.Visibility = Visibility.Visible
                TI_Notify.Visibility = Visibility.Collapsed
            Case 4
                MI_AppToTray.Visibility = Visibility.Collapsed
                TI_Notify.Visibility = Visibility.Collapsed
        End Select

        ' Check Write Access
        If Directory.Exists(AppSettings.osu_SongsPath) Then
            Tool_HasWriteAccessToOsu = DirAccessCheck(AppSettings.osu_SongsPath)
            If Tool_HasWriteAccessToOsu = False Then
                If AppSettings.Tool_RequestElevationOnStartup Then
                    If RequestElevation() Then
                        Windows.Application.Current.Shutdown()
                        Exit Sub
                    Else
                        MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, AppName)
                    End If
                End If
                With La_FooterWarn
                    .Content = _e("MainWindow_noAccess")
                    .ToolTip = _e("MainWindow_tt_noAccess")
                End With
            End If
        End If
    End Sub

    ''' <summary>
    ''' Updates the beatmap list interface.
    ''' </summary>
    ''' <param name="BeatmapList">List of Beatmaps to display</param>
    ''' <param name="Destination">Selects the list where to display the new list. Possible values <code>Installed</code>, <code>Importer</code>, <code>Exporter</code></param>
    ''' <param name="LastUpdateTime">Only required when <paramref name="Destination"/> = Installed</param>
    ''' <remarks></remarks>
    Sub BmDisplayUpdate(BeatmapList As Dictionary(Of Integer, Beatmap), Optional Destination As UpdateBmDisplayDestinations = UpdateBmDisplayDestinations.Installed, Optional LastUpdateTime As String = Nothing)
        Select Case Destination
            Case UpdateBmDisplayDestinations.Installed
                With La_FooterLastSync
                    If LastUpdateTime = Nothing Then
                        .Content = _e("MainWindow_lastSync").Replace("%0", Date.Now.ToString(_e("MainWindow_dateFormat") & " " & _e("MainWindow_timeFormat")))
                        .Tag = Date.Now.ToString("yyyyMMddHHmmss")
                    Else
                        .Content = _e("MainWindow_lastSync").Replace("%0", LastUpdateTime)
                        .Tag = LastUpdateTime
                    End If
                End With

                BeatmapWrapper.Children.Clear()

                For Each SelBm As KeyValuePair(Of Integer, Beatmap) In BeatmapList
                    Dim UI_Grid = New Grid() With {
                        .Height = 80,
                        .Margin = New Thickness(0, 0, 0, 5),
                        .Tag = SelBm.Value,
                        .Width = Double.NaN}
                    With UI_Grid.ColumnDefinitions
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(5)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(108)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition)
                    End With
                    Dim UI_DecoBorderLeft = New Rectangle
                    Dim UI_Thumbnail = New Image
                    Grid.SetColumn(UI_Thumbnail, 2)
                    If SelBm.Value.ID = -1 Then
                        AddHandler(UI_Thumbnail.MouseUp), AddressOf BmDP_Show
                    Else
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf BmDP_Show
                        AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf OpenBmListing
                    End If
                    If File.Exists(AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg") Then
                        Try
                            UI_Thumbnail.Source = New BitmapImage(New Uri(AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg"))
                        Catch ex As NotSupportedException
                            UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                        End Try
                    Else
                        UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                    End If
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 28,
                        .Foreground = StandardColors.GrayDark,
                        .Height = 36,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Text = SelBm.Value.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 4)
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 14,
                        .Foreground = StandardColors.GreenDark,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Margin = New Thickness(0, 38, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 4)
                    If Not SelBm.Value.ID = -1 Then UI_TextBlock_Caption.Text = SelBm.Value.ID.ToString & " | " & SelBm.Value.Artist Else UI_TextBlock_Caption.Text = _e("MainWindow_unsubmitted") & " | " & SelBm.Value.Artist
                    If Not SelBm.Value.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelBm.Value.Creator
                    Dim UI_Checkbox_IsInstalled = New CheckBox With {
                        .Content = _e("MainWindow_isInstalled"),
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .IsChecked = True,
                        .IsEnabled = False,
                        .Margin = New Thickness(0, 62, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsInstalled, 4)
                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsInstalled)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With
                    BeatmapWrapper.Children.Add(UI_Grid)
                Next
                If BeatmapList.Count = 0 Then
                    Dim UI_TextBlock As New TextBlock With {
                        .FontSize = 72,
                        .Foreground = StandardColors.GreenLight,
                        .HorizontalAlignment = HorizontalAlignment.Center,
                        .Margin = New Thickness(0, 86, 0, 0),
                        .Text = _e("MainWindow_beatmapsFound").Replace("%0", "0"),
                        .VerticalAlignment = VerticalAlignment.Center}
                    Dim UI_TextBlock_SubTitle As New TextBlock With {
                        .FontSize = 24,
                        .Foreground = StandardColors.GreenLight,
                        .HorizontalAlignment = HorizontalAlignment.Center,
                        .Text = _e("MainWindow_thatsImpressiveIGuess"),
                        .VerticalAlignment = VerticalAlignment.Center}
                    With BeatmapWrapper.Children
                        .Add(UI_TextBlock)
                        .Add(UI_TextBlock_SubTitle)
                    End With
                End If
                TB_BmCounter.Text = _e("MainWindow_beatmapsFound").Replace("%0", BeatmapList.Count.ToString)
                Bu_SyncRun.IsEnabled = True
            Case UpdateBmDisplayDestinations.Importer
                ImporterContainer = New Importer
                ImporterContainer.BmTotal = 0
                TI_Importer.Visibility = Visibility.Visible
                TC_Main.SelectedIndex = 1
                SP_ImporterWrapper.Children.Clear()
                CB_ImporterHideInstalled.IsChecked = False
                Bu_ImporterCancel.IsEnabled = False
                Bu_ImporterRun.IsEnabled = False
                If Sync_Done = False Then
                    Sync_Done_ImporterRequest = True
                    Bu_SyncRun.IsEnabled = False
                    Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With {
                       .Height = 150,
                       .HorizontalAlignment = HorizontalAlignment.Center,
                       .IsActive = True,
                       .Margin = New Thickness(0, 100, 0, 0),
                       .VerticalAlignment = VerticalAlignment.Center,
                       .Width = 150}
                    Dim UI_TextBlock_SubTitle As New TextBlock With {
                        .FontSize = 24,
                        .Foreground = StandardColors.GrayLighter,
                        .HorizontalAlignment = HorizontalAlignment.Center,
                        .Text = _e("MainWindow_pleaseWait") & vbNewLine & _e("MainWindow_syncing"),
                        .TextAlignment = TextAlignment.Center,
                        .VerticalAlignment = VerticalAlignment.Center}
                    Interface_LoaderText = UI_TextBlock_SubTitle
                    SP_ImporterWrapper.Children.Add(UI_ProgressRing)
                    SP_ImporterWrapper.Children.Add(UI_TextBlock_SubTitle)
                    Sync_Done_ImporterRequest_SaveValue = BeatmapList
                    Sync_GetIDs()
                    Exit Sub
                End If
                For Each SelBm As KeyValuePair(Of Integer, Beatmap) In BeatmapList
                    Bu_ImporterCancel.IsEnabled = True
                    Dim Check_IfInstalled As Boolean
                    If BmDic_Installed.ContainsKey(SelBm.Value.ID) Then Check_IfInstalled = True Else Check_IfInstalled = False
                    Dim UI_Grid = New Grid() With {
                        .Height = 51,
                        .Margin = New Thickness(0, 0, 0, 5),
                        .Width = Double.NaN}
                    With UI_Grid.ColumnDefinitions
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(73)})
                        .Add(New ColumnDefinition)
                    End With
                    Dim UI_DecoBorderLeft = New Rectangle With {
                        .Fill = StandardColors.GreenLight,
                        .VerticalAlignment = VerticalAlignment.Stretch}
                    If Check_IfInstalled Then UI_DecoBorderLeft.Fill = StandardColors.GreenLight Else UI_DecoBorderLeft.Fill = StandardColors.RedLight
                    Dim UI_Thumbnail = New Image With {
                        .Cursor = Cursors.Hand,
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .Margin = New Thickness(5, 0, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Stretch}
                    Grid.SetColumn(UI_Thumbnail, 1)
                    Dim ThumbPath As String = ""
                    If File.Exists(AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg") Then
                        ThumbPath = (AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg")
                    ElseIf File.Exists(AppTempPath & "\Cache\Thumbnails\" & SelBm.Value.ID & ".jpg") Then
                        ThumbPath = (AppTempPath & "\Cache\Thumbnails\" & SelBm.Value.ID & ".jpg")
                    End If
                    If Not ThumbPath = "" Then
                        Try
                            With UI_Thumbnail
                                .Source = New BitmapImage(New Uri(ThumbPath))
                                .ToolTip = _e("MainWindow_openBeatmapDetailPanel")
                            End With
                            AddHandler(UI_Thumbnail.MouseDown), AddressOf BmDP_Show
                        Catch ex As NotSupportedException
                            With UI_Thumbnail
                                .Source = New BitmapImage(New Uri("Resources/DownloadThumbnail.png", UriKind.Relative))
                                .ToolTip = _e("MainWindow_downladThumbnail")
                            End With
                            AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
                            AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf BmDP_Show
                        End Try
                    Else
                        With UI_Thumbnail
                            .Source = New BitmapImage(New Uri("Resources/DownloadThumbnail.png", UriKind.Relative))
                            .ToolTip = _e("MainWindow_downladThumbnail")
                        End With
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
                        AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf BmDP_Show
                    End If
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 22,
                        .Foreground = StandardColors.GrayDark,
                        .Height = 30,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelBm.Value.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 12,
                        .Foreground = StandardColors.GreenDark,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Text = SelBm.Value.ID.ToString & " | " & SelBm.Value.Artist,
                        .Margin = New Thickness(10, 30, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)
                    If Not SelBm.Value.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelBm.Value.Creator
                    Dim UI_Checkbox_IsSelected = New CheckBox With {
                        .Content = _e("MainWindow_downloadAndInstall"),
                        .HorizontalAlignment = HorizontalAlignment.Right,
                        .IsChecked = True,
                        .Margin = New Thickness(10, 5, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsSelected, 2)
                    If Check_IfInstalled Then
                        With UI_Checkbox_IsSelected
                            .IsChecked = False
                            .IsEnabled = False
                        End With
                    Else
                        With UI_Checkbox_IsSelected
                            .IsChecked = True
                            .IsEnabled = True
                        End With
                    End If
                    AddHandler(UI_Checkbox_IsSelected.Checked), AddressOf Importer_AddBmToSel
                    AddHandler(UI_Checkbox_IsSelected.Unchecked), AddressOf Importer_RemoveBmFromSel
                    Dim TagData As New Importer.TagData With {
                        .Beatmap = SelBm.Value,
                        .IsInstalled = Check_IfInstalled,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title,
                        .UI_Thumbnail = UI_Thumbnail}
                    If Check_IfInstalled = False Then ImporterContainer.BmList_TagsToInstall.Add(TagData)
                    UI_Grid.Tag = TagData
                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsSelected)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With
                    SP_ImporterWrapper.Children.Add(UI_Grid)
                    ImporterContainer.BmTotal += 1
                Next
                Bu_ImporterCancel.IsEnabled = True
                TB_ImporterInfo.ToolTip = TB_ImporterInfo.Text
                If ImporterContainer.BmList_TagsToInstall.Count = 0 Then Bu_ImporterRun.IsEnabled = False Else Bu_ImporterRun.IsEnabled = True
                Importer_UpdateInfo()
                TB_ImporterMirror.Text = _e("MainWindow_downloadMirror") & ": " & Application_Mirrors(AppSettings.Tool_DownloadMirror).DisplayName
            Case UpdateBmDisplayDestinations.Exporter
                SP_ExporterWrapper.Children.Clear()
                For Each SelBm As KeyValuePair(Of Integer, Beatmap) In BeatmapList
                    Dim UI_Grid = New Grid() With {
                        .Height = 51,
                        .Margin = New Thickness(0, 0, 0, 5),
                        .Width = Double.NaN}
                    With UI_Grid.ColumnDefinitions
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(73)})
                        .Add(New ColumnDefinition)
                    End With
                    Dim UI_DecoBorderLeft = New Rectangle With {
                        .Fill = StandardColors.GreenLight,
                        .VerticalAlignment = VerticalAlignment.Stretch}
                    Dim UI_Thumbnail = New Image With {
                        .Cursor = Cursors.Hand,
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .Margin = New Thickness(5, 0, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Stretch}
                    Grid.SetColumn(UI_Thumbnail, 1)
                    AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf BmDP_Show
                    If File.Exists(AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg") Then
                        Try
                            UI_Thumbnail.Source = New BitmapImage(New Uri(AppSettings.osu_Path & "\Data\bt\" & SelBm.Value.ID & "l.jpg"))
                        Catch ex As NotSupportedException
                            UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                        End Try
                    Else
                        UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                    End If
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 22,
                        .Foreground = StandardColors.GrayDark,
                        .Height = 30,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelBm.Value.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 12,
                        .Foreground = StandardColors.GreenDark,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Text = SelBm.Value.ID.ToString & " | " & SelBm.Value.Artist,
                        .Margin = New Thickness(10, 30, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)
                    If Not SelBm.Value.ID = -1 Then UI_TextBlock_Caption.Text = SelBm.Value.ID.ToString & " | " & SelBm.Value.Artist Else UI_TextBlock_Caption.Text = _e("MainWindow_unsubmittedBeatmapCantBeExported") & " | " & SelBm.Value.Artist
                    If Not SelBm.Value.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelBm.Value.Creator
                    Dim UI_Checkbox_IsSelected = New CheckBox With {
                        .Content = _e("MainWindow_selectToExport"),
                        .HorizontalAlignment = HorizontalAlignment.Right,
                        .IsChecked = True,
                        .Margin = New Thickness(10, 5, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsSelected, 2)
                    If SelBm.Value.ID = -1 Then
                        With UI_Checkbox_IsSelected
                            .IsChecked = False
                            .IsEnabled = False
                        End With
                        UI_DecoBorderLeft.Fill = StandardColors.GrayLight
                    Else
                        AddHandler(UI_Checkbox_IsSelected.Checked), AddressOf Exporter_AddBmToSel
                        AddHandler(UI_Checkbox_IsSelected.Unchecked), AddressOf Exporter_RemoveBmFromSel
                        AddHandler(UI_DecoBorderLeft.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_TextBlock_Title.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Exporter_DetermineWheterAddOrRemove
                    End If
                    Dim TagData As New Importer.TagData With {
                        .Beatmap = SelBm.Value,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title,
                        .UI_Thumbnail = UI_Thumbnail}
                    Exporter_BmList_SelectedTags.Add(TagData)
                    UI_Grid.Tag = TagData
                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsSelected)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With
                    SP_ExporterWrapper.Children.Add(UI_Grid)
                Next
                TI_Exporter.Visibility = Visibility.Visible
                TC_Main.SelectedIndex = 2
        End Select
    End Sub

    Sub BmDP_Client_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs) Handles BmDP_Client.DownloadStringCompleted
        BeatmapDetails_APIProgress.Visibility = Visibility.Collapsed

        If e.Cancelled Then
            UI_SetStatus(_e("MainWindow_aborted"))
            WriteToApiLog("/api/get_beatmaps", "{Cancelled}")
        Else
            Dim JSON_Array As JArray
            Try
                JSON_Array = CType(JsonConvert.DeserializeObject(e.Result), JArray)
                If Not JSON_Array.First Is Nothing Then
                    Dim CI As Globalization.CultureInfo
                    Try
                        CI = New Globalization.CultureInfo(AppSettings.Tool_Language.Replace("_", "-"))
                    Catch ex As Globalization.CultureNotFoundException
                        CI = New Globalization.CultureInfo("en-US")
                    End Try
                    WriteToApiLog("/api/get_beatmaps", e.Result)
                    BeatmapDetails_APIFavouriteCount.Text = CInt(JSON_Array.First.SelectToken("favourite_count")).ToString("n", CI).Substring(0, CInt(JSON_Array.First.SelectToken("favourite_count")).ToString("n", CI).Length - 3)    ' Isn't there a better way to do this?!

                    Dim PassCount(JSON_Array.Count), PlayCount(JSON_Array.Count) As Integer
                    Dim PassCount_TTText = "", PlayCount_TTText As String = ""
                    Dim i As Integer
                    For Each a As JObject In JSON_Array.Children
                        Dim CurrentPassCount = CInt(a.SelectToken("passcount")), CurrentPlayCount As Integer = CInt(a.SelectToken("playcount"))
                        PassCount(i) = CurrentPassCount
                        PlayCount(i) = CurrentPlayCount
                        If i = 0 Then
                            PassCount_TTText = a.SelectToken("version").ToString & ":" & vbTab & CurrentPassCount.ToString("n", CI).Substring(0, CurrentPassCount.ToString("n", CI).Length - 3)
                            PlayCount_TTText = a.SelectToken("version").ToString & ":" & vbTab & CurrentPlayCount.ToString("n", CI).Substring(0, CurrentPlayCount.ToString("n", CI).Length - 3)
                        Else
                            PassCount_TTText += vbNewLine & a.SelectToken("version").ToString & ":" & vbTab & CurrentPassCount.ToString("n", CI).Substring(0, CurrentPassCount.ToString("n", CI).Length - 3)
                            PlayCount_TTText += vbNewLine & a.SelectToken("version").ToString & ":" & vbTab & CurrentPlayCount.ToString("n", CI).Substring(0, CurrentPlayCount.ToString("n", CI).Length - 3)
                        End If
                        i += 1
                    Next

                    With BeatmapDetails_APIPassCount
                        .Text = Math.Round(PassCount.Sum / PassCount.Count, 2).ToString("n", CI)
                        .ToolTip = PassCount_TTText
                    End With
                    With BeatmapDetails_APIPlayCount
                        .Text = Math.Round(PlayCount.Sum / PlayCount.Count, 2).ToString("n", CI)
                        .ToolTip = PlayCount_TTText
                    End With

                    BmDP_SetRankedStatus(CType(JSON_Array.First.SelectToken("approved").ToString, Beatmap.OnlineApprovedStatuses))
                    UI_SetStatus(_e("MainWindow_finished"))
                Else
                    UI_SetStatus(_e("MainWindow_failed"))
                    WriteToApiLog("/api/get_beatmaps", "{UnexpectedAnswer} " & e.Result)
                    With BeatmapDetails_APIWarn
                        .Content = _e("MainWindow_detailsPanel_apiError")
                        .Visibility = Visibility.Visible
                    End With
                End If
            Catch ex As Exception
                UI_SetStatus(_e("MainWindow_failed"))
                WriteToApiLog("/api/get_beatmaps")
                With BeatmapDetails_APIWarn
                    .Content = _e("MainWindow_detailsPanel_apiError")
                    .Visibility = Visibility.Visible
                End With
            End Try
        End If
    End Sub

    Sub BmDP_SetRankedStatus(value As Beatmap.OnlineApprovedStatuses)
        With BeatmapDetails_RankedStatus
            Select Case value
                Case Beatmap.OnlineApprovedStatuses.Ranked, Beatmap.OnlineApprovedStatuses.Approved
                    .Background = StandardColors.GreenDark
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_ranked")
                Case Beatmap.OnlineApprovedStatuses.Pending
                    .Background = StandardColors.PurpleDark
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_pending")
                Case Else
                    .Background = StandardColors.GrayLight
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_unranked")
            End Select
        End With
    End Sub

    Sub BmDP_Show(sender As Object, e As MouseButtonEventArgs)
        Dim Csender_Bm As Beatmap
        If TypeOf sender Is Image Then
            Dim Cparent As Grid = CType(CType(sender, Image).Parent, Grid)   ' Get Tag from parent Grid
            If TypeOf Cparent.Tag Is Beatmap Then Csender_Bm = CType(Cparent.Tag, Beatmap) Else Csender_Bm = CType(Cparent.Tag, Importer.TagData).Beatmap
        Else
            Exit Sub
        End If
        UI_ShowBmDP(Csender_Bm.ID, New BmDPDetails With {
                    .Artist = Csender_Bm.Artist,
                    .Creator = Csender_Bm.Creator,
                    .IsUnplayed = Csender_Bm.IsUnplayed,
                    .RankedStatus = Csender_Bm.RankedStatus,
                    .Title = Csender_Bm.Title})
    End Sub

#Region "Bu - Button"
    Sub Bu_BmDetailsListing_Click(sender As Object, e As RoutedEventArgs) Handles Bu_BmDetailsListing.Click
        Dim Csender As Button = CType(sender, Button)
        Dim Csender_Tag As String = CStr(Csender.Tag)
        Process.Start("http://osu.ppy.sh/s/" & Csender_Tag)
    End Sub

    Sub Bu_SyncRun_Click(sender As Object, e As RoutedEventArgs) Handles Bu_SyncRun.Click
        If Tool_IsElevated AndAlso AppSettings.Tool_CheckFileAssociation Then FileAssociationCheck()
        Sync_GetIDs()
    End Sub
#End Region

    Sub Exporter_ExportBmDialog(Source As Dictionary(Of Integer, Beatmap), Optional DialogTitle As String = "")
        If DialogTitle = "" Then DialogTitle = _e("MainWindow_exportInstalledBeatmaps1")
        Dim Dialog_SaveFile As New Microsoft.Win32.SaveFileDialog()
        With Dialog_SaveFile
            .AddExtension = True
            .Filter = _e("MainWindow_fileext_osblx") & "     (*.nw520-osblx)|*.nw520-osblx|" &
                _e("MainWindow_fileext_osbl") & "     (*.nw520-osbl)|*.nw520-osbl|" &
                _e("MainWindow_fileext_zip") & "     (*.zip)|*.osblz.zip|" &
                _e("MainWindow_fileext_html") & "     [" & _e("MainWindow_notImportable") & "] (*.html)|*.html|" &
                _e("MainWindow_fileext_txt") & "     [" & _e("MainWindow_notImportable") & "] (*.txt)|*.txt|" &
                _e("MainWindow_fileext_json") & "     (*.json)|*.json|" &
                _e("MainWindow_fileext_csv") & "     [" & _e("MainWindow_notImportable") & "] (*.csv)|*.csv"
            .OverwritePrompt = True
            .Title = DialogTitle
            .ValidateNames = True
            .ShowDialog()
        End With
        If Dialog_SaveFile.FileName = "" Then
            OverlayShow(_e("MainWindow_exportAborted"), "")
            OverlayFadeOut()
            Exit Sub
        End If

        Select Case Dialog_SaveFile.FilterIndex
            Case 1      '.nw520-osblx
                Dim Content As String() = ConvertBmListToJSON(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(StringCompress(Content(0)))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBLX"))
                OverlayFadeOut()
            Case 2      '.nw520-osbl
                Dim Content As String() = ConvertBmListToJSON(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Content(0))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                             _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBL"))
                OverlayFadeOut()
            Case 3      '.osblz.zip
                Dim DirectName As String = AppTempPath & "\Zipper\Exporter-" & Date.Now.ToString("yyyy-MM-dd HH.mm.ss")
                Directory.CreateDirectory(DirectName)

                Dim Content As String() = ConvertBmListToJSON(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(DirectName & "\00.nw520-osblx", False)
                    File.Write(StringCompress(Content(0)))
                    File.Close()
                End Using
                PackageDirectoryAsZIP(DirectName, Dialog_SaveFile.FileName)
                Directory.Delete(DirectName, True)
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "Zipped OSBLX"))
                OverlayFadeOut()
            Case 4      '.html
                Dim Content As String() = ConvertBmListToHTML(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Content(0))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    Content(1) = Content(1).Insert(0, "# " & _e("MainWindow_unsubmittedBeatmapSets") & vbNewLine & _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") & vbNewLine & vbNewLine & "> " & _e("MainWindow_beatmaps") & ": ")
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                             _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "HTML"))
                OverlayFadeOut()
            Case 5     '.txt
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(ConvertBmListToTXT(Source))
                    File.Close()
                End Using
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "TXT"))
                OverlayFadeOut()
            Case 6     '.json
                Dim Content As String() = ConvertBmListToJSON(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Content(0))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                             _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "JSON"))
                OverlayFadeOut()
            Case 7     '.csv
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(ConvertBmListToCSV(Source))
                    File.Close()
                End Using
                OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "CSV"))
                OverlayFadeOut()
        End Select
    End Sub


    ''' <summary>
    ''' Checks osu!Sync's file associations and creates them if necessary.
    ''' </summary>
    ''' <remarks></remarks>
    Sub FileAssociationCheck()
        Dim FileExtension_Check As Integer = 0        '0 = OK, 1 = Missing File Extension, 2 = Invalid/Outdated File Extension
        For Each Extension() As String In Application_FileExtensions
            If My.Computer.Registry.ClassesRoot.OpenSubKey(Extension(0)) Is Nothing Then
                If FileExtension_Check = 0 Then
                    FileExtension_Check = 1
                    Exit For
                End If
            End If
        Next
        If Not FileExtension_Check = 1 Then
            For Each Extension() As String In Application_FileExtensions
                Dim RegistryPath As String = CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(Extension(1)).OpenSubKey("DefaultIcon").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None))
                RegistryPath = RegistryPath.Substring(1)
                RegistryPath = RegistryPath.Substring(0, RegistryPath.Length - 3)
                If Not RegistryPath = Reflection.Assembly.GetExecutingAssembly().Location.ToString Then
                    FileExtension_Check = 2
                    Exit For
                End If

                RegistryPath = (CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(Extension(1)).OpenSubKey("shell").OpenSubKey("open").OpenSubKey("command").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None)))
                If Not RegistryPath = """" & Reflection.Assembly.GetExecutingAssembly().Location.ToString & """ -openFile=""%1""" Then
                    FileExtension_Check = 2
                    Exit For
                End If
            Next
        End If

        If Not FileExtension_Check = 0 Then
            Dim MessageBox_Content As String
            If FileExtension_Check = 1 Then MessageBox_Content = _e("MainWindow_extensionNotAssociated") & vbNewLine & _e("MainWindow_doYouWantToFixThat") Else MessageBox_Content = _e("MainWindow_extensionWrong") & vbNewLine & _e("MainWindow_doYouWantToFixThat")
            If MessageBox.Show(MessageBox_Content, AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then FileAssociationsCreate()
        End If
    End Sub

    Sub FadeOut_Completed(sender As Object, e As EventArgs) Handles FadeOut.Completed
        Gr_Overlay.Visibility = Visibility.Hidden
    End Sub

    Sub Flyout_BmDetails_RequestBringIntoView(sender As Object, e As RequestBringIntoViewEventArgs) Handles Flyout_BmDetails.RequestBringIntoView
        Flyout_BmDetails.Width = AppSettings.Tool_Interface_BeatmapDetailPanelWidth * (ActualWidth / 100)
    End Sub

#Region "La - Label"
    Sub La_FooterUpdater_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles La_FooterUpdater.MouseDown
        UI_ShowUpdaterWindow()
    End Sub

    Sub La_FooterVer_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles La_FooterVer.MouseDown
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub
#End Region

    Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
#If DEBUG Then
        La_FooterVer.Content = "osu!Sync Version " & My.Application.Info.Version.ToString & " (Dev)"
#Else
        La_FooterVer.Content = "osu!Sync Version " & My.Application.Info.Version.ToString
#End If

        ' Load Configuration
        If File.Exists(AppDataPath & "\Settings\Settings.json") Then
            AppSettings.LoadSettings()
        Else
            Dim Window_Welcome As New Window_Welcome
            Window_Welcome.ShowDialog()
            AppSettings.SaveSettings()
        End If

        ' Apply settings (like NotifyIcon)
        ApplySettings()

        ' Delete old downloaded beatmaps
        If Directory.Exists(AppTempPath & "\Downloads\Beatmaps") Then Directory.Delete(AppTempPath & "\Downloads\Beatmaps", True)

        ' Check For Updates
        Select Case AppSettings.Tool_CheckForUpdates
            Case 0
                UpdateCheck()
            Case 1
                La_FooterUpdater.Content = _e("MainWindow_updatesDisabled")
            Case Else
                Dim Interval As Integer
                Select Case AppSettings.Tool_CheckForUpdates
                    Case 3
                        Interval = 1
                    Case 4
                        Interval = 7
                    Case 5
                        Interval = 30
                End Select
                If DateDiff(DateInterval.Day, Date.ParseExact(AppSettings.Tool_LastCheckForUpdates, "yyyyMMddhhmmss", Globalization.DateTimeFormatInfo.InvariantInfo), Date.Now) >= Interval Then
                    UpdateCheck()
                Else
                    La_FooterUpdater.Content = _e("MainWindow_updateCheckNotNecessary")
                End If
        End Select

        'Open File
        If AppStartArgs IsNot Nothing AndAlso Array.Exists(AppStartArgs, Function(s)
                                                                             If s.Substring(0, 10) = "-openFile=" Then
                                                                                 ImporterContainer = New Importer
                                                                                 ImporterContainer.FilePath = s.Substring(10)
                                                                                 Return True
                                                                             Else
                                                                                 Return False
                                                                             End If
                                                                         End Function) Then
            If File.Exists(ImporterContainer.FilePath) Then
                Importer_ReadListFile(ImporterContainer.FilePath)
            Else
                MsgBox(_e("MainWindow_file404"), MsgBoxStyle.Critical, AppName)
                If AppSettings.Tool_SyncOnStartup Then Sync_GetIDs()
            End If
        Else
            If AppSettings.Tool_SyncOnStartup Then Sync_GetIDs()
        End If
    End Sub

#Region "MI - MenuItem"
    Sub MI_AppExit_Click(sender As Object, e As RoutedEventArgs) Handles MI_AppExit.Click
        Windows.Application.Current.Shutdown()
    End Sub

    Sub MI_AppOsu_Click(sender As Object, e As RoutedEventArgs) Handles MI_AppOsu.Click
        StartOrFocusOsu()
    End Sub

    Sub MI_AppSettings_Click(sender As Object, e As RoutedEventArgs) Handles MI_AppSettings.Click
        UI_ShowSettingsWindow()
        If Not Tool_DontApplySettings Then ApplySettings() Else Tool_DontApplySettings = False
    End Sub

    Sub MI_AppToTray_Click(sender As Object, e As RoutedEventArgs) Handles MI_AppToTray.Click
        ToggleMinimizeToTray()
    End Sub

    Sub MI_FileConvert_Click(sender As Object, e As RoutedEventArgs) Handles MI_FileConvert.Click
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = _e("MainWindow_allSupportedFileFormats") & "|*.json;*.nw520-osbl;*.nw520-osblx|" &
                _e("MainWindow_fileext_osblx") & "|*.nw520-osblx|" &
                _e("MainWindow_fileext_osbl") & "|*.nw520-osbl|" &
                _e("MainWindow_fileext_json") & "|*.json"
            .Title = _e("MainWindow_selectASupportedFileToConvert")
            .ShowDialog()
        End With
        Dim Dialog_SaveFile As New Microsoft.Win32.SaveFileDialog()
        With Dialog_SaveFile
            .AddExtension = True
            .OverwritePrompt = True
            .Title = _e("MainWindow_selectADestination")
            .ValidateNames = True
        End With

        Dim OSBL_Content As String
        If Not Dialog_OpenFile.FileName = "" Then
            OSBL_Content = File.ReadAllText(Dialog_OpenFile.FileName)
            Select Case Path.GetExtension(Dialog_OpenFile.FileName)
                Case ".nw520-osbl"
                    Exporter_ExportBmDialog(ConvertSavedJSONtoListBeatmap(JObject.Parse(OSBL_Content)), _e("MainWindow_convertSelectedFile"))
                Case ".nw520-osblx"
                    Try
                        OSBL_Content = StringDecompress(OSBL_Content)
                    Catch ex As FormatException
                        OverlayShow(_e("MainWindow_conversionFailed"), "System.FormatException")
                        OverlayFadeOut()
                        Exit Sub
                    Catch ex As InvalidDataException
                        OverlayShow(_e("MainWindow_conversionFailed"), "System.IO.InvalidDataException")
                        OverlayFadeOut()
                        Exit Sub
                    End Try
                    Exporter_ExportBmDialog(ConvertSavedJSONtoListBeatmap(JObject.Parse(OSBL_Content)), "Convert selected file")
            End Select
        Else
            OverlayShow(_e("MainWindow_conversionAborted"), "")
            OverlayFadeOut()
            Exit Sub
        End If
    End Sub

    Sub MI_FileExportAll_Click(sender As Object, e As RoutedEventArgs) Handles MI_FileExportAll.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, AppName)
            Exit Sub
        End If
        Exporter_ExportBmDialog(BmDic_Installed)
    End Sub

    Sub MI_FileExportSelected_Click(sender As Object, e As RoutedEventArgs) Handles MI_FileExportSelected.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, AppName)
            Exit Sub
        End If
        BmDisplayUpdate(BmDic_Installed, UpdateBmDisplayDestinations.Exporter)
    End Sub

    Sub MI_FileOpenBmList_Click(sender As Object, e As RoutedEventArgs) Handles MI_FileOpenBmList.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, AppName)
            Exit Sub
        End If
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = _e("MainWindow_allSupportedFileFormats") & "|*.json;*.nw520-osbl;*.nw520-osblx;*.zip|" &
                _e("MainWindow_fileext_osblx") & "|*.nw520-osblx|" &
                _e("MainWindow_fileext_osbl") & "|*.nw520-osbl|" &
                _e("MainWindow_fileext_zip") & "|*.zip|" &
                _e("MainWindow_fileext_json") & "|*.json"
            .Title = _e("MainWindow_openBeatmapList")
            .ShowDialog()
        End With

        If Not Dialog_OpenFile.FileName = "" Then
            Importer_ReadListFile(Dialog_OpenFile.FileName)
        Else
            OverlayShow(_e("MainWindow_importAborted"), "")
            OverlayFadeOut()
        End If
    End Sub

    Sub MI_HelpAbout_Click(sender As Object, e As RoutedEventArgs) Handles MI_HelpAbout.Click
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

    Sub MI_HelpUpdater_Click(sender As Object, e As RoutedEventArgs) Handles MI_HelpUpdater.Click
        UI_ShowUpdaterWindow()
    End Sub

    Sub MI_NotifyAppShowHide_Click(sender As Object, e As RoutedEventArgs) Handles MI_NotifyAppShowHide.Click
        ToggleMinimizeToTray()
    End Sub

    Sub MI_NotifyExit_Click(sender As Object, e As RoutedEventArgs) Handles MI_NotifyExit.Click
        Windows.Application.Current.Shutdown()
    End Sub

    Sub MI_NotifyOsu_Click(sender As Object, e As RoutedEventArgs) Handles MI_NotifyOsu.Click
        StartOrFocusOsu()
    End Sub
#End Region

    Sub OpenBmListing(sender As Object, e As MouseButtonEventArgs)
        Dim Cparent As Grid = CType(CType(sender, Image).Parent, Grid)  ' Get Tag from parent grid
        Dim Csender_Tag As Beatmap = CType(Cparent.Tag, Beatmap)
        Process.Start("http://osu.ppy.sh/s/" & Csender_Tag.ID)
    End Sub

    Sub OverlayFadeOut()
        Visibility = Visibility.Visible
        Gr_Overlay.Visibility = Visibility.Visible
        With FadeOut
            .From = 1
            .To = 0
            .Duration = New Duration(TimeSpan.FromSeconds(1))
        End With
        Storyboard.SetTargetName(FadeOut, "Gr_Overlay")
        Storyboard.SetTargetProperty(FadeOut, New PropertyPath(OpacityProperty))

        Dim MyStoryboard As New Storyboard()
        With MyStoryboard
            .Children.Add(FadeOut)
            .Begin(Me)
        End With
    End Sub

    Sub OverlayShow(Optional Title As String = "", Optional Caption As String = "")
        TB_OverlayCaption.Text = Caption
        TB_OverlayTitle.Text = Title
        With Gr_Overlay
            .Opacity = 1
            .Visibility = Visibility.Visible
        End With
    End Sub

    Sub PackageDirectoryAsZIP(inputDirectory As String, outputPath As String)
        Using Zipper As New ZipFile
            With Zipper
                .AddDirectory(inputDirectory)
                .Save(outputPath)
            End With
        End Using
    End Sub

    ''' <summary>
    ''' Determines whether to start or (when it's running) to focus osu!.
    ''' </summary>
    ''' <remarks></remarks>
    Sub StartOrFocusOsu()
        If Not Process.GetProcessesByName("osu!").Count > 0 Then
            If File.Exists(AppSettings.osu_Path & "\osu!.exe") Then Process.Start(AppSettings.osu_Path & "\osu!.exe") Else MsgBox(_e("MainWindow_unableToFindOsuExe"), MsgBoxStyle.Critical, AppName)
        Else
            For Each ObjProcess As Process In Process.GetProcessesByName("osu!")
                AppActivate(ObjProcess.Id)
                ShowWindow(ObjProcess.MainWindowHandle, 1)
            Next
        End If
    End Sub

    ''' <summary>
    ''' Determines wether to load cache or to sync and will start the progress.
    ''' </summary>
    ''' <remarks></remarks>
    Sub Sync_GetIDs()
        Bu_SyncRun.IsEnabled = False
        UI_SetLoader(_e("MainWindow_parsingInstalledBeatmapSets"))
        La_FooterLastSync.Content = _e("MainWindow_syncing")
        BGW_SyncGetIDs.RunWorkerAsync(New BGWcallback_SyncGetIDs)
    End Sub

#Region "TI - TaskbarIcon"
    Sub TI_Notify_TrayBalloonTipClicked(sender As Object, e As RoutedEventArgs) Handles TI_Notify.TrayBalloonTipClicked
        Select Case CType(TI_Notify.Tag, NotifyNextAction)
            Case NotifyNextAction.OpenUpdater
                UI_ShowUpdaterWindow()
        End Select
    End Sub

    Sub TI_Notify_TrayMouseDoubleClick(sender As Object, e As RoutedEventArgs) Handles TI_Notify.TrayMouseDoubleClick
        ToggleMinimizeToTray()
    End Sub
#End Region

    Sub ToggleMinimizeToTray()
        If Visibility = Visibility.Visible Then
            Select Case AppSettings.Tool_EnableNotifyIcon
                Case 0, 2, 3
                    Visibility = Visibility.Hidden
                    TI_Notify.Visibility = Visibility.Visible
                Case Else
                    MI_AppToTray.IsEnabled = False
            End Select
        Else
            Visibility = Visibility.Visible
            Select Case AppSettings.Tool_EnableNotifyIcon
                Case 3, 4
                    TI_Notify.Visibility = Visibility.Collapsed
            End Select
        End If
    End Sub

#Region "UI"
    Sub UI_SetLoader(Optional Message As String = "Please wait")
        Dim UI_ProgressBar = New ProgressBar With {
            .HorizontalAlignment = HorizontalAlignment.Stretch,
            .Visibility = Visibility.Hidden,
            .Height = 25}
        Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With {
            .Height = 150,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .IsActive = True,
            .Margin = New Thickness(0, 100, 0, 0),
            .VerticalAlignment = VerticalAlignment.Center,
            .Width = 150}
        Dim UI_TextBlock_SubTitle As New TextBlock With {
            .FontSize = 24,
            .Foreground = StandardColors.GrayLighter,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Text = Message,
            .TextAlignment = TextAlignment.Center,
            .VerticalAlignment = VerticalAlignment.Center}

        Interface_LoaderText = UI_TextBlock_SubTitle
        Interface_LoaderProgressBar = UI_ProgressBar
        With BeatmapWrapper.Children
            .Clear()
            .Add(UI_ProgressBar)
            .Add(UI_ProgressRing)
            .Add(UI_TextBlock_SubTitle)
        End With
    End Sub

    Sub UI_SetStatus(Optional Message As String = "", Optional DoRecord As Boolean = False)
        If DoRecord Then    ' Keep previous statuses as tooltip
            Dim cTag As List(Of String)
            If La_FooterProg.Tag Is Nothing Then
                cTag = New List(Of String)
            Else
                cTag = CType(La_FooterProg.Tag, List(Of String))
            End If
            cTag.Add(Date.Now.ToString(_e("MainWindow_timeFormat")) & " | " & Message)
            If cTag.Count > 10 Then cTag.RemoveRange(0, cTag.Count - 10)

            Dim SB As New Text.StringBuilder
            For Each item As String In cTag
                SB.Append(item & vbNewLine)
            Next
            La_FooterProg.Tag = cTag
            La_FooterProg.ToolTip = SB.ToString(0, SB.Length - vbNewLine.Length) ' Remove last vbNewLine
        End If
        La_FooterProg.Content = Message
    End Sub

    Sub UI_ShowBmDP(ID As Integer, Details As BmDPDetails)
        BeatmapDetails_Artist.Text = Details.Artist
        Bu_BmDetailsListing.Tag = ID
        BeatmapDetails_Creator.Text = Details.Creator
        BeatmapDetails_Title.Text = Details.Title

        ' IsUnplayed status
        If Details.IsUnplayed Then
            With BeatmapDetails_IsUnplayed
                .Background = StandardColors.RedLight
                .Text = _e("MainWindow_detailsPanel_playedStatus_unplayed")
            End With
        Else
            With BeatmapDetails_IsUnplayed
                .Background = StandardColors.GreenDark
                .Text = _e("MainWindow_detailsPanel_playedStatus_played")
            End With
        End If

        ' Ranked status
        Select Case Details.RankedStatus
            Case Convert.ToByte(4), Convert.ToByte(5)   ' 4 = Ranked, 5 = Approved
                With BeatmapDetails_RankedStatus
                    .Background = StandardColors.GreenDark
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_ranked")
                End With
            Case Convert.ToByte(6)      ' Pending
                With BeatmapDetails_RankedStatus
                    .Background = StandardColors.PurpleDark
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_pending")
                End With
            Case Else
                With BeatmapDetails_RankedStatus
                    .Background = StandardColors.GrayLight
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_unranked")
                End With
        End Select

        ' Thumbnail
        Dim ThumbPath As String = ""
        If File.Exists(AppSettings.osu_Path & "\Data\bt\" & ID & "l.jpg") Then
            ThumbPath = (AppSettings.osu_Path & "\Data\bt\" & ID & "l.jpg")
        ElseIf File.Exists(AppTempPath & "\Cache\Thumbnails\" & ID & ".jpg") Then
            ThumbPath = (AppTempPath & "\Cache\Thumbnails\" & ID & ".jpg")
        End If
        If Not ThumbPath = "" Then
            Try
                BeatmapDetails_Thumbnail.Source = New BitmapImage(New Uri(ThumbPath))
            Catch ex As NotSupportedException
                BeatmapDetails_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
            End Try
        Else
            BeatmapDetails_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
        End If

        ' Api
        If AppSettings.Api_Enabled_BeatmapPanel And Not ID = -1 Then
            If BmDP_Client.IsBusy Then BmDP_Client.CancelAsync()
            ' Reset
            BeatmapDetails_APIFavouriteCount.Text = "..."
            BeatmapDetails_APIFunctions.Visibility = Visibility.Visible
            BeatmapDetails_APIPassCount.Text = "..."
            BeatmapDetails_APIPlayCount.Text = "..."
            BeatmapDetails_APIProgress.Visibility = Visibility.Visible
            BeatmapDetails_RankedStatus.Text = "..."
            BeatmapDetails_APIWarn.Visibility = Visibility.Collapsed

            Try
                UI_SetStatus(_e("MainWindow_fetching").Replace("%0", CStr(ID)), True)
                BmDP_Client.DownloadStringAsync(New Uri(WebOsuApiRoot & "get_beatmaps?k=" & AppSettings.Api_Key & "&s=" & ID))
            Catch ex As NotSupportedException
                With BeatmapDetails_APIWarn
                    .Content = _e("MainWindow_detailsPanel_generalError")
                    .Visibility = Visibility.Visible
                End With
            End Try
        Else
            BeatmapDetails_APIFunctions.Visibility = Visibility.Collapsed
        End If

        Flyout_BmDetails.IsOpen = True
    End Sub

    Shared Sub UI_ShowSettingsWindow(Optional ByVal SelectedIndex As Integer = 0)
        Dim Window_Settings As New Window_Settings
        Window_Settings.TC_Main.SelectedIndex = SelectedIndex
        Window_Settings.ShowDialog()
    End Sub

    Shared Sub UI_ShowUpdaterWindow()
        Dim Window_Updater As New Window_Updater
        Window_Updater.ShowDialog()
    End Sub
#End Region

    Sub UpdateCheck()
        La_FooterUpdater.Content = _e("MainWindow_checkingForUpdates")
        Dim UpdateClient As New WebClient
        UpdateClient.DownloadStringAsync(New Uri(WebNw520ApiRoot & "/app/updater.latestVersion.json"))
        AddHandler UpdateClient.DownloadStringCompleted, AddressOf UpdateClient_DownloadStringCompleted
        AppSettings.Tool_LastCheckForUpdates = Date.Now.ToString("yyyyMMddhhmmss")
        AppSettings.SaveSettings()
    End Sub

    Sub UpdateClient_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs)
        Dim Answer As JObject
        Try
            Answer = JObject.Parse(e.Result)
        Catch ex As JsonReaderException
            If AppSettings.Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine &
                       "> " & _e("MainWindow_invalidServerResponse") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, MsgTitleDisableable)
                MsgBox(e.Result, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            End If
            La_FooterUpdater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        Catch ex As Reflection.TargetInvocationException
            If AppSettings.Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine &
                       "> " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, MsgTitleDisableable)
            End If
            La_FooterUpdater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        End Try

        Dim LatestVer As String = CStr(Answer.SelectToken("latestRepoRelease").SelectToken("tag_name"))
        If LatestVer = My.Application.Info.Version.ToString Then
            La_FooterUpdater.Content = _e("MainWindow_latestVersion")
        Else
            La_FooterUpdater.Content = _e("MainWindow_updateAvailable").Replace("%0", LatestVer)
            BalloonShow(_e("MainWindow_aNewVersionIsAvailable").Replace("%0", My.Application.Info.Version.ToString).Replace("%1", LatestVer), , , NotifyNextAction.OpenUpdater)
            If AppSettings.Messages_Updater_OpenUpdater Then UI_ShowUpdaterWindow()
        End If
    End Sub

#Region "BGW_SyncGetIDs"
    Sub BGW_SyncGetIDs_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles BGW_SyncGetIDs.DoWork
        Dim Arguments As New BGWcallback_SyncGetIDs
        Arguments = TryCast(e.Argument, BGWcallback_SyncGetIDs)
        Dim Answer As New BGWcallback_SyncGetIDs

        If Not Directory.Exists(AppSettings.osu_SongsPath) Then
            Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist
            e.Result = Answer
            Exit Sub
        End If

        Select Case Arguments.Arg__Mode
            Case BGWcallback_SyncGetIDs.ArgModes.Sync
                BGW_SyncGetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                                    .Progress__CurrentAction = BGWcallback_SyncGetIDs.ProgressCurrentActions.CountingTotalFolders,
                                    .Progress__Current = Directory.GetDirectories(AppSettings.osu_SongsPath).Count})

                Dim Beatmap_InvalidFolder As String = ""
                Dim Beatmap_InvalidIDBeatmaps As String = ""
                If File.Exists(AppSettings.osu_Path + "\osu!.db") Then
                    Dim DatabasePath As String = AppSettings.osu_Path + "\osu!.db"
                    Try
                        Answer.Return__Sync_BmDic_Installed = ReadBmsFromDb(DatabasePath)
                    Catch ex As IOException
                        Try
                            Answer.Return__Sync_BmDic_Installed = ReadBmsFromDb(DatabasePath, True)
                        Catch ex_ As Exception
                            If MessageBox.Show(_e("MainWindow_unableToReadBms") & " " & _e("MainWindow_fallback"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) = MessageBoxResult.Yes Then
                                Answer = ReadBmsFromDir(AppSettings.osu_SongsPath)
                            Else
                                Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled
                            End If
                        End Try
                    Catch ex_ As Exception
                        If MessageBox.Show(_e("MainWindow_unableToReadBms") & " " & _e("MainWindow_fallback"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) = MessageBoxResult.Yes Then
                            Answer = ReadBmsFromDir(AppSettings.osu_SongsPath)
                        Else
                            Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled
                        End If
                    End Try
                Else
                    If Directory.Exists(AppSettings.osu_SongsPath) Then
                        If MessageBox.Show(_e("MainWindow_unableToReadBms") & " " & _e("MainWindow_fallback"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) = MessageBoxResult.Yes Then
                            Answer = ReadBmsFromDir(AppSettings.osu_SongsPath)
                        Else
                            Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled
                        End If
                    Else
                        Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Exception
                    End If
                End If
                e.Result = Answer
        End Select
    End Sub

    Sub BGW_SyncGetIDs_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles BGW_SyncGetIDs.ProgressChanged
        Dim Answer As BGWcallback_SyncGetIDs = CType(e.UserState, BGWcallback_SyncGetIDs)
        Select Case Answer.Progress__CurrentAction
            Case BGWcallback_SyncGetIDs.ProgressCurrentActions.Sync
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Progress__Current.ToString) & vbNewLine & _e("MainWindow_andStillWorking")
                With Interface_LoaderProgressBar
                    .Value = Answer.Progress__Current
                    .Visibility = Visibility.Visible
                End With
            Case BGWcallback_SyncGetIDs.ProgressCurrentActions.Done
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsInTotalParsed").Replace("%0", Answer.Progress__Current.ToString) & vbNewLine & _e("MainWindow_generatingInterface")
            Case BGWcallback_SyncGetIDs.ProgressCurrentActions.CountingTotalFolders
                Interface_LoaderProgressBar.Maximum = Answer.Progress__Current
        End Select
    End Sub

    Sub BGW_SyncGetIDs_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles BGW_SyncGetIDs.RunWorkerCompleted
        Dim Answer As BGWcallback_SyncGetIDs = TryCast(e.Result, BGWcallback_SyncGetIDs)
        Select Case Answer.Return__Status
            Case BGWcallback_SyncGetIDs.ReturnStatuses.Success
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Return__Sync_BmDic_Installed.Count.ToString)
                If Not Answer.Return__Sync_Warnings = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapsDifferFromNormal") & vbNewLine &
                                       _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        With Window_Message
                            .SetMessage(Answer.Return__Sync_Warnings, _e("MainWindow_exceptions"), "Sync")
                            .ShowDialog()
                        End With
                    End If
                End If
                BmDic_Installed = Answer.Return__Sync_BmDic_Installed

                Sync_Done = True
                BmDisplayUpdate(BmDic_Installed)
                OverlayShow(_e("MainWindow_syncCompleted"), "")
                OverlayFadeOut()

                If Sync_Done_ImporterRequest Then
                    Sync_Done_ImporterRequest = False
                    BmDisplayUpdate(Sync_Done_ImporterRequest_SaveValue, UpdateBmDisplayDestinations.Importer)
                End If
            Case BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled,
                 BGWcallback_SyncGetIDs.ReturnStatuses.Exception
                Dim UI_TextBlock As New TextBlock With {
                    .FontSize = 72,
                    .Foreground = StandardColors.GrayLighter,
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .Margin = New Thickness(0, 100, 0, 0),
                    .Text = _e("MainWindow_lastSyncFailed"),
                    .VerticalAlignment = VerticalAlignment.Center}
                Dim UI_TextBlock_SubTitle As New TextBlock With {
                    .FontSize = 24,
                    .Foreground = StandardColors.GrayLighter,
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .Text = _e("MainWindow_pleaseRetry"),
                    .VerticalAlignment = VerticalAlignment.Center}

                With BeatmapWrapper.Children
                    .Clear()
                    .Add(UI_TextBlock)
                    .Add(UI_TextBlock_SubTitle)
                End With
                Bu_SyncRun.IsEnabled = True
            Case BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist
                Dim UI_TextBlock As New TextBlock With {
                    .FontSize = 72,
                    .Foreground = StandardColors.GrayLighter,
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .Margin = New Thickness(0, 100, 0, 0),
                    .Text = _e("MainWindow_lastSyncFailed"),
                    .VerticalAlignment = VerticalAlignment.Center}
                Dim UI_TextBlock_SubTitle As New TextBlock With {
                    .FontSize = 24,
                    .Foreground = StandardColors.GrayLighter,
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .Text = _e("MainWindow_pleaseRetry"),
                    .VerticalAlignment = VerticalAlignment.Center}

                With BeatmapWrapper.Children
                    .Clear()
                    .Add(UI_TextBlock)
                    .Add(UI_TextBlock_SubTitle)
                End With
                MsgBox(_e("MainWindow_unableToFindOsuFolderPleaseSpecify"), MsgBoxStyle.Critical, AppName)
                UI_ShowSettingsWindow(1)
                Bu_SyncRun.IsEnabled = True
        End Select
    End Sub

    Function ReadBmsFromDb(DbPath As String, Optional Legacy As Boolean = False) As Dictionary(Of Integer, Beatmap)
        Dim FoundBms As New Dictionary(Of Integer, Beatmap)
        Using Reader As OsuReader = New OsuReader(File.OpenRead(DbPath))
            ' More details: http://j.mp/1PIyjCY
            Reader.ReadInt32()                                          ' osu! version (e.g. 20150203) 
            Reader.ReadInt32()                                          ' Folder Count 
            Reader.ReadBoolean()                                        ' AccountUnlocked (only false when the account is locked or banned in any way)
            Reader.ReadDate()                                           ' Date the account will be unlocked 
            Reader.ReadString()                                         ' Player name 
            Dim BeatmapCount As Integer = Reader.ReadInt32()            ' Number of beatmaps 
            For i = 1 To BeatmapCount
                Dim BeatmapDetails As New Beatmap
                If Not Legacy Then Reader.ReadInt32()                   ' Unknown 
                BeatmapDetails.Artist = Reader.ReadString()             ' Artist name
                Reader.ReadString()                                     '  Artist name, in Unicode
                BeatmapDetails.Title = Reader.ReadString()              ' Song title
                Reader.ReadString()                                     '  Song title, in Unicode
                BeatmapDetails.Creator = Reader.ReadString()            ' Creator name
                Reader.ReadString()                                     '  Difficulty (e.g. Hard, Insane, etc.)
                Reader.ReadString()                                     '  Audio file name
                BeatmapDetails.MD5 = Reader.ReadString()                ' MD5 hash of the beatmap
                Reader.ReadString()                                     '  Name of the .osu file corresponding to this beatmap
                BeatmapDetails.RankedStatus = Reader.ReadByte           ' Ranked status
                Reader.ReadBytes(38)                                    '  Other data No. of Circles/Sliders/Spinners, Last Edit, Settings etc.
                For j = 1 To 4                                          '  Star difficulties with various mods
                    Dim Count = Reader.ReadInt32()
                    If Count < 0 Then Continue For
                    For k = 1 To Count
                        Reader.ReadBytes(14)
                    Next
                Next
                Reader.ReadBytes(12)                                    '  Drain/Total/Preview Time
                Dim TimingPointCount = Reader.ReadInt32() 'You could probably optimise these loops. Reader.ReadBytes(Count*17) maybe. I don't have the time to test it.
                For j = 1 To TimingPointCount
                    Reader.ReadBytes(17)
                Next
                Reader.ReadInt32()                                      '  Beatmap ID
                BeatmapDetails.ID = Reader.ReadInt32()                  ' Beatmap set ID
                Reader.ReadInt32()                                      '  Thread ID
                Reader.ReadBytes(11)
                Reader.ReadString()                                     '  Song source
                BeatmapDetails.SongTags = Reader.ReadString()           ' Song tags
                Reader.ReadInt16()                                      '  Online offset 
                Reader.ReadString()                                     '  Font used for the title of the song 
                BeatmapDetails.IsUnplayed = Reader.ReadBoolean()        ' Is unplayed
                Reader.ReadBytes(9)
                Reader.ReadString()                                     '  Folder name of the beatmap, relative to Songs folder 
                Reader.ReadBytes(18)

                If Not FoundBms.ContainsKey(BeatmapDetails.ID) Then
                    FoundBms.Add(BeatmapDetails.ID, BeatmapDetails)
                    BGW_SyncGetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                        .Progress__Current = FoundBms.Count})
                End If
            Next
        End Using
        Return FoundBms
    End Function

    Function ReadBmsFromDir(DirPath As String, Optional Old As Boolean = False) As BGWcallback_SyncGetIDs
        Dim Answer As New BGWcallback_SyncGetIDs With {
            .Func_Invalid = New List(Of String),
            .Func_InvalidId = New List(Of String)}
        For Each DirectoryList As String In Directory.GetDirectories(DirPath)
            Dim DirectoryInfo As New DirectoryInfo(DirectoryList)
            If Not DirectoryInfo.Name.ToLower = "failed" And Not DirectoryInfo.Name.ToLower = "tutorial" Then
                Dim FoundFile As Boolean = False
                For Each FileInDir In Directory.GetFiles(DirectoryList)
                    If Path.GetExtension(FileInDir) = ".osu" Then
                        FoundFile = True
                        ' Read File
                        Dim FileReader As New StreamReader(FileInDir)
                        Dim TextLines As New List(Of String)
                        Dim BeatmapDetails As New Beatmap
                        Dim Found_ID As Boolean = False          ' Cause the older osu! file format doesn't include the set ID
                        Dim Found_Title As Boolean = False
                        Dim Found_Artist As Boolean = False
                        Dim Found_Creator As Boolean = False
                        Do While FileReader.Peek() <> -1 And TextLines.Count <= 50  ' Don't read more than 50 lines
                            TextLines.Add(FileReader.ReadLine())
                        Loop
                        For Each Line As String In TextLines
                            If Found_ID And Found_Title And Found_Artist And Found_Creator Then Exit For
                            If Line.StartsWith("Title:") Then
                                Found_Title = True
                                BeatmapDetails.Title = Line.Substring(6)
                            ElseIf Line.StartsWith("Artist:") Then
                                Found_Artist = True
                                BeatmapDetails.Artist = Line.Substring(7)
                            ElseIf Line.StartsWith("BeatmapSetID:") Then
                                Found_ID = True
                                Try
                                    BeatmapDetails.ID = CInt(Line.Substring(13))
                                Catch ex As InvalidCastException
                                    BeatmapDetails.ID = -1
                                    Found_ID = False
                                End Try
                            ElseIf Line.StartsWith("Creator:") Then
                                Found_Creator = True
                                BeatmapDetails.Creator = Line.Substring(8)
                            End If
                        Next
                        If Found_ID = False Then
                            ' Looks like it's an old file, so try to get ID from folder name
                            Try
                                BeatmapDetails.ID = CInt(DirectoryInfo.Name.Substring(0, DirectoryInfo.Name.IndexOf(" ")))
                            Catch ex As Exception
                                BeatmapDetails.ID = -1
                                Answer.Func_InvalidId.Add(BeatmapDetails.ID & " | " & BeatmapDetails.Artist & " | " & BeatmapDetails.Title)
                            End Try
                        Else
                            If Not Answer.Return__Sync_BmDic_Installed.ContainsKey(BeatmapDetails.ID) Then Answer.Return__Sync_BmDic_Installed.Add(BeatmapDetails.ID, BeatmapDetails)
                            BGW_SyncGetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                            .Progress__Current = Answer.Return__Sync_BmDic_Installed.Count})
                            Exit For
                        End If
                    End If
                Next

                ' Can't read/find osu! file
                If FoundFile = False Then
                    Try
                        Dim Beatmap_ID As String = DirectoryInfo.Name.Substring(0, DirectoryInfo.Name.IndexOf(" "))
                        Dim Beatmap_Artist As String = DirectoryInfo.Name.Substring(Beatmap_ID.Length + 1, DirectoryInfo.Name.IndexOf(" - ") - Beatmap_ID.Length - 1)
                        Dim Beatmap_Name As String = DirectoryInfo.Name.Substring(Beatmap_ID.Length + Beatmap_Artist.Length + 4)
                        Dim CurrentBeatmap As New Beatmap With {
                            .ID = CInt(Beatmap_ID),
                            .Title = Beatmap_Name,
                            .Artist = Beatmap_Artist}
                        If Not Answer.Return__Sync_BmDic_Installed.ContainsKey(CurrentBeatmap.ID) Then Answer.Return__Sync_BmDic_Installed.Add(CurrentBeatmap.ID, CurrentBeatmap)
                    Catch ex As Exception
                        Answer.Func_Invalid.Add(DirectoryInfo.Name)
                    End Try
                End If
            End If
        Next
        If Not Answer.Func_Invalid.Count = 0 Then
            Dim SB As New Text.StringBuilder
            SB.Append("# " & _e("MainWindow_ignoredFolders") & vbNewLine & _e("MainWindow_folderCouldntBeParsed") & vbNewLine & vbNewLine &
                      "> " & _e("MainWindow_folders") & ":" & vbNewLine)
            For Each Item As String In Answer.Func_Invalid
                SB.Append("* " & Item & vbNewLine)
            Next
            SB.Append(vbNewLine & vbNewLine)
            Answer.Return__Sync_Warnings += SB.ToString
        End If
        If Not Answer.Func_InvalidId.Count = 0 Then
            Dim SB As New Text.StringBuilder
            SB.Append("# " & _e("MainWindow_unableToGetId") & vbNewLine & _e("MainWindow_unableToGetIdOfSomeBeatmapsTheyllBeHandledAsUnsubmitted") & vbNewLine & vbNewLine &
                      "> " & _e("MainWindow_beatmaps") & ":" & vbNewLine)
            For Each Item As String In Answer.Func_InvalidId
                SB.Append("* " & Item & vbNewLine)
            Next
            SB.Append(vbNewLine & vbNewLine)
            Answer.Return__Sync_Warnings += SB.ToString
        End If
        Return Answer
    End Function
#End Region

#Region "Exporter"
    Sub Bu_ExporterCancel_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ExporterCancel.Click
        TI_Exporter.Visibility = Visibility.Collapsed
        TC_Main.SelectedIndex = 0
        SP_ExporterWrapper.Children.Clear()
    End Sub

    Sub Bu_ExporterInvertSel_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ExporterInvertSel.Click
        ' Copy data before manipulation
        Dim ListSelected As List(Of Importer.TagData) = Exporter_BmList_SelectedTags.ToList
        Dim ListUnselected As List(Of Importer.TagData) = Exporter_BmList_UnselectedTags.ToList
        ' Loop for selected elements
        Dim i As Integer = 0
        Do While i < ListSelected.Count
            ListSelected(i).UI_Checkbox_IsSelected.IsChecked = False
            i += 1
        Loop
        ' Loop for unselected elements
        i = 0
        Do While i < ListUnselected.Count
            ListUnselected(i).UI_Checkbox_IsSelected.IsChecked = True
            i += 1
        Loop
    End Sub

    Sub Bu_ExporterRun_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ExporterRun.Click
        Dim Result As New Dictionary(Of Integer, Beatmap)
        For Each Item As Importer.TagData In Exporter_BmList_SelectedTags
            Result.Add(Item.Beatmap.ID, Item.Beatmap)
        Next
        Exporter_ExportBmDialog(Result, "Export selected beatmaps")
        TI_Exporter.Visibility = Visibility.Collapsed
        TC_Main.SelectedIndex = 0
        SP_ExporterWrapper.Children.Clear()
    End Sub

    Sub Exporter_AddBmToSel(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        Exporter_BmList_UnselectedTags.Remove(Csender_Tag)
        Exporter_BmList_SelectedTags.Add(Csender_Tag)
        If Exporter_BmList_SelectedTags.Count > 0 Then Bu_ExporterRun.IsEnabled = True Else Bu_ExporterRun.IsEnabled = False
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GreenLight
    End Sub

    Sub Exporter_DetermineWheterAddOrRemove(sender As Object, e As EventArgs)
        Dim Cparent As Grid
        Dim Csender_Tag As Importer.TagData
        If TypeOf sender Is Image Then
            Cparent = CType(CType(sender, Image).Parent, Grid)
        ElseIf TypeOf sender Is Rectangle Then
            Cparent = CType(CType(sender, Rectangle).Parent, Grid)
        ElseIf TypeOf sender Is TextBlock Then
            Cparent = CType(CType(sender, TextBlock).Parent, Grid)
        Else
            Exit Sub
        End If
        Csender_Tag = CType(Cparent.Tag, Importer.TagData)

        If Csender_Tag.UI_Checkbox_IsSelected.IsChecked Then
            Exporter_BmList_SelectedTags.Remove(Csender_Tag)
            If Exporter_BmList_SelectedTags.Count = 0 Then Bu_ExporterRun.IsEnabled = False
            With Csender_Tag
                .UI_Checkbox_IsSelected.IsChecked = False
                .UI_DecoBorderLeft.Fill = StandardColors.GrayLight
            End With
        Else
            Exporter_BmList_SelectedTags.Add(Csender_Tag)
            If Exporter_BmList_SelectedTags.Count > 0 Then Bu_ExporterRun.IsEnabled = True Else Bu_ExporterRun.IsEnabled = False
            With Csender_Tag
                .UI_Checkbox_IsSelected.IsChecked = True
                .UI_DecoBorderLeft.Fill = StandardColors.GreenLight
            End With
        End If
    End Sub

    Sub Exporter_RemoveBmFromSel(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        Exporter_BmList_SelectedTags.Remove(Csender_Tag)
        Exporter_BmList_UnselectedTags.Add(Csender_Tag)
        If Exporter_BmList_SelectedTags.Count = 0 Then Bu_ExporterRun.IsEnabled = False
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GrayLight
    End Sub
#End Region

#Region "Importer"
    Sub Bu_ImporterCancel_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ImporterCancel.Click
        TC_Main.SelectedIndex = 0
        TI_Importer.Visibility = Visibility.Collapsed
        SP_ImporterWrapper.Children.Clear()
        ImporterContainer = Nothing
    End Sub

    Sub Bu_ImporterRun_Click(sender As Object, e As RoutedEventArgs) Handles Bu_ImporterRun.Click
        Importer_Init()
    End Sub

    Sub CB_ImporterHideInstalled_Checked(sender As Object, e As RoutedEventArgs) Handles CB_ImporterHideInstalled.Checked
        For Each _Selection As Grid In SP_ImporterWrapper.Children
            If CType(_Selection.Tag, Importer.TagData).IsInstalled Then
                _Selection.Visibility = Visibility.Collapsed
            End If
        Next
    End Sub

    Sub CB_ImporterHideInstalled_Unchecked(sender As Object, e As RoutedEventArgs) Handles CB_ImporterHideInstalled.Unchecked
        For Each _Selection As Grid In SP_ImporterWrapper.Children
            _Selection.Visibility = Visibility.Visible
        Next
    End Sub

    Sub Importer_AddBmToSel(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)   ' Get Tag from parent Grid
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        ImporterContainer.BmList_TagsToInstall.Add(Csender_Tag)
        ImporterContainer.BmList_TagsLeftOut.Remove(Csender_Tag)

        If ImporterContainer.BmList_TagsToInstall.Count > 0 Then
            Bu_ImporterRun.IsEnabled = True
            Bu_ImporterCancel.IsEnabled = True
        Else
            Bu_ImporterRun.IsEnabled = False
        End If
        Importer_UpdateInfo()
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.RedLight
    End Sub

    Sub Importer_Downloader_DownloadFileCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs)
        ImporterContainer.Counter += 1
        If File.Exists(AppTempPath & "\Downloads\Beatmaps\" & ImporterContainer.CurrentFileName) Then
            If My.Computer.FileSystem.GetFileInfo(AppTempPath & "\Downloads\Beatmaps\" & ImporterContainer.CurrentFileName).Length <= 3000 Then     ' Detect "Beatmap Not Found" pages
                ' File Empty
                ImporterContainer.BmList_TagsToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
                Try
                    File.Delete(AppTempPath & "\Downloads\Beatmaps\" & ImporterContainer.CurrentFileName)
                Catch ex As IOException
                End Try
                ImporterContainer.BmList_TagsFailed.Add(ImporterContainer.BmList_TagsToInstall.First)
                ImporterContainer.BmList_TagsToInstall.Remove(ImporterContainer.BmList_TagsToInstall.First)
                Importer_Downloader_ToNextDownload()
            Else    ' File Normal
                ImporterContainer.BmList_TagsToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.PurpleDark
                ImporterContainer.BmList_TagsDone.Add(ImporterContainer.BmList_TagsToInstall.First)
                ImporterContainer.BmList_TagsToInstall.Remove(ImporterContainer.BmList_TagsToInstall.First)
                Importer_Downloader_ToNextDownload()
            End If
        Else
            ' File Empty
            ImporterContainer.BmList_TagsToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
            ImporterContainer.BmList_TagsFailed.Add(ImporterContainer.BmList_TagsToInstall.First)
            ImporterContainer.BmList_TagsToInstall.Remove(ImporterContainer.BmList_TagsToInstall.First)
            Importer_Downloader_ToNextDownload()
        End If
    End Sub

    Sub Importer_Downloader_DownloadFinished()
        With PB_ImporterProg
            .IsIndeterminate = True
            .Value = 0
        End With

        Importer_UpdateInfo(_e("MainWindow_installing"))
        UI_SetStatus(_e("MainWindow_installingFiles"), True)

        For Each FilePath In Directory.GetFiles(AppTempPath & "\Downloads\Beatmaps")
            If Not File.Exists(AppSettings.osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then File.Move(FilePath, AppSettings.osu_SongsPath & "\" & Path.GetFileName(FilePath)) Else File.Delete(FilePath)
        Next
        With PB_ImporterProg
            .IsIndeterminate = False
            .Visibility = Visibility.Hidden
        End With

        UI_SetStatus(_e("MainWindow_finished"))
        Importer_UpdateInfo(_e("MainWindow_finished"))

        If ImporterContainer.BmList_TagsFailed.Count > 0 Then
            Dim Failed As String = "# " & _e("MainWindow_downloadFailed") & vbNewLine & _e("MainWindow_cantDownload") & vbNewLine & vbNewLine &
                "> " & _e("MainWindow_beatmaps") & ": "
            For Each _Selection As Importer.TagData In ImporterContainer.BmList_TagsFailed
                Failed += vbNewLine & "* " & _Selection.Beatmap.ID.ToString & " / " & _Selection.Beatmap.Artist & " / " & _Selection.Beatmap.Title
            Next
            If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenImported") & vbNewLine &
                               _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                Dim Window_Message As New Window_MessageWindow
                Window_Message.SetMessage(Failed, _e("MainWindow_downloadFailed"), "Import")
                Window_Message.ShowDialog()
            End If
        End If

        BalloonShow(_e("MainWindow_installationFinished") & vbNewLine &
                    _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString) & vbNewLine &
                    _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BmList_TagsFailed.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString))
        MsgBox(_e("MainWindow_installationFinished") & vbNewLine &
                    _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString) & vbNewLine &
                    _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BmList_TagsFailed.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString) & vbNewLine & vbNewLine &
                _e("MainWindow_pressF5"))

        If AppSettings.Messages_Importer_AskOsu AndAlso Not Process.GetProcessesByName("osu!").Count > 0 AndAlso MessageBox.Show(_e("MainWindow_doYouWantToStartOsuNow"), MsgTitleDisableable, MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then StartOrFocusOsu()
        Bu_SyncRun.IsEnabled = True
        Bu_ImporterCancel.IsEnabled = True
    End Sub

    Sub Importer_Downloader_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs)
        PB_ImporterProg.Value = e.ProgressPercentage
    End Sub

    Sub Importer_Downloader_ToNextDownload()
        If ImporterContainer.BmList_TagsToInstall.Count > 0 Then
            If Not AppSettings.Tool_Importer_AutoInstallCounter = 0 And AppSettings.Tool_Importer_AutoInstallCounter <= ImporterContainer.Counter Then  ' Install file if necessary
                ImporterContainer.Counter = 0
                With PB_ImporterProg
                    .IsIndeterminate = True
                End With

                Importer_UpdateInfo(_e("MainWindow_installing"))
                UI_SetStatus(_e("MainWindow_installingFiles"), True)

                For Each FilePath In Directory.GetFiles(AppTempPath & "\Downloads\Beatmaps")
                    If Not File.Exists(AppSettings.osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then
                        Try
                            File.Move(FilePath, AppSettings.osu_SongsPath & "\" & Path.GetFileName(FilePath))
                        Catch ex As IOException
                            MsgBox("Unable to install beatmap '" & Path.GetFileName(FilePath) & "'.", MsgBoxStyle.Critical, "Debug | " & AppName)
                        End Try
                    Else
                        File.Delete(FilePath)
                    End If
                Next
            End If
            Importer_DownloadBeatmap()
        Else    ' Finished
            Importer_Downloader_DownloadFinished()
        End If
    End Sub

    Sub Importer_DownloadBeatmap()
        Dim RequestURI As String

        With PB_ImporterProg
            .Value = 0
            .IsIndeterminate = True
        End With
        UI_SetStatus(_e("MainWindow_fetching").Replace("%0", CStr(ImporterContainer.BmList_TagsToInstall.First.Beatmap.ID)), True)
        TB_ImporterMirror.Text = _e("MainWindow_downloadMirror") & ": " & Application_Mirrors(AppSettings.Tool_DownloadMirror).DisplayName
        RequestURI = Application_Mirrors(AppSettings.Tool_DownloadMirror).DownloadURL.Replace("%0", CStr(ImporterContainer.BmList_TagsToInstall.First.Beatmap.ID))

        With ImporterContainer.BmList_TagsToInstall.First
            .UI_DecoBorderLeft.Fill = StandardColors.BlueLight
            .UI_Checkbox_IsSelected.IsEnabled = False
            .UI_Checkbox_IsSelected.IsThreeState = False
            .UI_Checkbox_IsSelected.IsChecked = Nothing
        End With

        Importer_UpdateInfo(_e("MainWindow_fetching1"))

        Dim req As HttpWebRequest = DirectCast(WebRequest.Create(RequestURI), HttpWebRequest)
        Dim Res As WebResponse
        Try
            Res = req.GetResponse()
        Catch ex As WebException
            If ImporterContainer.Pref_FetchFail_SkipAlways Then
                Importer_FetchFail_ToNext()
            Else
                Dim Win_GenericMsgBox As New Window_GenericMsgBox(_e("MainWindow_unableToFetchMirrorData"), New List(Of Window_GenericMsgBox.MsgBoxButtonHolder) From {
                    New Window_GenericMsgBox.MsgBoxButtonHolder(_e("Global_buttons_skip"), Window_GenericMsgBox.MsgBoxResult.Yes),
                    New Window_GenericMsgBox.MsgBoxButtonHolder(_e("Global_buttons_skipAlways"), Window_GenericMsgBox.MsgBoxResult.YesAll),
                    New Window_GenericMsgBox.MsgBoxButtonHolder(_e("Global_buttons_cancel"), Window_GenericMsgBox.MsgBoxResult.Cancel)
                },, System.Drawing.SystemIcons.Exclamation)
                Win_GenericMsgBox.ShowDialog()
                Select Case Win_GenericMsgBox.Result
                    Case Window_GenericMsgBox.MsgBoxResult.Yes
                        Importer_FetchFail_ToNext()
                    Case Window_GenericMsgBox.MsgBoxResult.YesAll
                        ImporterContainer.Pref_FetchFail_SkipAlways = True
                        Importer_FetchFail_ToNext()
                    Case Window_GenericMsgBox.MsgBoxResult.Cancel, Window_GenericMsgBox.MsgBoxResult.None
                        ImporterContainer.BmList_TagsToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
                        TB_ImporterInfo.Text = _e("MainWindow_installing") &
                            " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString)
                        If ImporterContainer.BmList_TagsLeftOut.Count > 0 Then TB_ImporterInfo.Text += " | " & _e("MainWindow_leftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString)
                        TB_ImporterInfo.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString)
                        UI_SetStatus(_e("MainWindow_installingFiles"), True)
                        For Each FilePath In Directory.GetFiles(AppTempPath & "\Downloads\Beatmaps")
                            File.Move(FilePath, AppSettings.osu_SongsPath & "\" & Path.GetFileName(FilePath))
                        Next
                        With PB_ImporterProg
                            .IsIndeterminate = False
                            .Visibility = Visibility.Hidden
                        End With
                        UI_SetStatus(_e("MainWindow_aborted"))
                        TB_ImporterInfo.Text = _e("MainWindow_aborted") &
                            " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString)
                        If ImporterContainer.BmList_TagsLeftOut.Count > 0 Then TB_ImporterInfo.Text += " | " & _e("MainWindow_leftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString)
                        TB_ImporterInfo.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString)
                        Bu_SyncRun.IsEnabled = True
                        Bu_ImporterRun.IsEnabled = True
                        Bu_ImporterCancel.IsEnabled = True
                End Select
            End If
            Exit Sub
        End Try
        Dim response As WebResponse
        response = req.GetResponse
        response.Close()

        If response.Headers("Content-Disposition") <> Nothing Then
            ImporterContainer.CurrentFileName = response.Headers("Content-Disposition").Substring(response.Headers("Content-Disposition").IndexOf("filename=") + 10).Replace("""", "")
            If ImporterContainer.CurrentFileName.Substring(ImporterContainer.CurrentFileName.Length - 1) = ";" Then ImporterContainer.CurrentFileName = ImporterContainer.CurrentFileName.Substring(0, ImporterContainer.CurrentFileName.Length - 1)
            If ImporterContainer.CurrentFileName.Contains("; filename*=UTF-8") Then ImporterContainer.CurrentFileName = ImporterContainer.CurrentFileName.Substring(0, ImporterContainer.CurrentFileName.IndexOf(".osz") + 4)
        Else
            ImporterContainer.CurrentFileName = CStr(ImporterContainer.BmList_TagsToInstall.First.Beatmap.ID) & ".osz"
        End If
        ImporterContainer.CurrentFileName = PathSanitize(ImporterContainer.CurrentFileName)   ' Issue #23: Replace invalid characters

        UI_SetStatus(_e("MainWindow_downloading").Replace("%0", CStr(ImporterContainer.BmList_TagsToInstall.First.Beatmap.ID)), True)
        Importer_UpdateInfo(_e("MainWindow_downloading1"))
        PB_ImporterProg.IsIndeterminate = False
        ImporterContainer.Downloader.DownloadFileAsync(New Uri(RequestURI), (AppTempPath & "\Downloads\Beatmaps\" & ImporterContainer.CurrentFileName))
    End Sub

    Sub Importer_DownloadThumb(sender As Object, e As MouseButtonEventArgs)
        Dim Csender As Image = CType(sender, Image)
        Dim Cparent As Grid = CType(Csender.Parent, Grid)
        Dim Csender_Bm As Importer.TagData = CType(Cparent.Tag, Importer.TagData)

        Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/ProgressThumbnail.png", UriKind.Relative))
        RemoveHandler(Csender_Bm.UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
        RemoveHandler(Csender_Bm.UI_Thumbnail.MouseRightButtonUp), AddressOf BmDP_Show
        AddHandler(Csender_Bm.UI_Thumbnail.MouseDown), AddressOf BmDP_Show
        Directory.CreateDirectory(AppTempPath & "\Cache\Thumbnails")
        UI_SetStatus(_e("MainWindow_downloadingThumbnail").Replace("%0", CStr(Csender_Bm.Beatmap.ID)), True)
        Dim ThumbClient As New WebClient
        AddHandler ThumbClient.DownloadFileCompleted, AddressOf Importer_DownloadThumb_DownloadFileCompleted
        ThumbClient.DownloadFileAsync(New Uri("https://b.ppy.sh/thumb/" & Csender_Bm.Beatmap.ID & ".jpg"), AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg", Csender_Bm)
    End Sub

    Sub Importer_DownloadThumb_DownloadFileCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs)
        UI_SetStatus(_e("MainWindow_finished"))
        Dim Csender_Bm As Importer.TagData = CType(e.UserState, Importer.TagData)
        If File.Exists(AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg") AndAlso My.Computer.FileSystem.GetFileInfo(AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg").Length >= 10 Then
            Try
                With Csender_Bm.UI_Thumbnail
                    .Source = New BitmapImage(New Uri(AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg"))
                    .ToolTip = _e("MainWindow_openBeatmapDetailPanel")
                End With
            Catch ex As NotSupportedException
                Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
            End Try
        ElseIf My.Computer.FileSystem.GetFileInfo(AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg").Length <= 10 Then
            File.Delete(AppTempPath & "\Cache\Thumbnails\" & Csender_Bm.Beatmap.ID & ".jpg")
            Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
        Else
            Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
        End If
    End Sub

    Sub Importer_FetchFail_ToNext()
        ImporterContainer.BmList_TagsToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
        ImporterContainer.BmList_TagsFailed.Add(ImporterContainer.BmList_TagsToInstall.First)
        ImporterContainer.BmList_TagsToInstall.Remove(ImporterContainer.BmList_TagsToInstall.First)
        Importer_Downloader_ToNextDownload()
    End Sub

    Sub Importer_Init()
        AddHandler ImporterContainer.Downloader.DownloadFileCompleted, AddressOf Importer_Downloader_DownloadFileCompleted
        AddHandler ImporterContainer.Downloader.DownloadProgressChanged, AddressOf Importer_Downloader_DownloadProgressChanged

        If Tool_HasWriteAccessToOsu Then
            Bu_SyncRun.IsEnabled = False
            Bu_ImporterRun.IsEnabled = False
            Bu_ImporterCancel.IsEnabled = False
            PB_ImporterProg.Visibility = Visibility.Visible
            Directory.CreateDirectory(AppTempPath & "\Downloads\Beatmaps")
            Importer_DownloadBeatmap()
        Else
            If MessageBox.Show(_e("MainWindow_requestElevation"), AppName, MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                If RequestElevation("-openFile=" & TB_ImporterInfo.ToolTip.ToString) Then
                    Windows.Application.Current.Shutdown()
                    Exit Sub
                Else
                    MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, AppName)
                    OverlayShow(_e("MainWindow_importAborted"), _e("MainWindow_insufficientPermissions"))
                    OverlayFadeOut()
                End If
            Else
                OverlayShow(_e("MainWindow_importAborted"), _e("MainWindow_insufficientPermissions"))
                OverlayFadeOut()
            End If
        End If
    End Sub

    Sub Importer_ReadListFile(FilePath As String)
        Select Case Path.GetExtension(FilePath)
            Case ".nw520-osblx"
                Try
                    Dim File_Content As String = StringDecompress(File.ReadAllText(FilePath))
                    Importer_ShowRawOSBL(File_Content, FilePath)
                Catch ex As FormatException
                    MessageBox.Show(_e("MainWindow_unableToReadFile") & vbNewLine & vbNewLine &
                                    "> " & _e("MainWindow_details") & ":" & vbNewLine & ex.Message, AppName, MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            Case ".nw520-osbl", ".json"
                Importer_ShowRawOSBL(File.ReadAllText(FilePath), FilePath)
            Case ".zip"     ' @TODO: If contains multiple OSBLX-files read and process each one
                Try
                    Using Zipper As ZipFile = ZipFile.Read(FilePath)
                        Dim DirectoryName As String = AppTempPath & "\Zipper\Importer-" & Date.Now.ToString("yyyy-MM-dd HH.mm.ss")
                        Directory.CreateDirectory(DirectoryName)
                        For Each ZipperEntry As ZipEntry In Zipper
                            If Path.GetExtension(ZipperEntry.FileName) = ".nw520-osblx" Then
                                ZipperEntry.Extract(DirectoryName)
                                Importer_ReadListFile(DirectoryName & "\" & ZipperEntry.FileName)
                                Exit For    ' TODO
                            End If
                        Next
                    End Using
                Catch ex As ZipException
                    MessageBox.Show(_e("MainWindow_unableToReadFile") & vbNewLine & vbNewLine &
                                    "> " & _e("MainWindow_details") & ":" & vbNewLine & ex.Message, AppName, MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            Case Else
                MsgBox(_e("MainWindow_unknownFileExtension") & ":" & vbNewLine & Path.GetExtension(FilePath), MsgBoxStyle.Exclamation, AppName)
        End Select
    End Sub

    Sub Importer_RemoveBmFromSel(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)   ' Get Tag from parent Grid
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        ImporterContainer.BmList_TagsToInstall.Remove(Csender_Tag)
        ImporterContainer.BmList_TagsLeftOut.Add(Csender_Tag)
        If ImporterContainer.BmList_TagsToInstall.Count = 0 Then
            Bu_ImporterRun.IsEnabled = False
            Bu_ImporterCancel.IsEnabled = True
        End If
        Importer_UpdateInfo()
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GrayLight
    End Sub

    Sub Importer_ShowRawOSBL(FileContent As String, FilePath As String)
        Try
            Dim File_Content_Json As JObject = CType(JsonConvert.DeserializeObject(FileContent), JObject)
            TB_ImporterInfo.Text = FilePath
            BmDisplayUpdate(ConvertSavedJSONtoListBeatmap(File_Content_Json), UpdateBmDisplayDestinations.Importer)
        Catch ex As JsonReaderException
            MessageBox.Show(_e("MainWindow_unableToReadFile") & vbNewLine & vbNewLine &
                            "> " & _e("MainWindow_details") & ":" & vbNewLine & ex.Message, AppName, MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Sub Importer_UpdateInfo(Optional Title As String = "osu!Sync")
        TB_ImporterInfo.Text = Title
        If Title = _e("MainWindow_fetching1") Or Title = _e("MainWindow_downloading1") Or Title = _e("MainWindow_installing") Then
            TB_ImporterInfo.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", ImporterContainer.BmList_TagsToInstall.Count.ToString) &
                " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString) &
                " | " & _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BmList_TagsFailed.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString)
        ElseIf Title = _e("MainWindow_finished") Then
            TB_ImporterInfo.Text += " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BmList_TagsDone.Count.ToString) &
                " | " & _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BmList_TagsFailed.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString)
        Else
            TB_ImporterInfo.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", ImporterContainer.BmList_TagsToInstall.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BmList_TagsLeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BmTotal.ToString)
        End If
    End Sub

    Sub TB_ImporterMirror_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TB_ImporterMirror.MouseDown
        Process.Start(Application_Mirrors(AppSettings.Tool_DownloadMirror).WebURL)
    End Sub
#End Region
End Class
