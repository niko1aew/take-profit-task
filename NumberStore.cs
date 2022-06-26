using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketApp
{
    internal class NumberStore
    {
        private ConcurrentBag<int> numbers = new();
        private int _numbersCount;
        private DateTime _createTime;

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
            var timeDelta = DateTime.Now - _createTime;
            numbers.Add(number);
            Console.Clear();
            var infoBuilder = new StringBuilder();
            infoBuilder.AppendLine($"Number {numbers.Count}/{_numbersCount}: {number}");
            infoBuilder.AppendLine($"Time: {timeDelta.ToString("c")}");
            Console.WriteLine(infoBuilder);
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
