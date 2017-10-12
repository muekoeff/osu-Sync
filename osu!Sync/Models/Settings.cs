using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace osuSync.Models {
    class Settings {
        public string _version = GlobalVar.AppVersion.ToString();
        public bool Api_Enabled_BeatmapPanel = false;
        public string Api_Key = "";
        public string Api_KeyEncrypted = "";
        public string osu_Path = OsuPathDetect(false);
        public string osu_SongsPath = OsuPathDetect(false) + "/Songs".Replace('/', Path.DirectorySeparatorChar);
        public int Tool_CheckForUpdates = 3;
        public bool Tool_CheckFileAssociation = true;
        public String Tool_ChosenDownloadMirror = MirrorManager.DEFAULT_MIRROR_ID;
        public int Tool_EnableNotifyIcon = 0;
        public int Tool_Importer_AutoInstallCounter = 10;
        public int Tool_Interface_BeatmapDetailPanelWidth = 40;
        public string Tool_Language = "en_US";
        public string Tool_LanguagePath;
        public string Tool_LastCheckForUpdates = "20000101000000";
        public bool Tool_SyncOnStartup = false;
        public bool Tool_RequestElevationOnStartup = false;
        public bool Tool_Update_DeleteFileAfter = true;
        public string Tool_Update_SavePath = GlobalVar.appTempPath + "/Updater".Replace('/', Path.DirectorySeparatorChar);
        public bool Tool_Update_UseDownloadPatcher = true;
        public bool Messages_Importer_AskOsu = true;
        public bool Messages_Updater_OpenUpdater = true;
        public bool Messages_Updater_UnableToCheckForUpdates = true;

        /// <param name="allowConfig"></param> Enable on initialization to prevent System.TypeInitializationException
        /// <returns>Path to osu!</returns>
        public static string OsuPathDetect(bool allowConfig = true) {
            if(allowConfig && Directory.Exists(GlobalVar.appSettings.osu_Path)) {
                return GlobalVar.appSettings.osu_Path;
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/osu!".Replace('/', Path.DirectorySeparatorChar))) {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/osu!".Replace('/', Path.DirectorySeparatorChar);
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/osu!".Replace('/', Path.DirectorySeparatorChar))) {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/osu!".Replace('/', Path.DirectorySeparatorChar);
            } else if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!".Replace('/', Path.DirectorySeparatorChar))) {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/osu!".Replace('/', Path.DirectorySeparatorChar);
            } else {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
        }

        public void LoadSettings() {
            if(File.Exists(GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar))) {
                try {
                    GlobalVar.appSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar)));
                    // Load language library
                    if(File.Exists(GlobalVar.appSettings.Tool_LanguagePath)) {
                        TranslationManager.TranslationLoad(GlobalVar.appSettings.Tool_LanguagePath);
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
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n"
                                + ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        } catch(FormatException ex) {
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n"
                                + ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        } catch(Exception ex) {
                            MessageBox.Show(GlobalVar._e("GlobalVar_unableToDecryptApi") + "\n\n"
                                + ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            SaveSettings();
                        }
                    }

                    // Perform compatibility check
                    GlobalVar.CompatibilityCheck(new Version(GlobalVar.appSettings._version));
                    MirrorManager.CheckMirror();
                } catch(Exception) {
                    MessageBox.Show(GlobalVar._e("GlobalVar_invalidConfiguration"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                    File.Delete(GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar));
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

            Directory.CreateDirectory(GlobalVar.appDataPath + "/Settings".Replace('/', Path.DirectorySeparatorChar));
            var test = GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar);
            using(StreamWriter configFile = File.CreateText(GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar))) {
                JObject thisAppSettings = JObject.FromObject(GlobalVar.appSettings);
                thisAppSettings.Remove("Api_Key");      // Don't save decrypted API key
                JsonSerializer JS = new JsonSerializer();
                JS.Serialize(configFile, thisAppSettings);
            }
        }
    }

}
