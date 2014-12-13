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

namespace VocaluxeLib.Menu
{
    public interface IMenu
    {
        int PartyModeID { get; }

        void Init();

        void LoadTheme(string xmlPath);
        void SaveTheme();
        void ReloadSkin();
        void UnloadSkin();
        void ReloadTheme(string xmlPath);

        bool HandleInput(SKeyEvent keyEvent);
        bool HandleMouse(SMouseEvent mouseEvent);
        bool HandleInputThemeEditor(SKeyEvent keyEvent);
        bool HandleMouseThemeEditor(SMouseEvent mouseEvent);

        bool UpdateGame();
        void Draw();
        void ApplyVolume();
        void OnShow();
        void OnShowFinish();
        void OnClose();

        SRectF ScreenArea { get; }
        /// <summary>
        ///     Type of currently playing music
        /// </summary>
        EMusicType CurrentMusicType { get; }

        void NextElement();
        void PrevElement();

        bool NextValue();
        bool PrevValue();

        void ProcessMouseClick(int x, int y);
        void ProcessMouseMove(int x, int y);
    }
}