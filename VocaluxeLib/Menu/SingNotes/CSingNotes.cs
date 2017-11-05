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
                _Rect.Y = PlayerNotes.Select(bp => bp.Rect.Y).Min();
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
