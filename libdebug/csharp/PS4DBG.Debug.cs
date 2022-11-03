using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Buffers;
using System.Buffers.Binary;

namespace libdebug
{
    public partial class PS4DBG
    {
        /// <summary>
        /// Debugger interrupt callback
        /// </summary>
        /// <param name="lwpid">Thread identifier</param>
        /// <param name="status">status</param>
        /// <param name="tdname">Thread name</param>
        /// <param name="regs">Registers</param>
        /// <param name="fpregs">Floating point registers</param>
        /// <param name="dbregs">Debug registers</param>
        public delegate void DebuggerInterruptCallback(uint lwpid, uint status, string tdname, GeneralRegisters regs, FloatingPointRegisters fpregs, DebugRegisters dbregs);
        private void DebuggerThread(object obj)
        {
            DebuggerInterruptCallback callback = (DebuggerInterruptCallback)obj;

            IPAddress ip = IPAddress.Parse("0.0.0.0");
            IPEndPoint endpoint = new IPEndPoint(ip, PS4DBG_DEBUG_PORT);

            Socket server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            server.Bind(endpoint);
            server.Listen(0);

            IsDebugging = true;

            Socket cl = server.Accept();

            cl.NoDelay = true;
            cl.Blocking = false;

            byte[] debugInterruptBuffer = new byte[DEBUG_INTERRUPT_SIZE];
            while (IsDebugging)
            {
                if (cl.Available == DEBUG_INTERRUPT_SIZE)
                {
                    int bytes = cl.Receive(debugInterruptBuffer, DEBUG_INTERRUPT_SIZE, SocketFlags.None);
                    if (bytes == DEBUG_INTERRUPT_SIZE)
                    {
                        DebuggerInterruptPacket packet = Utils.GetObjectFromBytes<DebuggerInterruptPacket>(debugInterruptBuffer);
                        callback(packet.LWPid, packet.Status, packet.ThreadName, packet.Registers, packet.FloatingRegisters, packet.DebugRegisters);
                    }
                }

                Thread.Sleep(100);
            }

            server.Close();
        }

        /// <summary>
        /// Attach the debugger
        /// </summary>
        /// <param name="pid">Process ID</param>
        /// <param name="callback">DebuggerInterruptCallback implementation</param>
        /// <returns></returns>
        public void AttachDebugger(int pid, DebuggerInterruptCallback callback)
        {
            CheckConnected();

            if (IsDebugging || _debugThread != null)
            {
                throw new Exception("libdbg: debugger already running?");
            }

            IsDebugging = false;

            _debugThread = new Thread(DebuggerThread) {IsBackground = true};
            _debugThread.Start(callback);

            // wait until server is started
            while (!IsDebugging)
            {
                Thread.Sleep(100);
            }

            SendCMDPacket(CMDS.CMD_DEBUG_ATTACH, CMD_DEBUG_ATTACH_PACKET_SIZE, pid);
            CheckStatus();
        }

        /// <summary>
        /// Detach the debugger
        /// </summary>
        /// <returns></returns>
        public void DetachDebugger()
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_DEBUG_DETACH, 0);
            CheckStatus();

