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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.Server;
using Vocaluxe.Base.ThemeSystem;
using Vocaluxe.Reporting;
using VocaluxeLib.Log;

[assembly: InternalsVisibleTo("VocaluxeTests")]

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
            COSFunctions.AddEnvironmentPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs\\unmanaged\\"));
            #if ARCH_X86
            COSFunctions.AddEnvironmentPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs\\unmanaged\\x86\\"));
#endif
#if ARCH_X64
            COSFunctions.AddEnvironmentPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs\\unmanaged\\x64\\"));
#endif
            

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
                CLog.Fatal(e, "Unhandled error: {ErrorMessage}", CLog.Params(e.Message));
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
                // Create data folder
                Directory.CreateDirectory(CSettings.DataFolder);

                // Init Log
                CLog.Init(CSettings.FolderNameLogs,
                    CSettings.FileNameMainLog, 
                    CSettings.FileNameSongLog, 
                    CSettings.FileNameCrashMarker, 
                    CSettings.GetFullVersionText(), 
                    CReporter.ShowReporterFunc, 
                    ELogLevel.Information);

                if (!CProgrammHelper.CheckRequirements())
                    return;
                CProgrammHelper.Init();
                
                using (CBenchmark.Time("Init Program"))
                {
                    CMain.Init();
                    Application.DoEvents();

                    // Init Language
                    using (CBenchmark.Time("Init Language"))
                    {
                        if (!CLanguage.Init())
                            throw new CLoadingException("Language");
                    }

                    Application.DoEvents();

                    // load config
                    using (CBenchmark.Time("Init Config"))
                    {
                        CConfig.LoadCommandLineParams(args);
                        CConfig.UseCommandLineParamsBefore();
                        CConfig.Init();
                        CConfig.UseCommandLineParamsAfter();
                    }

                    // Create folders
                    CSettings.CreateFolders();

                    _SplashScreen = new CSplashScreen();
                    Application.DoEvents();

                    // Init Draw
                    using (CBenchmark.Time("Init Draw"))
                    {
                        if (!CDraw.Init())
                            throw new CLoadingException("drawing");
                    }

                    Application.DoEvents();

                    // Init Playback
                    using (CBenchmark.Time("Init Playback"))
                    {
                        if (!CSound.Init())
                            throw new CLoadingException("playback");
                    }

                    Application.DoEvents();

                    // Init Record
                    using (CBenchmark.Time("Init Record"))
                    {
                        if (!CRecord.Init())
                            throw new CLoadingException("record");
                    }

                    Application.DoEvents();

                    // Init VideoDecoder
                    using (CBenchmark.Time("Init Videodecoder"))
                    {
                        if (!CVideo.Init())
                            throw new CLoadingException("video");
                    }

                    Application.DoEvents();

                    // Init Database
                    using (CBenchmark.Time("Init Database"))
                    {
                        if (!CDataBase.Init())
                            throw new CLoadingException("database");
                    }

                    Application.DoEvents();

                    //Init Webcam
                    using (CBenchmark.Time("Init Webcam"))
                    {
                        if (!CWebcam.Init())
                            throw new CLoadingException("webcam");
                    }

                    Application.DoEvents();

                    // Init Background Music
                    using (CBenchmark.Time("Init Background Music"))
                    {
                        CBackgroundMusic.Init();
                    }

                    Application.DoEvents();

                    // Init Profiles
                    using (CBenchmark.Time("Init Profiles"))
                    {
                        CProfiles.Init();
                    }

                    Application.DoEvents();

                    // Init Fonts
                    using (CBenchmark.Time("Init Fonts"))
                    {
                        if (!CFonts.Init())
                            throw new CLoadingException("fonts");
                    }

                    Application.DoEvents();

                    // Theme System
                    using (CBenchmark.Time("Init Theme"))
                    {
                        if (!CThemes.Init())
                            throw new CLoadingException("theme");
                    }
                    
                    using (CBenchmark.Time("Load Theme"))
                    {
                        CThemes.Load();
                    }

                    Application.DoEvents();

                    // Load Cover
                    using (CBenchmark.Time("Init Cover"))
                    {
                        if (!CCover.Init())
                            throw new CLoadingException("covertheme");
                    }

                    Application.DoEvents();

                    // Init Screens
                    using (CBenchmark.Time("Init Screens"))
                    {
                        CGraphics.Init();
                    }

                    Application.DoEvents();

                    // Init Server
                    using (CBenchmark.Time("Init Server"))
                    {
                        CVocaluxeServer.Init();
                    }

                    Application.DoEvents();

                    // Init Input
                    using (CBenchmark.Time("Init Input"))
                    {
                        CController.Init();
                        CController.Connect();
                    }

                    Application.DoEvents();

                    // Init Game
                    using (CBenchmark.Time("Init Game"))
                    {
                        CGame.Init();
                        CProfiles.Update();
                        CConfig.UsePlayers();
                    }

                    Application.DoEvents();

                    // Init Party Modes
                    using (CBenchmark.Time("Init Party Modes"))
                    {
                        if (!CParty.Init())
                            throw new CLoadingException("Party Modes");
                    }

                    Application.DoEvents();
                    //Only reasonable point to call GC.Collect() because initialization may cause lots of garbage
                    //Rely on GC doing its job afterwards and call Dispose methods where appropriate
                    GC.Collect();
                }
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error on start up: {ExceptionMessage}", CLog.Params(e.Message), show:true);
                if (_SplashScreen != null)
                    _SplashScreen.Close();
                _CloseProgram();
                return;
            }
            Application.DoEvents();

            // Start Main Loop
            if (_SplashScreen != null)
                _SplashScreen.Close();

            CDraw.MainLoop();
        }

        private static void _CloseProgram()
        {
            // Unloading in reverse order
            try
            {
                CController.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CController)");
            }

            try
            {
                CVocaluxeServer.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CVocaluxeServer)");
            }

            try
            {
                CGraphics.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CGraphics)");
            }

            try
            {
                CThemes.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CThemes)");
            }

            try
            {
                CCover.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CCover)");
            }

            try
            {
                CFonts.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CFonts)");
            }

            try
            {
                CBackgroundMusic.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CBackgroundMusic)");
            }

            try
            {
                CWebcam.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CWebcam)");
            }

            try
            {
                CDataBase.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CDataBase)");
            }

            try
            {
                CVideo.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CVideo)");
            }

            try
            {
                CRecord.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CRecord)");
            }

            try
            {
                CSound.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CSound)");
            }

            try
            {
                CDraw.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CDraw)");
            }

            GC.Collect(); // Do a GC run here before we close logs to have finalizers run
            try
            {
                CLog.Close(); // Do this last, so we get all log entries!
            }
            catch (Exception) {}
            Environment.Exit(Environment.ExitCode);
        }

