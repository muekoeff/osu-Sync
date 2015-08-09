Imports Hardcodet.Wpf.TaskbarNotification
Imports Newtonsoft.Json, Newtonsoft.Json.Linq
Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices, System.Runtime.Serialization.Formatters.Binary
Imports System.Windows.Media.Animation

Public Enum BGWcallback_ActionSyncGetIDs_ArgMode
    Sync = 0
End Enum

Public Enum BGWcallback_ActionSyncGetIDs_ProgressCurrentAction
    Sync = 0
    Done = 2
    CountingTotalFolders = 4
End Enum

Public Enum BGWcallback_ActionSyncGetIDs_ReturnStatus
    FolderDoesNotExist = 1
End Enum

Public Enum NotifyNextAction
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
    Public Property MD5 As String
    Public Property RankedStatus As Byte = Convert.ToByte(1)
    Public Property SongSource As String = ""
    Public Property Title As String
End Class

Public Class BeatmapPanelDetails
    Public Property Artist As String = "Unknown"
    Public Property Creator As String = "Unknown"
    Public Property RankedStatus As Byte = Convert.ToByte(0)
    Public Property SongSource As String = "Unknown"
    Public Property Title As String
End Class

Public Class BGWcallback__Action_Sync_GetIDs
    Public Property Arg__Mode As BGWcallback_ActionSyncGetIDs_ArgMode
    Public Property Arg__AutoSync As Boolean = False
    Public Property Return__Status As BGWcallback_ActionSyncGetIDs_ReturnStatus
    Public Property Return__Sync_BeatmapList_Installed As New List(Of Beatmap)
    Public Property Return__Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Public Property Return__Sync_Cache_Time As String
    Public Property Return__Sync_Warnings As String
    Public Property Progress__Current As Integer
    Public Property Progress__CurrentAction As BGWcallback_ActionSyncGetIDs_ProgressCurrentAction
End Class

