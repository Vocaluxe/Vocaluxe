#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
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

        public CCreditName(CStatic image, CStatic imgDot, CParticleEffect particle, int particleOffsetX, int particleOffsetY)
        {
            _Image = image;
            _ImgDot = imgDot;
            _Particle = particle;
            _ParticleOffsetX = (int)Math.Round(particleOffsetX * image.Rect.W / image.Texture.OrigSize.Width - particle.Rect.W / 2);
            _ParticleOffsetY = (int)Math.Round(particleOffsetY * image.Rect.H / image.Texture.OrigSize.Height - particle.Rect.H / 2);
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

    class CScreenCredits : CMenu
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

        private Stopwatch _LogoTimer;
        private Stopwatch _CreditsTimer;
        private Stopwatch _TextTimer;

        private List<string[]> _Paragraphs;
        private List<CText> _ParagraphTexts;

        private CTexture _TexLogo;
        private CTexture _TexPerfectNoteStar;

        private CTexture _TexRedDot;
        private CTexture _TexBlueDot;

        private CTexture _TexNameBrunzel;
        private CTexture _TexNameDarkice;
        private CTexture _TexNameFlokuep;
        private CTexture _TexNameFlamefire;
        private CTexture _TexNameMesand;
        private CTexture _TexNameBohning;
        private CTexture _TexNameBabene03;
        private CTexture _TexNamePantero;
        private CTexture _TexNamePinky007;

        public override void Init()
        {
            base.Init();

            _LogoTimer = new Stopwatch();
            _CreditsTimer = new Stopwatch();
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
        }

        public override void LoadTheme(string xmlPath)
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
            CDataBase.GetCreditsRessource("flamefire.png", ref _TexNameFlamefire);
            CDataBase.GetCreditsRessource("bohning.png", ref _TexNameBohning);
            CDataBase.GetCreditsRessource("mesand.png", ref _TexNameMesand);
            CDataBase.GetCreditsRessource("babene03.png", ref _TexNameBabene03);
            CDataBase.GetCreditsRessource("pantero.png", ref _TexNamePantero);
            CDataBase.GetCreditsRessource("Pinky007.png", ref _TexNamePinky007);

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
                }
            }
        }

        public override void ReloadTheme(string xmlPath) {}

        public override void SaveTheme() {}

        public override void UnloadTextures() {}

        public override void ReloadTextures() {}

        public override bool HandleInput(SKeyEvent keyEvent)
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

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB || mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        public override bool UpdateGame()
        {
            if (!_Animation())
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        private CParticleEffect _GetStarParticles(int numStars, bool isRed, SRectF rect, bool bigParticles)
        {
            SColorF partColor = isRed ? new SColorF(1, 0, 0, 1) : new SColorF(0.149f, 0.415f, 0.819f, 1);
            int partSize = bigParticles ? 35 : 25;
            return GetNewParticleEffect(numStars, partColor, rect, _TexPerfectNoteStar, partSize, EParticleType.Star);
        }

        private void _AddNewCreditName(CTexture texture, int particleOffsetX, int particleOffsetY, bool bigParticles)
        {
            bool isRight = _CreditNames.Count % 2 == 0;
            int partRectSize = bigParticles ? 25 : 20;
            int partCount = bigParticles ? 8 : 6;
            CTexture texDot = isRight ? _TexRedDot : _TexBlueDot;

            CStatic image = GetNewStatic(texture, new SColorF(1, 1, 1, 1), new SRectF(-1, -1, 400, 120, -4));

            SRectF particleRect = new SRectF(-1, -1, partRectSize, partRectSize, -6);
            SRectF imgDotRect = new SRectF(particleRect) {Z = -5};
            CStatic imgDot = GetNewStatic(texDot, new SColorF(1, 1, 1, 1), imgDotRect);
            CParticleEffect particle = _GetStarParticles(partCount, isRight, particleRect, bigParticles);

            CCreditName credit = new CCreditName(image, imgDot, particle, particleOffsetX, particleOffsetY);

            if (isRight)
            {
                credit.X = CSettings.RenderW;
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
            _Logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1),
                                 new SRectF((float)(CSettings.RenderW - _TexLogo.OrigSize.Width) / 2, -270, _TexLogo.OrigSize.Width, _TexLogo.OrigSize.Height, -2));

            //Little stars for logo
            int numstars = (int)(_Logo.Rect.W * 0.25f / 2f);
            SRectF partRect = new SRectF(_Logo.Rect.X, _Logo.Rect.Y, _Logo.Rect.W, _Logo.Rect.H, -1);
            _StarsRed = _GetStarParticles(numstars, true, partRect, true);
            _StarsBlue = _GetStarParticles(numstars, false, partRect, true);

            //Credit names
            _CreditNames = new List<CCreditName>();

            _AddNewCreditName(_TexNameBrunzel, 502, 29, true);
            _AddNewCreditName(_TexNameDarkice, 360, 55, true);
            _AddNewCreditName(_TexNameFlokuep, 214, 14, true);
            _AddNewCreditName(_TexNameFlamefire, 496, 46, true);
            _AddNewCreditName(_TexNameBohning, 383, 54, false);
            _AddNewCreditName(_TexNameMesand, 525, 13, false);
            _AddNewCreditName(_TexNameBabene03, 33, 26, false);
            _AddNewCreditName(_TexNamePantero, 311, 45, false);
            _AddNewCreditName(_TexNamePinky007, 113, 50, false);

            _TextTimer.Reset();
            _LogoTimer.Reset();
            _CreditsTimer.Reset();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            _LogoTimer.Start();
        }

        public override bool Draw()
        {
            base.Draw();

            //Draw background
            CDraw.DrawColor(new SColorF(0, 0.18f, 0.474f, 1), new SRectF(0, 0, CSettings.RenderW, CSettings.RenderH, 0));

            //Draw stars
            _StarsBlue.Draw();
            _StarsRed.Draw();

            _Logo.Draw();

            //Draw credit-entries
            foreach (CCreditName cn in _CreditNames)
                cn.Draw();

            //Draw Text
            if (_TextTimer.IsRunning)
            {
                foreach (CText text in _ParagraphTexts)
                    text.Draw();
            }
            return true;
        }

        private bool _Animation()
        {
            bool active = false;
            if (_LogoTimer.IsRunning)
            {
                active = true;

                _Logo.Rect.Y = -270 + (270f / 3000f) * _LogoTimer.ElapsedMilliseconds;
                _StarsRed.Rect.Y = _Logo.Rect.Y;
                _StarsBlue.Rect.Y = _Logo.Rect.Y;
                if (_LogoTimer.ElapsedMilliseconds >= 2000 && !_CreditsTimer.IsRunning)
                    _CreditsTimer.Start();
                if (_LogoTimer.ElapsedMilliseconds >= 3000)
                {
                    _LogoTimer.Stop();
                    _LogoTimer.Reset();
                }
            }

            if (_CreditsTimer.IsRunning)
            {
                active = true;

                for (int i = 0; i < _CreditNames.Count; i++)
                {
                    if (_CreditNames[i].Active)
                    {
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
                                    _CreditNames[i].Active = false;
                                    //Check, if last name is set to false
                                    if (i == _CreditNames.Count - 1)
                                    {
                                        //Stop and reset timer for next time
                                        _CreditsTimer.Stop();
                                        _CreditsTimer.Reset();

                                        //Start Text-Timer
                                        _TextTimer.Start();
                                    }
                                }
                                else if (_CreditNames[i].Y <= 360f)
                                {
                                    //Fade names out
                                    float alpha = (360 - _CreditNames[i].Y) / 200;
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

            if (_TextTimer.IsRunning)
                active = _TextTimer.ElapsedMilliseconds <= 60000;

            return active;
        }
    }
}