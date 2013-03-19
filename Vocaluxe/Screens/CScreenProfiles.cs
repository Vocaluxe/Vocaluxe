using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using System.Drawing;
using System.IO;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Screens
{
    enum EEditMode
    {
        None,
        PlayerName
    }

    class CScreenProfiles : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion { get { return 2; } }

        private const string SelectSlideProfiles = "SelectSlideProfiles";
        private const string SelectSlideDifficulty = "SelectSlideDifficulty";
        private const string SelectSlideAvatars = "SelectSlideAvatars";
        private const string SelectSlideGuestProfile = "SelectSlideGuestProfile";
        private const string SelectSlideActive = "SelectSlideActive";
        private const string ButtonPlayerName = "ButtonPlayerName";
        private const string ButtonExit = "ButtonExit";
        private const string ButtonSave = "ButtonSave";
        private const string ButtonNew = "ButtonNew";
        private const string ButtonDelete = "ButtonDelete";
        private const string ButtonWebcam = "ButtonWebcam";
        private const string ButtonSaveSnapshot = "ButtonSaveSnapshot";
        private const string ButtonDiscardSnapshot = "ButtonDiscardSnapshot";
        private const string ButtonTakeSnapshot = "ButtonTakeSnapshot";

        private const string StaticAvatar = "StaticAvatar";

        private EEditMode _EditMode;

        private STexture _WebcamTexture = new STexture(-1);
        private Bitmap _Snapshot = null;
        
        public CScreenProfiles()
        {
        }

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { ButtonPlayerName, ButtonExit, ButtonSave, ButtonNew, ButtonDelete, ButtonWebcam, ButtonSaveSnapshot, ButtonDiscardSnapshot, ButtonTakeSnapshot };
            _ThemeSelectSlides = new string[] { SelectSlideProfiles, SelectSlideDifficulty, SelectSlideAvatars, SelectSlideGuestProfile, SelectSlideActive };
            _ThemeStatics = new string[] { StaticAvatar };

            _EditMode = EEditMode.None;
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            Buttons[ButtonSaveSnapshot].Visible = false;
            Buttons[ButtonDiscardSnapshot].Visible = false;
            Buttons[ButtonTakeSnapshot].Visible = false;
            if (CWebcam.GetDevices().Length > 0)
                Buttons[ButtonWebcam].Visible = true;
            else
                Buttons[ButtonWebcam].Visible = false;
            SelectSlides[SelectSlideDifficulty].SetValues<EGameDifficulty>(0);
            SelectSlides[SelectSlideGuestProfile].SetValues<EOffOn>(0);
            SelectSlides[SelectSlideActive].SetValues<EOffOn>(0);
            
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {
                switch (_EditMode)
                {
                    case EEditMode.None:
                        break;
                    case EEditMode.PlayerName:
                        SelectSlides[SelectSlideProfiles].RenameValue(
                            CProfiles.AddGetPlayerName(SelectSlides[SelectSlideProfiles].Selection, KeyEvent.Unicode));
                        break;
                }
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;
                    
                    case Keys.Enter:
                        if (Buttons[ButtonExit].Selected)
                        {
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        } else if (Buttons[ButtonSave].Selected)
                        {
                            SaveProfiles();
                        } else if (Buttons[ButtonNew].Selected)
                        {
                            NewProfile();
                        } else if (Buttons[ButtonPlayerName].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        } else if (Buttons[ButtonDelete].Selected)
                        {
                            DeleteProfile();
                        } else if (Buttons[ButtonWebcam].Selected && CWebcam.GetDevices().Length > 0)
                        {
                            OnWebcam();
                        } else if (Buttons[ButtonSaveSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                        {
                            OnSaveSnapshot();
                        } else if (Buttons[ButtonDiscardSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                        {
                            OnDiscardSnapshot();
                        } else if (Buttons[ButtonTakeSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                        {
                            OnTakeSnapshot();
                        }
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            SelectSlides[SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(SelectSlides[SelectSlideProfiles].Selection));
                        }
                        else
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Delete:
                        DeleteProfile();
                        break;
                }
                if (SelectSlides[SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[SelectSlideProfiles].Selection,
                        (EGameDifficulty)SelectSlides[SelectSlideDifficulty].Selection);
                } else if (SelectSlides[SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[SelectSlideProfiles].Selection,
                        SelectSlides[SelectSlideAvatars].Selection);
                } else if (SelectSlides[SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[SelectSlideProfiles].Selection,
                        (EOffOn)SelectSlides[SelectSlideGuestProfile].Selection);
                } else if (SelectSlides[SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(SelectSlides[SelectSlideProfiles].Selection,
                        (EOffOn)SelectSlides[SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[ButtonExit].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenMain);
                } else if (Buttons[ButtonSave].Selected)
                {
                    SaveProfiles();
                } else if (Buttons[ButtonNew].Selected)
                {
                    NewProfile();
                } else if (Buttons[ButtonDelete].Selected) {
                    DeleteProfile();
                } else if (Buttons[ButtonPlayerName].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                } else if (SelectSlides[SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[SelectSlideProfiles].Selection,
                        (EGameDifficulty)SelectSlides[SelectSlideDifficulty].Selection);
                } else if (SelectSlides[SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[SelectSlideProfiles].Selection,
                        SelectSlides[SelectSlideAvatars].Selection);
                    if (CWebcam.GetDevices().Length > 0 && _WebcamTexture.index > 0)
                        OnDiscardSnapshot();
                } else if (SelectSlides[SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[SelectSlideProfiles].Selection,
                        (EOffOn)SelectSlides[SelectSlideGuestProfile].Selection);
                } else if (SelectSlides[SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(SelectSlides[SelectSlideProfiles].Selection,
                        (EOffOn)SelectSlides[SelectSlideActive].Selection);
                } else if (Buttons[ButtonWebcam].Selected && CWebcam.GetDevices().Length > 0)
                {
                    OnWebcam();
                } else if (Buttons[ButtonSaveSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                {
                    OnSaveSnapshot();
                } else if (Buttons[ButtonDiscardSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                {
                    OnDiscardSnapshot();
                } else if (Buttons[ButtonTakeSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                {
                    OnTakeSnapshot();
                }
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
        }

        private void OnTakeSnapshot()
        {
            Buttons[ButtonSaveSnapshot].Visible = true;
            Buttons[ButtonDiscardSnapshot].Visible = true;
            Buttons[ButtonWebcam].Visible = false;
            Buttons[ButtonTakeSnapshot].Visible = false;
            _Snapshot = CWebcam.GetBitmap();
        }

        private void OnDiscardSnapshot()
        {
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Snapshot = null;
            Buttons[ButtonSaveSnapshot].Visible = false;
            Buttons[ButtonDiscardSnapshot].Visible = false;
            Buttons[ButtonTakeSnapshot].Visible = false;
            Buttons[ButtonWebcam].Visible = true;
        }

        private void OnSaveSnapshot()
        {
            string filename = "snapshot";
            int i = 0;
            while (File.Exists(Path.Combine(CSettings.sFolderProfiles, filename + i + ".png")))
            {
                i++;
            }
            _Snapshot.Save(Path.Combine(CSettings.sFolderProfiles, filename + i + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            CProfiles.LoadAvatars();
            LoadAvatars();
            _Snapshot = null;
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);

            for (int j = 0; j < CProfiles.Avatars.Length; j++)
            {
                if (CProfiles.Avatars[j].FileName == (filename + i + ".png"))
                {
                    CProfiles.SetAvatar(SelectSlides[SelectSlideProfiles].Selection, j);
                    break;
                }

            }

            Buttons[ButtonSaveSnapshot].Visible = false;
            Buttons[ButtonDiscardSnapshot].Visible = false;
            Buttons[ButtonTakeSnapshot].Visible = false;
            Buttons[ButtonWebcam].Visible = true;
        }

        private void OnWebcam()
        {
            _Snapshot = null;
            CWebcam.Start();
            CWebcam.GetFrame(ref _WebcamTexture);
            Buttons[ButtonSaveSnapshot].Visible = false;
            Buttons[ButtonDiscardSnapshot].Visible = false;
            Buttons[ButtonTakeSnapshot].Visible = true;
            Buttons[ButtonWebcam].Visible = false;
        }

        public override bool UpdateGame()
        {
            if (SelectSlides[SelectSlideProfiles].Selection > -1)
            {
                Buttons[ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(SelectSlides[SelectSlideProfiles].Selection);
                if (_EditMode == EEditMode.PlayerName)
                    Buttons[ButtonPlayerName].Text.Text += "|";

                SelectSlides[SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideActive].Selection = (int)CProfiles.GetActive(SelectSlides[SelectSlideProfiles].Selection);

                int avatarNr = CProfiles.GetAvatarNr(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideAvatars].Selection = avatarNr;
                if (CWebcam.GetDevices().Length > 0 && _WebcamTexture.index > 0)
                {
                    if(_Snapshot == null)
                        CWebcam.GetFrame(ref _WebcamTexture);
                    Statics[StaticAvatar].Texture = _WebcamTexture;

                    RectangleF bounds = new RectangleF(_WebcamTexture.rect.X, _WebcamTexture.rect.Y, _WebcamTexture.rect.W, _WebcamTexture.rect.H);
                    RectangleF rect = new RectangleF(0f, 0f, _WebcamTexture.rect.W, _WebcamTexture.rect.H);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);
                }
                else
                    Statics[StaticAvatar].Texture = CProfiles.Avatars[avatarNr].Texture;
            }
                
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            CProfiles.LoadProfiles();
            LoadAvatars();
            LoadProfiles();
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();

            OnDiscardSnapshot();
        }

        public override bool Draw()
        {
            return base.Draw();
        }

        private void SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
            LoadProfiles();
            UpdateGame();
        }

        private void DeleteProfile()
        {
            CProfiles.DeleteProfile(SelectSlides[SelectSlideProfiles].Selection);
            LoadProfiles();
            UpdateGame();
        }

        private void LoadProfiles()
        {
            _EditMode = EEditMode.None;
            SelectSlides[SelectSlideProfiles].Clear();
 
            for (int i = 0; i < CProfiles.NumProfiles; i++)
            {
                SelectSlides[SelectSlideProfiles].AddValue(CProfiles.GetPlayerName(i));
            }

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                SelectSlides[SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideActive].Selection = (int)CProfiles.GetActive(SelectSlides[SelectSlideProfiles].Selection);
                SelectSlides[SelectSlideAvatars].Selection = CProfiles.GetAvatarNr(SelectSlides[SelectSlideProfiles].Selection);
            }
        }

        private void LoadAvatars()
        {
            SelectSlides[SelectSlideAvatars].Clear();
            for (int i = 0; i < CProfiles.NumAvatars; i++)
            {
                SelectSlides[SelectSlideAvatars].AddValue(CProfiles.Avatars[i].FileName);
            }
        }

        private void NewProfile()
        {
            _EditMode = EEditMode.None;
            CProfiles.NewProfile();
            LoadProfiles();
            SelectSlides[SelectSlideProfiles].LastValue();
          
            CProfiles.SetAvatar(SelectSlides[SelectSlideProfiles].Selection,
                SelectSlides[SelectSlideAvatars].Selection);

            SetInteractionToButton(Buttons[ButtonPlayerName]);
            _EditMode = EEditMode.PlayerName;
        }        
    }
}
