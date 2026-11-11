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
using System.IO;
using System.Runtime.InteropServices;
using Gst;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Playback.GstreamerSharp
{
    public class CGstreamerSharpAudio : CPlaybackBase
    {
#if MAC
        [DllImport("libc", EntryPoint = "setenv")]
        private static extern int _SetEnv(string name, string value, int overwrite);

        /// <summary>
        ///     Lets GStreamer use only the plugins shipped with the app bundle (libs/unmanaged/gstreamer, see MacOS/build-dependencies.sh).
        ///     The variables must be set in the native environment, Environment.SetEnvironmentVariable is not visible to GStreamer.
        /// </summary>
        private static void _UseBundledPlugins()
        {
            var gstreamer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", "unmanaged", "gstreamer");
            if (!Directory.Exists(gstreamer))
            {
                return; // e.g. a development build with GStreamer from Homebrew
            }

            _SetEnv("GST_PLUGIN_SYSTEM_PATH_1_0", Path.Combine(gstreamer, "lib", "gstreamer-1.0"), 1);
            _SetEnv("GST_PLUGIN_PATH_1_0", "", 1);
            _SetEnv("GST_PLUGIN_SCANNER_1_0", Path.Combine(gstreamer, "libexec", "gstreamer-1.0", "gst-plugin-scanner"), 1);
            // own registry: the plugin cache of another GStreamer installation must not be used
            var cache = Path.Combine(CSettings.DataFolder, "Cache");
            Directory.CreateDirectory(cache);
            _SetEnv("GST_REGISTRY_1_0", Path.Combine(cache, "gstreamer-registry.bin"), 1);
        }
#endif

        public override bool Init()
        {
            if (_Initialized)
            {
                return false;
            }
#if WIN
#if ARCH_X86
            const string varName = "GSTREAMER_1_0_ROOT_X86";
#endif
#if ARCH_X64
            const string varName = "GSTREAMER_1_0_ROOT_X86_64";
#endif
            string dllDirectory;

#if ARCH_X86
            dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs\\unmanaged\\gstreamer86\\bin\\");
#endif
#if ARCH_X64
            dllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs\\unmanaged\\gstreamer64\\bin\\");
#endif
            if (!Directory.Exists(dllDirectory))
            {
                var gstreamerEnvVar = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);

                if (gstreamerEnvVar == null || !Directory.Exists(gstreamerEnvVar))
                {
                    CLog.Error("Gstreamer not found! Make sure you installed it correctly and if it set the environment variable '" + varName + "'!", true);
                    return false;
                }
                else
                {
                    dllDirectory = gstreamerEnvVar + "bin\\";
                }
            }


            COSFunctions.AddEnvironmentPath(dllDirectory);
#endif
#if MAC
            _UseBundledPlugins();
#endif
            Application.Init();

            _Initialized = Application.IsInitialized;
            return _Initialized;
        }

        public override void Close()
        {
            if (!_Initialized)
            {
                return;
            }

            base.Close();
            Application.Deinit();
        }

        protected override IAudioStream _CreateStream(int id, string media, bool loop, EAudioEffect effect = EAudioEffect.None)
        {
            return new CGstreamerSharpAudioStream(id, media, loop, effect);
        }
    }
}