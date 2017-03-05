using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagingServer
{
    public class Handler
    {
        public static string GlobalConnectionString = 
            Properties.Settings.Default.MessagingServerConnectionString;

        private const int PollRateMS = 500;
        private StreamReader sr;
        private StreamWriter sw;

        private int connectedUserID = 0;
        private ConcurrentQueue<Message> userMessages;
        private bool _appearingOnline;
        private bool AppearingOnline
        {
            get
            {
                return _appearingOnline;
            }
            set
            {
                if (value != _appearingOnline)
                {
                    if (Program.UsersAppearingOnlineDict.ContainsKey(connectedUserID))
                    {
                        if (!Program.UsersAppearingOnlineDict.TryUpdate(connectedUserID, value, !value))
                            throw new Exception("Error Changing Online Appearance");
                    }
                    else if (!Program.UsersAppearingOnlineDict.TryAdd(connectedUserID, value))
                    {
                        throw new Exception("Error Changing Online Appearance");
                    } 
                    _appearingOnline = value;
                }
            }
        }
        private List<int> friendList = new List<int>();
        private List<int> friendRequests = new List<int>();

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
                // Create or Find messages queue
                if (!Program.USERSdictionary.TryGetValue(connectedUserID, out userMessages))
                {
                    userMessages = new ConcurrentQueue<Message>();
                    if (Program.USERSdictionary.TryAdd(connectedUserID, userMessages))
                        Console.WriteLine("Dictionary element added successfully (ID: "+ connectedUserID.ToString("D4") + ")");
                }
                // Add online status to dictionary
                AppearingOnline = true;

                // start socket poll
                socketPoll(connection);
            }
            // Close Thread
            Program.USERSdictionary.TryRemove(connectedUserID, out userMessages);
            Console.WriteLine(connectedUserID.ToString("D4") + " Dictionary Removed.");

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
                            Message nextMessageObj;
                            if (Message.InterpretString(nextMessage, out nextMessageObj))
                                messageHandler(nextMessageObj);
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
                    // HERE check for friends requests.
                    // Add request to list
                    // Check none from the same user already exist
                }
                catch (Exception exc)
                {
                    Console.WriteLine("POLLING ERROR" + exc.Message);
                    return;
                }
                // Wait
                Task.Delay(PollRateMS);
            }
            return;
        }

        private void messageHandler(Message recMessage)
        {
            switch (recMessage.Code)
            {
                case MessageCode.C001:
                    sendMessage(new Message(MessageCode.C002, 0, recMessage.senderID, ""));
                    break;
                case MessageCode.C002:
                    sendMessage(new Message(MessageCode.C007, 0, recMessage.senderID, ""));
                    break;
                case MessageCode.C003:
                    {
                        if (recMessage.receiverID == 0)
                            return;
                        ConcurrentQueue<Message> MessageQueue;
                        if (Program.USERSdictionary.TryGetValue(recMessage.receiverID, out MessageQueue))
                        {
                            Console.WriteLine("{0} C003 sent to {1}.",
                                recMessage.senderID.ToString("D4"),
                                recMessage.receiverID.ToString("D4"));
                            MessageQueue.Enqueue(recMessage);
                            sendMessage(new Message(MessageCode.C004, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                        else
                        {
                            Console.WriteLine("{0} C003 *NOT* sent to {1}.",
                                recMessage.senderID.ToString("D4"),
                                recMessage.receiverID.ToString("D4"));
                            sendMessage(new Message(MessageCode.C005, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                    }
                    break;
                case MessageCode.C008: //Request to get the online status all friends
                                       // (E.g. Sent during first Log-in or Manual Refresh).
                    {
                        string friendsStatusList = "";
                        foreach (int item in friendList)
                        {
                            bool IsFriendOnline;
                            if (Program.UsersAppearingOnlineDict.TryGetValue(item,
                                out IsFriendOnline) && IsFriendOnline)
                            {
                                friendsStatusList += item + 'T' + ';';
                            }
                            else friendsStatusList += item + 'F' + ';';
                        }
                        sendMessage(new Message(MessageCode.C010, 0, recMessage.senderID,
                            friendsStatusList));
                    }
                    break;
                case MessageCode.C009: // Request to send a friend request
                    {
                        if (!friendList.Contains(recMessage.receiverID)
                            && recMessage.receiverID != 0)
                        {
                            ConcurrentQueue<Message> MessageQueue;
                            if (Program.USERSdictionary.TryGetValue(recMessage.receiverID,
                                out MessageQueue))
                            {
                                MessageQueue.Enqueue(recMessage);
                                Console.WriteLine("{0} C009 sent to {1}.",
                                    recMessage.senderID.ToString("D4"),
                                    recMessage.receiverID.ToString("D4"));
                                sendMessage(new Message(MessageCode.C012, recMessage.receiverID,
                                    connectedUserID, recMessage.createdTimeStamp.ToString()));
                            }
                            else
                            {
                                Console.WriteLine("{0} C009 *NOT* sent to {1}.",
                                    recMessage.senderID.ToString("D4"),
                                    recMessage.receiverID.ToString("D4"));
                                sendMessage(new Message(MessageCode.C013, recMessage.receiverID,
                                    connectedUserID, recMessage.createdTimeStamp.ToString()));
                            }
                        }
                        else sendMessage(new Message(MessageCode.C013, 0, recMessage.senderID,
                                    recMessage.createdTimeStamp.ToString()));
                    }
                    break;
                case MessageCode.C011:
                    {
                        int newFriendID;
                        if (int.TryParse(recMessage.MessageString, out newFriendID) &&
                            friendList.Remove(newFriendID))
                        {
                            sendMessage(new Message(MessageCode.C012, 0, recMessage.senderID,
                                    recMessage.createdTimeStamp.ToString()));
                        }
                        else sendMessage(new Message(MessageCode.C013, 0, recMessage.senderID,
                            recMessage.createdTimeStamp.ToString()));
                    }
                    break;
                case MessageCode.C014:
                    try
                    {
                        AppearingOnline = false;
                    }
                    catch
                    {
                        sendMessage(new Message(MessageCode.C017, 0, recMessage.senderID, ""));
                        return;
                    }
                    sendMessage(new Message(MessageCode.C016, 0, recMessage.senderID, ""));
                    break;
                case MessageCode.C015:
                    try
                    {
                        AppearingOnline = true;
                    }
                    catch
                    {
                        sendMessage(new Message(MessageCode.C017, 0, recMessage.senderID, ""));
                        return;
                    }
                    sendMessage(new Message(MessageCode.C016, 0, recMessage.senderID, ""));
                    break;
                case MessageCode.C018:
                    if (friendRequests.Contains(recMessage.receiverID))
                    {
                        friendRequests.Remove(recMessage.receiverID);

                    }
                    break;
                case MessageCode.C019:
                    break;
                case MessageCode.C007: // Maybe Change Later?
                    throw new Exception("Connection Failure");
                default:
                    sendMessage(new Message(MessageCode.C006, 0, recMessage.senderID,
                        recMessage.createdTimeStamp.ToString()));
                    break;
            }
        }

        private bool ParseUserList(string message, out List<int> userList)
        {
            try
            {
                List<string> tempList = message.Split(';').ToList();
                userList = tempList.Select(s => int.Parse(s)).ToList();
            }
            catch
            {
                userList = new List<int>();
                return false;
            }
            return true;
        } // Remove Later?

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
            if (sendMessage(new Message(MessageCode.C001, 0, 0, string.Empty)) && sr.ReadNextMessage(out response))
            {
                Message recMess;
                if (Message.InterpretString(response, out recMess) && recMess.Code == MessageCode.C002)
                {
                    connectedUserID = recMess.senderID;
                    return true;
                }
            }
            else sendMessage(new Message(MessageCode.C007, 0, connectedUserID, ""));
            return false;
        }
    }
}
