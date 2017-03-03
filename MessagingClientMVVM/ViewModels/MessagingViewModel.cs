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
                            Message temp = Message.InterpretString(nextMessage);
                            if (temp.Code == MessageCode.C003)
                            {
                                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                                    _messageCollections.Add(temp);
                                })); 
                            }
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
        #endregion

        #region Commands
        void AddMessageExecute()
        {
            if (_messageCollections.Collection == null || string.IsNullOrWhiteSpace(MessageInput))
                return;
            Message m = new Message(MessageCode.C003, 1, 2, string.Format(MessageInput));
            _handler.SendMessage(m);
            _messageCollections.Add(m);
            MessageInput = "";
        }
        bool CanAddMessageExecute()
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
