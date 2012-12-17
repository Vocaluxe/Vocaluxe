using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Vocaluxe.Menu
{
    struct SThemeButton
    {
        public string Name;

        public string ColorName;
        public string SColorName;

        public string TextureName;
        public string STextureName;
    }

    public class CButton : IMenuElement
    {
        private Basic _Base;
        private SThemeButton _Theme;
        private bool _ThemeLoaded;
        private int _PartyModeID;

        public STexture Texture;
        public STexture STexture;
        public SRectF Rect;
        public SColorF Color;
        public SColorF SColor;

        public bool SelText;
        public CText Text;
        public CText SText;
        
        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool SReflection;
        public float SReflectionSpace;
        public float SReflectionHeight;

        public bool Pressed;

        public bool EditMode
        {
            get { return Text.EditMode; }
            set
            {
                Text.EditMode = value;
                SText.EditMode = value;
            }
        }

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                Text.Selected = value;
            }
        }
        public bool Visible;
        public bool Enabled;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public CButton(Basic Base, int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Base = Base;
            _Theme = new SThemeButton();
            Rect = new SRectF();
            Color = new SColorF();
            SColor = new SColorF();
            Texture = new STexture(-1);
            STexture = new STexture(-1);

            SelText = false;
            Text = new CText(_Base, _PartyModeID);
            SText = new CText(_Base, _PartyModeID);
            Selected = false;
            Visible = true;
            EditMode = false;
            Enabled = true;

            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            SReflection = false;
            SReflectionSpace = 0f;
            SReflectionHeight = 0f;
        }

        public CButton(CButton button)
        {
            _PartyModeID = button._PartyModeID;
            _Base = button._Base;
            _Theme = new SThemeButton();
            _Theme.ColorName = button._Theme.ColorName;
            _Theme.SColorName = button._Theme.SColorName;
            _Theme.TextureName = button._Theme.TextureName;
            _Theme.STextureName = button._Theme.STextureName;

            Rect = new SRectF(button.Rect);
            Color = new SColorF(button.Color);
            SColor = new SColorF(button.Color);
            Texture = button.Texture;
            STexture = button.STexture;

            SelText = false;
            Text = new CText(button.Text);
            SText = new CText(button.SText);
            Selected = false;
            Visible = true;
            EditMode = false;
            _ThemeLoaded = false;
            Enabled = button.Enabled;

            Reflection = button.Reflection;
            ReflectionHeight = button.ReflectionHeight;
            ReflectionSpace = button.ReflectionSpace;

            SReflection = button.SReflection;
            SReflectionHeight = button.SReflectionHeight;
            SReflectionSpace = button.SReflectionSpace;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Skin", navigator, ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinSelected", navigator, ref _Theme.STextureName, String.Empty);
            
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);

            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= _Base.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref Color.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref Color.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref Color.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref Color.A);
            }

            if (CHelper.GetValueFromXML(item + "/SColor", navigator, ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= _Base.Theme.GetColor(_Theme.SColorName, SkinIndex, ref SColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SR", navigator, ref SColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SG", navigator, ref SColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SB", navigator, ref SColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SA", navigator, ref SColor.A);
            }

            _ThemeLoaded &= Text.LoadTheme(item, "Text", navigator, SkinIndex, true);
            Text.Z = Rect.Z;
            if (CHelper.ItemExistsInXML(item + "/SText", navigator))
            {
                SelText = true;
                _ThemeLoaded &= SText.LoadTheme(item, "SText", navigator, SkinIndex, true);
                SText.Z = Rect.Z;
            }
            
            
            //Reflections
            if (CHelper.ItemExistsInXML(item + "/Reflection", navigator))
            {
                Reflection = true;
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Space", navigator, ref ReflectionSpace);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Height", navigator, ref ReflectionHeight);
            }
            else
                Reflection = false;

            if (CHelper.ItemExistsInXML(item + "/SReflection", navigator))
            {
                SReflection = true;
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SReflection/Space", navigator, ref SReflectionSpace);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SReflection/Height", navigator, ref SReflectionHeight);
            }
            else
                SReflection = false;

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

                writer.WriteComment("<SkinSelected>: Texture name for selected button");
                writer.WriteElementString("SkinSelected", _Theme.STextureName);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Button position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<Color>: Button color from ColorScheme (high priority)");
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

                writer.WriteComment("<SColor>: Selected button color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SColorName != String.Empty)
                {
                    writer.WriteElementString("SColor", _Theme.SColorName);
                }
                else
                {
                    writer.WriteElementString("SR", SColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SColor.A.ToString("#0.00"));
                }

                Text.SaveTheme(writer);
                if (SelText)
                    SText.SaveTheme(writer);

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

                writer.WriteComment("<SReflection> If exists:");
                writer.WriteComment("   <Space>: Reflection Space of selected button");
                writer.WriteComment("   <Height>: Reflection Height of selected button");
                if (SReflection)
                {
                    writer.WriteStartElement("SReflection");
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
            Draw(false);
        }

        public void ForceDraw()
        {
            Draw(true);
        }

        public void Draw(bool ForceDraw)
        {
            if (!Visible && _Base.Settings.GetGameState() != EGameState.EditTheme && !ForceDraw)
                return;

            STexture texture = new STexture(-1);

            if (!Selected && !Pressed || !Enabled)
            {
                if (Texture.index != -1)
                    texture = Texture;
                else
                    texture = _Base.Theme.GetSkinTexture(_Theme.TextureName);

                _Base.Drawing.DrawTexture(texture, Rect, Color);
                
                if (Reflection)
                {
                    _Base.Drawing.DrawTextureReflection(texture, Rect, Color, Rect, ReflectionSpace, ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if(!SelText)
            {
                if (Texture.index != -1)
                    texture = Texture;
                else
                    texture = _Base.Theme.GetSkinTexture(_Theme.STextureName);

                _Base.Drawing.DrawTexture(texture, Rect, SColor);

                if (Reflection)
                {
                    _Base.Drawing.DrawTextureReflection(texture, Rect, SColor, Rect, ReflectionSpace, ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if(SelText)
            {
                if (STexture.index != -1)
                    texture = STexture;
                else
                    texture = _Base.Theme.GetSkinTexture(_Theme.STextureName);

                _Base.Drawing.DrawTexture(texture, Rect, SColor);

                if (Reflection)
                {
                    _Base.Drawing.DrawTextureReflection(texture, Rect, SColor, Rect, ReflectionSpace, ReflectionHeight);
                    SText.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    SText.DrawRelative(Rect.X, Rect.Y);
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            Selected = CHelper.IsInBounds(Rect, x, y);
        }

        public void UnloadTextures()
        {
            Text.UnloadTextures();
        }

        public void LoadTextures()
        {
            Text.LoadTextures();

            if (_Theme.ColorName != String.Empty)
                Color = _Base.Theme.GetColor(_Theme.ColorName);

            if (_Theme.SColorName != String.Empty)
                SColor = _Base.Theme.GetColor(_Theme.SColorName);
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
