using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe
{
    // just a small comment for the new develop branch
    static class MainProgram
    {
        static SplashScreen _SplashScreen;

        [STAThread]
        static void Main(string[] args)
        {
            // Close program if there is another instance running
            if (!EnsureSingleInstance())
            {
                //TODO: put it into language file
                MessageBox.Show("Another Instance of Vocaluxe is already runnning!");
                return;
            }

            Application.DoEvents();

            // Init Log
            CLog.Init();

            Application.DoEvents();

            // Init Language
            CLog.StartBenchmark(0, "Init Language");
            CLanguage.Init();
            CLog.StopBenchmark(0, "Init Language");

            Application.DoEvents();

            // load config
            CLog.StartBenchmark(0, "Init Config");
            CConfig.LoadCommandLineParams(args);
            CConfig.UseCommandLineParamsBefore();
            CConfig.Init();
            CConfig.UseCommandLineParamsAfter();
            CLog.StopBenchmark(0, "Init Config");

            Application.DoEvents();
            _SplashScreen = new SplashScreen();
            Application.DoEvents();

            // Init Draw
            CLog.StartBenchmark(0, "Init Draw");
            CDraw.InitDraw();
            CLog.StopBenchmark(0, "Init Draw");

            Application.DoEvents();

            // Init Database
            CLog.StartBenchmark(0, "Init Database");
            CDataBase.Init();
            CLog.StopBenchmark(0, "Init Database");

            Application.DoEvents();

            // Init Playback
            CLog.StartBenchmark(0, "Init Playback");
            CSound.PlaybackInit();
            CLog.StopBenchmark(0, "Init Playback");

            Application.DoEvents();

            // Init Record
            CLog.StartBenchmark(0, "Init Record");
            CSound.RecordInit();
            CLog.StopBenchmark(0, "Init Record");

            Application.DoEvents();

            // Init Background Music
            CLog.StartBenchmark(0, "Init Background Music");
            CBackgroundMusic.init();
            CLog.StopBenchmark(0, "Init Background Music");

            // Init Profiles
            CLog.StartBenchmark(0, "Init Profiles");
            CProfiles.Init();
            CLog.StopBenchmark(0, "Init Profiles");

            Application.DoEvents();

            // Init Font
            CLog.StartBenchmark(0, "Init Font");
            CFonts.Init();
            CLog.StopBenchmark(0, "Init Font");

            Application.DoEvents();

            // Init VideoDecoder
            CLog.StartBenchmark(0, "Init Videodecoder");
            CVideo.Init();
            CLog.StopBenchmark(0, "Init Videodecoder");

            Application.DoEvents();

            // Load Cover
            CLog.StartBenchmark(0, "Init Cover");
            CCover.Init();
            CLog.StopBenchmark(0, "Init Cover");

            Application.DoEvents();

            // Theme System
            CLog.StartBenchmark(0, "Init Theme");
            CTheme.InitTheme();
            CLog.StopBenchmark(0, "Init Theme");

            Application.DoEvents();

            // Init Screens
            CLog.StartBenchmark(0, "Init Screens");
            CGraphics.InitGraphics();
            CLog.StopBenchmark(0, "Init Screens");

            Application.DoEvents();

            // Init Game;
            CLog.StartBenchmark(0, "Init Game");
            CGame.Init();
            CLog.StopBenchmark(0, "Init Game");

            Application.DoEvents();

            // Start Main Loop
            _SplashScreen.Close();
            CDraw.MainLoop();

            // Unloading
            CSound.RecordCloseAll();
            CSound.CloseAllStreams();
            CVideo.VdCloseAll();
            CDraw.Unload();
            CLog.CloseAll();
        }

        static bool EnsureSingleInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcesses();

            if (processes == null)
                return true;

            for (int i = 0; i < processes.Length; i++)
            {
                if (processes[i].Id != currentProcess.Id && currentProcess.ProcessName == processes[i].ProcessName)
                    return false;
            }

            return true;
        }
    }

    class SplashScreen : Form
    {
        public SplashScreen()
        {
            Bitmap logo = new Bitmap(Path.Combine(Environment.CurrentDirectory, Path.Combine("Graphics", "logo.png")));
            this.Icon = new System.Drawing.Icon(Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ClientSize = new Size(logo.Width, logo.Height);
            this.BackgroundImage = logo;
            this.Text = CSettings.sProgramName;
            this.CenterToScreen();
            this.Show();
        }
    }
}
