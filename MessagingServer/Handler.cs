using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagingServer
{
    public class Handler
    {
        private const int PollRateMS = 500;
        private StreamReader sr;
        private StreamWriter sw;

        public void beginHandle(Socket connection)
        {
            NetworkStream socketStream = new NetworkStream(connection);
            sr = new StreamReader(socketStream);
            sw = new StreamWriter(socketStream);
            string ip = connection.RemoteEndPoint.ToString();

            // Send connection info and data ~ contacts list

            // Test client for response
            if (connectionWorking())
            {
                // start socket poll
                socketPoll(connection);
            }

            // Close Thread
            sw.Close();
            sr.Close();
            socketStream.Close();
            connection.Close();
        }

        public void socketPoll(Socket connection)
        {
            while (connection.IsConnected())
            {
                // Check for messages
                string message = readStream();
                if (!string.IsNullOrEmpty(message))
                    messageHandler(message); // Handle Messages

                // Wait
                Thread.Sleep(PollRateMS);
            }
            return;
        }

        public void messageHandler(string recMessage)
        {
            // handle messages - send return update
        }

        public bool sendMessage(Message message)
        {
            try
            {
                sw.WriteLine(message.generateMessage());
                sw.Flush();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool connectionWorking()
        {
            if (sendMessage(new Message(MessageCode.C001, string.Empty))
                && readAndInterpretMessage().Code == MessageCode.C002)
                return true;
            else
            {
                Thread.Sleep(300);
                if (readStream() == "Connection Tested")
                    return true;
            }
            return false;
        }

        public Message readAndInterpretMessage() // Add better validation later
        {
            string streamData = readStream();
            if (string.IsNullOrEmpty(streamData))
                throw new Exception("BAD MESSAGE RECEIVED");

            // Extract Error Code
            int errorCodeNum;
            string errorCodeStr = streamData.Substring(2-4);
            if (!int.TryParse(streamData.Substring(2 - 4), out errorCodeNum))
                throw new Exception("BAD MESSAGE RECEIVED");
            MessageCode code = (MessageCode)errorCodeNum;

            // Extract Message
            int messageStart = streamData.IndexOf("\r\n");
            int messageEnd = streamData.LastIndexOf("\r\n\r\n##");
            string receivedMessage = streamData.Substring(messageStart,
                messageEnd - messageStart);

            return new Message(code, receivedMessage);
        }

        public string readStream() // IMPROVE LATER to work with bad messages better
        {
            string fullInput = "";
            while (sr.Peek() >= 0)
            {
                string tempstring = sr.ReadLine();
                if (tempstring.StartsWith("~~") || fullInput.StartsWith("~~"))
                {
                    fullInput += tempstring;
                    if (tempstring.EndsWith("##"))
                        return fullInput;
                    fullInput += "\r\n";
                }
            }
            return null;
        }
    }
}
