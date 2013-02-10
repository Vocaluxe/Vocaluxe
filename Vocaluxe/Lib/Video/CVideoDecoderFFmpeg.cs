using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Video.Acinerella;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Video
{
    delegate void CLOSEPROC(int StreamID);

    class CVideoDecoderFFmpeg : CVideoDecoder
    {
        private List<Decoder> _Decoder = new List<Decoder>();
        private CLOSEPROC closeproc;
        private int _Count = 1;

        private Object MutexDecoder = new Object();
                
        
        public override bool Init()
        {
            closeproc = new CLOSEPROC(close_proc);
            CloseAll();
                        
            return base.Init();
        }

        public override void CloseAll()
        {
            lock (MutexDecoder)
            {
                for (int i = 0; i < _Decoder.Count; i++)
			    {
			        _Decoder[i].Free(closeproc, i+1);
			    }
            }
        }

        public override int Load(string VideoFileName)
        {
            VideoStreams stream = new VideoStreams(0);
            Decoder decoder = new Decoder();

            if (decoder.Open(VideoFileName))
            {
                lock (MutexDecoder)
                {
                    _Decoder.Add(decoder);
                    stream.handle = _Count++;
                    stream.file = VideoFileName;
                    _Streams.Add(stream);
                    return stream.handle;
                }
                
            }
            return -1;
        }

        public override bool Close(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        _Decoder[GetStreamIndex(StreamID)].Free(closeproc, StreamID);
                        return true;
                    }
                }
                
            }
            return false;
        }

        public override bool GetFrame(int StreamID, ref STexture Frame, float Time, ref float VideoTime)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        return _Decoder[GetStreamIndex(StreamID)].GetFrame(ref Frame, Time, ref VideoTime);
                    }
                }
                
            }
            return false;
        }

        public override float GetLength(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        return _Decoder[GetStreamIndex(StreamID)].Length;
                    }
                }
                
            }
            return 0f;
        }

        public override bool Skip(int StreamID, float Start, float Gap)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        return _Decoder[GetStreamIndex(StreamID)].Skip(Start, Gap);
                    }
                }
                
            }
            return false;
        }

        public override void SetLoop(int StreamID, bool Loop)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        _Decoder[GetStreamIndex(StreamID)].Loop = Loop;
                    }
                }
                
            }
        }

        public override void Pause(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        _Decoder[GetStreamIndex(StreamID)].Paused = true;
                    }
                }

            }
        }

        public override void Resume(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        _Decoder[GetStreamIndex(StreamID)].Paused = false;
                    }
                }

            }
        }

        public override bool Finished(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        return _Decoder[GetStreamIndex(StreamID)].Finished;
                    }
                }
            }
            return true;
        }

        private void close_proc(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        int Index = GetStreamIndex(StreamID);
                        _Decoder.RemoveAt(Index);
                        _Streams.RemoveAt(Index);
                    }
                }
                
            }
        }
    }

    struct SFrameBuffer
    {
        public byte[] data;
        public float time;
        public bool displayed;
    }

    class Decoder
    {
        private IntPtr _instance = IntPtr.Zero;     // acinerella instance
        private IntPtr _videodecoder = IntPtr.Zero; // acinerella video decoder instance
          
        private Stopwatch _LoopTimer = new Stopwatch();
        private CLOSEPROC _Closeproc;               // delegate for stream closing
        private int _StreamID;                      // stream ID for stream closing
        private string _FileName;                   // current video file name
                
        private FileStream _fs;                     // video file stream
        private TAc_read_callback _rc;              // read callback for acinerella
        private TAc_seek_callback _sc;              // seek callback for acinerella
        
        private bool _FileOpened = false;
        
        private float _VideoTimeBase = 0f;          // frame time
        private float _VideoDecoderTime = 0f;       // time of last decoded frame
        private float _CurrentVideoTime = 0f;       // current video position
        private bool _BufferFull = false;           // buffer is full, waiting for free frame slot

        private bool _skip = false;                 // do skip
        private float _Time = 0f;
        private float _Gap = 0f;
        private float _Start = 0f;
        private bool _Loop = false;
        private float _Duration = 0f;
        private float _VideoSkipTime = 0f;          // = VideoGap
        private float _SkipTime = 0f;               // = Start + VideoGap
        
        private bool _Paused = false;
        private bool _NoMoreFrames = false;
        private bool _Finished = false;
        private bool _BeforeLoop = false;
        
        
        private int _Width = 0;
        private int _Height = 0;
        private float _SetTime = 0f;
        private float _SetGap = 0f;
        private float _SetStart = 0f;
        private bool _SetLoop = false;
        private bool _SetSkip = false;
        private bool _terminated = false;
                
        private Thread _thread;
        //AutoResetEvent EventDecode = new AutoResetEvent(false);
        SFrameBuffer[] _FrameBuffer = new SFrameBuffer[5];
        private bool _NewFrame = false;
        Object MutexFramebuffer = new Object();
        Object MutexSyncSignals = new Object();

        public Decoder()
        {
            _rc = new TAc_read_callback(read_proc);
            _sc = new TAc_seek_callback(seek_proc);            
            _thread = new Thread(Execute);
        }

        public void Free(CLOSEPROC close_proc, int StreamID)
        {
            _Closeproc = close_proc;
            _StreamID = StreamID;
            _terminated = true;                 
        }

        public float Length
        {
            get
            {
                if (_FileOpened)
                    return _Duration;

                return 0f;
            }
        }

        public bool Paused
        {
            get { return _Paused; }
            set 
            { 
                lock (MutexSyncSignals)
                {
                    _Paused = value;
                    if (_Paused)
                    {
                        _LoopTimer.Stop();
                    }
                    else
                    {
                        _LoopTimer.Start();
                    }
                }
            }
        }

        public bool Loop
        {
            get { return _SetLoop; }
            set { _SetLoop = value; }
        }

        public bool Finished
        {
            get
            {
                return _Finished;
            }
        }

        public bool Open(string FileName)
        {
            if (_FileOpened)
                return false;

            if (!System.IO.File.Exists(FileName))
                return false;

            _FileName = FileName;
            _thread.Priority = ThreadPriority.Normal;
            _thread.Name = Path.GetFileName(FileName);
            _thread.Start();
            return true;
        }
       
        public bool GetFrame(ref STexture frame, float Time, ref float VideoTime)
        {
            if (!_FileOpened)
                return false;

            if (_Paused)
                return false;

            if (_SetLoop)
            {
                lock (MutexSyncSignals)
                { 
                    _SetTime += _LoopTimer.ElapsedMilliseconds / 1000f;
                    VideoTime = _SetTime;

                    _LoopTimer.Stop();
                    _LoopTimer.Reset();
                    _LoopTimer.Start();
                }

                
                UploadNewFrame(ref frame);
                //EventDecode.Set();
                return true;
            }

            if (_SetTime != Time)
            {
                lock (MutexSyncSignals)
                {
                    _SetTime = Time;                   
                }
                UploadNewFrame(ref frame);
                VideoTime = _CurrentVideoTime;
                //EventDecode.Set();
                return true;
            }
            return false;
        }

        public bool Skip(float Start, float Gap)
        {
            lock (MutexSyncSignals)
            {
                _SetStart = Start;
                _SetGap = Gap;
                _SetSkip = true;
                _NoMoreFrames = false;
                _Finished = false;
            }
            //EventDecode.Set();

            return true;
        }

        #region Threading
        private void DoSkip()
        {            
            _VideoSkipTime = _Gap;
            _SkipTime = _Start + _Gap;
            _BeforeLoop = false;           

            if (_SkipTime > 0)
            {
                _VideoDecoderTime = _SkipTime;
                try
                {
                    CAcinerella.ac_seek(_videodecoder, -1, (Int64)(_SkipTime * 1000f));
                }
                catch (Exception e)
                {                   
                    CLog.LogError("Error seeking video file \"" + _FileName + "\": " + e.Message);
                }
                
            }
            else
            {
                _VideoDecoderTime = 0f;
                try 
	            {
                    CAcinerella.ac_seek(_videodecoder, -1, (Int64)0);
	            }
	            catch (Exception e)
	            {
                    CLog.LogError("Error seeking video file \"" + _FileName + "\": " + e.Message);
	            }              
            }

            lock (MutexSyncSignals)
            {
                _CurrentVideoTime = _VideoDecoderTime;
            }
            
            lock (MutexFramebuffer)
            {
                for (int i = 0; i < _FrameBuffer.Length; i++)
                {
                    _FrameBuffer[i].displayed = true;
                    _FrameBuffer[i].time = -1f;
                }
            }
            
            _BufferFull = false;
            _skip = false;
            _NewFrame = false;
            //EventDecode.Set();
        }

        private void Execute()
        {
            DoOpen();
            //EventDecode.Set();

            while (!_terminated)
            {
                //if (EventDecode.WaitOne(10))
                {
                    lock (MutexSyncSignals)
                    {
                        _Time = _SetTime;
                        if (_SetSkip)
                            _skip = true;

                        _SetSkip = false;
                        _Gap = _SetGap;
                        _Start = _SetStart;
                        _Loop = _SetLoop;
                    }

                    if (_skip)
                        DoSkip();

                    if (!_NewFrame)
                        DoDecode();

                    if (_NewFrame)
                        Copy();
                    Thread.Sleep(1);
                }
            }

            DoFree(); 
        }

        private void DoOpen()
        {
            bool ok = false;
            TAc_instance Instance = new TAc_instance();
            try
            {
                _fs = new FileStream(_FileName, FileMode.Open, FileAccess.Read, FileShare.Read);


                _instance = CAcinerella.ac_init();
                CAcinerella.ac_open(_instance, IntPtr.Zero, null, _rc, _sc, null, IntPtr.Zero);

                Instance = (TAc_instance)Marshal.PtrToStructure(_instance, typeof(TAc_instance));
                ok = true;
            }
            catch (Exception)
            {
                CLog.LogError("Error opening video file: " + _FileName);
                ok = false;
            }
            

            if (!Instance.opened || !ok)
            {
                //Free();
                return;
            }

            _Duration = (float)Instance.info.duration / 1000f;

            int VideoStreamIndex = -1;

            TAc_stream_info Info = new TAc_stream_info();
            for (int i = 0; i < Instance.stream_count; i++)
            {
                CAcinerella.ac_get_stream_info(_instance, i, out Info);

                if (Info.stream_type == TAc_stream_type.AC_STREAM_TYPE_VIDEO)
                {
                    _videodecoder = CAcinerella.ac_create_decoder(_instance, i);
                    
                    VideoStreamIndex = i;
                    break;
                }
            }

            if (VideoStreamIndex < 0)
            {
                //Free();
                return;
            }

            TAc_decoder Videodecoder = (TAc_decoder)Marshal.PtrToStructure(_videodecoder, typeof(TAc_decoder));

            _Width = Videodecoder.stream_info.video_info.frame_width;
            _Height = Videodecoder.stream_info.video_info.frame_height;

            if (Videodecoder.stream_info.video_info.frames_per_second > 0)
                _VideoTimeBase = 1f / (float)Videodecoder.stream_info.video_info.frames_per_second;

            _VideoDecoderTime = 0f;
            _Time = 0f;

            for (int i = 0; i < _FrameBuffer.Length; i++)
            {
                _FrameBuffer[i].time = -1f;
                _FrameBuffer[i].displayed = true;
                _FrameBuffer[i].data = new byte[_Width * _Height * 4];
            }
            _FileOpened = true;
        }

        private void DoDecode()
        {
            const int FRAMEDROPCOUNT = 4;

            if (!_FileOpened || _NewFrame)
                return;

            if (_Paused || _NoMoreFrames || _BufferFull)
                return;

            if ((_SkipTime < 0f) && (_Time + _SkipTime >= 0f))
                _SkipTime = 0f;

            float myTime = _Time + _VideoSkipTime;
            float TimeDifference = myTime - _VideoDecoderTime;

            bool DropFrame = false;
            if (TimeDifference >= (FRAMEDROPCOUNT - 1) * _VideoTimeBase)
                DropFrame = true;

            if (_terminated)
                return;

            int FrameFinished = 0;
            if (DropFrame && !_BeforeLoop)
            {
                try
                {
                    FrameFinished = CAcinerella.ac_skip_frames(_instance, _videodecoder, FRAMEDROPCOUNT - 1);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcSkipFrame " + _FileName);
                }
                
            }

            if (!_BeforeLoop && (!DropFrame || FrameFinished != 0))
            {
                try
                {
                    FrameFinished = CAcinerella.ac_get_frame(_instance, _videodecoder);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcGetFrame " + _FileName);
                }
            }

            if (FrameFinished == 0)
            {
                if (_Loop)
                {
                    _BeforeLoop = true;
                    bool doskip = true;
                    float tm = 0f;
                    int num = -1;
                    lock (MutexFramebuffer)
                    {
                        for (int i = 0; i < _FrameBuffer.Length; i++)
                        {
                            if (_FrameBuffer[i].time > tm && !_FrameBuffer[i].displayed)
                            {
                                tm = _FrameBuffer[i].time;
                                num = i;
                            }
                        }

                        if (num >= 0)
                            doskip = _FrameBuffer[num].displayed;
                    }

                    if (!doskip)
                        return;

                    lock (MutexSyncSignals)
                    {
                        _Start = 0f;
                        _Gap = 0f;
                        _SetTime = 0f;
                    }

                    DoSkip();
                }
                else
                {
                    _NoMoreFrames = true;
                }
                return;
            }
            else
            {
                _NewFrame = true;
                Copy();
            }
        }

        private void Copy()
        {
            if (!_NewFrame)
                return;

            int num = -1;
            
            lock (MutexFramebuffer)
            {
                for (int i = 0; i < _FrameBuffer.Length; i++)
                {
                    if (_FrameBuffer[i].displayed)
                    {
                        num = i;
                        break;
                    }
                }

                if (num == -1)
                {
                    _BufferFull = true;
                }
                else
                {
                    TAc_decoder Videodecoder = (TAc_decoder)Marshal.PtrToStructure(_videodecoder, typeof(TAc_decoder));

                    _VideoDecoderTime = (float)Videodecoder.timecode;
                    _FrameBuffer[num].time = _VideoDecoderTime;

                    if (Videodecoder.buffer != IntPtr.Zero)
                    {
                        Marshal.Copy(Videodecoder.buffer, _FrameBuffer[num].data, 0, _Width * _Height * 4);

                        _FrameBuffer[num].displayed = false;
                    }

                    int numfull = 0;
                    for (int i = 0; i < _FrameBuffer.Length; i++)
                    {
                        if (!_FrameBuffer[i].displayed)
                            numfull++;
                    }

                    if (numfull < _FrameBuffer.Length)
                        _BufferFull = false;

                    _NewFrame = false;
                }
            }

            //if (!_BufferFull)
            //    EventDecode.Set();
        }

        private void UploadNewFrame(ref STexture frame)
        {
            if (!_FileOpened)
                return;

            lock (MutexFramebuffer)
            {
                int num = FindFrame();

                if (num >= 0)
                {
                    if (frame.index == -1 || _Width != frame.width || _Height != frame.height)
                    {
                        CDraw.RemoveTexture(ref frame);
                        frame = CDraw.AddTexture(_Width, _Height, ref _FrameBuffer[num].data);
                    }
                    else
                    {
                        CDraw.UpdateTexture(ref frame, ref _FrameBuffer[num].data);
                    }

                    lock (MutexSyncSignals)
                    {
                        _CurrentVideoTime = _FrameBuffer[num].time;
                    }
                    _Finished = false;
                    //EventDecode.Set();
                }
                else
                {
                    if (_NoMoreFrames)
                        _Finished = true;
                }
            }
        }

        private int FindFrame()
        {
            int Result = -1;
            float diff = 10000000f;

            for (int i = 0; i < _FrameBuffer.Length; i++)
            {
                float td = _SetTime + _VideoSkipTime - _FrameBuffer[i].time;

                if (td > _VideoTimeBase * 2f)
                {
                    _FrameBuffer[i].displayed = true;
                }

                if (!_FrameBuffer[i].displayed && (td < diff) && (td > _VideoTimeBase))
                {
                    diff = Math.Abs(_FrameBuffer[i].time - _SetTime - _VideoSkipTime);
                    Result = i;
                }
            }

            if (Result != -1)
            {
                _FrameBuffer[Result].displayed = true;
                _BufferFull = false;
            }

            return Result;
        }

        private void DoFree()
        {
            if (_videodecoder != IntPtr.Zero)
                CAcinerella.ac_free_decoder(_videodecoder);

            if (_instance != IntPtr.Zero)
                CAcinerella.ac_close(_instance);

            if (_instance != IntPtr.Zero)
                CAcinerella.ac_free(_instance);

            _Closeproc(_StreamID);
        }
        #endregion Threading

        #region Callbacks
        private Int32 read_proc(IntPtr sender, IntPtr buf, Int32 size)
        {
            Int32 r = 0;

            byte[] bb = new byte[size];
            r = _fs.Read(bb, 0, size);
            Marshal.Copy(bb, 0, buf, size);

            return r;
        }

        private Int64 seek_proc(IntPtr sender, Int64 pos, Int32 whence)
        {
            return (Int64)_fs.Seek((long)pos, (SeekOrigin)whence);
        }
        #endregion Callbacks
    }
}
