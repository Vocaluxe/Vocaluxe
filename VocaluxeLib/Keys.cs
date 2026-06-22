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

namespace VocaluxeLib
{
    /// <summary>
    ///     Platform-independent key enumeration.
    ///     Replaces System.Windows.Forms.Keys so VocaluxeLib can target a
    ///     cross-platform .NET runtime without a dependency on WinForms.
    ///     The integer values mirror System.Windows.Forms.Keys so existing
    ///     code that treats the modifier bits as flags keeps working and the
    ///     input backends (OpenTK/SDL) can map onto the same values.
    /// </summary>
    [Flags]
    public enum Keys
    {
        None = 0,

        // Control keys
        Back = 8,
        Tab = 9,
        Return = 13,
        Enter = 13,
        Escape = 27,
        Space = 32,

        // Navigation
        PageUp = 33,
        PageDown = 34,
        End = 35,
        Home = 36,
        Left = 37,
        Up = 38,
        Right = 39,
        Down = 40,
        Delete = 46,

        // Top-row digits
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,

        // Letters
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,

        // Numpad
        NumPad0 = 96,
        NumPad1 = 97,
        NumPad2 = 98,
        NumPad3 = 99,
        NumPad4 = 100,
        NumPad5 = 101,
        NumPad6 = 102,
        NumPad7 = 103,
        NumPad8 = 104,
        NumPad9 = 105,
        Add = 107,
        Subtract = 109,

        // Function keys
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,

        // Media keys
        MediaNextTrack = 176,
        MediaPreviousTrack = 177,
        MediaPlayPause = 179,

        // Modifier flags (bit-flags, combined with the key codes above)
        Shift = 0x00010000,
        Control = 0x00020000,
        Alt = 0x00040000
    }
}
