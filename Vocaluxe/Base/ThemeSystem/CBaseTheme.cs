using System.IO;
using System.Linq;
using Vocaluxe.Base.Fonts;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.ThemeSystem
{
    class CBaseTheme : CTheme
    {
        public SThemeCursor CursorTheme;

        public CBaseTheme(string filePath) : base(filePath, -1) {}

        protected override CSkin _GetNewSkin(string path, string file)
        {
            return new CBaseSkin(path, file, this);
        }

        public override void Unload()
        {
            base.Unload();
            CFonts.UnloadThemeFonts(Name);
        }

        protected override bool _Load(CXMLReader xmlReader)
        {
            return _LoadCursor(xmlReader) &&
                   CFonts.LoadThemeFonts(Name, Path.Combine(_Folder, Name, CSettings.FolderNameThemeFonts), xmlReader);
        }

        private bool _LoadCursor(CXMLReader xmlReader)
        {
            bool ok = xmlReader.GetValue("//root/Cursor/Skin", out CursorTheme.SkinName);

            ok &= xmlReader.TryGetFloatValue("//root/Cursor/W", ref CursorTheme.W);
            ok &= xmlReader.TryGetFloatValue("//root/Cursor/H", ref CursorTheme.H);
            ok &= xmlReader.Read("//root/Cursor/Color", out CursorTheme.Color);

            return ok;
        }

        protected override bool _LoadSkin()
        {
            CSkin skin;
            if (!_Skins.TryGetValue(CConfig.Skin, out skin))
                skin = _Skins.Values.FirstOrDefault();
            while (skin != null)
            {
                if (skin.Load())
                    break;
                skin.Unload();
                CLog.LogError("Failed to load skin " + skin + "! Removing...", true);
                _Skins.Remove(skin.Name);
                skin = _Skins.Values.FirstOrDefault();
            }
            if (skin == null)
                return false;
            CurrentSkin = skin;
            return true;
        }
    }
}