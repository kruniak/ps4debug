using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Buffers;

namespace libdebug
{
    public partial class PS4DBG
    {   
        // console
        // note: the disconnect command actually uses the console api to end the connection
        /// <summary>
        /// Reboot console
        /// </summary>
        public void Reboot()
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_CONSOLE_REBOOT, 0);
            IsConnected = false;
        }

        /// <summary>
        /// Print to serial port
        /// </summary>
        public void Print(string str)
        {
            CheckConnected();

            int byteCnt = Encoding.ASCII.GetByteCount(str);
            byte[] strBuffer = ArrayPool<byte>.Shared.Rent(byteCnt + 1);
            Encoding.ASCII.GetBytes(str, strBuffer);
            strBuffer[byteCnt] = (byte)'\0';

            SendCMDPacket(CMDS.CMD_CONSOLE_PRINT, CMD_CONSOLE_PRINT_PACKET_SIZE, byteCnt + 1);
            SendData(strBuffer, byteCnt + 1);

            ArrayPool<byte>.Shared.Return(strBuffer);
            CheckStatus();
        }

        /// <summary>
        /// Notify console
        /// </summary>
        public void Notify(int messageType, string message)
        {
            CheckConnected();

            int byteCnt = Encoding.ASCII.GetByteCount(message);
            byte[] msgBuffer = ArrayPool<byte>.Shared.Rent(byteCnt + 1);
            Encoding.ASCII.GetBytes(message, msgBuffer);
            msgBuffer[byteCnt] = (byte)'\0';

            SendCMDPacket(CMDS.CMD_CONSOLE_NOTIFY, CMD_CONSOLE_NOTIFY_PACKET_SIZE, messageType, byteCnt + 1);
            SendData(msgBuffer, byteCnt + 1);

            ArrayPool<byte>.Shared.Return(msgBuffer);
            CheckStatus();
        }

        /// <summary>
        /// Console information
        /// </summary>
        public void GetConsoleInformation()
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_CONSOLE_INFO, 0);
            CheckStatus();

            // TODO return the data
        }
    }
}