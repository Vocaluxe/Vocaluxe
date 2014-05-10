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

namespace Vocaluxe.Lib.Sound.Playback
{
    public enum EStreamAction
    {
        Nothing,
        Pause,
        Stop,
        Close
    }

    interface IPlayback
    {
        bool Init();
        void Close();
        float GetGlobalVolume();

        /// <summary>
        ///     Set the global (maximum) volume<br />
        ///     All streams (existent and future ones) will have their maximum volume set to this value
        /// </summary>
        /// <param name="volume">Volume in percent</param>
        void SetGlobalVolume(float volume);

        int GetStreamCount();
        void CloseAll();

        #region stream Handling
        int Load(string medium, bool loop = false, bool prescan = false);
        void Close(int streamID);

        void Play(int streamID);
        void Pause(int streamID);
        void Stop(int streamID);
        void Fade(int streamID, float targetVolume, float seconds, EStreamAction afterFadeAction = EStreamAction.Nothing);

        /// <summary>
        ///     Set the streams current volume. Cancels fading
        /// </summary>
        /// <param name="streamID">Id of the stream (optained by Load)</param>
        /// <param name="volume">Volume in percent</param>
        void SetStreamVolume(int streamID, float volume);

        float GetLength(int streamID);
        float GetPosition(int streamID);
        void SetPosition(int streamID, float position);

        bool IsPlaying(int streamID);
        bool IsPaused(int streamID);
        bool IsFinished(int streamID);

        void Update();
        #endregion stream Handling
    }
}