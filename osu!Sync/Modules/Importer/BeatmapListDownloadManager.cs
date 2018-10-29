using osuSync.Models.Importer;
using osuSync.UserControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using static osuSync.Models.MirrorManager;

namespace osuSync.Modules.Importer {

    class BeatmapListDownloadManager {
        internal MainWindow window;
        internal ImporterHolder importerHolder;

        internal BeatmapDownloadHandler activeDownloadManager;
        internal int counter;
        internal bool pref_pingFail_skipAlways = false;


        internal BeatmapListDownloadManager(MainWindow window, ImporterHolder importerHolder) {
            this.window = window;
            this.importerHolder = importerHolder;

            // Prepare event handler
            importerHolder.Downloader.DownloadFileCompleted += _downloadFileCompleted;
            importerHolder.Downloader.DownloadProgressChanged += _downloadProgressChanged;
        }

        internal void CancelSession() {
            InstallFiles();
            SetState(GlobalVar._e("MainWindow_aborted"), null, null, null);
            window.UI_SetStatus(GlobalVar._e("MainWindow_aborted"));

            _resetUi();
            window.Bu_ImporterRun.IsEnabled = true;
        }

        internal DownloadMirror GetMirror() {
            return app_mirrors[GlobalVar.appSettings.Tool_ChosenDownloadMirror];
        }

