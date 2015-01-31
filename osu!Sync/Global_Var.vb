Imports Newtonsoft.Json, Newtonsoft.Json.Linq
Imports System.IO, System.IO.Compression
Imports System.Security.Cryptography
Imports System.Text

Module Global_Var

    Public FileExtensions() As String = {".nw520-osbl", _
                                         ".nw520-osblx"}
    Public FileExtensionsLong() As String = {"naseweis520.osuSync.osuBeatmapList", _
                                             "naseweis520.osuSync.compressedOsuBeatmapList"}
    Public FileExtensionsDescription() As String = {"osu! Beatmap List for osu!Sync", _
                                                    "Compressed osu! Beatmap List for osu!Sync"}
    Public FileExtensionsIcon() As String = {"""" & System.Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,2", _
                                             """" & System.Reflection.Assembly.GetExecutingAssembly().Location.ToString & """,1"}

    Public I__StartUpArguments() As String
    Public Const I__Path_Web_Host As String = "http://naseweis520.ml/osuSync"
    Public I__Path_Programm As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\naseweis520\osu!Sync"
    Public Const I__MsgBox_DefaultTitle As String = "Dialog | osu!Sync"
    Public Const I__MsgBox_DefaultTitle_CanBeDisabled As String = "osu!Sync | This message can be disabled in the settings"
    Public I__UserAgent As String = "osu!Sync Client - " + My.Application.Info.Version.ToString
    Public I__UserInfo As JObject
    Public Setting_osu_Path As String = "C:\Program Files (x86)\osu!"
    Public Setting_Tool_AutoLoadCacheOnStartup As Boolean = False
    Public Setting_Tool_CheckForUpdates As Integer = 3
    Public Setting_Tool_CheckFileAssociation As Boolean = True
    Public Setting_Tool_DownloadMirror As Integer = 0
    Public Setting_Tool_EnableNotifyIcon As Integer = 0
    Public Setting_Tool_LastCheckForUpdates As String = "01-01-2000 00:00:00"
    Public Setting_Tool_UpdateDeleteFileAfter As Boolean = True
    Public Setting_Tool_UpdateSavePath As String = Path.GetTempPath()
    Public Setting_Tool_UpdateUseDownloadPatcher As Boolean = True
    Public Setting_Messages_Sync_MoreThan1000Sets As Boolean = True
    Public Setting_Messages_Updater_OpenUpdater As Boolean = True
    Public Setting_Messages_Updater_UnableToCheckForUpdates As Boolean = True

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

    Function CreateFileAssociation(ByVal extension As String, _
    ByVal className As String, ByVal description As String, _
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
            MsgBox("Sorry, something went wrong.", MsgBoxStyle.OkOnly)
            MsgBox(e.Message, MsgBoxStyle.OkOnly, "Debug | osu!Sync")
            Return False
        End Try

        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0)
        Return True
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

    Sub Action_SaveSettings()
        If Not Directory.Exists(I__Path_Programm & "\Settings") Then
            Directory.CreateDirectory(I__Path_Programm & "\Settings")
        End If
        Using ConfigFile = File.CreateText(I__Path_Programm & "\Settings\Settings.config")
            Dim Content As New Dictionary(Of String, String)
            With Content
                .Add("_note", "DO NOT MODIFY THIS FILE!")
                .Add("_programm", "osu!Sync")
                .Add("version", My.Application.Info.Version.ToString)
                .Add("Setting_osu_Path", Setting_osu_Path)
                .Add("Setting_Tool_AutoLoadCacheOnStartup", CStr(Setting_Tool_AutoLoadCacheOnStartup))
                .Add("Setting_Tool_CheckFileAssociation", CStr(Setting_Tool_CheckFileAssociation))
                .Add("Setting_Tool_CheckForUpdates", CStr(Setting_Tool_CheckForUpdates))
                .Add("Setting_Tool_DownloadMirror", CStr(Setting_Tool_DownloadMirror))
                .Add("Setting_Tool_EnableNotifyIcon", CStr(Setting_Tool_EnableNotifyIcon))
                .Add("Setting_Tool_LastCheckForUpdates", CStr(Setting_Tool_LastCheckForUpdates))
                .Add("Setting_Tool_UpdateDeleteFileAfter", CStr(Setting_Tool_UpdateDeleteFileAfter))
                .Add("Setting_Tool_UpdateSavePath", CStr(Setting_Tool_UpdateSavePath))
                .Add("Setting_Tool_UpdateUseDownloadPatcher", CStr(Setting_Tool_UpdateUseDownloadPatcher))
                .Add("Setting_Messages_Sync_MoreThan1000Sets", CStr(Setting_Messages_Sync_MoreThan1000Sets))
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

            If Not ConfigFile.SelectToken("Setting_osu_Path") Is Nothing Then
                Setting_osu_Path = CType(ConfigFile.SelectToken("Setting_osu_Path"), String)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_AutoLoadCacheOnStartup") Is Nothing Then
                Setting_Tool_AutoLoadCacheOnStartup = CType(ConfigFile.SelectToken("Setting_Tool_AutoLoadCacheOnStartup"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation") Is Nothing Then
                Setting_Tool_CheckFileAssociation = CType(ConfigFile.SelectToken("Setting_Tool_CheckFileAssociation"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_CheckForUpdates") Is Nothing Then
                Setting_Tool_CheckForUpdates = CType(ConfigFile.SelectToken("Setting_Tool_CheckForUpdates"), Integer)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_DownloadMirror") Is Nothing Then
                Setting_Tool_DownloadMirror = CType(ConfigFile.SelectToken("Setting_Tool_DownloadMirror"), Integer)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon") Is Nothing Then
                Setting_Tool_EnableNotifyIcon = CType(ConfigFile.SelectToken("Setting_Tool_EnableNotifyIcon"), Integer)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates") Is Nothing Then
                Setting_Tool_LastCheckForUpdates = CType(ConfigFile.SelectToken("Setting_Tool_LastCheckForUpdates"), String)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_UpdateDeleteFileAfter") Is Nothing Then
                Setting_Tool_UpdateDeleteFileAfter = CType(ConfigFile.SelectToken("Setting_Tool_UpdateDeleteFileAfter"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_UpdateSavePath") Is Nothing Then
                Setting_Tool_UpdateSavePath = CType(ConfigFile.SelectToken("Setting_Tool_UpdateSavePath"), String)
            End If
            If Not ConfigFile.SelectToken("Setting_Tool_UpdateUseDownloadPatcher") Is Nothing Then
                Setting_Tool_UpdateUseDownloadPatcher = CType(ConfigFile.SelectToken("Setting_Tool_UpdateUseDownloadPatcher"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Messages_Sync_MoreThan1000Sets") Is Nothing Then
                Setting_Messages_Sync_MoreThan1000Sets = CType(ConfigFile.SelectToken("Setting_Messages_Sync_MoreThan1000Sets"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater") Is Nothing Then
                Setting_Messages_Updater_OpenUpdater = CType(ConfigFile.SelectToken("Setting_Messages_Updater_OpenUpdater"), Boolean)
            End If
            If Not ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates") Is Nothing Then
                Setting_Messages_Updater_UnableToCheckForUpdates = CType(ConfigFile.SelectToken("Setting_Messages_Updater_UnableToCheckForUpdates"), Boolean)
            End If
        Catch ex As Exception
            MsgBox("Your configuration file seems to be invalid or outdated." & vbNewLine & "osu!Sync will delete it and restart.", MsgBoxStyle.Exclamation, I__MsgBox_DefaultTitle)
            File.Delete(I__Path_Programm & "\Settings\Settings.config")
            System.Windows.Forms.Application.Restart()
            Application.Current.Shutdown()
            Exit Sub
        End Try
    End Sub

    <System.Runtime.InteropServices.DllImport("shell32.dll")> Sub SHChangeNotify(ByVal wEventId As Integer, ByVal uFlags As Integer, ByVal dwItem1 As Integer, ByVal dwItem2 As Integer)
    End Sub
End Module
