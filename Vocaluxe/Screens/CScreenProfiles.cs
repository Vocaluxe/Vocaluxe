using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

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
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const string _SelectSlideProfiles = "SelectSlideProfiles";
        private const string _SelectSlideDifficulty = "SelectSlideDifficulty";
        private const string _SelectSlideAvatars = "SelectSlideAvatars";
        private const string _SelectSlideGuestProfile = "SelectSlideGuestProfile";
        private const string _SelectSlideActive = "SelectSlideActive";
        private const string _ButtonPlayerName = "ButtonPlayerName";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonSave = "ButtonSave";
        private const string _ButtonNew = "ButtonNew";
        private const string _ButtonDelete = "ButtonDelete";
        private const string _ButtonWebcam = "ButtonWebcam";
        private const string _ButtonSaveSnapshot = "ButtonSaveSnapshot";
        private const string _ButtonDiscardSnapshot = "ButtonDiscardSnapshot";
        private const string _ButtonTakeSnapshot = "ButtonTakeSnapshot";

        private const string _StaticAvatar = "StaticAvatar";

        private EEditMode _EditMode;

        private STexture _WebcamTexture = new STexture(-1);
        private Bitmap _Snapshot;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]
                {_ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete, _ButtonWebcam, _ButtonSaveSnapshot, _ButtonDiscardSnapshot, _ButtonTakeSnapshot};
            _ThemeSelectSlides = new string[] {_SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideGuestProfile, _SelectSlideActive};
            _ThemeStatics = new string[] {_StaticAvatar};

            _EditMode = EEditMode.None;
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            Buttons[_ButtonSaveSnapshot].Visible = false;
            Buttons[_ButtonDiscardSnapshot].Visible = false;
            Buttons[_ButtonTakeSnapshot].Visible = false;
            if (CWebcam.GetDevices().Length > 0)
                Buttons[_ButtonWebcam].Visible = true;
            else
                Buttons[_ButtonWebcam].Visible = false;
            SelectSlides[_SelectSlideDifficulty].SetValues<EGameDifficulty>(0);
            SelectSlides[_SelectSlideGuestProfile].SetValues<EOffOn>(0);
            SelectSlides[_SelectSlideActive].SetValues<EOffOn>(0);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
            {
                switch (_EditMode)
                {
                    case EEditMode.None:
                        break;
                    case EEditMode.PlayerName:
                        SelectSlides[_SelectSlideProfiles].RenameValue(
                            CProfiles.AddGetPlayerName(SelectSlides[_SelectSlideProfiles].Selection, keyEvent.Unicode));
                        break;
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        if (Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        else if (Buttons[_ButtonSave].Selected)
                            _SaveProfiles();
                        else if (Buttons[_ButtonNew].Selected)
                            _NewProfile();
                        else if (Buttons[_ButtonPlayerName].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        }
                        else if (Buttons[_ButtonDelete].Selected)
                            _DeleteProfile();
                        else if (Buttons[_ButtonWebcam].Selected && CWebcam.GetDevices().Length > 0)
                            _OnWebcam();
                        else if (Buttons[_ButtonSaveSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                            _OnSaveSnapshot();
                        else if (Buttons[_ButtonDiscardSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                            _OnDiscardSnapshot();
                        else if (Buttons[_ButtonTakeSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                            _OnTakeSnapshot();
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            SelectSlides[_SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(SelectSlides[_SelectSlideProfiles].Selection));
                        }
                        else
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Delete:
                        _DeleteProfile();
                        break;
                }
                if (SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[_SelectSlideProfiles].Selection,
                                            (EGameDifficulty)SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[_SelectSlideProfiles].Selection,
                                        SelectSlides[_SelectSlideAvatars].Selection);
                }
                else if (SelectSlides[_SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[_SelectSlideProfiles].Selection,
                                              (EOffOn)SelectSlides[_SelectSlideGuestProfile].Selection);
                }
                else if (SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(SelectSlides[_SelectSlideProfiles].Selection,
                                        (EOffOn)SelectSlides[_SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);
                else if (Buttons[_ButtonSave].Selected)
                    _SaveProfiles();
                else if (Buttons[_ButtonNew].Selected)
                    _NewProfile();
                else if (Buttons[_ButtonDelete].Selected)
                    _DeleteProfile();
                else if (Buttons[_ButtonPlayerName].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                }
                else if (SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[_SelectSlideProfiles].Selection,
                                            (EGameDifficulty)SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[_SelectSlideProfiles].Selection,
                                        SelectSlides[_SelectSlideAvatars].Selection);
                    if (CWebcam.GetDevices().Length > 0 && _WebcamTexture.Index > 0)
                        _OnDiscardSnapshot();
                }
                else if (SelectSlides[_SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[_SelectSlideProfiles].Selection,
                                              (EOffOn)SelectSlides[_SelectSlideGuestProfile].Selection);
                }
                else if (SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(SelectSlides[_SelectSlideProfiles].Selection,
                                        (EOffOn)SelectSlides[_SelectSlideActive].Selection);
                }
                else if (Buttons[_ButtonWebcam].Selected && CWebcam.GetDevices().Length > 0)
                    _OnWebcam();
                else if (Buttons[_ButtonSaveSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                    _OnSaveSnapshot();
                else if (Buttons[_ButtonDiscardSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                    _OnDiscardSnapshot();
                else if (Buttons[_ButtonTakeSnapshot].Selected && CWebcam.GetDevices().Length > 0)
                    _OnTakeSnapshot();
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        private void _OnTakeSnapshot()
        {
            Buttons[_ButtonSaveSnapshot].Visible = true;
            Buttons[_ButtonDiscardSnapshot].Visible = true;
            Buttons[_ButtonWebcam].Visible = false;
            Buttons[_ButtonTakeSnapshot].Visible = false;
            _Snapshot = CWebcam.GetBitmap();
        }

        private void _OnDiscardSnapshot()
        {
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Snapshot = null;
            Buttons[_ButtonSaveSnapshot].Visible = false;
            Buttons[_ButtonDiscardSnapshot].Visible = false;
            Buttons[_ButtonTakeSnapshot].Visible = false;
            Buttons[_ButtonWebcam].Visible = true;
        }

        private void _OnSaveSnapshot()
        {
            string filename = "snapshot";
            int i = 0;
            while (File.Exists(Path.Combine(CSettings.FolderProfiles, filename + i + ".png")))
                i++;
            _Snapshot.Save(Path.Combine(CSettings.FolderProfiles, filename + i + ".png"), ImageFormat.Png);
            CProfiles.LoadAvatars();
            _LoadAvatars();
            _Snapshot = null;
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);

            for (int j = 0; j < CProfiles.Avatars.Length; j++)
            {
                if (CProfiles.Avatars[j].FileName == (filename + i + ".png"))
                {
                    CProfiles.SetAvatar(SelectSlides[_SelectSlideProfiles].Selection, j);
                    break;
                }
            }

            Buttons[_ButtonSaveSnapshot].Visible = false;
            Buttons[_ButtonDiscardSnapshot].Visible = false;
            Buttons[_ButtonTakeSnapshot].Visible = false;
            Buttons[_ButtonWebcam].Visible = true;
        }

        private void _OnWebcam()
        {
            _Snapshot = null;
            CWebcam.Start();
            CWebcam.GetFrame(ref _WebcamTexture);
            Buttons[_ButtonSaveSnapshot].Visible = false;
            Buttons[_ButtonDiscardSnapshot].Visible = false;
            Buttons[_ButtonTakeSnapshot].Visible = true;
            Buttons[_ButtonWebcam].Visible = false;
        }

        public override bool UpdateGame()
        {
            if (SelectSlides[_SelectSlideProfiles].Selection > -1)
            {
                Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(SelectSlides[_SelectSlideProfiles].Selection);
                if (_EditMode == EEditMode.PlayerName)
                    Buttons[_ButtonPlayerName].Text.Text += "|";

                SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(SelectSlides[_SelectSlideProfiles].Selection);

                int avatarNr = CProfiles.GetAvatarNr(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideAvatars].Selection = avatarNr;
                if (CWebcam.GetDevices().Length > 0 && _WebcamTexture.Index > 0)
                {
                    if (_Snapshot == null)
                        CWebcam.GetFrame(ref _WebcamTexture);
                    Statics[_StaticAvatar].Texture = _WebcamTexture;

                    RectangleF bounds = new RectangleF(_WebcamTexture.Rect.X, _WebcamTexture.Rect.Y, _WebcamTexture.Rect.W, _WebcamTexture.Rect.H);
                    RectangleF rect = new RectangleF(0f, 0f, _WebcamTexture.Rect.W, _WebcamTexture.Rect.H);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);
                }
                else
                    Statics[_StaticAvatar].Texture = CProfiles.Avatars[avatarNr].Texture;
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            CProfiles.LoadProfiles();
            _LoadAvatars();
            _LoadProfiles();
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();

            _OnDiscardSnapshot();
        }

        private void _SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
            _LoadProfiles();
            UpdateGame();
        }

        private void _DeleteProfile()
        {
            CProfiles.DeleteProfile(SelectSlides[_SelectSlideProfiles].Selection);
            _LoadProfiles();
            UpdateGame();
        }

        private void _LoadProfiles()
        {
            _EditMode = EEditMode.None;
            SelectSlides[_SelectSlideProfiles].Clear();

            for (int i = 0; i < CProfiles.NumProfiles; i++)
                SelectSlides[_SelectSlideProfiles].AddValue(CProfiles.GetPlayerName(i));

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(SelectSlides[_SelectSlideProfiles].Selection);
                SelectSlides[_SelectSlideAvatars].Selection = CProfiles.GetAvatarNr(SelectSlides[_SelectSlideProfiles].Selection);
            }
        }

        private void _LoadAvatars()
        {
            SelectSlides[_SelectSlideAvatars].Clear();
            for (int i = 0; i < CProfiles.NumAvatars; i++)
                SelectSlides[_SelectSlideAvatars].AddValue(CProfiles.Avatars[i].FileName);
        }

        private void _NewProfile()
        {
            _EditMode = EEditMode.None;
            CProfiles.NewProfile();
            _LoadProfiles();
            SelectSlides[_SelectSlideProfiles].LastValue();

            CProfiles.SetAvatar(SelectSlides[_SelectSlideProfiles].Selection,
                                SelectSlides[_SelectSlideAvatars].Selection);

            SetInteractionToButton(Buttons[_ButtonPlayerName]);
            _EditMode = EEditMode.PlayerName;
        }
    }
}