using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace osuSync {

    public partial class Window_About {

        public Window_About() {
            InitializeComponent();
        }

        #region "TB - TextArea"
        public void TB_Contact_MouseUp(object sender, MouseButtonEventArgs e) {
			Process.Start("mailto:team@nw520.de?subject=Contact%20|%20osu!Sync");
		}

		public void TB_Feedback_MouseUp(object sender, MouseButtonEventArgs e) {
			MainWindow.UI_ShowSettingsWindow(4);
		}

		public void TB_GitHub_MouseUp(object sender, MouseButtonEventArgs e) {
			Process.Start("https://github.com/naseweis520/osu-Sync");
		}

		public void TB_osuForum_MouseUp(object sender, MouseEventArgs e) {
			Process.Start("https://osu.ppy.sh/forum/t/270446");
		}

		public void TB_Version_MouseUp(object sender, MouseButtonEventArgs e) {
			MainWindow.UI_ShowUpdaterWindow();
		}
		#endregion
		
		public void WindowAbout_Loaded(object sender, RoutedEventArgs e) {
#if DEBUG
			TB_Version.Text = GlobalVar._e("WindowAbout_version").Replace("%0", GlobalVar.AppVersion.ToString() + " (Dev)").Replace("%1", GlobalVar.appSettings.Tool_Language);
#else
            TB_Version.Text = GlobalVar._e("WindowAbout_version").Replace("%0", GlobalVar.AppVersion.ToString()).Replace("%1", GlobalVar.appSettings.Tool_Language);
#endif
        }
	}
}
