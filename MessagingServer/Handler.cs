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
                Console.WriteLine("Connection Working: " + ip);
                // start socket poll
                socketPoll(connection);
            }

            // Close Thread
            sw.Close();
            sr.Close();
            socketStream.Close();
            connection.Close();
            Console.WriteLine("Connection Closed: " + ip);
        }

        private void socketPoll(Socket connection)
        {
            while (connection.IsConnected())
            {
                // Check for messages
                try
                {
                    if (sr.Peek() >= 0) // CHANGE to read until false and use out value
                    {
                        string nextMessage = string.Empty;
                        while (readNextMessage(out nextMessage))
                        {
                            messageHandler(Message.InterpretString(nextMessage)); // Handle Messages 
                            nextMessage = string.Empty;
                        }
                    }

                }
                catch
                {
                    Console.WriteLine("PEEK ERROR");
                    return;
                    throw;
                }
                // Wait
                Thread.Sleep(PollRateMS);
            }
            return;
        }

        private void messageHandler(Message recMessage) // amend later
        {
            Console.WriteLine("MESSAGE RECEIVED: " + recMessage.Code.ToString() + recMessage.MessageString);
        }

        private bool sendMessage(Message message)
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

        private bool connectionWorking()
        {
            string response;
            if (sendMessage(new Message(MessageCode.C001, string.Empty)))
            {
                if (readNextMessage(out response)
                    && Message.InterpretString(response).Code == MessageCode.C002)
                    return true;
            }
            else
            {
                Thread.Sleep(300);
                if (readNextMessage(out response)
                    && Message.InterpretString(response).Code == MessageCode.C002)
                    return true;
            }
            return false;
        }

        private bool readNextMessage(out string streamData) // IMPROVE LATER to work with bad messages better
        {
            streamData = string.Empty;
            try
            {
                string fullInput = "";
                while (sr.Peek() >= 0)
                {
                    string tempstring = sr.ReadLine();
                    if (tempstring.StartsWith("~~") || fullInput.StartsWith("~~"))
                    {
                        fullInput += tempstring;
                        if (tempstring.EndsWith("##"))
                        {
                            streamData = fullInput;
                            return true;
                        }
                        fullInput += "\r\n";
                    }
                }
            }
            catch{}
            finally
            {
                sr.DiscardBufferedData();
            }
            return false;
        }
    }
}
