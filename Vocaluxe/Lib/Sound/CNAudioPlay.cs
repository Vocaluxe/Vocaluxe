using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    class NAudioStream
    {
        const float DELAY = 0.15f;

        private string _FileName;
        
        private IWavePlayer _waveOutDevice;
        private WaveStream _mainOutputStream;
        private WaveChannel32 _volumeStream;
        
        private bool _loaded;
        private float _Volume = 1f;
        private float _Position = 0f;

        private Stopwatch _Timer;
        private float _CurrentTime = 0f;

        private Stopwatch _fadeTimer;

        private float _fadeTime;
        private float _targetVolume;
        private float _startVolume;
        private bool _closeStreamAfterFade = false;
        private bool _fading = false;
        
        public int Handle;
        
        public NAudioStream(string FileName)
        {
            _loaded = false;
            _FileName = FileName;
            Handle = 0;
            _Timer = new Stopwatch();
            _fadeTimer = new Stopwatch();

            try
            {
                _waveOutDevice = new WaveOut();
                _mainOutputStream = CreateInputStream(FileName);
                _waveOutDevice.Init(_mainOutputStream);
            }
            catch (Exception)
            {
                _loaded = false;
            }
            _loaded = true;
        }

        public void Play()
        {
            if (_loaded)
            {
                if (IsPaused())
                {
                    _waveOutDevice.Play();
                    _Timer.Start();
                }
                else
                {
                    _waveOutDevice.Play();
                    Position = _Position;
                    Volume = _Volume;
                    _CurrentTime = Position;
                    _Timer.Start();
                }
            }
        }

        public void Stop()
        {
            if (_loaded)
            {
                _waveOutDevice.Stop();
                _Timer.Stop();
                _Timer.Reset();
                _Position = 0f;
                _CurrentTime = _Position;
            }
        }

        public void Pause()
        {
            if (_loaded)
            {
                _waveOutDevice.Pause();
                _Timer.Stop();
            }
        }

        public void Close()
        {
            CloseWaveOut();
            _Timer.Stop();
        }

        public float Length
        {
            get
            {
                if (_loaded)
                {
                    TimeSpan tt = _volumeStream.TotalTime;
                    return (float)tt.TotalSeconds;
                }

                return 0f;
            }
        }

        public float Position
        {
            get
            {
                if (_loaded && _volumeStream != null)
                {
                    TimeSpan tt = _volumeStream.CurrentTime;
                    float time = (float)tt.TotalSeconds;
                    if (time != _CurrentTime)
                    {
                        _CurrentTime = time;
                        _Timer.Stop();
                        _Timer.Reset();
                        _Timer.Start();
                    }
                    else
                    {
                        time = _CurrentTime + _Timer.ElapsedMilliseconds / 1000f;
                    }
                    
                    //Console.WriteLine(time.ToString("#0.000"));
                    return time - DELAY;
                }
                return 0f;
            }

            set
            {
                if (_loaded && _volumeStream != null)
                {
                    float pos = value;
                    if (pos > Length)
                        pos = Length;

                    if (pos < 0L)
                        pos = 0L;

                    int skip = (int)(pos - _CurrentTime);

                    _volumeStream.Skip(skip);
                    _CurrentTime += skip;
                }
                _Position = value;
            }
        }

        public float Volume
        {
            get
            {
                if (_loaded && _volumeStream != null)
                {
                    return _volumeStream.Volume;
                }
                return 0f;
            }

            set
            {
                if (_loaded && _volumeStream != null)
                {
                    float volume = value;
                    if (volume > 1f)
                        volume = 1f;

                    if (volume < 0f)
                        volume = 0f;

                    _volumeStream.Volume = volume;
                }
                _Volume = value;
            }
        }

        public bool IsPlaying()
        {
            if (_loaded && _waveOutDevice != null)
                return _waveOutDevice.PlaybackState == PlaybackState.Playing;

            return false;
        }

        public bool IsPaused()
        {
            if (_loaded && _waveOutDevice != null)
                return _waveOutDevice.PlaybackState == PlaybackState.Paused;

            return false;
        }

        public bool IsFinished()
        {
            if (_loaded && _waveOutDevice != null)
                return _waveOutDevice.PlaybackState == PlaybackState.Stopped;

            return false;
        }

        public bool Update()
        {
            if (_fading)
            {
                if ((float)_fadeTimer.ElapsedMilliseconds / 1000f < _fadeTime)
                    Volume = _startVolume + (_targetVolume - _startVolume) * ((_fadeTimer.ElapsedMilliseconds / 1000f) / _fadeTime);
                else
                {
                    Volume = _targetVolume;
                    _fadeTimer.Stop();
                    _fading = false;

                    if (_closeStreamAfterFade)
                        return false;
                }
                return true;
            }
            return true;
        }

        public void Fade(float TargetVolume, float FadeTime)
        {
            if (!_loaded || _volumeStream == null)
                return;

            _fading = true;
            _fadeTimer.Stop();
            _fadeTimer.Reset();
            _startVolume = Volume;
            _targetVolume = TargetVolume / 100f;
            _fadeTime = FadeTime;
            _fadeTimer.Start();
        }

        public void FadeAndStop(float TargetVolume, float FadeTime)
        {
            if (!_loaded || _volumeStream == null)
                return;

            _closeStreamAfterFade = true;
            Fade(TargetVolume, FadeTime);
        }

        private WaveStream CreateInputStream(string fileName)
        {
            WaveChannel32 inputStream;
            if (fileName.EndsWith(".mp3"))
            {
                WaveStream mp3Reader = new Mp3FileReader(fileName);
                inputStream = new WaveChannel32(mp3Reader);
                
            }
            else
            {
                throw new InvalidOperationException("Unsupported extension");
            }
            _volumeStream = inputStream;
            return _volumeStream;
        }

        private void CloseWaveOut()
        {
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Stop();
            }
            if (_mainOutputStream != null)
            {
                // this one really closes the file and ACM conversion
                _volumeStream.Close();
                _volumeStream = null;
                // this one does the metering stream
                _mainOutputStream.Close();
                _mainOutputStream = null;
            }
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Dispose();
                _waveOutDevice = null;
            }
        }
    }



    class CNAudioPlay : IPlayback
    {
        private bool _Initialized = false;
        private int _HandleCounter = 0;
        private List<NAudioStream> _Streams;

        private Object MutexAudioStreams = new Object();
   
        public CNAudioPlay()
        {
            Init();
        }

        public bool Init()
        {
            if (_Initialized)
            {
                foreach (NAudioStream stream in _Streams)
                {
                    stream.Close();
                }
                _Streams.Clear();
                _Initialized = false;
            }
            else
                _Streams = new List<NAudioStream>();

            _Initialized = true;
            return _Initialized;
        }

        public void CloseAll()
        {
            while (_Streams.Count > 0)
            {
                Close(_Streams[_Streams.Count - 1].Handle);
            }
            lock (MutexAudioStreams)
            {
                _Streams.Clear();
            }
        }

        public void SetGlobalVolume(float Volume)
        {
            if (_Initialized)
            {
                //Bass.BASS_SetVolume(Volume / 100f);
            }
        }

        public int GetStreamCount()
        {
            if (!_Initialized)
                return 0;

            lock (MutexAudioStreams)
            {
                return _Streams.Count;
            }
        }

        public void Update()
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    List<int> StreamsToClose = new List<int>();

                    for (int i = 0; i < _Streams.Count; i++)
                    {
                        if (!_Streams[i].Update())
                            StreamsToClose.Add(_Streams[i].Handle);
                    }

                    foreach (int stream in StreamsToClose)
                    {
                        Close(stream);
                    }
                }

            }
        }

        #region Stream Handling
        public int Load(string Media)
        {
            return Load(Media, false);
        }

        public int Load(string Media, bool Prescan)
        {
            if (!_Initialized)
                return 0;

            //BASSFlag flags = BASSFlag.BASS_DEFAULT;
            //if (Prescan)
            //    flags = BASSFlag.BASS_STREAM_PRESCAN;

            NAudioStream stream = new NAudioStream(Media);
            stream.Handle = ++_HandleCounter;
            //stream.handle = Bass.BASS_StreamCreateFile(Media, 0L, 0L, flags);

            if (stream.Handle != 0)
            {
                lock (MutexAudioStreams)
                {
                    _Streams.Add(stream);
                }

                return stream.Handle;
            }
            return 0;
        }

        public void Close(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Close();
                        //Bass.BASS_StreamFree(Stream);
                        _Streams.RemoveAt(index);
                    }
                }

            }
        }
        
        public void Play(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        Play(Stream, false);
                    }
                }

            }
        }

        public void Play(int Stream, bool Loop)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Play();
                        //if (Loop)
                            //Bass.BASS_ChannelFlags(Stream, BASSFlag.BASS_SAMPLE_LOOP, BASSFlag.BASS_SAMPLE_LOOP);

                        //Bass.BASS_ChannelPlay(Stream, false);
                    }
                }

            }
        }

        public void Pause(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Pause();
                        //Bass.BASS_ChannelPause(Stream);
                    }
                }

            }
        }

        public void Stop(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Stop();
                        //Bass.BASS_ChannelStop(Stream);
                    }
                }

            }
        }

        public void Fade(int Stream, float TargetVolume, float Seconds)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Fade(TargetVolume, Seconds);                        
                    }
                }

            }
        }

        public void FadeAndStop(int Stream, float TargetVolume, float Seconds)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    //Stop(Stream);
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].FadeAndStop(TargetVolume, Seconds); 
                    }
                }

            }
        }

        public void SetStreamVolume(int Stream, float Volume)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Volume = Volume / 100f;
                        //Bass.BASS_ChannelSetAttribute(Stream, BASSAttribute.BASS_ATTRIB_VOL, Volume / 100f);
                    }
                }

            }
        }

        public float GetLength(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        return _Streams[index].Length;
                        //long len = Bass.BASS_ChannelGetLength(Stream);
                        //return (float)Bass.BASS_ChannelBytes2Seconds(Stream, len);
                    }
                }

                return 0f;
            }
            return 0f;
        }

        public float GetPosition(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        return _Streams[index].Position;
                        //long pos = Bass.BASS_ChannelGetPosition(Stream);
                        //return (float)Bass.BASS_ChannelBytes2Seconds(Stream, pos);
                    }
                }

                return 0f;
            }
            return 0f;
        }

        public bool IsPlaying(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        return _Streams[index].IsPlaying();
                        //return (Bass.BASS_ChannelIsActive(Stream) == BASSActive.BASS_ACTIVE_PLAYING);
                    }
                }

            }
            return false;
        }

        public bool IsPaused(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        return _Streams[index].IsPaused();
                        //return (Bass.BASS_ChannelIsActive(Stream) == BASSActive.BASS_ACTIVE_PAUSED);
                    }
                }

            }
            return false;
        }

        public bool IsFinished(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        return _Streams[index].IsFinished();
                        //return (Bass.BASS_ChannelIsActive(Stream) == BASSActive.BASS_ACTIVE_STOPPED);
                    }
                }

            }
            return true;
        }

        public void SetPosition(int Stream, float Position)
        {
            if (_Initialized)
            {
                lock (MutexAudioStreams)
                {
                    if (AlreadyAdded(Stream))
                    {
                        int index = GetStreamIndex(Stream);
                        _Streams[index].Position = Position;
                        //Bass.BASS_ChannelSetPosition(Stream, Position);
                    }
                }

            }
        }
        #endregion Stream Handling


        private bool AlreadyAdded(int Stream)
        {
            foreach (NAudioStream st in _Streams)
            {
                if (st.Handle == Stream)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetStreamIndex(int Stream)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].Handle == Stream)
                    return i;
            }
            return -1;
        }

        private void EndSync(int handle, int Stream, int data, IntPtr user)
        {
            if (_Initialized)
            {
                if (AlreadyAdded(Stream))
                {
                    Close(Stream);
                }
            }
        }
    
    }
}
