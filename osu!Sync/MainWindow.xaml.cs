using Hardcodet.Wpf.TaskbarNotification;
using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osuSync.Interfaces.UserControls;
using osuSync.Models;
using osuSync.Modules;
using osuSync.Modules.Importer;
using osuSync.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static osuSync.Modules.FileExtensions;
using static osuSync.Modules.TranslationManager;

namespace osuSync {

    public enum UpdateBmDisplayDestinations {
        Installed = 0,
        Importer = 1,
        Exporter = 2
    }

    public class BGWcallback_SyncGetIDs {
		public enum ArgModes {
			Sync = 0
		}

		public enum ProgressCurrentActions {
			Sync = 0,
			Done = 2,
			CountingTotalFolders = 4
		}

		public enum ReturnStatuses {
			Cancelled,
			Exception,
			FolderDoesNotExist,
			Success
		}

        public ArgModes Arg__Mode { get; set; }
		public List<string> Func_Invalid { get; set; }
		public List<string> Func_InvalidId { get; set; }
		public int Progress__Current { get; set; }
		public ProgressCurrentActions Progress__CurrentAction { get; set; }
        public ReturnStatuses Return__Status { get; set; } = ReturnStatuses.Success;
        public BeatmapDictionary Return__Sync_BmDic_Installed { get; set; }
		public string Return__Sync_Warnings { get; set; }
	}

    // Bm = Beatmap
    // BmDP = Beatmap Detail Panel

