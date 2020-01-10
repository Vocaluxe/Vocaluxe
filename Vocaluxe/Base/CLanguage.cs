#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    struct SLanguage
    {
        public string Name;
        public string FilePath;

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
        private static readonly List<SLanguage> _Languages = new List<SLanguage>();
        private static int _CurrentLanguage = -1;
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

        public static IEnumerable<string> GetLanguageNames()
        {
            var languages = new string[_Languages.Count];

            for (int i = 0; i < _Languages.Count; i++)
                languages[i] = _Languages[i].Name;

            return languages;
        }

        public static bool Init()
        {
            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderNameLanguages, "*.xml", true, true));

            foreach (string file in files)
                _LoadLanguageFile(file);
            return _CurrentLanguage >= 0 && _FallbackLanguage >= 0;
        }

        public static bool SetLanguage(string language)
        {
            int nr = _GetLanguageNr(language);
            if (nr != -1)
            {
                _CurrentLanguage = nr;
                return true;
            }
            return false;
        }

        private static int _GetLanguageNr(string language)
        {
            for (int i = 0; i < _Languages.Count; i++)
            {
                if (_Languages[i].Name == language)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///     Checks if a translation exists and returns it
        /// </summary>
        /// <param name="keyWord">Word to translate</param>
        /// <param name="partyModeID"></param>
        /// <param name="translation">Translated word or keyWord if translation does not exist</param>
        /// <returns>True if word was translated</returns>
        private static bool _GetTranslation(string keyWord, int partyModeID, out string translation)
        {
            if (keyWord == null)
            {
                translation = "Error";
                return false;
            }

            if (keyWord.Length < 3 || keyWord.Substring(0, 3) != "TR_")
            {
                translation = keyWord;
                return false;
            }

            if (partyModeID != -1)
            {
                Dictionary<string, string> partyModeTexts = _GetPartyModeTexts(_CurrentLanguage, partyModeID);
                if (partyModeTexts != null && partyModeTexts.TryGetValue(keyWord, out translation))
                    return true;

                partyModeTexts = _GetPartyModeTexts(_FallbackLanguage, partyModeID);
                if (partyModeTexts != null && partyModeTexts.TryGetValue(keyWord, out translation))
                    return true;
            }

            if (_Languages[_CurrentLanguage].Texts.TryGetValue(keyWord, out translation))
                return true;
            if (_Languages[_FallbackLanguage].Texts.TryGetValue(keyWord, out translation))
                return true;

            translation = keyWord;
            return false;
        }

        public static string Translate(string keyWord, int partyModeID = -1)
        {
            string translation;
            _GetTranslation(keyWord, partyModeID, out translation);
            return translation;
        }

        public static bool TranslationExists(string keyWord, int partyModeID = -1)
        {
            string translation;
            return _GetTranslation(keyWord, partyModeID, out translation);
        }

        public static bool LoadPartyLanguageFiles(int partyModeID, string path)
        {
            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(path, "*.xml", true, true));

            return files.All(file => _LoadPartyLanguageFile(partyModeID, file));
        }

        private static bool _LoadLanguageEntries(string filePath, out Dictionary<string, string> texts)
        {
            var deser = new CXmlDeserializer();
            try
            {
                texts = deser.Deserialize<Dictionary<string, string>>(filePath);
                string language;
                if (!texts.TryGetValue("language", out language))
                    throw new Exception("'language' entry is missing");
            }
            catch (Exception e)
            {
                CLog.Error("Error reading language file " + filePath + ": " + e.Message);
                texts = null;
                return false;
            }
            return true;
        }

        private static bool _LoadPartyLanguageFile(int partyModeID, string filePath)
        {
            var lang = new SPartyLanguage {PartyModeID = partyModeID};

            if (!_LoadLanguageEntries(filePath, out lang.Texts))
                return false;

            int nr = _GetLanguageNr(lang.Texts["language"]);

            if (nr >= 0)
                _Languages[nr].PartyModeTexts.Add(lang);
            return true;
        }

        private static void _LoadLanguageFile(string fileName)
        {
            var lang = new SLanguage {FilePath = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameLanguages, fileName), PartyModeTexts = new List<SPartyLanguage>()};
            if (!_LoadLanguageEntries(lang.FilePath, out lang.Texts))
                return;

            lang.Name = lang.Texts["language"];
            if (lang.Name == CSettings.FallbackLanguage)
            {
                _FallbackLanguage = _Languages.Count;
                if (_CurrentLanguage < 0)
                    _CurrentLanguage = _FallbackLanguage;
            }

            _Languages.Add(lang);
        }

        private static Dictionary<string, string> _GetPartyModeTexts(int language, int partyModeID)
        {
            return
                _Languages[language].PartyModeTexts.Where(partyLanguage => partyLanguage.PartyModeID == partyModeID).Select(partyLanguage => partyLanguage.Texts).FirstOrDefault();
        }
    }
}