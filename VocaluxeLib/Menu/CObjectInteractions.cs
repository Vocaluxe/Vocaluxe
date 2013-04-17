using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VocaluxeLib.Menu
{
    class CObjectInteractions
    {
        private List<CInteraction> _Interactions;
        private int _Selection;

        private List<CStatic> _Statics;
        private List<CText> _Texts;
        private List<CButton> _Buttons;
        private List<CSelectSlide> _SelectSlides;

        private int _PrevMouseX;
        private int _PrevMouseY;

        protected int _MouseDX;
        protected int _MouseDY;

        public bool Active;

        protected SRectF _ScreenArea;

        public SRectF ScreenArea
        {
            get { return _ScreenArea; }
        }

        public CObjectInteractions()
        {
            _Init();
        }

        protected void _Init()
        {
            _Interactions = new List<CInteraction>();
            _Selection = 0;

            _Statics = new List<CStatic>();
            _Texts = new List<CText>();
            _Buttons = new List<CButton>();
            _SelectSlides = new List<CSelectSlide>();

            _PrevMouseX = 0;
            _PrevMouseY = 0;

            _MouseDX = 0;
            _MouseDY = 0;

            Active = false;
            _ScreenArea = new SRectF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), 0f);
        }

        public void Clear()
        {
            _Interactions.Clear();
            _Statics.Clear();
            _Texts.Clear();
            _Buttons.Clear();
            _SelectSlides.Clear();

            _Selection = 0;

            _PrevMouseX = 0;
            _PrevMouseY = 0;

            _MouseDX = 0;
            _MouseDY = 0;

            Active = false;
        }

        #region GetLists
        public List<CButton> GetButtons()
        {
            return _Buttons;
        }

        public List<CSelectSlide> GetSelectSlides()
        {
            return _SelectSlides;
        }
        #endregion GetLists

        #region ElementHandler

        #region Get Arrays
        public CButton[] Buttons
        {
            get { return _Buttons.ToArray(); }
        }

        public CSelectSlide[] SelectSlides
        {
            get { return _SelectSlides.ToArray(); }
        }
        #endregion Get Arrays

        #endregion ElementHandler

        #region MenuHandler
        public bool HandleInput(SKeyEvent keyEvent)
        {
            if (!CBase.Settings.IsTabNavigation())
            {
                if (keyEvent.Key == Keys.Left)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = PrevElement();
                    else
                        keyEvent.Handled = _NextInteraction(keyEvent);
                }

                if (keyEvent.Key == Keys.Right)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.SelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = NextElement();
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
                    PrevElement();

                if (keyEvent.Key == Keys.Right)
                    NextElement();
            }

            return true;
        }

        public bool HandleMouse(SMouseEvent mouseEvent)
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

        public bool HandleInputThemeEditor(SKeyEvent keyEvent)
        {
            _UnsetHighlighted(_Selection);
            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.S:
                        CBase.Graphics.SaveTheme();
                        break;

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

        public bool HandleMouseThemeEditor(SMouseEvent mouseEvent)
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
        public void Draw()
        {
            if (_Interactions.Count <= 0)
                return;

            List<SZSort> items = new List<SZSort>();

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_IsVisible(i) && (
                                         _Interactions[i].Type == EType.Button ||
                                         _Interactions[i].Type == EType.SelectSlide ||
                                         _Interactions[i].Type == EType.Static ||
                                         _Interactions[i].Type == EType.NameSelection ||
                                         _Interactions[i].Type == EType.Text ||
                                         _Interactions[i].Type == EType.SongMenu ||
                                         _Interactions[i].Type == EType.Equalizer))
                {
                    SZSort zs = new SZSort();
                    zs.ID = i;
                    zs.Z = _GetZValue(i);
                    items.Add(zs);
                }
            }

            if (items.Count <= 0)
                return;


            items.Sort(delegate(SZSort s1, SZSort s2) { return s2.Z.CompareTo(s1.Z); });

            for (int i = 0; i < items.Count; i++)
                _DrawInteraction(items[i].ID);
        }
        #endregion Drawing

        #region Elements
        public int AddStatic(CStatic stat)
        {
            _Statics.Add(stat);
            _AddInteraction(_Statics.Count - 1, EType.Static);
            return _Statics.Count - 1;
        }

        public int AddText(CText text)
        {
            _Texts.Add(text);
            _AddInteraction(_Texts.Count - 1, EType.Text);
            return _Texts.Count - 1;
        }

        public int AddButton(CButton button)
        {
            _Buttons.Add(button);
            _AddInteraction(_Buttons.Count - 1, EType.Button);
            return _Buttons.Count - 1;
        }

        public int AddSelectSlide(CSelectSlide slide)
        {
            _SelectSlides.Add(slide);
            _AddInteraction(_SelectSlides.Count - 1, EType.SelectSlide);
            return _SelectSlides.Count - 1;
        }
        #endregion Elements

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
        ///     Selects the next element in a menu interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        public bool NextElement()
        {
            if (_Interactions.Count > 0)
                return _NextElement();

            return false;
        }

        /// <summary>
        ///     Selects the previous element in a menu interaction.
        /// </summary>
        /// <returns>True if the previous element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        public bool PrevElement()
        {
            if (_Interactions.Count > 0)
                return _PrevElement();

            return false;
        }

        public bool SetInteractionToButton(CButton button)
        {
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type == EType.Button)
                {
                    if (_Buttons[_Interactions[i].Num].Visible && _Buttons[_Interactions[i].Num] == button)
                    {
                        _UnsetSelected();
                        _UnsetHighlighted(_Selection);
                        _Selection = i;
                        _SetSelected();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool SetInteractionToSelectSlide(CSelectSlide slide)
        {
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type == EType.SelectSlide)
                {
                    if (_SelectSlides[_Interactions[i].Num].Visible && _SelectSlides[_Interactions[i].Num] == slide)
                    {
                        _UnsetSelected();
                        _UnsetHighlighted(_Selection);
                        _Selection = i;
                        _SetSelected();
                        return true;
                    }
                }
            }
            return false;
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
            SelectByMouse(x, y);

            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.SelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseMove(x, y);
            }
        }

        public void SelectByMouse(int x, int y)
        {
            float z = CBase.Settings.GetZFar();
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if ((CBase.Settings.GetGameState() == EGameState.EditTheme) || (!_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i)))
                {
                    if (_IsMouseOver(x, y, _Interactions[i]))
                    {
                        if (_GetZValue(_Interactions[i]) <= z)
                        {
                            z = _GetZValue(_Interactions[i]);
                            _UnsetSelected();
                            _Selection = i;
                            _SetSelected();
                        }
                    }
                }
            }
        }

        public bool IsMouseOver(SMouseEvent mouseEvent)
        {
            return IsMouseOver(mouseEvent.X, mouseEvent.Y);
        }

        public bool IsMouseOver(int x, int y)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            if (_Interactions.Count > 0)
                return _IsMouseOver(x, y, _Interactions[_Selection]);
            else
                return false;
        }

        private bool _IsMouseOver(int x, int y, CInteraction interact)
        {
            switch (interact.Type)
            {
                case EType.Button:
                    if (CHelper.IsInBounds(_Buttons[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.SelectSlide:
                    if (CHelper.IsInBounds(_SelectSlides[interact.Num].Rect, x, y) ||
                        CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowLeft, x, y) ||
                        CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowRight, x, y))
                        return true;
                    break;
            }
            return false;
        }

        private float _GetZValue(CInteraction interact)
        {
            switch (interact.Type)
            {
                case EType.Static:
                    return _Statics[interact.Num].Rect.Z;
                case EType.Text:
                    return _Texts[interact.Num].Z;
                case EType.Button:
                    return _Buttons[interact.Num].Rect.Z;
                case EType.SelectSlide:
                    return _SelectSlides[interact.Num].Rect.Z;
            }
            return CBase.Settings.GetZFar();
        }

        private float _GetZValue(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.Static:
                    return _Statics[_Interactions[interaction].Num].Rect.Z;
                case EType.Text:
                    return _Texts[_Interactions[interaction].Num].Z;
                case EType.Button:
                    return _Buttons[_Interactions[interaction].Num].Rect.Z;
                case EType.SelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Rect.Z;
            }

            return CBase.Settings.GetZFar();
        }

        private void _NextInteraction()
        {
            _UnsetSelected();
            if (CBase.Settings.GetGameState() != EGameState.EditTheme)
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
            if (CBase.Settings.GetGameState() != EGameState.EditTheme)
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
            SKeyEvent[] directions = new SKeyEvent[4];
            float[] distances = new float[4];
            int[] stages = new int[4];
            int[] elements = new int[4];

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
            float distance = float.MaxValue;
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
                    distance = distances[i];
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
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
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
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
        }

        private float _GetDistance180(SKeyEvent key, SRectF actualRect, SRectF targetRect)
        {
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
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
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
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

        private void _ToggleHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.SelectSlide)
                _SelectSlides[_Interactions[_Selection].Num].Highlighted = !_SelectSlides[_Interactions[_Selection].Num].Highlighted;
        }

        private bool _IsHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            if (_Interactions[_Selection].Type == EType.SelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].Highlighted;

            return false;
        }

        private bool _IsVisible(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.Static:
                    return _Statics[_Interactions[interaction].Num].Visible;

                case EType.Text:
                    return _Texts[_Interactions[interaction].Num].Visible;

                case EType.Button:
                    return _Buttons[_Interactions[interaction].Num].Visible;

                case EType.SelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private bool _IsEnabled(int interaction)
        {
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
            }

            return false;
        }

        private SRectF _GetRect(int interaction)
        {
            SRectF result = new SRectF();
            switch (_Interactions[interaction].Type)
            {
                case EType.Button:
                    return _Buttons[_Interactions[interaction].Num].Rect;

                case EType.SelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Rect;
            }

            return result;
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
            bool sel = false;
            switch (_Interactions[interaction].Type)
            {
                case EType.Static:
                    _Statics[_Interactions[interaction].Num].Draw();
                    break;

                case EType.Text:
                    _Texts[_Interactions[interaction].Num].Draw();
                    break;

                case EType.Button:
                    sel = _Buttons[_Interactions[interaction].Num].Selected;
                    if (!Active)
                        _Buttons[_Interactions[interaction].Num].Selected = false;
                    _Buttons[_Interactions[interaction].Num].Draw();
                    _Buttons[_Interactions[interaction].Num].Selected = sel;
                    break;

                case EType.SelectSlide:
                    sel = _SelectSlides[_Interactions[interaction].Num].Selected;
                    if (!Active)
                        _SelectSlides[_Interactions[interaction].Num].Selected = false;
                    _SelectSlides[_Interactions[interaction].Num].Draw();
                    _SelectSlides[_Interactions[interaction].Num].Selected = sel;
                    break;
            }
        }
        #endregion InteractionHandling

        #region Theme Handling
        private void _MoveElement(int stepX, int stepY)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.Button:
                        _Buttons[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.SelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;
                }
            }
        }

        private void _ResizeElement(int stepW, int stepH)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.Button:
                        _Buttons[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.SelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;
                }
            }
        }
        #endregion Theme Handling
    }
}