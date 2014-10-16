using System;
using System.IO;
using System.Linq;
using Vocaluxe.Base.Fonts;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.ThemeSystem
{
    class CPartyTheme : CTheme
    {
        public CPartyTheme(string filePath, int partyModeID) : base(filePath, partyModeID) {}

        public override void Unload()
        {
            base.Unload();
            CFonts.UnloadPartyModeFonts(PartyModeID);
        }

        protected override CSkin _GetNewSkin(string path, string file)
        {
            return new CPartySkin(path, file, this);
        }

        protected override bool _Load(CXMLReader xmlReader)
        {
            return CFonts.LoadPartyModeFonts(PartyModeID, Path.Combine(_Folder, "..", CSettings.FolderNamePartyModeFonts), xmlReader);
        }

        private CSkin _GetSkinToLoad(int fallbackNum)
        {
            CSkin skin;
            switch (fallbackNum)
            {
                case 0:
                    _Skins.TryGetValue(CConfig.Skin, out skin);
                    break;
                case 1:
                    _Skins.TryGetValue(CSettings.DefaultName, out skin);
                    break;
                case 2:
                    skin = _Skins.Values.FirstOrDefault();
                    break;
                default:
                    throw new ArgumentException();
            }
            return skin;
        }

        protected override bool _LoadSkin()
        {
            CSkin skin = null;
            for (int i = 0; i < 3; i++)
            {
                skin = _GetSkinToLoad(i);
                if (skin == null)
                    continue;
                if (skin.Load())
                    break;
                skin.Unload();
                CLog.LogError("Failed to load skin " + skin + "! Removing...", true);
                _Skins.Remove(skin.Name);
            }
            if (skin == null)
                return false;
            CurrentSkin = skin;
            return true;
        }
    }
}