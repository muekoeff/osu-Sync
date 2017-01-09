using Ionic.Zip;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace osuSync_UpdatePatcher {
    partial class MainWindow {

        private bool arg_deletePkgAfter = true;
        private string arg_targetVer;
        private string arg_srcVer;
        private string arg_pathToApp;
        private string arg_pathToUpdatePkg;
        private KillAfterCloseCode killAfterClose = KillAfterCloseCode.None;
        private int updater_zipCount;
        private int updater_zipCurrentCount;
        private BackgroundWorker withEventsFieldWorker = new BackgroundWorker {
            WorkerReportsProgress = true
        };
        private BackgroundWorker worker {
            get { return withEventsFieldWorker; }
            set {
                if(withEventsFieldWorker != null) {
                    withEventsFieldWorker.DoWork -= Worker_DoWork;
                    withEventsFieldWorker.ProgressChanged -= Worker_ProgressChanged;
                }
                withEventsFieldWorker = value;
                if(withEventsFieldWorker != null) {
                    withEventsFieldWorker.DoWork += Worker_DoWork;
                    withEventsFieldWorker.ProgressChanged += Worker_ProgressChanged;
                }
            }
        }

        private enum KillAfterCloseCode {
            None,
            DeleteSelf,
            OpenFolder
        }

        public MainWindow() {
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;

        }

        private void Action_ZipProgress(object sender, ExtractProgressEventArgs e) {
            if(e.TotalBytesToTransfer != 0) {
                int percentage = Convert.ToInt32(e.BytesTransferred / e.TotalBytesToTransfer * 100);
                worker.ReportProgress(0, "Unzipping... | " + percentage + " %");
                worker.ReportProgress(0, "[PROGRESSBAR] " + (updater_zipCount * 100) + ";" + (updater_zipCurrentCount * 100 + percentage));
            }
        }

        private void Button_closeUpdater_Click(object sender, RoutedEventArgs e) {
            Environment.Exit(1);
        }

        private void Button_startOsusync_Click(object sender, RoutedEventArgs e) {
            Process.Start(arg_pathToApp + Path.DirectorySeparatorChar + "osu!Sync.exe");
            Environment.Exit(1);
        }

        private void MainWindow_Closed(object sender, EventArgs e) {
            switch(killAfterClose) {
                case KillAfterCloseCode.DeleteSelf:
                    // Delete self
                    ProcessStartInfo delSelfProcess = new ProcessStartInfo {
                        Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"",
                        CreateNoWindow = true,
                        FileName = "cmd.exe",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(delSelfProcess);
                    break;
                case KillAfterCloseCode.OpenFolder:
                    // Open folder
                    ProcessStartInfo thisProcess = new ProcessStartInfo() {
                        Arguments = "/select," + System.Reflection.Assembly.GetExecutingAssembly().Location,
                        FileName = "Explorer.exe"
                    };
                    Process.Start(thisProcess);
                    break;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            TextBlock_CurrentProcess.Text = "Preparing...";
            ProgressBar.Visibility = Visibility.Visible;

            // Assign variables
            worker.ReportProgress(0, "Reading arguments...");

            // Check whether start up arguments are set (program has been run by osu!Sync) or not (ran by user)
            if(GlobalVar.startupArgs == null) {
                System.Windows.Forms.FolderBrowserDialog chooser_pathToApp = new System.Windows.Forms.FolderBrowserDialog {
                    Description = "Please specify the path the the root folder of osu!Sync (pathToApp)"
                };
                System.Windows.Forms.OpenFileDialog chooser_pathToUpdate = new System.Windows.Forms.OpenFileDialog {
                    Filter = "Update Packages (*.zip)|*.zip|All files (*.*)|*.*",
                    Title = "Please specify the path to the update package (pathToUpdate)"
                };
                arg_deletePkgAfter = false;
                arg_targetVer = "another manually chosen version";
                if(chooser_pathToApp.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    arg_pathToApp = chooser_pathToApp.SelectedPath;
                } else {
                    Environment.Exit(1);
                    return;
                }
                if(chooser_pathToUpdate.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    arg_pathToUpdatePkg = chooser_pathToUpdate.FileName;
                } else {
                    Environment.Exit(1);
                    return;
                }
                arg_srcVer = "a manually chosen version";
            } else {
                foreach(string i in GlobalVar.startupArgs) {
                    string[] thisSplit = i.Split(Convert.ToChar("="));
                    switch(thisSplit[0]) {
                        case "-deletePackageAfter":
                            arg_deletePkgAfter = Convert.ToBoolean(thisSplit[1]);
                            break;
                        case "-destinationVersion":
                            arg_targetVer = thisSplit[1];
                            break;
                        case "-pathToApp":
                            arg_pathToApp = thisSplit[1];
                            break;
                        case "-pathToUpdate":
                            arg_pathToUpdatePkg = thisSplit[1];
                            break;
                        case "-sourceVersion":
                            arg_srcVer = thisSplit[1];
                            break;
                        default:
                            worker.ReportProgress(0, "Update failed!");
                            MessageBox.Show("Whoops, one of the arguments seems to be invalid.\n" +
                                "Update failed.", "Dialog | osu!Sync Software Update Patcher", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            worker.ReportProgress(0, "[CANCEL]");
                            return;
                    }
                }
            }
            worker.RunWorkerAsync();
        }

        private void StackPanel_Paths_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            TextBlock_Paths.Visibility = Visibility.Collapsed;
            TextBlock_PathsFull.Visibility = Visibility.Visible;
        }

        private void StackPanel_Paths_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
            TextBlock_Paths.Visibility = Visibility.Visible;
            TextBlock_PathsFull.Visibility = Visibility.Collapsed;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            try {
                // Check if osu!Sync is running
                worker.ReportProgress(0, "Checking, if osu!Sync is running...");
                Process[] mainProcess = Process.GetProcessesByName("osu!Sync");
                if(mainProcess.Length > 0) {
                    worker.ReportProgress(0, "Killing osu!Sync...");
                    mainProcess[0].Kill();
                    mainProcess[0].WaitForExit();
                }

                // Set VersionInfo
                worker.ReportProgress(0, "[VERSIONINFO] Update from " + arg_srcVer + " to " + arg_targetVer);
                worker.ReportProgress(0, "[PATHS] " + arg_pathToUpdatePkg + ";" + arg_pathToApp);

                // Check if files exist
                worker.ReportProgress(0, "Checking...");
                if(!Directory.Exists(arg_pathToApp)) {
                    worker.ReportProgress(0, "Update failed!");
                    MessageBox.Show("The path to the application can't be found.", "Dialog | osu!Sync Software Update Patcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    worker.ReportProgress(0, "[CANCEL]");
                    return;
                } else if(!File.Exists(arg_pathToUpdatePkg)) {
                    worker.ReportProgress(0, "Update failed!");
                    MessageBox.Show("The path to the update package can't be found.", "Dialog | osu!Sync Software Update Patcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    worker.ReportProgress(0, "[CANCEL]");
                    return;
                }

                // Unzip
                worker.ReportProgress(0, "Unzipping...");
                using(ZipFile thisZipper = ZipFile.Read(arg_pathToUpdatePkg)) {
                    thisZipper.ExtractProgress += Action_ZipProgress;
                    updater_zipCount = thisZipper.Count;
                    foreach(ZipEntry ZipperEntry in thisZipper) {
                        ZipperEntry.Extract(arg_pathToApp, ExtractExistingFileAction.OverwriteSilently);
                        updater_zipCurrentCount += 1;
                    }
                }

                // Delete update package
                if(arg_deletePkgAfter) {
                    worker.ReportProgress(0, "Deleting update package...");
                    File.Delete(arg_pathToUpdatePkg);
                }

                worker.ReportProgress(0, "[FINISHED]");
            } catch(Exception ex) {
                worker.ReportProgress(0, "[FAILED] " + ex.Message);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if(e.UserState.ToString().StartsWith("[VERSIONINFO] ")) {
                TextBlock_VersionInfo.Text = e.UserState.ToString().Substring("[VERSIONINFO] ".Length);
            } else if(e.UserState.ToString().StartsWith("[PATHS] ")) {
                string[] textSplit = e.UserState.ToString().Substring("[VERSIONINFO] ".Length).Split(Convert.ToChar(";"));
			    TextBlock_Paths.Text = textSplit[0];
                TextBlock_PathsFull.Text = "Update Package: " + textSplit[0] + "\n" +
                    "osu!Sync: " + textSplit[1];
            } else if(e.UserState.ToString().StartsWith("[PROGRESSBAR] ")) {
                string[] textSplit = e.UserState.ToString().Substring("[VERSIONINFO] ".Length).Split(Convert.ToChar(";"));
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Maximum = Convert.ToDouble(textSplit[0]);
                ProgressBar.Value = Convert.ToDouble(textSplit[1]);
		    } else if(e.UserState.ToString().StartsWith("[FAILED] ")) {
			    string text = e.UserState.ToString().Substring("[FAILED] ".Length);
                killAfterClose = KillAfterCloseCode.OpenFolder;
			    TextBlock_CurrentProcess.Text = "Update failed!";
			    Button_closeUpdater.Visibility = Visibility.Visible;
			    ProgressBar.Visibility = Visibility.Hidden;
			    MessageBox.Show("An error occured!\n\n" + 
                    "// Message:\n" + 
                    text, "Debug | osu!Sync", MessageBoxButton.OK, MessageBoxImage.Error);
		    } else if(e.UserState.ToString() == "[CANCEL]") {
			    killAfterClose = KillAfterCloseCode.OpenFolder;
			    Button_closeUpdater.Visibility = Visibility.Visible;
                Button_startOsusync.IsEnabled = false;
                Button_startOsusync.Visibility = Visibility.Visible;
			    ProgressBar.Visibility = Visibility.Hidden;
			    TextBlock_CurrentProcess.Text = "Aborted!";
		    } else if(e.UserState.ToString() == "[FINISHED]") {
			    killAfterClose = KillAfterCloseCode.DeleteSelf;
			    TextBlock_CurrentProcess.Text = "Update successfully finished!";
			    Button_closeUpdater.Visibility = Visibility.Visible;
			    Button_startOsusync.Visibility = Visibility.Visible;
			    ProgressBar.Visibility = Visibility.Hidden;
		    } else {
			    TextBlock_CurrentProcess.Text = e.UserState.ToString();
		    }
	    }
    }
}