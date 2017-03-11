using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using MessagingServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.ViewModels
{
    public class CommunicationHandlerViewModel : ObservableObject
    {
        #region Properties
        public int myID
        {
            get
            {
                return CommunicationHandler.myID;
            }
            set
            {
                CommunicationHandler.myID = value;
                RaisePropertyChanged("myID");
            }
        }
        public string DisplayName
        {
            get
            {
                return CommunicationHandler.DisplayName;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && value.Length <= 16)
                {
                    CommunicationHandler.DisplayName = value;
                    RaisePropertyChanged("DisplayName");
                }
            }
        }
        public int PollRateMS { get { return CommunicationHandler.PollRateMS; } }
        public bool Connected
        {
            get
            {
                return CommunicationHandler.Connected;
            }
        }
        public string ConnectedString
        {
            get
            {
                if (CommunicationHandler.Connected)
                    return "Connected";
                else return "No Connection";
            }
        }
        public StreamReader sr
        {
            get
            {
                return CommunicationHandler.sr;
            }
        }
        public StreamWriter sw
        {
            get
            {
                return CommunicationHandler.sw;
            }
        }
        #endregion

        #region Methods
        public void Connect()
        {
            //CommunicationHandler.Connect();
            //RaisePropertyChanged("Connected");
            //RaisePropertyChanged("DisplayName");
            //RaisePropertyChanged("ConnectedString");
        }
        public bool SendMessage(Message message)
        {
            bool temp = CommunicationHandler.SendMessage(message);
            if (!temp)
            {
                RaisePropertyChanged("Connected");
                RaisePropertyChanged("ConnectedString");
            }
            return temp;
        }
        public MTObservableCollection<User> ParseC010Message(string inputMessage)
        {
            return CommunicationHandler.ParseC010Message(inputMessage);
        }
        public void CloseConnection()
        {
            CommunicationHandler.CloseConnection();
            RaisePropertyChanged("Connected");
            RaisePropertyChanged("ConnectedString");
        }
        #endregion
    }
}
