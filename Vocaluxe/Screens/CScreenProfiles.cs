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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

namespace Vocaluxe.Screens
{
    enum EEditMode
    {
        None,
        PlayerName
    }

    enum EProfilesPopup
    {
        None,
        Delete,
        UnsavedChanges
    }

    public class CScreenProfiles : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 5; }
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
        private const string _NameSelection = "NameSelection";
        private const string _StaticAvatar = "StaticAvatar";
        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _TextPopup = "TextPopup";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";
        private const string _ButtonPopupCancel = "ButtonPopupCancel";
        private bool _ProfilesChanged;
        private bool _AvatarsChanged;
        private bool _SelectingKeyboardActive;

        private Dictionary<int, Guid> _SelectSlideGuids = new Dictionary<int, Guid>();

        private EEditMode _EditMode;
        private EProfilesPopup _Popup;
        private bool _UnsavedChanges;
        private string _NameBeforeEdit = string.Empty;

        private CTextureRef _WebcamTexture;
        private Bitmap _Snapshot;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]
            {
                _ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete, _ButtonWebcam, _ButtonSaveSnapshot, _ButtonDiscardSnapshot, _ButtonTakeSnapshot,
                _ButtonPopupYes, _ButtonPopupNo, _ButtonPopupCancel
            };
            _ThemeSelectSlides = new string[] { _SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideUserRole, _SelectSlideActive };
            _ThemeStatics = new string[] { _StaticAvatar, _StaticPopupBG };
            _ThemeTexts = new string[] { _TextPopup };
            _ThemeNameSelections = new string[] { _NameSelection };

            _EditMode = EEditMode.None;
            _ProfilesChanged = false;
            _AvatarsChanged = false;
            _SelectingKeyboardActive = false;
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
            _NameSelections[_NameSelection].ShowInactive = true;
            _ShowPopup(EProfilesPopup.None);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_Popup != EProfilesPopup.None)
            {
                _HandlePopupInput(keyEvent);
                return true;
            }

            if (_SelectingKeyboardActive)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                        _NameSelections[_NameSelection].HandleInput(keyEvent);
                        return true;

                    case Keys.Enter:
                        if (_NameSelections[_NameSelection].SelectedId != Guid.Empty)
                        {
                            _SelectProfileById(_NameSelections[_NameSelection].SelectedId);
                        }

                        _NameSelections[_NameSelection].FastSelection(false, -1);
                        _SelectingKeyboardActive = false;
                        _SelectElement(_Buttons[_ButtonPlayerName]);
                        return true;

                    case Keys.Escape:
                    case Keys.Back:
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                        _SelectingKeyboardActive = false;
                        _SelectElement(_Buttons[_ButtonExit]);
                        return true;
                }
            }

            if (_EditMode == EEditMode.None && keyEvent.Key == Keys.Up && _Buttons[_ButtonExit].Selected)
            {
                if (CProfiles.NumProfiles <= 0)
                {
                    return true;
                }

                _SelectingKeyboardActive = true;
                _NameSelections[_NameSelection].Init();
                _NameSelections[_NameSelection].UpdateList();
                _NameSelections[_NameSelection].FastSelection(true, 1);
            }

            if (_EditMode == EEditMode.None)
            {
                base.HandleInput(keyEvent);
            }

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
                        _UnsavedChanges = true;
                        break;
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            _EndNameEdit(true);
                        }
                        else
                        {
                            _Leave();
                        }

                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                        {
                            _Leave();
                        }
                        else if (_Buttons[_ButtonSave].Selected)
                        {
                            _SaveProfiles();
                        }
                        else if (_Buttons[_ButtonNew].Selected)
                        {
                            _NewProfile();
                        }
                        else if (_Buttons[_ButtonPlayerName].Selected)
                        {
                            _ToggleNameEdit();
                        }
                        else if (_Buttons[_ButtonDelete].Selected)
                        {
                            _RequestDeleteProfile();
                        }
                        else if (_Buttons[_ButtonWebcam].Selected)
                        {
                            _OnWebcam();
                        }
                        else if (_Buttons[_ButtonSaveSnapshot].Selected)
                        {
                            _OnSaveSnapshot();
                        }
                        else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                        {
                            _OnDiscardSnapshot();
                        }
                        else if (_Buttons[_ButtonTakeSnapshot].Selected)
                        {
                            _OnTakeSnapshot();
                        }

                        break;

                    case Keys.Back:
                    case Keys.Delete:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            _SelectSlides[_SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag)));
                            _ProfilesChanged = true;
                            _UnsavedChanges = true;
                        }
                        else if (keyEvent.Key == Keys.Delete)
                        {
                            _RequestDeleteProfile();
                        }
                        else
                        {
                            _Leave();
                        }

                        break;
                }

                _ApplySelectSlideValues();
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_Popup != EProfilesPopup.None)
            {
                _HandlePopupMouse(mouseEvent);
                return true;
            }

            if (_EditMode == EEditMode.None)
            {
                base.HandleMouse(mouseEvent);
            }

            _NameSelections[_NameSelection].HandleMouse(mouseEvent);

            if (mouseEvent.LB && _NameSelections[_NameSelection].IsOverTile(mouseEvent))
            {
                var profileId = _NameSelections[_NameSelection].TilePlayerId(mouseEvent);
                _SelectProfileById(profileId);
                return true;
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                {
                    _Leave();
                }
                else if (_Buttons[_ButtonSave].Selected)
                {
                    _SaveProfiles();
                }
                else if (_Buttons[_ButtonNew].Selected)
                {
                    _NewProfile();
                }
                else if (_Buttons[_ButtonDelete].Selected)
                {
                    _RequestDeleteProfile();
                }
                else if (_Buttons[_ButtonPlayerName].Selected)
                {
                    _ToggleNameEdit();
                }
                else if (_SelectSlides[_SelectSlideDifficulty].Selected || _SelectSlides[_SelectSlideAvatars].Selected ||
                         _SelectSlides[_SelectSlideUserRole].Selected || _SelectSlides[_SelectSlideActive].Selected)
                {
                    _ApplySelectSlideValues();
                }
                else if (_Buttons[_ButtonWebcam].Selected)
                {
                    _OnWebcam();
                }
                else if (_Buttons[_ButtonSaveSnapshot].Selected)
                {
                    _OnSaveSnapshot();
                }
                else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                {
                    _OnDiscardSnapshot();
                }
                else if (_Buttons[_ButtonTakeSnapshot].Selected)
                {
                    _OnTakeSnapshot();
                }
            }

            if (mouseEvent.RB)
            {
                _Leave();
            }

            if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_NameSelections[_NameSelection].Rect, mouseEvent))
            {
                var offset = _NameSelections[_NameSelection].Offset + mouseEvent.Wheel;
                _NameSelections[_NameSelection].UpdateList(offset);
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_AvatarsChanged)
            {
                _LoadAvatars(true);
                _NameSelections[_NameSelection].UpdateList();
            }

            if (_ProfilesChanged)
            {
                _LoadProfiles(true);
                _NameSelections[_NameSelection].UpdateList();
            }

            if (_SelectSlides[_SelectSlideProfiles].Selection > -1)
            {
                _Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                if (_EditMode == EEditMode.PlayerName)
                {
                    _Buttons[_ButtonPlayerName].Text.Text += "|";
                }

                _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));

                var avatarId = CProfiles.GetAvatarId(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                _SelectSlides[_SelectSlideAvatars].SelectedTag = avatarId;
                if (_Snapshot == null)
                {
                    if (CWebcam.IsCapturing())
                    {
                        if (CWebcam.GetFrame(ref _WebcamTexture))
                        {
                            _Statics[_StaticAvatar].Texture = _WebcamTexture;
                        }
                    }
                    else
                    {
                        _Statics[_StaticAvatar].Texture = CProfiles.GetAvatarTexture(avatarId);
                    }
                }
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _NameSelections[_NameSelection].Init();
            _LoadAvatars(false);
            _LoadProfiles(false);
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();
            _EditMode = EEditMode.None;
            _SelectingKeyboardActive = false;
            _ShowPopup(EProfilesPopup.None);
            _OnDiscardSnapshot();
        }

        private void _Leave()
        {
            _EndNameEdit(false);
            if (_UnsavedChanges)
            {
                _ShowPopup(EProfilesPopup.UnsavedChanges);
                return;
            }

            CGraphics.FadeTo(EScreen.Main);
        }

        private void _DiscardChanges()
        {
            _UnsavedChanges = false;
            CProfiles.LoadProfiles();
        }

        private void _ShowPopup(EProfilesPopup popup)
        {
            _Popup = popup;
            var visible = popup != EProfilesPopup.None;
            _Statics[_StaticPopupBG].Visible = visible;
            _Texts[_TextPopup].Visible = visible;
            _Buttons[_ButtonPopupYes].Visible = visible;
            _Buttons[_ButtonPopupNo].Visible = popup == EProfilesPopup.UnsavedChanges;
            _Buttons[_ButtonPopupCancel].Visible = visible;

            switch (popup)
            {
                case EProfilesPopup.Delete:
                    var name = CProfiles.GetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                    _Texts[_TextPopup].Text = CLanguage.Translate("TR_SCREENPROFILES_POPUP_DELETE").Replace("%s", name);
                    _SelectElement(_Buttons[_ButtonPopupCancel]);
                    break;

                case EProfilesPopup.UnsavedChanges:
                    _Texts[_TextPopup].Text = CLanguage.Translate("TR_SCREENPROFILES_POPUP_UNSAVED");
                    _SelectElement(_Buttons[_ButtonPopupYes]);
                    break;
            }
        }

        private List<string> _GetPopupButtons()
        {
            return new[] { _ButtonPopupYes, _ButtonPopupNo, _ButtonPopupCancel }.Where(name => _Buttons[name].Visible).ToList();
        }

        private void _HandlePopupInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed)
            {
                return;
            }

            var buttons = _GetPopupButtons();
            var selected = buttons.FindIndex(name => _Buttons[name].Selected);
            switch (keyEvent.Key)
            {
                case Keys.Left:
                case Keys.Up:
                    _SelectElement(_Buttons[buttons[(selected - 1 + buttons.Count) % buttons.Count]]);
                    break;

                case Keys.Right:
                case Keys.Down:
                case Keys.Tab:
                    _SelectElement(_Buttons[buttons[(selected + 1) % buttons.Count]]);
                    break;

                case Keys.Escape:
                case Keys.Back:
                    _ExecutePopupButton(_ButtonPopupCancel);
                    break;

                case Keys.Enter:
                    _ExecutePopupButton(selected >= 0 ? buttons[selected] : _ButtonPopupCancel);
                    break;
            }
        }

        private void _HandlePopupMouse(SMouseEvent mouseEvent)
        {
            foreach (var name in _GetPopupButtons())
            {
                if (!CHelper.IsInBounds(_Buttons[name].Rect, mouseEvent))
                {
                    continue;
                }

                if (!_Buttons[name].Selected)
                {
                    _SelectElement(_Buttons[name]);
                }

                if (mouseEvent.LB)
                {
                    _ExecutePopupButton(name);
                }

                return;
            }

            if (mouseEvent.RB)
            {
                _ExecutePopupButton(_ButtonPopupCancel);
            }
        }

        private void _ExecutePopupButton(string button)
        {
            var popup = _Popup;
            _ShowPopup(EProfilesPopup.None);

            if (popup == EProfilesPopup.Delete)
            {
                if (button == _ButtonPopupYes)
                {
                    _DeleteProfile();
                }

                _SelectElement(_Buttons[_ButtonDelete]);
            }
            else if (popup == EProfilesPopup.UnsavedChanges)
            {
                if (button == _ButtonPopupCancel)
                {
                    _SelectElement(_Buttons[_ButtonSave]);
                    return;
                }

                if (button == _ButtonPopupYes)
                {
                    _SaveProfiles();
                }
                else
                {
                    _DiscardChanges();
                }

                CGraphics.FadeTo(EScreen.Main);
            }
        }

        private void _RequestDeleteProfile()
        {
            _EndNameEdit(false);
            if (!CProfiles.IsProfileIdValid(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag)))
            {
                return;
            }

            _ShowPopup(EProfilesPopup.Delete);
        }

        private void _ToggleNameEdit()
        {
            if (_EditMode == EEditMode.PlayerName)
            {
                _EndNameEdit(false);
            }
            else
            {
                _StartNameEdit();
            }
        }

        private void _StartNameEdit()
        {
            var id = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            if (!CProfiles.IsProfileIdValid(id))
            {
                return;
            }

            _NameBeforeEdit = CProfiles.GetPlayerName(id);
            _EditMode = EEditMode.PlayerName;
        }

        private void _EndNameEdit(bool restore)
        {
            if (_EditMode != EEditMode.PlayerName)
            {
                return;
            }

            _EditMode = EEditMode.None;
            var id = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            if (!restore && !string.IsNullOrWhiteSpace(CProfiles.GetPlayerName(id)))
            {
                return;
            }

            var name = string.IsNullOrWhiteSpace(_NameBeforeEdit) ? CProfiles.GetPlayerName(Guid.Empty) : _NameBeforeEdit;
            CProfiles.SetPlayerName(id, name);
            _SelectSlides[_SelectSlideProfiles].RenameValue(name);
            _ProfilesChanged = true;
        }

        private void _ApplySelectSlideValues()
        {
            var id = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            if (!CProfiles.IsProfileIdValid(id))
            {
                return;
            }

            if (_SelectSlides[_SelectSlideDifficulty].Selected)
            {
                var difficulty = (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection;
                if (difficulty != CProfiles.GetDifficulty(id))
                {
                    CProfiles.SetDifficulty(id, difficulty);
                    _UnsavedChanges = true;
                }
            }
            else if (_SelectSlides[_SelectSlideAvatars].Selected)
            {
                var avatarId = _SelectSlides[_SelectSlideAvatars].SelectedTag;
                if (avatarId != CProfiles.GetAvatarId(id))
                {
                    CProfiles.SetAvatar(id, avatarId);
                    _UnsavedChanges = true;
                    if (_WebcamTexture != null || CWebcam.IsCapturing())
                    {
                        _ResetSnapshot();
                    }
                }
            }
            else if (_SelectSlides[_SelectSlideUserRole].Selected)
            {
                var userRole = (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection;
                if (userRole != CProfiles.GetUserRoleProfile(id))
                {
                    CProfiles.SetUserRoleProfile(id, userRole);
                    _UnsavedChanges = true;
                }
            }
            else if (_SelectSlides[_SelectSlideActive].Selected)
            {
                var active = (EOffOn)_SelectSlides[_SelectSlideActive].Selection;
                if (active != CProfiles.GetActive(id))
                {
                    CProfiles.SetActive(id, active);
                    _UnsavedChanges = true;
                    _NameSelections[_NameSelection].UpdateList();
                }
            }
        }

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Avatar == (EProfileChangedFlags.Avatar & flags))
            {
                _AvatarsChanged = true;
            }

            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
            {
                _ProfilesChanged = true;
            }
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
                {
                    _Statics[_StaticAvatar].Texture = _WebcamTexture;
                }

                _Buttons[_ButtonSaveSnapshot].Visible = true;
                _Buttons[_ButtonDiscardSnapshot].Visible = true;
                _Buttons[_ButtonTakeSnapshot].Visible = false;
                _Buttons[_ButtonWebcam].Visible = false;

                _SelectElement(_Buttons[_ButtonSaveSnapshot]);
            }
        }

        private void _OnDiscardSnapshot()
        {
            _ResetSnapshot();
            _SelectElement(_Buttons[_ButtonWebcam]);
        }

        private void _ResetSnapshot()
        {
            if (CWebcam.IsCapturing())
            {
                CWebcam.Stop();
            }

            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();
        }

        private void _OnSaveSnapshot()
        {
            var file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CConfig.ProfileFolders[0]), "snapshot.png");
            _Snapshot.Save(file, ImageFormat.Png);

            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();

            var id = CProfiles.NewAvatar(file);
            CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), id);
            _UnsavedChanges = true;
            _LoadAvatars(false);
            _NameSelections[_NameSelection].UpdateList();

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
            _EndNameEdit(false);
            var id = CProfiles.NewProfile();
            _LoadProfiles(false);
            var num = CProfiles.NumProfiles;
            _SelectSlides[_SelectSlideProfiles].SelectedTag = num;
            _SelectSlideGuids.Add(num, id);

            CProfiles.SetAvatar(id, _SelectSlides[_SelectSlideAvatars].SelectedTag);

            _NameSelections[_NameSelection].UpdateList();
            _SelectElement(_Buttons[_ButtonPlayerName]);
            _UnsavedChanges = true;
            _NameBeforeEdit = string.Empty;
            _EditMode = EEditMode.PlayerName;
        }

        private void _SaveProfiles()
        {
            _EndNameEdit(false);
            CProfiles.SaveProfiles();
            _UnsavedChanges = false;
            _LoadProfiles(true);
            _NameSelections[_NameSelection].UpdateList();
        }

        private void _DeleteProfile()
        {
            _EndNameEdit(false);

            CProfiles.DeleteProfile(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
            _SelectSlideGuids.Remove(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            _NameSelections[_NameSelection].UpdateList();

            var selection = _SelectSlides[_SelectSlideProfiles].Selection;
            if (_SelectSlides[_SelectSlideProfiles].NumValues - 1 > selection)
            {
                _SelectSlides[_SelectSlideProfiles].Selection = selection + 1;
            }
            else
            {
                _SelectSlides[_SelectSlideProfiles].Selection = selection - 1;
            }
        }

        private void _LoadProfiles(bool keep)
        {
            var name = string.Empty;
            if (_EditMode == EEditMode.PlayerName)
            {
                name = CProfiles.GetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
            }

            var selectedProfileId = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
            _SelectSlides[_SelectSlideProfiles].Clear();
            _SelectSlideGuids.Clear();

            var profiles = CProfiles.GetProfiles();
            var i = 0;
            foreach (var profile in profiles)
            {
                _SelectSlides[_SelectSlideProfiles].AddValue(profile.PlayerName, null, i);
                _SelectSlideGuids.Add(i, profile.Id);
                i++;
            }

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                if (selectedProfileId != Guid.Empty)
                {
                    _SelectSlides[_SelectSlideProfiles].SelectedTag = _SelectSlideGuids.FirstOrDefault(x => x.Value.Equals(selectedProfileId)).Key;
                }
                else
                {
                    _SelectSlides[_SelectSlideProfiles].Selection = 0;
                    selectedProfileId = _GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                }

                if (!keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileId);
                    _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(selectedProfileId);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileId);
                    _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarId(selectedProfileId);
                }

                if (_EditMode == EEditMode.PlayerName)
                {
                    CProfiles.SetPlayerName(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), name);
                    _NameSelections[_NameSelection].UpdateList();
                }
            }

            _ProfilesChanged = false;
        }

        private void _LoadAvatars(bool keep)
        {
            var selectedAvatarId = _SelectSlides[_SelectSlideAvatars].SelectedTag;
            _SelectSlides[_SelectSlideAvatars].Clear();
            var avatars = CProfiles.GetAvatars();
            if (avatars != null)
            {
                foreach (var avatar in avatars)
                {
                    _SelectSlides[_SelectSlideAvatars].AddValue(avatar.GetDisplayName(), null, avatar.Id);
                }
            }

            if (keep)
            {
                _SelectSlides[_SelectSlideAvatars].SelectedTag = selectedAvatarId;
                CProfiles.SetAvatar(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag), selectedAvatarId);
            }
            else
            {
                _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarId(_GetIdFromTag(_SelectSlides[_SelectSlideProfiles].SelectedTag));
            }

            _AvatarsChanged = false;
        }

        private void _SelectProfileById(Guid profileId)
        {
            if (profileId == Guid.Empty)
            {
                return;
            }

            _EndNameEdit(false);
            var entry = _SelectSlideGuids.FirstOrDefault(x => x.Value.Equals(profileId));
            if (_SelectSlideGuids.ContainsKey(entry.Key))
            {
                _SelectSlides[_SelectSlideProfiles].SelectedTag = entry.Key;
            }
        }

        private Guid _GetIdFromTag(int tag)
        {
            if (tag == -1 || !_SelectSlideGuids.ContainsKey(tag))
            {
                return Guid.Empty;
            }
            else
            {
                return _SelectSlideGuids[tag];
            }
        }
    }
}