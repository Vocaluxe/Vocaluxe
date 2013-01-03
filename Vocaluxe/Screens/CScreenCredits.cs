using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

enum EDirection
{
    Left,
    Right,
    Up
}

class CCreditName
{
    public CStatic image;
    public CParticleEffect particle;
    public SRectF particleRect;
    public EDirection direction;
    public float StartTimeUp;
    public bool active;
}

namespace Vocaluxe.Screens
{
    class CScreenCredits :CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private CStatic logo;
        private CParticleEffect starsRed;
        private CParticleEffect starsBlue;
        private List<CCreditName> _CreditNames;

        private Stopwatch LogoTimer;
        private Stopwatch CreditsTimer;
        private Stopwatch TextTimer;

        private List<string[]> paragraphs;
        private List<CText> paragraphTexts;

        private STexture _TexLogo;
        private STexture _TexPerfectNoteStar;

        private STexture _TexNameBrunzel;
        private STexture _TexNameDarkice;
        private STexture _TexNameFlokuep;
        private STexture _TexNameMesand;
        private STexture _TexNameBohning;
        private STexture _TexNameBabene03;
        private STexture _TexNamePantero;
        private STexture _TexNamePinky007;
       
        public CScreenCredits()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenCredits";
            _ScreenVersion = ScreenVersion;

            LogoTimer = new Stopwatch();
            CreditsTimer = new Stopwatch();
            TextTimer = new Stopwatch();

            //Text for last part of credits.
            paragraphTexts = new List<CText>();
            paragraphs = new List<string[]>();

            string paragraph;
            string[] words;

            paragraph = "Inspired by the achievements of UltraStar Deluxe and its variants and pursuing the goal of making " +
                "a good thing even better, we ended up rewriting the game from scratch. And a new implementation in a new " +
                "programming language called for a new name - and VOCALUXE [ˈvoʊˈkəˈlʌks] it is!";
            words = paragraph.Split(new Char[] { ' ' });
            paragraphs.Add(words);

            paragraph = "This first public version has already implemented many of the original features and it is fully " +
                "compatible with all the song files in your existing song collection. The code design allows a much faster " +
                "implementation of new features, thus the roadmap for the next few stable releases is packed and we expect much " +
                "shorter release cycles than ever before. And, of course, our and your ideas may be the features of tomorrow.";
            words = paragraph.Split(new Char[] { ' ' });
            paragraphs.Add(words);

            paragraph = "We appreciate the feedback in the beta release phase and are, of course, always open for bug reports, " +
                "suggestions for improvements and ideas for new features. We would also like to thank the translators who make " +
                "Vocaluxe an international experience from the very beginning and all those diligent song makers out there - " +
                "there's something for everyone in the huge collection of available songs! Last but not least, thanks to " +
                "Kosal Sen's Philly Sans type used in the Vocaluxe Logo.";
            words = paragraph.Split(new Char[] { ' ' });
            paragraphs.Add(words);

