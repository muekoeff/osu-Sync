using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osuSync.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using static osuSync.FileExtensions;

namespace osuSync {

    public partial class Window_Settings {

        public Window_Settings() {
            InitializeComponent();
        }

        public void ApplySettings() {
			GlobalVar.appSettings.Api_Enabled_BeatmapPanel = Convert.ToBoolean(CB_ApiEnableInBeatmapPanel.IsChecked);
			GlobalVar.appSettings.Api_Key = TB_ApiKey.Text;
			GlobalVar.appSettings.osu_Path = TB_osu_Path.Text;
			GlobalVar.appSettings.osu_SongsPath = TB_osu_SongsPath.Text;
			GlobalVar.appSettings.Tool_CheckFileAssociation = Convert.ToBoolean(CB_ToolCheckFileAssociation.IsChecked);
			GlobalVar.appSettings.Tool_CheckForUpdates = CB_ToolCheckForUpdates.SelectedIndex;
			GlobalVar.appSettings.Tool_ChosenDownloadMirror = ((MirrorManager.DownloadMirror)CB_ToolDownloadMirror.SelectedItem).Id;
			GlobalVar.appSettings.Tool_EnableNotifyIcon = CB_ToolEnableNotifyIcon.SelectedIndex;

            if(int.TryParse(TB_ToolImporterAutoInstallCounter.Text, out int val_TB_ToolImporterAutoInstallCounter))
                GlobalVar.appSettings.Tool_Importer_AutoInstallCounter = val_TB_ToolImporterAutoInstallCounter;

            if(int.TryParse(TB_ToolInterface_BeatmapDetailPanelWidth.Text, out int val_TB_ToolInterface_BeatmapDetailPanelWidth)
                && val_TB_ToolInterface_BeatmapDetailPanelWidth >= 5 & val_TB_ToolInterface_BeatmapDetailPanelWidth <= 95)
                GlobalVar.appSettings.Tool_Interface_BeatmapDetailPanelWidth = Convert.ToInt32(TB_ToolInterface_BeatmapDetailPanelWidth.Text);

            GlobalVar.appSettings.Tool_SyncOnStartup = Convert.ToBoolean(CB_ToolSyncOnStartup.IsChecked);
			// Load Language
			string LangCode = CB_ToolLanguages.Text.Substring(0, CB_ToolLanguages.Text.IndexOf(" "));
			if(!string.IsNullOrEmpty(CB_ToolLanguages.Text) & !(GlobalVar.appSettings.Tool_Language == LangCode) & TranslationManager.translationList.ContainsKey(LangCode)) {
				if(TranslationManager.TranslationLoad(TranslationManager.translationList[LangCode].Path))
					MessageBox.Show(GlobalVar._e("WindowSettings_languageUpdated"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Information);
			} else if(LangCode == "en_US" & TranslationManager.translationHolder != null) {
                GlobalVar.appSettings.Tool_Language = "en_US";
				System.Windows.Application.Current.Resources.MergedDictionaries.Remove(TranslationManager.translationHolder);
                TranslationManager.translationHolder = null;
			}
			GlobalVar.appSettings.Tool_RequestElevationOnStartup = Convert.ToBoolean(CB_ToolRequestElevationOnStartup.IsChecked);
			GlobalVar.appSettings.Tool_Update_SavePath = TB_ToolUpdate_Path.Text;
			GlobalVar.appSettings.Tool_Update_DeleteFileAfter = Convert.ToBoolean(CB_ToolUpdateDeleteFileAfter.IsChecked);
			GlobalVar.appSettings.Tool_Update_UseDownloadPatcher = Convert.ToBoolean(CB_ToolUpdate_UseDownloadPatcher.IsChecked);
			GlobalVar.appSettings.Messages_Importer_AskOsu = Convert.ToBoolean(CB_MessagesImporterAskOsu.IsChecked);
			GlobalVar.appSettings.Messages_Updater_OpenUpdater = Convert.ToBoolean(CB_MessagesUpdaterOpenUpdater.IsChecked);
			GlobalVar.appSettings.Messages_Updater_UnableToCheckForUpdates = Convert.ToBoolean(CB_MessagesUpdaterUnableToCheckForUpdates.IsChecked);
			GlobalVar.appSettings.SaveSettings();
		}

		public void ApiClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			JArray JSON_Array = null;
			try {
                GlobalVar.WriteToApiLog("/api/get_beatmaps", e.Result);
				JSON_Array = (JArray)JsonConvert.DeserializeObject(e.Result);
				if(((JObject)JSON_Array.First).SelectToken("beatmapset_id") != null) {
                    Bu_ApiKey_Validate.Content = GlobalVar._e("WindowSettings_valid");
                    Bu_ApiKey_Validate.IsEnabled = true;
					TB_ApiKey.IsEnabled = true;
				} else {
					throw new ArgumentException("Unexpected value");
				}
			} catch(Exception) {
                GlobalVar.WriteToApiLog("/api/get_beatmaps");
                Bu_ApiKey_Validate.Content = GlobalVar._e("WindowSettings_invalid");
                Bu_ApiKey_Validate.IsEnabled = true;
				TB_ApiKey.IsEnabled = true;
			}
		}

