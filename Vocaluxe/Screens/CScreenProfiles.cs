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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

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
        private bool _ProfilesChanged;

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
            CProfiles.AddNotificationCallback(_OnProfilesChanged);
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();
            _SelectSlides[_SelectSlideDifficulty].SetValues<EGameDifficulty>(0);
            _SelectSlides[_SelectSlideGuestProfile].SetValues<EOffOn>(0);
            _SelectSlides[_SelectSlideActive].SetValues<EOffOn>(0);
            _Statics[_StaticAvatar].Aspect = EAspect.Crop;
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
                        _SelectSlides[_SelectSlideProfiles].RenameValue(
                            CProfiles.AddGetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex, keyEvent.Unicode));
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
                        if (_Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        else if (_Buttons[_ButtonSave].Selected)
                            _SaveProfiles();
                        else if (_Buttons[_ButtonNew].Selected)
                            _NewProfile();
                        else if (_Buttons[_ButtonPlayerName].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        }
                        else if (_Buttons[_ButtonDelete].Selected)
                            _DeleteProfile();
                        else if (_Buttons[_ButtonWebcam].Selected && CWebcam.IsDeviceAvailable())
                            _OnWebcam();
                        else if (_Buttons[_ButtonSaveSnapshot].Selected && CWebcam.IsDeviceAvailable())
                            _OnSaveSnapshot();
                        else if (_Buttons[_ButtonDiscardSnapshot].Selected && CWebcam.IsDeviceAvailable())
                            _OnDiscardSnapshot();
                        else if (_Buttons[_ButtonTakeSnapshot].Selected && CWebcam.IsDeviceAvailable())
                            _OnTakeSnapshot();
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            _SelectSlides[_SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex));
                        }
                        else
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Delete:
                        _DeleteProfile();
                        break;
                }
                if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                        _SelectSlides[_SelectSlideAvatars].ValueIndex);
                }
                else if (_SelectSlides[_SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                              (EOffOn)_SelectSlides[_SelectSlideGuestProfile].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);
                else if (_Buttons[_ButtonSave].Selected)
                    _SaveProfiles();
                else if (_Buttons[_ButtonNew].Selected)
                    _NewProfile();
                else if (_Buttons[_ButtonDelete].Selected)
                    _DeleteProfile();
                else if (_Buttons[_ButtonPlayerName].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                }
                else if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                        _SelectSlides[_SelectSlideAvatars].ValueIndex);
                    if (CWebcam.IsDeviceAvailable() && _WebcamTexture.Index > 0)
                        _OnDiscardSnapshot();
                }
                else if (_SelectSlides[_SelectSlideGuestProfile].Selected)
                {
                    CProfiles.SetGuestProfile(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                              (EOffOn)_SelectSlides[_SelectSlideGuestProfile].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
                else if (_Buttons[_ButtonWebcam].Selected && CWebcam.IsDeviceAvailable())
                    _OnWebcam();
                else if (_Buttons[_ButtonSaveSnapshot].Selected && CWebcam.IsDeviceAvailable())
                    _OnSaveSnapshot();
                else if (_Buttons[_ButtonDiscardSnapshot].Selected && CWebcam.IsDeviceAvailable())
                    _OnDiscardSnapshot();
                else if (_Buttons[_ButtonTakeSnapshot].Selected && CWebcam.IsDeviceAvailable())
                    _OnTakeSnapshot();
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        private void _OnTakeSnapshot()
        {
            _Buttons[_ButtonSaveSnapshot].Visible = true;
            _Buttons[_ButtonDiscardSnapshot].Visible = true;
            _Buttons[_ButtonWebcam].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Snapshot = CWebcam.GetBitmap();
        }

        private void _OnDiscardSnapshot()
        {
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Snapshot = null;
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = true;
        }

        private void _OnSaveSnapshot()
        {
            const string filename = "snapshot";
            int i = 0;
            while (File.Exists(Path.Combine(CSettings.FolderProfiles, filename + i + ".png")))
                i++;
            _Snapshot.Save(Path.Combine(CSettings.FolderProfiles, filename + i + ".png"), ImageFormat.Png);
            CProfiles.LoadAvatars();
            _LoadAvatars(false);
            _Snapshot = null;
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);

            CAvatar[] avatars = CProfiles.GetAvatars();
            for (int j = 0; j < avatars.Length; j++)
            {
                if (Path.GetFileName(avatars[j].FileName) == (filename + i + ".png"))
                {
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex, avatars[j].ID);
                    break;
                }
            }

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = true;
        }

        private void _OnWebcam()
        {
            _Snapshot = null;
            CWebcam.Start();
            CWebcam.GetFrame(ref _WebcamTexture);
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = true;
            _Buttons[_ButtonWebcam].Visible = false;
        }

        public override bool UpdateGame()
        {
            if (_ProfilesChanged)
            {
                _LoadAvatars(true);
                _LoadProfiles(true);
                _ProfilesChanged = false;
            }

            if (_SelectSlides[_SelectSlideProfiles].Selection > -1)
            {
                _Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex);
                if (_EditMode == EEditMode.PlayerName)
                    _Buttons[_ButtonPlayerName].Text.Text += "|";

                _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(_SelectSlides[_SelectSlideProfiles].ValueIndex);
                _SelectSlides[_SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(_SelectSlides[_SelectSlideProfiles].ValueIndex);
                _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(_SelectSlides[_SelectSlideProfiles].ValueIndex);

                int avatarID = CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].ValueIndex);
                _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(avatarID);
                if (CWebcam.IsDeviceAvailable() && _WebcamTexture.Index != -1)
                {
                    if (_Snapshot == null)
                        CWebcam.GetFrame(ref _WebcamTexture);

                    _Statics[_StaticAvatar].Texture = _WebcamTexture;
                }
                else
                    _Statics[_StaticAvatar].Texture = CProfiles.GetAvatarTexture(avatarID);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            CProfiles.LoadProfiles();
            _LoadAvatars(false);
            _LoadProfiles(false);
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();

            _OnDiscardSnapshot();
        }

        private void _OnProfilesChanged()
        {
            _ProfilesChanged = true;
        }

        private void _SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
            _LoadProfiles(true);
            UpdateGame();
        }

        private void _DeleteProfile()
        {
            _EditMode = EEditMode.None;
            int selection = _SelectSlides[_SelectSlideProfiles].Selection;
            CProfiles.DeleteProfile(_SelectSlides[_SelectSlideProfiles].ValueIndex);
            _LoadProfiles(false);

            if (_SelectSlides[_SelectSlideProfiles].NumValues > selection)
                _SelectSlides[_SelectSlideProfiles].Selection = selection;
            else
                _SelectSlides[_SelectSlideProfiles].Selection = selection - 1;
            UpdateGame();
        }

        private void _LoadProfiles(bool Keep)
        {
            string name = String.Empty;
            if (_EditMode == EEditMode.PlayerName)
                name = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex);

            int selectedProfileID = _SelectSlides[_SelectSlideProfiles].ValueIndex;
            _SelectSlides[_SelectSlideProfiles].Clear();

            CProfile[] profiles = CProfiles.GetProfiles();
            for (int i = 0; i < profiles.Length; i++)
                _SelectSlides[_SelectSlideProfiles].AddValue(
                    profiles[i].PlayerName, 
                    new STexture(-1), 
                    profiles[i].ID, 
                    -1);

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                _SelectSlides[_SelectSlideProfiles].SetSelectionByValueIndex(selectedProfileID);

                if (!Keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileID);
                    _SelectSlides[_SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(selectedProfileID);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileID);
                    _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(CProfiles.GetAvatarID(selectedProfileID));
                }

                if (_EditMode == EEditMode.PlayerName)
                    CProfiles.SetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex, name);
            }
        }

        private void _LoadAvatars(bool Keep)
        {
            int selectedAvatarID = _SelectSlides[_SelectSlideAvatars].ValueIndex;
            _SelectSlides[_SelectSlideAvatars].Clear();
            CAvatar[] avatars = CProfiles.GetAvatars();
            for (int i = 0; i < avatars.Length; i++)
                _SelectSlides[_SelectSlideAvatars].AddValue(
                    Path.GetFileName(avatars[i].FileName),
                    new STexture(-1),
                    avatars[i].ID,
                    -1);

            if (Keep)
            {
                _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(selectedAvatarID);
                CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex, selectedAvatarID);
            }
            else
                _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].ValueIndex));
        }

        private void _NewProfile()
        {
            _EditMode = EEditMode.None;
            int id = CProfiles.NewProfile();
            _LoadProfiles(false);
            _SelectSlides[_SelectSlideProfiles].SetSelectionByValueIndex(id);

            CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex,
                                _SelectSlides[_SelectSlideAvatars].ValueIndex);

            _SetInteractionToButton(_Buttons[_ButtonPlayerName]);
            _EditMode = EEditMode.PlayerName;
        }
    }
}