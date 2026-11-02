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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using SQLitePCL;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.Web;
using Vocaluxe.Base.ThemeSystem;
using Vocaluxe.Lib;
using Vocaluxe.Reporting;
using VocaluxeLib.Log;

[assembly: InternalsVisibleTo("VocaluxeTests")]

namespace Vocaluxe
{
    class CLoadingException : Exception
    {
        public CLoadingException(string component)
            : base("Failed to load " + component) { }
    }

    static class CMainProgram
    {
        private static CSplashScreen _SplashScreen;

        [STAThread]
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
            // ReSharper restore InconsistentNaming
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
#endif
            AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolver;
            AssemblyLoadContext.Default.ResolvingUnmanagedDll += _NativeLibraryResolver;
            _Main(args);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void _Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            foreach (var path in _NativeLibraryPaths)
            {
                COSFunctions.AddEnvironmentPath(path);
            }

            // Make sure SQLitePCL is initialized before any DB usage
            Batteries_V2.Init();


            // Close program if there is another instance running
            if (!_EnsureSingleInstance())
            {
                return;
            }
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
            CSdl.DoEvents();

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
                {
                    return;
                }

                if (!CSdl.Init())
                {
                    throw new CLoadingException("SDL");
                }

                CProgrammHelper.Init();

                using (CBenchmark.Time("Init Program"))
                {
                    CMain.Init();
                    CSdl.DoEvents();

                    // Init Language
                    using (CBenchmark.Time("Init Language"))
                    {
                        if (!CLanguage.Init())
                        {
                            throw new CLoadingException("Language");
                        }
                    }

                    CSdl.DoEvents();

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
                    CSdl.DoEvents();

                    // Init Draw
                    using (CBenchmark.Time("Init Draw"))
                    {
                        if (!CDraw.Init())
                        {
                            throw new CLoadingException("drawing");
                        }
                    }

                    CSdl.DoEvents();

                    // Init Playback
                    using (CBenchmark.Time("Init Playback"))
                    {
                        if (!CSound.Init())
                        {
                            throw new CLoadingException("playback");
                        }
                    }

                    CSdl.DoEvents();

                    // Init Record
                    using (CBenchmark.Time("Init Record"))
                    {
                        if (!CRecord.Init())
                        {
                            throw new CLoadingException("record");
                        }
                    }

                    CSdl.DoEvents();

                    // Init VideoDecoder
                    using (CBenchmark.Time("Init Videodecoder"))
                    {
                        if (!CVideo.Init())
                        {
                            throw new CLoadingException("video");
                        }
                    }

                    CSdl.DoEvents();

                    // Init Database
                    using (CBenchmark.Time("Init Database"))
                    {
                        if (!CDataBase.Init())
                        {
                            throw new CLoadingException("database");
                        }
                    }

                    CSongs.StartLoadSongs();

                    CSdl.DoEvents();

                    //Init Webcam
                    using (CBenchmark.Time("Init Webcam"))
                    {
                        if (!CWebcam.Init())
                        {
                            throw new CLoadingException("webcam");
                        }
                    }

                    CSdl.DoEvents();

                    // Init Background Music
                    using (CBenchmark.Time("Init Background Music"))
                    {
                        CBackgroundMusic.Init();
                    }

                    CSdl.DoEvents();

                    // Init Profiles
                    using (CBenchmark.Time("Init Profiles"))
                    {
                        CProfiles.Init();
                    }

                    CSdl.DoEvents();

                    // Init Fonts
                    using (CBenchmark.Time("Init Fonts"))
                    {
                        if (!CFonts.Init())
                        {
                            throw new CLoadingException("fonts");
                        }
                    }

                    CSdl.DoEvents();

                    // Theme System
                    using (CBenchmark.Time("Init Theme"))
                    {
                        if (!CThemes.Init())
                        {
                            throw new CLoadingException("theme");
                        }
                    }

                    using (CBenchmark.Time("Load Theme"))
                    {
                        CThemes.Load();
                    }

                    CSdl.DoEvents();

                    // Load Cover
                    using (CBenchmark.Time("Init Cover"))
                    {
                        if (!CCover.Init())
                        {
                            throw new CLoadingException("covertheme");
                        }
                    }

                    CSdl.DoEvents();

                    // Init Screens
                    using (CBenchmark.Time("Init Screens"))
                    {
                        CGraphics.Init();
                    }

                    CSdl.DoEvents();

                    // Init Server
                    using (CBenchmark.Time("Init Server"))
                    {
                        CWebServer.Init();
                    }

                    CSdl.DoEvents();

                    // Init Input
                    using (CBenchmark.Time("Init Input"))
                    {
                        CController.Init();
                        CController.Connect();
                    }

                    CSdl.DoEvents();

                    // Init Game
                    using (CBenchmark.Time("Init Game"))
                    {
                        CGame.Init();
                        CProfiles.Update();
                        CConfig.UsePlayers();
                    }

                    CSdl.DoEvents();

                    // Init Party Modes
                    using (CBenchmark.Time("Init Party Modes"))
                    {
                        if (!CParty.Init())
                        {
                            throw new CLoadingException("Party Modes");
                        }
                    }

                    CSdl.DoEvents();
                    //Only reasonable point to call GC.Collect() because initialization may cause lots of garbage
                    //Rely on GC doing its job afterwards and call Dispose methods where appropriate
                    GC.Collect();
                }
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error on start up: {ExceptionMessage}", CLog.Params(e.Message), show: true);
                if (_SplashScreen != null)
                {
                    _SplashScreen.Close();
                }