            paragraph = "Go ahead and grab your mics, crank up your stereo, warm up your voice and get ready to sing to the best " +
                "of your abilities!";
            words = paragraph.Split(new Char[] { ' ' });
            paragraphs.Add(words);
        }

        public override void LoadTheme(string XmlPath)
        {
            //Vocaluxe-Logo
            CDataBase.GetCreditsRessource("Logo_voc.png", ref _TexLogo);
            
            //Little stars for logo
            CDataBase.GetCreditsRessource("PerfectNoteStar.png", ref _TexPerfectNoteStar);
            
            //brunzel
            CDataBase.GetCreditsRessource("brunzel.png", ref _TexNameBrunzel);
            
            //Darkice
            CDataBase.GetCreditsRessource("Darkice.png", ref _TexNameDarkice);
            
            //flokuep
            CDataBase.GetCreditsRessource("flokuep.png", ref _TexNameFlokuep);
            
            //bohning
            CDataBase.GetCreditsRessource("bohning.png", ref _TexNameBohning);
            
            //mesand
            CDataBase.GetCreditsRessource("mesand.png", ref _TexNameMesand);
            
            //babene03
            CDataBase.GetCreditsRessource("babene03.png", ref _TexNameBabene03);
            
            //pantero
            CDataBase.GetCreditsRessource("pantero.png", ref _TexNamePantero);
            
            //Pinky007
            CDataBase.GetCreditsRessource("Pinky007.png", ref _TexNamePinky007);
            
            //Prepare Text
            int lastY = 280;
            for (int i = 0; i < paragraphs.Count; i++)
            {
                string line = "";
                for (int e = 0; e < paragraphs[i].Length; e++)
                {
                    if (paragraphs[i][e] != null)
                    {
                        string newline = line + " " + paragraphs[i][e];
                        CText text = GetNewText(75, lastY, -2, 30, -1, EAlignment.Left, EStyle.Bold, "Outline", new SColorF(1, 1, 1, 1), line);
                        if (CDraw.GetTextBounds(text).Width < (CSettings.iRenderW - 220))
                        {
                            line = line + " " + paragraphs[i][e];

                            //Check if all words are used
                            if ((e + 1) == paragraphs[i].Length)
                            {
                                text.Text = line;
                                paragraphTexts.Add(text);
                                line = "";
                                lastY += 40;
                            }
                        }
                        else
                        {
                            paragraphTexts.Add(text);
                            line = " " + paragraphs[i][e];
                            lastY += 27;
                        }
                    }
                }
            }

        }

        public override void ReloadTheme(string XmlPath) { }

        public override void SaveTheme() { }

        public override void UnloadTextures() {}

        public override void ReloadTextures() {}

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {

            }

            if (MouseEvent.LB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (!animation())
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            //Vocaluxe-Logo
            logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1), new SRectF((CSettings.iRenderW - _TexLogo.width) / 2, -270, _TexLogo.width, _TexLogo.height, -2));
            
            //Little stars for logo
            int numstars = (int)(logo.Rect.W * 0.25f / 2f);
            starsRed = GetNewParticleEffect(numstars, new SColorF(1, 0, 0, 1), new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1), _TexPerfectNoteStar, 35, EParticleType.Star);
            starsBlue = GetNewParticleEffect(numstars, new SColorF(0.149f, 0.415f, 0.819f, 1), new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1), _TexPerfectNoteStar, 35, EParticleType.Star);

            //Credit names
            _CreditNames = new List<CCreditName>();
            CCreditName CreditEntry = new CCreditName();

            //brunzel
            CCreditName CreditEntryBrunzel = new CCreditName();
            CreditEntryBrunzel.image = GetNewStatic(_TexNameBrunzel, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 400, 120, -4));
            CreditEntryBrunzel.particleRect = new SRectF(CreditEntryBrunzel.image.Rect.X + 342, CreditEntryBrunzel.image.Rect.Y + 4, 30, 30, -5);
            CreditEntryBrunzel.particle = GetNewParticleEffect(8, new SColorF(1, 0, 0, 1), CreditEntryBrunzel.particleRect, _TexPerfectNoteStar, 35, EParticleType.Star);
            CreditEntryBrunzel.active = true;
            CreditEntryBrunzel.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryBrunzel);

            //Darkice
            CCreditName CreditEntryDarkice = new CCreditName();
            CreditEntryDarkice.image = GetNewStatic(_TexNameDarkice, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 400, 120, -4));
            CreditEntryDarkice.particleRect = new SRectF(CreditEntryDarkice.image.Rect.X + 242, CreditEntryDarkice.image.Rect.Y + 23, 30, 30, -5);
            CreditEntryDarkice.particle = GetNewParticleEffect(8, new SColorF(0.149f, 0.415f, 0.819f, 1), CreditEntryDarkice.particleRect, _TexPerfectNoteStar, 35, EParticleType.Star);
            CreditEntryDarkice.active = true;
            CreditEntryDarkice.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryDarkice);

            //flokuep
            CCreditName CreditEntryFlokuep = new CCreditName();
            CreditEntryFlokuep.image = GetNewStatic(_TexNameFlokuep, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 400, 120, -4));
            CreditEntryFlokuep.particleRect = new SRectF(CreditEntryFlokuep.image.Rect.X + 141, CreditEntryFlokuep.image.Rect.Y - 2, 30, 30, -5);
            CreditEntryFlokuep.particle = GetNewParticleEffect(8, new SColorF(1, 0, 0, 1), CreditEntryFlokuep.particleRect, _TexPerfectNoteStar, 35, EParticleType.Star);
            CreditEntryFlokuep.active = true;
            CreditEntryFlokuep.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryFlokuep);

            //bohning
            CCreditName CreditEntryBohning = new CCreditName();
            CreditEntryBohning.image = GetNewStatic(_TexNameBohning, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryBohning.particleRect = new SRectF(CreditEntryBohning.image.Rect.X + 172, CreditEntryBohning.image.Rect.Y + 16, 10, 10, -5);
            CreditEntryBohning.particle = GetNewParticleEffect(4, new SColorF(0.149f, 0.415f, 0.819f, 1), CreditEntryBohning.particleRect, _TexPerfectNoteStar, 25, EParticleType.Star);
            CreditEntryBohning.active = true;
            CreditEntryBohning.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryBohning);

            //mesand
            CCreditName CreditEntryMesand = new CCreditName();
            CreditEntryMesand.image = GetNewStatic(_TexNameMesand, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 350, 110, -4));
            CreditEntryMesand.particleRect = new SRectF(CreditEntryMesand.image.Rect.X + 240, CreditEntryMesand.image.Rect.Y - 2, 10, 10, -5);
            CreditEntryMesand.particle = GetNewParticleEffect(4, new SColorF(1, 0, 0, 1), CreditEntryMesand.particleRect, _TexPerfectNoteStar, 25, EParticleType.Star);
            CreditEntryMesand.active = true;
            CreditEntryMesand.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryMesand);

            //babene03
            CCreditName CreditEntryBabene03 = new CCreditName();
            CreditEntryBabene03.image = GetNewStatic(_TexNameBabene03, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryBabene03.particleRect = new SRectF(CreditEntryBabene03.image.Rect.X + 7, CreditEntryBabene03.image.Rect.Y + 4, 10, 10, -5);
            CreditEntryBabene03.particle = GetNewParticleEffect(4, new SColorF(0.149f, 0.415f, 0.819f, 1), CreditEntryBabene03.particleRect, _TexPerfectNoteStar, 25, EParticleType.Star);
            CreditEntryBabene03.active = true;
            CreditEntryBabene03.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryBabene03);

            //pantero
            CCreditName CreditEntrypantero = new CCreditName();
            CreditEntrypantero.image = GetNewStatic(_TexNamePantero, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 350, 110, -4));
            CreditEntrypantero.particleRect = new SRectF(CreditEntrypantero.image.Rect.X + 140, CreditEntrypantero.image.Rect.Y + 15, 10, 10, -5);
            CreditEntrypantero.particle = GetNewParticleEffect(4, new SColorF(1, 0, 0, 1), CreditEntrypantero.particleRect, _TexPerfectNoteStar, 25, EParticleType.Star);
            CreditEntrypantero.active = true;
            CreditEntrypantero.direction = EDirection.Left;
            _CreditNames.Add(CreditEntrypantero);

            //Pinky007
            CCreditName CreditEntryPinky007 = new CCreditName();
            CreditEntryPinky007.image = GetNewStatic(_TexNamePinky007, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryPinky007.particleRect = new SRectF(CreditEntryPinky007.image.Rect.X + 42, CreditEntryPinky007.image.Rect.Y + 15, 10, 10, -5);
            CreditEntryPinky007.particle = GetNewParticleEffect(4, new SColorF(0.149f, 0.415f, 0.819f, 1), CreditEntryPinky007.particleRect, _TexPerfectNoteStar, 25, EParticleType.Star);
            CreditEntryPinky007.active = true;
            CreditEntryPinky007.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryPinky007);

            TextTimer.Reset();
            LogoTimer.Reset();
            CreditsTimer.Reset();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            LogoTimer.Start();
        }

        public override bool Draw()
        {
            base.Draw();

            //Draw background
            CDraw.DrawColor(new SColorF(0, 0.18f, 0.474f, 1), new SRectF(0, 0, CSettings.iRenderW, CSettings.iRenderH, 0));

            //Draw stars
            starsBlue.Draw();
            starsRed.Draw();

            logo.Draw();

            //Draw credit-entries
            for (int i = 0; i < _CreditNames.Count; i++)
            {
                if (_CreditNames[i].active)
                {
                    _CreditNames[i].image.Draw();
                    _CreditNames[i].particle.Draw();
                }
            }

            //Draw Text
            if (TextTimer.IsRunning)
            {
                for (int i = 0; i < paragraphTexts.Count; i++)
                {
                    paragraphTexts[i].Draw();
                }
            }
            return true;
        }

        private bool animation()
        {
            bool active = false;
            if (LogoTimer.IsRunning)
            {
                active = true;

                logo.Rect.Y = -270 + (270f / 3000f) * LogoTimer.ElapsedMilliseconds;
                SRectF rect = new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1);
                starsRed.Rect = rect;
                starsBlue.Rect = rect;
                if (LogoTimer.ElapsedMilliseconds >= 2000 && !CreditsTimer.IsRunning)
                {
                    CreditsTimer.Start();
                }
                if (LogoTimer.ElapsedMilliseconds >= 3000)
                {
                    LogoTimer.Stop();
                    LogoTimer.Reset();
                }
            }

            if (CreditsTimer.IsRunning)
            {
                active = true;

                for (int i = 0; i < _CreditNames.Count; i++)
                {
                    if (_CreditNames[i].active)
                    {
                        switch (_CreditNames[i].direction)
                        {
                            case EDirection.Left:
                                if (i * 4000f <= CreditsTimer.ElapsedMilliseconds)
                                {
                                    float xOld = _CreditNames[i].image.Rect.X;
                                    _CreditNames[i].image.Rect.X = -450 + (((CSettings.iRenderW - _CreditNames[i].image.Rect.W) / 2)/3000f) * (CreditsTimer.ElapsedMilliseconds-(i*4000f));
                                    _CreditNames[i].particleRect.X += (_CreditNames[i].image.Rect.X - xOld);
                                    _CreditNames[i].particle.Rect = _CreditNames[i].particleRect;

                                    //Check if name is in middle of screen and should go up
                                    if (_CreditNames[i].image.Rect.X >= (CSettings.iRenderW - _CreditNames[i].image.Rect.W) / 2)
                                    {
                                        _CreditNames[i].direction = EDirection.Up;
                                        _CreditNames[i].StartTimeUp = CreditsTimer.ElapsedMilliseconds;
                                    }

                                }
                                break;

                            case EDirection.Right:
                                if (i * 4000f <= CreditsTimer.ElapsedMilliseconds)
                                {
                                    float xOld = _CreditNames[i].image.Rect.X;
                                    _CreditNames[i].image.Rect.X = CSettings.iRenderW - (((CSettings.iRenderW - _CreditNames[i].image.Rect.W) / 2) / 3000f) * (CreditsTimer.ElapsedMilliseconds - (i * 4000f));
                                    _CreditNames[i].particleRect.X -= (xOld - _CreditNames[i].image.Rect.X);
                                    _CreditNames[i].particle.Rect = _CreditNames[i].particleRect;

                                    //Check if name is in middle of screen and should go up
                                    if (_CreditNames[i].image.Rect.X <= (CSettings.iRenderW - _CreditNames[i].image.Rect.W) / 2 )
                                    {
                                        _CreditNames[i].direction = EDirection.Up;
                                        _CreditNames[i].StartTimeUp = CreditsTimer.ElapsedMilliseconds;
                                    }

                                }
                                break;

                            case EDirection.Up:
                                float yOld = _CreditNames[i].image.Rect.Y;
                                _CreditNames[i].image.Rect.Y = 580 - (430f / 3000f) * (CreditsTimer.ElapsedMilliseconds - _CreditNames[i].StartTimeUp);
                                _CreditNames[i].particleRect.Y -= (yOld - _CreditNames[i].image.Rect.Y);
                                _CreditNames[i].particle.Rect = _CreditNames[i].particleRect;

                                //Fade names out
                                if (_CreditNames[i].image.Rect.Y <= 350f)
                                {
                                    //Catch some bad alpha-values
                                    float alpha = ((250 - _CreditNames[i].image.Rect.Y) / 20);
                                    if(alpha > 1)
                                    {
                                        alpha = 1;
                                    }
                                    else if(alpha < 0)
                                    {
                                        alpha = 0;
                                    }
                                    _CreditNames[i].image.Alpha = 1 - alpha;
                                }

                                //Set name inactive
                                if (_CreditNames[i].image.Rect.Y <= 150f)
                                {
                                    _CreditNames[i].active = false;
                                    //Check, if last name is set to false
                                    if (i == _CreditNames.Count - 1)
                                    {
                                        //Stop and reset timer for next time
                                        CreditsTimer.Stop();
                                        CreditsTimer.Reset();

                                        //Start Text-Timer
                                        TextTimer.Start();
                                        active = true;
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            if (TextTimer.IsRunning)
            {
                if (TextTimer.ElapsedMilliseconds > 600000)
                {
                    active = false;
                }
                else
                {
                    active = true;
                }
            }

            return active;
        }
    }
}
