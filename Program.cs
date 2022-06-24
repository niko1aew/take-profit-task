using SocketApp;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("___Socket App___");

ConcurrentBag<int> numbers = new();



//var test = "  fg  .. 854694596 ... ,,";

//var strNumber = rxCropNumber.Replace(test, "");




Console.ReadLine();

var inputNumberRange = Enumerable.Range(1, 100);
var options = new ParallelOptions { MaxDegreeOfParallelism = inputNumberRange.Count() };
await Parallel.ForEachAsync(inputNumberRange, options, async (inputNumber, token) =>
{
    Console.WriteLine($"Running in {Environment.CurrentManagedThreadId}");

    Regex rxCropNumber = new(@"[^0-9]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

    Regex rxLF = new(@".*[\n]",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);
    int? receivedNumber = null;

    while (receivedNumber is null)
    {
        using (var client = new TcpClient())
        {
            try
            {
                await client.ConnectAsync("88.212.241.115", 2013);


                var tcpStream = client.GetStream();
                var sendBytes = Encoding.UTF8.GetBytes($"{inputNumber}\n");
                await tcpStream.WriteAsync(sendBytes, 0, sendBytes.Length);

                using (var reader = new StreamReader(tcpStream))
                {
                    var message = await reader.ReadToEndAsync();
                    var isCompleteMessage = rxLF.Matches(message).Count > 0;

                    if (!isCompleteMessage)
                    {
                        Console.WriteLine("MessageBroken");
                    }

                    if (isCompleteMessage)
                    {
                        var strNumber = rxCropNumber.Replace(message, "");
                        receivedNumber = int.TryParse(strNumber, out int parsedNumber) ? parsedNumber : null;
                    }

                    if (receivedNumber is null)
                    {
                        Console.WriteLine("Number still null");
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }
    numbers.Add(receivedNumber.Value);
    Console.WriteLine($"{Environment.CurrentManagedThreadId} found number: " + receivedNumber.ToString());
});

foreach (var number in numbers)
{
    Console.WriteLine(number);
}

Console.WriteLine("Press any key to quit");
Console.ReadLine();







//var inputNumberRange = Enumerable.Range(1, 100);

//Parallel.ForEach(inputNumberRange, new ParallelOptions { MaxDegreeOfParallelism = 100 }, inputNumber =>
//{
//    Console.WriteLine("Running in thread: " + Environment.CurrentManagedThreadId);
//    int? number = null;

//    while (number is null)
//    {
//        number = SocketHelper.GetNumber(inputNumber);
//        if (number is null)
//        {
//            Console.WriteLine($"{Environment.CurrentManagedThreadId}: Number is not received.Wait 5sec to retry...");
//            Thread.Sleep(5000);
//        }
//        else
//        {
//            numbers.Add(number.Value);
//        }
//    }
//});

//foreach (var number in numbers)
//{
//    Console.WriteLine(number);
//}

//Console.WriteLine("Press any key to quit");
//Console.ReadLine();