    public partial class MainWindow {
        private System.ComponentModel.BackgroundWorker BGW_syncGetIds = new System.ComponentModel.BackgroundWorker {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        private WebClient BmDP_client = new WebClient();
		private DoubleAnimation fadeOut = new DoubleAnimation();
		private BeatmapDictionary bmDic_installed = new BeatmapDictionary();
		private List<BeatmapItem_Exporter> exporter_bmList_selectedTags = new List<BeatmapItem_Exporter>();
		private List<BeatmapItem_Exporter> exporter_bmList_unselectedTags = new List<BeatmapItem_Exporter>();

        internal ImporterHolder importerHolder;
		private TextBlock interface_loaderText = new TextBlock();
		private ProgressBar interface_loaderProgressBar = new ProgressBar();

		private bool sync_isDone = false;
		private bool sync_isDone_importerRequest = false;
		private BeatmapDictionary sync_isDone_importerRequest_saveValue = new BeatmapDictionary();

        public MainWindow() {
            InitializeComponent();
            importerHolder = new ImporterHolder(this);
        }

		public bool BalloonShow(string content, string title = null, BalloonIcon icon = BalloonIcon.Info, RoutedEventHandler clickHandler = null) {
			if(GlobalVar.appSettings.Tool_EnableNotifyIcon == 0) {
                TI_Notify.Tag = clickHandler;
                if(clickHandler != null) {
                    TI_Notify.TrayBalloonTipClicked += clickHandler;
                }
                TI_Notify.ShowBalloonTip((title ?? GlobalVar.appName), content, icon);
				return true;
			} else {
				return false;
			}
		}

		public BeatmapDictionary ConvertSavedJsonToBmList(JObject source) {
            BeatmapDictionary bmList = new BeatmapDictionary();

			foreach(JToken thisToken in source.Values()) {
				if(!thisToken.Path.StartsWith("_")) {
					Beatmap thisBm = new Beatmap {
						Id = Convert.ToInt32(thisToken.SelectToken("id")),
						Title = Convert.ToString(thisToken.SelectToken("title")),
						Artist = Convert.ToString(thisToken.SelectToken("artist"))
					};
					if((thisToken.SelectToken("artist") != null))
                        thisBm.Creator = Convert.ToString(thisToken.SelectToken("creator"));
                    bmList.Add(thisBm.Id, thisBm);
				}
			}
			return bmList;
		}

		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern int ShowWindow(IntPtr handle, int nCmdShow);

		public void ApplySettings() {
            La_FooterWarn.Content = "";
            La_FooterWarn.ToolTip = "";

			// NotifyIcon
			switch(GlobalVar.appSettings.Tool_EnableNotifyIcon) {
				case 0:
				case 2:
					MI_AppToTray.Visibility = Visibility.Visible;
					TI_Notify.Visibility = Visibility.Visible;
					break;
				case 3:
					MI_AppToTray.Visibility = Visibility.Visible;
					TI_Notify.Visibility = Visibility.Collapsed;
					break;
				case 4:
					MI_AppToTray.Visibility = Visibility.Collapsed;
					TI_Notify.Visibility = Visibility.Collapsed;
					break;
			}

			// Check Write Access
			if(Directory.Exists(GlobalVar.appSettings.osu_SongsPath)) {
				GlobalVar.tool_hasWriteAccessToOsu = GlobalVar.DirAccessCheck(GlobalVar.appSettings.osu_SongsPath);
				if(GlobalVar.tool_hasWriteAccessToOsu == false) {
					if(GlobalVar.appSettings.Tool_RequestElevationOnStartup) {
						if(GlobalVar.RequestElevation()) {
							System.Windows.Application.Current.Shutdown();
							return;
						} else {
							MessageBox.Show(_e("MainWindow_elevationFailed"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
                    La_FooterWarn.Content = _e("MainWindow_noAccess");
                    La_FooterWarn.ToolTip = _e("MainWindow_tt_noAccess");
				}
			}
		}

        /// <summary>
        /// Updates the beatmap list interface.
        /// </summary>
        /// <param name="bmList">List of Beatmaps to display</param>
        /// <param name="destination">Selects the list where to display the new list. Possible values <code>Installed</code>, <code>Importer</code>, <code>Exporter</code></param>
        /// <param name="lastUpdateTime">Only required when <paramref name="destination"/> = Installed</param>
        /// <remarks></remarks>
        public void BmDisplayUpdate(BeatmapDictionary bmList, UpdateBmDisplayDestinations destination = UpdateBmDisplayDestinations.Installed, string lastUpdateTime = null) {
			switch(destination) {
				case UpdateBmDisplayDestinations.Installed:
                    La_FooterLastSync.Content = (lastUpdateTime == null ? _e("MainWindow_lastSync").Replace("%0", DateTime.Now.ToString(_e("MainWindow_dateFormat") + " " + _e("MainWindow_timeFormat"))) : _e("MainWindow_lastSync").Replace("%0", lastUpdateTime));
                    La_FooterLastSync.Tag = (lastUpdateTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"));
					BeatmapWrapper.Children.Clear();

                    foreach(KeyValuePair<int, Beatmap> thisBm in bmList) {
                        var bmItem = new BeatmapItem_Installed(thisBm.Value, this);
                        BeatmapWrapper.Children.Add(bmItem);
                    }

                    if(bmList.Count == 0) {
						TextBlock UI_TextBlock = new TextBlock {
							FontSize = 72,
							Foreground = (SolidColorBrush)FindResource("GreenLightBrush"),

                            HorizontalAlignment = HorizontalAlignment.Center,
							Margin = new Thickness(0, 86, 0, 0),
							Text = _e("MainWindow_beatmapsFound").Replace("%0", "0"),
							VerticalAlignment = VerticalAlignment.Center
						};
						TextBlock UI_TextBlock_SubTitle = new TextBlock {
							FontSize = 24,
							Foreground = (SolidColorBrush)FindResource("GreenLightBrush"),

                            HorizontalAlignment = HorizontalAlignment.Center,
							Text = _e("MainWindow_thatsImpressiveIGuess"),
							VerticalAlignment = VerticalAlignment.Center
						};
                        BeatmapWrapper.Children.Add(UI_TextBlock);
                        BeatmapWrapper.Children.Add(UI_TextBlock_SubTitle);
					}
					TB_BmCounter.Text = _e("MainWindow_beatmapsFound").Replace("%0", bmList.Count.ToString());
					Bu_SyncRun.IsEnabled = true;
					break;
				case UpdateBmDisplayDestinations.Importer:
                    importerHolder = new ImporterHolder(this) {
                        BmTotal = 0
                    };
                    TI_Importer.Visibility = Visibility.Visible;
					TC_Main.SelectedIndex = 1;
					SP_ImporterWrapper.Children.Clear();
					CB_ImporterHideInstalled.IsChecked = false;
					Bu_ImporterCancel.IsEnabled = false;
					Bu_ImporterRun.IsEnabled = false;
					if(sync_isDone == false) {
						sync_isDone_importerRequest = true;
						Bu_SyncRun.IsEnabled = false;
						var UI_ProgressRing = new MahApps.Metro.Controls.ProgressRing {
							Height = 150,
							HorizontalAlignment = HorizontalAlignment.Center,
							IsActive = true,
							Margin = new Thickness(0, 100, 0, 0),
							VerticalAlignment = VerticalAlignment.Center,
							Width = 150
						};
						TextBlock UI_TextBlock_SubTitle = new TextBlock {
							FontSize = 24,
							Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
							HorizontalAlignment = HorizontalAlignment.Center,
							Text = _e("MainWindow_pleaseWait") + "\n" + 
                                _e("MainWindow_syncing"),
							TextAlignment = TextAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
						};
						interface_loaderText = UI_TextBlock_SubTitle;
						SP_ImporterWrapper.Children.Add(UI_ProgressRing);
						SP_ImporterWrapper.Children.Add(UI_TextBlock_SubTitle);
						sync_isDone_importerRequest_saveValue = bmList;
						Sync_GetIDs();
						return;
					}

                    Bu_ImporterCancel.IsEnabled = true;
                    foreach(KeyValuePair<int, Beatmap> thisBm in bmList) {
                        var isInstalled = bmDic_installed.ContainsKey(thisBm.Value.Id);
                        var bmItem = new BeatmapItem_Importer(thisBm.Value, this, isInstalled);

						if(!isInstalled) {
                            importerHolder.BmList_TagsToInstall.Add(bmItem);
                        }

						SP_ImporterWrapper.Children.Add(bmItem);
						importerHolder.BmTotal += 1;
					}

					Bu_ImporterCancel.IsEnabled = true;
					TB_ImporterInfo.ToolTip = TB_ImporterInfo.Text;
					Bu_ImporterRun.IsEnabled = (importerHolder.BmList_TagsToInstall.Count != 0);
                    importerHolder.SetState();
					TB_ImporterMirror.Text = _e("MainWindow_downloadMirror") + ": " + MirrorManager.app_mirrors[GlobalVar.appSettings.Tool_ChosenDownloadMirror].DisplayName;
					break;
				case UpdateBmDisplayDestinations.Exporter:
					SP_ExporterWrapper.Children.Clear();
					foreach(KeyValuePair<int, Beatmap> thisBm in bmList) {
                        var bmItem = new BeatmapItem_Exporter(thisBm.Value, this);

						exporter_bmList_selectedTags.Add(bmItem);
						SP_ExporterWrapper.Children.Add(bmItem);
					}

					TI_Exporter.Visibility = Visibility.Visible;
					TC_Main.SelectedIndex = 2;
					break;
			}
		}

		public void BmDP_Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			BeatmapDetails_APIProgress.Visibility = Visibility.Collapsed;

			if(e.Cancelled) {
				UI_SetStatus(_e("MainWindow_aborted"));
				GlobalVar.WriteToApiLog("/api/get_beatmaps", "{Cancelled}");
			} else {
				JArray JSON_Array = null;
				try {
					JSON_Array = (JArray)JsonConvert.DeserializeObject(e.Result);
					if(JSON_Array.First != null) {
						System.Globalization.CultureInfo CI = null;
						try {
							CI = new System.Globalization.CultureInfo(GlobalVar.appSettings.Tool_Language.Replace("_", "-"));
						} catch(System.Globalization.CultureNotFoundException) {
							CI = new System.Globalization.CultureInfo("en-US");
						}
						GlobalVar.WriteToApiLog("/api/get_beatmaps", e.Result);
						BeatmapDetails_APIFavouriteCount.Text = Convert.ToInt32(JSON_Array.First.SelectToken("favourite_count")).ToString("n", CI).Substring(0, Convert.ToInt32(JSON_Array.First.SelectToken("favourite_count")).ToString("n", CI).Length - 3);

						int[] passCount = new int[JSON_Array.Count + 1];
						int[] playCount = new int[JSON_Array.Count + 1];
						StringBuilder sb_passCount = new StringBuilder();
                        StringBuilder sb_playCount = new StringBuilder();
                        int i = 0;
						foreach(JObject a in JSON_Array.Children()) {
							int CurrentPassCount = Convert.ToInt32(a.SelectToken("passcount"));
							int CurrentPlayCount = Convert.ToInt32(a.SelectToken("playcount"));
							passCount[i] = CurrentPassCount;
							playCount[i] = CurrentPlayCount;
							if(i == 0) {
                                sb_passCount.Append(a.SelectToken("version").ToString() + ":\t" + CurrentPassCount.ToString("n", CI).Substring(0, CurrentPassCount.ToString("n", CI).Length - 3));
                                sb_playCount.Append(a.SelectToken("version").ToString() + ":\t" + CurrentPlayCount.ToString("n", CI).Substring(0, CurrentPlayCount.ToString("n", CI).Length - 3));
							} else {
                                sb_passCount.Append("\n" + a.SelectToken("version").ToString() + ":\t" + CurrentPassCount.ToString("n", CI).Substring(0, CurrentPassCount.ToString("n", CI).Length - 3));
                                sb_playCount.Append("\n" + a.SelectToken("version").ToString() + ":\t" + CurrentPlayCount.ToString("n", CI).Substring(0, CurrentPlayCount.ToString("n", CI).Length - 3));
							}
							i++;
						}

                        BeatmapDetails_APIPassCount.Text = Math.Round((double)(passCount.Sum() / passCount.Count()), 2).ToString("n", CI);
                        BeatmapDetails_APIPassCount.ToolTip = sb_passCount.ToString();

                        BeatmapDetails_APIPlayCount.Text = Math.Round((double)(playCount.Sum() / playCount.Count()), 2).ToString("n", CI);
                        BeatmapDetails_APIPlayCount.ToolTip = sb_playCount.ToString();

                        BmDP_SetRankedStatus((Beatmap.OnlineApprovedStatuses)Enum.Parse(typeof(Beatmap.OnlineApprovedStatuses), JSON_Array.First.SelectToken("approved").ToString(), true));
						UI_SetStatus(_e("MainWindow_finished"));
					} else {
						UI_SetStatus(_e("MainWindow_failed"));
                        GlobalVar.WriteToApiLog("/api/get_beatmaps", "{UnexpectedAnswer} " + e.Result);
                        BeatmapDetails_APIWarn.Content = _e("MainWindow_detailsPanel_apiError");
                        BeatmapDetails_APIWarn.Visibility = Visibility.Visible;
					}
				} catch(Exception) {
					UI_SetStatus(_e("MainWindow_failed"));
                    GlobalVar.WriteToApiLog("/api/get_beatmaps");
                    BeatmapDetails_APIWarn.Content = _e("MainWindow_detailsPanel_apiError");
                    BeatmapDetails_APIWarn.Visibility = Visibility.Visible;
				}
			}
		}

		public void BmDP_SetRankedStatus(Beatmap.OnlineApprovedStatuses value) {
			switch(value) {
				case Beatmap.OnlineApprovedStatuses.Ranked:
				case Beatmap.OnlineApprovedStatuses.Approved:
                    BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("GreenDarkBrush");
                    BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_ranked");
					break;
				case Beatmap.OnlineApprovedStatuses.Pending:
                    BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("PurpleDarkBrush");
                    BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_pending");
					break;
				default:
                    BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("GrayLightBrush");
                    BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_unranked");
					break;
			}
		}

		public void BmDP_Show(object sender, MouseButtonEventArgs e) {
			Beatmap cSender_Bm = null;
			if(sender is Image) {
                IBeatmapItem cParent = (IBeatmapItem)((Grid)((Image)sender).Parent).Parent;
				cSender_Bm = cParent.Beatmap;
			} else {
				return;
			}
			UI_ShowBmDP(cSender_Bm);
		}

		#region "Bu - Button"
		public void Bu_BmDetailsListing_Click(object sender, RoutedEventArgs e) {
			Button cSender = (Button)sender;
			string cSender_Tag = Convert.ToString(cSender.Tag);
			Process.Start("https://osu.ppy.sh/s/" + cSender_Tag);
		}

		public void Bu_SyncRun_Click(object sender, RoutedEventArgs e) {
			if(GlobalVar.tool_isElevated && GlobalVar.appSettings.Tool_CheckFileAssociation)
				FileAssociationCheck();
			Sync_GetIDs();
		}
		#endregion

		public void Exporter_ExportBmDialog(BeatmapDictionary source, string dialogTitle = null) {
            SaveFileDialog Dialog_SaveFile = new SaveFileDialog() {
                AddExtension = true,
                Filter = _e("MainWindow_fileext_osblx") + "     (*.nw520-osblx)|*.nw520-osblx|"
                    + _e("MainWindow_fileext_osbl") + "     (*.nw520-osbl)|*.nw520-osbl|"
                    + _e("MainWindow_fileext_zip") + "     (*.zip)|*.osblz.zip|"
                    + _e("MainWindow_fileext_html") + "     [" + _e("MainWindow_notImportable") + "] (*.html)|*.html|"
                    + _e("MainWindow_fileext_txt") + "     [" + _e("MainWindow_notImportable") + "] (*.txt)|*.txt|"
                    + _e("MainWindow_fileext_json") + "     (*.json)|*.json|"
                    + _e("MainWindow_fileext_csv") + "     [" + _e("MainWindow_notImportable") + "] (*.csv)|*.csv",
                OverwritePrompt = true,
                Title = (dialogTitle ?? _e("MainWindow_exportInstalledBeatmaps1")),
                ValidateNames = true
            };
            Dialog_SaveFile.ShowDialog();
			if(string.IsNullOrEmpty(Dialog_SaveFile.FileName)) {
				OverlayShow(_e("MainWindow_exportAborted"), null);
				OverlayFadeOut();
				return;
			}

			switch(Dialog_SaveFile.FilterIndex) {
				case 1: //.nw520-osblx
					string content_osblx = source.ConvertToJson(out string failMessageOsblx);
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(GlobalVar.StringCompress(content_osblx));
						File.Close();
					}

					if(!string.IsNullOrEmpty(failMessageOsblx)) {
						if(MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") + "\n" +
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
							Window_Message.SetMessage(failMessageOsblx, _e("MainWindow_skippedBeatmaps"), "Export");
							Window_Message.ShowDialog();
						}
					}
					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBLX"));
					OverlayFadeOut();
					break;
				case 2:
					//.nw520-osbl
					string content_osbl = source.ConvertToJson(out string failMessageOsbl);
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(content_osbl);
						File.Close();
					}

					if(!string.IsNullOrEmpty(failMessageOsbl)) {
						if(MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") + "\n" +
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
							Window_Message.SetMessage(failMessageOsbl, _e("MainWindow_skippedBeatmaps"), "Export");
							Window_Message.ShowDialog();
						}
					}
					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "OSBL"));
					OverlayFadeOut();
					break;
				case 3:
					//.osblz.zip
					string directName = GlobalVar.appTempPath + "/Zipper/Exporter-".Replace('/', Path.DirectorySeparatorChar) + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
					Directory.CreateDirectory(directName);

					string contenz_osblz = source.ConvertToJson(out string failMessageOsblz);
					using(StreamWriter File = new StreamWriter(directName + "/00.nw520-osblx".Replace('/', Path.DirectorySeparatorChar), false)) {
						File.Write(GlobalVar.StringCompress(contenz_osblz));
						File.Close();
					}

					PackageDirectoryAsZIP(directName, Dialog_SaveFile.FileName);
					Directory.Delete(directName, true);
					if(!string.IsNullOrEmpty(failMessageOsblz)) {
						if(MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") + "\n" +
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
							Window_Message.SetMessage(failMessageOsblz, _e("MainWindow_skippedBeatmaps"), "Export");
							Window_Message.ShowDialog();
						}
					}
					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "Zipped OSBLX"));
					OverlayFadeOut();
					break;
				case 4:
					//.html
					string content_html = source.ConvertToHtml(out string failMessageHtml);
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(content_html);
						File.Close();
					}

