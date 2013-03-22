using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

enum EDirection
{
    Left,
    Right,
    Up
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
    public bool Active;

    public CCreditName(CStatic Image, CStatic ImgDot, CParticleEffect Particle, int ParticleOffsetX, int ParticleOffsetY)
    {
        _Image = Image;
        _ImgDot = ImgDot;
        _Particle = Particle;
        _ParticleOffsetX = (int)Math.Round(ParticleOffsetX * Image.Rect.W / Image.Texture.w2 - Particle.Rect.W / 2);
        _ParticleOffsetY = (int)Math.Round(ParticleOffsetY * Image.Rect.H / Image.Texture.h2 - Particle.Rect.H / 2);
        Active = true;
    }

    public float X
    {
        get { return _Image.Rect.X; }
        set
        {
            _Image.Rect.X = value;
            _ImgDot.Rect.X = value + _ParticleOffsetX;
            _Particle.Rect.X = value + _ParticleOffsetX;
        }
    }
    public float Y
    {
        get { return _Image.Rect.Y; }
        set
        {
            _Image.Rect.Y = value;
            _ImgDot.Rect.Y = value + _ParticleOffsetY;
            _Particle.Rect.Y = value + _ParticleOffsetY;
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

    public void Draw()
    {
        if (Active)
        {
            _Image.Draw();
            _ImgDot.Draw();
            _Particle.Draw();
        }
    }
}

namespace Vocaluxe.Screens
{
    class CScreenCredits : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

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

        private STexture _TexRedDot;
        private STexture _TexBlueDot;

        private STexture _TexNameBrunzel;
        private STexture _TexNameDarkice;
        private STexture _TexNameFlokuep;
        private STexture _TexNameMesand;
        private STexture _TexNameBohning;
        private STexture _TexNameBabene03;
        private STexture _TexNamePantero;
        private STexture _TexNamePinky007;

        public override void Init()
        {
            base.Init();

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
            words = paragraph.Split(new[] {' '});
            paragraphs.Add(words);

            paragraph = "This first public version has already implemented many of the original features and it is fully " +
                        "compatible with all the song files in your existing song collection. The code design allows a much faster " +
                        "implementation of new features, thus the roadmap for the next few stable releases is packed and we expect much " +
                        "shorter release cycles than ever before. And, of course, our and your ideas may be the features of tomorrow.";
            words = paragraph.Split(new[] {' '});
            paragraphs.Add(words);

            paragraph = "We appreciate the feedback in the beta release phase and are, of course, always open for bug reports, " +
                        "suggestions for improvements and ideas for new features. We would also like to thank the translators who make " +
                        "Vocaluxe an international experience from the very beginning and all those diligent song makers out there - " +
                        "there's something for everyone in the huge collection of available songs! Last but not least, thanks to " +
                        "Kosal Sen's Philly Sans type used in the Vocaluxe Logo.";
            words = paragraph.Split(new[] {' '});
            paragraphs.Add(words);

            paragraph = "Go ahead and grab your mics, crank up your stereo, warm up your voice and get ready to sing to the best " +
                        "of your abilities!";
            words = paragraph.Split(new[] {' '});
            paragraphs.Add(words);
        }

        public override void LoadTheme(string XmlPath)
        {
            //Vocaluxe-Logo
            CDataBase.GetCreditsRessource("Logo_voc.png", ref _TexLogo);

            //Little stars for logo
            CDataBase.GetCreditsRessource("PerfectNoteStar.png", ref _TexPerfectNoteStar);

            CDataBase.GetCreditsRessource("redDot.png", ref _TexRedDot);
            CDataBase.GetCreditsRessource("blueDot.png", ref _TexBlueDot);

            CDataBase.GetCreditsRessource("brunzel.png", ref _TexNameBrunzel);
            CDataBase.GetCreditsRessource("Darkice.png", ref _TexNameDarkice);
            CDataBase.GetCreditsRessource("flokuep.png", ref _TexNameFlokuep);
            CDataBase.GetCreditsRessource("bohning.png", ref _TexNameBohning);
            CDataBase.GetCreditsRessource("mesand.png", ref _TexNameMesand);
            CDataBase.GetCreditsRessource("babene03.png", ref _TexNameBabene03);
            CDataBase.GetCreditsRessource("pantero.png", ref _TexNamePantero);
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

        public override void ReloadTheme(string XmlPath) {}

        public override void SaveTheme() {}

        public override void UnloadTextures() {}

        public override void ReloadTextures() {}

        public override bool HandleInput(KeyEvent keyEvent)
        {
            if (!keyEvent.KeyPressed || Char.IsControl(keyEvent.Unicode))
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            if (mouseEvent.LB || mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        public override bool UpdateGame()
        {
            if (!animation())
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        public CParticleEffect _GetStarParticles(int NumStars, bool isRed, SRectF Rect, bool BigParticles)
        {
            SColorF partColor = (isRed) ? new SColorF(1, 0, 0, 1) : new SColorF(0.149f, 0.415f, 0.819f, 1);
            int partSize = (BigParticles) ? 35 : 25;
            return GetNewParticleEffect(NumStars, partColor, Rect, _TexPerfectNoteStar, partSize, EParticleType.Star);
        }

        private void _AddNewCreditName(STexture Texture, int ParticleOffsetX, int ParticleOffsetY, bool BigParticles)
        {
            bool isRight = _CreditNames.Count % 2 == 0;
            int partRectSize = (BigParticles) ? 25 : 20;
            int partCount = (BigParticles) ? 8 : 6;
            STexture TexDot = (isRight) ? _TexRedDot : _TexBlueDot;

            CStatic Image = GetNewStatic(Texture, new SColorF(1, 1, 1, 1), new SRectF(-1, -1, 400, 120, -4));

            SRectF particleRect = new SRectF(-1, -1, partRectSize, partRectSize, -6);
            SRectF imgDotRect = new SRectF(particleRect);
            imgDotRect.Z = -5;
            CStatic ImgDot = GetNewStatic(TexDot, new SColorF(1, 1, 1, 1), imgDotRect);
            CParticleEffect Particle = _GetStarParticles(partCount, isRight, particleRect, BigParticles);

            CCreditName credit = new CCreditName(Image, ImgDot, Particle, ParticleOffsetX, ParticleOffsetY);

            if (isRight)
            {
                credit.X = CSettings.iRenderW;
                credit.Direction = EDirection.Right;
            }
            else
            {
                credit.X = -450;
                credit.Direction = EDirection.Left;
            }
            credit.Y = 580;
            _CreditNames.Add(credit);
        }

        public override void OnShow()
        {
            base.OnShow();

            //Vocaluxe-Logo
            logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1), new SRectF((CSettings.iRenderW - _TexLogo.width) / 2, -270, _TexLogo.width, _TexLogo.height, -2));

            //Little stars for logo
            int numstars = (int)(logo.Rect.W * 0.25f / 2f);
            SRectF partRect = new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1);
            starsRed = _GetStarParticles(numstars, true, partRect, true);
            starsBlue = _GetStarParticles(numstars, false, partRect, true);

            //Credit names
            _CreditNames = new List<CCreditName>();

            _AddNewCreditName(_TexNameBrunzel, 502, 29, true);
            _AddNewCreditName(_TexNameDarkice, 360, 55, true);
            _AddNewCreditName(_TexNameFlokuep, 214, 14, true);
            _AddNewCreditName(_TexNameBohning, 383, 54, false);
            _AddNewCreditName(_TexNameMesand, 525, 13, false);
            _AddNewCreditName(_TexNameBabene03, 33, 26, false);
            _AddNewCreditName(_TexNamePantero, 311, 45, false);
            _AddNewCreditName(_TexNamePinky007, 113, 50, false);

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
            foreach (CCreditName cn in _CreditNames)
                cn.Draw();

            //Draw Text
            if (TextTimer.IsRunning)
            {
                for (int i = 0; i < paragraphTexts.Count; i++)
                    paragraphTexts[i].Draw();
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
                starsRed.Rect.Y = logo.Rect.Y;
                starsBlue.Rect.Y = logo.Rect.Y;
                if (LogoTimer.ElapsedMilliseconds >= 2000 && !CreditsTimer.IsRunning)
                    CreditsTimer.Start();
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
                    if (_CreditNames[i].Active)
                    {
                        switch (_CreditNames[i].Direction)
                        {
                            case EDirection.Right:
                                if (i * 4000f <= CreditsTimer.ElapsedMilliseconds)
                                {
                                    _CreditNames[i].X = -450 + (((CSettings.iRenderW - _CreditNames[i].W) / 2) / 3000f) * (CreditsTimer.ElapsedMilliseconds - (i * 4000f));

                                    //Check if name is in middle of screen and should go up
                                    if (_CreditNames[i].X >= (CSettings.iRenderW - _CreditNames[i].W) / 2)
                                    {
                                        _CreditNames[i].Direction = EDirection.Up;
                                        _CreditNames[i].StartTimeUp = CreditsTimer.ElapsedMilliseconds;
                                    }
                                }
                                break;

                            case EDirection.Left:
                                if (i * 4000f <= CreditsTimer.ElapsedMilliseconds)
                                {
                                    _CreditNames[i].X = CSettings.iRenderW -
                                                        (((CSettings.iRenderW - _CreditNames[i].W) / 2) / 3000f) * (CreditsTimer.ElapsedMilliseconds - (i * 4000f));

                                    //Check if name is in middle of screen and should go up
                                    if (_CreditNames[i].X <= (CSettings.iRenderW - _CreditNames[i].W) / 2)
                                    {
                                        _CreditNames[i].Direction = EDirection.Up;
                                        _CreditNames[i].StartTimeUp = CreditsTimer.ElapsedMilliseconds;
                                    }
                                }
                                break;

                            case EDirection.Up:
                                _CreditNames[i].Y = 580 - (430f / 3000f) * (CreditsTimer.ElapsedMilliseconds - _CreditNames[i].StartTimeUp);

                                //Set name inactive
                                if (_CreditNames[i].Y <= 160f)
                                {
                                    _CreditNames[i].Active = false;
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
                                else if (_CreditNames[i].Y <= 360f)
                                {
                                    //Fade names out
                                    float alpha = ((360 - _CreditNames[i].Y) / 200);
                                    //Catch some bad alpha-values
                                    if (alpha > 1)
                                        alpha = 1;
                                    else if (alpha < 0)
                                        alpha = 0;
                                    _CreditNames[i].Alpha = 1 - alpha;
                                }

                                break;
                        }
                    }
                }
            }

            if (TextTimer.IsRunning)
                active = TextTimer.ElapsedMilliseconds <= 60000;

            return active;
        }
    }
}