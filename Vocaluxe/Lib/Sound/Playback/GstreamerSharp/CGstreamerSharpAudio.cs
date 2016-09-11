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

using Gst;
using System;
using Vocaluxe.Base;
using System.IO;

namespace Vocaluxe.Lib.Sound.Playback.GstreamerSharp
{
    public class CGstreamerSharpAudio : CPlaybackBase
    {
        public override bool Init()
        {
            if (_Initialized)
                return false;
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
                string gstreamerEnvVar = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);

                if (gstreamerEnvVar == null || !Directory.Exists(gstreamerEnvVar))
                {
                    CLog.LogError("Gstreamer not found! Make sure you installed it correctly and if it set the environment variable '" + varName + "'!", true);
                    return false;
                }
                else
                {
                    dllDirectory = gstreamerEnvVar + "bin\\";
                }
            }
           
            
            COSFunctions.AddEnvironmentPath(dllDirectory);
            #endif
            Application.Init();

            _Initialized = Application.IsInitialized;
            return _Initialized;
        }

        public override void Close()
        {
            if (!_Initialized)
                return;
            base.Close();
            Application.Deinit();
        }

        protected override IAudioStream _CreateStream(int id, string media, bool loop, EAudioEffect effect = EAudioEffect.None)
        {
            return new CGstreamerSharpAudioStream(id, media, loop, effect);
        }
    }
}