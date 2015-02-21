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

namespace Vocaluxe.Base.Server
{
    [Flags]
    enum EUserRoles
    {
        // ReSharper disable InconsistentNaming
        TR_USERROLE_GUEST = 0,
        TR_USERROLE_NORMAL = 1,
        TR_USERROLE_ADMIN = 2,
        TR_USERROLE_KEYBOARDUSER = 4,
        TR_USERROLE_ADDSONGSUSER = 8,
        TR_USERROLE_PLAYLISTEDITOR = 16,
        TR_USERROLE_PROFILEEDITOR = 32
        // ReSharper restore InconsistentNaming
    }
}