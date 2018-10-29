using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static osuSync.Modules.TranslationManager;

namespace osuSync.Models {
    public class BeatmapDictionary : Dictionary<int, Beatmap> {

        /// <summary>
        /// Converts the current beatmap list to CSV.
        /// </summary>
        /// <returns><code>BeatmapDictionary</code> as CSV.</returns>
        public string ConvertToCsv() {
            StringBuilder sb = new StringBuilder();
            sb.Append("sep=;\n");
            sb.Append("ID;Artist;Creator;Title\n");
            foreach(KeyValuePair<int, Beatmap> thisBm in this) {
                sb.Append(thisBm.Value.Id + ";" + "\"" + thisBm.Value.Artist + "\";\"" + thisBm.Value.Creator + "\";\"" + thisBm.Value.Title + "\"\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts the current beatmap dictionary to HTML-Code.
        /// </summary>
        /// <param name="failMessage">Potential error message</param>
        /// <returns><code>BeatmapDictionary</code> as HTML.</returns>
        public string ConvertToHtml(out string failMessage) {
            StringBuilder fail = new StringBuilder();
            StringBuilder sb_html = new StringBuilder();
            sb_html.Append("<!doctype html>\n"
                + "<html>\n"
                + "<head><meta charset=\"utf-8\"><meta name=\"author\" content=\"osu!Sync\"/><meta name=\"generator\" content=\"osu!Sync " + GlobalVar.AppVersion + "\"/><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=yes\"/><title>Beatmap List | osu!Sync</title><link rel=\"icon\" type=\"image/png\" href=\"https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Favicon.png\"/><link href=\"http://fonts.googleapis.com/css?family=Open+Sans:400,300,600,700\" rel=\"stylesheet\" type=\"text/css\" /><link href=\"https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/style.css\" rel=\"stylesheet\" type=\"text/css\"/><link rel=\"stylesheet\" type=\"text/css\" href=\"https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/css/tooltipster.css\"/></head>\n"
                + "<body>\n"
                + "<div id=\"Wrapper\">\n"
                + "\t<header><p>Beatmap List | osu!Sync</p></header>\n"
                + "\t<div id=\"Sort\"><ul><li><strong>Sort by...</strong></li><li><a class=\"SortParameter\" href=\"#Sort_Artist\">Artist</a></li><li><a class=\"SortParameter\" href=\"#Sort_Creator\">Creator</a></li><li><a class=\"SortParameter\" href=\"#Sort_SetName\">Name</a></li><li><a class=\"SortParameter\" href=\"#Sort_SetID\">Set ID</a></li></ul></div>\n"
                + "\t<div id=\"ListWrapper\">");

            foreach(KeyValuePair<int, Beatmap> thisBm in this) {
                if(thisBm.Value.Id == -1) {
                    fail.Append("\n" +
                        "* " + thisBm.Value.Id.ToString() + " / " + thisBm.Value.Artist + " / " + thisBm.Value.Title);
                } else {
                    thisBm.Value.Artist.Replace("\"", "'");
                    thisBm.Value.Creator.Replace("\"", "'");
                    thisBm.Value.Title.Replace("\"", "'");
                    sb_html.Append("\n\t\t" + "<article id=\"beatmap-" + thisBm.Value.Id + "\" data-artist=\"" + thisBm.Value.Artist + "\" data-creator=\"" + thisBm.Value.Creator + "\" data-setName=\"" + thisBm.Value.Title + "\" data-setID=\"" + thisBm.Value.Id + "\"><a class=\"DownloadArrow\" href=\"https://osu.ppy.sh/d/" + thisBm.Value.Id + "\" target=\"_blank\">&#8250;</a><h1><span title=\"Beatmap Set Name\">" + thisBm.Value.Title + "</span></h1><h2><span title=\"Beatmap Set ID\">" + thisBm.Value.Id + "</span></h2><p><a class=\"InfoTitle\" data-function=\"artist\" href=\"https://osu.ppy.sh/p/beatmaplist?q=" + thisBm.Value.Artist + "\" target=\"_blank\">Artist.</a> " + thisBm.Value.Artist + " <a class=\"InfoTitle\" data-function=\"creator\" href=\"https://osu.ppy.sh/p/beatmaplist?q=" + thisBm.Value.Creator + "\" target=\"_blank\">Creator.</a> " + thisBm.Value.Creator + " <a class=\"InfoTitle\" data-function=\"overview\" href=\"https://osu.ppy.sh/s/" + thisBm.Value.Id + "\" target=\"_blank\">Overview.</a> <a class=\"InfoTitle\" data-function=\"discussion\" href=\"https://osu.ppy.sh/s/" + thisBm.Value.Id + "#disqus_thread\" target=\"_blank\">Discussion.</a></p></article>");
                }
            }
            sb_html.Append("</div>\n"
                + "</div>\n"
                + "<footer><p>Generated with osu!Sync, an open-source tool made by <a href=\"http://nw520.de/\" target=\"_blank\">nw520</a>.</p></footer>\n"
                + "<script src=\"http://code.jquery.com/jquery-latest.min.js\"></script><script src=\"https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/Tooltipster/3.2.6/js/jquery.tooltipster.min.js\"></script><script src=\"https://dl.dropboxusercontent.com/u/62617267/Projekte/osu%21Sync/export-html/1.0.0.0/script.js\"></script>\n"
                + "</body>\n"
                + "</html>");

            failMessage = fail.ToString();
            return sb_html.ToString();
        }

        /// <summary>
        /// Converts the current beatmap dictionary to JSON.
        /// </summary>
        /// <param name="failMessage">Potential error message</param>
        /// <returns><code>BeatmapDictionary</code> as JSON.</returns>
        public string ConvertToJson(out string failMessage) {
            StringBuilder fail = new StringBuilder();
            StringBuilder fail_unsubmitted = new StringBuilder();
            StringBuilder fail_alreadyAssigned = new StringBuilder();
            Dictionary<string, Dictionary<string, string>> content = new Dictionary<string, Dictionary<string, string>> {
                {
                    "_info",
                    new Dictionary<string, string> {
                {
                    "_date",
                    DateTime.Now.ToString("yyyyMMdd")
                },
                {
                    "_version",
                    GlobalVar.AppVersion.ToString()
                }
            }
                }
            };
            foreach(KeyValuePair<int, Beatmap> thisBm in this) {
                if(thisBm.Value.Id == -1) {
                    fail_unsubmitted.Append("\n* " + thisBm.Value.Id.ToString() + " / " + thisBm.Value.Artist + " / " + thisBm.Value.Title);
                } else if(content.ContainsKey(thisBm.Value.Id.ToString())) {
                    fail_alreadyAssigned.Append("\n* " + thisBm.Value.Id.ToString() + " / " + thisBm.Value.Artist + " / " + thisBm.Value.Title);
                } else {
                    content.Add(thisBm.Value.Id.ToString(), new Dictionary<string, string> {
                        {
                            "artist",
                            thisBm.Value.Artist
                        },
                        {
                            "creator",
                            thisBm.Value.Creator
                        },
                        {
                            "id",
                            thisBm.Value.Id.ToString()
                        },
                        {
                            "title",
                            thisBm.Value.Title
                        }
                    });
                }
            }

            if(fail_unsubmitted.Length != 0)
                fail.Append("# " + _e("MainWindow_unsubmittedBeatmapSets") + "\n" +
                    _e("MainWindow_unsubmittedBeatmapCantBeExportedToThisFormat") + "\n\n" +
                    "> " + _e("MainWindow_beatmaps") + ":" + fail_unsubmitted.ToString() + "\n\n");
            if(fail_alreadyAssigned.Length != 0)
                fail.Append("# " + _e("MainWindow_idAlreadyAssigned") + "\n" + _e("MainWindow_beatmapsIdsCanBeUsedOnlyOnce") + "\n\n" +
                    "> " + _e("MainWindow_beatmaps") + ":" + fail_alreadyAssigned.ToString());

            failMessage = fail.ToString();
            return JsonConvert.SerializeObject(content);
        }

        /// <summary>
        /// Converts the current beatmap dictionary to TXT.
        /// </summary>
        /// <returns><code>BeatmapDictionary</code> as TXT.</returns>
        public string ConvertToTxt() {
            StringBuilder content = new StringBuilder();
            content.Append("// osu!Sync (" + GlobalVar.AppVersion.ToString() + ") | " + DateTime.Now.ToString("dd.MM.yyyy") + "\n\n");
            foreach(KeyValuePair<int, Beatmap> thisBm in this) {
                content.Append("# " + thisBm.Value.Id + "\n"
                    + "* Creator: \t" + thisBm.Value.Creator + "\n"
                    + "* Artist: \t" + thisBm.Value.Artist + "\n"
                    + "* ID: " + "\t\t\t" + thisBm.Value.Id + "\n"
                    + "* Title: " + "\t\t" + thisBm.Value.Title + "\n\n");
            }
            return content.ToString();
        }
    }
}
