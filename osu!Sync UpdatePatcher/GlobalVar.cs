using System;
using System.IO;

namespace osuSync_UpdatePatcher {
    static class GlobalVar {

        public static string[] startupArgs;

		public static string WriteCrashLog(Exception ex) {
			if(!Directory.Exists(Path.GetTempPath() + "naseweis520/osu!Sync/Crashes".Replace('/', Path.DirectorySeparatorChar)))
				Directory.CreateDirectory(Path.GetTempPath() + "naseweis520/osu!Sync/Crashes".Replace('/', Path.DirectorySeparatorChar));
			string crashFile = Path.GetTempPath() + "naseweis520/osu!Sync/Crashes/".Replace('/', Path.DirectorySeparatorChar) + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
			using(StreamWriter file = new StreamWriter(crashFile, false)) {
				string content = "=====   osu!Sync Crash | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   =====\n\n" + 
                    "// Information\n" +
                    "An exception occured in osu!Sync. If this problem persists please report it using the Feedback-window, on GitHub or on the osu!Forum.\n" +
                    "When reporting please try to describe as detailed as possible what you've done and how the applicationen reacted.\n" + 
                    @"GitHub: http://j.mp/1PDuDFp   |   osu!Forum: http://j.mp/1PDuCkK" + "\n\n" +
                    "// Exception\n" +
                    ex.ToString();
				file.Write(content);
				file.Close();
			}
			return crashFile;
		}
	}
}
