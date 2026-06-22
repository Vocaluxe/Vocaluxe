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

using System.Diagnostics.CodeAnalysis;

// Suppressions for the dropped backends (CDirect3D, CDrawWinForm, the GStreamer wrapper) and the old
// GDI+ font system were removed in the cross-platform port (#768) - those types no longer exist.

[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Scope = "member",
        Target = "Vocaluxe.MainProgram.#AssemblyResolver(System.Object,System.ResolveEventArgs)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Vocaluxe.Lib.Input.WiiMote.InputReport")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "Vocaluxe.Lib.Input.CHIDAPI.#ReadTimeout(System.IntPtr,System.Byte[]&,System.Int32,System.Int32)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "Vocaluxe.Lib.Input.CHIDAPI.#Read(System.IntPtr,System.Byte[]&,System.Int32)")]
