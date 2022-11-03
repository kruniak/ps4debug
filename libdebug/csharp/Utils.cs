using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;

namespace libdebug
{
    public static class Utils
    {
        // General helper functions, make code cleaner
        public static string ReadNullTerminated(byte[] data, int offset)
        {
            var span = data.AsSpan(offset);
            var terminatorIndex = span.IndexOf((byte)'\0');
            return Encoding.ASCII.GetString(span.Slice(0, terminatorIndex));
        }

        public static T GetObjectFromBytes<T>(byte[] buffer)
        {
            int size = Marshal.SizeOf<T>();

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(buffer, 0, ptr, size);
            T obj = Marshal.PtrToStructure<T>(ptr);

            Marshal.FreeHGlobal(ptr);

            return obj;
        }

        public static void GetBytesFromObject<T>(byte[] buffer, int objSize, T obj)
        {
            IntPtr ptr = Marshal.AllocHGlobal(objSize);

            Marshal.StructureToPtr<T>(obj, ptr, false);
            Marshal.Copy(ptr, buffer, 0, objSize);

            Marshal.FreeHGlobal(ptr);
        }

        public static byte[] GetBytesFromObject<T>(T obj)
        {
            int size = Marshal.SizeOf<T>();

            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr<T>(obj, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);

            Marshal.FreeHGlobal(ptr);

            return bytes;
        }

        // General networking functions
        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        }
    }
}
