using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Vocaluxe.Base
{
    struct SLanguage
    {
        public string Name;
        public string LanguageFilePath;

        public Hashtable Texts;
    }

    static class CLanguage
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<SLanguage> _Languages;
        private static CHelper Helper = new CHelper();
        private static int _CurrentLanguage = 0;
        private static int _FallbackLanguage = 0;

        public static int Language
        {
            get { return _CurrentLanguage; }
            set
            {
                if (value >= 0 && value < _Languages.Count)
                    _CurrentLanguage = value;
            }
        }

        public static List<string> GetLanguages()
        {
            List<string> Languages = new List<string>();

            for (int i = 0; i < _Languages.Count; i++)
            {
                Languages.Add(_Languages[i].Name);
            }

            return Languages;
        }

        public static void Init()
        {
            _Languages = new List<SLanguage>();
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderLanguages, "*.xml", true, true));
            
            foreach (string file in files)
	        {
		        LoadLanguageFile(file);
	        }
        }

        public static bool SetLanguage(string Language)
        {
            for (int i = 0; i < _Languages.Count; i++)
            {
                if (_Languages[i].Name == Language)
                {
                    _CurrentLanguage = i;
                    return true;
                }
            }
            return false;
        }

        public static string Translate(string KeyWord)
        {
            if (KeyWord == null)
                return null;

            if (KeyWord.Length < 3)
                return KeyWord;

            string tag = KeyWord.Substring(0, 3);
            if (tag != "TR_")
                return KeyWord;

            string result = String.Empty;
            try
            {
                result = (string)_Languages[_CurrentLanguage].Texts[KeyWord];
            }
            catch (Exception)
            {
                ;
            }

            if (result == null)
            {
                // keyword not found, try fallback-language
                try
                {
                    result = (string)_Languages[_FallbackLanguage].Texts[KeyWord];
                }
                catch (Exception)
                {
                    ;
                }

                if (result == null)
                    return KeyWord;
            }

            return result;
        }

        public static bool TranslationExists(string KeyWord)
        {
            if (KeyWord.Length < 3)
                return false;

            string tag = KeyWord.Substring(0, 3);
            if (tag != "TR_")
                return false;

            string result = String.Empty;
            try
            {
                result = (string)_Languages[_CurrentLanguage].Texts[KeyWord];
            }
            catch (Exception)
            {
                ;
            }

            if (result == null)
            {
                // keyword not found, try fallback-language
                try
                {
                    result = (string)_Languages[_FallbackLanguage].Texts[KeyWord];
                }
                catch (Exception)
                {
                    ;
                }

                if (result == null)
                    return false;
            }

            return true;
        }

        private static void LoadLanguageFile(string FileName)
        {
            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;
            SLanguage lang = new SLanguage();
            lang.LanguageFilePath = Path.Combine(CSettings.sFolderLanguages, FileName);

            try
            {
                xPathDoc = new XPathDocument(lang.LanguageFilePath);
                navigator = xPathDoc.CreateNavigator();
                loaded = true;
            }
            catch (Exception e)
            {
                loaded = false;
                if (navigator != null)
                    navigator = null;

                if (xPathDoc != null)
                    xPathDoc = null;

                CLog.LogError("Error opening Language File " + FileName + ": " + e.Message); 
            }

            if (loaded)
            {
                string value = string.Empty;
                if (CHelper.GetValueFromXML("//root/Info/Name", navigator, ref value, value))
                {
                    lang.Name = value;

                    if (lang.Name == CSettings.FallbackLanguage)
                        _FallbackLanguage = _Languages.Count;

                    lang.Texts = new Hashtable();

                    List<string> texts = CHelper.GetValuesFromXML("Texts", navigator);
                    for (int i = 0; i < texts.Count; i++)
                    {
                        if (CHelper.GetValueFromXML("//root/Texts/" + texts[i], navigator, ref value, value))
                        {
                            try
                            {
                                lang.Texts.Add(texts[i], value);
                            }
                            catch (Exception e)
                            {
                                CLog.LogError("Error reading Language File " + FileName + ": " + e.Message);
                            }
                        }
                        
                    }

                    _Languages.Add(lang);
                }
            }
        }
    }
}
