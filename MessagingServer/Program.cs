using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Concurrent;

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
    // signup user ability
    // Tell users if their message has been sent successfully
    // Error handling and logging

    // CHANGE C019 TO DELETE FRIEND REQUEST not delcline them. Or add a new command for this?

    class Program
    {
        /// <summary>
        /// A Dictionary containing an int Key for each User containing their ID. The Concurrent Queue
        /// represents a queue of messages passed on from other users.
        /// </summary>
        public static ConcurrentDictionary<int, ConcurrentQueue<Message>> PassOnMessageDictionary
            = new ConcurrentDictionary<int, ConcurrentQueue<Message>>(); // Add to this to pass a message
        /// <summary>
        /// A Dictionary of all the Online Users and whether they are appearing online or not
        /// 
        /// int - The ID of the User
        /// 
        /// bool - True if the User is appearing Online, False if they are appearing Offline
        /// </summary>
        public static ConcurrentDictionary<int, bool> UsersAppearingOnlineDict // Check this to see friend status'
            = new ConcurrentDictionary<int, bool>();
        /// <summary>
        /// A Dictionary of all the Online Users and a bool representing whether there are status
        /// updates from friends to be checked or not.
        /// 
        /// True = Friend Online/Offline Status' have changed
        /// False = No Changes
        /// 
        /// </summary>
        public static ConcurrentDictionary<int, bool> OnlineStatusUpdates = new ConcurrentDictionary<int, bool>();
        // Check this to see if friend status' have changed.
        // Add to this for each of your friends if your status has changed
        /// <summary>
        /// List of users and their respective display names
        /// </summary>
        public static ConcurrentDictionary<int, string> displayNameDictionary = new ConcurrentDictionary<int, string>();

        static void Main(string[] args)
        {
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
                    Task task = new Task(() => connectionHandler.beginHandle(connection));
                    task.Start();
                    Console.WriteLine("Connection Found . . .");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("**ERROR**" 
                    + Environment.NewLine + e.ToString());
                throw;
            }
        }

        public static void testFunc()
        {

        }
    }
}
