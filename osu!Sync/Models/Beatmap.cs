using System.IO;

namespace osuSync.Models {

    public class Beatmap {
        public enum OnlineApprovedStatuses {
            Graveyard = -2,
            WIP = -1,
            Pending = 0,
            Ranked = 1,
            Approved = 2,
            Qualified = 3
        }

        public string Artist { get; set; }
        public string Creator { get; set; }
        public int Id { get; set; }
        public bool IsUnplayed { get; set; }
        public string Md5 { get; set; }
        public byte RankedStatus { get; set; }
        public string SongSource { get; set; }
        public string SongTags { get; set; }
        public string Title { get; set; }

        public string ThumbnailPath {
            get {
                if(File.Exists(GlobalVar.appSettings.osu_Path + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "bt" + Path.DirectorySeparatorChar + Id + "l.jpg")) {
                    return GlobalVar.appSettings.osu_Path + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "bt" + Path.DirectorySeparatorChar + Id + "l.jpg";
                } else if(File.Exists(GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + Id + ".jpg")) {
                    return GlobalVar.appTempPath + Path.DirectorySeparatorChar + "Cache" + Path.DirectorySeparatorChar + "Thumbnails" + Path.DirectorySeparatorChar + Id + ".jpg";
                } else if(File.Exists(GlobalVar.appSettings.osu_Path + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "bt" + Path.DirectorySeparatorChar + Id + ".jpg")) {
                    return GlobalVar.appSettings.osu_Path + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "bt" + Path.DirectorySeparatorChar + Id + ".jpg";
                } else {
                    return null;
                }
            }
        }
    }
}
