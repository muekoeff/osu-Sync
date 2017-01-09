using System.Linq;
using System;
using System.Diagnostics;
using System.Windows;

namespace osuSync_UpdatePatcher {
    partial class Application {

		#if !DEBUG
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
			e.Handled = true;
			MessageBox.Show("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡\n\n" + 
                "Sorry, it looks like an exception occured.\n" + 
                "osu!Sync is going to shutdown now.", "Debug | osu!Sync", MessageBoxButton.OK, MessageBoxImage.Error);
			Process.Start(GlobalVar.WriteCrashLog(e.Exception));
			try {
                System.Windows.Forms.Application.Exit();
            } catch (Exception) {}
		}
		#endif

		private void Application_Startup(object sender, StartupEventArgs e) {
			// Save Startup Arguments
			if(e.Args.Length != 0)
				GlobalVar.startupArgs = e.Args;

			// Check if already running
			if(Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Count() > 1)
                System.Windows.Forms.Application.Exit();
		}
	}
}
