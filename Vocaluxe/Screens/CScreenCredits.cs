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
using System.IO;
using Vocaluxe.Base;
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

        private const float _ScrollSpeed = 60f;
        private const float _EdgeFadeHeight = 150f;
        private const float _EndHoldTime = 4f;
        private const float _EndFadeTime = 1.5f;

        private CStatic _Logo;

        private Stopwatch _ScrollTimer;

        private List<dynamic> _ScrollingElements;
        private Dictionary<dynamic, float> _ElementStartYPositions;
        private List<CText> _HeadlineTexts;
        private List<CStatic> _HeadlineLines;
        private float _previousVideoElapsedMilliseconds;
        private float _MaxScrollOffset;
        private int _NumEndElements;
        private bool _Leaving;
        private EScreen _ReturnScreen = EScreen.OptionsGame;

        private CTextureRef _TexLogo;

        private CVideoStream _BackgroundVideo;
        private float _BackgroundVideoTime;

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
            _HeadlineTexts = new List<CText>();
            _HeadlineLines = new List<CStatic>();
        }

        public override void LoadTheme(string xmlPath)
        {
            // Vocaluxe-Logo
            var ressourceOK = true;
            var path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameGraphics, CSettings.FileNameCreditsLogo);

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
                // Prevent further execution
                return;
            }

            // Position Y for the first scrolling element
            var scrollY = CSettings.RenderH - 1f;

            CStatic AddLogo()
            {
                var logo = GetNewStatic(_TexLogo, new SColorF(1, 1, 1, 1),
                    new SRectF((float)(CSettings.RenderW - _TexLogo.OrigSize.Width) / 2, scrollY, _TexLogo.OrigSize.Width, _TexLogo.OrigSize.Height, -2));
                logo.Visible = false;
                _AddStatic(logo);
                _ScrollingElements.Add(logo);
                _ElementStartYPositions[logo] = scrollY;
                return logo;
            }

            // Create logo
            if (_TexLogo != null)
            {
                _Logo = AddLogo();

                scrollY += _Logo.Rect.H + 20f; // Update scrollY after logo
            }

            // Helper variables for font sizes
            var bigHeadlineSize = 45;
            var headlineSize = 40;
            var boldSize = 35;
            var textSize = 30;
            var paragraphSpacing = 30f; // Space between paragraphs

            // Helper method to add text with formatting
            CText AddText(string content, int size, EStyle style, float yOffset, EAlignment alignment = EAlignment.Center)
            {
                var text = GetNewText(new CText(CSettings.RenderW / 2, scrollY + yOffset, -2f, size, -1, alignment, style, "Outline", new SColorF(1, 1, 1, 1), content));
                text.Visible = false;
                _ScrollingElements.Add(text);
                _AddText(text);
                _ElementStartYPositions[text] = text.Y;
                scrollY += size + yOffset;
                return text;
            }

            void AddHeadline(string content)
            {
                _HeadlineTexts.Add(AddText(content, headlineSize, EStyle.Bold, 0));
                var line = GetNewStatic(null, new SColorF(1, 1, 1, 1), new SRectF(CSettings.RenderW / 2 - 200f, scrollY + 6f, 400f, 3f, -2));
                line.Visible = false;
                _AddStatic(line);
                _ScrollingElements.Add(line);
                _ElementStartYPositions[line] = line.Y;
                _HeadlineLines.Add(line);
                scrollY += 9f;
            }

            // Intro
            AddText("Bringing people together to sing since 2011", bigHeadlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing;
            AddText("A heartfelt thank you to all the people who have contributed to making Vocaluxe a reality.", textSize, EStyle.Normal, 0);
            AddText("Your dedication, time, and open-source spirit have been incredible.", textSize, EStyle.Normal, 0);
            AddText("Keep contributing or re-join and make the world sing!", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;
            AddText("VOCALUXE Team (2011-present)", bigHeadlineSize, EStyle.Bold, 0);
            scrollY += paragraphSpacing * 2;

            // Production
            AddHeadline("PRODUCTION");
            scrollY += paragraphSpacing;
            AddText("Project Leader", boldSize, EStyle.Bold, 0);
            AddText("Florian Ostertag (2013-present)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Project Management", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-2025)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Design
            AddHeadline("DESIGN");
            scrollY += paragraphSpacing;
            AddText("Original Concept/Idea", boldSize, EStyle.Bold, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Party Mode Design", boldSize, EStyle.Bold, 0);
            AddText("Florian Ostertag (2013-present)", textSize, EStyle.Normal, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Programming/Engineering
            AddHeadline("PROGRAMMING/ENGINEERING");
            scrollY += paragraphSpacing;
            AddText("Technical Director", boldSize, EStyle.Bold, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Software Engineer", boldSize, EStyle.Bold, 0);
            AddText("Florian Ostertag (2013-present)", textSize, EStyle.Normal, 0);
            AddText("LukeIam (2013-2019)", textSize, EStyle.Normal, 0);
            AddText("Alexander Eckhart (2011-2015)", textSize, EStyle.Normal, 0);
            AddText("Alexander Grund (2013-2015)", textSize, EStyle.Normal, 0);
            AddText("Darkice", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Programmer", boldSize, EStyle.Bold, 0);
            AddText("Jiiniasu (2016-present)", textSize, EStyle.Normal, 0);
            AddText("Dirk Heinke (2025-present)", textSize, EStyle.Normal, 0);
            AddText("Edi316 (2025-present)", textSize, EStyle.Normal, 0);
            AddText("Florian Schulze (2025-present)", textSize, EStyle.Normal, 0);
            AddText("Jeff Dolan (2025-present)", textSize, EStyle.Normal, 0);
            AddText("Kacper Majorkowski (2025-present)", textSize, EStyle.Normal, 0);
            AddText("Metin Celik (2025-present)", textSize, EStyle.Normal, 0);
            AddText("friedPotat0 (2026-present)", textSize, EStyle.Normal, 0);
            AddText("McSchneidar (2026-present)", textSize, EStyle.Normal, 0);
            AddText("Quentin Bray (2026-present)", textSize, EStyle.Normal, 0);
            AddText("Franz Schuster (2025)", textSize, EStyle.Normal, 0);
            AddText("Damien Laguerre (2024)", textSize, EStyle.Normal, 0);
            AddText("GRaff844 (2024)", textSize, EStyle.Normal, 0);
            AddText("Rap-Voca (2024)", textSize, EStyle.Normal, 0);
            AddText("Stefan1200 (2020-2022)", textSize, EStyle.Normal, 0);
            AddText("GaryCXJk (2020-2021)", textSize, EStyle.Normal, 0);
            AddText("Stephan Sundermann (2012-2014)", textSize, EStyle.Normal, 0);
            AddText("Babene03 (2012)", textSize, EStyle.Normal, 0);
            AddText("Markus Bohning (2012)", textSize, EStyle.Normal, 0);
            AddText("Mesand (2012)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Art/Graphics
            AddHeadline("ART/GRAPHICS");
            scrollY += paragraphSpacing;
            AddText("UI Artist/Graphics", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-2025)", textSize, EStyle.Normal, 0);
            AddText("Jiiniasu (2016-2020)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Sound/Audio/Music Design", boldSize, EStyle.Bold, 0);
            AddText("Marwin (2023-2025)", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing;
            AddText("Audio Producer", boldSize, EStyle.Bold, 0);
            AddText("Ivymusic", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            // Translation
            AddHeadline("TRANSLATION");
            scrollY += paragraphSpacing;
            AddText("Thanks to everyone translating Vocaluxe into different languages.", textSize, EStyle.Normal, 0);
            scrollY += paragraphSpacing * 3;

            var endBlockTop = scrollY;
            var numElementsBeforeEnd = _ScrollingElements.Count;
            if (_TexLogo != null)
            {
                scrollY += AddLogo().Rect.H + 20f;
            }

            // Website
            AddText("www.vocaluxe.org", headlineSize, EStyle.Bold, 0);

            _NumEndElements = _ScrollingElements.Count - numElementsBeforeEnd;
            _MaxScrollOffset = endBlockTop - (CSettings.RenderH - (scrollY - endBlockTop)) / 2;
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
                        _LeaveScreen();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB || mouseEvent.RB)
            {
                _LeaveScreen();
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_BackgroundVideo != null)
            {
                var deltaTime = (_ScrollTimer.ElapsedMilliseconds - _previousVideoElapsedMilliseconds) / 1000f;
                _previousVideoElapsedMilliseconds = _ScrollTimer.ElapsedMilliseconds;

                _BackgroundVideoTime += deltaTime;

                var videoLength = CVideo.GetLength(_BackgroundVideo);

                // Keep time within video length to avoid seeking backward
                if (_BackgroundVideoTime >= videoLength)
                {
                    _BackgroundVideoTime -= videoLength;
                }

                CVideo.GetFrame(_BackgroundVideo, _BackgroundVideoTime);
            }

            if (!_Animation())
            {
                _LeaveScreen();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _Leaving = false;
            _ReturnScreen = CGraphics.CurrentScreen == CGraphics.GetScreen(EScreen.Main) ? EScreen.Main : EScreen.OptionsGame;

            // Reset positions
            foreach (var element in _ScrollingElements)
            {
                if (_ElementStartYPositions.TryGetValue(element, out float startY))
                {
                    element.Y = startY;
                    element.Alpha = 1f;
                }
            }

            if (CBase.Themes.GetColor("ButtonSelColor", PartyModeId, out SColorF headlineColor))
            {
                foreach (var text in _HeadlineTexts)
                {
                    text.Color = headlineColor;
                }

                foreach (var line in _HeadlineLines)
                {
                    line.Color = headlineColor;
                }
            }

            for (var i = 0; i < _HeadlineTexts.Count; i++)
            {
                var textRect = _HeadlineTexts[i].Rect;
                _HeadlineLines[i].X = textRect.X;
                _HeadlineLines[i].W = textRect.W;
            }

            _previousVideoElapsedMilliseconds = 0f;

            var path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameGraphics, CSettings.FileNameCreditsVideo);
            if (File.Exists(path))
            {
                // Load the video if not already loaded
                if (_BackgroundVideo == null)
                {
                    _BackgroundVideo = CVideo.Load(path);
                    CVideo.SetLoop(_BackgroundVideo, true); // Set the video to loop
                }
            }
            else
            {
                CLog.Error("Background video not found: " + path);
            }

            var musicPath = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameSounds, CSettings.FileNameMusicCredits);
            if (!File.Exists(musicPath) || !CBackgroundMusic.PlayEventMusic(musicPath))
            {
                CLog.Warning("Credits music not found or not playable: " + musicPath);
            }

            foreach (var element in _ScrollingElements)
            {
                element.Visible = true;
            }

            // Start the scroll timer immediately
            _ScrollTimer.Reset();
            _ScrollTimer.Start();
            _BackgroundVideoTime = 0f;
        }

        public override void OnClose()
        {
            base.OnClose();
            CBackgroundMusic.StopEventMusic();
        }

        public override void Draw()
        {
            if (_Active && _BackgroundVideo != null && _BackgroundVideo.Texture != null)
            {
                var background = _BackgroundVideo.Texture;
                var bounds = CSettings.RenderRect;
                var rect = CHelper.FitInBounds(bounds, background.OrigAspect, EAspect.Crop);
                CDraw.DrawTexture(background, rect, background.Color, bounds);
            }

            base.Draw();
        }

        private void _LeaveScreen()
        {
            if (_Leaving)
            {
                return;
            }

            _Leaving = true;
            CBackgroundMusic.StopEventMusic();
            CGraphics.FadeTo(_ReturnScreen);

            if (_BackgroundVideo != null)
            {
                CVideo.Close(ref _BackgroundVideo);
                _BackgroundVideo = null;
            }
        }

        private bool _Animation()
        {
            if (!_ScrollTimer.IsRunning)
            {
                return false;
            }

            var elapsed = _ScrollTimer.ElapsedMilliseconds / 1000f; // Convert milliseconds to seconds
            var offset = elapsed * _ScrollSpeed;
            var endOffset = Math.Min(offset, _MaxScrollOffset);
            var endAlpha = 1f;

            if (endOffset >= _MaxScrollOffset)
            {
                var endTime = elapsed - _MaxScrollOffset / _ScrollSpeed;
                if (endTime >= _EndHoldTime + _EndFadeTime)
                {
                    _ScrollTimer.Stop();
                    return false;
                }

                if (endTime > _EndHoldTime)
                {
                    endAlpha = 1f - (endTime - _EndHoldTime) / _EndFadeTime;
                }
            }

            for (var i = 0; i < _ScrollingElements.Count; i++)
            {
                var element = _ScrollingElements[i];
                if (_ElementStartYPositions.TryGetValue(element, out float startY))
                {
                    var isEndElement = i >= _ScrollingElements.Count - _NumEndElements;
                    element.Y = startY - (isEndElement ? endOffset : offset);
                    SRectF rect = element.Rect;
                    var edgeDistance = Math.Min(rect.Y, CSettings.RenderH - rect.Y - rect.H);
                    element.Alpha = Math.Max(0f, Math.Min(1f, edgeDistance / _EdgeFadeHeight)) * (isEndElement ? endAlpha : 1f);
                }
            }

            return true;
        }
    }
}
