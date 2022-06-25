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

if (false)
{
    var lines = File.ReadLines("numbers.txt");
    var nums = lines.Select(x => double.Parse(x)).ToArray();
    var median1 = GetMedian<double>(nums);
}

Console.WriteLine("Press any key to start...");
Console.ReadLine();

var inputNumberRange = Enumerable.Range(1, 2018);

ThreadPool.GetMaxThreads(out int workerThreadsCount, out int completionPortThreadsCount);

//var options = new ParallelOptions { MaxDegreeOfParallelism = 100 };
var options = new ParallelOptions { MaxDegreeOfParallelism = 200 };
Regex rxCropNumber = new(@"[^0-9]",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

Regex rxNumberTail = new(@"\d[^\d]",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);
Regex rxLF = new(@".*[\n]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);
string ip = "88.212.241.115";
int port = 2013;

int startTimeout = 0;

long errorCount = 0;
long cyclesCount = 0;
long tasksDispatched = 0;
long receiveTimeouts = 0;
long connectionTimeoutsCount = 0;
var startTime = DateTime.Now;
var timeout = 30000;
await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{
    tasksDispatched++;
    //Thread.Sleep(startTimeout += 5);
    //Thread.Sleep(startTimeout += 5);
    int? receivedNumber = null;

    while (receivedNumber is null)
    {
        cyclesCount++;
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(ip, port);

            if (client.Connected)
            {
                var tcpStream = client.GetStream();
                ReadOnlyMemory<byte> sendBytes = Encoding.UTF8.GetBytes($"{inputNumber}\n");
                await tcpStream.WriteAsync(sendBytes);
                //var writeTask = tcpStream.WriteAsync(sendBytes);
                //if (await Task.WhenAny(writeTask, Task.Delay(timeout)) == writeTask)
                //{
                //    await writeTask;
                //}
                //else
                //{
                //    Console.WriteLine("Timeout");
                //}
                //Thread.Sleep(timeout);

                using var reader = new StreamReader(tcpStream);
                string? message = "";
                //message = await reader.ReadLineAsync();
                var isTimeout = false;

                int retryReadCounter = 15;
                while (retryReadCounter > 0 && !isTimeout && (string.IsNullOrEmpty(message) || rxNumberTail.Matches(message).Count == 0))
                {
                    retryReadCounter--;
                    //Console.Write($" {retryReadCounter} ");
                    var task = reader.ReadLineAsync();
                    //var task = reader.ReadToEndAsync();

                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    {
                        message = await task;
                    }
                    else
                    {
                        //Console.WriteLine("Timeout");
                        isTimeout = true;
                        connectionTimeoutsCount++;
                    }
                    //var test1 = string.IsNullOrEmpty(message);
                    //var test2 = rxNumberTail.Matches(message).Count == 0;
                    //Console.WriteLine($"{test1} {test2}");
                    if (string.IsNullOrEmpty(message) || rxNumberTail.Matches(message).Count == 0)
                    {
                        await Task.Delay(1000);
                    }
                }
                if (!string.IsNullOrEmpty(message) && rxNumberTail.Matches(message).Count > 0)
                {
                    var strNumber = rxCropNumber.Replace(message, "");
                    receivedNumber = int.TryParse(strNumber, out int parsedNumber) ? parsedNumber : null;
                }
                else
                {
                    if (!isTimeout)
                    {
                        //Console.Write($" \"{message}\" ");
                        receiveTimeouts++;

                        await Task.Delay(1000);
                    }
                    //Thread.Sleep(1000);
                }
            }
        }
        catch (Exception e)
        {
            errorCount++;
            Debug.Print(e.Message);
            Thread.Sleep(5000);
        }
    }
    numbers.Add(receivedNumber.Value);
    var currentTime = DateTime.Now - startTime;
    if (numbers.Count() % 10 == 0)
    {
        Console.Clear();
        Console.WriteLine($"Number {numbers.Count()}/2018: " + receivedNumber.ToString() + "\r\n" + "Errors: " + errorCount.ToString() + "\r\n" + "Cycles: " + cyclesCount.ToString() + "\r\n" + "Time: " + currentTime.ToString("c") + "\r\n" + "Tasks Dispatched: " + tasksDispatched.ToString() + "\r\n" + "Receive timeouts: " + receiveTimeouts.ToString());
        Console.WriteLine("Connection timeouts: " + connectionTimeoutsCount.ToString());
    }
});

File.WriteAllLines("numbers.txt", numbers.Select(x => x.ToString()));

var doubleNums = numbers.Select(x => (double)x).ToArray();

var median = GetMedian(doubleNums);

Console.WriteLine(median);

Console.WriteLine("Press any key to quit");
Console.ReadLine();