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
    public struct SThemeScreenSetting
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public string Value;
        public ESettingType Type;
    }

    // ReSharper disable ClassNeverInstantiated.Global
    //Instantiated by reflection
    public class CScreenSetting : IThemeable
        // ReSharper restore ClassNeverInstantiated.Global
    {
        private readonly int _PartyModeID;

        private SThemeScreenSetting _Theme;
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
            _Theme = new SThemeScreenSetting();
            _ThemeLoaded = false;
        }

        public CScreenSetting(SThemeScreenSetting theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
        }

        public object GetValue()
        {
            switch (_Theme.Type)
            {
                case ESettingType.Int:
                    return _GetIntValue();

                case ESettingType.String:
                    return _Theme.Value;

                case ESettingType.Color:
                    return _GetColorValue();

                case ESettingType.Texture:
                    return _GetTextureValue();
            }

            return null;
        }

        public object GetTheme()
        {
            return _Theme;
        }

        private int _GetIntValue()
        {
            try
            {
                return Convert.ToInt32(_Theme.Value);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private CTextureRef _GetTextureValue()
        {
            return CBase.Themes.GetSkinTexture(_Theme.Value, _PartyModeID);
        }

        private SColorF _GetColorValue()
        {
            SColorF color;
            CBase.Themes.GetColor(_Theme.Value, _PartyModeID, out color);
            return color;
        }

        #region Dummy methods for interface
        public void UnloadSkin() {}

        public void LoadSkin() {}

        public void ReloadSkin() {}
        #endregion
    }
}