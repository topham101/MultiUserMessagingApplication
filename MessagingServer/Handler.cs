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
            // handle messages - sent return update
        }

        public bool sendMessage(string message)
        {
            try
            {
                sw.WriteLine(message + '#');
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
            if (sendMessage("001. Connection Test Request") && readStream() == "002. Connection Test Confirmation")
                return true;
            else
            {
                Thread.Sleep(300);
                if (readStream() == "Connection Tested")
                return true;
            }
            return false;
        }

        public string readStream()
        {
            string fullInput = "";
            while (sr.Peek() >= 0)
                fullInput += sr.ReadLine() + "\r\n";
            return fullInput;
        }
    }
}
