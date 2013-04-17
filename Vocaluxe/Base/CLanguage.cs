using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using VocaluxeLib.Menu;

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
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();
        private static List<SLanguage> _Languages;
        private static int _CurrentLanguage;
        private static int _FallbackLanguage;

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
            string[] languages = new string[_Languages.Count];

            for (int i = 0; i < _Languages.Count; i++)
                languages[i] = _Languages[i].Name;

            return languages;
        }

        public static void Init()
        {
            _Languages = new List<SLanguage>();
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;

            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderLanguages, "*.xml", true, true));

            foreach (string file in files)
                _LoadLanguageFile(file);
        }

        public static bool SetLanguage(string language)
        {
            int nr = GetLanguageNr(language);
            if (nr != -1)
            {
                _CurrentLanguage = nr;
                return true;
            }
            return false;
        }

        public static int GetLanguageNr(string language)
        {
            for (int i = 0; i < _Languages.Count; i++)
            {
                if (_Languages[i].Name == language)
                    return i;
            }
            return -1;
        }

        public static string Translate(string keyWord)
        {
            return Translate(keyWord, -1);
        }

        public static string Translate(string keyWord, int partyModeID)
        {
            if (keyWord == null)
                return "Error";

            if (keyWord.Length < 3 || keyWord.Substring(0, 3) != "TR_")
                return keyWord;

            string result;
            if (partyModeID != -1)
            {
                int partyModeNr = _GetPartyModeNr(partyModeID, _CurrentLanguage);
                if (partyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[partyModeNr].Texts.TryGetValue(keyWord, out result))
                    return result;

                partyModeNr = _GetPartyModeNr(partyModeID, _FallbackLanguage);
                if (partyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[partyModeNr].Texts.TryGetValue(keyWord, out result))
                    return result;
            }

            if (_Languages[_CurrentLanguage].Texts.TryGetValue(keyWord, out result))
                return result;
            if (_Languages[_FallbackLanguage].Texts.TryGetValue(keyWord, out result))
                return result;

            return keyWord;
        }

        public static bool TranslationExists(string keyWord, int partyModeID = -1)
        {
            if (keyWord == null)
                return false;

            if (keyWord.Length < 3 || keyWord.Substring(0, 3) != "TR_")
                return false;

            if (partyModeID != -1)
            {
                int partyModeNr = _GetPartyModeNr(partyModeID, _CurrentLanguage);
                if (partyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[partyModeNr].Texts.ContainsKey(keyWord))
                    return true;

                partyModeNr = _GetPartyModeNr(partyModeID, _FallbackLanguage);
                if (partyModeNr != -1 && _Languages[_CurrentLanguage].PartyModeTexts[partyModeNr].Texts.ContainsKey(keyWord))
                    return true;
            }

            if (_Languages[_CurrentLanguage].Texts.ContainsKey(keyWord))
                return true;
            if (_Languages[_FallbackLanguage].Texts.ContainsKey(keyWord))
                return true;

            return false;
        }

        public static bool LoadPartyLanguageFiles(int partyModeID, string path)
        {
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(path, "*.xml", true, true));

            foreach (string file in files)
            {
                if (!_LoadPartyLanguageFile(partyModeID, file))
                    return false;
            }
            return true;
        }

        private static bool _LoadLanguageEntries(CXMLReader xmlReader, ref Dictionary<string, string> texts)
        {
            texts = new Dictionary<string, string>();
            List<string> names = xmlReader.GetAttributes("resources", "name");
            string value = string.Empty;
            foreach (string name in names)
            {
                if (xmlReader.GetValue("//resources/string[@name='" + name + "']", ref value, ""))
                {
                    try
                    {
                        texts.Add(name, value);
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

        private static bool _LoadPartyLanguageFile(int partyModeID, string file)
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
                lang.PartyModeID = partyModeID;
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

        private static void _LoadLanguageFile(string fileName)
        {
            SLanguage lang = new SLanguage();
            lang.LanguageFilePath = Path.Combine(CSettings.FolderLanguages, fileName);

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

        private static int _GetPartyModeNr(int partyModeID, int language)
        {
            for (int i = 0; i < _Languages[language].PartyModeTexts.Count; i++)
            {
                if (_Languages[language].PartyModeTexts[i].PartyModeID == partyModeID)
                    return i;
            }
            return -1;
        }
    }
}