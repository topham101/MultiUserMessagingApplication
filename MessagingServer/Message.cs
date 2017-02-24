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
        C003 = 3,
        C004 = 4,
        C005 = 5
    }
    public class Message
    {
        public Message(MessageCode _Code, int senderID, int receiverID, string _MessageString)
        {
            Code = _Code;
            this.senderID = senderID;
            this.receiverID = receiverID;
            MessageString = _MessageString;
            createdTimeStamp = DateTime.Now.ToFileTime();
        }

        private Message(MessageCode _Code, int senderID, int receiverID,
            string _MessageString, long Time)
        {
            Code = _Code;
            this.senderID = senderID;
            this.receiverID = receiverID;
            MessageString = _MessageString;
            createdTimeStamp = Time;
        }

        public long createdTimeStamp { get; private set; }
        public MessageCode Code { get; set; }
        public int senderID { get; private set; }
        public int receiverID { get; private set; }

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
            return string.Format("~~{0}\r\n{1}{2}{3}\r\n\r\n##{4}~~",
                ((int)Code).ToString("D3"), senderID.ToString("D4"),
                receiverID.ToString("D4"), MessageString, createdTimeStamp);//FINISH
        }

        public static Message InterpretString(string streamData) // Add better validation later
        {
            while (streamData.EndsWith("\r\n"))
                streamData = streamData.Substring(0, streamData.Length-2);
            if (string.IsNullOrEmpty(streamData))
                throw new Exception("BAD MESSAGE RECEIVED");
            
            // Extract Error Code
            int errorCodeNum;
            if (!int.TryParse(streamData.Substring(2, 3), out errorCodeNum))
                throw new Exception("BAD MESSAGE RECEIVED");
            MessageCode code = (MessageCode)errorCodeNum;

            // Extract Sender ID
            int messageStart = streamData.IndexOf("\r\n") + 2;
            int mesSenderID;
            if (!int.TryParse(streamData.Substring(messageStart, 4), out mesSenderID))
                throw new Exception("BAD MESSAGE RECEIVED");

            // Extract Receiver ID
            int receiverID;
            if (!int.TryParse(streamData.Substring(messageStart + 4, 4), out receiverID))
                throw new Exception("BAD MESSAGE RECEIVED");

            // Extract Message
            messageStart += 8;
            int messageEnd = streamData.LastIndexOf("\r\n\r\n##");
            string receivedMessage = streamData.Substring(messageStart,
                messageEnd - messageStart);

            // Extract File Time
            messageEnd += 6;
            string FileTime = streamData.Substring(messageEnd,
                (streamData.Length - messageEnd) - 2);
            long FileTimeVal;
            if (!long.TryParse(FileTime, out FileTimeVal))
                throw new Exception("BAD MESSAGE RECEIVED");


            return new Message(code, mesSenderID, receiverID, receivedMessage, FileTimeVal);
        }
    }
}
