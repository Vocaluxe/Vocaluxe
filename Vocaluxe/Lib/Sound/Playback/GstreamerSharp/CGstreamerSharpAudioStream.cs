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
    class CGstreamerSharpAudioStream : CAudioStreamBase
    {
        private Element _Element;
        private bool _FileOpened;

        private volatile bool _IsFinished;

        private volatile float _Position;

        private readonly object _ElementLock = new object(); //Hold this if you set _Element=null or when accessing _Element from outside the main thread

        public override float Volume
        {
            get { return base.Volume; }
            set
            {
                base.Volume = value;
                _SetElementVolume();
            }
        }

        public override float VolumeMax
        {
            get { return base.VolumeMax; }
            set
            {
                base.VolumeMax = value;
                _SetElementVolume();
            }
        }

        public override float Length
        {
            get
            {
                //TODO: Is it ok, to remove this? Gst should post a message if duration was changed so this should not be required
                // Removing would not work in some cases where length is not know at startup
                if (base.Length < 0 && _Element != null)
                    _UpdateDuration();
                return base.Length >= 0f ? base.Length : 0f;
            }
        }

        public override float Position
        {
            get
            {
                
                if (_Element == null || (_Element.CurrentState == State.Paused && _Element.PendingState != State.VoidPending))
                {
                    return _Position;
                }
                    
                    
                long position;
                if (!_Element.QueryPosition(Format.Time, out position))
                    CLog.LogError("Could not query position");
                else
                    _Position = ((float)position / Constants.SECOND);
                    
                    
                return _Position;
            }
            set
            {
                if (_Element != null)
                    _Element.SeekSimple(Format.Time, SeekFlags.Accurate | SeekFlags.Flush, (long)(value * Constants.SECOND));
            }
        }

        public override bool IsFinished
        {
            get { return _Element == null || _IsFinished; }
        }

        public override bool IsPaused
        {
            get { return _Element == null || _Element.TargetState == State.Paused; }
            set
            {
                if (_Element != null)
                    _Element.SetState(value ? State.Paused : State.Playing);
            }
        }

        public CGstreamerSharpAudioStream(int id, string medium, bool loop, EAudioEffect effect = EAudioEffect.None) : base(id, medium, loop, effect) {}

        public override bool Open(bool prescan)
        {
            System.Diagnostics.Debug.Assert(!_FileOpened);
            if (_FileOpened)
                return false;
            Length = -1;
            Element convert = ElementFactory.Make("audioconvert", "convert");
            Element audiosink = ElementFactory.Make("autoaudiosink", "audiosink");

            if (convert == null || audiosink == null)
            {
                CLog.LogError("Could not create pipeline");
                if (convert != null)
                    convert.Dispose();
                if (audiosink != null)
                    audiosink.Dispose();
                return false;
            }

            var audioSinkBin = new Bin("Audiosink");
            Element audiokaraoke = null;
            if (_Effect.HasFlag(EAudioEffect.Karaoke))
            {
                audiokaraoke = ElementFactory.Make("audiokaraoke", "karaoke");
                audioSinkBin.Add(audiokaraoke);
                audioSinkBin.Add(convert); 
                audioSinkBin.Add(audiosink); 
                
                audiokaraoke.Link(audiosink);
                audiokaraoke["level"] = CConfig.Config.Sound.KaraokeEffectLevel;
                audiokaraoke["mono-level"] = CConfig.Config.Sound.KaraokeEffectLevel;
               
                convert.Link(audiokaraoke);
            }
            else
            {
                audioSinkBin.Add(convert);
                audioSinkBin.Add(audiosink);
                convert.Link(audiosink);
            }

            
            
            Pad pad = convert.GetStaticPad("sink");
            GhostPad ghostpad = new GhostPad("sink", pad);

            if (pad == null)
            {
                CLog.LogError("Could not create pads");
                convert.Dispose();
                audiosink.Dispose();
                audioSinkBin.Dispose();
                if (audiokaraoke != null)
                {
                    audiokaraoke.Dispose();
                }
                return false;
            }

            if (!ghostpad.SetActive(true))
            {
                CLog.LogError("Could not link pads");
                convert.Dispose();
                audiosink.Dispose();
                audioSinkBin.Dispose();
                ghostpad.Dispose();
                if (audiokaraoke != null)
                {
                    audiokaraoke.Dispose();
                }
                return false;
            }
            if (!audioSinkBin.AddPad(ghostpad))
            {
                CLog.LogError("Could not add pad");
                convert.Dispose();
                audiosink.Dispose();
                audioSinkBin.Dispose();
                ghostpad.Dispose();
                if (audiokaraoke != null)
                {
                    audiokaraoke.Dispose();
                }
                return false;
            }

            _Element = ElementFactory.Make("playbin", "playbin");
            if (_Element == null)
            {
                CLog.LogError("Could not create playbin");
                convert.Dispose();
                audiosink.Dispose();
                audioSinkBin.Dispose();
                ghostpad.Dispose();
                if (audiokaraoke != null)
                {
                    audiokaraoke.Dispose();
                }
                return false;
            }
            _Element["audio-sink"] = audioSinkBin;
            _Element["flags"] = 1 << 1;
            _Element["uri"] = new Uri(_Medium).AbsoluteUri;
            _Element.SetState(State.Paused);

            // Passing CLOCK_TIME_NONE here causes the pipeline to block for a long time so with
            // prescan enabled the pipeline will wait 500ms for stream to initialize and then continue
            // if it takes more than 500ms, duration queries will be performed asynchronously
            Message msg = _Element.Bus.TimedPopFiltered(prescan ? ulong.MaxValue : 0L, MessageType.AsyncDone | MessageType.Error);
            if (!_OnMessage(msg))
            {
                _Dispose(true);
                return false;
            }
            _FileOpened = true;
            return true;
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Element == null)
                return;
            Element element;
            lock (_ElementLock)
            {
                //Atomic get and set
                if (_Element == null)
                    return;
                element = _Element;
                _Element = null; //Now everything "seems" closed
            }

            // Should not be needed but we had an error report (#226) - so we double check here to prevent a crash
            if (element == null)
                return;
            
            if (element.TargetState == State.Playing)
                element.SetState(State.Paused); //Stop output
            if (_CloseStreamListener != null)
                _CloseStreamListener.OnCloseStream(this);
            //Now really close it in the background
            var t = new System.Threading.Thread(() => _TerminateStream(element)) {Name = "GSt Terminate"};
            t.Start();
        }

        private static void _TerminateStream(Element element)
        {
            if (element == null)
                return;
            //One of these might take a bit, so better do this in the background to avoid lags
            element.SetState(State.Null);
            element.Dispose();
        }

        public override void Play()
        {
            if (_Element != null)
                _Element.SetState(State.Playing);
        }

        public override void Stop()
        {
            if (_Element != null)
                _Element.SetState(State.Ready);
            Position = 0;
        }

        private void _SetElementVolume()
        {
            if (_Element != null)
                _Element["volume"] = Volume * VolumeMax;
        }

        private bool _OnMessage(Message msg)
        {
            if (msg == null || msg.Handle == IntPtr.Zero)
                return true;
            switch (msg.Type)
            {
                case MessageType.Eos:
                    if (_Loop)
                        Position = 0;
                    else
                        _IsFinished = true;
                    break;
                case MessageType.Error:
                    GException error;
                    string debug;
                    msg.ParseError(out error, out debug);
                    CLog.LogError("Gstreamer error: message" + error.Message + ", code" + error.Code + " ,debug information" + debug);
                    return false;
                case MessageType.DurationChanged:
                    _UpdateDuration();
                    break;
            }
            msg.Dispose();
            return true;
        }

        public override void Update()
        {
            base.Update();
            while (_Element != null && _Element.Bus != null && _Element.Bus.HavePending())
                _OnMessage(_Element.Bus.Pop());
        }

        private void _UpdateDuration()
        {
            lock (_ElementLock)
            {
                if (_Element == null)
                    return;
                long duration;
                if (_Element.QueryDuration(Format.Time, out duration))
                    Length = duration / (float)Constants.SECOND;
            }
        }
    }
}