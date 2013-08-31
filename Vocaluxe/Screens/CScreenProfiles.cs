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
using VocaluxeLib.Draw;

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
        private bool _ProfilesChanged;
        private bool _AvatarsChanged;

        private EEditMode _EditMode;

        private CTexture _WebcamTexture;
        private Bitmap _Snapshot;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]
                {_ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete, _ButtonWebcam, _ButtonSaveSnapshot, _ButtonDiscardSnapshot, _ButtonTakeSnapshot};
            _ThemeSelectSlides = new string[] {_SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideGuestProfile, _SelectSlideActive};
            _ThemeStatics = new string[] {_StaticAvatar};

            _EditMode = EEditMode.None;
            _ProfilesChanged = false;
            _AvatarsChanged = false;
            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
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
                        _ProfilesChanged = true;
                        break;
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        if (_EditMode == EEditMode.PlayerName)
                            _EditMode = EEditMode.None;
                        else
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
                            _ProfilesChanged = true;
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
                    if (CWebcam.IsDeviceAvailable() && _WebcamTexture != null)
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

        public override bool UpdateGame()
        {
            if (_AvatarsChanged)
                _LoadAvatars(true);

            if (_ProfilesChanged)
                _LoadProfiles(true);

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
                if (CWebcam.IsDeviceAvailable() && CWebcam.IsCapturing())
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
            _LoadAvatars(false);
            _LoadProfiles(false);
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();
            _EditMode = EEditMode.None;
            _OnDiscardSnapshot();
        }

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Avatar == (EProfileChangedFlags.Avatar & flags))
                _AvatarsChanged = true;

            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
                _ProfilesChanged = true;
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
            string filename = Path.Combine(CSettings.DataPath, CSettings.FolderProfiles, "snapshot");
            int i = 0;
            while (File.Exists(filename + i + ".png"))
                i++;

            filename = filename + i + ".png";
            _Snapshot.Save(filename, ImageFormat.Png);

            _Snapshot = null;
            CWebcam.Stop();
            CDraw.RemoveTexture(ref _WebcamTexture);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = true;

            int id = CProfiles.NewAvatar(filename);
            CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex, id);
            _LoadAvatars(false);
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

        private void _SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
        }

        private void _DeleteProfile()
        {
            _EditMode = EEditMode.None;

            CProfiles.DeleteProfile(_SelectSlides[_SelectSlideProfiles].ValueIndex);

            int selection = _SelectSlides[_SelectSlideProfiles].Selection;
            if (_SelectSlides[_SelectSlideProfiles].NumValues - 1 > selection)
                _SelectSlides[_SelectSlideProfiles].Selection = selection + 1;
            else
                _SelectSlides[_SelectSlideProfiles].Selection = selection - 1;
        }

        private void _LoadProfiles(bool keep)
        {
            string name = String.Empty;
            if (_EditMode == EEditMode.PlayerName)
                name = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex);

            int selectedProfileID = _SelectSlides[_SelectSlideProfiles].ValueIndex;
            _SelectSlides[_SelectSlideProfiles].Clear();

            CProfile[] profiles = CProfiles.GetProfiles();
            foreach (CProfile profile in profiles)
            {
                _SelectSlides[_SelectSlideProfiles].AddValue(
                    profile.PlayerName,
                    null,
                    profile.ID,
                    -1);
            }

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                if (selectedProfileID != -1)
                    _SelectSlides[_SelectSlideProfiles].SetSelectionByValueIndex(selectedProfileID);
                else
                {
                    _SelectSlides[_SelectSlideProfiles].Selection = 0;
                    selectedProfileID = _SelectSlides[_SelectSlideProfiles].ValueIndex;
                }

                if (!keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileID);
                    _SelectSlides[_SelectSlideGuestProfile].Selection = (int)CProfiles.GetGuestProfile(selectedProfileID);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileID);
                    _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(CProfiles.GetAvatarID(selectedProfileID));
                }

                if (_EditMode == EEditMode.PlayerName)
                    CProfiles.SetPlayerName(_SelectSlides[_SelectSlideProfiles].ValueIndex, name);
            }
            _ProfilesChanged = false;
        }

        private void _LoadAvatars(bool keep)
        {
            int selectedAvatarID = _SelectSlides[_SelectSlideAvatars].ValueIndex;
            _SelectSlides[_SelectSlideAvatars].Clear();
            IEnumerable<CAvatar> avatars = CProfiles.GetAvatars();
            if (avatars != null)
            {
                foreach (CAvatar avatar in avatars)
                {
                    _SelectSlides[_SelectSlideAvatars].AddValue(
                        Path.GetFileName(avatar.FileName),
                        null,
                        avatar.ID,
                        -1);
                }
            }

            if (keep)
            {
                _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(selectedAvatarID);
                CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].ValueIndex, selectedAvatarID);
            }
            else
                _SelectSlides[_SelectSlideAvatars].SetSelectionByValueIndex(CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].ValueIndex));

            _AvatarsChanged = false;
        }
    }
}