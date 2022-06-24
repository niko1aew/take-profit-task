// See https://aka.ms/new-console-template for more information
using SocketApp;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("___Socket App___");

ConcurrentBag<int> numbers = new();

static int? GetNumber(int inputNumber)
{
    Console.WriteLine("Running in thread: " + Environment.CurrentManagedThreadId);
    var tcpEndpoint = SocketHelper.GetEndpoint();
    var tcpSocket = SocketHelper.GetSocket();

    var message = $"{inputNumber}\n";
    var data = Encoding.UTF8.GetBytes(message);
    var buffer = new byte[256];
    var answer = new StringBuilder();

    var availableIdleCycles = 30;

    string str;

    var isNumberReceived = false;

    try
    {
        tcpSocket.Connect(tcpEndpoint);
        Console.WriteLine($"{Environment.CurrentManagedThreadId}: Socket {tcpSocket.Connected}");
        Console.WriteLine($"{Environment.CurrentManagedThreadId}: Sending {message}");
        var bytesSent = tcpSocket.Send(data);
        Console.WriteLine($"{Environment.CurrentManagedThreadId}: sent {bytesSent} bytes");
        do
        {
            if (tcpSocket.Available == 0)
            {
                availableIdleCycles--;
                Thread.Sleep(100);
            }

            var size = tcpSocket.Receive(buffer);
            str = Encoding.UTF8.GetString(buffer, 0, size);
            answer.Append(str);
            if (!string.IsNullOrWhiteSpace(str))
            {
                Console.WriteLine($"{Environment.CurrentManagedThreadId}: receive str \"{str}\"");
            }
            if (SocketHelper.CheckReceiveTerminationRequired(str, answer.ToString())) isNumberReceived = true;
            
        } while (!isNumberReceived && tcpSocket.Connected && availableIdleCycles > 0);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }

    if (tcpSocket.Connected)
    {
        tcpSocket.Shutdown(SocketShutdown.Both);
        tcpSocket.Close();
        Console.WriteLine($"{Environment.CurrentManagedThreadId}: Socket closed");
    }

    if (!isNumberReceived) return null;

    var strNumber = Regex.Replace(answer.ToString(), @"[^0-9]", "");
    Console.WriteLine($"{Environment.CurrentManagedThreadId}: Find number {strNumber}");
    return Int32.TryParse(strNumber, out int resultNumber) ? resultNumber : null;
}

var inputNumberRange = Enumerable.Range(1, 100);

Parallel.ForEach(inputNumberRange, inputNumber =>
{
    int? number = null;

    while (number is null)
    {
        number = GetNumber(inputNumber);
        if (number is null)
        {
            // Restart socket
            Console.WriteLine("Number is not received. Wait 5sec to retry...");
            Thread.Sleep(5000);
        }
        else
        {
            //Console.WriteLine(number);
            numbers.Add(number.Value);
        }
    }
});


foreach(var number in numbers)
{
    Console.WriteLine(number);
}

Console.WriteLine("Press any key to quit");
Console.ReadLine();