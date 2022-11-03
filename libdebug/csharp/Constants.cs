using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libdebug
{
    public partial class PS4DBG
    {
        private const string LIBRARY_VERSION = "1.2";
        private const int PS4DBG_PORT = 744;
        private const int PS4DBG_DEBUG_PORT = 755;
        private const int NET_MAX_LENGTH = 8192;

        private const int BROADCAST_PORT = 1010;
        private const uint BROADCAST_MAGIC = 0xFFFFAAAA;

        // from protocol.h
        // each packet starts with the magic
        // each C# base type can translate into a packet field
        // some packets, such as write take an additional data whose length will be specified in the cmd packet data field structure specific to that cmd type
        // ushort - 2 bytes | uint - 4 bytes | ulong - 8 bytes
        private const uint CMD_PACKET_MAGIC = 0xFFAABBCC;

        // from debug.h
        //struct debug_breakpoint {
        //    uint32_t valid;
        //    uint64_t address;
        //    uint8_t original;
        //};
        public static uint MAX_BREAKPOINTS = 10;
        public static uint MAX_WATCHPOINTS = 4;

        //  struct cmd_packet {
        //    uint32_t magic;
        //    uint32_t cmd;
        //    uint32_t datalen;
        //    // (field not actually part of packet, comes after)
        //    uint8_t* data;
        //  }
        //  __attribute__((packed));
        //  #define CMD_PACKET_SIZE 12
        private const int CMD_PACKET_SIZE = 12;

        //debug
        // packet sizes
        //send size
        private const int CMD_DEBUG_ATTACH_PACKET_SIZE = 4;
        private const int CMD_DEBUG_BREAKPT_PACKET_SIZE = 16;
        private const int CMD_DEBUG_WATCHPT_PACKET_SIZE = 24;
        private const int CMD_DEBUG_STOPTHR_PACKET_SIZE = 4;
        private const int CMD_DEBUG_RESUMETHR_PACKET_SIZE = 4;
        private const int CMD_DEBUG_GETREGS_PACKET_SIZE = 4;
        private const int CMD_DEBUG_SETREGS_PACKET_SIZE = 8;
        private const int CMD_DEBUG_STOPGO_PACKET_SIZE = 4;
        private const int CMD_DEBUG_THRINFO_PACKET_SIZE = 4;
        //receive size
        private const int DEBUG_INTERRUPT_SIZE = 0x4A0;
        private const int DEBUG_THRINFO_SIZE = 40;
        private const int DEBUG_REGS_SIZE = 0xB0;
        private const int DEBUG_FPREGS_SIZE = 0x340;
        private const int DEBUG_DBGREGS_SIZE = 0x80;

        //proc
        // packet sizes
        // send size
        private const int CMD_PROC_READ_PACKET_SIZE = 16;
        private const int CMD_PROC_WRITE_PACKET_SIZE = 16;
        private const int CMD_PROC_MAPS_PACKET_SIZE = 4;
        private const int CMD_PROC_INSTALL_PACKET_SIZE = 4;
        private const int CMD_PROC_CALL_PACKET_SIZE = 68;
        private const int CMD_PROC_ELF_PACKET_SIZE = 8;
        private const int CMD_PROC_PROTECT_PACKET_SIZE = 20;
        private const int CMD_PROC_SCAN_PACKET_SIZE = 10;
        private const int CMD_PROC_INFO_PACKET_SIZE = 4;
        private const int CMD_PROC_ALLOC_PACKET_SIZE = 8;
        private const int CMD_PROC_FREE_PACKET_SIZE = 16;
        // receive size
        private const int PROC_LIST_ENTRY_SIZE = 36;
        private const int PROC_MAP_ENTRY_SIZE = 58;
        private const int PROC_INSTALL_SIZE = 8;
        private const int PROC_CALL_SIZE = 12;
        private const int PROC_PROC_INFO_SIZE = 188;
        private const int PROC_ALLOC_SIZE = 8;

        //console
        // packet sizes
        // send size
        private const int CMD_CONSOLE_PRINT_PACKET_SIZE = 4;
        private const int CMD_CONSOLE_NOTIFY_PACKET_SIZE = 8;

        // kernel
        //packet sizes
        //send size
        private const int CMD_KERN_READ_PACKET_SIZE = 12;
        private const int CMD_KERN_WRITE_PACKET_SIZE = 12;
        //receive size
        private const int KERN_BASE_SIZE = 8;
    }
}
