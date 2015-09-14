Imports Newtonsoft.Json, Newtonsoft.Json.Linq
Imports System.IO, System.IO.Compression, System.Security.Principal
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Class DownloadMirror
    Property DisplayName As String
    Property DownloadURL As String
    Property Index As Integer
    Property WebURL As String
End Class
Class Language
    Property Code As String
    Property DisplayName As String
    Property DisplayName_English As String
End Class
Module Global_Var
    Public Application_FileExtensions() As String = {".nw520-osbl",
                                         ".nw520-osblx"}
    Public Application_FileExtensionsLong() As String = {"naseweis520.osuSync.osuBeatmapList",
                                             "naseweis520.osuSync.compressedOsuBeatmapList"}
    Public Application_FileExtensionsDescription() As String = {_e("GlobalVar_extensionBeatmapList"),
                                                    _e("GlobalVar_extensionCompressedBeatmapList")}
    Public Application_FileExtensionsIcon() As String = {"""" & System.Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,2",
                                             """" & System.Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,1"}
    Public Application_Languages As New Dictionary(Of String, Language) ' See Action_PrepareData()
    Public Application_Mirrors As New Dictionary(Of Integer, DownloadMirror)

    Public I__StartUpArguments() As String
    Public Const I__Path_Web_Host As String = "http://naseweis520.ml/osuSync"
    Public I__Path_Programm As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\naseweis520\osu!Sync"
    Public Const I__MsgBox_DefaultTitle As String = "Dialog | osu!Sync"
    Public I__MsgBox_DefaultTitle_CanBeDisabled As String = "osu!Sync | " & _e("GlobalVar_messageCanBeDisabled")
    Public Setting_osu_Path As String = GetDetectedOsuPath()
    Public Setting_osu_SongsPath As String = Setting_osu_Path & "\Songs"
    Public Setting_Tool_CheckForUpdates As Integer = 3
    Public Setting_Tool_CheckFileAssociation As Boolean = True
    Public Setting_Tool_DownloadMirror As Integer = 0
    Public Setting_Tool_EnableNotifyIcon As Integer = 0
    Public Setting_Tool_Importer_AutoInstallCounter As Integer = 10
    Public Setting_Tool_Interface_BeatmapDetailPanelWidth As Integer = 40
    Public Setting_Tool_Language As String = "en"
    Public Setting_Tool_LastCheckForUpdates As String = "01-01-2000 00:00:00"
    Public Setting_Tool_SyncOnStartup As Boolean = False
    Public Setting_Tool_Update_DeleteFileAfter As Boolean = True
    Public Setting_Tool_Update_SavePath As String = Path.GetTempPath() & "naseweis520\osu!Sync\Updater"
    Public Setting_Tool_Update_UseDownloadPatcher As Boolean = True
    Public Setting_Messages_Updater_OpenUpdater As Boolean = True
    Public Setting_Messages_Updater_UnableToCheckForUpdates As Boolean = True

    Function _e(ByRef Text As String) As String
        Try
            Return Windows.Application.Current.FindResource(Text).ToString
        Catch ex As ResourceReferenceKeyNotFoundException
            MsgBox("The application just tried to load a text (= string) which isn't registered." & vbNewLine & "Normally, this shouldn't happen." &
                   vbNewLine & vbNewLine & "Please report this by using the Feedback-box in the settings, contacting me using the link in the about window, reporting an issue on GitHub, or contacting me on the osu!Forum." & vbNewLine & vbNewLine & "// Additional informations:" & vbNewLine & Text, MsgBoxStyle.Critical, I__MsgBox_DefaultTitle)
            Return "[Missing String: " + Text + "]"
        End Try
    End Function

    Function CompressString(ByVal text As String) As String
        Dim buffer() As Byte = Encoding.UTF8.GetBytes(text)
        Dim memoryStream = New IO.MemoryStream()
        Using gZipStream = New GZipStream(memoryStream, CompressionMode.Compress, True)
            gZipStream.Write(buffer, 0, buffer.Length)
        End Using

        memoryStream.Position = 0

        Dim compressedData = New Byte(CInt(memoryStream.Length - 1)) {}
        memoryStream.Read(compressedData, 0, compressedData.Length)

        Dim gZipBuffer = New Byte(compressedData.Length + 4 - 1) {}
        System.Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length)
        System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4)
        Return Convert.ToBase64String(gZipBuffer)
    End Function

    Function CreateFileAssociation(ByVal extension As String,
    ByVal className As String, ByVal description As String,
    ByVal iconPath As String, ByVal exeProgram As String) As Boolean
        Const SHCNE_ASSOCCHANGED = &H8000000
        Const SHCNF_IDLIST = 0
        ' ensure that there is a leading dot
        If extension.Substring(0, 1) <> "." Then
            extension = "." & extension
        End If

        Dim key1, key2, key3, key4 As Microsoft.Win32.RegistryKey
        Try
            ' create a value for this key that contains the classname
            key1 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension)
            key1.SetValue("", className)
            ' create a new key for the Class name
            key2 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className)
            key2.SetValue("", description)
            ' associate the program to open the files with this extension
            key3 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className & "\shell\open\command")
            key3.SetValue("", """" & exeProgram & """ -openFile=""%1""")
            ' icon
            key4 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className & "\DefaultIcon")
            key4.SetValue("", iconPath)
        Catch e As Exception
            MsgBox(e.Message, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            Return False
        End Try
        If Not key1 Is Nothing Then key1.Close()
        If Not key2 Is Nothing Then key2.Close()
        If Not key3 Is Nothing Then key3.Close()
        If Not key4 Is Nothing Then key4.Close()

        ' notify Windows that file associations have changed
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        Return True
    End Function

    Function DecompressString(ByVal compressedText As String) As String
        Dim gZipBuffer() As Byte = Convert.FromBase64String(compressedText)
        Using memoryStream = New MemoryStream()
            Dim dataLength As Integer = BitConverter.ToInt32(gZipBuffer, 0)
            memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4)

            Dim buffer = New Byte(dataLength - 1) {}

            memoryStream.Position = 0
            Using gZipStream = New GZipStream(memoryStream, CompressionMode.Decompress)
                gZipStream.Read(buffer, 0, buffer.Length)
            End Using

            Return Encoding.UTF8.GetString(buffer)
        End Using
    End Function

    Function DeleteFileAssociation(ByVal extension As String, ByVal className As String) As Boolean
        Const SHCNE_ASSOCCHANGED = &H8000000
        Const SHCNF_IDLIST = 0
        If extension.Substring(0, 1) <> "." Then
            extension = "." & extension
        End If

        Try
            If Not Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension) Is Nothing Then
                Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension)
            End If
            If Not Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(className) Is Nothing Then
                Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(className)
            End If
        Catch e As Exception
            MsgBox(_e("GlobalVar_sorrySomethingWentWrong"), MsgBoxStyle.OkOnly)
            MsgBox(e.Message, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            Return False
        End Try

        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        Return True
    End Function

    Function GetDetectedOsuPath() As String
        If IO.Directory.Exists(Setting_osu_Path) Then
            Return Setting_osu_Path
        ElseIf IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) & "\osu!"
        ElseIf IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) & "\osu!"
        ElseIf IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\osu!"
        Else
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        End If
    End Function

    Function GetTranslationName(ByVal LanguageCode_Short As String) As String
        If Application_Languages.ContainsKey(LanguageCode_Short) Then
            Return Application_Languages(LanguageCode_Short).Code
        Else
            Return ""
        End If
    End Function

    Function GetProgramInfoJson() As JObject
        Dim identity = WindowsIdentity.GetCurrent()
        Dim principal = New WindowsPrincipal(identity)
        Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)

        Dim JContent As New JObject
        With JContent
            .Add("application", New JObject From {
                 {"isElevated", CStr(isElevated)},
                 {"lastUpdateCheck", Setting_Tool_LastCheckForUpdates},
                 {"version", My.Application.Info.Version.ToString}})
            .Add("config", New JObject From {
                 {"downloadMirror", Setting_Tool_DownloadMirror.ToString},
                 {"updateInterval", Setting_Tool_CheckForUpdates.ToString}})
            .Add("language", New JObject From {
                 {"code", New JObject From {
                    {"long", GetTranslationName(Setting_Tool_Language)},
                    {"short", Setting_Tool_Language}
                 }}})
            .Add("system", New JObject From {
                 {"cultureInfo", System.Globalization.CultureInfo.CurrentCulture.ToString()},
                 {"is64bit", CStr(Environment.Is64BitOperatingSystem)},
                 {"operatingSystem", Environment.OSVersion.Version.ToString}
            })
        End With

        Return JContent
    End Function

    Sub LoadLanguage(ByVal LanguageCode_Long As String, ByVal LanguageCode_Short As String)
        Setting_Tool_Language = LanguageCode_Short
        Try
            Application.Current.Resources.MergedDictionaries.Add(New ResourceDictionary() With {
                                                                     .Source = New Uri("Languages/" & LanguageCode_Long & ".xaml", UriKind.Relative)})
        Catch ex As FileNotFoundException
            MsgBox("Unable to load language package." & vbNewLine & vbNewLine & "// Details:" & vbNewLine & "System: " & System.Globalization.CultureInfo.CurrentCulture.ToString() & vbNewLine & "Short code: " & LanguageCode_Short & vbNewLine & "Long code: " & LanguageCode_Long)
        End Try
    End Sub

    Function md5(ByVal Input As String) As String
        Dim MD5StringHash As New MD5CryptoServiceProvider
        Dim Data As Byte()
        Dim Result As Byte()
        Dim Res As String = ""
        Dim Tmp As String = ""

        Data = Encoding.ASCII.GetBytes(Input)
        Result = MD5StringHash.ComputeHash(Data)
        For i As Integer = 0 To Result.Length - 1
            Tmp = Hex(Result(i))
            If Len(Tmp) = 1 Then Tmp = "0" & Tmp
            Res += Tmp
        Next
        Return Res.ToLower
    End Function

    Function RemoveIllegalCharactersFromPath(Input As String) As String
        Dim IllegalRegex As New Regex(@"[\\/:*?""<>|]")
        Dim Result As String = IllegalRegex.Replace(Input, "")

        Return Result
    End Function

    Sub Action_CheckCompatibility(ConfigVersion As Version)
        Dim AppVersion As Version = My.Application.Info.Version
        ' Detect update
        If ConfigVersion < AppVersion Then
            If Setting_Tool_DownloadMirror = 1 Then
                Setting_Tool_DownloadMirror = 0
                MsgBox("The previously selected mirror 'Loli.al' has been shutdown by the owner and therefore caused crashes in previous versions." & vbNewLine & "Your mirror will be reset to 'Bloodcat.com'.", MsgBoxStyle.Information, "Update Compatibility Check | osu!Sync")
                Action_SaveSettings()
            End If
        End If
    End Sub

    Sub Action_SaveSettings()
        If Not Directory.Exists(I__Path_Programm & "\Settings") Then
            Directory.CreateDirectory(I__Path_Programm & "\Settings")
        End If
        Using ConfigFile = File.CreateText(I__Path_Programm & "\Settings\Settings.config")
            Dim Content As New Dictionary(Of String, String)
            With Content
                .Add("_version", My.Application.Info.Version.ToString)
                .Add("Setting_osu_Path", Setting_osu_Path)
                .Add("Setting_osu_SongsPath", Setting_osu_SongsPath)
                .Add("Setting_Tool_CheckFileAssociation", CStr(Setting_Tool_CheckFileAssociation))
                .Add("Setting_Tool_CheckForUpdates", CStr(Setting_Tool_CheckForUpdates))
                .Add("Setting_Tool_DownloadMirror", CStr(Setting_Tool_DownloadMirror))
                .Add("Setting_Tool_EnableNotifyIcon", CStr(Setting_Tool_EnableNotifyIcon))
                .Add("Setting_Tool_Importer_AutoInstallCounter", CStr(Setting_Tool_Importer_AutoInstallCounter))
                .Add("Setting_Tool_Interface_BeatmapDetailPanelWidth", CStr(Setting_Tool_Interface_BeatmapDetailPanelWidth))
                .Add("Setting_Tool_Language", Setting_Tool_Language)
                .Add("Setting_Tool_LastCheckForUpdates", CStr(Setting_Tool_LastCheckForUpdates))
                .Add("Setting_Tool_SyncOnStartup", CStr(Setting_Tool_SyncOnStartup))
                .Add("Setting_Tool_Update_DeleteFileAfter", CStr(Setting_Tool_Update_DeleteFileAfter))
                .Add("Setting_Tool_Update_SavePath", CStr(Setting_Tool_Update_SavePath))
                .Add("Setting_Tool_Update_UseDownloadPatcher", CStr(Setting_Tool_Update_UseDownloadPatcher))
                .Add("Setting_Messages_Updater_OpenUpdater", CStr(Setting_Messages_Updater_OpenUpdater))
                .Add("Setting_Messages_Updater_UnableToCheckForUpdates", CStr(Setting_Messages_Updater_UnableToCheckForUpdates))
            End With
            Dim Serializer = New JsonSerializer()
            Serializer.Serialize(ConfigFile, Content)
        End Using
    End Sub

    Sub Action_LoadSettings()
        Try
            Dim ConfigFile As JObject = CType(JsonConvert.DeserializeObject(File.ReadAllText(I__Path_Programm & "\Settings\Settings.config")), JObject)
            Dim PreviousVersion As Version

            PreviousVersion = Version.Parse(CStr(ConfigFile.SelectToken("_version")))
            If Not ConfigFile.SelectToken("Setting_osu_Path") Is Nothing Then
                Setting_osu_Path = CStr(ConfigFile.SelectToken("Setting_osu_Path"))
            End If
            If Not ConfigFile.SelectToken("Setting_osu_SongsPath") Is Nothing Then
                Setting_osu_SongsPath = CStr(ConfigFile.SelectToken("Setting_osu_SongsPath"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation") Is Nothing Then
                Setting_Tool_CheckFileAssociation = CBool(ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_CheckForUpdates") Is Nothing Then
                Setting_Tool_CheckForUpdates = CInt(ConfigFile.SelectToken("Setting_Tool_CheckForUpdates"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_DownloadMirror") Is Nothing Then
                Setting_Tool_DownloadMirror = CInt(ConfigFile.SelectToken("Setting_Tool_DownloadMirror"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon") Is Nothing Then
                Setting_Tool_EnableNotifyIcon = CInt(ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Importer_AutoInstallCounter") Is Nothing Then
                Setting_Tool_Importer_AutoInstallCounter = CInt(ConfigFile.SelectToken("Setting_Tool_Importer_AutoInstallCounter"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Interface_BeatmapDetailPanelWidth") Is Nothing Then
                Setting_Tool_Interface_BeatmapDetailPanelWidth = CInt(ConfigFile.SelectToken("Setting_Tool_Interface_BeatmapDetailPanelWidth"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Language") Is Nothing Then
                Setting_Tool_Language = CStr(ConfigFile.SelectToken("Setting_Tool_Language"))

                ' Load language library
                If Not GetTranslationName(Setting_Tool_Language) = "" Then
                    LoadLanguage(GetTranslationName(Setting_Tool_Language), Setting_Tool_Language)
                End If
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates") Is Nothing Then
                Setting_Tool_LastCheckForUpdates = CStr(ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_SyncOnStartup") Is Nothing Then
                Setting_Tool_SyncOnStartup = CBool(ConfigFile.SelectToken("Setting_Tool_SyncOnStartup"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Update_DeleteFileAfter") Is Nothing Then
                Setting_Tool_Update_DeleteFileAfter = CBool(ConfigFile.SelectToken("Setting_Tool_Update_DeleteFileAfter"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Update_SavePath") Is Nothing Then
                Setting_Tool_Update_SavePath = CStr(ConfigFile.SelectToken("Setting_Tool_Update_SavePath"))
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_Update_UseDownloadPatcher") Is Nothing Then
                Setting_Tool_Update_UseDownloadPatcher = CBool(ConfigFile.SelectToken("Setting_Tool_Update_UseDownloadPatcher"))
            End If
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater") Is Nothing Then
                Setting_Messages_Updater_OpenUpdater = CBool(ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater"))
            End If
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates") Is Nothing Then
                Setting_Messages_Updater_UnableToCheckForUpdates = CBool(ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates"))
            End If

            Action_CheckCompatibility(PreviousVersion)
        Catch ex As Exception
            MsgBox(_e("GlobalVar_invalidConfiguration"), MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            File.Delete(I__Path_Programm & "\Settings\Settings.config")
            System.Windows.Forms.Application.Restart()
            Application.Current.Shutdown()
            Exit Sub
        End Try
    End Sub

    Sub Action_PrepareData()
        ' Languages
        Dim LangDic As New Dictionary(Of String, Language)
        With LangDic        ' Please sort alphabetically
            .Add("de", New Language With {
                 .Code = "de_DE",
                 .DisplayName = "Deutsch",
                 .DisplayName_English = "German"})
            .Add("en", New Language With {
                 .Code = "en_US",
                 .DisplayName = "English",
                 .DisplayName_English = "English"})
            .Add("es", New Language With {
                 .Code = "es_EM",
                 .DisplayName = "Español",
                 .DisplayName_English = "Spanish (Modern)"})
            .Add("fr", New Language With {
                 .Code = "fr_FR",
                 .DisplayName = "Français",
                 .DisplayName_English = "French"})
            .Add("id", New Language With {
                .Code = "id_ID",
                .DisplayName = "Bahasa Indonesia",
                .DisplayName_English = "Indonesian"})
            ' Currently too many strings incomplete
            '.Add("jp", New Language With {
            '    .Code = "ja_JP",
            '    .DisplayName = "日本語",
            '    .DisplayName_English = "Japanese"})
            .Add("no", New Language With {
                .Code = "no_NO",
                .DisplayName = "Norwegian",
                .DisplayName_English = "Norsk"})
            .Add("pl", New Language With {
                .Code = "pl_PL",
                .DisplayName = "Polski",
                .DisplayName_English = "Polish"})
            .Add("ru", New Language With {
                .Code = "ru_RU",
                .DisplayName = "Русский",
                .DisplayName_English = "Russian"})

            Dim Lang_zh As New Language With {
                .Code = "zh_CN",
                .DisplayName = "中文 (简体)",
                .DisplayName_English = "Chinese Simplified"}
            .Add("zh_CN", Lang_zh)
            .Add("zh", Lang_zh)
            .Add("zh_TW", New Language With {
                .Code = "zh_TW",
                .DisplayName = "中文 (繁體)",
                .DisplayName_English = "Chinese Traditional"})
        End With
        Application_Languages = LangDic

        ' Mirrors
        With Application_Mirrors
            .Add(0, New DownloadMirror With {
                .DisplayName = "Bloodcat.com",
                .DownloadURL = "http://bloodcat.com/osu/s/%0",
                .Index = 0,
                .WebURL = "http://bloodcat.com/osu"
            })
            .Add(2, New DownloadMirror With {
                .DisplayName = "osu.uu.gl",
                .DownloadURL = "http://osu.uu.gl/s/%0",
                .Index = 0,
                .WebURL = "http://osu.uu.gl/"
            })
        End With
    End Sub

    Function Action_WriteCrashLog(ex As Exception) As String
        If Not Directory.Exists(Path.GetTempPath & "naseweis520\osu!Sync\Crashes") Then
            Directory.CreateDirectory(Path.GetTempPath & "naseweis520\osu!Sync\Crashes")
        End If
        Dim CrashFile As String = Path.GetTempPath & "naseweis520\osu!Sync\Crashes\" & DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") & ".txt"
        Using File As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(CrashFile, False)
            Dim Content As String = "=====   osu!Sync Crash | " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "   =====" & vbNewLine & vbNewLine &
                "// Information" & vbNewLine & "An exception occured in osu!Sync. If this problem persists please report it using the Feedback-window, on GitHub or on the osu!Forum." & vbNewLine & "When reporting please try to describe as detailed as possible what you've done and how the applicationen reacted." & vbNewLine & "GitHub: http://j.mp/1PDuDFp   |   osu!Forum: http://j.mp/1PDuCkK" & vbNewLine & vbNewLine &
                "// Configuration" & vbNewLine & Newtonsoft.Json.JsonConvert.SerializeObject(GetProgramInfoJson, Formatting.None) & vbNewLine & vbNewLine &
                "// Exception" & vbNewLine & ex.ToString
            File.Write(Content)
            File.Close()
        End Using

        Return CrashFile
    End Function

    <System.Runtime.InteropServices.DllImport("shell32.dll")> Sub SHChangeNotify(ByVal wEventId As Integer, ByVal uFlags As Integer, ByVal dwItem1 As Integer, ByVal dwItem2 As Integer)
    End Sub
End Module
