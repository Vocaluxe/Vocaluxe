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

using OpenTK.Audio.OpenAL;

namespace Vocaluxe.Lib.Sound.Playback.OpenAL
{
    class COpenALPlay : CPlaybackBase
    {
        private ALDevice _Device;
        private ALContext _Context;

        public override bool Init()
        {
            if (_Initialized)
            {
                return false;
            }

            _Device = ALC.OpenDevice(null);
            if (_Device == ALDevice.Null)
            {
                return false;
            }

            _Context = ALC.CreateContext(_Device, (int[])null);
            ALC.MakeContextCurrent(_Context);

            _Initialized = true;
            return true;
        }

        public override void Close()
        {
            if (!_Initialized)
            {
                return;
            }

            base.Close();
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_Context);
            ALC.CloseDevice(_Device);
            _Context = ALContext.Null;
            _Device = ALDevice.Null;
        }

        protected override IAudioStream _CreateStream(int id, string media, bool loop, EAudioEffect effect = EAudioEffect.None)
        {
            return new COpenAlStream(id, media, loop, effect);
        }
    }
}