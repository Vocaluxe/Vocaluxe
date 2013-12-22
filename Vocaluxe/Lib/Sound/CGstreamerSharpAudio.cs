using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gst;
using GLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    public class CGstreamerSharpAudio : IPlayback
    {
        private Dictionary<int, CGstreamerSharpAudioStream> Streams = new Dictionary<int, CGstreamerSharpAudioStream>();
        private static int idCount;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void SetDllDirectory(string lpPathName);

        public bool Init()
        {
            string path;
#if ARCH_X86
            path = ".\\x86\\gstreamer";
#endif
#if ARCH_X64
            path = ".\\x64\\gstreamer";
#endif
            SetDllDirectory(path);
            Application.Init();
            Registry reg = Registry.Get();
            reg.ScanPath(path);
            
            return Application.IsInitialized;
        }

        public void SetGlobalVolume(float volume)
        {
            foreach (CGstreamerSharpAudioStream stream in Streams.Values)
            {
                stream.Volume = volume;
            }
        }

        public int GetStreamCount()
        {
            return Streams.Count;
        }

        public void CloseAll()
        {
            foreach (CGstreamerSharpAudioStream stream in Streams.Values)
            {
                stream.Close();
            }
        }

        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            var stream = new CGstreamerSharpAudioStream (media, prescan);
            Streams[idCount] = stream;
            return idCount++;
        }

        public void Close(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Close();
        }

        public void Play(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Playing = true;
        }

        public void Play(int stream, bool loop)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Play(stream);
        }

        public void Pause(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Paused = true;
        }

        public void Stop(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Stop();
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Fade(targetVolume, seconds);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].FadeAndPause(targetVolume, seconds);
        }

        public void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].FadeAndClose(targetVolume, seconds);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].FadeAndStop(targetVolume, seconds);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Volume = volume;
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].MaxVolume = volume;
        }

        public float GetLength(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return -1;
            return Streams[stream].Length;
        }

        public float GetPosition(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return -1f;
            return Streams[stream].Position;
        }

        public bool IsPlaying(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return false;
            return Streams[stream].Playing;
        }

        public bool IsPaused(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return true;
            return Streams[stream].Paused;
        }

        public bool IsFinished(int stream)
        {
            if (!Streams.ContainsKey(stream))
                return true;
            return Streams[stream]._Finished;
        }

        public void SetPosition(int stream, float position)
        {
            if (!Streams.ContainsKey(stream))
                return;
            Streams[stream].Position = position;
        }

        public void Update()
        {
            var streamsToDelete = new List<int>();
            foreach (KeyValuePair<int,CGstreamerSharpAudioStream> stream in Streams)
            {
                if (stream.Value._Closed)
                    streamsToDelete.Add(stream.Key);
                stream.Value.Update();
            }
            foreach (int key in streamsToDelete)
            {
                Streams.Remove(key);
            }
        }

        private class CGstreamerSharpAudioStream
        {
            private Element _Element;
            private bool _Loop;
            public bool _Closed;
            public bool _Finished;

            private bool _Fading;
            private bool _CloseStreamAfterFade;
            private bool _PauseStreamAfterFade;
            private bool _StopStreamAfterFade;
            private Stopwatch _FadeTimer = new Stopwatch();
            private float _FadeTime;
            private float _FadeVolume;
            private float _Volume = 100f;

            public float _MaxVolume = 100f;
            public CGstreamerSharpAudioStream(string media, bool prescan)
            {
                var convert = ElementFactory.Make("audioconvert", "convert");
                var audiosink = ElementFactory.Make("directsoundsink", "audiosink");
                var audioSinkBin = new Bin("Audiosink");

                if (convert == null || audiosink == null || audioSinkBin == null)
                    CLog.LogError("Could not create pipeline");

                audioSinkBin.Add(convert);
                audioSinkBin.Add(audiosink);
                convert.Link(audiosink);
                Pad pad = convert.GetStaticPad("sink");
                GhostPad ghostpad = new GhostPad("sink", pad);

                if (pad == null || ghostpad == null)
                    CLog.LogError("Could not create pads");

                if (!ghostpad.SetActive(true))
                    CLog.LogError("Could not link pads");
                if (!audioSinkBin.AddPad(ghostpad))
                    CLog.LogError("Could not add pad");

                _Element = ElementFactory.Make("playbin", "playbin");
                if (_Element == null)
                    CLog.LogError("Could not create playbin");
                _Element["audio-sink"] = audioSinkBin;
                _Element["flags"] = 1 << 1;
                _Element["uri"] = new Uri(media).AbsoluteUri;
                _Element.SetState(State.Paused);

                if (prescan)
                    _Element.Bus.TimedPopFiltered(0xffffffffffffffff, MessageType.AsyncDone);
            }

            private void OnMessage(Message msg)
            {
                switch (msg.Type)
                {
                    case MessageType.Eos:
                        if (_Loop)
                            Position = 0;
                        else
                            Close();
                        break;
                    case MessageType.Error:
                        GException error;
                        string debug;
                        msg.ParseError(out error, out debug);
                        CLog.LogError("Gstreamer error: message" + error.Message + ", code" + error.Code + " ,debug information" + debug);
                        break;                     
                }
                msg.Unref();
            }


            public void Close()
            {
                if (_Element != null)
                {
                    _Element.SetState(State.Null);
                    _Element.Dispose();
                }
                _Closed = true;
                _Finished = true;
            }

            public void Play(bool loop = false)
            {
                if (_Element != null)
                    _Element.SetState(State.Playing);
                this._Loop = loop;
            }

            public void Stop()
            {
                if (_Element != null)
                    _Element.SetState(State.Null);
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
                get
                {
                    return _Element != null ? (float)(double)_Element["volume"] : 0;
                }
                set
                {
                    _Volume = value;
                    if (_Element != null)
                        _Element["volume"] = (double) ((value / 100d) * (_MaxVolume / 100d));
                }
            }

            public float MaxVolume
            {
                get
                {
                    return _MaxVolume;
                }
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
                    long duration = 0;
                    if (_Element != null)
                        if (!_Element.QueryDuration(Format.Time, out duration))
                            CLog.LogError("Could not query duration");
                    return duration > 0 ? (duration / (long)Constants.SECOND) : 100;
                }
            }

            public float Position
            {
                get
                {
                    long position = 0;
                    if (_Element != null)
                        if (!_Element.QueryPosition(Format.Time, out position))
                            CLog.LogError("Could not query position");
                    return (float)(position / (double)Constants.SECOND);
                }
                set
                {
                    if (_Element != null)
                        _Element.SeekSimple(Format.Time, SeekFlags.Accurate | SeekFlags.Flush, (long)(value * (double)Constants.SECOND));
                }
            }

            public bool Paused
            {
                get
                {
                    return _Element != null ? _Element.CurrentState == State.Paused : true;
                }
                set
                {
                    if (value && _Element != null)
                        _Element.SetState(State.Paused);
                }
            }

            public bool Playing
            {
                get
                {
                    return _Element != null ? _Element.CurrentState == State.Playing : false;
                }
                set
                {
                    if(value && _Element != null)
                        _Element.SetState(State.Playing);
                }
            }

            public void Update()
            {
                while (_Element != null && _Element.Bus != null && _Element.Bus.HavePending())
                {
                    OnMessage(_Element.Bus.Pop());
                }

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
        }
    }
}
