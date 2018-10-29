using osuSync.UserControls;
using System.Collections.Generic;
using System.Net;
using System.Text;
using static osuSync.Modules.TranslationManager;

namespace osuSync.Modules.Importer {

    class ImporterHolder {
        internal List<BeatmapItem_Importer> BmList_TagsDone = new List<BeatmapItem_Importer>();
        internal List<BeatmapItem_Importer> BmList_TagsFailed = new List<BeatmapItem_Importer>();
        internal List<BeatmapItem_Importer> BmList_TagsLeftOut = new List<BeatmapItem_Importer>();
        internal List<BeatmapItem_Importer> BmList_TagsToInstall = new List<BeatmapItem_Importer>();
        internal int BmTotal;
        internal WebClient Downloader = new WebClient();
        internal BeatmapListDownloadManager beatmapListDownloadManager;
        internal string FilePath;
        internal bool Pref_FetchFail_SkipAlways = false;
        internal MainWindow window;

        internal ImporterHolder(MainWindow window) {
            this.window = window;
        }

        internal void SetState(string state = null, bool? setsDone = true, bool? setsLeftout = null, bool? setsTotal = true) {
            state = (state ?? GlobalVar.appName);
            var sb = new StringBuilder();

            sb.Append(state);

            var setDoneCount = BmList_TagsDone.Count;
            if(setsDone == true || setsDone == null && setDoneCount > 0) sb.Append(" | " + _e("MainWindow_setsDone").Replace("%0", setDoneCount.ToString()));

            var setsLeftoutCount = BmList_TagsLeftOut.Count;
            if(setsLeftout == true || setsLeftout == null && setsLeftoutCount > 0) sb.Append(" | " + _e("MainWindow_leftOut").Replace("%0", setsLeftoutCount.ToString()));

            var setsTotalCount = BmTotal;
            if(setsTotal == true || setsTotal == null && setsTotalCount > 0) sb.Append(" | " + _e("MainWindow_setsTotal").Replace("%0", setsTotalCount.ToString()));

            window.TB_ImporterInfo.Text = sb.ToString();
        }
    }
}
