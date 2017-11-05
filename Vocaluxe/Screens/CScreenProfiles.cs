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
using System.Linq;
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

    public class CScreenProfiles : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideProfiles = "SelectSlideProfiles";
        private const string _SelectSlideDifficulty = "SelectSlideDifficulty";
        private const string _SelectSlideAvatars = "SelectSlideAvatars";
        private const string _SelectSlideUserRole = "SelectSlideUserRole";
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

        private Dictionary<int, Guid> _SelectSlideGuids = new Dictionary<int, Guid>();

        private EEditMode _EditMode;

        private CTextureRef _WebcamTexture;
        private Bitmap _Snapshot;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]
                {_ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete, _ButtonWebcam, _ButtonSaveSnapshot, _ButtonDiscardSnapshot, _ButtonTakeSnapshot};
            _ThemeSelectSlides = new string[] {_SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideUserRole, _SelectSlideActive};
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
            _SelectSlides[_SelectSlideUserRole].SetValues<EUserRole>(0);
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
                            CProfiles.AddGetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), keyEvent.Unicode));
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
                            CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreen.Main);
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
                        else if (_Buttons[_ButtonWebcam].Selected)
                            _OnWebcam();
                        else if (_Buttons[_ButtonSaveSnapshot].Selected)
                            _OnSaveSnapshot();
                        else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                            _OnDiscardSnapshot();
                        else if (_Buttons[_ButtonTakeSnapshot].Selected)
                            _OnTakeSnapshot();
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            _SelectSlides[_SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag)));
                            _ProfilesChanged = true;
                        }
                        else
                            CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.Delete:
                        _DeleteProfile();
                        break;
                }
                if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreen.Main);
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
                    CProfiles.SetDifficulty(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                    if (CWebcam.IsDeviceAvailable() && _WebcamTexture != null)
                        _OnDiscardSnapshot();
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag),
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
                else if (_Buttons[_ButtonWebcam].Selected)
                    _OnWebcam();
                else if (_Buttons[_ButtonSaveSnapshot].Selected)
                    _OnSaveSnapshot();
                else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                    _OnDiscardSnapshot();
                else if (_Buttons[_ButtonTakeSnapshot].Selected)
                    _OnTakeSnapshot();
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreen.Main);
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
                _Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                if (_EditMode == EEditMode.PlayerName)
                    _Buttons[_ButtonPlayerName].Text.Text += "|";

                _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));

                int avatarID = CProfiles.GetAvatarID(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideAvatars].SelectedTag = avatarID;
                if (_Snapshot == null)
                {
                    if (CWebcam.IsCapturing())
                    {
                        if (CWebcam.GetFrame(ref _WebcamTexture))
                            _Statics[_StaticAvatar].Texture = _WebcamTexture;
                    }
                    else
                        _Statics[_StaticAvatar].Texture = CProfiles.GetAvatarTexture(avatarID);
                }
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
            if (!CWebcam.IsDeviceAvailable())
            {
                CDraw.RemoveTexture(ref _WebcamTexture);
                _Snapshot = null;
                _Buttons[_ButtonSaveSnapshot].Visible = false;
                _Buttons[_ButtonDiscardSnapshot].Visible = false;
                _Buttons[_ButtonTakeSnapshot].Visible = false;
                _Buttons[_ButtonWebcam].Visible = false;

                _SelectElement(_Buttons[_ButtonSave]);
            }
            else
            {
                CWebcam.Stop(); //Do this first to get consistent frame and bitmap
                _Snapshot = CWebcam.GetBitmap();
                if (CWebcam.GetFrame(ref _WebcamTexture))
                    _Statics[_StaticAvatar].Texture = _WebcamTexture;
                _Buttons[_ButtonSaveSnapshot].Visible = true;
                _Buttons[_ButtonDiscardSnapshot].Visible = true;
                _Buttons[_ButtonTakeSnapshot].Visible = false;
                _Buttons[_ButtonWebcam].Visible = false;

                _SelectElement(_Buttons[_ButtonSaveSnapshot]);
            }
        }

        private void _OnDiscardSnapshot()
        {
            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();

            _SelectElement(_Buttons[_ButtonWebcam]);
        }

        private void _OnSaveSnapshot()
        {
            string file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CConfig.ProfileFolders[0]), "snapshot.png");
            _Snapshot.Save(file, ImageFormat.Png);

            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();

            int id = CProfiles.NewAvatar(file);
            CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), id);
            _LoadAvatars(false);

            _SelectElement(_Buttons[_ButtonSave]);
        }

        private void _OnWebcam()
        {
            if (!CWebcam.IsDeviceAvailable())
            {
                _Buttons[_ButtonWebcam].Visible = false;
                return;
            }
            _Snapshot = null;
            CWebcam.Start();
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = true;
            _Buttons[_ButtonWebcam].Visible = false;

            _SelectElement(_Buttons[_ButtonTakeSnapshot]);
        }

        private void _NewProfile()
        {
            _EditMode = EEditMode.None;
            Guid id = CProfiles.NewProfile();
            _LoadProfiles(false);
            int num = CProfiles.NumProfiles;
            _SelectSlides[_SelectSlideProfiles].SelectedTag = num;
            _SelectSlideGuids.Add(num, id);

            CProfiles.SetAvatar(id, _SelectSlides[_SelectSlideAvatars].SelectedTag);

            _SelectElement(_Buttons[_ButtonPlayerName]);
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

            CProfiles.DeleteProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
            _SelectSlideGuids.Remove(_SelectSlides[_SelectSlideProfiles].SelectedTag);

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
                name = CProfiles.GetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));

            Guid selectedProfileID = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            _SelectSlides[_SelectSlideProfiles].Clear();
            _SelectSlideGuids.Clear();

            CProfile[] profiles = CProfiles.GetProfiles();
            int i = 0;
            foreach (CProfile profile in profiles)
            {
                _SelectSlides[_SelectSlideProfiles].AddValue(profile.PlayerName, null, i);
                _SelectSlideGuids.Add(i, profile.ID);
                i++;
            }

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                if (selectedProfileID != Guid.Empty)
                    _SelectSlides[_SelectSlideProfiles].SelectedTag = _SelectSlideGuids.FirstOrDefault(x => x.Value.Equals(selectedProfileID)).Key;
                else
                {
                    _SelectSlides[_SelectSlideProfiles].Selection = 0;
                    selectedProfileID = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                }

                if (!keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileID);
                    _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(selectedProfileID);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileID);
                    _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(selectedProfileID);
                }

                if (_EditMode == EEditMode.PlayerName)
                    CProfiles.SetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), name);
            }
            _ProfilesChanged = false;
        }

        private void _LoadAvatars(bool keep)
        {
            int selectedAvatarID = _SelectSlides[_SelectSlideAvatars].SelectedTag;
            _SelectSlides[_SelectSlideAvatars].Clear();
            IEnumerable<CAvatar> avatars = CProfiles.GetAvatars();
            if (avatars != null)
            {
                foreach (CAvatar avatar in avatars)
                    _SelectSlides[_SelectSlideAvatars].AddValue(avatar.GetDisplayName(), null, avatar.ID);
            }

            if (keep)
            {
                _SelectSlides[_SelectSlideAvatars].SelectedTag = selectedAvatarID;
                CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), selectedAvatarID);
            }
            else
                _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));

            _AvatarsChanged = false;
        }

        private Guid _GetIdFromTag(int tag)
        {
            if (tag == -1 || !_SelectSlideGuids.ContainsKey(tag))
                return Guid.Empty;
            else
                return _SelectSlideGuids[tag];
        }
    }
}