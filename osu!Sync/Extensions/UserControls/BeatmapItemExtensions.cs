using osuSync.Interfaces.UserControls;
using System;
using System.IO;
using System.Net;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace osuSync.Extensions.UserControls {

    public class BeatmapItemExtensionHelper {
        public IBeatmapItem BmItem { get; set; }
        public Image Target { get; set; }
        public MainWindow Host { get; set; }

        public BeatmapItemExtensionHelper(IBeatmapItem bmItem, Image target, MainWindow host) {
            BmItem = bmItem;
            Target = target;
            Host = host;
        }

        public void EventHandler_DownloadThumb(object sender, MouseButtonEventArgs e) {
            var cParent = (IBeatmapItem)((Grid)((Image)sender).Parent).Parent;
            cParent.DownloadThumbnail(this);

            Target.MouseLeftButtonUp -= EventHandler_DownloadThumb;
            Target.MouseRightButtonUp -= Host.BmDP_Show;
            Target.MouseDown += Host.BmDP_Show;
        }

        public void WebClient_DownloadThumbnailCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) {
            Host.UI_SetStatus(GlobalVar._e("MainWindow_finished"));
            if(File.Exists(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + BmItem.Beatmap.Id + ".jpg") && (new FileInfo(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + BmItem.Beatmap.Id + ".jpg")).Length >= 10) {
                try {
                    Target.Source = new BitmapImage(new Uri(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + BmItem.Beatmap.Id + ".jpg"));
                    Target.ToolTip = GlobalVar._e("MainWindow_openBeatmapDetailPanel");
                } catch(NotSupportedException) {
                    Target.Source = new BitmapImage(new Uri("../Resources/NoThumbnail.png", UriKind.Relative));
                }
            } else if((new FileInfo(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + BmItem.Beatmap.Id + ".jpg")).Length <= 10) {
                File.Delete(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + BmItem.Beatmap.Id + ".jpg");
                Target.Source = new BitmapImage(new Uri("../Resources/NoThumbnail.png", UriKind.Relative));
            } else {
                Target.Source = new BitmapImage(new Uri("Resources/NoThumbnail.png", UriKind.Relative));
            }
        }
    }

    public static class BeatmapItemExtensions {
        public static void DownloadThumbnail(this IBeatmapItem bmItem, BeatmapItemExtensionHelper hmExHelp) {
            hmExHelp.Target.Source = new BitmapImage(new Uri("../Resources/ProgressThumbnail.png", UriKind.Relative));
            Directory.CreateDirectory(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails");
            hmExHelp.Host.UI_SetStatus(GlobalVar._e("MainWindow_downloadingThumbnail").Replace("%0", Convert.ToString(bmItem.Beatmap.Id)), true);
            WebClient thumbClient = new WebClient();
            thumbClient.DownloadFileCompleted += hmExHelp.WebClient_DownloadThumbnailCompleted;
            thumbClient.DownloadFileAsync(new Uri("https://b.ppy.sh/thumb/" + bmItem.Beatmap.Id + ".jpg"), GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + bmItem.Beatmap.Id + ".jpg");
        }

        public static bool LoadThumbnail(this IBeatmapItem bmItem, Image target, MainWindow host, bool enableBmdp = true, bool enableThumbDownload = true) {
            if(!string.IsNullOrEmpty(bmItem.Beatmap.ThumbnailPath)) {
                try {
                    target.Source = new BitmapImage(new Uri(bmItem.Beatmap.ThumbnailPath));

                    if(enableBmdp) {
                        target.ToolTip = GlobalVar._e("MainWindow_openBeatmapDetailPanel");
                        target.MouseDown += host.BmDP_Show;
                    }
                    return true;
                } catch(NotSupportedException) {
                }
            }

            // If successfull, already returned at this point
            if(enableThumbDownload) {
                target.Source = new BitmapImage(new Uri("../../Resources/DownloadThumbnail.png", UriKind.Relative));
                target.ToolTip = GlobalVar._e("MainWindow_downladThumbnail");

                var bmExHelp = new BeatmapItemExtensionHelper(bmItem, target, host);
                target.MouseLeftButtonUp += bmExHelp.EventHandler_DownloadThumb;
                target.MouseRightButtonUp += host.BmDP_Show;
            } else {
                target.Source = new BitmapImage(new Uri("../../Resources/NoThumbnail.png", UriKind.Relative));

                if(enableBmdp) {
                    target.ToolTip = GlobalVar._e("MainWindow_openBeatmapDetailPanel");
                    target.MouseDown += host.BmDP_Show;
                }
            }
            return false;
        }
    }
}
