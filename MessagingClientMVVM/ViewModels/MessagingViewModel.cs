using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using MessagingClientMVVM.ViewModels;
using MessagingServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MessagingClientMVVM
{
    class MessagingViewModel : ObservableObject
    {
        #region Members
        ObservableCollection<User> _users = new ObservableCollection<User>();
        MessageCollectionViewModel _messageCollections = new MessageCollectionViewModel();
        CommunicationHandler _handler = new CommunicationHandler();
        User _selectedUser;
        string _messageInput;
        #endregion

        #region Constructors
        public MessagingViewModel()
        {
            _users.Add(new User(0, "Server"));
            _users.Add(new User(1, "Test User"));
            _users.Add(new User(2, "Test User 2"));
            _users.Add(new User(3, "Test User 3"));
        }
        #endregion

        #region Properties
        public MessageCollectionViewModel MessageCollections
        {
            get
            {
                return _messageCollections;
            }
            set
            {
                _messageCollections = value;
            }
        }
        public CommunicationHandler Handler
        {
            get
            {
                return _handler;
            }
        }
        public ObservableCollection<User> Users
        {
            get
            {
                return _users;
            }
            set
            {
                _users = value;
            }
        }
        public string MessageInput
        {
            get
            {
                return _messageInput;
            }
            set
            {
                _messageInput = value;
                RaisePropertyChanged("MessageInput");
            }
        }
        public User SelectedUser
        {
            get
            {
                return _selectedUser;
            }
            set
            {
                _selectedUser = value;
                _messageCollections.SelectedUserID = _selectedUser.ID;
                MessageInput = "";
            }
        }
        #endregion

        #region Methods
        private void PollStreamForNew()
        {
            while (Handler.Connected)
            {
                try
                {
                    if (Handler.sr.Peek() >= 0)
                    {
                        string nextMessage;
                        while (Handler.sr.ReadNextMessage(out nextMessage))
                        {
                            // Handle Messages
                            Message nextMessageObj;
                            if (Message.InterpretString(nextMessage, out nextMessageObj))
                                MessageHandler(nextMessageObj);
                            nextMessage = string.Empty;
                        }
                    }
                    Task.Delay(Handler.PollRateMS);
                }
                catch (Exception)
                {
                    MessageInput = "CONNECTION FAILED";
                    Handler.CloseConnection();
                    return;
                }
            }
        }

        private void MessageHandler(Message tempMessage)
        {
            switch (tempMessage.Code)
            {
                case MessageCode.C002: // Implement Later
                    break;
                case MessageCode.C003: // Message from another User
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        _messageCollections.Add(tempMessage, _handler.myID);
                    }));
                    break;
                case MessageCode.C004: // Implement Later
                    break;
                case MessageCode.C005: // Message sending fail
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        _messageCollections.RemoveByTimeStamp(tempMessage);
                    }));
                    break;
                case MessageCode.C006: // Implement Later
                    break;
                case MessageCode.C007: // Connection Closing / Connection Test Fail
                    Handler.CloseConnection();
                    break;
                case MessageCode.C009:
                    break;
                case MessageCode.C010:
                    Users = new ObservableCollection<User>(Handler.ParseC010Message(tempMessage.MessageString));
                    break;
                case MessageCode.C011:
                    break;
                case MessageCode.C012:
                    break;
                case MessageCode.C013:
                    break;
                case MessageCode.C016:
                    break;
                case MessageCode.C017:
                    break;
                case MessageCode.C018:
                    break;
                default:
                    throw new Exception("Invalid Message Code");
            }
        }
        #endregion

        #region Commands
        void AddMessageExecute() // AMEND LATER
        {
            if (!Handler.Connected) // Remove this later???????
            {
                Handler.DisplayName = MessageInput;
                MessageInput = "";
                return;
            }


            if (_messageCollections.Collection == null || string.IsNullOrWhiteSpace(MessageInput))
                return;
            Message m = new Message(MessageCode.C003, 1, _selectedUser.ID, string.Format(MessageInput));
            _messageCollections.Add(m, _handler.myID);
            _handler.SendMessage(m);
            MessageInput = "";
        }
        bool CanAddMessageExecute() // Add IsConnected return later
        {
            return true;
        }
        public ICommand AddMessages { get { return new RelayCommand(AddMessageExecute, CanAddMessageExecute); } }

        void Connect()
        {
            Handler.Connect();
            if (Handler.Connected)
            {
                Task t1 = new Task(() => PollStreamForNew());
                t1.Start();
            }
        }
        bool CanConnect()
        {
            return !Handler.Connected;
        }
        public ICommand ConnectCommand { get { return new RelayCommand(Connect, CanConnect); } }
        #endregion
    }
}
