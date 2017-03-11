using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using MessagingServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.ViewModels
{
    public class MessageCollectionViewModel : ObservableObject
    {
        #region Members
        ConcurrentDictionary<int, MTObservableCollection<Message>> _messageCollections = new ConcurrentDictionary<int, MTObservableCollection<Message>>();
        int? _selectedUserID;
        #endregion

        #region Properties
        public int? SelectedUserID
        {
            get
            {
                return _selectedUserID;
            }
            set
            {
                _selectedUserID = value;
                RaisePropertyChanged("SelectedUserID");
                RaisePropertyChanged("Collection");
            }
        }
        public MTObservableCollection<Message> Collection
        {
            get
            {
                if (_selectedUserID == null)
                    return new MTObservableCollection<Message>();

                MTObservableCollection<Message> messageCollectionTemp;

                if (!_messageCollections.TryGetValue((int)_selectedUserID, out messageCollectionTemp))
                {
                    if (!_messageCollections.TryAdd((int)_selectedUserID, new MTObservableCollection<Message>()))
                        throw new Exception("Failed Adding New Queue");

                    if (!_messageCollections.TryGetValue((int)_selectedUserID, out messageCollectionTemp))
                        throw new Exception("FAILED ADDING NEW MESSAGE-COLLECTION");

                    RaisePropertyChanged("Collection");
                }

                return messageCollectionTemp;
            }
        }
        #endregion

        #region Methods
        public MTObservableCollection<Message> GetCollection(int clientID)
        {
            MTObservableCollection<Message> messageCollectionTemp;
            if (!_messageCollections.TryGetValue(clientID, out messageCollectionTemp))
            {
                if (!_messageCollections.TryAdd(clientID, new MTObservableCollection<Message>()))
                    throw new Exception("Failed Adding New Queue");
                if (!_messageCollections.TryGetValue(clientID, out messageCollectionTemp))
                    throw new Exception("FAILED ADDING NEW MESSAGE-COLLECTION");
            }
            return messageCollectionTemp;
        }
        public void Add(Message message, int CurrentLoginID)
        {
            if (CurrentLoginID != message.senderID)
            {
                GetCollection(message.senderID).Add(message);
                if (message.senderID == _selectedUserID)
                    RaisePropertyChanged("Collection");
            }
            else
            {
                GetCollection(message.receiverID).Add(message);
                if (message.receiverID == _selectedUserID)
                    RaisePropertyChanged("Collection");
            }
        }
        public void RemoveByTimeStamp(Message message)
        {
            Message foundMessage = GetCollection(message.senderID).Where(Message => Message.createdTimeStamp.ToString() == message.MessageString).First();
            GetCollection(message.senderID).Remove(foundMessage);
            if (message.senderID == _selectedUserID)
                RaisePropertyChanged("Collection");
        }
        #endregion
    }
}
