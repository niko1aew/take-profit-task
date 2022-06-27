using SocketApp;
using System.Diagnostics;
using System.Net.Sockets;

const int startNumber = 1;
const int endNumber = 2018;
const int connectionTimeout = 30000;
const int errorCooldownTime = 5000;
const int retryCooldownTime = 1000;
const int retriesReadCount = 15;

var useToken = Helper.GetBoolInputValue("Use token? yes|no (default - yes): ");
var ip = Helper.GetIpInputValue();
var port = Helper.GetPortInputValue();

Helper.ShowConfig(ip, port, useToken);

Console.WriteLine("Press any key to start...");
Console.ReadLine();

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
Console.Clear();
Console.WriteLine("Wait...");

var numberStore = new NumberStore(endNumber);

var inputNumberRange = Enumerable.Range(startNumber, endNumber);

ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);

var options = new ParallelOptions { MaxDegreeOfParallelism = maxIoThreads / 3 };

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

                var remainingAttepts = retriesReadCount;
                var numberIsReceived = false;
                var numberIsInValidRange = false;
                int? parsedNumber = null;

                while (remainingAttepts > 0
                    && !isReceiveTimeout
                    && !numberIsReceived
                    && client.Connected)
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
                    if (numberIsReceived)
                    {
                        parsedNumber = Helper.ParseNumber(message);
                        numberIsInValidRange = parsedNumber.HasValue
                            && NumberStore.CheckNumberIsInValidRange(parsedNumber.Value);
                    }
                    if (!numberIsReceived || !numberIsInValidRange)
                    {
                        await Task.Delay(retryCooldownTime, token);
                    }
                }

                receivedNumber = parsedNumber;
            }
        }
        catch (Exception e)
        {
            Debug.Print(e.Message);            
            await Task.Delay(errorCooldownTime, token);
        }
    }
    numberStore.AddNumber(receivedNumber.Value);
});

if (useToken)
{
    KeyStore.StopRefreshing();
}

Console.SetCursorPosition(0, 3);

Console.WriteLine($"Median: {numberStore.GetMedian()}");

Console.WriteLine("Press any key to quit");
Console.ReadLine();