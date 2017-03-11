using MessagingClientMVVM.MicroMVVM;
using MessagingClientMVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.ViewModels
{
    class FriendRequestViewModel : ObservableObject
    {
        private FriendRequest _fr;

        public FriendRequestViewModel(int _senderID, int _receiverID, string _displayName, bool _isSent)
        {
            _fr = new FriendRequest(_senderID, _receiverID, _displayName, _isSent);
        }

        public FriendRequest Fr
        {
            get
            {
                return _fr;
            }
            set
            {
                _fr = value;
                RaisePropertyChanged("Fr");
            }
        }

        public string ID
        {
            get
            {
                if (_fr.IsSent)
                    return '#' + _fr.ReceiverID.ToString("D4");
                else return '#' + _fr.SenderID.ToString("D4");
            }
        }
        public int IDnumeric
        {
            get
            {
                if (_fr.IsSent)
                    return _fr.ReceiverID;
                else return _fr.SenderID;
            }
        }
        public bool IsSent
        {
            get
            {
                return _fr.IsSent;
            }
            set
            {
                _fr.IsSent = value;
                RaisePropertyChanged("IsSent");
                RaisePropertyChanged("IsSentStr");
                RaisePropertyChanged("IDnumeric");
                RaisePropertyChanged("ID");
            }
        }
        public string DisplayName
        {
            get
            {
                return _fr.DisplayName;
            }
            set
            {
                _fr.DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        public string IsSentStr
        {
            get
            {
                if (_fr.IsSent)
                    return "Sent";
                else return "Received";
            }
        }
    }
}
