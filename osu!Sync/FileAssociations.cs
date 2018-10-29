using System;
using System.Reflection;
using System.Windows;
using static osuSync.GlobalVar;

namespace osuSync {
    static class FileExtensions {
        public class FileExtensionDefinition {
            public string fileExtension;
            public string className;
            public string description;
            public string iconPath;

            public FileExtensionDefinition(string fileExtension, string className, string description, string iconPath) {
                this.fileExtension = fileExtension;
                this.className = className;
                this.description = description;
                this.iconPath = iconPath;
            }
        }

        public static FileExtensionDefinition[] app_fileExtensions = {
            new FileExtensionDefinition(".nw520-osbl", "naseweis520.osuSync.osuBeatmapList", "MainWindow_fileext_osbl", "\"" + Assembly.GetExecutingAssembly().Location.ToString() + "\",2"),
            new FileExtensionDefinition(".nw520-osblx", "naseweis520.osuSync.compressedOsuBeatmapList", "MainWindow_fileext_osblx", "\"" + Assembly.GetExecutingAssembly().Location.ToString() + "\",1"),
        };

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, int uFlags, int dwItem1, int dwItem2);

        public static bool FileAssociationCreate(string extension, string className, string description, string iconPath, string exeProgram) {
            const int SHCNE_ASSOCCHANGED = 0x8000000;
            const int SHCNF_IDLIST = 0;
            if(extension.Substring(0, 1) != ".")
                extension = "." + extension;
            Microsoft.Win32.RegistryKey key1 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key2 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key3 = default(Microsoft.Win32.RegistryKey);
            Microsoft.Win32.RegistryKey key4 = default(Microsoft.Win32.RegistryKey);
            try {
                key1 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension);
                key1.SetValue("", className);
                key2 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className);
                key2.SetValue("", description);
                key3 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className + "\\shell\\open\\command");
                key3.SetValue("", "\"" + exeProgram + "\" -openFile=\"%1\"");
                key4 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(className + "\\DefaultIcon");
                key4.SetValue("", iconPath);
            } catch(Exception e) {
                MessageBox.Show(e.Message, "Debug | " + appName, MessageBoxButton.OK);
                return false;
            }
            if(key1 != null)
                key1.Close();
            if(key2 != null)
                key2.Close();
            if(key3 != null)
                key3.Close();
            if(key4 != null)
                key4.Close();
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return true;
        }

        public static bool FileAssociationDelete(string extension, string className) {
            const int SHCNE_ASSOCCHANGED = 0x8000000;
            const int SHCNF_IDLIST = 0;
            if(extension.Substring(0, 1) != ".")
                extension = "." + extension;

            try {
                if((Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension) != null))
                    Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension);
                if((Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(className) != null))
                    Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(className);
            } catch(Exception e) {
                MessageBox.Show(_e("GlobalVar_sorrySomethingWentWrong"), appName, MessageBoxButton.OK);
                MessageBox.Show(e.Message, "Debug |  " + appName, MessageBoxButton.OK);
                return false;
            }

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return true;
        }

        public static bool FileAssociationsCreate() {
            bool registrationErrors = false;
            foreach(FileExtensionDefinition thisExtension in app_fileExtensions) {
                if(!FileAssociationCreate(thisExtension.fileExtension, thisExtension.className, _e(thisExtension.description), thisExtension.iconPath, Assembly.GetExecutingAssembly().Location.ToString())) {
                    registrationErrors = true;
                    break;
                }
            }
            if(!registrationErrors) {
                MessageBox.Show(_e("MainWindow_extensionDone"), appName, MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            } else {
                MessageBox.Show(_e("MainWindow_extensionFailed"), appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool FileAssociationsDelete() {
            bool registrationErrors = false;
            foreach(FileExtensionDefinition thisExtension in app_fileExtensions) {
                if(!FileAssociationDelete(thisExtension.fileExtension, thisExtension.className)) {
                    registrationErrors = true;
                    break;
                }
            }
            if(registrationErrors) {
                MessageBox.Show(_e("MainWindow_extensionDeleteFailed"), appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            } else {
                return true;
            }
        }
    }
}