            if (IsDebugging && _debugThread != null)
            {
                IsDebugging = false;

                _debugThread.Join();
                _debugThread = null;
            }
        }

        /// <summary>
        /// Stop the current process
        /// </summary>
        /// <returns></returns>
        public void ProcessStop()
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_STOPGO, CMD_DEBUG_STOPGO_PACKET_SIZE, 1);
            CheckStatus();
        }

        /// <summary>
        /// Kill the current process, it will detach before doing so
        /// </summary>
        /// <returns></returns>
        public void ProcessKill()
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_STOPGO, CMD_DEBUG_STOPGO_PACKET_SIZE, 2);
            CheckStatus();
        }

        /// <summary>
        /// Resume the current process
        /// </summary>
        /// <returns></returns>
        public void ProcessResume()
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_STOPGO, CMD_DEBUG_STOPGO_PACKET_SIZE, 0);
            CheckStatus();
        }

        /// <summary>
        /// Change breakpoint, to remove said breakpoint send the same index but disable it (address is ignored)
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="enabled">Enabled</param>
        /// <param name="address">Address</param>
        /// <returns></returns>
        public void ChangeBreakpoint(int index, bool enabled, ulong address)
        {
            CheckConnected();
            CheckDebugging();

            if (index >= MAX_BREAKPOINTS)
            {
                throw new Exception("libdbg: breakpoint index out of range");
            }

            SendCMDPacket(CMDS.CMD_DEBUG_BREAKPT, CMD_DEBUG_BREAKPT_PACKET_SIZE, index, enabled ? 1 : 0, address);
            CheckStatus();
        }

        /// <summary>
        /// Change watchpoint
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="enabled">Enabled</param>
        /// <param name="length">Length</param>
        /// <param name="breaktype">Break type</param>
        /// <param name="address">Address</param>
        /// <returns></returns>
        public void ChangeWatchpoint(int index, bool enabled, WATCHPT_LENGTH length, WATCHPT_BREAKTYPE breaktype, ulong address)
        {
            CheckConnected();
            CheckDebugging();

            if (index >= MAX_WATCHPOINTS)
            {
                throw new Exception("libdbg: watchpoint index out of range");
            }

            SendCMDPacket(CMDS.CMD_DEBUG_WATCHPT, CMD_DEBUG_WATCHPT_PACKET_SIZE, index, enabled ? 1 : 0, (uint)length, (uint)breaktype, address);
            CheckStatus();
        }

        /// <summary>
        /// Get a list of threads from the current process
        /// </summary>
        /// <returns></returns>
        public uint[] GetThreadList()
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_THREADS, 0);
            CheckStatus();

            Span<byte> data = stackalloc byte[4];
            _socket.Receive(data, SocketFlags.None);
            int number = BinaryPrimitives.ReadInt32LittleEndian(data);

            byte[] threads = ArrayPool<byte>.Shared.Rent(number * 4);
            ReceiveData(threads, number * 4);

            Span<uint> threadsIds = MemoryMarshal.Cast<byte, uint>(threads);
            uint[] thrlist = new uint[number];
            for (int i = 0; i < number; i++)
                thrlist[i] = threadsIds[i];

            ArrayPool<byte>.Shared.Return(threads);

            return thrlist;
        }

        /// <summary>
        /// Get thread information
        /// </summary>
        /// <returns></returns>
        /// <param name="lwpid">Thread identifier</param>
        public ThreadInfo GetThreadInfo(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_THRINFO, CMD_DEBUG_THRINFO_PACKET_SIZE, lwpid);
            CheckStatus();

            return ReceiveData<ThreadInfo>();
        }

        /// <summary>
        /// Stop a thread from running
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <returns></returns>
        public void StopThread(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_STOPTHR, CMD_DEBUG_STOPTHR_PACKET_SIZE, lwpid);
            CheckStatus();
        }

        /// <summary>
        /// Resume a thread from being stopped
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <returns></returns>
        public void ResumeThread(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_RESUMETHR, CMD_DEBUG_RESUMETHR_PACKET_SIZE, lwpid);
            CheckStatus();
        }

        /// <summary>
        /// Get registers from thread
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <returns></returns>
        public GeneralRegisters GetRegisters(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_GETREGS, CMD_DEBUG_GETREGS_PACKET_SIZE, lwpid);
            CheckStatus();

            // No marshalling needed
            byte[] buffer = new byte[DEBUG_REGS_SIZE];
            ReceiveData(buffer, DEBUG_REGS_SIZE);
            return MemoryMarshal.Cast<byte, GeneralRegisters>(buffer)[0];
        }

        /// <summary>
        /// Set thread registers
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <param name="regs">Register data</param>
        /// <returns></returns>
        public void SetRegisters(uint lwpid, GeneralRegisters regs)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_SETREGS, CMD_DEBUG_SETREGS_PACKET_SIZE, lwpid, DEBUG_REGS_SIZE);
            CheckStatus();

            // No marshalling needed
            Span<GeneralRegisters> regSpan = MemoryMarshal.CreateSpan(ref regs, 1);
            Span<byte> bytes = MemoryMarshal.Cast<GeneralRegisters, byte>(regSpan);
            SendData(bytes, DEBUG_REGS_SIZE);

            CheckStatus();
        }

        /// <summary>
        /// Get floating point registers from thread
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <returns></returns>
        public FloatingPointRegisters GetFloatRegisters(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_GETFPREGS, CMD_DEBUG_GETREGS_PACKET_SIZE, lwpid);
            CheckStatus();

            return ReceiveData<FloatingPointRegisters>();
        }

        /// <summary>
        /// Set floating point thread registers
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <param name="fpregs">Floating point register data</param>
        /// <returns></returns>
        public void SetFloatRegisters(uint lwpid, FloatingPointRegisters fpregs)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_SETFPREGS, CMD_DEBUG_SETREGS_PACKET_SIZE, lwpid, DEBUG_FPREGS_SIZE);
            CheckStatus();

            SendData(fpregs);
            CheckStatus();
        }

       

        /// <summary>
        /// Get debug registers from thread
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <returns></returns>
        public DebugRegisters GetDebugRegisters(uint lwpid)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_GETDBGREGS, CMD_DEBUG_GETREGS_PACKET_SIZE, lwpid);
            CheckStatus();

            // No marshalling needed
            byte[] buffer = new byte[DEBUG_REGS_SIZE];
            ReceiveData(buffer, DEBUG_REGS_SIZE);
            return MemoryMarshal.Cast<byte, DebugRegisters>(buffer)[0];
        }

        /// <summary>
        /// Set debug thread registers
        /// </summary>
        /// <param name="lwpid">Thread id</param>
        /// <param name="dbregs">debug register data</param>
        /// <returns></returns>
        public void SetDebugRegisters(uint lwpid, DebugRegisters dbregs)
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_SETDBGREGS, CMD_DEBUG_SETREGS_PACKET_SIZE, lwpid, DEBUG_DBGREGS_SIZE);
            CheckStatus();

            // No marshalling needed
            Span<DebugRegisters> regSpan = MemoryMarshal.CreateSpan(ref dbregs, 1);
            Span<byte> bytes = MemoryMarshal.Cast<DebugRegisters, byte>(regSpan);
            SendData(bytes, DEBUG_DBGREGS_SIZE);

            CheckStatus();
        }

        /// <summary>
        /// Executes a single instruction
        /// </summary>
        public void SingleStep()
        {
            CheckConnected();
            CheckDebugging();

            SendCMDPacket(CMDS.CMD_DEBUG_SINGLESTEP, 0);
            CheckStatus();
        }
    }
}