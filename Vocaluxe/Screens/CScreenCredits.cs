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
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    enum EDirection
    {
        Left,
        Right,
        Up
    }

    class CCreditTranslation
    {
        private readonly string _Language;
        private readonly List<CText> _Texts;
        public float StartTime;

        public float Y
        {
            get { return _Texts[0].Y; }
            set
            {
                float diff = value - _Texts[0].Y;
                foreach (CText t in _Texts)
                    t.Y += diff;
            }
        }

        public float Alpha
        {
            get { return _Texts[0].Alpha; }
            set
            {
                foreach (CText t in _Texts)
                    t.Alpha = value;
            }
        }

        public bool Visible
        {
            set
            {
                foreach (CText t in _Texts)
                    t.Visible = value;
            }
        }

        public float LastY
        {
            get
            {
                int lastText = _Texts.Count - 1;
                return _Texts[lastText].Y + _Texts[lastText].H;
            }
        }

        public CCreditTranslation(string language, List<CText> texts)
        {
            _Language = language;
            _Texts = texts;
        }

        public void Reset()
        {
            StartTime = -1;
            Alpha = 1f;
            Visible = true;
            _Texts[0].Y = CSettings.RenderH + 1;
            float y = CSettings.RenderH + 1;
            foreach (CText t in _Texts)
            {
                y += 25;
                t.Y = y;
            }
        }
    }

    class CCreditName
    {
        private readonly CStatic _Image;
        private readonly CStatic _ImgDot;
        private readonly CParticleEffect _Particle;
        private readonly int _ParticleOffsetX;
        private readonly int _ParticleOffsetY;
        public EDirection Direction;
        public float StartTimeUp;

        public CCreditName(CStatic image, CStatic imgDot, CParticleEffect particle, int particleOffsetX, int particleOffsetY)
        {
            _Image = image;
            _ImgDot = imgDot;
            _Particle = particle;
            _ParticleOffsetX = (int)Math.Round(particleOffsetX * image.Rect.W / image.Texture.OrigSize.Width - particle.Rect.W / 2);
            _ParticleOffsetY = (int)Math.Round(particleOffsetY * image.Rect.H / image.Texture.OrigSize.Height - particle.Rect.H / 2);
            Visible = true;
        }

        public float X
        {
            get { return _Image.X; }
            set
            {
                _Image.X = value;
                _ImgDot.X = value + _ParticleOffsetX;
                _Particle.X = value + _ParticleOffsetX;
            }
        }
        public float Y
        {
            get { return _Image.Rect.Y; }
            set
            {
                _Image.Y = value;
                _ImgDot.Y = value + _ParticleOffsetY;
                _Particle.Y = value + _ParticleOffsetY;
            }
        }

        public float W
        {
            get { return _Image.Rect.W; }
        }

        public float H
        {
            get { return _Image.Rect.H; }
        }

        public float Alpha
        {
            get { return _Image.Alpha; }
            set
            {
                _Image.Alpha = value;
                _ImgDot.Alpha = value;
            }
        }

        public bool Visible
        {
            set
            {
                _Image.Visible = value;
                _ImgDot.Visible = value;
                _Particle.Visible = value;
            }
            get { return _Image.Visible; }
        }
    }

    public class CScreenCredits : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private CStatic _Logo;
        private CParticleEffect _StarsRed;
        private CParticleEffect _StarsBlue;
        private List<CCreditName> _CreditNames;

        private List<CCreditTranslation> _Translations;
        private int _NumTranslationTexts;

        private Stopwatch _LogoTimer;
        private Stopwatch _CreditsTimer;
        private Stopwatch _TranslationsTimer;
        private Stopwatch _TextTimer;

        private List<string[]> _Paragraphs;
        private List<CText> _ParagraphTexts;

        private CTextureRef _TexLogo;
        private CTextureRef _TexPerfectNoteStar;

        private CTextureRef _TexRedDot;
        private CTextureRef _TexBlueDot;

        private CTextureRef _TexNameBrunzel;
        private CTextureRef _TexNameDarkice;
        private CTextureRef _TexNameFlokuep;
        private CTextureRef _TexNameFlamefire;
        private CTextureRef _TexNameLukeIam;
        private CTextureRef _TexNameMesand;
        private CTextureRef _TexNameBohning;
        private CTextureRef _TexNameBabene03;

        private SThemeBackground _BGTheme;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.None; }
        }

        public override void Init()
        {
            base.Init();

            _LogoTimer = new Stopwatch();
            _CreditsTimer = new Stopwatch();
            _TranslationsTimer = new Stopwatch();
            _TextTimer = new Stopwatch();

            //Text for last part of credits.
            _ParagraphTexts = new List<CText>();
            _Paragraphs = new List<string[]>();

            string paragraph = "Inspired by the achievements of UltraStar Deluxe and its variants and pursuing the goal of making " +
                               "a good thing even better, we ended up rewriting the game from scratch. And a new implementation in a new " +
                               "programming language called for a new name - and VOCALUXE [ˈvoʊˈkəˈlʌks] it is!";
            string[] words = paragraph.Split(new char[] {' '});
            _Paragraphs.Add(words);

            paragraph = "This first public version has already implemented many of the original features and it is fully " +
                        "compatible with all the song files in your existing song collection. The code design allows a much faster " +
                        "implementation of new features, thus the roadmap for the next few stable releases is packed and we expect much " +
                        "shorter release cycles than ever before. And, of course, our and your ideas may be the features of tomorrow.";
            words = paragraph.Split(new char[] {' '});
            _Paragraphs.Add(words);

            paragraph = "We appreciate the feedback in the beta release phase and are, of course, always open for bug reports, " +
                        "suggestions for improvements and ideas for new features. We would also like to thank the translators who make " +
                        "Vocaluxe an international experience from the very beginning and all those diligent song makers out there - " +
                        "there's something for everyone in the huge collection of available songs! Last but not least, thanks to " +
                        "Kosal Sen's Philly Sans type used in the Vocaluxe Logo.";
            words = paragraph.Split(new char[] {' '});
            _Paragraphs.Add(words);

            paragraph = "Go ahead and grab your mics, crank up your stereo, warm up your voice and get ready to sing to the best " +
                        "of your abilities!";
            words = paragraph.Split(new char[] {' '});
            _Paragraphs.Add(words);

            _BGTheme.Type = EBackgroundTypes.Color;
            _BGTheme.Color = new SThemeColor {Name = null, R = 0, G = 0.18f, B = 0.474f, A = 1};
        }

        public override void LoadTheme(string xmlPath)
        {
            bool ressourceOK = true;
            //Vocaluxe-Logo
            ressourceOK &= CDataBase.GetCreditsRessource("Logo_voc.png", ref _TexLogo);

            //Little stars for logo
            ressourceOK &= CDataBase.GetCreditsRessource("PerfectNoteStar.png", ref _TexPerfectNoteStar);

            ressourceOK &= CDataBase.GetCreditsRessource("redDot.png", ref _TexRedDot);
            ressourceOK &= CDataBase.GetCreditsRessource("blueDot.png", ref _TexBlueDot);

            ressourceOK &= CDataBase.GetCreditsRessource("brunzel.png", ref _TexNameBrunzel);
            ressourceOK &= CDataBase.GetCreditsRessource("Darkice.png", ref _TexNameDarkice);
            ressourceOK &= CDataBase.GetCreditsRessource("flokuep.png", ref _TexNameFlokuep);
            ressourceOK &= CDataBase.GetCreditsRessource("flamefire.png", ref _TexNameFlamefire);
            ressourceOK &= CDataBase.GetCreditsRessource("lukeIam.png", ref _TexNameLukeIam);
            ressourceOK &= CDataBase.GetCreditsRessource("bohning.png", ref _TexNameBohning);
            ressourceOK &= CDataBase.GetCreditsRessource("mesand.png", ref _TexNameMesand);
            ressourceOK &= CDataBase.GetCreditsRessource("babene03.png", ref _TexNameBabene03);

            if (!ressourceOK)
                CLog.Fatal("Could not load all ressources!");

            //Prepare Text
            int lastY = 280;
            foreach (string[] paragraph in _Paragraphs)
            {
                string line = "";
                for (int e = 0; e < paragraph.Length; e++)
                {
                    if (paragraph[e] == null)
                        continue;
                    string newLine = " " + paragraph[e];
                    CText text = GetNewText(75, lastY, -2, 25, -1, EAlignment.Left, EStyle.Bold, "Outline", new SColorF(1, 1, 1, 1), line);
                    text.Visible = false;
                    if (CFonts.GetTextBounds(text).Width < (CSettings.RenderW - 220))
                    {
                        line += newLine;

                        //Check if all words are used
                        if ((e + 1) == paragraph.Length)
                        {
                            text.Text = line;
                            _ParagraphTexts.Add(text);
                            line = "";
                            lastY += 40;
                        }
                    }
                    else
                    {
                        _ParagraphTexts.Add(text);
                        line = newLine;
                        lastY += 27;
                    }
                    _AddText(text);
                }
            }

            CBackground bg = new CBackground(_BGTheme, -1);
            bg.LoadSkin();
            _AddBackground(bg);

            //Vocaluxe-Logo
            _Logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1),
                                 new SRectF((float)(CSettings.RenderW - _TexLogo.OrigSize.Width) / 2, -270, _TexLogo.OrigSize.Width, _TexLogo.OrigSize.Height, -2));
            _AddStatic(_Logo);

            //Little stars for logo
            var numstars = (int)(_Logo.Rect.W * 0.25f / 2f);
            var partRect = new SRectF(_Logo.Rect.X, _Logo.Rect.Y, _Logo.Rect.W, _Logo.Rect.H, -1);
            _StarsRed = _GetStarParticles(numstars, true, partRect, true);
            _StarsBlue = _GetStarParticles(numstars, false, partRect, true);
            _AddParticleEffect(_StarsRed);
            _AddParticleEffect(_StarsBlue);

            //Credit names
            _CreditNames = new List<CCreditName>();

            _AddNewCreditName(_TexNameBrunzel, 502, 29, true);
            _AddNewCreditName(_TexNameDarkice, 360, 55, true);
            _AddNewCreditName(_TexNameFlokuep, 214, 14, true);
            _AddNewCreditName(_TexNameFlamefire, 496, 46, true);
            _AddNewCreditName(_TexNameLukeIam, 411, 26, true);
            _AddNewCreditName(_TexNameBohning, 383, 54, false);
            _AddNewCreditName(_TexNameMesand, 525, 13, false);
            _AddNewCreditName(_TexNameBabene03, 33, 26, false);

            _AddTranslations();
        }

        public override void ReloadTheme(string xmlPath) {}

        public override void SaveTheme() {}

        public override void UnloadSkin() {}

        public override void ReloadSkin() {}

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (!keyEvent.KeyPressed || Char.IsControl(keyEvent.Unicode))
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        CGraphics.FadeTo(EScreen.Main);
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB || mouseEvent.RB)
                CGraphics.FadeTo(EScreen.Main);
            return true;
        }

        public override bool UpdateGame()
        {
            if (!_Animation() && CGraphics.NextScreen != CGraphics.GetScreen(EScreen.Main))
                CGraphics.FadeTo(EScreen.Main);
            return true;
        }

        private CParticleEffect _GetStarParticles(int numStars, bool isRed, SRectF rect, bool bigParticles)
        {
            SColorF partColor = isRed ? new SColorF(1, 0, 0, 1) : new SColorF(0.149f, 0.415f, 0.819f, 1);
            int partSize = bigParticles ? 35 : 25;
            return GetNewParticleEffect(numStars, partColor, rect, _TexPerfectNoteStar, partSize, EParticleType.Star);
        }

        private void _AddNewCreditName(CTextureRef texture, int particleOffsetX, int particleOffsetY, bool bigParticles)
        {
            bool isRed = _CreditNames.Count % 2 == 0;
            int partRectSize = bigParticles ? 25 : 20;
            int partCount = bigParticles ? 8 : 6;
            CTextureRef texDot = isRed ? _TexRedDot : _TexBlueDot;

            CStatic image = GetNewStatic(texture, new SColorF(1, 1, 1, 1), new SRectF(-1, -1, 400, 120, -4));

            var particleRect = new SRectF(-1, -1, partRectSize, partRectSize, -6);
            SRectF imgDotRect = particleRect;
            imgDotRect.Z = -5;
            CStatic imgDot = GetNewStatic(texDot, new SColorF(1, 1, 1, 1), imgDotRect);
            CParticleEffect particle = _GetStarParticles(partCount, isRed, particleRect, bigParticles);

            var credit = new CCreditName(image, imgDot, particle, particleOffsetX, particleOffsetY);

            _CreditNames.Add(credit);
            _AddStatic(image);
            _AddStatic(imgDot);
            _AddParticleEffect(particle);
        }

        private void _AddTranslations()
        {
            CCreditTranslation intro = _AddNewTranslation("A special thanks to all our translators", new List<string> { });
            CCreditTranslation asturian = _AddNewTranslation("Asturian", new List<string> { "Puxarra" });
            CCreditTranslation czech = _AddNewTranslation("Czech", new List<string> { "fri" });
            CCreditTranslation dutch = _AddNewTranslation("Dutch", new List<string> { "thijsblaauw", "DeMarin" });
            CCreditTranslation french = _AddNewTranslation("French", new List<string> { "pinky007", "javafrog" });
            CCreditTranslation hungarian = _AddNewTranslation("Hungarian", new List<string> { "warez", "skyli" });
            CCreditTranslation italian = _AddNewTranslation("Italian", new List<string> { "giuseppep", "LFactory", "yogotosleepnow" });
            CCreditTranslation portugese = _AddNewTranslation("Portuguese", new List<string> { "2borG", "xventil" });
            CCreditTranslation spanish = _AddNewTranslation("Spanish", new List<string> { "Pantero03", "RubenDjOn", "TeLiX", "karv" });
            CCreditTranslation swedish = _AddNewTranslation("Swedish", new List<string> { "u28151", "Jiiniasu" });
            CCreditTranslation turkish = _AddNewTranslation("Turkish", new List<string> { "spirax", "Swertyy" });

            _Translations = new List<CCreditTranslation> { intro, asturian, czech, dutch, french, hungarian, italian, spanish, swedish, turkish };
        }

        private CCreditTranslation _AddNewTranslation(string language, List<string> translators)
        {
            List<CText> texts = new List<CText>();
            CText text = GetNewText(new CText(CSettings.RenderW / 2, CSettings.RenderH + 1, -4f, 30, -1, EAlignment.Center, EStyle.Bold, "Outline", new SColorF(1, 1, 1, 1), language));
            _AddText(text);
            texts.Add(text);
            _NumTranslationTexts++;
            float y = texts[0].Y;
            foreach (string t in translators)
            {
                y += 30;
                text = GetNewText(new CText(CSettings.RenderW / 2, y, -4f, 27, -1, EAlignment.Center, EStyle.Normal, "Outline", new SColorF(1, 1, 1, 1), t));
                _AddText(text);
                texts.Add(text);
                _NumTranslationTexts++;
            }
            CCreditTranslation cct = new CCreditTranslation(language, texts);
            return cct;
        }

        public override void OnShow()
        {
            base.OnShow();

            foreach (CText text in _ParagraphTexts)
                text.Visible = false;
            bool isRight = true;
            foreach (CCreditName name in _CreditNames)
            {
                name.Visible = true;
                name.Direction = isRight ? EDirection.Right : EDirection.Left;
                name.X = -name.W;
                name.Y = 580;
                name.Alpha = 1;
                isRight = !isRight;
            }
            _Logo.Y = -270;
            _StarsRed.Y = _Logo.Y;
            _StarsBlue.Y = _Logo.Y;
            foreach (CCreditTranslation translation in _Translations)
                translation.Reset();

            _TextTimer.Reset();
            _LogoTimer.Reset();
            _CreditsTimer.Reset();
            _TranslationsTimer.Reset();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            _LogoTimer.Start();
        }

        private bool _Animation()
        {
            bool active = false;
            if (_LogoTimer.IsRunning)
            {
                active = true;

                _Logo.Y = -270 + (270f / 3000f) * _LogoTimer.ElapsedMilliseconds;
                _StarsRed.Y = _Logo.Y;
                _StarsBlue.Y = _Logo.Y;
                if (_LogoTimer.ElapsedMilliseconds >= 2000 && !_CreditsTimer.IsRunning)
                    _CreditsTimer.Start();
                if (_LogoTimer.ElapsedMilliseconds >= 3000)
                    _LogoTimer.Stop();
            }

            if (_CreditsTimer.IsRunning)
            {
                active = true;

                for (int i = 0; i < _CreditNames.Count; i++)
                {
                    if (!_CreditNames[i].Visible)
                        continue;
                    switch (_CreditNames[i].Direction)
                    {
                        case EDirection.Right:
                            if (i * 4000f <= _CreditsTimer.ElapsedMilliseconds)
                            {
                                _CreditNames[i].X = -450 + (((CSettings.RenderW - _CreditNames[i].W) / 2) / 3000f) * (_CreditsTimer.ElapsedMilliseconds - (i * 4000f));

                                //Check if name is in middle of screen and should go up
                                if (_CreditNames[i].X >= (CSettings.RenderW - _CreditNames[i].W) / 2)
                                {
                                    _CreditNames[i].Direction = EDirection.Up;
                                    _CreditNames[i].StartTimeUp = _CreditsTimer.ElapsedMilliseconds;
                                }
                            }
                            break;

                        case EDirection.Left:
                            if (i * 4000f <= _CreditsTimer.ElapsedMilliseconds)
                            {
                                _CreditNames[i].X = CSettings.RenderW -
                                                    (((CSettings.RenderW - _CreditNames[i].W) / 2) / 3000f) * (_CreditsTimer.ElapsedMilliseconds - (i * 4000f));

                                //Check if name is in middle of screen and should go up
                                if (_CreditNames[i].X <= (CSettings.RenderW - _CreditNames[i].W) / 2)
                                {
                                    _CreditNames[i].Direction = EDirection.Up;
                                    _CreditNames[i].StartTimeUp = _CreditsTimer.ElapsedMilliseconds;
                                }
                            }
                            break;

                        case EDirection.Up:
                            _CreditNames[i].Y = 580 - (430f / 3000f) * (_CreditsTimer.ElapsedMilliseconds - _CreditNames[i].StartTimeUp);

                            //Set name inactive
                            if (_CreditNames[i].Y <= 160f)
                            {
                                _CreditNames[i].Visible = false;
                                //Check, if last name is shown
                                if (i == _CreditNames.Count - 1)
                                {
                                    _CreditsTimer.Stop();
                                    _TranslationsTimer.Start();
                                }
                            }
                            else if (_CreditNames[i].Y <= 360f)
                            {
                                //Fade names out
                                float alpha = ((360 - _CreditNames[i].Y) / 200).Clamp(0, 1);
                                _CreditNames[i].Alpha = 1 - alpha;
                            }

                            break;
                    }
                }
            }
            if (_TranslationsTimer.IsRunning)
            {
                active = true;

                for (int i = 0; i < _Translations.Count; i++) 
                {
                    if (i * 1500f < _TranslationsTimer.ElapsedMilliseconds)
                    {
                        if (_Translations[i].StartTime == -1)
                            _Translations[i].StartTime = _TranslationsTimer.ElapsedMilliseconds;
                        float newY = 720f - (520f / 5000f) * (_TranslationsTimer.ElapsedMilliseconds - _Translations[i].StartTime);
                        _Translations[i].Y = newY;
                    }

                    if (_Translations[i].Y <= 160f)
                    {
                        _Translations[i].Visible = false;
                        if (i == _Translations.Count - 1)
                        {
                            _TranslationsTimer.Stop();
                            _TextTimer.Start();
                            foreach (CText text in _ParagraphTexts)
                                text.Visible = true;
                        }
                    }
                    if (_Translations[i].Y <= 360f)
                    {
                        //Fade out
                        float alpha = ((360 - _Translations[i].Y) / 200).Clamp(0,1);
                        _Translations[i].Alpha = 1 - alpha;
                    }
                }

                
            }
            if (_TextTimer.IsRunning)
                active = _TextTimer.ElapsedMilliseconds <= 10000;

            return active;
        }
    }
}