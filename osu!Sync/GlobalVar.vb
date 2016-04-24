Imports Newtonsoft.Json, Newtonsoft.Json.Linq
Imports System.IO, System.IO.Compression
Imports System.Security.Cryptography, System.Security.Principal
Imports System.Text

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
Class Settings
    Public _version As String = My.Application.Info.Version.ToString
    Public Api_Key As String = ""
    Public Api_Enabled_BeatmapPanel As Boolean = False
    Public osu_Path As String = OsuPathDetect(False)
    Public osu_SongsPath As String = osu_Path & "\Songs"
    Public Tool_CheckForUpdates As Integer = 3
    Public Tool_CheckFileAssociation As Boolean = True
    Public Tool_DownloadMirror As Integer = 0
    Public Tool_EnableNotifyIcon As Integer = 0
    Public Tool_Importer_AutoInstallCounter As Integer = 10
    Public Tool_Interface_BeatmapDetailPanelWidth As Integer = 40
    Public Tool_Language As String = "en"
    Public Tool_LastCheckForUpdates As String = "20000101000000"
    Public Tool_SyncOnStartup As Boolean = False
    Public Tool_RequestElevationOnStartup As Boolean = False
    Public Tool_Update_DeleteFileAfter As Boolean = True
    Public Tool_Update_SavePath As String = AppTempPath & "\Updater"
    Public Tool_Update_UseDownloadPatcher As Boolean = True
    Public Messages_Importer_AskOsu As Boolean = True
    Public Messages_Updater_OpenUpdater As Boolean = True
    Public Messages_Updater_UnableToCheckForUpdates As Boolean = True

    ''' <param name="AllowConfig"></param> Enable on initialization to prevent System.TypeInitializationException
    ''' <returns>Path to osu!</returns>
    Function OsuPathDetect(Optional AllowConfig As Boolean = True) As String
        If AllowConfig AndAlso Directory.Exists(AppSettings.osu_Path) Then
            Return AppSettings.osu_Path
        ElseIf Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) & "\osu!"
        ElseIf Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) & "\osu!"
        ElseIf Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\osu!") Then
            Return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\osu!"
        Else
            Return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        End If
    End Function

    Sub LoadSettings()
        If File.Exists(AppDataPath & "\Settings\Settings.json") Then
            Try
                AppSettings = JsonConvert.DeserializeObject(Of Settings)(File.ReadAllText(AppDataPath & "\Settings\Settings.json"))
                ' Load language library
                If Not TranslationNameGet(AppSettings.Tool_Language) = "" Then LanguageLoad(TranslationNameGet(AppSettings.Tool_Language), AppSettings.Tool_Language)
                ' Perform compatibility check
                CompatibilityCheck(New Version(AppSettings._version))
            Catch ex As Exception
                MessageBox.Show(_e("GlobalVar_invalidConfiguration"), AppDataPath, MessageBoxButton.OK, MessageBoxImage.Error)
                File.Delete(AppDataPath & "\Settings\Settings.json")
                Process.Start(Reflection.Assembly.GetExecutingAssembly().Location.ToString)
                Windows.Application.Current.Shutdown()
                Exit Sub
            End Try
        End If
    End Sub

    Sub SaveSettings()
        Directory.CreateDirectory(AppDataPath & "\Settings")
        Using ConfigFile = File.CreateText(AppDataPath & "\Settings\Settings.json")
            Dim JO As JObject = JObject.FromObject(AppSettings)
            Dim JS = New JsonSerializer()
            JS.Serialize(ConfigFile, AppSettings)
        End Using
    End Sub
End Class

