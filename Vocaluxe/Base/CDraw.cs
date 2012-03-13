using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    static class CDraw
    {
        private static Object mutex = new object();

        private static IDraw _Draw = null;
        
        public static void InitDraw()
        {
            switch (CConfig.Renderer)
            {
                case ERenderer.TR_CONFIG_SOFTWARE:
                    _Draw = new CDrawWinForm();
                    break;
                case ERenderer.TR_CONFIG_OPENGL:
                    _Draw = new COpenGL();
                    break;
                case ERenderer.TR_CONFIG_DIRECT3D:
                    _Draw = new CDirect3D();
                    break;

                default:
                    _Draw = new CDrawWinForm();
                    break;
            }
            _Draw.Init();
        }

        public static void MainLoop()
        {
            CGraphics.InitFirstScreen();
            _Draw.MainLoop();
        }

        public static bool Unload()
        {
            return _Draw.Unload();
        }

        public static int GetScreenWidth()
        {
            lock (mutex)
            {
                return _Draw.GetScreenWidth();
            }
        }

        public static int GetScreenHeight()
        {
            lock (mutex)
            {
                return _Draw.GetScreenHeight();
            }
        }

        public static RectangleF GetTextBounds(CText text)
        {
            lock (mutex)
            {
                return _Draw.GetTextBounds(text);
            }
        }

        public static RectangleF GetTextBounds(CText text, float Height)
        {
            lock (mutex)
            {
                return _Draw.GetTextBounds(text, Height);
            }
        }

        public static void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            lock (mutex)
            {
                _Draw.DrawLine(a, r, g, b, w, x1, y1, x2, y2);
            }
        }

        public static void DrawRect(SColorF Color, SRectF Rect, float Thickness)
        {
            lock (mutex)
            {
                if (Thickness <= 0f)
                    return;

                _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y - Thickness / 2, Rect.W + Thickness, Thickness, Rect.Z));
                _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y + Rect.H - Thickness / 2, Rect.W + Thickness, Thickness, Rect.Z));
                _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y - Thickness / 2, Thickness, Rect.H + Thickness, Rect.Z));
                _Draw.DrawColor(Color, new SRectF(Rect.X + Rect.W - Thickness / 2, Rect.Y - Thickness / 2, Thickness, Rect.H + Thickness, Rect.Z));
            }
        }

        public static void DrawColor(SColorF color, SRectF rect)
        {
            lock (mutex)
            {
                _Draw.DrawColor(color, rect);
            }
        }

        public static void ClearScreen()
        {
            lock (mutex)
            {
                _Draw.ClearScreen();
            }
        }

        public static STexture CopyScreen()
        {
            lock (mutex)
            {
                return _Draw.CopyScreen();
            }
        }

        public static void CopyScreen(ref STexture Texture)
        {
            lock (mutex)
            {
                _Draw.CopyScreen(ref Texture);
            }
        }

        public static void MakeScreenShot()
        {
            lock (mutex)
            {
                _Draw.MakeScreenShot();
            }
        }
        
        // Draw Basic Text (must be deleted later)
        public static void DrawText(string Text, int x, int y, int h)
        {
            lock (mutex)
            {
                _Draw.DrawText(Text, x, y, h);
            }
        }

        public static STexture AddTexture(Bitmap Bitmap)
        {
            lock (mutex)
            {
                return _Draw.AddTexture(Bitmap);
            }
        }

        public static STexture AddTexture(string TexturePath)
        {
            lock (mutex)
            {
                return _Draw.AddTexture(TexturePath);
            }
        }

        public static STexture AddTexture(string TexturePath, int MaxSize)
        {
            lock (mutex)
            {
                if (MaxSize == 0)
                    return _Draw.AddTexture(TexturePath);

                if (!System.IO.File.Exists(TexturePath))
                    return new STexture(-1);

                Bitmap origin = new Bitmap(TexturePath);
                int w = MaxSize;
                int h = MaxSize;

                if (origin.Width >= origin.Height && origin.Width > w)
                    h = (int)Math.Round((float)w / origin.Width * origin.Height);
                else if (origin.Height > origin.Width && origin.Height > h)
                    w = (int)Math.Round((float)h / origin.Height * origin.Width);

                Bitmap bmp = new Bitmap(origin, w, h);
                STexture tex = _Draw.AddTexture(bmp);
                bmp.Dispose();
                return tex;
            }
        }

        public static STexture AddTexture(int W, int H, IntPtr Data)
        {
            lock (mutex)
            {
                return _Draw.AddTexture(W, H, Data);
            }
        }

        public static STexture AddTexture(int W, int H, ref byte[] Data)
        {
            lock (mutex)
            {
                return _Draw.AddTexture(W, H, ref Data);
            }
        }

        public static bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            lock (mutex)
            {
                return _Draw.UpdateTexture(ref Texture, ref Data);
            }
        }

        public static bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            lock (mutex)
            {
                return _Draw.UpdateTexture(ref Texture, Data);
            }
        }

        public static void RemoveTexture(ref STexture Texture)
        {
            lock (mutex)
            {
                _Draw.RemoveTexture(ref Texture);
            }
        }

        public static void DrawTexture(STexture Texture)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect, color);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect, color, bounds);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color,bool mirrored)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect, color, mirrored);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect, color, bounds, mirrored);
            }
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {
            lock (mutex)
            {
                _Draw.DrawTexture(Texture, rect, color, begin, end);
            }
        }

        public static void DrawTexture(CStatic StaticBounds, STexture Texture, EAspect Aspect)
        {
            RectangleF bounds = new RectangleF(StaticBounds.Rect.X, StaticBounds.Rect.Y, StaticBounds.Rect.W, StaticBounds.Rect.H);
            RectangleF rect = new RectangleF(0f, 0f, Texture.width, Texture.height);

            if (rect.Height <= 0f)
                return;

            CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, Aspect);
            DrawTexture(Texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, StaticBounds.Rect.Z),
                    Texture.color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
        }

        public static void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            lock (mutex)
            {
                _Draw.DrawTextureReflection(Texture, rect, color, bounds, space, height);
            }
        }

        public static int TextureCount()
        {
            lock (mutex)
            {
                return _Draw.TextureCount();
            }
        }
    }

    enum EParticeType
    {
        Twinkle,
        Star,
        Snow,
        Flare,
        PerfNoteStar
    }

    class CParticle
    {
        #region private vars
        private string _TextureName;
        private STexture _Texture;
        private SRectF _Rect;
        private float _Size;
        private SColorF _Color;
        private float _Alpha;
        private float _Angle;       //0..360°
        private float _MaxAge;      //[s]
        private float _Age;         //[s]
        private float _Vx;          //movement speed in x-axis [pix/s]
        private float _Vy;          //movement speed in y-axis [pix/s]
        private float _Vr;          //rotation speed [rpm]
        private float _Rotation;    //start rotation 0..360°
        private float _Vsize;       //size changing speed: period [s]
        private float _LastTime;
        private EParticeType _Type;
        
        private Stopwatch _Timer;
        #endregion private vars

        #region public vars
        //public bool Visible;
        public float Alpha2 = 1f;

        public float X
        {
            get { return _Rect.X; }
            set { _Rect.X = value; }
        }

        public float Y
        {
            get { return _Rect.Y; }
            set { _Rect.Y = value; }
        }

        public float Size
        {
            get { return _Size; }
            set
            {
                _Rect.W = value;
                _Rect.H = value;
                _Size = value;
            }
        }

        public float Alpha
        {
            get { return _Alpha; }
            set { _Alpha = value; }
        }

        public SColorF Color
        {
            get { return _Color; }
            set { _Color = value; }
        }

        public bool IsAlive
        {
            get { return (_Age < _MaxAge || _MaxAge == 0f); }
        }
        #endregion public vars

        #region Constructors
        public CParticle(string textureName, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize, EParticeType type)
        {
            _TextureName = textureName;
            _Texture = new STexture(-1);
            _Color = color;
            _Rect = new SRectF(x, y, size, size, z);
            _Size = size;
            _Alpha = 1f;
            _Angle = 0f;
            _Vx = vx;
            _Vy = vy;
            _Vr = vr;
            _Vsize = vsize;
            _LastTime = 0f;
            _Type = type;

            _Timer = new Stopwatch();
            _Age = 0f;
            _MaxAge = maxage;
            _Rotation = (float)(CGame.Rand.NextDouble() * 360.0);
        }

        public CParticle(STexture texture, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize, EParticeType type)
        {
            _TextureName = String.Empty;
            _Texture = texture;
            _Color = color;
            _Rect = new SRectF(x, y, size, size, z);
            _Size = size;
            _Alpha = 1f;
            _Angle = 0f;
            _Vx = vx;
            _Vy = vy;
            _Vr = vr;
            _Vsize = vsize;
            _LastTime = 0f;
            _Type = type;

            _Timer = new Stopwatch();
            _Age = 0f;
            _MaxAge = maxage;
            _Rotation = (float)(CGame.Rand.NextDouble() * 360.0);
        }

        #endregion Constructors

        public void Update()
        {
            if (!IsAlive)
                return;

            if (!_Timer.IsRunning)
                _Timer.Start();

            float CurrentTime = _Timer.ElapsedMilliseconds / 1000f;
            float timediff = CurrentTime  - _LastTime;
            
            _Age = CurrentTime;

            // update alpha
            if (_MaxAge > 0f)
            {
                switch (_Type)
                {
                    case EParticeType.Twinkle:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticeType.Star:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticeType.Snow:
                        _Alpha = (float)Math.Sqrt((Math.Sin(_Age / _MaxAge * Math.PI * 2 - 0.5 * Math.PI) + 1) / 2);
                        break;

                    case EParticeType.Flare:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticeType.PerfNoteStar:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    default:
                        break;
                }
                
            }

            // update position
            switch (_Type)
            {
                case EParticeType.Twinkle:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticeType.Star:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticeType.Snow:
                    int maxy = (int)Math.Round(CSettings.iRenderH - _Size * 0.4f);

                    if (Math.Round(Y) < maxy)
                    {
                        float vdx = 0f;
                        if (_Vx != 0)
                            vdx = (float)Math.Sin(CurrentTime / _Vx * Math.PI);

                        X += _Vx * timediff * (0.5f + vdx);

                        Y += _Vy * timediff * (vdx * vdx / 2f + 0.5f);
                        if (Y >= maxy)
                            Y = maxy;
                    }
                    break;

                case EParticeType.Flare:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticeType.PerfNoteStar:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                default:
                    break;
            }
            

            // update size
            if (_Vsize != 0f)
            {
                float size = _Size;
                switch (_Type)
                {
                    case EParticeType.Twinkle:
                        size = _Size * (1f - CurrentTime / _Vsize);
                        break;

                    case EParticeType.Star:
                        size = _Size * (1f - CurrentTime / _Vsize);
                        break;

                    case EParticeType.Snow:
                        size = _Size * (float)Math.Sqrt((Math.Sin(CurrentTime / _Vsize * Math.PI * 2 - 0.5 * Math.PI) + 1) / 2);
                        break;

                    case EParticeType.Flare:
                        size = _Size * (1f - CurrentTime / _Vsize);
                        break;

                    case EParticeType.PerfNoteStar:
                        size = _Size * (1f - CurrentTime / _Vsize);
                        break;

                    default:
                        break;
                }
                
                _Rect.X += (_Rect.W - size) / 2f;
                _Rect.Y += (_Rect.H - size) / 2f;
                _Rect.W = size;
                _Rect.H = size;
            }

            // update rotation
            if (_Vr != 0f)
            {
                float r = CurrentTime * _Vr / 60f;
                _Angle = _Rotation + 360f * (r - (float)Math.Floor(r));
                _Rect.Rotation = _Angle;
            }

            _LastTime = CurrentTime;
        }

        public void Pause()
        {
            _Timer.Stop();
        }

        public void Resume()
        {
            _Timer.Start();
        }

        public void Draw()
        {
            if (_TextureName != String.Empty)
                CDraw.DrawTexture(CTheme.GetSkinTexture(_TextureName), _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha));
            else
                CDraw.DrawTexture(_Texture, _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha));
        }
    }

    class CParticleEffect
    {
        private List<CParticle> _Stars;
        private string _TextureName;
        private STexture _Texture;
        private int _MaxNumber;
        private SRectF _Area;
        private SColorF _Color;
        private float _Size;
        private EParticeType _Type;
        private Stopwatch _SpawnTimer;
        private float _NextSpawnTime;

        public float Alpha = 1f;

        public bool IsAlive
        {
            get
            {
                return (_Stars.Count > 0 || !_SpawnTimer.IsRunning);
            }
        }

        public SRectF Area
        {
            get { return _Area; }
            set { _Area = value; }
        }

        public CParticleEffect(int MaxNumber, SColorF Color, SRectF Area, string TextureName, float Size, EParticeType Type)
        {
            _Stars = new List<CParticle>();
            _Area = Area;
            _Color = Color;
            _TextureName = TextureName;
            _Texture = new STexture(-1);
            _MaxNumber = MaxNumber;
            _Size = Size;
            _Type = Type;
            _SpawnTimer = new Stopwatch();
            _NextSpawnTime = 0f;
        }

        public CParticleEffect(int MaxNumber, SColorF Color, SRectF Area, STexture texture, float Size, EParticeType Type)
        {
            _Stars = new List<CParticle>();
            _Area = Area;
            _Color = Color;
            _TextureName = String.Empty;
            _Texture = texture;
            _MaxNumber = MaxNumber;
            _Size = Size;
            _Type = Type;
            _SpawnTimer = new Stopwatch();
            _NextSpawnTime = 0f;
        }

        public void Update()
        {
            bool DoSpawn = false;
            if (!_SpawnTimer.IsRunning)
            {
                _SpawnTimer.Start();
                _NextSpawnTime = 0f;
                DoSpawn = true;
            }
            
            if (_SpawnTimer.ElapsedMilliseconds / 1000f > _NextSpawnTime && _NextSpawnTime >= 0f)
            {
                DoSpawn = true;
                _SpawnTimer.Reset();
                _SpawnTimer.Start();
            }
            
            while (_Stars.Count < _MaxNumber && DoSpawn)
            {
                float size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                float lifetime = 0f;
                float vx = 0f;
                float vy = 0f;
                float vr = 0f;
                float vsize = 0f;
                _NextSpawnTime = 0f;

                switch (_Type)
                {
                    case EParticeType.Twinkle:
                        size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                        lifetime = CGame.Rand.Next(500) / 1000f + 0.5f;
                        vx = -CGame.Rand.Next(10000) / 50f + 100f;
                        vy = -CGame.Rand.Next(10000) / 50f + 100f;
                        vr = -CGame.Rand.Next(500) / 100f + 2.5f;
                        vsize = lifetime * 2f;                  
                        break;

                    case EParticeType.Star:
                        size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                        lifetime = CGame.Rand.Next(1000) / 500f + 0.2f;
                        vx = -CGame.Rand.Next(1000) / 50f + 10f;
                        vy = -CGame.Rand.Next(1000) / 50f + 10f;
                        vr = -CGame.Rand.Next(500) / 100f + 2.5f;
                        vsize = lifetime * 2f;
                        break;

                    case EParticeType.Snow:
                        size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                        lifetime = CGame.Rand.Next(5000) / 50f + 10f;
                        vx = -CGame.Rand.Next(1000) / 50f + 10f;
                        vy = CGame.Rand.Next(1000) / 50f + Math.Abs(vx) + 10f;
                        vr = -CGame.Rand.Next(200) / 50f + 2f;
                        vsize = lifetime * 2f;

                        _NextSpawnTime = lifetime / _MaxNumber;
                        DoSpawn = false;
                        break;

                    case EParticeType.Flare:
                        size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                        lifetime = CGame.Rand.Next(500) / 1000f + 0.1f;
                        vx = -CGame.Rand.Next(2000) / 50f;
                        vy = -CGame.Rand.Next(2000) / 50f + 20f;
                        vr = -CGame.Rand.Next(2000) / 50f + 20f;
                        vsize = lifetime * 2f;
                        break;

                    case EParticeType.PerfNoteStar:
                        size = CGame.Rand.Next((int)_Size / 2) + _Size / 2;
                        lifetime = CGame.Rand.Next(1000) / 500f + 1.2f;
                        vx = 0f;
                        vy = 0f;
                        vr = CGame.Rand.Next(500) / 50f + 10f;
                        vsize = lifetime * 2f;
                        break;

                    default:
                        break;
                }

                int w = (int)(_Area.W - size / 4f);
                int h = (int)(_Area.H - size / 4f);

                if (w < 0)
                    w = 0;

                if (h < 0)
                    h = 0;

                CParticle star;
                if (_TextureName != String.Empty)
                {
                    star = new CParticle(_TextureName, _Color,
                        CGame.Rand.Next(w) + _Area.X - size / 4f,
                        CGame.Rand.Next(h) + _Area.Y - size / 4f,
                        size, lifetime, _Area.Z, vx, vy, vr, vsize, _Type);
                }
                else
                {
                    star = new CParticle(_Texture, _Color,
                        CGame.Rand.Next(w) + _Area.X - size / 4f,
                        CGame.Rand.Next(h) + _Area.Y - size / 4f,
                        size, lifetime, _Area.Z, vx, vy, vr, vsize, _Type);
                }

                _Stars.Add(star);
            }

            if (_Type == EParticeType.Flare || _Type == EParticeType.PerfNoteStar || _Type == EParticeType.Twinkle)
                _NextSpawnTime = -1f;

            int i = 0;
            while (i < _Stars.Count)
            {
                _Stars[i].Update();
                if (!_Stars[i].IsAlive)
                {
                    _Stars.RemoveAt(i);
                }
                else
                    i++;
            }
        }

        public void Pause()
        {
            foreach (CParticle star in _Stars)
            {
                star.Pause();
            }
        }

        public void Resume()
        {
            foreach (CParticle star in _Stars)
            {
                star.Resume();
            }
        }

        public void Draw()
        {
            foreach (CParticle star in _Stars)
            {
                star.Alpha2 = Alpha;
                star.Draw();
            }
        }
    }
}
