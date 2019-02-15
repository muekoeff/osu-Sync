using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osuSync.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;
using static osuSync.Modules.TranslationManager;

namespace osuSync {


    static class GlobalVar {
        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/naseweis520/osu!Sync".Replace('/', Path.DirectorySeparatorChar);
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
    }
}
