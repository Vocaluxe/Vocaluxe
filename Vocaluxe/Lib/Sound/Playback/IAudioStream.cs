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
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback
{
    public interface ICloseStreamListener
    {
        void OnCloseStream(IAudioStream stream);
    }

    public interface IAudioStream : IDisposable
    {
        int ID { get; }
        bool IsFading { get; }
        float Volume { get; set; }
        float VolumeMax { get; set; }
        float Length { get; }
        float Position { get; set; }
        bool IsPaused { get; set; }
        bool IsFinished { get; }

        void SetOnCloseListener(ICloseStreamListener listener);

        void Update();

        bool Open(bool prescan);
        void Close();
        void Play();
        void Stop();
        void Fade(float targetVolume, float seconds, EStreamAction afterFadeAction);
        void CancelFading();
    }
}