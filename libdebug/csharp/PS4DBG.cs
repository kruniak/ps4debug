using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace libdebug
{
    /// <summary>
    /// The PS4 Debugger.
    /// </summary>
    public partial class PS4DBG : IDisposable
    {
        private Socket _socket = null;
        private IPEndPoint _endpoint = null;
        private Thread _debugThread = null;

        /// <summary>
        /// Whether the debugger is connected to the console.
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Whether the debugger is active and attached to a process.
        /// </summary>
        public bool IsDebugging { get; private set; } = false;

        /// <summary>
        /// Initializes PS4DBG class
        /// </summary>
        /// <param name="addr">PlayStation 4 address</param>
        public PS4DBG(IPAddress addr)
        {
            _endpoint = new IPEndPoint(addr, PS4DBG_PORT);
            _socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Initializes PS4DBG class
        /// </summary>
        /// <param name="ip">PlayStation 4 ip address</param>
        public PS4DBG(string ip)
        {
            if (!IPAddress.TryParse(ip, out IPAddress addr))
                throw new FormatException("Unable to parse IP Address.");

            _endpoint = new IPEndPoint(addr, PS4DBG_PORT);
            _socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Find the playstation ip
        /// </summary>
        public static string FindPlayStation()
        {
            UdpClient uc = new UdpClient();
            IPEndPoint server = new IPEndPoint(IPAddress.Any, 0);
            uc.EnableBroadcast = true;
            uc.Client.ReceiveTimeout = 4000;

            IPAddress addr = null;
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    addr = ip;
            }

            if (addr == null)
                throw new Exception("libdbg broadcast error: could not get host ip");

            Span<byte> magicBuf = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(magicBuf, BROADCAST_MAGIC);

            uc.Send(magicBuf, new IPEndPoint(Utils.GetBroadcastAddress(addr, IPAddress.Parse("255.255.255.0")), BROADCAST_PORT));

            byte[] resp = uc.Receive(ref server);
            if (BinaryPrimitives.ReadUInt32LittleEndian(resp) != BROADCAST_MAGIC)
                throw new Exception("libdbg broadcast error: wrong magic on udp server");

            uc.Dispose();

            return server.Address.ToString();
        }

        /// <summary>
        /// Connects to PlayStation 4
        /// </summary>
        public void Connect()
        {
            if (!IsConnected)
            {
                _socket.NoDelay = true;
                _socket.ReceiveBufferSize = NET_MAX_LENGTH;
                _socket.SendBufferSize = NET_MAX_LENGTH;

                _socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

                _socket.Connect(_endpoint);
                IsConnected = true;
            }
        }

        /// <summary>
        /// Disconnects from PlayStation 4
        /// </summary>
        public void Disconnect()
        {
            SendCMDPacket(CMDS.CMD_CONSOLE_END, 0);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            IsConnected = false;
        }

        /// <summary>
        /// Get current ps4debug version from library
        /// </summary>
        public string GetLibraryDebugVersion()
        {
            return LIBRARY_VERSION;
        }

        /// <summary>
        /// Get the current ps4debug version from console
        /// </summary>
        public string GetConsoleDebugVersion()
        {
            CheckConnected();

            SendCMDPacket(CMDS.CMD_VERSION, 0);

            Span<byte> ldata = stackalloc byte[4];
            _socket.Receive(ldata, SocketFlags.None);

            int length = BinaryPrimitives.ReadInt32LittleEndian(ldata);

            byte[] data = new byte[length];
            _socket.Receive(data, length, SocketFlags.None);

            return Utils.ReadNullTerminated(data, 0);
        }

        private void SendCMDPacket(CMDS cmd, int length, params object[] fields)
        {
            CMDPacket packet = new CMDPacket
            {
                Magic = CMD_PACKET_MAGIC,
                Command = (uint) cmd,
                DataLength = (uint) length
            };


            const int MaxCmdPacketSize = 0x2000;
            byte[] dataBuffer = ArrayPool<byte>.Shared.Rent(MaxCmdPacketSize);

            if (length > 0)
            {
                using MemoryStream rs = new MemoryStream(dataBuffer);
                using BinaryWriter bw = new BinaryWriter(rs);

                foreach (object field in fields)
                {
                    switch (field)
                    {
                        case char c:
                            rs.WriteByte((byte)c);
                            break;
                        case byte b:
                            rs.WriteByte(b);
                            break;
                        case short s:
                            bw.Write(s);
                            break;
                        case ushort us:
                            bw.Write(us);
                            break;
                        case int i:
                            bw.Write(i);
                            break;
                        case uint u:
                            bw.Write(u);
                            break;
                        case long l:
                            bw.Write(l);
                            break;
                        case ulong ul:
                            bw.Write(ul);
                            break;
                        case byte[] ba:
                            bw.Write(ba);
                            break;
                    }
                }
            }

            var span = MemoryMarshal.CreateReadOnlySpan(ref packet, 1);
            ReadOnlySpan<byte> packetData = MemoryMarshal.Cast<CMDPacket, byte>(span);
            SendData(packetData, CMD_PACKET_SIZE);

            if (length > 0)
                SendData(dataBuffer, length);

            ArrayPool<byte>.Shared.Return(dataBuffer);
        }

        /// <summary>
        /// Send bytes
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        private void SendData(ReadOnlySpan<byte> data, int length)
        {
            int left = length;
            int offset = 0;
            int sent = 0;

            while (left > 0)
            {
                ReadOnlySpan<byte> toSend = data.Slice(offset, Math.Min(left, NET_MAX_LENGTH)); 
                sent = _socket.Send(toSend, SocketFlags.None);
                
                offset += sent;
                left -= sent;
            }
        }

        /// <summary>
        /// Send object through marshalling
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="pktSize"></param>
        private void SendData<T>(T obj)
        {
            int size = Marshal.SizeOf<T>();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            Utils.GetBytesFromObject(buffer, size, obj);
            SendData(buffer, size);

            ArrayPool<byte>.Shared.Return(buffer);
        }

        /// <summary>
        /// Receives specified amount of bytes
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        private void ReceiveData(byte[] buffer, int length)
        {
            using var outputStream = new MemoryStream(buffer);
            byte[] recvBuffer = ArrayPool<byte>.Shared.Rent(NET_MAX_LENGTH);

            int left = length;
            int recv = 0;
            while (left > 0)
            {
                recv = _socket.Receive(recvBuffer, Math.Min(left, NET_MAX_LENGTH), SocketFlags.None);
                outputStream.Write(recvBuffer, 0, recv);
                left -= recv;
            }

            ArrayPool<byte>.Shared.Return(recvBuffer);
        }

        /// <summary>
        /// Receives an object data through marshalling
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T ReceiveData<T>()
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            ReceiveData(buffer, size);

            T obj = Utils.GetObjectFromBytes<T>(buffer);
            ArrayPool<byte>.Shared.Return(buffer);

            return obj;
        }

        private CMD_STATUS ReceiveStatus()
        {
            Span<byte> status = stackalloc byte[4];
            _socket.Receive(status, SocketFlags.None);
            return (CMD_STATUS)BinaryPrimitives.ReadUInt32LittleEndian(status);
        }

        private void CheckStatus()
        {
            CMD_STATUS status = ReceiveStatus();
            if (status != CMD_STATUS.CMD_SUCCESS)
                throw new Exception($"libdbg status {status} (0x{status:X8})");
        }

        private void CheckConnected()
        {
            if (!IsConnected)
                throw new Exception("libdbg: not connected");
        }

        private void CheckDebugging()
        {
            if (!IsDebugging)
                throw new Exception("libdbg: not debugging");
        }

        public void Dispose()
        {
            if (IsDebugging)
            {
                DetachDebugger();
            }

            ((IDisposable)_socket).Dispose();
        }
    }
}