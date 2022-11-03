using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace libdebug
{
    public partial class PS4DBG
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CMDPacket
        {
            public uint Magic;
            public uint Command;
            public uint DataLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DebuggerInterruptPacket
        {
            public uint LWPid;
            public uint Status;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string ThreadName;
            public GeneralRegisters Registers;
            public FloatingPointRegisters FloatingRegisters;
            public DebugRegisters DebugRegisters;
        }
    }
}
