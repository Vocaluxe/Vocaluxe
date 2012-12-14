using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Vocaluxe.Menu
{
    public enum EParticeType
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
        private Basic _Base;
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
        public CParticle(Basic Base, string textureName, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize, EParticeType type)
        {
            _Base = Base;
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
            _Rotation = (float)(_Base.Game.GetRandomDouble() * 360.0);
        }

        public CParticle(Basic Base, STexture texture, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize, EParticeType type)
        {
            _Base = Base;
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
            _Rotation = (float)(_Base.Game.GetRandomDouble() * 360.0);
        }
        #endregion Constructors

        public void Update()
        {
            if (!IsAlive)
                return;

            if (!_Timer.IsRunning)
                _Timer.Start();

            float CurrentTime = _Timer.ElapsedMilliseconds / 1000f;
            float timediff = CurrentTime - _LastTime;

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
                    int maxy = (int)Math.Round(_Base.Settings.GetRenderH() - _Size * 0.4f);

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
                _Base.Drawing.DrawTexture(_Base.Theme.GetSkinTexture(_TextureName), _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha));
            else
                _Base.Drawing.DrawTexture(_Texture, _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha));
        }
    }

    public class CParticleEffect
    {
        private Basic _Base;
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

        public CParticleEffect(Basic Base, int MaxNumber, SColorF Color, SRectF Area, string TextureName, float Size, EParticeType Type)
        {
            _Base = Base;
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

        public CParticleEffect(Basic Base, int MaxNumber, SColorF Color, SRectF Area, STexture texture, float Size, EParticeType Type)
        {
            _Base = Base;
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
                float size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                float lifetime = 0f;
                float vx = 0f;
                float vy = 0f;
                float vr = 0f;
                float vsize = 0f;
                _NextSpawnTime = 0f;

                switch (_Type)
                {
                    case EParticeType.Twinkle:
                        size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                        lifetime = _Base.Game.GetRandom(500) / 1000f + 0.5f;
                        vx = -_Base.Game.GetRandom(10000) / 50f + 100f;
                        vy = -_Base.Game.GetRandom(10000) / 50f + 100f;
                        vr = -_Base.Game.GetRandom(500) / 100f + 2.5f;
                        vsize = lifetime * 2f;
                        break;

                    case EParticeType.Star:
                        size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                        lifetime = _Base.Game.GetRandom(1000) / 500f + 0.2f;
                        vx = -_Base.Game.GetRandom(1000) / 50f + 10f;
                        vy = -_Base.Game.GetRandom(1000) / 50f + 10f;
                        vr = -_Base.Game.GetRandom(500) / 100f + 2.5f;
                        vsize = lifetime * 2f;
                        break;

                    case EParticeType.Snow:
                        size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                        lifetime = _Base.Game.GetRandom(5000) / 50f + 10f;
                        vx = -_Base.Game.GetRandom(1000) / 50f + 10f;
                        vy = _Base.Game.GetRandom(1000) / 50f + Math.Abs(vx) + 10f;
                        vr = -_Base.Game.GetRandom(200) / 50f + 2f;
                        vsize = lifetime * 2f;

                        _NextSpawnTime = lifetime / _MaxNumber;
                        DoSpawn = false;
                        break;

                    case EParticeType.Flare:
                        size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                        lifetime = _Base.Game.GetRandom(500) / 1000f + 0.1f;
                        vx = -_Base.Game.GetRandom(2000) / 50f;
                        vy = -_Base.Game.GetRandom(2000) / 50f + 20f;
                        vr = -_Base.Game.GetRandom(2000) / 50f + 20f;
                        vsize = lifetime * 2f;
                        break;

                    case EParticeType.PerfNoteStar:
                        size = _Base.Game.GetRandom((int)_Size / 2) + _Size / 2;
                        lifetime = _Base.Game.GetRandom(1000) / 500f + 1.2f;
                        vx = 0f;
                        vy = 0f;
                        vr = _Base.Game.GetRandom(500) / 50f + 10f;
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
                    star = new CParticle(_Base, _TextureName, _Color,
                        _Base.Game.GetRandom(w) + _Area.X - size / 4f,
                        _Base.Game.GetRandom(h) + _Area.Y - size / 4f,
                        size, lifetime, _Area.Z, vx, vy, vr, vsize, _Type);
                }
                else
                {
                    star = new CParticle(_Base, _Texture, _Color,
                        _Base.Game.GetRandom(w) + _Area.X - size / 4f,
                        _Base.Game.GetRandom(h) + _Area.Y - size / 4f,
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
