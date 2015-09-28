using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace MinecraftStatus
{
    /// <summary>
    /// Query a minecraft server to obtains the status (according this documentation : http://wiki.vg/Query).
    /// </summary>
    public class Status
    {

        const Byte Statistic = 0x00;
        const Byte Handshake = 0x09;

        private readonly Dictionary<string, string> _keyValues;
        private List<string> _players;

        public string MessageOfTheDay
        {
            get { return _keyValues["hostname"]; }
        }

        public string Gametype
        {
            get { return _keyValues["gametype"]; }
        }

        public string GameId
        {
            get { return _keyValues["game_id"]; }
        }

        public string Version
        {
            get { return _keyValues["version"]; }
        }

        public string Plugins
        {
            get { return _keyValues["plugins"]; }
        }

        public string Map
        {
            get { return _keyValues["map"]; }
        }
        public string NumPlayers
        {
            get { return _keyValues["numplayers"]; }
        }
        public string MaxPlayers
        {
            get { return _keyValues["maxplayers"]; }
        }
        public string HostPort
        {
            get { return _keyValues["hostport"]; }
        }
        public string HostIp
        {
            get { return _keyValues["hostip"]; }
        }

        public IEnumerable<string> Players
        {
            get { return _players; }
        }

        internal Status(byte[] message)
        {
            _keyValues = new Dictionary<string, string>();
            _players = new List<string>();

            var buffer = new byte[256];
            Stream stream = new MemoryStream(message);

            stream.Read(buffer, 0, 5);// Read Type + SessionID
            stream.Read(buffer, 0, 11); // Padding: 11 bytes constant
            var constant1 = new byte[] { 0x73, 0x70, 0x6C, 0x69, 0x74, 0x6E, 0x75, 0x6D, 0x00, 0x80, 0x00 };
            for (int i = 0; i < constant1.Length; i++) Debug.Assert(constant1[i] == buffer[i], "Byte mismatch at " + i + " Val :" + buffer[i]);

            var sb = new StringBuilder();
            string lastKey = string.Empty;
            int currentByte;
            while ((currentByte = stream.ReadByte()) != -1)
            {
                if (currentByte == 0x00)
                {
                    if (!string.IsNullOrEmpty(lastKey))
                    {
                        _keyValues.Add(lastKey, sb.ToString());
                        lastKey = string.Empty;
                    }
                    else
                    {
                        lastKey = sb.ToString();
                        if (string.IsNullOrEmpty(lastKey)) break;
                    }
                    sb.Clear();
                }
                else sb.Append((char)currentByte);
            }

            stream.Read(buffer, 0, 10); // Padding: 10 bytes constant
            var constant2 = new byte[] { 0x01, 0x70, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x5F, 0x00, 0x00 };
            for (int i = 0; i < constant2.Length; i++) Debug.Assert(constant2[i] == buffer[i], "Byte mismatch at " + i + " Val :" + buffer[i]);

            while ((currentByte = stream.ReadByte()) != -1)
            {
                if (currentByte == 0x00)
                {
                    var player = sb.ToString();
                    if (string.IsNullOrEmpty(player)) break;
                    _players.Add(player);
                    sb.Clear();
                }
                else sb.Append((char)currentByte);
            }
        }


        /// <summary>
        /// Get the status of the given host and optional port
        /// </summary>
        /// <param name="host">The host name or address (monserver.com or 123.123.123.123)</param>
        /// <param name="port">The query port, by default is 25565</param>
        public static Status GetStatus(string host, int port = 25565)
        {
            var e = new IPEndPoint(IPAddress.Any, port);
            using (var u = new UdpClient(e))
            {
                try
                {
                    var s = new UdpState { EndPoint = e, Client = u };
                    u.Connect(host, port);
                    var status = GetStatus(s);
                    return new Status(status);
                }
                finally
                {
                    u.Close();
                }
            }
        }


        static byte[] GetStatus(UdpState s)
        {
            var challengeToken = GetChallengeToken(s);

            //append 4 bytes to obtains the Full status
            WriteData(s, Statistic, challengeToken, new byte[] { 0x00, 0x00, 0x00, 0x00 });
            return ReceiveMessages(s);
        }

        static byte[] GetChallengeToken(UdpState s)
        {
            WriteData(s, Handshake);

            var message = ReceiveMessages(s);

            var challangeBytes = new byte[16];
            Array.Copy(message, 5, challangeBytes, 0, message.Length - 5);
            var challengeInt = int.Parse(Encoding.ASCII.GetString(challangeBytes));
            return BitConverter.GetBytes(challengeInt).Reverse().ToArray();

        }


        static void WriteData(UdpState s, byte cmd, byte[] append = null, byte[] append2 = null)
        {
            var cmdData = new byte[] { 0xFE, 0xFD, cmd, 0x01, 0x02, 0x03, 0x04 };
            var dataLength = cmdData.Length + (append != null ? append.Length : 0) + (append2 != null ? append2.Length : 0);
            var data = new byte[dataLength];
            cmdData.CopyTo(data, 0);
            if (append != null) append.CopyTo(data, cmdData.Length);
            if (append2 != null) append2.CopyTo(data, cmdData.Length + (append != null ? append.Length : 0));
            s.Client.Send(data, data.Length);
        }

        static byte[] ReceiveMessages(UdpState s)
        {
            return s.Client.Receive(ref s.EndPoint);
        }

        class UdpState
        {
            public UdpClient Client;
            public IPEndPoint EndPoint;
        }
    }
}
