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

using System.Diagnostics;

namespace VocaluxeLib
{
    public class CFading
    {
        private readonly float _FromValue;
        private readonly float _ToValue;
        private readonly float _Duration; //in ms
        private readonly Stopwatch _Timer = new Stopwatch();

        /// <summary>
        ///     Starts a fading process adjusting between 2 values over a given amount of time
        /// </summary>
        /// <param name="fromValue">Start value</param>
        /// <param name="toValue">Target value</param>
        /// <param name="duration">Duration of the fading in seconds</param>
        public CFading(float fromValue, float toValue, float duration)
        {
            if (duration < 0)
                duration = 0;
            _FromValue = fromValue;
            _ToValue = toValue;
            _Duration = duration * 1000f;
            _Timer.Start();
        }

        /// <summary>
        /// Gets the current value and sets finished
        /// </summary>
        /// <param name="finished">Set to whether fading has finished</param>
        /// <returns>Current value (between from and to values)</returns>
        public float GetValue(out bool finished)
        {
            float result;
            if (_Timer.ElapsedMilliseconds < _Duration)
            {
                result = (_Timer.ElapsedMilliseconds / _Duration) * (_ToValue - _FromValue) + _FromValue;
                finished = false;
            }
            else
            {
                result = _ToValue;
                finished = true;
            }
            return result;
        }
    }
}