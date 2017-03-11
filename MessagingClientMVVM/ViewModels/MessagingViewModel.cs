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
        private MTObservableCollection<FriendRequestViewModel> _friendRequestCollection = new MTObservableCollection<FriendRequestViewModel>();
        private MTObservableCollection<User> _users = new MTObservableCollection<User>();
        private MessageCollectionViewModel _messageCollections = new MessageCollectionViewModel();
        private CommunicationHandler _handler = new CommunicationHandler();
        private FriendRequestViewModel _selectedFriendRequest;
        private User _selectedUser;
        private string _messageInput;
        private string _newDisplayName;
        private int? _friendRequestID;
        #endregion

        #region Constructors
        public MessagingViewModel()
        {
            //_users.Add(new User(0, "Server"));
            //_users.Add(new User(1, "E1eet101"));
            //_users.Add(new User(2, "alphAD0nkeyK0ng"));
            //_users.Add(new User(3, "Topham101"));
            //_friendRequestCollection.Add(new FriendRequest(1, 2, "Swcharzzneggerr", true));
            //_friendRequestCollection.Add(new FriendRequest(3, 1, "TooTrumpForYoo", false));
        }
        #endregion

        #region Properties
        public MTObservableCollection<FriendRequestViewModel> FriendRequestCollection
        {
            get
            {
                return _friendRequestCollection;
            }
            set
            {
                _friendRequestCollection = value;
                RaisePropertyChanged("FriendRequestCollection");
            }
        }
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
        public MTObservableCollection<User> Users
        {
            get
            {
                return _users;
            }
            set
            {
                _users = value;
                RaisePropertyChanged("Users");
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
                RaisePropertyChanged("SelectedUser");
                if (value != null)
                {
                    _messageCollections.SelectedUserID = _selectedUser.ID;
                    RaisePropertyChanged("Collection");
                }
                MessageInput = "";
            }
        }
        public FriendRequestViewModel SelectedFriendRequest
        {
            get
            {
                return _selectedFriendRequest;
            }
            set
            {
                _selectedFriendRequest = value;
                RaisePropertyChanged("DeleteIgnore");
            }
        }
        public string NewDisplayName
        {
            get
            {
                return _newDisplayName;
            }
            set
            {
                _newDisplayName = value;
                RaisePropertyChanged("NewDisplayName");
            }
        }
        public string FriendRequestID
        {
            get
            {
                if (_friendRequestID != null)
                {
                    return ((int)_friendRequestID).ToString();
                }
                else return "";
            }
            set
            {
                int temp;
                if (int.TryParse(value, out temp))
                {
                    _friendRequestID = temp;
                    RaisePropertyChanged("FriendRequestID");
                }
            }
        }
        public string DeleteIgnore
        {
            get
            {
                if (SelectedFriendRequest != null)
                {
                    if (SelectedFriendRequest.IsSent)
                        return "Delete";
                    else return "Ignore";
                }
                else return "Ignore";
            }
        } // Not used atm? or is it... :s
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
                catch (Exception e)
                {
                    MessageInput = "CONNECTION FAILED" + e.Message;
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
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        _handler.DisplayName = tempMessage.MessageString;
                    }));
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
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        Handler.CloseConnection();
                    }));
                    break;
                case MessageCode.C009:
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        FriendRequestCollection.Add(new FriendRequestViewModel(tempMessage.senderID, _handler.myID,
                            tempMessage.MessageString, false));
                    }));
                    break;
                case MessageCode.C010:
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        SelectedUser = null;
                        Users = Handler.ParseC010Message(tempMessage.MessageString);
                    }));
                    break;
                case MessageCode.C011: // Implement Later
                    break;
                case MessageCode.C012: // Implement Later
                    break;
                case MessageCode.C013: // Implement Later
                    break;
                case MessageCode.C016: // Implement Later
                    break;
                case MessageCode.C017: // Implement Later
                    break;
                case MessageCode.C018: // Friend Request Was Accepted
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                        FriendRequestViewModel tempFR = FriendRequestCollection.Single(s => s.IDnumeric == tempMessage.senderID);
                        FriendRequestCollection.Remove(tempFR);
                        RaisePropertyChanged("FriendRequestCollection");
                        Users.Add(new User(tempFR.IDnumeric, tempFR.DisplayName));
                        SelectedFriendRequest = null;
                    }));
                    break;
                case MessageCode.C020:
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                    for (int i = 0; i < FriendRequestCollection.Count; i++)
                        {
                            if (FriendRequestCollection[i].IsSent &&
                                FriendRequestCollection[i].DisplayName == "_" &&
                                FriendRequestCollection[i].IDnumeric == tempMessage.senderID)
                            {
                                FriendRequestCollection[i].DisplayName = tempMessage.MessageString;
                                break;
                            }
                        }
                    }));
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
                Handler.myID = int.Parse(MessageInput);
                MessageInput = "";
                return;
            }


            if (_messageCollections.Collection == null || string.IsNullOrWhiteSpace(MessageInput) || _selectedUser == null)
                return;
            Message m = new Message(MessageCode.C003, _handler.myID, _selectedUser.ID, string.Format(MessageInput));
            _messageCollections.Add(m, _handler.myID);
            if (m.senderID != m.receiverID)
            {
                _handler.SendMessage(m); 
            }
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

        void AcceptRequest()
        {
            _handler.SendMessage(new Message(MessageCode.C018, _handler.myID, SelectedFriendRequest.IDnumeric, string.Empty));
            Users.Add(new User(SelectedFriendRequest.IDnumeric, SelectedFriendRequest.DisplayName));
            FriendRequestCollection.Remove(SelectedFriendRequest);
            RaisePropertyChanged("FriendRequestCollection");
            SelectedFriendRequest = null;
        }
        bool CanAcceptRequest()
        {
            if (_handler.Connected && SelectedFriendRequest != null)
            {
                return !SelectedFriendRequest.IsSent;
            }
            else return false;
        }
        public ICommand AcceptRequestCommand { get { return new RelayCommand(AcceptRequest, CanAcceptRequest); } }

        void IgnoreRequest()
        {
            if (!SelectedFriendRequest.IsSent)
            {
                FriendRequestCollection.Remove(SelectedFriendRequest);
                _handler.SendMessage(new Message(MessageCode.C019, _handler.myID, SelectedFriendRequest.IDnumeric,
                    string.Empty));
            }
        }
        bool CanIgnoreRequest()
        {
            if (_handler.Connected && SelectedFriendRequest != null && !SelectedFriendRequest.IsSent)
                return true;
            else return false;
        }
        public ICommand IgnoreRequestCommand { get { return new RelayCommand(IgnoreRequest, CanIgnoreRequest); } }

        void ChangeDisplayName()
        {
            if (!_handler.Connected)
            {
                _handler.DisplayName = NewDisplayName;
                NewDisplayName = string.Empty;
            }
            else
            {
                _handler.SendMessage(new Message(MessageCode.C001, _handler.myID, 0, NewDisplayName));
                NewDisplayName = string.Empty;
            }
        }
        bool CanChangeDisplayName()
        {
            return !string.IsNullOrWhiteSpace(NewDisplayName);
        }
        public ICommand ChangeDisplayNameCommand { get { return new RelayCommand(ChangeDisplayName, CanChangeDisplayName); } }

        void SendFriendRequest()
        {
            _handler.SendMessage(new Message(MessageCode.C009, _handler.myID, (int)_friendRequestID, _handler.DisplayName));
            FriendRequestCollection.Add(new FriendRequestViewModel(_handler.myID, (int)_friendRequestID, "_", true));
        }
        bool CanSendFriendRequest()
        {
            if (_handler.Connected && _friendRequestID != null)
            {
                return true;
            }
            else return false;
        }
        public ICommand SendFriendRequestCommand { get { return new RelayCommand(SendFriendRequest, CanSendFriendRequest); } }
        #endregion
    }
}
