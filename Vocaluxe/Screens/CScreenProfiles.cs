using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

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
        const int ScreenVersion = 1;

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

        private const string StaticAvatar = "StaticAvatar";

        private EEditMode _EditMode;
        
        public CScreenProfiles()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenProfiles";
            _ScreenVersion = ScreenVersion;
            _ThemeButtons = new string[] { ButtonPlayerName, ButtonExit, ButtonSave, ButtonNew, ButtonDelete };
            _ThemeSelectSlides = new string[] { SelectSlideProfiles, SelectSlideDifficulty, SelectSlideAvatars, SelectSlideGuestProfile, SelectSlideActive };
            _ThemeStatics = new string[] { StaticAvatar };

            _EditMode = EEditMode.None;
        }

        public override void LoadTheme()
        {
            base.LoadTheme();

            SelectSlides[htSelectSlides(SelectSlideDifficulty)].SetValues<EGameDifficulty>(0);
            SelectSlides[htSelectSlides(SelectSlideGuestProfile)].SetValues<EOffOn>(0);
            SelectSlides[htSelectSlides(SelectSlideActive)].SetValues<EOffOn>(0);
            
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
                        SelectSlides[htSelectSlides(SelectSlideProfiles)].RenameValue(
                            CProfiles.AddGetPlayerName(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection, KeyEvent.Unicode));
                        break;
                    default:
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
                        if (Buttons[htButtons(ButtonExit)].Selected)
                        {
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        } else if (Buttons[htButtons(ButtonSave)].Selected)
                        {
                            SaveProfiles();
                        } else if (Buttons[htButtons(ButtonNew)].Selected)
                        {
                            NewProfile();
                        } else if (Buttons[htButtons(ButtonPlayerName)].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        } else if (Buttons[htButtons(ButtonDelete)].Selected)
                        {
                            DeleteProfile();
                        }
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            SelectSlides[htSelectSlides(SelectSlideProfiles)].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection));
                        }
                        else
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Delete:
                        DeleteProfile();
                        break;
                }
                if (SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EGameDifficulty)SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideAvatars)].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        SelectSlides[htSelectSlides(SelectSlideAvatars)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EOffOn)SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideActive)].Selected)
                {
                    CProfiles.SetActive(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EOffOn)SelectSlides[htSelectSlides(SelectSlideActive)].Selection);
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
                if (Buttons[htButtons(ButtonExit)].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenMain);
                } else if (Buttons[htButtons(ButtonSave)].Selected)
                {
                    SaveProfiles();
                } else if (Buttons[htButtons(ButtonNew)].Selected)
                {
                    NewProfile();
                } else if (Buttons[htButtons(ButtonDelete)].Selected) {
                    DeleteProfile();
                } else if (Buttons[htButtons(ButtonPlayerName)].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                } else if (SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selected)
                {
                    CProfiles.SetDifficulty(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EGameDifficulty)SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideAvatars)].Selected)
                {
                    CProfiles.SetAvatar(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        SelectSlides[htSelectSlides(SelectSlideAvatars)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selected)
                {
                    CProfiles.SetGuestProfile(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EOffOn)SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selection);
                } else if (SelectSlides[htSelectSlides(SelectSlideActive)].Selected)
                {
                    CProfiles.SetActive(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                        (EOffOn)SelectSlides[htSelectSlides(SelectSlideActive)].Selection);
                }
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection > -1)
            {
                Buttons[htButtons(ButtonPlayerName)].Text.Text = CProfiles.GetPlayerName(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                if (_EditMode == EEditMode.PlayerName)
                    Buttons[htButtons(ButtonPlayerName)].Text.Text += "|";

                SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selection = (int)CProfiles.GetDifficulty(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideActive)].Selection = (int)CProfiles.GetActive(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);

                int avatarNr = CProfiles.GetAvatarNr(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideAvatars)].Selection = avatarNr;
                Statics[htStatics(StaticAvatar)].Texture = CProfiles.Avatars[avatarNr].Texture;
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
            CProfiles.DeleteProfile(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
            LoadProfiles();
            UpdateGame();
        }

        private void LoadProfiles()
        {
            _EditMode = EEditMode.None;
            SelectSlides[htSelectSlides(SelectSlideProfiles)].Clear();
 
            for (int i = 0; i < CProfiles.NumProfiles; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideProfiles)].AddValue(CProfiles.GetPlayerName(i));
            }

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                SelectSlides[htSelectSlides(SelectSlideDifficulty)].Selection = (int)CProfiles.GetDifficulty(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideGuestProfile)].Selection = (int)CProfiles.GetGuestProfile(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideActive)].Selection = (int)CProfiles.GetActive(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
                SelectSlides[htSelectSlides(SelectSlideAvatars)].Selection = CProfiles.GetAvatarNr(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection);
            }
        }

        private void LoadAvatars()
        {
            SelectSlides[htSelectSlides(SelectSlideAvatars)].Clear();
            for (int i = 0; i < CProfiles.NumAvatars; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideAvatars)].AddValue(CProfiles.Avatars[i].FileName);
            }
        }

        private void NewProfile()
        {
            _EditMode = EEditMode.None;
            CProfiles.NewProfile();
            LoadProfiles();
            SelectSlides[htSelectSlides(SelectSlideProfiles)].LastValue();
          
            CProfiles.SetAvatar(SelectSlides[htSelectSlides(SelectSlideProfiles)].Selection,
                SelectSlides[htSelectSlides(SelectSlideAvatars)].Selection);

            SetInteractionToButton(Buttons[htButtons(ButtonPlayerName)]);
            _EditMode = EEditMode.PlayerName;
        }        
    }
}
