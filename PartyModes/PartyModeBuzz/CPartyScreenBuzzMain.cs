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
using System.Windows.Forms;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.Buzz
{
    enum EStage
    {
        Preview,
        Vote,
        Song
    }
    public class CPartyScreenBuzzMain : CPartyScreenChallenge
    {
        private const string _TextNextPlayer = "TextNextPlayer";
        private const string _TextNextSongArtist = "TextNextSongArtist";
        private const string _TextNextSongTitle = "TextNextSongTitle";
        private const string _StaticNextPlayer = "StaticNextPlayer";
        private const string _StaticNextSongCover = "StaticNextSongCover";
        private const string _StaticPlayPreviewIcon = "StaticPlayPreviewIcon";
        private List<CText> _NextPlayerTexts;
        private List<CText> _NextSongArtistTexts;
        private List<CText> _NextSongTitleTexts;
        private List<CStatic> _NextPlayerStatics;
        private List<CStatic> _NextSongCoverStatics;

        private EStage _CurrentStage;
        private int _CurrentPreview;
        private int[] _Vote;
        private int[] _NextSong;
        private bool _PlayPreview;
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        public override bool UpdateGame()
        {
            //Little Hack to start the first Preview after Screenfading
            if (_PlayPreview)
            {
                _Vote = new int[_PartyMode.GameData.NumPlayerAtOnce];
                _StartPreview(0);
                _PlayPreview = false; 
            }
            return true;
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _NextPlayerTexts = new List<CText>();
            _NextSongArtistTexts = new List<CText>();
            _NextSongTitleTexts = new List<CText>();
            _NextPlayerStatics = new List<CStatic>();
            _NextSongCoverStatics = new List<CStatic>();

            for (int i = 0; i < _PartyMode.MaxPlayers; i++)
            {
                _NextPlayerTexts.Add(GetNewText(_Texts[_TextNextPlayer]));
                _AddText(_NextPlayerTexts[_NextPlayerTexts.Count - 1]);
                _NextPlayerStatics.Add(GetNewStatic(_Statics[_StaticNextPlayer]));
                _AddStatic(_NextPlayerStatics[_NextPlayerStatics.Count - 1]);
            }
            _Statics[_StaticNextPlayer].Visible = false;

            for (int i = 0; i < 4; i++)
            {
                _NextSongArtistTexts.Add(GetNewText(_Texts[_TextNextSongArtist]));
                _AddText(_NextSongArtistTexts[_NextSongArtistTexts.Count - 1]);
                _NextSongTitleTexts.Add(GetNewText(_Texts[_TextNextSongTitle]));
                _AddText(_NextSongTitleTexts[_NextSongTitleTexts.Count - 1]);
                _NextSongCoverStatics.Add(GetNewStatic(_Statics[_StaticNextSongCover]));
                _AddStatic(_NextSongCoverStatics[_NextSongCoverStatics.Count - 1]);
            }
            _Statics[_StaticNextSongCover].Visible = false;
            _NextSongArtistTexts[0].Color = new SColorF(0, (float)38 / 255, 1, 1);
            _NextSongTitleTexts[0].Color = _NextSongArtistTexts[0].Color;
            _NextSongArtistTexts[1].Color = new SColorF(1, (float)106 / 255, 0, 1);
            _NextSongTitleTexts[1].Color = _NextSongArtistTexts[1].Color;
            _NextSongArtistTexts[2].Color = new SColorF(0, 1, (float)33 / 255, 1);
            _NextSongTitleTexts[2].Color = _NextSongArtistTexts[2].Color;
            _NextSongArtistTexts[3].Color = new SColorF(1, (float)216 / 255, 0, 1);
            _NextSongTitleTexts[3].Color = _NextSongArtistTexts[3].Color;

        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (!keyEvent.KeyPressed)
            {
                if (_CurrentStage == EStage.Preview)
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.D1:
                            _Vote[0] = 1;
                            break;
                        case Keys.Q:
                            if (_Vote.Length > 1) _Vote[1] = 1;
                            break;
                        case Keys.A:
                            if (_Vote.Length > 2) _Vote[2] = 1;
                            break;
                        case Keys.Y:
                            if (_Vote.Length > 3) _Vote[3] = 1;
                            break;
                    }
                    bool end = true;
                    foreach (int i in _Vote)
                    {
                        if (i == 0)
                            end = false;
                    }
                    if (end)
                    {
                        if (_CurrentPreview < _PartyMode.GameData.NumChoices - 1)
                            _StartPreview(++_CurrentPreview);
                        else
                        {
                            _StartVote();
                        } 
                    }
                }
                else if (_CurrentStage == EStage.Vote)
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.D2:
                            if (_Vote[0] == 0) _Vote[0] = 1;
                            break;
                        case Keys.D3:
                            if (_Vote[0] == 0) _Vote[0] = 2;
                            break;
                        case Keys.D4:
                            if ((_NextSong.Length > 2) && (_Vote[0] == 0)) _Vote[0] = 3;
                            break;
                        case Keys.D5:
                            if ((_NextSong.Length > 3) && (_Vote[0] == 0)) _Vote[0] = 4;
                            break;
                        case Keys.W:
                            if ((_Vote.Length > 1) && (_Vote[1] == 0)) _Vote[1] = 1;
                            break;
                        case Keys.E:
                            if ((_Vote.Length > 1) && (_Vote[1] == 0)) _Vote[1] = 2;
                            break;
                        case Keys.R:
                            if ((_NextSong.Length > 2) && (_Vote.Length > 1) && (_Vote[1] == 0)) _Vote[1] = 3;
                            break;
                        case Keys.T:
                            if ((_NextSong.Length > 3) && (_Vote.Length > 1) && (_Vote[1] == 0)) _Vote[1] = 4;
                            break;
                        case Keys.S:
                            if ((_Vote.Length > 2) && (_Vote[2] == 0)) _Vote[2] = 1;
                            break;
                        case Keys.D:
                            if ((_Vote.Length > 2) && (_Vote[2] == 0)) _Vote[2] = 2;
                            break;
                        case Keys.F:
                            if ((_NextSong.Length > 2) && (_Vote.Length > 2) && (_Vote[2] == 0)) _Vote[2] = 3;
                            break;
                        case Keys.G:
                            if ((_NextSong.Length > 3) && (_Vote.Length > 2) && (_Vote[2] == 0)) _Vote[2] = 4;
                            break;
                        case Keys.X:
                            if ((_Vote.Length > 3) && (_Vote[3] == 0)) _Vote[3] = 1;
                            break;
                        case Keys.C:
                            if ((_Vote.Length > 3) && (_Vote[3] == 0)) _Vote[3] = 2;
                            break;
                        case Keys.V:
                            if ((_NextSong.Length > 2) && (_Vote.Length > 3) && (_Vote[3] == 0)) _Vote[3] = 3;
                            break;
                        case Keys.B:
                            if ((_NextSong.Length > 3) && (_Vote.Length > 3) && (_Vote[3] == 0)) _Vote[3] = 4;
                            break;
                    }
                    bool end = true;
                    foreach (int i in _Vote)
                    {
                        if (i == 0)
                            end = false;
                    }
                    if (end)
                    {
                        _EndVote();
                    }
                }

                if ((_CurrentStage == EStage.Preview) || (_CurrentStage == EStage.Vote))
                {
                    switch (_Vote.Length)
                    {
                        case 4:
                            CBase.Controller.SetLEDs((_Vote[0] == 0), (_Vote[1] == 0), (_Vote[2] == 0), (_Vote[3] == 0));
                            break;
                        case 3:
                            CBase.Controller.SetLEDs((_Vote[0] == 0), (_Vote[1] == 0), (_Vote[2] == 0), false);
                            break;
                        case 2:
                            CBase.Controller.SetLEDs((_Vote[0] == 0), (_Vote[1] == 0), false, false);
                            break;
                        case 1:
                            CBase.Controller.SetLEDs((_Vote[0] == 0), false, false, false);
                            break;
                    }
                }
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _Statics[_StaticPlayPreviewIcon].Visible = false;
            _UpdateNextPlayerPositions();
            _UpdateNextPlayerContents();
            _UpdateNextSongPositions();
            _UpdateNextSongContents();
            if (_PartyMode.GameData.Preview)
            {
                _CurrentStage = EStage.Preview;
                _PlayPreview = true;
            }
            else
            {
                _StartVote();
            }
        }

        private void _UpdateNextPlayerPositions()
        {
            float x = (float)CBase.Settings.GetRenderW() / 2 -
                      ((_PartyMode.GameData.NumPlayerAtOnce * _Statics[_StaticNextPlayer].Rect.W) + ((_PartyMode.GameData.NumPlayerAtOnce - 1) * 15)) / 2;
            const float staticY = 590;
            const float textY = 550;
            for (int i = 0; i < _PartyMode.GameData.NumPlayerAtOnce; i++)
            {
                //static
                _NextPlayerStatics[i].X = x;
                _NextPlayerStatics[i].Y = staticY;
                _NextPlayerStatics[i].Visible = true;
                //text
                _NextPlayerTexts[i].X = x + _Statics[_StaticNextPlayer].Rect.W / 2;
                _NextPlayerTexts[i].Y = textY;
                _NextPlayerTexts[i].Visible = true;

                x += _Statics[_StaticNextPlayer].Rect.W + 15;
            }
            for (int i = _PartyMode.GameData.NumPlayerAtOnce; i < _PartyMode.MaxPlayers; i++)
            {
                _NextPlayerStatics[i].Visible = false;
                _NextPlayerTexts[i].Visible = false;
            }
        }

        private void _UpdateNextPlayerContents()
        {
            //_Texts[_TextNextPlayerMessage].Visible = true;
            for (int i = 0; i < _PartyMode.GameData.NumPlayerAtOnce; i++)
            {
                int id = _PartyMode.GameData.ProfileIDs[_PartyMode.GameData.Rounds[_PartyMode.GameData.CurrentRoundNr - 1].Players[i]];
                _NextPlayerStatics[i].Texture = CBase.Profiles.GetAvatar(id);
                _NextPlayerTexts[i].Text = CBase.Profiles.GetPlayerName(id);
                _NextPlayerTexts[i].Color = CBase.Themes.GetPlayerColor(i + 1);
            }
        }

        private void _UpdateNextSongPositions()
        {
            float x = (float)CBase.Settings.GetRenderW() / 2 -
                      ((_PartyMode.GameData.NumChoices * _Statics[_StaticNextSongCover].Rect.W) + ((_PartyMode.GameData.NumChoices - 1) * 15)) / 2;
            const float staticY = 200;
            const float artistY = 420;
            const float titleY = 460;
            _Statics[_StaticPlayPreviewIcon].Y = staticY + _Statics[_StaticNextSongCover].Rect.W / 4;
            for (int i = 0; i < _PartyMode.GameData.NumChoices; i++)
            {
                //static
                _NextSongCoverStatics[i].X = x;
                _NextSongCoverStatics[i].Y = staticY;
                _NextSongCoverStatics[i].Visible = true;
                //text
                _NextSongArtistTexts[i].X = x + _Statics[_StaticNextSongCover].Rect.W / 2;
                _NextSongArtistTexts[i].Y = artistY;
                _NextSongArtistTexts[i].Visible = true;
                _NextSongTitleTexts[i].X = x + _Statics[_StaticNextSongCover].Rect.W / 2;
                _NextSongTitleTexts[i].Y = titleY;
                _NextSongTitleTexts[i].Visible = true;

                x += _Statics[_StaticNextSongCover].Rect.W + 15;
            }
            for (int i = _PartyMode.GameData.NumChoices; i < 4; i++)
            {
                _NextSongCoverStatics[i].Visible = false;
                _NextSongTitleTexts[i].Visible = false;
                _NextSongArtistTexts[i].Visible = false;
            }
        }

        private void _UpdateNextSongContents()
        {
            _NextSong = new int[_PartyMode.GameData.NumChoices];
            for (int i = 0; i < _NextSong.Length; i++)
            {
                _PartyMode.UpdateSongList();
                _NextSong[i] = _PartyMode.GameData.Songs[0];
                _PartyMode.GameData.Songs.RemoveAt(0);
                CSong song = CBase.Songs.GetSongByID(_NextSong[i]);
                _NextSongCoverStatics[i].Texture = song.CoverTextureBig;
                _NextSongArtistTexts[i].Text = song.Artist;
                _NextSongTitleTexts[i].Text = song.Title;
            }
        }

        private void _StartPreview(int songNr)
        {
            _CurrentPreview = songNr;
            CSong song = CBase.Songs.GetSongByID(_NextSong[songNr]);
            _Statics[_StaticPlayPreviewIcon].X = _NextSongCoverStatics[songNr].X + _NextSongCoverStatics[songNr].W / 4;
            _Statics[_StaticPlayPreviewIcon].Color = _NextSongArtistTexts[songNr].Color;
            _Statics[_StaticPlayPreviewIcon].Visible = true;
            CBase.BackgroundMusic.LoadPreview(song);
            _Vote = new int[_PartyMode.GameData.NumPlayerAtOnce];
            CBase.Controller.SetLEDs(true, (_Vote.Length > 1), (_Vote.Length > 2), (_Vote.Length > 3));
        }

        private void _StartVote()
        {
            _CurrentStage = EStage.Vote;
            _Statics[_StaticPlayPreviewIcon].Visible = false;
            CBase.BackgroundMusic.StopPreview();
            _Vote = new int[_PartyMode.GameData.NumPlayerAtOnce];
            CBase.Controller.SetLEDs(true, (_Vote.Length > 1), (_Vote.Length > 2), (_Vote.Length > 3));
        }
        
        private void _EndVote()
        {
            _CurrentStage = EStage.Song;
            int[] result = new int[_PartyMode.GameData.NumChoices];
            CBase.Controller.SetLEDs(false, false, false, false);
            foreach (int i in _Vote)
            {
                result[i - 1]++;
            }
            List<int> winner = new List<int>();
            for (int i = 0; i < result.Length; i++)
            {
                if (winner.Count > 0)
                {
                    if (result[winner[0]] < result[i])
                    {
                        winner.Clear();
                        winner.Add(i);
                    }
                    else if (result[winner[0]] == result[i])
                    {
                        winner.Add(i);
                    }
                }
                else
                    winner.Add(i);
            }
            int nextSongId;
            if (winner.Count == 1)
            {
                nextSongId = _NextSong[winner[0]];
            }
            else
            {
                Random r = new Random();
                nextSongId = _NextSong[winner[r.Next(0, winner.Count - 1)]];
            }
            CSong song = CBase.Songs.GetSongByID(nextSongId);
            CBase.BackgroundMusic.LoadPreview(song);
            _PartyMode.GameData.Rounds[_PartyMode.GameData.CurrentRoundNr].SongID = nextSongId;
            _PartyMode.Next();
        }
    }
}
