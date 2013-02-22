using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;

namespace Vocaluxe.Menu
{
    struct SThemeStatic
    {
        public string Name;
        public string TextureName;
        public string ColorName;
    }

    public class CStatic : IMenuElement
    {
        private int _PartyModeID;

        private SThemeStatic _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        private STexture _Texture;
        public STexture Texture
        {
            get
            {
                if (_Texture.index != -1)
                    return _Texture;
                else
                    return CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            }

            set { _Texture = value; }
        }

        public SColorF Color;
        public SRectF Rect;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool Selected;
        public bool Visible;

        public float Alpha;

        public EAspect Aspect = EAspect.Stretch;

        public CStatic(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeStatic();
            _ThemeLoaded = false;

            _Texture = new STexture(-1);
            Color = new SColorF();
            Rect = new SRectF();
            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            Selected = false;
            Alpha = 1f;
            Visible = true;
        }

        public CStatic(CStatic s)
        {
            _PartyModeID = s._PartyModeID;
            _Theme = new SThemeStatic();
            _ThemeLoaded = false;

            _Texture = s.Texture;
            Color = new SColorF(s.Color);
            Rect = new SRectF(s.Rect);
            Reflection = s.Reflection;
            ReflectionSpace = s.ReflectionHeight;
            ReflectionHeight = s.ReflectionSpace;

            Selected = s.Selected;
            Alpha = s.Alpha;
            Visible = s.Visible;
        }

        public CStatic(int PartyModeID, STexture texture, SColorF color, SRectF rect)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeStatic();
            _ThemeLoaded = false;

            _Texture = texture;
            Color = color;
            Rect = rect;
            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            Selected = false;
            Alpha = 1f;
            Visible = true;
        }

        public CStatic(int PartyModeID, string TextureSkinName, SColorF color, SRectF rect)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeStatic();
            _Theme.TextureName = TextureSkinName;
            _ThemeLoaded = false;

            _Texture = new STexture(-1);
            Color = color;
            Rect = rect;
            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            Selected = false;
            Alpha = 1f;
            Visible = true;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", ref _Theme.TextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.ItemExists(item + "/Reflection"))
            {
                Reflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);
            }
            else
                Reflection = false;

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<Skin>: Texture name");
                writer.WriteElementString("Skin", _Theme.TextureName);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Static position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<Color>: Static color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<Reflection> If exists:");
                writer.WriteComment("   <Space>: Reflection Space");
                writer.WriteComment("   <Height>: Reflection Height");
                if (Reflection)
                {
                    writer.WriteStartElement("Reflection");
                    writer.WriteElementString("Space", ReflectionSpace.ToString("#0"));
                    writer.WriteElementString("Height", ReflectionHeight.ToString("#0"));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                return true;
            }
            return false;
        }

        public void Draw()
        {
            Draw(1f, Rect.Z, Aspect, false);
        }

        public void Draw(EAspect aspect)
        {
            Draw(1f, Rect.Z, aspect, false);
        }

        public void Draw(float scale, EAspect aspect)
        {
            Draw(scale, Rect.Z, aspect, false);
        }

        public void ForceDraw()
        {
            Draw(1f, Rect.Z, Aspect, true);
        }

        public void Draw(float scale, float z, EAspect aspect, bool ForceDraw)
        {
            STexture texture;
            if (_Texture.index != -1)
                texture = _Texture;
            else
                texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);

            SRectF bounds = new SRectF(
                Rect.X - Rect.W * (scale - 1f),
                Rect.Y - Rect.H * (scale - 1f),
                Rect.W + 2 * Rect.W * (scale - 1f),
                Rect.H + 2 * Rect.H * (scale - 1f),
                z);

            SRectF rect = bounds;

            if (aspect != EAspect.Stretch)
            {
                RectangleF bounds2 = new RectangleF(bounds.X, bounds.Y, bounds.W, bounds.H);
                RectangleF rect2 = new RectangleF(0f, 0f, texture.width, texture.height);
                CHelper.SetRect(bounds2, ref rect2, texture.width / texture.height, aspect);

                rect.X = rect2.X;
                rect.Y = rect2.Y;
                rect.W = rect2.Width;
                rect.H = rect2.Height;
            }
            
            SColorF color = new SColorF(Color.R, Color.G, Color.B, Color.A * Alpha);
            if (Visible || ForceDraw || (CBase.Settings.GetGameState() == EGameState.EditTheme))
            {
                CBase.Drawing.DrawTexture(texture, rect, color, bounds);
                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, rect, color, bounds, ReflectionSpace, ReflectionHeight);
                }
            }

            if (Selected && (CBase.Settings.GetGameState() == EGameState.EditTheme))
            {
                CBase.Drawing.DrawColor(new SColorF(1f, 1f, 1f, 0.5f), rect);
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;
        }
        #endregion ThemeEdit
    }
}
