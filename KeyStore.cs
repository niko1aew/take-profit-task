using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketApp
{
    internal static class KeyStore
    {
        private static string _ip = string.Empty;
        private static int _port;
        private static readonly int _errorCooldownTime = 10000;
        private static int _refreshTimeout = 20000;
        private static bool _refreshRequired = false;

        public static string TokenString { get; set; } = string.Empty;

        
        public static void StartRefreshing(string ip, int port, int timeout)
        {
            _ip = ip;
            _port = port;
            _refreshTimeout = timeout;
            _refreshRequired = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallback), new Object());
        }

        public static void StopRefreshing()
        {
            _refreshRequired = false;
        }

        async static void TaskCallback(Object? state)
        {
            while (_refreshRequired)
            {
                try
                {
                    using var client = new TcpClient();

                    await client.ConnectAsync(_ip, _port);
                    var sendBytes = SocketHelper.EncodeString("Register");
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
