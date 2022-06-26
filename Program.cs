using SocketApp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("___Socket App___");

ConcurrentBag<int> numbers = new();

const string ip = "88.212.241.115";
const int port = 2013;
const int startNumber = 1;
const int endNumber = 2018;
const int maxDegreeOfParallelism = 200;
const int connectionTimeout = 30000;
const int errorCooldownTime = 5000;
const int retryCooldownTime = 1000;
const int retriesReadCount = 15;

Console.WriteLine("Press any key to start...");
Console.ReadLine();

var useToken = Helper.GetBoolInputValue("Use token? (yes|no): ");

Console.Clear();

var startTime = DateTime.Now;

if (useToken)
{
    Console.WriteLine("Receiving token...");
    KeyStore.StartRefreshing(ip, port);

    while (string.IsNullOrEmpty(KeyStore.TokenString))
    {
        Console.Write(".");
        await Task.Delay(1000);
    }
}

Console.WriteLine("Wait...");

var inputNumberRange = Enumerable.Range(startNumber, endNumber);

var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{
    int? receivedNumber = null;

    while (receivedNumber is null)
    {
        var sendBytes = Helper.EncodeString(useToken
            ? $"{KeyStore.TokenString}|{inputNumber}"
            : inputNumber.ToString());

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(ip, port);

            if (client.Connected)
            {
                var tcpStream = client.GetStream();

                await tcpStream.WriteAsync(sendBytes, token);

                using var reader = new StreamReader(tcpStream);
                string? message = string.Empty;
                var isReceiveTimeout = false;

                int remainingAttepts = retriesReadCount;
                bool numberIsReceived = false;
                while (remainingAttepts > 0
                    && !isReceiveTimeout
                    && !numberIsReceived)
                {
                    remainingAttepts--;
                    var readLineTask = reader.ReadLineAsync();

                    // Performing read operation with timeout
                    if (await Task.WhenAny(readLineTask, Task.Delay(connectionTimeout, token))
                        == readLineTask)
                    {
                        message = await readLineTask;
                    }
                    else
                    {
                        isReceiveTimeout = true;
                    }

                    numberIsReceived = Helper.CheckNumberIsReceived(message);
                    if (!numberIsReceived)
                    {
                        await Task.Delay(retryCooldownTime, token);
                    }
                }

                receivedNumber = Helper.ParseNumber(message);
            }
        }
        catch (Exception e)
        {
            Debug.Print(e.Message);
            await Task.Delay(errorCooldownTime, token);
        }
    }

    numbers.Add(receivedNumber.Value);

    var timeDelta = DateTime.Now - startTime;
    if (numbers.Count % 10 == 0 || numbers.Count == endNumber)
    {
        Console.Clear();
        var infoBuilder = new StringBuilder();
        infoBuilder.AppendLine($"Number {numbers.Count}/{endNumber}: {receivedNumber}");
        infoBuilder.AppendLine($"Time: {timeDelta.ToString("c")}");
        Console.WriteLine(infoBuilder);
    }
});

if (useToken)
{
    KeyStore.StopRefreshing();
}

var median = Helper.GetMedian(numbers.ToArray());

Console.WriteLine($"Median: {median}");

Console.WriteLine("Press any key to quit");
Console.ReadLine();