using System.Diagnostics;
using System.Net.Sockets;

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
        private static readonly SemaphoreSlim semaphore = new(1);

        /// <summary>
        /// Server key
        /// </summary>
        public static string TokenString { get; set; } = string.Empty;

        /// <summary>
        /// Show if actual key is expired
        /// </summary>
        public static bool IsKeyExpired { get; set; } = false;


        /// <summary>
        /// Initialize
        /// </summary>
        public static void Init(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public static async void RefreshKey(string expiredKey)
        {

            await semaphore.WaitAsync();

            if (expiredKey != TokenString)
            {
                semaphore.Release();
                return;
            }

            IsKeyExpired = true;

            string? receivedToken = null;

            while (string.IsNullOrEmpty(receivedToken))
            {
                try
                {
                    using var client = new TcpClient();

                    client.Connect(_ip, _port);

                    var sendBytes = Helper.EncodeString("Register");
                    var tcpStream = client.GetStream();
                    await tcpStream.WriteAsync(sendBytes);

                    using var reader = new StreamReader(tcpStream);

                    receivedToken = await reader.ReadLineAsync();

                    if (receivedToken == "Rate limit. Please wait some time then repeat.")
                    {
                        throw new Exception("Rate limit exceeded");
                    }
                    if (!string.IsNullOrWhiteSpace(receivedToken))
                    {
                        TokenString = receivedToken;
                        IsKeyExpired = false;
                    }
                }

                catch (Exception e)
                {
                    Debug.Print("Token update error: " + e.Message);
                    await Task.Delay(_errorCooldownTime);
                }
            }

            semaphore.Release();
        }
    }
}
