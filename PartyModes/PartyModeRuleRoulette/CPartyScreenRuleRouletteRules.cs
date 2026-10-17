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
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.RuleRoulette
{
    class CRuleRow
    {
        public ERule Rule;
        public CStatic Highlight;
        public CText Name;
        public CText Description;
        public CText State;
        public CStatic ArrowLeft;
        public CStatic ArrowRight;
    }

    public class CPartyScreenRuleRouletteRules : CPartyScreenRuleRoulette
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private const string _StaticRowHighlight = "StaticRowHighlight";
        private const string _StaticArrowLeft = "StaticArrowLeft";
        private const string _StaticArrowRight = "StaticArrowRight";
        private const string _TextRuleName = "TextRuleName";
        private const string _TextRuleDescription = "TextRuleDescription";
        private const string _TextRuleState = "TextRuleState";
        private const string _TextStateAlways = "TextStateAlways";
        private const string _TextStateRandom = "TextStateRandom";
        private const string _TextStateNever = "TextStateNever";

        private const float _FirstRowY = 190f;
        private const float _RowStep = 68f;
        private const float _DescriptionOffset = 33f;
        private const int _RowsPerColumn = 10;
        private const float _StateWidth = 190f;
        private const float _ArrowSpace = 6f;

        private readonly float[] _ColumnX = new float[] { 50f, 980f };

        private List<CRuleRow> _Rows;
        private int _SelectedRow;
        private bool _OnButtons;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { _ButtonNext, _ButtonBack };
            _ThemeStatics = new string[] { _StaticRowHighlight, _StaticArrowLeft, _StaticArrowRight };
            _ThemeTexts = new string[] { _TextRuleName, _TextRuleDescription, _TextRuleState, _TextStateAlways, _TextStateRandom, _TextStateNever };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Rows = new List<CRuleRow>();
            var rules = Enum.GetValues(typeof(ERule)).Cast<ERule>().ToList();
            for (var i = 0; i < rules.Count; i++)
            {
                var x = _ColumnX[i / _RowsPerColumn];
                var y = _FirstRowY + (i % _RowsPerColumn) * _RowStep;
                var row = new CRuleRow
                {
                    Rule = rules[i],
                    Highlight = GetNewStatic(_Statics[_StaticRowHighlight]),
                    Name = GetNewText(_Texts[_TextRuleName]),
                    Description = GetNewText(_Texts[_TextRuleDescription]),
                    State = GetNewText(_Texts[_TextRuleState]),
                    ArrowLeft = GetNewStatic(_Statics[_StaticArrowLeft]),
                    ArrowRight = GetNewStatic(_Statics[_StaticArrowRight])
                };

                var highlight = _Statics[_StaticRowHighlight].Rect;
                row.Highlight.MaxRect = new SRectF(x, y, highlight.W, highlight.H, highlight.Z);
                row.Name.X = x + 16f;
                row.Name.Y = y + 3f;
                row.Description.X = x + 16f;
                row.Description.Y = y + _DescriptionOffset;
                var arrow = _Statics[_StaticArrowRight].Rect;
                var arrowY = y + (highlight.H - arrow.H) / 2;
                var rightX = x + highlight.W - 12f - arrow.W;
                var leftX = rightX - _ArrowSpace - _StateWidth - _ArrowSpace - arrow.W;
                row.ArrowRight.MaxRect = new SRectF(rightX, arrowY, arrow.W, arrow.H, arrow.Z);
                row.ArrowLeft.MaxRect = new SRectF(leftX, arrowY, arrow.W, arrow.H, arrow.Z);
                row.State.X = leftX + arrow.W + _ArrowSpace + _StateWidth / 2;
                row.State.Y = y + (highlight.H - row.State.H) / 2;
                row.Name.Text = CPartyModeRuleRoulette.GetRuleKey(row.Rule);
                row.Description.Text = CPartyModeRuleRoulette.GetRuleKey(row.Rule) + "_DESC";

                _AddStatic(row.Highlight);
                _AddText(row.Name);
                _AddText(row.Description);
                _AddText(row.State);
                _AddStatic(row.ArrowLeft);
                _AddStatic(row.ArrowRight);
                _Rows.Add(row);
            }

            _Statics[_StaticRowHighlight].Visible = false;
            _Statics[_StaticArrowLeft].Visible = false;
            _Statics[_StaticArrowRight].Visible = false;
            _Texts[_TextRuleName].Visible = false;
            _Texts[_TextRuleDescription].Visible = false;
            _Texts[_TextRuleState].Visible = false;
            _Texts[_TextStateAlways].Visible = false;
            _Texts[_TextStateRandom].Visible = false;
            _Texts[_TextStateNever].Visible = false;
        }

        public override void OnShow()
        {
            base.OnShow();

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

                    if (_SelectedRow + 1 < _Rows.Count)
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
                        _ChangeState(_SelectedRow, keyEvent.Key == Keys.Left ? -1 : 1);
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
                for (var r = 0; r < _Rows.Count; r++)
                {
                    if (CHelper.IsInBounds(_Rows[r].ArrowLeft.Rect, mouseEvent))
                    {
                        _SelectRow(r);
                        _ChangeState(r, -1);
                        return true;
                    }

                    if (CHelper.IsInBounds(_Rows[r].Highlight.Rect, mouseEvent))
                    {
                        _SelectRow(r);
                        _ChangeState(r, 1);
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
            _SelectedRow = Math.Max(0, Math.Min(_Rows.Count - 1, row));
            _UpdateRows();
        }

        private void _ChangeState(int row, int direction)
        {
            if (row < 0 || row >= _Rows.Count)
            {
                return;
            }

            var rule = _Rows[row].Rule;
            var order = new ERuleState[] { ERuleState.Never, ERuleState.Random, ERuleState.Always };
            var index = Array.IndexOf(order, _PartyMode.GameData.RuleStates[rule]);
            index = (index + direction + order.Length) % order.Length;
            _PartyMode.GameData.RuleStates[rule] = order[index];
            _UpdateRows();
        }

        private void _UpdateRows()
        {
            for (var r = 0; r < _Rows.Count; r++)
            {
                var row = _Rows[r];
                row.Highlight.Visible = r == _SelectedRow && !_OnButtons;
                var arrowColor = r == _SelectedRow && !_OnButtons ? _Statics[_StaticArrowRight].Color : _Statics[_StaticArrowLeft].Color;
                row.ArrowLeft.Color = arrowColor;
                row.ArrowRight.Color = arrowColor;

                CText template;
                switch (_PartyMode.GameData.RuleStates[row.Rule])
                {
                    case ERuleState.Always:
                        template = _Texts[_TextStateAlways];
                        break;
                    case ERuleState.Never:
                        template = _Texts[_TextStateNever];
                        break;
                    default:
                        template = _Texts[_TextStateRandom];
                        break;
                }

                row.State.Text = template.Text;
                row.State.Color = template.Color;
                var nameColor = _Texts[_TextRuleName].Color;
                row.Name.Color = _PartyMode.GameData.RuleStates[row.Rule] == ERuleState.Never
                                     ? new SColorF(nameColor.R, nameColor.G, nameColor.B, nameColor.A * 0.45f)
                                     : nameColor;
            }
        }
    }
}
