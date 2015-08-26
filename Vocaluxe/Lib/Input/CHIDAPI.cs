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
using System.Runtime.InteropServices;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Input
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SHIDDeviceInfo
    {
        // ReSharper disable MemberCanBePrivate.Global
        [MarshalAs(UnmanagedType.LPTStr)] public readonly String Path;
        public readonly ushort VendorString;
        public readonly ushort ProductID;
        [MarshalAs(UnmanagedType.LPWStr)] public readonly String SerialNumber;
        public readonly ushort ReleaseNumber;
        [MarshalAs(UnmanagedType.LPWStr)] public readonly String ManufacturerString;
        [MarshalAs(UnmanagedType.LPWStr)] public readonly String ProductString;
        public readonly ushort UsagePage;
        public readonly ushort Usage;
        [MarshalAs(UnmanagedType.LPWStr)] public readonly int InterfaceNumber;
        internal IntPtr Next;
        // ReSharper restore MemberCanBePrivate.Global
    }

    public static class CHIDApi
    {
#if ARCH_X86
#if WIN
        private const string _HIDApiDll = "hidapi.dll";
#endif

#if LINUX
        private const string _HIDApiDll = "libhidapi-libusb.so";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string _HIDApiDll = "hidapi.dll";
#endif

#if LINUX
        private const string _HIDApiDll = "libhidapi-libusb.so";
#endif
#endif

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_init", CharSet = CharSet.Unicode)]
        private static extern int hid_init();

        public static bool Init()
        {
            int result;
            try
            {
                result = hid_init();
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Init(): " + e);
                return false;
            }

            return result == 0;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_exit", CharSet = CharSet.Unicode)]
        private static extern int hid_exit();

        public static bool Exit()
        {
            int result;
            try
            {
                result = hid_exit();
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Exit(): " + e);
                return false;
            }

            return result == 0;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_enumerate", CharSet = CharSet.Ansi)]
        private static extern IntPtr hid_enumerate(ushort vendorID, ushort productID);

        //HIDDeviceInfo
        public static IntPtr Enumerate(ushort vendorID, ushort productID)
        {
            //HIDDeviceInfo
            return hid_enumerate(vendorID, productID);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_free_enumeration", CharSet = CharSet.Unicode)]
        private static extern void hid_free_enumeration(SHIDDeviceInfo devs);

        public static void FreeEnumeration(SHIDDeviceInfo devs)
        {
            hid_free_enumeration(devs);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_open", CharSet = CharSet.Unicode)]
        private static extern IntPtr hid_open(ushort vendorID, ushort productID, IntPtr serialNumber);

        public static bool Open(ushort vendorID, ushort productID, out IntPtr handle)
        {
            handle = IntPtr.Zero;
            try
            {
                handle = hid_open(vendorID, productID, IntPtr.Zero);
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Open(): " + e);
                return false;
            }

            if (handle != IntPtr.Zero)
                return true;

            return false;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_open_path", CharSet = CharSet.Unicode)]
        private static extern IntPtr hid_open_path(string path);

        public static IntPtr OpenPath(string path)
        {
            return hid_open_path(path);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_write", CharSet = CharSet.Unicode)]
        private static extern int hid_write(IntPtr device, byte[] data, int length);

        public static int Write(IntPtr device, byte[] data)
        {
            return hid_write(device, data, data.Length);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_read_timeout", CharSet = CharSet.Unicode)]
        private static extern int hid_read_timeout(IntPtr device, IntPtr data, int length, int milliseconds);

        public static int ReadTimeout(IntPtr device, ref byte[] data, int length, int milliseconds)
        {
            IntPtr dataPtr = Marshal.AllocHGlobal(length);
            int bytesRead;
            try
            {
                bytesRead = hid_read_timeout(device, dataPtr, length, milliseconds);
            }
            catch (Exception e)
            {
                bytesRead = -1;
                CLog.LogError("Error CHIDAPI.ReadTimeout(): " + e);
            }

            if (bytesRead != -1)
                Marshal.Copy(dataPtr, data, 0, bytesRead);
            else
                data = null;

            Marshal.FreeHGlobal(dataPtr);

            return bytesRead;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_read", CharSet = CharSet.Unicode)]
        private static extern int hid_read(IntPtr device, IntPtr data, int length);

        public static int Read(IntPtr device, out byte[] data, int length)
        {
            data = new byte[length];
            IntPtr dataPtr = Marshal.AllocHGlobal(length);

            int result;
            try
            {
                result = hid_read(device, dataPtr, length);
            }
            catch (Exception e)
            {
                result = -1;
                CLog.LogError("Error CHIDAPI.Read(): " + e);
            }

            if (result != -1)
                Marshal.Copy(dataPtr, data, 0, result);
            else
                data = null;

            Marshal.FreeHGlobal(dataPtr);
            return result;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_set_nonblocking", CharSet = CharSet.Unicode)]
        private static extern int hid_set_nonblocking(IntPtr device, bool nonblock);

        public static int Read(IntPtr device, bool nonblocking)
        {
            return hid_set_nonblocking(device, nonblocking);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_send_feature_report", CharSet = CharSet.Unicode)]
        private static extern int hid_send_feature_report(IntPtr device, string data, int length);

        public static int SendFeatureReport(IntPtr device, string data)
        {
            return hid_send_feature_report(device, data, data.Length);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_feature_report", CharSet = CharSet.Unicode)]
        private static extern int hid_get_feature_report(IntPtr device, string data, int length);

        public static int GetFeatureReport(IntPtr device, string data)
        {
            return hid_get_feature_report(device, data, data.Length);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_close", CharSet = CharSet.Unicode)]
        private static extern void hid_close(IntPtr device);

        public static bool Close(IntPtr device)
        {
            try
            {
                hid_close(device);
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Close(): " + e);
                return false;
            }
            return true;
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_manufacturer_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_manufacturer_string(IntPtr device, string data, int maxlength);

        public static int GetManufacturerString(IntPtr device, string data, int maxLength)
        {
            return hid_get_manufacturer_string(device, data, maxLength);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_product_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_product_string(IntPtr device, string data, int maxlength);

        public static int GetProductString(IntPtr device, string data, int maxLength)
        {
            return hid_get_product_string(device, data, maxLength);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_serial_number_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_serial_number_string(IntPtr device, string data, int maxlength);

        public static int GetSerialNumberString(IntPtr device, string data, int maxLength)
        {
            return hid_get_serial_number_string(device, data, maxLength);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_indexed_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_indexed_string(IntPtr device, string data, int maxlength);

        public static int GetIndexedString(IntPtr device, string data, int maxLength)
        {
            return hid_get_indexed_string(device, data, maxLength);
        }

        [DllImport(_HIDApiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_error", CharSet = CharSet.Unicode)]
        private static extern string hid_error(IntPtr device);

        public static string Error(IntPtr device)
        {
            return hid_error(device);
        }
    }
}