					if(!string.IsNullOrEmpty(failMessageHtml)) {
                        failMessageHtml = failMessageHtml.Insert(0, "# " + _e("MainWindow_unsubmittedBeatmapSets") + "\n" 
                            + _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") + "\n\n" 
                            + "> " + _e("MainWindow_beatmaps") + ": ");
						if(MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") + "\n" +
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
							Window_Message.SetMessage(failMessageHtml, _e("MainWindow_skippedBeatmaps"), "Export");
							Window_Message.ShowDialog();
						}
					}
					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "HTML"));
					OverlayFadeOut();
					break;
				case 5:
					//.txt
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(source.ConvertToTxt());
						File.Close();
					}

					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "TXT"));
					OverlayFadeOut();
					break;
				case 6:
					//.json
					string content_json = source.ConvertToJson(out string failMessageJson);
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(content_json);
						File.Close();
					}

					if(!string.IsNullOrEmpty(failMessageJson)) {
						if(MessageBox.Show(_e("MainWindow_someBeatmapSetsHadntBeenExported") + "\n" +
                            _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
							Window_Message.SetMessage(failMessageJson, _e("MainWindow_skippedBeatmaps"), "Export");
							Window_Message.ShowDialog();
						}
					}
					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "JSON"));
					OverlayFadeOut();
					break;
				case 7:
					//.csv
					using(StreamWriter File = new StreamWriter(Dialog_SaveFile.FileName, false)) {
						File.Write(source.ConvertToCsv());
						File.Close();
					}

					OverlayShow(_e("MainWindow_exportCompleted"), _e("MainWindow_exportedAs").Replace("%0", "CSV"));
					OverlayFadeOut();
					break;
			}
		}

		public void FadeOut_Completed(object sender, EventArgs e) {
			Gr_Overlay.Visibility = Visibility.Hidden;
		}

		public void Flyout_BmDetails_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e) {
			Flyout_BmDetails.Width = GlobalVar.appSettings.Tool_Interface_BeatmapDetailPanelWidth * (ActualWidth / 100);
		}

		#region "La - Label"
		public void La_FooterVer_MouseDown(object sender, MouseButtonEventArgs e) {
			Window_About Window_About = new Window_About();
			Window_About.ShowDialog();
		}
		#endregion

		
		public void MainWindow_Loaded(object sender, RoutedEventArgs e) {
#if DEBUG
			La_FooterVer.Content = "osu!Sync Version " + GlobalVar.AppVersion.ToString() + " (Dev)";
#else
            La_FooterVer.Content = "osu!Sync Version " + GlobalVar.AppVersion.ToString();
#endif

            // BGW_syncGetIds
            BGW_syncGetIds.DoWork += BGW_syncGetIds_DoWork;
            BGW_syncGetIds.ProgressChanged += BGW_syncGetIds_ProgressChanged;
            BGW_syncGetIds.RunWorkerCompleted += BGW_syncGetIds_RunWorkerCompleted;
            // BmDP_client
            BmDP_client.DownloadStringCompleted += BmDP_Client_DownloadStringCompleted;
            // fadeOut
            fadeOut.Completed += FadeOut_Completed;

            // Load Configuration
            if(File.Exists(GlobalVar.appDataPath + "/Settings/Settings.json".Replace('/', Path.DirectorySeparatorChar))) {
				GlobalVar.appSettings.LoadSettings();
			} else {
				Window_Welcome Window_Welcome = new Window_Welcome();
				Window_Welcome.ShowDialog();
				GlobalVar.appSettings.SaveSettings();
			}

			// Apply settings (like NotifyIcon)
			ApplySettings();

			// Delete old downloaded beatmaps
			if(Directory.Exists(GlobalVar.appTempPath + "/Downloads/Beatmaps".Replace('/', Path.DirectorySeparatorChar)))
				Directory.Delete(GlobalVar.appTempPath + "/Downloads/Beatmaps".Replace('/', Path.DirectorySeparatorChar), true);

			// Open File
			if(GlobalVar.appStartArgs != null && Array.Exists(GlobalVar.appStartArgs, s => {
				if(s.Substring(0, 10) == "-openFile=") {
                    importerHolder = new ImporterHolder(this) {
                        FilePath = s.Substring(10)
                    };
                    return true;
				} else {
					return false;
				}
			})) {
				if(File.Exists(importerHolder.FilePath)) {
					Importer_ReadListFile(importerHolder.FilePath);
				} else {
					MessageBox.Show(_e("MainWindow_file404"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
					if(GlobalVar.appSettings.Tool_SyncOnStartup)
						Sync_GetIDs();
				}
			} else if(GlobalVar.appSettings.Tool_SyncOnStartup) {
                Sync_GetIDs();
			}
        }

		#region "MI - MenuItem"
		public void MI_AppExit_Click(object sender, RoutedEventArgs e) {
			System.Windows.Application.Current.Shutdown();
		}

		public void MI_AppOsu_Click(object sender, RoutedEventArgs e) {
			StartOrFocusOsu();
		}

		public void MI_AppSettings_Click(object sender, RoutedEventArgs e) {
			UI_ShowSettingsWindow();
			if(!GlobalVar.tool_dontApplySettings)
				ApplySettings();
			else
                GlobalVar.tool_dontApplySettings = false;
		}

		public void MI_AppToTray_Click(object sender, RoutedEventArgs e) {
			ToggleMinimizeToTray();
		}

		public void MI_FileConvert_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog Dialog_OpenFile = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = _e("MainWindow_allSupportedFileFormats") + "|*.json;*.nw520-osbl;*.nw520-osblx|"
                    + _e("MainWindow_fileext_osblx") + "|*.nw520-osblx|"
                    + _e("MainWindow_fileext_osbl") + "|*.nw520-osbl|"
                    + _e("MainWindow_fileext_json") + "|*.json",
                Title = _e("MainWindow_selectASupportedFileToConvert")
            };
            Dialog_OpenFile.ShowDialog();

            SaveFileDialog Dialog_SaveFile = new SaveFileDialog() {
                AddExtension = true,
                OverwritePrompt = true,
                Title = _e("MainWindow_selectADestination"),
                ValidateNames = true
            };

			string content_osbl = null;
			if(!string.IsNullOrEmpty(Dialog_OpenFile.FileName)) {
                content_osbl = File.ReadAllText(Dialog_OpenFile.FileName);
				switch(Path.GetExtension(Dialog_OpenFile.FileName)) {
					case ".nw520-osbl":
						Exporter_ExportBmDialog(ConvertSavedJsonToBmList(JObject.Parse(content_osbl)), _e("MainWindow_convertSelectedFile"));
						break;
					case ".nw520-osblx":
						try {
                            content_osbl = GlobalVar.StringDecompress(content_osbl);
						} catch(FormatException) {
							OverlayShow(_e("MainWindow_conversionFailed"), "System.FormatException");
							OverlayFadeOut();
							return;
						} catch(InvalidDataException) {
							OverlayShow(_e("MainWindow_conversionFailed"), "System.IO.InvalidDataException");
							OverlayFadeOut();
							return;
						}
						Exporter_ExportBmDialog(ConvertSavedJsonToBmList(JObject.Parse(content_osbl)), _e("MainWindow_convertSelectedFile"));
						break;
				}
			} else {
				OverlayShow(_e("MainWindow_conversionAborted"));
				OverlayFadeOut();
				return;
			}
		}

		public void MI_FileExportAll_Click(object sender, RoutedEventArgs e) {
			if(sync_isDone == false) {
				MessageBox.Show(_e("MainWindow_youNeedToSyncFirst"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			Exporter_ExportBmDialog(bmDic_installed);
		}

		public void MI_FileExportSelected_Click(object sender, RoutedEventArgs e) {
			if(sync_isDone == false) {
                MessageBox.Show(_e("MainWindow_youNeedToSyncFirst"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
			}
			BmDisplayUpdate(bmDic_installed, UpdateBmDisplayDestinations.Exporter);
		}

		public void MI_FileOpenBmList_Click(object sender, RoutedEventArgs e) {
			if(sync_isDone == false) {
				MessageBox.Show(_e("MainWindow_youNeedToSyncFirst"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

            OpenFileDialog Dialog_OpenFile = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = _e("MainWindow_allSupportedFileFormats") + "|*.json;*.nw520-osbl;*.nw520-osblx;*.zip|" +
                    _e("MainWindow_fileext_osblx") + "|*.nw520-osblx|" +
                    _e("MainWindow_fileext_osbl") + "|*.nw520-osbl|" +
                    _e("MainWindow_fileext_zip") + "|*.zip|" +
                    _e("MainWindow_fileext_json") + "|*.json",
                Title = _e("MainWindow_openBeatmapList")
            };
            Dialog_OpenFile.ShowDialog();

			if(!string.IsNullOrEmpty(Dialog_OpenFile.FileName)) {
				Importer_ReadListFile(Dialog_OpenFile.FileName);
			} else {
				OverlayShow(_e("MainWindow_importAborted"));
				OverlayFadeOut();
			}
		}

		public void MI_About_Click(object sender, RoutedEventArgs e) {
			Window_About Window_About = new Window_About();
			Window_About.ShowDialog();
		}

		public void MI_NotifyAppShowHide_Click(object sender, RoutedEventArgs e) {
			ToggleMinimizeToTray();
		}

		public void MI_NotifyExit_Click(object sender, RoutedEventArgs e) {
			System.Windows.Application.Current.Shutdown();
		}

		public void MI_NotifyOsu_Click(object sender, RoutedEventArgs e) {
			StartOrFocusOsu();
		}
		#endregion

		public void OpenBmListing(object sender, MouseButtonEventArgs e) {
			Grid cParent = (Grid)((Image)sender).Parent;    // Get Tag from parent grid
			Beatmap cSender_tag = (Beatmap)cParent.Tag;
			Process.Start("https://osu.ppy.sh/s/" + cSender_tag.Id);
		}

		public void OverlayFadeOut() {
			Visibility = Visibility.Visible;
			Gr_Overlay.Visibility = Visibility.Visible;
            fadeOut.From = 1;
            fadeOut.To = 0;
            fadeOut.Duration = new Duration(TimeSpan.FromSeconds(1));
			Storyboard.SetTargetName(fadeOut, "Gr_Overlay");
			Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));

			Storyboard thisStoryboard = new Storyboard();
            thisStoryboard.Children.Add(fadeOut);
            thisStoryboard.Begin(this);
		}

		public void OverlayShow(string title = null, string caption = null) {
			TB_OverlayCaption.Text = (caption ?? "");
			TB_OverlayTitle.Text = (title ?? "");
            Gr_Overlay.Opacity = 1;
            Gr_Overlay.Visibility = Visibility.Visible;
		}

		public void PackageDirectoryAsZIP(string inputDir, string outputPath) {
			using(ZipFile zipper = new ZipFile()) {
                zipper.AddDirectory(inputDir);
                zipper.Save(outputPath);
			}
		}

		/// <summary>
		/// Determines whether to start or (when it's running) to focus osu!.
		/// </summary>
		/// <remarks></remarks>
		public void StartOrFocusOsu() {
			if(Process.GetProcessesByName("osu!").Count() <= 0) {
				if(File.Exists(GlobalVar.appSettings.osu_Path + "/osu!.exe".Replace('/', Path.DirectorySeparatorChar)))
					Process.Start(GlobalVar.appSettings.osu_Path + "/osu!.exe".Replace('/', Path.DirectorySeparatorChar));
				else
					MessageBox.Show(_e("MainWindow_unableToFindOsuExe"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
			} else {
				foreach(Process ObjProcess in Process.GetProcessesByName("osu!")) {
                    Microsoft.VisualBasic.Interaction.AppActivate(ObjProcess.Id);   // @TODO: Find alternative
					ShowWindow(ObjProcess.MainWindowHandle, 1);
				}
			}
		}

		/// <summary>
		/// Determines wether to load cache or to sync and will start the progress.
		/// </summary>
		/// <remarks></remarks>
		public void Sync_GetIDs() {
			Bu_SyncRun.IsEnabled = false;
			UI_SetLoader(_e("MainWindow_parsingInstalledBeatmapSets"));
			La_FooterLastSync.Content = _e("MainWindow_syncing");
			BGW_syncGetIds.RunWorkerAsync(new BGWcallback_SyncGetIDs());
		}

		#region "TI - TaskbarIcon"
		public void TI_Notify_TrayBalloonTipClicked(object sender, RoutedEventArgs e) {
            if(TI_Notify.Tag != null && TI_Notify.Tag is RoutedEventHandler) {
                TI_Notify.TrayBalloonTipClicked -= (RoutedEventHandler)TI_Notify.Tag;
            }
		}

		public void TI_Notify_TrayMouseDoubleClick(object sender, RoutedEventArgs e) {
			ToggleMinimizeToTray();
		}
		#endregion

		public void ToggleMinimizeToTray() {
			if(Visibility == Visibility.Visible) {
				switch(GlobalVar.appSettings.Tool_EnableNotifyIcon) {
					case 0:
					case 2:
					case 3:
						Visibility = Visibility.Hidden;
						TI_Notify.Visibility = Visibility.Visible;
						break;
					default:
						MI_AppToTray.IsEnabled = false;
						break;
				}
			} else {
				Visibility = Visibility.Visible;
				switch(GlobalVar.appSettings.Tool_EnableNotifyIcon) {
					case 3:
					case 4:
						TI_Notify.Visibility = Visibility.Collapsed;
						break;
				}
			}
		}

		#region "UI"
		public void UI_SetLoader(string message = null) {
            if(message == null)
                message = _e("MainWindow_pleaseWait");

			var UI_ProgressBar = new ProgressBar {
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Visibility = Visibility.Hidden,
				Height = 25
			};
			var UI_ProgressRing = new MahApps.Metro.Controls.ProgressRing {
				Height = 150,
				HorizontalAlignment = HorizontalAlignment.Center,
				IsActive = true,
				Margin = new Thickness(0, 100, 0, 0),
				VerticalAlignment = VerticalAlignment.Center,
				Width = 150
			};
			TextBlock UI_TextBlock_SubTitle = new TextBlock {
				FontSize = 24,
				Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
				HorizontalAlignment = HorizontalAlignment.Center,
				Text = message,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			interface_loaderText = UI_TextBlock_SubTitle;
			interface_loaderProgressBar = UI_ProgressBar;

            BeatmapWrapper.Children.Clear();
            BeatmapWrapper.Children.Add(UI_ProgressBar);
            BeatmapWrapper.Children.Add(UI_ProgressRing);
            BeatmapWrapper.Children.Add(UI_TextBlock_SubTitle);
		}

		public void UI_SetStatus(string message = null, bool doRecord = false) {
            if(message == null)
                message = "";

            if(doRecord) {      // Keep previous statuses as tooltip
                List<string> cTag = (La_FooterProg.Tag == null ? new List<string>() : (List<string>)La_FooterProg.Tag);
				cTag.Add(DateTime.Now.ToString(_e("MainWindow_timeFormat")) + " | " + message);
				if(cTag.Count > 10)
					cTag.RemoveRange(0, cTag.Count - 10);

                StringBuilder sb = new StringBuilder();
				foreach(string item in cTag) {
					sb.Append(item + "\n");
				}
				La_FooterProg.Tag = cTag;
				La_FooterProg.ToolTip = sb.ToString(0, sb.Length - "\n".Length);
				// Remove last "\n"
			}
			La_FooterProg.Content = message;
		}

		public void UI_ShowBmDP(Beatmap bm) {
			BeatmapDetails_Artist.Text = bm.Artist;
			Bu_BmDetailsListing.Tag = bm.Id;
			BeatmapDetails_Creator.Text = bm.Creator;
			BeatmapDetails_Title.Text = bm.Title;

			// IsUnplayed status
            BeatmapDetails_IsUnplayed.Background = (bm.IsUnplayed ? (SolidColorBrush)FindResource("RedLightBrush") : (SolidColorBrush)FindResource("GreenDarkBrush"));
            BeatmapDetails_IsUnplayed.Text = (bm.IsUnplayed ? _e("MainWindow_detailsPanel_playedStatus_unplayed") : _e("MainWindow_detailsPanel_playedStatus_played"));

            // Ranked status
            if(bm.RankedStatus == Convert.ToByte(4)
                || bm.RankedStatus == Convert.ToByte(5)) {
                BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("GreenDarkBrush");
                BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_ranked");
            } else if(bm.RankedStatus == Convert.ToByte(6)) {
                BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("PurpleDarkBrush");
                BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_pending");
            } else {
                BeatmapDetails_RankedStatus.Background = (SolidColorBrush)FindResource("GrayLightBrush");
                BeatmapDetails_RankedStatus.Text = _e("MainWindow_detailsPanel_beatmapStatus_unranked");
            }

			// Thumbnail
			if(!string.IsNullOrEmpty(bm.ThumbnailPath)) {
				try {
					BeatmapDetails_Thumbnail.Source = new BitmapImage(new Uri(bm.ThumbnailPath));
				} catch(Exception) { // IOException, NotSupportedException
                    BeatmapDetails_Thumbnail.Source = new BitmapImage(new Uri("Resources/NoThumbnail.png", UriKind.Relative));
				}
			} else {
				BeatmapDetails_Thumbnail.Source = new BitmapImage(new Uri("Resources/NoThumbnail.png", UriKind.Relative));
			}

			// Api
			if(GlobalVar.appSettings.Api_Enabled_BeatmapPanel & bm.Id != -1) {
				if(BmDP_client.IsBusy)
					BmDP_client.CancelAsync();
				// Reset
				BeatmapDetails_APIFavouriteCount.Text = "...";
				BeatmapDetails_APIFunctions.Visibility = Visibility.Visible;
				BeatmapDetails_APIPassCount.Text = "...";
				BeatmapDetails_APIPlayCount.Text = "...";
				BeatmapDetails_APIProgress.Visibility = Visibility.Visible;
				BeatmapDetails_RankedStatus.Text = "...";
				BeatmapDetails_APIWarn.Visibility = Visibility.Collapsed;

				try {
					UI_SetStatus(_e("MainWindow_fetching").Replace("%0", Convert.ToString(bm.Id)), true);
					BmDP_client.DownloadStringAsync(new Uri(GlobalVar.webOsuApiRoot + "get_beatmaps?k=" + GlobalVar.appSettings.Api_Key + "&s=" + bm.Id));
				} catch(NotSupportedException) {
                    BeatmapDetails_APIWarn.Content = _e("MainWindow_detailsPanel_generalError");
                    BeatmapDetails_APIWarn.Visibility = Visibility.Visible;
				}
			} else {
				BeatmapDetails_APIFunctions.Visibility = Visibility.Collapsed;
			}

			Flyout_BmDetails.IsOpen = true;
		}

		public static void UI_ShowSettingsWindow(int selectedIndex = 0) {
			Window_Settings Window_Settings = new Window_Settings();
			Window_Settings.TC_Main.SelectedIndex = selectedIndex;
			Window_Settings.ShowDialog();
		}
        #endregion

		#region "BGW_syncGetIds"
		public void BGW_syncGetIds_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e) {
			BGWcallback_SyncGetIDs arguments = e.Argument as BGWcallback_SyncGetIDs;
			BGWcallback_SyncGetIDs answer = new BGWcallback_SyncGetIDs();

			if(!Directory.Exists(GlobalVar.appSettings.osu_SongsPath)) {
				answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist;
				e.Result = answer;
				return;
			}

			switch(arguments.Arg__Mode) {
				case BGWcallback_SyncGetIDs.ArgModes.Sync:
					BGW_syncGetIds.ReportProgress(0, new BGWcallback_SyncGetIDs {
						Progress__CurrentAction = BGWcallback_SyncGetIDs.ProgressCurrentActions.CountingTotalFolders,
						Progress__Current = Directory.GetDirectories(GlobalVar.appSettings.osu_SongsPath).Count()
					});

					if(File.Exists(GlobalVar.appSettings.osu_Path + "/osu!.db".Replace('/', Path.DirectorySeparatorChar))) {
						string dbPath = GlobalVar.appSettings.osu_Path + "/osu!.db".Replace('/', Path.DirectorySeparatorChar);
						try {
							answer.Return__Sync_BmDic_Installed = ReadBmsFromDb(dbPath);
						} catch(IOException) {
							try {
								answer.Return__Sync_BmDic_Installed = ReadBmsFromDb(dbPath, true);
							} catch(Exception) {
								if(MessageBox.Show(_e("MainWindow_unableToReadBms") + " " + _e("MainWindow_fallback"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) == MessageBoxResult.Yes) {
									answer = ReadBmsFromDir(GlobalVar.appSettings.osu_SongsPath);
								} else {
									answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled;
								}
							}
						} catch(Exception) {
							if(MessageBox.Show(_e("MainWindow_unableToReadBms") + " " + _e("MainWindow_fallback"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) == MessageBoxResult.Yes) {
								answer = ReadBmsFromDir(GlobalVar.appSettings.osu_SongsPath);
							} else {
								answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled;
							}
						}
					} else {
						if(Directory.Exists(GlobalVar.appSettings.osu_SongsPath)) {
							if(MessageBox.Show(_e("MainWindow_unableToReadBms") + " " + _e("MainWindow_fallback"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.No) == MessageBoxResult.Yes) {
								answer = ReadBmsFromDir(GlobalVar.appSettings.osu_SongsPath);
							} else {
								answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled;
							}
						} else {
							answer.Return__Status = BGWcallback_SyncGetIDs.ReturnStatuses.Exception;
						}
					}
					e.Result = answer;
					break;
			}
		}

		public void BGW_syncGetIds_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
			BGWcallback_SyncGetIDs Answer = (BGWcallback_SyncGetIDs)e.UserState;
			switch(Answer.Progress__CurrentAction) {
				case BGWcallback_SyncGetIDs.ProgressCurrentActions.Sync:
					interface_loaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", Answer.Progress__Current.ToString()) + "\n"
                        + _e("MainWindow_andStillWorking");

                    interface_loaderProgressBar.Value = Answer.Progress__Current;
                    interface_loaderProgressBar.Visibility = Visibility.Visible;
					break;
				case BGWcallback_SyncGetIDs.ProgressCurrentActions.Done:
                    interface_loaderText.Text = _e("MainWindow_beatmapSetsInTotalParsed").Replace("%0", Answer.Progress__Current.ToString()) + "\n"
                        + _e("MainWindow_generatingInterface");
					break;
				case BGWcallback_SyncGetIDs.ProgressCurrentActions.CountingTotalFolders:
                    interface_loaderProgressBar.Maximum = Answer.Progress__Current;
					break;
			}
		}

		public void BGW_syncGetIds_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) {
			BGWcallback_SyncGetIDs answer = e.Result as BGWcallback_SyncGetIDs;
			switch(answer.Return__Status) {
				case BGWcallback_SyncGetIDs.ReturnStatuses.Success:
					interface_loaderText.Text = _e("MainWindow_beatmapSetsParsed").Replace("%0", answer.Return__Sync_BmDic_Installed.Count.ToString());
					if(!string.IsNullOrEmpty(answer.Return__Sync_Warnings)) {
						if(MessageBox.Show(_e("MainWindow_someBeatmapsDifferFromNormal") + "\n"
                            + _e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
							Window_MessageWindow Window_Message = new Window_MessageWindow();
                            Window_Message.SetMessage(answer.Return__Sync_Warnings, _e("MainWindow_exceptions"), "Sync");
                            Window_Message.ShowDialog();
						}
					}
					bmDic_installed = answer.Return__Sync_BmDic_Installed;

					sync_isDone = true;
					BmDisplayUpdate(bmDic_installed);
					OverlayShow(_e("MainWindow_syncCompleted"));
					OverlayFadeOut();

					if(sync_isDone_importerRequest) {
                        sync_isDone_importerRequest = false;
						BmDisplayUpdate(sync_isDone_importerRequest_saveValue, UpdateBmDisplayDestinations.Importer);
					}
					break;
				case BGWcallback_SyncGetIDs.ReturnStatuses.Cancelled:
				case BGWcallback_SyncGetIDs.ReturnStatuses.Exception:
					TextBlock UI_TextBlock = new TextBlock {
						FontSize = 72,
						Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
						HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(0, 100, 0, 0),
						Text = _e("MainWindow_lastSyncFailed"),
						VerticalAlignment = VerticalAlignment.Center
					};
					TextBlock UI_TextBlock_SubTitle = new TextBlock {
						FontSize = 24,
						Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
						HorizontalAlignment = HorizontalAlignment.Center,
						Text = _e("MainWindow_pleaseRetry"),
						VerticalAlignment = VerticalAlignment.Center
					};

                    BeatmapWrapper.Children.Clear();
                    BeatmapWrapper.Children.Add(UI_TextBlock);
                    BeatmapWrapper.Children.Add(UI_TextBlock_SubTitle);

					Bu_SyncRun.IsEnabled = true;
					break;
				case BGWcallback_SyncGetIDs.ReturnStatuses.FolderDoesNotExist:
					TextBlock UI_TextBlock_ = new TextBlock {
						FontSize = 72,
						Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
						HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(0, 100, 0, 0),
						Text = _e("MainWindow_lastSyncFailed"),
						VerticalAlignment = VerticalAlignment.Center
					};
					TextBlock UI_TextBlock_SubTitle_ = new TextBlock {
						FontSize = 24,
						Foreground = (SolidColorBrush)FindResource("GrayLighterBrush"),
						HorizontalAlignment = HorizontalAlignment.Center,
						Text = _e("MainWindow_pleaseRetry"),
						VerticalAlignment = VerticalAlignment.Center
					};

                    BeatmapWrapper.Children.Clear();
                    BeatmapWrapper.Children.Add(UI_TextBlock_);
                    BeatmapWrapper.Children.Add(UI_TextBlock_SubTitle_);

					MessageBox.Show(_e("MainWindow_unableToFindOsuFolderPleaseSpecify"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
					UI_ShowSettingsWindow(1);
					Bu_SyncRun.IsEnabled = true;
					break;
			}
		}

		public BeatmapDictionary ReadBmsFromDb(string dbPath, bool legacy = false) {
            BeatmapDictionary foundBms = new BeatmapDictionary();
			using(OsuReader Reader = new OsuReader(File.OpenRead(dbPath))) {    // More details: http://j.mp/1PIyjCY
				Reader.ReadInt32();                                     // osu! version (e.g. 20150203) 
				Reader.ReadInt32();                                     // Folder Count 
				Reader.ReadBoolean();                                   // AccountUnlocked (only false when the account is locked or banned in any way)
				Reader.ReadDate();                                      // Date the account will be unlocked 
				Reader.ReadString();                                    // Player name 
				int bmCount = Reader.ReadInt32();                       // Number of beatmaps 
				for(int i = 1; i <= bmCount; i++) {
					Beatmap BeatmapDetails = new Beatmap();
					if(!legacy)
						Reader.ReadInt32();                             // Unknown 
					BeatmapDetails.Artist = Reader.ReadString();        // Artist name
					Reader.ReadString();                                //  Artist name, in Unicode
					BeatmapDetails.Title = Reader.ReadString();         // Song title
					Reader.ReadString();                                //  Song title, in Unicode
					BeatmapDetails.Creator = Reader.ReadString();       // Creator name
					Reader.ReadString();                                //  Difficulty (e.g. Hard, Insane, etc.)
					Reader.ReadString();                                //  Audio file name
					BeatmapDetails.Md5 = Reader.ReadString();           // MD5 hash of the beatmap
					Reader.ReadString();                                //  Name of the .osu file corresponding to this beatmap
					BeatmapDetails.RankedStatus = Reader.ReadByte();    // Ranked status
					Reader.ReadBytes(38);                               //  Other data No. of Circles/Sliders/Spinners, Last Edit, Settings etc.
					for(int j = 1; j <= 4; j++) {                       //  Star difficulties with various mods
                        var count = Reader.ReadInt32();
						if(count < 0)
							continue;
						for(int k = 1; k <= count; k++) {
							Reader.ReadBytes(14);
						}
					}
					Reader.ReadBytes(12);                               //  Drain/Total/Preview Time
					var TimingPointCount = Reader.ReadInt32();          // @TODO: You could probably optimise these loops. Reader.ReadBytes(Count*17) maybe. I don't have the time to test it.
					for(int j = 1; j <= TimingPointCount; j++) {
						Reader.ReadBytes(17);
					}
					Reader.ReadInt32();                                 //  Beatmap ID
					BeatmapDetails.Id = Reader.ReadInt32();             // Beatmap set ID
					Reader.ReadInt32();                                 //  Thread ID
					Reader.ReadBytes(11);
					Reader.ReadString();                                //  Song source
					BeatmapDetails.SongTags = Reader.ReadString();      // Song tags
					Reader.ReadInt16();                                 //  Online offset 
					Reader.ReadString();                                //  Font used for the title of the song 
					BeatmapDetails.IsUnplayed = Reader.ReadBoolean();   // Is unplayed
					Reader.ReadBytes(9);
					Reader.ReadString();                                //  Folder name of the beatmap, relative to Songs folder 
					Reader.ReadBytes(18);

					if(!foundBms.ContainsKey(BeatmapDetails.Id)) {
						foundBms.Add(BeatmapDetails.Id, BeatmapDetails);
						BGW_syncGetIds.ReportProgress(0, new BGWcallback_SyncGetIDs { Progress__Current = foundBms.Count });
					}
				}
			}
			return foundBms;
		}

		public BGWcallback_SyncGetIDs ReadBmsFromDir(string dirPath) {
			BGWcallback_SyncGetIDs answer = new BGWcallback_SyncGetIDs {
				Func_Invalid = new List<string>(),
				Func_InvalidId = new List<string>()
			};
			foreach(string thisDir in Directory.GetDirectories(dirPath)) {
				DirectoryInfo DirectoryInfo = new DirectoryInfo(thisDir);
				if(DirectoryInfo.Name.ToLower() != "failed" & DirectoryInfo.Name.ToLower() != "tutorial") {
					bool foundFile = false;
					foreach(string thisFile in Directory.GetFiles(thisDir)) {
						if(Path.GetExtension(thisFile) == ".osu") {
							foundFile = true;
							// Read File
							StreamReader fileReader = new StreamReader(thisFile);
							List<string> textLines = new List<string>();
							Beatmap bmDetails = new Beatmap();
							bool found_id = false;      // Cause the older osu! file format doesn't include the set ID
							bool found_title = false;
							bool found_artist = false;
							bool found_creator = false;
							while(fileReader.Peek() != -1 & textLines.Count <= 50) {    // Don't read more than 50 lines
                                textLines.Add(fileReader.ReadLine());
							}
							foreach(string thisLine in textLines) {
								if(found_id & found_title & found_artist & found_creator)
									break;
								if(thisLine.StartsWith("Title:")) {
                                    found_title = true;
									bmDetails.Title = thisLine.Substring(6);
								} else if(thisLine.StartsWith("Artist:")) {
                                    found_artist = true;
                                    bmDetails.Artist = thisLine.Substring(7);
								} else if(thisLine.StartsWith("BeatmapSetID:")) {
                                    found_id = true;
									try {
                                        bmDetails.Id = Convert.ToInt32(thisLine.Substring(13));
									} catch(InvalidCastException) {
                                        bmDetails.Id = -1;
                                        found_id = false;
									}
								} else if(thisLine.StartsWith("Creator:")) {
									found_creator = true;
                                    bmDetails.Creator = thisLine.Substring(8);
								}
							}
							if(found_id == false) {
								// Looks like it's an old file, so try to get ID from folder name
								try {
                                    bmDetails.Id = Convert.ToInt32(DirectoryInfo.Name.Substring(0, DirectoryInfo.Name.IndexOf(" ")));
								} catch(Exception) {
                                    bmDetails.Id = -1;
									answer.Func_InvalidId.Add(bmDetails.Id + " | " + bmDetails.Artist + " | " + bmDetails.Title);
								}
							} else {
								if(!answer.Return__Sync_BmDic_Installed.ContainsKey(bmDetails.Id))
                                    answer.Return__Sync_BmDic_Installed.Add(bmDetails.Id, bmDetails);
								BGW_syncGetIds.ReportProgress(0, new BGWcallback_SyncGetIDs { Progress__Current = answer.Return__Sync_BmDic_Installed.Count });
								break;
							}
						}
					}

					if(!foundFile) {    // Can't read/find osu! file
                        try {
							string bm_id = DirectoryInfo.Name.Substring(0, DirectoryInfo.Name.IndexOf(" "));
							string bm_artist = DirectoryInfo.Name.Substring(bm_id.Length + 1, DirectoryInfo.Name.IndexOf(" - ") - bm_id.Length - 1);
							string bm_name = DirectoryInfo.Name.Substring(bm_id.Length + bm_artist.Length + 4);
							Beatmap thisBm = new Beatmap {
								Id = Convert.ToInt32(bm_id),
								Title = bm_name,
								Artist = bm_artist
                            };
							if(!answer.Return__Sync_BmDic_Installed.ContainsKey(thisBm.Id))
                                answer.Return__Sync_BmDic_Installed.Add(thisBm.Id, thisBm);
						} catch(Exception) {
                            answer.Func_Invalid.Add(DirectoryInfo.Name);
						}
					}
				}
			}
			if(answer.Func_Invalid.Count != 0) {
                StringBuilder sb = new StringBuilder();
				sb.Append("# " + _e("MainWindow_ignoredFolders") + "\n"
                    + _e("MainWindow_folderCouldntBeParsed") + "\n\n"
                    + "> " + _e("MainWindow_folders") + ":\n");
				foreach(string thisItem in answer.Func_Invalid) {
					sb.Append("* " + thisItem + "\n");
				}
				sb.Append("\n\n");
				answer.Return__Sync_Warnings += sb.ToString();
			}
			if(answer.Func_InvalidId.Count != 0) {
                StringBuilder sb = new StringBuilder();
				sb.Append("# " + _e("MainWindow_unableToGetId") + "\n"
                    + _e("MainWindow_unableToGetIdOfSomeBeatmapsTheyllBeHandledAsUnsubmitted") + "\n\n"
                    + "> " + _e("MainWindow_beatmaps") + ":\n");
				foreach(string thisItem in answer.Func_InvalidId) {
					sb.Append("* " + thisItem + "\n");
				}
				sb.Append("\n\n");
				answer.Return__Sync_Warnings += sb.ToString();
			}
			return answer;
		}
		#endregion

		#region "Exporter"
		public void Bu_ExporterCancel_Click(object sender, RoutedEventArgs e) {
			TI_Exporter.Visibility = Visibility.Collapsed;
			TC_Main.SelectedIndex = 0;
			SP_ExporterWrapper.Children.Clear();
		}

		public void Bu_ExporterInvertSel_Click(object sender, RoutedEventArgs e) {
			// Copy data before manipulation
			List<BeatmapItem_Exporter> listSelected = exporter_bmList_selectedTags.ToList();
			List<BeatmapItem_Exporter> listUnselected = exporter_bmList_unselectedTags.ToList();
			
			int i = 0;
			while(i < listSelected.Count) { // Loop selected elements
                listSelected[i].CB_IsSelected.IsChecked = false;
				i++;
			}

			i = 0;
			while(i < listUnselected.Count) {   // Loop unselected elements
                listUnselected[i].CB_IsSelected.IsChecked = true;
				i++;
			}
		}

		public void Bu_ExporterRun_Click(object sender, RoutedEventArgs e) {
            BeatmapDictionary answer = new BeatmapDictionary();
			foreach(var Item in exporter_bmList_selectedTags) {
                answer.Add(Item.Beatmap.Id, Item.Beatmap);
			}
            Exporter_ExportBmDialog(answer, _e("MainWindow_exportSelectedBeatmaps"));
			TI_Exporter.Visibility = Visibility.Collapsed;
			TC_Main.SelectedIndex = 0;
			SP_ExporterWrapper.Children.Clear();
		}

		public void Exporter_AddBmToSel(object sender, EventArgs e) {
			BeatmapItem_Exporter cParent = (BeatmapItem_Exporter)((Grid)((CheckBox)sender).Parent).Parent;
			exporter_bmList_unselectedTags.Remove(cParent);
			exporter_bmList_selectedTags.Add(cParent);
			Bu_ExporterRun.IsEnabled = (exporter_bmList_selectedTags.Count > 0);
            cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("GreenLightBrush");

        }

		public void Exporter_DetermineWheterAddOrRemove(object sender, EventArgs e) {
            BeatmapItem_Exporter cParent = null;
			if(sender is Image) {
                cParent = (BeatmapItem_Exporter)((Grid)((Image)sender).Parent).Parent;
			} else if(sender is System.Windows.Shapes.Rectangle) {
                cParent = (BeatmapItem_Exporter)((Grid)((System.Windows.Shapes.Rectangle)sender).Parent).Parent;
			} else if(sender is TextBlock) {
                cParent = (BeatmapItem_Exporter)((Grid)((StackPanel)((TextBlock)sender).Parent).Parent).Parent;
			} else {
				return;
			}

			if((bool)cParent.CB_IsSelected.IsChecked) {
				exporter_bmList_selectedTags.Remove(cParent);
				if(exporter_bmList_selectedTags.Count == 0)
					Bu_ExporterRun.IsEnabled = false;

                cParent.CB_IsSelected.IsChecked = false;
                cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("GrayLightBrush");

            } else {
				exporter_bmList_selectedTags.Add(cParent);
				Bu_ExporterRun.IsEnabled = (exporter_bmList_selectedTags.Count > 0);

                cParent.CB_IsSelected.IsChecked = true;
                cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("GreenLightBrush");

            }
		}

		public void Exporter_RemoveBmFromSel(object sender, EventArgs e) {
            BeatmapItem_Exporter cParent = (BeatmapItem_Exporter)((Grid)((CheckBox)sender).Parent).Parent;
			exporter_bmList_selectedTags.Remove(cParent);
			exporter_bmList_unselectedTags.Add(cParent);
			if(exporter_bmList_selectedTags.Count == 0)
				Bu_ExporterRun.IsEnabled = false;
            cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("GrayLightBrush");

        }
		#endregion

		#region "Importer"
		public void Bu_ImporterCancel_Click(object sender, RoutedEventArgs e) {
			TC_Main.SelectedIndex = 0;
			TI_Importer.Visibility = Visibility.Collapsed;
			SP_ImporterWrapper.Children.Clear();
			importerHolder = null;
		}

		public void Bu_ImporterRun_Click(object sender, RoutedEventArgs e) {
            importerHolder.beatmapListDownloadManager = new BeatmapListDownloadManager(this, importerHolder);
            importerHolder.beatmapListDownloadManager.StartDownload();
        }

		public void CB_ImporterHideInstalled_Checked(object sender, RoutedEventArgs e) {
			foreach(BeatmapItem_Importer i in SP_ImporterWrapper.Children) {
				if(i.IsInstalled) {
                    i.Gr_Grid.Visibility = Visibility.Collapsed;
				}
			}
		}

		public void CB_ImporterHideInstalled_Unchecked(object sender, RoutedEventArgs e) {
			foreach(BeatmapItem_Importer i in SP_ImporterWrapper.Children) {
                i.Gr_Grid.Visibility = Visibility.Visible;
			}
		}

		public void Importer_AddBmToSel(object sender, EventArgs e) {
            BeatmapItem_Importer cParent = (BeatmapItem_Importer)((Grid)((CheckBox)sender).Parent).Parent;
			importerHolder.BmList_TagsToInstall.Add(cParent);
			importerHolder.BmList_TagsLeftOut.Remove(cParent);

			if(importerHolder.BmList_TagsToInstall.Count > 0) {
				Bu_ImporterRun.IsEnabled = true;
				Bu_ImporterCancel.IsEnabled = true;
			} else {
				Bu_ImporterRun.IsEnabled = false;
			}
			importerHolder.SetState();
            cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("RedLightBrush");

        }

		public void Importer_ReadListFile(string filePath) {
			switch(Path.GetExtension(filePath)) {
				case ".nw520-osblx":
					try {
						string File_Content = GlobalVar.StringDecompress(File.ReadAllText(filePath));
						Importer_ShowRawOSBL(File_Content, filePath);
					} catch(FormatException ex) {
						MessageBox.Show(_e("MainWindow_unableToReadFile") + "\n\n" + 
                            "> " + _e("MainWindow_details") + ":\n" + 
                            ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
					}
					break;
				case ".nw520-osbl":
				case ".json":
					Importer_ShowRawOSBL(File.ReadAllText(filePath), filePath);
					break;
				case ".zip":
					// @TODO: If contains multiple OSBLX-files read and process each one
					try {
						using(ZipFile zipper = ZipFile.Read(filePath)) {
							string directoryName = GlobalVar.appTempPath + "/Zipper/Importer-".Replace('/', Path.DirectorySeparatorChar) + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
							Directory.CreateDirectory(directoryName);
							foreach(ZipEntry ZipperEntry in zipper) {
								if(Path.GetExtension(ZipperEntry.FileName) == ".nw520-osblx") {
									ZipperEntry.Extract(directoryName);
									Importer_ReadListFile(directoryName + Path.DirectorySeparatorChar + ZipperEntry.FileName);
									break;
								}
							}
						}
					} catch(ZipException ex) {
						MessageBox.Show(_e("MainWindow_unableToReadFile") + "\n\n" + 
                            "> " + _e("MainWindow_details") + ":\n" + 
                            ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
					}
					break;
				default:
					MessageBox.Show(_e("MainWindow_unknownFileExtension") + ":\n" + 
                        Path.GetExtension(filePath), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
			}
		}

		public void Importer_RemoveBmFromSel(object sender, EventArgs e) {
			BeatmapItem_Importer cParent = (BeatmapItem_Importer)((Grid)((CheckBox)sender).Parent).Parent;
			// Get Tag from parent Grid
			importerHolder.BmList_TagsToInstall.Remove(cParent);
			importerHolder.BmList_TagsLeftOut.Add(cParent);
			if(importerHolder.BmList_TagsToInstall.Count == 0) {
				Bu_ImporterRun.IsEnabled = false;
				Bu_ImporterCancel.IsEnabled = true;
			}
			importerHolder.SetState();
            cParent.Re_DecoBorder.Fill = (SolidColorBrush)FindResource("GrayLightBrush");

        }

		public void Importer_ShowRawOSBL(string fileContent, string filePath) {
			try {
				JObject fileContentJson = (JObject)JsonConvert.DeserializeObject(fileContent);
				TB_ImporterInfo.Text = filePath;
				BmDisplayUpdate(ConvertSavedJsonToBmList(fileContentJson), UpdateBmDisplayDestinations.Importer);
			} catch(JsonReaderException ex) {
				MessageBox.Show(_e("MainWindow_unableToReadFile") + "\n\n" +
                    "> " + _e("MainWindow_details") + ":\n" +
                    ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void TB_ImporterMirror_MouseDown(object sender, MouseButtonEventArgs e) {
			Process.Start(MirrorManager.app_mirrors[GlobalVar.appSettings.Tool_ChosenDownloadMirror].WebUrl);
		}
		#endregion
	}
}
