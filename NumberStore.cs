using System.Collections.Concurrent;
using System.Text;

namespace SocketApp
{
    internal class NumberStore
    {
        private readonly ConcurrentBag<int> numbers = new();
        private readonly int _numbersCount;
        private readonly DateTime _createTime;
        private static readonly Object lockObj = new();

        public NumberStore(int numbersCount)
        {
            _numbersCount = numbersCount;
            _createTime = DateTime.Now;
        }
        
        /// <summary>
        /// Add number to collection
        /// </summary>
        internal void AddNumber(int number)
        {
            numbers.Add(number);
            lock(lockObj)
            {
                var timeDelta = DateTime.Now - _createTime;

                Console.SetCursorPosition(0, 0);
                var infoBuilder = new StringBuilder();
                infoBuilder.AppendLine($"Number {numbers.Count}/{_numbersCount}: {number}");
                infoBuilder.AppendLine($"Time: {timeDelta.ToString("c")}");
                Console.WriteLine(infoBuilder);
            }
        }

        /// <summary>
        /// Check number is in (0 <= x< 1e7) range
        /// </summary>
        internal static bool CheckNumberIsInValidRange(int number)
        {
            return number >= 0 && number < 10000000;
        }

        /// <summary>
        /// Gets the median value of numbers
        /// </summary>
        internal double GetMedian()
        {
            var sourceArray = numbers.ToArray();
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
    }
}
