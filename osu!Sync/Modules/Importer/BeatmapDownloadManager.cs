using osuSync.Modules.Importer;
using osuSync.UserControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Media;
using static osuSync.Modules.MirrorManager;
using static osuSync.Modules.TranslationManager;
using static osuSync.Window_GenericMsgBox;

namespace osuSync.Models.Importer {

    class BeatmapDownloadHandler {
        MainWindow window;
        ImporterHolder importerHolder;
        BeatmapItem_Importer beatmapItemImporter;
        BeatmapListDownloadManager beatmapListDownloadManager;
        DownloadMirror mirror;

        internal string currentFileName { get; private set; }

        internal BeatmapDownloadHandler(MainWindow window, ImporterHolder importerHolder, BeatmapItem_Importer beatmapItemImporter, BeatmapListDownloadManager beatmapListDownloadManager, DownloadMirror mirror) {
            this.window = window;
            this.importerHolder = importerHolder;
            this.beatmapItemImporter = beatmapItemImporter;
            this.beatmapListDownloadManager = beatmapListDownloadManager;
            this.mirror = mirror;
        }

        internal void PerformDownload() {
            _state_prePing();

            // HttpWebRequest req
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_getRequestUri());
            req.Referer = "https://osu.ppy.sh/forum/t/270446";
            req.UserAgent = "osu!Sync v" + GlobalVar.AppVersion.ToString();

            _performPing(req);
        }


        private void _downloadFile(string requestUri, string fileName) {
            window.UI_SetStatus(_e("MainWindow_downloading").Replace("%0", Convert.ToString(beatmapItemImporter.Beatmap.Id)), true);
            beatmapListDownloadManager.SetState(_e("MainWindow_downloading1"));
            window.PB_ImporterProg.IsIndeterminate = false;
            importerHolder.Downloader.DownloadFileAsync(new Uri(requestUri), (GlobalVar.appTempPath + "/Downloads/Beatmaps/".Replace('/', Path.DirectorySeparatorChar) + fileName));
        }

        private string _getFileName(WebResponse res) {
            string result;

            if(res.Headers["Content-Disposition"] != null) {
                // File name header is set, extract actual file name
                result = res.Headers["Content-Disposition"].Substring(res.Headers["Content-Disposition"].IndexOf("filename=") + 10).Replace("\"", "");

                // Remove excessive `;` at end if present
                if(result.Substring(result.Length - 1) == ";")
                    result = result.Substring(0, result.Length - 1);

                // Remove file name encoding string at end if present
                if(result.Contains("; filename*=UTF-8"))
                    result = result.Substring(0, result.IndexOf(".osz") + 4);
            } else {
                // File name not set by server. Use beatmap id as fallback
                result = Convert.ToString(beatmapItemImporter.Beatmap.Id) + ".osz";
            }
            result = GlobalVar.PathSanitize(result);      // Issue #23: Replace invalid characters

            return result;
        }

        private string _getRequestUri() {
            return mirror.DownloadUrl.Replace("%0", Convert.ToString(beatmapItemImporter.Beatmap.Id));
        }

        private void _performPing(HttpWebRequest req) {
            try {
                WebResponse response = req.GetResponse();
                currentFileName = _getFileName(response);
                _downloadFile(_getRequestUri(), currentFileName);
            } catch(WebException ex) {
                _state_postPingFailed();
            }
        }

        private void _state_prePing() {
            window.PB_ImporterProg.Value = 0;
            window.PB_ImporterProg.IsIndeterminate = true;

            window.UI_SetStatus(_e("MainWindow_fetching").Replace("%0", Convert.ToString(beatmapItemImporter.Beatmap.Id)), true);
            window.TB_ImporterMirror.Text = _e("MainWindow_downloadMirror") + ": " + app_mirrors[GlobalVar.appSettings.Tool_ChosenDownloadMirror].DisplayName;

            beatmapItemImporter.Re_DecoBorder.Fill = (SolidColorBrush)window.FindResource("BlueLightBrush");
            beatmapItemImporter.CB_IsSelected.IsEnabled = false;
            beatmapItemImporter.CB_IsSelected.IsThreeState = false;
            beatmapItemImporter.CB_IsSelected.IsChecked = null;

            beatmapListDownloadManager.SetState(_e("MainWindow_fetching1"));
        }

        private void _state_postPingFailed() {
            if(importerHolder.Pref_FetchFail_SkipAlways) {
                beatmapListDownloadManager.ProceedWithNextBeatmap();
            } else {
                Window_GenericMsgBox errorMsgBox = new Window_GenericMsgBox(_e("MainWindow_unableToFetchMirrorData"), new List<MsgBoxButtonHolder> {
                        new MsgBoxButtonHolder(_e("Global_buttons_skip"), (int)MsgBoxResult.Yes),
                        new MsgBoxButtonHolder(_e("Global_buttons_skipAlways"), (int)MsgBoxResult.YesAll),
                        new MsgBoxButtonHolder(_e("Global_buttons_cancel"), (int)MsgBoxResult.Cancel)
                    }, null, System.Drawing.SystemIcons.Exclamation);
                errorMsgBox.ShowDialog();
                switch(errorMsgBox.Result) {
                    case (int)MsgBoxResult.Yes:
                        beatmapListDownloadManager.SetBeatmapPostDownloadState("OrangeLightBrush", importerHolder.BmList_TagsToInstall);

                        beatmapListDownloadManager.ProceedWithNextBeatmap();
                        break;
                    case (int)MsgBoxResult.YesAll:
                        importerHolder.Pref_FetchFail_SkipAlways = true;
                        beatmapListDownloadManager.SetBeatmapPostDownloadState("OrangeLightBrush", importerHolder.BmList_TagsToInstall);

                        beatmapListDownloadManager.ProceedWithNextBeatmap();
                        break;
                    case (int)MsgBoxResult.Cancel:
                    case (int)MsgBoxResult.None:
                        beatmapItemImporter.Re_DecoBorder.Fill = (SolidColorBrush)window.FindResource("OrangeLightBrush");

                        beatmapListDownloadManager.CancelSession();
                        break;
                }
            }
        }
    }
}
