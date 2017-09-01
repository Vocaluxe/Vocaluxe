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
using System.Drawing;
using System.Windows.Forms;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;

namespace VocaluxeLib.Menu
{
    public abstract class CObjectInteractions
    {
        protected readonly List<CInteraction> _Elements = new List<CInteraction>();
        private int _Selection;

        private Point _PrevMouse;

        protected bool _Active;

        protected readonly COrderedDictionaryLite<CButton> _Buttons;
        protected readonly COrderedDictionaryLite<CText> _Texts;
        protected readonly COrderedDictionaryLite<CBackground> _Backgrounds;
        protected readonly COrderedDictionaryLite<CStatic> _Statics;
        protected readonly COrderedDictionaryLite<CSelectSlide> _SelectSlides;
        protected readonly COrderedDictionaryLite<ISongMenu> _SongMenus;
        protected readonly COrderedDictionaryLite<CLyric> _Lyrics;
        protected readonly COrderedDictionaryLite<CSingNotes> _SingNotes;
        protected readonly COrderedDictionaryLite<CNameSelection> _NameSelections;
        protected readonly COrderedDictionaryLite<CEqualizer> _Equalizers;
        protected readonly COrderedDictionaryLite<CPlaylist> _Playlists;
        protected readonly COrderedDictionaryLite<CParticleEffect> _ParticleEffects;
        private readonly SColorF _HighlightColor = new SColorF(1, 0, 0, 0);

        protected CObjectInteractions()
        {
            _Backgrounds = new COrderedDictionaryLite<CBackground>(this);
            _Buttons = new COrderedDictionaryLite<CButton>(this);
            _Texts = new COrderedDictionaryLite<CText>(this);
            _Statics = new COrderedDictionaryLite<CStatic>(this);
            _SelectSlides = new COrderedDictionaryLite<CSelectSlide>(this);
            _SongMenus = new COrderedDictionaryLite<ISongMenu>(this);
            _Lyrics = new COrderedDictionaryLite<CLyric>(this);
            _SingNotes = new COrderedDictionaryLite<CSingNotes>(this);
            _NameSelections = new COrderedDictionaryLite<CNameSelection>(this);
            _Equalizers = new COrderedDictionaryLite<CEqualizer>(this);
            _Playlists = new COrderedDictionaryLite<CPlaylist>(this);
            _ParticleEffects = new COrderedDictionaryLite<CParticleEffect>(this);
        }

        public virtual void Init()
        {
            _Selection = -1;
            _PrevMouse = new Point(0, 0);
            _Active = false;
        }

        protected virtual void _ClearElements()
        {
            _Elements.Clear();
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
        }

        private void _SetSelected(int newSelection)
        {
            IMenuElement el = _GetElement(_Selection);
            if (newSelection == _Selection)
            {
                // Don't change current selection
                if (el == null)
                    _Selection = -1;
                else if (!el.Selected)
                    el.Selected = true;
                return;
            }
            if (el != null)
                el.Selected = false;
            _Selection = newSelection;
            el = _GetElement(_Selection);
            if (el != null)
                el.Selected = true;
        }

