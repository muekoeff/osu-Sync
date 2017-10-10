using Microsoft.VisualBasic;
using osuSync.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;

namespace osuSync {
    partial class Application {
#if !DEBUG
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
			e.Handled = true;
			MessageBox.Show("B-ba-baka     ｡･ﾟﾟ･(>д<)･ﾟﾟ･｡\n\n" +
                "Sorry, it looks like an exception occured.\n" + 
                "osu!Sync is going to shutdown now.", "Debug | " + GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
			Process.Start(GlobalVar.CrashLogWrite(e.Exception));
			try {
                Environment.Exit(1);
			} catch (Exception) {}
		}
#else
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { }
#endif

        private void Application_Startup(object sender, StartupEventArgs e) {
			// Save Startup Arguments
			if(e.Args.Length != 0)
                GlobalVar.appStartArgs = e.Args;
			if(GlobalVar.appStartArgs == null) {
				FocusAndShutdown();
			} else {
				if(!GlobalVar.appStartArgs.Contains("--ignoreInstances")) {
					FocusAndShutdown();
				}
			}

            // Check if elevated
            WindowsPrincipal winPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            GlobalVar.tool_isElevated = winPrincipal.IsInRole(WindowsBuiltInRole.Administrator);

			// Load language package
			if(Directory.Exists(System.Windows.Forms.Application.StartupPath + "/data/l10n".Replace('/', Path.DirectorySeparatorChar))) {
                TranslationManager.translationList = TranslationManager.TranslationMap(System.Windows.Forms.Application.StartupPath + "/data/l10n".Replace('/', Path.DirectorySeparatorChar));
				if(TranslationManager.translationList.ContainsKey(System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_"))) {
                    TranslationManager.TranslationLoad(TranslationManager.translationList[System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 5).Replace("-", "_")].Path);
				} else if(TranslationManager.translationList.ContainsKey(System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)) & !(System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2) == "en")) {
                    // Prevent loading of en_UD
                    TranslationManager.TranslationLoad(TranslationManager.translationList[System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2)].Path);
                }
            }
		}

		private void FocusAndShutdown() {
			if(Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Count() > 1) {
				try {
					Process process = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).First();
					Interaction.AppActivate(process.Id);
					ShowWindow(process.MainWindowHandle, 1);
				} catch(ArgumentException) {}
                Environment.Exit(1);
				return;
			}
		}

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int ShowWindow(IntPtr handle, int nCmdShow);
    }
}
