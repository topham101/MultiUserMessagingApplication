using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using MessagingServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.ViewModels
{
    class MessageCollectionViewModel : ObservableObject
    {
        #region Members
        private Dictionary<int, ObservableCollection<Message>> _messageCollections = new Dictionary<int, ObservableCollection<Message>>();
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
        public ObservableCollection<Message> Collection
        {
            get
            {
                if (_selectedUserID == null)
                    return new ObservableCollection<Message>();
                ObservableCollection<Message> messageCollectionTemp;
                if (!_messageCollections.TryGetValue((int)_selectedUserID, out messageCollectionTemp))
                {
                    _messageCollections.Add((int)_selectedUserID, new ObservableCollection<Message>());
                    if (!_messageCollections.TryGetValue((int)_selectedUserID, out messageCollectionTemp))
                        throw new Exception("FAILED ADDING NEW MESSAGE COLLECTION");
                    RaisePropertyChanged("Collection");
                }
                return messageCollectionTemp;
            }
        }
        #endregion
    }
}
