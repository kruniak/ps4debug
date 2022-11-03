using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace libdebug
{
    public partial class PS4DBG
    {
        /// <summary>
        /// Get kernel base address
        /// </summary>
        /// <returns></returns>
        public ulong KernelBase()
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_KERN_BASE, 0);
            CheckStatus();

            byte[] buffer = new byte[KERN_BASE_SIZE];
            ReceiveData(buffer, KERN_BASE_SIZE);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        /// <summary>
        /// Read memory from kernel
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="length">Data length</param>
        /// <returns></returns>
        public void KernelReadMemory(byte[] buffer, int length, ulong address)
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_KERN_READ, CMD_KERN_READ_PACKET_SIZE, address, length);
            CheckStatus();

            ReceiveData(buffer, length);
        }

        /// <summary>
        /// Write memory in kernel
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="data">Data</param>
        public void KernelWriteMemory(ulong address, byte[] data)
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_KERN_WRITE, CMD_KERN_WRITE_PACKET_SIZE, address, data.Length);
            CheckStatus();
            SendData(data, data.Length);
            CheckStatus();
        }
    }
}