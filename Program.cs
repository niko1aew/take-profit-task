// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");

//try
//{
//    var listen = new TcpListener(IPAddress.Parse("88.212.241.115"), 2013);
//    listen.Start();
//    Byte[] bytes;
//    while (true)
//    {
//        TcpClient client = listen.AcceptTcpClient();
//        NetworkStream ns = client.GetStream();
//        if (client.ReceiveBufferSize > 0)
//        {
//            bytes = new byte[client.ReceiveBufferSize];
//            ns.Read(bytes, 0, client.ReceiveBufferSize);
//            string msg = Encoding.UTF8.GetString(bytes); //the message incoming
//            Console.Write(msg);
//        }
//    }
//}
//catch (Exception e)
//{
//    Console.WriteLine(e.Message);
//}










var tcpEndpoint = new IPEndPoint(IPAddress.Parse("88.212.241.115"), 2013);

var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var message = "111\n";
var data = Encoding.UTF8.GetBytes(message);
//try
//{
    tcpSocket.Connect(tcpEndpoint);
    tcpSocket.Send(data);

    var buffer = new byte[256];
    var size = 0;
    var answer = new StringBuilder();

    string str;

    var rxNumberTail = new Regex(@"\d[^\d]",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

    var rxFullNumber = new Regex(@"\d+",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

    var isNumberReceived = false;
    do
    {
        size = tcpSocket.Receive(buffer);
        str = Encoding.UTF8.GetString(buffer, 0, size);
        Console.Write(str);

        answer.Append(str);
        var matches = rxNumberTail.Matches(answer.ToString());
        if (matches.Count > 0) isNumberReceived = true;
    } while (!isNumberReceived);
//} catch(Exception e)
//{
//    Console.WriteLine(e.Message);
//}
Console.WriteLine("Answer: " + answer.ToString());
var source = Regex.Replace(answer.ToString(), @"[^0-9]", "");
Console.WriteLine("Source: " + source);
MatchCollection matches1 = rxFullNumber.Matches(answer.ToString());
Console.WriteLine("Cropped: " + matches1[0].Value.ToString());
tcpSocket.Shutdown(SocketShutdown.Both);
tcpSocket.Close();
Console.WriteLine("Socket closed");
Console.ReadLine();