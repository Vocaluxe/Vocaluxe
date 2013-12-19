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

using ServerLib;
using Vocaluxe.Base;
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

        private CTexture _QRServerAddress;
        private CTexture _QRAndroidLink;
        private CTexture _QRSymbianLink;
        private CTexture _QRWebOSLink;
        private CTexture _QRWindowsPhoneLink;

        private const string _StaticQRServer = "StaticQRServer";
        private const string _StaticQRAndroid = "StaticQRAndroid";
        private const string _StaticQRSymbian = "StaticQRSymbian";
        private const string _StaticQRWebOS = "StaticQRWebOS";
        private const string _StaticQRWindowsPhone = "StaticQRWindowsPhone";

        private const string _TextServerAddress = "TextServerAddress";

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] { _StaticQRServer, _StaticQRAndroid, _StaticQRSymbian, _StaticQRWebOS, _StaticQRWindowsPhone };
            _ThemeTexts = new string[] { _TextServerAddress };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _GenerateQRs();
            _Statics[_StaticQRServer].Texture = _QRServerAddress;
            _Statics[_StaticQRAndroid].Texture = _QRAndroidLink;
            _Statics[_StaticQRSymbian].Texture = _QRSymbianLink;
            _Statics[_StaticQRWebOS].Texture = _QRWebOSLink;
            _Statics[_StaticQRWindowsPhone].Texture = _QRWindowsPhoneLink;
            _Texts[_TextServerAddress].Text = CServer.GetLocalAddress() + ":" + CConfig.ServerPort;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupServerQR);
                return true;
            }
            return false;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;

            return base.Draw();
        }

        private void _GenerateQRs()
        {
            QRCodeGenerator qr = new QRCodeGenerator();

            //ServerAddress
            QRCodeGenerator.QRCode qrcode = qr.CreateQrCode(CServer.GetLocalAddress() + ":" + CConfig.ServerPort, QRCodeGenerator.ECCLevel.H);
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
