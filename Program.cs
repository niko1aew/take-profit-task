// See https://aka.ms/new-console-template for more information
using SocketApp;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("___Socket App___");

static int? GetNumber(int inputNumber)
{
    var tcpEndpoint = SocketHelper.GetEndpoint();
    var tcpSocket = SocketHelper.GetSocket();

    var message = $"{inputNumber}\n";
    var data = Encoding.UTF8.GetBytes(message);
    var buffer = new byte[256];
    var answer = new StringBuilder();
    
    string str;

    var isNumberReceived = false;

    try
    {
        tcpSocket.Connect(tcpEndpoint);
        tcpSocket.Send(data);

        do
        {
            var size = tcpSocket.Receive(buffer);
            str = Encoding.UTF8.GetString(buffer, 0, size);
            Console.Write(str);

            answer.Append(str);
            if (SocketHelper.CheckReceiveTerminationRequired(str, answer.ToString())) isNumberReceived = true;
        } while (!isNumberReceived && tcpSocket.Connected);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }

    if (tcpSocket.Connected)
    {
        tcpSocket.Shutdown(SocketShutdown.Both);
        tcpSocket.Close();
        Console.WriteLine("Socket closed");
    }

    if (!isNumberReceived) return null;

    var strNumber = Regex.Replace(answer.ToString(), @"[^0-9]", "");
    return Int32.TryParse(strNumber, out int resultNumber) ? resultNumber : null;
}

int? number = null;

while (number is null)
{
    number = GetNumber(111);
    if (number is null)
    {
        // Restart socket
        Console.WriteLine("Number is not received. Wait 5sec to retry...");
        Thread.Sleep(5000);
    }
    else
    {
        Console.WriteLine(number);
    }
}

Console.WriteLine("Press any key to quit");
Console.ReadLine();