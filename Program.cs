using SocketApp;
using System.Diagnostics;
using System.Net.Sockets;

const int startNumber = 1;
const int endNumber = 2018;
const int connectionTimeout = 30000;
const int errorCooldownTime = 5000;
const int retryCooldownTime = 1000;

//var useToken = Helper.GetBoolInputValue("Use token? yes|no (default - yes): ");
//var ip = Helper.GetIpInputValue();
//var port = Helper.GetPortInputValue();

var useToken = true;
//var ip = "127.0.0.1";
var ip = "88.212.241.115";
//var port = 8888;
var port = 2013;

Helper.ShowConfig(ip, port, useToken);

ThreadPool.GetMaxThreads(out int w, out int e);
Console.WriteLine(e);
Console.WriteLine("Press any key to start...");
Console.ReadLine();

if (useToken)
{
    Console.WriteLine("Receiving token...");
    KeyStore.StartRefreshing(ip, port);
    KeyStore.RefreshKey("");
    while (KeyStore.IsKeyExpired)
    {
        await Task.Delay(200);
    }
    //Console.WriteLine(await KeyStore.RefreshKey());
    Console.WriteLine(KeyStore.TokenString);
    Console.ReadLine();

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

var options = new ParallelOptions { MaxDegreeOfParallelism = maxIoThreads / 10 };

await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{
    int? receivedNumber = null;

    Guid guid = Guid.NewGuid();

    while (receivedNumber is null)
    {

        //while (KeyStore.IsKeyExpired)
        //{
        //    //Console.WriteLine("In while loop");
        //    await Task.Delay(200);
        //}

        //}
        var tokenString = KeyStore.TokenString;
        var sendString = useToken
            ? $"{tokenString}|{inputNumber}"
            : inputNumber.ToString();
        var sendBytes = Helper.EncodeString(sendString);

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

                var numberIsReceived = false;
                var numberIsInValidRange = false;
                int? parsedNumber = null;


                var readLineTask = reader.ReadLineAsync();

                // Performing read operation with timeout
                if (await Task.WhenAny(readLineTask, Task.Delay(connectionTimeout, token))
                    == readLineTask)
                {
                    message = await readLineTask;
                    Console.WriteLine($"{guid}: {message}");
                    if (useToken && message == "Key has expired")
                    {
                        await Task.Delay(1000);
                        //Console.WriteLine($"<{Thread.CurrentThread.ManagedThreadId}> Key expired");

                        KeyStore.RefreshKey(tokenString);


                        //continue;
                        //Console.WriteLine($"{message} <{Thread.CurrentThread.ManagedThreadId}>");
                    }

                    if (!KeyStore.IsKeyExpired)
                    {
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


                        receivedNumber = parsedNumber;
                    }

                }
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