        #region "Bu - Button"
        public void Bu_ApiKey_Validate_Click(object sender, RoutedEventArgs e) {
            Bu_ApiKey_Validate.Content = "...";
            Bu_ApiKey_Validate.IsEnabled = false;
			TB_ApiKey.IsEnabled = false;
			WebClient ApiClient = new WebClient();
			ApiClient.DownloadStringCompleted += ApiClient_DownloadStringCompleted;
			ApiClient.DownloadStringAsync(new Uri(GlobalVar.webOsuApiRoot + "get_beatmaps?k=" + TB_ApiKey.Text));
		}

		public void Bu_ApiOpenLog_Click(object sender, RoutedEventArgs e) {
			if(File.Exists(GlobalVar.appDataPath + "/Logs/ApiAccess.txt".Replace('/', Path.DirectorySeparatorChar))) {
				Process.Start(GlobalVar.appDataPath + "/Logs/ApiAccess.txt".Replace('/', Path.DirectorySeparatorChar));
			} else {
				MessageBox.Show(GlobalVar._e("WindowSettings_nopeDirectoryDoesNotExit"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		public void Bu_ApiRequest_Click(object sender, RoutedEventArgs e) {
			Process.Start("https://osu.ppy.sh/p/api");
		}

		public void Bu_Apply_Click(object sender, RoutedEventArgs e) {
			ApplySettings();
		}

		public void Bu_Cancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		public void Bu_CreateShortcut_Click(object sender, RoutedEventArgs e) {
			if(!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/osu!Sync.lnk".Replace('/', Path.DirectorySeparatorChar))) {
				if(CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/osu!Sync.lnk".Replace('/', Path.DirectorySeparatorChar), System.Reflection.Assembly.GetExecutingAssembly().Location.ToString(), "", GlobalVar._e("WindowSettings_launchOsuSync"))) {
				} else {
                    MessageBox.Show(GlobalVar._e("WindowSettings_unableToCreateShortcut"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
			} else {
                MessageBox.Show(GlobalVar._e("WindowSettings_theresAlreadyAShortcut"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
		}

		public void Bu_Done_Click(object sender, RoutedEventArgs e) {
			ApplySettings();
			Close();
		}

		public void Bu_osuSongPathDefault_Click(object sender, RoutedEventArgs e) {
			TB_osu_SongsPath.Text = TB_osu_Path.Text + "/Songs".Replace('/', Path.DirectorySeparatorChar);
		}

		public void Bu_ToolDeleteConfiguration_Click(object sender, RoutedEventArgs e) {
			if(MessageBox.Show(GlobalVar._e("WindowSettings_areYouSureYouWantToDeleteConfig"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) {
				if(File.Exists(GlobalVar.appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar))) {
					File.Delete(GlobalVar.appDataPath + "/Settings/Settings.config".Replace('/', Path.DirectorySeparatorChar));

					if(MessageBox.Show(GlobalVar._e("WindowSettings_okDoneDoYouWantToRestart"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
						System.Windows.Forms.Application.Restart();
					}
					System.Windows.Application.Current.Shutdown();
				} else {
                    MessageBox.Show(GlobalVar._e("WindowSettings_nopeNoConfig"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		public void Bu_ToolDeleteFileAssociation_Click(object sender, RoutedEventArgs e) {
			if(FileAssociationsDelete())
                MessageBox.Show(GlobalVar._e("MainWindow_extensionDeleteDone"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void Bu_ToolImporterAutoInstallCounterDown_Click(object sender, RoutedEventArgs e) {
            if(int.TryParse(TB_ToolImporterAutoInstallCounter.Text, out int TB_ToolImporterAutoInstallCounter_value) && TB_ToolImporterAutoInstallCounter_value > 0) {
                TB_ToolImporterAutoInstallCounter.Text = Convert.ToString(TB_ToolImporterAutoInstallCounter_value - 1);
            } else {
                TB_ToolImporterAutoInstallCounter.Text = "10";
            }
        }

		public void Bu_ToolImporterAutoInstallCounterUp_Click(object sender, RoutedEventArgs e) {
            if(int.TryParse(TB_ToolImporterAutoInstallCounter.Text, out int TB_ToolImporterAutoInstallCounter_value) && TB_ToolImporterAutoInstallCounter_value > 0) {
                TB_ToolImporterAutoInstallCounter.Text = Convert.ToString(TB_ToolImporterAutoInstallCounter_value + 1);
            } else {
                TB_ToolImporterAutoInstallCounter.Text = "10";
            }
        }

		public void Bu_ToolInterface_BeatmapDetailPanelWidth_Down_Click(object sender, RoutedEventArgs e) {
            if(int.TryParse(TB_ToolInterface_BeatmapDetailPanelWidth.Text, out int TB_ToolInterface_BeatmapDetailPanelWidth_value) && TB_ToolInterface_BeatmapDetailPanelWidth_value > 5) {
                TB_ToolInterface_BeatmapDetailPanelWidth.Text = Convert.ToString(TB_ToolInterface_BeatmapDetailPanelWidth_value - 1);
            } else {
                TB_ToolInterface_BeatmapDetailPanelWidth.Text = "40";
            }
        }

		public void Bu_ToolInterface_BeatmapDetailPanelWidth_Up_Click(object sender, RoutedEventArgs e) {
            if(int.TryParse(TB_ToolInterface_BeatmapDetailPanelWidth.Text, out int TB_ToolInterface_BeatmapDetailPanelWidth_value) && TB_ToolInterface_BeatmapDetailPanelWidth_value < 95) {
                TB_ToolInterface_BeatmapDetailPanelWidth.Text = Convert.ToString(TB_ToolInterface_BeatmapDetailPanelWidth_value + 1);
            } else {
                TB_ToolInterface_BeatmapDetailPanelWidth.Text = "40";
            }
        }

		public void Bu_ToolOpenDataFolder_Click(object sender, RoutedEventArgs e) {
			if(Directory.Exists(GlobalVar.appDataPath)) {
				Process.Start(GlobalVar.appDataPath);
			} else {
                MessageBox.Show(GlobalVar._e("WindowSettings_nopeDirectoryDoesNotExit"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		public void Bu_ToolReset_Click(object sender, RoutedEventArgs e) {
			if(MessageBox.Show(GlobalVar._e("WindowSettings_areYouSureYouWantToReset"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) {
                FileAssociationsDelete();
				if(Directory.Exists(GlobalVar.appDataPath)) {
					try {
						Directory.Delete(GlobalVar.appDataPath, true);
					} catch (IOException) {}
				}
				if(Directory.Exists(GlobalVar.appTempPath)) {
					try {
						Directory.Delete(GlobalVar.appTempPath, true);
					} catch (IOException) {}
				}
				if(MessageBox.Show(GlobalVar._e("WindowSettings_okDoneDoYouWantToRestart"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
					System.Windows.Forms.Application.Restart();
				System.Windows.Application.Current.Shutdown();
			}
		}

		public void Bu_ToolRestartElevated_Click(object sender, RoutedEventArgs e) {
			if(GlobalVar.RequestElevation()) {
                GlobalVar.tool_dontApplySettings = true;
				System.Windows.Application.Current.Shutdown();
				return;
			} else {
				MessageBox.Show(GlobalVar._e("MainWindow_elevationFailed"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void Bu_ToolUpdateFileAssociation_Click(object sender, RoutedEventArgs e) {
            FileAssociationsCreate();
		}

		public void Bu_ToolUpdate_PathDefault_Click(object sender, RoutedEventArgs e) {
			TB_ToolUpdate_Path.Text = GlobalVar.appTempPath + "/Updater".Replace('/', Path.DirectorySeparatorChar);
		}
        #endregion

        public bool CreateShortcut(string sLinkFile, string sTargetFile, string sArguments = "", string sDescription = "", string sWorkingDir = "") {
            // Source: http://www.vbarchiv.net/tipps/details.php?id=1601
            // @TODO: Find alternative
            try {
                Shell32.Shell oShell = new Shell32.Shell();
                Shell32.Folder oFolder = null;
                Shell32.ShellLinkObject oLink = null;
                string sPath = sLinkFile.Substring(0, sLinkFile.LastIndexOf(Path.DirectorySeparatorChar));
                string sFile = Path.GetFileName(sLinkFile);
                short F = Convert.ToInt16(Microsoft.VisualBasic.FileSystem.FreeFile());
                Microsoft.VisualBasic.FileSystem.FileOpen(F, sLinkFile, Microsoft.VisualBasic.OpenMode.Output);
                Microsoft.VisualBasic.FileSystem.FileClose(F);
                oFolder = oShell.NameSpace(sPath);

                oLink = (Shell32.ShellLinkObject)oFolder.Items().Item(sFile).GetLink;
                if(sArguments.Length > 0)
                    oLink.Arguments = sArguments;
                if(sDescription.Length > 0)
                    oLink.Description = sDescription;
                if(sWorkingDir.Length > 0)
                    oLink.WorkingDirectory = sWorkingDir;
                oLink.Path = sTargetFile;
                oLink.Save();
                oLink = null;

                oFolder = null;
                oShell = null;
                return true;
            } catch(Exception) {
                if(File.Exists(sLinkFile))
                    File.Delete(sLinkFile);
                return false;
            }
        }

        #region "TB - TextBox"
        public void TB_ApiKey_TextChanged(object sender, TextChangedEventArgs e) {
			Bu_ApiKey_Validate.Content = GlobalVar._e("WindowSettings_validate");
		}

		public void TB_osu_Path_GotFocus(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog SelectFile = new System.Windows.Forms.OpenFileDialog {
				CheckFileExists = true,
				CheckPathExists = true,
				DefaultExt = "exe",
				FileName = "osu!",
				Filter = GlobalVar._e("WindowSettings_executableFiles") + " (*.exe)|*.exe",
				InitialDirectory = Settings.OsuPathDetect(),
				Multiselect = false,
				Title = GlobalVar._e("WindowSettings_pleaseOpenOsu")
			};

			if(!(SelectFile.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)) {
				if(Path.GetFileName(SelectFile.FileName) == "osu!.exe") {
					TB_osu_Path.Text = Path.GetDirectoryName(SelectFile.FileName);
				} else {
					MessageBox.Show(GlobalVar._e("WindowSettings_youSelectedTheWrongFile"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		public void TB_osu_SongsPath_GotFocus(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog SelectFile = new System.Windows.Forms.FolderBrowserDialog { Description = GlobalVar._e("WindowSettings_pleaseSelectSongsFolder") };

			if(SelectFile.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
				TB_osu_SongsPath.Text = SelectFile.SelectedPath;
		}

		public void TB_ToolImporterAutoInstallCounter_LostFocus(object sender, RoutedEventArgs e) {
            if(!int.TryParse(TB_ToolImporterAutoInstallCounter.Text, out int TB_ToolImporterAutoInstallCounter_value))
                TB_ToolImporterAutoInstallCounter.Text = GlobalVar._e("WindowSettings_invalidValue");
        }

		public void TB_ToolInterface_BeatmapDetailPanelWidth_LostFocus(object sender, RoutedEventArgs e) {
            if(!int.TryParse(TB_ToolInterface_BeatmapDetailPanelWidth.Text, out int TB_ToolInterface_BeatmapDetailPanelWidth_value) || TB_ToolInterface_BeatmapDetailPanelWidth_value < 5 || TB_ToolInterface_BeatmapDetailPanelWidth_value > 95)
                TB_ToolInterface_BeatmapDetailPanelWidth.Text = GlobalVar._e("WindowSettings_invalidValue");
        }

		public void TB_ToolUpdate_Path_GotFocus(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog SelectDirectory = new System.Windows.Forms.FolderBrowserDialog {
				Description = GlobalVar._e("WindowSettings_pleaseSelectDirectoryWhereToSaveUpdates"),
				ShowNewFolderButton = false
			};
            SelectDirectory.SelectedPath = (Directory.Exists(GlobalVar.appSettings.Tool_Update_SavePath) ? GlobalVar.appSettings.Tool_Update_SavePath : Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

			if(SelectDirectory.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
				TB_ToolUpdate_Path.Text = SelectDirectory.SelectedPath;
		}
        #endregion

        public void WindowSettings_Loaded(object sender, RoutedEventArgs e) {
			if(GlobalVar.tool_isElevated) {
				Bu_ToolRestartElevated.IsEnabled = false;
				TB_NotElevated.Visibility = Visibility.Collapsed;
			} else {
				Bu_ToolDeleteFileAssociation.IsEnabled = false;
				Bu_ToolReset.IsEnabled = false;
				Bu_ToolUpdateFileAssociation.IsEnabled = false;
			}

			CB_ApiEnableInBeatmapPanel.IsChecked = GlobalVar.appSettings.Api_Enabled_BeatmapPanel;
			CB_MessagesImporterAskOsu.IsChecked = GlobalVar.appSettings.Messages_Importer_AskOsu;
			CB_MessagesUpdaterOpenUpdater.IsChecked = GlobalVar.appSettings.Messages_Updater_OpenUpdater;
			CB_MessagesUpdaterUnableToCheckForUpdates.IsChecked = GlobalVar.appSettings.Messages_Updater_UnableToCheckForUpdates;
			CB_ToolCheckFileAssociation.IsChecked = GlobalVar.appSettings.Tool_CheckFileAssociation;
			CB_ToolRequestElevationOnStartup.IsChecked = GlobalVar.appSettings.Tool_RequestElevationOnStartup;
			CB_ToolSyncOnStartup.IsChecked = GlobalVar.appSettings.Tool_SyncOnStartup;
			CB_ToolUpdateDeleteFileAfter.IsChecked = GlobalVar.appSettings.Tool_Update_DeleteFileAfter;
			CB_ToolUpdate_UseDownloadPatcher.IsChecked = GlobalVar.appSettings.Tool_Update_UseDownloadPatcher;
			CB_ToolCheckForUpdates.SelectedIndex = GlobalVar.appSettings.Tool_CheckForUpdates;

            // Load mirrors and select current one
            var i = 0;
            foreach(KeyValuePair<string, MirrorManager.DownloadMirror> thisPair in MirrorManager.app_mirrors) {
                CB_ToolDownloadMirror.Items.Add(thisPair.Value);

                if(thisPair.Key == GlobalVar.appSettings.Tool_ChosenDownloadMirror)
                    CB_ToolDownloadMirror.SelectedIndex = i;
                i++;
            }

            CB_ToolEnableNotifyIcon.SelectedIndex = GlobalVar.appSettings.Tool_EnableNotifyIcon;

			// Load languages and select current one
			int j = 1;
			int indexUserLanguage = -1;
			List<string> AlreadyAdded = new List<string>();
			CB_ToolLanguages.Items.Add("en_US | English/English");      // 0
			foreach(var thisPair in TranslationManager.translationList.Values) {
				if(!AlreadyAdded.Contains(thisPair.Code)) {
					AlreadyAdded.Add(thisPair.Code);
					if(thisPair.Code == GlobalVar.appSettings.Tool_Language) {
                        indexUserLanguage = j;
					}
					CB_ToolLanguages.Items.Add(thisPair.Code + " | " + thisPair.DisplayName_en + "/" + thisPair.DisplayName);
					j++;
				}
			}
			if(indexUserLanguage != -1) {
				CB_ToolLanguages.SelectedIndex = indexUserLanguage;
			} else {
				CB_ToolLanguages.SelectedIndex = 0;
			}

			TB_ApiKey.Text = GlobalVar.appSettings.Api_Key;
			TB_osu_Path.Text = GlobalVar.appSettings.osu_Path;
			TB_osu_SongsPath.Text = GlobalVar.appSettings.osu_SongsPath;
			TB_ToolImporterAutoInstallCounter.Text = GlobalVar.appSettings.Tool_Importer_AutoInstallCounter.ToString();
			TB_ToolInterface_BeatmapDetailPanelWidth.Text = GlobalVar.appSettings.Tool_Interface_BeatmapDetailPanelWidth.ToString();
			TB_ToolUpdate_Path.Text = GlobalVar.appSettings.Tool_Update_SavePath;

			Ex_Language.Header = GlobalVar._e("WindowSettings_language") + " / Language";
		}
	}
}