Class MainWindow
    Private WithEvents Client As New WebClient
    Private WithEvents FadeOut As New DoubleAnimation()
    Private Notify_NextAction As NotifyNextAction

    Private Sync_BeatmapList_Installed As New List(Of Beatmap)
    Private Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Private Sync_Done As Boolean = False
    Private Sync_Done_ImporterRequest As Boolean = False
    Private Sync_Done_ImporterRequest_SaveValue As New List(Of Beatmap)

    Private Exporter_BeatmapList_Tag_Selected As New List(Of Importer_TagData)
    Private Exporter_BeatmapList_Tag_Unselected As New List(Of Importer_TagData)

    Private WithEvents Importer_CurrentFileName As String
    Private WithEvents Importer_Downloader As New Net.WebClient
    Private Importer_BeatmapList_Tag_ToInstall As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_LeftOut As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_Done As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_Failed As New List(Of Importer_TagData)
    Private Importer_BeatmapsTotal As Integer
    Private Importer_Counter As Integer
    Private Importer_FilePath As String

    Private Color_999999 As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF999999"), Brush)          ' Light Gray
    Private Color_8E44AD As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF8E44AD"), Brush)          ' Dark Purple
    Private Color_555555 As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF555555"), Brush)          ' Gray
    Private Color_3498DB As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF3498DB"), Brush)          ' Light Blue
    Private Color_27AE60 As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF27AE60"), Brush)          ' Light Green
    Private Color_008136 As Brush = DirectCast(New BrushConverter().ConvertFrom("#FF008136"), Brush)          ' Dark Green
    Private Color_E74C3C As Brush = DirectCast(New BrushConverter().ConvertFrom("#FFE74C3C"), Brush)          ' Red
    Private Color_E67E2E As Brush = DirectCast(New BrushConverter().ConvertFrom("#FFE67E2E"), Brush)          ' Light Orange

    Private Interface_LoaderText As New TextBlock
    Private Interface_LoaderProgressBar As New ProgressBar

    Private WithEvents BGW__Action_Sync_GetIDs As New System.ComponentModel.BackgroundWorker With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}

    Private Class Importer_TagData
        Public Property Beatmap As Beatmap
        Public Property UI_Checkbox_IsInstalled As CheckBox
        Public Property UI_Grid As Grid
        Public Property UI_DecoBorderLeft As Rectangle
        Public Property UI_TextBlock_Title As TextBlock
        Public Property UI_TextBlock_Caption As TextBlock
        Public Property UI_Checkbox_IsSelected As CheckBox
    End Class

    Private Function Action_ConvertSavedJSONtoListBeatmap(ByVal Source As JObject) As List(Of Beatmap)
        Dim BeatmapList As New List(Of Beatmap)

        For Each SelectedToken As JToken In Source.Values
            If Not SelectedToken.Path.StartsWith("_") Then
                Dim CurrentBeatmap As New Beatmap With {
                    .ID = CInt(SelectedToken.SelectToken("id")),
                    .Title = CStr(SelectedToken.SelectToken("title")),
                    .Artist = CStr(SelectedToken.SelectToken("artist"))}

                If Not SelectedToken.SelectToken("artist") Is Nothing Then
                    CurrentBeatmap.Creator = CStr(SelectedToken.SelectToken("creator"))
                End If
                BeatmapList.Add(CurrentBeatmap)
            End If
        Next

        Return BeatmapList
    End Function

    Private Function Action_ConvertSavedJSONtoListBeatmapIDs(ByVal Source As JObject) As List(Of Integer)
        Dim BeatmapList As New List(Of Integer)

        For Each SelectedToken As JToken In Source.Values
            If Not SelectedToken.Path.StartsWith("_") Then
                BeatmapList.Add(CInt(SelectedToken.SelectToken("id")))
            End If
        Next

        Return BeatmapList
    End Function

    Private Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmdShow As Integer) As Integer

    ''' <summary>
    ''' Checks osu!Sync's file associations and creates them if necessary.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub Action_CheckFileAssociation()
        Dim FileExtension_Check As Integer = 0        '0 = OK, 1 = Missing File Extension, 2 = Invalid/Outdated File Extension
        For Each FileExtension As String In FileExtensions
            If My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension) Is Nothing Then
                If FileExtension_Check = 0 Then
                    FileExtension_Check = 1
                    Exit For
                End If
            End If
        Next
        If Not FileExtension_Check = 1 Then
            For Each FileExtension As String In FileExtensionsLong
                Dim RegistryPath As String = CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension).OpenSubKey("DefaultIcon").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None))
                RegistryPath = RegistryPath.Substring(1)
                RegistryPath = RegistryPath.Substring(0, RegistryPath.Length - 3)
                If Not RegistryPath = System.Reflection.Assembly.GetExecutingAssembly().Location.ToString Then
                    FileExtension_Check = 2
                    Exit For
                End If

                RegistryPath = (CStr(My.Computer.Registry.ClassesRoot.OpenSubKey(FileExtension).OpenSubKey("shell").OpenSubKey("open").OpenSubKey("command").GetValue(Nothing, "", Microsoft.Win32.RegistryValueOptions.None)))
                If Not RegistryPath = """" & System.Reflection.Assembly.GetExecutingAssembly().Location.ToString & """ -openFile=""%1""" Then
                    FileExtension_Check = 2
                    Exit For
                End If
            Next
        End If

        If Not FileExtension_Check = 0 Then
            Dim MessageBox_Content As String
            If FileExtension_Check = 1 Then
                MessageBox_Content = _e("MainWindow_extensionNotAssociated") & vbNewLine & _e("MainWindow_doYouWantToFixThat")
            Else
                MessageBox_Content = _e("MainWindow_extensionWrong") & vbNewLine & _e("MainWindow_doYouWantToFixThat")
            End If
            If MessageBox.Show(MessageBox_Content, I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                Dim RegisterError As Boolean = False
                Dim RegisterCounter As Integer = 0
                For Each Extension As String In FileExtensions
                    If CreateFileAssociation(Extension,
                                                             FileExtensionsLong(RegisterCounter),
                                                             FileExtensionsDescription(RegisterCounter),
                                                             FileExtensionsIcon(RegisterCounter),
                                                             System.Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
                        RegisterCounter += 1
                    Else
                        RegisterError = True
                        Exit For
                    End If
                Next

                If Not RegisterError Then
                    MsgBox(_e("MainWindow_extensionDone"), MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
                Else
                    MsgBox(_e("MainWindow_extensionFailed"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                End If
            End If
        End If
    End Sub

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a CSV-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as CSV-String.</returns>
    ''' <remarks></remarks>
    Private Function Action_ConvertBeatmapListToCSV(ByVal Source As List(Of Beatmap)) As String
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
    Private Function Action_ConvertBeatmapListToHTML(ByVal Source As List(Of Beatmap)) As String()
        Dim Failed As String = ""
        Dim HTML_Source As String = "<!doctype html>" & vbNewLine &
            "<!-- Information: This file was generated with osu!Sync " & My.Application.Info.Version.ToString & " by naseweis520 (http://naseweis520.ml/) | " & DateTime.Now.ToString("dd.MM.yyyy") & " -->" & vbNewLine &
            "<html>" & vbNewLine &
            "<head><meta charset=""utf-8""><meta name=""author"" content=""naseweis520, osu!Sync""/><meta name=""generator"" content=""osu!Sync " & My.Application.Info.Version.ToString & """/><meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=yes""/><title>Beatmap List | osu!Sync</title><link rel=""icon"" type=""image/png"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Favicon.png""/><link href=""http://fonts.googleapis.com/css?family=Open+Sans:400,300,600,700"" rel=""stylesheet"" type=""text/css"" /><link href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/style.css"" rel=""stylesheet"" type=""text/css""/><link rel=""stylesheet"" type=""text/css"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/css/tooltipster.css""/></head>" & vbNewLine &
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
        "<footer><p>Generated with osu!Sync, a free tool made by <a href=""http://naseweis520.ml/"" target=""_blank"">naseweis520</a>.</p></footer>" & vbNewLine &
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
    Private Function Action_ConvertBeatmapListToOSBL(ByVal Source As List(Of Beatmap)) As String()
        Dim Failed_Unsubmitted As String = ""
        Dim Failed_Alread_Assigned As String = ""
        Dim Content As New Dictionary(Of String, Dictionary(Of String, String))
        Dim Content_ProgrammInfo As New Dictionary(Of String, String)
        Content_ProgrammInfo.Add("_author", "naseweis520")
        Content_ProgrammInfo.Add("_author_uri", "http://naseweis520.ml/")
        Content_ProgrammInfo.Add("_file_generationdate", DateTime.Now.ToString("dd/MM/yyyy"))
        Content_ProgrammInfo.Add("_programm", "osu!Sync")
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
        If Not Failed_Unsubmitted = "" Then
            Failed += "======   " & _e("MainWindow_unsubmittedBeatmapSets") & "   =====" & vbNewLine & _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & Failed_Unsubmitted & vbNewLine & vbNewLine
        End If
        If Not Failed_Alread_Assigned = "" Then
            Failed += "=====   " & _e("MainWindow_idAlreadyAssigned") & "   =====" & vbNewLine & _e("MainWindow_beatmapsIdsCanBeUsedOnlyOnce") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & Failed_Alread_Assigned
        End If
        Dim Answer As String() = {Content_Json, Failed}
        Return Answer
    End Function

    ''' <summary>
    ''' Converts the given <code>List(Of Beatmap)</code> to a TXT-String.
    ''' </summary>
    ''' <param name="Source">List of beatmaps</param>
    ''' <returns><code>List(Of Beatmap)</code> as TXT-String.</returns>
    ''' <remarks></remarks>
    Private Function Action_ConvertBeatmapListToTXT(ByVal Source As List(Of Beatmap)) As String
        Dim Content As String = "Information: This file was generated with osu!Sync " & My.Application.Info.Version.ToString & " by naseweis520 (http://naseweis520.ml/) | " & DateTime.Now.ToString("dd.MM.yyyy") & vbNewLine & vbNewLine
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
        If DialogTitle = "" Then
            DialogTitle = _e("MainWindow_exportInstalledBeatmaps1")
        End If
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
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
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
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
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
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
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
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Action_ConvertBeatmapListToTXT(Source))
                    File.Close()
                End Using
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "TXT"))
                Action_OverlayFadeOut()
            Case 5     '.csv
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    File.Write(Action_ConvertBeatmapListToCSV(Source))
                    File.Close()
                End Using
                Action_OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "CSV"))
                Action_OverlayFadeOut()
        End Select
    End Sub

    Private Sub Action_OpenBeatmapDetails(sender As Object, e As MouseButtonEventArgs)
        Dim SelectedSender As Image = CType(sender, Image)
        Dim SelectedSender_Tag As Beatmap = CType(SelectedSender.Tag, Beatmap)

        Interface_ShowBeatmapDetails(SelectedSender_Tag.ID, New BeatmapPanelDetails With {
                                     .Artist = SelectedSender_Tag.Artist,
                                     .Creator = SelectedSender_Tag.Creator,
                                     .RankedStatus = SelectedSender_Tag.RankedStatus,
                                     .Title = SelectedSender_Tag.Title})
    End Sub

    Private Sub Action_OverlayFadeOut()
        Me.Visibility = Windows.Visibility.Visible

        Overlay.Visibility = Windows.Visibility.Visible
        With FadeOut
            .From = 1
            .To = 0
            .Duration = New Duration(TimeSpan.FromSeconds(1))
        End With
        Storyboard.SetTargetName(FadeOut, "Overlay")
        Storyboard.SetTargetProperty(FadeOut, New PropertyPath(Window.OpacityProperty))

        Dim MyStoryboard As New Storyboard()
        MyStoryboard.Children.Add(FadeOut)

        MyStoryboard.Begin(Me)
    End Sub

    Private Sub Action_OverlayShow(Optional ByRef title As String = Nothing, Optional ByRef caption As String = Nothing)
        If Not title Is Nothing Then
            Overlay_Title.Text = title
        End If
        If Not caption Is Nothing Then
            Overlay_Caption.Text = caption
        End If
        With Overlay
            .Opacity = 1
            .Visibility = Windows.Visibility.Visible
        End With
    End Sub

    ''' <summary>
    ''' Determines wheter to start or (when it's running) to focus osu!.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub Action_StartOrFocusOsu()
        If Not Process.GetProcessesByName("osu!").Count > 0 Then
            If File.Exists(Setting_osu_Path & "\osu!.exe") Then
                Process.Start(Setting_osu_Path & "\osu!.exe")
            Else
                MsgBox(_e("MainWindow_unableToFindOsuExe"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
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
    Private Sub Action_Sync_GetIDs()
        Button_SyncDo.IsEnabled = False
        Interface_SetLoader(_e("MainWindow_parsingInstalledBeatmapSets"))
        TextBlock_Sync_LastUpdate.Content = _e("MainWindow_syncing")
        BGW__Action_Sync_GetIDs.RunWorkerAsync(New BGWcallback__Action_Sync_GetIDs)
    End Sub

    Private Sub Action_Tool_UpdateSettings()
        Select Case Setting_Tool_EnableNotifyIcon
            Case 0, 2, 3
                NotifyIcon.Visibility = Windows.Visibility.Visible
                If Setting_Tool_EnableNotifyIcon = 3 Then
                    NotifyIcon.Visibility = Windows.Visibility.Collapsed
                Else
                    MenuItem_Program_MinimizeToTray.Visibility = Windows.Visibility.Visible
                End If
            Case 4
                NotifyIcon.Visibility = Windows.Visibility.Collapsed
                MenuItem_Program_MinimizeToTray.Visibility = Windows.Visibility.Collapsed
        End Select
    End Sub

    ''' <summary>
    ''' Updates the beatmap list interface.
    ''' </summary>
    ''' <param name="BeatmapList">List of Beatmaps to display</param>
    ''' <param name="Destination">Selects the list where to display the new list. Possible values <code>Installed</code>, <code>Importer</code>, <code>Exporter</code></param>
    ''' <param name="LastUpdateTime">Only required when <paramref name="Destination"/> = Installed</param>
    ''' <remarks></remarks>
    Private Sub Action_UpdateBeatmapDisplay(ByVal BeatmapList As List(Of Beatmap), Optional ByVal Destination As UpdateBeatmapDisplayDestinations = UpdateBeatmapDisplayDestinations.Installed, Optional LastUpdateTime As String = Nothing)
        Select Case Destination
            Case UpdateBeatmapDisplayDestinations.Installed
                If LastUpdateTime = Nothing Then
                    With TextBlock_Sync_LastUpdate
                        .Content = _e("MainWindow_lastSync").Replace("%0", DateTime.Now.ToString("dd.MM.yyyy | HH:mm:ss"))
                        .Tag = DateTime.Now.ToString("dd.MM.yyyy | HH:mm:ss")
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
                        .Margin = New Thickness(0, 0, 0, 10),
                        .Tag = SelectedBeatmap,
                        .Width = Double.NaN}

                    With UI_Grid.ColumnDefinitions
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(125)})
                        .Add(New ColumnDefinition)
                    End With

                    ' Color_27AE60 = Light Green
                    Dim UI_DecoBorderLeft = New Rectangle With {
                        .Fill = Color_27AE60,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
                        .Tag = SelectedBeatmap,
                        .VerticalAlignment = Windows.VerticalAlignment.Stretch}

                    Dim UI_Thumbnail = New Image With {
                        .Cursor = Cursors.Hand,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
                        .Margin = New Thickness(5, 0, 0, 0),
                        .Tag = SelectedBeatmap,
                        .ToolTip = _e("MainWindow_openBeatmapDetailPanel"),
                        .VerticalAlignment = Windows.VerticalAlignment.Stretch}
                    Grid.SetColumn(UI_Thumbnail, 1)
                    AddHandler(UI_Thumbnail.MouseUp), AddressOf Action_OpenBeatmapDetails
                    If File.Exists(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg") Then
                        UI_Thumbnail.Source = New BitmapImage(New Uri(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg"))
                    Else
                        UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                    End If

                    ' Color_555555 = Gray
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 28,
                        .Foreground = Color_555555,
                        .Height = 36,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelectedBeatmap.Title,
                        .Tag = SelectedBeatmap,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)

                    ' Color_008136 = Dark Green
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 14,
                        .Foreground = Color_008136,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Tag = SelectedBeatmap,
                        .Margin = New Thickness(10, 38, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)

                    If Not SelectedBeatmap.ID = -1 Then
                        UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist
                    Else
                        UI_TextBlock_Caption.Text = _e("MainWindow_unsubmitted") & " | " & SelectedBeatmap.Artist
                    End If
                    If Not SelectedBeatmap.Creator = "Unknown" Then
                        UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator
                    End If

                    Dim UI_Checkbox_IsInstalled = New CheckBox With {
                        .Content = _e("MainWindow_installed") & "?",
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .IsChecked = True,
                        .IsEnabled = False,
                        .Margin = New Thickness(10, 62, 0, 0),
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsInstalled, 2)

                    With UI_Grid.Children
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_Thumbnail)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Checkbox_IsInstalled)
                    End With
                    BeatmapWrapper.Children.Add(UI_Grid)
                Next
                If BeatmapList.Count = 0 Then
                    Dim UI_TextBlock As New TextBlock With {
                        .FontSize = 72,
                        .Foreground = Color_27AE60,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                        .Margin = New Thickness(0, 86, 0, 0),
                        .Text = _e("MainWindow_beatmapsFound").Replace("%0", "0"),
                        .VerticalAlignment = Windows.VerticalAlignment.Center}
                    Dim UI_TextBlock_SubTitle As New TextBlock With {
                        .FontSize = 24,
                        .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FF2ECC71"), Brush),
                        .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                        .Text = _e("MainWindow_thatsImpressiveIGuess"),
                        .VerticalAlignment = Windows.VerticalAlignment.Center}

                    With BeatmapWrapper.Children
                        .Add(UI_TextBlock)
                        .Add(UI_TextBlock_SubTitle)
                    End With
                End If

                TextBlock_BeatmapCounter.Text = _e("MainWindow_beatmapsFound").Replace("%0", BeatmapList.Count.ToString)
                Button_SyncDo.IsEnabled = True
            Case UpdateBeatmapDisplayDestinations.Importer
                Importer_BeatmapsTotal = 0
                TabberItem_Import.Visibility = Windows.Visibility.Visible
                Tabber.SelectedIndex = 1
                ImporterWrapper.Children.Clear()
                Importer_Cancel.IsEnabled = False
                Importer_Run.IsEnabled = False
                If Sync_Done = False Then
                    Sync_Done_ImporterRequest = True
                    Button_SyncDo.IsEnabled = False
                    Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With {
                       .Height = 150,
                       .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                       .IsActive = True,
                       .Margin = New Thickness(0, 100, 0, 0),
                       .VerticalAlignment = Windows.VerticalAlignment.Center,
                       .Width = 150}
                    Dim UI_TextBlock_SubTitle As New TextBlock With {
                               .FontSize = 24,
                               .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                               .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                               .Text = _e("MainWindow_pleaseWait") & vbNewLine & _e("MainWindow_syncing"),
                               .TextAlignment = TextAlignment.Center,
                               .VerticalAlignment = Windows.VerticalAlignment.Center}

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
                    If Sync_BeatmapList_ID_Installed.Contains(SelectedBeatmap.ID) Then
                        Check_IfInstalled = True
                    Else
                        Check_IfInstalled = False
                    End If
                    Dim UI_Checkbox_IsInstalled = New CheckBox With {
                        .Content = _e("MainWindow_installed") & "?",
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .IsChecked = Check_IfInstalled,
                        .IsEnabled = False,
                        .Margin = New Thickness(10, 62, 0, 0),
                        .VerticalAlignment = Windows.VerticalAlignment.Top}

                    Dim UI_Grid = New Grid() With {
                        .Height = 80,
                        .Margin = New Thickness(0, 0, 0, 10),
                        .Width = Double.NaN}

                    ' Color_27AE60 = Light Green
                    ' Color_E74C3C = Red
                    Dim UI_DecoBorderLeft = New Rectangle With {
                        .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
                        .VerticalAlignment = Windows.VerticalAlignment.Top,
                        .Width = 10}
                    If Check_IfInstalled Then
                        UI_DecoBorderLeft.Fill = Color_27AE60
                    Else
                        UI_DecoBorderLeft.Fill = Color_E74C3C
                    End If

                    ' Color_555555 = Gray
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 28,
                        .Foreground = Color_555555,
                        .Height = 36,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelectedBeatmap.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}

                    ' Color_008136 = Dark Green
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 14,
                        .Foreground = Color_008136,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                        .Margin = New Thickness(10, 38, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    If Not SelectedBeatmap.Creator = "Unknown" Then
                        UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator
                    End If

                    Dim UI_Checkbox_IsSelected = New CheckBox With {
                        .Content = _e("MainWindow_downloadAndInstall"),
                        .HorizontalAlignment = Windows.HorizontalAlignment.Right,
                        .Margin = New Thickness(10, 5, 0, 0),
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
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

                    Dim TagData As New Importer_TagData With {
                        .Beatmap = SelectedBeatmap,
                        .UI_Checkbox_IsInstalled = UI_Checkbox_IsInstalled,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title}

                    If Check_IfInstalled = False Then
                        Importer_BeatmapList_Tag_ToInstall.Add(TagData)
                    End If
                    UI_Checkbox_IsInstalled.Tag = TagData
                    UI_Checkbox_IsSelected.Tag = TagData
                    UI_Checkbox_IsSelected.Tag = TagData
                    UI_DecoBorderLeft.Tag = TagData
                    UI_Grid.Tag = TagData
                    UI_TextBlock_Caption.Tag = TagData
                    UI_TextBlock_Title.Tag = TagData

                    UI_Grid.Children.Add(UI_DecoBorderLeft)
                    UI_Grid.Children.Add(UI_TextBlock_Title)
                    UI_Grid.Children.Add(UI_TextBlock_Caption)
                    UI_Grid.Children.Add(UI_Checkbox_IsInstalled)
                    UI_Grid.Children.Add(UI_Checkbox_IsSelected)
                    ImporterWrapper.Children.Add(UI_Grid)
                    Importer_BeatmapsTotal += 1
                Next

                Importer_Cancel.IsEnabled = True
                Importer_Info.ToolTip = Importer_Info.Text

                If Importer_BeatmapList_Tag_ToInstall.Count = 0 Then
                    Importer_Run.IsEnabled = False
                Else
                    Importer_Run.IsEnabled = True
                End If
                Importer_UpdateInfo("osu!Sync")
                Select Case Setting_Tool_DownloadMirror
                    Case 0
                        Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": Bloodcat.com"
                    Case 1
                        Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": Loli.al"
                End Select
            Case UpdateBeatmapDisplayDestinations.Exporter
                ExporterWrapper.Children.Clear()
                For Each SelectedBeatmap As Beatmap In BeatmapList
                    Dim UI_Grid = New Grid() With {
                        .Height = 51,
                        .Margin = New Thickness(0, 0, 0, 10),
                        .Width = Double.NaN}

                    With UI_Grid.ColumnDefinitions
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(10)})
                        .Add(New ColumnDefinition With {
                            .Width = New GridLength(73)})
                        .Add(New ColumnDefinition)
                    End With

                    ' Color_27AE60 = Light Green
                    Dim UI_DecoBorderLeft = New Rectangle With {
                        .Fill = Color_27AE60,
                        .VerticalAlignment = Windows.VerticalAlignment.Stretch}

                    Dim UI_Thumbnail = New Image With {
                        .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
                        .Margin = New Thickness(5, 0, 0, 0),
                        .Tag = SelectedBeatmap,
                        .VerticalAlignment = Windows.VerticalAlignment.Stretch}
                    Grid.SetColumn(UI_Thumbnail, 1)
                    AddHandler(UI_Thumbnail.MouseUp), AddressOf Action_OpenBeatmapDetails
                    If File.Exists(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg") Then
                        UI_Thumbnail.Source = New BitmapImage(New Uri(Setting_osu_Path & "\Data\bt\" & SelectedBeatmap.ID & "l.jpg"))
                    Else
                        UI_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
                    End If

                    ' Color_555555 = Gray
                    Dim UI_TextBlock_Title = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI"),
                        .FontSize = 22,
                        .Foreground = Color_555555,
                        .Height = 30,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Margin = New Thickness(10, 0, 0, 0),
                        .Text = SelectedBeatmap.Title,
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Title, 2)

                    ' Color_008136 = Dark Green
                    Dim UI_TextBlock_Caption = New TextBlock With {
                        .FontFamily = New FontFamily("Segoe UI Light"),
                        .FontSize = 12,
                        .Foreground = Color_008136,
                        .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                        .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                        .Margin = New Thickness(10, 30, 0, 0),
                        .TextWrapping = TextWrapping.Wrap,
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_TextBlock_Caption, 2)

                    If Not SelectedBeatmap.ID = -1 Then
                        UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist
                    Else
                        UI_TextBlock_Caption.Text = _e("MainWindow_unsubmittedBeatmapCantBeExported") & " | " & SelectedBeatmap.Artist
                    End If
                    If Not SelectedBeatmap.Creator = "Unknown" Then
                        UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator
                    End If

                    Dim UI_Checkbox_IsSelected = New CheckBox With {
                        .Content = _e("MainWindow_selectToExport"),
                        .HorizontalAlignment = Windows.HorizontalAlignment.Right,
                        .IsChecked = True,
                        .Margin = New Thickness(10, 5, 0, 0),
                        .VerticalAlignment = Windows.VerticalAlignment.Top}
                    Grid.SetColumn(UI_Checkbox_IsSelected, 2)

                    If SelectedBeatmap.ID = -1 Then
                        With UI_Checkbox_IsSelected
                            .IsChecked = False
                            .IsEnabled = False
                        End With
                        UI_DecoBorderLeft.Fill = Color_999999
                    Else
                        AddHandler(UI_Checkbox_IsSelected.Checked), AddressOf Exporter_AddBeatmapToSelection
                        AddHandler(UI_Checkbox_IsSelected.Unchecked), AddressOf Exporter_RemoveBeatmapFromSelection
                        AddHandler(UI_DecoBorderLeft.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_TextBlock_Title.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                        AddHandler(UI_Thumbnail.MouseUp), AddressOf Exporter_DetermineWheterAddOrRemove
                    End If

                    Dim TagData As New Importer_TagData With {
                        .Beatmap = SelectedBeatmap,
                        .UI_Checkbox_IsSelected = UI_Checkbox_IsSelected,
                        .UI_DecoBorderLeft = UI_DecoBorderLeft,
                        .UI_Grid = UI_Grid,
                        .UI_TextBlock_Caption = UI_TextBlock_Caption,
                        .UI_TextBlock_Title = UI_TextBlock_Title}

                    Exporter_BeatmapList_Tag_Selected.Add(TagData)

                    UI_Checkbox_IsSelected.Tag = TagData
                    UI_Checkbox_IsSelected.Tag = TagData
                    UI_DecoBorderLeft.Tag = TagData
                    UI_Grid.Tag = TagData
                    UI_TextBlock_Caption.Tag = TagData
                    UI_TextBlock_Title.Tag = TagData
                    UI_Thumbnail.Tag = TagData

                    With UI_Grid.Children
                        .Add(UI_Checkbox_IsSelected)
                        .Add(UI_DecoBorderLeft)
                        .Add(UI_TextBlock_Title)
                        .Add(UI_TextBlock_Caption)
                        .Add(UI_Thumbnail)
                    End With

                    ExporterWrapper.Children.Add(UI_Grid)
                Next

                TabberItem_Export.Visibility = Windows.Visibility.Visible
                Tabber.SelectedIndex = 2
        End Select
    End Sub

    Private Sub BeatmapDetails_BeatmapListing_Click(sender As Object, e As RoutedEventArgs) Handles BeatmapDetails_BeatmapListing.Click
        Dim SelectedSender As Button = CType(sender, Button)
        Dim SelectedSender_Tag As String = CStr(SelectedSender.Tag)
        Process.Start("http://osu.ppy.sh/s/" & SelectedSender_Tag)
    End Sub

    Private Sub Button_SyncDo_Click(sender As Object, e As RoutedEventArgs) Handles Button_SyncDo.Click
        If Setting_Tool_CheckFileAssociation Then
            Action_CheckFileAssociation()
        End If

        If Directory.Exists(Setting_osu_SongsPath) And Setting_Messages_Sync_MoreThan1000Sets Then
            Dim counter As Integer = Directory.GetDirectories(Setting_osu_SongsPath).Count
            If counter > 1000 Then
                If MessageBox.Show(_e("MainWindow_youveGotAboutBeatmaps").Replace("%0", counter.ToString), I__MsgBox_DefaultTitle_CanBeDisabled, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.No Then
                    Exit Sub
                End If
            End If
        End If
        Action_Sync_GetIDs()
    End Sub

    Private Sub Client_DownloadStringCompleted(sender As Object, e As Net.DownloadStringCompletedEventArgs) Handles Client.DownloadStringCompleted
        Dim Answer As JObject
        Try
            Answer = JObject.Parse(e.Result)
        Catch ex As Newtonsoft.Json.JsonReaderException
            If Setting_Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_invalidServerResponse") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
                MsgBox(e.Result, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            End If
            TextBlock_Programm_Updater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        Catch ex As System.Reflection.TargetInvocationException
            If Setting_Messages_Updater_UnableToCheckForUpdates Then
                MsgBox(_e("MainWindow_unableToCheckForUpdates") & vbNewLine & "// " & _e("MainWindow_cantConnectToServer") & vbNewLine & vbNewLine & _e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
            End If
            TextBlock_Programm_Updater.Content = _e("MainWindow_unableToCheckForUpdates")
            Exit Sub
        End Try

        If CStr(Answer.SelectToken("latestVersion")) = My.Application.Info.Version.ToString Then
            TextBlock_Programm_Updater.Content = _e("MainWindow_latestVersion")
        Else
            TextBlock_Programm_Updater.Content = _e("MainWindow_updateAvailable").Replace("%0", CStr(Answer.SelectToken("latestVersion")))
            If Setting_Tool_EnableNotifyIcon = 0 Then
                Notify_NextAction = NotifyNextAction.OpenUpdater
                NotifyIcon.ShowBalloonTip("Updater | osu!Sync", _e("MainWindow_aNewVersionIsAvailable").Replace("%0", My.Application.Info.Version.ToString).Replace("%1", CStr(Answer.SelectToken("latestVersion"))), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info)
            End If
            If Setting_Messages_Updater_OpenUpdater Then
                Interface_ShowUpdaterWindow()
            End If
        End If
    End Sub

    Private Sub FadeOut_Completed(sender As Object, e As EventArgs) Handles FadeOut.Completed
        Overlay.Visibility = Windows.Visibility.Hidden
    End Sub

    Private Sub Flyout_BeatmapDetails_RequestBringIntoView(sender As Object, e As RequestBringIntoViewEventArgs) Handles Flyout_BeatmapDetails.RequestBringIntoView
        Flyout_BeatmapDetails.Width = 2 * (Me.Width / 5)
    End Sub

    Private Sub Interface_SetLoader(Optional Message As String = "Please wait")
        Dim UI_ProgressBar = New ProgressBar With {
            .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
            .Visibility = Windows.Visibility.Hidden,
            .Height = 25}
        Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With {
            .Height = 150,
            .HorizontalAlignment = Windows.HorizontalAlignment.Center,
            .IsActive = True,
            .Margin = New Thickness(0, 100, 0, 0),
            .VerticalAlignment = Windows.VerticalAlignment.Center,
            .Width = 150}
        Dim UI_TextBlock_SubTitle As New TextBlock With {
                   .FontSize = 24,
                   .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                   .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                   .Text = Message,
                   .TextAlignment = TextAlignment.Center,
                   .VerticalAlignment = Windows.VerticalAlignment.Center}

        Interface_LoaderText = UI_TextBlock_SubTitle
        Interface_LoaderProgressBar = UI_ProgressBar
        BeatmapWrapper.Children.Clear()
        BeatmapWrapper.Children.Add(UI_ProgressBar)
        BeatmapWrapper.Children.Add(UI_ProgressRing)
        BeatmapWrapper.Children.Add(UI_TextBlock_SubTitle)
    End Sub

    Private Sub Interface_ShowBeatmapDetails(ID As Integer, Details As BeatmapPanelDetails)
        BeatmapDetails_Artist.Text = Details.Artist
        BeatmapDetails_BeatmapListing.Tag = ID
        BeatmapDetails_Creator.Text = Details.Creator
        BeatmapDetails_Title.Text = Details.Title

        ' Ranked status
        Select Case Details.RankedStatus
            Case Convert.ToByte(4)      ' Ranked
                With BeatmapDetails_RankedStatus
                    .Background = Color_008136
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_ranked")
                End With
            Case Convert.ToByte(5)      ' Approved
                With BeatmapDetails_RankedStatus
                    .Background = Color_008136
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_approved")
                End With
            Case Convert.ToByte(6)      ' Pending
                With BeatmapDetails_RankedStatus
                    .Background = Color_8E44AD
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_pending")
                End With
            Case Else
                With BeatmapDetails_RankedStatus
                    .Background = Color_999999
                    .Text = _e("MainWindow_detailsPanel_beatmapStatus_unranked")
                End With
        End Select

        ' Thumbnail
        If File.Exists(Setting_osu_Path & "\Data\bt\" & ID & "l.jpg") Then
            BeatmapDetails_Thumbnail.Source = New BitmapImage(New Uri(Setting_osu_Path & "\Data\bt\" & ID & "l.jpg"))
        Else
            BeatmapDetails_Thumbnail.Source = New BitmapImage(New Uri("Resources/NoThumbnail.png", UriKind.Relative))
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

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TextBlock_Programm_Version.Content = "osu!Sync Version " & My.Application.Info.Version.ToString

        ' Prepare languages
        Action_PrepareLanguages()

        ' Load Configuration
        If File.Exists(I__Path_Programm & "\Settings\Settings.config") Then
            Action_LoadSettings()
        Else
            Dim Window_Welcome As New Window_Welcome
            Window_Welcome.ShowDialog()

            Action_SaveSettings()
        End If

        ' Set settings like NotifyIcon
        Action_Tool_UpdateSettings()

        ' Delete old downloaded beatmaps
        If Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload") Then
            Directory.Delete(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload", True)
        End If

        ' Check For Updates
        Select Case Setting_Tool_CheckForUpdates
            Case 0
                TextBlock_Programm_Updater.Content = _e("MainWindow_checkingForUpdates")
                Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=AutoCheck&updaterInterval=" & Setting_Tool_CheckForUpdates))
                Setting_Tool_LastCheckForUpdates = Date.Now.ToString("dd-MM-yyyy hh:mm:ss")
                Action_SaveSettings()
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

                If DateDiff(DateInterval.Day, Date.ParseExact(Setting_Tool_LastCheckForUpdates, "dd-MM-yyyy hh:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), Date.Now) >= Interval Then
                    TextBlock_Programm_Updater.Content = _e("MainWindow_checkingForUpdates")
                    Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=AutoCheck&updaterInterval=" & Setting_Tool_CheckForUpdates))
                    Setting_Tool_LastCheckForUpdates = Date.Now.ToString("dd-MM-yyyy hh:mm:ss")
                    Action_SaveSettings()
                Else
                    TextBlock_Programm_Updater.Content = _e("MainWindow_updateCheckNotNecessary")
                End If
        End Select

        'Open File
        If I__StartUpArguments IsNot Nothing AndAlso Array.Exists(I__StartUpArguments, Function(s)
                                                                                           If s.Substring(0, 10) = "-openFile=" Then
                                                                                               Importer_FilePath = s.Substring(10)
                                                                                               Return True
                                                                                           Else
                                                                                               Return False
                                                                                           End If
                                                                                       End Function) Then
            Importer_ReadListFile(Importer_FilePath)
        Else
            If Setting_Tool_SyncOnStartup Then
                Action_Sync_GetIDs()
            End If
        End If
    End Sub

    Private Sub MenuItem_File_Export_ConvertSelector_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_ConvertSelector.Click
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
                    Catch ex As System.IO.InvalidDataException
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

    Private Sub MenuItem_File_Export_InstalledBeatmaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_InstalledBeatmaps.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Action_ExportBeatmapDialog(Sync_BeatmapList_Installed)
    End Sub

    Private Sub MenuItem_File_Export_SelectedMaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_SelectedMaps.Click
        If Sync_Done = False Then
            MsgBox(_e("MainWindow_youNeedToSyncFirst"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If

        Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed, UpdateBeatmapDisplayDestinations.Exporter)
    End Sub

    Private Sub MenuItem_File_OpenBeatmapList_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_OpenBeatmapList.Click
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

    Private Sub MenuItem_Help_About_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_About.Click
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

    Private Sub MenuItem_Help_Updater_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_Updater.Click
        Interface_ShowUpdaterWindow()
    End Sub

    Private Sub MenuItem_Program_Exit_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Exit.Click
        Application.Current.Shutdown()
    End Sub

    Private Sub MenuItem_Program_MinimizeToTray_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_MinimizeToTray.Click
        Select Case Setting_Tool_EnableNotifyIcon
            Case 0, 2, 3
                Me.Visibility = Windows.Visibility.Hidden
                If Setting_Tool_EnableNotifyIcon = 3 Then
                    NotifyIcon.Visibility = Windows.Visibility.Visible
                End If
            Case Else
                MenuItem_Program_MinimizeToTray.IsEnabled = False
        End Select
    End Sub

    Private Sub MenuItem_Program_RunOsu_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_RunOsu.Click
        Action_StartOrFocusOsu()
    End Sub

    Private Sub MenuItem_Program_Settings_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Settings.Click
        Interface_ShowSettingsWindow()
        Action_Tool_UpdateSettings()
    End Sub

    Private Sub NotifyIcon_Exit_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_Exit.Click
        Application.Current.Shutdown()
    End Sub

    Private Sub NotifyIcon_RunOsu_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_RunOsu.Click
        Action_StartOrFocusOsu()
    End Sub

    Private Sub NotifyIcon_ShowHide_Click(sender As Object, e As RoutedEventArgs) Handles NotifyIcon_ShowHide.Click
        If Me.IsVisible Then
            Me.Visibility = Windows.Visibility.Hidden
        Else
            Me.Visibility = Windows.Visibility.Visible
        End If
    End Sub

    Private Sub NotifyIcon_TrayBalloonTipClicked(sender As Object, e As RoutedEventArgs) Handles NotifyIcon.TrayBalloonTipClicked
        Select Case Notify_NextAction
            Case NotifyNextAction.OpenUpdater
                Interface_ShowUpdaterWindow()
        End Select
    End Sub

    Private Sub NotifyIcon_TrayMouseDoubleClick(sender As Object, e As RoutedEventArgs) Handles NotifyIcon.TrayMouseDoubleClick
        If Me.IsVisible Then
            Me.Visibility = Windows.Visibility.Hidden
        Else
            Me.Visibility = Windows.Visibility.Visible
            Me.Focus()
            If Setting_Tool_EnableNotifyIcon = 3 Then
                NotifyIcon.Visibility = Windows.Visibility.Collapsed
            End If
        End If
    End Sub

    Private Sub TextBlock_Programm_Updater_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Programm_Updater.MouseDown
        Interface_ShowUpdaterWindow()
    End Sub

    Private Sub TextBlock_Programm_Version_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Programm_Version.MouseDown
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

#Region "Background Worker"
#Region "BGW__Action_Sync_GetIDs"
    Private Sub BGW__Action_Sync_GetIDs_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles BGW__Action_Sync_GetIDs.DoWork
        Dim Arguments As New BGWcallback__Action_Sync_GetIDs
        Arguments = TryCast(e.Argument, BGWcallback__Action_Sync_GetIDs)
        Dim Answer As New BGWcallback__Action_Sync_GetIDs

        If Not Directory.Exists(Setting_osu_SongsPath) Then
            Answer.Return__Status = BGWcallback_ActionSyncGetIDs_ReturnStatus.FolderDoesNotExist
            e.Result = Answer
            Exit Sub
        End If

        Select Case Arguments.Arg__Mode
            Case BGWcallback_ActionSyncGetIDs_ArgMode.Sync
                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With {
                                    .Progress__CurrentAction = BGWcallback_ActionSyncGetIDs_ProgressCurrentAction.CountingTotalFolders,
                                    .Progress__Current = Directory.GetDirectories(Setting_osu_SongsPath).Count})

                Dim Beatmap_InvalidFolder As String = ""
                Dim Beatmap_InvalidIDBeatmaps As String = ""
                Dim UseDatabase As Boolean = True
                If UseDatabase Then
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
                            Reader.ReadString()                                     ' ´Song source
                            Reader.ReadString()                                     '  Song tags
                            Reader.ReadInt16()                                      '  Online offset 
                            Reader.ReadString()                                     '  Font used for the title of the song 
                            Reader.ReadBytes(10)
                            Reader.ReadString()                                     '  Folder name of the beatmap, relative to Songs folder 
                            Reader.ReadBytes(18)

                            If Not FoundIDs.Contains(BeatmapDetails.ID) Then
                                FoundIDs.Add(BeatmapDetails.ID)
                                Answer.Return__Sync_BeatmapList_Installed.Add(BeatmapDetails)
                                Answer.Return__Sync_BeatmapList_ID_Installed.Add(CInt(BeatmapDetails.ID))
                                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With {
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
                                    Dim FileReader As New System.IO.StreamReader(FileInDir)
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
                                        If Found_ID And Found_Title And Found_Artist And Found_Creator Then
                                            Exit For
                                        End If

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
                                    Answer.Return__Sync_BeatmapList_ID_Installed.Add(CInt(BeatmapDetails.ID))
                                    BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With {
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
                    If Not Beatmap_InvalidFolder = "" Then
                        Answer.Return__Sync_Warnings += "=====   " & _e("MainWindow_ignoredFolders") & "   =====" & vbNewLine & _e("MainWindow_itSeemsThatSomeFoldersCantBeParsedTheyllBeIgnored") & vbNewLine & vbNewLine & "// " & _e("MainWindow_folders") & ":" & vbNewLine & Beatmap_InvalidFolder & vbNewLine & vbNewLine
                    End If
                    If Not Beatmap_InvalidIDBeatmaps = "" Then
                        Answer.Return__Sync_Warnings += "=====   " & _e("MainWindow_unableToGetId") & "   =====" & vbNewLine & _e("MainWindow_unableToGetIdOfSomeBeatmapsTheyllBeHandledAsUnsubmitted") & vbNewLine & vbNewLine & "// " & _e("MainWindow_beatmaps") & ":" & vbNewLine & Beatmap_InvalidIDBeatmaps & vbNewLine & vbNewLine & vbNewLine
                    End If
                End If
                e.Result = Answer
        End Select
    End Sub

    Private Sub BGW__Action_Sync_GetIDs_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles BGW__Action_Sync_GetIDs.ProgressChanged
        Dim Answer As New BGWcallback__Action_Sync_GetIDs
        Answer = CType(e.UserState, BGWcallback__Action_Sync_GetIDs)
        Select Case Answer.Progress__CurrentAction
            Case BGWcallback_ActionSyncGetIDs_ProgressCurrentAction.Sync
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Progress__Current.ToString) & vbNewLine & _e("MainWindow_andStillWorking")
                With Interface_LoaderProgressBar
                    .Value = Answer.Progress__Current
                    .Visibility = Windows.Visibility.Visible
                End With
            Case BGWcallback_ActionSyncGetIDs_ProgressCurrentAction.Done
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsInTotalParsed").Replace("%0", Answer.Progress__Current.ToString) & vbNewLine & _e("MainWindow_generatingInterface")
            Case BGWcallback_ActionSyncGetIDs_ProgressCurrentAction.CountingTotalFolders
                Interface_LoaderProgressBar.Maximum = Answer.Progress__Current
        End Select
    End Sub

    Private Sub BGW__Action_Sync_GetIDs_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles BGW__Action_Sync_GetIDs.RunWorkerCompleted
        Dim Answer As New BGWcallback__Action_Sync_GetIDs
        Answer = TryCast(e.Result, BGWcallback__Action_Sync_GetIDs)
        Select Case Answer.Return__Status
            Case 0
                Interface_LoaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Return__Sync_BeatmapList_ID_Installed.Count.ToString)
                If Not Answer.Return__Sync_Warnings = "" Then
                    If MessageBox.Show(_e("MainWindow_itSeemsThatSomeBeatmapsDiffer") & vbNewLine &
                                       _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Answer.Return__Sync_Warnings, _e("MainWindow_exceptions"), "Sync")
                        Window_Message.ShowDialog()
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
            Case BGWcallback_ActionSyncGetIDs_ReturnStatus.FolderDoesNotExist
                MsgBox(_e("MainWindow_unableToFindOsuFolderPleaseSpecify"), MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                Interface_ShowSettingsWindow(1)

                Dim UI_TextBlock As New TextBlock With {
                    .FontSize = 72,
                    .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Margin = New Thickness(0, 100, 0, 0),
                    .Text = _e("MainWindow_lastSyncFailed"),
                    .VerticalAlignment = Windows.VerticalAlignment.Center}
                Dim UI_TextBlock_SubTitle As New TextBlock With {
                    .FontSize = 24,
                    .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Text = _e("MainWindow_pleaseRetry"),
                    .VerticalAlignment = Windows.VerticalAlignment.Center}

                With BeatmapWrapper.Children
                    .Clear()
                    .Add(UI_TextBlock)
                    .Add(UI_TextBlock_SubTitle)
                End With
                Button_SyncDo.IsEnabled = True
        End Select
    End Sub
#End Region
#End Region

#Region "Exporter"
#Region "Actions « Exporter"
    Private Sub Exporter_AddBeatmapToSelection(sender As Object, e As EventArgs)
        Dim SelectedSender As CheckBox = CType(sender, CheckBox)
        Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
        Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)
        Exporter_BeatmapList_Tag_Unselected.Remove(SelectedSender_Tag)
        Exporter_BeatmapList_Tag_Selected.Add(SelectedSender_Tag)

        If Exporter_BeatmapList_Tag_Selected.Count > 0 Then
            Export_Run.IsEnabled = True
        Else
            Export_Run.IsEnabled = False
        End If

        SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_27AE60        ' Color_27AE60 = Light Green
    End Sub

    Private Sub Exporter_DetermineWheterAddOrRemove(sender As Object, e As EventArgs)
        If TypeOf sender Is TextBlock Then
            Dim SelectedSender As TextBlock = CType(sender, TextBlock)

            Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
            Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)

            If SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked Then
                Exporter_BeatmapList_Tag_Selected.Remove(SelectedSender_Tag)

                If Exporter_BeatmapList_Tag_Selected.Count = 0 Then
                    Export_Run.IsEnabled = False
                End If

                SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked = False
                SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_999999            ' Color_999999 = Light Gray
            Else
                Exporter_BeatmapList_Tag_Selected.Add(SelectedSender_Tag)

                If Exporter_BeatmapList_Tag_Selected.Count > 0 Then
                    Export_Run.IsEnabled = True
                Else
                    Export_Run.IsEnabled = False
                End If

                SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked = True
                SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_27AE60        ' Color_27AE60 = Light Green
            End If
        ElseIf TypeOf sender Is Rectangle Then
            Dim SelectedSender As Rectangle = CType(sender, Rectangle)

            Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
            Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)

            If SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked Then
                Exporter_BeatmapList_Tag_Selected.Remove(SelectedSender_Tag)

                If Exporter_BeatmapList_Tag_Selected.Count = 0 Then
                    Export_Run.IsEnabled = False
                End If

                SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked = False
                SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_999999            ' Color_999999 = Light Gray
            Else
                Exporter_BeatmapList_Tag_Selected.Add(SelectedSender_Tag)

                If Exporter_BeatmapList_Tag_Selected.Count > 0 Then
                    Export_Run.IsEnabled = True
                Else
                    Export_Run.IsEnabled = False
                End If

                SelectedSender_Tag.UI_Checkbox_IsSelected.IsChecked = True
                SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_27AE60        ' Color_27AE60 = Light Green
            End If
        End If
    End Sub

    Private Sub Exporter_RemoveBeatmapFromSelection(sender As Object, e As EventArgs)
        Dim SelectedSender As CheckBox = CType(sender, CheckBox)
        Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
        Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)
        Exporter_BeatmapList_Tag_Selected.Remove(SelectedSender_Tag)
        Exporter_BeatmapList_Tag_Unselected.Add(SelectedSender_Tag)

        If Exporter_BeatmapList_Tag_Selected.Count = 0 Then
            Export_Run.IsEnabled = False
        End If

        SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_999999            ' Color_999999 = Light Gray
    End Sub
#End Region
#Region "Controls « Exporter"
    Private Sub Export_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Export_Cancel.Click
        TabberItem_Export.Visibility = Windows.Visibility.Collapsed
        Tabber.SelectedIndex = 0
        ExporterWrapper.Children.Clear()
    End Sub

    Private Sub Export_InvertSelection_Click(sender As Object, e As RoutedEventArgs) Handles Export_InvertSelection.Click
        ' Save unselected elements
        Dim ListUnselected As New List(Of Importer_TagData)
        ListUnselected = Exporter_BeatmapList_Tag_Unselected.ToList
        ' Save selected elements
        Dim ListSelected As New List(Of Importer_TagData)
        ListSelected = Exporter_BeatmapList_Tag_Selected.ToList

        ' --- Loop for selected elements
        Dim LoopPreviousCount As Integer = 0
        Dim LoopCount As Integer = 0
        Do While LoopCount < ListSelected.Count
            ListSelected(LoopCount).UI_Checkbox_IsSelected.IsChecked = False
            LoopCount += 1
        Loop

        ' --- Loop for unselected elements
        LoopPreviousCount = 0
        LoopCount = 0
        Do While LoopCount < ListUnselected.Count
            ListUnselected(LoopCount).UI_Checkbox_IsSelected.IsChecked = True
            LoopCount += 1
        Loop
    End Sub

    Private Sub Export_Run_Click(sender As Object, e As RoutedEventArgs) Handles Export_Run.Click
        Dim Result As New List(Of Beatmap)

        For Each Item As Importer_TagData In Exporter_BeatmapList_Tag_Selected
            Result.Add(Item.Beatmap)
        Next
        Action_ExportBeatmapDialog(Result, "Export selected beatmaps")
        TabberItem_Export.Visibility = Windows.Visibility.Collapsed
        Tabber.SelectedIndex = 0
        ExporterWrapper.Children.Clear()
    End Sub
#End Region
#End Region

#Region "Importer"
#Region "Actions « Importer"
    Private Sub Importer_AddBeatmapToSelection(sender As Object, e As EventArgs)
        Dim SelectedSender As CheckBox = CType(sender, CheckBox)
        Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
        Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)
        Importer_BeatmapList_Tag_ToInstall.Add(SelectedSender_Tag)
        Importer_BeatmapList_Tag_LeftOut.Remove(SelectedSender_Tag)

        If Importer_BeatmapList_Tag_ToInstall.Count > 0 Then
            Importer_Run.IsEnabled = True
            Importer_Cancel.IsEnabled = True
        Else
            Importer_Run.IsEnabled = False
        End If
        Importer_UpdateInfo("osu!Sync")

        SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_E74C3C            ' Color_E74C3C = Red
    End Sub

    Private Sub Importer_DownloadBeatmap()
        Importer_Progress.Value = 0
        Importer_Progress.IsIndeterminate = True
        Dim RequestURI As String
        TextBlock_Progress.Content = _e("MainWindow_fetching").Replace("%0", CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID))
        Select Case Setting_Tool_DownloadMirror
            Case 1
                Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": Loli.al"
                RequestURI = "http://loli.al/s/" + CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID)
            Case Else
                Importer_DownloadMirrorInfo.Text = _e("MainWindow_downloadMirror") & ": Bloodcat.com"
                RequestURI = "http://bloodcat.com/osu/s/" + CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID)
        End Select

        With Importer_BeatmapList_Tag_ToInstall.First
            .UI_DecoBorderLeft.Fill = Color_3498DB
            .UI_Checkbox_IsSelected.IsEnabled = False
            .UI_Checkbox_IsSelected.IsThreeState = False
            .UI_Checkbox_IsSelected.IsChecked = Nothing
            .UI_Checkbox_IsInstalled.IsThreeState = True
            .UI_Checkbox_IsInstalled.IsChecked = Nothing
        End With

        Importer_UpdateInfo(_e("MainWindow_fetching1"))

        Dim req As HttpWebRequest = DirectCast(HttpWebRequest.Create(RequestURI), HttpWebRequest)
        Dim Res As WebResponse
        Try
            Res = req.GetResponse()
        Catch ex As WebException
            If MessageBox.Show(_e("MainWindow_itLooksLikeSomethingIsWrongWithThisDownload"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then
                'Yes
                Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_E67E2E      ' Orange
                Importer_BeatmapList_Tag_Failed.Add(Importer_BeatmapList_Tag_ToInstall.First)
                Importer_BeatmapList_Tag_ToInstall.Remove(Importer_BeatmapList_Tag_ToInstall.First)
                Importer_Downloader_ToNextDownload()
            Else
                'No
                Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_E67E2E      ' Orange
                Importer_Info.Text = _e("MainWindow_installing")
                Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", Importer_BeatmapList_Tag_Done.Count.ToString)
                If Importer_BeatmapList_Tag_LeftOut.Count > 0 Then
                    Importer_Info.Text += " | " & _e("MainWindow_leftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString)
                End If
                Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString)

                TextBlock_Progress.Content = _e("MainWindow_installingFiles")

                For Each FilePath In Directory.GetFiles(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload")
                    File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath))
                Next
                With Importer_Progress
                    .IsIndeterminate = False
                    .Visibility = Windows.Visibility.Hidden
                End With

                TextBlock_Progress.Content = ""
                Importer_Info.Text = _e("MainWindow_aborted")
                Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", Importer_BeatmapList_Tag_Done.Count.ToString)
                If Importer_BeatmapList_Tag_LeftOut.Count > 0 Then
                    Importer_Info.Text += " | " & _e("MainWindow_leftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString)
                End If
                Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString)
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
            Importer_CurrentFileName = response.Headers("Content-Disposition").Substring(response.Headers("Content-Disposition").IndexOf("filename=") + 10).Replace("""", "")
            If Importer_CurrentFileName.Substring(Importer_CurrentFileName.Length - 1) = ";" Then
                Importer_CurrentFileName = Importer_CurrentFileName.Substring(0, Importer_CurrentFileName.Length - 1)
            End If
        Else
            Importer_CurrentFileName = CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID) & ".osz"
        End If

        TextBlock_Progress.Content = _e("MainWindow_downloading").Replace("%0", CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID))
        Importer_UpdateInfo(_e("MainWindow_downloading1"))
        Importer_Progress.IsIndeterminate = False
        Importer_Downloader.DownloadFileAsync(New Uri(RequestURI), Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\" & Importer_CurrentFileName)
    End Sub

    Private Sub Importer_Downloader_ToNextDownload()
        If Importer_BeatmapList_Tag_ToInstall.Count > 0 Then
            If Not Setting_Tool_ImporterAutoInstallCounter = 0 And Setting_Tool_ImporterAutoInstallCounter <= Importer_Counter Then
                Importer_Counter = 0
                With Importer_Progress
                    .IsIndeterminate = True
                End With

                Importer_UpdateInfo(_e("MainWindow_installing"))
                TextBlock_Progress.Content = _e("MainWindow_installingFiles")

                For Each FilePath In Directory.GetFiles(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload")
                    If Not File.Exists(Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then
                        File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath))
                    Else
                        File.Delete(FilePath)
                    End If
                Next
            End If
            Importer_DownloadBeatmap()
        Else
            With Importer_Progress
                .IsIndeterminate = True
                .Value = 0
            End With

            Importer_UpdateInfo(_e("MainWindow_installing"))
            TextBlock_Progress.Content = _e("MainWindow_installingFiles")

            For Each FilePath In Directory.GetFiles(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload")
                If Not File.Exists(Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath)) Then
                    File.Move(FilePath, Setting_osu_SongsPath & "\" & Path.GetFileName(FilePath))
                Else
                    File.Delete(FilePath)
                End If
            Next
            With Importer_Progress
                .IsIndeterminate = False
                .Visibility = Windows.Visibility.Hidden
            End With

            TextBlock_Progress.Content = ""
            Importer_UpdateInfo(_e("MainWindow_done"))

            My.Computer.Audio.PlaySystemSound(System.Media.SystemSounds.Beep)
            If Setting_Tool_EnableNotifyIcon = 0 Then
                NotifyIcon.ShowBalloonTip("osu!Sync", _e("MainWindow_installationFinished") & vbNewLine &
                        _e("MainWindow_setsDone").Replace("%0", Importer_BeatmapList_Tag_Done.Count.ToString) & vbNewLine &
                        _e("MainWindow_setsFailed").Replace("%0", Importer_BeatmapList_Tag_Failed.Count.ToString) & vbNewLine &
                         _e("MainWindow_setsLeftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString) & vbNewLine &
                         _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString), BalloonIcon.None)
            End If
            MsgBox(_e("MainWindow_installationFinished") & vbNewLine &
                        _e("MainWindow_setsDone").Replace("%0", Importer_BeatmapList_Tag_Done.Count.ToString) & vbNewLine &
                        _e("MainWindow_setsFailed").Replace("%0", Importer_BeatmapList_Tag_Failed.Count.ToString) & vbNewLine &
                         _e("MainWindow_setsLeftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString) & vbNewLine &
                         _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString) & vbNewLine & vbNewLine &
                    _e("MainWindow_pressF5"))

            If Not Process.GetProcessesByName("osu!").Count > 0 Then
                If MessageBox.Show(_e("MainWindow_doYouWantToStartOsuNow"), I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                    Action_StartOrFocusOsu()
                End If
            End If
            Button_SyncDo.IsEnabled = True
            Importer_Cancel.IsEnabled = True
        End If
    End Sub

    Private Sub Importer_Init()
        Button_SyncDo.IsEnabled = False
        Importer_Run.IsEnabled = False
        Importer_Cancel.IsEnabled = False
        Importer_Progress.Visibility = Windows.Visibility.Visible
        If Not Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\") Then
            Directory.CreateDirectory(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\")
        End If
        Importer_DownloadBeatmap()
    End Sub

    Private Sub Importer_ReadListFile(ByRef FilePath As String)
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

    Private Sub Importer_RemoveBeatmapFromSelection(sender As Object, e As EventArgs)
        Dim SelectedSender As CheckBox = CType(sender, CheckBox)
        Dim SelectedSender_Tag As Importer_TagData = CType(SelectedSender.Tag, Importer_TagData)
        Dim SelectedSender_Beatmap As Beatmap = CType(SelectedSender_Tag.Beatmap, Beatmap)
        Importer_BeatmapList_Tag_ToInstall.Remove(SelectedSender_Tag)
        Importer_BeatmapList_Tag_LeftOut.Add(SelectedSender_Tag)
        If Importer_BeatmapList_Tag_ToInstall.Count = 0 Then
            Importer_Run.IsEnabled = False
            Importer_Cancel.IsEnabled = True
        End If
        Importer_UpdateInfo("osu!Sync")

        SelectedSender_Tag.UI_DecoBorderLeft.Fill = Color_999999            ' Color_999999 = Light Gray
    End Sub

    Private Sub Importer_UpdateInfo(ByRef Title As String)
        Importer_Info.Text = Title
        If Title = _e("MainWindow_fetching1") Or Title = _e("MainWindow_downloading1") Or Title = _e("MainWindow_installing") Then
            Importer_Info.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", Importer_BeatmapList_Tag_ToInstall.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsDone").Replace("%0", Importer_BeatmapList_Tag_Done.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsFailed").Replace("%0", Importer_BeatmapList_Tag_Failed.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsLeftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString)
        Else
            Importer_Info.Text += " | " & _e("MainWindow_setsLeft").Replace("%0", Importer_BeatmapList_Tag_ToInstall.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsLeftOut").Replace("%0", Importer_BeatmapList_Tag_LeftOut.Count.ToString)
            Importer_Info.Text += " | " & _e("MainWindow_setsTotal").Replace("%0", Importer_BeatmapsTotal.ToString)
        End If
    End Sub
#End Region
#Region "Events « Importer "
    Private Sub Importer_Cancel_Click(sender As Object, e As RoutedEventArgs) Handles Importer_Cancel.Click
        Tabber.SelectedIndex = 0
        TabberItem_Import.Visibility = Windows.Visibility.Collapsed
        ImporterWrapper.Children.Clear()
    End Sub

    Private Sub Importer_Downloader_DownloadFileCompleted(sender As Object, e As ComponentModel.AsyncCompletedEventArgs) Handles Importer_Downloader.DownloadFileCompleted
        Importer_Counter += 1
        If File.ReadAllBytes(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\" & Importer_CurrentFileName).Length = 0 Then
            ' File Empty
            Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_E67E2E      ' Orange
            If File.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\" & Importer_CurrentFileName) Then
                File.Delete(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\" & Importer_CurrentFileName)
            End If
            MsgBox("It seems that the beatmap with the ID " & Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID & " doesn't exist.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)

            Importer_BeatmapList_Tag_Failed.Add(Importer_BeatmapList_Tag_ToInstall.First)
            Importer_BeatmapList_Tag_ToInstall.Remove(Importer_BeatmapList_Tag_ToInstall.First)
            Importer_Downloader_ToNextDownload()
        Else
            ' File Normal
            Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_8E44AD
            Importer_BeatmapList_Tag_Done.Add(Importer_BeatmapList_Tag_ToInstall.First)
            Importer_BeatmapList_Tag_ToInstall.Remove(Importer_BeatmapList_Tag_ToInstall.First)
            Importer_Downloader_ToNextDownload()
        End If
    End Sub

    Private Sub Importer_Downloader_DownloadProgressChanged(sender As Object, e As Net.DownloadProgressChangedEventArgs) Handles Importer_Downloader.DownloadProgressChanged
        Importer_Progress.Value = e.ProgressPercentage
    End Sub

    Private Sub Importer_DownloadMirrorInfo_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Importer_DownloadMirrorInfo.MouseDown
        Select Case Setting_Tool_DownloadMirror
            Case 0
                Process.Start("http://bloodcat.com/osu")
            Case 1
                Process.Start("http://loli.al/")
        End Select
    End Sub

    Private Sub Importer_Run_Click(sender As Object, e As RoutedEventArgs) Handles Importer_Run.Click
        Importer_Init()
    End Sub
#End Region
#End Region
End Class
