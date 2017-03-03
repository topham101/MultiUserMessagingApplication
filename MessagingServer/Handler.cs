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
using System.Diagnostics;

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
        private ConcurrentQueue<Message> userMessages
            = new ConcurrentQueue<Message>();

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
                    connectedUserID, userMessages))
                {
                    Console.WriteLine("Dictionary element added successfully (ID: "
                        + connectedUserID.ToString("D4") + ")");
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
                try
                {
                    while (connection.Available > 0)
                    {
                        string nextMessage;
                        if (sr.ReadNextMessage(out nextMessage))
                        {
                            // Handle Messages 
                            messagePassOnHandler(Message.InterpretString(nextMessage));
                            nextMessage = string.Empty;
                        }
                        sr.DiscardBufferedData();
                    }
                    if (userMessages.Count > 0)
                    {
                        Message latestMessage;
                        while (userMessages.TryDequeue(out latestMessage))
                        {
                            Debugger.Log(1, "General", "Done");
                            if (!sendMessage(latestMessage))
                                throw new Exception();
                        } 
                    }
                }
                catch
                {
                    Console.WriteLine("POLLING ERROR");
                    return;
                }
                // Wait
                Task.Delay(PollRateMS);
            }
            return;
        }

        private void messagePassOnHandler(Message recMessage) // amend later
        {
            Console.WriteLine("Sent");
            //Console.WriteLine("User " + recMessage.senderID.ToString("D4") + " sent: "
            //    + Environment.NewLine + recMessage.Code.ToString() + " "
            //    + recMessage.MessageString + Environment.NewLine + "To: "
            //    + recMessage.receiverID.ToString("D4"));
            //SqlConnection connection = new SqlConnection(Handler.GlobalConnectionString);
            //connection.Open();
            //SqlCommand insert = new SqlCommand(@"");
            //insert.

            switch (recMessage.Code)
            {
                case MessageCode.C003:
                    if (recMessage.receiverID == 0)
                        return;
                    ConcurrentQueue<Message> MessageQueue;
                    if (Program.USERSdictionary.TryGetValue(recMessage.receiverID, out MessageQueue))
                    {
                        MessageQueue.Enqueue(recMessage);
                        sendMessage(new Message(MessageCode.C004, 0, connectedUserID,
                            recMessage.createdTimeStamp.ToString()));
                    }
                    else
                    {
                        sendMessage(new Message(MessageCode.C005, 0, connectedUserID,
                            recMessage.createdTimeStamp.ToString()));
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
                if (sr.ReadNextMessage(out response))
                {
                    Message recMess = Message.InterpretString(response);
                    if (recMess.Code == MessageCode.C002)
                    {
                        connectedUserID = recMess.senderID;
                        return true;
                    }
                }
                else
                {
                    Task.Delay(500);
                    if (sr.ReadNextMessage(out response))
                    {
                        Message recMess = Message.InterpretString(response);
                        if (recMess.Code == MessageCode.C002)
                        {
                            connectedUserID = recMess.senderID;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

    }
}
