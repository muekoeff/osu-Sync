using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osuSync.Models;
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
        public string Id { get; set; }
        public string WebUrl { get; set; }
    }

    class Language {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string DisplayName_en { get; set; }
        public string Path { get; set; }
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
        public static SortedDictionary<string, DownloadMirror> app_mirrors = new SortedDictionary<string, DownloadMirror>() {
            {
                "osu.hexide.com",
                new DownloadMirror {
                    DisplayName = "Hexide",
                    DownloadUrl = "https://osu.hexide.com/beatmaps/%0/download",
                    Id = "osu.hexide.com",
                    WebUrl = "https://osu.hexide.com/doc"
                }
            },
            {
 +                "Ripple's Mirror",
 +                new DownloadMirror {
 +                    DisplayName = "Ripple Mirror",
 +                    DownloadUrl = "https://storage.ripple.moe/d/%0",
 +                    Id = "ripple.moe",
 +                    WebUrl = "ripple.moe"
 +                }
 +          },
            {
                "osu.uu.gl",
                new DownloadMirror {
                    DisplayName = "osu.uu.gl",
                    DownloadUrl = "http://osu.uu.gl/s/%0",
                    Id = "osu.uu.gl",
                    WebUrl = "http://osu.uu.gl/"
                }
            }
        };
        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "naseweis520" + Path.DirectorySeparatorChar + "osu!Sync";
        public static string appName = (new AssemblyName(Assembly.GetExecutingAssembly().FullName)).Name;
        public static string[] appStartArgs;
        public static string appTempPath = Path.GetTempPath() + "naseweis520/osu!Sync".Replace('/', Path.DirectorySeparatorChar);
        public static Settings appSettings = new Settings();
        public static Version AppVersion {
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
            if(configVersion < AppVersion) {
                switch(configVersion.ToString()) {
                    case "1.0.0.13":
                        if(File.Exists(appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar))) {
                            if(MessageBox.Show("osu!Sync 1.0.0.13 has an improved method of saving its configuration which will replace the old one in the next version.\n" +
                                "Your current, outdated version, is going to be migrated to the new one now.", "Post-Update Compatibility check | " + appName, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) == MessageBoxResult.OK) {
                                appSettings.SaveSettings();
                                File.Delete(appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar));
                            }
                        }
                        break;
                    case "1.0.0.15":
                        if(File.Exists(appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar)))
                            File.Delete(appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar));
                        break;
                    case "1.0.0.16":
                        MessageBox.Show("The way how osu!Sync stores your chosen download mirror has been improved.\n"
                            + "Because of this your mirror has been set to 'osu.uu.gl', the current default choice.\n\n"
                            + "Additionally, Bloodcat now requires a captcha authorisation und therefore has been removed from osu!Sync until further notice.\n"
                            + "If you want to switch to Hexide you can do so in the settings window.", "Post-Update Compatibility check | " + appName, MessageBoxButton.OK, MessageBoxImage.Information);
                        appSettings._version = AppVersion.ToString();
                        appSettings.SaveSettings();
                        break;
                    }
            }
        }

        public static string CrashLogWrite(Exception ex) {
            Directory.CreateDirectory(appTempPath + "/Crashes".Replace('/', Path.DirectorySeparatorChar));
            string crashFile = appTempPath + "/Crashes/".Replace('/', Path.DirectorySeparatorChar) + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt";
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
                FileStream fileStream = new FileStream(directory + "/prep.osuSync.tmp".Replace('/', Path.DirectorySeparatorChar), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.Dispose();
                File.Delete(directory + "/prep.osuSync.tmp".Replace('/', Path.DirectorySeparatorChar));
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

        public static string Md5(string input) {    // Source: http://stackoverflow.com/a/11477466
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

            JObject jContent = new JObject {
                {
                    "application",
                    new JObject {
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
                    AppVersion.ToString()
                }
            }
                },
                {
                    "config",
                    new JObject {
                {
                    "chosenDownloadMirror",
                    appSettings.Tool_ChosenDownloadMirror
                },
                {
                    "updateInterval",
                    appSettings.Tool_CheckForUpdates.ToString()
                }
            }
                },
                {
                    "language",
                    new JObject { {
                "code",
                appSettings.Tool_Language
            } }
                },
                {
                    "system",
                    new JObject {
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
            }
                }
            };

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
            Directory.CreateDirectory(appDataPath + "/Logs".Replace('/', Path.DirectorySeparatorChar));
            try {
                if(result.Length > 250) // Trim
                    result = result.Substring(0, 247) + "...";
                StreamWriter stream = File.AppendText(appDataPath + "/Logs/ApiAccess.txt".Replace('/', Path.DirectorySeparatorChar));
                string content = "";
                content += "[" + DateTime.Now.ToString() + " / " + AppVersion.ToString() + "] ";
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
