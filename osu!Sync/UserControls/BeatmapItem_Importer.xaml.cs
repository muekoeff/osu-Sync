using osuSync.Extensions.UserControls;
using osuSync.Interfaces.UserControls;
using osuSync.Models;
using System.Windows.Controls;
using System.Windows.Media;

namespace osuSync.UserControls {

    public partial class BeatmapItem_Importer : UserControl, IBeatmapItem {
        public Beatmap Beatmap { get; private set; }
        public bool IsInstalled { get; private set; }

        public BeatmapItem_Importer(Beatmap bm, MainWindow host, bool isInstalled) {
            Beatmap = bm;
            IsInstalled = isInstalled;
            InitializeComponent();

            CB_IsSelected.IsChecked = !isInstalled;
            CB_IsSelected.IsEnabled = !isInstalled;
            CB_IsSelected.Checked += host.Importer_AddBmToSel;
            CB_IsSelected.Unchecked += host.Importer_RemoveBmFromSel;

            this.LoadThumbnail(Im_Thumbnail, host);

            Re_DecoBorder.Fill = (isInstalled ? (SolidColorBrush)FindResource("GreenLightBrush") : (SolidColorBrush)FindResource("RedLightBrush"));

            TBl_Caption.Text = bm.Id.ToString() + " | " + bm.Artist + (bm.Creator != "Unknown" ? " | "
                + bm.Creator : "");
            TBl_Title.Text = bm.Title;
        }
    }
}
