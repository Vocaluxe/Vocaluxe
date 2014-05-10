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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.Server;

namespace Vocaluxe
{
    class CLoadingException : Exception
    {
        public CLoadingException(string component)
            : base("Failed to load " + component) {}
    }

    static class CMainProgram
    {
        private static CSplashScreen _SplashScreen;

        [STAThread, HandleProcessCorruptedStateExceptions]
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
            // ReSharper restore InconsistentNaming
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
#endif
            AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolver;
            // Close program if there is another instance running
            if (!_EnsureSingleInstance())
                return;
#if !DEBUG 
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
#else
            _Run(args);
#endif
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
                CProgrammHelper.Init();

                CMain.Init();
                Application.DoEvents();

                // Init Language
                CLog.StartBenchmark("Init Language");
                CLanguage.Init();
                CLog.StopBenchmark("Init Language");

                Application.DoEvents();

                // load config
                CLog.StartBenchmark("Init Config");
                CConfig.LoadCommandLineParams(args);
                CConfig.UseCommandLineParamsBefore();
                CConfig.Init();
                CConfig.UseCommandLineParamsAfter();
                CLog.StopBenchmark("Init Config");

                // Create folders
                CSettings.CreateFolders();

                _SplashScreen = new CSplashScreen();
                Application.DoEvents();

                // Init Draw
                CLog.StartBenchmark("Init Draw");
                if (!CDraw.Init())
                    throw new CLoadingException("drawing");
                CLog.StopBenchmark("Init Draw");

                Application.DoEvents();

                // Init Playback
                CLog.StartBenchmark("Init Playback");
                if (!CSound.Init())
                    throw new CLoadingException("playback");
                CLog.StopBenchmark("Init Playback");

                Application.DoEvents();

                // Init Record
                CLog.StartBenchmark("Init Record");
                if (!CRecord.Init())
                    throw new CLoadingException("record");
                CLog.StopBenchmark("Init Record");

                Application.DoEvents();

                // Init VideoDecoder
                CLog.StartBenchmark("Init Videodecoder");
                if (!CVideo.Init())
                    throw new CLoadingException("video");
                CLog.StopBenchmark("Init Videodecoder");

                Application.DoEvents();

                // Init Database
                CLog.StartBenchmark("Init Database");
                if (!CDataBase.Init())
                    throw new CLoadingException("database");
                CLog.StopBenchmark("Init Database");

                Application.DoEvents();

                //Init Webcam
                CLog.StartBenchmark("Init Webcam");
                if (!CWebcam.Init())
                    throw new CLoadingException("webcam");
                CLog.StopBenchmark("Init Webcam");

                Application.DoEvents();

                // Init Background Music
                CLog.StartBenchmark("Init Background Music");
                CBackgroundMusic.Init();
                CLog.StopBenchmark("Init Background Music");

                Application.DoEvents();

                // Init Profiles
                CLog.StartBenchmark("Init Profiles");
                CProfiles.Init();
                CLog.StopBenchmark("Init Profiles");

                Application.DoEvents();

                // Init Font
                CLog.StartBenchmark("Init Font");
                CFonts.Init();
                CLog.StopBenchmark("Init Font");

                Application.DoEvents();

                // Load Cover
                CLog.StartBenchmark("Init Cover");
                CCover.Init();
                CLog.StopBenchmark("Init Cover");

                Application.DoEvents();

                // Theme System
                CLog.StartBenchmark("Init Theme");
                if (!CTheme.Init())
                    throw new CLoadingException("theme");
                CLog.StopBenchmark("Init Theme");

                Application.DoEvents();

                // Init Screens
                CLog.StartBenchmark("Init Screens");
                CGraphics.Init();
                CLog.StopBenchmark("Init Screens");

                Application.DoEvents();

                // Init Server
                CLog.StartBenchmark("Init Server");
                CVocaluxeServer.Init();
                CLog.StopBenchmark("Init Server");

                Application.DoEvents();

                // Init Input
                CLog.StartBenchmark("Init Input");
                CController.Init();
                CController.Connect();
                CLog.StopBenchmark("Init Input");

                Application.DoEvents();

                // Init Game;
                CLog.StartBenchmark("Init Game");
                CGame.Init();
                CProfiles.Update();
                CConfig.UsePlayers();
                CLog.StopBenchmark("Init Game");

                Application.DoEvents();

                // Init Party Modes;
                CLog.StartBenchmark("Init Party Modes");
                CParty.Init();
                CLog.StopBenchmark("Init Party Modes");

                Application.DoEvents();
                //Only reasonable point to call GC.Collect() because initialization may cause lots of garbage
                //Rely on GC doing its job afterwards and call Dispose methods where appropriate
                GC.Collect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error on start up: " + e.Message);
                CLog.LogError("Error on start up: " + e);
                if (_SplashScreen != null)
                    _SplashScreen.Close();
                _CloseProgram();
                return;
            }
            Application.DoEvents();

            // Start Main Loop
            if (_SplashScreen != null)
                _SplashScreen.Close();
            CVocaluxeServer.Start();

            CDraw.MainLoop();
        }

        private static void _CloseProgram()
        {
            // Unloading in reverse order
            try
            {
                CController.Close();
                CVocaluxeServer.Close();
                CGraphics.Close();
                CCover.Close();
                CBackgroundMusic.Close();
                CWebcam.Close();
                CDataBase.Close();
                CVideo.Close();
                CRecord.Close();
                CSound.Close();
                CDraw.Unload();
                GC.Collect(); // Do a GC run here before we close logs to have finalizers run
                CLog.CloseAll(); // Do this last, so we get all log entries!
            }
            catch (Exception) {}
            Environment.Exit(Environment.ExitCode);
        }

#if !DEBUG
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
#endif

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

                foreach (Process process in processes.Where(t => t.Id != currentProcess.Id))
                {
                    IntPtr wnd = process.MainWindowHandle;
                    if (wnd != IntPtr.Zero)
                        COSFunctions.SetForegroundWindow(wnd);
                }
#endif
                return false;
            }
            return true;
        }
    }
}