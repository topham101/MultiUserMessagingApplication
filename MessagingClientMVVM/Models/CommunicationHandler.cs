using MessagingServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.ViewModels;
using System.Security;

namespace MessagingClientMVVM.Models
{
    public static class CommunicationHandler
    {
        #region Properties
        private static TcpClient Client { get; set; }
        public static int myID { get; set; } // make private set later
        public static string DisplayName { get; set;}
        public static int PollRateMS { get { return 500; } }
        public static bool Connected { get; private set; }
        public static StreamReader sr { get; private set; }
        public static StreamWriter sw { get; private set; }
        #endregion

        #region Methods
        public static void Connect(string username, string password, bool IsNewUser) // FIX LATER
        {
            if (Client == null)
            {
                try
                {
                    Client = new TcpClient();
                    Client.Connect("localhost", 25566);
                    if (Client.Connected)
                    {
                        sr = new StreamReader(Client.GetStream());
                        sw = new StreamWriter(Client.GetStream());

                        // FIX LATER Check if password contains illegal strings/characters
                        string messageCommand = IsNewUser ? "REGIS" : "LOGIN";
                        SendMessage(new Message(MessageCode.C001, 0, 0, string.Format(messageCommand + username + ":" + password)));

                        string streamData;
                        if (!sr.ReadNextMessage(out streamData))
                            throw new Exception("No Server Response");
                        sr.DiscardBufferedData();

                        Message serverMessageObj;
                        if (!Message.InterpretString(streamData, out serverMessageObj))
                            throw new Exception("Unintelligible Server Response.");

                        if (serverMessageObj.Code != MessageCode.C002 && serverMessageObj.Code != MessageCode.C021)
                            throw new Exception("No Connection Test Received");
                        else if (serverMessageObj.Code == MessageCode.C021)
                            throw new Exception(serverMessageObj.MessageString);

                        KeyValuePair<int, string> tempKeyVal = ParseC002Message(serverMessageObj.MessageString);
                        myID = tempKeyVal.Key;
                        DisplayName = tempKeyVal.Value;

                        Connected = true;
                    }
                }
                catch (Exception e)
                {
                    Connected = false;
                    if (sr != null)
                    {
                        sr.Dispose();
                        sr = null;
                    }
                    if (sw != null)
                    {
                        sw.Dispose();
                        sw = null;
                    }
                    if (Client != null)
                    {
                        Client.Close();
                        Client = null;
                    }
                    throw e;
                }
            }
        }
        public static bool SendMessage(Message message)
        {
            try
            {
                sw.WriteLine(message.generateMessage());
                sw.Flush();
            }
            catch
            {
                Connected = false;
                return false;
            }
            return true;
        }
        public static KeyValuePair<int, string> ParseC002Message(string inputMessage) // improve validation later
        {
            string[] temp = inputMessage.Split(';');
            return new KeyValuePair<int, string>(int.Parse(temp[0]), temp[1]);
        }
        public static MTObservableCollection<User> ParseC010Message(string inputMessage)
        {
            MTObservableCollection<User> returnList = new MTObservableCollection<User>();
            string[] friends = inputMessage.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in friends)
            {
                if (item.Length < 5)
                    throw new Exception("Invalid Input");
                int tempID;
                if (!int.TryParse(item.Substring(0, 4), out tempID))
                    throw new Exception("Invalid Input");
                bool tempStatus;
                if (item.ToLower()[4] == 't')
                    tempStatus = true;
                else if (item.ToLower()[4] == 'f')
                    tempStatus = false;
                else throw new Exception("Invalid Input");
                returnList.Add(new User(tempID, item.Substring(5, item.Length - 5), tempStatus));
            }
            return returnList;
        }
        public static void CloseConnection()
        {
            Connected = false;
            if (sr != null)
            {
                sr.Dispose();
                sr = null;
            }
            if (sw != null)
            {
                sw.Dispose();
                sw = null;
            }
            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
        }
        #endregion
    }
}
