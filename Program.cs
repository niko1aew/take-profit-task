using SocketApp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("___Socket App___");

ConcurrentBag<int> numbers = new();

/// <summary>
/// Gets the median value from an array
/// </summary>
/// <typeparam name="T">The array type</typeparam>
/// <param name="sourceArray">The source array</param>
/// <param name="cloneArray">If it doesn't matter if the source array is sorted, you can pass false to improve performance</param>
/// <returns></returns>
static T GetMedian<T>(T[] sourceArray, bool cloneArray = true) where T : IComparable<T>
{
    //Framework 2.0 version of this method. there is an easier way in F4        
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

    dynamic value1 = sortedArray[mid];
    dynamic value2 = sortedArray[mid - 1];
    return (sortedArray[mid] + value2) * 0.5;
}

if (true)
{
    var lines = File.ReadLines("numbers.txt");

    var nums = lines.Select(x => double.Parse(x)).ToArray();
    var median1 = GetMedian<double>(nums);
}



Console.WriteLine("Press any key to start...");
Console.ReadLine();

var inputNumberRange = Enumerable.Range(1, 2018);

ThreadPool.GetMaxThreads(out int workerThreadsCount, out int completionPortThreadsCount);

var options = new ParallelOptions { MaxDegreeOfParallelism = completionPortThreadsCount / 5 };

Regex rxCropNumber = new(@"[^0-9]",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

Regex rxNumberTail = new(@"\d[^\d]",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

string ip = "88.212.241.115";
int port = 2013;

await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{

int? receivedNumber = null;

    while (receivedNumber is null)
    {
        using (var client = new TcpClient())
        {
            try
            {
                await client.ConnectAsync(ip, port);

                if (client.Connected)
                {
                    var tcpStream = client.GetStream();
                    var sendBytes = Encoding.UTF8.GetBytes($"{inputNumber}\n");
                    await tcpStream.WriteAsync(sendBytes, 0, sendBytes.Length);

                    using (var reader = new StreamReader(tcpStream))
                    {
                        var message = await reader.ReadToEndAsync();
                        var isCompleteMessage = rxNumberTail.Matches(message).Count > 0;

                        if (isCompleteMessage)
                        {
                            
                            var strNumber = rxCropNumber.Replace(message, "");
                            receivedNumber = int.TryParse(strNumber, out int parsedNumber) ? parsedNumber : null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
            }
        }
    }
    numbers.Add(receivedNumber.Value);
    Console.Clear();
    Console.WriteLine($"Found number {numbers.Count()}/2018: " + receivedNumber.ToString());
});

File.WriteAllLines("numbers.txt", numbers.Select(x => x.ToString()));

var doubleNums = numbers.Select(x => (double)x).ToArray();

var median = GetMedian(doubleNums);

Console.WriteLine(median);

Console.WriteLine("Press any key to quit");
Console.ReadLine();