        internal void InstallFiles() {
            // Pre
            window.PB_ImporterProg.IsIndeterminate = true;
            window.PB_ImporterProg.Visibility = Visibility.Visible;

            // Main
            SetState(GlobalVar._e("MainWindow_installing"));
            window.UI_SetStatus(GlobalVar._e("MainWindow_installingFiles"), true);

            foreach(string thisPath in Directory.GetFiles(GlobalVar.appTempPath + "/Downloads/Beatmaps".Replace('/', Path.DirectorySeparatorChar))) {
                if(!File.Exists(GlobalVar.appSettings.osu_SongsPath + Path.DirectorySeparatorChar + Path.GetFileName(thisPath))) {
                    try {
                        File.Move(thisPath, GlobalVar.appSettings.osu_SongsPath + Path.DirectorySeparatorChar + Path.GetFileName(thisPath));
                    } catch(IOException) {
                        MessageBox.Show(GlobalVar._e("MainWindow_unableToInstallBeatmap").Replace("%0", Path.GetFileName(thisPath)), "Debug | " + GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                } else {
                    File.Delete(thisPath);
                }
            }
            counter = 0; // Reset counter

            // Post
            window.PB_ImporterProg.IsIndeterminate = false;
            window.PB_ImporterProg.Visibility = Visibility.Hidden;
        }

        internal void ProceedWithNextBeatmap() {
            if(importerHolder.BmList_TagsToInstall.Count > 0) {
                // There are still beatmaps left to download

                // Install files if necessary
                if(_isInstallationRequired()) {
                    InstallFiles();
                }

                // Donload next beatmap
                _performDownload();
            } else {
                _finishSession();
            }
        }

        internal void StartDownload() {
            if(GlobalVar.tool_hasWriteAccessToOsu) {
                // Set UI
                window.Bu_SyncRun.IsEnabled = false;
                window.Bu_ImporterRun.IsEnabled = false;
                window.Bu_ImporterCancel.IsEnabled = false;
                window.PB_ImporterProg.Value = 0;
                window.PB_ImporterProg.Visibility = Visibility.Visible;
                
                // Prepare directory
                Directory.CreateDirectory(GlobalVar.appTempPath + "/Downloads/Beatmaps".Replace('/', Path.DirectorySeparatorChar));

                // Start
                _performDownload();
            } else {
                if(MessageBox.Show(GlobalVar._e("MainWindow_requestElevation"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
                    // @TODO: fix
                    if(GlobalVar.RequestElevation("-openFile=" + window.TB_ImporterInfo.ToolTip.ToString())) {
                        System.Windows.Application.Current.Shutdown();
                        return;
                    } else {
                        MessageBox.Show(GlobalVar._e("MainWindow_elevationFailed"), GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                        window.OverlayShow(GlobalVar._e("MainWindow_importAborted"), GlobalVar._e("MainWindow_insufficientPermissions"));
                        window.OverlayFadeOut();
                    }
                } else {
                    window.OverlayShow(GlobalVar._e("MainWindow_importAborted"), GlobalVar._e("MainWindow_insufficientPermissions"));
                    window.OverlayFadeOut();
                }
            }
        }

        internal void SetState(string state = null, bool? setsDone = true, bool? setsLeftout = null, bool? setsTotal = true) {
            importerHolder.SetState(state, setsDone, setsLeftout, setsTotal);
        }

        internal void SetBeatmapPostDownloadState(string borderColor, List<BeatmapItem_Importer> addTo) {
            var target = importerHolder.BmList_TagsToInstall.First();
            var removeFrom = importerHolder.BmList_TagsToInstall;

            target.Re_DecoBorder.Fill = (SolidColorBrush)window.FindResource(borderColor);
            if(addTo != null) {
                addTo.Add(target);
            }
            if(removeFrom != null) {
                removeFrom.Remove(target);
            }
        }

        
        private void _downloadFileCompleted(object sender, AsyncCompletedEventArgs e) {
            counter++;

            // Move beatmap to correct list and set UI
            if(File.Exists(GlobalVar.appTempPath + "/Downloads/Beatmaps/".Replace('/', Path.DirectorySeparatorChar) + activeDownloadManager.currentFileName)) {
                // Detect "Beatmap Not Found" pages
                if(new FileInfo(GlobalVar.appTempPath + "/Downloads/Beatmaps/".Replace('/', Path.DirectorySeparatorChar) + activeDownloadManager.currentFileName).Length <= 3000) {
                    // File Empty
                    SetBeatmapPostDownloadState("OrangeLightBrush", importerHolder.BmList_TagsFailed);
                    importerHolder.BmList_TagsToInstall.First().Re_DecoBorder.Fill = (SolidColorBrush)window.FindResource("OrangeLightBrush");

                    // Try to delete corpse file
                    try {
                        File.Delete(GlobalVar.appTempPath + "/Downloads/Beatmaps/" + activeDownloadManager.currentFileName);
                    } catch(IOException) {}
                } else {
                    // File Normal
                    SetBeatmapPostDownloadState("PurpleDarkBrush", importerHolder.BmList_TagsDone);
                }
            } else {
                // File Empty
                SetBeatmapPostDownloadState("OrangeLightBrush", importerHolder.BmList_TagsFailed);
            }
            ProceedWithNextBeatmap();
        }

        private void _downloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            window.PB_ImporterProg.Value = e.ProgressPercentage;
        }

        private void _finishSession() {
            InstallFiles();

            window.UI_SetStatus(GlobalVar._e("MainWindow_finished"));
            SetState(GlobalVar._e("MainWindow_finished"));

            // Display fail summary
            if(importerHolder.BmList_TagsFailed.Count > 0) {
                string Failed = "# " + GlobalVar._e("MainWindow_downloadFailed") + "\n"
                    + GlobalVar._e("MainWindow_cantDownload") + "\n\n"
                    + "> " + GlobalVar._e("MainWindow_beatmaps") + ": ";
                foreach(var thisTagData in importerHolder.BmList_TagsFailed) {
                    Failed += "\n" + "* " + thisTagData.Beatmap.Id.ToString() + " / " + thisTagData.Beatmap.Artist + " / " + thisTagData.Beatmap.Title;
                }
                if(MessageBox.Show(GlobalVar._e("MainWindow_someBeatmapSetsHadntBeenImported") + "\n"
                    + GlobalVar._e("MainWindow_doYouWantToCheckWhichBeatmapSetsAreAffected"), GlobalVar.appName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
                    Window_MessageWindow Window_Message = new Window_MessageWindow();
                    Window_Message.SetMessage(Failed, GlobalVar._e("MainWindow_downloadFailed"), "Import");
                    Window_Message.ShowDialog();
                }
            }

            // Display final report
            window.BalloonShow(_generateShortReport());
            MessageBox.Show(_generateShortReport() + "\n\n" + GlobalVar._e("MainWindow_pressF5"), GlobalVar.appName, MessageBoxButton.OK);

            // Request to start osu! if configured
            if(GlobalVar.appSettings.Messages_Importer_AskOsu && !(Process.GetProcessesByName("osu!").Count() > 0)
                && MessageBox.Show(GlobalVar._e("MainWindow_doYouWantToStartOsuNow"), GlobalVar.msgTitleDisableable, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                window.StartOrFocusOsu();

            _resetUi();


            string _generateShortReport() {
                return GlobalVar._e("MainWindow_installationFinished") + "\n"
                + GlobalVar._e("MainWindow_setsDone").Replace("%0", importerHolder.BmList_TagsDone.Count.ToString()) + "\n"
                + GlobalVar._e("MainWindow_setsFailed").Replace("%0", importerHolder.BmList_TagsFailed.Count.ToString()) + "\n"
                + GlobalVar._e("MainWindow_setsLeftOut").Replace("%0", importerHolder.BmList_TagsLeftOut.Count.ToString()) + "\n"
                + GlobalVar._e("MainWindow_setsTotal").Replace("%0", importerHolder.BmTotal.ToString());
            }
        }

        private void _performDownload() {
            activeDownloadManager = new BeatmapDownloadHandler(window, importerHolder, importerHolder.BmList_TagsToInstall.First(), this, GetMirror());
            activeDownloadManager.PerformDownload();
        }

        private bool _isInstallationRequired() {
            return GlobalVar.appSettings.Tool_Importer_AutoInstallCounter != 0 && GlobalVar.appSettings.Tool_Importer_AutoInstallCounter <= counter;
        }

        private void _resetUi() {
            window.Bu_SyncRun.IsEnabled = true;
            window.Bu_ImporterCancel.IsEnabled = true;
        }
    }
}
