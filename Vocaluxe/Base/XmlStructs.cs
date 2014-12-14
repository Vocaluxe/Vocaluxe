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

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    public struct SInfo
    {
        public string Name;
        public string Author;
        public int VersionMajor;
        public int VersionMinor;
    }

    struct SThemeCursor
    {
        [XmlElement("Skin")] public string SkinName;

        public float W;
        public float H;

        public SThemeColor Color;
    }

    struct STheme
    {
        public int ThemeSystemVersion;
        public SInfo Info;
        [XmlArray, DefaultValue(null)] public SFontFamily[] Fonts;
        public SThemeCursor? Cursor;
    }

    struct SDefaultFonts
    {
        [XmlArray] public SFontFamily[] Fonts;
    }

    /// <summary>
    ///     Struct used for describing a font family (a type of text with 4 different styles)
    /// </summary>
    [XmlType("Font")]
    struct SFontFamily
    {
        public string Name;
        [XmlIgnore] public int PartyModeID;
        [XmlIgnore] public string ThemeName;

        [XmlNormalized] public float Outline; //0..1, 0=not outline 1=100% outline
        public SColorF OutlineColor;

        public string Folder;

        public string FileNormal;
        public string FileBold;
        public string FileItalic;
        public string FileBoldItalic;

        [XmlIgnore] public CFontStyle Normal;
        [XmlIgnore] public CFontStyle Italic;
        [XmlIgnore] public CFontStyle Bold;
        [XmlIgnore] public CFontStyle BoldItalic;

        public void Dispose()
        {
            Normal.Dispose();
            Italic.Dispose();
            Bold.Dispose();
            BoldItalic.Dispose();
        }
    }

    struct SSkin
    {
        public int SkinSystemVersion;
        public SInfo Info;
        public Dictionary<string, SColorF> Colors;
        public Dictionary<string, string> Skins;
        public Dictionary<string, string> Videos;
    }

    struct SPlaylistInfo
    {
        [XmlElement("PlaylistName")] public string Name;
    }

    struct SPlaylist
    {
        public SPlaylistInfo Info;
        [XmlArray] public SPlaylistSong[] Songs;
    }

    #region Partymode
    struct SPartyModeInfos
    {
        public string Name;
        public string Description;
        public string Author;
        public string Folder;
        public string PartyModeFile;
        [XmlAltName("PartyModeVersionMajor")] public int VersionMajor;
        [XmlAltName("PartyModeVersionMinor")] public int VersionMinor;
        public string TargetAudience;
        [XmlIgnore] public IPartyModeInfo ExtInfo;
    }

    struct SPartyMode
    {
        public int PartyModeSystemVersion;
        [XmlArray("PartyScreens"), XmlArrayItem("ScreenFile")] public List<string> ScreenFiles;
        [XmlIgnore] public IPartyMode PartyMode;
        public SPartyModeInfos Info;
    }
    #endregion Partymode
}