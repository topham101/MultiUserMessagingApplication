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

        #region Properties
        public TcpClient Client { get; private set; }
        public int myID { get; private set; }
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
                myID = 1; // change later
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
                        Message serverMessageObj = Message.InterpretString(streamData);
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
                    sr.Close();
                    sw.Close();
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
