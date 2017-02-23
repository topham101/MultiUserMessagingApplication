using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Concurrent;

namespace MessagingServer
{
    public class Handler
    {
        public static string GlobalConnectionString = 
            Properties.Settings.Default.MessagingServerConnectionString;

        private const int PollRateMS = 500;
        private StreamReader sr;
        private StreamWriter sw;

        private int connectedUserID;

        public void beginHandle(Socket connection)
        {
            NetworkStream socketStream = new NetworkStream(connection);
            sr = new StreamReader(socketStream);
            sw = new StreamWriter(socketStream);
            string ip = connection.RemoteEndPoint.ToString();


            // Test client for response
            if (connectionWorking())
            {
                Console.WriteLine("Connection Working: " + ip + " USER: "
                    + connectedUserID.ToString("D4"));
                if (Program.USERSdictionary.TryAdd(
                    connectedUserID,new ConcurrentQueue<Message>()))
                {
                    Console.WriteLine("Dictionary element added successfully (ID: "
                        + connectedUserID + ")");
                }


                // Send connection info and data ~ contacts list


                // start socket poll
                socketPoll(connection);
            }

            // Close Thread
            sw.Close();
            sr.Close();
            socketStream.Close();
            connection.Close();
            Console.WriteLine("Connection Closed: " + ip);
        }

        private void socketPoll(Socket connection)
        {
            while (connection.IsConnected())
            {
                // Check for messages
                try
                {
                    if (sr.Peek() >= 0)
                    {
                        string nextMessage = string.Empty;
                        while (readNextMessage(out nextMessage))
                        {
                            messageHandler(Message.InterpretString(nextMessage)); // Handle Messages 
                            nextMessage = string.Empty;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("PEEK ERROR");
                    return;
                    throw;
                }
                // Wait
                Task.Delay(PollRateMS);
            }
            return;
        }

        private void messageHandler(Message recMessage) // amend later
        {
            Console.WriteLine("User " + recMessage.senderID.ToString("D4") + " sent: "
                + Environment.NewLine + recMessage.Code.ToString() + " "
                + recMessage.MessageString);
            //SqlConnection connection = new SqlConnection(Handler.GlobalConnectionString);
            //connection.Open();
            //SqlCommand insert = new SqlCommand(@"");
            //insert.

            switch (recMessage.Code)
            {
                case MessageCode.C003:
                    if (recMessage.receiverID != 0)
                    {
                        ConcurrentQueue<Message> MessageQueue;
                        if (Program.USERSdictionary.TryGetValue(0, out MessageQueue))
                        {
                            MessageQueue.Enqueue(recMessage); // FINISH
                        }
                    }
                    break;
                case MessageCode.C001:
                    break;
                case MessageCode.C002:
                    break;
                default:
                    break;
            }
        }

        private void PassOnMessage()
        {
            
        }

        private bool sendMessage(Message message)
        {
            try
            {
                sw.WriteLine(message.generateMessage());
                sw.Flush();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool connectionWorking() // ADD LOGIN DETAILS LATER
        {
            string response;
            if (sendMessage(new Message(MessageCode.C001, 0, 0, string.Empty)))
            {
                if (readNextMessage(out response))
                {
                    Message recMess = Message.InterpretString(response);
                    if (recMess.Code == MessageCode.C002)
                    {
                        connectedUserID = recMess.senderID;
                        return true;
                    }
                }
            }
            else
            {
                Thread.Sleep(300);
                if (readNextMessage(out response))
                {
                    Message recMess = Message.InterpretString(response);
                    if (recMess.Code == MessageCode.C002)
                    {
                        connectedUserID = recMess.senderID;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool readNextMessage(out string streamData) // IMPROVE LATER to work with bad messages better
        {
            streamData = string.Empty;
            try
            {
                string fullInput = "";
                while (sr.Peek() >= 0)
                {
                    string tempstring = sr.ReadLine();
                    if (tempstring.StartsWith("~~") || fullInput.StartsWith("~~"))
                    {
                        fullInput += tempstring;
                        if (tempstring.EndsWith("##"))
                        {
                            streamData = fullInput;
                            return true;
                        }
                        fullInput += "\r\n";
                    }
                }
            }
            catch{}
            finally
            {
                sr.DiscardBufferedData();
            }
            return false;
        }
    }
}
