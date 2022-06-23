using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocketApp
{
    internal static class SocketHelper
    {
        private static Regex rxLF = new Regex(@".*[\n]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Regex rxNumberTail = new Regex(@"\d[^\d]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static Socket GetSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 4000;
            return socket;
        }

        internal static IPEndPoint GetEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse("88.212.241.115"), 2013);
        }

        internal static bool CheckReceiveTerminationRequired(string lastMsg, string completeMsg)
        {
            if (rxLF.Matches(lastMsg).Count > 0) return true;
            if (rxNumberTail.Matches(completeMsg).Count > 0) return true;
            return false;
        }
    }
}
