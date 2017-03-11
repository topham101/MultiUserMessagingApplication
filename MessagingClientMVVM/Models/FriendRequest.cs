using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.Models
{
    public class FriendRequest
    {
        public FriendRequest(int _senderID, int _receiverID, string _displayName, bool _isSent)
        {
            SenderID = _senderID;
            ReceiverID = _receiverID;
            DisplayName = _displayName;
            IsSent = _isSent;
        }

        public int SenderID { get; private set; }
        public int ReceiverID { get; private set; }
        public bool IsSent { get; set; }
        public string DisplayName { get; set; }
    }
}