        #region MenuHandler
        public virtual bool HandleInput(SKeyEvent keyEvent)
        {
            if (!CBase.Settings.IsTabNavigation())
            {
                if (keyEvent.Key == Keys.Left)
                {
                    if (_IsSelectionValid() && _Elements[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = PrevValue();
                    else
                        keyEvent.Handled = _NextElement(keyEvent);

                    return true;
                }
                else if (keyEvent.Key == Keys.Right)
                {
                    if (_IsSelectionValid() && _Elements[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = NextValue();
                    else
                        keyEvent.Handled = _NextElement(keyEvent);

                    return true;
                }
                else if (keyEvent.Key == Keys.Up || keyEvent.Key == Keys.Down)
                {
                    keyEvent.Handled = _NextElement(keyEvent);

                    return true;
                }
            }
            else
            {
                if (keyEvent.Key == Keys.Tab)
                {
                    if (keyEvent.Mod == EModifier.Shift)
                        PrevElement();
                    else
                        NextElement();

                    return true;
                }

                if (keyEvent.Key == Keys.Left)
                {
                    PrevValue();

                    return true;
                }
                else if (keyEvent.Key == Keys.Right)
                {
                    NextValue();

                    return true;
                }
            }

            return false;
        }

        public virtual bool HandleMouse(SMouseEvent mouseEvent)
        {
            ProcessMouseMove(mouseEvent.X, mouseEvent.Y);

            if (mouseEvent.LB)
                ProcessMouseClick(mouseEvent.X, mouseEvent.Y);

            _PrevMouse.X = mouseEvent.X;
            _PrevMouse.Y = mouseEvent.Y;

            return true;
        }

        public virtual bool HandleInputThemeEditor(SKeyEvent keyEvent)
        {
            int dx = 0;
            int dy = 0;
            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Up:
                        dy = -1;
                        break;
                    case Keys.Down:
                        dy = 1;
                        break;
                    case Keys.Right:
                        dx = 1;
                        break;
                    case Keys.Left:
                        dx = -1;
                        break;
                    case Keys.Tab:
                        _NextElement(keyEvent);
                        return true;
                    default:
                        return false;
                }
            }
            else
                return false;

            if ((keyEvent.Mod & EModifier.Ctrl) != EModifier.Ctrl)
            {
                dx *= 5;
                dy *= 5;
            }

            if ((keyEvent.Mod & EModifier.Alt) == EModifier.Alt)
                _MoveElement(dx, dy);
            else if ((keyEvent.Mod & EModifier.Shift) == EModifier.Shift)
                _ResizeElement(dx, dy);
            else
            {
                if (_IsSelectionValid())
                    _GetElement(_Selection).Highlighted = false;
                if (_NextElement(keyEvent))
                    _GetElement(_Selection).Highlighted = true;
            }

            return true;
        }

        public virtual bool HandleMouseThemeEditor(SMouseEvent mouseEvent)
        {
            if (_IsSelectionValid())
                _GetElement(_Selection).Highlighted = false;

            Point mouse = new Point(mouseEvent.X, mouseEvent.Y);
            if ((mouseEvent.Mod & EModifier.Ctrl) != EModifier.Ctrl)
            {
                // Clip to raster
                mouse.X = (int)(Math.Round((double)mouse.X / 5) * 5);
                mouse.Y = (int)(Math.Round((double)mouse.Y / 5) * 5);
            }

            int mouseDX = mouse.X - _PrevMouse.X;
            int mouseDY = mouse.Y - _PrevMouse.Y;

            _PrevMouse.X = mouseEvent.X;
            _PrevMouse.Y = mouseEvent.Y;

            if (mouseEvent.LBH)
            {
                if (mouseEvent.Mod == EModifier.Shift)
                    _ResizeElement(mouseDX, mouseDX);
                else
                    _MoveElement(mouseDX, mouseDY);
            }
            else
                ProcessMouseMove(mouseEvent.X, mouseEvent.Y);

            return true;
        }
        #endregion MenuHandler

        private bool _IsSelectionValid()
        {
            return _Selection >= 0 && _Selection < _Elements.Count;
        }

        protected IMenuElement _GetElement(CInteraction element)
        {
            switch (element.Type)
            {
                case EType.Background:
                    return _Backgrounds[element.Num];
                case EType.Button:
                    return _Buttons[element.Num];
                case EType.SelectSlide:
                    return _SelectSlides[element.Num];
                case EType.Text:
                    return _Texts[element.Num];
                case EType.Static:
                    return _Statics[element.Num];
                case EType.SongMenu:
                    return _SongMenus[element.Num];
                case EType.Lyric:
                    return _Lyrics[element.Num];
                case EType.SingNote:
                    return _SingNotes[element.Num];
                case EType.NameSelection:
                    return _NameSelections[element.Num];
                case EType.Equalizer:
                    return _Equalizers[element.Num];
                case EType.Playlist:
                    return _Playlists[element.Num];
                case EType.ParticleEffect:
                    return _ParticleEffects[element.Num];
            }
            throw new ArgumentException("Invalid element type: " + element.Type);
        }

        private IMenuElement _GetElement(int element)
        {
            return (element >= 0 && element < _Elements.Count) ? _GetElement(_Elements[element]) : null;
        }

        #region Drawing
        public virtual void Draw()
        {
            if (!_Active)
                return;
            if (_Elements.Count <= 0)
                return;

            var items = new List<SZSort>();

            for (int i = 0; i < _Elements.Count; i++)
            {
                if (_IsVisible(i))
                {
                    var zs = new SZSort {ID = i, Z = _GetZValue(i)};
                    items.Add(zs);
                }
            }

            if (items.Count <= 0)
                return;


            items.Sort((s1, s2) => s2.Z.CompareTo(s1.Z));

            for (int i = 0; i < items.Count; i++)
            {
                IMenuElement el = _GetElement(items[i].ID);
                if (el.Highlighted)
                    CBase.Drawing.DrawRect(_HighlightColor, el.MaxRect);
                el.Draw();
            }
        }
        #endregion Drawing

        #region Element-Adding
        protected void _AddBackground(CBackground bg, String key = null)
        {
            _AddElement(_Backgrounds.Add(bg, key), EType.Background);
        }

        protected void _AddButton(CButton button, String key = null)
        {
            _AddElement(_Buttons.Add(button, key), EType.Button);
        }

        protected void _AddSelectSlide(CSelectSlide slide, String key = null)
        {
            _AddElement(_SelectSlides.Add(slide, key), EType.SelectSlide);
        }

        protected void _AddStatic(CStatic stat, String key = null)
        {
            _AddElement(_Statics.Add(stat, key), EType.Static);
        }

        protected void _AddText(CText text, String key = null)
        {
            _AddElement(_Texts.Add(text, key), EType.Text);
        }

        protected void _AddSongMenu(ISongMenu songmenu, String key = null)
        {
            _AddElement(_SongMenus.Add(songmenu, key), EType.SongMenu);
        }

        protected void _AddLyric(CLyric lyric, String key = null)
        {
            _AddElement(_Lyrics.Add(lyric, key), EType.Lyric);
        }

        protected void _AddSingNote(CSingNotes sn, String key = null)
        {
            _AddElement(_SingNotes.Add(sn, key), EType.SingNote);
        }

        protected void _AddNameSelection(CNameSelection ns, String key = null)
        {
            _AddElement(_NameSelections.Add(ns, key), EType.NameSelection);
        }

        protected void _AddEqualizer(CEqualizer eq, String key = null)
        {
            _AddElement(_Equalizers.Add(eq, key), EType.Equalizer);
        }

        protected void _AddPlaylist(CPlaylist pls, String key = null)
        {
            _AddElement(_Playlists.Add(pls, key), EType.Playlist);
        }

        protected void _AddParticleEffect(CParticleEffect pe, String key = null)
        {
            _AddElement(_ParticleEffects.Add(pe, key), EType.ParticleEffect);
        }
        #endregion Element-Adding

        #region InteractionHandling
        protected void _SelectElement(IMenuElement element)
        {
            if (!element.Visible)
                return;
            for (int i = 0; i < _Elements.Count; i++)
            {
                if (_GetElement(i) == element)
                {
                    _SetSelected(i);
                    return;
                }
            }
        }

        public void ProcessMouseClick(int x, int y)
        {
            if (!_IsSelectionValid())
                return;

            if (_Elements[_Selection].Type == EType.SelectSlide)
            {
                if (_SelectSlides[_Elements[_Selection].Num].Visible)
                    _SelectSlides[_Elements[_Selection].Num].ProcessMouseLBClick(x, y);
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            _SelectByMouse(x, y);

            if (!_IsSelectionValid())
                return;

            if (_Elements[_Selection].Type == EType.SelectSlide)
            {
                if (_SelectSlides[_Elements[_Selection].Num].Visible)
                    _SelectSlides[_Elements[_Selection].Num].ProcessMouseMove(x, y);
            }
        }

        private void _SelectByMouse(int x, int y)
        {
            float z = CBase.Settings.GetZFar();
            int element = -1;
            for (int i = 0; i < _Elements.Count; i++)
            {
                if (!_IsSelectable(i))
                    continue;
                if (!_IsMouseOverElement(x, y, _Elements[i]))
                    continue;
                if (_GetZValue(i) > z)
                    continue;
                z = _GetZValue(i);
                element = i;
            }
            _SetSelected(element);
        }

        protected bool _IsMouseOverCurSelection(SMouseEvent mouseEvent)
        {
            if (!_IsSelectionValid())
                return false;

            return _IsMouseOverElement(mouseEvent.X, mouseEvent.Y, _Elements[_Selection]);
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

        public void NextElement()
        {
            int element = _Selection;
            for (int i = 0; i < _Elements.Count; i++)
            {
                element++;
                if (element >= _Elements.Count)
                    element = 0;
                if (_IsSelectable(element))
                    break;
            }
            _SetSelected(element);
        }

        public void PrevElement()
        {
            Debug.Assert(_Selection == 0 || _Selection < _Elements.Count);
            int element = _Selection;
            for (int i = 0; i < _Elements.Count; i++)
            {
                element--;
                if (element < 0)
                    element = _Elements.Count - 1;
                if (_IsSelectable(element))
                    break;
            }
            _SetSelected(element);
        }

        /// <summary>
        ///     Selects the next best element in a menu.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool _NextElement(SKeyEvent key)
        {
            int element;
            switch (key.Key)
            {
                case Keys.Up:
                    element = _GetNextElement(EDirection.Up);
                    break;
                case Keys.Right:
                    element = _GetNextElement(EDirection.Right);
                    break;
                case Keys.Down:
                    element = _GetNextElement(EDirection.Down);
                    break;
                case Keys.Left:
                    element = _GetNextElement(EDirection.Left);
                    break;
                case Keys.Tab:
                    if (key.Mod == EModifier.Shift)
                        PrevElement();
                    else
                        NextElement();
                    return true;
                default:
                    return false;
            }
            if (element < 0)
                return false;

            // select the new element
            _SetSelected(element);
            return true;
        }

        private int _GetNextElement(EDirection direction)
        {
            float distance = float.MaxValue;
            int min = -1;
            SRectF currentRect = _IsSelectionValid() ? _GetRect(_Selection) : new SRectF(_PrevMouse.X, _PrevMouse.Y, 1, 1, 1);

            for (int i = 0; i < _Elements.Count; i++)
            {
                if (i != _Selection && _IsSelectable(i))
                {
                    float dist = _GetDistanceDirect(direction, currentRect, _GetRect(i));
                    if (dist >= 0f && dist < distance)
                    {
                        distance = dist;
                        min = i;
                    }
                }
            }
            if (min >= 0)
                return min;

            for (int i = 0; i < _Elements.Count; i++)
            {
                if (i != _Selection && _IsSelectable(i))
                {
                    float dist = _GetDistance180(direction, currentRect, _GetRect(i));
                    if (dist >= 0f && dist < distance)
                    {
                        distance = dist;
                        min = i;
                    }
                }
            }
            if (min >= 0)
                return min;

            switch (direction)
            {
                case EDirection.Up:
                    currentRect = new SRectF(currentRect.X, CBase.Settings.GetRenderH(), 1, 1, currentRect.Z);
                    break;
                case EDirection.Down:
                    currentRect = new SRectF(currentRect.X, 0, 1, 1, currentRect.Z);
                    break;
                case EDirection.Left:
                    currentRect = new SRectF(CBase.Settings.GetRenderW(), currentRect.Y, 1, 1, currentRect.Z);
                    break;
                case EDirection.Right:
                    currentRect = new SRectF(0, currentRect.Y, 1, 1, currentRect.Z);
                    break;
            }

            for (int i = 0; i < _Elements.Count; i++)
            {
                if (i != _Selection && _IsSelectable(i))
                {
                    float dist = _GetDistance180(direction, currentRect, _GetRect(i));
                    if (dist >= 0f && dist < distance)
                    {
                        distance = dist;
                        min = i;
                    }
                }
            }
            return min;
        }

        private static float _GetDistanceDirect(EDirection direction, SRectF current, SRectF other)
        {
            switch (direction)
            {
                case EDirection.Up:
                case EDirection.Down:
                    if (!other.X.IsInRange(current.X, current.Right) && !other.Right.IsInRange(current.X, current.Right) && !current.X.IsInRange(other.X, other.Right))
                        return float.MaxValue;
                    break;

                case EDirection.Left:
                case EDirection.Right:
                    if (!other.Y.IsInRange(current.Y, current.Bottom) && !other.Bottom.IsInRange(current.Y, current.Bottom) && !current.Y.IsInRange(other.Y, other.Bottom))
                        return float.MaxValue;
                    break;
            }
            return _GetDistance180(direction, current, other);
        }

        private static float _GetDistance180(EDirection direction, SRectF current, SRectF other)
        {
            var source = new PointF(current.X + current.W / 2f, current.Y + current.H / 2f);
            var dest = new PointF(other.X + other.W / 2f, other.Y + other.H / 2f);

            var vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            var distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            switch (direction)
            {
                case EDirection.Up:
                    if (vector.Y < 0f)
                        return distance;
                    break;

                case EDirection.Down:
                    if (vector.Y > 0f)
                        return distance;
                    break;

                case EDirection.Left:
                    if (vector.X < 0f)
                        return distance;
                    break;

                case EDirection.Right:
                    if (vector.X > 0f)
                        return distance;
                    break;
            }
            return float.MaxValue;
        }

        /// <summary>
        ///     Selects the next value in a menu element.
        /// </summary>
        /// <returns>True if the next value is selected. False if either there is no next value or the element does not provide such a method.</returns>
        public bool NextValue()
        {
            if (_IsSelectionValid() && _Elements[_Selection].Type == EType.SelectSlide)
                return _SelectSlides[_Elements[_Selection].Num].SelectNextValue();

            return false;
        }

        /// <summary>
        ///     Selects the previous value in a menu element.
        /// </summary>
        /// <returns>
        ///     True if the previous value is selected. False if either there is no previous value or the element does not provide such a method.
        /// </returns>
        public bool PrevValue()
        {
            if (_IsSelectionValid() && _Elements[_Selection].Type == EType.SelectSlide)
                return _SelectSlides[_Elements[_Selection].Num].SelectPrevValue();

            return false;
        }

        private void _AddElement(int num, EType type)
        {
            _Elements.Add(new CInteraction(num, type));
            if (_IsSelectable(_Selection))
                _SetSelected(_Selection);
            else
                NextElement();
        }
        #endregion InteractionHandling

        #region Element-property getters
        private bool _IsVisible(int element)
        {
            IMenuElement el = _GetElement(element);
            return el != null && el.Visible;
        }

        private bool _IsSelectable(int element)
        {
            IMenuElement el = _GetElement(element);
            return el != null && (el.Selectable || CBase.Settings.GetProgramState() == EProgramState.EditTheme);
        }

        private SRectF _GetRect(CInteraction element)
        {
            return _GetElement(element).Rect;
        }

        private SRectF _GetRect(int element)
        {
            return _GetRect(_Elements[element]);
        }

        private float _GetZValue(int element)
        {
            return _GetElement(_Elements[element]).Rect.Z;
        }
        #endregion

        #region Theme Handling
        private void _MoveElement(int stepX, int stepY)
        {
            IMenuElement el = _GetElement(_Selection);
            if (el != null)
                el.MoveElement(stepX, stepY);
        }

        private void _ResizeElement(int stepW, int stepH)
        {
            IMenuElement el = _GetElement(_Selection);
            if (el != null)
                el.ResizeElement(stepW, stepH);
        }
        #endregion Theme Handling
    }
}