using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using MessagingClientMVVM.ViewModels;
using MessagingServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MessagingClientMVVM
{
    class MessagingViewModel : ObservableObject
    {
        #region Members
        ObservableCollection<User> _users = new ObservableCollection<User>();
        MessageCollectionViewModel _messageCollections = new MessageCollectionViewModel();
        User _selectedUser;
        string _messageInput;
        //int count = 0;
        #endregion

        #region Constructors
        public MessagingViewModel()
        {
            _users.Add(new User(0, "Server"));
            _users.Add(new User(1, "Test User"));
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
                //count++;
                //_messageCollections.Collection.Add(new Message(MessageCode.C003, 0000, 0001, string.Format("{0} Test Message One", count)));
            }
        }
        #endregion

        #region Commands
        void AddMessageExecute()
        {
            if (_messageCollections.Collection == null)
                return;

            _messageCollections.Collection.Add(new Message(MessageCode.C003, 1, 0, string.Format(MessageInput)));
        }
        bool CanAddMessageExecute()
        {
            return true;
        }

        public ICommand AddMessages { get { return new RelayCommand(AddMessageExecute, CanAddMessageExecute); } }
        #endregion
    }
}
