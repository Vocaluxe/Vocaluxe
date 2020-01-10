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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe
{
    class CSplashScreen : Form
    {
        private readonly Bitmap _Logo;

        public CSplashScreen()
        {
            string path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameGraphics, CSettings.FileNameLogo);
            if (File.Exists(path))
            {
                try
                {
                    _Logo = new Bitmap(path);
                    ClientSize = new Size(_Logo.Width, _Logo.Height);
                }
                catch (Exception e)
                {
                    CLog.Error("Error loading logo: " + e.Message);
                }
            }
            else
                CLog.Error("Can't find " + path);

            path = Path.Combine(CSettings.ProgramFolder, CSettings.FileNameIcon);
            if (File.Exists(path))
            {
                try
                {
                    Icon = new Icon(path);
                }
                catch (Exception e)
                {
                    CLog.Error("Error loading icon: " + e.Message);
                }
            }
            else
                CLog.Error("Can't find " + path);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            FormBorderStyle = FormBorderStyle.None;
            Text = CSettings.ProgramName;
            CenterToScreen();
            Show();
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public override sealed Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        protected override void OnPaint(PaintEventArgs e) {}

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_Logo == null)
                return;

            Graphics g = e.Graphics;
            g.DrawImage(_Logo, new Rectangle(0, 0, Width, Height));
        }
    }
}