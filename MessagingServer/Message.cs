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
        C002 = 2,
        C003 = 3
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
                ((int)Code).ToString("D3"), MessageString);//FINISH
        }

        public static Message InterpretString(string streamData) // Add better validation later
        {
            if (string.IsNullOrEmpty(streamData))
                throw new Exception("BAD MESSAGE RECEIVED");

            // Extract Error Code
            int errorCodeNum;
            if (!int.TryParse(streamData.Substring(2, 3), out errorCodeNum))
                throw new Exception("BAD MESSAGE RECEIVED");
            MessageCode code = (MessageCode)errorCodeNum;

            // Extract Message
            int messageStart = streamData.IndexOf("\r\n");
            int messageEnd = streamData.LastIndexOf("\r\n\r\n##");
            string receivedMessage = streamData.Substring(messageStart,
                messageEnd - messageStart);

            return new Message(code, receivedMessage);
        }
    }
}
