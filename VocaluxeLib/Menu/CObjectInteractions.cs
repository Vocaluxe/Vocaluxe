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
using System.Drawing;
using System.Windows.Forms;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;

namespace VocaluxeLib.Menu
{
    public abstract class CObjectInteractions
    {
        private readonly List<CInteraction> _Interactions = new List<CInteraction>();
        private int _Selection;

        private int _PrevMouseX;
        private int _PrevMouseY;

        protected int _MouseDX;
        protected int _MouseDY;

        protected bool _Active;

        protected readonly COrderedDictionaryLite<CButton> _Buttons;
        protected readonly COrderedDictionaryLite<CText> _Texts;
        protected readonly COrderedDictionaryLite<CBackground> _Backgrounds;
        protected readonly COrderedDictionaryLite<CStatic> _Statics;
        protected readonly COrderedDictionaryLite<CSelectSlide> _SelectSlides;
        protected readonly COrderedDictionaryLite<CSongMenu> _SongMenus;
        protected readonly COrderedDictionaryLite<CLyric> _Lyrics;
        protected readonly COrderedDictionaryLite<CSingNotes> _SingNotes;
        protected readonly COrderedDictionaryLite<CNameSelection> _NameSelections;
        protected readonly COrderedDictionaryLite<CEqualizer> _Equalizers;
        protected readonly COrderedDictionaryLite<CPlaylist> _Playlists;
        protected readonly COrderedDictionaryLite<CParticleEffect> _ParticleEffects;
        protected readonly COrderedDictionaryLite<CScreenSetting> _ScreenSettings;

        public CObjectInteractions()
        {
            _Backgrounds = new COrderedDictionaryLite<CBackground>(this);
            _Buttons = new COrderedDictionaryLite<CButton>(this);
            _Texts = new COrderedDictionaryLite<CText>(this);
            _Statics = new COrderedDictionaryLite<CStatic>(this);
            _SelectSlides = new COrderedDictionaryLite<CSelectSlide>(this);
            _SongMenus = new COrderedDictionaryLite<CSongMenu>(this);
            _Lyrics = new COrderedDictionaryLite<CLyric>(this);
            _SingNotes = new COrderedDictionaryLite<CSingNotes>(this);
            _NameSelections = new COrderedDictionaryLite<CNameSelection>(this);
            _Equalizers = new COrderedDictionaryLite<CEqualizer>(this);
            _Playlists = new COrderedDictionaryLite<CPlaylist>(this);
            _ParticleEffects = new COrderedDictionaryLite<CParticleEffect>(this);
            _ScreenSettings = new COrderedDictionaryLite<CScreenSetting>(this);
        }

        public virtual void Init()
        {
            _Selection = 0;

            _PrevMouseX = 0;
            _PrevMouseY = 0;

            _MouseDX = 0;
            _MouseDY = 0;

            _Active = false;
        }

        protected void _ClearElements()
        {
            _Interactions.Clear();
            _Backgrounds.Clear();
            _Buttons.Clear();
            _Texts.Clear();
            _Statics.Clear();
            _SelectSlides.Clear();
            _SongMenus.Clear();
            _Lyrics.Clear();
            _SingNotes.Clear();
            _NameSelections.Clear();
            _Equalizers.Clear();
            _Playlists.Clear();
            _ParticleEffects.Clear();
            _ScreenSettings.Clear();
        }

        #region MenuHandler
        public virtual bool HandleInput(SKeyEvent keyEvent)
        {
            if (!CBase.Settings.IsTabNavigation())
            {
                if (keyEvent.Key == Keys.Left)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = _PrevElement();
                    else
                        keyEvent.Handled = _NextInteraction(keyEvent);
                }

                if (keyEvent.Key == Keys.Right)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = _NextElement();
                    else
                        keyEvent.Handled = _NextInteraction(keyEvent);
                }

                if (keyEvent.Key == Keys.Up || keyEvent.Key == Keys.Down)
                    keyEvent.Handled = _NextInteraction(keyEvent);
            }
            else
            {
                if (keyEvent.Key == Keys.Tab)
                {
                    if (keyEvent.Mod == EModifier.Shift)
                        PrevInteraction();
                    else
                        NextInteraction();
                }

                if (keyEvent.Key == Keys.Left)
                    _PrevElement();

                if (keyEvent.Key == Keys.Right)
                    _NextElement();
            }

