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
using System.Drawing;
using System.IO;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenCredits : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private CStatic _Logo;
        
        private Stopwatch _ScrollTimer;

        private List<dynamic> _ScrollingElements;
        private Dictionary<dynamic, float> _ElementStartYPositions;
        private float _previousElapsedMilliseconds;

        private CTextureRef _TexLogo;

        
        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Background; }
        }

        public override void Init()
        {
            base.Init();

            _ScrollTimer = new Stopwatch();
            _ScrollingElements = new List<dynamic>();
            _ElementStartYPositions = new Dictionary<dynamic, float>();
        }

        public override void LoadTheme(string xmlPath)
        {
            // Vocaluxe-Logo
            bool ressourceOK = true;
            string path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameGraphics, CSettings.FileNameCreditsLogo);

            if (File.Exists(path))
            {
                 _TexLogo = CDraw.AddTexture(path);
                 ressourceOK &= _TexLogo != null;
            }
            else
            {
                ressourceOK = false;
            }
            if (!ressourceOK)
            {
                 CLog.Fatal("Could not load all resources!");
            }

            // Initialize background theme
            SThemeBackground _BGTheme = new SThemeBackground
            {
                Type = EBackgroundTypes.Color,
                Color = new SThemeColor { Name = "Black", R = 0f, G = 0f, B = 0f, A = 1f }
            };

            // Create and add background
            CBackground bg = new CBackground(_BGTheme, -1);
            bg.LoadSkin();
            _AddBackground(bg);

            // Position Y for the first scrolling element
            float scrollY = CSettings.RenderH - 1f;

            // Create logo
            _Logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1),
                new SRectF((float)(CSettings.RenderW - _TexLogo.OrigSize.Width) / 2, scrollY, _TexLogo.OrigSize.Width, _TexLogo.OrigSize.Height, -2));
            _Logo.Visible = false;
            _AddStatic(_Logo);
            _ScrollingElements.Add(_Logo);
            _ElementStartYPositions[_Logo] = scrollY;

            scrollY += _Logo.Rect.H + 20f; // Update scrollY after logo

            // Helper variables for font sizes
            int bigHeadlineSize = 45;
            int headlineSize = 40;
            int boldSize = 35;
            int textSize = 30;
            float paragraphSpacing = 30f; // Space between paragraphs

            // Helper method to add text with formatting
            void AddText(string content, int size, EStyle style, float yOffset, EAlignment alignment = EAlignment.Center)
            {
                CText text = GetNewText(new CText(CSettings.RenderW / 2, scrollY + yOffset, -2f, size, -1, alignment, style, "Outline", new SColorF(1, 1, 1, 1), content));
                text.Visible = false;
                _ScrollingElements.Add(text);
                _AddText(text);
                _ElementStartYPositions[text] = text.Y;
                scrollY += size + yOffset;
            }

            // Intro
            AddText("12 Years Vocaluxe!", bigHeadlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("A heartfelt thank you to all the people who have contributed to making Vocaluxe a reality over the past 12 years.", textSize, EStyle.Normal, 0);
            AddText("Your dedication, time, and open-source spirit have been incredible. Keep contributing or re-join and make the world sing!", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;
            AddText("VOCALUXE Team (2011-today)", bigHeadlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing * 2;

            // Production
            AddText("PRODUCTION", headlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("Project Leader", boldSize, EStyle.Bold, 0);
            AddText("Florian Ostertag (2013-today)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Project Management", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-today)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Design
            AddText("DESIGN", headlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("Original Concept/Idea", boldSize, EStyle.Bold, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Party Mode Design", boldSize, EStyle.Bold, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            AddText("Florian Ostertag (2013-today)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Programming/Engineering
            AddText("PROGRAMMING/ENGINEERING", headlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("Technical Director", boldSize, EStyle.Bold, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Software Engineer", boldSize, EStyle.Bold, 0);
            AddText("Florian Ostertag (2013-today)", textSize, EStyle.Normal, 0);
            AddText("LukeIam (2013-2019)", textSize, EStyle.Normal, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            AddText("Alexander Grund (2013-2015)", textSize, EStyle.Normal, 0);
            AddText("Darkice", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Programmer", boldSize, EStyle.Bold, 0);
            AddText("Stefan1200 (2020-2022)", textSize, EStyle.Normal, 0);
            AddText("Stephan Sundermann (2012-2014)", textSize, EStyle.Normal, 0);
            AddText("GaryCXJk (2020-2021)", textSize, EStyle.Normal, 0);
            AddText("Damien Laguerre (2024-today)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Art/Graphics
            AddText("ART/GRAPHICS", headlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("UI Artist/Graphics", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-today)", textSize, EStyle.Normal, 0);
            AddText("Jiiniasu (2016-2020)", textSize, EStyle.Normal, 0);
            AddText("Markus Bohning (2012)", textSize, EStyle.Normal, 0);
            AddText("Mesand (2012)", textSize, EStyle.Normal, 0);
            AddText("Babene03 (2012)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Sound/Audio/Music Design", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-today)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Audio Producer", boldSize, EStyle.Bold, 0);
            AddText("Ivymusic", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Translation
            AddText("TRANSLATION", headlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("Thanks to everyone translating Vocaluxe into different languages.", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 4;
            
            // Website
            AddText("www.vocaluxe.org", headlineSize, EStyle.Bold, 0);
            AddText("www.open-music-games.org", headlineSize, EStyle.Bold, 0);
        }

        public override void ReloadTheme(string xmlPath) { }

        public override void SaveTheme() { }

        public override void UnloadSkin() { }

        public override void ReloadSkin() { }

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

        public override void OnShow()
        {
            base.OnShow();

            // Reset positions
            foreach (dynamic element in _ScrollingElements)
            {
                if (_ElementStartYPositions.TryGetValue(element, out float startY))
                {
                    if (element is CText text)
                    {
                        text.Y = startY;
                    }
                    else if (element == _Logo)
                    {
                        _Logo.Y = startY;
                    }
                }
            }

            // Set all elements to visible immediately
            foreach (dynamic element in _ScrollingElements)
            {
                element.Visible = true;
            }

            // Start the scroll timer immediately
            _ScrollTimer.Reset();
            _ScrollTimer.Start();
            _previousElapsedMilliseconds = 0;
        }

        
        private bool _Animation()
        {
            if (!_ScrollTimer.IsRunning)
                return false;

            float deltaTime = (_ScrollTimer.ElapsedMilliseconds - _previousElapsedMilliseconds) / 1000f; // Convert milliseconds to seconds
            _previousElapsedMilliseconds = _ScrollTimer.ElapsedMilliseconds;

            float scrollSpeed = 60f;

            foreach (dynamic element in _ScrollingElements)
            {
                if (element is CText text)
                {
                    text.Y -= scrollSpeed * deltaTime;
                }
                else if (element == _Logo)
                {
                    _Logo.Y -= scrollSpeed * deltaTime;
                }
            }

            // Check if the last element has scrolled off the top
            dynamic lastElement = _ScrollingElements[_ScrollingElements.Count - 1];
            float elementBottomY = 0;
            if (lastElement is CText lastText)
            {
                elementBottomY = lastText.Y + lastText.H;
            }
            else if (lastElement == _Logo)
            {
                elementBottomY = _Logo.Y + _Logo.Rect.H;
            }

            if (elementBottomY < 0)
            {
                _ScrollTimer.Stop();
                return false;
            }

            return true;
        }
    }
}
