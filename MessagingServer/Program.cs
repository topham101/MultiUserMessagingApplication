using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MessagingServer
{
    // THINGS TO DO:
    // login
    // acount details & picture
    // chat customisation?
    // proper server controls
    // change MessageCodeTable to read from text file
    // send messages to people who are offline
    // attempt fix of protocol code file
    // add to github
    class Program
    {
        static void Main(string[] args)
        {
            if (!MessageCodes.PopulateDictionary())
            {
                Console.WriteLine("ProtocolCodes.txt is INVALID.");
                Console.WriteLine("Please fix and start again.");
                Environment.Exit(0);
            }
            Console.WriteLine("Protocol Codes Read");
            Thread t = new Thread(() => RunServer(25566));
            t.Start();
            Console.WriteLine("Press Q at anytime to quit.");
            while (Console.ReadKey().Key != ConsoleKey.Q) { }
            Environment.Exit(0);
        }

        private static void RunServer(int port)
        {
            Console.WriteLine("Launching Server . . .");
            TcpListener listener;
            Socket connection;
            Handler connectionHandler;
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                while (true)
                {
                    connection = listener.AcceptSocket();
                    connectionHandler = new Handler();
                    Thread t = new Thread(
                        () => connectionHandler.beginHandle(connection));
                    Console.WriteLine("Connection Found . . .");
                    t.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("**ERROR**" 
                    + Environment.NewLine + e.ToString());
                throw;
            }
        }
    }
}
