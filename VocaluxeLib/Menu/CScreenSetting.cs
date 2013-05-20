#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Xml;
using VocaluxeLib.Draw;

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

    // ReSharper disable ClassNeverInstantiated.Global
    //Instantiated by reflection
    public class CScreenSetting : IMenuElement
        // ReSharper restore ClassNeverInstantiated.Global
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

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Value", out _Theme.Value, String.Empty);
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

        private CTexture _GetTextureValue(string value)
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