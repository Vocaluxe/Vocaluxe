#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using System.Runtime.ExceptionServices;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.Server;

namespace Vocaluxe
{
    static class CMainProgram
    {
#if WIN
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
#endif

        private static CSplashScreen _SplashScreen;

        [STAThread, HandleProcessCorruptedStateExceptions]
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
            // ReSharper restore InconsistentNaming
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolver;
            // Close program if there is another instance running
            if (!_EnsureSingleInstance())
                return;
            try
            {
                _Run(args);
            }
            catch (Exception e)
            {
                string stackTrace = "";
                try
                {
                    stackTrace = e.StackTrace;
                }
                catch {}
                MessageBox.Show("Unhandled error: " + e.Message + stackTrace);
                CLog.LogError("Unhandled error: " + e.Message + stackTrace);
            }
            _CloseProgram();
        }

        private static void _Run(string[] args)
        {
            Application.DoEvents();

            try
            {
                // Init Log
                CLog.Init();

                if (!CProgrammHelper.CheckRequirements())
                    return;

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
                _SplashScreen = new CSplashScreen();
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
                CSound.Init();
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

                // Init Server
                CLog.StartBenchmark(0, "Init Server");
                CVocaluxeServer.Init();
                CLog.StopBenchmark(0, "Init Server");

                Application.DoEvents();

                // Init Input
                CLog.StartBenchmark(0, "Init Input");
                CController.Init();
                CController.Connect();
                CLog.StopBenchmark(0, "Init Input");

                Application.DoEvents();

                // Init Game;
                CLog.StartBenchmark(0, "Init Game");
                CGame.Init();
                CProfiles.Update();
                CConfig.UsePlayers();
                CLog.StopBenchmark(0, "Init Game");

                Application.DoEvents();

                // Init Party Modes;
                CLog.StartBenchmark(0, "Init Party Modes");
                CParty.Init();
                CLog.StopBenchmark(0, "Init Party Modes");

                Application.DoEvents();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error on start up: " + e.Message + e.StackTrace);
                CLog.LogError("Error on start up: " + e);
                _SplashScreen.Close();
                _CloseProgram();
            }
            Application.DoEvents();

            // Start Main Loop
            _SplashScreen.Close();
            CVocaluxeServer.Start();

            CDraw.MainLoop();
        }

        private static void _CloseProgram()
        {
            // Unloading
            try
            {
                CVocaluxeServer.Close();
                CController.Close();
                CSound.RecordCloseAll();
                CSound.CloseAllStreams();
                CVideo.CloseAll();
                CDraw.Unload();
                CDataBase.CloseConnections();
                CWebcam.Close();
                CLog.CloseAll();
            }
            catch (Exception) {}
            Environment.Exit(Environment.ExitCode);
        }

        [HandleProcessCorruptedStateExceptions]
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            string stackTrace = "";
            try
            {
                stackTrace = e.StackTrace;
            }
            catch {}
            MessageBox.Show("Unhandled exception: " + e.Message + stackTrace);
            CLog.LogError("Unhandled exception: " + e.Message + stackTrace);
            _CloseProgram();
        }

        private static Assembly _AssemblyResolver(Object sender, ResolveEventArgs args)
        {
            // a fix to handle mscorlib.resources
            if (args.Name.IndexOf(".resources", StringComparison.Ordinal) >= 0)
                return null;

            string[] arr = args.Name.Split(new char[] {','});
            if (arr.Length > 0)
            {
#if ARCH_X86
                string path = "x86";
#endif

#if ARCH_X64
                string path = "x64";
#endif
                path = Path.Combine(path, arr[0] + ".dll");
                try
                {
                    Assembly assembly;
                    try
                    {
                        assembly = Assembly.LoadFrom(path);
                    }
                    catch (FileLoadException)
                    {
                        //possibly loading from network, this would work always but better use the normal LoadFrom for more specific errors
                        assembly = Assembly.Load(File.ReadAllBytes(path));
                    }
                    return assembly;
                }
                catch (Exception e)
                {
                    CLog.LogError("Cannot load assembly " + args.Name + " from " + path + ": " + e);
                }
            }
            return null;
        }

        private static readonly Mutex _Mutex = new Mutex(false, Application.ProductName + "-SingleInstanceMutex");

        private static bool _EnsureSingleInstance()
        {
            // wait a few seconds in case that the instance is just shutting down
            if (!_Mutex.WaitOne(TimeSpan.FromSeconds(2), false))
            {
                //TODO: put it into language file
                MessageBox.Show("Another Instance of Vocaluxe is already runnning!");
#if WIN
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes.Where(t => t.Id != currentProcess.Id))
                {
                    var wnd = process.MainWindowHandle;
                    if (wnd != IntPtr.Zero)
                        SetForegroundWindow(wnd);
                }
#endif
                return false;
            }
            return true;
        }
    }
}