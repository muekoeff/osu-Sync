using osuSync.Interfaces.UserControls;
using osuSync.Models;
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static osuSync.Modules.TranslationManager;

namespace osuSync.UserControls {

    public partial class BeatmapItem_Installed : UserControl, IBeatmapItem {
        public Beatmap Beatmap { get; private set; }

        public BeatmapItem_Installed(Beatmap bm, MainWindow host) {
            Beatmap = bm;
            InitializeComponent();

            // Thumbnail
            if(!string.IsNullOrEmpty(bm.ThumbnailPath)) {
                try {
                    Im_Thumbnail.Source = new BitmapImage(new Uri(bm.ThumbnailPath));
                } catch(NotSupportedException) {
                    Im_Thumbnail.Source = new BitmapImage(new Uri("../Resources/NoThumbnail.png", UriKind.Relative));
                }
            } else {
                Im_Thumbnail.Source = new BitmapImage(new Uri("../Resources/NoThumbnail.png", UriKind.Relative));
            }
            if(bm.Id == -1) {
                Im_Thumbnail.MouseUp += host.BmDP_Show;
            } else {
                Im_Thumbnail.MouseLeftButtonUp += host.BmDP_Show;
                Im_Thumbnail.MouseRightButtonUp += host.OpenBmListing;
            }

            Gr_Grid.Tag = bm;
            TBl_Title.Text = bm.Title;
            TBl_Caption.Text = (bm.Id != -1 ? bm.Id.ToString() + " | " + bm.Artist : _e("MainWindow_unsubmitted") + " | " + bm.Artist);
            if(bm.Creator != "Unknown") {
                TBl_Caption.Text += " | " + bm.Creator;
            }
        }
    }
}
