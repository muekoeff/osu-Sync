using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace osuSync {

    public partial class Window_Updater {

        private enum DownloadModes {
            Info = 0,
            DownloadPatcher = 1,
            DownloadUpdate = 2
        }

        private WebClient client = new WebClient();
		private DownloadModes downloadMode = DownloadModes.Info;
		private string update_downloadPatcherToPath = GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Updater" + Path.DirectorySeparatorChar + "UpdatePatcher.exe";
		private string update_downloadToPath;
		private string update_fileName;
		private string update_path;
        private string update_totalBytes;
        private string update_path_updatePatcher;
		private string update_version;

        public Window_Updater() {
            InitializeComponent();
        }

        public void Action_DownloadUpdate() {
			downloadMode = DownloadModes.DownloadUpdate;
			client.DownloadFileAsync(new Uri(update_path), update_downloadToPath + ".tmp");
		}

		public void Action_LoadUpdateInformation(JObject answer) {
			update_path_updatePatcher = Convert.ToString(answer.SelectToken("patcher").SelectToken("path"));

			foreach(JToken thisToken in answer.SelectToken("latestRepoRelease").SelectToken("assets")) {
				if(Convert.ToString(thisToken.SelectToken("name")).StartsWith("osu.Sync.") & Convert.ToString(thisToken.SelectToken("name")).EndsWith(".zip")) {
					update_fileName = Convert.ToString(thisToken.SelectToken("name"));
					update_path = Convert.ToString(thisToken.SelectToken("browser_download_url"));
				}
			}

			if(update_path != null) {
				Bu_Update.IsEnabled = true;
			} else {
				MessageBox.Show(GlobalVar._e("MainWindow_unableToGetUpdatePath"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

        #region "Bu - Button"
        public void Bu_Done_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		public void Bu_Update_Click(object sender, RoutedEventArgs e) {
			Cursor = Cursors.AppStarting;
			Bu_Done.IsEnabled = false;
			Bu_Update.IsEnabled = false;

			update_downloadToPath = GlobalVar.appSettings.Tool_Update_SavePath + Path.DirectorySeparatorChar + update_fileName;
			if(File.Exists(update_downloadToPath))
				File.Delete(update_downloadToPath);
			Directory.CreateDirectory(Path.GetDirectoryName(update_downloadToPath));
			if(GlobalVar.appSettings.Tool_Update_UseDownloadPatcher) {
				Directory.CreateDirectory(Path.GetDirectoryName(update_downloadPatcherToPath));
				if(!File.Exists(update_downloadPatcherToPath)) {
					if(File.Exists(update_downloadPatcherToPath + ".tmp"))
						File.Delete(update_downloadPatcherToPath + ".tmp");
					downloadMode = DownloadModes.DownloadPatcher;
					client.DownloadFileAsync(new Uri(update_path_updatePatcher), update_downloadPatcherToPath + ".tmp");
				} else {
					Action_DownloadUpdate();
				}
			} else {
				Action_DownloadUpdate();
			}
		}
        #endregion

        #region "Client"
        public void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) {
			switch(downloadMode) {
				case DownloadModes.DownloadPatcher:
					File.Move(update_downloadPatcherToPath + ".tmp", update_downloadPatcherToPath);
					Action_DownloadUpdate();
					break;
				case DownloadModes.DownloadUpdate:
					Cursor = Cursors.Arrow;
					TB_Status.Text = GlobalVar._e("WindowUpdater_downloadFinished").Replace("%0", update_totalBytes);
					Bu_Done.IsEnabled = true;
					File.Move(update_downloadToPath + ".tmp", update_downloadToPath);
					if(GlobalVar.appSettings.Tool_Update_UseDownloadPatcher) {
                        // Run UpdatePatcher
                        ProcessStartInfo UpdatePatcher = new ProcessStartInfo() {
                            Arguments = "-deletePackageAfter=\"" + GlobalVar.appSettings.Tool_Update_DeleteFileAfter.ToString() + "\""
                                + " -destinationVersion=\"" + update_version + "\""
                                + " -pathToApp=\"" + Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\""
                                + " -pathToUpdate=\"" + update_downloadToPath + "\""
                                + " -sourceVersion=\"" + GlobalVar.AppVersion.ToString() + "\"",
                            FileName = update_downloadPatcherToPath
                        };
                        Process.Start(UpdatePatcher);
						System.Windows.Application.Current.Shutdown();
						return;
					} else {
						if(MessageBox.Show(GlobalVar._e("WindowUpdater_doYouWantToOpenPathWhereUpdatedFilesHaveBeenSaved"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
							Process.Start(GlobalVar.appSettings.Tool_Update_SavePath);
					}
					break;
			}
		}

		public void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
			switch(downloadMode) {
				case DownloadModes.DownloadPatcher:
					update_totalBytes = Convert.ToString(e.TotalBytesToReceive);
					PB_Progress.Value = e.ProgressPercentage;
					TB_Status.Text = GlobalVar._e("WindowUpdater_downloadingInstaller").Replace("%0", e.BytesReceived.ToString()).Replace("%1", e.TotalBytesToReceive.ToString());
					break;
				case DownloadModes.DownloadUpdate:
					update_totalBytes = Convert.ToString(e.TotalBytesToReceive);
					PB_Progress.Value = e.ProgressPercentage;
					TB_Status.Text = GlobalVar._e("WindowUpdater_downloadingUpdatePackage").Replace("%0", e.BytesReceived.ToString()).Replace("%1", e.TotalBytesToReceive.ToString());
					break;
			}
		}

		public void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			switch(downloadMode) {
				case DownloadModes.Info:
					JObject answer = null;
					try {
						answer = JObject.Parse(e.Result);
					} catch(JsonReaderException) {
						MessageBox.Show(GlobalVar._e("MainWindow_unableToCheckForUpdates") + "\n"
                            + "> " + GlobalVar._e("MainWindow_invalidServerResponse") + "\n\n"
                            + GlobalVar._e("MainWindow_ifThisProblemPersistsPleaseLaveAFeedbackMessage"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
						TB_VersionInfo.Text += " | " + GlobalVar._e("WindowUpdater_unableToCommunicateWithServer");
						TB_Status.Text = GlobalVar._e("WindowUpdater_unableToCommunicateWithServer");
						PB_Progress.IsIndeterminate = false;
						return;
					} catch(System.Reflection.TargetInvocationException) {
						Clipboard.SetText("https: //osu.ppy.sh/forum/t/270446");
						MessageBox.Show(GlobalVar._e("MainWindow_unableToCheckForUpdates") + "\n"
                            + "> " + GlobalVar._e("MainWindow_cantConnectToServer") + "\n\n"
                            + GlobalVar._e("MainWindow_ifThisProblemPersistsVisitTheOsuForum"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
						TB_VersionInfo.Text += " | " + GlobalVar._e("WindowUpdater_unableToCommunicateWithServer");
						TB_Status.Text = GlobalVar._e("WindowUpdater_unableToCommunicateWithServer");
						PB_Progress.IsIndeterminate = false;
						return;
					}

					update_version = Convert.ToString(answer.SelectToken("latestRepoRelease").SelectToken("tag_name"));
					TB_VersionInfo.Text += " | " + GlobalVar._e("WindowUpdater_latestVersion").Replace("%0", update_version);

					Paragraph Paragraph = new Paragraph();
					FlowDocument FlowDocument = new FlowDocument();
					try {
                        Paragraph.Inlines.Add(new Run("# " + GlobalVar._e("WindowUpdater_dateOfPublication").Replace("%0", Convert.ToString(answer.SelectToken("latestRepoRelease").SelectToken("published_at")).Substring(0, 10))));
                        Paragraph.Inlines.Add(new LineBreak());
                        Paragraph.Inlines.Add(new LineBreak());
                        Paragraph.Inlines.Add(new Run(Convert.ToString(answer.SelectToken("latestRepoRelease").SelectToken("body")).Replace("```Indent", "").Replace("```", "")));
					} catch(Exception) {
						MessageBox.Show(GlobalVar._e("MainWindow_unableToCheckForUpdates") + "\n"
                            + "> " + GlobalVar._e("MainWindow_cantConnectToServer") + "\n\n"
                            + GlobalVar._e("MainWindow_ifThisProblemPersistsVisitTheOsuForum"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
						Close();
						return;
					}
					FlowDocument.Blocks.Add(Paragraph);
					RTB_Changelog.Document = FlowDocument;
					Cursor = Cursors.Arrow;
					PB_Progress.IsIndeterminate = false;

					if(update_version == GlobalVar.AppVersion.ToString()) {
						TB_Status.Text = GlobalVar._e("WindowUpdater_yourUsingTheLatestVersion");
#if DEBUG
						Console.WriteLine("[DEBUG] Enabled Download button");
						Action_LoadUpdateInformation(answer);
#endif
					} else {
						TB_Status.Text = GlobalVar._e("WindowUpdater_anUpdateIsAvailable");
						Action_LoadUpdateInformation(answer);
					}
					break;
			}
		}
        #endregion

        public void WindowUpdater_Loaded(object sender, RoutedEventArgs e) {
            // client
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadStringCompleted += Client_DownloadStringCompleted;

            if(GlobalVar.appSettings.Tool_Update_UseDownloadPatcher == false)
				Bu_Update.Content = GlobalVar._e("WindowUpdater_download");
#if DEBUG
			TB_VersionInfo.Text = GlobalVar._e("WindowUpdater_yourVersion").Replace("%0", GlobalVar.AppVersion.ToString() + " (Dev)");
#else
            TB_VersionInfo.Text = GlobalVar._e("WindowUpdater_yourVersion").Replace("%0", GlobalVar.AppVersion.ToString());
			#endif
			client.DownloadStringAsync(new Uri(GlobalVar.webNw520ApiRoot + "app/updater.latestVersion.json"));
		}
	}
}
