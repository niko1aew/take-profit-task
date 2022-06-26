using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketApp
{
    /// <summary>
    /// Abstraction for manipulating server key
    /// </summary>
    internal static class KeyStore
    {
        private static string _ip = string.Empty;
        private static int _port;
        private static readonly int _errorCooldownTime = 10000;
        private static int _refreshTimeout = 20000;
        private static bool _refreshRequired = false;

        /// <summary>
        /// Server key
        /// </summary>
        public static string TokenString { get; set; } = string.Empty;

        /// <summary>
        /// Start polling key from server every <paramref name="timeout"/> ms
        /// </summary>
        public static void StartRefreshing(string ip, int port)
        {
            _ip = ip;
            _port = port;
            _refreshRequired = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(KeyRequestTaskCallback));
        }

        /// <summary>
        /// Stop key polling
        /// </summary>
        public static void StopRefreshing()
        {
            _refreshRequired = false;
        }

        async static void KeyRequestTaskCallback(object? state)
        {
            while (_refreshRequired)
            {
                try
                {
                    using var client = new TcpClient();

                    await client.ConnectAsync(_ip, _port);
                    var sendBytes = Helper.EncodeString("Register");
                    var tcpStream = client.GetStream();
                    await tcpStream.WriteAsync(sendBytes);

                    using var reader = new StreamReader(tcpStream);

                    var receivedToken = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(receivedToken))
                    {
                        TokenString = receivedToken;
                    }

                    Console.WriteLine("Received token: " + receivedToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Token update error: " + e.Message);
                    await Task.Delay(_errorCooldownTime);
                }

                await Task.Delay(_refreshTimeout);
            }
        }
    }
}
