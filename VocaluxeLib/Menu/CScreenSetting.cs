using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;

namespace Vocaluxe.Menu
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
        private int _PartyModeID;

        private SScreenSetting _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public CScreenSetting(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SScreenSetting();
            _ThemeLoaded = false;
        }

        public CScreenSetting(CScreenSetting ts)
        {
            _PartyModeID = ts._PartyModeID;
            _Theme = ts._Theme;
            _ThemeLoaded = ts._ThemeLoaded;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Value", ref _Theme.Value, String.Empty);
            _ThemeLoaded &= xmlReader.TryGetEnumValue<ESettingType>(item + "/Type", ref _Theme.Type);

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<Type>: Type of theme-setting-value: "+ CHelper.ListStrings(Enum.GetNames(typeof(ESettingType))));
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
                    return GetIntValue(_Theme.Value);

                case ESettingType.String:
                    return _Theme.Value;

                case ESettingType.Color:
                    return GetColorValue(_Theme.Value);

                case ESettingType.Texture:
                    return GetTextureValue(_Theme.Value);
            }

            return null;
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
        }

        public void ReloadTextures()
        {
        }

        #region Private
        private int GetIntValue(string _string)
        {
            try
            {
                return Convert.ToInt32(_string);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private STexture GetTextureValue(string _string)
        {
            return CBase.Theme.GetSkinTexture(_string, _PartyModeID);
        }

        private SColorF GetColorValue(string _string)
        {
            return CBase.Theme.GetColor(_string, _PartyModeID);
        }
        #endregion Private

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
        }

        public void ResizeElement(int stepW, int stepH)
        {
        }
        #endregion ThemeEdit
    }
}
