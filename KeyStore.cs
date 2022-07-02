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
        private static readonly int _refreshTimeout = 20000;
        private static bool _refreshRequired = false;
        private static Object _lockObj = new object();
        private static bool _isRefreshing = false;
        //private static readonly SemaphoreLocker _locker = new SemaphoreLocker();
        private static SemaphoreSlim semaphore = new(1);
        /// <summary>
        /// Server key
        /// </summary>
        public static string TokenString { get; set; } = string.Empty;

        public static bool IsKeyExpired { get; set; } = false;

        //public static bool IsRefreshInProgress { get; set; }

        public static int RefreshCount { get; set; } = 0;


        /// <summary>
        /// Start polling key from server every <paramref name="timeout"/> ms
        /// </summary>
        public static void StartRefreshing(string ip, int port)
        {
            _ip = ip;
            _port = port;
            _refreshRequired = true;
            //ThreadPool.QueueUserWorkItem(new WaitCallback(KeyRequestTaskCallback));
        }

        /// <summary>
        /// Stop key polling
        /// </summary>
        public static void StopRefreshing()
        {
            _refreshRequired = false;
        }

        //public static void SetKeyExpired()
        //{
        //    IsKeyExpired = true;
        //    RefreshKey();
        //}

        public static async void RefreshKey(string key)
        {
            try
            {
                await semaphore.WaitAsync();

                if (key != TokenString)
                {
                    semaphore.Release();
                    return;
                }
                if (!_isRefreshing)
                {
                    _isRefreshing = true;
                    IsKeyExpired = true;
                    //Console.SetCursorPosition(0, 20);
                    Console.WriteLine($"<{Thread.CurrentThread.ManagedThreadId}> refreshing");
                    //IsRefreshInProgress = true;
                    string? receivedToken = null;
                    RefreshCount++;
                    //Console.SetCursorPosition(0, 15);
                    Console.WriteLine("Refresh count" + RefreshCount.ToString());
                    while (string.IsNullOrEmpty(receivedToken))
                    {

                        using var client = new TcpClient();

                        client.Connect(_ip, _port);
                        var sendBytes = Helper.EncodeString("Register");
                        var tcpStream = client.GetStream();
                        await tcpStream.WriteAsync(sendBytes);

                        using var reader = new StreamReader(tcpStream);

                        receivedToken = await reader.ReadLineAsync();

                        Console.WriteLine($"Token: {receivedToken}");

                        if (!string.IsNullOrWhiteSpace(receivedToken)
                            && receivedToken != "Rate limit. Please wait some time then repeat.")
                        {
                            TokenString = receivedToken;
                            IsKeyExpired = false;

                            //IsRefreshInProgress = false;
                        }

                    };
                    _isRefreshing = false;
                    semaphore.Release();
                }
                semaphore.Release();
            }
            catch (Exception e)
            {
                _isRefreshing = false;
                semaphore.Release();
                Debug.Print("Token update error: " + e.Message);
                //await Task.Delay(_errorCooldownTime);
                //Thread.Sleep(_errorCooldownTime);
            }
        }



        //Console.WriteLine($"<{Thread.CurrentThread.ManagedThreadId}> enter refresh");
        //await _locker.LockAsync(async () =>
        //{
        //    if (true)
        //    {
        //        Console.WriteLine($"<{Thread.CurrentThread.ManagedThreadId}> refreshing");
        //        IsRefreshInProgress = true;
        //        string? receivedToken = null;
        //        RefreshCount++;
        //        Console.SetCursorPosition(0, 3);
        //        Console.WriteLine("Refresh count" + RefreshCount.ToString());
        //        while (string.IsNullOrEmpty(receivedToken))
        //        {
        //            try
        //            {
        //                using var client = new TcpClient();

        //                await client.ConnectAsync(_ip, _port);
        //                var sendBytes = Helper.EncodeString("Register");
        //                var tcpStream = client.GetStream();
        //                await tcpStream.WriteAsync(sendBytes);

        //                using var reader = new StreamReader(tcpStream);

        //                receivedToken = await reader.ReadLineAsync();

        //                if (!string.IsNullOrWhiteSpace(receivedToken)
        //                    && receivedToken != "Rate limit. Please wait some time then repeat.")
        //                {
        //                    TokenString = receivedToken;
        //                    IsKeyExpired = false;
        //                    IsRefreshInProgress = false;
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                Debug.Print("Token update error: " + e.Message);
        //                await Task.Delay(_errorCooldownTime);
        //            }
        //        }
        //    }


        //});
        //IsKeyExpired = false;
        //return true;
    

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
            }
            catch (Exception e)
            {
                Debug.Print("Token update error: " + e.Message);
                await Task.Delay(_errorCooldownTime);
            }

            await Task.Delay(_refreshTimeout);
        }
    }
}
}
