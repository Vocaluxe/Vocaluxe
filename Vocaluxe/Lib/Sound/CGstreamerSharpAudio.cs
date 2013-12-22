using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gst;
using GLib;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound
{
    public class CGstreamerSharpAudio : IPlayback
    {
        private Dictionary<int, Element> Streams = new Dictionary<int, Element>();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void SetDllDirectory(string lpPathName);

        public bool Init()
        {
#if ARCH_X86
            SetDllDirectory(".\\x86\\gstreamer");
#endif
#if ARCH_X64
            SetDllDirectory(".\\x64\\gstreamer");
#endif
            Application.Init();
            return Application.IsInitialized;
        }

        public void SetGlobalVolume(float volume)
        {
            foreach (Element e in Streams.Values)
            {
                e["volume"] = (double)volume;
            }
        }

        public int GetStreamCount()
        {
            return Streams.Count;
        }

        public void CloseAll()
        {
            foreach (Element e in Streams.Values)
            {
                e.SetState(State.Null);
            }
        }

        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            Registry reg = Registry.Get();
#if ARCH_X86
            reg.ScanPath(".\\x86\\gstreamer");
#endif
#if ARCH_X64
            reg.ScanPath(".\\x64\\gstreamer");
#endif

            Element element = ElementFactory.Make("playbin", "playbin");
            element["audio-sink"] = ElementFactory.Make("directsoundsink", "audiosink");
            element["uri"] = new Uri(media).AbsoluteUri;
            element.SetState(State.Paused);
            Streams[(int)element.Handle.ToInt64()] = element;

            if (prescan)
                element.Bus.TimedPopFiltered(0xffffffffffffffff, MessageType.AsyncDone);

            return (int)element.Handle.ToInt64();
        }

        public void Close(int stream)
        {
            Streams[stream].SetState(State.Null);
            Streams.Remove(stream);
        }

        public void Play(int stream)
        {
            Streams[stream].SetState(State.Playing);
        }

        public void Play(int stream, bool loop)
        {
            Play(stream);
        }

        public void Pause(int stream)
        {
            Streams[stream].SetState(State.Playing);
        }

        public void Stop(int stream)
        {
            Streams[stream].SetState(State.Null);
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            SetStreamVolume(stream, targetVolume);
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            SetStreamVolume(stream, targetVolume);
            Pause(stream);
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            SetStreamVolume(stream, targetVolume);
            Stop(stream);
        }

        public void SetStreamVolume(int stream, float volume)
        {
            Streams[stream]["volume"] = (double)volume/100d;
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            
        }

        public float GetLength(int stream)
        {
            long duration;
            Streams[stream].QueryDuration(Format.Time, out duration);
            return duration > 0 ? (duration / (long)Constants.SECOND) : 100;
        }

        public float GetPosition(int stream)
        {
            long position;
            Streams[stream].QueryPosition(Format.Time, out position);
            return (position / (long)Constants.SECOND);
        }

        public bool IsPlaying(int stream)
        {
            return true;
        }

        public bool IsPaused(int stream)
        {
            return false;
        }

        public bool IsFinished(int stream)
        {
            return false;
        }

        public void SetPosition(int stream, float position)
        {
            Streams[stream].SeekSimple(Format.Time, SeekFlags.Accurate, (long)(position * (double)Constants.SECOND));
        }

        public void Update()
        {
            
        }
    }
}
