using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace osuSync {
    class DownloadMirror {
        public string DisplayName { get; set; }
        public string DownloadUrl { get; set; }
        public int Index { get; set; }
        public string WebUrl { get; set; }
    }

    class Language {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string DisplayName_en { get; set; }
        public string Path { get; set; }
    }

    class Settings {

        public string _version = GlobalVar.appVersion.ToString();
        public bool Api_Enabled_BeatmapPanel = false;
        public string Api_Key = "";
        public string Api_KeyEncrypted = "";
        public string osu_Path = osuPathDetect(false);
        public string osu_SongsPath = osuPathDetect(false) + Path.DirectorySeparatorChar + "Songs";
        public int Tool_CheckForUpdates = 3;
        public bool Tool_CheckFileAssociation = true;
        public int Tool_DownloadMirror = 0;
        public int Tool_EnableNotifyIcon = 0;
        public int Tool_Importer_AutoInstallCounter = 10;
        public int Tool_Interface_BeatmapDetailPanelWidth = 40;
        public string Tool_Language = "en_US";
        public Dictionary<string, Language> Tool_LanguageMeta = new Dictionary<string, Language>();
        public string Tool_LanguagePath;
        public string Tool_LastCheckForUpdates = "20000101000000";
        public bool Tool_SyncOnStartup = false;
        public bool Tool_RequestElevationOnStartup = false;
        public bool Tool_Update_DeleteFileAfter = true;
        public string Tool_Update_SavePath = GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Updater";
        public bool Tool_Update_UseDownloadPatcher = true;
        public bool Messages_Importer_AskOsu = true;
        public bool Messages_Updater_OpenUpdater = true;
        public bool Messages_Updater_UnableToCheckForUpdates = true;

        /// <param name="allowConfig"></param> Enable on initialization to prevent System.TypeInitializationException
        /// <returns>Path to osu!</returns>
        public static string osuPathDetect(bool allowConfig = true) {
            if(allowConfig && Directory.Exists(GlobalVar.appSettings.osu_Path)) {
                return GlobalVar.appSettings.osu_Path;
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar + "osu!")) {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar + "osu!";
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + Path.DirectorySeparatorChar + "osu!")) {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + Path.DirectorySeparatorChar + "osu!";
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "osu!")) {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "osu!";
            } else {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
        }

        public void LoadSettings() {
            if(File.Exists(GlobalVar.appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar +  "Settings.json")) {
                try {
                    GlobalVar.appSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(GlobalVar.appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.json"));
                    // Load language library
                    if(File.Exists(GlobalVar.appSettings.Tool_LanguagePath)) {
                        GlobalVar.TranslationLoad(GlobalVar.appSettings.Tool_LanguagePath);
                    } else {
                        MessageBox.Show(GlobalVar._e("GlobalVar_unableToFindTranslationPackage") + "\n\n" 
                            + "Details:\nPath: " + GlobalVar.appSettings.Tool_LanguagePath, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // Decrypt API key
                    if(!string.IsNullOrEmpty(GlobalVar.appSettings.Api_KeyEncrypted)) {
                        try {
                            GlobalVar.appSettings.Api_Key = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(GlobalVar.appSettings.Api_KeyEncrypted), GlobalVar.entropy, DataProtectionScope.CurrentUser));
                            GlobalVar.appSettings.Api_KeyEncrypted = "";
                        } catch(CryptographicException ex) {
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n" + 
                                ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        } catch(FormatException ex) {
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n" +
                                ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        } catch(Exception ex) {
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n" +
                                ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        }
                    }

                    // Perform compatibility check
                    GlobalVar.CompatibilityCheck(new Version(GlobalVar.appSettings._version));
                } catch(Exception) {
                    MessageBox.Show(GlobalVar._e("GlobalVar_invalidConfiguration"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                    File.Delete(GlobalVar.appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.json");
                    Process.Start(Assembly.GetExecutingAssembly().Location.ToString());
                    Environment.Exit(1);
                    return;
                }
            }
        }

        public void SaveSettings() {
            // Encrypt API key
            if(!string.IsNullOrEmpty(GlobalVar.appSettings.Api_Key)) {
                try {
                    GlobalVar.appSettings.Api_KeyEncrypted = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(GlobalVar.appSettings.Api_Key), GlobalVar.entropy, DataProtectionScope.CurrentUser));
                } catch(CryptographicException) {
                    GlobalVar.appSettings.Api_KeyEncrypted = "";
                    MessageBox.Show(GlobalVar._e("GlobalVar_unableToEncryptApi"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            Directory.CreateDirectory(GlobalVar.appDataPath + Path.DirectorySeparatorChar + "Settings");
            using(StreamWriter configFile = File.CreateText(GlobalVar.appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.json")) {
                JObject thisAppSettings = JObject.FromObject(GlobalVar.appSettings);
                thisAppSettings.Remove("Api_Key");      // Don't save decrypted API key
                JsonSerializer JS = new JsonSerializer();
                JS.Serialize(configFile, thisAppSettings);
            }
        }
    }

    static class GlobalVar {
        public class FileExtensionDefinition {
            public string fileExtension;
            public string className;
            public string description;
            public string iconPath;

            public FileExtensionDefinition(string fileExtension, string className, string description, string iconPath) {
                this.fileExtension = fileExtension;
                this.className = className;
                this.description = description;
                this.iconPath = iconPath;
            }
        }

        public static FileExtensionDefinition[] app_fileExtensions = {
            new FileExtensionDefinition(".nw520-osbl", "naseweis520.osuSync.osuBeatmapList", "MainWindow_fileext_osbl", "\"" + Assembly.GetExecutingAssembly().Location.ToString() + "\",2"),
            new FileExtensionDefinition(".nw520-osblx", "naseweis520.osuSync.compressedOsuBeatmapList", "MainWindow_fileext_osblx", "\"" + Assembly.GetExecutingAssembly().Location.ToString() + "\",1"),
        };
        public static Dictionary<int, DownloadMirror> app_mirrors = new Dictionary<int, DownloadMirror>(2) {
            {
                0,
                new DownloadMirror {
                    DisplayName = "Bloodcat.com",
                    DownloadUrl = "http://bloodcat.com/osu/s/%0",
                    Index = 0,
                    WebUrl = "http://bloodcat.com/osu"
                }
            },
            {
                2,
                new DownloadMirror {
                    DisplayName = "osu.uu.gl",
                    DownloadUrl = "http://osu.uu.gl/s/%0",
                    Index = 0,
                    WebUrl = "http://osu.uu.gl/"
                }
            }
        };
        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "naseweis520" + Path.DirectorySeparatorChar + "osu!Sync";
        public static string appName = (new AssemblyName(Assembly.GetExecutingAssembly().FullName)).Name;
        public static string[] appStartArgs;
        public static string appTempPath = Path.GetTempPath() + "naseweis520" + Path.DirectorySeparatorChar + "osu!Sync";
        public static Settings appSettings = new Settings();
        public static Version appVersion {
            get {
                return (new AssemblyName(Assembly.GetExecutingAssembly().FullName)).Version;
            }
        }
        public static byte[] entropy = {
            239,
            130,
            24,
            162,
            121
        };
        public static string msgTitleDisableable = appName + " | " + _e("GlobalVar_messageCanBeDisabled");
        public static ResourceDictionary translationHolder;
        public static Dictionary<string, Language> translationList = new Dictionary<string, Language>();
        public const string webNw520ApiRoot = "http://api.nw520.de/osuSync/";

        public const string webOsuApiRoot = "https://osu.ppy.sh/api/";
        public static bool tool_dontApplySettings = false;
        // Set in MainWindow.xaml.vb\MainWindow_Loaded()
        public static bool tool_hasWriteAccessToOsu = false;
        // Set in Application.xaml.vb\Application_Startup()
        public static bool tool_isElevated = false;

        /// <param name="text">English string to translate</param>
        /// <returns>Translation of <paramref>Text</paramref></returns>
        public static string _e(string text) {
            try {
                return System.Windows.Application.Current.FindResource(text).ToString();
            } catch(ResourceReferenceKeyNotFoundException) {
                MessageBox.Show("The application just tried to load a text (= string) which isn't registered.\n" +
                    "Normally, this shouldn't happen.\n\n" +
                    "Please report this by using the Feedback-box in the settings, contacting me using the link in the about window, reporting an issue on GitHub, or contacting me on the osu!Forum.\n\n" +
                    "// Additional information:\n" +
                    text, appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return "[Missing:" + text + "]";
            }
        }

        public static void CompatibilityCheck(Version configVersion) {
            // Detect update
            if(configVersion < appVersion) {
                switch(configVersion.ToString()) {
                    case "1.0.0.13":
                        if(File.Exists(appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.config")) {
                            if(MessageBox.Show("osu!Sync 1.0.0.13 has an improved method of saving its configuration which will replace the old one in the next version.\n" +
                                "Your current, outdated version, is going to be migrated to the new one now.", "Post-Update Compatibility check | " + appName, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) == MessageBoxResult.OK) {
                                appSettings.SaveSettings();
                                File.Delete(appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.config");
                            }
                        }
                        break;
                    case "1.0.0.15":
                        if(File.Exists(appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.config"))
                            File.Delete(appDataPath + Path.DirectorySeparatorChar + "Settings" + Path.DirectorySeparatorChar + "Settings.config");
                        break;
                }
            }
        }

        public static string CrashLogWrite(Exception ex) {
            Directory.CreateDirectory(appTempPath + Path.DirectorySeparatorChar + "Crashes");
            string crashFile = appTempPath + Path.DirectorySeparatorChar + "Crashes" + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt";
            using(StreamWriter File = new StreamWriter(crashFile, false)) {
                string content = "===== osu!Sync Crash | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   =====\n\n" +
                    "// Information\n" +
                    "An exception occured in osu!Sync. If this problem persists please report it using the Feedback-window, on GitHub or on the osu!Forum.\n" +
                    "When reporting please try to describe as detailed as possible what you've done and how the applicationen reacted.\n" +
                    "GitHub: http://j.mp/1PDuDFp   |   osu!Forum: http://j.mp/1PDuCkK \n\n" +
                    "// Configuration\n" +
                    JsonConvert.SerializeObject(ProgramInfoJsonGet(), Newtonsoft.Json.Formatting.None) + "\n\n" + 
                    "// Exception\n" + 
                    ex.ToString();
                File.Write(content);
                File.Close();
            }
            return crashFile;
        }

        public static bool DirAccessCheck(string directory) {
            try {
                FileStream fileStream = new FileStream(directory + Path.DirectorySeparatorChar + "prep.osuSync.tmp", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.Dispose();
                File.Delete(directory + Path.DirectorySeparatorChar + "prep.osuSync.tmp");
                return true;
            } catch(Exception) {
                return false;
            }
        }

        public static bool FileAssociationCreate(string extension, string className, string description, string iconPath, string exeProgram) {
            const int SHCNE_ASSOCCHANGED = 0x8000000;
            const int SHCNF_IDLIST = 0;
            if(extension.Substring(0, 1) != ".")
                extension = "." + extension;
            Microsoft.Win32.RegistryKey key1 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key2 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key3 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key4 = default(Microsoft.Win32.RegistryKey);
            try {
                key1 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension);
                key1.SetValue("", className);
                key2 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className);
                key2.SetValue("", description);
                key3 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className + "\\shell\\open\\command");
                key3.SetValue("", "\"" + exeProgram + "\" -openFile=\"%1\"");
                key4 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className + "\\DefaultIcon");
                key4.SetValue("", iconPath);
            } catch(Exception e) {
                MessageBox.Show(e.Message, "Debug | " + appName, MessageBoxButton.OK);
                return false;
            }
            if(key1 != null)
                key1.Close();
            if(key2 != null)
                key2.Close();
            if(key3 != null)
                key3.Close();
            if(key4 != null)
                key4.Close();
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return true;
        }

        public static bool FileAssociationDelete(string extension, string className) {
            const int SHCNE_ASSOCCHANGED = 0x8000000;
            const int SHCNF_IDLIST = 0;
            if(extension.Substring(0, 1) != ".")
                extension = "." + extension;

            try {
                if((Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension) != null))
                    Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension);
                if((Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(className) != null))
                    Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(className);
            } catch(Exception e) {
                MessageBox.Show(_e("GlobalVar_sorrySomethingWentWrong"), appName, MessageBoxButton.OK);
                MessageBox.Show(e.Message, "Debug |  " + appName, MessageBoxButton.OK);
                return false;
            }

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return true;
        }

        public static bool FileAssociationsCreate() {
            bool registrationErrors = false;
            foreach(FileExtensionDefinition thisExtension in app_fileExtensions) {
                if(!FileAssociationCreate(thisExtension.fileExtension, thisExtension.className, _e(thisExtension.description), thisExtension.iconPath, Assembly.GetExecutingAssembly().Location.ToString())) {
                    registrationErrors = true;
                    break;
                }
            }
            if(!registrationErrors) {
                MessageBox.Show(_e("MainWindow_extensionDone"), appName, MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            } else {
                MessageBox.Show(_e("MainWindow_extensionFailed"), appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool FileAssociationsDelete() {
            bool registrationErrors = false;
            foreach(FileExtensionDefinition thisExtension in app_fileExtensions) {
                if(!FileAssociationDelete(thisExtension.fileExtension, thisExtension.className)) {
                    registrationErrors = true;
                    break;
                }
            }
            if(registrationErrors) {
                MessageBox.Show(_e("MainWindow_extensionDeleteFailed"), appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            } else {
                return true;
            }
        }

        public static Language TranslationGetMeta(string filePath) {
            if(appSettings.Tool_LanguageMeta.ContainsKey(filePath)) {
                return appSettings.Tool_LanguageMeta[filePath];
            } else {
                try {
                    XmlReader xmlRead = XmlReader.Create(filePath);
                    ResourceDictionary thisTranslationHolder = (ResourceDictionary)XamlReader.Load(xmlRead);
                    xmlRead.Close();

                    if(thisTranslationHolder.Contains("Meta_langCode")) {
                        Language resLanguage = new Language();
                        if(thisTranslationHolder.Contains("Meta_langCode"))
                            resLanguage.Code = thisTranslationHolder["Meta_langCode"].ToString();
                        if(thisTranslationHolder.Contains("Meta_langName"))
                            resLanguage.DisplayName = thisTranslationHolder["Meta_langName"].ToString();
                        if(thisTranslationHolder.Contains("Meta_langNameEn"))
                            resLanguage.DisplayName_en = thisTranslationHolder["Meta_langNameEn"].ToString();
                        appSettings.Tool_LanguageMeta.Add(filePath, resLanguage);
                        return resLanguage;
                    } else {
                        return null;
                    }
                } catch(Exception) {
                    return null;
                }
            }
        }

        public static bool TranslationLoad(string filePath) {
            try {
                XmlReader xmlRead = XmlReader.Create(filePath);
                ResourceDictionary thisTranslationHolder = (ResourceDictionary)XamlReader.Load(xmlRead);
                xmlRead.Close();

                if(thisTranslationHolder.Contains("Meta_langCode")) {
                    appSettings.Tool_Language = thisTranslationHolder["Meta_langCode"].ToString();
                    appSettings.Tool_LanguagePath = filePath;
                    if(translationHolder != null)
                        System.Windows.Application.Current.Resources.MergedDictionaries.Remove(translationHolder);
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(thisTranslationHolder);
                    translationHolder = thisTranslationHolder;
                    return true;
                } else {
                    MessageBox.Show("Invalid/Incompatible language package.", appName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            } catch(Exception ex) {
                MessageBox.Show("Unable to load language package.\n\n" +
                    "// Details:\n" +
                    "FilePath: " + filePath + "\n" +
                    "Exception: " + ex.Message, appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static Dictionary<string, Language> TranslationMap(string rootPath) {
            Dictionary<string, Language> result = new Dictionary<string, Language>();
            foreach(string thisFile in Directory.EnumerateFiles(rootPath)) {
                string thisFileName = Path.GetFileNameWithoutExtension(thisFile);
                if(new Regex("^[a-z]{2}(?:_)[A-Z]{2}$").Match(thisFileName).Success) {
                    Language resLanguage = new Language();
                    Language langMeta = TranslationGetMeta(thisFile);
                    if(langMeta != null) {
                        resLanguage = langMeta;
                    } else {
                        resLanguage.Code = thisFileName;
                    }
                    resLanguage.Path = thisFile;
                    result.Add(thisFileName, resLanguage);
                    if(!result.ContainsKey(thisFileName.Substring(0, 2)))
                        result.Add(thisFileName.Substring(0, 2), resLanguage);
                }
            }
            return result;
        }

        public static string md5(string input) {    // Source: http://stackoverflow.com/a/11477466
            byte[] encodedPassword = new UTF8Encoding().GetBytes(input);
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
            string encoded = BitConverter.ToString(hash)
               .Replace("-", string.Empty)
               .ToLower();

            return encoded;
        }

        public static string PathSanitize(string input) {
            return string.Join("_", input.Split(Path.GetInvalidFileNameChars()));
        }

        public static JObject ProgramInfoJsonGet() {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

            JObject jContent = new JObject();
            jContent.Add("application", new JObject {
                {
                    "isElevated",
                    Convert.ToString(isElevated)
                },
                {
                    "lastUpdateCheck",
                    appSettings.Tool_LastCheckForUpdates
                },
                {
                    "version",
                    appVersion.ToString()
                }
            });
            jContent.Add("config", new JObject {
                {
                    "downloadMirror",
                    appSettings.Tool_DownloadMirror.ToString()
                },
                {
                    "updateInterval",
                    appSettings.Tool_CheckForUpdates.ToString()
                }
            });
            jContent.Add("language", new JObject { {
                "code",
                appSettings.Tool_Language
            } });
            jContent.Add("system", new JObject {
                {
                    "cultureInfo",
                    System.Globalization.CultureInfo.CurrentCulture.ToString()
                },
                {
                    "is64bit",
                    Convert.ToString(Environment.Is64BitOperatingSystem)
                },
                {
                    "operatingSystem",
                    Environment.OSVersion.Version.ToString()
                }
            });

            return jContent;
        }

        public static bool RequestElevation(string parameters = "") {
            if(!string.IsNullOrEmpty(parameters))
                parameters = " " + parameters;
            try {
                Process elevatedProcess = new Process();
                elevatedProcess.StartInfo.Arguments = "--ignoreInstances" + parameters;
                elevatedProcess.StartInfo.FileName = Assembly.GetExecutingAssembly().Location.ToString();
                elevatedProcess.StartInfo.UseShellExecute = true;
                elevatedProcess.StartInfo.Verb = "runas";
                elevatedProcess.Start();
                return true;
            } catch(Exception) {
                return false;
            }
        }

        public static string StringCompress(string text) {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream memoryStream = new MemoryStream();
            using(GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true)) {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            byte[] compressedData = new byte[Convert.ToInt32(memoryStream.Length - 1) + 1];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            byte[] gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        public static string StringDecompress(string compressedText) {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using(MemoryStream memoryStream = new MemoryStream()) {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                byte[] buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using(GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress)) {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }

        public static void WriteToApiLog(string method, string result = "{Failed}") {
            Directory.CreateDirectory(appDataPath + Path.DirectorySeparatorChar + "Logs");
            try {
                if(result.Length > 250) // Trim
                    result = result.Substring(0, 247) + "...";
                StreamWriter stream = File.AppendText(appDataPath + Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar + "ApiAccess.txt");
                string content = "";
                content += "[" + DateTime.Now.ToString() + " / " + appVersion.ToString() + "] ";
                content += method + ":\n" +
                    "\t" + result;
                stream.WriteLine(content);
                stream.Close();
            } catch(Exception) { }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, int uFlags, int dwItem1, int dwItem2);
    }
}