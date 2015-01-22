Imports System.IO
Imports Newtonsoft.Json, Newtonsoft.Json.Linq
Imports System.Net
Imports System.Runtime.InteropServices, System.Runtime.Serialization.Formatters.Binary
Imports System.Windows.Media.Animation

Public Class Beatmap
    Public Property ID As Integer
    Public Property Title As String
    Public Property Artist As String
    Public Property Creator As String = "Unknown"
End Class

Public Class BGWcallback__Action_Sync_GetIDs
    Public Property Arg__Mode As Integer         ' 0 = Sync | 1 = LoadFromCache
    Public Property Arg__AutoSync As Boolean = False
    Public Property Return__Status As Integer   ' 1 = FolderDoesntExist | 2 = LoadedFromCache
    Public Property Return__Sync_BeatmapList_Installed As New List(Of Beatmap)
    Public Property Return__Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Public Property Return__Sync_Cache_Time As String
    Public Property Return__Sync_Warnings As String
    Public Property Progress__Current As Integer
    Public Property Progress__CurrentAction As Integer  ' 0 = Sync | 1 = Writing Cache | 2 = Done | 3 = CacheFileOutdatedAndSyncing
End Class

Class MainWindow
    Private WithEvents Client As New WebClient
    Private WithEvents FadeOut As New DoubleAnimation()
    Private FadeOut_Status As String = "FadeOut"
    Private Sync_BeatmapList_Installed As New List(Of Beatmap)
    Private Sync_BeatmapList_ID_Installed As New List(Of Integer)
    Private Sync_Done As Boolean = False
    Private Sync_Done_ImporterRequest As Boolean = False
    Private Sync_Done_ImporterRequest_SaveValue As New List(Of Beatmap)
    Private Sync_LoadedFromCache As Boolean = False

    Private Exporter_BeatmapList_Tag_Selected As New List(Of Importer_TagData)

    Private WithEvents Importer_CurrentFileName As String
    Private WithEvents Importer_Downloader As New Net.WebClient
    Private Importer_BeatmapList_Tag_ToInstall As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_LeftOut As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_Done As New List(Of Importer_TagData)
    Private Importer_BeatmapList_Tag_Failed As New List(Of Importer_TagData)
    Private Importer_BeatmapsTotal As Integer
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
                Dim CurrentBeatmap As New Beatmap With { _
                    .ID = CInt(SelectedToken.SelectToken("id")),
                    .Title = CStr(SelectedToken.SelectToken("title")),
                    .Artist = CStr(SelectedToken.SelectToken("artist"))}
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
                MessageBox_Content = "It looks like some file extensions aren't associated with osu!Sync." & vbNewLine & "Do you want to fix that?"
            Else
                MessageBox_Content = "It looks like some file extensions have got wrong values." & vbNewLine & "Do you want to fix that?"
            End If
            If MessageBox.Show(MessageBox_Content, I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                Dim RegisterError As Boolean = False
                Dim RegisterCounter As Integer = 0
                For Each Extension As String In FileExtensions
                    If CreateFileAssociation(Extension, _
                                                             FileExtensionsLong(RegisterCounter), _
                                                             FileExtensionsDescription(RegisterCounter), _
                                                             FileExtensionsIcon(RegisterCounter), _
                                                             System.Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
                        RegisterCounter += 1
                    Else
                        RegisterError = True
                        Exit For
                    End If
                Next

                If Not RegisterError Then
                    MsgBox("File association successfully registered, thank you :).", MsgBoxStyle.Information, I__MsgBox_DefaultTitle)
                Else
                    MsgBox("Unable to register file association.", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                End If
            End If
        End If
    End Sub

    Public Sub Action_ExportBeatmapDialog(ByRef Source As List(Of Beatmap), Optional ByRef DialogTitle As String = "Export installed beatmaps")
        Dim Dialog_SaveFile As New Microsoft.Win32.SaveFileDialog()
        With Dialog_SaveFile
            .AddExtension = True
            .Filter = "Compressed osu!Sync Beatmap List|*.nw520-osblx|osu!Sync Beatmap List|*.nw520-osbl|HTML page (Can't be imported)|*.html|Text file (Can't be imported)|*.txt"
            .OverwritePrompt = True
            .Title = DialogTitle
            .ValidateNames = True
            .ShowDialog()
        End With
        If Dialog_SaveFile.FileName = "" Then
            Action_OverlayShow("Export aborted", "")
            Action_OverlayFadeOut()
            Exit Sub
        End If

        Select Case Dialog_SaveFile.FilterIndex
            Case 1      '.nw520-osblx
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
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
                    Dim Content_Json As String
                    Content_Json = JsonConvert.SerializeObject(Content)
                    File.Write(CompressString(Content_Json))
                    File.Close()

                    Dim Failed As String = ""
                    If Not Failed_Unsubmitted = "" Then
                        Failed += "======   Unsubmitted Beatmap Sets   =====" & vbNewLine & "Unsubmitted beatmap sets can't be exported to this format." & vbNewLine & vbNewLine & "// Beatmap set(s): " & Failed_Unsubmitted & vbNewLine & vbNewLine
                    End If
                    If Not Failed_Alread_Assigned = "" Then
                        Failed += "=====   ID already assigned   =====" & vbNewLine & "Beatmap IDs can be used only for one set." & vbNewLine & vbNewLine & "// Beatmap set(s): " & Failed_Alread_Assigned
                    End If

                    If Not Failed = "" Then
                        If MessageBox.Show("Some beatmap sets hadn't been exported." & vbNewLine & _
                                "Do you want to check which beatmap sets are affected?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                            Dim Window_Message As New Window_MessageWindow
                            Window_Message.SetMessage(Failed, "Skipped Beatmaps", "Export")
                            Window_Message.ShowDialog()
                        End If
                    End If
                End Using
                Action_OverlayShow("Export completed", "Exported as OSBLX-File")
                Action_OverlayFadeOut()
            Case 2      '.nw520-osbl
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
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
                    Dim Serializer = New JsonSerializer()
                    Serializer.Serialize(File, Content)
                    File.Close()

                    Dim Failed As String = ""
                    If Not Failed_Unsubmitted = "" Then
                        Failed += "======   Unsubmitted Beatmap Sets   =====" & vbNewLine & "Unsubmitted beatmap sets can't be exported to this format." & vbNewLine & vbNewLine & "// Beatmap set(s): " & Failed_Unsubmitted & vbNewLine & vbNewLine
                    End If
                    If Not Failed_Alread_Assigned = "" Then
                        Failed += "=====   ID already assigned   =====" & vbNewLine & "Beatmap IDs can be used only for one set." & vbNewLine & vbNewLine & "// Beatmap set(s): " & Failed_Alread_Assigned
                    End If

                    If Not Failed = "" Then
                        If MessageBox.Show("Some beatmap sets hadn't been exported." & vbNewLine & _
                                "Do you want to check which beatmap sets are affected?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                            Dim Window_Message As New Window_MessageWindow
                            Window_Message.SetMessage(Failed, "Skipped Beatmaps", "Export")
                            Window_Message.ShowDialog()
                        End If
                    End If
                End Using
                Action_OverlayShow("Export completed", "Exported as OSBL-File")
                Action_OverlayFadeOut()
            Case 3      '.html
                Dim Failed As String = ""
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    Dim HTML_Source As String = "<!doctype html>" & vbNewLine & _
                        "<!-- Information: This file was generated with osu!Sync " & My.Application.Info.Version.ToString & " by naseweis520 (http://naseweis520.ml/) | " & DateTime.Now.ToString("dd.MM.yyyy") & " -->" & vbNewLine & _
                        "<html>" & vbNewLine & _
                        "<head><meta charset=""utf-8""><meta name=""author"" content=""naseweis520, osu!Sync""/><meta name=""generator"" content=""osu!Sync " & My.Application.Info.Version.ToString & """/><meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=yes""/><title>Beatmap List | osu!Sync</title><link rel=""icon"" type=""image/png"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Favicon.png""/><link href=""http://fonts.googleapis.com/css?family=Open+Sans:400,300,600,700"" rel=""stylesheet"" type=""text/css"" /><link href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/style.css"" rel=""stylesheet"" type=""text/css""/><link rel=""stylesheet"" type=""text/css"" href=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/css/tooltipster.css""/></head>" & vbNewLine & _
                        "<body>" & vbNewLine & _
                        "<div id=""Wrapper"">" & vbNewLine & _
                        vbTab & "<header><p>Beatmap List | osu!Sync</p></header>" & vbNewLine & _
                        vbTab & "<div id=""Sort""><ul><li><strong>Sort by...</strong></li><li><a class=""SortParameter"" href=""#Sort_Artist"">Artist</a></li><li><a class=""SortParameter"" href=""#Sort_Creator"">Creator</a></li><li><a class=""SortParameter"" href=""#Sort_SetName"">Name</a></li><li><a class=""SortParameter"" href=""#Sort_SetID"">Set ID</a></li></ul></div>" & vbNewLine & _
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
                    HTML_Source += "</div>" & vbNewLine & _
                    "</div>" & vbNewLine & _
                    "<footer><p>Generated with osu!Sync, a free tool made by <a href=""http://naseweis520.ml/"" target=""_blank"">naseweis520</a>.</p></footer>" & vbNewLine & _
                    "<script src=""http://code.jquery.com/jquery-latest.min.js""></script><script src=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/js/jquery.tooltipster.min.js""></script><script src=""https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/script.js""></script>" & vbNewLine & _
                    "</body>" & vbNewLine & _
                    "</html>"
                    File.Write(HTML_Source)
                    File.Close()
                    If Not Failed = "" Then
                        Failed = Failed.Insert(0, "=====   Unsubmitted Beatmap Sets   =====" & vbNewLine & "Unsubmitted beatmap sets can't be exported to this format." & vbNewLine & vbNewLine & "// Beatmap set(s):")
                        If MessageBox.Show("Some beatmap sets hadn't been exported." & vbNewLine & _
                                "Do you want to check which beatmap sets are affected?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                            Dim Window_Message As New Window_MessageWindow
                            Window_Message.SetMessage(Failed, "Skipped Beatmaps", "Export")
                            Window_Message.ShowDialog()
                        End If
                    End If
                End Using
                Action_OverlayShow("Export completed", "Exported as HTML-File")
                Action_OverlayFadeOut()
            Case 4     '.txt
                Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                    Dim Content As String = _
                        "Information: This file was generated with osu!Sync " & My.Application.Info.Version.ToString & " by naseweis520 (http://naseweis520.ml/) | " & DateTime.Now.ToString("dd.MM.yyyy") & vbNewLine & vbNewLine
                    For Each SelectedBeatmap As Beatmap In Source
                        Content += "=====   " & SelectedBeatmap.ID & "   =====" & vbNewLine & _
                            "Artist: " & vbTab & SelectedBeatmap.Artist & vbNewLine & _
                            "ID: " & vbTab & vbTab & SelectedBeatmap.ID & vbNewLine & _
                            "Name: " & vbTab & SelectedBeatmap.Title & vbNewLine & vbNewLine
                    Next
                    File.Write(Content)
                    File.Close()
                End Using
                Action_OverlayShow("Export completed", "Exported as TXT-File")
                Action_OverlayFadeOut()
        End Select
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

    Private Sub Action_StartOrFocusOsu()
        If Not Process.GetProcessesByName("osu!").Count > 0 Then
            If File.Exists(Setting_osu_Path & "\osu!.exe") Then
                Process.Start(Setting_osu_Path & "\osu!.exe")
            Else
                MsgBox("Unable to find osu!.exe", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            End If
        Else
            For Each ObjProcess As Process In Process.GetProcessesByName("osu!")
                AppActivate(ObjProcess.Id)
                ShowWindow(ObjProcess.MainWindowHandle, 1)
            Next
        End If
    End Sub

    Private Sub Action_Sync_GetIDs()
        Button_SyncDo.IsEnabled = False
        If File.Exists(I__Path_Programm & "\Cache\LastSync.nw520-osblx") And Sync_LoadedFromCache = False Then
            Interface_SetLoader("Reading cache file...")
            TextBlock_Sync_LastUpdate.Content = "Reading cache..."
            BGW__Action_Sync_GetIDs.RunWorkerAsync(New BGWcallback__Action_Sync_GetIDs With { _
                                               .Arg__Mode = 1})
        Else
            If Directory.Exists(Setting_osu_Path & "\Songs") Then
                Interface_LoaderProgressBar.Maximum = Directory.GetDirectories(Setting_osu_Path & "\Songs").Count
            End If
            Interface_SetLoader("Parsing installed beatmap sets...")
            TextBlock_Sync_LastUpdate.Content = "Syncing..."
            BGW__Action_Sync_GetIDs.RunWorkerAsync(New BGWcallback__Action_Sync_GetIDs)
        End If
        
    End Sub

    Private Sub Action_UpdateBeatmapDisplay(ByVal BeatmapList As List(Of Beatmap), Optional ByVal Destination As String = "Installed", Optional LastUpdateTime As String = Nothing)
        If Destination = "Installed" Then
            If LastUpdateTime = Nothing Then
                With TextBlock_Sync_LastUpdate
                    .Content = "Last sync: " & DateTime.Now.ToString("dd.MM.yyyy | HH:mm:ss")
                    .Tag = DateTime.Now.ToString("dd.MM.yyyy | HH:mm:ss")
                End With
            Else
                With TextBlock_Sync_LastUpdate
                    .Content = "Last sync: " & LastUpdateTime
                    .Tag = LastUpdateTime
                End With
            End If

            BeatmapWrapper.Children.Clear()

            For Each SelectedBeatmap As Beatmap In BeatmapList
                Dim UI_Grid = New Grid() With { _
                    .Height = 100,
                    .Margin = New Thickness(0, 0, 0, 10),
                    .Tag = SelectedBeatmap,
                    .Width = Double.NaN}

                ' Color_27AE60 = Light Green
                Dim UI_DecoBorderLeft = New Rectangle With { _
                    .Fill = Color_27AE60,
                    .Height = 100,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Tag = SelectedBeatmap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top,
                    .Width = 10}

                ' Color_555555 = Gray
                Dim UI_TextBlock_Title = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI"),
                    .FontSize = 36,
                    .Foreground = Color_555555,
                    .Height = 48,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Margin = New Thickness(25, 0, 0, 0),
                    .Text = SelectedBeatmap.Title,
                    .Tag = SelectedBeatmap,
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                ' Color_008136 = Dark Green
                Dim UI_TextBlock_Caption = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI Light"),
                    .FontSize = 14,
                    .Foreground = Color_008136,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Tag = SelectedBeatmap,
                    .Margin = New Thickness(25, 47, 0, 0),
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                If Not SelectedBeatmap.ID = -1 Then
                    UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist
                Else
                    UI_TextBlock_Caption.Text = "Unsubmitted | " & SelectedBeatmap.Artist
                End If
                If Not SelectedBeatmap.Creator = "Unknown" Then
                    UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator
                End If

                Dim UI_Checkbox_IsInstalled = New CheckBox With { _
                    .Content = "Installed?",
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .IsChecked = True,
                    .IsEnabled = False,
                    .Margin = New Thickness(25, 72, 0, 0),
                    .VerticalAlignment = Windows.VerticalAlignment.Top}
                ' Click Event
                UI_Grid.Children.Add(UI_DecoBorderLeft)
                UI_Grid.Children.Add(UI_TextBlock_Title)
                UI_Grid.Children.Add(UI_TextBlock_Caption)
                UI_Grid.Children.Add(UI_Checkbox_IsInstalled)
                BeatmapWrapper.Children.Add(UI_Grid)
            Next
            If BeatmapList.Count = 0 Then
                Dim UI_TextBlock As New TextBlock With { _
                    .FontSize = 72,
                    .Foreground = Color_27AE60,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Margin = New Thickness(0, 100, 0, 0),
                    .Text = "0 Beatmaps found.",
                    .VerticalAlignment = Windows.VerticalAlignment.Center}
                Dim UI_TextBlock_SubTitle As New TextBlock With { _
                    .FontSize = 24,
                    .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FF2ECC71"), Brush),
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Text = "That's... impressive, I guess.",
                    .VerticalAlignment = Windows.VerticalAlignment.Center}

                With BeatmapWrapper.Children
                    .Add(UI_TextBlock)
                    .Add(UI_TextBlock_SubTitle)
                End With
            End If

            TextBlock_BeatmapCounter.Text = BeatmapList.Count & " Beatmap sets found"
            Button_SyncDo.IsEnabled = True
        ElseIf Destination = "Importer" Then
            Dim MapsTotal As Integer = 0
            TabberItem_Import.Visibility = Windows.Visibility.Visible
            Tabber.SelectedIndex = 1
            ImporterWrapper.Children.Clear()
            Importer_Cancel.IsEnabled = False
            Importer_Run.IsEnabled = False
            If Sync_Done = False Then
                Sync_Done_ImporterRequest = True
                Button_SyncDo.IsEnabled = False
                Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With { _
                   .Height = 150,
                   .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                   .IsActive = True,
                   .Margin = New Thickness(0, 100, 0, 0),
                   .VerticalAlignment = Windows.VerticalAlignment.Center,
                   .Width = 150}
                Dim UI_TextBlock_SubTitle As New TextBlock With { _
                           .FontSize = 24,
                           .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                           .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                           .Text = "Please wait..." & vbNewLine & "Syncing beatmaps (check progress in Installed tab)",
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
                Dim UI_Checkbox_IsInstalled = New CheckBox With { _
                    .Content = "Installed?",
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .IsChecked = Check_IfInstalled,
                    .IsEnabled = False,
                    .Margin = New Thickness(25, 72, 0, 0),
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                Dim UI_Grid = New Grid() With { _
                    .Height = 100,
                    .Margin = New Thickness(0, 0, 0, 10),
                    .Width = Double.NaN}

                ' Color_27AE60 = Light Green
                ' Color_E74C3C = Red
                Dim UI_DecoBorderLeft = New Rectangle With { _
                    .Height = 100,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .VerticalAlignment = Windows.VerticalAlignment.Top,
                    .Width = 10}
                If Check_IfInstalled Then
                    UI_DecoBorderLeft.Fill = Color_27AE60
                Else
                    UI_DecoBorderLeft.Fill = Color_E74C3C
                End If

                ' Color_555555 = Gray
                Dim UI_TextBlock_Title = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI"),
                    .FontSize = 36,
                    .Foreground = Color_555555,
                    .Height = 48,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Margin = New Thickness(25, 0, 0, 0),
                    .Text = SelectedBeatmap.Title,
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                ' Color_008136 = Dark Green
                Dim UI_TextBlock_Caption = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI Light"),
                    .FontSize = 14,
                    .Foreground = Color_008136,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                    .Margin = New Thickness(25, 47, 0, 0),
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                Dim UI_Checkbox_IsSelected = New CheckBox With { _
                    .Content = "Download and install",
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

                AddHandler (UI_Checkbox_IsSelected.Checked), AddressOf Importer_AddBeatmapToSelection
                AddHandler (UI_Checkbox_IsSelected.Unchecked), AddressOf Importer_RemoveBeatmapFromSelection

                Dim TagData As New Importer_TagData With { _
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
            Next
            Importer_BeatmapsTotal = Sync_BeatmapList_ID_Installed.Count

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
                    Importer_DownloadMirrorInfo.Text = "Download Mirror: Bloodcat.com"
                Case 1
                    Importer_DownloadMirrorInfo.Text = "Download Mirror: Loli.al"
            End Select
        ElseIf Destination = "Exporter" Then
            ExporterWrapper.Children.Clear()
            For Each SelectedBeatmap As Beatmap In BeatmapList
                Dim UI_Grid = New Grid() With { _
                    .Height = 50,
                    .Margin = New Thickness(0, 0, 0, 10),
                    .Width = Double.NaN}

                ' Color_27AE60 = Light Green
                Dim UI_DecoBorderLeft = New Rectangle With { _
                    .Fill = Color_27AE60,
                    .Height = 50,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .VerticalAlignment = Windows.VerticalAlignment.Top,
                    .Width = 10}

                ' Color_555555 = Gray
                Dim UI_TextBlock_Title = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI"),
                    .FontSize = 22,
                    .Foreground = Color_555555,
                    .Height = 30,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Margin = New Thickness(25, 0, 0, 0),
                    .Text = SelectedBeatmap.Title,
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                ' Color_008136 = Dark Green
                Dim UI_TextBlock_Caption = New TextBlock With { _
                    .FontFamily = New FontFamily("Segoe UI Light"),
                    .FontSize = 12,
                    .Foreground = Color_008136,
                    .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                    .Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist,
                    .Margin = New Thickness(25, 30, 0, 0),
                    .TextWrapping = TextWrapping.Wrap,
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                If Not SelectedBeatmap.ID = -1 Then
                    UI_TextBlock_Caption.Text = SelectedBeatmap.ID.ToString & " | " & SelectedBeatmap.Artist
                Else
                    UI_TextBlock_Caption.Text = "You can't export unsubmitted maps | " & SelectedBeatmap.Artist
                End If
                If Not SelectedBeatmap.Creator = "Unknown" Then
                    UI_TextBlock_Caption.Text += " | " & SelectedBeatmap.Creator
                End If

                Dim UI_Checkbox_IsSelected = New CheckBox With { _
                    .Content = "Select to export",
                    .HorizontalAlignment = Windows.HorizontalAlignment.Right,
                    .IsChecked = True,
                    .Margin = New Thickness(10, 5, 0, 0),
                    .VerticalAlignment = Windows.VerticalAlignment.Top}

                If SelectedBeatmap.ID = -1 Then
                    With UI_Checkbox_IsSelected
                        .IsChecked = False
                        .IsEnabled = False
                    End With
                    UI_DecoBorderLeft.Fill = Color_999999
                Else
                    AddHandler (UI_Checkbox_IsSelected.Checked), AddressOf Exporter_AddBeatmapToSelection
                    AddHandler (UI_Checkbox_IsSelected.Unchecked), AddressOf Exporter_RemoveBeatmapFromSelection
                    AddHandler (UI_TextBlock_Title.MouseDown), AddressOf Exporter_DetermineWheterAddOrRemove
                    AddHandler (UI_DecoBorderLeft.MouseDown), AddressOf Exporter_DetermineWheterAddOrRemove
                End If

                Dim TagData As New Importer_TagData With { _
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

                UI_Grid.Children.Add(UI_DecoBorderLeft)
                UI_Grid.Children.Add(UI_TextBlock_Title)
                UI_Grid.Children.Add(UI_TextBlock_Caption)
                UI_Grid.Children.Add(UI_Checkbox_IsSelected)
                ExporterWrapper.Children.Add(UI_Grid)
            Next

            TabberItem_Export.Visibility = Windows.Visibility.Visible
            Tabber.SelectedIndex = 2
        End If
    End Sub

    Private Sub Button_SyncDo_Click(sender As Object, e As RoutedEventArgs) Handles Button_SyncDo.Click
        If Setting_Tool_CheckFileAssociation Then
            Action_CheckFileAssociation()
        End If

        If Directory.Exists(Setting_osu_Path & "\Songs") And Setting_Messages_Sync_MoreThan1000Sets Then
            Dim counter As System.Collections.ObjectModel.ReadOnlyCollection(Of String)
            counter = My.Computer.FileSystem.GetDirectories(Setting_osu_Path & "\Songs")

            If counter.Count > 1000 Then
                If MessageBox.Show("You've got about " & counter.Count & " beatmap sets." & vbNewLine & "It will take some time (maybe some minutes) to read all sets, do you want to proceed?", I__MsgBox_DefaultTitle_CanBeDisabled, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.No Then
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
                MsgBox("Unable to check for updates!" & vbNewLine & "// Invalid Server response" & vbNewLine & vbNewLine & "If this problem persists you can visit the osu! forum at http://bit.ly/1Bbmn6E (in your clipboard).", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
                MsgBox(e.Result, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            End If
            TextBlock_Programm_Updater.Content = "Unable to check for updates!"
            Exit Sub
        Catch ex As System.Reflection.TargetInvocationException
            If Setting_Messages_Updater_UnableToCheckForUpdates Then
                MsgBox("Unable to check for updates!" & vbNewLine & "// Can't connect to server" & vbNewLine & vbNewLine & "If this problem persists you can visit the osu! forum at http://bit.ly/1Bbmn6E (in your clipboard).", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle_CanBeDisabled)
            End If
            TextBlock_Programm_Updater.Content = "Unable to check for updates!"
            Exit Sub
        End Try

        If CStr(Answer.SelectToken("latestVersion")) = My.Application.Info.Version.ToString Then
            TextBlock_Programm_Updater.Content = "Using the latest version (" + My.Application.Info.Version.ToString + ")"
        Else
            TextBlock_Programm_Updater.Content = "Update available (New: " + CStr(Answer.SelectToken("latestVersion")) + " | Running: " & My.Application.Info.Version.ToString & ")"
            If Setting_Messages_Updater_OpenUpdater Then
                Dim Window_Updater As New Window_Updater
                Window_Updater.ShowDialog()
            End If
        End If
    End Sub

    Private Sub FadeOut_Completed(sender As Object, e As EventArgs) Handles FadeOut.Completed
        If FadeOut_Status = "FadeOut" Then
            Overlay.Visibility = Windows.Visibility.Hidden
        End If
    End Sub

    Private Sub Interface_SetLoader(Optional Message As String = "Please wait")
        Dim UI_ProgressBar = New ProgressBar With { _
            .HorizontalAlignment = Windows.HorizontalAlignment.Stretch,
            .Visibility = Windows.Visibility.Hidden,
            .Height = 25}
        Dim UI_ProgressRing = New MahApps.Metro.Controls.ProgressRing With { _
            .Height = 150,
            .HorizontalAlignment = Windows.HorizontalAlignment.Center,
            .IsActive = True,
            .Margin = New Thickness(0, 100, 0, 0),
            .VerticalAlignment = Windows.VerticalAlignment.Center,
            .Width = 150}
        Dim UI_TextBlock_SubTitle As New TextBlock With { _
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

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Check if already running
        If Diagnostics.Process.GetProcessesByName(Diagnostics.Process.GetCurrentProcess.ProcessName).Count > 1 Then
            Dim SelectedProcess As Process = Process.GetProcessesByName(Diagnostics.Process.GetCurrentProcess.ProcessName).First
            AppActivate(SelectedProcess.Id)
            ShowWindow(SelectedProcess.MainWindowHandle, 1)
            Application.Current.Shutdown()
            Exit Sub
        End If

        ' Delete old downloaded beatmaps
        If Directory.Exists(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload") Then
            Directory.Delete(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload", True)
        End If

        ' Load Configuration
        If File.Exists(I__Path_Programm & "\Settings\Settings.config") Then
            Action_LoadSettings()
        Else
            Dim Window_Welcome As New Window_Welcome
            Window_Welcome.ShowDialog()

            Action_SaveSettings()
        End If

        'Check For Updates
        Select Case Setting_Tool_CheckForUpdates
            Case 0
                TextBlock_Programm_Updater.Content = "Checking for updates..."
                Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=AutoCheck&updaterInterval=" & Setting_Tool_CheckForUpdates))
                Setting_Tool_LastCheckForUpdates = Date.Now.ToString("dd-MM-yyyy hh:mm:ss")
                Action_SaveSettings()
            Case 1
                TextBlock_Programm_Updater.Content = "Updates disabled"
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
                    TextBlock_Programm_Updater.Content = "Checking for updates..."
                    Client.DownloadStringAsync(New Uri(I__Path_Web_Host + "/data/files/software/LatestVersion.php?version=" & My.Application.Info.Version.ToString & "&from=AutoCheck&updaterInterval=" & Setting_Tool_CheckForUpdates))
                    Setting_Tool_LastCheckForUpdates = Date.Now.ToString("dd-MM-yyyy hh:mm:ss")
                    Action_SaveSettings()
                Else
                    TextBlock_Programm_Updater.Content = "Update check not necessary"
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
            If File.Exists(I__Path_Programm & "\Cache\LastSync.nw520-osblx") And Sync_LoadedFromCache = False And Setting_Tool_AutoLoadCacheOnStartup Then
                Button_SyncDo.IsEnabled = False
                Interface_SetLoader("Reading cache file...")
                TextBlock_Sync_LastUpdate.Content = "Reading cache..."
                BGW__Action_Sync_GetIDs.RunWorkerAsync(New BGWcallback__Action_Sync_GetIDs With { _
                                                   .Arg__Mode = 1,
                                                   .Arg__AutoSync = True})
            End If
        End If
    End Sub

    Private Sub MenuItem_File_Export_ConvertOSBLToOSBLX_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_ConvertOSBLToOSBLX.Click
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = "osu!Sync Beatmap List|*.nw520-osbl"
            .Title = "Convert OSBL to OSBLX"
            .ShowDialog()
        End With

        Dim OSBL_Content As String
        If Not Dialog_OpenFile.FileName = "" Then
            OSBL_Content = File.ReadAllText(Dialog_OpenFile.FileName)
        Else
            Action_OverlayShow("Conversion aborted", "")
            Action_OverlayFadeOut()
            Exit Sub
        End If

        Dim Dialog_SaveFile As New Microsoft.Win32.SaveFileDialog()
        With Dialog_SaveFile
            .AddExtension = True
            .Filter = "Compressed osu!Sync Beatmap List|*.nw520-osblx"
            .OverwritePrompt = True
            .Title = "Export OSBL to OSBLX"
            .ValidateNames = True
            .ShowDialog()
        End With

        If Not Dialog_SaveFile.FileName = "" Then
            Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Dialog_SaveFile.FileName, False)
                File.Write(CompressString(OSBL_Content))
                File.Close()
            End Using
        Else
            Action_OverlayShow("Conversion aborted", "")
            Action_OverlayFadeOut()
            Exit Sub
        End If

        Action_OverlayShow("Conversion completed", "Converted OSBL to OSBLX-File")
        Action_OverlayFadeOut()
    End Sub

    Private Sub MenuItem_File_Export_InstalledBeatmaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_InstalledBeatmaps.Click
        If Sync_Done = False Then
            MsgBox("In order to use this function you need to sync first.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Action_ExportBeatmapDialog(Sync_BeatmapList_Installed)
    End Sub

    Private Sub MenuItem_File_Export_SelectedMaps_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_Export_SelectedMaps.Click
        If Sync_Done = False Then
            MsgBox("In order to use this function you need to sync first.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If

        Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed, "Exporter")
    End Sub

    Private Sub MenuItem_File_OpenBeatmapList_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_File_OpenBeatmapList.Click
        If Sync_Done = False Then
            MsgBox("In order to use this function you need to sync first.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            Exit Sub
        End If
        Dim Dialog_OpenFile As New Microsoft.Win32.OpenFileDialog()
        With Dialog_OpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .Filter = "Compressed osu!Sync Beatmap List|*.nw520-osblx|osu!Sync Beatmap List|*.nw520-osbl"
            .Title = "Open beatmap list"
            .ShowDialog()
        End With

        If Not Dialog_OpenFile.FileName = "" Then
            Importer_ReadListFile(Dialog_OpenFile.FileName)
        Else
            Action_OverlayShow("Import aborted", "")
            Action_OverlayFadeOut()
        End If
    End Sub

    Private Sub MenuItem_Help_About_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_About.Click
        Dim Window_About As New Window_About
        Window_About.ShowDialog()
    End Sub

    Private Sub MenuItem_Help_Updater_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Help_Updater.Click
        Dim Window_Updater As New Window_Updater
        Window_Updater.ShowDialog()
    End Sub

    Private Sub MenuItem_Program_Exit_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Exit.Click
        Application.Current.Shutdown()
    End Sub

    Private Sub MenuItem_Program_RunOsu_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_RunOsu.Click
        Action_StartOrFocusOsu()
    End Sub

    Private Sub MenuItem_Program_Settings_Click(sender As Object, e As RoutedEventArgs) Handles MenuItem_Program_Settings.Click
        Dim Window_Settings As New Window_Settings
        Window_Settings.ShowDialog()
    End Sub

    Private Sub TextBlock_Programm_Updater_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles TextBlock_Programm_Updater.MouseDown
        Dim Window_Updater As New Window_Updater
        Window_Updater.ShowDialog()
    End Sub

#Region "Background Worker"
#Region "BGW__Action_Sync_GetIDs"
    Private Sub BGW__Action_Sync_GetIDs_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles BGW__Action_Sync_GetIDs.DoWork
        Dim Arguments As New BGWcallback__Action_Sync_GetIDs
        Arguments = TryCast(e.Argument, BGWcallback__Action_Sync_GetIDs)
        Dim Answer As New BGWcallback__Action_Sync_GetIDs

        If Not Directory.Exists(Setting_osu_Path & "\Songs") Then
            Answer.Return__Status = 1
            e.Result = Answer
            Exit Sub
        End If

        If Arguments.Arg__Mode = 1 Then
            Try
                Dim File_Content_Compressed As String = File.ReadAllText(I__Path_Programm & "\Cache\LastSync.nw520-osblx")
                Dim File_Content As String = DecompressString(File_Content_Compressed)
                Dim File_Content_Json As JObject = CType(JsonConvert.DeserializeObject(File_Content), JObject)
                Dim Cache_Time As String = CStr(File_Content_Json.Item("_info").Item("_file_generationdate_syncFormat"))

                If Not DateDiff(DateInterval.Day, Date.ParseExact(Cache_Time, "dd.MM.yyyy | HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), Date.Now) >= 14 Then
                    With Answer
                        .Return__Status = 2
                        .Return__Sync_BeatmapList_ID_Installed = Action_ConvertSavedJSONtoListBeatmapIDs(File_Content_Json)
                        .Return__Sync_BeatmapList_Installed = Action_ConvertSavedJSONtoListBeatmap(File_Content_Json)
                        .Return__Sync_Cache_Time = Cache_Time
                    End With
                    e.Result = Answer
                    Exit Sub
                Else
                    BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                                .Progress__CurrentAction = 3})
                End If
            Catch ex As System.IO.InvalidDataException
                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                                .Progress__CurrentAction = 3})
            Catch ex As JsonReaderException
                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                                .Progress__CurrentAction = 3})
            Catch ex As System.FormatException
                BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                                .Progress__CurrentAction = 3})
            End Try
        End If

        Dim Beatmap_InvalidFolder As String = ""
        Dim Beatmap_InvalidIDBeatmaps As String = ""

        For Each DirectoryList As String In Directory.GetDirectories(Setting_osu_Path & "\Songs")
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

                        Do While FileReader.Peek() <> -1 And TextLines.Count <= 50  ' don't read more than 50 lines
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
                                    MsgBox("Fetched ID is not a number." & vbNewLine & "Trying to use alternative way via folder name.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
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
                        BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
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
                        Dim CurrentBeatmap As New Beatmap With { _
                            .ID = CInt(Beatmap_ID),
                            .Title = Beatmap_Name,
                            .Artist = Beatmap_Artist}
                        Console.WriteLine(DirectoryInfo.Name & " | " & Beatmap_Artist & " | " & Beatmap_ID & " | " & Beatmap_Name)
                        Answer.Return__Sync_BeatmapList_Installed.Add(CurrentBeatmap)
                        Answer.Return__Sync_BeatmapList_ID_Installed.Add(CInt(Beatmap_ID))
                    Catch ex As Exception
                        Beatmap_InvalidFolder += "• " & DirectoryInfo.Name & vbNewLine
                    End Try
                End If
            End If
        Next

        If Not Beatmap_InvalidFolder = "" Or Not Beatmap_InvalidIDBeatmaps = "" Then
            If Not Beatmap_InvalidFolder = "" Then
                Answer.Return__Sync_Warnings += "=====   Ignored Folders   =====" & vbNewLine & "It seems that some folder(s) can't be parsed." & vbNewLine & "They'll be ignored." & vbNewLine & vbNewLine & "// Folder(s): " & vbNewLine & Beatmap_InvalidFolder & vbNewLine & vbNewLine
            End If
            If Not Beatmap_InvalidIDBeatmaps = "" Then
                Answer.Return__Sync_Warnings += "=====   Unable to get ID   =====" & vbNewLine & "osu!Sync was unable to get the IDs of some beatmaps." & vbNewLine & "They will be handled as unsubmitted (can't be exported)." & vbNewLine & vbNewLine & "// Beatmap(s): " & vbNewLine & Beatmap_InvalidIDBeatmaps & vbNewLine & vbNewLine & vbNewLine
            End If
        End If

        ' Write Cache
        BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                            .Progress__Current = Answer.Return__Sync_BeatmapList_ID_Installed.Count,
                            .Progress__CurrentAction = 1})
        If Not Directory.Exists(I__Path_Programm & "\Cache") Then
            Directory.CreateDirectory(I__Path_Programm & "\Cache")
        End If
        Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(I__Path_Programm & "\Cache\LastSync.nw520-osblx", False)
            Dim Content As New Dictionary(Of String, Dictionary(Of String, String))
            Dim Content_ProgrammInfo As New Dictionary(Of String, String)
            Content_ProgrammInfo.Add("_author", "naseweis520")
            Content_ProgrammInfo.Add("_author_uri", "http://naseweis520.ml/")
            Content_ProgrammInfo.Add("_file_generationdate", DateTime.Now.ToString("dd/MM/yyyy"))
            Content_ProgrammInfo.Add("_file_generationdate_syncFormat", Date.Now.ToString("dd.MM.yyyy | HH:mm:ss"))
            Content_ProgrammInfo.Add("_file_usage", "Sync-Cache")
            Content_ProgrammInfo.Add("_programm", "osu!Sync")
            Content_ProgrammInfo.Add("_version", My.Application.Info.Version.ToString)
            Content.Add("_info", Content_ProgrammInfo)
            For Each SelectedBeatmap As Beatmap In Answer.Return__Sync_BeatmapList_Installed
                If Not SelectedBeatmap.ID = -1 And Not Content.ContainsKey(SelectedBeatmap.ID.ToString) Then
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
            Dim Content_Json As String
            Content_Json = JsonConvert.SerializeObject(Content)
            File.Write(CompressString(Content_Json))
            BGW__Action_Sync_GetIDs.ReportProgress(Nothing, New BGWcallback__Action_Sync_GetIDs With { _
                           .Progress__Current = Answer.Return__Sync_BeatmapList_ID_Installed.Count,
                           .Progress__CurrentAction = 2})
            File.Close()
        End Using
        e.Result = Answer
    End Sub

    Private Sub BGW__Action_Sync_GetIDs_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles BGW__Action_Sync_GetIDs.ProgressChanged
        Dim Answer As New BGWcallback__Action_Sync_GetIDs
        Answer = CType(e.UserState, BGWcallback__Action_Sync_GetIDs)
        Select Case Answer.Progress__CurrentAction
            Case 0
                Interface_LoaderText.Text = Answer.Progress__Current & " beatmap sets parsed." & vbNewLine & "And still working..."
                With Interface_LoaderProgressBar
                    .Value = Answer.Progress__Current
                    .Visibility = Windows.Visibility.Visible
                End With
            Case 1
                Interface_LoaderProgressBar.IsIndeterminate = True
                Interface_LoaderText.Text = Answer.Progress__Current & " beatmap sets in total parsed." & vbNewLine & "Writing cache file..."
            Case 2
                Interface_LoaderText.Text = Answer.Progress__Current & " beatmap sets in total parsed." & vbNewLine & "Generating interface..."
            Case 3
                TextBlock_Sync_LastUpdate.Content = "Cache file outdated/Syncing..."
        End Select
    End Sub

    Private Sub BGW__Action_Sync_GetIDs_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles BGW__Action_Sync_GetIDs.RunWorkerCompleted
        Dim Answer As New BGWcallback__Action_Sync_GetIDs
        Answer = TryCast(e.Result, BGWcallback__Action_Sync_GetIDs)
        Select Case Answer.Return__Status
            Case 0
                Sync_LoadedFromCache = True
                Interface_LoaderText.Text = Answer.Return__Sync_BeatmapList_ID_Installed.Count & " beatmap sets parsed."
                If Not Answer.Return__Sync_Warnings = "" Then
                    If MessageBox.Show("It seems like some of your beatmap sets differ from the normal format." & vbNewLine & _
                                       "Do you want to check which beatmap sets are affected?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) = MessageBoxResult.Yes Then
                        Dim Window_Message As New Window_MessageWindow
                        Window_Message.SetMessage(Answer.Return__Sync_Warnings, "Exceptions", "Sync")
                        Window_Message.ShowDialog()
                    End If
                End If
                Sync_BeatmapList_Installed = Answer.Return__Sync_BeatmapList_Installed
                Sync_BeatmapList_ID_Installed = Answer.Return__Sync_BeatmapList_ID_Installed

                Sync_Done = True
                Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed)
                Action_OverlayShow("Sync completed", "")
                Action_OverlayFadeOut()

                If Sync_Done_ImporterRequest Then
                    Sync_Done_ImporterRequest = False
                    Action_UpdateBeatmapDisplay(Sync_Done_ImporterRequest_SaveValue, "Importer")
                End If
            Case 1
                MsgBox("Unable to find osu! folder." & vbNewLine & "Please specify the path to osu! in the following window.", MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
                Dim Window_Settings As New Window_Settings
                Window_Settings.Tabber.SelectedIndex = 1
                Window_Settings.ShowDialog()

                Dim UI_TextBlock As New TextBlock With { _
                    .FontSize = 72,
                    .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Margin = New Thickness(0, 100, 0, 0),
                    .Text = "Last sync failed.",
                    .VerticalAlignment = Windows.VerticalAlignment.Center}
                Dim UI_TextBlock_SubTitle As New TextBlock With { _
                    .FontSize = 24,
                    .Foreground = DirectCast(New BrushConverter().ConvertFrom("#FFDDDDDD"), Brush),
                    .HorizontalAlignment = Windows.HorizontalAlignment.Center,
                    .Text = "Please retry.",
                    .VerticalAlignment = Windows.VerticalAlignment.Center}

                With BeatmapWrapper.Children
                    .Clear()
                    .Add(UI_TextBlock)
                    .Add(UI_TextBlock_SubTitle)
                End With
                Button_SyncDo.IsEnabled = True
            Case 2
                Sync_LoadedFromCache = True
                Sync_BeatmapList_Installed = Answer.Return__Sync_BeatmapList_Installed
                Sync_BeatmapList_ID_Installed = Answer.Return__Sync_BeatmapList_ID_Installed

                Sync_Done = True
                Action_UpdateBeatmapDisplay(Sync_BeatmapList_Installed, "Installed", Answer.Return__Sync_Cache_Time)
                Action_OverlayShow("Sync completed", "Loaded from cache")
                Action_OverlayFadeOut()

                If Sync_Done_ImporterRequest Then
                    Sync_Done_ImporterRequest = False
                    Action_UpdateBeatmapDisplay(Sync_Done_ImporterRequest_SaveValue, "Importer")
                End If
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
        TextBlock_Progress.Content = "Fetching " & CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID) & "..."
        Select Case Setting_Tool_DownloadMirror
            Case 1
                Importer_DownloadMirrorInfo.Text = "Download Mirror: Loli.al"
                RequestURI = "http://loli.al/s/" + CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID)
            Case Else
                Importer_DownloadMirrorInfo.Text = "Download Mirror: Bloodcat.com"
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

        Importer_UpdateInfo("Fetching")

        Dim req As HttpWebRequest = DirectCast(HttpWebRequest.Create(RequestURI), HttpWebRequest)
        Dim Res As WebResponse
        Try
            Res = req.GetResponse()
        Catch ex As WebException
            If MessageBox.Show("It looks, like something is wrong with this download, do you want to skip this file (Yes) or to cancel the whole download (No)?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then
                'Yes
                Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_E67E2E      ' Orange
                Importer_BeatmapList_Tag_Failed.Add(Importer_BeatmapList_Tag_ToInstall.First)
                Importer_BeatmapList_Tag_ToInstall.Remove(Importer_BeatmapList_Tag_ToInstall.First)
                Importer_Downloader_ToNextDownload()
            Else
                'No
                Importer_BeatmapList_Tag_ToInstall.First.UI_DecoBorderLeft.Fill = Color_E67E2E      ' Orange
                Importer_Info.Text = "Installing"
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_Done.Count & " Sets done"
                If Importer_BeatmapList_Tag_LeftOut.Count > 0 Then
                    Importer_Info.Text += " | " & Importer_BeatmapList_Tag_LeftOut.Count & " Left out"
                End If
                Importer_Info.Text += " | " & Importer_BeatmapsTotal & " Sets total"

                TextBlock_Progress.Content = "Installing files..."

                For Each FilePath In Directory.GetFiles(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload")
                    File.Move(FilePath, "C:\Program Files (x86)\osu!\Songs\" & Path.GetFileName(FilePath))
                Next
                With Importer_Progress
                    .IsIndeterminate = False
                    .Visibility = Windows.Visibility.Hidden
                End With

                TextBlock_Progress.Content = ""
                Importer_Info.Text = "Aborted"
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_Done.Count & " Sets done"
                If Importer_BeatmapList_Tag_LeftOut.Count > 0 Then
                    Importer_Info.Text += " | " & Importer_BeatmapList_Tag_LeftOut.Count & " Left out"
                End If
                Importer_Info.Text += " | " & Importer_BeatmapsTotal & " Sets total"
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

        TextBlock_Progress.Content = "Downloading " & CStr(Importer_BeatmapList_Tag_ToInstall.First.Beatmap.ID) & "..."
        Importer_UpdateInfo("Downloading")
        Importer_Progress.IsIndeterminate = False
        Importer_Downloader.DownloadFileAsync(New Uri(RequestURI), Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload\" & Importer_CurrentFileName)
    End Sub

    Private Sub Importer_Downloader_ToNextDownload()
        If Importer_BeatmapList_Tag_ToInstall.Count > 0 Then
            Importer_DownloadBeatmap()
        Else
            With Importer_Progress
                .IsIndeterminate = True
                .Value = 0
            End With

            Importer_UpdateInfo("Installing")
            TextBlock_Progress.Content = "Installing files..."

            For Each FilePath In Directory.GetFiles(Path.GetTempPath() & "naseweis520\osu!Sync\BeatmapDownload")
                If Not File.Exists(Setting_osu_Path & "\Songs\" & Path.GetFileName(FilePath)) Then
                    File.Move(FilePath, Setting_osu_Path & "\Songs\" & Path.GetFileName(FilePath))
                Else
                    File.Delete(FilePath)
                End If
            Next
            With Importer_Progress
                .IsIndeterminate = False
                .Visibility = Windows.Visibility.Hidden
            End With

            TextBlock_Progress.Content = ""
            Importer_UpdateInfo("Done")
            My.Computer.Audio.PlaySystemSound(System.Media.SystemSounds.Beep)

            MsgBox("Installation finished!" & vbNewLine & _
                    Importer_BeatmapList_Tag_Done.Count & " Sets done" & vbNewLine & _
                    Importer_BeatmapList_Tag_Failed.Count & " Sets failed" & vbNewLine & _
                    Importer_BeatmapList_Tag_LeftOut.Count & " Left out" & vbNewLine & _
                    Importer_BeatmapsTotal & " Sets total" & vbNewLine & vbNewLine & _
                    "Press F5 in osu! to reload your beatmaps or go back to the main menu.")

            If Not Process.GetProcessesByName("osu!").Count > 0 Then
                If MessageBox.Show("Do you want to start osu! now?", I__MsgBox_DefaultTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
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
            Action_UpdateBeatmapDisplay(Action_ConvertSavedJSONtoListBeatmap(File_Content_Json), "Importer")
        ElseIf Path.GetExtension(FilePath) = ".nw520-osbl" Then
            Dim File_Content_Json As JObject = CType(JsonConvert.DeserializeObject(File.ReadAllText(FilePath)), JObject)
            Importer_Info.Text = FilePath
            Action_UpdateBeatmapDisplay(Action_ConvertSavedJSONtoListBeatmap(File_Content_Json), "Importer")
        Else
            MsgBox("Unknown file extension:" & vbNewLine & Path.GetExtension(FilePath), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
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
        If Title = "Fetching" Or Title = "Downloading" Or Title = "Installing" Then
            If Importer_BeatmapList_Tag_ToInstall.Count = 1 Then
                Importer_Info.Text += " | 1 Set left to install"
            Else
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_ToInstall.Count & " Sets left to install"
            End If
            If Importer_BeatmapList_Tag_Done.Count = 1 Then
                Importer_Info.Text += " | 1 Set done"
            ElseIf Importer_BeatmapList_Tag_Done.Count > 1 Then
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_Done.Count & " Sets done"
            End If
            If Importer_BeatmapList_Tag_Failed.Count = 1 Then
                Importer_Info.Text += " | 1 Set failed"
            ElseIf Importer_BeatmapList_Tag_Failed.Count > 1 Then
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_Failed.Count & " Sets failed"
            End If
            If Importer_BeatmapList_Tag_LeftOut.Count = 1 Then
                Importer_Info.Text += " | 1 Set Left out"
            ElseIf Importer_BeatmapList_Tag_LeftOut.Count > 1 Then
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_LeftOut.Count & " Sets Left out"
            End If
            Importer_Info.Text += " | " & Importer_BeatmapsTotal & " Sets total"
        Else
            If Importer_BeatmapList_Tag_ToInstall.Count = 1 Then
                Importer_Info.Text += " | 1 Set to install"
            Else
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_ToInstall.Count & " Sets to install"
            End If
            If Importer_BeatmapList_Tag_LeftOut.Count = 1 Then
                Importer_Info.Text += " | 1 Set Left out"
            ElseIf Importer_BeatmapList_Tag_LeftOut.Count > 1 Then
                Importer_Info.Text += " | " & Importer_BeatmapList_Tag_LeftOut.Count & " Sets Left out"
            End If
            If Importer_BeatmapsTotal = 1 Then
                Importer_Info.Text += " | 1 Set total"
            Else
                Importer_Info.Text += " | " & Importer_BeatmapsTotal & " Sets total"
            End If
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