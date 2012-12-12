using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu
{
    interface IMenu
    {
        void LoadTheme();
        void SaveTheme();
        void ReloadTextures();
        void UnloadTextures();
        void ReloadTheme();

        bool HandleInput(KeyEvent KeyEvent);
        bool HandleMouse(MouseEvent MouseEvent);
        bool HandleInputThemeEditor(KeyEvent KeyEvent);
        bool HandleMouseThemeEditor(MouseEvent MouseEvent);

        bool UpdateGame();
        void ApplyVolume();
        void OnShow();
        void OnShowFinish();
        void OnClose();

        bool Draw();
        SRectF GetScreenArea();

        void NextInteraction();
        void PrevInteraction();

        bool NextElement();
        bool PrevElement();

        void ProcessMouseClick(int x, int y);
        void ProcessMouseMove(int x, int y);
    }
}
