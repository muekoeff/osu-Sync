Imports Hardcodet.Wpf.TaskbarNotification
Imports System.IO
Imports System.Net
Imports System.Windows.Media.Animation
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Enum NotifyNextAction
    None = 0
    OpenUpdater = 1
End Enum

Public Enum UpdateBeatmapDisplayDestinations
    Installed = 0
    Importer = 1
    Exporter = 2
End Enum

Public Class Beatmap
    Public Property Artist As String = ""
    Public Property Creator As String = "Unknown"
    Public Property ID As Integer
    Public Property IsUnplayed As Boolean
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
        FolderDoesNotExist = 1
    End Enum

    Property Arg__Mode As ArgModes
    Property Arg__AutoSync As Boolean = False
    Property Return__Status As ReturnStatuses
    Property Return__Sync_BeatmapList_Installed As New List(Of Beatmap)
    Property Return__Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Property Return__Sync_Cache_Time As String
    Property Return__Sync_Warnings As String
    Property Progress__Current As Integer
    Property Progress__CurrentAction As ProgressCurrentActions
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

    Public BeatmapList_Tag_ToInstall As New List(Of TagData)
    Public BeatmapList_Tag_LeftOut As New List(Of TagData)
    Public BeatmapList_Tag_Done As New List(Of TagData)
    Public BeatmapList_Tag_Failed As New List(Of TagData)
    Public BeatmapsTotal As Integer
    Public Counter As Integer
    Public CurrentFileName As String
    Public Downloader As New WebClient
    Public FilePath As String
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

' BmDP = Beatmap Detail Panel

