using System.Runtime.InteropServices;


namespace libdebug
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GeneralRegisters
    {
        /// <summary>
        /// General purpose register - callee-saved 
        /// </summary>
        public ulong r15;

        /// <summary>
        /// General purpose register - callee-saved 
        /// </summary>
        public ulong r14;

        /// <summary>
        /// General purpose register - callee-saved 
        /// </summary>
        public ulong r13;

        /// <summary>
        /// General purpose register - callee-saved 
        /// </summary>
        public ulong r12;

        /// <summary>
        /// General purpose register - temporary 
        /// </summary>
        public ulong r11;

        /// <summary>
        /// General purpose register - temporary 
        /// </summary>
        public ulong r10;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong r9;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong r8;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong rdi;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong rsi;

        /// <summary>
        /// General purpose register - stack management/base frame pointer (meant for stack frames)
        /// </summary>
        public ulong rbp;

        /// <summary>
        /// General purpose register - callee-saved
        /// </summary>
        public ulong rbx;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong rdx;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong rcx;

        /// <summary>
        /// General purpose register - argument passing for function calls 
        /// </summary>
        public ulong rax;

        /// <summary>
        /// Exception vector number
        /// </summary>
        public uint trapno;

        /// <summary>
        /// General-purpose Segment
        /// </summary>
        public ushort fs;

        /// <summary>
        /// General-purpose Segment
        /// </summary>
        public ushort gs;

        /// <summary>
        /// Exception error code register
        /// </summary>
        public uint err;

        /// <summary>
        /// Extra Segment register
        /// </summary>
        public ushort es;

        /// <summary>
        /// Data segment register
        /// </summary>
        public ushort ds;

        /// <summary>
        /// Instruction Pointer
        /// </summary>
        public ulong rip;

        /// <summary>
        /// Code segment register
        /// </summary>
        public ulong cs;
        
        /// <summary>
        /// Flags register
        /// </summary>
        public ulong rflags;

        /// <summary>
        /// Stack pointer register
        /// </summary>
        public ulong rsp;

        /// <summary>
        /// Stack segment register
        /// </summary>
        public ulong ss;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct envxmm
    {
        /// <summary>
        /// Control Word (16 bits)
        /// </summary>
        public ushort en_cw;

        /// <summary>
        /// Status word (16 bits)
        /// </summary>
        public ushort en_sw;

        /// <summary>
        /// Tag word (8 bits)
        /// </summary>
        public byte en_tw;
        public byte en_zero;

        /// <summary>
        /// Opcode Last Executed (11 bits)
        /// </summary>
        public ushort en_opcode;

        /// <summary>
        /// Floating point instruction pointer
        /// </summary>
        public ulong en_rip;

        /// <summary>
        /// Floating Operand Pointer
        /// </summary>
        public ulong en_rdp;

        /// <summary>
        /// SSE control/status register
        /// </summary>
        public uint en_mxcsr;

        /// <summary>
        /// Valid bits in mxcsr
        /// </summary>
        public uint en_mxcsr_mask; /* valid bits in mxcsr */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct acc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] fp_bytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private byte[] fp_pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmmacc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] xmm_bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ymmacc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ymm_bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xstate_hdr
    {
        public ulong xstate_bv;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private byte[] xstate_rsrv0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        private byte[] xstate_rsrv;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct savefpu_xstate
    {
        public xstate_hdr sx_hd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ymmacc[] sx_ymm;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 64)]
    public struct FloatingPointRegisters
    {
        public envxmm svn_env;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public acc[] sv_fp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public xmmacc[] sv_xmm;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        private byte[] sv_pad;

        public savefpu_xstate sv_xstate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DebugRegisters
    {
        public ulong dr0;
        public ulong dr1;
        public ulong dr2;
        public ulong dr3;
        public ulong dr4;
        public ulong dr5;
        public ulong dr6;
        public ulong dr7;
        public ulong dr8;
        public ulong dr9;
        public ulong dr10;
        public ulong dr11;
        public ulong dr12;
        public ulong dr13;
        public ulong dr14;
        public ulong dr15;
    }

}