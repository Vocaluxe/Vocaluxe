using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gst;
using GLib;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
            if (stream < 0)
                return;
            Streams[stream].Close();
        }

        public void Play(int stream)
        {
            if (stream < 0)
                return;
            Streams[stream].Playing = true;
        }

        public void Play(int stream, bool loop)
        {
            if (stream < 0)
                return;
            Play(stream);
        }

        public void Pause(int stream)
        {
            if (stream < 0)
                return;
            Streams[stream].Paused = true;
        }

        public void Stop(int stream)
        {
            if (stream < 0)
                return;
            Streams[stream].Stop();
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (stream < 0)
                return;
            Streams[stream].Fade(targetVolume, seconds);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (stream < 0)
                return;
            Streams[stream].FadeAndPause(targetVolume, seconds);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (stream < 0)
                return;
            Streams[stream].FadeAndStop(targetVolume, seconds);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (stream < 0)
                return;
            Streams[stream].Volume = volume;
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (stream < 0)
                return;
            Streams[stream].MaxVolume = volume;
        }

        public float GetLength(int stream)
        {
            if (stream < 0)
                return -1;
            return Streams[stream].Length;
        }

        public float GetPosition(int stream)
        {
            if (stream < 0)
                return -1f;
            return Streams[stream].Position;
        }

        public bool IsPlaying(int stream)
        {
            if (stream < 0)
                return false;
            return Streams[stream].Playing;
        }

        public bool IsPaused(int stream)
        {
            if (stream < 0)
                return true;
            return Streams[stream].Paused;
        }

        public bool IsFinished(int stream)
        {
            if (stream < 0)
                return true;
            return Streams[stream]._Finished;
        }

        public void SetPosition(int stream, float position)
        {
            if (stream < 0)
                return;
            Streams[stream].Position = position;
        }

        public void Update()
        {
            foreach (KeyValuePair<int,CGstreamerSharpAudioStream> stream in Streams)
            {
                //if (stream.Value._Closed)
                //    Streams.Remove(stream.Key);
                stream.Value.Update();
            }
        }

        private class CGstreamerSharpAudioStream
        {
            private Element _Element;
            private bool _Loop;
            public bool _Closed;
            public bool _Finished;

            private bool _Fading;
            private bool _StopStreamAfterFade;
            private bool _PauseStreamAfterFade;
            private Stopwatch _FadeTimer = new Stopwatch();
            private float _FadeTime;
            private float _FadeVolume;

            public float _MaxVolume = 100f;
            public CGstreamerSharpAudioStream(string media, bool prescan)
            {
                var convert = ElementFactory.Make("audioconvert", "convert");
                var audiosink = ElementFactory.Make("directsoundsink", "audiosink");
                var audioSinkBin = new Bin("Audiosink");
                audioSinkBin.Add(convert);
                audioSinkBin.Add(audiosink);
                convert.Link(audiosink);
                Pad pad = convert.GetStaticPad("sink");
                GhostPad ghostpad = new GhostPad("sink", pad);
                ghostpad.SetActive(true);
                audioSinkBin.AddPad(ghostpad);

                _Element = ElementFactory.Make("playbin", "playbin");
                _Element["audio-sink"] = audioSinkBin;
                _Element["flags"] = 1 << 1;
                _Element["uri"] = new Uri(media).AbsoluteUri;
                _Element.SetState(State.Paused);

                if (prescan)
                    _Element.Bus.TimedPopFiltered(0xffffffffffffffff, MessageType.AsyncDone);

                _Element.Bus.Message += OnMessage;
            }

            private void OnMessage(object o, MessageArgs args)
            {
                switch (args.Message.Type)
                {
                    case MessageType.Eos:
                        if (_Loop)
                            Position = 0;
                        else
                            Close();
                        break;
                }
            }


            public void Close()
            {
                _Element.SetState(State.Null);
                _Closed = true;
                _Finished = true;
            }

            public void Play(bool loop = false)
            {
                _Element.SetState(State.Playing);
                this._Loop = loop;
            }

            public void Stop()
            {
                _Element.SetState(State.Null);
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

            public void FadeAndStop(float targetVolume, float seconds)
            {
                Fade(targetVolume, seconds);
                _StopStreamAfterFade = true;
            }

            public float Volume
            {
                get
                {
                    return (float)(double)_Element["volume"];
                }
                set
                {
                    _Element["volume"] = (double) (value / 100d * (_MaxVolume / 100d));
                    System.Console.WriteLine(value);
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
                    Volume = Volume * (MaxVolume / 100f);
                }
            }

            public float Length
            {
                get
                {
                    long duration;
                    _Element.QueryDuration(Format.Time, out duration);
                    return duration > 0 ? (duration / (long)Constants.SECOND) : 100;
                }
            }

            public float Position
            {
                get
                {
                    long position;
                    _Element.QueryPosition(Format.Time, out position);
                    return (float)(position / (double)Constants.SECOND);
                }
                set
                {
                    _Element.SeekSimple(Format.Time, SeekFlags.Accurate | SeekFlags.Flush, (long)(value * (double)Constants.SECOND));
                }
            }

            public bool Paused
            {
                get
                {
                    return _Element.CurrentState == State.Paused;
                }
                set
                {
                    if (value)
                        _Element.SetState(State.Paused);
                }
            }

            public bool Playing
            {
                get
                {
                    return _Element.CurrentState == State.Playing;
                }
                set
                {
                    if(value)
                        _Element.SetState(State.Playing);
                }
            }

            public void Update()
            {
                if (_Fading)
                {
                    if (_FadeTimer.ElapsedMilliseconds < _FadeTime * 1000f)
                        Volume = (_FadeTimer.ElapsedMilliseconds / 10f) / _FadeTime;
                    else
                    {
                        if (_StopStreamAfterFade)
                            Stop();
                        if (_PauseStreamAfterFade)
                            Paused = true;

                        _StopStreamAfterFade = false;
                        _PauseStreamAfterFade = false;
                        _Fading = false;
                        _FadeTimer.Reset();
                    }
                }
            }
        }
    }
}
