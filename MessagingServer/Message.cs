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

        private string messageString;
        public string MessageString
        {
            get { return messageString; }
            set {
                if (!string.IsNullOrWhiteSpace(value) && !value.Contains("\r\n\r\n##"))
                    messageString = value;
                else messageString = string.Empty;
            }
        }
        public string generateMessage()
        {
            return string.Format("~~{0}\r\n{1}\r\n\r\n##", 
                (int)Code, MessageString);//FINISH
        }
    }
}
