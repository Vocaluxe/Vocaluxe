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
using Gst;
using GLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Vocaluxe.Base;
using Thread = System.Threading.Thread;

namespace Vocaluxe.Lib.Sound
{
    public class CGstreamerSharpAudio : IPlayback
    {
        private readonly Dictionary<int, CGstreamerSharpAudioStream> _Streams = new Dictionary<int, CGstreamerSharpAudioStream>();
        private static int _IDCount;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void SetDllDirectory(string lpPathName);

        public bool Init()
        {
#if ARCH_X86
            const string path = ".\\x86\\gstreamer";
#endif
#if ARCH_X64
            const string path = ".\\x64\\gstreamer";
#endif
            SetDllDirectory(path);
            Application.Init();
            Registry reg = Registry.Get();
            reg.ScanPath(path);

            return Application.IsInitialized;
        }

        public void SetGlobalVolume(float volume)
        {
            foreach (CGstreamerSharpAudioStream stream in _Streams.Values)
                stream.Volume = volume;
        }

        public int GetStreamCount()
        {
            return _Streams.Count;
        }

        public void CloseAll()
        {
            foreach (CGstreamerSharpAudioStream stream in _Streams.Values)
                stream.Close();
        }

        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            var stream = new CGstreamerSharpAudioStream(media, prescan);
            _Streams[_IDCount] = stream;
            return _IDCount++;
        }

        public void Close(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Close();
        }

        public void Play(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Playing = true;
        }