#if !DEBUG
        [HandleProcessCorruptedStateExceptions]
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            CLog.Fatal(e, "Unhandled exception: {ExceptionMessage}", CLog.Params(e.Message));
        }
#endif

        private static Assembly _AssemblyResolver(Object sender, ResolveEventArgs args)
        {
            // a fix to handle mscorlib.resources
            if (args.Name.IndexOf(".resources", StringComparison.Ordinal) >= 0)
                return null;

            Assembly assembly = null;
            string[] arr = args.Name.Split(new char[] {','});
            if (arr.Length > 0)
            {
#if ARCH_X86
                string path = "x86";
#endif

#if ARCH_X64
                string path = "x64";
#endif
                path = Path.Combine(CSettings.ProgramFolder, path, arr[0] + ".dll");
                try
                {
                    assembly = Assembly.LoadFrom(path);
                }
                catch (FileLoadException e1)
                {
                    //possibly loading from network, this would work always but better use the normal LoadFrom for more specific errors
                    try
                    {
                        assembly = Assembly.Load(File.ReadAllBytes(path));
                    }
                    catch (Exception e)
                    {
                        CLog.Error("Cannot load assembly " + args.Name + " from " + path + ": " + e + "\r\nOuter Error: " + e1);
                    }
                }
                #if LINUX
                catch(FileNotFoundException){}
                #endif
            }
            return assembly;
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