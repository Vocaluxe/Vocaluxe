using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
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
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolver);

            // Close program if there is another instance running
            if (!EnsureSingleInstance())
            {
                //TODO: put it into language file
                MessageBox.Show("Another Instance of Vocaluxe is already runnning!");
                return;
            }

            Application.DoEvents();

            try
            {
                // Init Log
                CLog.Init();

                CMain.Init();
                CSettings.CreateFolders();
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

                //Init Webcam
                CLog.StartBenchmark(0, "Init Webcam");
                CWebcam.Init();
                CLog.StopBenchmark(0, "Init Webcam");

                Application.DoEvents();

                // Init Background Music
                CLog.StartBenchmark(0, "Init Background Music");
                CBackgroundMusic.Init();
                CLog.StopBenchmark(0, "Init Background Music");

                Application.DoEvents();

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

                // Init Input
                CLog.StartBenchmark(0, "Init Input");
                CInput.Init();
                CLog.StopBenchmark(0, "Init Input");

                // Init Game;
                CLog.StartBenchmark(0, "Init Game");
                CGame.Init();
                CLog.StopBenchmark(0, "Init Game");

                // Init Party Modes;
                CLog.StartBenchmark(0, "Init Party Modes");
                CParty.Init();
                CLog.StopBenchmark(0, "Init Party Modes");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error on start up: " + e.Message + e.StackTrace);
                CLog.LogError("Error on start up: " + e.Message + e.StackTrace);
                CloseProgram();
                Environment.Exit(Environment.ExitCode);
            }
            Application.DoEvents();

            // Start Main Loop
            _SplashScreen.Close();

            try
            {
                CDraw.MainLoop();
            }
            catch (Exception e)
            {
                MessageBox.Show("Unhandled error: " + e.Message + e.StackTrace);
                CLog.LogError("Unhandled error: " + e.Message + e.StackTrace);
            }

            CloseProgram();
        }

        static void CloseProgram()
        {
            // Unloading
            try
            {
                CInput.Close();
                CSound.RecordCloseAll();
                CSound.CloseAllStreams();
                CVideo.VdCloseAll();
                CDraw.Unload();
                CLog.CloseAll();
                CDataBase.CloseConnections();
                CWebcam.Close();
            }
            catch (Exception)
            {
            }
            
        }

        static Assembly AssemblyResolver(Object sender, ResolveEventArgs args)
        {
            string[] arr = args.Name.Split(new Char[] { ',' });
            if (arr != null)
            {
#if ARCH_X86
                Assembly assembly = Assembly.LoadFrom(Path.Combine("x86", arr[0] + ".dll"));
#endif

#if ARCH_X64
                Assembly assembly = Assembly.LoadFrom(Path.Combine("x64", arr[0] + ".dll"));
#endif

                return assembly;
            }
            return null;
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
        Bitmap logo;

        public SplashScreen()
        {
            string path = Path.Combine(Environment.CurrentDirectory, Path.Combine(CSettings.sFolderGraphics, CSettings.sLogo));
            if (File.Exists(path))
            {
                try
                {
                    logo = new Bitmap(path);
                    this.ClientSize = new Size(logo.Width, logo.Height);
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading logo: " + e.Message);
                }
                
            }
            else
                CLog.LogError("Can't find " + path);

            path = Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon);
            if (File.Exists(path))
            {
                try
                {
                    this.Icon = new System.Drawing.Icon(path);
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading icon: " + e.Message);                    
                }
            }
            else
                CLog.LogError("Can't find " + path);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;         
            this.Text = CSettings.sProgramName;
            this.CenterToScreen();
            this.Show();
        }
        
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {          
        }

        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e)
        {
            if (logo == null)
                return;

            Graphics g = e.Graphics;
            g.DrawImage(logo, new Rectangle(0, 0, this.Width, this.Height));
        }       
    }
}
