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
const int readRetriesCount = 15;
const int tokenRefreshInterval = 20000;


Console.WriteLine("Press any key to start...");

Key keyStore = new() { RefreshTimeout = tokenRefreshInterval };

static async void TaskCallback(Object? obj)
{
    if (obj is not Key tokenObj) return;

    while (tokenObj.refreshRequired)
    {
        try
        {
            // TODO: error handling | timeout handling
            using var client = new TcpClient();

            await client.ConnectAsync(ip, port);
            var sendBytes = SocketHelper.EncodeString("Register");
            var tcpStream = client.GetStream();

            await tcpStream.WriteAsync(sendBytes);

            using var reader = new StreamReader(tcpStream);
            var receivedToken = await reader.ReadLineAsync();

            if (!string.IsNullOrWhiteSpace(receivedToken))
            {
                tokenObj.TokenString = receivedToken;
            }

            Console.WriteLine("Received token: " + receivedToken);
        } catch (Exception e)
        {
            Console.WriteLine("Token update error: " + e.Message);
            await Task.Delay(10000);
        }
        
        await Task.Delay(tokenObj.RefreshTimeout);
    }
}



Console.ReadLine();

Console.Clear();

Console.WriteLine("Receiving token...");
ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallback), keyStore);

while (string.IsNullOrEmpty(keyStore.TokenString))
{
    Console.Write(".");
    await Task.Delay(1000);
}

Console.WriteLine("Wait...");

var inputNumberRange = Enumerable.Range(startNumber, endNumber);

var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

var errorCount = 0;
var tasksDispatchedCount = 0;
var reconnectRequestsCount = 0;
var connectionTimeoutsCount = 0;

var startTime = DateTime.Now;

await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{
    tasksDispatchedCount++;
    int? receivedNumber = null;
    

    while (receivedNumber is null)
    {
        var sendBytes = SocketHelper.EncodeString(string.IsNullOrEmpty(keyStore.TokenString) ? inputNumber.ToString() : $"{keyStore.TokenString}|{inputNumber}");
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
                var isTimeout = false;

                int retryReadCounter = readRetriesCount;
                while (retryReadCounter > 0
                    && !isTimeout
                    && (string.IsNullOrEmpty(message)
                        || !SocketHelper.CheckNumberIsReceived(message)))
                {
                    retryReadCounter--;
                    var task = reader.ReadLineAsync();

                    if (await Task.WhenAny(task, Task.Delay(connectionTimeout, token)) == task)
                    {
                        message = await task;
                    }
                    else
                    {
                        isTimeout = true;
                        connectionTimeoutsCount++;
                    }
                    if (string.IsNullOrEmpty(message) || !SocketHelper.CheckNumberIsReceived(message))
                    {
                        await Task.Delay(retryCooldownTime, token);
                    }
                }

                receivedNumber = SocketHelper.ParseNumber(message);

                if (receivedNumber is null)
                {
                    if (!isTimeout)
                    {
                        reconnectRequestsCount++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            errorCount++;
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
        infoBuilder.AppendLine($"Errors: {errorCount}");
        infoBuilder.AppendLine($"Time: {timeDelta.ToString("c")}");
        infoBuilder.AppendLine($"Tasks Dispatched: {tasksDispatchedCount}");
        infoBuilder.AppendLine($"Reconnect requests: {reconnectRequestsCount}");
        infoBuilder.AppendLine($"Connection timeouts: {connectionTimeoutsCount}");
        Console.WriteLine(infoBuilder);
    }
});

keyStore.refreshRequired = false;

var doubleNums = numbers.Select(x => (double)x).ToArray();

var median = SocketHelper.GetMedian(doubleNums);

Console.WriteLine($"Median: {median}");

Console.WriteLine("Press any key to quit");
Console.ReadLine();