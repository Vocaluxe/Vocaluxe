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
using System.Xml;
using System.Xml.Serialization;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    public enum ESettingType
    {
        Int,
        String,
        Color,
        Texture
    }

    [XmlType("ScreenSetting")]
    public struct SScreenSetting
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        [XmlElement("Value")] public string Value;
        [XmlElement("Type")] public ESettingType Type;
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

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public CScreenSetting(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SScreenSetting();
            _ThemeLoaded = false;
        }

        public CScreenSetting(SScreenSetting theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
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

        public SScreenSetting GetTheme()
        {
            return _Theme;
        }

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

        private CTextureRef _GetTextureValue(string value)
        {
            return CBase.Theme.GetSkinTexture(value, _PartyModeID);
        }

        private SColorF _GetColorValue(string value)
        {
            SColorF color;
            CBase.Theme.GetColor(value, _PartyModeID, out color);
            return color;
        }
        #endregion Private

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) {}

        public void ResizeElement(int stepW, int stepH) {}
        #endregion ThemeEdit
    }
}