                _CloseProgram();
                return;
            }

            CSdl.DoEvents();

            // Start Main Loop
            if (_SplashScreen != null)
            {
                _SplashScreen.Close();
            }

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
                CWebServer.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CWebServer)");
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

            try
            {
                CSdl.Close();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during shutdown! (CSdl)");
            }

            GC.Collect(); // Do a GC run here before we close logs to have finalizers run
            try
            {
                CLog.Close(); // Do this last, so we get all log entries!
            }
            catch (Exception) { }

            Environment.Exit(Environment.ExitCode);
        }

#if !DEBUG
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
            {
                return null;
            }

            Assembly assembly = null;
            var arr = args.Name.Split(new char[] { ',' });
            if (arr.Length > 0)
            {
                var fileName = arr[0] + ".dll";

#if ARCH_X86
                string arch = "x86";
#endif
#if ARCH_X64
                var arch = "x64";
#endif

                string[] probePaths =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "managed", fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "managed", arch, fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arch, fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
                };

                var path = probePaths.FirstOrDefault(File.Exists);
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

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
                        _LogError("Cannot load assembly " + args.Name + " from " + path + ": " + e + "\r\nOuter Error: " + e1);
                    }
                }
#if LINUX
                catch (FileNotFoundException) { }
#endif
            }

            return assembly;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void _LogError(string message)
        {
            CLog.Error(message);
        }

        private static string _Arch
        {
            get
            {
#if ARCH_X86
                return "x86";
#else
                return "x64";
#endif
            }
        }

        private static readonly string[] _NativeLibraryPaths =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "unmanaged"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "unmanaged", _Arch),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "managed"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "managed", _Arch),
            AppDomain.CurrentDomain.BaseDirectory
        };

        private static readonly Regex _VersionedLibraryName = new Regex(@"^(?:lib)?(.+?)-(\d+)$");

        private static IntPtr _NativeLibraryResolver(Assembly assembly, string libraryName)
        {
            var mapped = _GetMappedLibraryName(assembly, libraryName);
            var names = mapped != null ? new[] { mapped }.Concat(_GetNativeLibraryNames(libraryName)) : _GetNativeLibraryNames(libraryName);
            var folders = OperatingSystem.IsMacOS() ? _NativeLibraryPaths.Concat(new[] { "/opt/homebrew/lib", "/usr/local/lib" }) : _NativeLibraryPaths;
            foreach (var name in names)
            {
                foreach (var folder in folders)
                {
                    var path = Path.Combine(folder, name);
                    IntPtr handle;
                    if (File.Exists(path) && NativeLibrary.TryLoad(path, out handle))
                    {
                        return handle;
                    }
                }

                IntPtr systemHandle;
                if (NativeLibrary.TryLoad(name, out systemHandle))
                {
                    return systemHandle;
                }
            }

            return IntPtr.Zero;
        }

        private static string _GetMappedLibraryName(Assembly assembly, string libraryName)
        {
            if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(assembly.Location))
            {
                return null;
            }

            var config = assembly.Location + ".config";
            if (!File.Exists(config))
            {
                return null;
            }

            try
            {
                var os = OperatingSystem.IsMacOS() ? "osx" : "linux";
                foreach (var map in XDocument.Load(config).Descendants("dllmap"))
                {
                    var mapOs = (string)map.Attribute("os");
                    if (string.Equals((string)map.Attribute("dll"), libraryName, StringComparison.OrdinalIgnoreCase) &&
                        (mapOs == null || mapOs.Split(',').Any(o => o.Trim() == os)))
                    {
                        return (string)map.Attribute("target");
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        private static IEnumerable<string> _GetNativeLibraryNames(string libraryName)
        {
            var baseName = Path.GetFileName(libraryName);
            foreach (var extension in new[] { ".dll", ".so", ".dylib" })
            {
                if (baseName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - extension.Length);
                }
            }

            var soIndex = baseName.IndexOf(".so.", StringComparison.Ordinal);
            if (soIndex > 0)
            {
                baseName = baseName.Substring(0, soIndex);
            }

            if (baseName.StartsWith("lib", StringComparison.Ordinal))
            {
                baseName = baseName.Substring(3);
            }

            if (OperatingSystem.IsWindows())
            {
                yield return baseName + ".dll";
                yield return "lib" + baseName + ".dll";
                yield break;
            }

            var names = new List<string> { baseName, baseName.ToLowerInvariant() };
            var versioned = _VersionedLibraryName.Match(baseName);
            if (versioned.Success)
            {
                names.Insert(0, versioned.Groups[1].Value);
            }

            if (baseName.EndsWith("-libusb", StringComparison.Ordinal))
            {
                names.Add(baseName.Substring(0, baseName.Length - "-libusb".Length));
            }

            foreach (var name in names)
            {
                if (OperatingSystem.IsMacOS())
                {
                    if (versioned.Success && name == versioned.Groups[1].Value)
                    {
                        yield return "lib" + name + "." + versioned.Groups[2].Value + ".dylib";
                    }

                    yield return "lib" + name + ".dylib";
                    yield return name + ".dylib";
                }
                else
                {
                    if (versioned.Success && name == versioned.Groups[1].Value)
                    {
                        yield return "lib" + name + ".so." + versioned.Groups[2].Value;
                    }

                    yield return "lib" + name + ".so";
                    yield return name + ".so";
                    for (var version = 0; version <= 2; version++)
                    {
                        yield return "lib" + name + ".so." + version;
                    }
                }
            }
        }

        private static readonly Mutex _Mutex = new Mutex(false, CSettings.ProgramName + "-SingleInstanceMutex");

        private static bool _EnsureSingleInstance()
        {
            // wait a few seconds in case that the instance is just shutting down
            if (!_Mutex.WaitOne(TimeSpan.FromSeconds(2), false))
            {
                //TODO: put it into language file
                CSdl.ShowMessage(CSettings.ProgramName, "Another Instance of Vocaluxe is already runnning!");
#if WIN
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes.Where(t => t.Id != currentProcess.Id))
                {
                    var wnd = process.MainWindowHandle;
                    if (wnd != IntPtr.Zero)
                    {
                        COSFunctions.SetForegroundWindow(wnd);
                    }
                }
#endif
                return false;
            }

            return true;
        }
    }
}
