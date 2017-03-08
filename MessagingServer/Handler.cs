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
                    updateFriendsOfNewStatus();
                    _appearingOnline = value;
                }
            }
        }
        private ConcurrentQueue<Message> userMessages;
        private List<int> friendList = new List<int>();
        private List<int> friendRequests = new List<int>();
        private bool DoesFriendUpdateExist
        {
            get
            {
                bool outVal;
                if (Program.OnlineStatusUpdates.TryGetValue(connectedUserID, out outVal))
                    return outVal;
                else throw new Exception("Online Status Update Fail");
            }
            set
            {
                if (!Program.OnlineStatusUpdates.TryUpdate(connectedUserID, value, !value))
                    throw new Exception("Online Status Update Fail");
            }
        }

        public void beginHandle(Socket connection)
        {
            NetworkStream socketStream = new NetworkStream(connection);
            sr = new StreamReader(socketStream);
            sw = new StreamWriter(socketStream);
            string ip = connection.RemoteEndPoint.ToString();

            try
            {
                // Test client for response
                if (connectionWorking())
                {
                    Console.WriteLine("Connection Working: " + ip + " USER: "
                        + connectedUserID.ToString("D4"));
                    // Create or Find messages queue
                    if (!Program.PassOnMessageDictionary.TryGetValue(connectedUserID, out userMessages))
                    {
                        userMessages = new ConcurrentQueue<Message>();
                        if (Program.PassOnMessageDictionary.TryAdd(connectedUserID, userMessages))
                            Console.WriteLine("Dictionary element added successfully (ID: "
                                + connectedUserID.ToString("D4") + ")");
                    }
                    // Create new friend update entry for the Connected User
                    if (!Program.OnlineStatusUpdates.TryAdd(connectedUserID, false))
                        throw new Exception("Online Status Update List Add ~ Failed");

                    // Add online status to dictionary
                    AppearingOnline = true;

                    // start socket poll
                    socketPoll(connection);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Handle Failure: " + exc.Message);
            }
            finally
            {
                // Cleanup
                AppearingOnline = false;
                bool tempBool;
                //string tempString;
                //if (Program.displayNameDictionary.TryRemove(connectedUserID, out tempString))
                //    Console.WriteLine(connectedUserID.ToString("D4")
                //        + " Display Name Dictionary Entry Removed.");
                if (Program.OnlineStatusUpdates.TryRemove(connectedUserID, out tempBool))
                    Console.WriteLine(connectedUserID.ToString("D4")
                        + " Status Update Dictionary Entry Removed.");
                if (Program.UsersAppearingOnlineDict.TryRemove(connectedUserID, out tempBool))
                    Console.WriteLine(connectedUserID.ToString("D4")
                        + " User Online Status Dictionary Entry Removed.");
                if (Program.PassOnMessageDictionary.TryRemove(connectedUserID, out userMessages))
                    Console.WriteLine(connectedUserID.ToString("D4")
                        + " Pass On Message Dictionary Entry Removed.");

                // Close Thread
                sw.Close();
                sr.Close();
                socketStream.Close();
                connection.Close();
                Console.WriteLine("Connection Closed: " + ip);
            }
        }

        private void socketPoll(Socket connection)
        {
            while (connection.IsConnected())
            {
                try
                {
                    // Read Messages
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

                    // Read Passed-On Messages
                    while (userMessages.Count > 0)
                    {
                        Message latestMessage;
                        if (userMessages.TryDequeue(out latestMessage))
                            messagePassOnHandler(latestMessage); 
                    }

                    // Send New Friend Online/Offline Updates
                    if (DoesFriendUpdateExist)
                    {
                        DoesFriendUpdateExist = false;
                        sendMessage(new Message(MessageCode.C010, 0, connectedUserID,
                            GetAllFriendStatus()));
                    }

                }
                catch (Exception exc)
                {
                    Console.WriteLine("POLLING ERROR: " + exc.Message);
                    return;
                }
                // Wait
                Task.Delay(PollRateMS);
            }
            return;
        }

        private void messagePassOnHandler(Message recMessage)
        {
            switch (recMessage.Code)
            {
                case MessageCode.C003:
                    sendMessage(recMessage);
                    break;
                case MessageCode.C009:
                    if (!friendRequests.Contains(recMessage.senderID) && sendMessage(recMessage))
                        friendRequests.Add(recMessage.senderID);
                    break;
                case MessageCode.C011: // IMPLEMENT LATER
                    break;
                case MessageCode.C018:
                    if (sendMessage(recMessage))
                        friendList.Add(recMessage.senderID);
                    sendMessage(new Message(MessageCode.C010, 0, recMessage.senderID,
                        GetAllFriendStatus()));
                    break;
                default:
                    throw new Exception("Unhandled Message Received From 'Pass-On' Service");
            }
            
        }

        private string GetAllFriendStatus()
        {
            string friendsStatusList = "";
            foreach (int friendID in friendList)
            {
                bool IsFriendOnline;
                string displayName;
                if (Program.displayNameDictionary.ContainsKey(friendID))
                    displayName = Program.displayNameDictionary[friendID];
                else displayName = "NODISPLAYNAME";
                if (Program.UsersAppearingOnlineDict.TryGetValue(friendID,out IsFriendOnline) && IsFriendOnline)
                {
                    friendsStatusList += friendID.ToString("D4") + 'T' + displayName + ';';
                }
                else friendsStatusList += friendID.ToString("D4") + 'F' + displayName + ';';
            }
            return friendsStatusList;
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
                        if (Program.PassOnMessageDictionary.TryGetValue(recMessage.receiverID, out MessageQueue))
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
                case MessageCode.C008: // Get Friend Status'
                    sendMessage(new Message(MessageCode.C010, 0, recMessage.senderID,
                        GetAllFriendStatus()));
                    break;
                case MessageCode.C009: // New Friend Request
                    {
                        if (!friendList.Contains(recMessage.receiverID)
                            && recMessage.receiverID != 0)
                        {
                            ConcurrentQueue<Message> MessageQueue;
                            if (Program.PassOnMessageDictionary.TryGetValue(recMessage.receiverID,
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
                case MessageCode.C011: // Remove Friend MODIFY LATER AFTER DbS ******
                    {
                        //// FINISH AFTER DBS?
                        //if (friendList.Remove(recMessage.receiverID))
                        //{
                        //    ThreadSafeList<int> tempTSList;
                        //    // Doesn't account for when the other person is offline
                        //    // Needs DBS implementation
                        //    if (Program.OnlineStatusUpdates.TryGetValue(recMessage.receiverID,
                        //        out tempTSList) && tempTSList.Remove(connectedUserID))
                        //    {
                        //        // Pass on friend remove request to other friend?
                        //
                        //        sendMessage(new Message(MessageCode.C012, 0, recMessage.senderID,
                        //            recMessage.createdTimeStamp.ToString()));
                        //        return;
                        //    }
                        //}
                        //sendMessage(new Message(MessageCode.C013, 0, recMessage.senderID,
                        //    recMessage.createdTimeStamp.ToString()));
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
                case MessageCode.C018: // Friend Request Accepted
                    if (friendRequests.Contains(recMessage.receiverID)) // If request exists
                    {
                        friendRequests.Remove(recMessage.receiverID); // Remove request
                        ConcurrentQueue<Message> MessageQueue;
                        if (Program.PassOnMessageDictionary.TryGetValue(recMessage.receiverID, out MessageQueue)) // If the other user is online
                        { 
                            Console.WriteLine("{0} C018 sent to {1}.", recMessage.senderID.ToString("D4"),recMessage.receiverID.ToString("D4"));
                            MessageQueue.Enqueue(recMessage);
                            sendMessage(new Message(MessageCode.C012, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                        else // If the other user is offline
                        {
                            Console.WriteLine("{0} C018 *NOT* sent to {1}.", recMessage.senderID.ToString("D4"), recMessage.receiverID.ToString("D4"));
                            sendMessage(new Message(MessageCode.C013, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                    }
                    else
                    {
                        sendMessage(new Message(MessageCode.C013, 0, recMessage.senderID,
                                    recMessage.createdTimeStamp.ToString()));
                    }
                    break;
                case MessageCode.C019:
                    if (friendRequests.Contains(recMessage.receiverID)) // If request exists
                    {
                        friendRequests.Remove(recMessage.receiverID); // Remove request
                    }
                    else
                    {
                        sendMessage(new Message(MessageCode.C013, 0, recMessage.senderID,
                                    recMessage.createdTimeStamp.ToString()));
                    }
                    break;
                case MessageCode.C007: // Maybe Change Later?
                    throw new Exception("Connection Failure");
                default:
                    sendMessage(new Message(MessageCode.C006, 0, recMessage.senderID,
                        recMessage.createdTimeStamp.ToString()));
                    break;
            }
        }

        private void updateFriendsOfNewStatus()
        {
            foreach (int friendID in friendList)
            {
                Program.OnlineStatusUpdates.TryUpdate(friendID, true, false);
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

        private bool connectionWorking() // ADD LOGIN DETAILS LATER?
        {
            string response;
            if (sendMessage(new Message(MessageCode.C001, 0, 0, string.Empty)) && sr.ReadNextMessage(out response))
            {
                Message recMess;
                if (Message.InterpretString(response, out recMess) && recMess.Code == MessageCode.C002)
                {
                    connectedUserID = recMess.senderID;
                    if (string.IsNullOrWhiteSpace(recMess.MessageString) &&
                        Program.displayNameDictionary.TryAdd(connectedUserID, recMess.MessageString))
                        return true;
                }
            }
            sendMessage(new Message(MessageCode.C007, 0, connectedUserID, ""));
            return false;
        }
    }
}
