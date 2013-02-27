using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    struct SLanguage
    {
        public string Name;
        public string LanguageFilePath;

        public Hashtable Texts;
        public List<SPartyLanguage> PartyModeTexts;
    }

    struct SPartyLanguage
    {
        public int PartyModeID;
        public Hashtable Texts;
    }

    static class CLanguage
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<SLanguage> _Languages;
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
            files.AddRange(CHelper.ListFiles(CSettings.sFolderLanguages, "*.xml", true, true));
            
            foreach (string file in files)
	        {
		        LoadLanguageFile(file);
	        }
        }

        public static bool SetLanguage(string Language)
        {
            int nr = GetLanguageNr(Language);
            if (nr != -1)
            {
                _CurrentLanguage = nr;
                return true;
            }
            return false;
        }

        public static int GetLanguageNr(string Language)
        {
            for (int i = 0; i < _Languages.Count; i++)
            {
                if (_Languages[i].Name == Language)
                    return i;
            }
            return -1;
        }

        public static string Translate(string KeyWord)
        {
            return Translate(KeyWord, -1);
        }

        public static string Translate(string KeyWord, int PartyModeID)
        {
            if (KeyWord == null)
                return "Error";

            if (KeyWord.Length < 3)
                return KeyWord;

            string tag = KeyWord.Substring(0, 3);
            if (tag != "TR_")
                return KeyWord;

            string result = null;

            int PartyModeNr = GetPartyModeNr(PartyModeID, _CurrentLanguage);
            if (PartyModeID != -1)
            {
                if (PartyModeNr != -1)
                {
                    try
                    {
                        result = (string)_Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts[KeyWord];
                    }
                    catch { }
                }

                if (result == null && (PartyModeNr = GetPartyModeNr(PartyModeID, _FallbackLanguage)) != -1)
                {
                    try
                    {
                        result = (string)_Languages[_FallbackLanguage].PartyModeTexts[PartyModeNr].Texts[KeyWord];
                    }
                    catch { }
                }
            }


            if (result == null)
            {
                try
                {
                    result = (string)_Languages[_CurrentLanguage].Texts[KeyWord];
                }
                catch { }
            }

            if (result == null)
            {
                // keyword not found, try fallback-language
                try
                {
                    result = (string)_Languages[_FallbackLanguage].Texts[KeyWord];
                }
                catch { }

                if (result == null)
                    return KeyWord;
            }

            return result;
        }

        public static bool TranslationExists(string KeyWord)
        {
            return TranslationExists(KeyWord, -1);
        }

        public static bool TranslationExists(string KeyWord, int PartyModeID)
        {
            if (KeyWord == null)
                return false;

            if (KeyWord.Length < 3)
                return false;

            string tag = KeyWord.Substring(0, 3);
            if (tag != "TR_")
                return false;

            string result = String.Empty;

            int PartyModeNr = GetPartyModeNr(PartyModeID, _CurrentLanguage);
            if (PartyModeID != -1)
            {
                if (PartyModeNr != -1)
                {
                    try
                    {
                        result = (string)_Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts[KeyWord];
                    }
                    catch { }
                }

                if (result == null && (PartyModeNr = GetPartyModeNr(PartyModeID, _FallbackLanguage)) != -1)
                {
                    try
                    {
                        result = (string)_Languages[_FallbackLanguage].PartyModeTexts[PartyModeNr].Texts[KeyWord];
                    }
                    catch { }
                }
            }


            if (result == null)
            {
                try
                {
                    result = (string)_Languages[_CurrentLanguage].Texts[KeyWord];
                }
                catch { }
            }

            if (result == null)
            {
                // keyword not found, try fallback-language
                try
                {
                    result = (string)_Languages[_FallbackLanguage].Texts[KeyWord];
                }
                catch { }

                if (result == null)
                    return false;
            }

            return true;
        }

        public static bool LoadPartyLanguageFiles(int PartyModeID, string Path)
        {
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(Path, "*.xml", true, true));

            foreach (string file in files)
            {
                if (!LoadPartyLanguageFile(PartyModeID, file))
                    return false;
            }
            return true;
        }

        private static bool LoadPartyLanguageFile(int PartyModeID, string file)
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(file);
            if (xmlReader == null)
                return false;

            string value = string.Empty;
            if (xmlReader.GetValue("//resources/string[@name='language']", ref value, value))
            {
                int nr = GetLanguageNr(value);

                if (nr == -1)
                    return true;

                SPartyLanguage lang = new SPartyLanguage();
                lang.PartyModeID = PartyModeID;
                lang.Texts = new Hashtable();

                List<string> texts = xmlReader.GetAttributes("resources", "name");
                for (int i = 0; i < texts.Count; i++)
                {
                    if (xmlReader.GetValue("//resources/string[@name='" + texts[i] + "']", ref value, value))
                    {
                        try
                        {
                            lang.Texts.Add(texts[i], value);
                        }
                        catch (Exception e)
                        {
                            CLog.LogError("Error reading Party Language File " + file + ": " + e.Message);
                            return false;
                        }
                    }

                }
                _Languages[nr].PartyModeTexts.Add(lang);
                return true;
            }
            else
            {
                CLog.LogError("Error reading Party Language File " + file);
                return false;
            }
        }

        private static void LoadLanguageFile(string FileName)
        {
            SLanguage lang = new SLanguage();
            lang.LanguageFilePath = Path.Combine(CSettings.sFolderLanguages, FileName);

            CXMLReader xmlReader = CXMLReader.OpenFile(lang.LanguageFilePath);
            if (xmlReader == null)
                return;

            string value = string.Empty;
            if (xmlReader.GetValue("//resources/string[@name='language']", ref value, value))
            {
                lang.Name = value;

                if (lang.Name == CSettings.FallbackLanguage)
                    _FallbackLanguage = _Languages.Count;

                lang.Texts = new Hashtable();
                lang.PartyModeTexts = new List<SPartyLanguage>();

                List<string> texts = xmlReader.GetAttributes("resources", "name");
                for (int i = 0; i < texts.Count; i++)
                {
                    if (xmlReader.GetValue("//resources/string[@name='" + texts[i] + "']", ref value, value))
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

        private static int GetPartyModeNr(int PartyModeID, int Language)
        {
            for (int i = 0; i < _Languages[Language].PartyModeTexts.Count; i++)
			{
                if (_Languages[Language].PartyModeTexts[i].PartyModeID == PartyModeID)
                    return i;
			}
            return -1;
        }
    }
}
