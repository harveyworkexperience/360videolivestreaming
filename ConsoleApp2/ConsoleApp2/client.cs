using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

class Client
{
    // A function for listening to server responses
    static void ReplyThread(UdpClient udpclient, IPEndPoint ep)
    {
        while (true)
        {
            try
            {
                var data = Encoding.ASCII.GetString(udpclient.Receive(ref ep));
                Console.WriteLine(data);
                if (string.Equals(data.ToString(),"quit".ToString()))
                    break;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }
        Console.WriteLine("Thread closed");
    }

    // Starts the thread and provides the threaded function parameters
    static Thread StartTheThread(UdpClient arg1, IPEndPoint arg2)
    {
        var t = new Thread(() => ReplyThread(arg1, arg2));
        t.Start();
        return t;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("CLIENT\n=====================");

        var udpclient = new UdpClient();
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening
        udpclient.Connect(ep);

        // Listening to replies on another thread
        Thread thread1 = StartTheThread(udpclient, ep);

        // Message the server
        string read = "";
        while (read != "quit")
        {
            read = Console.ReadLine();
            var datagram = Encoding.ASCII.GetBytes(read);
            udpclient.Send(datagram, datagram.Length);
        }
        Console.WriteLine("Closing client");
        return;
    }
}
