using System;
using System.Xml;

namespace VocaluxeLib.Menu
{
    enum ESettingType
    {
        Int,
        String,
        Color,
        Texture
    }

    struct SScreenSetting
    {
        public string Name;
        public string Value;
        public ESettingType Type;
    }

    public class CScreenSetting : IMenuElement
    {
        private readonly int _PartyModeID;

        private SScreenSetting _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public CScreenSetting(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SScreenSetting();
            _ThemeLoaded = false;
        }

        public CScreenSetting(CScreenSetting ts)
        {
            _PartyModeID = ts._PartyModeID;
            _Theme = ts._Theme;
            _ThemeLoaded = ts._ThemeLoaded;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Value", ref _Theme.Value, String.Empty);
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Type", ref _Theme.Type);

            if (_ThemeLoaded)
                _Theme.Name = elementName;
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<Type>: Type of theme-setting-value: " + CHelper.ListStrings(Enum.GetNames(typeof(ESettingType))));
                writer.WriteElementString("Type", Enum.GetName(typeof(ESettingType), _Theme.Type));
                writer.WriteComment("<Value>: Value of theme-setting");
                writer.WriteElementString("Value", _Theme.Value);
                writer.WriteEndElement();
                return true;
            }
            return false;
        }

        public object GetValue()
        {
            switch (_Theme.Type)
            {
                case ESettingType.Int:
                    return _GetIntValue(_Theme.Value);

                case ESettingType.String:
                    return _Theme.Value;

                case ESettingType.Color:
                    return _GetColorValue(_Theme.Value);

                case ESettingType.Texture:
                    return _GetTextureValue(_Theme.Value);
            }

            return null;
        }

        public void UnloadTextures() {}

        public void LoadTextures() {}

        public void ReloadTextures() {}

        #region Private
        private int _GetIntValue(string value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private STexture _GetTextureValue(string value)
        {
            return CBase.Theme.GetSkinTexture(value, _PartyModeID);
        }

        private SColorF _GetColorValue(string value)
        {
            return CBase.Theme.GetColor(value, _PartyModeID);
        }
        #endregion Private

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) {}

        public void ResizeElement(int stepW, int stepH) {}
        #endregion ThemeEdit
    }
}