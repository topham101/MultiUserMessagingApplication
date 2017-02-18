using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServer
{
    public enum MessageCode
    {
        C001 = 1,
        C002 = 2
    }
    public class Message
    {
        public Message(MessageCode _Code, string _MessageString)
        {
            Code = _Code;
            MessageString = _MessageString;
        }
        public MessageCode Code { get; set; }
        public string MessageString
        {
            get { return MessageString; }
            set {
                if (!string.IsNullOrEmpty(value))
                    MessageString = value;
            }
        }
        public string generateMessage()
        {
            return string.Format("#{0}\r\n{1}\r\n\r\n##", 
                MessageCodes.MessageCodeDict[(int)Code]);//FINISH
        }
    }
}
