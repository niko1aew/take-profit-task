using System.Text;
using System.Text.RegularExpressions;

namespace SocketApp
{
    internal static class Helper
    {
        static readonly Regex rxCropNumber = new(@"[^0-9]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex rxNumberTail = new(@"\d[^\d]",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the median value from an array
        /// </summary>
        internal static double GetMedian(int[] sourceArray)
        {
            if (sourceArray == null || sourceArray.Length == 0)
                throw new ArgumentException("Median of empty array not defined.");

            // Make sure the list is sorted
            Array.Sort(sourceArray);

            // Get the median
            int size = sourceArray.Length;
            int mid = size / 2;
            if (size % 2 != 0)
                return sourceArray[mid];

            dynamic value2 = sourceArray[mid - 1];
            return (sourceArray[mid] + value2) * 0.5;
        }

        /// <summary>
        /// Get result number from source string
        /// </summary>
        internal static int? ParseNumber(string? source)
        {
            int? result = null;
            if (!string.IsNullOrEmpty(source) && CheckNumberIsReceived(source))
            {
                var strNumber = rxCropNumber.Replace(source, "");
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
            bool value = false;
            bool valid = false;
            do
            {
                Console.WriteLine(prompt);
                var inputString = Console.ReadLine();
                if (string.IsNullOrEmpty(inputString))
                {
                    continue;
                }
                if (string.Equals(inputString, "yes"))
                {
                    value = true;
                    valid = true;
                }
                else if (string.Equals(inputString, "no"))
                {
                    value = false;
                    valid = true;
                }

            } while (!valid);

            return value;
        }
    }
}
