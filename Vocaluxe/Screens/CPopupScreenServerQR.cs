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

using System.Windows.Forms;
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
            get { return 1; }
        }

        private CTextureRef _QRServerAddress;
        private CTextureRef _QRAndroidLink;
        private CTextureRef _QRSymbianLink;
        private CTextureRef _QRWebOSLink;
        private CTextureRef _QRWindowsPhoneLink;

        private const string _StaticQRServer = "StaticQRServer";
        private const string _StaticQRAndroid = "StaticQRAndroid";
        private const string _StaticQRSymbian = "StaticQRSymbian";
        private const string _StaticQRWebOS = "StaticQRWebOS";
        private const string _StaticQRWindowsPhone = "StaticQRWindowsPhone";

        private const string _TextServerAddress = "TextServerAddress";
        private const string _TextServerNotRunning = "TextServerNotRunning";

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticQRServer, _StaticQRAndroid, _StaticQRSymbian, _StaticQRWebOS, _StaticQRWindowsPhone};
            _ThemeTexts = new string[] {_TextServerAddress, _TextServerNotRunning};
        }

        public override void OnShow()
        {
            base.OnShow();
            if (_QRServerAddress == null)
            {
                _GenerateQRs();
                _Statics[_StaticQRServer].Texture = _QRServerAddress;
                _Statics[_StaticQRAndroid].Texture = _QRAndroidLink;
                _Statics[_StaticQRSymbian].Texture = _QRSymbianLink;
                _Statics[_StaticQRWebOS].Texture = _QRWebOSLink;
                _Statics[_StaticQRWindowsPhone].Texture = _QRWindowsPhoneLink;
                _Texts[_TextServerAddress].Text = CVocaluxeServer.GetServerAddress();
            }
            _Texts[_TextServerAddress].Visible = CVocaluxeServer.IsServerRunning();
            _Statics[_StaticQRServer].Visible = CVocaluxeServer.IsServerRunning();
            _Texts[_TextServerNotRunning].Visible = !CVocaluxeServer.IsServerRunning();
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
            QRCodeGenerator qr = new QRCodeGenerator();

            //ServerAddress
            QRCodeGenerator.QRCode qrcode = qr.CreateQrCode(CVocaluxeServer.GetServerAddress(), QRCodeGenerator.ECCLevel.H);
            _QRServerAddress = CDraw.AddTexture(qrcode.GetGraphic(20));

            //Android
            qrcode = qr.CreateQrCode(CSettings.LinkAndroidApp, QRCodeGenerator.ECCLevel.H);
            _QRAndroidLink = CDraw.AddTexture(qrcode.GetGraphic(20));

            //Symbian
            qrcode = qr.CreateQrCode(CSettings.LinkSymbianApp, QRCodeGenerator.ECCLevel.H);
            _QRSymbianLink = CDraw.AddTexture(qrcode.GetGraphic(20));

            //WebOS
            qrcode = qr.CreateQrCode(CSettings.LinkWebOSApp, QRCodeGenerator.ECCLevel.H);
            _QRWebOSLink = CDraw.AddTexture(qrcode.GetGraphic(20));

            //WindowsPhone
            qrcode = qr.CreateQrCode(CSettings.LinkWindowsPhoneApp, QRCodeGenerator.ECCLevel.H);
            _QRWindowsPhoneLink = CDraw.AddTexture(qrcode.GetGraphic(20));
        }
    }
}