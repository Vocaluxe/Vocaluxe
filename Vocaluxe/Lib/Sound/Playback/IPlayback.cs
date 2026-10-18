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

using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback
{
    interface IPlayback
    {
        bool Init();
        void Close();
        int GetGlobalVolume();

        /// <summary>
        ///     Set the global (maximum) volume<br />
        ///     All streams (existent and future ones) will have their maximum volume set to this value
        /// </summary>
        /// <param name="volume">Volume in percent</param>
        void SetGlobalVolume(int volume);

        int GetStreamCount();
        void CloseAll();

        #region stream Handling
        int Load(string medium, bool loop = false, bool prescan = false, EAudioEffect effekt = EAudioEffect.None);
        void Close(int streamId);

        void Play(int streamId);
        void Pause(int streamId);
        void Stop(int streamId);
        void Fade(int streamId, int targetVolume, float seconds, EStreamAction afterFadeAction = EStreamAction.Nothing);

        /// <summary>
        ///     Set the stream's current volume. Cancels fading
        /// </summary>
        /// <param name="streamId">Id of the stream (obtained by Load)</param>
        /// <param name="volume">Volume in percent</param>
        void SetStreamVolume(int streamId, int volume);

        float GetLength(int streamId);
        float GetPosition(int streamId);
        void SetPosition(int streamId, float position);

        bool IsPlaying(int streamId);
        bool IsPaused(int streamId);
        bool IsFinished(int streamId);

        void Update();
        #endregion stream Handling
    }
}