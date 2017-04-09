using osuSync.Extensions.UserControls;
using osuSync.Interfaces.UserControls;
using osuSync.Models;
using System.Windows.Controls;
using System.Windows.Media;

namespace osuSync.UserControls {

    public partial class BeatmapItem_Exporter : UserControl, IBeatmapItem {
        public Beatmap Beatmap { get; private set; }

        public BeatmapItem_Exporter(Beatmap bm, MainWindow host) {
            Beatmap = bm;
            InitializeComponent();

            CB_IsSelected.IsChecked = (Beatmap.Id != -1);
            CB_IsSelected.IsEnabled = (Beatmap.Id != -1);
            CB_IsSelected.Checked += host.Exporter_AddBmToSel;
            CB_IsSelected.Unchecked += host.Exporter_RemoveBmFromSel;

            this.LoadThumbnail(Im_Thumbnail, host);

            Re_DecoBorder.Fill = (Beatmap.Id != -1 ? (SolidColorBrush)FindResource("GreenLightBrush") : (SolidColorBrush)FindResource("GrayLightBrush"));
            if(Beatmap.Id == -1) {
                Re_DecoBorder.MouseUp += host.Exporter_DetermineWheterAddOrRemove;
            }

            TBl_Caption.Text = (Beatmap.Id != -1 ? Beatmap.Id.ToString() + " | " + Beatmap.Artist : GlobalVar._e("MainWindow_unsubmittedBeatmapCantBeExported") + " | " + Beatmap.Artist)
                + (Beatmap.Creator != "Unknown" ? TBl_Caption.Text += " | " + Beatmap.Creator : "");
            TBl_Title.Text = bm.Title;
            if(Beatmap.Id == -1) {
                TBl_Title.MouseUp += host.Exporter_DetermineWheterAddOrRemove;
            }
        }
    }
}
