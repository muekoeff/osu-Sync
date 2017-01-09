using System.Windows;

namespace osuSync {

    public partial class Window_Welcome {
        
		private bool shutdownAfterClose = true;

        public Window_Welcome() {
            InitializeComponent();
        }

		private void Bu_Continue_Click(object sender, RoutedEventArgs e) {
            shutdownAfterClose = false;
			((MainWindow)System.Windows.Application.Current.MainWindow).Activate();
			Close();
		}

		private void Window_Welcome_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if(shutdownAfterClose)
				System.Windows.Application.Current.Shutdown();
		}
	}
}
