using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    struct SLanguage
    {
        public string Name;
        public string LanguageFilePath;

        public Dictionary<string, string> Texts;
        public List<SPartyLanguage> PartyModeTexts;
    }

    struct SPartyLanguage
    {
        public int PartyModeID;
        public Dictionary<string, string> Texts;
    }

    static class CLanguage
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<SLanguage> _Languages;
        private static int _CurrentLanguage = 0;
        private static int _FallbackLanguage = 0;

        public static int LanguageId
        {
            get { return _CurrentLanguage; }
            set
            {
                if (value >= 0 && value < _Languages.Count)
                    _CurrentLanguage = value;
            }
        }

        public static string GetLanguageName(int lang)
        {
            return _Languages[lang].Name;
        }

        public static string[] GetLanguageNames()
        {
            string[] Languages = new string[_Languages.Count];

            for (int i = 0; i < _Languages.Count; i++)
            {
                Languages[i]=_Languages[i].Name;
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

            if (KeyWord.Length < 3 ||  KeyWord.Substring(0, 3) != "TR_")
                return KeyWord;

            string result;
            if (PartyModeID != -1)
            {
                int PartyModeNr = GetPartyModeNr(PartyModeID, _CurrentLanguage);
                if (PartyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts.TryGetValue(KeyWord, out result))
                    return result;

                PartyModeNr = GetPartyModeNr(PartyModeID, _FallbackLanguage);
                if (PartyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts.TryGetValue(KeyWord, out result))
                    return result;
            }

            if (_Languages[_CurrentLanguage].Texts.TryGetValue(KeyWord, out result))
                return result;
            if (_Languages[_FallbackLanguage].Texts.TryGetValue(KeyWord, out result))
                return result;

            return KeyWord;
        }

        public static bool TranslationExists(string KeyWord, int PartyModeID = -1)
        {
            if (KeyWord == null)
                return false;

            if (KeyWord.Length < 3 || KeyWord.Substring(0, 3) != "TR_")
                return false;

            if (PartyModeID != -1)
            {
                int PartyModeNr = GetPartyModeNr(PartyModeID, _CurrentLanguage);
                if (PartyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts.ContainsKey(KeyWord))
                    return true;

                PartyModeNr = GetPartyModeNr(PartyModeID, _FallbackLanguage);
                if (PartyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[PartyModeNr].Texts.ContainsKey(KeyWord))
                    return true;
            }

            if (_Languages[_CurrentLanguage].Texts.ContainsKey(KeyWord))
                return true;
            if (_Languages[_FallbackLanguage].Texts.ContainsKey(KeyWord))
                return true;

            return false;
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

        private static bool _LoadLanguageEntries(CXMLReader xmlReader, ref Dictionary<string, string> Texts)
        {
            Texts = new Dictionary<string, string>();
            List<string> names = xmlReader.GetAttributes("resources", "name");
            string value = string.Empty;
            foreach(string name in names)
            {
                if (xmlReader.GetValue("//resources/string[@name='" + name + "']", ref value, ""))
                {
                    try
                    {
                        Texts.Add(name, value);
                    }
                    catch (Exception e)
                    {
                        CLog.LogError("Error reading language file " + xmlReader.FileName + ": " + e.Message);
                        return false;
                    }
                }
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
                if (!_LoadLanguageEntries(xmlReader, ref lang.Texts))
                    return false;
       
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

                lang.PartyModeTexts = new List<SPartyLanguage>();

                _LoadLanguageEntries(xmlReader, ref lang.Texts);

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
