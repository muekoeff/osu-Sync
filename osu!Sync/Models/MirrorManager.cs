using Newtonsoft.Json;
using osuSync.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace osuSync.Models {
    class MirrorManager {

        public const string DEFAULT_MIRROR_ID = "osu.uu.gl";

        public static SortedDictionary<string, DownloadMirror> app_mirrors = new SortedDictionary<string, DownloadMirror>() {
            {
                "osu.hexide.com",
                new DownloadMirror {
                    DisplayName = "Hexide",
                    DownloadUrl = "https://osu.hexide.com/beatmaps/%0/download",
                    Id = "osu.hexide.com",
                    WebUrl = "https://osu.hexide.com/doc"
                }
            },
            {
                 "ripple.moe",
                 new DownloadMirror {
                     DisplayName = "Ripple Mirror",
                     DownloadUrl = "https://storage.ripple.moe/d/%0",
                     Id = "ripple.moe",
                     WebUrl = "https://ripple.moe/"
                 }
            },
            {
                "osu.uu.gl",
                new DownloadMirror {
                    DisplayName = "osu.uu.gl",
                    DownloadUrl = "http://osu.uu.gl/s/%0",
                    Id = "osu.uu.gl",
                    WebUrl = "http://osu.uu.gl/"
                }
            }
        };

        public static void CheckMirror() {
            // Check, if set mirror really exists
            if(!app_mirrors.ContainsKey(GlobalVar.appSettings.Tool_ChosenDownloadMirror)) {
                // @TODO
                MessageBox.Show("Failed to load mirror, reset to " + app_mirrors[DEFAULT_MIRROR_ID].DisplayName + ".", GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                GlobalVar.appSettings.Tool_ChosenDownloadMirror = app_mirrors[DEFAULT_MIRROR_ID].Id;
                GlobalVar.appSettings.SaveSettings();
            }
        }

        public static void LoadMirrors() {
            var externalMirrors = loadMirrorsFromFile(System.Windows.Forms.Application.StartupPath + "/data/mirrors.json".Replace('/', Path.DirectorySeparatorChar), true);
            if(externalMirrors != null) {
                app_mirrors = app_mirrors.Merge(externalMirrors, true);
            }
        }

        private static SortedDictionary<string, DownloadMirror> loadMirrorsFromFile(string path, bool deleteOnError = false) {
            if(File.Exists(path)) {
                try {
                    // Parse file
                    return JsonConvert.DeserializeObject<SortedDictionary<string, DownloadMirror>>(File.ReadAllText(path));
                } catch(Exception) {
                    if(deleteOnError) {
                        // @TODO
                        MessageBox.Show("Invalid mirror definition file.", GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                        File.Delete(path);
                    }
                    return null;
                }
            } else {
                return null;
            }
        }

        public class DownloadMirror {
            public string DisplayName { get; set; }
            public string DownloadUrl { get; set; }
            public string Id { get; set; }
            public string WebUrl { get; set; }
        }
    }
}
