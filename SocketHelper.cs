using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SocketApp
{
    internal static class SocketHelper
    {
        private static readonly int port = 2013;
        private static readonly string ip = "88.212.241.115";

        private static readonly Regex rxLF = new(@".*[\n]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex rxNumberTail = new(@"\d[^\d]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex rxCropNumber = new(@"[^0-9]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static Socket GetSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 4000,
                SendTimeout = 4000
            };

            return socket;
        }

        internal static IPEndPoint GetEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse(ip), port);
        }

        internal static bool CheckReceiveTerminationRequired(string lastMsg, string completeMsg)
        {
            if (rxLF.Matches(lastMsg).Count > 0) return true;
            if (rxNumberTail.Matches(completeMsg).Count > 0) return true;
            return false;
        }

        internal static int? GetNumberFromSourceString(string sourceString)
        {
            var strNumber = rxCropNumber.Replace(sourceString.ToString(), "");
            Console.WriteLine($"{Environment.CurrentManagedThreadId}: Find number {strNumber}");
            return int.TryParse(strNumber, out int resultNumber) ? resultNumber : null;
        }

        internal static int? GetNumber(int inputNumber)
        {
            
            var tcpEndpoint = GetEndpoint();
            var tcpSocket = GetSocket();

            var message = $"{inputNumber}\n";
            var data = Encoding.UTF8.GetBytes(message);
            var buffer = new byte[256];
            var answer = new StringBuilder();

            var availableIdleCycles = 30;

            string str;

            var isNumberReceived = false;

            try
            {
                tcpSocket.Connect(tcpEndpoint);
                //Console.WriteLine($"{Environment.CurrentManagedThreadId}: Socket {tcpSocket.Connected}");
                //Console.WriteLine($"{Environment.CurrentManagedThreadId}: Sending {message}");
                var bytesSent = tcpSocket.Send(data);
                //Console.WriteLine($"{Environment.CurrentManagedThreadId}: sent {bytesSent} bytes");
                do
                {
                    if (tcpSocket.Available == 0)
                    {
                        availableIdleCycles--;
                        Thread.Sleep(100);
                    }

                    var size = tcpSocket.Receive(buffer);
                    str = Encoding.UTF8.GetString(buffer, 0, size);
                    answer.Append(str);
                    //if (!string.IsNullOrWhiteSpace(str))
                    //{
                    //    Console.WriteLine($"{Environment.CurrentManagedThreadId}: receive str \"{str}\"");
                    //}
                    if (CheckReceiveTerminationRequired(str, answer.ToString())) isNumberReceived = true;

                } while (!isNumberReceived && tcpSocket.Connected && availableIdleCycles > 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (tcpSocket.Connected)
            {
                tcpSocket.Shutdown(SocketShutdown.Both);
                tcpSocket.Close();
                //Console.WriteLine($"{Environment.CurrentManagedThreadId}: Socket closed");
            }

            if (!isNumberReceived) return null;

            var strNumber = Regex.Replace(answer.ToString(), @"[^0-9]", "");
            Console.WriteLine($"{Environment.CurrentManagedThreadId}: Find number {strNumber}");
            return int.TryParse(strNumber, out int resultNumber) ? resultNumber : null;
        }
    }
}
