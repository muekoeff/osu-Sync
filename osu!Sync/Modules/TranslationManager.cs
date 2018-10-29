using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace osuSync.Modules {
    static class TranslationManager {
        public class Language {
            public string Code { get; set; }
            public string DisplayName { get; set; }
            public string DisplayName_en { get; set; }
            public string Path { get; set; }
        }

        public static ResourceDictionary translationHolder;
        public static Dictionary<string, Language> translationList = new Dictionary<string, Language>();

        /// <param name="text">English string to translate</param>
        /// <returns>Translation of <paramref>Text</paramref></returns>
        public static string _e(string text) {
            try {
                return System.Windows.Application.Current.FindResource(text).ToString();
            } catch(ResourceReferenceKeyNotFoundException) {
                MessageBox.Show("The application just tried to load a text (= string) which isn't registered.\n" +
                    "Normally, this shouldn't happen.\n\n" +
                    "Please report this by using the Feedback-box in the settings, contacting me using the link in the about window, reporting an issue on GitHub, or contacting me on the osu!Forum.\n\n" +
                    "// Additional information:\n" +
                    text, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return "[Missing:" + text + "]";
            }
        }

        public static Language TranslationGetMeta(string filePath) {
            try {
                XmlReader xmlRead = XmlReader.Create(filePath);
                ResourceDictionary thisTranslationHolder = (ResourceDictionary)XamlReader.Load(xmlRead);
                xmlRead.Close();

                if(thisTranslationHolder.Contains("Meta_langCode")) {
                    Language resLanguage = new Language();
                    if(thisTranslationHolder.Contains("Meta_langCode"))
                        resLanguage.Code = thisTranslationHolder["Meta_langCode"].ToString();
                    if(thisTranslationHolder.Contains("Meta_langName"))
                        resLanguage.DisplayName = thisTranslationHolder["Meta_langName"].ToString();
                    if(thisTranslationHolder.Contains("Meta_langNameEn"))
                        resLanguage.DisplayName_en = thisTranslationHolder["Meta_langNameEn"].ToString();
                    return resLanguage;
                } else {
                    return null;
                }
            } catch(Exception) {
                return null;
            }
        }

        public static bool TranslationLoad(string filePath) {
            try {
                XmlReader xmlRead = XmlReader.Create(filePath);
                ResourceDictionary thisTranslationHolder = (ResourceDictionary)XamlReader.Load(xmlRead);
                xmlRead.Close();

                if(thisTranslationHolder.Contains("Meta_langCode")) {
                    GlobalVar.appSettings.Tool_Language = thisTranslationHolder["Meta_langCode"].ToString();
                    GlobalVar.appSettings.Tool_LanguagePath = filePath;
                    if(translationHolder != null)
                        System.Windows.Application.Current.Resources.MergedDictionaries.Remove(translationHolder);
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(thisTranslationHolder);
                    translationHolder = thisTranslationHolder;
                    return true;
                } else {
                    MessageBox.Show("Invalid/Incompatible language package.", GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            } catch(Exception ex) {
                MessageBox.Show("Unable to load language package.\n\n" +
                    "// Details:\n" +
                    "FilePath: " + filePath + "\n" +
                    "Exception: " + ex.Message, GlobalVar.appName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static Dictionary<string, Language> TranslationMap(string rootPath) {
            Dictionary<string, Language> result = new Dictionary<string, Language>();
            foreach(string thisFile in Directory.EnumerateFiles(rootPath)) {
                string thisFileName = Path.GetFileNameWithoutExtension(thisFile);
                if(new Regex("^[a-z]{2}(?:_)[A-Z]{2}$").Match(thisFileName).Success) {
                    Language resLanguage = new Language();
                    Language langMeta = TranslationGetMeta(thisFile);
                    if(langMeta != null) {
                        resLanguage = langMeta;
                    } else {
                        resLanguage.Code = thisFileName;
                    }
                    resLanguage.Path = thisFile;
                    result.Add(thisFileName, resLanguage);
                    if(!result.ContainsKey(thisFileName.Substring(0, 2)))
                        result.Add(thisFileName.Substring(0, 2), resLanguage);
                }
            }
            return result;
        }
    }
}
