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
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SingNotes
{
    [XmlType("Position")]
    public struct SBarPosition
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;
        public SRectF Rect;
    }

    [XmlType("SingBar")]
    public struct SThemeSingBar
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;

        public string SkinLeft;
        public string SkinMiddle;
        public string SkinRight;

        public string SkinFillLeft;
        public string SkinFillMiddle;
        public string SkinFillRight;

        public string SkinBackgroundLeft;
        public string SkinBackgroundMiddle;
        public string SkinBackgroundRight;

        public string SkinGoldenStar;
        public string SkinToneHelper;
        public string SkinPerfectNoteStart;

        [XmlArray("BarPositions")]
        public SBarPosition[] BarPos;
    }

    public class CSingNotes : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;
        private SThemeSingBar _Theme;

        public readonly List<CNoteBars> PlayerNotes = new List<CNoteBars>();

        private SRectF _Rect;
        public override SRectF Rect
        {
            get { return _Rect; }
        }

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
        private SRectF[,,] _BarPos { get; set; }

        public CSingNotes(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeSingBar { BarPos = new SBarPosition[CHelper.Sum(CBase.Settings.GetMaxNumPlayer() * CBase.Settings.GetMaxNumScreens())] };
            ThemeLoaded = false;
        }

        public CSingNotes(SThemeSingBar theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _BarPos = new SRectF[CBase.Settings.GetMaxNumScreens(), CBase.Settings.GetMaxNumPlayer(), CBase.Settings.GetMaxNumPlayer()];

            ThemeLoaded = true;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            ThemeLoaded = true;

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinLeft", out _Theme.SkinLeft, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinMiddle", out _Theme.SkinMiddle, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinRight", out _Theme.SkinRight, String.Empty);

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinFillLeft", out _Theme.SkinLeft, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinFillMiddle", out _Theme.SkinMiddle, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinFillRight", out _Theme.SkinRight, String.Empty);

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundLeft", out _Theme.SkinBackgroundLeft, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundMiddle", out _Theme.SkinBackgroundMiddle, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundRight", out _Theme.SkinBackgroundRight, String.Empty);

            ThemeLoaded &= xmlReader.GetValue(item + "/SkinGoldenStar", out _Theme.SkinGoldenStar, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinToneHelper", out _Theme.SkinToneHelper, String.Empty);
            ThemeLoaded &= xmlReader.GetValue(item + "/SkinPerfectNoteStar", out _Theme.SkinPerfectNoteStart, String.Empty);

            _BarPos = new SRectF[CBase.Settings.GetMaxNumScreens(), CBase.Settings.GetMaxNumPlayer(), CBase.Settings.GetMaxNumPlayer()];
            for (int numplayer = 0; numplayer < CBase.Settings.GetMaxNumPlayer(); numplayer++)
            {
                for (int player = 0; player <= numplayer; player++)
                {
                    _BarPos[0, player, numplayer] = new SRectF();
                    string target = "/BarPositions/P" + (player + 1) + "N" + (numplayer + 1);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "X", ref _BarPos[0, player, numplayer].X);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Y", ref _BarPos[0, player, numplayer].Y);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Z", ref _BarPos[0, player, numplayer].Z);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "W", ref _BarPos[0, player, numplayer].W);
                    ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "H", ref _BarPos[0, player, numplayer].H);
                    _Theme.BarPos[player].Name = "P" + (player + 1) + "N" + (numplayer + 1);
                    _Theme.BarPos[player].Rect = _BarPos[0, player, numplayer];
                }
            }

            if (ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadSkin();
            }

            return ThemeLoaded;
        }

        public void Init(int numPlayers, int numScreens = 0)
        {

            PlayerNotes.Clear();
            if (numPlayers > 0 && numScreens > 0)
            {
                int[] screenAssignment = new int[numPlayers];
                int screenPlayers = numPlayers / numScreens;
                int remainingPlayers = numPlayers - (screenPlayers * numScreens);
                int player = 0;

                for (int s = 0; s < numScreens; s++)
                {
                    for (int p = 0; p < screenPlayers; p++)
                    {
                        if (remainingPlayers > 0)
                        {
                            PlayerNotes.Add(new CNoteBars(_PartyModeID, player++, _BarPos[s, p, screenPlayers], _Theme));
                            if (p == screenPlayers - 1)
                            {
                                PlayerNotes.Add(new CNoteBars(_PartyModeID, player++, _BarPos[s, p + 1, screenPlayers], _Theme));
                                remainingPlayers--;
                            }
                        }
                        else
                        {
                            PlayerNotes.Add(new CNoteBars(_PartyModeID, player++, _BarPos[s, p, screenPlayers - 1], _Theme));
                        }

                    }
                    //Handle when players < screens
                    if (screenPlayers == 0 && remainingPlayers > 0)
                    {
                        PlayerNotes.Add(new CNoteBars(_PartyModeID, player++, _BarPos[s, 0, 0], _Theme));
                        remainingPlayers--;
                    }
                }
            }

            if (numPlayers == 0)
                _Rect = new SRectF(0, 0, 0, 0, Rect.Z);
            else
            {
                _Rect.X = PlayerNotes.Select(bp => bp.Rect.X).Min();
                _Rect.Y = PlayerNotes.Select(bp => bp.Rect.X).Min();
                _Rect.Right = PlayerNotes.Select(bp => bp.Rect.Right).Max();
                _Rect.Bottom = PlayerNotes.Select(bp => bp.Rect.Bottom).Max();
                _Rect.Z = PlayerNotes.Select(bp => bp.Rect.Z).Average();
            }
        }

        public void Draw()
        {
            foreach (CNoteBars noteBars in PlayerNotes)
                noteBars.Draw();
        }

        public void UnloadSkin() { }

        public void LoadSkin()
        {
            Debug.Assert(_Theme.BarPos.Length > 0);
            X = _Theme.BarPos.Select(bp => bp.Rect.X).Min();
            Y = _Theme.BarPos.Select(bp => bp.Rect.X).Min();
            W = X - _Theme.BarPos.Select(bp => bp.Rect.Right).Max();
            H = Y - _Theme.BarPos.Select(bp => bp.Rect.Bottom).Max();
            Z = _Theme.BarPos.Select(bp => bp.Rect.Z).Average();
            foreach (SBarPosition bp in _Theme.BarPos)
            {
                int n = Int32.Parse(bp.Name.Substring(3, 1)) - 1;
                int p = Int32.Parse(bp.Name.Substring(1, 1)) - 1;
                for (int s = 0; s < CBase.Settings.GetMaxNumScreens(); s++)
                {
                    _BarPos[s, p, n] = bp.Rect;
                    _BarPos[s, p, n].X += s * CBase.Settings.GetRenderW();
                }
            }
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) { }

        public void ResizeElement(int stepW, int stepH) { }
        #endregion ThemeEdit
    }
}