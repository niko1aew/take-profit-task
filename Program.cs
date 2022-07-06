using SocketApp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

const int startNumber = 1;
const int endNumber = 2018;
const int connectionTimeout = 30000;
const int errorCooldownTime = 5000;
const int keyExpiredCooldownTime = 100;
const int waitDelay = 1000;

var useToken = Helper.GetBoolInputValue("Use token? yes|no (default - yes): ");
var ip = Helper.GetIpInputValue();
var port = Helper.GetPortInputValue();

Helper.ShowConfig(ip, port, useToken);

Console.WriteLine("Press any key to start...");
Console.ReadLine();

if (useToken)
{
    Console.WriteLine("Receiving token...");
    KeyStore.Init(ip, port);
    KeyStore.RefreshKey("");

    while (string.IsNullOrEmpty(KeyStore.TokenString))
    {
        Console.Write(".");
        await Task.Delay(waitDelay);
    }

    Console.WriteLine($"Received token: {KeyStore.TokenString}." +
        $" Will start at 1 second...");
    await Task.Delay(waitDelay);
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

    while (receivedNumber is null)
    {
        if (useToken && KeyStore.IsKeyExpired)
        {
            await Task.Delay(keyExpiredCooldownTime);
            continue;
        }

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

                var messageBuilder = new StringBuilder();
                var messagePart = string.Empty;
                Memory<char> buffer = new char[1];

                var readCharsCount = 0;
                do
                {
                    var result = reader.ReadAsync(buffer);

                    await Task.WhenAny(result.AsTask(), Task.Delay(connectionTimeout));

                    if (!result.IsCompleted)
                    {
                        throw new Exception("Read timeout");
                    }
                    else
                    {
                        readCharsCount = await result;
                        if (readCharsCount > 0)
                        {
                            messagePart = buffer.ToString();

                            if (!string.IsNullOrEmpty(messagePart))
                            {
                                messageBuilder.Append(messagePart);
                            }
                            message = messageBuilder.ToString();

                            if (useToken && message == "Key has expired")
                            {
                                KeyStore.RefreshKey(tokenString);
                                break;
                            }

                            numberIsReceived = Helper.CheckNumberIsReceived(message);

                            if (numberIsReceived)
                            {
                                parsedNumber = Helper.ParseNumber(message);
                                numberIsInValidRange = parsedNumber.HasValue
                                    && NumberStore.CheckNumberIsInValidRange(parsedNumber.Value);
                            }

                            if (numberIsReceived && numberIsInValidRange)
                            {
                                receivedNumber = parsedNumber;
                            }
                        }
                    }
                } while (readCharsCount > 0 && receivedNumber is null);
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

Console.SetCursorPosition(0, 3);

Console.WriteLine($"Median: {numberStore.GetMedian()}");

Console.WriteLine("Press any key to quit");
Console.ReadLine();