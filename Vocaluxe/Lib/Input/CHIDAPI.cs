using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Vocaluxe.Base;

namespace Vocaluxe.Lib.Input
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct HIDDeviceInfo
    {
        [MarshalAsAttribute(UnmanagedType.LPTStr)]
        public String Path;
        public ushort VendorString;
        public ushort ProductID;
        public String SerialNumber;
        public ushort ReleaseNumber;
        public String ManufacturerString;
        public String ProductString;
        public ushort UsagePage;
        public ushort Usage;
        public int InterfaceNumber;
        public IntPtr Next;
    }
    public static class CHIDAPI
    {
#if ARCH_X86
#if WIN
        private const string HIDapiDll = "x86\\hidapi.dll";
#endif

#if LINUX
        private const string HIDapiDll = "libhidapi.so";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string HIDapiDll = "x64\\hidapi.dll";
#endif

#if LINUX
        private const string HIDapiDll = "libhidapi.so";
#endif
#endif


        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_init", CharSet = CharSet.Unicode)]
        private static extern int hid_init();
        public static bool Init()
        {
            int result = -1;
            try
            {
                result = hid_init();
            }
            catch(Exception e)
            {
                CLog.LogError("Error CHIDAPI.Init(): " + e.ToString());
                return false;
            }
            
            if (result == 0)
                return true;

            return false;
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_exit", CharSet = CharSet.Unicode)]
        private static extern int hid_exit();
        public static bool Exit()
        {
            int result = -1;
            try
            {
                result = hid_exit();
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Exit(): " + e.ToString());
                return false;
            }

            if (result == 0)
                return true;

            return false;
        }
        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_enumerate", CharSet = CharSet.Ansi)]
        private static extern IntPtr hid_enumerate(ushort VendorID, ushort ProductID); //HIDDeviceInfo
        public static IntPtr Enumerate(ushort VendorID, ushort ProductID) //HIDDeviceInfo
        {
            return hid_enumerate(VendorID, ProductID);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_free_enumeration", CharSet = CharSet.Unicode)]
        private static extern void hid_free_enumeration(HIDDeviceInfo devs);
        public static void FreeEnumeration(HIDDeviceInfo devs)
        {
            hid_free_enumeration(devs);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_open", CharSet = CharSet.Unicode)]
        private static extern IntPtr hid_open(ushort VendorID, ushort ProductID, IntPtr SerialNumber);
        public static bool Open(ushort VendorID, ushort ProductID, out IntPtr Handle)
        {
            Handle = IntPtr.Zero;
            try
            {
                Handle = hid_open(VendorID, ProductID, IntPtr.Zero);
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Open(): " + e.ToString());
                return false;
            }

            if (Handle != IntPtr.Zero)
                return true;

            return false;
            
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_open_path", CharSet = CharSet.Unicode)]
        private static extern IntPtr hid_open_path(string Path);
        public static IntPtr OpenPath(string Path)
        {
            return hid_open_path(Path);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_write", CharSet = CharSet.Unicode)]
        private static extern int hid_write(IntPtr device, byte[] data, int length);
        public static int Write(IntPtr Device, byte[] Data)
        {
            return hid_write(Device, Data, Data.Length);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_read_timeout", CharSet = CharSet.Unicode)]
        private static extern int hid_read_timeout(IntPtr device, IntPtr data, int length, int milliseconds);
        public static int ReadTimeout(IntPtr Device, out byte[] Data, int length, int milliseconds)
        {
            Data = new byte[length];
            IntPtr data = Marshal.AllocHGlobal(length);

            int result = -1;
            try
            {
                result = hid_read_timeout(Device, data, length, milliseconds);
            }
            catch (Exception e)
            {
                result = -1;
                CLog.LogError("Error CHIDAPI.ReadTimeout(): " + e.ToString());
            }

            if (result != -1)
                Marshal.Copy(data, Data, 0, result);
            else
                Data = null;

            Marshal.FreeHGlobal(data);
            return result;
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_read", CharSet = CharSet.Unicode)]
        private static extern int hid_read(IntPtr device, IntPtr data, int length);
        public static int Read(IntPtr Device, out byte[] Data, int length)
        {
            Data = new byte[length];
            IntPtr data = Marshal.AllocHGlobal(length);

            int result = -1;
            try
            {
                result = hid_read(Device, data, length);
            }
            catch (Exception e)
            {
                result = -1;
                CLog.LogError("Error CHIDAPI.Read(): " + e.ToString());
            }

            if (result != -1)
                Marshal.Copy(data, Data, 0, result);
            else
                Data = null;

            Marshal.FreeHGlobal(data);
            return result;
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_set_nonblocking", CharSet = CharSet.Unicode)]
        private static extern int hid_set_nonblocking(IntPtr device, bool nonblock);
        public static int Read(IntPtr Device, bool Nonblocking)
        {
            return hid_set_nonblocking(Device, Nonblocking);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_send_feature_report", CharSet = CharSet.Unicode)]
        private static extern int hid_send_feature_report(IntPtr device, string data, int length);
        public static int SendFeatureReport(IntPtr Device, string Data)
        {
            return hid_send_feature_report(Device, Data, Data.Length);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_feature_report", CharSet = CharSet.Unicode)]
        private static extern int hid_get_feature_report(IntPtr device, string Data, int length);
        public static int GetFeatureReport(IntPtr Device, string Data)
        {
            return hid_get_feature_report(Device, Data, Data.Length);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_close", CharSet = CharSet.Unicode)]
        private static extern void hid_close(IntPtr device);
        public static bool Close(IntPtr Device)
        {
            try
            {
                hid_close(Device);
            }
            catch (Exception e)
            {
                CLog.LogError("Error CHIDAPI.Close(): " + e.ToString());
                return false;
            }
            return true;
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_manufacturer_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_manufacturer_string(IntPtr device, string Data, int maxlength);
        public static int GetManufacturerString(IntPtr Device, string Data, int MaxLength)
        {
            return hid_get_manufacturer_string(Device, Data, MaxLength);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_product_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_product_string(IntPtr device, string Data, int maxlength);
        public static int GetProductString(IntPtr Device, string Data, int MaxLength)
        {
            return hid_get_product_string(Device, Data, MaxLength);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_serial_number_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_serial_number_string(IntPtr device, string Data, int maxlength);
        public static int GetSerialNumberString(IntPtr Device, string Data, int MaxLength)
        {
            return hid_get_serial_number_string(Device, Data, MaxLength);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_get_indexed_string", CharSet = CharSet.Unicode)]
        private static extern int hid_get_indexed_string(IntPtr device, string Data, int maxlength);
        public static int GetIndexedString(IntPtr Device, string Data, int MaxLength)
        {
            return hid_get_indexed_string(Device, Data, MaxLength);
        }

        [DllImport(HIDapiDll, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, EntryPoint = "hid_error", CharSet = CharSet.Unicode)]
        private static extern string hid_error(IntPtr device);
        public static string Error(IntPtr Device)
        {
            return hid_error(Device);
        }
    }
}
