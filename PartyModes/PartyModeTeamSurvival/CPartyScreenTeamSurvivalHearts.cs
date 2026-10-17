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
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TeamSurvival
{
    class CHeartsRow
    {
        public Guid ProfileId;
        public CStatic Highlight;
        public CStatic Avatar;
        public CText Name;
        public CStatic[] Hearts;
    }

    public class CPartyScreenTeamSurvivalHearts : CPartyScreenTeamSurvival
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private const string _StaticRowHighlight = "StaticRowHighlight";
        private const string _StaticRowAvatar = "StaticRowAvatar";
        private const string _StaticHeart = "StaticHeart";
        private const string _TextRowName = "TextRowName";

        private const float _FirstRowY = 260f;
        private const float _RowH = 70f;
        private const float _HeartSize = 46f;
        private const float _HeartSpace = 10f;
        private const float _LostHeartAlpha = 0.2f;

        private readonly float[] _ColumnX = new float[] { 160f, 1020f };

        private List<CHeartsRow> _Rows;
        private int _NumRowsUsed;
        private int _SelectedRow;
        private bool _OnButtons;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { _ButtonNext, _ButtonBack };
            _ThemeStatics = new string[] { _StaticRowHighlight, _StaticRowAvatar, _StaticHeart };
            _ThemeTexts = new string[] { _TextRowName };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Rows = new List<CHeartsRow>();
            for (var i = 0; i < CPartyModeTeamSurvival.TeamCount * CPartyModeTeamSurvival.MaxPlayersInTeam; i++)
            {
                var row = new CHeartsRow
                {
                    Highlight = GetNewStatic(_Statics[_StaticRowHighlight]),
                    Avatar = GetNewStatic(_Statics[_StaticRowAvatar]),
                    Name = GetNewText(_Texts[_TextRowName]),
                    Hearts = new CStatic[CPartyModeTeamSurvival.MaxHearts]
                };
                row.Avatar.Aspect = EAspect.Crop;
                _AddStatic(row.Highlight);
                _AddStatic(row.Avatar);
                _AddText(row.Name);
                for (var h = 0; h < CPartyModeTeamSurvival.MaxHearts; h++)
                {
                    row.Hearts[h] = GetNewStatic(_Statics[_StaticHeart]);
                    _AddStatic(row.Hearts[h]);
                }

                _Rows.Add(row);
            }

            _Statics[_StaticRowHighlight].Visible = false;
            _Statics[_StaticRowAvatar].Visible = false;
            _Statics[_StaticHeart].Visible = false;
            _Texts[_TextRowName].Visible = false;
        }

        public override void OnShow()
        {
            base.OnShow();

            _NumRowsUsed = 0;
            var teams = new List<Guid>[] { _PartyMode.GameData.ProfileIdsTeam1, _PartyMode.GameData.ProfileIdsTeam2 };
            for (var team = 0; team < teams.Length; team++)
            {
                for (var i = 0; i < teams[team].Count && i < CPartyModeTeamSurvival.MaxPlayersInTeam; i++)
                {
                    var row = _Rows[_NumRowsUsed++];
                    row.ProfileId = teams[team][i];
                    _PlaceRow(row, _ColumnX[team], _FirstRowY + i * _RowH);
                    row.Avatar.Texture = CBase.Profiles.GetAvatar(row.ProfileId);
                    row.Name.Text = CBase.Profiles.GetPlayerName(row.ProfileId);
                }
            }

            for (var i = 0; i < _Rows.Count; i++)
            {
                var visible = i < _NumRowsUsed;
                _Rows[i].Avatar.Visible = visible;
                _Rows[i].Name.Visible = visible;
                foreach (var heart in _Rows[i].Hearts)
                {
                    heart.Visible = visible;
                }
            }

            _SelectedRow = 0;
            _UpdateRows();
            _FocusRows();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed)
            {
                return true;
            }

            switch (keyEvent.Key)
            {
                case Keys.Back:
                case Keys.Escape:
                    _PartyMode.Back();
                    break;

                case Keys.Enter:
                    if (_OnButtons && _Buttons[_ButtonBack].Selected)
                    {
                        _PartyMode.Back();
                    }
                    else
                    {
                        _PartyMode.Next();
                    }

                    break;

                case Keys.Up:
                    if (_OnButtons)
                    {
                        _FocusRows();
                    }
                    else
                    {
                        _SelectRow(_SelectedRow - 1);
                    }

                    break;

                case Keys.Down:
                    if (_OnButtons)
                    {
                        break;
                    }

                    if (_SelectedRow + 1 < _NumRowsUsed)
                    {
                        _SelectRow(_SelectedRow + 1);
                    }
                    else
                    {
                        _FocusButtons();
                    }

                    break;

                case Keys.Left:
                case Keys.Right:
                    if (_OnButtons)
                    {
                        _SwitchButton();
                    }
                    else
                    {
                        _ChangeHearts(_SelectedRow, _GetHearts(_SelectedRow) + (keyEvent.Key == Keys.Left ? -1 : 1));
                    }

                    break;

                case Keys.Tab:
                    if (_OnButtons)
                    {
                        _SwitchButton();
                    }
                    else
                    {
                        _FocusButtons();
                    }

                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (_Buttons[_ButtonBack].Selected || _Buttons[_ButtonNext].Selected)
            {
                _OnButtons = true;
            }

            if (mouseEvent.LB)
            {
                for (var r = 0; r < _NumRowsUsed; r++)
                {
                    for (var h = 0; h < CPartyModeTeamSurvival.MaxHearts; h++)
                    {
                        if (CHelper.IsInBounds(_Rows[r].Hearts[h].Rect, mouseEvent))
                        {
                            _SelectRow(r);
                            _ChangeHearts(r, h + 1);
                            return true;
                        }
                    }

                    if (CHelper.IsInBounds(_Rows[r].Highlight.Rect, mouseEvent))
                    {
                        _SelectRow(r);
                        return true;
                    }
                }

                if (_IsMouseOverCurSelection(mouseEvent))
                {
                    if (_Buttons[_ButtonBack].Selected)
                    {
                        _PartyMode.Back();
                    }

                    if (_Buttons[_ButtonNext].Selected)
                    {
                        _PartyMode.Next();
                    }
                }
            }

            if (mouseEvent.RB)
            {
                _PartyMode.Back();
            }

            return true;
        }

        private void _PlaceRow(CHeartsRow row, float x, float y)
        {
            var highlight = _Statics[_StaticRowHighlight].Rect;
            row.Highlight.MaxRect = new SRectF(x, y, highlight.W, highlight.H, highlight.Z);

            var avatar = _Statics[_StaticRowAvatar].Rect;
            row.Avatar.MaxRect = new SRectF(x + 8f, y + (highlight.H - avatar.H) / 2, avatar.W, avatar.H, avatar.Z);

            row.Name.X = x + avatar.W + 24f;
            row.Name.Y = y + (highlight.H - row.Name.H) / 2;

            var heartsX = x + highlight.W - CPartyModeTeamSurvival.MaxHearts * (_HeartSize + _HeartSpace);
            for (var h = 0; h < row.Hearts.Length; h++)
            {
                row.Hearts[h].MaxRect = new SRectF(heartsX + h * (_HeartSize + _HeartSpace), y + (highlight.H - _HeartSize) / 2, _HeartSize, _HeartSize,
                    _Statics[_StaticHeart].Rect.Z);
            }
        }

        private int _GetHearts(int row)
        {
            if (row < 0 || row >= _NumRowsUsed)
            {
                return 0;
            }

            return _PartyMode.GetStartHearts(_Rows[row].ProfileId);
        }

        private void _ChangeHearts(int row, int hearts)
        {
            if (row < 0 || row >= _NumRowsUsed)
            {
                return;
            }

            hearts = Math.Max(1, Math.Min(CPartyModeTeamSurvival.MaxHearts, hearts));
            _PartyMode.GameData.PlayerHearts[_Rows[row].ProfileId] = hearts;
            _UpdateRows();
        }

        private void _FocusRows()
        {
            _OnButtons = false;
            _Buttons[_ButtonBack].Selected = false;
            _Buttons[_ButtonNext].Selected = false;
            _UpdateRows();
        }

        private void _FocusButtons()
        {
            _OnButtons = true;
            _SelectElement(_Buttons[_ButtonNext]);
            _UpdateRows();
        }

        private void _SwitchButton()
        {
            _SelectElement(_Buttons[_ButtonNext].Selected ? _Buttons[_ButtonBack] : _Buttons[_ButtonNext]);
        }

        private void _SelectRow(int row)
        {
            _OnButtons = false;
            _Buttons[_ButtonBack].Selected = false;
            _Buttons[_ButtonNext].Selected = false;
            if (_NumRowsUsed == 0)
            {
                return;
            }

            _SelectedRow = Math.Max(0, Math.Min(_NumRowsUsed - 1, row));
            _UpdateRows();
        }

        private void _UpdateRows()
        {
            for (var r = 0; r < _NumRowsUsed; r++)
            {
                var row = _Rows[r];
                row.Highlight.Visible = r == _SelectedRow && !_OnButtons;

                var hearts = _GetHearts(r);
                var color = _Statics[_StaticHeart].Color;
                for (var h = 0; h < row.Hearts.Length; h++)
                {
                    row.Hearts[h].Color = new SColorF(color.R, color.G, color.B, h < hearts ? color.A : color.A * _LostHeartAlpha);
                }
            }

            for (var r = _NumRowsUsed; r < _Rows.Count; r++)
            {
                _Rows[r].Highlight.Visible = false;
            }
        }
    }
}
