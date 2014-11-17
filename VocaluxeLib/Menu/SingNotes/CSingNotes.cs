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
using System.Xml.Serialization;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SingNotes
{
    [XmlType("Position")]
    public struct SBarPosition
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public SRectF Rect;
    }

    [XmlType("SingBar")]
    public struct SThemeSingBar
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string SkinLeft;
        public string SkinMiddle;
        public string SkinRight;

        public string SkinBackgroundLeft;
        public string SkinBackgroundMiddle;
        public string SkinBackgroundRight;

        public string SkinGoldenStar;
        public string SkinToneHelper;
        public string SkinPerfectNoteStart;

        [XmlArray("BarPositions")] public SBarPosition[] BarPos;
    }

    public class CSingNotes : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;
        private SThemeSingBar _Theme;
        private bool _ThemeLoaded;

        public readonly List<CNoteBars> PlayerNotes = new List<CNoteBars>();

        public bool Selectable
        {
            get { return false; }
        }

        /// <summary>
        ///     Player bar positions
        /// </summary>
        /// <remarks>
        ///     first index = player number; second index = num players seen on screen
        /// </remarks>
        private SRectF[,] _BarPos { get; set; }

        public CSingNotes(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeSingBar {BarPos = new SBarPosition[CHelper.Sum(CBase.Settings.GetMaxNumPlayer())]};
            _ThemeLoaded = false;
        }

        public CSingNotes(SThemeSingBar theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _BarPos = new SRectF[CBase.Settings.GetMaxNumPlayer(),CBase.Settings.GetMaxNumPlayer()];

            LoadSkin();
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinLeft", out _Theme.SkinLeft, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinMiddle", out _Theme.SkinMiddle, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinRight", out _Theme.SkinRight, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundLeft", out _Theme.SkinBackgroundLeft, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundMiddle", out _Theme.SkinBackgroundMiddle, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundRight", out _Theme.SkinBackgroundRight, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinGoldenStar", out _Theme.SkinGoldenStar, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinToneHelper", out _Theme.SkinToneHelper, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinPerfectNoteStar", out _Theme.SkinPerfectNoteStart, String.Empty);

            int i = 0;
            _BarPos = new SRectF[CBase.Settings.GetMaxNumPlayer(),CBase.Settings.GetMaxNumPlayer()];
            for (int numplayer = 0; numplayer < CBase.Settings.GetMaxNumPlayer(); numplayer++)
            {
                for (int player = 0; player <= numplayer; player++)
                {
                    _BarPos[player, numplayer] = new SRectF();
                    string target = "/BarPositions/P" + (player + 1) + "N" + (numplayer + 1);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "X", ref _BarPos[player, numplayer].X);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Y", ref _BarPos[player, numplayer].Y);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Z", ref _BarPos[player, numplayer].Z);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "W", ref _BarPos[player, numplayer].W);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "H", ref _BarPos[player, numplayer].H);
                    _Theme.BarPos[i].Name = "P" + (player + 1) + "N" + (numplayer + 1);
                    _Theme.BarPos[i].Rect = _BarPos[player, numplayer];
                    i++;
                }
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadSkin();
            }

            return _ThemeLoaded;
        }

        public void Init(int numPlayers)
        {
            PlayerNotes.Clear();
            for (int p = 0; p < numPlayers; p++)
                PlayerNotes.Add(new CNoteBars(_PartyModeID, p, _BarPos[p, numPlayers - 1], _Theme));
        }

        public void Draw()
        {
            foreach (CNoteBars noteBars in PlayerNotes)
                noteBars.Draw();
        }

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            foreach (SBarPosition bp in _Theme.BarPos)
            {
                int n = Int32.Parse(bp.Name.Substring(3, 1)) - 1;
                int p = Int32.Parse(bp.Name.Substring(1, 1)) - 1;

                _BarPos[p, n] = bp.Rect;
            }
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public SThemeSingBar GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) {}

        public void ResizeElement(int stepW, int stepH) {}
        #endregion ThemeEdit
    }
}