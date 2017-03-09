using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.Models
{
    public class FriendRequest
    {
        private int SenderID;
        private int ReceiverID;
        public FriendRequest(int _senderID, int _receiverID, string _displayName, bool _isSent)
        {
            SenderID = _senderID;
            ReceiverID = _receiverID;
            DisplayName = _displayName;
            IsSent = _isSent;
        }
        public string ID
        {
            get
            {
                if (IsSent)
                    return '#' + ReceiverID.ToString("D4");
                else return '#' + SenderID.ToString("D4");
            }
        }
        public int IDnumeric
        {
            get
            {
                if (IsSent)
                    return ReceiverID;
                else return SenderID;
            }
        }
        public bool IsSent { get; set; }
        public string DisplayName { get; set; }
        public string IsSentStr
        {
            get
            {
                if (IsSent)
                    return "Sent";
                else return "Received";
            }
        }
    }
}
