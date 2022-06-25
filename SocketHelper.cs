using System.Text;
using System.Text.RegularExpressions;

namespace SocketApp
{
    internal static class SocketHelper
    {
        static Regex rxCropNumber = new(@"[^0-9]",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static Regex rxNumberTail = new(@"\d[^\d]",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the median value from an array
        /// </summary>
        /// <typeparam name="T">The array type</typeparam>
        /// <param name="sourceArray">The source array</param>
        /// <param name="cloneArray">If it doesn't matter if the source array is sorted, you can pass false to improve performance</param>
        /// <returns></returns>
        internal static T GetMedian<T>(T[] sourceArray, bool cloneArray = true) where T : IComparable<T>
        {      
            if (sourceArray == null || sourceArray.Length == 0)
                throw new ArgumentException("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            T[] sortedArray = cloneArray ? (T[])sourceArray.Clone() : sourceArray;
            Array.Sort(sortedArray);

            //get the median
            int size = sortedArray.Length;
            int mid = size / 2;
            if (size % 2 != 0)
                return sortedArray[mid];

            dynamic value2 = sortedArray[mid - 1];
            return (sortedArray[mid] + value2) * 0.5;
        }

        internal static int? ParseNumber(string? source)
        {
            int? result = null;
            if (!string.IsNullOrEmpty(source) && rxNumberTail.Matches(source).Count > 0)
            {
                var strNumber = rxCropNumber.Replace(source, "");
                result = int.TryParse(strNumber, out int parsedNumber) ? parsedNumber : null;
            }
            return result;
        }

        internal static bool CheckNumberIsReceived(string source)
        {
            return rxNumberTail.Matches(source).Count > 0;
        }

        internal static ReadOnlyMemory<byte> EncodeString(string source)
        {
            return Encoding.UTF8.GetBytes($"{source}\n");
        }
    }
}
