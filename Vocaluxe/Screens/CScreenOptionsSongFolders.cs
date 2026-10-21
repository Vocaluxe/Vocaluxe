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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsSongFolders : CScreenOptionsBase
    {
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        protected override EScreen _OptionsScreen
        {
            get { return EScreen.OptionsSongFolders; }
        }

        private const int _NumRows = 8;

        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonAddFolder = "ButtonAddFolder";
        private const string _TextNoFolders = "TextNoFolders";
        private const string _TextWarningRestart = "TextWarningRestart";
        private const string _StaticWarningRestart = "StaticWarningRestart";
        private const string _StaticScrollBar = "StaticScrollBar";
        private const string _StaticScrollBarThumb = "StaticScrollBarThumb";

        private readonly string[] _StaticFolder = new string[_NumRows];
        private readonly string[] _TextFolder = new string[_NumRows];
        private readonly string[] _ButtonRemoveFolder = new string[_NumRows];

        private int _Offset;
        private int _WarningStream = -1;
        private bool _HasPlayedWarningSound;

        public override void Init()
        {
            base.Init();

            for (var i = 0; i < _NumRows; i++)
            {
                _StaticFolder[i] = "StaticFolder" + (i + 1);
                _TextFolder[i] = "TextFolder" + (i + 1);
                _ButtonRemoveFolder[i] = "ButtonRemoveFolder" + (i + 1);
            }

            var buttons = new List<string> { _ButtonExit, _ButtonAddFolder };
            buttons.AddRange(_ButtonRemoveFolder);
            _ThemeButtons = buttons.ToArray();

            var texts = new List<string> { _TextNoFolders, _TextWarningRestart };
            texts.AddRange(_TextFolder);
            _ThemeTexts = texts.ToArray();

            var statics = new List<string> { _StaticWarningRestart, _StaticScrollBar, _StaticScrollBarThumb };
            statics.AddRange(_StaticFolder);
            _ThemeStatics = statics.ToArray();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Texts[_TextWarningRestart].Visible = false;
            _Statics[_StaticWarningRestart].Visible = false;
            _UpdateList();
        }

        public override void OnShow()
        {
            base.OnShow();

            _Offset = 0;
            _HasPlayedWarningSound = false;
            _UpdateList();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_HandleNavigationInput(keyEvent))
            {
                return true;
            }

            if (!keyEvent.KeyPressed && _ScrollWithSelection(keyEvent.Key))
            {
                return true;
            }

            base.HandleInput(keyEvent);

            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        _Leave(EScreen.Main);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        _Leave(EScreen.Song);
                        break;

                    case Keys.PageUp:
                        _Scroll(-_NumRows);
                        break;

                    case Keys.PageDown:
                        _Scroll(_NumRows);
                        break;

                    case Keys.Enter:
                        _OnButtonAction();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_HandleNavigationMouse(mouseEvent))
            {
                return true;
            }

            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _OnButtonAction();
            }

            if (mouseEvent.RB)
            {
                _Leave(EScreen.Main);
            }

            if (mouseEvent.Wheel != 0)
            {
                _Scroll(mouseEvent.Wheel);
            }

            if ((mouseEvent.LB || mouseEvent.LBH) && _Statics[_StaticScrollBar].Visible)
            {
                var bar = _Statics[_StaticScrollBar].Rect;
                var area = new SRectF(bar.X - 15, bar.Y, bar.W + 30, bar.H, bar.Z);
                if (CHelper.IsInBounds(area, mouseEvent))
                {
                    _ScrollTo((mouseEvent.Y - bar.Y) / bar.H);
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_Texts[_TextWarningRestart].Visible && !_HasPlayedWarningSound)
            {
                _WarningStream = CSound.PlaySound(ESounds.Warning, false);
                CSound.SetStreamVolume(_WarningStream, CConfig.SoundEffectVolume);
                _HasPlayedWarningSound = true;
            }

            return true;
        }

        private void _OnButtonAction()
        {
            if (_Buttons[_ButtonExit].Selected)
            {
                _Leave(EScreen.Main);
                return;
            }

            if (_Buttons[_ButtonAddFolder].Selected)
            {
                _AddFolder();
                return;
            }

            for (var i = 0; i < _NumRows; i++)
            {
                if (_Buttons[_ButtonRemoveFolder[i]].Selected)
                {
                    _RemoveFolder(_Offset + i);
                    return;
                }
            }
        }

        private void _AddFolder()
        {
            var folder = _OpenSongFolderDialog();
            if (folder == null)
            {
                return;
            }

            // Update config with new folder
            var folders = _GetSongFolders().ToList();
            if (!folders.Contains(folder))
            {
                folders.Add(folder);
                CConfig.Config.Game.SongFolder = folders.ToArray();
                CConfig.SaveConfig();
                _ShowRestartWarning();
            }

            _Offset = Math.Max(0, folders.IndexOf(folder) - _NumRows + 1);
            _UpdateList();
        }

        private void _RemoveFolder(int index)
        {
            var folders = _GetSongFolders().ToList();
            if (index < 0 || index >= folders.Count)
            {
                return;
            }

            folders.RemoveAt(index);
            CConfig.Config.Game.SongFolder = folders.ToArray();
            CConfig.SaveConfig();
            _ShowRestartWarning();
            _UpdateList();
        }

        private void _Scroll(int rows)
        {
            _Offset += rows;
            _UpdateList();
        }

        private bool _ScrollWithSelection(Keys key)
        {
            var numFolders = _GetSongFolders().Length;
            if (key == Keys.Down && _Buttons[_ButtonRemoveFolder[_NumRows - 1]].Selected && _Offset + _NumRows < numFolders)
            {
                _Scroll(1);
                return true;
            }

            if (key == Keys.Up && _Buttons[_ButtonRemoveFolder[0]].Selected && _Offset > 0)
            {
                _Scroll(-1);
                return true;
            }

            return false;
        }

        private void _ScrollTo(float position)
        {
            var maxOffset = Math.Max(0, _GetSongFolders().Length - _NumRows);
            _Offset = (int)Math.Round(position.Clamp(0f, 1f) * maxOffset);
            _UpdateList();
        }

        private void _UpdateList()
        {
            var folders = _GetSongFolders();
            _Offset = _Offset.Clamp(0, Math.Max(0, folders.Length - _NumRows));

            for (var i = 0; i < _NumRows; i++)
            {
                var index = _Offset + i;
                var visible = index < folders.Length;
                _Statics[_StaticFolder[i]].Visible = visible;
                _Texts[_TextFolder[i]].Visible = visible;
                _Buttons[_ButtonRemoveFolder[i]].Visible = visible;
                if (visible)
                {
                    _Texts[_TextFolder[i]].Text = folders[index];
                    _Texts[_TextFolder[i]].Alpha = Directory.Exists(folders[index]) ? 1f : 0.5f;
                }
            }

            _Texts[_TextNoFolders].Visible = folders.Length == 0;
            _UpdateScrollBar(folders.Length);

            if (!_Buttons.Any(button => button.Selected && button.Visible))
            {
                _SelectElement(_Buttons[_ButtonAddFolder]);
            }
        }

        private void _UpdateScrollBar(int numFolders)
        {
            var scrollBar = _Statics[_StaticScrollBar];
            var thumb = _Statics[_StaticScrollBarThumb];
            var visible = numFolders > _NumRows;
            scrollBar.Visible = visible;
            thumb.Visible = visible;
            if (!visible)
            {
                return;
            }

            var track = scrollBar.Rect;
            var thumbH = Math.Min(track.H, Math.Max(track.H * _NumRows / numFolders, thumb.Rect.W * 3f));
            thumb.Y = track.Y + (track.H - thumbH) * _Offset / (numFolders - _NumRows);
            thumb.H = thumbH;
        }

        private void _ShowRestartWarning()
        {
            _Texts[_TextWarningRestart].Visible = true;
            _Statics[_StaticWarningRestart].Visible = true;
        }

        private void _Leave(EScreen screen)
        {
            _SaveOptions();
            CGraphics.FadeTo(screen);
        }

        protected override void _SaveOptions()
        {
            if (_WarningStream != -1)
            {
                CSound.Close(_WarningStream);
                _WarningStream = -1;
            }
        }

        private static string[] _GetSongFolders()
        {
            return CConfig.Config.Game.SongFolder?.Where(folder => !string.IsNullOrEmpty(folder)).ToArray() ?? new string[0];
        }

        private static string _OpenSongFolderDialog()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = CLanguage.Translate("TR_SCREENO_SONGFOLDER");
                dialog.ShowNewFolderButton = true;
                var owner = Application.OpenForms.Cast<Form>().LastOrDefault(form => form.Visible);
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    return dialog.SelectedPath; // Folder was selected
                }
            }

            return null; // Dialog was cancelled
        }
    }
}
