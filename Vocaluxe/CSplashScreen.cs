using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;

namespace Vocaluxe
{
    class CSplashScreen : Form
    {
        private readonly Bitmap _Logo;

        public CSplashScreen()
        {
            string path = Path.Combine(Environment.CurrentDirectory, Path.Combine(CSettings.FolderGraphics, CSettings.Logo));
            if (File.Exists(path))
            {
                try
                {
                    _Logo = new Bitmap(path);
                    ClientSize = new Size(_Logo.Width, _Logo.Height);
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading logo: " + e.Message);
                }
            }
            else
                CLog.LogError("Can't find " + path);

            path = Path.Combine(Environment.CurrentDirectory, CSettings.Icon);
            if (File.Exists(path))
            {
                try
                {
                    Icon = new Icon(path);
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading icon: " + e.Message);
                }
            }
            else
                CLog.LogError("Can't find " + path);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            FormBorderStyle = FormBorderStyle.None;
            Text = CSettings.ProgramName;
            CenterToScreen();
            Show();
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