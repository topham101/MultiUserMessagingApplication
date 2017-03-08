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

namespace MessagingClientMVVM.Models
{
    public class CommunicationHandler : ObservableObject
    {
        bool _connected = false;
        string _displayName;

        #region Properties
        public TcpClient Client { get; private set; }
        public int myID { get; private set; }
        public string DisplayName // Make Private Set later??
        {
            get
            {
                return _displayName;
            }
            set
            {
                if (value.Length <= 20)
                {
                    _displayName = value;
                    RaisePropertyChanged("DisplayName"); 
                }
            }
        }
        public int PollRateMS { get { return 500; } }
        public bool Connected
        {
            get
            {
                return _connected;
            }
            private set
            {
                _connected = value;
                RaisePropertyChanged("ConnectedString");
            }
        }
        public string ConnectedString
        {
            get
            {
                if (_connected)
                    return "Connected";
                else return "No Connection";
            }
        }
        public StreamReader sr { get; private set; }
        public StreamWriter sw { get; private set; }
        #endregion

        #region Methods
        public void Connect()
        {
            if (Client == null)
            {
                myID = 1; // CHANGE LATER
                if (string.IsNullOrWhiteSpace(DisplayName))
                    DisplayName = "TestUser99";

                try
                {
                    //localMessageQueue = new ConcurrentQueue<Message>();
                    Client = new TcpClient();
                    Client.Connect("localhost", 25566);
                    if (Client.Connected)
                    {
                        //OutputTextBlock.Text = "Connected";

                        sr = new StreamReader(Client.GetStream());
                        sw = new StreamWriter(Client.GetStream());

                        string streamData;
                        if (!sr.ReadNextMessage(out streamData))
                            throw new Exception("No Server Response");
                        sr.DiscardBufferedData();

                        Message serverMessageObj;
                        if (!Message.InterpretString(streamData, out serverMessageObj))
                        {
                            throw new Exception("Unintelligible Server Response.");
                        }
                        // SendMessage(serverMessageObj);

                        if (serverMessageObj.Code == MessageCode.C001)
                            SendMessage(new Message(MessageCode.C002, myID, 0, string.Empty));
                        else throw new Exception("No Connection Test Received");

                        Connected = true;

                    }
                }
                catch
                {
                    Connected = false;
                    if (sr != null)
                        sr.Close();
                    if (sw != null)
                        sw.Close();
                    if (Client != null)
                        Client.Close();
                }
            }
        }
        public bool SendMessage(Message message)
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
        public List<User> ParseC010Message(string inputMessage)
        {
            List<User> returnList = new List<User>();
            string[] friends = inputMessage.Split(';');
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
        public void CloseConnection()
        {
            Connected = false;
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (Client != null)
                Client.Close();
        }
        #endregion
    }
}
