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
using System.Diagnostics;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    class CParticle
    {
        #region private vars
        private readonly int _PartyModeID;
        private readonly string _TextureName;
        private readonly CTextureRef _Texture;
        private SRectF _Rect;
        private float _Size;
        private SColorF _Color;
        private float _Alpha = 1;
        private float _Angle; //0..360°
        private readonly float _MaxAge; //[s]
        private float _Age; //[s]
        private readonly float _Vx; //movement speed in x-axis [pix/s]
        private readonly float _Vy; //movement speed in y-axis [pix/s]
        private readonly float _Vr; //rotation speed [rpm]
        private readonly float _Rotation; //start rotation 0..360°
        private readonly float _Vsize; //size changing speed: period [s]
        private float _LastTime;
        private readonly EParticleType _Type;

        private readonly Stopwatch _Timer = new Stopwatch();
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
            get { return _Age < _MaxAge || Math.Abs(_MaxAge) < float.Epsilon; }
        }
        #endregion public vars

        #region Constructors
        public CParticle(int partyModeID, string textureName, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize,
                         EParticleType type)
        {
            _PartyModeID = partyModeID;
            _TextureName = textureName;
            _Color = color;
            _Rect = new SRectF(x, y, size, size, z);
            _Size = size;
            _Vx = vx;
            _Vy = vy;
            _Vr = vr;
            _Vsize = vsize;
            _Type = type;

            _MaxAge = maxage;
            _Rotation = (float)(CBase.Game.GetRandomDouble() * 360.0);
        }

        public CParticle(int partyModeID, CTextureRef texture, SColorF color, float x, float y, float size, float maxage, float z, float vx, float vy, float vr, float vsize,
                         EParticleType type)
        {
            _PartyModeID = partyModeID;
            _TextureName = String.Empty;
            _Texture = texture;
            _Color = color;
            _Rect = new SRectF(x, y, size, size, z);
            _Size = size;
            _Vx = vx;
            _Vy = vy;
            _Vr = vr;
            _Vsize = vsize;
            _Type = type;

            _MaxAge = maxage;
            _Rotation = (float)(CBase.Game.GetRandomDouble() * 360.0);
        }
        #endregion Constructors

        public void Update()
        {
            if (!IsAlive)
                return;

            if (!_Timer.IsRunning)
                _Timer.Start();

            float currentTime = _Timer.ElapsedMilliseconds / 1000f;
            float timediff = currentTime - _LastTime;

            _Age = currentTime;

            // update alpha
            if (_MaxAge > 0f)
            {
                switch (_Type)
                {
                    case EParticleType.Twinkle:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticleType.Star:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticleType.Snow:
                        _Alpha = (float)Math.Sqrt((Math.Sin(_Age / _MaxAge * Math.PI * 2 - 0.5 * Math.PI) + 1) / 2);
                        break;

                    case EParticleType.Flare:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;

                    case EParticleType.PerfNoteStar:
                        _Alpha = 1f - _Age / _MaxAge;
                        break;
                }
            }

            // update position
            switch (_Type)
            {
                case EParticleType.Twinkle:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticleType.Star:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticleType.Snow:
                    var maxy = (int)Math.Round(CBase.Settings.GetRenderH() - _Size * 0.4f);

                    if (Math.Round(Y) < maxy)
                    {
                        float vdx = 0f;
                        if (Math.Abs(_Vx) > float.Epsilon)
                            vdx = (float)Math.Sin(currentTime / _Vx * Math.PI);

                        X += _Vx * timediff * (0.5f + vdx);

                        Y += _Vy * timediff * (vdx * vdx / 2f + 0.5f);
                        if (Y >= maxy)
                            Y = maxy;
                    }
                    break;

                case EParticleType.Flare:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;

                case EParticleType.PerfNoteStar:
                    X += _Vx * timediff;
                    Y += _Vy * timediff;
                    break;
            }


            // update size
            if (Math.Abs(_Vsize) > float.Epsilon)
            {
                float size = _Size;
                switch (_Type)
                {
                    case EParticleType.Twinkle:
                        size = _Size * (1f - currentTime / _Vsize);
                        break;

                    case EParticleType.Star:
                        size = _Size * (1f - currentTime / _Vsize);
                        break;

                    case EParticleType.Snow:
                        size = _Size * (float)Math.Sqrt((Math.Sin(currentTime / _Vsize * Math.PI * 2 - 0.5 * Math.PI) + 1) / 2);
                        break;

                    case EParticleType.Flare:
                        size = _Size * (1f - currentTime / _Vsize);
                        break;

                    case EParticleType.PerfNoteStar:
                        size = _Size * (1f - currentTime / _Vsize);
                        break;
                }

                _Rect.X += (_Rect.W - size) / 2f;
                _Rect.Y += (_Rect.H - size) / 2f;
                _Rect.W = size;
                _Rect.H = size;
            }

            // update rotation
            if (Math.Abs(_Vr) > 0.01)
            {
                float r = currentTime * _Vr / 60f;
                _Angle = _Rotation + 360f * (r - (float)Math.Floor(r));
                _Rect.Rotation = _Angle;
            }

            _LastTime = currentTime;
        }

        public void Pause()
        {
            _Timer.Stop();
        }

        public void Resume()
        {
            _Timer.Start();
        }

        public void Draw(bool allMonitors = true)
        {
            // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
            if (!String.IsNullOrEmpty(_TextureName))
                // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                CBase.Drawing.DrawTexture(CBase.Themes.GetSkinTexture(_TextureName, _PartyModeID), _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha), allMonitors);
            else
                CBase.Drawing.DrawTexture(_Texture, _Rect, new SColorF(_Color.R, _Color.G, _Color.B, _Color.A * Alpha2 * _Alpha), allMonitors);
        }
    }
}