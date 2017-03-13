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
        public string GlobalConnectionString = 
            Properties.Settings.Default.MessagingServerConnectionString;

        private SqlConnection sqlConn;
        private string UserLoginQuery = "SELECT * FROM Users WHERE UserName=@username AND user_password=@password;";
        private string CheckUserExistsQuery = "select count(1) from Users where UserName=@username;";
        private string UserRegisterInsert = "INSERT INTO Users(UserName, DisplayName, user_password) VALUES(@username, 'New User', @password);";
        private string UserNameUpdate = "UPDATE Users SET DisplayName=@NewDispName WHERE Id=@UserID;";
        private string FindFriendshipsQuery = "SELECT * FROM Friendships WHERE UserID_1 = @userID OR UserID_2 = @userID;";
        private string NewFriendshipsInsert = "INSERT INTO Friendships (UserID_1, UserID_2) VALUES (@userID_1, @userID_2);";

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
        private string DisplayName
        {
            get
            {
                return Program.displayNameDictionary[connectedUserID];
            }
            set
            {
                Program.displayNameDictionary[connectedUserID] = value;
                updateFriendsOfNewStatus();
            }
        }

        public void beginHandle(Socket connection)
        {
            // Initialise
            NetworkStream socketStream = new NetworkStream(connection);
            sr = new StreamReader(socketStream);
            sw = new StreamWriter(socketStream);
            string ip = connection.RemoteEndPoint.ToString();
            sqlConn = new SqlConnection(GlobalConnectionString);

            // Begin Communication
            try
            {
                sqlConn.Open();

                // Test client for response
                if (connectionWorking())
                {
                    Console.WriteLine("Connection Working: " + ip + " USER: "
                        + connectedUserID.ToString("D4") + " Name: " + DisplayName);

                    // Get friends from database
                    loadFriends();
                    // Send Friends Status
                    sendMessage(new Message(MessageCode.C010, 0, connectedUserID,
                        GetAllFriendStatus()));

                    // Create or Find messages queue
                    if (!Program.PassOnMessageDictionary.TryGetValue(connectedUserID, out userMessages))
                    {
                        userMessages = new ConcurrentQueue<Message>();
                        if (Program.PassOnMessageDictionary.TryAdd(connectedUserID, userMessages))
                            Console.WriteLine("Dictionary Entry Added Successfully (ID: "
                                + connectedUserID.ToString("D4") + ")");
                    }

                    // Create new friend-update entry for the Connected User
                    if (!Program.OnlineStatusUpdates.TryAdd(connectedUserID, false))
                        throw new Exception("Online Status Update List Add ~ Failed");
                    Console.WriteLine("Online Status Dictionary Entry Added Successfully");

                    // Set Online Status
                    AppearingOnline = true;
                    Console.WriteLine("User: " + connectedUserID + " Appearing Online");

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
                updateFriendsOfNewStatus();

                bool tempBool;
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
                sqlConn.Close();
                sqlConn.Dispose();
                sw.Close();
                sr.Close();
                socketStream.Close();
                connection.Close();
                Console.WriteLine("Connection Closed: " + ip);
            }
        }

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
        private void updateFriendsOfNewStatus()
        {
            foreach (int friendID in friendList)
            {
                Program.OnlineStatusUpdates.TryUpdate(friendID, true, false);
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
            Console.WriteLine(recMessage.Code + " Passed on to " + connectedUserID + ". Handled.");
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
                else displayName = "_NODISPLAYNAME";
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
                    if (UpdateDispName(recMessage.MessageString) && sendMessage(
                        new Message(MessageCode.C002, 0, recMessage.senderID, recMessage.MessageString)))
                            DisplayName = recMessage.MessageString;
                    else sendMessage(new Message(MessageCode.C007, 0, recMessage.senderID, recMessage.MessageString));
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
                        if (!friendList.Contains(recMessage.receiverID) && recMessage.receiverID != 0)
                        {
                            string othersDisplayName;
                            ConcurrentQueue<Message> MessageQueue;
                            if (Program.PassOnMessageDictionary.TryGetValue(recMessage.receiverID, out MessageQueue)
                                && Program.displayNameDictionary.TryGetValue(recMessage.receiverID, out othersDisplayName))
                            {
                                recMessage.MessageString = DisplayName;
                                MessageQueue.Enqueue(recMessage);
                                Console.WriteLine("{0} C009 sent to {1}.",
                                    recMessage.senderID.ToString("D4"),
                                    recMessage.receiverID.ToString("D4"));
                                sendMessage(new Message(MessageCode.C012, recMessage.receiverID,
                                    connectedUserID, recMessage.createdTimeStamp.ToString()));
                                sendMessage(new Message(MessageCode.C020, recMessage.receiverID,
                                    connectedUserID, othersDisplayName));
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
                        friendList.Add(recMessage.receiverID);
                        ConcurrentQueue<Message> MessageQueue;
                        if (Program.PassOnMessageDictionary.TryGetValue(recMessage.receiverID, out MessageQueue)) // If the other user is online
                        { 
                            MessageQueue.Enqueue(recMessage);
                            Console.WriteLine("{0} C018 sent to {1}.", recMessage.senderID.ToString("D4"),recMessage.receiverID.ToString("D4"));
                            sendMessage(new Message(MessageCode.C012, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                        else // If the other user is offline
                        {
                            Console.WriteLine("{0} C018 *NOT* sent to {1}.", recMessage.senderID.ToString("D4"), recMessage.receiverID.ToString("D4"));
                            sendMessage(new Message(MessageCode.C013, recMessage.receiverID,
                                connectedUserID, recMessage.createdTimeStamp.ToString()));
                        }
                        CreateNewFriendship(recMessage.senderID, recMessage.receiverID);
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
        private bool connectionWorking()
        {
            string response;
            Message recMess;
            if (sr.ReadNextMessage(out response) &&
                Message.InterpretString(response, out recMess) &&
                recMess.Code == MessageCode.C001)
            {
                bool isNewUser;
                if (recMess.MessageString.StartsWith("REGIS"))
                {
                    isNewUser = true;
                }
                else if (recMess.MessageString.StartsWith("LOGIN"))
                {
                    isNewUser = false;
                }
                else
	            {
                    sendMessage(new Message(MessageCode.C021, 0, connectedUserID, ""));
                    return false;
                }

                string[] detailsArray;
                try
                {
                    detailsArray = recMess.MessageString.Substring
                        (5, (recMess.MessageString.Length - 5)).Split(':');
                }
                catch
                {
                    sendMessage(new Message(MessageCode.C021, 0, connectedUserID, ""));
                    return false; ;
                }
                if (detailsArray.Length == 2 &&
                    !string.IsNullOrWhiteSpace(detailsArray[0]) &&
                    !string.IsNullOrWhiteSpace(detailsArray[1]))
                {
                    if (!isNewUser) // Log in User
                    {
                        // Check username and password combination exists
                        // Returns ID and DisplayName to Client
                        try
                        {
                            Tuple<int, string> LoginReturn = 
                                QueryLoginData(detailsArray[0], detailsArray[1]);

                            sendMessage(new Message(MessageCode.C002, 0, connectedUserID,
                                string.Format(LoginReturn.Item1 + ";" + LoginReturn.Item2)));

                        }
                        catch (Exception)
                        {
                            sendMessage(new Message(MessageCode.C021, 0,
                                connectedUserID, "Incorrect Login Details"));
                            return false;
                        }

                        // Load friends list in here

                        return true;
                    }
                    else // Register New User
                    {
                        try
                        {
                            // Check if user already exists
                            if (DoesUserExist(detailsArray[0]))
                            {
                                sendMessage(new Message(MessageCode.C021, 0,
                                connectedUserID, "Username already taken."));
                                return false;
                            }

                            // Create new user entry
                            Tuple<int, string> RegisterReturn = 
                                CreateNewUser(detailsArray[0], detailsArray[1]);

                            // Return ID and Display Name to client
                            sendMessage(new Message(MessageCode.C002, 0, connectedUserID,
                                string.Format(RegisterReturn.Item1 + ";" + RegisterReturn.Item2)));
                        }
                        catch (Exception)
                        {
                            sendMessage(new Message(MessageCode.C021, 0,
                                connectedUserID, "Unknown Error"));
                            return false;
                        }

                        // Load friends list in here

                        return true;
                    }
                }
            }
            sendMessage(new Message(MessageCode.C021, 0, connectedUserID, ""));
            return false;
        }
        private Tuple<int, string> QueryLoginData(string usernameInput, string passwordInput)
        {
            using (SqlCommand loginCommand = new SqlCommand(UserLoginQuery, sqlConn))
            {
                loginCommand.Parameters.Add(
                    new SqlParameter("username", usernameInput));
                loginCommand.Parameters.Add(
                    new SqlParameter("password", passwordInput));

                string username; // Find in DBS
                string password;
                int tempId;
                string tempDisplayName;

                using (SqlDataReader reader = loginCommand.ExecuteReader())
                {
                    if (!reader.Read() && !reader.HasRows)
                        throw new Exception("No Results Found");

                    tempId = (int)reader[0];
                    username = (string)reader[1];
                    tempDisplayName = (string)reader[2];
                    password = (string)reader[3];

                    connectedUserID = tempId;
                    DisplayName = tempDisplayName;

                    // If incorrect results, throw Exception
                    if (usernameInput != username || passwordInput != password)
                        throw new Exception("Unknown SQL Results Error");

                    if (reader.Read()) // If more than one line throw Exception
                        throw new Exception("Multiple Results Found");
                        
                    return new Tuple<int, string>(tempId, tempDisplayName);
                }
            }
        }
        private bool DoesUserExist(string usernameInput)
        {
            using (SqlCommand CheckExistCommand = new SqlCommand(CheckUserExistsQuery, sqlConn))
            {
                CheckExistCommand.Parameters.Add(
                    new SqlParameter("username", usernameInput));

                using (SqlDataReader reader = CheckExistCommand.ExecuteReader())
                {
                    if (!reader.Read() && !reader.HasRows)
                        throw new Exception("No Results Found");

                    int returnVal = (int)reader[0];

                    if (reader.Read()) // If more than one line throw Exception
                        throw new Exception("Multiple Results Found");

                    if (returnVal == 1)
                        return true;
                    else return false;
                }
            }
        }
        private Tuple<int, string> CreateNewUser(string usernameInput, string passwordInput)
        {
            using (SqlCommand RegisterCommand = new SqlCommand(UserRegisterInsert, sqlConn))
            {
                RegisterCommand.Parameters.Add(
                    new SqlParameter("username", usernameInput));
                RegisterCommand.Parameters.Add(
                    new SqlParameter("password", passwordInput));

                if (RegisterCommand.ExecuteNonQuery() > 1)
                    throw new Exception("Query Failed?");

                return QueryLoginData(usernameInput, passwordInput);
            }
        }
        private bool UpdateDispName(string DisplayNameInput)
        {
            try
            {
                using (SqlCommand UpdateCommand = new SqlCommand(UserNameUpdate, sqlConn))
                {
                    UpdateCommand.Parameters.Add(new SqlParameter("NewDispName", DisplayNameInput));
                    UpdateCommand.Parameters.Add(new SqlParameter("UserID", connectedUserID));

                    if (UpdateCommand.ExecuteNonQuery() > 1)
                        throw new Exception("Update Failed?");

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private void loadFriends()
        {
            using (SqlCommand LoadFriendsCommand = new SqlCommand(FindFriendshipsQuery, sqlConn))
            {
                LoadFriendsCommand.Parameters.Add(new SqlParameter("UserID", connectedUserID));

                List<int> friends = new List<int>();
                using (SqlDataReader Reader = LoadFriendsCommand.ExecuteReader())
                    while (Reader.Read())
                    {
                        if ((int)Reader[1] == connectedUserID)
                            friendList.Add((int)Reader[2]);
                        else if ((int)Reader[2] == connectedUserID)
                            friendList.Add((int)Reader[1]);
                        else throw new Exception("Invalid SQL Query Result");
                    }
                return;
            }
        }
        private bool CreateNewFriendship(int userID1, int userID2)
        {
            try
            {
                using (SqlCommand NewFriendshipCommand = new SqlCommand(NewFriendshipsInsert, sqlConn))
                {
                    NewFriendshipCommand.Parameters.Add(
                        new SqlParameter("userID_1", userID1));
                    NewFriendshipCommand.Parameters.Add(
                        new SqlParameter("userID_2", userID2));

                    if (NewFriendshipCommand.ExecuteNonQuery() > 1)
                        throw new Exception("Query Failed?");

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