        public void Play(int stream, bool loop)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            Play(stream, loop);
        }

        public void Pause(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Paused = true;
        }

        public void Stop(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Stop();
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Fade(targetVolume, seconds);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndPause(targetVolume, seconds);
        }

        public void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndClose(targetVolume, seconds);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].FadeAndStop(targetVolume, seconds);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Volume = volume;
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].MaxVolume = volume;
        }

        public float GetLength(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return -1;
            return _Streams[stream].Length;
        }

        public float GetPosition(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return -1f;
            return _Streams[stream].Position;
        }

        public bool IsPlaying(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return false;
            return _Streams[stream].Playing;
        }

        public bool IsPaused(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return false;
            return _Streams[stream].Paused;
        }

        public bool IsFinished(int stream)
        {
            if (!_Streams.ContainsKey(stream))
                return true;
            return _Streams[stream].Finished;
        }

        public void SetPosition(int stream, float position)
        {
            if (!_Streams.ContainsKey(stream))
                return;
            _Streams[stream].Position = position;
        }

        public void Update()
        {
            var streamsToDelete = new List<int>();
            foreach (KeyValuePair<int, CGstreamerSharpAudioStream> stream in _Streams)
            {
                if (stream.Value.Closed)
                    streamsToDelete.Add(stream.Key);
                stream.Value.Update();
            }
            foreach (int key in streamsToDelete)
                _Streams.Remove(key);
        }

        private class CGstreamerSharpAudioStream
        {
            private Element _Element;
            private bool _Loop;
            public volatile bool Closed = true;
            public volatile bool Finished;

            private bool _Fading;
            private bool _CloseStreamAfterFade;
            private bool _PauseStreamAfterFade;
            private bool _StopStreamAfterFade;
            private readonly Stopwatch _FadeTimer = new Stopwatch();
            private float _FadeTime;
            private float _FadeVolume;
            private float _Volume = 100f;

            private float _MaxVolume = 100f;

            private volatile float _Duration = -1f;
            private volatile float _Position;
            private volatile bool _QueryingDuration;

            public CGstreamerSharpAudioStream(string media, bool prescan)
            {
                Element convert = ElementFactory.Make("audioconvert", "convert");
                Element audiosink = ElementFactory.Make("directsoundsink", "audiosink");
                var audioSinkBin = new Bin("Audiosink");

                if (convert == null || audiosink == null)
                {
                    CLog.LogError("Could not create pipeline");
                    return;
                }

                audioSinkBin.Add(convert);
                audioSinkBin.Add(audiosink);
                convert.Link(audiosink);
                Pad pad = convert.GetStaticPad("sink");
                GhostPad ghostpad = new GhostPad("sink", pad);

                if (pad == null)
                {
                    CLog.LogError("Could not create pads");
                    return;
                }

                if (!ghostpad.SetActive(true))
                {
                    CLog.LogError("Could not link pads");
                    return;
                }
                if (!audioSinkBin.AddPad(ghostpad))
                {
                    CLog.LogError("Could not add pad");
                    return;
                }

                _Element = ElementFactory.Make("playbin", "playbin");
                if (_Element == null)
                {
                    CLog.LogError("Could not create playbin");
                    return;
                }
                _Element["audio-sink"] = audioSinkBin;
                _Element["flags"] = 1 << 1;
                _Element["uri"] = new Uri(media).AbsoluteUri;
                _Element.SetState(State.Paused);

                Closed = false; //Enable stream

                // Passing CLOCK_TIME_NONE here causes the pipeline to block for a long time so with
                // prescan enabled the pipeline will wait 500ms for stream to initialize and then continue
                // if it takes more than 500ms, duration queries will be performed asynchronously
                if (prescan)
                {
                    Message msg = _Element.Bus.TimedPopFiltered(0xffffffffffffffff, MessageType.AsyncDone);
                    if (msg.Handle != IntPtr.Zero)
                        _UpdateDuration();
                }
            }

            private void _OnMessage(Message msg)
            {
                if (msg.Handle == IntPtr.Zero)
                    return;
                switch (msg.Type)
                {
                    case MessageType.Eos:
                        if (_Loop)
                            Position = 0;
                        else
                            Finished = true;
                        Close();
                        break;
                    case MessageType.Error:
                        GException error;
                        string debug;
                        msg.ParseError(out error, out debug);
                        CLog.LogError("Gstreamer error: message" + error.Message + ", code" + error.Code + " ,debug information" + debug);
                        break;
                    case MessageType.DurationChanged:
                        _UpdateDuration();
                        break;
                }
                msg.Unref();
            }

            public void Close()
            {
                if (!Closed)
                {
                    Closed = true;
                    Finished = true;
                    var t = new Thread(_TerminateStream) {Name = "GSt Terminate"};
                    t.Start();
                }
            }

            private void _TerminateStream()
            {
                if (_Element != null)
                {
                    _Element.SetState(State.Null);
                    _Element.Dispose();
                    _Element = null;
                }
            }

            public void Play(bool loop = false)
            {
                if (_Element != null)
                    _Element.SetState(State.Playing);
                _Loop = loop;
            }

            public void Stop()
            {
                if (_Element != null)
                    _Element.SetState(State.Ready);
                Position = 0;
            }

            public void Fade(float targetVolume, float seconds)
            {
                _Fading = true;
                _FadeTimer.Restart();
                _FadeTime = seconds;
                _FadeVolume = targetVolume;
            }

            public void FadeAndPause(float targetVolume, float seconds)
            {
                Fade(targetVolume, seconds);
                _PauseStreamAfterFade = true;
            }

            public void FadeAndClose(float targetVolume, float seconds)
            {
                Fade(targetVolume, seconds);
                _CloseStreamAfterFade = true;
            }

            public void FadeAndStop(float targetVolume, float seconds)
            {
                Fade(targetVolume, seconds);
                _StopStreamAfterFade = true;
            }

            public float Volume
            {
                get { return _Element != null ? (float)(double)_Element["volume"] : 0; }
                set
                {
                    _Volume = value;
                    if (_Element != null)
                        _Element["volume"] = ((value / 100d) * (_MaxVolume / 100d));
                }
            }

            public float MaxVolume
            {
                get { return _MaxVolume; }
                set
                {
                    _MaxVolume = value;
                    Volume = _Volume;
                }
            }

            public float Length
            {
                get
                {
                    if (_Duration < 0 && !_QueryingDuration)
                    {
                        _QueryingDuration = true; // Set this to avoid race conditions
                        var t = new Thread(_UpdateDuration) {Name = "GSt Update Duration"};
                        t.Start();
                    }
                    return _Duration > 0 ? _Duration : -1;
                }
            }

            public float Position
            {
                get
                {
                    long position;
                    if (!_Element.QueryPosition(Format.Time, out position))
                        CLog.LogError("Could not query position");
                    else
                        _Position = (float)(position / (double)Constants.SECOND);
                    return _Position;
                }
                set
                {
                    if (_Element != null)
                        _Element.SeekSimple(Format.Time, SeekFlags.Accurate | SeekFlags.Flush, (long)(value * (double)Constants.SECOND));
                }
            }

            public bool Paused
            {
                get { return _Element == null || _Element.TargetState == State.Paused; }
                set
                {
                    if (value && _Element != null)
                        _Element.SetState(State.Paused);
                }
            }

            public bool Playing
            {
                get { return _Element != null && (_Element.TargetState == State.Playing && !Finished); }
                set
                {
                    if (value && _Element != null)
                        _Element.SetState(State.Playing);
                }
            }

            public void Update()
            {
                while (_Element != null && _Element.Bus != null && _Element.Bus.HavePending())
                    _OnMessage(_Element.Bus.Pop());

                if (_Fading)
                {
                    if (_FadeTimer.ElapsedMilliseconds < (_FadeTime * 1000f))
                        Volume = ((_FadeTimer.ElapsedMilliseconds) / (_FadeTime * 1000f)) * _FadeVolume;
                    else
                    {
                        Volume = _FadeVolume;
                        if (_CloseStreamAfterFade)
                            Close();
                        if (_PauseStreamAfterFade)
                            Paused = true;
                        if (_StopStreamAfterFade)
                            Stop();

                        _CloseStreamAfterFade = false;
                        _PauseStreamAfterFade = false;
                        _StopStreamAfterFade = false;
                        _Fading = false;
                        _FadeTimer.Reset();
                    }
                }
            }

            private void _UpdateDuration()
            {
                _QueryingDuration = true;
                long duration = -1;
                while (duration < 0 && !Closed && !Finished && _Element != null)
                {
                    if (_Element.QueryDuration(Format.Time, out duration))
                    {
                        _Duration = duration / (float)Constants.SECOND;
                        _QueryingDuration = false;
                    }
                }
            }
        }
    }
}