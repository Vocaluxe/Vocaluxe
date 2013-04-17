using System;
using System.Xml;

namespace VocaluxeLib.Menu
{
    struct SThemeButton
    {
        public string Name;

        public string ColorName;
        public string SelColorName;

        public string TextureName;
        public string SelTextureName;
    }

    public class CButton : IMenuElement
    {
        private SThemeButton _Theme;
        private bool _ThemeLoaded;
        private readonly int _PartyModeID;

        public STexture Texture;
        public STexture SelTexture;
        public SRectF Rect;
        public SColorF Color;
        public SColorF SelColor;

        public bool IsSelText;
        public CText Text;
        public CText SelText;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool SelReflection;
        public float SelReflectionSpace;
        public float SelReflectionHeight;

        public bool Pressed;

        public bool EditMode
        {
            get { return Text.EditMode; }
            set
            {
                Text.EditMode = value;
                SelText.EditMode = value;
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

        public CButton(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeButton();
            Rect = new SRectF();
            Color = new SColorF();
            SelColor = new SColorF();
            Texture = new STexture(-1);
            SelTexture = new STexture(-1);

            IsSelText = false;
            Text = new CText(_PartyModeID);
            SelText = new CText(_PartyModeID);
            Selected = false;
            Visible = true;
            EditMode = false;
            Enabled = true;

            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;

            SelReflection = false;
            SelReflectionSpace = 0f;
            SelReflectionHeight = 0f;
        }

        public CButton(CButton button)
        {
            _PartyModeID = button._PartyModeID;
            _Theme = new SThemeButton();
            _Theme.ColorName = button._Theme.ColorName;
            _Theme.SelColorName = button._Theme.SelColorName;
            _Theme.TextureName = button._Theme.TextureName;
            _Theme.SelTextureName = button._Theme.SelTextureName;

            Rect = new SRectF(button.Rect);
            Color = new SColorF(button.Color);
            SelColor = new SColorF(button.Color);
            Texture = button.Texture;
            SelTexture = button.SelTexture;

            IsSelText = false;
            Text = new CText(button.Text);
            SelText = new CText(button.SelText);
            Selected = false;
            Visible = true;
            EditMode = false;
            _ThemeLoaded = false;
            Enabled = button.Enabled;

            Reflection = button.Reflection;
            ReflectionHeight = button.ReflectionHeight;
            ReflectionSpace = button.ReflectionSpace;

            SelReflection = button.SelReflection;
            SelReflectionHeight = button.SelReflectionHeight;
            SelReflectionSpace = button.SelReflectionSpace;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", ref _Theme.SelTextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", ref _Theme.SelColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColorName, skinIndex, out SelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
            }

            _ThemeLoaded &= Text.LoadTheme(item, "Text", xmlReader, skinIndex, true);
            Text.Z = Rect.Z;
            if (xmlReader.ItemExists(item + "/SText"))
            {
                IsSelText = true;
                _ThemeLoaded &= SelText.LoadTheme(item, "SText", xmlReader, skinIndex, true);
                SelText.Z = Rect.Z;
            }


            //Reflections
            if (xmlReader.ItemExists(item + "/Reflection"))
            {
                Reflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);
            }
            else
                Reflection = false;

            if (xmlReader.ItemExists(item + "/SReflection"))
            {
                SelReflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Space", ref SelReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Height", ref SelReflectionHeight);
            }
            else
                SelReflection = false;

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
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
                writer.WriteElementString("SkinSelected", _Theme.SelTextureName);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Button position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<Color>: Button color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (_Theme.ColorName.Length > 0)
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Selected button color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SelColorName.Length > 0)
                    writer.WriteElementString("SColor", _Theme.SelColorName);
                else
                {
                    writer.WriteElementString("SR", SelColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SelColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SelColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SelColor.A.ToString("#0.00"));
                }

                Text.SaveTheme(writer);
                if (IsSelText)
                    SelText.SaveTheme(writer);

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
                if (SelReflection)
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

        public void Draw(bool forceDraw)
        {
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme && !forceDraw)
                return;

            STexture texture = new STexture(-1);

            if (!Selected && !Pressed || !Enabled)
            {
                if (Texture.Index != -1)
                    texture = Texture;
                else
                    texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, Color);

                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, Color, Rect, ReflectionSpace, ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if (!IsSelText)
            {
                if (Texture.Index != -1)
                    texture = Texture;
                else
                    texture = CBase.Theme.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SelColor);

                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SelColor, Rect, ReflectionSpace, ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if (IsSelText)
            {
                if (SelTexture.Index != -1)
                    texture = SelTexture;
                else
                    texture = CBase.Theme.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SelColor);

                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SelColor, Rect, ReflectionSpace, ReflectionHeight);
                    SelText.DrawRelative(Rect.X, Rect.Y, ReflectionSpace, ReflectionHeight, Rect.H);
                }
                else
                    SelText.DrawRelative(Rect.X, Rect.Y);
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

            if (_Theme.ColorName.Length > 0)
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SelColorName.Length > 0)
                SelColor = CBase.Theme.GetColor(_Theme.SelColorName, _PartyModeID);
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