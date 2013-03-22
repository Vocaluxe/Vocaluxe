using System;
using System.Xml;

namespace VocaluxeLib.Menu
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

        public CButton(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeButton();
            Rect = new SRectF();
            Color = new SColorF();
            SColor = new SColorF();
            Texture = new STexture(-1);
            STexture = new STexture(-1);

            SelText = false;
            Text = new CText(_PartyModeID);
            SText = new CText(_PartyModeID);
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

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", ref _Theme.STextureName, String.Empty);
            
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, out Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SColorName, SkinIndex, out SColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SColor.A);
            }

            _ThemeLoaded &= Text.LoadTheme(item, "Text", xmlReader, SkinIndex, true);
            Text.Z = Rect.Z;
            if (xmlReader.ItemExists(item + "/SText"))
            {
                SelText = true;
                _ThemeLoaded &= SText.LoadTheme(item, "SText", xmlReader, SkinIndex, true);
                SText.Z = Rect.Z;
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
                SReflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Space", ref SReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Height", ref SReflectionHeight);
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
                if (_Theme.ColorName.Length > 0)
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
                if (_Theme.SColorName.Length > 0)
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
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme && !ForceDraw)
                return;

            STexture texture = new STexture(-1);

            if (!Selected && !Pressed || !Enabled)
            {
                if (Texture.index != -1)
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
            else if(!SelText)
            {
                if (Texture.index != -1)
                    texture = Texture;
                else
                    texture = CBase.Theme.GetSkinTexture(_Theme.STextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SColor);

                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SColor, Rect, ReflectionSpace, ReflectionHeight);
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
                    texture = CBase.Theme.GetSkinTexture(_Theme.STextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SColor);

                if (Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SColor, Rect, ReflectionSpace, ReflectionHeight);
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

            if (_Theme.ColorName.Length > 0)
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SColorName.Length > 0)
                SColor = CBase.Theme.GetColor(_Theme.SColorName, _PartyModeID);
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
