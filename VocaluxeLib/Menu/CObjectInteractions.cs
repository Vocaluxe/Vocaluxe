using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Menu
{
    class CObjectInteractions
    {
        private List<CInteraction> _Interactions;
        private int _Selection = 0;

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
            Init();
        }

        protected virtual void Init()
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
            get
            {
                return _Buttons.ToArray();
            }
        }

        public CSelectSlide[] SelectSlides
        {
            get
            {
                return _SelectSlides.ToArray();
            }
        }
        #endregion Get Arrays
        #endregion ElementHandler

        #region MenuHandler
        public bool HandleInput(KeyEvent KeyEvent)
        {
            if (!CBase.Settings.IsTabNavigation())
            {
                if (KeyEvent.Key == Keys.Left)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.TSelectSlide && KeyEvent.Mod != EModifier.Shift)
                        KeyEvent.Handled = PrevElement();
                    else
                        KeyEvent.Handled = _NextInteraction(KeyEvent);
                }

                if (KeyEvent.Key == Keys.Right)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.TSelectSlide && KeyEvent.Mod != EModifier.Shift)
                        KeyEvent.Handled = NextElement();
                    else
                        KeyEvent.Handled = _NextInteraction(KeyEvent);
                }

                if (KeyEvent.Key == Keys.Up || KeyEvent.Key == Keys.Down)
                {
                    KeyEvent.Handled = _NextInteraction(KeyEvent);
                }
            }
            else
            {
                if (KeyEvent.Key == Keys.Tab)
                {
                    if (KeyEvent.Mod == EModifier.Shift)
                        PrevInteraction();
                    else
                        NextInteraction();
                }

                if (KeyEvent.Key == Keys.Left)
                    PrevElement();

                if (KeyEvent.Key == Keys.Right)
                    NextElement();
            }

            return true;
        }

        public bool HandleMouse(MouseEvent MouseEvent)
        {
            int selection = _Selection;
            ProcessMouseMove(MouseEvent.X, MouseEvent.Y);
            if (selection != _Selection)
            {
                _UnsetHighlighted(selection);
                _SetHighlighted(_Selection);
            }

            if (MouseEvent.LB)
                ProcessMouseClick(MouseEvent.X, MouseEvent.Y);

            _PrevMouseX = MouseEvent.X;
            _PrevMouseY = MouseEvent.Y;

            return true;
        }

        public bool HandleInputThemeEditor(KeyEvent KeyEvent)
        {
            _UnsetHighlighted(_Selection);
            if (KeyEvent.KeyPressed)
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.S:
                        CBase.Graphics.SaveTheme();
                        break;

                    case Keys.Up:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(0, -1);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(0, 1);

                        break;
                    case Keys.Down:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(0, 1);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(0, -1);

                        break;

                    case Keys.Right:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(1, 0);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(1, 0);

                        if (KeyEvent.Mod == EModifier.None)
                            NextInteraction();
                        break;
                    case Keys.Left:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(-1, 0);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(-1, 0);

                        if (KeyEvent.Mod == EModifier.None)
                            PrevInteraction();
                        break;
                }
            }
            return true;
        }

        public bool HandleMouseThemeEditor(MouseEvent MouseEvent)
        {
            _UnsetHighlighted(_Selection);
            _MouseDX = MouseEvent.X - _PrevMouseX;
            _MouseDY = MouseEvent.Y - _PrevMouseY;

            int stepX = 0;
            int stepY = 0;

            if ((MouseEvent.Mod & EModifier.Ctrl) == EModifier.Ctrl)
            {
                _PrevMouseX = MouseEvent.X;
                _PrevMouseY = MouseEvent.Y;
            }
            else
            {
                while (Math.Abs(MouseEvent.X - _PrevMouseX) >= 5)
                {
                    if (MouseEvent.X - _PrevMouseX >= 5)
                        stepX += 5;

                    if (MouseEvent.X - _PrevMouseX <= -5)
                        stepX -= 5;

                    _PrevMouseX = MouseEvent.X - (_MouseDX - stepX);
                }

                while (Math.Abs(MouseEvent.Y - _PrevMouseY) >= 5)
                {
                    if (MouseEvent.Y - _PrevMouseY >= 5)
                        stepY += 5;

                    if (MouseEvent.Y - _PrevMouseY <= -5)
                        stepY -= 5;

                    _PrevMouseY = MouseEvent.Y - (_MouseDY - stepY);
                }
            }

            if (MouseEvent.LBH)
            {
                //if (IsMouseOver(MouseEvent.X, _PrevMouseY))
                //{
                if (MouseEvent.Mod == EModifier.None)
                    MoveElement(stepX, stepY);

                if (MouseEvent.Mod == EModifier.Ctrl)
                    MoveElement(_MouseDX, _MouseDY);

                if (MouseEvent.Mod == EModifier.Shift)
                    ResizeElement(stepX, stepY);

                if (MouseEvent.Mod == (EModifier.Shift | EModifier.Ctrl))
                    ResizeElement(_MouseDX, _MouseDY);
                //}
            }
            else
                ProcessMouseMove(MouseEvent.X, MouseEvent.Y);

            return true;
        }
        #endregion MenuHandler

        #region Drawing
        public void Draw()
        {
            if (_Interactions.Count <= 0)
                return;

            List<ZSort> items = new List<ZSort>();

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_IsVisible(i) && (
                    _Interactions[i].Type == EType.TButton ||
                    _Interactions[i].Type == EType.TSelectSlide ||
                    _Interactions[i].Type == EType.TStatic ||
                    _Interactions[i].Type == EType.TNameSelection ||
                    _Interactions[i].Type == EType.TText ||
                    _Interactions[i].Type == EType.TSongMenu ||
                    _Interactions[i].Type == EType.TEqualizer))
                {
                    ZSort zs = new ZSort();
                    zs.ID = i;
                    zs.z = _GetZValue(i);
                    items.Add(zs);
                }
            }

            if (items.Count <= 0)
                return;


            items.Sort(delegate(ZSort s1, ZSort s2) { return (s2.z.CompareTo(s1.z)); });

            for (int i = 0; i < items.Count; i++)
            {
                _DrawInteraction(items[i].ID);
            }

        }
        #endregion Drawing

        #region Elements
        public int AddStatic(CStatic stat)
        {
            _Statics.Add(stat);
            _AddInteraction(_Statics.Count - 1, EType.TStatic);
            return _Statics.Count - 1;
        }

        public int AddText(CText text)
        {
            _Texts.Add(text);
            _AddInteraction(_Texts.Count - 1, EType.TText);
            return _Texts.Count - 1;
        }

        public int AddButton(CButton button)
        {
            _Buttons.Add(button);
            _AddInteraction(_Buttons.Count - 1, EType.TButton);
            return _Buttons.Count - 1;
        }

        public int AddSelectSlide(CSelectSlide slide)
        {
            _SelectSlides.Add(slide);
            _AddInteraction(_SelectSlides.Count - 1, EType.TSelectSlide);
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
        /// Selects the next element in a menu interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        public bool NextElement()
        {
            if (_Interactions.Count > 0)
                return _NextElement();

            return false;
        }

        /// <summary>
        /// Selects the previous element in a menu interaction.
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
                if (_Interactions[i].Type == EType.TButton)
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
                if (_Interactions[i].Type == EType.TSelectSlide)
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

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                {
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseLBClick(x, y);
                }
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            SelectByMouse(x, y);

            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                {
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseMove(x, y);
                }
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

        public bool IsMouseOver(MouseEvent MouseEvent)
        {
            return IsMouseOver(MouseEvent.X, MouseEvent.Y);
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
                case EType.TButton:
                    if (CHelper.IsInBounds(_Buttons[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TSelectSlide:
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
                case EType.TStatic:
                    return _Statics[interact.Num].Rect.Z;
                case EType.TText:
                    return _Texts[interact.Num].Z;
                case EType.TButton:
                    return _Buttons[interact.Num].Rect.Z;
                case EType.TSelectSlide:
                    return _SelectSlides[interact.Num].Rect.Z;
            }
            return CBase.Settings.GetZFar();
        }

        private float _GetZValue(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.TStatic:
                    return _Statics[_Interactions[interaction].Num].Rect.Z;
                case EType.TText:
                    return _Texts[_Interactions[interaction].Num].Z;
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Rect.Z;
                case EType.TSelectSlide:
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
        /// Selects the next best interaction in a menu.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private bool _NextInteraction(KeyEvent Key)
        {
            KeyEvent[] Directions = new KeyEvent[4];
            float[] Distances = new float[4];
            int[] stages = new int[4];
            int[] elements = new int[4];

            for (int i = 0; i < 4; i++)
            {
                Directions[i] = new KeyEvent();
            }

            Directions[0].Key = Keys.Up;
            Directions[1].Key = Keys.Right;
            Directions[2].Key = Keys.Down;
            Directions[3].Key = Keys.Left;

            for (int i = 0; i < 4; i++)
            {
                elements[i] = _GetNextElement(Directions[i], out Distances[i], out stages[i]);
            }

            int element = _Selection;
            int stage = int.MaxValue;
            float distance = float.MaxValue;
            int direction = -1;

            int mute = -1;
            switch (Key.Key)
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
                if (i != mute && elements[i] != _Selection && (stages[i] <= stage && Directions[i].Key == Key.Key))
                {
                    stage = stages[i];
                    element = elements[i];
                    distance = Distances[i];
                    direction = i;
                }
            }

            if (direction != -1)
            {
                // select the new element
                if (Directions[direction].Key == Key.Key)
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

        private int _GetNextElement(KeyEvent Key, out float Distance, out int Stage)
        {
            Distance = float.MaxValue;
            int min = _Selection;
            SRectF actualRect = _GetRect(_Selection);
            Stage = int.MaxValue;

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                {
                    SRectF targetRect = _GetRect(i);
                    float dist = _GetDistanceDirect(Key, actualRect, targetRect);
                    if (dist >= 0f && dist < Distance)
                    {
                        Distance = dist;
                        min = i;
                        Stage = 10;
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
                        float dist = _GetDistance180(Key, actualRect, targetRect);
                        if (dist >= 0f && dist < Distance)
                        {
                            Distance = dist;
                            min = i;
                            Stage = 20;
                        }
                    }
                }
            }

            if (min == _Selection)
            {
                switch (Key.Key)
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
                    default:
                        break;
                }

                for (int i = 0; i < _Interactions.Count; i++)
                {
                    if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                    {
                        SRectF targetRect = _GetRect(i);
                        float dist = _GetDistance180(Key, actualRect, targetRect);
                        if (dist >= 0f && dist < Distance)
                        {
                            Distance = dist;
                            min = i;
                            Stage = 30;
                        }
                    }
                }
            }
            return min;
        }

        private float _GetDistanceDirect(KeyEvent Key, SRectF actualRect, SRectF targetRect)
        {
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (Key.Key)
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

                default:
                    break;
            }
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
        }

        private float _GetDistance180(KeyEvent Key, SRectF actualRect, SRectF targetRect)
        {
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (Key.Key)
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

                default:
                    break;
            }
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
        }

        /// <summary>
        /// Selects the next element in a menu interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        private bool _NextElement()
        {
            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].NextValue();

            return false;
        }

        /// <summary>
        /// Selects the previous element in a menu interaction.
        /// </summary>
        /// <returns>
        /// True if the previous element is selected. False if either there is no previous element or the interaction does not provide such a method.
        /// </returns>
        private bool _PrevElement()
        {
            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].PrevValue();

            return false;
        }

        private void _SetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.TButton:
                    _Buttons[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _UnsetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.TButton:
                    _Buttons[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TSelectSlide:
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
                case EType.TButton:
                    //_Buttons[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = true;
                    break;
                case EType.TStatic:
                    //_Statics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TText:
                    //_Texts[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TSongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TLyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TNameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TPlaylist:
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
                case EType.TButton:
                    //_Buttons[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = false;
                    break;
                case EType.TStatic:
                    //_Statics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TText:
                    //_Texts[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TSongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TLyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TNameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TPlaylist:
                    //_Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _ToggleHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
            {
                _SelectSlides[_Interactions[_Selection].Num].Highlighted = !_SelectSlides[_Interactions[_Selection].Num].Highlighted;
            }
        }

        private bool _IsHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].Highlighted;

            return false;
        }

        private bool _IsVisible(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.TStatic:
                    return _Statics[_Interactions[interaction].Num].Visible;

                case EType.TText:
                    return _Texts[_Interactions[interaction].Num].Visible;

                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Visible;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private bool _IsEnabled(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Enabled;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;

                case EType.TStatic:
                    return false; //_Statics[_Interactions[interaction].Num].Visible;

                case EType.TText:
                    return false; //_Texts[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private SRectF _GetRect(int interaction)
        {
            SRectF Result = new SRectF();
            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Rect;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Rect;
            }

            return Result;
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
                case EType.TStatic:
                    _Statics[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TText:
                    _Texts[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TButton:
                    sel = _Buttons[_Interactions[interaction].Num].Selected;
                    if (!Active)
                        _Buttons[_Interactions[interaction].Num].Selected = false;
                    _Buttons[_Interactions[interaction].Num].Draw();
                    _Buttons[_Interactions[interaction].Num].Selected = sel;
                    break;

                case EType.TSelectSlide:
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
        private void MoveElement(int stepX, int stepY)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.TButton:
                        _Buttons[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TSelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;
                }
            }
        }

        private void ResizeElement(int stepW, int stepH)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.TButton:
                        _Buttons[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TSelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;
                }
            }
        }
        #endregion Theme Handling
    }
}