Module GlobalVar
    Public Application_FileExtensions()() As String = {
            ({".nw520-osbl", "naseweis520.osuSync.osuBeatmapList", "MainWindow_fileext_osbl", """" & Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,2"}),
            ({".nw520-osblx", "naseweis520.osuSync.compressedOsuBeatmapList", "MainWindow_fileext_osblx", """" & Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,1"})
        }
    Public Application_Languages As New Dictionary(Of String, Language) ' See PrepareData()
    Public Application_Mirrors As New Dictionary(Of Integer, DownloadMirror)(2) From {
        {0, New DownloadMirror With {
            .DisplayName = "Bloodcat.com",
            .DownloadURL = "http://bloodcat.com/osu/s/%0",
            .Index = 0,
            .WebURL = "http://bloodcat.com/osu"
        }},
        {2, New DownloadMirror With {
            .DisplayName = "osu.uu.gl",
            .DownloadURL = "http://osu.uu.gl/s/%0",
            .Index = 0,
            .WebURL = "http://osu.uu.gl/"
        }}
    }

    Public AppDataPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\naseweis520\osu!Sync"
    Public AppName As String = My.Application.Info.AssemblyName
    Public AppStartArgs As String()
    Public AppTempPath As String = Path.GetTempPath() & "naseweis520\osu!Sync"
    Public AppSettings As New Settings
    Public MsgTitleDisableable As String = AppName & " | " & _e("GlobalVar_messageCanBeDisabled")
    Public Const WebNw520ApiRoot As String = "http://api.nw520.de/osuSync/"
    Public Const WebOsuApiRoot As String = "https://osu.ppy.sh/api/"

    Public Tool_DontApplySettings As Boolean = False
    Public Tool_HasWriteAccessToOsu As Boolean = False  ' Set in MainWindow.xaml.vb\MainWindow_Loaded()
    Public Tool_IsElevated As Boolean = False   ' Set in Application.xaml.vb\Application_Startup()


    ' @DEPRECATED SINCE 1.0.0.13
    Sub LoadSettings()
        Try
            Dim ConfigFile As JObject = CType(JsonConvert.DeserializeObject(File.ReadAllText(AppDataPath & "\Settings\Settings.config")), JObject)
            Dim PreviousVersion As Version

            PreviousVersion = Version.Parse(CStr(ConfigFile.SelectToken("_version")))
            If Not ConfigFile.SelectToken("Setting_Api_Enabled_BeatmapPanel") Is Nothing Then AppSettings.Api_Enabled_BeatmapPanel = CBool(ConfigFile.SelectToken("Setting_Api_Enabled_BeatmapPanel"))
            If Not ConfigFile.SelectToken("Setting_Api_Key") Is Nothing Then AppSettings.Api_Key = CStr(ConfigFile.SelectToken("Setting_Api_Key"))
            If Not ConfigFile.SelectToken("Setting_osu_Path") Is Nothing Then AppSettings.osu_Path = CStr(ConfigFile.SelectToken("Setting_osu_Path"))
            If Not ConfigFile.SelectToken("Setting_osu_SongsPath") Is Nothing Then AppSettings.osu_SongsPath = CStr(ConfigFile.SelectToken("Setting_osu_SongsPath"))
            If Not ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation") Is Nothing Then AppSettings.Tool_CheckFileAssociation = CBool(ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation"))
            If Not ConfigFile.SelectToken("Setting_Tool_CheckForUpdates") Is Nothing Then AppSettings.Tool_CheckForUpdates = CInt(ConfigFile.SelectToken("Setting_Tool_CheckForUpdates"))
            If Not ConfigFile.SelectToken("Setting_Tool_DownloadMirror") Is Nothing Then AppSettings.Tool_DownloadMirror = CInt(ConfigFile.SelectToken("Setting_Tool_DownloadMirror"))
            If Not ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon") Is Nothing Then AppSettings.Tool_EnableNotifyIcon = CInt(ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon"))
            If Not ConfigFile.SelectToken("Setting_Tool_Importer_AutoInstallCounter") Is Nothing Then AppSettings.Tool_Importer_AutoInstallCounter = CInt(ConfigFile.SelectToken("Setting_Tool_Importer_AutoInstallCounter"))
            If Not ConfigFile.SelectToken("Setting_Tool_Interface_BeatmapDetailPanelWidth") Is Nothing Then AppSettings.Tool_Interface_BeatmapDetailPanelWidth = CInt(ConfigFile.SelectToken("Setting_Tool_Interface_BeatmapDetailPanelWidth"))
            If Not ConfigFile.SelectToken("Setting_Tool_Language") Is Nothing Then
                AppSettings.Tool_Language = CStr(ConfigFile.SelectToken("Setting_Tool_Language"))
                ' Load language library
                If Not TranslationNameGet(AppSettings.Tool_Language) = "" Then LanguageLoad(TranslationNameGet(AppSettings.Tool_Language), AppSettings.Tool_Language)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates") Is Nothing Then AppSettings.Tool_LastCheckForUpdates = Date.ParseExact(CStr(ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates")), "dd-MM-yyyy hh:mm:ss", Globalization.DateTimeFormatInfo.InvariantInfo).ToString("yyyyMMddhhmmss")
            MsgBox(Date.ParseExact(CStr(ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates")), "dd-MM-yyyy hh:mm:ss", Globalization.DateTimeFormatInfo.InvariantInfo).ToString())
            If Not ConfigFile.SelectToken("Setting_Tool_RequestElevationOnStartup") Is Nothing Then AppSettings.Tool_RequestElevationOnStartup = CBool(ConfigFile.SelectToken("Setting_Tool_RequestElevationOnStartup"))
            If Not ConfigFile.SelectToken("Setting_Tool_SyncOnStartup") Is Nothing Then AppSettings.Tool_SyncOnStartup = CBool(ConfigFile.SelectToken("Setting_Tool_SyncOnStartup"))
            If Not ConfigFile.SelectToken("Setting_Tool_Update_DeleteFileAfter") Is Nothing Then AppSettings.Tool_Update_DeleteFileAfter = CBool(ConfigFile.SelectToken("Setting_Tool_Update_DeleteFileAfter"))
            If Not ConfigFile.SelectToken("Setting_Tool_Update_SavePath") Is Nothing Then AppSettings.Tool_Update_SavePath = CStr(ConfigFile.SelectToken("Setting_Tool_Update_SavePath"))
            If Not ConfigFile.SelectToken("Setting_Tool_Update_UseDownloadPatcher") Is Nothing Then AppSettings.Tool_Update_UseDownloadPatcher = CBool(ConfigFile.SelectToken("Setting_Tool_Update_UseDownloadPatcher"))
            If Not ConfigFile.SelectToken("Setting_Messages_Importer_AskOsu") Is Nothing Then AppSettings.Messages_Importer_AskOsu = CBool(ConfigFile.SelectToken("Setting_Messages_Importer_AskOsu"))
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater") Is Nothing Then AppSettings.Messages_Updater_OpenUpdater = CBool(ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater"))
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates") Is Nothing Then AppSettings.Messages_Updater_UnableToCheckForUpdates = CBool(ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates"))
            CompatibilityCheck(PreviousVersion)
        Catch ex As Exception
            MsgBox(_e("GlobalVar_invalidConfiguration"), MsgBoxStyle.Exclamation, AppName)
            File.Delete(AppDataPath & "\Settings\Settings.config")
            Forms.Application.Restart()
            Application.Current.Shutdown()
            Exit Sub
        End Try
    End Sub

    ''' <param name="Text">English string to translate</param>
    ''' <returns>Translation of <paramref>Text</paramref></returns>
    Function _e(ByRef Text As String) As String
        Try
            Return Windows.Application.Current.FindResource(Text).ToString
        Catch ex As ResourceReferenceKeyNotFoundException
            MsgBox("The application just tried to load a text (= string) which isn't registered." & vbNewLine &
                   "Normally, this shouldn't happen." & vbNewLine & vbNewLine &
                   "Please report this by using the Feedback-box in the settings, contacting me using the link in the about window, reporting an issue on GitHub, or contacting me on the osu!Forum." & vbNewLine & vbNewLine & "// Additional information:" & vbNewLine & Text, MsgBoxStyle.Critical, AppName)
            Return "[Missing:" + Text + "]"
        End Try
    End Function

    Function CrashLogWrite(ex As Exception) As String
        Directory.CreateDirectory(AppTempPath & "\Crashes")
        Dim CrashFile As String = AppTempPath & "\Crashes\" & Date.Now.ToString("yyyy-MM-dd HH.mm.ss") & ".txt"
        Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(CrashFile, False)
            Dim Content As String = "===== osu!Sync Crash | " & Date.Now.ToString("yyyy-MM-dd HH:mm:ss") & "   =====" & vbNewLine & vbNewLine &
                "// Information" & vbNewLine & "An exception occured in osu!Sync. If this problem persists please report it using the Feedback-window, on GitHub or on the osu!Forum." & vbNewLine & "When reporting please try to describe as detailed as possible what you've done and how the applicationen reacted." & vbNewLine & "GitHub: http://j.mp/1PDuDFp   |   osu!Forum: http://j.mp/1PDuCkK" & vbNewLine & vbNewLine &
                "// Configuration" & vbNewLine & JsonConvert.SerializeObject(ProgramInfoJsonGet, Formatting.None) & vbNewLine & vbNewLine &
                "// Exception" & vbNewLine & ex.ToString
            File.Write(Content)
            File.Close()
        End Using
        Return CrashFile
    End Function

    Function DirAccessCheck(ByVal Directory As String) As Boolean
        Try
            Dim fs As New FileStream(Directory & "\Preparation.osuSync.tmp", FileMode.OpenOrCreate, FileAccess.ReadWrite)
            Dim s As New StreamWriter(fs)
            s.Dispose()
            File.Delete(Directory & "\Preparation.osuSync.tmp")
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Function FileAssociationCreate(ByVal extension As String,
    ByVal className As String, ByVal description As String,
    ByVal iconPath As String, ByVal exeProgram As String) As Boolean
        Const SHCNE_ASSOCCHANGED = &H8000000
        Const SHCNF_IDLIST = 0
        If extension.Substring(0, 1) <> "." Then extension = "." & extension
        Dim key1, key2, key3, key4 As Microsoft.Win32.RegistryKey
        Try
            key1 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension)
            key1.SetValue("", className)
            key2 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className)
            key2.SetValue("", description)
            key3 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className & "\shell\open\command")
            key3.SetValue("", """" & exeProgram & """ -openFile=""%1""")
            key4 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className & "\DefaultIcon")
            key4.SetValue("", iconPath)
        Catch e As Exception
            MsgBox(e.Message, MsgBoxStyle.OkOnly, "Debug | " & AppName)
            Return False
        End Try
        If Not key1 Is Nothing Then key1.Close()
        If Not key2 Is Nothing Then key2.Close()
        If Not key3 Is Nothing Then key3.Close()
        If Not key4 Is Nothing Then key4.Close()
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        Return True
    End Function

    Function FileAssociationDelete(ByVal extension As String, ByVal className As String) As Boolean
        Const SHCNE_ASSOCCHANGED = &H8000000
        Const SHCNF_IDLIST = 0
        If extension.Substring(0, 1) <> "." Then extension = "." & extension

        Try
            If Not Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension) Is Nothing Then Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension)
            If Not Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(className) Is Nothing Then Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(className)
        Catch e As Exception
            MsgBox(_e("GlobalVar_sorrySomethingWentWrong"), MsgBoxStyle.OkOnly, AppName)
            MsgBox(e.Message, MsgBoxStyle.OkOnly, "Debug |  " & AppName)
            Return False
        End Try

        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        Return True
    End Function

    Function FileAssociationsCreate() As Boolean
        Dim RegisterErrors As Boolean = False
        For Each Extension() As String In Application_FileExtensions
            If Not FileAssociationCreate(Extension(0),
                                     Extension(1),
                                     _e(Extension(2)),
                                     Extension(3),
                                     Reflection.Assembly.GetExecutingAssembly().Location.ToString) Then
                RegisterErrors = True
                Exit For
            End If
        Next
        If RegisterErrors Then
            MsgBox(_e("MainWindow_extensionDone"), MsgBoxStyle.Information, AppName)
            Return True
        Else
            MsgBox(_e("MainWindow_extensionFailed"), MsgBoxStyle.Critical, AppName)
            Return False
        End If
    End Function

    Function FileAssociationsDelete() As Boolean
        Dim RegisterError As Boolean = False
        For Each Extension() As String In Application_FileExtensions
            If Not FileAssociationDelete(Extension(0), Extension(1)) Then
                RegisterError = True
                Exit For
            End If
        Next
        If RegisterError Then
            MsgBox(_e("MainWindow_extensionDeleteFailed"), MsgBoxStyle.Critical, AppName)
            Return False
        Else
            Return True
        End If
    End Function

    Function LanguageLoad(ByVal LanguageCode_Long As String, ByVal LanguageCode_Short As String) As Boolean
        AppSettings.Tool_Language = LanguageCode_Short
        Try
            Windows.Application.Current.Resources.MergedDictionaries.Add(New ResourceDictionary() With {
                                                                     .Source = New Uri("Languages/" & LanguageCode_Long & ".xaml", UriKind.Relative)})
            Return True
        Catch ex As FileNotFoundException
            MsgBox("Unable to load language package." & vbNewLine & vbNewLine & "// Details:" & vbNewLine & "System: " & Globalization.CultureInfo.CurrentCulture.ToString() & vbNewLine & "Short code: " & LanguageCode_Short & vbNewLine & "Long code: " & LanguageCode_Long, MsgBoxStyle.Critical, AppName)
            Return False
        End Try
    End Function

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

    Function PathSanitize(Input As String) As String
        Return String.Join("_", Input.Split(Path.GetInvalidFileNameChars()))
    End Function

    Function ProgramInfoJsonGet() As JObject
        Dim principal = New WindowsPrincipal(WindowsIdentity.GetCurrent())
        Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)

        Dim JContent As New JObject
        With JContent
            .Add("application", New JObject From {
                 {"isElevated", CStr(isElevated)},
                 {"lastUpdateCheck", AppSettings.Tool_LastCheckForUpdates},
                 {"version", My.Application.Info.Version.ToString}})
            .Add("config", New JObject From {
                 {"downloadMirror", AppSettings.Tool_DownloadMirror.ToString},
                 {"updateInterval", AppSettings.Tool_CheckForUpdates.ToString}})
            .Add("language", New JObject From {
                 {"code", New JObject From {
                    {"long", TranslationNameGet(AppSettings.Tool_Language)},
                    {"short", AppSettings.Tool_Language}
                 }}})
            .Add("system", New JObject From {
                 {"cultureInfo", System.Globalization.CultureInfo.CurrentCulture.ToString()},
                 {"is64bit", CStr(Environment.Is64BitOperatingSystem)},
                 {"operatingSystem", Environment.OSVersion.Version.ToString}
            })
        End With

        Return JContent
    End Function

    Function RequestElevation(Optional Parameters As String = "") As Boolean
        If Not Parameters = "" Then Parameters = " " & Parameters
        Try
            Dim ElevateProcess As New Process
            With ElevateProcess.StartInfo
                .Arguments = "--ignoreInstances" & Parameters
                .FileName = Reflection.Assembly.GetExecutingAssembly().Location.ToString
                .UseShellExecute = True
                .Verb = "runas"
            End With
            ElevateProcess.Start()
            Return True
        Catch ex As ComponentModel.Win32Exception
            Return False
        End Try
    End Function

    Function StringCompress(ByVal text As String) As String
        Dim buffer() As Byte = Encoding.UTF8.GetBytes(text)
        Dim memoryStream = New MemoryStream()
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

    Function StringDecompress(ByVal compressedText As String) As String
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

    Function TranslationNameGet(ByVal LanguageCode_Short As String) As String
        If Application_Languages.ContainsKey(LanguageCode_Short) Then
            Return Application_Languages(LanguageCode_Short).Code
        Else
            Return ""
        End If
    End Function

    Sub CompatibilityCheck(ConfigVersion As Version)
        If ConfigVersion < My.Application.Info.Version Then  ' Detect update
            Select Case ConfigVersion
                Case < New Version("1.0.0.1")
                    If AppSettings.Tool_DownloadMirror = 1 Then
                        AppSettings.Tool_DownloadMirror = 0
                        MsgBox("The previously selected mirror 'Loli.al' has been shutdown by the owner and therefore caused crashes in previous versions." & vbNewLine &
                               "Your mirror will be reset to 'Bloodcat.com'.", MsgBoxStyle.Information, "Post-Update Compatibility check | " & AppName)
                        AppSettings.SaveSettings()
                    End If
                Case < New Version("1.0.0.13")
                    If File.Exists(AppDataPath & "\Settings\Settings.config") Then
                        If MessageBox.Show("osu!Sync 1.0.0.13 has an improved method of saving its configuration which will replace the old one in the next version." & vbNewLine &
                                           "Your current, outdated version, is going to migrated to the new one now.", "Post-Update Compatibility check | " & AppName, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) = MessageBoxResult.OK Then
                            AppSettings.SaveSettings()
                            File.Delete(AppDataPath & "\Settings\Settings.config")
                        End If
                    End If
            End Select
        End If
    End Sub

    Sub DataPrepare()
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
            .Add("en_ud", New Language With {
                 .Code = "en_ud",
                 .DisplayName = "(uʍop ǝpısdn) ɥsıןƃuǝ",
                 .DisplayName_English = "English (Upside Down)"})
            '.Add("eo", New Language With {     |   Not ready for release
            '    .Code = "eo_UY",
            '    .DisplayName = "Esperanto",
            '    .DisplayName_English = "Esperanto"})
            .Add("es", New Language With {
                 .Code = "es_EM",
                 .DisplayName = "Español",
                 .DisplayName_English = "Spanish (Modern)"})
            .Add("fr", New Language With {
                 .Code = "fr_FR",
                 .DisplayName = "Français",
                 .DisplayName_English = "French"})
            .Add("hu", New Language With {
                .Code = "hu_HU",
                .DisplayName = "Magyar",
                .DisplayName_English = "Hungarian"})
            .Add("id", New Language With {
                .Code = "id_ID",
                .DisplayName = "Bahasa Indonesia",
                .DisplayName_English = "Indonesian"})
            '.Add("jp", New Language With {     |   Not ready for release
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
            .Add("th", New Language With {
                .Code = "th_TH",
                .DisplayName = "ภาษาไทย",
                .DisplayName_English = "Thai"})
            '.Add("tr", New Language With {     |   Not ready for release
            '    .Code = "tr_TR",
            '    .DisplayName = "Türkçe",
            '    .DisplayName_English = "Turkish"})
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
    End Sub

    Sub WriteToApiLog(Method As String, Optional Result As String = "{Failed}")
        Directory.CreateDirectory(AppDataPath & "\Logs")
        Try
            ' Trim
            If Result.Length > 250 Then Result = Result.Substring(0, 247) & "..."
            Dim Stream As StreamWriter = File.AppendText(AppDataPath & "\Logs\ApiAccess.txt")
            Dim Content As String = ""
            Content += "[" & Now.ToString() & " / " & My.Application.Info.Version.ToString & "] "
            Content += Method & ":" & vbNewLine & vbTab & Result
            Stream.WriteLine(Content)
            Stream.Close()
        Catch ex As Exception
        End Try
    End Sub

    <Runtime.InteropServices.DllImport("shell32.dll")> Sub SHChangeNotify(ByVal wEventId As Integer, ByVal uFlags As Integer, ByVal dwItem1 As Integer, ByVal dwItem2 As Integer)
    End Sub
End Module
