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
using GLib;
using Gst;
using Vocaluxe.Base;
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback.GstreamerSharp
{
    public class CGstreamerSharpAudioStream
    {
        private Element _Element;
        private bool _Loop;
        public volatile bool Closed = true;
        public volatile bool Finished;

        private CFading _Fading;
        private EStreamAction _AfterFadeAction;
        private float _Volume = 1f;

        private float _MaxVolume = 1f;

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
                    {
                        Finished = true;
                        Close();
                    }
                    break;
                case MessageType.Error:
                    GException error;
                    string debug;
                    msg.ParseError(out error, out debug);
                    CLog.LogError("Gstreamer error: message" + error.Message + ", code" + error.Code + " ,debug information" + debug);
                    break;
                case MessageType.DurationChanged:
                    if (!_QueryingDuration)
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
                var t = new System.Threading.Thread(_TerminateStream) {Name = "GSt Terminate"};
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
            targetVolume.Clamp(0f, 100f);
            _Fading = new CFading(_Volume, targetVolume / 100f, seconds);
            _AfterFadeAction = EStreamAction.Nothing;
        }

        public void FadeAndPause(float targetVolume, float seconds)
        {
            Fade(targetVolume, seconds);
            _AfterFadeAction = EStreamAction.Pause;
        }

        public void FadeAndClose(float targetVolume, float seconds)
        {
            Fade(targetVolume, seconds);
            _AfterFadeAction = EStreamAction.Close;
        }

        public void FadeAndStop(float targetVolume, float seconds)
        {
            Fade(targetVolume, seconds);
            _AfterFadeAction = EStreamAction.Stop;
        }

        public float Volume
        {
            get { return _Volume * 100f; }
            set
            {
                value.Clamp(0f, 100f);
                _Volume = value / 100f;
                _SetElementVolume();
            }
        }

        private void _SetElementVolume()
        {
            if (_Element != null)
                _Element["volume"] = (_Volume * _MaxVolume);
        }

        public float MaxVolume
        {
            get { return _MaxVolume * 100f; }
            set
            {
                value.Clamp(0f, 100f);
                _MaxVolume = value / 100f;
                _SetElementVolume();
            }
        }

        public float Length
        {
            get
            {
                if (_Duration < 0 && !_QueryingDuration)
                {
                    _QueryingDuration = true; // Set this to avoid race conditions
                    var t = new System.Threading.Thread(_UpdateDuration) {Name = "GSt Update Duration"};
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

            if (_Fading != null)
            {
                bool finished;
                _Volume = _Fading.GetValue(out finished);
                _SetElementVolume();
                if (finished)
                {
                    switch (_AfterFadeAction)
                    {
                        case EStreamAction.Pause:
                            Paused = true;
                            break;
                        case EStreamAction.Stop:
                            Stop();
                            break;
                        case EStreamAction.Close:
                            Close();
                            break;
                    }
                    _Fading = null;
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