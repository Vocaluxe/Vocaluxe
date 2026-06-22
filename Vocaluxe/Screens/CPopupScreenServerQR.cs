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

using Vocaluxe.Base;
using Vocaluxe.Base.Server;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Draw;
using QRCoder;

namespace Vocaluxe.Screens
{
    class CPopupScreenServerQR : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private CTextureRef _QRServerAddress;
        private const string _StaticQRServer = "StaticQRServer";

        private const string _TextServerAddress = "TextServerAddress";
        private const string _TextServerNotRunning = "TextServerNotRunning";

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticQRServer};
            _ThemeTexts = new string[] {_TextServerAddress, _TextServerNotRunning};
        }

        public override void OnShow()
        {
            base.OnShow();
            if (_QRServerAddress == null)
            {
                _GenerateQRs();
                _Statics[_StaticQRServer].Texture = _QRServerAddress;
                _Texts[_TextServerAddress].Text = CVocaluxeServer.GetServerAddress();
            }
            _Texts[_TextServerAddress].Visible = CVocaluxeServer.IsServerRunning();
            _Statics[_StaticQRServer].Visible = CVocaluxeServer.IsServerRunning();
            _Texts[_TextServerNotRunning].Visible = !CVocaluxeServer.IsServerRunning();

            // When the server is enabled but failed to start, show the (translated) reason - e.g.
            // "port in use" - instead of the generic "not running" text.
            string statusKey = CVocaluxeServer.GetStatusKey();
            if (!CVocaluxeServer.IsServerRunning() && !string.IsNullOrEmpty(statusKey))
                _Texts[_TextServerNotRunning].Text = CLanguage.Translate(statusKey).Replace("%d", CConfig.Config.Server.ServerPort.ToString());
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB || mouseEvent.RB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupServerQR);
                return true;
            }
            return false;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Keys.Escape:
                case Keys.Back:
                    CGraphics.HidePopup(EPopupScreens.PopupServerQR);
                    return true;
            }
            return base.HandleInput(keyEvent);
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _GenerateQRs()
        {
            // TODO(linux-port): QRCoder's GetGraphic returns a GDI+ Bitmap (Windows-only). Re-implement
            // via a cross-platform renderer (PngByteQRCode -> texture) once the ASP.NET Core webserver
            // (S2) is back; the remote-control server is disabled for now so there is nothing to encode.
        }
    }
}