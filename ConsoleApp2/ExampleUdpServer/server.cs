using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;

class Server
{
    static void Main()
    {
        Console.WriteLine("SERVER\n=====================");
        UdpClient udpserver = new UdpClient(11000);

        while (true)
        {
            var ep = new IPEndPoint(IPAddress.Any, 11000);
            var data = Encoding.ASCII.GetString( udpserver.Receive(ref ep) ); // Listen on port 11000
            Console.Write("Received data from " + ep.ToString() + "\n");
            Console.Write("data: " + data +"\n\n");
            // Replying back to clients
            var datagram = Encoding.ASCII.GetBytes("Server has received message: " + data + "\n");
            if (data == "quit")
                datagram = Encoding.ASCII.GetBytes("quit");
            udpserver.Send(datagram, datagram.Length, ep);
        }
    }
}