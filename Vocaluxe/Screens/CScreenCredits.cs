﻿using System;
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
       
        public CScreenCredits()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenCredits";
            _ScreenVersion = ScreenVersion;

            LogoTimer = new Stopwatch();
            CreditsTimer = new Stopwatch();
        }

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
            STexture tex = new STexture();
            STexture particleTex = new STexture();

            //Vocaluxe-Logo
            CDataBase.GetCreditsRessource("Logo_voc.png", ref tex);
            logo = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF((CSettings.iRenderW - tex.width)/2, -270 , tex.width, tex.height, -2));

            //Little stars for logo
            CDataBase.GetCreditsRessource("PerfectNoteStar.png", ref particleTex);
            int numstars = (int)(logo.Rect.W * 0.25f /2f);
            starsRed = new CParticleEffect(numstars, new SColorF(1, 0, 0, 1), new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1), particleTex, 35, EParticeType.Star);
            starsBlue = new CParticleEffect(numstars, new SColorF(0.149f, 0.415f, 0.819f, 1), new SRectF(logo.Rect.X, logo.Rect.Y, logo.Rect.W, logo.Rect.H, -1), particleTex, 35, EParticeType.Star);

            //Credit names
            _CreditNames = new List<CCreditName>();

            CCreditName CreditEntry = new CCreditName();

            //brunzel
            CDataBase.GetCreditsRessource("brunzel.png", ref tex);
            CCreditName CreditEntryBrunzel = new CCreditName();
            CreditEntryBrunzel.image = new CStatic(tex, new SColorF(1,1,1,1), new SRectF(-450, 580, 400, 120, -4));
            CreditEntryBrunzel.particleRect = new SRectF(CreditEntryBrunzel.image.Rect.X + 342, CreditEntryBrunzel.image.Rect.Y + 4, 30, 30, -5);
            CreditEntryBrunzel.particle = new CParticleEffect(8, new SColorF(1, 0, 0, 1), CreditEntryBrunzel.particleRect, particleTex, 35, EParticeType.Star);
            CreditEntryBrunzel.active = true;
            CreditEntryBrunzel.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryBrunzel);

            //Darkice
            CDataBase.GetCreditsRessource("Darkice.png", ref tex);
            CCreditName CreditEntryDarkice = new CCreditName();
            CreditEntryDarkice.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 400, 120, -4));
            CreditEntryDarkice.particleRect = new SRectF(CreditEntryDarkice.image.Rect.X + 242, CreditEntryDarkice.image.Rect.Y + 23, 30, 30, -5);
            CreditEntryDarkice.particle = new CParticleEffect(8, new SColorF(0.149f, 0.415f ,0.819f, 1), CreditEntryDarkice.particleRect, particleTex, 35, EParticeType.Star);
            CreditEntryDarkice.active = true;
            CreditEntryDarkice.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryDarkice);

            //flokuep
            CDataBase.GetCreditsRessource("flokuep.png", ref tex);
            CCreditName CreditEntryFlokuep = new CCreditName();
            CreditEntryFlokuep.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 400, 120, -4));
            CreditEntryFlokuep.particleRect = new SRectF(CreditEntryFlokuep.image.Rect.X + 141, CreditEntryFlokuep.image.Rect.Y-2, 30, 30, -5);
            CreditEntryFlokuep.particle = new CParticleEffect(8, new SColorF(1, 0, 0, 1), CreditEntryFlokuep.particleRect, particleTex, 35, EParticeType.Star);
            CreditEntryFlokuep.active = true;
            CreditEntryFlokuep.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryFlokuep);

            //bohning
            CDataBase.GetCreditsRessource("bohning.png", ref tex);
            CCreditName CreditEntryBohning = new CCreditName();
            CreditEntryBohning.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryBohning.particleRect = new SRectF(CreditEntryBohning.image.Rect.X + 172, CreditEntryBohning.image.Rect.Y + 16, 10, 10, -5);
            CreditEntryBohning.particle = new CParticleEffect(4, new SColorF(0.149f, 0.415f ,0.819f, 1), CreditEntryBohning.particleRect, particleTex, 25, EParticeType.Star);
            CreditEntryBohning.active = true;
            CreditEntryBohning.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryBohning);

            //mesand
            CDataBase.GetCreditsRessource("mesand.png", ref tex);
            CCreditName CreditEntryMesand = new CCreditName();
            CreditEntryMesand.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 350, 110, -4));
            CreditEntryMesand.particleRect = new SRectF(CreditEntryMesand.image.Rect.X + 240, CreditEntryMesand.image.Rect.Y - 2, 10, 10, -5);
            CreditEntryMesand.particle = new CParticleEffect(4, new SColorF(1, 0, 0, 1), CreditEntryMesand.particleRect, particleTex, 25, EParticeType.Star);
            CreditEntryMesand.active = true;
            CreditEntryMesand.direction = EDirection.Left;
            _CreditNames.Add(CreditEntryMesand);

            //babene03
            CDataBase.GetCreditsRessource("babene03.png", ref tex);
            CCreditName CreditEntryBabene03 = new CCreditName();
            CreditEntryBabene03.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryBabene03.particleRect = new SRectF(CreditEntryBabene03.image.Rect.X + 7, CreditEntryBabene03.image.Rect.Y + 4, 10, 10, -5);
            CreditEntryBabene03.particle = new CParticleEffect(4, new SColorF(0.149f, 0.415f ,0.819f, 1), CreditEntryBabene03.particleRect, particleTex, 25, EParticeType.Star);
            CreditEntryBabene03.active = true;
            CreditEntryBabene03.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryBabene03);

            //pantero
            CDataBase.GetCreditsRessource("pantero.png", ref tex);
            CCreditName CreditEntrypantero = new CCreditName();
            CreditEntrypantero.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(-450, 580, 350, 110, -4));
            CreditEntrypantero.particleRect = new SRectF(CreditEntrypantero.image.Rect.X + 140, CreditEntrypantero.image.Rect.Y + 15, 10, 10, -5);
            CreditEntrypantero.particle = new CParticleEffect(4, new SColorF(1, 0, 0, 1), CreditEntrypantero.particleRect, particleTex, 25, EParticeType.Star);
            CreditEntrypantero.active = true;
            CreditEntrypantero.direction = EDirection.Left;
            _CreditNames.Add(CreditEntrypantero);
       
            //Pinky007
            CDataBase.GetCreditsRessource("Pinky007.png", ref tex);
            CCreditName CreditEntryPinky007 = new CCreditName();
            CreditEntryPinky007.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(CSettings.iRenderW, 580, 350, 110, -4));
            CreditEntryPinky007.particleRect = new SRectF(CreditEntryPinky007.image.Rect.X + 42, CreditEntryPinky007.image.Rect.Y + 15, 10, 10, -5);
            CreditEntryPinky007.particle = new CParticleEffect(4, new SColorF(0.149f, 0.415f ,0.819f, 1), CreditEntryPinky007.particleRect, particleTex, 25, EParticeType.Star);
            CreditEntryPinky007.active = true;
            CreditEntryPinky007.direction = EDirection.Right;
            _CreditNames.Add(CreditEntryPinky007);


            //CreditEntry.image = new CStatic(tex, new SColorF(1, 1, 1, 1), new SRectF(0, 0, 350, 110, -4));

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
            starsBlue.Update();
            starsBlue.Draw();
            starsRed.Update();
            starsRed.Draw();

            logo.Draw();

            //Draw credit-entries
            for (int i = 0; i < _CreditNames.Count; i++)
            {
                if (_CreditNames[i].active)
                {
                    _CreditNames[i].image.Draw();
                    _CreditNames[i].particle.Update();
                    _CreditNames[i].particle.Draw();
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
                starsRed.Area = rect;
                starsBlue.Area = rect;
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
                                    _CreditNames[i].particle.Area = _CreditNames[i].particleRect;

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
                                    _CreditNames[i].particle.Area = _CreditNames[i].particleRect;

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
                                _CreditNames[i].particle.Area = _CreditNames[i].particleRect;

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
                                        //Will later fade to Main-Screen
                                        active = false;

                                        //Stop and reset timer for next time
                                        CreditsTimer.Stop();
                                        CreditsTimer.Reset();
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return active;
        }
    }
}