            return true;
        }

        public virtual bool HandleMouse(SMouseEvent mouseEvent)
        {
            int selection = _Selection;
            ProcessMouseMove(mouseEvent.X, mouseEvent.Y);
            if (selection != _Selection)
            {
                _UnsetHighlighted(selection);
                _SetHighlighted(_Selection);
            }

            if (mouseEvent.LB)
                ProcessMouseClick(mouseEvent.X, mouseEvent.Y);

            _PrevMouseX = mouseEvent.X;
            _PrevMouseY = mouseEvent.Y;

            return true;
        }

        public virtual bool HandleInputThemeEditor(SKeyEvent keyEvent)
        {
            _UnsetHighlighted(_Selection);
            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Up:
                        if (keyEvent.Mod == EModifier.Ctrl)
                            _MoveElement(0, -1);

                        if (keyEvent.Mod == EModifier.Shift)
                            _ResizeElement(0, 1);

                        break;
                    case Keys.Down:
                        if (keyEvent.Mod == EModifier.Ctrl)
                            _MoveElement(0, 1);

                        if (keyEvent.Mod == EModifier.Shift)
                            _ResizeElement(0, -1);

                        break;

                    case Keys.Right:
                        if (keyEvent.Mod == EModifier.Ctrl)
                            _MoveElement(1, 0);

                        if (keyEvent.Mod == EModifier.Shift)
                            _ResizeElement(1, 0);

                        if (keyEvent.Mod == EModifier.None)
                            NextInteraction();
                        break;
                    case Keys.Left:
                        if (keyEvent.Mod == EModifier.Ctrl)
                            _MoveElement(-1, 0);

                        if (keyEvent.Mod == EModifier.Shift)
                            _ResizeElement(-1, 0);

                        if (keyEvent.Mod == EModifier.None)
                            PrevInteraction();
                        break;
                }
            }
            return true;
        }

        public virtual bool HandleMouseThemeEditor(SMouseEvent mouseEvent)
        {
            _UnsetHighlighted(_Selection);
            _MouseDX = mouseEvent.X - _PrevMouseX;
            _MouseDY = mouseEvent.Y - _PrevMouseY;

            int stepX = 0;
            int stepY = 0;

            if ((mouseEvent.Mod & EModifier.Ctrl) == EModifier.Ctrl)
            {
                _PrevMouseX = mouseEvent.X;
                _PrevMouseY = mouseEvent.Y;
            }
            else
            {
                while (Math.Abs(mouseEvent.X - _PrevMouseX) >= 5)
                {
                    if (mouseEvent.X - _PrevMouseX >= 5)
                        stepX += 5;

                    if (mouseEvent.X - _PrevMouseX <= -5)
                        stepX -= 5;

                    _PrevMouseX = mouseEvent.X - (_MouseDX - stepX);
                }

                while (Math.Abs(mouseEvent.Y - _PrevMouseY) >= 5)
                {
                    if (mouseEvent.Y - _PrevMouseY >= 5)
                        stepY += 5;

                    if (mouseEvent.Y - _PrevMouseY <= -5)
                        stepY -= 5;

                    _PrevMouseY = mouseEvent.Y - (_MouseDY - stepY);
                }
            }

            if (mouseEvent.LBH)
            {
                //if (IsMouseOver(MouseEvent.X, _PrevMouseY))
                //{
                if (mouseEvent.Mod == EModifier.None)
                    _MoveElement(stepX, stepY);

                if (mouseEvent.Mod == EModifier.Ctrl)
                    _MoveElement(_MouseDX, _MouseDY);

                if (mouseEvent.Mod == EModifier.Shift)
                    _ResizeElement(stepX, stepY);

                if (mouseEvent.Mod == (EModifier.Shift | EModifier.Ctrl))
                    _ResizeElement(_MouseDX, _MouseDY);
                //}
            }
            else
                ProcessMouseMove(mouseEvent.X, mouseEvent.Y);

            return true;
        }
        #endregion MenuHandler

        #region Drawing
        public virtual bool Draw()
        {
            _DrawBG();
            _DrawFG();
            return true;
        }

        protected void _DrawBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Draw();
        }

        protected void _DrawFG()
        {
            if (_Interactions.Count <= 0)
                return;

            var items = new List<SZSort>();

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_IsVisible(i) && _Interactions[i].DrawAsForeground)
                {
                    var zs = new SZSort {ID = i, Z = _GetZValue(i)};
                    items.Add(zs);
                }
            }

            if (items.Count <= 0)
                return;


            items.Sort((s1, s2) => s2.Z.CompareTo(s1.Z));

            for (int i = 0; i < items.Count; i++)
                _DrawInteraction(items[i].ID);
        }
        #endregion Drawing

        #region Element-Adding
        protected void _AddBackground(CBackground bg, String key = null)
        {
            _AddInteraction(_Backgrounds.Add(bg, key), EType.Background);
        }

        protected void _AddButton(CButton button, String key = null)
        {
            _AddInteraction(_Buttons.Add(button, key), EType.Button);
        }

        protected void _AddSelectSlide(CSelectSlide slide, String key = null)
        {
            _AddInteraction(_SelectSlides.Add(slide, key), EType.SelectSlide);
        }

        protected void _AddStatic(CStatic stat, String key = null)
        {
            _AddInteraction(_Statics.Add(stat, key), EType.Static);
        }

        protected void _AddText(CText text, String key = null)
        {
            _AddInteraction(_Texts.Add(text, key), EType.Text);
        }

        protected void _AddSongMenu(CSongMenu songmenu, String key = null)
        {
            _AddInteraction(_SongMenus.Add(songmenu, key), EType.SongMenu);
        }

        protected void _AddLyric(CLyric lyric, String key = null)
        {
            _AddInteraction(_Lyrics.Add(lyric, key), EType.Lyric);
        }

        protected void _AddSingNote(CSingNotes sn, String key = null)
        {
            _AddInteraction(_SingNotes.Add(sn, key), EType.SingNote);
        }

        protected void _AddNameSelection(CNameSelection ns, String key = null)
        {
            _AddInteraction(_NameSelections.Add(ns, key), EType.NameSelection);
        }

        protected void _AddEqualizer(CEqualizer eq, String key = null)
        {
            _AddInteraction(_Equalizers.Add(eq, key), EType.Equalizer);
        }

        protected void _AddPlaylist(CPlaylist pls, String key = null)
        {
            _AddInteraction(_Playlists.Add(pls, key), EType.Playlist);
        }

        protected void _AddParticleEffect(CParticleEffect pe, String key = null)
        {
            _AddInteraction(_ParticleEffects.Add(pe, key), EType.ParticleEffect);
        }

        protected void _AddScreenSetting(CScreenSetting se, String key = null)
        {
            _ScreenSettings.Add(se, key);
        }
        #endregion Element-Adding

        #region InteractionHandling
        public void NextInteraction()
        {
            if (_Interactions.Count > 0)
                _NextInteraction();
        }

        public void PrevInteraction()
        {
            if (_Interactions.Count > 0)
                _PrevInteraction();
        }

        /// <summary>
        ///     Selects the next element in a menu Interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the Interaction does not provide such a method.</returns>
        public bool NextElement()
        {
            if (_Interactions.Count > 0)
                return _NextElement();

            return false;
        }

        /// <summary>
        ///     Selects the previous element in a menu Interaction.
        /// </summary>
        /// <returns>True if the previous element is selected. False if either there is no next element or the Interaction does not provide such a method.</returns>
        public bool PrevElement()
        {
            if (_Interactions.Count > 0)
                return _PrevElement();

            return false;
        }

        protected void _SetInteractionToButton(CButton button)
        {
            if (!button.Visible)
                return;
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type != EType.Button)
                    continue;
                if (_Buttons[_Interactions[i].Num] == button)
                {
                    _UnsetSelected();
                    _UnsetHighlighted(_Selection);
                    _Selection = i;
                    _SetSelected();
                    return;
                }
            }
        }

        protected void _SetInteractionToSelectSlide(CSelectSlide slide)
        {
            if (!slide.Visible)
                return;
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type != EType.SelectSlide)
                    continue;
                if (_SelectSlides[_Interactions[i].Num] == slide)
                {
                    _UnsetSelected();
                    _UnsetHighlighted(_Selection);
                    _Selection = i;
                    _SetSelected();
                    return;
                }
            }
        }

        public void ProcessMouseClick(int x, int y)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.SelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseLBClick(x, y);
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            _SelectByMouse(x, y);

            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.SelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseMove(x, y);
            }
        }

        private void _SelectByMouse(int x, int y)
        {
            float z = CBase.Settings.GetZFar();
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if ((CBase.Settings.GetProgramState() != EProgramState.EditTheme) && (_Interactions[i].ThemeEditorOnly || !_IsVisible(i) || !_IsEnabled(i)))
                    continue;
                if (!_IsMouseOverElement(x, y, _Interactions[i]))
                    continue;
                if (_GetZValue(_Interactions[i]) > z)
                    continue;
                z = _GetZValue(_Interactions[i]);
                _UnsetSelected();
                _Selection = i;
                _SetSelected();
            }
        }

        protected bool _IsMouseOverCurSelection(SMouseEvent mouseEvent)
        {
            return _IsMouseOverCurSelection(mouseEvent.X, mouseEvent.Y);
        }

        private bool _IsMouseOverCurSelection(int x, int y)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            return _Interactions.Count > 0 && _IsMouseOverElement(x, y, _Interactions[_Selection]);
        }

        private bool _IsMouseOverElement(int x, int y, CInteraction interact)
        {
            bool result = CHelper.IsInBounds(_GetRect(interact), x, y);
            if (result)
                return true;
            if (interact.Type == EType.SelectSlide)
            {
                return CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowLeft, x, y) ||
                       CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowRight, x, y);
            }
            return false;
        }

        private float _GetZValue(CInteraction interact)
        {
            switch (interact.Type)
            {
                case EType.Button:
                    return _Buttons[interact.Num].Rect.Z;
                case EType.SelectSlide:
                    return _SelectSlides[interact.Num].Rect.Z;
                case EType.Static:
                    return _Statics[interact.Num].Rect.Z;
                case EType.SongMenu:
                    return _SongMenus[interact.Num].Rect.Z;
                case EType.Text:
                    return _Texts[interact.Num].Z;
                case EType.Lyric:
                    return _Lyrics[interact.Num].Rect.Z;
                case EType.NameSelection:
                    return _NameSelections[interact.Num].Rect.Z;
                case EType.Equalizer:
                    return _Equalizers[interact.Num].Rect.Z;
                case EType.Playlist:
                    return _Playlists[interact.Num].Rect.Z;
                case EType.ParticleEffect:
                    return _ParticleEffects[interact.Num].Rect.Z;
            }
            return CBase.Settings.GetZFar();
        }

        private float _GetZValue(int interaction)
        {
            return _GetZValue(_Interactions[interaction]);
        }

        private void _NextInteraction()
        {
            _UnsetSelected();
            if (CBase.Settings.GetProgramState() != EProgramState.EditTheme)
            {
                bool found = false;
                int start = _Selection;
                do
                {
                    start++;
                    if (start > _Interactions.Count - 1)
                        start = 0;

                    if ((start == _Selection) || (!_Interactions[start].ThemeEditorOnly && _IsVisible(start) && _IsEnabled(start)))
                        found = true;
                } while (!found);
                _Selection = start;
            }
            else
            {
                _Selection++;
                if (_Selection > _Interactions.Count - 1)
                    _Selection = 0;
            }
            _SetSelected();
        }

        private void _PrevInteraction()
        {
            _UnsetSelected();
            if (CBase.Settings.GetProgramState() != EProgramState.EditTheme)
            {
                bool found = false;
                int start = _Selection;
                do
                {
                    start--;
                    if (start < 0)
                        start = _Interactions.Count - 1;

                    if ((start == _Selection) || (!_Interactions[start].ThemeEditorOnly && _IsVisible(start) && _IsEnabled(start)))
                        found = true;
                } while (!found);
                _Selection = start;
            }
            else
            {
                _Selection--;
                if (_Selection < 0)
                    _Selection = _Interactions.Count - 1;
            }
            _SetSelected();
        }

        /// <summary>
        ///     Selects the next best interaction in a menu.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool _NextInteraction(SKeyEvent key)
        {
            var directions = new SKeyEvent[4];
            var distances = new float[4];
            var stages = new int[4];
            var elements = new int[4];

            for (int i = 0; i < 4; i++)
                directions[i] = new SKeyEvent();

            directions[0].Key = Keys.Up;
            directions[1].Key = Keys.Right;
            directions[2].Key = Keys.Down;
            directions[3].Key = Keys.Left;

            for (int i = 0; i < 4; i++)
                elements[i] = _GetNextElement(directions[i], out distances[i], out stages[i]);

            int element = _Selection;
            int stage = int.MaxValue;
            int direction = -1;

            int mute = -1;
            switch (key.Key)
            {
                case Keys.Up:
                    mute = 2;
                    break;
                case Keys.Right:
                    mute = 3;
                    break;
                case Keys.Down:
                    mute = 0;
                    break;
                case Keys.Left:
                    mute = 1;
                    break;
            }

            for (int i = 0; i < 4; i++)
            {
                if (i != mute && elements[i] != _Selection && (stages[i] <= stage && directions[i].Key == key.Key))
                {
                    stage = stages[i];
                    element = elements[i];
                    direction = i;
                }
            }

            if (direction != -1)
            {
                // select the new element
                if (directions[direction].Key == key.Key)
                {
                    _UnsetHighlighted(_Selection);
                    _UnsetSelected();

                    _Selection = element;
                    _SetSelected();
                    _SetHighlighted(_Selection);

                    return true;
                }
            }
            return false;
        }

        private int _GetNextElement(SKeyEvent key, out float distance, out int stage)
        {
            distance = float.MaxValue;
            int min = _Selection;
            SRectF actualRect = _GetRect(_Selection);
            stage = int.MaxValue;

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                {
                    SRectF targetRect = _GetRect(i);
                    float dist = _GetDistanceDirect(key, actualRect, targetRect);
                    if (dist >= 0f && dist < distance)
                    {
                        distance = dist;
                        min = i;
                        stage = 10;
                    }
                }
            }

            if (min == _Selection)
            {
                for (int i = 0; i < _Interactions.Count; i++)
                {
                    if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                    {
                        SRectF targetRect = _GetRect(i);
                        float dist = _GetDistance180(key, actualRect, targetRect);
                        if (dist >= 0f && dist < distance)
                        {
                            distance = dist;
                            min = i;
                            stage = 20;
                        }
                    }
                }
            }

            if (min == _Selection)
            {
                switch (key.Key)
                {
                    case Keys.Up:
                        actualRect = new SRectF(actualRect.X, CBase.Settings.GetRenderH(), 1, 1, actualRect.Z);
                        break;
                    case Keys.Down:
                        actualRect = new SRectF(actualRect.X, 0, 1, 1, actualRect.Z);
                        break;
                    case Keys.Left:
                        actualRect = new SRectF(CBase.Settings.GetRenderW(), actualRect.Y, 1, 1, actualRect.Z);
                        break;
                    case Keys.Right:
                        actualRect = new SRectF(0, actualRect.Y, 1, 1, actualRect.Z);
                        break;
                }

                for (int i = 0; i < _Interactions.Count; i++)
                {
                    if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                    {
                        SRectF targetRect = _GetRect(i);
                        float dist = _GetDistance180(key, actualRect, targetRect);
                        if (dist >= 0f && dist < distance)
                        {
                            distance = dist;
                            min = i;
                            stage = 30;
                        }
                    }
                }
            }
            return min;
        }

        private float _GetDistanceDirect(SKeyEvent key, SRectF actualRect, SRectF targetRect)
        {
            var source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            var dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            var vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            var distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (key.Key)
            {
                case Keys.Up:
                    if (vector.Y < 0f && (targetRect.X + targetRect.W > actualRect.X && actualRect.X + actualRect.W > targetRect.X))
                        inDirection = true;
                    break;

                case Keys.Down:
                    if (vector.Y > 0f && (targetRect.X + targetRect.W > actualRect.X && actualRect.X + actualRect.W > targetRect.X))
                        inDirection = true;
                    break;

                case Keys.Left:
                    if (vector.X < 0f && (targetRect.Y + targetRect.H > actualRect.Y && actualRect.Y + actualRect.H > targetRect.Y))
                        inDirection = true;
                    break;

                case Keys.Right:
                    if (vector.X > 0f && (targetRect.Y + targetRect.H > actualRect.Y && actualRect.Y + actualRect.H > targetRect.Y))
                        inDirection = true;
                    break;
            }
            return !inDirection ? float.MaxValue : distance;
        }

        private float _GetDistance180(SKeyEvent key, SRectF actualRect, SRectF targetRect)
        {
            var source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            var dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            var vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            var distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (key.Key)
            {
                case Keys.Up:
                    if (vector.Y < 0f)
                        inDirection = true;
                    break;

                case Keys.Down:
                    if (vector.Y > 0f)
                        inDirection = true;
                    break;

                case Keys.Left:
                    if (vector.X < 0f)
                        inDirection = true;
                    break;

                case Keys.Right:
                    if (vector.X > 0f)
                        inDirection = true;
                    break;
            }
            return !inDirection ? float.MaxValue : distance;
        }

        /// <summary>
        ///     Selects the next element in a menu interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        private bool _NextElement()
        {
            if (_Interactions[_Selection].Type == EType.SelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].NextValue();

            return false;
        }

        /// <summary>
        ///     Selects the previous element in a menu interaction.
        /// </summary>
        /// <returns>
        ///     True if the previous element is selected. False if either there is no previous element or the interaction does not provide such a method.
        /// </returns>
        private bool _PrevElement()
        {
            if (_Interactions[_Selection].Type == EType.SelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].PrevValue();

            return false;
        }

        private void _SetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.Button:
                    _Buttons[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.SelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.Static:
                    _Statics[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.Text:
                    _Texts[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.SongMenu:
                    _SongMenus[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.Lyric:
                    _Lyrics[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.NameSelection:
                    _NameSelections[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.Equalizer:
                    _Equalizers[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.Playlist:
                    _Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.ParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _UnsetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.Button:
                    _Buttons[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.SelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.Static:
                    _Statics[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.Text:
                    _Texts[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.SongMenu:
                    _SongMenus[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.Lyric:
                    _Lyrics[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.NameSelection:
                    _NameSelections[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.Equalizer:
                    _Equalizers[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.Playlist:
                    _Playlists[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.ParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].Selected = false;
                    break;
            }
        }

        private void _SetHighlighted(int selection)
        {
            if (selection >= _Interactions.Count || selection < 0)
                return;

            switch (_Interactions[selection].Type)
            {
                case EType.Button:
                    //_Buttons[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.SelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = true;
                    break;
                case EType.Static:
                    //_Statics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.Text:
                    //_Texts[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.SongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.Lyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.NameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.Playlist:
                    //_Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _UnsetHighlighted(int selection)
        {
            if (selection >= _Interactions.Count || selection < 0)
                return;

            switch (_Interactions[selection].Type)
            {
                case EType.Button:
                    //_Buttons[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.SelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = false;
                    break;
                case EType.Static:
                    //_Statics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.Text:
                    //_Texts[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.SongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.Lyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.NameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.Playlist:
                    //_Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private bool _IsVisible(int interaction)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            switch (_Interactions[interaction].Type)
            {
                case EType.Button:
                    return _Buttons[_Interactions[interaction].Num].Visible;

                case EType.SelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;

                case EType.Static:
                    return _Statics[_Interactions[interaction].Num].Visible;

                case EType.Text:
                    return _Texts[_Interactions[interaction].Num].Visible;

                case EType.SongMenu:
                    return _SongMenus[_Interactions[interaction].Num].Visible;

                case EType.Lyric:
                    return _Lyrics[_Interactions[interaction].Num].Visible;

                case EType.NameSelection:
                    return _NameSelections[_Interactions[interaction].Num].Visible;

                case EType.Equalizer:
                    return _Equalizers[_Interactions[interaction].Num].Visible;

                case EType.Playlist:
                    return _Playlists[_Interactions[interaction].Num].Visible;

                case EType.ParticleEffect:
                    return _ParticleEffects[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private bool _IsEnabled(int interaction)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            switch (_Interactions[interaction].Type)
            {
                case EType.Button:
                    return _Buttons[_Interactions[interaction].Num].Enabled;

                case EType.SelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;

                case EType.Static:
                    return false; //_Statics[_Interactions[interaction].Num].Visible;

                case EType.Text:
                    return false; //_Texts[_Interactions[interaction].Num].Visible;

                case EType.SongMenu:
                    return _SongMenus[_Interactions[interaction].Num].Visible;

                case EType.Lyric:
                    return false; //_Lyrics[_Interactions[interaction].Num].Visible;

                case EType.NameSelection:
                    return _NameSelections[_Interactions[interaction].Num].Visible;

                case EType.Equalizer:
                    return false; //_Equalizers[_Interactions[interaction].Num].Visible;

                case EType.Playlist:
                    return _Playlists[_Interactions[interaction].Num].Visible;

                case EType.ParticleEffect:
                    return false; //_ParticleEffects[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private SRectF _GetRect(CInteraction interaction)
        {
            var result = new SRectF();
            switch (interaction.Type)
            {
                case EType.Button:
                    return _Buttons[interaction.Num].Rect;

                case EType.SelectSlide:
                    return _SelectSlides[interaction.Num].Rect;

                case EType.Static:
                    return _Statics[interaction.Num].Rect;

                case EType.Text:
                    return _Texts[interaction.Num].Rect;

                case EType.SongMenu:
                    return _SongMenus[interaction.Num].Rect;

                case EType.Lyric:
                    return _Lyrics[interaction.Num].Rect;

                case EType.NameSelection:
                    return _NameSelections[interaction.Num].Rect;

                case EType.Equalizer:
                    return _Equalizers[interaction.Num].Rect;

                case EType.Playlist:
                    return _Playlists[interaction.Num].Rect;

                case EType.ParticleEffect:
                    return _ParticleEffects[interaction.Num].Rect;
            }

            return result;
        }

        private SRectF _GetRect(int interaction)
        {
            return _GetRect(_Interactions[interaction]);
        }

        private void _AddInteraction(int num, EType type)
        {
            _Interactions.Add(new CInteraction(num, type));
            if (!_Interactions[_Selection].ThemeEditorOnly)
                _SetSelected();
            else
                _NextInteraction();
        }

        private void _DrawInteraction(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.Button:
                    _Buttons[_Interactions[interaction].Num].Draw();
                    break;

                case EType.SelectSlide:
                    _SelectSlides[_Interactions[interaction].Num].Draw();
                    break;

                case EType.Static:
                    _Statics[_Interactions[interaction].Num].Draw();
                    break;

                case EType.Text:
                    _Texts[_Interactions[interaction].Num].Draw();
                    break;

                case EType.SongMenu:
                    _SongMenus[_Interactions[interaction].Num].Draw();
                    break;

                case EType.NameSelection:
                    _NameSelections[_Interactions[interaction].Num].Draw();
                    break;

                case EType.Equalizer:
                    if (!_Equalizers[_Interactions[interaction].Num].ScreenHandles)
                    {
                        //TODO:
                        //Call Update-Method of Equalizer and give infos about bg-sound.
                        //_Equalizers[_Interactions[interaction].Num].Draw();
                    }
                    break;

                case EType.Playlist:
                    _Playlists[_Interactions[interaction].Num].Draw();
                    break;

                case EType.ParticleEffect:
                    _ParticleEffects[_Interactions[interaction].Num].Draw();
                    break;

                    //TODO:
                    //case EType.TLyric:
                    //    _Lyrics[_Interactions[interaction].Num].Draw(0);
                    //    break;
            }
        }
        #endregion InteractionHandling

        #region Theme Handling
        private void _MoveElement(int stepX, int stepY)
        {
            if (_Interactions.Count <= 0)
                return;
            switch (_Interactions[_Selection].Type)
            {
                case EType.Button:
                    _Buttons[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.SelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.Static:
                    _Statics[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.Text:
                    _Texts[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.SongMenu:
                    _SongMenus[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.Lyric:
                    _Lyrics[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.NameSelection:
                    _NameSelections[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.Equalizer:
                    _Equalizers[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.Playlist:
                    _Playlists[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;

                case EType.ParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                    break;
            }
        }

        private void _ResizeElement(int stepW, int stepH)
        {
            if (_Interactions.Count <= 0)
                return;
            switch (_Interactions[_Selection].Type)
            {
                case EType.Button:
                    _Buttons[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.SelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.Static:
                    _Statics[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.Text:
                    _Texts[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.SongMenu:
                    _SongMenus[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.Lyric:
                    _Lyrics[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.NameSelection:
                    _NameSelections[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.Equalizer:
                    _Equalizers[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.Playlist:
                    _Playlists[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;

                case EType.ParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                    break;
            }
        }
        #endregion Theme Handling
    }
}