Class MainWindow
    Private WithEvents BeatmapDetailClient As New WebClient
    Private WithEvents FadeOut As New DoubleAnimation()

    Private Sync_BeatmapList_Installed As New List(Of Beatmap)
    Private Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Private Sync_Done As Boolean = False
    Private Sync_Done_ImporterRequest As Boolean = False
    Private Sync_Done_ImporterRequest_SaveValue As New List(Of Beatmap)

    Private Exporter_BeatmapList_Tag_Selected As New List(Of Importer.TagData)
    Private Exporter_BeatmapList_Tag_Unselected As New List(Of Importer.TagData)

    Private ImporterContainer As New Importer

    Private Interface_LoaderText As New TextBlock
    Private Interface_LoaderProgressBar As New ProgressBar

    Private WithEvents BGW__Action_Sync_GetIDs As New ComponentModel.BackgroundWorker With {
        .WorkerReportsProgress = True,
        .WorkerSupportsCancellation = True}

    Function Action_ConvertSavedJSONtoListBeatmap(ByVal Source As JObject) As List(Of Beatmap)
        Dim BeatmapList As New List(Of Beatmap)

        For Each SelectedToken As JToken In Source.Values
            If Not SelectedToken.Path.StartsWith("_") Then
                Dim CurrentBeatmap As New Beatmap With {
                    .ID = CInt(SelectedToken.SelectToken("id")),
                    .Title = CStr(SelectedToken.SelectToken("title")),
                    .Artist = CStr(SelectedToken.SelectToken("artist"))}

                If Not SelectedToken.SelectToken("artist") Is Nothing Then CurrentBeatmap.Creator = CStr(SelectedToken.SelectToken("creator"))
                BeatmapList.Add(CurrentBeatmap)
            End If
        Next

        Return BeatmapList
    End Function

    Function Action_ConvertSavedJSONtoListBeatmapIDs(ByVal Source As JObject) As List(Of Integer)
        Dim BeatmapList As New List(Of Integer)

        For Each SelectedToken As JToken In Source.Values
            If Not SelectedToken.Path.StartsWith("_") Then BeatmapList.Add(CInt(SelectedToken.SelectToken("id")))
        Next

        Return BeatmapList
    End Function

    Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

    ''' <summary>
    ''' Checks osu!Sync's file associations and creates them if necessary.
    ''' </summary>
    ''' <remarks></remarks>
    Sub Action_CheckFileAssociation()
        Dim FileExtension_Check As Integer = 0        '0 = OK, 1 = Missing File Extension, 2 = Invalid/Outdated File Extension
        For Each FileExtension As String In Application_FileExtensions
            If My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension) Is Nothing Then
                If FileExtension_Check = 0 Then
                    FileExtension_Check = 1
                    Exit For
                End If
            End If
        Next
        If Not FileExtension_Check = 1 Then
            For Each FileExtension As String In Application_FileExtensionsLong
                Dim RegistryPath As String = CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension).OpenSubKey("DefaultIcon").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None))
                RegistryPath = RegistryPath.Substring(1)
                RegistryPath = RegistryPath.Substring(0, RegistryPath.Length - 3)
                If Not RegistryPath = Reflection.Assembly.GetExecutingAssembly().Location.ToString Then
                    FileExtension_Check = 2
                    Exit For
                End If

                RegistryPath = (CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension).OpenSubKey("shell").OpenSubKey("open").OpenSubKey("command").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None)))
                If Not RegistryPath = """" & Reflection.Assembly.GetExecutingAssembly().Location.ToString & """ -openFile=""%1""" Then
                    FileExtension_Check = 2
                    Exit For
                End If
            Next
        End If

        If Not FileExtension_Check = 0 Then
            Dim MessageBox_Content As String
            If FileExtension_Check = 1 Then MessageBox_Content = _e("MainWindow_extensionNotAssociated") & vbNewLine & _e("MainWindow_doYouWantToFixThat") Else MessageBox_Content = _e("MainWindow_extensionWrong") & vbNewLine & _e("MainWindow_doYouWantToFixThat")
            If MessageBox.Show(MessageBox_Content, I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                Dim RegisterError As Boolean = False
                Dim RegisterCounter As Integer = 0
                For Each Extension As String In Application_FileExtensions
                    If CreateFileAssociation(Extension,
                                                             Application_FileExtensionsLong(RegisterCounter),
                                                             Application_FileExtensionsDescription(RegisterCounter),
                                                             Application_FileExtensionsIcon(RegisterCounter),
                                                             Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
                        RegisterCounter += 1
                    Else
                        RegisterError = True
                        Exit For
                    End If
                Next

                If Not RegisterError Then MsgBox(_e("MainWindow_extensionDone"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle) Else MsgBox(_e("MainWindow_extensionFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
        End If
    End Sub

    Sub Action_CheckForUpdates()
        TextBlock_Programm_Updater.Content = _e("MainWindow_checkingForUpdates")
        Dim UpdateClient As New WebClient
        UpdateClient.DownloadStringAsync(New Uri(I__Path_Web_nw520OsySyncApi & "/app/updater.latestVersion.json"))
        AddHandler UpdateClient.DownloadStringCompleted, AddressOf UpdateClient_DownloadStringCompleted
        Setting_Tool_LastCheckForUpdates = Date.Now.ToString("dd-MM-yyyy hh:mm:ss")
        Action_SaveSettings()
    End Sub

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a CSV-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as CSV-String.</returns>
    ''' <remarks></remarks>
    Function Action_ConvertBeatmapListToCSV(ByVal Source As List(Of Beatmap)) As String
        Dim Content As String = "sep=;" & vbNewLine
        Content += "ID;Artist;Creator;Title" & vbNewLine
        For Each SelectedBeatmap As Beatmap In Source
            Content += SelectedBeatmap.ID & ";" & """" & SelectedBeatmap.Artist & """;""" & SelectedBeatmap.Creator & """;""" & SelectedBeatmap.Title & """" & vbNewLine
        Next
        Return Content
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to HTML-Code.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as HTML and possible warnings together in a String().</returns>
    ''' <remarks></remarks>
    Function Action_ConvertBeatmapListToHTML(ByVal Source As List(Of Beatmap)) As String()
        Dim Failed As String = ""
        Dim HTML_Source As String = "<!doctype html>" & vbNewLine &
            "<html>" & vbNewLine &
            "<head><meta charset=""utf-8""><meta name=""author"" content=""osu!Sync""/><meta name=""generator"" content=""osu!Sync " & My.Application.Info.Version.ToString & """/><meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=yes""/><title>Beatmap List | osu!Sync</title><link rel=""icon"" type=""image/png"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Favicon.png""/><link href=""http://fonts.googleapis.com/css?family=Open+Sans:400,300,600,700"" rel=""stylesheet"" type=""text/css"" /><link href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/style.css"" rel=""stylesheet"" type=""text/css""/><link rel=""stylesheet"" type=""text/css"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/css/tooltipster.css""/></head>" & vbNewLine &
            "<body>" & vbNewLine &
            "<div id=""Wrapper"">" & vbNewLine &
            vbTab & "<header><p>Beatmap List | osu!Sync</p></header>" & vbNewLine &
            vbTab & "<div id=""Sort""><ul><li><strong>Sort by...</strong></li><li><a class=""SortParameter"" href=""#Sort_Artist"">Artist</a></li><li><a class=""SortParameter"" href=""#Sort_Creator"">Creator</a></li><li><a class=""SortParameter"" href=""#Sort_SetName"">Name</a></li><li><a class=""SortParameter"" href=""#Sort_SetID"">Set ID</a></li></ul></div>" & vbNewLine &
            vbTab & "<div id=""ListWrapper"">"

        For Each SelectedBeatmap As Beatmap In Source
            If SelectedBeatmap.ID = -1 Then
                Failed += vbNewLine & "• " & SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist & " | " & SelectedBeatmap.Title
            Else
                SelectedBeatmap.Artist.Replace("""", "'")
                SelectedBeatmap.Creator.Replace("""", "'")
                SelectedBeatmap.Title.Replace("""", "'")
                HTML_Source += vbNewLine & vbTab & vbTab & "<article id=""beatmap-" & SelectedBeatmap.ID & """ data-artist=""" & SelectedBeatmap.Artist & """ data-creator=""" & SelectedBeatmap.Creator & """ data-setName=""" & SelectedBeatmap.Title & """ data-setID=""" & SelectedBeatmap.ID & """><a class=""DownloadArrow"" href=""https://osu.ppy.sh/d/" & SelectedBeatmap.ID & """ target=""_blank"">&#8250;</a><h1><span title=""Beatmap Set Name"">" & SelectedBeatmap.Title & "</span></h1><h2><span title=""Beatmap Set ID"">" & SelectedBeatmap.ID & "</span></h2><p><a class=""InfoTitle"" data-function=""artist"" href=""https://osu.ppy.sh/p/beatmaplist?q=" & SelectedBeatmap.Artist & """ target=""_blank"">Artist.</a> " & SelectedBeatmap.Artist & " <a class=""InfoTitle"" data-function=""creator"" href=""https://osu.ppy.sh/p/beatmaplist?q=" & SelectedBeatmap.Creator & """ target=""_blank"">Creator.</a> " & SelectedBeatmap.Creator & " <a class=""InfoTitle"" data-function=""overview"" href=""https://osu.ppy.sh/s/" & SelectedBeatmap.ID & """ target=""_blank"">Overview.</a> <a class=""InfoTitle"" data-function=""discussion"" href=""https://osu.ppy.sh/s/" & SelectedBeatmap.ID & "#disqus_thread"" target=""_blank"">Discussion.</a></p></article>"
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
    Function Action_ConvertBeatmapListToOSBL(ByVal Source As List(Of Beatmap)) As String()
        Dim Failed_Unsubmitted As String = ""
        Dim Failed_Alread_Assigned As String = ""
        Dim Content As New Dictionary(Of String, Dictionary(Of String, String))
        Dim Content_ProgrammInfo As New Dictionary(Of String, String)
        Content_ProgrammInfo.Add("_file_generationdate", Date.Now.ToString("dd/MM/yyyy"))
        Content_ProgrammInfo.Add("_version", My.Application.Info.Version.ToString)
        Content.Add("_info", Content_ProgrammInfo)
        For Each SelectedBeatmap As Beatmap In Source
            If SelectedBeatmap.ID = -1 Then
                Failed_Unsubmitted += vbNewLine & "• " & SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist & " | " & SelectedBeatmap.Title
            ElseIf Content.ContainsKey(SelectedBeatmap.ID.ToString) Then
                Failed_Alread_Assigned += vbNewLine & "• " & SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist & " | " & SelectedBeatmap.Title
            Else
                Dim ContentDictionary As New Dictionary(Of String, String)
                With ContentDictionary
                    .Add("artist", SelectedBeatmap.Artist)
                    .Add("creator", SelectedBeatmap.Creator)
                    .Add("id", SelectedBeatmap.ID.ToString)
                    .Add("title", SelectedBeatmap.Title)
                End With
                Content.Add(SelectedBeatmap.ID.ToString, ContentDictionary)
            End If
        Next
        Dim Content_Json As String = JsonConvert.SerializeObject(Content)

        Dim Failed As String = ""
        If Not Failed_Unsubmitted = "" Then Failed += "======   " & _e("MainWindow_unsubmittedBeatmapSets") & "   =====" & vbNewLine & _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & Failed_Unsubmitted & vbNewLine & vbNewLine
        If Not Failed_Alread_Assigned = "" Then Failed += "=====   " & _e("MainWindow_idAlreadyAssigned") & "   =====" & vbNewLine & _e("MainWindow_beatmapsIdsCanBeUsedOnlyOnce") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & Failed_Alread_Assigned
        Dim Answer As String() = {Content_Json, Failed}
        Return Answer
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a TXT-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as TXT-String.</returns>
    ''' <remarks></remarks>
    Function Action_ConvertBeatmapListToTXT(ByVal Source As List(Of Beatmap)) As String
        Dim Content As String = "// osu!Sync (" & My.Application.Info.Version.ToString & ") | " & Date.Now.ToString("dd.MM.yyyy") & vbNewLine & vbNewLine
        For Each SelectedBeatmap As Beatmap In Source
            Content += "=====   " & SelectedBeatmap.ID & "   =====" & vbNewLine &
                "Creator: " & vbTab & SelectedBeatmap.Creator & vbNewLine &
                "Artist: " & vbTab & SelectedBeatmap.Artist & vbNewLine &
                "ID: " & vbTab & vbTab & vbTab & SelectedBeatmap.ID & vbNewLine &
                "Title: " & vbTab & vbTab & SelectedBeatmap.Title & vbNewLine & vbNewLine
        Next
        Return Content
    End Function

    Public Sub Action_ExportBeatmapDialog(ByRef Source As List(Of Beatmap), Optional ByRef DialogTitle As String = "")
        If DialogTitle = "" Then DialogTitle = _e("MainWindow_exportInstalledBeatmaps1")
        Dim Dialog_SaveFile As New Microsoft.Win32.SaveFileDialog()
        With Dialog_SaveFile
            .AddExtension = True
            .Filter = _e("MainWindow_compressedOsuSyncBeatmapList") & "|*.nw520-osblx|" & _e("MainWindow_osuSyncBeatmapList") & "|*.nw520-osbl|HTML page (" & _e("MainWindow_notImportable") & ")|*.html|Text file (" & _e("MainWindow_notImportable") & ")|*.txt|CSV file (" & _e("MainWindow_notImportable") & ")|*.csv"
            .OverwritePrompt = True
            .Title = DialogTitle
            .ValidateNames = True
            .ShowDialog()
        End With
        If Dialog_SaveFile.FileName = "" Then
            Action_OverlayShow(_e("MainWindow_exportAborted"), "")
            Action_OverlayFadeOut()
            Exit Sub
        End If

        Select Case Dialog_SaveFile.FilterIndex
            Case 1      '.nw520-osblx
                Dim Content As String() = Action_ConvertBeatmapListToOSBL(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(CompressString(Content(0)))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBLX"))
                Action_OverlayFadeOut()
            Case 2      '.nw520-osbl
                Dim Content As String() = Action_ConvertBeatmapListToOSBL(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Content(0))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                             _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBL"))
                Action_OverlayFadeOut()
            Case 3      '.html
                Dim Content As String() = Action_ConvertBeatmapListToHTML(Source)
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Content(0))
                    File.Close()
                End Using
                If Not Content(1) = "" Then
                    Content(1) = Content(1).Insert(0, "=====   " & _e("MainWindow_unsubmittedBeatmapSets") & "   =====" & vbNewLine & _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":")
                    If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") & vbNewLine &
                             _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Content(1), _e("MainWindow_skippedBeatmaps"), "Export")
                        Window_Message.ShowDialog()
                    End If
                End If
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "HTML"))
                Action_OverlayFadeOut()
            Case 4     '.txt
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Action_ConvertBeatmapListToTXT(Source))
                    File.Close()
                End Using
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "TXT"))
                Action_OverlayFadeOut()
            Case 5     '.csv
                Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Action_ConvertBeatmapListToCSV(Source))
                    File.Close()
                End Using
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "CSV"))
                Action_OverlayFadeOut()
        End Select
    End Sub

    Sub Action_OpenBmDP(sender As Object, e As MouseButtonEventArgs)
        Dim Csender_Bm As Beatmap
        If TypeOf sender Is Image Then
            Dim Cparent As Grid = CType(CType(sender, Image).Parent, Grid)   ' Get Tag from parent Grid
            If TypeOf Cparent.Tag Is Beatmap Then Csender_Bm = CType(Cparent.Tag, Beatmap) Else Csender_Bm = CType(Cparent.Tag, Importer.TagData).Beatmap
        Else
            Exit Sub
        End If
        Interface_ShowBmDP(Csender_Bm.ID, New BmDPDetails With {
                           .Artist = Csender_Bm.Artist,
                           .Creator = Csender_Bm.Creator,
                           .IsUnplayed = Csender_Bm.IsUnplayed,
                           .RankedStatus = Csender_Bm.RankedStatus,
                           .Title = Csender_Bm.Title})
    End Sub

    Sub Action_OpenBeatmapListing(sender As Object, e As MouseButtonEventArgs)
        Dim Cparent As Grid = CType(CType(sender, Image).Parent, Grid)  ' Get Tag from parent grid
        Dim Csender_Tag As Beatmap = CType(Cparent.Tag, Beatmap)
        Process.Start("http://osu.ppy.sh/s/" & Csender_Tag.ID)
    End Sub

    Sub Action_OverlayFadeOut()
        Visibility = Visibility.Visible
        Overlay.Visibility = Visibility.Visible
        With FadeOut
            .From = 1
            .To = 0
            .Duration = New Duration(TimeSpan.FromSeconds(1))
        End With
        Storyboard.SetTargetName(FadeOut, "Overlay")
        Storyboard.SetTargetProperty(FadeOut, New PropertyPath(OpacityProperty))

        Dim MyStoryboard As New Storyboard()
        With MyStoryboard
            .Children.Add(FadeOut)
            .Begin(Me)
        End With
    End Sub

    Sub Action_OverlayShow(Optional Title As String = Nothing, Optional Caption As String = Nothing)
        If Not Title Is Nothing Then Overlay_Title.Text = Title
        If Not Caption Is Nothing Then Overlay_Caption.Text = Caption
        With Overlay
            .Opacity = 1
            .Visibility = Visibility.Visible
        End With
    End Sub

    Function Action_ShowBalloon(Content As String, Optional Title As String = "osu!Sync", Optional Icon As BalloonIcon = BalloonIcon.Info, Optional BallonNextAction As NotifyNextAction = NotifyNextAction.None) As Boolean
        If Setting_Tool_EnableNotifyIcon = 0 Then
            With NotifyIcon
                .Tag = BallonNextAction
                .ShowBalloonTip(Title, Content, Icon)
            End With
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Determines wheter to start or (when it's running) to focus osu!.
    ''' </summary>
    ''' <remarks></remarks>
    Sub Action_StartOrFocusOsu()
        If Not Process.GetProcessesByName("osu!").Count > 0 Then
            If File.Exists(Setting_osu_Path & "\osu!.exe") Then Process.Start(Setting_osu_Path & "\osu!.exe") Else MsgBox(_e("MainWindow_unableToFindOsuExe"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
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
    Sub Action_Sync_GetIDs()
        Button_SyncDo.IsEnabled = False
        Interface_SetLoader(_e("MainWindow_parsingInstalledBeatmapSets"))
        TextBlock_Sync_LastUpdate.Content = _e("MainWindow_syncing")
        BGW__Action_Sync_GetIDs.RunWorkerAsync(New BGWcallback_SyncGetIDs)
    End Sub

    Sub Action_ToggleMinimizeToTray()
        If Visibility = Visibility.Visible Then
            Select Case Setting_Tool_EnableNotifyIcon
                Case 0, 2, 3
                    Visibility = Visibility.Hidden
                    NotifyIcon.Visibility = Visibility.Visible
                Case Else
                    MenuItem_Program_MinimizeToTray.IsEnabled = False
            End Select
        Else
            Visibility = Visibility.Visible
            Select Case Setting_Tool_EnableNotifyIcon
                Case 3, 4
                    NotifyIcon.Visibility = Visibility.Collapsed
            End Select
        End If
    End Sub

    Sub Action_Tool_ApplySettings()
        With TextBlock_Warn
            .Content = ""
            .ToolTip = ""
        End With

        ' NotifyIcon
        Select Case Setting_Tool_EnableNotifyIcon
            Case 0, 2
                MenuItem_Program_MinimizeToTray.Visibility = Visibility.Visible
                NotifyIcon.Visibility = Visibility.Visible
            Case 3
                MenuItem_Program_MinimizeToTray.Visibility = Visibility.Visible
                NotifyIcon.Visibility = Visibility.Collapsed
            Case 4
                MenuItem_Program_MinimizeToTray.Visibility = Visibility.Collapsed
                NotifyIcon.Visibility = Visibility.Collapsed
        End Select

        ' Check Write Access
        Tool_HasWriteAccessToOsu = CheckDirAccess(Setting_osu_SongsPath)
        If Tool_HasWriteAccessToOsu = False Then
            If Setting_Tool_RequestElevationOnStartup Then
                If Action_RequestElevation() Then
                    Windows.Application.Current.Shutdown()
                    Exit Sub
                Else
                    MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                End If
            End If
            With TextBlock_Warn
                .Content = _e("MainWindow_noAccess")
                .ToolTip = _e("MainWindow_tt_noAccess")
            End With
        End If
    End Sub

    ''' <summary>
    ''' Updates the beatmap list interface.
    ''' </summary>
    ''' <param name="BeatmapList">List of Beatmaps to display</param>
    ''' <param name="Destination">Selects the list where to display the new list. Possible values <code>Installed</code>, <code>Importer</code>, <code>Exporter</code></param>
    ''' <param name="LastUpdateTime">Only required when <paramref name="Destination"/> = Installed</param>
    ''' <remarks></remarks>
    Sub Action_UpdateBeatmapDisplay(BeatmapList As List(Of Beatmap), Optional Destination As UpdateBeatmapDisplayDestinations = UpdateBeatmapDisplayDestinations.Installed, Optional LastUpdateTime As String = Nothing)
        Select Case Destination
            Case UpdateBeatmapDisplayDestinations.Installed
                If LastUpdateTime = Nothing Then
                    With TextBlock_Sync_LastUpdate
                        .Content = _e("MainWindow_lastSync").Replace("%0", Date.Now.ToString("dd.MM.yyyy | HH:mm:ss"))
                        .Tag = Date.Now.ToString("dd.MM.yyyy | HH:mm:ss")
                    End With
                Else
                    With TextBlock_Sync_LastUpdate
                        .Content = _e("MainWindow_lastSync").Replace("%0", LastUpdateTime)
                        .Tag = LastUpdateTime
                    End With
                End If

                BeatmapWrapper.Children.Clear()

                For Each SelectedBeatmap As Beatmap In BeatmapList
                    Dim UI_Grid = New Grid() With {
                        .Height = 80,
                        .Margin = New Thickness(0, 0, 0, 5),
                        .Tag = SelectedBeatmap,
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
                    If SelectedBeatmap.ID = -1 Then
                        AddHandler(UI_Thumbnail.MouseUp), AddressOf Action_OpenBmDP
                    Else
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Action_OpenBmDP
                        AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf Action_OpenBeatmapListing
                    End If
                    If File.Exists(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg") Then
                        Try
                            UI_Thumbnail.Source = New BitmapImage(New Uri(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg"))
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
                        .Text = SelectedBeatmap.Title,
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
                    If Not SelectedBeatmap.ID = -1 Then UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist Else UI_TextBlock_Caption.Text = _e("MainWindow_unsubmitted") & " | " & SelectedBeatmap.Artist
                    If Not SelectedBeatmap.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator

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

                TextBlock_BeatmapCounter.Text = _e("MainWindow_beatmapsFound").Replace("%0", BeatmapList.Count.ToString)
                Button_SyncDo.IsEnabled = True
            Case UpdateBeatmapDisplayDestinations.Importer
                ImporterContainer = New Importer
                ImporterContainer.BeatmapsTotal = 0
                TabberItem_Import.Visibility = Visibility.Visible
                Tabber.SelectedIndex = 1
                ImporterWrapper.Children.Clear()
                Importer_HideInstalled.IsChecked = False
                Importer_Cancel.IsEnabled = False
                Importer_Run.IsEnabled = False
                If Sync_Done = False Then
                    Sync_Done_ImporterRequest = True
                    Button_SyncDo.IsEnabled = False
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
                    ImporterWrapper.Children.Add(UI_ProgressRing)
                    ImporterWrapper.Children.Add(UI_TextBlock_SubTitle)
                    Sync_Done_ImporterRequest_SaveValue = BeatmapList
                    Action_Sync_GetIDs()
                    Exit Sub
                End If
                For Each SelectedBeatmap As Beatmap In BeatmapList
                    Importer_Cancel.IsEnabled = True

                    Dim Check_IfInstalled As Boolean
                    If Sync_BeatmapList_ID_Installed.Contains(SelectedBeatmap.ID) Then Check_IfInstalled = True Else Check_IfInstalled = False

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
                    If File.Exists(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg") Then
                        ThumbPath = (Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg")
                    ElseIf File.Exists(I__Path_Temp & "\ThumbCache\" & SelectedBeatmap.ID & ".jpg") Then
                        ThumbPath = (I__Path_Temp & "\ThumbCache\" & SelectedBeatmap.ID & ".jpg")
                    End If

                    If Not ThumbPath = "" Then
                        Try
                            With UI_Thumbnail
                                .Source = New BitmapImage(New Uri(ThumbPath))
                                .ToolTip = _e("MainWindow_openBeatmapDetailPanel")
                            End With
                            AddHandler(UI_Thumbnail.MouseDown), AddressOf Action_OpenBmDP
                        Catch ex As NotSupportedException
                            With UI_Thumbnail
                                .Source = New BitmapImage(New Uri("Resources/DownloadThumbnail.png", UriKind.Relative))
                                .ToolTip = _e("MainWindow_downladThumbnail")
                            End With
                            AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
                            AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf Action_OpenBmDP
                        End Try
                    Else
                        With UI_Thumbnail
                            .Source = New BitmapImage(New Uri("Resources/DownloadThumbnail.png", UriKind.Relative))
                            .ToolTip = _e("MainWindow_downladThumbnail")
                        End With
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
                        AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf Action_OpenBmDP
                    End If

                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 22,
                        .Foreground = StandardColors.GrayDark,
                        .Height = 30,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelectedBeatmap.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)

                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 12,
                        .Foreground = StandardColors.GreenDark,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                        .Margin = New Thickness(10, 30, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)
                    If Not SelectedBeatmap.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator

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
                    AddHandler(UI_Checkbox_IsSelected.Checked), AddressOf Importer_AddBeatmapToSelection
                    AddHandler(UI_Checkbox_IsSelected.Unchecked), AddressOf Importer_RemoveBeatmapFromSelection

                    Dim TagData As New Importer.TagData With {
                        .Beatmap = SelectedBeatmap,
                        .IsInstalled = Check_IfInstalled,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title,
                        .UI_Thumbnail = UI_Thumbnail}

                    If Check_IfInstalled = False Then ImporterContainer.BeatmapList_Tag_ToInstall.Add(TagData)
                    UI_Grid.Tag = TagData

                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsSelected)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With
                    ImporterWrapper.Children.Add(UI_Grid)
                    ImporterContainer.BeatmapsTotal += 1
                Next

                Importer_Cancel.IsEnabled = True
                Importer_Info.ToolTip = Importer_Info.Text

                If ImporterContainer.BeatmapList_Tag_ToInstall.Count = 0 Then Importer_Run.IsEnabled = False Else Importer_Run.IsEnabled = True
                Importer_UpdateInfo()
                Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": " & Application_Mirrors(Setting_Tool_DownloadMirror).DisplayName
            Case UpdateBeatmapDisplayDestinations.Exporter
                ExporterWrapper.Children.Clear()
                For Each SelectedBeatmap As Beatmap In BeatmapList
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
                    AddHandler(UI_Thumbnail.MouseRightButtonUp), AddressOf Action_OpenBmDP
                    If File.Exists(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg") Then
                        Try
                            UI_Thumbnail.Source = New BitmapImage(New Uri(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg"))
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
                        .Text = SelectedBeatmap.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)

                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 12,
                        .Foreground = StandardColors.GreenDark,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                        .Margin = New Thickness(10, 30, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)
                    If Not SelectedBeatmap.ID = -1 Then UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist Else UI_TextBlock_Caption.Text = _e("MainWindow_unsubmittedBeatmapCantBeExported") & " | " & SelectedBeatmap.Artist
                    If Not SelectedBeatmap.Creator = "Unknown" Then UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator

                    Dim UI_Checkbox_IsSelected = New CheckBox With {
                        .Content = _e("MainWindow_selectToExport"),
                        .HorizontalAlignment = HorizontalAlignment.Right,
                        .IsChecked = True,
                        .Margin = New Thickness(10, 5, 0, 0),
                        .VerticalAlignment = VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsSelected, 2)

                    If SelectedBeatmap.ID = -1 Then
                        With UI_Checkbox_IsSelected
                            .IsChecked = False
                            .IsEnabled = False
                        End With
                        UI_DecoBorderLeft.Fill = StandardColors.GrayLight
                    Else
                        AddHandler(UI_Checkbox_IsSelected.Checked), AddressOf Exporter_AddBeatmapToSelection
                        AddHandler(UI_Checkbox_IsSelected.Unchecked), AddressOf Exporter_RemoveBeatmapFromSelection
                        AddHandler(UI_DecoBorderLeft.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_TextBlock_Title.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_Thumbnail.MouseLeftButtonUp), AddressOf Exporter_DetermineWheterAddOrRemove
                    End If

                    Dim TagData As New Importer.TagData With {
                        .Beatmap = SelectedBeatmap,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title,
                        .UI_Thumbnail = UI_Thumbnail}

                    Exporter_BeatmapList_Tag_Selected.Add(TagData)

                    UI_Grid.Tag = TagData

                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsSelected)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With

                    ExporterWrapper.Children.Add(UI_Grid)
                Next

                TabberItem_Export.Visibility = Visibility.Visible
                Tabber.SelectedIndex = 2
        End Select
    End Sub

    Sub BeatmapDetailClient_DownloadStringCompleted(sender As Object, e As DownloadStringCompletedEventArgs) Handles BeatmapDetailClient.DownloadStringCompleted
        BeatmapDetails_APIProgress.Visibility = Visibility.Collapsed

        If e.Cancelled Then
            WriteToApiLog("/api/get_beatmaps", "{Cancelled}")
        Else
            Dim JSON_Array As JArray
            Try
                JSON_Array = CType(JsonConvert.DeserializeObject(e.Result), JArray)
                If Not JSON_Array.First Is Nothing Then
                    Dim CI As Globalization.CultureInfo
                    Try
                        CI = New Globalization.CultureInfo(GetTranslationName(Setting_Tool_Language).Replace("_", "-"))
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
                Else
                    WriteToApiLog("/api/get_beatmaps", "{UnexpectedAnswer} " & e.Result)
                    With BeatmapDetails_APIWarn
                        .Content = _e("MainWindow_detailsPanel_apiError")
                        .Visibility = Visibility.Visible
                    End With
                End If
            Catch ex As Exception
                WriteToApiLog("/api/get_beatmaps")
                With BeatmapDetails_APIWarn
                    .Content = _e("MainWindow_detailsPanel_apiError")
                    .Visibility = Visibility.Visible
                End With

            End Try
        End If
    End Sub

    Sub BeatmapDetails_BeatmapListing_Click(sender As Object, e As RoutedEventArgs) Handles BeatmapDetails_BeatmapListing.Click
        Dim Csender As Button = CType(sender, Button)
        Dim Csender_Tag As String = CStr(Csender.Tag)
        Process.Start("http://osu.ppy.sh/s/" & Csender_Tag)
    End Sub

    Sub Button_SyncDo_Click(sender As Object, e As RoutedEventArgs) Handles Button_SyncDo.Click
        If Tool_IsElevated AndAlso Setting_Tool_CheckFileAssociation Then Action_CheckFileAssociation()
        Action_Sync_GetIDs()
    End Sub

    Sub FadeOut_Completed(sender As Object, e As EventArgs) Handles FadeOut.Completed
        Overlay.Visibility = Visibility.Hidden
    End Sub

    Sub Flyout_BeatmapDetails_RequestBringIntoView(sender As Object, e As RequestBringIntoViewEventArgs) Handles Flyout_BeatmapDetails.RequestBringIntoView
        Flyout_BeatmapDetails.Width = Setting_Tool_Interface_BeatmapDetailPanelWidth * (ActualWidth / 100)
    End Sub

    Sub Interface_SetLoader(Optional Message As String = "Please wait")
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

    Sub Interface_ShowBmDP(ID As Integer, Details As BmDPDetails)
        BeatmapDetails_Artist.Text = Details.Artist
        BeatmapDetails_BeatmapListing.Tag = ID
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
        If File.Exists(Setting_osu_Path & "\Data\bt\" & ID & "l.jpg") Then
            ThumbPath = (Setting_osu_Path & "\Data\bt\" & ID & "l.jpg")
        ElseIf File.Exists(I__Path_Temp & "\ThumbCache\" & ID & ".jpg") Then
            ThumbPath = (I__Path_Temp & "\ThumbCache\" & ID & ".jpg")
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
        If Setting_Api_Enabled_BeatmapPanel And Not ID = -1 Then
            If BeatmapDetailClient.IsBusy Then BeatmapDetailClient.CancelAsync()

            ' Reset
            BeatmapDetails_APIFavouriteCount.Text = "..."
            BeatmapDetails_APIFunctions.Visibility = Visibility.Visible
            BeatmapDetails_APIPassCount.Text = "..."
            BeatmapDetails_APIPlayCount.Text = "..."
            BeatmapDetails_APIProgress.Visibility = Visibility.Visible
            BeatmapDetails_APIWarn.Visibility = Visibility.Collapsed

            Try
                BeatmapDetailClient.DownloadStringAsync(New Uri(I__Path_Web_osuApi & "get_beatmaps?k=" & Setting_Api_Key & "&s=" & ID))
            Catch ex As NotSupportedException
                With BeatmapDetails_APIWarn
                    .Content = _e("MainWindow_detailsPanel_generalError")
                    .Visibility = Visibility.Visible
                End With
            End Try
        Else
            BeatmapDetails_APIFunctions.Visibility = Visibility.Collapsed
        End If

        Flyout_BeatmapDetails.IsOpen = True
    End Sub

    Shared Sub Interface_ShowSettingsWindow(Optional ByVal SelectedIndex As Integer = 0)
        Dim Window_Settings As New Window_Settings
        Window_Settings.Tabber.SelectedIndex = SelectedIndex
        Window_Settings.ShowDialog()
    End Sub

    Shared Sub Interface_ShowUpdaterWindow()
        Dim Window_Updater As New Window_Updater
        Window_Updater.ShowDialog()
    End Sub

    Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
#If DEBUG Then
        TextBlock_Programm_Version.Content = "osu!Sync Version " & My.Application.Info.Version.ToString & " (Dev)"
#Else
        TextBlock_Programm_Version.Content = "osu!Sync Version " & My.Application.Info.Version.ToString
#End If

        ' Load Configuration
        If File.Exists(I__Path_Programm & "\Settings\Settings.config") Then
            Action_LoadSettings()
        Else
            Dim Window_Welcome As New Window_Welcome
            Window_Welcome.ShowDialog()

            Action_SaveSettings()
        End If

        ' Apply settings (like NotifyIcon)
        Action_Tool_ApplySettings()

        ' Delete old downloaded beatmaps
        If Directory.Exists(I__Path_Temp & "\BeatmapDownload") Then Directory.Delete(I__Path_Temp & "\BeatmapDownload", True)

        ' Check For Updates
        Select Case Setting_Tool_CheckForUpdates
            Case 0
                Action_CheckForUpdates()
            Case 1
                TextBlock_Programm_Updater.Content = _e("MainWindow_updatesDisabled")
            Case Else
                Dim Interval As Integer
                Select Case Setting_Tool_CheckForUpdates
                    Case 3
                        Interval = 1
                    Case 4
                        Interval = 7
                    Case 5
                        Interval = 30
                End Select

                If DateDiff(DateInterval.Day, Date.ParseExact(Setting_Tool_LastCheckForUpdates, "dd-MM-yyyy hh:mm:ss", Globalization.DateTimeFormatInfo.InvariantInfo), Date.Now) >= Interval Then
                    Action_CheckForUpdates()
                Else
                    TextBlock_Programm_Updater.Content = _e("MainWindow_updateCheckNotNecessary")
                End If
        End Select

        'Open File
        If I__StartUpArguments IsNot Nothing AndAlso Array.Exists(I__StartUpArguments, Function(s)
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
                MsgBox(_e("MainWindow_file404"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                If Setting_Tool_SyncOnStartup Then Action_Sync_GetIDs()
            End If
        Else
            If Setting_Tool_SyncOnStartup Then Action_Sync_GetIDs()
        End If
    End Sub

    Sub MenuItem_File_Export_ConvertSelector_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_ConvertSelector.Click
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = _e("MainWindow_allSupportedFileFormats") & "|*.nw520-osbl;*.nw520-osblx|" & _e("MainWindow_osuSyncBeatmapList") & "|*.nw520-osbl|" & _e("MainWindow_compressedOsuSyncBeatmapList") & "|*.nw520-osbl"
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
                    Action_ExportBeatmapDialog(Action_ConvertSavedJSONtoListBeatmap(JObject.Parse(OSBL_Content)), _e("MainWindow_convertSelectedFile"))
                Case ".nw520-osblx"
                    Try
                        OSBL_Content = DecompressString(OSBL_Content)
                    Catch ex As FormatException
                        Action_OverlayShow(_e("MainWindow_conversionFailed"), "System.FormatException")
                        Action_OverlayFadeOut()
                        Exit Sub
                    Catch ex As InvalidDataException
                        Action_OverlayShow(_e("MainWindow_conversionFailed"), "System.IO.InvalidDataException")
                        Action_OverlayFadeOut()
                        Exit Sub
                    End Try
                    Action_ExportBeatmapDialog(Action_ConvertSavedJSONtoListBeatmap(JObject.Parse(OSBL_Content)), "Convert selected file")
            End Select
        Else
            Action_OverlayShow(_e("MainWindow_conversionAborted"), "")
            Action_OverlayFadeOut()
            Exit Sub
        End If
    End Sub

    Sub MenuItem_File_Export_InstalledBeatmaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_InstalledBeatmaps.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Action_ExportBeatmapDialog(Sync_BeatmapList_Installed)
    End Sub

    Sub MenuItem_File_Export_SelectedMaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_SelectedMaps.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed, UpdateBeatmapDisplayDestinations.Exporter)
    End Sub

    Sub MenuItem_File_OpenBeatmapList_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_OpenBeatmapList.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = _e("MainWindow_allSupportedFileFormats") & "|*.nw520-osbl;*.nw520-osblx|" & _e("MainWindow_compressedOsuSyncBeatmapList") & "|*.nw520-osblx|" & _e("MainWindow_osuSyncBeatmapList") & "|*.nw520-osbl"
            .Title = _e("MainWindow_openBeatmapList")
            .ShowDialog()
        End With

        If Not Dialog_OpenFile.FileName = "" Then
            Importer_ReadListFile(Dialog_OpenFile.FileName)
        Else
            Action_OverlayShow(_e("MainWindow_importAborted"), "")
            Action_OverlayFadeOut()
        End If
    End Sub

    Sub MenuItem_Help_About_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_About.Click
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

    Sub MenuItem_Help_Updater_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_Updater.Click
        Interface_ShowUpdaterWindow()
    End Sub

    Sub MenuItem_Program_Exit_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Exit.Click
        Windows.Application.Current.Shutdown()
    End Sub

    Sub MenuItem_Program_MinimizeToTray_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_MinimizeToTray.Click
        Action_ToggleMinimizeToTray()
    End Sub

    Sub MenuItem_Program_RunOsu_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_RunOsu.Click
        Action_StartOrFocusOsu()
    End Sub

    Sub MenuItem_Program_Settings_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Settings.Click
        Interface_ShowSettingsWindow()
        If Not Tool_DontApplySettings Then Action_Tool_ApplySettings() Else Tool_DontApplySettings = False
    End Sub

    Sub NotifyIcon_Exit_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_Exit.Click
        Windows.Application.Current.Shutdown()
    End Sub

    Sub NotifyIcon_RunOsu_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_RunOsu.Click
        Action_StartOrFocusOsu()
    End Sub

    Sub NotifyIcon_ShowHide_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_ShowHide.Click
        Action_ToggleMinimizeToTray()
    End Sub

    Sub NotifyIcon_TrayBalloonTipClicked(sender As Object, e As RoutedEventArgs) Handles NotifyIcon.TrayBalloonTipClicked
        Select Case CType(NotifyIcon.Tag, NotifyNextAction)
            Case NotifyNextAction.OpenUpdater
                Interface_ShowUpdaterWindow()
        End Select
    End Sub

    Sub NotifyIcon_TrayMouseDoubleClick(sender As Object, e As RoutedEventArgs) Handles NotifyIcon.TrayMouseDoubleClick
        Action_ToggleMinimizeToTray()
    End Sub

    Sub TextBlock_Programm_Updater_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Programm_Updater.MouseDown
        Interface_ShowUpdaterWindow()
    End Sub

    Sub TextBlock_Programm_Version_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Programm_Version.MouseDown
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

    Sub UpdateClient_DownloadStringCompleted(sender As Object, e As Net.DownloadStringCompletedEventArgs)
        Dim Answer As JObject
        Try
            Answer = JObject.Parse(e.Result)
        Catch ex As JsonReaderException
            If Setting_Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_invalidServerResponse") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
                MsgBox(e.Result, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            End If
            TextBlock_Programm_Updater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        Catch ex As Reflection.TargetInvocationException
            If Setting_Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
                MsgBox(e.Result, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            End If
            TextBlock_Programm_Updater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        End Try

        Dim LatestVer As String = CStr(Answer.SelectToken("latestRepoRelease").SelectToken("tag_name"))
        If LatestVer = My.Application.Info.Version.ToString Then
            TextBlock_Programm_Updater.Content = _e("MainWindow_latestVersion")
        Else
            TextBlock_Programm_Updater.Content = _e("MainWindow_updateAvailable").Replace("%0", LatestVer)
            Action_ShowBalloon(_e("MainWindow_aNewVersionIsAvailable").Replace("%0", My.Application.Info.Version.ToString).Replace("%1", LatestVer), , , NotifyNextAction.OpenUpdater)
            If Setting_Messages_Updater_OpenUpdater Then Interface_ShowUpdaterWindow()
        End If
    End Sub

#Region "BGW__Action_Sync_GetIDs"
    Sub BGW__Action_Sync_GetIDs_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles BGW__Action_Sync_GetIDs.DoWork
        Dim Arguments As New BGWcallback_SyncGetIDs
        Arguments = TryCast(e.Argument, BGWcallback_SyncGetIDs)
        Dim Answer As New BGWcallback_SyncGetIDs

        If Not Directory.Exists(Setting_osu_SongsPath) Then
            Answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist
            e.Result = Answer
            Exit Sub
        End If

        Select Case Arguments.Arg__Mode
            Case BGWcallback_SyncGetIDs.ArgModes.Sync
                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                                    .Progress__CurrentAction = BGWcallback_SyncGetIDs.ProgressCurrentActions.CountingTotalFolders,
                                    .Progress__Current = Directory.GetDirectories(Setting_osu_SongsPath).Count})

                Dim Beatmap_InvalidFolder As String = ""
                Dim Beatmap_InvalidIDBeatmaps As String = ""
                If File.Exists(Setting_osu_Path + "\osu!.db") Then
                    ' Reads straight from osu!.db
                    Dim DatabasePath As String = Setting_osu_Path + "\osu!.db"
                    Using Reader As OsuReader = New OsuReader(File.OpenRead(DatabasePath))
                        Dim Version = Reader.ReadInt32()
                        Dim FolderCount = Reader.ReadInt32()
                        Reader.ReadBytes(9)
                        Dim User = Reader.ReadString()
                        Dim FoundIDs As New List(Of Integer)
                        Dim BeatmapCount = Reader.ReadInt32()
                        For i = 1 To BeatmapCount                                   ' More details: http://j.mp/1PIyjCY
                            Dim BeatmapDetails As New Beatmap
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

                            If Not FoundIDs.Contains(BeatmapDetails.ID) Then
                                FoundIDs.Add(BeatmapDetails.ID)
                                Answer.Return__Sync_BeatmapList_Installed.Add(BeatmapDetails)
                                Answer.Return__Sync_BeatmapList_ID_Installed.Add(BeatmapDetails.ID)
                                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                                    .Progress__Current = Answer.Return__Sync_BeatmapList_ID_Installed.Count})
                            End If
                        Next
                    End Using
                Else
                    For Each DirectoryList As String In Directory.GetDirectories(Setting_osu_SongsPath)
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
                                                MsgBox(_e("MainWindow_fetchedIdIsNaN"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
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
                                            Beatmap_InvalidIDBeatmaps += "• " & BeatmapDetails.ID & " | " & BeatmapDetails.Artist & " | " & BeatmapDetails.Title & vbNewLine
                                        End Try
                                    End If
                                    Answer.Return__Sync_BeatmapList_Installed.Add(BeatmapDetails)
                                    Answer.Return__Sync_BeatmapList_ID_Installed.Add(BeatmapDetails.ID)
                                    BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback_SyncGetIDs With {
                                        .Progress__Current = Answer.Return__Sync_BeatmapList_ID_Installed.Count})
                                    Exit For
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
                                    Answer.Return__Sync_BeatmapList_Installed.Add(CurrentBeatmap)
                                    Answer.Return__Sync_BeatmapList_ID_Installed.Add(CInt(Beatmap_ID))
                                Catch ex As Exception
                                    Beatmap_InvalidFolder += "• " & DirectoryInfo.Name & vbNewLine
                                End Try
                            End If
                        End If
                    Next
                End If

                If Not Beatmap_InvalidFolder = "" Or Not Beatmap_InvalidIDBeatmaps = "" Then
                    If Not Beatmap_InvalidFolder = "" Then Answer.Return__Sync_Warnings += "=====   " & _e("MainWindow_ignoredFolders") & "   =====" & vbNewLine & _e("MainWindow_folderCouldntBeParsed") & vbNewLine & vbNewLine & "// " & _e("MainWindow_folders") & ":" & vbNewLine & Beatmap_InvalidFolder & vbNewLine & vbNewLine
                    If Not Beatmap_InvalidIDBeatmaps = "" Then Answer.Return__Sync_Warnings += "=====   " & _e("MainWindow_unableToGetId") & "   =====" & vbNewLine & _e("MainWindow_unableToGetIdOfSomeBeatmapsTheyllBeHandledAsUnsubmitted") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & vbNewLine & Beatmap_InvalidIDBeatmaps & vbNewLine & vbNewLine & vbNewLine
                End If
                e.Result = Answer
        End Select
    End Sub

    Sub BGW__Action_Sync_GetIDs_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles BGW__Action_Sync_GetIDs.ProgressChanged
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

    Sub BGW__Action_Sync_GetIDs_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles BGW__Action_Sync_GetIDs.RunWorkerCompleted
        Dim Answer As BGWcallback_SyncGetIDs = TryCast(e.Result, BGWcallback_SyncGetIDs)
        Select Case Answer.Return__Status
            Case 0
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Return__Sync_BeatmapList_ID_Installed.Count.ToString)
                If Not Answer.Return__Sync_Warnings = "" Then
                    If MessageBox.Show(_e("MainWindow_someBeatmapsDifferFromNormal") & vbNewLine &
                                       _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        With Window_Message
                            .SetMessage(Answer.Return__Sync_Warnings, _e("MainWindow_exceptions"), "Sync")
                            .ShowDialog()
                        End With
                    End If
                End If
                Sync_BeatmapList_Installed = Answer.Return__Sync_BeatmapList_Installed
                Sync_BeatmapList_ID_Installed = Answer.Return__Sync_BeatmapList_ID_Installed

                Sync_Done = True
                Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed)
                Action_OverlayShow(_e("MainWindow_syncCompleted"), "")
                Action_OverlayFadeOut()

                If Sync_Done_ImporterRequest Then
                    Sync_Done_ImporterRequest = False
                    Action_UpdateBeatmapDisplay(Sync_Done_ImporterRequest_SaveValue, UpdateBeatmapDisplayDestinations.Importer)
                End If
            Case BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist
                MsgBox(_e("MainWindow_unableToFindOsuFolderPleaseSpecify"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                Interface_ShowSettingsWindow(1)

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
                Button_SyncDo.IsEnabled = True
        End Select
    End Sub
#End Region

#Region "Exporter"
    Sub Export_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Export_Cancel.Click
        TabberItem_Export.Visibility = Visibility.Collapsed
        Tabber.SelectedIndex = 0
        ExporterWrapper.Children.Clear()

    End Sub

    Sub Export_InvertSelection_Click(sender As Object, e As RoutedEventArgs) Handles Export_InvertSelection.Click
        ' Save unselected elements
        Dim ListUnselected As List(Of Importer.TagData) = Exporter_BeatmapList_Tag_Unselected.ToList
        ' Save selected elements
        Dim ListSelected As List(Of Importer.TagData) = Exporter_BeatmapList_Tag_Selected.ToList

        ' Loop for selected elements
        Dim LoopPreviousCount As Integer = 0
        Dim LoopCount As Integer = 0
        Do While LoopCount < ListSelected.Count
            ListSelected(LoopCount).UI_Checkbox_IsSelected.IsChecked = False
            LoopCount += 1
        Loop

        ' Loop for unselected elements
        LoopPreviousCount = 0
        LoopCount = 0
        Do While LoopCount < ListUnselected.Count
            ListUnselected(LoopCount).UI_Checkbox_IsSelected.IsChecked = True
            LoopCount += 1
        Loop
    End Sub

    Sub Export_Run_Click(sender As Object, e As RoutedEventArgs) Handles Export_Run.Click
        Dim Result As New List(Of Beatmap)
        For Each Item As Importer.TagData In Exporter_BeatmapList_Tag_Selected
            Result.Add(Item.Beatmap)
        Next
        Action_ExportBeatmapDialog(Result, "Export selected beatmaps")
        TabberItem_Export.Visibility = Visibility.Collapsed
        Tabber.SelectedIndex = 0
        ExporterWrapper.Children.Clear()
    End Sub

    Sub Exporter_AddBeatmapToSelection(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        Exporter_BeatmapList_Tag_Unselected.Remove(Csender_Tag)
        Exporter_BeatmapList_Tag_Selected.Add(Csender_Tag)
        If Exporter_BeatmapList_Tag_Selected.Count > 0 Then Export_Run.IsEnabled = True Else Export_Run.IsEnabled = False
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GreenLight
    End Sub

    Sub Exporter_DetermineWheterAddOrRemove(sender As Object, e As EventArgs)
        Dim Cparent As Grid
        Dim Csender_Tag As Importer.TagData
        If TypeOf sender Is Image Then
            Dim Csender As Image = CType(sender, Image)
            Cparent = CType(Csender.Parent, Grid)
        ElseIf TypeOf sender Is Rectangle Then
            Dim Csender As Rectangle = CType(sender, Rectangle)
            Cparent = CType(Csender.Parent, Grid)
        ElseIf TypeOf sender Is TextBlock Then
            Dim Csender As TextBlock = CType(sender, TextBlock)
            Cparent = CType(Csender.Parent, Grid)
        Else
            Exit Sub
        End If
        Csender_Tag = CType(Cparent.Tag, Importer.TagData)

        If Csender_Tag.UI_Checkbox_IsSelected.IsChecked Then
            Exporter_BeatmapList_Tag_Selected.Remove(Csender_Tag)
            If Exporter_BeatmapList_Tag_Selected.Count = 0 Then Export_Run.IsEnabled = False
            With Csender_Tag
                .UI_Checkbox_IsSelected.IsChecked = False
                .UI_DecoBorderLeft.Fill = StandardColors.GrayLight
            End With
        Else
            Exporter_BeatmapList_Tag_Selected.Add(Csender_Tag)
            If Exporter_BeatmapList_Tag_Selected.Count > 0 Then Export_Run.IsEnabled = True Else Export_Run.IsEnabled = False
            With Csender_Tag
                .UI_Checkbox_IsSelected.IsChecked = True
                .UI_DecoBorderLeft.Fill = StandardColors.GreenLight
            End With
        End If
    End Sub

    Sub Exporter_RemoveBeatmapFromSelection(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        Exporter_BeatmapList_Tag_Selected.Remove(Csender_Tag)
        Exporter_BeatmapList_Tag_Unselected.Add(Csender_Tag)
        If Exporter_BeatmapList_Tag_Selected.Count = 0 Then Export_Run.IsEnabled = False
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GrayLight
    End Sub
#End Region

#Region "Importer"
    Sub Importer_AddBeatmapToSelection(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)   ' Get Tag from parent Grid
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        ImporterContainer.BeatmapList_Tag_ToInstall.Add(Csender_Tag)
        ImporterContainer.BeatmapList_Tag_LeftOut.Remove(Csender_Tag)

        If ImporterContainer.BeatmapList_Tag_ToInstall.Count > 0 Then
            Importer_Run.IsEnabled = True
            Importer_Cancel.IsEnabled = True
        Else
            Importer_Run.IsEnabled = False
        End If
        Importer_UpdateInfo()
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.RedLight
    End Sub

    Sub Importer_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Importer_Cancel.Click
        Tabber.SelectedIndex = 0
        TabberItem_Import.Visibility = Visibility.Collapsed
        ImporterWrapper.Children.Clear()
        ImporterContainer = Nothing
    End Sub

    Sub Importer_DownloadBeatmap()
        Dim RequestURI As String

        Importer_Progress.Value = 0
        Importer_Progress.IsIndeterminate = True
        TextBlock_Progress.Content = _e("MainWindow_fetching").Replace("%0", CStr(ImporterContainer.BeatmapList_Tag_ToInstall.First.Beatmap.ID))
        Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": " & Application_Mirrors(Setting_Tool_DownloadMirror).DisplayName
        RequestURI = Application_Mirrors(Setting_Tool_DownloadMirror).DownloadURL.Replace("%0", CStr(ImporterContainer.BeatmapList_Tag_ToInstall.First.Beatmap.ID))

        With ImporterContainer.BeatmapList_Tag_ToInstall.First
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
            If MessageBox.Show(_e("MainWindow_unableToFetchData"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then
                ImporterContainer.BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
                ImporterContainer.BeatmapList_Tag_Failed.Add(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                ImporterContainer.BeatmapList_Tag_ToInstall.Remove(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                Importer_Downloader_ToNextDownload()
                Exit Sub
            Else    ' No
                ImporterContainer.BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
                Importer_Info.Text = _e("MainWindow_installing")
                Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString)
                If ImporterContainer.BeatmapList_Tag_LeftOut.Count > 0 Then Importer_Info.Text += " | " & _e("MainWindow_leftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString)
                Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString)

                TextBlock_Progress.Content = _e("MainWindow_installingFiles")

                For Each FilePath In Directory.GetFiles(I__Path_Temp & "\BeatmapDownload")
                    File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath))
                Next
                With Importer_Progress
                    .IsIndeterminate = False
                    .Visibility = Visibility.Hidden
                End With

                TextBlock_Progress.Content = ""
                Importer_Info.Text = _e("MainWindow_aborted")
                Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString)
                If ImporterContainer.BeatmapList_Tag_LeftOut.Count > 0 Then Importer_Info.Text += " | " & _e("MainWindow_leftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString)
                Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString)
                Button_SyncDo.IsEnabled = True
                Importer_Run.IsEnabled = True
                Importer_Cancel.IsEnabled = True
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
            ImporterContainer.CurrentFileName = CStr(ImporterContainer.BeatmapList_Tag_ToInstall.First.Beatmap.ID) & ".osz"
        End If
        ImporterContainer.CurrentFileName = SanitizePath(ImporterContainer.CurrentFileName)   ' Issue #23: Replace invalid characters

        TextBlock_Progress.Content = _e("MainWindow_downloading").Replace("%0", CStr(ImporterContainer.BeatmapList_Tag_ToInstall.First.Beatmap.ID))
        Importer_UpdateInfo(_e("MainWindow_downloading1"))
        Importer_Progress.IsIndeterminate = False
        ImporterContainer.Downloader.DownloadFileAsync(New Uri(RequestURI), (I__Path_Temp & "\BeatmapDownload\" & ImporterContainer.CurrentFileName))
    End Sub

    Sub Importer_DownloadMirrorInfo_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Importer_DownloadMirrorInfo.MouseDown
        Process.Start(Application_Mirrors(Setting_Tool_DownloadMirror).WebURL)
    End Sub

    Sub Importer_DownloadThumb(sender As Object, e As MouseButtonEventArgs)
        Dim Csender As Image = CType(sender, Image)
        Dim Cparent As Grid = CType(Csender.Parent, Grid)
        Dim Csender_Bm As Importer.TagData = CType(Cparent.Tag, Importer.TagData)

        Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/ProgressThumbnail.png", UriKind.Relative))
        RemoveHandler(Csender_Bm.UI_Thumbnail.MouseLeftButtonUp), AddressOf Importer_DownloadThumb
        RemoveHandler(Csender_Bm.UI_Thumbnail.MouseRightButtonUp), AddressOf Action_OpenBmDP
        AddHandler(Csender_Bm.UI_Thumbnail.MouseDown), AddressOf Action_OpenBmDP
        If Not Directory.Exists(I__Path_Temp & "\ThumbCache") Then Directory.CreateDirectory(I__Path_Temp & "\ThumbCache")
        Dim ThumbClient As New WebClient
        AddHandler ThumbClient.DownloadFileCompleted, AddressOf Importer_DownloadThumbCompleted
        ThumbClient.DownloadFileAsync(New Uri("https://b.ppy.sh/thumb/" & Csender_Bm.Beatmap.ID & ".jpg"), I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg", Csender_Bm)
    End Sub

    Sub Importer_DownloadThumbCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs)
        Dim Csender_Bm As Importer.TagData = CType(e.UserState, Importer.TagData)
        If File.Exists(I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg") AndAlso My.Computer.FileSystem.GetFileInfo(I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg").Length >= 10 Then
            Try
                With Csender_Bm.UI_Thumbnail
                    .Source = New BitmapImage(New Uri(I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg"))
                    .ToolTip = _e("MainWindow_openBeatmapDetailPanel")
                End With
            Catch ex As NotSupportedException
                Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
            End Try
        ElseIf My.Computer.FileSystem.GetFileInfo(I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg").Length <= 10 Then
            File.Delete(I__Path_Temp & "\ThumbCache\" & Csender_Bm.Beatmap.ID & ".jpg")
            Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
        Else
            Csender_Bm.UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
        End If
    End Sub

    Sub Importer_Downloader_DownloadFileCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs)
        ImporterContainer.Counter += 1
        If File.Exists(I__Path_Temp & "\BeatmapDownload\" & ImporterContainer.CurrentFileName) Then
            If My.Computer.FileSystem.GetFileInfo(I__Path_Temp & "\BeatmapDownload\" & ImporterContainer.CurrentFileName).Length <= 3000 Then     ' Detect "Beatmap Not Found" pages
                ' File Empty
                ImporterContainer.BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
                Try
                    File.Delete(I__Path_Temp & "\BeatmapDownload\" & ImporterContainer.CurrentFileName)
                Catch ex As IOException
                End Try
                ImporterContainer.BeatmapList_Tag_Failed.Add(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                ImporterContainer.BeatmapList_Tag_ToInstall.Remove(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                Importer_Downloader_ToNextDownload()
            Else    ' File Normal
                ImporterContainer.BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.PurpleDark
                ImporterContainer.BeatmapList_Tag_Done.Add(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                ImporterContainer.BeatmapList_Tag_ToInstall.Remove(ImporterContainer.BeatmapList_Tag_ToInstall.First)
                Importer_Downloader_ToNextDownload()
            End If
        Else
            ' File Empty
            ImporterContainer.BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = StandardColors.OrangeLight
            ImporterContainer.BeatmapList_Tag_Failed.Add(ImporterContainer.BeatmapList_Tag_ToInstall.First)
            ImporterContainer.BeatmapList_Tag_ToInstall.Remove(ImporterContainer.BeatmapList_Tag_ToInstall.First)
            Importer_Downloader_ToNextDownload()
        End If
    End Sub

    Sub Importer_Downloader_DownloadFinished()
        With Importer_Progress
            .IsIndeterminate = True
            .Value = 0
        End With

        Importer_UpdateInfo(_e("MainWindow_installing"))
        TextBlock_Progress.Content = _e("MainWindow_installingFiles")

        For Each FilePath In Directory.GetFiles(I__Path_Temp & "\BeatmapDownload")
            If Not File.Exists(Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath)) Else File.Delete(FilePath)
        Next
        With Importer_Progress
            .IsIndeterminate = False
            .Visibility = Visibility.Hidden
        End With

        TextBlock_Progress.Content = ""
        Importer_UpdateInfo(_e("MainWindow_finished"))

        If ImporterContainer.BeatmapList_Tag_Failed.Count > 0 Then
            Dim Failed As String = "======   " & _e("MainWindow_downloadFailed") & "   =====" & vbNewLine & _e("MainWindow_cantDownload") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ": "
            For Each _Selection As Importer.TagData In ImporterContainer.BeatmapList_Tag_Failed
                Failed += vbNewLine & "• " & _Selection.Beatmap.ID.ToString & " | " & _Selection.Beatmap.Artist & " | " & _Selection.Beatmap.Title
            Next
            If MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenImported") & vbNewLine &
                               _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                Dim Window_Message As New Window_MessageWindow
                Window_Message.SetMessage(Failed, _e("MainWindow_downloadFailed"), "Import")
                Window_Message.ShowDialog()
            End If
        End If

        Action_ShowBalloon(_e("MainWindow_installationFinished") & vbNewLine &
                    _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString) & vbNewLine &
                    _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BeatmapList_Tag_Failed.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString))
        MsgBox(_e("MainWindow_installationFinished") & vbNewLine &
                    _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString) & vbNewLine &
                    _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BeatmapList_Tag_Failed.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString) & vbNewLine &
                     _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString) & vbNewLine & vbNewLine &
                _e("MainWindow_pressF5"))

        If Setting_Messages_Importer_AskOsu AndAlso Not Process.GetProcessesByName("osu!").Count > 0 AndAlso MessageBox.Show(_e("MainWindow_doYouWantToStartOsuNow"), I__MsgBox_DefaultTitle_CanBeDisabled, MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then Action_StartOrFocusOsu()
        Button_SyncDo.IsEnabled = True
        Importer_Cancel.IsEnabled = True
    End Sub

    Sub Importer_Downloader_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs)
        Importer_Progress.Value = e.ProgressPercentage
    End Sub

    Sub Importer_Downloader_ToNextDownload()
        If ImporterContainer.BeatmapList_Tag_ToInstall.Count > 0 Then
            If Not Setting_Tool_Importer_AutoInstallCounter = 0 And Setting_Tool_Importer_AutoInstallCounter <= ImporterContainer.Counter Then  ' Install file if necessary
                ImporterContainer.Counter = 0
                With Importer_Progress
                    .IsIndeterminate = True
                End With

                Importer_UpdateInfo(_e("MainWindow_installing"))
                TextBlock_Progress.Content = _e("MainWindow_installingFiles")

                For Each FilePath In Directory.GetFiles(I__Path_Temp & "\BeatmapDownload")
                    If Not File.Exists(Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then
                        Try
                            File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath))
                        Catch ex As IOException
                            MsgBox("Unable to install beatmap '" & Path.GetFileName(FilePath) & "'.", MsgBoxStyle.Critical, "Debug | osu!Sync")
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

    Sub Importer_HideInstalled_Checked(sender As Object, e As RoutedEventArgs) Handles Importer_HideInstalled.Checked
        For Each _Selection As Grid In ImporterWrapper.Children
            Dim TEMP As Importer.TagData = CType(_Selection.Tag, Importer.TagData)
            If CType(_Selection.Tag, Importer.TagData).IsInstalled Then
                _Selection.Visibility = Visibility.Collapsed
            End If
        Next
    End Sub

    Sub Importer_HideInstalled_Unchecked(sender As Object, e As RoutedEventArgs) Handles Importer_HideInstalled.Unchecked
        For Each _Selection As Grid In ImporterWrapper.Children
            _Selection.Visibility = Visibility.Visible
        Next
    End Sub

    Sub Importer_Init()
        AddHandler ImporterContainer.Downloader.DownloadFileCompleted, AddressOf Importer_Downloader_DownloadFileCompleted
        AddHandler ImporterContainer.Downloader.DownloadProgressChanged, AddressOf Importer_Downloader_DownloadProgressChanged

        If Tool_HasWriteAccessToOsu Then
            Button_SyncDo.IsEnabled = False
            Importer_Run.IsEnabled = False
            Importer_Cancel.IsEnabled = False
            Importer_Progress.Visibility = Visibility.Visible
            If Not Directory.Exists(I__Path_Temp & "\BeatmapDownload") Then Directory.CreateDirectory(I__Path_Temp & "\BeatmapDownload")
            Importer_DownloadBeatmap()
        Else
            If MessageBox.Show(_e("MainWindow_requestElevation"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                If Action_RequestElevation("-openFile=" & Importer_Info.ToolTip.ToString) Then
                    Windows.Application.Current.Shutdown()
                    Exit Sub
                Else
                    MsgBox(_e("MainWindow_elevationFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                    Action_OverlayShow(_e("MainWindow_importAborted"), _e("MainWindow_insufficientPermissions"))
                    Action_OverlayFadeOut()
                End If
            Else
                Action_OverlayShow(_e("MainWindow_importAborted"), _e("MainWindow_insufficientPermissions"))
                Action_OverlayFadeOut()
            End If
        End If
    End Sub

    Sub Importer_ReadListFile(FilePath As String)
        If Path.GetExtension(FilePath) = ".nw520-osblx" Then
            Dim File_Content_Compressed As String = File.ReadAllText(FilePath)
            Dim File_Content As String = DecompressString(File_Content_Compressed)
            Dim File_Content_Json As JObject = CType(JsonConvert.DeserializeObject(File_Content), JObject)
            Importer_Info.Text = FilePath
            Action_UpdateBeatmapDisplay(Action_ConvertSavedJSONtoListBeatmap(File_Content_Json), UpdateBeatmapDisplayDestinations.Importer)
        ElseIf Path.GetExtension(FilePath) = ".nw520-osbl" Then
            Dim File_Content_Json As JObject = CType(JsonConvert.DeserializeObject(File.ReadAllText(FilePath)), JObject)
            Importer_Info.Text = FilePath
            Action_UpdateBeatmapDisplay(Action_ConvertSavedJSONtoListBeatmap(File_Content_Json), UpdateBeatmapDisplayDestinations.Importer)
        Else
            MsgBox(_e("MainWindow_unknownFileExtension") & ":" & vbNewLine & Path.GetExtension(FilePath), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
        End If
    End Sub

    Sub Importer_RemoveBeatmapFromSelection(sender As Object, e As EventArgs)
        Dim Cparent As Grid = CType(CType(sender, CheckBox).Parent, Grid)   ' Get Tag from parent Grid
        Dim Csender_Tag As Importer.TagData = CType(Cparent.Tag, Importer.TagData)
        ImporterContainer.BeatmapList_Tag_ToInstall.Remove(Csender_Tag)
        ImporterContainer.BeatmapList_Tag_LeftOut.Add(Csender_Tag)
        If ImporterContainer.BeatmapList_Tag_ToInstall.Count = 0 Then
            Importer_Run.IsEnabled = False
            Importer_Cancel.IsEnabled = True
        End If
        Importer_UpdateInfo()
        Csender_Tag.UI_DecoBorderLeft.Fill = StandardColors.GrayLight
    End Sub

    Sub Importer_Run_Click(sender As Object, e As RoutedEventArgs) Handles Importer_Run.Click
        Importer_Init()
    End Sub

    Sub Importer_UpdateInfo(Optional Title As String = "osu!Sync")
        Importer_Info.Text = Title
        If Title = _e("MainWindow_fetching1") Or Title = _e("MainWindow_downloading1") Or Title = _e("MainWindow_installing") Then
            Importer_Info.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", ImporterContainer.BeatmapList_Tag_ToInstall.Count.ToString) &
                " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString) &
                " | " & _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BeatmapList_Tag_Failed.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString)
        ElseIf Title = _e("MainWindow_finished") Then
            Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", ImporterContainer.BeatmapList_Tag_Done.Count.ToString) &
                " | " & _e("MainWindow_setsFailed").Replace("%0", ImporterContainer.BeatmapList_Tag_Failed.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString)
        Else
            Importer_Info.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", ImporterContainer.BeatmapList_Tag_ToInstall.Count.ToString) &
                " | " & _e("MainWindow_setsLeftOut").Replace("%0", ImporterContainer.BeatmapList_Tag_LeftOut.Count.ToString) &
                " | " & _e("MainWindow_setsTotal").Replace("%0", ImporterContainer.BeatmapsTotal.ToString)
        End If
    End Sub
#End Region
End Class
