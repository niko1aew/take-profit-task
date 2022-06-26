using System.Text;
using System.Text.RegularExpressions;

namespace SocketApp
{
    internal static class Helper
    {
        const string defaultIp = "88.212.241.115";
        const string defaultPort = "2013";
        
        static readonly Regex rxFindNumber = new(@"[^0-9]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex rxNumberTail = new(@"\d[^\d]",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex rxIP = new(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)(\.(?!$)|$)){4}$",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex rxPort = new(@"^([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);


        /// <summary>
        /// Get result number from source string
        /// </summary>
        internal static int? ParseNumber(string? source)
        {
            int? result = null;
            if (!string.IsNullOrEmpty(source) && CheckNumberIsReceived(source))
            {
                var strNumber = rxFindNumber.Replace(source, "");
                result = int.TryParse(strNumber, out int parsedNumber) ? parsedNumber : null;
            }
            return result;
        }

        /// <summary>
        /// Check if string contains result number
        /// </summary>
        internal static bool CheckNumberIsReceived(string? source)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            return rxNumberTail.Matches(source).Count > 0;
        }


        /// <summary>
        /// Encode string to appropriate payload format
        /// </summary>
        internal static ReadOnlyMemory<byte> EncodeString(string source) =>
            Encoding.UTF8.GetBytes($"{source}\n");


        /// <summary>
        /// Get boolean value from user input
        /// </summary>
        internal static bool GetBoolInputValue(string prompt)
        {
            Console.Clear();
            do
            {
                Console.WriteLine(prompt);
                var inputString = Console.ReadLine();
                if (inputString == String.Empty)
                {
                    return false;
                }
                else if (string.IsNullOrEmpty(inputString))
                {
                    continue;
                }
                else if (string.Equals(inputString, "yes"))
                {
                    return true;
                }
                else if (string.Equals(inputString, "no"))
                {
                    return false;
                }

            } while (true);
        }

        /// <summary>
        /// Get server IP value from user input
        /// </summary>
        internal static string GetIpInputValue()
        {
            Console.Clear();
            do
            {
                Console.WriteLine("Enter server IP. Press enter to use default: ");
                var inputString = Console.ReadLine();
                if (inputString == string.Empty)
                {
                    return defaultIp;
                }
                else if (string.IsNullOrWhiteSpace(inputString)
                    || rxIP.Matches(inputString).Count == 0)
                {
                    continue;
                }
                else
                {
                    return inputString;
                }

            } while (true);
        }

        /// <summary>
        /// Get server port value from user input
        /// </summary>
        internal static int GetPortInputValue()
        {
            Console.Clear();
            do
            {
                Console.WriteLine("Enter server port. Press enter to use default: ");
                var inputString = Console.ReadLine();
                if (inputString == string.Empty)
                {
                    return int.Parse(defaultPort);
                }
                else if (string.IsNullOrWhiteSpace(inputString)
                    || rxPort.Matches(inputString).Count == 0)
                {
                    continue;
                }
                else
                {
                    return int.Parse(inputString);
                }

            } while (true);
        }

        /// <summary>
        /// Print formatted configuration to console
        /// </summary>
        internal static void ShowConfig(string ip, int port, bool useToken)
        {
            Console.Clear();

            var configInfoBuilder = new StringBuilder();
            configInfoBuilder.AppendLine($"Config:");
            configInfoBuilder.AppendLine($"IP: {ip}");
            configInfoBuilder.AppendLine($"Port: {port}");
            configInfoBuilder.AppendLine($"UseToken: {useToken}");

            Console.WriteLine(configInfoBuilder);
        }
    }
}
